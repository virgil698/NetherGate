using System.Collections.Concurrent;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.Core.Protocol;

namespace NetherGate.Core.Network;

/// <summary>
/// 网络连接管理器 - 统一管理所有网络连接
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, INetworkConnection> _connections = new();
    private readonly Timer _healthCheckTimer;
    private readonly Timer _reconnectTimer;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);
    private bool _disposed;

    public int ConnectionCount => _connections.Count;
    public IReadOnlyDictionary<string, INetworkConnection> Connections => _connections;

    public ConnectionManager(ILogger logger)
    {
        _logger = logger;
        
        // 启动健康检查定时器
        _healthCheckTimer = new Timer(OnHealthCheck, null, _healthCheckInterval, _healthCheckInterval);
        
        // 启动重连定时器
        _reconnectTimer = new Timer(OnReconnect, null, _reconnectInterval, _reconnectInterval);
    }

    /// <summary>
    /// 注册连接
    /// </summary>
    public void RegisterConnection(string name, INetworkConnection connection)
    {
        if (_connections.TryAdd(name, connection))
        {
            _logger.Info($"连接管理器: 注册连接 '{name}'");
            
            // 订阅连接状态变化
            connection.StateChanged += (sender, state) =>
            {
                _logger.Info($"连接 '{name}' 状态变化: {state}");
            };
        }
        else
        {
            _logger.Warning($"连接管理器: 连接 '{name}' 已存在");
        }
    }

    /// <summary>
    /// 注销连接
    /// </summary>
    public void UnregisterConnection(string name)
    {
        if (_connections.TryRemove(name, out var connection))
        {
            _logger.Info($"连接管理器: 注销连接 '{name}'");
        }
    }

    /// <summary>
    /// 获取连接
    /// </summary>
    public T? GetConnection<T>(string name) where T : class, INetworkConnection
    {
        return _connections.TryGetValue(name, out var connection) ? connection as T : null;
    }

    /// <summary>
    /// 连接所有已注册的连接
    /// </summary>
    public async Task ConnectAllAsync()
    {
        _logger.Info("连接管理器: 开始连接所有已注册连接...");
        
        var tasks = _connections.Values
            .Where(c => !c.IsConnected)
            .Select(async c =>
            {
                try
                {
                    await c.ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error($"连接失败: {ex.Message}");
                }
            });

        await Task.WhenAll(tasks);
        
        _logger.Info($"连接管理器: 已连接 {_connections.Values.Count(c => c.IsConnected)}/{_connections.Count} 个连接");
    }

    /// <summary>
    /// 断开所有连接
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        _logger.Info("连接管理器: 断开所有连接...");
        
        var tasks = _connections.Values
            .Where(c => c.IsConnected)
            .Select(async c =>
            {
                try
                {
                    await c.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error($"断开连接失败: {ex.Message}");
                }
            });

        await Task.WhenAll(tasks);
        
        _logger.Info("连接管理器: 所有连接已断开");
    }

    /// <summary>
    /// 健康检查回调
    /// </summary>
    private void OnHealthCheck(object? state)
    {
        try
        {
            var disconnectedConnections = _connections.Values
                .Where(c => !c.IsConnected)
                .ToList();

            if (disconnectedConnections.Any())
            {
                _logger.Warning($"连接管理器: 发现 {disconnectedConnections.Count} 个断开的连接");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"连接管理器: 健康检查失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 自动重连回调
    /// </summary>
    private void OnReconnect(object? state)
    {
        try
        {
            var disconnectedConnections = _connections.Values
                .Where(c => !c.IsConnected && c.AutoReconnect)
                .ToList();

            foreach (var connection in disconnectedConnections)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.Info($"连接管理器: 尝试重新连接...");
                        await connection.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"重连失败: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"连接管理器: 重连失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取连接统计
    /// </summary>
    public ConnectionStatistics GetStatistics()
    {
        return new ConnectionStatistics
        {
            TotalConnections = _connections.Count,
            ConnectedCount = _connections.Values.Count(c => c.IsConnected),
            DisconnectedCount = _connections.Values.Count(c => !c.IsConnected),
            ConnectionDetails = _connections.ToDictionary(
                kvp => kvp.Key,
                kvp => new ConnectionInfo
                {
                    IsConnected = kvp.Value.IsConnected,
                    State = kvp.Value.State,
                    AutoReconnect = kvp.Value.AutoReconnect
                })
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _healthCheckTimer?.Dispose();
        _reconnectTimer?.Dispose();
        
        // 不自动断开连接，由调用方控制
        _logger.Info("连接管理器已释放");
    }
}

/// <summary>
/// 网络连接接口
/// </summary>
public interface INetworkConnection
{
    bool IsConnected { get; }
    ConnectionState State { get; }
    bool AutoReconnect { get; }
    event EventHandler<ConnectionState>? StateChanged;
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
}

/// <summary>
/// 连接状态
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}

/// <summary>
/// 连接统计
/// </summary>
public class ConnectionStatistics
{
    public int TotalConnections { get; set; }
    public int ConnectedCount { get; set; }
    public int DisconnectedCount { get; set; }
    public Dictionary<string, ConnectionInfo> ConnectionDetails { get; set; } = new();
}

/// <summary>
/// 连接信息
/// </summary>
public class ConnectionInfo
{
    public bool IsConnected { get; set; }
    public ConnectionState State { get; set; }
    public bool AutoReconnect { get; set; }
}

