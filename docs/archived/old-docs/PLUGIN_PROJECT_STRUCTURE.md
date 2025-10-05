# NetherGate 插件项目结构指南

本文档说明推荐的插件项目目录结构，兼顾 Java 开发者习惯和 .NET 最佳实践。

---

## 📋 概述

为了让来自 Bukkit/Spigot/Paper 的 Java 开发者更容易上手，NetherGate 插件项目结构借鉴了 Maven/Gradle 的目录布局，同时利用 .NET 的强大特性。

---

## 📁 推荐项目结构

### 标准结构（类似 Maven）

```
MyPlugin/                           # 插件根目录
├── MyPlugin.csproj                 # 项目文件
├── README.md                       # 插件说明
│
├── src/                            # 源代码目录 (类似 src/main/java)
│   ├── MyPlugin.cs                 # 主类
│   ├── Commands/                   # 命令处理
│   │   ├── HelloCommand.cs
│   │   └── InfoCommand.cs
│   ├── Events/                     # 事件监听器
│   │   ├── PlayerJoinListener.cs
│   │   └── PlayerQuitListener.cs
│   ├── Services/                   # 业务逻辑
│   │   └── DatabaseService.cs
│   └── Models/                     # 数据模型
│       └── PlayerData.cs
│
├── resources/                      # 资源文件目录 (类似 src/main/resources)
│   ├── plugin.json                 # 插件元数据（必需）
│   ├── config.json                 # 默认配置模板（JSON 或 YAML）
│   ├── config.yaml                 # 默认配置模板（YAML 格式，可选）
│   ├── lang/                       # 多语言文件
│   │   ├── en_US.json
│   │   ├── zh_CN.json
│   │   └── ja_JP.json
│   └── data/                       # 静态数据文件
│       └── default-settings.json
│
└── tests/                          # 单元测试 (类似 src/test/java)
    ├── MyPluginTests.cs
    └── CommandTests.cs
```

---

## 🔧 项目文件配置

### MyPlugin.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- 复制所有依赖到输出目录 -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    
    <!-- 输出路径 -->
    <OutputPath>bin\$(Configuration)\</OutputPath>
  </PropertyGroup>

  <!-- NetherGate API 依赖 -->
  <ItemGroup>
    <PackageReference Include="NetherGate.API" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <ExcludeAssets>runtime</ExcluateAssets>
    </PackageReference>
  </ItemGroup>

  <!-- 外部依赖（示例） -->
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>

  <!-- 资源文件处理 -->
  <ItemGroup>
    <!-- plugin.json 必须复制到输出目录根 -->
    <None Include="resources\plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>plugin.json</Link>
    </None>
    
    <!-- 默认配置模板（可选，用于首次生成） -->
    <None Include="resources\config.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>config.default.json</Link>
    </None>
    
    <!-- 多语言文件：复制到 lang/ 子目录 -->
    <None Include="resources\lang\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>lang\%(RecursiveDir)%(Filename)%(Extension)</Link>
    </None>
    
    <!-- 其他数据文件：复制到 data/ 子目录 -->
    <None Include="resources\data\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>data\%(RecursiveDir)%(Filename)%(Extension)</Link>
    </None>
  </ItemGroup>

  <!-- 源代码组织（仅为 IDE 显示，不影响编译） -->
  <ItemGroup>
    <Compile Include="src\**\*.cs" />
  </ItemGroup>

</Project>
```

---

## 📦 编译输出结构

编译后的输出目录结构（`bin/Release/net9.0/`）：

```
MyPlugin/
├── MyPlugin.dll                    # 插件主 DLL
├── Newtonsoft.Json.dll             # 依赖 DLL
├── plugin.json                     # 插件元数据
├── config.default.json             # 默认配置模板（可选）
├── lang/                           # 语言文件
│   ├── en_US.json
│   ├── zh_CN.json
│   └── ja_JP.json
└── data/                           # 数据文件
    └── default-settings.json
```

部署时，整个输出目录复制到 NetherGate 的 `plugins/my-plugin/` 即可。

---

## 🌐 多语言支持示例

### resources/lang/zh_CN.json

```json
{
    "commands": {
        "hello": {
            "success": "你好，{player}！",
            "no_permission": "你没有权限执行此命令"
        }
    },
    "events": {
        "player_join": "欢迎 {player} 加入服务器！",
        "player_quit": "{player} 离开了服务器"
    }
}
```

### 在代码中使用

```csharp
public class MyPlugin : PluginBase
{
    private LanguageManager _lang = null!;
    
    public override async Task OnEnableAsync()
    {
        // 加载语言文件
        _lang = new LanguageManager(DataDirectory, "zh_CN");
        
        Logger.Info(_lang.Get("plugin.loaded"));
    }
}

public class LanguageManager
{
    private Dictionary<string, object> _messages = new();
    
    public LanguageManager(string dataDir, string locale)
    {
        // 从 lang/{locale}.json 加载
        var langFile = Path.Combine(dataDir, "lang", $"{locale}.json");
        if (File.Exists(langFile))
        {
            var json = File.ReadAllText(langFile);
            _messages = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        }
    }
    
    public string Get(string key, Dictionary<string, string>? vars = null)
    {
        var message = GetNested(key);
        if (vars != null)
        {
            foreach (var (k, v) in vars)
            {
                message = message.Replace($"{{{k}}}", v);
            }
        }
        return message;
    }
    
    private string GetNested(string key)
    {
        var parts = key.Split('.');
        object? current = _messages;
        
        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict && dict.TryGetValue(part, out var next))
            {
                current = next;
            }
            else
            {
                return key; // 找不到则返回 key 本身
            }
        }
        
        return current?.ToString() ?? key;
    }
}
```

---

## 🔄 .NET 特性：嵌入资源

### 方式 1: 复制到输出目录（推荐）

如上所示，使用 `<CopyToOutputDirectory>` 标签。

**优点**：
- ✅ 文件独立，可以手动编辑
- ✅ 更新无需重新编译
- ✅ 类似 Java 的 resources 行为

**缺点**：
- ❌ 文件可能丢失或被修改

---

### 方式 2: 嵌入到 DLL 中

```xml
<ItemGroup>
  <!-- 嵌入到 DLL 中 -->
  <EmbeddedResource Include="resources\lang\**\*.json" />
</ItemGroup>
```

**读取嵌入资源**：

```csharp
public class ResourceHelper
{
    public static string ReadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullName = $"MyPlugin.resources.lang.{resourceName}";
        
        using var stream = assembly.GetManifestResourceStream(fullName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

// 使用
var enLang = ResourceHelper.ReadEmbeddedResource("en_US.json");
```

**优点**：
- ✅ 文件打包在 DLL 中，不会丢失
- ✅ 单个 DLL 包含所有资源

**缺点**：
- ❌ 无法手动编辑
- ❌ 更新需要重新编译

---

### 推荐策略：混合使用

```xml
<ItemGroup>
  <!-- plugin.json 必须复制（NetherGate 需要读取） -->
  <None Include="resources\plugin.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>plugin.json</Link>
  </None>
  
  <!-- 默认语言文件嵌入到 DLL（备用） -->
  <EmbeddedResource Include="resources\lang\en_US.json" />
  
  <!-- 其他语言文件复制（允许用户自定义） -->
  <None Include="resources\lang\zh_CN.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>lang\zh_CN.json</Link>
  </None>
</ItemGroup>
```

---

## 🎯 Java 开发者对比

### Maven 项目结构

```
my-plugin/
├── pom.xml
└── src/
    └── main/
        ├── java/
        │   └── com/example/myplugin/
        │       ├── MyPlugin.java
        │       └── commands/
        └── resources/
            ├── plugin.yml
            └── config.yml
```

### NetherGate 插件结构

```
MyPlugin/
├── MyPlugin.csproj              # 类似 pom.xml
└── src/                         # 类似 src/main/java
    ├── MyPlugin.cs              # 类似 MyPlugin.java
    └── Commands/
├── resources/                   # 类似 src/main/resources
    ├── plugin.json              # 类似 plugin.yml
    └── config.json              # 类似 config.yml
```

### 对比表

| 功能 | Java/Bukkit | NetherGate (.NET) |
|------|-------------|-------------------|
| **项目文件** | `pom.xml` / `build.gradle` | `*.csproj` |
| **源代码** | `src/main/java/` | `src/` |
| **资源文件** | `src/main/resources/` | `resources/` |
| **插件元数据** | `plugin.yml` | `plugin.json` |
| **配置文件** | `config.yml` | `config.json` |
| **构建输出** | `target/` | `bin/` |
| **依赖管理** | Maven Central / Gradle | NuGet |
| **打包格式** | `.jar` | `.dll` |

---

## 🛠️ 快速开始脚本

### 创建新插件项目

```bash
#!/bin/bash
# create-plugin.sh

PLUGIN_NAME=$1

if [ -z "$PLUGIN_NAME" ]; then
    echo "Usage: ./create-plugin.sh <PluginName>"
    exit 1
fi

echo "Creating plugin project: $PLUGIN_NAME"

# 创建目录结构
mkdir -p $PLUGIN_NAME/src/{Commands,Events,Services,Models}
mkdir -p $PLUGIN_NAME/resources/{lang,data}
mkdir -p $PLUGIN_NAME/tests

# 创建 .csproj 文件
cat > $PLUGIN_NAME/$PLUGIN_NAME.csproj << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NetherGate.API" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <None Include="resources\plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>plugin.json</Link>
    </None>
    <None Include="resources\lang\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>lang\%(RecursiveDir)%(Filename)%(Extension)</Link>
    </None>
  </ItemGroup>

  <ItemGroup>
    <Compile Include="src\**\*.cs" />
  </ItemGroup>
</Project>
EOF

# 创建主类
cat > $PLUGIN_NAME/src/$PLUGIN_NAME.cs << EOF
using NetherGate.API;

namespace $PLUGIN_NAME;

public class $PLUGIN_NAME : PluginBase
{
    public override async Task OnEnableAsync()
    {
        Logger.Info("$PLUGIN_NAME is starting...");
        // TODO: 初始化插件
    }

    public override async Task OnDisableAsync()
    {
        Logger.Info("$PLUGIN_NAME is stopping...");
        // TODO: 清理资源
    }
}
EOF

# 创建 plugin.json
cat > $PLUGIN_NAME/resources/plugin.json << EOF
{
    "id": "$(echo $PLUGIN_NAME | tr '[:upper:]' '[:lower:]')",
    "name": "$PLUGIN_NAME",
    "version": "1.0.0",
    "author": "YourName",
    "description": "A NetherGate plugin",
    "main": "$PLUGIN_NAME.dll"
}
EOF

# 创建默认语言文件
cat > $PLUGIN_NAME/resources/lang/en_US.json << EOF
{
    "plugin": {
        "loaded": "$PLUGIN_NAME has been loaded!",
        "unloaded": "$PLUGIN_NAME has been unloaded!"
    }
}
EOF

# 创建 README
cat > $PLUGIN_NAME/README.md << EOF
# $PLUGIN_NAME

A plugin for NetherGate.

## Features

- TODO: List features

## Installation

1. Build the project: \`dotnet build -c Release\`
2. Copy \`bin/Release/net9.0/\` contents to NetherGate's \`plugins/$PLUGIN_NAME/\`

## Configuration

Edit `config/$PLUGIN_NAME/config.json` (or `config.yaml`) in your NetherGate installation.

Supports both JSON and YAML formats - choose whichever you prefer!

## License

MIT
EOF

echo "Plugin project created successfully!"
echo "Navigate to: cd $PLUGIN_NAME"
echo "Build: dotnet build"
```

### PowerShell 版本

```powershell
# create-plugin.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$PluginName
)

Write-Host "Creating plugin project: $PluginName"

# 创建目录结构
New-Item -ItemType Directory -Force -Path "$PluginName\src\Commands" | Out-Null
New-Item -ItemType Directory -Force -Path "$PluginName\src\Events" | Out-Null
New-Item -ItemType Directory -Force -Path "$PluginName\resources\lang" | Out-Null

# ... (内容类似 Bash 版本)

Write-Host "Plugin project created successfully!"
```

---

## 📄 配置文件格式支持

NetherGate 插件配置系统支持 **JSON** 和 **YAML** 两种格式，可根据个人喜好选择。

### JSON 配置示例

```json
{
  "enabled": true,
  "max_players": 100,
  "message": "Welcome!",
  "features": {
    "teleport": {
      "enabled": true,
      "cooldown": 10
    }
  },
  "allowed_commands": [
    "tp",
    "give"
  ]
}
```

### YAML 配置示例

```yaml
# 插件配置文件
enabled: true
max_players: 100
message: "Welcome!"

# 功能配置
features:
  teleport:
    enabled: true
    cooldown: 10

# 允许的命令列表
allowed_commands:
  - tp
  - give
```

### 使用配置文件

配置文件会被自动识别（根据扩展名 `.json` 或 `.yaml`/`.yml`）：

```csharp
public class MyPlugin : PluginBase
{
    private MyConfig _config = null!;
    
    public override async Task OnLoadAsync()
    {
        // 加载 JSON 配置
        _config = await Config.LoadConfigAsync<MyConfig>("config.json");
        
        // 或加载 YAML 配置
        _config = await Config.LoadConfigAsync<MyConfig>("config.yaml");
        
        Logger.Info($"Max players: {_config.MaxPlayers}");
    }
    
    public override async Task OnDisableAsync()
    {
        // 保存配置（保持原格式）
        await Config.SaveConfigAsync(_config, "config.yaml");
    }
}

public class MyConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxPlayers { get; set; } = 100;
    public string Message { get; set; } = "Welcome!";
    public Dictionary<string, FeatureConfig> Features { get; set; } = new();
    public List<string> AllowedCommands { get; set; } = new();
}

public class FeatureConfig
{
    public bool Enabled { get; set; }
    public int Cooldown { get; set; }
}
```

### 格式选择建议

| 特性 | JSON | YAML |
|------|------|------|
| **可读性** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **注释支持** | ❌ (仅部分解析器) | ✅ |
| **多行文本** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **编辑器支持** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **学习曲线** | 低 | 中 |

**推荐**：
- 简单配置使用 JSON
- 需要注释和复杂结构使用 YAML
- 可以为插件同时提供两种格式的默认配置

### 配置文件示例

参考文档中的示例文件：
- [config.example.json](config.example.json) - JSON 格式示例
- [config.example.yaml](config.example.yaml) - YAML 格式示例

---

## 📝 最佳实践

### 1. 目录组织

✅ **推荐**：
```
src/
├── Commands/           # 按功能分类
├── Events/
├── Services/
└── Models/
```

❌ **不推荐**：
```
src/
├── File1.cs           # 所有文件堆在一起
├── File2.cs
└── File3.cs
```

### 2. 资源文件

✅ **推荐**：
- 使用 `resources/` 目录
- 配置 `<CopyToOutputDirectory>`
- 保持结构清晰

❌ **不推荐**：
- 资源文件和代码混在一起
- 硬编码资源路径

### 3. 命名空间

✅ **推荐**：
```csharp
namespace MyPlugin;              // 文件范围命名空间（C# 10+）
namespace MyPlugin.Commands;     // 子命名空间
```

❌ **不推荐**：
```csharp
namespace Plugin1 { }            // 太通用
namespace com.example.plugin { } // Java 风格（不符合 C# 习惯）
```

---

## 🔗 相关文档

- [插件依赖管理](PLUGIN_DEPENDENCIES.md)
- [API 设计](API_DESIGN.md)
- [示例插件项目](SAMPLES_PROJECT.md)

---

**更新日期**: 2025-10-04

