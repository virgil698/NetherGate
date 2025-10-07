using System.Text.Json;
using NetherGate.API.Events;
using NetherGate.API.Logging;

namespace NetherGate.Core.WebSocket;

/// <summary>
/// 将事件总线上的重要事件广播到所有 WebSocket 客户端
/// </summary>
public class EventBridge : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly WebSocketServer _server;
    private readonly ILogger _logger;
    private Func<ServerCommandResponseEvent, Task>? _onCmd;
    private Func<SystemHealthEvent, Task>? _onHealth;
    private Func<ServerLogEvent, Task>? _onLog;
    private bool _started;

    public EventBridge(IEventBus eventBus, WebSocketServer server, ILogger logger)
    {
        _eventBus = eventBus;
        _server = server;
        _logger = logger;
    }

    public Task StartAsync()
    {
        if (_started) return Task.CompletedTask;

        _onCmd = async (e) =>
        {
            await _server.BroadcastEventAsync("event.server.command_response", new { e.Command, e.Success, e.Response, e.Channel, ts = e.Timestamp });
        };
        _eventBus.Subscribe(_onCmd);

        _onHealth = async (e) =>
        {
            await _server.BroadcastEventAsync("event.system.health", new { e.ServerProcessRunning, e.ServerReady, e.RconConnected, e.SmpConnected, e.WebSocketRunning, e.PluginsLoaded, ts = e.Timestamp });
        };
        _eventBus.Subscribe(_onHealth);

        _onLog = async (e) =>
        {
            await _server.BroadcastEventAsync("event.server.log", new { level = e.LogLevel, message = e.Message, thread = e.Thread, logger = e.Logger, ts = e.Timestamp });
        };
        _eventBus.Subscribe(_onLog);

        _started = true;
        _logger.Debug("WebSocket EventBridge 已启动");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_started) return Task.CompletedTask;

        if (_onCmd != null) _eventBus.Unsubscribe(_onCmd);
        if (_onHealth != null) _eventBus.Unsubscribe(_onHealth);
        if (_onLog != null) _eventBus.Unsubscribe(_onLog);
        _onCmd = null; _onHealth = null; _onLog = null;
        _started = false;
        _logger.Debug("WebSocket EventBridge 已停止");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { StopAsync().Wait(); } catch { }
    }
}


