using System.Collections.Concurrent;
using NetherGate.API.Logging;

namespace NetherGate.Core.Network;

/// <summary>
/// 连接池 - 复用 WebSocket/RCON 连接
/// </summary>
public class ConnectionPool<T> : IDisposable where T : class
{
    private readonly ConcurrentBag<PooledConnection<T>> _availableConnections = new();
    private readonly ConcurrentDictionary<string, PooledConnection<T>> _activeConnections = new();
    private readonly Func<Task<T>> _connectionFactory;
    private readonly Action<T> _connectionDisposer;
    private readonly ILogger _logger;
    private readonly int _maxPoolSize;
    private readonly TimeSpan _connectionTimeout;
    private int _totalConnections;
    private bool _disposed;

    public int AvailableCount => _availableConnections.Count;
    public int ActiveCount => _activeConnections.Count;
    public int TotalCount => _totalConnections;

    public ConnectionPool(
        Func<Task<T>> connectionFactory,
        Action<T> connectionDisposer,
        ILogger logger,
        int maxPoolSize = 10,
        TimeSpan? connectionTimeout = null)
    {
        _connectionFactory = connectionFactory;
        _connectionDisposer = connectionDisposer;
        _logger = logger;
        _maxPoolSize = maxPoolSize;
        _connectionTimeout = connectionTimeout ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// 获取连接
    /// </summary>
    public async Task<PooledConnection<T>> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionPool<T>));

        // 尝试从池中获取可用连接
        if (_availableConnections.TryTake(out var pooledConnection))
        {
            // 检查连接是否过期
            if (DateTime.UtcNow - pooledConnection.CreatedAt < _connectionTimeout)
            {
                pooledConnection.MarkActive();
                _activeConnections.TryAdd(pooledConnection.Id, pooledConnection);
                _logger.Debug($"连接池: 复用连接 {pooledConnection.Id}");
                return pooledConnection;
            }
            else
            {
                // 连接过期，释放并创建新连接
                _logger.Debug($"连接池: 连接 {pooledConnection.Id} 已过期");
                _connectionDisposer(pooledConnection.Connection);
                Interlocked.Decrement(ref _totalConnections);
            }
        }

        // 创建新连接
        if (_totalConnections >= _maxPoolSize)
        {
            _logger.Warning($"连接池: 已达到最大连接数 {_maxPoolSize}，等待可用连接...");
            
            // 等待连接释放（最多等待 30 秒）
            var waitTask = Task.Run(async () =>
            {
                while (_availableConnections.IsEmpty && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            });

            if (await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(30), cancellationToken)) == waitTask)
            {
                return await AcquireAsync(cancellationToken);
            }

            throw new TimeoutException("等待连接池可用连接超时");
        }

        var connection = await _connectionFactory();
        Interlocked.Increment(ref _totalConnections);
        
        var newPooledConnection = new PooledConnection<T>(connection, this);
        newPooledConnection.MarkActive();
        _activeConnections.TryAdd(newPooledConnection.Id, newPooledConnection);
        
        _logger.Debug($"连接池: 创建新连接 {newPooledConnection.Id} (总数: {_totalConnections})");
        return newPooledConnection;
    }

    /// <summary>
    /// 释放连接
    /// </summary>
    internal void Release(PooledConnection<T> connection)
    {
        if (_disposed)
        {
            _connectionDisposer(connection.Connection);
            return;
        }

        _activeConnections.TryRemove(connection.Id, out _);
        connection.MarkAvailable();
        _availableConnections.Add(connection);
        
        _logger.Debug($"连接池: 释放连接 {connection.Id} (可用: {_availableConnections.Count})");
    }

    /// <summary>
    /// 清理过期连接（定期调用）
    /// </summary>
    public void CleanupExpiredConnections()
    {
        var expiredConnections = _availableConnections
            .Where(c => DateTime.UtcNow - c.CreatedAt >= _connectionTimeout)
            .ToList();

        foreach (var connection in expiredConnections)
        {
            if (_availableConnections.TryTake(out var removed) && removed.Id == connection.Id)
            {
                _connectionDisposer(removed.Connection);
                Interlocked.Decrement(ref _totalConnections);
                _logger.Debug($"连接池: 清理过期连接 {removed.Id}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // 释放所有连接
        foreach (var connection in _activeConnections.Values)
        {
            _connectionDisposer(connection.Connection);
        }
        
        foreach (var connection in _availableConnections)
        {
            _connectionDisposer(connection.Connection);
        }

        _activeConnections.Clear();
        _availableConnections.Clear();
        
        _logger.Info($"连接池已释放 (总连接数: {_totalConnections})");
    }
}

/// <summary>
/// 池化连接
/// </summary>
public class PooledConnection<T> : IDisposable where T : class
{
    private readonly ConnectionPool<T> _pool;
    
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public T Connection { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; private set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; }

    public PooledConnection(T connection, ConnectionPool<T> pool)
    {
        Connection = connection;
        _pool = pool;
    }

    internal void MarkActive()
    {
        IsActive = true;
        LastUsedAt = DateTime.UtcNow;
    }

    internal void MarkAvailable()
    {
        IsActive = false;
        LastUsedAt = DateTime.UtcNow;
    }

    public void Dispose()
    {
        if (IsActive)
        {
            _pool.Release(this);
        }
    }
}

