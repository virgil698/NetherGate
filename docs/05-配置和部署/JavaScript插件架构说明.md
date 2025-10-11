# JavaScript/TypeScript 插件架构说明

本文档详细说明 `NetherGate.Script` 的架构设计和实现原理。

---

## 🏗️ 架构概览

`NetherGate.Script` 使用 **Jint** 库实现 C# 与 JavaScript 的互操作，提供与 Python 插件类似的桥接架构。

### 核心组件

```
┌─────────────────────────────────────┐
│     NetherGate.Core                 │
│     (PluginManager)                 │
└────────────┬────────────────────────┘
             │
             ↓
┌─────────────────────────────────────┐
│  NetherGate.Script                  │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  JavaScriptRuntime            │ │
│  │  - 管理 Jint 引擎             │ │
│  │  - 初始化/关闭                │ │
│  │  - 执行脚本                   │ │
│  └───────────────────────────────┘ │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  JavaScriptPluginLoader       │ │
│  │  - 扫描 JS 插件               │ │
│  │  - 处理 .ngplugin 打包       │ │
│  │  - 解压和加载                 │ │
│  └───────────────────────────────┘ │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  JavaScriptPluginAdapter      │ │
│  │  - 实现 IPlugin 接口          │ │
│  │  - 桥接 C# ↔ JavaScript      │ │
│  │  - 依赖注入                   │ │
│  └───────────────────────────────┘ │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  JavaScriptApiWrapper         │ │
│  │  - API 封装层                 │ │
│  │  - 类型转换                   │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
             │
             ↓ Jint
┌─────────────────────────────────────┐
│  JavaScript Runtime (Jint)          │
│                                     │
│  ┌───────────────────────────────┐ │
│  │  JavaScript Plugin (.js)      │ │
│  │  - class MyPlugin { ... }     │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
```

---

## 🔑 关键实现

### 1. JavaScriptRuntime

**职责**: 管理 Jint 引擎生命周期

```csharp
using Jint;
using Jint.Runtime;
using NetherGate.API.Logging;

namespace NetherGate.Script;

public class JavaScriptRuntime
{
    private readonly ILogger _logger;
    private Engine? _engine;
    
    public JavaScriptRuntime(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// 初始化 JavaScript 引擎
    /// </summary>
    public void Initialize()
    {
        _engine = new Engine(options =>
        {
            // 配置 Jint 引擎
            options.AllowClr(); // 允许访问 CLR 类型
            options.CatchClrExceptions(); // 捕获 CLR 异常
            options.TimeoutInterval(TimeSpan.FromSeconds(10)); // 执行超时
        });
        
        _logger.Info("JavaScript 引擎已初始化");
    }
    
    /// <summary>
    /// 执行 JavaScript 代码
    /// </summary>
    public object? Execute(string code, string? fileName = null)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }
        
        try
        {
            return _engine.Evaluate(code, fileName ?? "script.js").ToObject();
        }
        catch (JavaScriptException ex)
        {
            _logger.Error($"JavaScript 执行错误: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 加载模块
    /// </summary>
    public void LoadModule(string modulePath, string moduleContent)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }
        
        // 执行模块代码
        _engine.Execute(moduleContent, modulePath);
    }
    
    /// <summary>
    /// 设置全局变量
    /// </summary>
    public void SetGlobal(string name, object value)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }
        
        _engine.SetValue(name, value);
    }
    
    /// <summary>
    /// 获取全局变量
    /// </summary>
    public object? GetGlobal(string name)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }
        
        return _engine.GetValue(name).ToObject();
    }
    
    /// <summary>
    /// 关闭引擎
    /// </summary>
    public void Shutdown()
    {
        _engine = null;
        _logger.Info("JavaScript 引擎已关闭");
    }
}
```

**关键点**:
- 使用 `Jint.Engine` 作为 JavaScript 执行引擎
- 支持 ES5/ES6+ 部分特性
- 配置超时保护，防止无限循环
- 支持 CLR 类型互操作

---

### 2. JavaScriptPluginLoader

**职责**: 扫描和加载 JavaScript 插件

```csharp
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Script;

public class JavaScriptPluginLoader
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JavaScriptRuntime _jsRuntime;
    private readonly string _tempDirectory;

    public JavaScriptPluginLoader(
        ILogger logger,
        IServiceProvider serviceProvider,
        JavaScriptRuntime jsRuntime,
        string pluginsDirectory)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _jsRuntime = jsRuntime;
        _tempDirectory = Path.Combine(pluginsDirectory, ".temp");
        
        // 确保临时目录存在
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// 检测并加载 JavaScript 插件
    /// </summary>
    public IPlugin? LoadJavaScriptPlugin(string pluginPath, PluginMetadata metadata)
    {
        try
        {
            _logger.Info($"加载 JavaScript 插件: {metadata.Name}");
            
            // 1. 判断是目录还是 .ngplugin 文件
            string pluginDirectory;
            if (File.Exists(pluginPath) && pluginPath.EndsWith(".ngplugin"))
            {
                // 解压 .ngplugin 文件
                pluginDirectory = ExtractNgPlugin(pluginPath);
            }
            else if (Directory.Exists(pluginPath))
            {
                // 直接使用目录
                pluginDirectory = pluginPath;
            }
            else
            {
                _logger.Error($"无效的插件路径: {pluginPath}");
                return null;
            }
            
            // 2. 读取主文件
            var mainFilePath = Path.Combine(pluginDirectory, metadata.Main);
            if (!File.Exists(mainFilePath))
            {
                _logger.Error($"找不到主文件: {mainFilePath}");
                return null;
            }
            
            var mainFileContent = File.ReadAllText(mainFilePath);
            
            // 3. 创建适配器
            var adapter = new JavaScriptPluginAdapter(
                pluginDirectory,
                mainFileContent,
                mainFilePath,
                _serviceProvider,
                _logger,
                _jsRuntime
            );
            
            _logger.Info($"JavaScript 插件加载成功: {adapter.Info.Name} v{adapter.Info.Version}");
            return adapter;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载 JavaScript 插件失败: {metadata.Name}", ex);
            return null;
        }
    }
    
    /// <summary>
    /// 解压 .ngplugin 文件
    /// </summary>
    private string ExtractNgPlugin(string ngPluginPath)
    {
        var pluginName = Path.GetFileNameWithoutExtension(ngPluginPath);
        var extractPath = Path.Combine(_tempDirectory, pluginName);
        
        // 如果已存在，先删除
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }
        
        // 解压
        ZipFile.ExtractToDirectory(ngPluginPath, extractPath);
        _logger.Debug($"已解压 .ngplugin 文件到: {extractPath}");
        
        return extractPath;
    }
    
    /// <summary>
    /// 从目录扫描 JavaScript 插件元数据
    /// </summary>
    public static bool IsJavaScriptPlugin(string pluginDirectory, out PluginMetadata? metadata)
    {
        metadata = null;
        
        try
        {
            // 检查 .ngplugin 文件
            if (File.Exists(pluginDirectory) && pluginDirectory.EndsWith(".ngplugin"))
            {
                // 临时解压读取 plugin.json
                using var archive = ZipFile.OpenRead(pluginDirectory);
                var pluginJsonEntry = archive.GetEntry("resource/plugin.json");
                
                if (pluginJsonEntry != null)
                {
                    using var stream = pluginJsonEntry.Open();
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    metadata = JsonSerializer.Deserialize<PluginMetadata>(json);
                    
                    return metadata?.Type?.Equals("javascript", StringComparison.OrdinalIgnoreCase) == true;
                }
            }
            // 检查目录
            else if (Directory.Exists(pluginDirectory))
            {
                var metadataPath = Path.Combine(pluginDirectory, "resource", "plugin.json");
                if (!File.Exists(metadataPath))
                {
                    return false;
                }
                
                var json = File.ReadAllText(metadataPath);
                metadata = JsonSerializer.Deserialize<PluginMetadata>(json);
                
                return metadata?.Type?.Equals("javascript", StringComparison.OrdinalIgnoreCase) == true;
            }
        }
        catch
        {
            // 忽略错误
        }
        
        return false;
    }
}
```

**关键实现**:
- 支持目录和 `.ngplugin` 打包文件两种格式
- 自动解压 zip 格式的插件包
- 临时目录管理（避免污染插件目录）

---

### 3. JavaScriptPluginAdapter

**职责**: 将 JavaScript 插件适配为 IPlugin 接口

```csharp
using Jint.Native;
using Jint.Native.Function;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Script;

public class JavaScriptPluginAdapter : IPlugin
{
    private readonly ILogger _logger;
    private readonly JavaScriptRuntime _jsRuntime;
    private readonly string _pluginDirectory;
    private JsValue _pluginInstance;
    
    public PluginInfo Info { get; private set; }

    public JavaScriptPluginAdapter(
        string pluginDirectory,
        string scriptContent,
        string scriptPath,
        IServiceProvider serviceProvider,
        ILogger logger,
        JavaScriptRuntime jsRuntime)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
        _pluginDirectory = pluginDirectory;
        
        // 1. 加载脚本并获取导出的类
        _jsRuntime.LoadModule(scriptPath, scriptContent);
        
        // 2. 获取 module.exports (插件类)
        var pluginClass = _jsRuntime.GetGlobal("module")
            ?.AsObject()
            ?.Get("exports") as JsValue;
        
        if (pluginClass == null || !pluginClass.IsConstructor)
        {
            throw new InvalidOperationException($"插件主文件必须导出一个类: {scriptPath}");
        }
        
        // 3. 依赖注入：创建插件实例
        _pluginInstance = CreatePluginInstance(pluginClass.AsConstructor(), serviceProvider);
        
        // 4. 读取插件信息
        Info = ExtractPluginInfo(_pluginInstance);
    }
    
    /// <summary>
    /// 创建插件实例（支持依赖注入）
    /// </summary>
    private JsValue CreatePluginInstance(ScriptFunction pluginClass, IServiceProvider serviceProvider)
    {
        // 解析构造函数参数（通过函数参数名）
        var paramNames = GetConstructorParameterNames(pluginClass);
        var args = new List<object?>();
        
        foreach (var paramName in paramNames)
        {
            var service = ResolveService(paramName, serviceProvider);
            if (service != null)
            {
                args.Add(service);
            }
            else
            {
                _logger.Warning($"无法解析依赖: {paramName}");
                args.Add(null);
            }
        }
        
        // 调用构造函数
        return pluginClass.Construct(args.ToArray());
    }
    
    /// <summary>
    /// 获取构造函数参数名
    /// </summary>
    private List<string> GetConstructorParameterNames(ScriptFunction pluginClass)
    {
        // 通过 toString() 解析函数定义
        var funcStr = pluginClass.ToString();
        
        // 正则匹配 constructor(param1, param2, ...)
        var match = System.Text.RegularExpressions.Regex.Match(
            funcStr, 
            @"constructor\s*\((.*?)\)"
        );
        
        if (!match.Success)
        {
            return new List<string>();
        }
        
        var paramStr = match.Groups[1].Value.Trim();
        if (string.IsNullOrEmpty(paramStr))
        {
            return new List<string>();
        }
        
        return paramStr
            .Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }
    
    /// <summary>
    /// 解析服务（参数名 → C# 服务类型）
    /// </summary>
    private object? ResolveService(string paramName, IServiceProvider serviceProvider)
    {
        // 映射表：JavaScript 参数名 → C# 服务类型
        var serviceMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["logger"] = typeof(ILogger),
            ["eventbus"] = typeof(IEventBus),
            ["commandregistry"] = typeof(ICommandRegistry),
            ["rcon"] = typeof(IRconClient),
            ["scheduler"] = typeof(IScheduler),
            ["configmanager"] = typeof(IConfigManager),
            ["permissionmanager"] = typeof(IPermissionManager),
            ["scoreboardapi"] = typeof(IScoreboardApi),
            ["playerdatareader"] = typeof(IPlayerDataReader),
            ["websocketserver"] = typeof(IWebSocketServer)
        };
        
        if (serviceMap.TryGetValue(paramName, out var serviceType))
        {
            return serviceProvider.GetService(serviceType);
        }
        
        return null;
    }
    
    /// <summary>
    /// 提取插件信息
    /// </summary>
    private PluginInfo ExtractPluginInfo(JsValue pluginInstance)
    {
        var infoObj = pluginInstance.AsObject().Get("info");
        
        return new PluginInfo
        {
            Id = infoObj.Get("id").AsString(),
            Name = infoObj.Get("name").AsString(),
            Version = infoObj.Get("version").AsString(),
            Description = infoObj.Get("description")?.AsString() ?? "",
            Author = infoObj.Get("author")?.AsString() ?? "Unknown"
        };
    }
    
    // IPlugin 接口实现
    
    public async Task OnLoadAsync()
    {
        await InvokePluginMethodAsync("onLoad");
    }
    
    public async Task OnEnableAsync()
    {
        await InvokePluginMethodAsync("onEnable");
    }
    
    public async Task OnDisableAsync()
    {
        await InvokePluginMethodAsync("onDisable");
    }
    
    public async Task OnUnloadAsync()
    {
        await InvokePluginMethodAsync("onUnload");
    }
    
    /// <summary>
    /// 调用插件方法
    /// </summary>
    private async Task InvokePluginMethodAsync(string methodName)
    {
        try
        {
            var method = _pluginInstance.AsObject().Get(methodName);
            
            if (method.IsUndefined())
            {
                // 方法未定义，跳过
                return;
            }
            
            if (!method.IsCallable)
            {
                _logger.Warning($"方法 {methodName} 不可调用");
                return;
            }
            
            // 调用方法
            var result = method.Invoke(_pluginInstance);
            
            // 检查是否返回 Promise
            if (IsPromise(result))
            {
                // 等待 Promise 完成
                await WaitForPromise(result);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"调用插件方法 {methodName} 失败", ex);
            throw;
        }
    }
    
    /// <summary>
    /// 检查是否是 Promise
    /// </summary>
    private bool IsPromise(JsValue value)
    {
        if (!value.IsObject())
        {
            return false;
        }
        
        var thenMethod = value.AsObject().Get("then");
        return thenMethod.IsCallable;
    }
    
    /// <summary>
    /// 等待 Promise 完成
    /// </summary>
    private async Task WaitForPromise(JsValue promise)
    {
        var tcs = new TaskCompletionSource<object?>();
        
        // 注册 then/catch 回调
        var thenMethod = promise.AsObject().Get("then").AsFunction();
        thenMethod.Invoke(promise,
            // onFulfilled
            new JsValue((engine, thisObj, args) =>
            {
                tcs.SetResult(args.FirstOrDefault()?.ToObject());
                return JsValue.Undefined;
            }),
            // onRejected
            new JsValue((engine, thisObj, args) =>
            {
                var error = args.FirstOrDefault()?.ToString() ?? "Unknown error";
                tcs.SetException(new Exception(error));
                return JsValue.Undefined;
            })
        );
        
        await tcs.Task;
    }
}
```

**关键实现**:

#### 依赖注入

```csharp
// 通过解析构造函数参数名进行服务解析
constructor(logger, eventBus, rcon) // JavaScript
    ↓
["logger", "eventBus", "rcon"] // 参数名列表
    ↓
[ILogger, IEventBus, IRconClient] // C# 服务类型
    ↓
new PluginClass(loggerInstance, eventBusInstance, rconInstance) // 创建实例
```

#### 异步方法调用

```csharp
// JavaScript 的 async 函数返回 Promise
async onEnable() { ... }
    ↓
返回 Promise 对象
    ↓
检测 .then 方法存在
    ↓
使用 TaskCompletionSource 等待 Promise 完成
```

---

### 4. JavaScriptApiWrapper

**职责**: 封装 C# API 为 JavaScript 友好的接口

```csharp
using NetherGate.API.Logging;

namespace NetherGate.Script;

/// <summary>
/// Logger 的 JavaScript 包装器
/// </summary>
public class LoggerWrapper
{
    private readonly ILogger _logger;
    
    public LoggerWrapper(ILogger logger)
    {
        _logger = logger;
    }
    
    public void trace(string message) => _logger.Trace(message);
    public void debug(string message) => _logger.Debug(message);
    public void info(string message) => _logger.Info(message);
    public void warning(string message) => _logger.Warning(message);
    public void error(string message) => _logger.Error(message);
}

/// <summary>
/// EventBus 的 JavaScript 包装器
/// </summary>
public class EventBusWrapper
{
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, Action<object>> _handlers = new();
    
    public EventBusWrapper(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void subscribe(string eventName, Action<object> handler)
    {
        _handlers[eventName] = handler;
        // 订阅到 C# EventBus
        _eventBus.Subscribe(eventName, handler);
    }
    
    public void unsubscribe(string eventName, Action<object> handler)
    {
        _handlers.Remove(eventName);
        _eventBus.Unsubscribe(eventName, handler);
    }
    
    public void publish(string eventName, object eventData)
    {
        _eventBus.Publish(eventName, eventData);
    }
}
```

**关键点**:
- 使用小写方法名（JavaScript 约定）
- 简化参数类型（避免复杂的 CLR 类型）
- 提供 JavaScript 友好的返回值

---

## 📦 打包插件支持

### .ngplugin 文件格式

`.ngplugin` 文件实际上是一个 **ZIP 文件**，包含以下结构：

```
MyPlugin.ngplugin (ZIP)
├── src/              # 源代码 (或 dist/)
│   └── index.js
├── resource/         # 资源文件
│   ├── plugin.json
│   └── config.yaml
├── README.md
└── LICENSE
```

### 加载流程

```
.ngplugin 文件
    ↓ (ZipFile.ExtractToDirectory)
临时目录: plugins/.temp/MyPlugin/
    ↓ (读取 resource/plugin.json)
解析元数据
    ↓ (读取 src/index.js 或 dist/index.js)
加载 JavaScript 代码
    ↓ (Jint Engine)
执行并创建插件实例
    ↓
注册到 PluginManager
```

### 临时目录管理

```csharp
public class TempDirectoryManager
{
    private readonly string _tempRoot;
    
    public TempDirectoryManager(string pluginsDirectory)
    {
        _tempRoot = Path.Combine(pluginsDirectory, ".temp");
        CleanOldTemp();
    }
    
    /// <summary>
    /// 清理旧的临时文件
    /// </summary>
    private void CleanOldTemp()
    {
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, true);
            }
            catch
            {
                // 忽略错误（可能被其他进程占用）
            }
        }
        
        Directory.CreateDirectory(_tempRoot);
    }
    
    /// <summary>
    /// 获取插件的临时目录
    /// </summary>
    public string GetTempDirectory(string pluginId)
    {
        return Path.Combine(_tempRoot, pluginId);
    }
}
```

---

## 🔐 安全考虑

### 1. 执行超时

```csharp
var engine = new Engine(options =>
{
    options.TimeoutInterval(TimeSpan.FromSeconds(10));
});
```

防止无限循环导致服务器卡死。

### 2. 沙箱隔离

```csharp
var engine = new Engine(options =>
{
    options.AllowClr(type =>
    {
        // 只允许访问特定的 CLR 类型
        return type.Namespace?.StartsWith("NetherGate.API") == true;
    });
});
```

限制 JavaScript 代码访问的 .NET 类型。

### 3. 文件访问限制

```csharp
public class SafeFileAccess
{
    private readonly string _pluginDataDirectory;
    
    public string ReadFile(string relativePath)
    {
        // 确保路径在插件数据目录内
        var fullPath = Path.GetFullPath(Path.Combine(_pluginDataDirectory, relativePath));
        if (!fullPath.StartsWith(_pluginDataDirectory))
        {
            throw new SecurityException("不允许访问插件目录外的文件");
        }
        
        return File.ReadAllText(fullPath);
    }
}
```

---

## ⚠️ 已知限制

1. **ES 模块系统**: Jint 不完全支持 ES6 模块（`import`/`export`），建议使用 CommonJS（`require`/`module.exports`）
2. **Node.js API**: 不支持 Node.js 原生模块（如 `fs`, `http` 等）
3. **异步支持**: Promise 支持有限，建议使用简单的 async/await
4. **性能**: 比原生 .NET 代码慢 10-100 倍
5. **调试**: 缺少完整的调试工具支持

---

## 🚀 性能优化建议

### 1. 缓存引擎实例

```csharp
// ❌ 不推荐：每次创建新引擎
public void Execute(string code)
{
    var engine = new Engine();
    engine.Execute(code);
}

// ✅ 推荐：复用引擎实例
private readonly Engine _engine = new Engine();

public void Execute(string code)
{
    _engine.Execute(code);
}
```

### 2. 批量操作

```csharp
// ✅ 推荐：一次调用处理多个项
await InvokeJsMethod("processBatch", items);

// ❌ 不推荐：多次跨语言调用
foreach (var item in items)
{
    await InvokeJsMethod("processItem", item);
}
```

### 3. 预编译脚本

```csharp
// 预编译脚本，避免重复解析
var preparedScript = engine.PrepareScript(code);

// 多次执行
preparedScript.Execute();
preparedScript.Execute();
```

---

## 📝 与 Python 插件的对比

| 特性 | Python 插件 | JavaScript 插件 |
|------|------------|----------------|
| 桥接库 | Python.NET | Jint |
| 运行时 | Python 解释器 | 嵌入式 JS 引擎 |
| 外部依赖 | 需要 Python 环境 | 无需外部依赖 |
| 包管理 | pip | 不支持 NPM（仅开发时） |
| 性能 | 较慢（GIL） | 较慢（解释执行） |
| 生态系统 | 完整的 PyPI | 仅标准 JS（无 Node.js） |
| 类型系统 | 动态类型 + 类型提示 | 动态类型 + TypeScript |
| 调试支持 | 中等 | 较差 |

---

## 🔗 参考资源

- [Jint 文档](https://github.com/sebastienros/jint)
- [JavaScript 插件开发指南](../03-插件开发/JavaScript插件开发指南.md)
- [NetherGate 架构设计](./架构优化指南.md)

---

## 🎯 后续计划

- [ ] 支持 TypeScript 的直接执行（不需要预编译）
- [ ] 完善 API 封装（覆盖更多 .NET API）
- [ ] 提供插件市场和仓库
- [ ] 改进调试工具和错误追踪
- [ ] 性能优化和基准测试

