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
    public override string EventType => "player_banned";
    
    public UserBanDto Ban { get; init; } = new();
}

/// <summary>
/// 玩家解封事件
/// </summary>
public record PlayerUnbannedEvent : SmpEvent
{
    public override string EventType => "player_unbanned";
    
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// IP 封禁事件
/// </summary>
public record IpBannedEvent : SmpEvent
{
    public override string EventType => "ip_banned";
    
    public IpBanDto Ban { get; init; } = new();
}

/// <summary>
/// IP 解封事件
/// </summary>
public record IpUnbannedEvent : SmpEvent
{
    public override string EventType => "ip_unbanned";
    
    public string Ip { get; init; } = string.Empty;
}

// ========== 玩家事件 ==========

/// <summary>
/// 玩家加入事件
/// </summary>
public record PlayerJoinedEvent : SmpEvent
{
    public override string EventType => "player_joined";
    
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// 玩家离开事件
/// </summary>
public record PlayerLeftEvent : SmpEvent
{
    public override string EventType => "player_left";
    
    public PlayerDto Player { get; init; } = new();
}

/// <summary>
/// 玩家被踢出事件
/// </summary>
public record PlayerKickedEvent : SmpEvent
{
    public override string EventType => "player_kicked";
    
    public PlayerDto Player { get; init; } = new();
    public string? Reason { get; init; }
}

// ========== 管理员事件 ==========

/// <summary>
/// 管理员添加事件
/// </summary>
public record OperatorAddedEvent : SmpEvent
{
    public override string EventType => "operator_added";
    
    public OperatorDto Operator { get; init; } = new();
}

/// <summary>
/// 管理员移除事件
/// </summary>
public record OperatorRemovedEvent : SmpEvent
{
    public override string EventType => "operator_removed";
    
    public PlayerDto Player { get; init; } = new();
}

// ========== 服务器事件 ==========

/// <summary>
/// 服务器启动事件
/// </summary>
public record ServerStartedEvent : SmpEvent
{
    public override string EventType => "server_started";
    
    public VersionInfo Version { get; init; } = new();
}

/// <summary>
/// 服务器停止事件
/// </summary>
public record ServerStoppingEvent : SmpEvent
{
    public override string EventType => "server_stopping";
}

/// <summary>
/// 服务器崩溃事件
/// </summary>
public record ServerCrashedEvent : SmpEvent
{
    public override string EventType => "server_crashed";
    
    public string? Reason { get; init; }
}

/// <summary>
/// 世界保存事件
/// </summary>
public record WorldSavedEvent : SmpEvent
{
    public override string EventType => "world_saved";
}

// ========== 游戏规则事件 ==========

/// <summary>
/// 游戏规则变更事件
/// </summary>
public record GameRuleChangedEvent : SmpEvent
{
    public override string EventType => "gamerule_changed";
    
    public string Rule { get; init; } = string.Empty;
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

// ========== 服务器设置事件 ==========

/// <summary>
/// 服务器设置变更事件
/// </summary>
public record ServerSettingChangedEvent : SmpEvent
{
    public override string EventType => "server_setting_changed";
    
    public string Key { get; init; } = string.Empty;
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

// ========== 连接事件 ==========

/// <summary>
/// SMP 连接建立事件
/// </summary>
public record SmpConnectedEvent : SmpEvent
{
    public override string EventType => "smp_connected";
}

/// <summary>
/// SMP 连接断开事件
/// </summary>
public record SmpDisconnectedEvent : SmpEvent
{
    public override string EventType => "smp_disconnected";
    
    public string? Reason { get; init; }
}

/// <summary>
/// SMP 重连事件
/// </summary>
public record SmpReconnectingEvent : SmpEvent
{
    public override string EventType => "smp_reconnecting";
    
    public int Attempt { get; init; }
    public int MaxAttempts { get; init; }
}

