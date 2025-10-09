using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.Core.Process;
using NetherGate.Core.WebSocket;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Monitoring;

/// <summary>
/// 统一健康检查：周期发布 SystemHealthEvent
/// </summary>
public class HealthService : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly ServerProcessManager? _processManager;
    private readonly IRconClient? _rconClient;
    private readonly ISmpApi? _smpApi;
    private readonly WebSocketServer? _wsServer;
    private readonly Func<int> _getPluginsCount;
    private readonly TimeSpan _interval;
    private volatile bool _serverReady;
    private CancellationTokenSource? _cts;

    public HealthService(
        IEventBus eventBus,
        ILogger logger,
        ServerProcessManager? processManager,
        IRconClient? rconClient,
        ISmpApi? smpApi,
        WebSocketServer? wsServer,
        Func<int> getPluginsCount,
        TimeSpan? interval = null)
    {
        _eventBus = eventBus;
        _logger = logger;
        _processManager = processManager;
        _rconClient = rconClient;
        _smpApi = smpApi;
        _wsServer = wsServer;
        _getPluginsCount = getPluginsCount;
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    public Task StartAsync()
    {
        if (_cts != null)
            return Task.CompletedTask;

        _cts = new CancellationTokenSource();
        _eventBus.Subscribe<ServerReadyEvent>(OnServerReady);
        _ = Task.Run(() => LoopAsync(_cts.Token));
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _eventBus.Unsubscribe<ServerReadyEvent>(OnServerReady);
        _cts = null;
        return Task.CompletedTask;
    }

    private async Task LoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                double? tps1 = null, tps5 = null, tps15 = null, msptAvg = null;
                try
                {
                    if (_rconClient is IRconPerformance perf && _rconClient.IsConnected)
                    {
                        var tps = await perf.GetTpsAsync();
                        if (tps != null)
                        {
                            tps1 = tps.Tps1m; tps5 = tps.Tps5m; tps15 = tps.Tps15m;
                        }
                        var mspt = await perf.GetMsptAsync();
                        if (mspt != null)
                        {
                            msptAvg = mspt.Average;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Trace($"获取 TPS/MSPT 失败: {ex.Message}");
                }

                int? players = null;
                try
                {
                    var smp = _smpApi;
                    if (smp != null)
                    {
                        bool isConnected = false;
                        try { isConnected = ((dynamic)smp).IsConnected == true; } catch { /* 若无此属性则尝试调用 */ isConnected = true; }
                        if (isConnected)
                        {
                            var list = await smp.GetPlayersAsync();
                            players = list?.Count;
                        }
                    }
                }
                catch (Exception ex) 
                { 
                    _logger.Debug($"获取玩家数量失败: {ex.Message}"); 
                }

                var ev = new SystemHealthEvent
                {
                    ServerProcessRunning = _processManager?.IsRunning ?? false,
                    ServerReady = _serverReady,
                    RconConnected = _rconClient?.IsConnected ?? false,
                    SmpConnected = (_smpApi as dynamic)?.IsConnected ?? false,
                    WebSocketRunning = _wsServer?.IsRunning ?? false,
                    PluginsLoaded = _getPluginsCount(),
                    PlayerCount = players,
                    Tps1m = tps1,
                    Tps5m = tps5,
                    Tps15m = tps15,
                    MsptAverage = msptAvg
                };
                await _eventBus.PublishAsync(ev);
            }
            catch (Exception ex)
            {
                _logger.Warning($"发布健康状态失败: {ex.Message}");
            }

            try 
            { 
                await Task.Delay(_interval, token); 
            } 
            catch (OperationCanceledException) 
            { 
                // 正常取消，无需记录
            }
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
            _logger.Debug($"Dispose 时停止 HealthService 失败: {ex.Message}"); 
        }
    }

    private Task OnServerReady(ServerReadyEvent e)
    {
        _serverReady = true;
        return Task.CompletedTask;
    }
}


