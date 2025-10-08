using NetherGate.API.Configuration;
using NetherGate.API.Logging;

namespace NetherGate.Core.Configuration;

/// <summary>
/// 配置文件监视器 - 支持配置热重载
/// </summary>
public class ConfigWatcher : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _configPath;
    private readonly FileSystemWatcher? _watcher;
    private readonly Action<NetherGateConfig> _onConfigChanged;
    private readonly object _lock = new();
    private DateTime _lastReloadTime = DateTime.MinValue;
    private const int ReloadDebounceMs = 1000; // 防抖延迟 1 秒

    public ConfigWatcher(string configPath, Action<NetherGateConfig> onConfigChanged, ILogger logger)
    {
        _configPath = configPath;
        _onConfigChanged = onConfigChanged;
        _logger = logger;

        if (!File.Exists(configPath))
        {
            _logger.Warning($"配置文件不存在，无法启用热重载: {configPath}");
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(configPath) ?? ".";
            var fileName = Path.GetFileName(configPath);

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnConfigFileChanged;
            _logger.Info($"已启用配置热重载监听: {configPath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"启动配置热重载监听失败: {ex.Message}", ex);
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            // 防抖：避免短时间内多次触发
            var now = DateTime.Now;
            if ((now - _lastReloadTime).TotalMilliseconds < ReloadDebounceMs)
            {
                return;
            }
            _lastReloadTime = now;
        }

        _logger.Info("检测到配置文件变化，重新加载...");

        try
        {
            // 等待文件写入完成（某些编辑器会分多次写入）
            Thread.Sleep(200);

            // 重新加载配置
            var newConfig = ConfigurationLoader.Load(_logger);
            
            // 触发回调
            _onConfigChanged?.Invoke(newConfig);
            
            _logger.Info("配置重载成功");
        }
        catch (Exception ex)
        {
            _logger.Error($"配置重载失败: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        if (_watcher != null)
        {
            _watcher.Changed -= OnConfigFileChanged;
            _watcher.Dispose();
            _logger.Debug("配置热重载监听已停止");
        }
    }
}

