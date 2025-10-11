using Jint;
using Jint.Runtime;
using NetherGate.API.Logging;

namespace NetherGate.Script;

/// <summary>
/// JavaScript 运行时
/// 负责管理 Jint 引擎生命周期
/// </summary>
public class JavaScriptRuntime
{
    private readonly ILogger _logger;
    private Engine? _engine;
    private readonly object _lock = new();

    public JavaScriptRuntime(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化 JavaScript 引擎
    /// </summary>
    public void Initialize()
    {
        lock (_lock)
        {
            if (_engine != null)
            {
                _logger.Warning("JavaScript 引擎已经初始化");
                return;
            }

            _engine = new Engine(options =>
            {
                // 允许访问 CLR 类型（限制仅访问 NetherGate.API）
                options.AllowClr();

                // 执行超时保护（10秒）
                options.TimeoutInterval(TimeSpan.FromSeconds(10));

                // 启用严格模式
                options.Strict();
            });

            // 设置 console 对象
            _engine.SetValue("console", new
            {
                log = new Action<object>(obj => _logger.Info(obj?.ToString() ?? "null")),
                debug = new Action<object>(obj => _logger.Debug(obj?.ToString() ?? "null")),
                warn = new Action<object>(obj => _logger.Warning(obj?.ToString() ?? "null")),
                error = new Action<object>(obj => _logger.Error(obj?.ToString() ?? "null"))
            });

            _logger.Info("JavaScript 引擎已初始化");
        }
    }

    /// <summary>
    /// 执行 JavaScript 代码
    /// </summary>
    public object? Execute(string code, string? fileName = null)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化，请先调用 Initialize()");
        }

        try
        {
            var result = _engine.Evaluate(code, fileName ?? "script.js");
            return result.ToObject();
        }
        catch (JavaScriptException ex)
        {
            _logger.Error($"JavaScript 执行错误 [{fileName ?? "script.js"}:{ex.Location.Start.Line}]: {ex.Message}");
            throw new InvalidOperationException($"JavaScript 执行错误: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error($"JavaScript 执行失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 加载模块（执行 JavaScript 文件）
    /// </summary>
    public void LoadModule(string modulePath, string moduleContent)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }

        try
        {
            _logger.Debug($"加载 JavaScript 模块: {modulePath}");
            _engine.Execute(moduleContent, modulePath);
        }
        catch (JavaScriptException ex)
        {
            _logger.Error($"加载模块失败 [{modulePath}:{ex.Location.Start.Line}]: {ex.Message}");
            throw new InvalidOperationException($"加载模块失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 设置全局变量
    /// </summary>
    public void SetGlobal(string name, object? value)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }

        _engine.SetValue(name, value);
        _logger.Trace($"设置全局变量: {name} = {value}");
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

        var value = _engine.GetValue(name);
        return value.IsUndefined() ? null : value.ToObject();
    }

    /// <summary>
    /// 获取 Jint 引擎实例（用于高级操作）
    /// </summary>
    public Engine GetEngine()
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("JavaScript 引擎未初始化");
        }

        return _engine;
    }

    /// <summary>
    /// 检查引擎是否已初始化
    /// </summary>
    public bool IsInitialized => _engine != null;

    /// <summary>
    /// 关闭引擎
    /// </summary>
    public void Shutdown()
    {
        lock (_lock)
        {
            if (_engine != null)
            {
                _engine = null;
                _logger.Info("JavaScript 引擎已关闭");
            }
        }
    }
}

