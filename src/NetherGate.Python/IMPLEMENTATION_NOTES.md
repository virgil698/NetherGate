# NetherGate.Python 实现说明

## 🏗️ 架构概览

NetherGate.Python 使用 **Python.NET (pythonnet)** 库实现 C# 与 Python 的互操作。

### 核心组件

```
┌─────────────────────────────────────┐
│     NetherGate.Core                 │
│     (PluginManager)                 │
└────────────┬────────────────────────┘
             │
             ↓
┌─────────────────────────────────────┐
│  NetherGate.Python                  │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  PythonRuntime                │ │
│  │  - 管理 Python 解释器         │ │
│  │  - 初始化/关闭                │ │
│  └───────────────────────────────┘ │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  PythonPluginLoader           │ │
│  │  - 扫描 Python 插件           │ │
│  │  - 安装依赖                   │ │
│  └───────────────────────────────┘ │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  PythonPluginAdapter          │ │
│  │  - 实现 IPlugin 接口          │ │
│  │  - 桥接 C# ↔ Python          │ │
│  │  - 依赖注入                   │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
             │
             ↓ Python.NET
┌─────────────────────────────────────┐
│  Python Runtime                     │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  Python Plugin (.py)          │ │
│  │  - class MyPlugin(Plugin)     │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
```

## 🔑 关键实现

### 1. PythonRuntime

**职责**: 管理 Python 解释器生命周期

```csharp
public class PythonRuntime
{
    public void Initialize()
    {
        // 初始化 Python 引擎
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
    }
    
    public void Shutdown()
    {
        PythonEngine.Shutdown();
    }
}
```

**关键点**:
- 使用 `Py.GIL()` 获取全局解释器锁
- 支持虚拟环境
- 自动安装 pip 包

### 2. PythonPluginAdapter

**职责**: 将 Python 插件适配为 IPlugin 接口

```csharp
public class PythonPluginAdapter : IPlugin
{
    private readonly PyObject _pythonInstance;
    
    public async Task OnLoadAsync()
    {
        using (Py.GIL())
        {
            await InvokePythonMethodAsync("on_load");
        }
    }
}
```

**关键实现**:

#### 依赖注入

```csharp
private PyObject CreatePythonInstance(PyObject pluginClass, IServiceProvider sp)
{
    // 1. 检查构造函数参数
    using var inspect = Py.Import("inspect");
    using var signature = inspect.InvokeMethod("signature", pluginClass);
    
    // 2. 解析每个参数
    foreach (var (name, info) in parameters)
    {
        var service = ResolveService(name, sp);
        if (service != null)
        {
            args.Add(service.ToPython());
        }
    }
    
    // 3. 调用构造函数
    return pluginClass.Invoke(args.ToArray());
}
```

#### 异步方法调用

```csharp
private async Task InvokePythonMethodAsync(string methodName)
{
    using (Py.GIL())
    {
        using var method = _pythonInstance.GetAttr(methodName);
        using var result = method.Invoke();
        
        // 检查是否是协程
        using var inspect = Py.Import("inspect");
        if (inspect.InvokeMethod("iscoroutine", result).IsTrue())
        {
            // 运行协程
            using var asyncio = Py.Import("asyncio");
            await Task.Run(() => asyncio.InvokeMethod("run", result));
        }
    }
}
```

### 3. PluginLoader 集成

**职责**: 在 NetherGate.Core 中支持多种插件类型

```csharp
private PluginContainer? ScanPlugin(string pluginDirectory)
{
    var metadata = ScanPluginMetadataFromDirectory(pluginDirectory);
    
    if (metadata.Type == "python")
    {
        // Python 插件使用目录作为"程序集路径"
        assemblyPath = pluginDirectory;
    }
    else
    {
        // C# 插件使用 DLL 路径
        assemblyPath = Path.Combine(pluginDirectory, $"{pluginName}.dll");
    }
    
    return new PluginContainer(metadata, pluginDirectory, assemblyPath, dataDirectory);
}
```

## 🔐 GIL 管理

Python 的全局解释器锁 (GIL) 需要谨慎管理：

```csharp
// ✅ 正确：短时间持有 GIL
using (Py.GIL())
{
    var result = _pythonInstance.GetAttr("some_property");
    return result.As<string>();
}

// ❌ 错误：长时间持有 GIL
using (Py.GIL())
{
    await SomeLongRunningOperation();  // 会阻塞其他线程
}
```

## 📦 依赖管理

### Python 包安装

```csharp
public void InstallPackage(string packageSpec, string? venvPath = null)
{
    string pipExecutable = GetPipExecutable(venvPath);
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = pipExecutable,
        Arguments = $"install {packageSpec}",
        // ...
    });
    process.WaitForExit();
}
```

### 虚拟环境支持

```csharp
public void CreateVirtualEnvironment(string venvPath)
{
    using (Py.GIL())
    {
        dynamic venv = Py.Import("venv");
        venv.create(venvPath, with_pip: true);
    }
}
```

## 🎯 服务解析映射

Python 参数名 → C# 服务类型：

| Python 参数名 | C# 服务类型 |
|--------------|-----------|
| `logger` | `ILogger` |
| `event_bus` | `IEventBus` |
| `rcon` | `IRconClient` |
| `scheduler` | `IScheduler` |
| `scoreboard` | `IScoreboardApi` |
| `permissions` | `IPermissionManager` |
| `player_data` | `IPlayerDataReader` |
| `websocket` | `IWebSocketServer` |

## ⚠️ 已知限制

1. **GIL 性能开销**: Python 代码运行时会持有 GIL
2. **对象生命周期**: 需要正确 Dispose PyObject
3. **类型转换**: 某些复杂类型可能无法自动转换
4. **异步限制**: Python 协程与 C# Task 的桥接有开销

## 🔄 Python.NET API 变化

本实现基于 **pythonnet 3.0.3**，关键 API：

```csharp
// 导入模块
using var module = Py.Import("module_name");

// 获取属性
using var attr = obj.GetAttr("attr_name");

// 调用方法
using var result = obj.InvokeMethod("method_name", args);

// 迭代器
using var iterator = obj.GetIterator();
while (iterator.MoveNext())
{
    using var item = iterator.Current;
    // ...
}

// 类型转换
var csharpValue = pyObject.As<string>();
var pyValue = csharpObj.ToPython();
```

## 🚀 性能优化建议

1. **最小化 GIL 持有时间**
   ```csharp
   // 准备数据（不在 GIL 内）
   var data = await PrepareDataAsync();
   
   // 快速调用 Python（在 GIL 内）
   using (Py.GIL())
   {
       _pythonInstance.InvokeMethod("process", data.ToPython());
   }
   ```

2. **批量操作**
   ```csharp
   // ✅ 批量执行
   using (Py.GIL())
   {
       var batch = commands.ToPython();
       plugin.InvokeMethod("execute_batch", batch);
   }
   
   // ❌ 逐个执行
   foreach (var cmd in commands)
   {
       using (Py.GIL())
       {
           plugin.InvokeMethod("execute", cmd.ToPython());
       }
   }
   ```

3. **对象缓存**
   ```csharp
   // 缓存常用的 Python 对象
   private static readonly Dictionary<Type, PyObject> _typeCache = new();
   ```

## 📝 待实现功能

- [ ] ConfigManager 接口实现
- [ ] 更完整的 Python SDK 文档
- [ ] 插件间通信支持
- [ ] 热重载机制完善
- [ ] 性能监控和分析工具

## 🔗 参考资源

- [Python.NET 文档](https://pythonnet.github.io/)
- [Python asyncio](https://docs.python.org/3/library/asyncio.html)
- [NetherGate 架构设计](../../docs/05-配置和部署/Python插件架构说明.md)

