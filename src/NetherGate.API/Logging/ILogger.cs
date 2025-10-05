namespace NetherGate.API.Logging;

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}

/// <summary>
/// 日志接口
/// </summary>
public interface ILogger
{
    /// <summary>
    /// 记录跟踪日志
    /// </summary>
    void Trace(string message);

    /// <summary>
    /// 记录调试日志
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// 记录信息日志
    /// </summary>
    void Info(string message);

    /// <summary>
    /// 记录警告日志
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// 记录致命错误日志
    /// </summary>
    void Fatal(string message, Exception? exception = null);

    /// <summary>
    /// 是否启用指定日志级别
    /// </summary>
    bool IsEnabled(LogLevel level);
}

/// <summary>
/// 日志工厂接口
/// </summary>
public interface ILoggerFactory
{
    /// <summary>
    /// 创建指定名称的日志器
    /// </summary>
    ILogger CreateLogger(string name);

    /// <summary>
    /// 创建指定类型的日志器
    /// </summary>
    ILogger CreateLogger<T>();
}

