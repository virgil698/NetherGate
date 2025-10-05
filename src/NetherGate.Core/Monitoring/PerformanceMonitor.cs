using System.Diagnostics;
using System.Collections.Concurrent;
using NetherGate.API.Monitoring;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Monitoring;

/// <summary>
/// 性能监控实现
/// </summary>
public class PerformanceMonitor : IPerformanceMonitor, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<PerformanceSnapshot> _history;
    private readonly int _maxHistoryMinutes;
    private System.Threading.Timer? _monitoringTimer;
    private System.Diagnostics.Process? _serverProcess;
    private IRconPerformance? _rconPerformance;

    private double _cpuWarningThreshold = 80.0;
    private double _memoryWarningThreshold = 90.0;
    private double _diskWarningThreshold = 90.0;

    public bool IsMonitoring => _monitoringTimer != null;

    public event EventHandler<PerformanceWarningEvent>? PerformanceWarning;

    public PerformanceMonitor(ILogger logger, int maxHistoryMinutes = 120)
    {
        _logger = logger;
        _history = new ConcurrentQueue<PerformanceSnapshot>();
        _maxHistoryMinutes = maxHistoryMinutes;
    }

    public void SetServerProcess(System.Diagnostics.Process? process)
    {
        _serverProcess = process;
    }

    /// <summary>
    /// 设置 RCON 性能接口（用于获取 TPS/MSPT）
    /// </summary>
    public void SetRconPerformance(IRconPerformance? rconPerformance)
    {
        _rconPerformance = rconPerformance;
    }

    public void SetWarningThresholds(double cpu = 80.0, double memory = 90.0, double disk = 90.0)
    {
        _cpuWarningThreshold = cpu;
        _memoryWarningThreshold = memory;
        _diskWarningThreshold = disk;
    }

    public PerformanceSnapshot GetSnapshot()
    {
        try
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                CpuUsage = GetCpuUsage(),
                Memory = GetMemoryUsage(),
                Disk = GetDiskUsage(),
                ServerProcess = GetServerProcessInfo(),
                Tps = GetTpsFromRcon() // 通过 RCON 获取 TPS
            };

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.Error("获取性能快照失败", ex);
            throw;
        }
    }

    /// <summary>
    /// 通过 RCON 获取 TPS（仅支持 Paper/Purpur）
    /// </summary>
    private double? GetTpsFromRcon()
    {
        if (_rconPerformance == null)
            return null;

        try
        {
            // 使用同步方式避免在 Timer 回调中使用 async
            var tpsTask = _rconPerformance.GetTpsAsync();
            tpsTask.Wait(TimeSpan.FromSeconds(2)); // 2秒超时
            
            var tpsData = tpsTask.Result;
            return tpsData?.Tps1m; // 返回最近1分钟的TPS
        }
        catch (Exception ex)
        {
            _logger.Trace($"通过 RCON 获取 TPS 失败: {ex.Message}");
            return null;
        }
    }

    public List<PerformanceSnapshot> GetHistory(int minutes = 60)
    {
        var cutoffTime = DateTime.Now.AddMinutes(-minutes);
        return _history.Where(s => s.Timestamp >= cutoffTime).ToList();
    }

    public void Start(int intervalSeconds = 10)
    {
        if (IsMonitoring)
        {
            _logger.Warning("性能监控已在运行中");
            return;
        }

        var interval = TimeSpan.FromSeconds(intervalSeconds);

        _monitoringTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                var snapshot = GetSnapshot();

                // 添加到历史记录
                _history.Enqueue(snapshot);

                // 清理旧记录
                var cutoffTime = DateTime.Now.AddMinutes(-_maxHistoryMinutes);
                while (_history.TryPeek(out var peeked) && peeked != null && peeked.Timestamp < cutoffTime)
                {
                    _history.TryDequeue(out var _);
                }

                // 检查警告阈值
                CheckWarningThresholds(snapshot);
            }
            catch (Exception ex)
            {
                _logger.Error("性能监控执行失败", ex);
            }
        }, null, TimeSpan.Zero, interval);

        _logger.Info($"性能监控已启动 (间隔: {intervalSeconds} 秒)");
    }

    public void Stop()
    {
        if (_monitoringTimer != null)
        {
            _monitoringTimer.Dispose();
            _monitoringTimer = null;
            _logger.Info("性能监控已停止");
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private double GetCpuUsage()
    {
        // PerformanceCounter 仅在 Windows 上可用
        if (!OperatingSystem.IsWindows())
        {
            return 0;
        }

        try
        {
            // 获取系统总 CPU 使用率
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // 首次调用返回 0，需要预热
            System.Threading.Thread.Sleep(100);
            return Math.Round(cpuCounter.NextValue(), 2);
        }
        catch (Exception ex)
        {
            _logger.Warning($"获取 CPU 使用率失败: {ex.Message}");
            return 0;
        }
    }

    private MemoryUsage GetMemoryUsage()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemoryMB = gcInfo.TotalAvailableMemoryBytes / 1024 / 1024;
            var usedMemoryMB = (gcInfo.TotalAvailableMemoryBytes - gcInfo.MemoryLoadBytes) / 1024 / 1024;

            // 如果 GC 信息不可用，使用系统性能计数器（仅 Windows）
            if (totalMemoryMB == 0 && OperatingSystem.IsWindows())
            {
                using var availableCounter = new PerformanceCounter("Memory", "Available MBytes");
                var availableMB = (long)availableCounter.NextValue();

                using var totalCounter = new PerformanceCounter("Memory", "Installed Bytes");
                totalMemoryMB = (long)(totalCounter.NextValue() / 1024 / 1024);
                usedMemoryMB = totalMemoryMB - availableMB;
            }

            return new MemoryUsage
            {
                UsedMB = usedMemoryMB,
                TotalMB = totalMemoryMB
            };
        }
        catch (Exception ex)
        {
            _logger.Warning($"获取内存使用情况失败: {ex.Message}");
            return new MemoryUsage { UsedMB = 0, TotalMB = 0 };
        }
    }

    private DiskUsage GetDiskUsage()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:\\");

            return new DiskUsage
            {
                UsedGB = (drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024 / 1024,
                TotalGB = drive.TotalSize / 1024 / 1024 / 1024
            };
        }
        catch (Exception ex)
        {
            _logger.Warning($"获取磁盘使用情况失败: {ex.Message}");
            return new DiskUsage { UsedGB = 0, TotalGB = 0 };
        }
    }

    private ProcessInfo? GetServerProcessInfo()
    {
        if (_serverProcess == null || _serverProcess.HasExited)
        {
            return null;
        }

        try
        {
            _serverProcess.Refresh();

            return new ProcessInfo
            {
                ProcessId = _serverProcess.Id,
                ProcessName = _serverProcess.ProcessName,
                ThreadCount = _serverProcess.Threads.Count,
                Uptime = DateTime.Now - _serverProcess.StartTime,
                MemoryMB = _serverProcess.WorkingSet64 / 1024 / 1024
            };
        }
        catch (Exception ex)
        {
            _logger.Warning($"获取服务器进程信息失败: {ex.Message}");
            return null;
        }
    }

    private void CheckWarningThresholds(PerformanceSnapshot snapshot)
    {
        // CPU 警告
        if (snapshot.CpuUsage > _cpuWarningThreshold)
        {
            RaiseWarning(new PerformanceWarningEvent
            {
                Type = PerformanceWarningType.HighCpuUsage,
                Message = $"CPU 使用率过高: {snapshot.CpuUsage:F1}%",
                CurrentValue = snapshot.CpuUsage,
                Threshold = _cpuWarningThreshold,
                Snapshot = snapshot
            });
        }

        // 内存警告
        if (snapshot.Memory.UsagePercent > _memoryWarningThreshold)
        {
            RaiseWarning(new PerformanceWarningEvent
            {
                Type = PerformanceWarningType.HighMemoryUsage,
                Message = $"内存使用率过高: {snapshot.Memory.UsagePercent:F1}%",
                CurrentValue = snapshot.Memory.UsagePercent,
                Threshold = _memoryWarningThreshold,
                Snapshot = snapshot
            });
        }

        // 磁盘警告
        if (snapshot.Disk.UsagePercent > _diskWarningThreshold)
        {
            RaiseWarning(new PerformanceWarningEvent
            {
                Type = PerformanceWarningType.LowDiskSpace,
                Message = $"磁盘空间不足: {snapshot.Disk.UsagePercent:F1}%",
                CurrentValue = snapshot.Disk.UsagePercent,
                Threshold = _diskWarningThreshold,
                Snapshot = snapshot
            });
        }
    }

    private void RaiseWarning(PerformanceWarningEvent warning)
    {
        try
        {
            PerformanceWarning?.Invoke(this, warning);
        }
        catch (Exception ex)
        {
            _logger.Error("性能警告事件处理失败", ex);
        }
    }
}

