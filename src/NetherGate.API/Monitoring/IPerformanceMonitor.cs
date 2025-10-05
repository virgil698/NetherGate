namespace NetherGate.API.Monitoring;

/// <summary>
/// 性能监控接口
/// 提供服务器性能指标监控
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// 获取当前性能快照
    /// </summary>
    PerformanceSnapshot GetSnapshot();

    /// <summary>
    /// 获取历史性能数据
    /// </summary>
    /// <param name="minutes">获取最近 N 分钟的数据</param>
    List<PerformanceSnapshot> GetHistory(int minutes = 60);

    /// <summary>
    /// 启动性能监控
    /// </summary>
    /// <param name="intervalSeconds">监控间隔（秒）</param>
    void Start(int intervalSeconds = 10);

    /// <summary>
    /// 停止性能监控
    /// </summary>
    void Stop();

    /// <summary>
    /// 是否正在监控
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// 性能警告事件
    /// </summary>
    event EventHandler<PerformanceWarningEvent>? PerformanceWarning;
}

/// <summary>
/// 性能快照
/// </summary>
public class PerformanceSnapshot
{
    /// <summary>
    /// 快照时间
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// CPU 使用率（百分比）
    /// </summary>
    public double CpuUsage { get; init; }

    /// <summary>
    /// 内存使用情况
    /// </summary>
    public MemoryUsage Memory { get; init; } = new();

    /// <summary>
    /// 磁盘使用情况
    /// </summary>
    public DiskUsage Disk { get; init; } = new();

    /// <summary>
    /// 服务器进程信息
    /// </summary>
    public ProcessInfo? ServerProcess { get; init; }

    /// <summary>
    /// TPS（每秒滴答数，Minecraft 特有）
    /// </summary>
    public double? Tps { get; init; }
}

/// <summary>
/// 内存使用情况
/// </summary>
public class MemoryUsage
{
    /// <summary>
    /// 已使用内存（MB）
    /// </summary>
    public long UsedMB { get; init; }

    /// <summary>
    /// 总内存（MB）
    /// </summary>
    public long TotalMB { get; init; }

    /// <summary>
    /// 使用率（百分比）
    /// </summary>
    public double UsagePercent => TotalMB > 0 ? (double)UsedMB / TotalMB * 100 : 0;

    /// <summary>
    /// 可用内存（MB）
    /// </summary>
    public long AvailableMB => TotalMB - UsedMB;
}

/// <summary>
/// 磁盘使用情况
/// </summary>
public class DiskUsage
{
    /// <summary>
    /// 已使用磁盘空间（GB）
    /// </summary>
    public long UsedGB { get; init; }

    /// <summary>
    /// 总磁盘空间（GB）
    /// </summary>
    public long TotalGB { get; init; }

    /// <summary>
    /// 使用率（百分比）
    /// </summary>
    public double UsagePercent => TotalGB > 0 ? (double)UsedGB / TotalGB * 100 : 0;

    /// <summary>
    /// 可用磁盘空间（GB）
    /// </summary>
    public long AvailableGB => TotalGB - UsedGB;
}

/// <summary>
/// 进程信息
/// </summary>
public class ProcessInfo
{
    /// <summary>
    /// 进程 ID
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// 线程数
    /// </summary>
    public int ThreadCount { get; init; }

    /// <summary>
    /// 已运行时间
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// 内存使用（MB）
    /// </summary>
    public long MemoryMB { get; init; }
}

/// <summary>
/// 性能警告事件
/// </summary>
public class PerformanceWarningEvent : EventArgs
{
    /// <summary>
    /// 警告类型
    /// </summary>
    public PerformanceWarningType Type { get; init; }

    /// <summary>
    /// 警告消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 当前值
    /// </summary>
    public double CurrentValue { get; init; }

    /// <summary>
    /// 阈值
    /// </summary>
    public double Threshold { get; init; }

    /// <summary>
    /// 性能快照
    /// </summary>
    public PerformanceSnapshot Snapshot { get; init; } = new();
}

/// <summary>
/// 性能警告类型
/// </summary>
public enum PerformanceWarningType
{
    /// <summary>
    /// CPU 使用率过高
    /// </summary>
    HighCpuUsage,

    /// <summary>
    /// 内存使用率过高
    /// </summary>
    HighMemoryUsage,

    /// <summary>
    /// 磁盘空间不足
    /// </summary>
    LowDiskSpace,

    /// <summary>
    /// TPS 过低
    /// </summary>
    LowTps
}

