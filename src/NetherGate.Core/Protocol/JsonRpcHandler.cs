using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NetherGate.Core.Protocol;

/// <summary>
/// JSON-RPC 2.0 处理器
/// </summary>
public class JsonRpcHandler
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<object, TaskCompletionSource<JsonRpcResponse>> _pendingRequests;
    private readonly ConcurrentDictionary<string, List<Action<JsonRpcNotification>>> _notificationHandlers;
    private int _nextId;

    public JsonRpcHandler(ILogger logger)
    {
        _logger = logger;
        _pendingRequests = new ConcurrentDictionary<object, TaskCompletionSource<JsonRpcResponse>>();
        _notificationHandlers = new ConcurrentDictionary<string, List<Action<JsonRpcNotification>>>();
        _nextId = 1;
    }

    /// <summary>
    /// 创建请求
    /// </summary>
    public JsonRpcRequest CreateRequest(string method, object? parameters = null)
    {
        var id = Interlocked.Increment(ref _nextId);
        return new JsonRpcRequest
        {
            Id = id,
            Method = method,
            Params = parameters
        };
    }

    /// <summary>
    /// 发送请求并等待响应
    /// </summary>
    public async Task<JsonRpcResponse> SendRequestAsync(
        JsonRpcRequest request,
        Func<string, Task> sendFunc,
        TimeSpan timeout)
    {
        if (request.Id == null)
            throw new ArgumentException("Request must have an ID", nameof(request));

        var tcs = new TaskCompletionSource<JsonRpcResponse>();
        _pendingRequests[request.Id] = tcs;

        try
        {
            var json = JsonSerializer.Serialize(request);
            _logger.Trace($"发送 JSON-RPC 请求: {json}");

            await sendFunc(json);

            using var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            _pendingRequests.TryRemove(request.Id, out _);
            _logger.Warning($"请求超时: {request.Method}");
            throw new TimeoutException($"JSON-RPC request timed out: {request.Method}");
        }
        catch (Exception ex)
        {
            _pendingRequests.TryRemove(request.Id, out _);
            _logger.Error($"发送请求失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 调用方法并等待响应
    /// </summary>
    public async Task<TResponse?> InvokeAsync<TResponse>(
        string method,
        object? parameters,
        Func<string, Task> sendFunc,
        TimeSpan timeout)
    {
        var request = CreateRequest(method, parameters);
        var response = await SendRequestAsync(request, sendFunc, timeout);

        if (response.Error != null)
        {
            throw new JsonRpcException(
                response.Error.Code,
                response.Error.Message,
                response.Error.Data);
        }

        if (response.Result == null)
            return default;

        // 反序列化结果
        var resultJson = JsonSerializer.Serialize(response.Result);
        return JsonSerializer.Deserialize<TResponse>(resultJson);
    }

    /// <summary>
    /// 发送通知（不等待响应）
    /// </summary>
    public async Task SendNotificationAsync(
        string method,
        object? parameters,
        Func<string, Task> sendFunc)
    {
        var notification = new JsonRpcNotification
        {
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(notification);
        _logger.Trace($"发送 JSON-RPC 通知: {json}");

        await sendFunc(json);
    }

    /// <summary>
    /// 注册通知处理器
    /// </summary>
    public void RegisterNotificationHandler(string method, Action<JsonRpcNotification> handler)
    {
        var handlers = _notificationHandlers.GetOrAdd(method, _ => new List<Action<JsonRpcNotification>>());
        
        lock (handlers)
        {
            handlers.Add(handler);
        }

        _logger.Debug($"注册通知处理器: {method}");
    }

    /// <summary>
    /// 注销通知处理器
    /// </summary>
    public void UnregisterNotificationHandler(string method, Action<JsonRpcNotification> handler)
    {
        if (_notificationHandlers.TryGetValue(method, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <summary>
    /// 处理收到的消息
    /// </summary>
    public void HandleMessage(string json)
    {
        try
        {
            _logger.Trace($"处理 JSON-RPC 消息: {json}");

            // 尝试解析为响应
            var response = TryParseResponse(json);
            if (response != null)
            {
                HandleResponse(response);
                return;
            }

            // 尝试解析为通知
            var notification = TryParseNotification(json);
            if (notification != null)
            {
                HandleNotification(notification);
                return;
            }

            _logger.Warning($"无法解析 JSON-RPC 消息: {json}");
        }
        catch (Exception ex)
        {
            _logger.Error($"处理消息时出错: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理响应
    /// </summary>
    private void HandleResponse(JsonRpcResponse response)
    {
        if (response.Id == null)
        {
            _logger.Warning("收到没有 ID 的响应");
            return;
        }

        if (_pendingRequests.TryRemove(response.Id, out var tcs))
        {
            _logger.Trace($"完成请求: {response.Id}");
            tcs.SetResult(response);
        }
        else
        {
            _logger.Warning($"收到未知请求的响应: {response.Id}");
        }
    }

    /// <summary>
    /// 处理通知
    /// </summary>
    private void HandleNotification(JsonRpcNotification notification)
    {
        _logger.Debug($"收到通知: {notification.Method}");

        if (_notificationHandlers.TryGetValue(notification.Method, out var handlers))
        {
            List<Action<JsonRpcNotification>> handlersCopy;
            lock (handlers)
            {
                handlersCopy = new List<Action<JsonRpcNotification>>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    handler(notification);
                }
                catch (Exception ex)
                {
                    _logger.Error($"通知处理器执行失败: {ex.Message}", ex);
                }
            }
        }
        else
        {
            _logger.Debug($"没有注册 {notification.Method} 的处理器");
        }
    }

    /// <summary>
    /// 尝试解析响应
    /// </summary>
    private JsonRpcResponse? TryParseResponse(string json)
    {
        try
        {
            var response = JsonSerializer.Deserialize<JsonRpcResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // 有 ID 字段才是响应
            return response?.Id != null ? response : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 尝试解析通知
    /// </summary>
    private JsonRpcNotification? TryParseNotification(string json)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<JsonRpcNotification>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // 没有 ID 字段才是通知
            return notification?.Method != null ? notification : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 清理挂起的请求
    /// </summary>
    public void ClearPendingRequests()
    {
        foreach (var kvp in _pendingRequests)
        {
            kvp.Value.TrySetCanceled();
        }
        _pendingRequests.Clear();
        _logger.Info("清理所有挂起的请求");
    }
}

/// <summary>
/// JSON-RPC 异常
/// </summary>
public class JsonRpcException : Exception
{
    public int Code { get; }
    public new object? Data { get; }

    public JsonRpcException(int code, string message, object? data = null)
        : base(message)
    {
        Code = code;
        Data = data;
    }
}

