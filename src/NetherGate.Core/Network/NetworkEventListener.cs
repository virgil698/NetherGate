using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Network;

namespace NetherGate.Core.Network;

/// <summary>
/// 网络事件监听器实现
/// 根据不同模式采用不同的监听策略
/// </summary>
public class NetworkEventListener : INetworkEventListener
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly NetworkListenerMode _mode;
    private readonly List<INetworkEventHandler> _handlers = new();
    private readonly NetworkStatisticsCollector _statistics;
    private bool _isListening;
    private bool _disposed;

    public bool IsListening => _isListening;
    public NetworkListenerMode Mode => _mode;

    public NetworkEventListener(
        ILogger logger,
        IEventBus eventBus,
        NetworkListenerMode mode = NetworkListenerMode.LogBased)
    {
        _logger = logger;
        _eventBus = eventBus;
        _mode = mode;
        _statistics = new NetworkStatisticsCollector();
    }

    public async Task StartAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NetworkEventListener));

        if (_isListening)
        {
            _logger.Warning("网络监听器已在运行中");
            return;
        }

        _logger.Info($"启动网络事件监听器 (模式: {_mode})");

        switch (_mode)
        {
            case NetworkListenerMode.Disabled:
                _logger.Info("网络监听已禁用");
                return;

            case NetworkListenerMode.LogBased:
                await StartLogBasedListenerAsync();
                break;

            case NetworkListenerMode.PluginBridge:
                await StartPluginBridgeListenerAsync();
                break;

            case NetworkListenerMode.ModBridge:
                await StartModBridgeListenerAsync();
                break;

            case NetworkListenerMode.ProxyBridge:
                await StartProxyBridgeListenerAsync();
                break;

            default:
                _logger.Warning($"不支持的监听模式: {_mode}");
                return;
        }

        _isListening = true;
        _statistics.Start();
        _logger.Info("网络事件监听器已启动");
    }

    public async Task StopAsync()
    {
        if (!_isListening)
            return;

        _logger.Info("停止网络事件监听器");
        _isListening = false;
        _statistics.Stop();
        
        await Task.CompletedTask;
    }

    public void RegisterEventHandler(INetworkEventHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _handlers.Add(handler);
        _logger.Debug($"注册网络事件处理器: {handler.GetType().Name}");
    }

    public NetworkStatistics GetStatistics()
    {
        return _statistics.GetStatistics();
    }

    private async Task StartLogBasedListenerAsync()
    {
        _logger.Info("使用日志解析模式监听网络事件");
        _logger.Info("提示: 此模式仅提供基础事件（玩家加入/离开），完整功能需要 PluginBridge 或 ModBridge 模式");
        
        // 订阅日志事件来解析网络相关信息
        _eventBus.Subscribe<PlayerJoinedServerEvent>(OnPlayerJoinedFromLog);
        _eventBus.Subscribe<PlayerLeftServerEvent>(OnPlayerLeftFromLog);
        
        await Task.CompletedTask;
    }

    private async Task StartPluginBridgeListenerAsync()
    {
        _logger.Info("使用插件桥接模式监听网络事件");
        _logger.Warning("============================================");
        _logger.Warning("此模式需要在 Paper/Spigot 服务器上安装配套插件:");
        _logger.Warning("插件名: NetherGate-Bridge");
        _logger.Warning("下载地址: https://github.com/your-repo/NetherGate-Bridge/releases");
        _logger.Warning("============================================");
        _logger.Info("插件将通过 WebSocket 转发网络事件到 NetherGate");
        _logger.Info("等待插件连接...");
        
        // TODO: 实现 WebSocket 服务器接收来自 Paper 插件的事件
        // 这需要在 WebSocketServer 中添加对应的处理逻辑
        
        await Task.CompletedTask;
    }

    private async Task StartModBridgeListenerAsync()
    {
        _logger.Info("使用 Mod 桥接模式监听网络事件");
        _logger.Warning("============================================");
        _logger.Warning("此模式需要在 Fabric/Forge 服务器上安装配套 Mod:");
        _logger.Warning("Mod 名: NetherGate-Mod");
        _logger.Warning("下载地址: https://github.com/your-repo/NetherGate-Mod/releases");
        _logger.Warning("============================================");
        _logger.Info("Mod 将通过 WebSocket 转发底层数据包事件到 NetherGate");
        _logger.Info("等待 Mod 连接...");
        
        // TODO: 实现 WebSocket 服务器接收来自 Mod 的数据包事件
        
        await Task.CompletedTask;
    }

    private async Task StartProxyBridgeListenerAsync()
    {
        _logger.Info("使用 Proxy 桥接模式监听网络事件");
        _logger.Warning("============================================");
        _logger.Warning("此模式需要在 Velocity/BungeeCord 代理上安装配套插件:");
        _logger.Warning("插件名: NetherGate-Proxy");
        _logger.Warning("下载地址: https://github.com/your-repo/NetherGate-Proxy/releases");
        _logger.Warning("============================================");
        _logger.Info("代理插件将转发群组服务器的网络事件到 NetherGate");
        _logger.Info("等待代理插件连接...");
        
        // TODO: 实现 WebSocket 服务器接收来自 Proxy 插件的事件
        
        await Task.CompletedTask;
    }

    private async void OnPlayerJoinedFromLog(PlayerJoinedServerEvent e)
    {
        _statistics.RecordConnection();
        
        // 发布网络层玩家登录成功事件
        await _eventBus.PublishAsync(new PlayerLoginSuccessEvent
        {
            PlayerName = e.PlayerName,
            IpAddress = e.IpAddress ?? "unknown"
        });
        
        // 通知所有处理器
        var data = new PlayerLoginData
        {
            PlayerName = e.PlayerName,
            IpAddress = e.IpAddress ?? "unknown"
        };
        
        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnPlayerLoginSuccessAsync(data);
            }
            catch (Exception ex)
            {
                _logger.Error($"网络事件处理器执行失败: {ex.Message}");
            }
        }
    }

    private async void OnPlayerLeftFromLog(PlayerLeftServerEvent e)
    {
        _statistics.RecordDisconnection();
        
        // 发布网络层玩家断开连接事件
        await _eventBus.PublishAsync(new PlayerDisconnectedEvent
        {
            PlayerName = e.PlayerName,
            Reason = e.Reason ?? "unknown"
        });
        
        // 通知所有处理器
        var data = new PlayerDisconnectData
        {
            PlayerName = e.PlayerName,
            Reason = e.Reason ?? "unknown"
        };
        
        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnPlayerDisconnectedAsync(data);
            }
            catch (Exception ex)
            {
                _logger.Error($"网络事件处理器执行失败: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopAsync().Wait();
        _logger.Debug("NetworkEventListener 已释放");
    }
}

/// <summary>
/// 网络统计收集器
/// </summary>
internal class NetworkStatisticsCollector
{
    private long _totalBytesReceived;
    private long _totalBytesSent;
    private long _totalPacketsReceived;
    private long _totalPacketsSent;
    private int _currentConnections;
    private long _totalConnections;
    private long _failedConnections;
    private DateTime _startTime;
    private bool _isRunning;

    public void Start()
    {
        _startTime = DateTime.UtcNow;
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
    }

    public void RecordConnection()
    {
        if (!_isRunning) return;
        _currentConnections++;
        _totalConnections++;
    }

    public void RecordDisconnection()
    {
        if (!_isRunning) return;
        if (_currentConnections > 0)
            _currentConnections--;
    }

    public void RecordFailedConnection()
    {
        if (!_isRunning) return;
        _failedConnections++;
    }

    public void RecordPacketReceived(int bytes)
    {
        if (!_isRunning) return;
        _totalPacketsReceived++;
        _totalBytesReceived += bytes;
    }

    public void RecordPacketSent(int bytes)
    {
        if (!_isRunning) return;
        _totalPacketsSent++;
        _totalBytesSent += bytes;
    }

    public NetworkStatistics GetStatistics()
    {
        return new NetworkStatistics
        {
            TotalBytesReceived = _totalBytesReceived,
            TotalBytesSent = _totalBytesSent,
            TotalPacketsReceived = _totalPacketsReceived,
            TotalPacketsSent = _totalPacketsSent,
            CurrentConnections = _currentConnections,
            TotalConnections = _totalConnections,
            FailedConnections = _failedConnections,
            AverageLatencyMs = 0, // TODO: 实现延迟监控
            StartTime = _startTime,
            LastUpdate = DateTime.UtcNow
        };
    }
}
