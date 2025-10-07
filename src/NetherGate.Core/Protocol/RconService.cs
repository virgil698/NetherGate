using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Protocol;

/// <summary>
/// RCON 生命周期管理与事件桥接
/// - 在 ServerReady 后连接（或按配置立即连接）
/// - 断线指数回退重连
/// - 发布 RconConnectedEvent / RconDisconnectedEvent
/// </summary>
public class RconService : IDisposable
{
    private readonly RconConfig _config;
    private readonly IRconClient _rcon;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    private bool _disposed;
    private int _retryCount;
    private bool _started;

    public RconService(RconConfig config, IRconClient rcon, IEventBus eventBus, ILogger logger)
    {
        _config = config;
        _rcon = rcon;
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync()
    {
        if (_started)
            return Task.CompletedTask;

        _eventBus.Subscribe<ServerReadyEvent>(OnServerReady);
        _rcon.ConnectionStateChanged += OnRconConnectionStateChanged;

        // 如果不需要等待就绪且配置自动连接，立即尝试连接
        if (_config.AutoConnect)
        {
            _ = TryConnectAsync();
        }

        _started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_started)
            return Task.CompletedTask;

        _eventBus.Unsubscribe<ServerReadyEvent>(OnServerReady);
        _rcon.ConnectionStateChanged -= OnRconConnectionStateChanged;
        _started = false;
        return Task.CompletedTask;
    }

    private async void OnServerReady(ServerReadyEvent _)
    {
        if (_config.AutoConnect && !_rcon.IsConnected)
        {
            await TryConnectAsync();
        }
    }

    private async Task TryConnectAsync()
    {
        try
        {
            var ok = await _rcon.ConnectAsync();
            if (ok)
            {
                _retryCount = 0;
                await _eventBus.PublishAsync(new RconConnectedEvent());
            }
            else
            {
                await ScheduleReconnectAsync();
            }
        }
        catch
        {
            await ScheduleReconnectAsync();
        }
    }

    private async void OnRconConnectionStateChanged(object? sender, bool connected)
    {
        if (connected)
        {
            _retryCount = 0;
            await _eventBus.PublishAsync(new RconConnectedEvent());
        }
        else
        {
            await _eventBus.PublishAsync(new RconDisconnectedEvent());
            if (_config.Reconnect.Enabled)
            {
                await ScheduleReconnectAsync();
            }
        }
    }

    private async Task ScheduleReconnectAsync()
    {
        if (!_config.Reconnect.Enabled)
            return;

        _retryCount++;
        if (_config.Reconnect.MaxRetries > 0 && _retryCount > _config.Reconnect.MaxRetries)
        {
            _logger.Warning("RCON 重连次数已达上限，停止重连");
            return;
        }

        var baseMs = Math.Max(1000, _config.Reconnect.Interval);
        var delay = Math.Min(baseMs * (int)Math.Pow(2, _retryCount - 1), 30000); // 封顶 30s
        _logger.Warning($"RCON 断开，{delay}ms 后重试（第 {_retryCount} 次）");
        await Task.Delay(delay);
        await TryConnectAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { StopAsync().Wait(); } catch { }
    }
}


