namespace NetherGate.API.WebSocket;

/// <summary>
/// 数据广播器接口 - 通过 WebSocket 推送实时数据
/// </summary>
public interface IDataBroadcaster
{
    /// <summary>
    /// 向指定频道广播数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="data">数据</param>
    Task BroadcastAsync<T>(string channel, T data);

    /// <summary>
    /// 向指定频道的特定客户端发送数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="data">数据</param>
    Task SendToClientAsync<T>(string channel, string clientId, T data);

    /// <summary>
    /// 注册数据源（自动推送）
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="channel">频道名称</param>
    /// <param name="dataProvider">数据提供者</param>
    /// <param name="interval">推送间隔</param>
    Task RegisterDataSourceAsync<T>(string channel, Func<Task<T>> dataProvider, TimeSpan interval);

    /// <summary>
    /// 取消注册数据源
    /// </summary>
    /// <param name="channel">频道名称</param>
    Task UnregisterDataSourceAsync(string channel);

    /// <summary>
    /// 获取频道的订阅者数量
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <returns>订阅者数量</returns>
    int GetSubscriberCount(string channel);

    /// <summary>
    /// 获取所有活跃频道
    /// </summary>
    /// <returns>频道列表</returns>
    List<string> GetActiveChannels();

    /// <summary>
    /// 客户端连接事件
    /// </summary>
    event EventHandler<ClientConnectedEventArgs>? ClientConnected;

    /// <summary>
    /// 客户端断开事件
    /// </summary>
    event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
}

/// <summary>
/// 客户端连接事件参数
/// </summary>
public class ClientConnectedEventArgs : EventArgs
{
    /// <summary>
    /// 客户端 ID
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// 订阅的频道
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 客户端 IP 地址
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// 客户端断开事件参数
/// </summary>
public class ClientDisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// 客户端 ID
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// 频道
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// 断开时间
    /// </summary>
    public DateTime DisconnectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 断开原因
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// WebSocket 消息
/// </summary>
public class WebSocketMessage<T>
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public string Type { get; set; } = "data";

    /// <summary>
    /// 频道
    /// </summary>
    public required string Channel { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// 消息 ID
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
}

