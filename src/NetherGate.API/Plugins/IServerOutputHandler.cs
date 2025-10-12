namespace NetherGate.API.Plugins;

/// <summary>
/// 服务器输出处理器接口
/// 允许插件自定义处理服务器的原始输出（stdout/stderr）
/// </summary>
public interface IServerOutputHandler
{
    /// <summary>
    /// 处理器名称（用于标识和日志记录）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 处理器优先级（数值越大越先执行）
    /// 推荐优先级范围:
    /// - 100+: 紧急处理（如服务器崩溃检测）
    /// - 50-99: 高优先级（如玩家事件）
    /// - 10-49: 普通优先级（如统计收集）
    /// - 0-9: 低优先级（如调试信息）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 处理服务器输出行
    /// </summary>
    /// <param name="context">处理器上下文</param>
    /// <returns>处理结果，如果返回 true 则停止后续处理器执行（拦截模式）</returns>
    Task<HandlerResult> HandleAsync(ServerOutputContext context);

    /// <summary>
    /// 判断是否应该处理此消息（可选的快速过滤）
    /// </summary>
    /// <param name="line">原始日志行</param>
    /// <returns>如果返回 false，则跳过 HandleAsync 调用</returns>
    bool ShouldHandle(string line) => true;
}

/// <summary>
/// 服务器输出上下文
/// </summary>
public class ServerOutputContext
{
    /// <summary>
    /// 原始日志行（完整输出）
    /// </summary>
    public string RawLine { get; init; } = string.Empty;

    /// <summary>
    /// 日志消息（剥离了时间戳、线程、级别等前缀）
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 日志级别（INFO, WARN, ERROR 等）
    /// </summary>
    public string Level { get; init; } = "INFO";

    /// <summary>
    /// 线程名称（如果可解析）
    /// </summary>
    public string? ThreadName { get; init; }

    /// <summary>
    /// 时间戳（如果可解析）
    /// </summary>
    public string? Timestamp { get; init; }

    /// <summary>
    /// 输出流类型
    /// </summary>
    public OutputStreamType StreamType { get; init; } = OutputStreamType.StandardOutput;

    /// <summary>
    /// 处理器间共享数据（用于在处理器链之间传递信息）
    /// </summary>
    public Dictionary<string, object> SharedData { get; init; } = new();

    /// <summary>
    /// 是否已被处理（标记为已处理后，后续低优先级处理器可以选择跳过）
    /// </summary>
    public bool IsHandled { get; set; }
}

/// <summary>
/// 输出流类型
/// </summary>
public enum OutputStreamType
{
    /// <summary>
    /// 标准输出 (stdout)
    /// </summary>
    StandardOutput,

    /// <summary>
    /// 标准错误 (stderr)
    /// </summary>
    StandardError
}

/// <summary>
/// 处理器执行结果
/// </summary>
public class HandlerResult
{
    /// <summary>
    /// 是否成功处理
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// 是否应该停止后续处理器（拦截模式）
    /// </summary>
    public bool StopPropagation { get; init; } = false;

    /// <summary>
    /// 处理消息（可选，用于日志记录）
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static HandlerResult Ok(string? message = null) => new() { Success = true, Message = message };

    /// <summary>
    /// 创建成功结果并停止后续处理
    /// </summary>
    public static HandlerResult OkAndStop(string? message = null) => new() { Success = true, StopPropagation = true, Message = message };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static HandlerResult Fail(string message) => new() { Success = false, Message = message };

    /// <summary>
    /// 创建跳过结果（不处理，继续执行后续处理器）
    /// </summary>
    public static HandlerResult Skip() => new() { Success = true, StopPropagation = false };
}

/// <summary>
/// 服务器输出处理器基类
/// 提供常用功能的默认实现
/// </summary>
public abstract class ServerOutputHandlerBase : IServerOutputHandler
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public virtual int Priority => 50;

    /// <inheritdoc/>
    public abstract Task<HandlerResult> HandleAsync(ServerOutputContext context);

    /// <inheritdoc/>
    public virtual bool ShouldHandle(string line) => true;
}

