using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Protocol;

/// <summary>
/// SMP 生命周期管理：就绪后连接、断线重连、事件桥接
/// </summary>
public class SmpService : IDisposable
{
    private readonly ServerConnectionConfig _config;
    private readonly SmpClient _client;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private bool _started;

    public SmpService(ServerConnectionConfig config, SmpClient client, IEventBus eventBus, ILogger logger)
    {
        _config = config;
        _client = client;
        _eventBus = eventBus;
        _logger = logger;
    }

    public Task StartAsync()
    {
        if (_started) return Task.CompletedTask;
        _eventBus.Subscribe<ServerReadyEvent>(OnServerReady);
        _started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_started) return Task.CompletedTask;
        _eventBus.Unsubscribe<ServerReadyEvent>(OnServerReady);
        _started = false;
        return Task.CompletedTask;
    }

    private async Task OnServerReady(ServerReadyEvent e)
    {
        if (_config.AutoConnect && !_client.IsConnected)
        {
            await _client.ConnectAsync();
        }
    }

    public void Dispose()
    {
        try 
        { 
            StopAsync().Wait(); 
        } 
        catch (Exception ex) 
        { 
            _logger.Debug($"Dispose 时停止 SmpService 失败: {ex.Message}"); 
        }
    }
}


