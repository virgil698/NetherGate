using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using NetherGate.API.Data;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Permissions;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;
using NetherGate.API.Scheduling;
using NetherGate.API.Scoreboard;
using NetherGate.API.WebSocket;
using NetherGate.Script.Wrappers;

namespace NetherGate.Script;

/// <summary>
/// JavaScript 插件适配器
/// 将 JavaScript 插件适配为 IPlugin 接口
/// </summary>
public class JavaScriptPluginAdapter : IPlugin
{
    private readonly ILogger _logger;
    private readonly JavaScriptRuntime _jsRuntime;
    private readonly string _pluginDirectory;
    private JsValue _pluginInstance;
    private readonly EventBusWrapper? _eventBusWrapper;

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
        _pluginInstance = JsValue.Undefined;

        try
        {
            var engine = _jsRuntime.GetEngine();

            // 初始化 module.exports 支持
            engine.Execute(@"
                var module = { exports: {} };
                var exports = module.exports;
            ");

            // 1. 加载脚本
            _jsRuntime.LoadModule(scriptPath, scriptContent);

            // 2. 获取 module.exports (插件类)
            var moduleValue = engine.GetValue("module");
            if (!moduleValue.IsObject())
            {
                throw new InvalidOperationException("module 不是对象");
            }

            var moduleObj = moduleValue.AsObject();
            var exportsValue = moduleObj.Get("exports");

            if (exportsValue.IsUndefined() || exportsValue.IsNull())
            {
                throw new InvalidOperationException($"插件主文件必须导出一个类: {scriptPath}");
            }

            // 检查是否是对象（应该是函数）
            if (!exportsValue.IsObject())
            {
                throw new InvalidOperationException($"插件主文件必须导出一个构造函数: {scriptPath}");
            }

            // 3. 依赖注入：创建插件实例
            _pluginInstance = CreatePluginInstance(exportsValue, serviceProvider, out var wrapper);
            _eventBusWrapper = wrapper;

            // 4. 读取插件信息
            Info = ExtractPluginInfo(_pluginInstance);

            _logger.Debug($"JavaScript 插件适配器已创建: {Info.Name}");
        }
        catch (Exception ex)
        {
            _logger.Error($"创建 JavaScript 插件适配器失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 创建插件实例（支持依赖注入）
    /// </summary>
    private JsValue CreatePluginInstance(
        JsValue pluginClass,
        IServiceProvider serviceProvider,
        out EventBusWrapper? eventBusWrapper)
    {
        eventBusWrapper = null;

        try
        {
            // 简化版本：直接注入 Logger 和 EventBus
            var logger = serviceProvider.GetService(typeof(ILogger)) as ILogger;
            var eventBus = serviceProvider.GetService(typeof(IEventBus)) as IEventBus;

            var engine = _jsRuntime.GetEngine();

            // 创建包装器
            var loggerWrapper = logger != null ? new LoggerWrapper(logger) : null;
            EventBusWrapper? ebWrapper = null;
            if (eventBus != null)
            {
                ebWrapper = new EventBusWrapper(eventBus);
                eventBusWrapper = ebWrapper;
            }

            // 准备构造函数参数
            var args = new JsValue[] 
            { 
                loggerWrapper != null ? JsValue.FromObject(engine, loggerWrapper) : JsValue.Undefined,
                ebWrapper != null ? JsValue.FromObject(engine, ebWrapper) : JsValue.Undefined
            };

            // 使用 Engine.Construct 方法调用构造函数
            var instance = engine.Construct(pluginClass, args);

            if (instance.IsUndefined() || instance.IsNull())
            {
                throw new InvalidOperationException("构造函数未返回有效的对象实例");
            }

            return instance;
        }
        catch (Exception ex)
        {
            _logger.Error($"调用插件构造函数失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 提取插件信息
    /// </summary>
    private PluginInfo ExtractPluginInfo(JsValue pluginInstance)
    {
        try
        {
            if (!pluginInstance.IsObject())
            {
                throw new InvalidOperationException("插件实例不是对象");
            }

            var obj = pluginInstance.AsObject();
            var infoValue = obj.Get("info");

            if (infoValue.IsUndefined() || !infoValue.IsObject())
            {
                throw new InvalidOperationException("插件必须包含 info 属性");
            }

            var info = infoValue.AsObject();

            return new PluginInfo
            {
                Id = GetStringProperty(info, "id") ?? throw new InvalidOperationException("插件 info.id 不能为空"),
                Name = GetStringProperty(info, "name") ?? throw new InvalidOperationException("插件 info.name 不能为空"),
                Version = GetStringProperty(info, "version") ?? "1.0.0",
                Description = GetStringProperty(info, "description") ?? "",
                Author = GetStringProperty(info, "author") ?? "Unknown",
                Website = GetStringProperty(info, "website"),
                Dependencies = new List<string>(),
                SoftDependencies = new List<string>(),
                LoadOrder = 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"提取插件信息失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 从对象中获取字符串属性
    /// </summary>
    private string? GetStringProperty(ObjectInstance obj, string propertyName)
    {
        var value = obj.Get(propertyName);
        if (value.IsUndefined() || value.IsNull())
        {
            return null;
        }

        return value.ToString();
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

        // 清理事件监听器
        _eventBusWrapper?.clearAll();
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
            if (!_pluginInstance.IsObject())
            {
                return;
            }

            var obj = _pluginInstance.AsObject();
            var method = obj.Get(methodName);

            if (method.IsUndefined())
            {
                // 方法未定义，跳过
                _logger.Trace($"方法 {methodName} 未定义，跳过");
                return;
            }

            // 检查是否是对象（函数也是对象）
            if (!method.IsObject())
            {
                _logger.Warning($"方法 {methodName} 不是对象/函数");
                return;
            }

            // 使用 Engine.Invoke 调用方法
            var engine = _jsRuntime.GetEngine();
            var result = engine.Invoke(method, _pluginInstance);

            // 简化：暂不处理 Promise，假设是同步方法
            // TODO: 后续可以添加 Promise 检测和等待逻辑
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error($"调用插件方法 {methodName} 失败: {ex.Message}", ex);
            throw;
        }
    }
}
