namespace NetherGate.API.Events;

/// <summary>
/// 服务器事件基类
/// </summary>
public abstract record ServerEvent
{
    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 服务器进程启动事件
/// 进程已启动但服务器尚未完全就绪
/// </summary>
public record ServerProcessStartedEvent : ServerEvent
{
    /// <summary>
    /// 进程 ID
    /// </summary>
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
    /// <summary>
    /// 退出代码
    /// </summary>
    public int ExitCode { get; init; }
}

/// <summary>
/// 服务器进程崩溃事件
/// </summary>
public record ServerProcessCrashedEvent : ServerEvent
{
    /// <summary>
    /// 退出代码
    /// </summary>
    public int ExitCode { get; init; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 服务器输出日志事件
/// </summary>
public record ServerLogEvent : ServerEvent
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public string LogLevel { get; init; } = "INFO";
    
    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// 线程名称
    /// </summary>
    public string? Thread { get; init; }
    
    /// <summary>
    /// 日志记录器名称
    /// </summary>
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
    /// <summary>
    /// 断开原因
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// 通过统一执行器执行的命令及其回显
/// </summary>
public record ServerCommandResponseEvent : ServerEvent
{
    /// <summary>
    /// 执行的命令
    /// </summary>
    public string Command { get; init; } = string.Empty;
    
    /// <summary>
    /// 是否执行成功
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// 命令响应
    /// </summary>
    public string? Response { get; init; }
    
    /// <summary>
    /// 执行通道 (rcon | stdin)
    /// </summary>
    public string Channel { get; init; } = "rcon";
}

/// <summary>
/// 系统健康状态事件（周期性发布）
/// </summary>
public record SystemHealthEvent : ServerEvent
{
    /// <summary>
    /// 服务器进程是否正在运行
    /// </summary>
    public bool ServerProcessRunning { get; init; }
    
    /// <summary>
    /// 服务器是否已就绪
    /// </summary>
    public bool ServerReady { get; init; }
    
    /// <summary>
    /// RCON 是否已连接
    /// </summary>
    public bool RconConnected { get; init; }
    
    /// <summary>
    /// SMP 是否已连接
    /// </summary>
    public bool SmpConnected { get; init; }
    
    /// <summary>
    /// WebSocket 是否正在运行
    /// </summary>
    public bool WebSocketRunning { get; init; }
    
    /// <summary>
    /// 已加载的插件数量
    /// </summary>
    public int PluginsLoaded { get; init; }
    
    /// <summary>
    /// 玩家数量
    /// </summary>
    public int? PlayerCount { get; init; }
    
    /// <summary>
    /// 1分钟 TPS
    /// </summary>
    public double? Tps1m { get; init; }
    
    /// <summary>
    /// 5分钟 TPS
    /// </summary>
    public double? Tps5m { get; init; }
    
    /// <summary>
    /// 15分钟 TPS
    /// </summary>
    public double? Tps15m { get; init; }
    
    /// <summary>
    /// 平均 MSPT (毫秒每tick)
    /// </summary>
    public double? MsptAverage { get; init; }
}

/// <summary>
/// 玩家聊天事件（从日志解析）
/// </summary>
public record PlayerChatEvent : ServerEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 聊天消息
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 玩家执行命令事件（从日志解析）
/// </summary>
public record PlayerCommandEvent : ServerEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 执行的命令
    /// </summary>
    public string Command { get; init; } = string.Empty;
}

/// <summary>
/// 玩家加入服务器事件（从日志解析）
/// </summary>
public record PlayerJoinedServerEvent : ServerEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// IP 地址
    /// </summary>
    public string? IpAddress { get; init; }
}

/// <summary>
/// 玩家离开服务器事件（从日志解析）
/// </summary>
public record PlayerLeftServerEvent : ServerEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 离开原因
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// 玩家死亡事件（从日志解析）
/// </summary>
public record PlayerDeathEvent : ServerEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 死亡消息
    /// </summary>
    public string DeathMessage { get; init; } = string.Empty;
}

/// <summary>
/// 玩家成就事件（从日志解析）
/// </summary>
public record PlayerAchievementEvent : ServerEvent
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;
    
    /// <summary>
    /// 成就名称
    /// </summary>
    public string Achievement { get; init; } = string.Empty;
}

