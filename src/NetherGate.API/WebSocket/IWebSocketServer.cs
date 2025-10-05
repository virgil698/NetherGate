namespace NetherGate.API.WebSocket;

/// <summary>
/// WebSocket 服务器接口
/// </summary>
public interface IWebSocketServer
{
    /// <summary>
    /// 启动服务器
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止服务器
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 当前连接数
    /// </summary>
    int ConnectionCount { get; }

    /// <summary>
    /// 广播消息给所有客户端
    /// </summary>
    Task BroadcastAsync(string message);

    /// <summary>
    /// 发送消息给指定客户端
    /// </summary>
    Task SendToClientAsync(string clientId, string message);
}

/// <summary>
/// WebSocket 消息
/// </summary>
public class WebSocketMessage
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 请求 ID（用于匹配请求和响应）
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// WebSocket 响应
/// </summary>
public class WebSocketResponse : WebSocketMessage
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    public static WebSocketResponse Ok(object? data = null, string? requestId = null)
    {
        return new WebSocketResponse
        {
            Type = "response",
            Success = true,
            Data = data,
            RequestId = requestId
        };
    }

    public static WebSocketResponse Fail(string error, string? requestId = null)
    {
        return new WebSocketResponse
        {
            Type = "response",
            Success = false,
            Error = error,
            RequestId = requestId
        };
    }
}

/// <summary>
/// WebSocket 事件
/// </summary>
public class WebSocketEvent : WebSocketMessage
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public string Event { get; set; } = string.Empty;

    public WebSocketEvent()
    {
        Type = "event";
    }
}
