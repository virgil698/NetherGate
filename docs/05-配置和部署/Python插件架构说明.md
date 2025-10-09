# Python 插件架构说明

本文档介绍 NetherGate Python 插件支持的技术架构和实现方案。

---

## 📋 目录

- [架构概述](#架构概述)
- [核心组件](#核心组件)
- [技术选型](#技术选型)
- [加载流程](#加载流程)
- [API 桥接](#api-桥接)
- [性能考虑](#性能考虑)
- [安全性](#安全性)
- [未来规划](#未来规划)

---

## 架构概述

NetherGate 通过 `NetherGate.Python` 扩展模块实现 Python 插件支持，采用桥接模式将 Python 插件无缝集成到 .NET 运行时中。

### 整体架构图

```
┌─────────────────────────────────────────────────────────┐
│                  NetherGate.Host                        │
│  ┌───────────────────────────────────────────────────┐  │
│  │            NetherGate.Core                        │  │
│  │                                                    │  │
│  │  ┌──────────────────────┐  ┌──────────────────┐  │  │
│  │  │  PluginManager       │  │  EventBus        │  │  │
│  │  │  - LoadPlugin()      │  │  - Subscribe()   │  │  │
│  │  │  - EnablePlugin()    │  │  - Publish()     │  │  │
│  │  └──────────────────────┘  └──────────────────┘  │  │
│  │                                                    │  │
│  │  ┌──────────────────────────────────────────────┐ │  │
│  │  │      PluginLoader                            │ │  │
│  │  │      - ScanPlugins()                         │ │  │
│  │  │      - LoadPlugin(container)                 │ │  │
│  │  └──────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                           │
                           │ Plugin Interface (IPlugin)
                           ↓
    ┌──────────────────────┴──────────────────────┐
    │                                              │
┌───▼──────────────────┐          ┌───────────────▼──────┐
│  C# Plugin           │          │  NetherGate.Python   │
│  (Native .NET DLL)   │          │  (Python Bridge)     │
│                      │          │                      │
│  class MyPlugin :    │          │  ┌─────────────────┐ │
│    IPlugin { }       │          │  │ PythonAdapter   │ │
│                      │          │  │ : IPlugin       │ │
└──────────────────────┘          │  └────────┬────────┘ │
                                  │           │          │
                                  │           │ Python.NET│
                                  │           ↓          │
                                  │  ┌─────────────────┐ │
                                  │  │ Python Runtime  │ │
                                  │  │ (Embedded)      │ │
                                  │  └────────┬────────┘ │
                                  │           │          │
                                  │           ↓          │
                                  │  ┌─────────────────┐ │
                                  │  │ Python Plugin   │ │
                                  │  │ (.py files)     │ │
                                  │  │                 │ │
                                  │  │ class MyPlugin: │ │
                                  │  │   Plugin { }    │ │
                                  │  └─────────────────┘ │
                                  └──────────────────────┘
```

### 关键设计原则

1. **最小侵入**: 不修改 `NetherGate.Core` 核心代码
2. **统一接口**: Python 插件和 C# 插件使用相同的 `IPlugin` 接口
3. **透明桥接**: 通过适配器模式实现语言间的无缝调用
4. **性能优先**: 最小化跨语言调用开销
5. **隔离性**: Python 运行时与主进程适当隔离

---

## 核心组件

### 1. NetherGate.Python 程序集

独立的 .NET 程序集，负责 Python 集成。

**文件结构:**
```
NetherGate.Python/
├── NetherGate.Python.csproj
├── PythonPluginLoader.cs       # Python 插件加载器
├── PythonPluginAdapter.cs      # IPlugin 适配器
├── PythonRuntime.cs            # Python 运行时管理
├── PythonApiExport.cs          # C# API 导出到 Python
└── Interop/
    ├── EventBridge.cs          # 事件系统桥接
    ├── CommandBridge.cs        # 命令系统桥接
    └── ServiceBridge.cs        # 服务桥接
```

**关键依赖:**
```xml
<PackageReference Include="pythonnet" Version="3.0.1" />
<PackageReference Include="NetherGate.API" Version="*" />
```

### 2. PythonPluginAdapter

将 Python 插件适配为 C# IPlugin 接口。

```csharp
using Python.Runtime;
using NetherGate.API.Plugins;

namespace NetherGate.Python;

/// <summary>
/// Python 插件适配器
/// 将 Python 插件桥接到 IPlugin 接口
/// </summary>
public class PythonPluginAdapter : IPlugin
{
    private readonly dynamic _pythonInstance;
    private readonly ILogger _logger;
    private readonly string _pluginPath;
    
    public PluginInfo Info { get; }
    
    public PythonPluginAdapter(
        string pluginPath, 
        string mainModule, 
        string mainClass,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _pluginPath = pluginPath;
        _logger = logger;
        
        using (Py.GIL())
        {
            try
            {
                // 1. 添加插件目录到 Python 路径
                dynamic sys = Py.Import("sys");
                sys.path.insert(0, Path.Combine(pluginPath, "src"));
                
                // 2. 导入插件模块
                dynamic module = Py.Import(mainModule);
                
                // 3. 创建插件实例（支持依赖注入）
                dynamic pluginClass = module.GetAttr(mainClass);
                _pythonInstance = CreatePythonInstance(pluginClass, serviceProvider);
                
                // 4. 读取插件信息
                Info = ExtractPluginInfo(_pythonInstance);
                
                _logger.Info($"Python 插件适配器已创建: {Info.Name}");
            }
            catch (PythonException ex)
            {
                _logger.Error($"创建 Python 插件失败: {ex.Message}", ex);
                throw;
            }
        }
    }
    
    public async Task OnLoadAsync()
    {
        using (Py.GIL())
        {
            await InvokePythonMethodAsync("on_load");
        }
    }
    
    public async Task OnEnableAsync()
    {
        using (Py.GIL())
        {
            await InvokePythonMethodAsync("on_enable");
        }
    }
    
    public async Task OnDisableAsync()
    {
        using (Py.GIL())
        {
            await InvokePythonMethodAsync("on_disable");
        }
    }
    
    public async Task OnUnloadAsync()
    {
        using (Py.GIL())
        {
            await InvokePythonMethodAsync("on_unload");
        }
    }
    
    /// <summary>
    /// 调用 Python 异步方法
    /// </summary>
    private async Task InvokePythonMethodAsync(string methodName)
    {
        try
        {
            dynamic method = _pythonInstance.GetAttr(methodName);
            dynamic coroutine = method();
            
            // 如果是协程，需要运行到完成
            if (coroutine.GetPythonType().Name == "coroutine")
            {
                dynamic asyncio = Py.Import("asyncio");
                await Task.Run(() => asyncio.run(coroutine));
            }
        }
        catch (PythonException ex)
        {
            _logger.Error($"调用 Python 方法 {methodName} 失败: {ex.Message}", ex);
            throw;
        }
    }
    
    /// <summary>
    /// 创建 Python 插件实例（支持依赖注入）
    /// </summary>
    private dynamic CreatePythonInstance(dynamic pluginClass, IServiceProvider serviceProvider)
    {
        // 检查构造函数参数
        dynamic inspect = Py.Import("inspect");
        dynamic signature = inspect.signature(pluginClass);
        dynamic parameters = signature.parameters;
        
        if (parameters.Length == 0)
        {
            // 无参构造函数
            return pluginClass();
        }
        
        // 构造函数注入
        var args = new List<dynamic>();
        foreach (var param in parameters.items())
        {
            string paramName = param[0];
            dynamic paramInfo = param[1];
            
            // 尝试从服务提供者解析
            var service = ResolveService(paramName, serviceProvider);
            if (service != null)
            {
                args.Add(ToPython(service));
            }
        }
        
        return pluginClass(*args.ToArray());
    }
    
    /// <summary>
    /// 从 Python 实例提取插件信息
    /// </summary>
    private PluginInfo ExtractPluginInfo(dynamic instance)
    {
        dynamic info = instance.info;
        
        return new PluginInfo
        {
            Id = info.id.ToString(),
            Name = info.name.ToString(),
            Version = info.version.ToString(),
            Description = info.description?.ToString() ?? "",
            Author = info.author?.ToString() ?? "",
            Website = info.website?.ToString(),
            Dependencies = ExtractList(info.dependencies),
            SoftDependencies = ExtractList(info.soft_dependencies),
            LoadOrder = (int)(info.load_order ?? 100)
        };
    }
    
    // ... 辅助方法
}
```

### 3. PythonPluginLoader

扩展 `PluginLoader` 以支持 Python 插件。

```csharp
namespace NetherGate.Python;

/// <summary>
/// Python 插件加载器
/// 扩展核心 PluginLoader 以支持 Python 插件
/// </summary>
public class PythonPluginLoader
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PythonRuntime _pythonRuntime;
    
    public PythonPluginLoader(
        ILogger logger, 
        IServiceProvider serviceProvider,
        PythonRuntime pythonRuntime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _pythonRuntime = pythonRuntime;
    }
    
    /// <summary>
    /// 检测并加载 Python 插件
    /// </summary>
    public IPlugin? LoadPythonPlugin(string pluginDirectory)
    {
        // 1. 读取 plugin.json
        var metadataPath = Path.Combine(pluginDirectory, "resource", "plugin.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }
        
        var metadata = JsonSerializer.Deserialize<PluginMetadata>(
            File.ReadAllText(metadataPath)
        );
        
        // 2. 检查类型
        if (metadata.Type != "python")
        {
            return null; // 不是 Python 插件
        }
        
        // 3. 解析主类
        var mainParts = metadata.Main.Split('.');
        if (mainParts.Length != 2)
        {
            _logger.Error($"无效的 Python 主类格式: {metadata.Main}");
            return null;
        }
        
        string mainModule = mainParts[0];
        string mainClass = mainParts[1];
        
        // 4. 安装 Python 依赖
        if (metadata.PythonDependencies?.Count > 0)
        {
            InstallPythonDependencies(pluginDirectory, metadata.PythonDependencies);
        }
        
        // 5. 创建适配器
        try
        {
            var adapter = new PythonPluginAdapter(
                pluginDirectory,
                mainModule,
                mainClass,
                _serviceProvider,
                _logger
            );
            
            _logger.Info($"Python 插件已加载: {adapter.Info.Name}");
            return adapter;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载 Python 插件失败: {ex.Message}", ex);
            return null;
        }
    }
    
    /// <summary>
    /// 安装 Python 依赖
    /// </summary>
    private void InstallPythonDependencies(string pluginDirectory, List<string> dependencies)
    {
        _logger.Info("安装 Python 依赖...");
        
        // 创建虚拟环境（如果不存在）
        var venvPath = Path.Combine(pluginDirectory, ".venv");
        if (!Directory.Exists(venvPath))
        {
            _pythonRuntime.CreateVirtualEnvironment(venvPath);
        }
        
        // 安装依赖
        foreach (var dep in dependencies)
        {
            _pythonRuntime.InstallPackage(dep, venvPath);
        }
        
        _logger.Info("Python 依赖安装完成");
    }
}
```

### 4. 集成到 PluginLoader

修改 `PluginLoader.ScanPlugin` 以支持 Python 插件：

```csharp
// 在 NetherGate.Core/Plugins/PluginLoader.cs 中

private PluginContainer? ScanPlugin(string pluginDirectory)
{
    // 1. 读取元数据
    var metadata = ScanPluginMetadataFromDirectory(pluginDirectory);
    if (metadata == null)
    {
        return null;
    }
    
    // 2. 根据类型选择加载器
    IPlugin? pluginInstance = null;
    string assemblyPath = "";
    
    if (metadata.Type == "python")
    {
        // Python 插件
        var pythonLoader = _serviceProvider.GetService<PythonPluginLoader>();
        if (pythonLoader != null)
        {
            pluginInstance = pythonLoader.LoadPythonPlugin(pluginDirectory);
            assemblyPath = pluginDirectory; // 使用目录路径
        }
    }
    else
    {
        // C# 插件（默认）
        var pluginName = Path.GetFileName(pluginDirectory);
        assemblyPath = Path.Combine(pluginDirectory, $"{pluginName}.dll");
        
        if (!File.Exists(assemblyPath))
        {
            _logger.Error($"插件 DLL 不存在: {assemblyPath}");
            return null;
        }
    }
    
    // 3. 创建容器
    var dataDirectory = Path.Combine(_configDirectory, metadata.Id);
    if (!Directory.Exists(dataDirectory))
    {
        Directory.CreateDirectory(dataDirectory);
    }
    
    return new PluginContainer(metadata, pluginDirectory, assemblyPath, dataDirectory)
    {
        Instance = pluginInstance
    };
}
```

---

## 技术选型

### Python.NET (pythonnet)

**选择理由:**
- ✅ 成熟稳定，社区活跃
- ✅ 支持 .NET 6/7/8
- ✅ 双向互操作（C# ↔ Python）
- ✅ 支持嵌入 Python 运行时
- ✅ 性能优异

**版本要求:**
- Python: 3.8+
- pythonnet: 3.0+

**安装:**
```bash
dotnet add package pythonnet
```

---

## 加载流程

### 完整加载流程图

```
1. NetherGate 启动
   │
   ↓
2. PluginManager.LoadAllPluginsAsync()
   │
   ↓
3. PluginLoader.ScanPlugins()
   │
   ├─→ 扫描 C# 插件 (.dll)
   │
   └─→ 扫描 Python 插件 (plugin.json type="python")
       │
       ↓
4. PythonPluginLoader.LoadPythonPlugin()
   │
   ├─→ 读取 plugin.json
   │
   ├─→ 检查 Python 版本
   │
   ├─→ 安装 Python 依赖 (requirements.txt)
   │
   ├─→ 初始化 Python 运行时
   │
   ├─→ 导入 Python 模块
   │
   ├─→ 创建 PythonPluginAdapter
   │   │
   │   ├─→ 实例化 Python 插件类
   │   │
   │   ├─→ 依赖注入 (Logger, EventBus, etc.)
   │   │
   │   └─→ 提取 PluginInfo
   │
   └─→ 返回 IPlugin 实例
       │
       ↓
5. PluginManager.InitializePluginAsync()
   │
   └─→ adapter.OnLoadAsync()
       │
       └─→ Python: plugin.on_load()
           │
           ↓
6. PluginManager.EnablePluginAsync()
   │
   └─→ adapter.OnEnableAsync()
       │
       └─→ Python: plugin.on_enable()
           │
           └─→ 注册事件、命令等
```

---

## API 桥接

### 事件系统桥接

```csharp
// C# 事件桥接到 Python

public class EventBridge
{
    public static PyObject ToPythonEvent(ServerStartedEvent csEvent)
    {
        using (Py.GIL())
        {
            dynamic pyEvent = PyModule.Import("nethergate.events").GetAttr("ServerStartedEvent");
            return pyEvent(timestamp: csEvent.Timestamp.ToString());
        }
    }
    
    public static void SubscribePythonHandler(
        IEventBus eventBus, 
        Type eventType, 
        PyObject pythonHandler)
    {
        eventBus.Subscribe(eventType, async (csEvent) => 
        {
            using (Py.GIL())
            {
                var pyEvent = ToPythonEvent((dynamic)csEvent);
                await Task.Run(() => pythonHandler(pyEvent));
            }
        });
    }
}
```

### Python API 包

在 Python 侧提供完整的 API 包装：

```python
# nethergate/__init__.py

from .plugin import Plugin, PluginInfo
from .logging import Logger, LogLevel
from .events import EventBus
# ... 其他导出

__all__ = ['Plugin', 'PluginInfo', 'Logger', 'LogLevel', 'EventBus']
```

---

## 性能考虑

### 1. GIL 管理

```csharp
// 最小化 GIL 锁定时间
public async Task OnEnableAsync()
{
    // 准备数据（不在 GIL 内）
    var data = await PrepareDataAsync();
    
    // 快速调用 Python（在 GIL 内）
    using (Py.GIL())
    {
        _pythonInstance.on_enable(ToPython(data));
    }
}
```

### 2. 对象转换缓存

```csharp
// 缓存常用的 Python 对象
private static readonly Dictionary<Type, PyObject> _typeCache = new();

public static PyObject ToPython<T>(T obj)
{
    var type = typeof(T);
    if (_typeCache.TryGetValue(type, out var cached))
    {
        return cached;
    }
    
    // 创建并缓存
    var pyObj = CreatePyObject(obj);
    _typeCache[type] = pyObj;
    return pyObj;
}
```

### 3. 批量操作

```csharp
// 批量调用以减少 GIL 切换
public async Task ExecuteBatchAsync(List<string> commands)
{
    using (Py.GIL())
    {
        dynamic batch = ToPythonList(commands);
        await _pythonInstance.execute_batch(batch);
    }
}
```

---

## 安全性

### 1. 沙箱隔离

```python
# 限制 Python 插件的系统访问
import sys
import os

# 禁用危险模块
BLACKLIST = ['os.system', 'subprocess', 'eval', 'exec']

def validate_import(module_name):
    if any(blocked in module_name for blocked in BLACKLIST):
        raise ImportError(f"Module {module_name} is not allowed")
```

### 2. 资源限制

```csharp
// 限制 Python 插件的资源使用
public class PythonRuntime
{
    public void SetResourceLimits()
    {
        using (Py.GIL())
        {
            dynamic resource = Py.Import("resource");
            
            // 限制内存使用 (256MB)
            resource.setrlimit(
                resource.RLIMIT_AS, 
                (256 * 1024 * 1024, 256 * 1024 * 1024)
            );
            
            // 限制 CPU 时间
            resource.setrlimit(
                resource.RLIMIT_CPU, 
                (60, 60)  // 60 秒
            );
        }
    }
}
```

---

## 未来规划

### 短期目标

- [ ] 完善 Python API 包装
- [ ] 实现自动依赖管理
- [ ] 提供插件开发模板
- [ ] 编写详细文档和示例

### 中期目标

- [ ] 支持 Python 插件热重载
- [ ] 实现 Python ↔ C# 插件通信
- [ ] 提供性能分析工具
- [ ] 添加单元测试覆盖

### 长期目标

- [ ] 支持其他脚本语言（JavaScript, Lua）
- [ ] 插件市场集成
- [ ] 可视化插件开发工具
- [ ] 云端插件部署

---

## 参考资源

- [Python.NET 官方文档](https://pythonnet.github.io/)
- [.NET 互操作性指南](https://docs.microsoft.com/dotnet/standard/native-interop/)
- [Python asyncio 文档](https://docs.python.org/3/library/asyncio.html)

---

## 贡献

欢迎为 NetherGate.Python 贡献代码！

- GitHub: https://github.com/your-org/NetherGate.Python
- Issues: https://github.com/your-org/NetherGate.Python/issues
- Discussions: https://github.com/your-org/NetherGate.Python/discussions

