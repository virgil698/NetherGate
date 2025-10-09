using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using Python.Runtime;

namespace NetherGate.Python;

/// <summary>
/// Python 插件适配器
/// 将 Python 插件桥接到 IPlugin 接口
/// </summary>
public class PythonPluginAdapter : IPlugin
{
    private readonly PyObject _pythonInstance;
    private readonly ILogger _logger;
    private readonly string _pluginPath;
    private readonly PythonRuntime _runtime;

    public PluginInfo Info { get; }

    public PythonPluginAdapter(
        string pluginPath,
        string mainModule,
        string mainClass,
        IServiceProvider serviceProvider,
        ILogger logger,
        PythonRuntime runtime)
    {
        _pluginPath = pluginPath;
        _logger = logger;
        _runtime = runtime;

        using (Py.GIL())
        {
            try
            {
                // 1. 添加插件源码目录到 Python 路径
                var srcPath = Path.Combine(pluginPath, "src");
                if (Directory.Exists(srcPath))
                {
                    _runtime.AddToPath(srcPath);
                }

                // 2. 导入插件模块
                _logger.Debug($"导入 Python 模块: {mainModule}");
                using var module = Py.Import(mainModule);

                // 3. 获取插件类
                using var pluginClass = module.GetAttr(mainClass);

                // 4. 创建插件实例（支持依赖注入）
                _pythonInstance = CreatePythonInstance(pluginClass, serviceProvider);

                // 5. 提取插件信息
                Info = ExtractPluginInfo(_pythonInstance);

                _logger.Info($"Python 插件适配器已创建: {Info.Name} v{Info.Version}");
            }
            catch (PythonException ex)
            {
                _logger.Error($"创建 Python 插件失败: {ex.Message}", ex);
                _logger.Error($"Python 堆栈追踪: {ex.StackTrace}");
                throw new InvalidOperationException($"Python 插件初始化失败: {ex.Message}", ex);
            }
        }
    }

    public async Task OnLoadAsync()
    {
        await InvokePythonMethodAsync("on_load");
    }

    public async Task OnEnableAsync()
    {
        await InvokePythonMethodAsync("on_enable");
    }

    public async Task OnDisableAsync()
    {
        await InvokePythonMethodAsync("on_disable");
    }

    public async Task OnUnloadAsync()
    {
        await InvokePythonMethodAsync("on_unload");
    }

    /// <summary>
    /// 调用 Python 异步方法
    /// </summary>
    private async Task InvokePythonMethodAsync(string methodName)
    {
        try
        {
            using (Py.GIL())
            {
                // 检查方法是否存在
                if (!_pythonInstance.HasAttr(methodName))
                {
                    _logger.Warning($"Python 插件缺少方法: {methodName}");
                    return;
                }

                using var method = _pythonInstance.GetAttr(methodName);
                using var result = method.Invoke();

                // 检查是否是协程
                using var inspect = Py.Import("inspect");
                using var isCoroutine = inspect.InvokeMethod("iscoroutine", result);

                if (isCoroutine.IsTrue())
                {
                    // 运行协程
                    using var asyncio = Py.Import("asyncio");
                    
                    // 检查是否有运行中的事件循环
                    PyObject? loop = null;
                    try
                    {
                        loop = asyncio.InvokeMethod("get_running_loop");
                    }
                    catch (PythonException)
                    {
                        // 没有运行中的循环，创建新的
                    }

                    if (loop != null)
                    {
                        // 使用现有循环
                        using var createTask = asyncio.GetAttr("create_task");
                        using var task = createTask.Invoke(result);
                        
                        // 等待任务完成
                        await Task.Run(() =>
                        {
                            using (Py.GIL())
                            {
                                using var loopRun = loop.GetAttr("run_until_complete");
                                loopRun.Invoke(task);
                            }
                        });
                        loop?.Dispose();
                    }
                    else
                    {
                        // 创建新循环运行
                        await Task.Run(() =>
                        {
                            using (Py.GIL())
                            {
                                using var run = asyncio.GetAttr("run");
                                run.Invoke(result);
                            }
                        });
                    }
                }
                // 如果不是协程，方法已经执行完毕
            }

            _logger.Trace($"Python 方法 {methodName} 执行成功");
        }
        catch (PythonException ex)
        {
            _logger.Error($"调用 Python 方法 {methodName} 失败: {ex.Message}", ex);
            _logger.Error($"Python 堆栈追踪:\n{ex.StackTrace}");
            throw new InvalidOperationException($"Python 方法调用失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建 Python 插件实例（支持依赖注入）
    /// </summary>
    private PyObject CreatePythonInstance(PyObject pluginClass, IServiceProvider serviceProvider)
    {
        using var inspect = Py.Import("inspect");
        using var signature = inspect.InvokeMethod("signature", pluginClass);
        using var parameters = signature.GetAttr("parameters");
        using var items = parameters.InvokeMethod("items");

        // 转换为列表以便遍历
        var paramList = new List<(string name, PyObject info)>();
        using var iterator = items.GetIterator();
        
        while (iterator.MoveNext())
        {
            using var item = iterator.Current;
            using var paramName = item.GetItem(0);
            var paramInfo = item.GetItem(1);
            
            var name = paramName.ToString() ?? "";
            paramList.Add((name, paramInfo));
        }

        if (paramList.Count == 0)
        {
            // 无参构造函数
            _logger.Debug("使用无参构造函数创建 Python 插件实例");
            var instance = pluginClass.Invoke();
            
            // 清理
            foreach (var param in paramList)
            {
                param.info.Dispose();
            }
            
            return instance;
        }

        // 构造函数注入
        _logger.Debug($"使用依赖注入创建 Python 插件实例 ({paramList.Count} 个参数)");
        var args = new List<PyObject>();

        try
        {
            foreach (var (name, info) in paramList)
            {
                // 尝试从服务提供者解析
                var service = ResolveService(name, serviceProvider);
                if (service != null)
                {
                    args.Add(ToPython(service));
                    _logger.Trace($"  - 注入参数: {name} ({service.GetType().Name})");
                }
                else
                {
                    // 检查是否有默认值
                    using var defaultAttr = info.GetAttr("default");
                    using var inspectModule = Py.Import("inspect");
                    using var parameterClass = inspectModule.GetAttr("Parameter");
                    using var emptyType = parameterClass.GetAttr("empty");
                    
                    if (!defaultAttr.Equals(emptyType))
                    {
                        args.Add(defaultAttr);
                        _logger.Trace($"  - 使用默认值: {name}");
                    }
                    else
                    {
                        _logger.Warning($"  - 无法解析参数: {name}");
                        throw new InvalidOperationException($"无法解析构造函数参数: {name}");
                    }
                }
            }

            // 调用构造函数
            var instance = pluginClass.Invoke(args.ToArray());

            return instance;
        }
        finally
        {
            // 清理
            foreach (var arg in args)
            {
                arg.Dispose();
            }
            foreach (var param in paramList)
            {
                param.info.Dispose();
            }
        }
    }

    /// <summary>
    /// 从服务提供者解析服务
    /// </summary>
    private object? ResolveService(string parameterName, IServiceProvider serviceProvider)
    {
        // 根据参数名称推断服务类型
        return parameterName.ToLower() switch
        {
            "logger" => serviceProvider.GetService(typeof(ILogger)),
            "event_bus" or "eventbus" => serviceProvider.GetService(typeof(API.Events.IEventBus)),
            "commands" or "command_registry" => serviceProvider.GetService(typeof(API.Protocol.IServerCommandExecutor)),
            "rcon" or "rcon_client" => serviceProvider.GetService(typeof(API.Protocol.IRconClient)),
            "scheduler" => serviceProvider.GetService(typeof(API.Scheduling.IScheduler)),
            "config" or "config_manager" => null, // TODO: 实现配置管理器
            "scoreboard" or "scoreboard_manager" => serviceProvider.GetService(typeof(API.Scoreboard.IScoreboardApi)),
            "permissions" or "permission_manager" => serviceProvider.GetService(typeof(API.Permissions.IPermissionManager)),
            "player_data" or "player_data_reader" => serviceProvider.GetService(typeof(API.Data.IPlayerDataReader)),
            "websocket" or "ws" or "websocket_server" => serviceProvider.GetService(typeof(API.WebSocket.IWebSocketServer)),
            _ => null
        };
    }

    /// <summary>
    /// 将 C# 对象转换为 Python 对象
    /// </summary>
    private PyObject ToPython(object obj)
    {
        // 使用 Python.NET 的自动转换
        return obj.ToPython();
    }

    /// <summary>
    /// 从 Python 实例提取插件信息
    /// </summary>
    private PluginInfo ExtractPluginInfo(PyObject instance)
    {
        using (Py.GIL())
        {
            try
            {
                using var info = instance.GetAttr("info");

                var pluginInfo = new PluginInfo
                {
                    Id = GetStringAttr(info, "id") ?? "",
                    Name = GetStringAttr(info, "name") ?? "",
                    Version = GetStringAttr(info, "version") ?? "1.0.0",
                    Description = GetStringAttr(info, "description") ?? "",
                    Author = GetStringAttr(info, "author") ?? "",
                    Website = GetStringAttr(info, "website"),
                    Dependencies = GetListAttr(info, "dependencies"),
                    SoftDependencies = GetListAttr(info, "soft_dependencies"),
                    LoadOrder = GetIntAttr(info, "load_order", 100)
                };

                return pluginInfo;
            }
            catch (Exception ex)
            {
                _logger.Error($"提取插件信息失败: {ex.Message}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// 获取 Python 对象的字符串属性
    /// </summary>
    private string? GetStringAttr(PyObject obj, string attrName)
    {
        try
        {
            if (!obj.HasAttr(attrName))
                return null;

            using var attr = obj.GetAttr(attrName);
            if (attr.IsNone())
                return null;

            return attr.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取 Python 对象的列表属性
    /// </summary>
    private List<string> GetListAttr(PyObject obj, string attrName)
    {
        var result = new List<string>();

        try
        {
            if (!obj.HasAttr(attrName))
                return result;

            using var attr = obj.GetAttr(attrName);
            if (attr.IsNone())
                return result;

            using var iterator = attr.GetIterator();
            while (iterator.MoveNext())
            {
                using var item = iterator.Current;
                var itemStr = item.ToString();
                if (!string.IsNullOrEmpty(itemStr))
                {
                    result.Add(itemStr);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"读取列表属性 {attrName} 失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取 Python 对象的整数属性
    /// </summary>
    private int GetIntAttr(PyObject obj, string attrName, int defaultValue)
    {
        try
        {
            if (!obj.HasAttr(attrName))
                return defaultValue;

            using var attr = obj.GetAttr(attrName);
            if (attr.IsNone())
                return defaultValue;

            return attr.As<int>();
        }
        catch
        {
            return defaultValue;
        }
    }
}

