namespace NetherGate.API.Events;

/// <summary>
/// 网络事件基类
/// </summary>
public abstract record NetworkEvent
{
    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// ========== 连接事件 ==========

/// <summary>
/// 玩家连接事件（握手阶段）
/// </summary>
public record PlayerConnectionAttemptEvent : NetworkEvent
{
    /// <summary>
    /// 连接的 IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// 连接端口
    /// </summary>
    public int Port { get; init; }
    
    /// <summary>
    /// 协议版本
    /// </summary>
    public int ProtocolVersion { get; init; }
    
    /// <summary>
    /// 服务器地址（玩家输入的地址）
    /// </summary>
    public string ServerAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// 连接状态（1=状态查询, 2=登录）
    /// </summary>
    public int NextState { get; init; }
}

/// <summary>
/// 玩家登录开始事件
/// </summary>
public record PlayerLoginStartEvent : NetworkEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid? PlayerUuid { get; init; }
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
}

/// <summary>
/// 玩家登录成功事件（已验证）
/// </summary>
public record PlayerLoginSuccessEvent : NetworkEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid PlayerUuid { get; init; }
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
}

/// <summary>
/// 玩家登录失败事件
/// </summary>
public record PlayerLoginFailedEvent : NetworkEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 失败原因
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
}

/// <summary>
/// 玩家断开连接事件（网络层）
/// </summary>
public record PlayerDisconnectedEvent : NetworkEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid? PlayerUuid { get; init; }
    
    /// <summary>
    /// 断开原因
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
}

// ========== 数据包事件 ==========

/// <summary>
/// 接收数据包事件（低级）
/// </summary>
public record PacketReceivedEvent : NetworkEvent
{
    /// <summary>
    /// 数据包 ID
    /// </summary>
    public int PacketId { get; init; }
    
    /// <summary>
    /// 数据包类型
    /// </summary>
    public string PacketType { get; init; } = string.Empty;
    
    /// <summary>
    /// 数据长度
    /// </summary>
    public int DataLength { get; init; }
    
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid? PlayerUuid { get; init; }
}

/// <summary>
/// 发送数据包事件（低级）
/// </summary>
public record PacketSentEvent : NetworkEvent
{
    /// <summary>
    /// 数据包 ID
    /// </summary>
    public int PacketId { get; init; }
    
    /// <summary>
    /// 数据包类型
    /// </summary>
    public string PacketType { get; init; } = string.Empty;
    
    /// <summary>
    /// 数据长度
    /// </summary>
    public int DataLength { get; init; }
    
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid? PlayerUuid { get; init; }
}

// ========== 状态查询事件 ==========

/// <summary>
/// 服务器状态查询事件（Server List Ping）
/// </summary>
public record ServerStatusQueryEvent : NetworkEvent
{
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// 协议版本
    /// </summary>
    public int ProtocolVersion { get; init; }
}

/// <summary>
/// 服务器 Ping 测试事件
/// </summary>
public record ServerPingEvent : NetworkEvent
{
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// Ping 载荷
    /// </summary>
    public long Payload { get; init; }
}

// ========== 异常事件 ==========

/// <summary>
/// 网络异常事件
/// </summary>
public record NetworkExceptionEvent : NetworkEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// 异常类型
    /// </summary>
    public string ExceptionType { get; init; } = string.Empty;
    
    /// <summary>
    /// 异常消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// 堆栈跟踪
    /// </summary>
    public string? StackTrace { get; init; }
}

/// <summary>
/// 恶意数据包检测事件
/// </summary>
public record MaliciousPacketDetectedEvent : NetworkEvent
{
    /// <summary>
    /// IP 地址
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;
    
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 数据包类型
    /// </summary>
    public string PacketType { get; init; } = string.Empty;
    
    /// <summary>
    /// 检测原因
    /// </summary>
    public string Reason { get; init; } = string.Empty;
    
    /// <summary>
    /// 是否已拦截
    /// </summary>
    public bool WasBlocked { get; init; }
}

// ========== 带宽监控事件 ==========

/// <summary>
/// 网络流量统计事件
/// </summary>
public record NetworkTrafficEvent : NetworkEvent
{
    /// <summary>
    /// 统计周期开始时间
    /// </summary>
    public DateTime PeriodStart { get; init; }
    
    /// <summary>
    /// 统计周期结束时间
    /// </summary>
    public DateTime PeriodEnd { get; init; }
    
    /// <summary>
    /// 接收字节数
    /// </summary>
    public long BytesReceived { get; init; }
    
    /// <summary>
    /// 发送字节数
    /// </summary>
    public long BytesSent { get; init; }
    
    /// <summary>
    /// 接收数据包数
    /// </summary>
    public long PacketsReceived { get; init; }
    
    /// <summary>
    /// 发送数据包数
    /// </summary>
    public long PacketsSent { get; init; }
    
    /// <summary>
    /// 当前在线玩家数
    /// </summary>
    public int OnlinePlayerCount { get; init; }
}
