namespace NetherGate.API.Network;

/// <summary>
/// 网络事件监听器接口
/// 用于监听 Minecraft 服务器的底层网络事件
/// </summary>
public interface INetworkEventListener : IDisposable
{
    /// <summary>
    /// 是否正在监听
    /// </summary>
    bool IsListening { get; }
    
    /// <summary>
    /// 监听模式
    /// </summary>
    NetworkListenerMode Mode { get; }
    
    /// <summary>
    /// 开始监听网络事件
    /// </summary>
    Task StartAsync();
    
    /// <summary>
    /// 停止监听网络事件
    /// </summary>
    Task StopAsync();
    
    /// <summary>
    /// 注册事件处理器
    /// </summary>
    void RegisterEventHandler(INetworkEventHandler handler);
    
    /// <summary>
    /// 获取网络统计信息
    /// </summary>
    NetworkStatistics GetStatistics();
}

/// <summary>
/// 网络监听模式
/// </summary>
public enum NetworkListenerMode
{
    /// <summary>
    /// 禁用 - 不监听网络事件
    /// </summary>
    Disabled,
    
    /// <summary>
    /// 日志模式 - 通过解析服务器日志获取基本网络事件
    /// （玩家连接、断开等）
    /// </summary>
    LogBased,
    
    /// <summary>
    /// 插件桥接模式 - 通过 Paper/Spigot 插件转发网络事件
    /// （推荐，支持完整的网络事件）
    /// </summary>
    PluginBridge,
    
    /// <summary>
    /// Mod 桥接模式 - 通过 Fabric/Forge Mod 转发网络事件
    /// （支持最底层的数据包监控）
    /// </summary>
    ModBridge,
    
    /// <summary>
    /// Proxy 模式 - 通过 Velocity/BungeeCord 代理服务器监听
    /// （适用于群组服务器）
    /// </summary>
    ProxyBridge
}

/// <summary>
/// 网络事件处理器接口
/// </summary>
public interface INetworkEventHandler
{
    /// <summary>
    /// 处理玩家连接尝试
    /// </summary>
    Task OnPlayerConnectionAttemptAsync(PlayerConnectionData data);
    
    /// <summary>
    /// 处理玩家登录开始
    /// </summary>
    Task OnPlayerLoginStartAsync(PlayerLoginData data);
    
    /// <summary>
    /// 处理玩家登录成功
    /// </summary>
    Task OnPlayerLoginSuccessAsync(PlayerLoginData data);
    
    /// <summary>
    /// 处理玩家登录失败
    /// </summary>
    Task OnPlayerLoginFailedAsync(PlayerLoginFailureData data);
    
    /// <summary>
    /// 处理玩家断开连接
    /// </summary>
    Task OnPlayerDisconnectedAsync(PlayerDisconnectData data);
    
    /// <summary>
    /// 处理数据包接收（可选，仅在 ModBridge 模式下）
    /// </summary>
    Task OnPacketReceivedAsync(PacketData data);
    
    /// <summary>
    /// 处理数据包发送（可选，仅在 ModBridge 模式下）
    /// </summary>
    Task OnPacketSentAsync(PacketData data);
    
    /// <summary>
    /// 处理网络异常
    /// </summary>
    Task OnNetworkExceptionAsync(NetworkExceptionData data);
}

/// <summary>
/// 玩家连接数据
/// </summary>
public record PlayerConnectionData
{
    /// <summary>IP 地址</summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>端口号</summary>
    public int Port { get; init; }
    
    /// <summary>协议版本</summary>
    public int ProtocolVersion { get; init; }
    
    /// <summary>服务器地址</summary>
    public string ServerAddress { get; init; } = string.Empty;
    
    /// <summary>下一状态</summary>
    public int NextState { get; init; }
    
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 玩家登录数据
/// </summary>
public record PlayerLoginData
{
    /// <summary>玩家名称</summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>玩家 UUID</summary>
    public Guid? PlayerUuid { get; init; }
    
    /// <summary>IP 地址</summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 玩家登录失败数据
/// </summary>
public record PlayerLoginFailureData
{
    /// <summary>玩家名称</summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>失败原因</summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>IP 地址</summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 玩家断开连接数据
/// </summary>
public record PlayerDisconnectData
{
    /// <summary>玩家名称</summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>玩家 UUID</summary>
    public Guid? PlayerUuid { get; init; }
    
    /// <summary>断开原因</summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>IP 地址</summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 数据包数据
/// </summary>
public record PacketData
{
    /// <summary>数据包 ID</summary>
    public int PacketId { get; init; }
    
    /// <summary>数据包类型</summary>
    public string PacketType { get; init; } = string.Empty;
    
    /// <summary>数据长度</summary>
    public int DataLength { get; init; }
    
    /// <summary>玩家名称</summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>玩家 UUID</summary>
    public Guid? PlayerUuid { get; init; }
    
    /// <summary>原始数据</summary>
    public byte[]? RawData { get; init; }
    
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 网络异常数据
/// </summary>
public record NetworkExceptionData
{
    /// <summary>玩家名称</summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>IP 地址</summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>异常类型</summary>
    public string ExceptionType { get; init; } = string.Empty;
    
    /// <summary>异常消息</summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>堆栈跟踪</summary>
    public string? StackTrace { get; init; }
    
    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 网络统计信息
/// </summary>
public record NetworkStatistics
{
    /// <summary>
    /// 总接收字节数
    /// </summary>
    public long TotalBytesReceived { get; init; }
    
    /// <summary>
    /// 总发送字节数
    /// </summary>
    public long TotalBytesSent { get; init; }
    
    /// <summary>
    /// 总接收数据包数
    /// </summary>
    public long TotalPacketsReceived { get; init; }
    
    /// <summary>
    /// 总发送数据包数
    /// </summary>
    public long TotalPacketsSent { get; init; }
    
    /// <summary>
    /// 当前连接数
    /// </summary>
    public int CurrentConnections { get; init; }
    
    /// <summary>
    /// 总连接数（历史）
    /// </summary>
    public long TotalConnections { get; init; }
    
    /// <summary>
    /// 失败连接数
    /// </summary>
    public long FailedConnections { get; init; }
    
    /// <summary>
    /// 平均延迟（毫秒）
    /// </summary>
    public double AverageLatencyMs { get; init; }
    
    /// <summary>
    /// 统计开始时间
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdate { get; init; } = DateTime.UtcNow;
}
