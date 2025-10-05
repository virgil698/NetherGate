namespace NetherGate.API.Network;

/// <summary>
/// Minecraft 服务器监控接口
/// 持续监控多个 Minecraft 服务器的状态
/// </summary>
public interface IServerMonitor
{
    /// <summary>
    /// 是否正在监控
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// 添加要监控的服务器
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="checkInterval">检查间隔（秒，默认 60）</param>
    void AddServer(ServerAddress address, int checkInterval = 60);

    /// <summary>
    /// 移除监控的服务器
    /// </summary>
    /// <param name="host">服务器主机名</param>
    /// <param name="port">服务器端口</param>
    void RemoveServer(string host, int port = 25565);

    /// <summary>
    /// 获取所有监控的服务器
    /// </summary>
    List<MonitoredServer> GetMonitoredServers();

    /// <summary>
    /// 获取服务器的最新状态
    /// </summary>
    /// <param name="host">服务器主机名</param>
    /// <param name="port">服务器端口</param>
    ServerStatusResponse? GetServerStatus(string host, int port = 25565);

    /// <summary>
    /// 立即刷新所有服务器状态
    /// </summary>
    Task RefreshAllAsync();

    /// <summary>
    /// 立即刷新指定服务器状态
    /// </summary>
    /// <param name="host">服务器主机名</param>
    /// <param name="port">服务器端口</param>
    Task RefreshServerAsync(string host, int port = 25565);

    /// <summary>
    /// 开始监控
    /// </summary>
    void Start();

    /// <summary>
    /// 停止监控
    /// </summary>
    void Stop();

    /// <summary>
    /// 服务器状态变更事件
    /// </summary>
    event EventHandler<ServerStatusChangedEvent>? StatusChanged;

    /// <summary>
    /// 服务器上线事件
    /// </summary>
    event EventHandler<ServerEvent>? ServerOnline;

    /// <summary>
    /// 服务器下线事件
    /// </summary>
    event EventHandler<ServerEvent>? ServerOffline;

    /// <summary>
    /// 玩家数量变化事件
    /// </summary>
    event EventHandler<PlayerCountChangedEvent>? PlayerCountChanged;
}

/// <summary>
/// 被监控的服务器
/// </summary>
public class MonitoredServer
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public ServerAddress Address { get; init; } = new();

    /// <summary>
    /// 检查间隔（秒）
    /// </summary>
    public int CheckInterval { get; init; } = 60;

    /// <summary>
    /// 当前状态
    /// </summary>
    public ServerStatusResponse? CurrentStatus { get; set; }

    /// <summary>
    /// 上次检查时间
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// 下次检查时间
    /// </summary>
    public DateTime NextCheck { get; set; }

    /// <summary>
    /// 连续失败次数
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// 历史在线率（百分比）
    /// </summary>
    public double UptimePercent { get; set; } = 100.0;

    /// <summary>
    /// 平均延迟（毫秒）
    /// </summary>
    public long AverageLatency { get; set; }

    /// <summary>
    /// 是否当前在线
    /// </summary>
    public bool IsOnline => CurrentStatus?.IsOnline ?? false;
}

/// <summary>
/// 服务器状态变更事件
/// </summary>
public class ServerStatusChangedEvent : EventArgs
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public ServerAddress Address { get; init; } = new();

    /// <summary>
    /// 旧状态
    /// </summary>
    public ServerStatusResponse? OldStatus { get; init; }

    /// <summary>
    /// 新状态
    /// </summary>
    public ServerStatusResponse? NewStatus { get; init; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// 服务器事件
/// </summary>
public class ServerEvent : EventArgs
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public ServerAddress Address { get; init; } = new();

    /// <summary>
    /// 服务器状态
    /// </summary>
    public ServerStatusResponse? Status { get; init; }

    /// <summary>
    /// 事件时间
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.Now;
}

/// <summary>
/// 玩家数量变化事件
/// </summary>
public class PlayerCountChangedEvent : EventArgs
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public ServerAddress Address { get; init; } = new();

    /// <summary>
    /// 旧玩家数
    /// </summary>
    public int OldCount { get; init; }

    /// <summary>
    /// 新玩家数
    /// </summary>
    public int NewCount { get; init; }

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; init; }

    /// <summary>
    /// 变化量
    /// </summary>
    public int Delta => NewCount - OldCount;

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime ChangedAt { get; init; } = DateTime.Now;
}

