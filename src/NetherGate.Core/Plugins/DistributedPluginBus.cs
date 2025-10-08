using System.Collections.Concurrent;
using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.WebSocket;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 分布式插件消息总线 - 支持跨服务器节点的插件间通信
/// </summary>
public class DistributedPluginBus : IDisposable
{
    private readonly ILogger _logger;
    private readonly IWebSocketServer? _wsServer;
    private readonly ConcurrentDictionary<string, List<Action<DistributedMessage>>> _subscribers = new();
    private readonly ConcurrentDictionary<string, PluginNode> _nodes = new();
    private readonly string _currentNodeId;
    private bool _disposed;

    public string CurrentNodeId => _currentNodeId;
    public IReadOnlyDictionary<string, PluginNode> Nodes => _nodes;

    public DistributedPluginBus(ILogger logger, IWebSocketServer? wsServer = null)
    {
        _logger = logger;
        _wsServer = wsServer;
        _currentNodeId = GenerateNodeId();
        
        // 注册当前节点
        RegisterNode(new PluginNode
        {
            NodeId = _currentNodeId,
            NodeName = Environment.MachineName,
            IsLocal = true,
            LastSeen = DateTime.UtcNow
        });

        _logger.Info($"分布式插件总线已初始化 (节点 ID: {_currentNodeId})");
    }

    /// <summary>
    /// 发布消息（本地 + 远程）
    /// </summary>
    public async Task PublishAsync(string channel, object data, string? targetNodeId = null)
    {
        var message = new DistributedMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Channel = channel,
            SourceNodeId = _currentNodeId,
            TargetNodeId = targetNodeId,
            Data = JsonSerializer.Serialize(data),
            Timestamp = DateTime.UtcNow
        };

        // 本地分发
        await DispatchLocalAsync(message);

        // 远程分发（通过 WebSocket）
        if (_wsServer != null && (_nodes.Count > 1 || targetNodeId != null))
        {
            await DispatchRemoteAsync(message);
        }
    }

    /// <summary>
    /// 订阅消息
    /// </summary>
    public void Subscribe(string channel, Action<DistributedMessage> handler)
    {
        _subscribers.AddOrUpdate(
            channel,
            _ => new List<Action<DistributedMessage>> { handler },
            (_, handlers) =>
            {
                handlers.Add(handler);
                return handlers;
            });

        _logger.Debug($"订阅频道: {channel}");
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(string channel, Action<DistributedMessage> handler)
    {
        if (_subscribers.TryGetValue(channel, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _subscribers.TryRemove(channel, out _);
            }
        }
    }

    /// <summary>
    /// 注册远程节点
    /// </summary>
    public void RegisterNode(PluginNode node)
    {
        _nodes.AddOrUpdate(node.NodeId, node, (_, _) => node);
        _logger.Info($"注册节点: {node.NodeName} ({node.NodeId})");
    }

    /// <summary>
    /// 注销节点
    /// </summary>
    public void UnregisterNode(string nodeId)
    {
        if (_nodes.TryRemove(nodeId, out var node))
        {
            _logger.Info($"注销节点: {node.NodeName} ({nodeId})");
        }
    }

    /// <summary>
    /// 本地分发
    /// </summary>
    private async Task DispatchLocalAsync(DistributedMessage message)
    {
        // 如果消息指定了目标节点且不是当前节点，跳过本地分发
        if (!string.IsNullOrEmpty(message.TargetNodeId) && message.TargetNodeId != _currentNodeId)
        {
            return;
        }

        if (_subscribers.TryGetValue(message.Channel, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    await Task.Run(() => handler(message));
                }
                catch (Exception ex)
                {
                    _logger.Error($"处理消息失败 (频道: {message.Channel}): {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 远程分发（通过 WebSocket）
    /// </summary>
    private async Task DispatchRemoteAsync(DistributedMessage message)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                type = "plugin_message",
                data = message
            });

            await _wsServer!.BroadcastAsync(payload);
            _logger.Debug($"远程分发消息: {message.Channel} -> {message.TargetNodeId ?? "所有节点"}");
        }
        catch (Exception ex)
        {
            _logger.Error($"远程分发失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 接收远程消息（由 WebSocket 服务器调用）
    /// </summary>
    public async Task OnRemoteMessageAsync(DistributedMessage message)
    {
        // 更新源节点的最后活跃时间
        if (_nodes.TryGetValue(message.SourceNodeId, out var node))
        {
            node.LastSeen = DateTime.UtcNow;
        }

        // 分发到本地订阅者
        await DispatchLocalAsync(message);
    }

    /// <summary>
    /// 请求-响应模式
    /// </summary>
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        string channel,
        TRequest request,
        string? targetNodeId = null,
        TimeSpan? timeout = null)
    {
        var requestId = Guid.NewGuid().ToString();
        var responseChannel = $"{channel}:response:{requestId}";
        var tcs = new TaskCompletionSource<TResponse?>();

        // 订阅响应
        void ResponseHandler(DistributedMessage msg)
        {
            try
            {
                var response = JsonSerializer.Deserialize<TResponse>(msg.Data);
                tcs.TrySetResult(response);
            }
            catch (Exception ex)
            {
                _logger.Error($"解析响应失败: {ex.Message}");
                tcs.TrySetResult(default);
            }
        }

        Subscribe(responseChannel, ResponseHandler);

        try
        {
            // 发送请求
            var requestMessage = new
            {
                RequestId = requestId,
                Data = request
            };

            await PublishAsync(channel, requestMessage, targetNodeId);

            // 等待响应
            var timeoutTask = Task.Delay(timeout ?? TimeSpan.FromSeconds(30));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.Warning($"请求超时: {channel}");
                return default;
            }

            return await tcs.Task;
        }
        finally
        {
            Unsubscribe(responseChannel, ResponseHandler);
        }
    }

    /// <summary>
    /// 生成节点 ID
    /// </summary>
    private string GenerateNodeId()
    {
        return $"{Environment.MachineName}_{Guid.NewGuid().ToString("N")[..8]}";
    }

    /// <summary>
    /// 清理过期节点（定期调用）
    /// </summary>
    public void CleanupStaleNodes(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        var staleNodes = _nodes.Values
            .Where(n => !n.IsLocal && now - n.LastSeen > timeout)
            .ToList();

        foreach (var node in staleNodes)
        {
            UnregisterNode(node.NodeId);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _subscribers.Clear();
        _logger.Info("分布式插件总线已释放");
    }
}

/// <summary>
/// 分布式消息
/// </summary>
public class DistributedMessage
{
    /// <summary>
    /// 消息 ID
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 频道
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// 源节点 ID
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点 ID（null 表示广播到所有节点）
    /// </summary>
    public string? TargetNodeId { get; set; }

    /// <summary>
    /// 消息数据（JSON）
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 插件节点
/// </summary>
public class PluginNode
{
    /// <summary>
    /// 节点 ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 是否为本地节点
    /// </summary>
    public bool IsLocal { get; set; }

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// 元数据
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

