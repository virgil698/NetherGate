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
/// 进程已启动但服务器尚未完全就绪
/// </summary>
public record ServerProcessStartedEvent : ServerEvent
{
    public int ProcessId { get; init; }
}

/// <summary>
/// 服务器启动完成事件
/// 服务器已完全启动并准备接受玩家连接
/// 通过日志解析 "Done (X.XXXs)!" 触发
/// </summary>
public record ServerReadyEvent : ServerEvent
{
    /// <summary>
    /// 启动耗时（秒）
    /// </summary>
    public double StartupTimeSeconds { get; init; }
}

/// <summary>
/// 服务器准备关闭事件（从日志解析）
/// 服务器收到停止命令，正在执行关闭流程
/// 通过日志解析 "Stopping server" 触发
/// 注意: SMP 协议也会推送 ServerStoppingEvent，但基类不同
/// </summary>
public record ServerShuttingDownEvent : ServerEvent
{
}

/// <summary>
/// 服务器进程停止事件
/// 服务器进程已完全退出
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
/// RCON 已连接
/// </summary>
public record RconConnectedEvent : ServerEvent
{
}

/// <summary>
/// RCON 已断开
/// </summary>
public record RconDisconnectedEvent : ServerEvent
{
    public string? Reason { get; init; }
}

/// <summary>
/// 通过统一执行器执行的命令及其回显
/// </summary>
public record ServerCommandResponseEvent : ServerEvent
{
    public string Command { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Response { get; init; }
    public string Channel { get; init; } = "rcon"; // rcon | stdin
}

/// <summary>
/// 系统健康状态事件（周期性发布）
/// </summary>
public record SystemHealthEvent : ServerEvent
{
    public bool ServerProcessRunning { get; init; }
    public bool ServerReady { get; init; }
    public bool RconConnected { get; init; }
    public bool SmpConnected { get; init; }
    public bool WebSocketRunning { get; init; }
    public int PluginsLoaded { get; init; }
    public int? PlayerCount { get; init; }
    public double? Tps1m { get; init; }
    public double? Tps5m { get; init; }
    public double? Tps15m { get; init; }
    public double? MsptAverage { get; init; }
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

