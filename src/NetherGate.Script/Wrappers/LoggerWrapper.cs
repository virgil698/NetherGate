using NetherGate.API.Logging;

namespace NetherGate.Script.Wrappers;

/// <summary>
/// Logger 的 JavaScript 包装器
/// 提供小写方法名以符合 JavaScript 命名约定
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
    public void warn(string message) => _logger.Warning(message);
    public void error(string message) => _logger.Error(message);
}

