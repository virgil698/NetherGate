# NetherGate 代码裁剪说明

## 为什么禁用了 Trimming？

NetherGate 的发布配置中**禁用了 .NET 代码裁剪（trimming）** 功能。本文档解释了原因和权衡。

---

## 什么是 Trimming？

代码裁剪（Trimming）是 .NET 的一个功能，它通过静态分析移除未使用的代码，从而减小发布包的大小。

### 优势
- ✅ 显著减小发布包大小（通常减少 30-70%）
- ✅ 减少内存占用
- ✅ 加快启动速度

### 劣势
- ❌ 可能意外移除反射使用的代码
- ❌ 可能破坏序列化/反序列化
- ❌ 可能移除插件系统需要的类型
- ❌ 需要大量手动配置来保留必要的类型

---

## NetherGate 不适合使用 Trimming 的原因

### 1. 插件系统大量使用反射

NetherGate 的核心功能是动态加载插件：

```csharp
// 动态加载程序集
var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

// 动态获取类型
var pluginType = assembly.GetType(metadata.EntryPoint);

// 动态创建实例
var pluginInstance = (IPlugin)Activator.CreateInstance(pluginType);

// 动态访问属性
var contextProperty = plugin.GetType().GetProperty("Context");
contextProperty.SetValue(plugin, context);
```

Trimmer **无法静态分析**这些动态行为，可能会移除插件实际需要的类型和成员。

### 2. JSON/YAML 序列化需要完整的类型信息

配置系统使用 `System.Text.Json` 和 `YamlDotNet` 进行序列化：

```csharp
// 反序列化配置
var config = JsonSerializer.Deserialize<NetherGateConfig>(json);

// 序列化插件配置
var json = JsonSerializer.Serialize(pluginConfig);
```

Trimmer 可能会移除配置类的属性，导致序列化失败或数据丢失。

### 3. 插件 API 必须完整保留

插件会调用框架提供的各种接口和类：

```csharp
// 插件调用 IPluginContext 的所有成员
context.Logger.Info("Hello");
context.EventBus.Subscribe<ServerStartedEvent>(...);
await context.PlayerDataReader.ReadPlayerDataAsync(...);
```

如果 Trimmer 移除了插件可能调用的任何接口成员，运行时会抛出 `MissingMethodException`。

### 4. 第三方库不兼容 Trimming

NetherGate 依赖的许多第三方库不支持 trimming：

- `YamlDotNet` - 大量使用反射
- `NuGet.Protocol` - 内部使用反射
- `fNbt` - NBT 解析需要反射
- `Newtonsoft.Json`（间接依赖） - 不支持 trimming

启用 trimming 会产生大量警告（IL2026, IL2075, IL2104 等）。

---

## 警告类型说明

如果启用了 trimming，您会看到以下警告：

### IL2026 - RequiresUnreferencedCodeAttribute
```
Using member 'System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)' 
which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```
**含义**：使用的 API 依赖于反射，trimming 可能破坏功能。

### IL2075 / IL2072 / IL2070 / IL2067 - DynamicallyAccessedMembers
```
'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties' 
in call to 'System.Type.GetProperty(String)'.
```
**含义**：动态访问类型成员（如 `GetProperty`、`GetMethod`），trimmer 可能移除这些成员。

### IL2104 - Assembly produced trim warnings
```
Assembly 'YamlDotNet' produced trim warnings.
```
**含义**：第三方程序集不兼容 trimming。

---

## 解决 Trimming 警告的方法（如果需要启用）

如果您坚持要启用 trimming，需要大量配置：

### 方法 1：保留根程序集

在 `NetherGate.Host.csproj` 中添加：

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="NetherGate.API" />
  <TrimmerRootAssembly Include="NetherGate.Core" />
</ItemGroup>
```

**效果**：保留 API 和 Core 的所有类型（部分解决问题，但发布包会变大）。

### 方法 2：使用 DynamicallyAccessedMembers 特性

标注所有可能被反射访问的类型：

```csharp
public class PluginLoader
{
    public IPlugin LoadPlugin([DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.PublicProperties
    )] Type pluginType)
    {
        // ...
    }
}
```

**效果**：告诉 trimmer 保留特定成员，但需要手动标注大量代码。

### 方法 3：使用 JSON Source Generator

对于 `System.Text.Json`，使用 source generation 代替反射：

```csharp
[JsonSerializable(typeof(NetherGateConfig))]
[JsonSerializable(typeof(PluginMetadata))]
// ... 所有需要序列化的类型
public partial class NetherGateJsonContext : JsonSerializerContext { }

// 使用
var config = JsonSerializer.Deserialize<NetherGateConfig>(
    json, 
    NetherGateJsonContext.Default.NetherGateConfig
);
```

**效果**：避免反射序列化，但需要显式列出所有类型。

### 方法 4：禁用特定警告（不推荐）

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);IL2026;IL2075;IL2072;IL2070;IL2067;IL2104</NoWarn>
</PropertyGroup>
```

**效果**：隐藏警告，但不解决问题，运行时仍可能出错。

---

## 当前配置

NetherGate 的发布脚本使用以下配置：

```bash
dotnet publish src/NetherGate.Host/NetherGate.Host.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output publish/win-x64 \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true
    # ❌ 不使用 -p:PublishTrimmed=true
```

### 保留的优化
- ✅ **PublishSingleFile**：将所有 DLL 打包到单个可执行文件（便于分发）
- ✅ **EnableCompressionInSingleFile**：压缩单文件内的程序集（减小约 30%）
- ✅ **self-contained**：包含 .NET 运行时（无需用户安装 .NET）

### 发布包大小对比（估计）

| 配置 | 大小 | 风险 |
|-----|------|-----|
| 不使用 Trimming（当前） | ~50-70 MB | ✅ 无风险 |
| 使用 Trimming | ~20-30 MB | ❌ 高风险，可能破坏插件系统 |
| framework-dependent | ~5-10 MB | ⚠️ 用户需要安装 .NET 9.0 |

---

## 建议

### 对于 NetherGate 开发者
1. ✅ **保持禁用 trimming**（当前设置）
2. ✅ 如果需要减小包大小，考虑改为 **framework-dependent** 发布
3. ❌ 不要轻易启用 trimming，除非愿意投入大量时间处理兼容性问题

### 对于插件开发者
1. ✅ 插件本身可以启用 trimming（因为插件不需要动态加载其他插件）
2. ⚠️ 如果插件使用大量反射，也应该禁用 trimming
3. ✅ 优先使用 `PublishSingleFile` 来简化插件分发

---

## 参考资料

- [.NET Trimming Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/)
- [Trim warnings (IL2026, IL20xx)](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-warnings)
- [Prepare .NET libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
- [System.Text.Json source generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)

---

## 总结

**NetherGate 是一个插件加载器，核心功能依赖于动态加载和反射，因此不适合使用 trimming。**

当前配置在**功能稳定性**和**合理的发布包大小**之间取得了最佳平衡。
