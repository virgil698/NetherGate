namespace NetherGate.API.Plugins;

/// <summary>
/// 服务器处理器注册表接口
/// 用于管理插件注册的各种处理器
/// </summary>
public interface IServerHandlerRegistry
{
    /// <summary>
    /// 注册服务器输出处理器
    /// </summary>
    /// <param name="handler">处理器实例</param>
    /// <param name="pluginId">所属插件 ID</param>
    void RegisterOutputHandler(IServerOutputHandler handler, string pluginId);

    /// <summary>
    /// 注销服务器输出处理器
    /// </summary>
    /// <param name="handlerName">处理器名称</param>
    /// <param name="pluginId">所属插件 ID</param>
    void UnregisterOutputHandler(string handlerName, string pluginId);

    /// <summary>
    /// 注册服务器消息处理器
    /// </summary>
    /// <param name="processor">处理器实例</param>
    /// <param name="pluginId">所属插件 ID</param>
    Task RegisterMessageProcessorAsync(IServerMessageProcessor processor, string pluginId);

    /// <summary>
    /// 注销服务器消息处理器
    /// </summary>
    /// <param name="processorName">处理器名称</param>
    /// <param name="pluginId">所属插件 ID</param>
    Task UnregisterMessageProcessorAsync(string processorName, string pluginId);

    /// <summary>
    /// 注销插件的所有处理器
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    Task UnregisterAllHandlersAsync(string pluginId);

    /// <summary>
    /// 获取所有已注册的输出处理器
    /// </summary>
    IReadOnlyList<RegisteredHandler<IServerOutputHandler>> GetOutputHandlers();

    /// <summary>
    /// 获取所有已注册的消息处理器
    /// </summary>
    IReadOnlyList<RegisteredHandler<IServerMessageProcessor>> GetMessageProcessors();

    /// <summary>
    /// 获取指定插件的所有处理器
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    HandlerSummary GetPluginHandlers(string pluginId);
}

/// <summary>
/// 已注册的处理器
/// </summary>
public class RegisteredHandler<T> where T : class
{
    /// <summary>
    /// 处理器实例
    /// </summary>
    public T Handler { get; init; } = null!;

    /// <summary>
    /// 所属插件 ID
    /// </summary>
    public string PluginId { get; init; } = string.Empty;

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 处理器摘要
/// </summary>
public class HandlerSummary
{
    /// <summary>
    /// 输出处理器数量
    /// </summary>
    public int OutputHandlerCount { get; init; }

    /// <summary>
    /// 消息处理器数量
    /// </summary>
    public int MessageProcessorCount { get; init; }

    /// <summary>
    /// 输出处理器列表
    /// </summary>
    public List<string> OutputHandlerNames { get; init; } = new();

    /// <summary>
    /// 消息处理器列表
    /// </summary>
    public List<string> MessageProcessorNames { get; init; } = new();
}

