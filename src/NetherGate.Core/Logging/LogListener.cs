using System.Text.RegularExpressions;
using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;

namespace NetherGate.Core.Logging;

/// <summary>
/// 日志监听服务：订阅 <see cref="ServerLogEvent"/>，按配置过滤并缓冲最近日志
/// </summary>
public class LogListener : IDisposable
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly LogListenerConfig _config;
    private readonly List<ServerLogEvent> _buffer = new();
    private readonly List<Regex> _ignoreRegexes;
    private bool _isStarted;

    private Func<ServerLogEvent, Task>? _asyncHandler;

    public event Action<ServerLogEvent>? OnLog;

    public LogListener(ILogger logger, IEventBus eventBus, LogListenerConfig config)
    {
        _logger = logger;
        _eventBus = eventBus;
        _config = config;
        _ignoreRegexes = BuildIgnoreRegexes(config.Filters.IgnorePatterns);
    }

    public Task StartAsync()
    {
        if (_isStarted)
            return Task.CompletedTask;

        _asyncHandler = async (e) =>
        {
            try
            {
                if (!PassesFilters(e))
                    return;

                AppendToBuffer(e);
                OnLog?.Invoke(e);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Warning($"LogListener 处理日志失败: {ex.Message}");
            }
        };

        _eventBus.Subscribe(_asyncHandler);
        _isStarted = true;
        _logger.Debug("LogListener 已启动");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_isStarted)
            return Task.CompletedTask;

        if (_asyncHandler != null)
        {
            _eventBus.Unsubscribe(_asyncHandler);
            _asyncHandler = null;
        }
        _isStarted = false;
        _logger.Debug("LogListener 已停止");
        return Task.CompletedTask;
    }

    public IReadOnlyList<ServerLogEvent> GetBufferedLogs()
    {
        return _buffer.ToList();
    }

    private bool PassesFilters(ServerLogEvent e)
    {
        // 级别过滤
        if (_config.Filters.LogLevels?.Count > 0)
        {
            var level = (e.LogLevel ?? string.Empty).ToUpperInvariant().Trim();
            if (!_config.Filters.LogLevels.Any(l => string.Equals(l.Trim(), level, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // 忽略模式
        if (_ignoreRegexes.Count > 0)
        {
            var text = e.Message ?? string.Empty;
            if (_ignoreRegexes.Any(rx => rx.IsMatch(text)))
                return false;
        }

        return true;
    }

    private void AppendToBuffer(ServerLogEvent e)
    {
        var max = Math.Max(0, _config.Buffer.Size);
        if (max == 0)
            return;

        if (_buffer.Count >= max)
        {
            if (_config.Buffer.DropOld)
            {
                // 丢弃最旧
                _buffer.RemoveAt(0);
            }
            else
            {
                // 丢弃新消息
                return;
            }
        }
        _buffer.Add(e);
    }

    private static List<Regex> BuildIgnoreRegexes(List<string> patterns)
    {
        var list = new List<Regex>();
        if (patterns == null)
            return list;

        foreach (var p in patterns)
        {
            if (string.IsNullOrWhiteSpace(p))
                continue;
            try
            {
                list.Add(new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
            catch
            {
                // 如果正则无效，按字面量转义
                var escaped = Regex.Escape(p);
                list.Add(new Regex(escaped, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }
        return list;
    }

    public void Dispose()
    {
        try { StopAsync().Wait(); } catch { /* ignore */ }
    }
}


