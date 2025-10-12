using NetherGate.API.Events;

namespace NetherGate.API.Plugins;

/// <summary>
/// 服务器消息处理器接口（高级）
/// 提供比 IServerOutputHandler 更高级的功能，包括异步处理、批量处理等
/// </summary>
public interface IServerMessageProcessor
{
    /// <summary>
    /// 处理器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 处理器优先级
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 初始化处理器
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 处理单条消息
    /// </summary>
    Task ProcessMessageAsync(ServerMessage message);

    /// <summary>
    /// 批量处理消息（可选，用于性能优化）
    /// </summary>
    /// <param name="messages">消息批次</param>
    async Task ProcessBatchAsync(IReadOnlyList<ServerMessage> messages)
    {
        // 默认实现：逐条处理
        foreach (var message in messages)
        {
            await ProcessMessageAsync(message);
        }
    }

    /// <summary>
    /// 关闭处理器（清理资源）
    /// </summary>
    Task ShutdownAsync();

    /// <summary>
    /// 判断是否应该处理此消息
    /// </summary>
    bool ShouldProcess(ServerMessage message);
}

/// <summary>
/// 服务器消息
/// </summary>
public class ServerMessage
{
    /// <summary>
    /// 消息 ID（用于追踪）
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 原始日志行
    /// </summary>
    public string RawLine { get; init; } = string.Empty;

    /// <summary>
    /// 解析后的消息内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 消息级别
    /// </summary>
    public ServerMessageLevel Level { get; init; } = ServerMessageLevel.Info;

    /// <summary>
    /// 消息类型（如果可识别）
    /// </summary>
    public ServerMessageType Type { get; init; } = ServerMessageType.Unknown;

    /// <summary>
    /// 接收时间
    /// </summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的事件（如果已解析为事件）
    /// </summary>
    public ServerEvent? AssociatedEvent { get; set; }

    /// <summary>
    /// 元数据（扩展字段）
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// 服务器消息级别
/// </summary>
public enum ServerMessageLevel
{
    /// <summary>
    /// 跟踪级别
    /// </summary>
    Trace,
    
    /// <summary>
    /// 调试级别
    /// </summary>
    Debug,
    
    /// <summary>
    /// 信息级别
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告级别
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误级别
    /// </summary>
    Error,
    
    /// <summary>
    /// 致命错误级别
    /// </summary>
    Fatal
}

/// <summary>
/// 服务器消息类型
/// </summary>
public enum ServerMessageType
{
    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 服务器启动
    /// </summary>
    ServerStartup,
    
    /// <summary>
    /// 服务器关闭
    /// </summary>
    ServerShutdown,
    
    /// <summary>
    /// 玩家加入
    /// </summary>
    PlayerJoin,
    
    /// <summary>
    /// 玩家离开
    /// </summary>
    PlayerLeave,
    
    /// <summary>
    /// 玩家聊天
    /// </summary>
    PlayerChat,
    
    /// <summary>
    /// 玩家命令
    /// </summary>
    PlayerCommand,
    
    /// <summary>
    /// 玩家死亡
    /// </summary>
    PlayerDeath,
    
    /// <summary>
    /// 玩家成就
    /// </summary>
    PlayerAchievement,
    
    /// <summary>
    /// 世界保存
    /// </summary>
    WorldSave,
    
    /// <summary>
    /// 区块加载
    /// </summary>
    ChunkLoad,
    
    /// <summary>
    /// 插件消息
    /// </summary>
    PluginMessage,
    
    /// <summary>
    /// 性能相关
    /// </summary>
    Performance,
    
    /// <summary>
    /// 错误消息
    /// </summary>
    Error,
    
    /// <summary>
    /// 自定义类型
    /// </summary>
    Custom
}

/// <summary>
/// 服务器消息处理器基类
/// </summary>
public abstract class ServerMessageProcessorBase : IServerMessageProcessor
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public virtual int Priority => 50;

    /// <inheritdoc/>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public abstract Task ProcessMessageAsync(ServerMessage message);

    /// <inheritdoc/>
    public virtual Task ShutdownAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual bool ShouldProcess(ServerMessage message) => true;
}

