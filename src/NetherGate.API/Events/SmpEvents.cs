using NetherGate.API.Protocol;

namespace NetherGate.API.Events;

/// <summary>
/// SMP 事件基类
/// </summary>
public abstract record SmpEvent
{
    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 事件类型
    /// </summary>
    public abstract string EventType { get; }
}

// ========== 白名单事件 ==========

/// <summary>
/// 白名单变更事件
/// </summary>
public record AllowlistChangedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "allowlist_changed";
    
    /// <summary>
    /// 变更的玩家列表
    /// </summary>
    public List<PlayerDto> Players { get; init; } = new();
    
    /// <summary>
    /// 操作类型：added, removed, set, cleared
    /// </summary>
    public string Operation { get; init; } = string.Empty;
}

// ========== 封禁事件 ==========

/// <summary>
/// 玩家封禁事件
/// </summary>
public record PlayerBannedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "player_banned";
    
    /// <summary>
    /// 封禁信息
    /// </summary>
    public UserBanDto Ban { get; init; } = new();
}

/// <summary>
/// 玩家解封事件
/// </summary>
public record PlayerUnbannedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "player_unbanned";
    
    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// IP 封禁事件
/// </summary>
public record IpBannedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "ip_banned";
    
    /// <summary>
    /// 封禁信息
    /// </summary>
    public IpBanDto Ban { get; init; } = new();
}

/// <summary>
/// IP 解封事件
/// </summary>
public record IpUnbannedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "ip_unbanned";
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string Ip { get; init; } = string.Empty;
}

// ========== 玩家事件 ==========

/// <summary>
/// 玩家加入事件
/// </summary>
public record PlayerJoinedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "player_joined";
    
    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// 玩家离开事件
/// </summary>
public record PlayerLeftEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "player_left";
    
    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// 玩家被踢出事件
/// </summary>
public record PlayerKickedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "player_kicked";
    
    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayerDto Player { get; init; } = new();
    
    /// <summary>
    /// 踢出原因
    /// </summary>
    public string? Reason { get; init; }
}

// ========== 管理员事件 ==========

/// <summary>
/// 管理员添加事件
/// </summary>
public record OperatorAddedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "operator_added";
    
    /// <summary>
    /// 管理员信息
    /// </summary>
    public OperatorDto Operator { get; init; } = new();
}

/// <summary>
/// 管理员移除事件
/// </summary>
public record OperatorRemovedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "operator_removed";
    
    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayerDto Player { get; init; } = new();
}

// ========== 服务器事件 ==========

/// <summary>
/// 服务器启动事件
/// </summary>
public record ServerStartedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "server_started";
    
    /// <summary>
    /// 版本信息
    /// </summary>
    public VersionInfo Version { get; init; } = new();
}

/// <summary>
/// 服务器停止事件
/// </summary>
public record ServerStoppingEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "server_stopping";
}

/// <summary>
/// 服务器崩溃事件
/// </summary>
public record ServerCrashedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "server_crashed";
    
    /// <summary>
    /// 崩溃原因
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// 世界保存事件
/// </summary>
public record WorldSavedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "world_saved";
}

// ========== 游戏规则事件 ==========

/// <summary>
/// 游戏规则变更事件
/// </summary>
public record GameRuleChangedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "gamerule_changed";
    
    /// <summary>
    /// 游戏规则名称
    /// </summary>
    public string Rule { get; init; } = string.Empty;
    
    /// <summary>
    /// 旧值
    /// </summary>
    public object? OldValue { get; init; }
    
    /// <summary>
    /// 新值
    /// </summary>
    public object? NewValue { get; init; }
}

// ========== 服务器设置事件 ==========

/// <summary>
/// 服务器设置变更事件
/// </summary>
public record ServerSettingChangedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "server_setting_changed";
    
    /// <summary>
    /// 设置键名
    /// </summary>
    public string Key { get; init; } = string.Empty;
    
    /// <summary>
    /// 旧值
    /// </summary>
    public object? OldValue { get; init; }
    
    /// <summary>
    /// 新值
    /// </summary>
    public object? NewValue { get; init; }
}

// ========== 连接事件 ==========

/// <summary>
/// SMP 连接建立事件
/// </summary>
public record SmpConnectedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "smp_connected";
}

/// <summary>
/// SMP 连接断开事件
/// </summary>
public record SmpDisconnectedEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "smp_disconnected";
    
    /// <summary>
    /// 断开原因
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// SMP 重连事件
/// </summary>
public record SmpReconnectingEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "smp_reconnecting";
    
    /// <summary>
    /// 当前重试次数
    /// </summary>
    public int Attempt { get; init; }
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxAttempts { get; init; }
}

// ========== 服务器心跳事件 ==========

/// <summary>
/// 服务器心跳事件
/// 定期接收服务器状态更新（通过配置 status-heartbeat-interval 启用）
/// </summary>
public record ServerHeartbeatEvent : SmpEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public override string EventType => "server_heartbeat";
    
    /// <summary>
    /// 服务器当前状态
    /// </summary>
    public ServerState Status { get; init; } = new();
    
    /// <summary>
    /// 心跳间隔（秒）
    /// 对应配置项 status-heartbeat-interval
    /// </summary>
    public int IntervalSeconds { get; init; }
}

