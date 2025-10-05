namespace NetherGate.API.Events;

/// <summary>
/// 服务器事件基类
/// </summary>
public abstract record ServerEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 服务器进程启动事件
/// </summary>
public record ServerProcessStartedEvent : ServerEvent
{
    public int ProcessId { get; init; }
}

/// <summary>
/// 服务器进程停止事件
/// </summary>
public record ServerProcessStoppedEvent : ServerEvent
{
    public int ExitCode { get; init; }
}

/// <summary>
/// 服务器进程崩溃事件
/// </summary>
public record ServerProcessCrashedEvent : ServerEvent
{
    public int ExitCode { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 服务器输出日志事件
/// </summary>
public record ServerLogEvent : ServerEvent
{
    public string LogLevel { get; init; } = "INFO";
    public string Message { get; init; } = string.Empty;
    public string? Thread { get; init; }
    public string? Logger { get; init; }
}

/// <summary>
/// 玩家聊天事件（从日志解析）
/// </summary>
public record PlayerChatEvent : ServerEvent
{
    public string PlayerName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 玩家执行命令事件（从日志解析）
/// </summary>
public record PlayerCommandEvent : ServerEvent
{
    public string PlayerName { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
}

/// <summary>
/// 玩家加入服务器事件（从日志解析）
/// </summary>
public record PlayerJoinedServerEvent : ServerEvent
{
    public string PlayerName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}

/// <summary>
/// 玩家离开服务器事件（从日志解析）
/// </summary>
public record PlayerLeftServerEvent : ServerEvent
{
    public string PlayerName { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

/// <summary>
/// 玩家死亡事件（从日志解析）
/// </summary>
public record PlayerDeathEvent : ServerEvent
{
    public string PlayerName { get; init; } = string.Empty;
    public string DeathMessage { get; init; } = string.Empty;
}

/// <summary>
/// 玩家成就事件（从日志解析）
/// </summary>
public record PlayerAchievementEvent : ServerEvent
{
    public string PlayerName { get; init; } = string.Empty;
    public string Achievement { get; init; } = string.Empty;
}

