namespace NetherGate.API.Plugins;

/// <summary>
/// 插件间消息传递接口
/// 允许插件之间安全地发送和接收消息
/// </summary>
public interface IPluginMessenger
{
    /// <summary>
    /// 向指定插件发送消息
    /// </summary>
    /// <param name="targetPluginId">目标插件 ID</param>
    /// <param name="channel">消息频道（用于区分不同类型的消息）</param>
    /// <param name="data">消息数据</param>
    /// <param name="requireResponse">是否需要响应</param>
    /// <returns>如果 requireResponse 为 true，则返回目标插件的响应；否则返回 null</returns>
    Task<object?> SendMessageAsync(string targetPluginId, string channel, object data, bool requireResponse = false);

    /// <summary>
    /// 广播消息给所有插件
    /// </summary>
    /// <param name="channel">消息频道</param>
    /// <param name="data">消息数据</param>
    /// <param name="excludeSelf">是否排除自己</param>
    Task BroadcastMessageAsync(string channel, object data, bool excludeSelf = true);

    /// <summary>
    /// 订阅消息频道
    /// </summary>
    /// <param name="channel">消息频道</param>
    /// <param name="handler">消息处理器</param>
    void Subscribe(string channel, Func<PluginMessage, Task<object?>> handler);

    /// <summary>
    /// 订阅消息频道（无需返回响应）
    /// </summary>
    /// <param name="channel">消息频道</param>
    /// <param name="handler">消息处理器</param>
    void Subscribe(string channel, Func<PluginMessage, Task> handler);

    /// <summary>
    /// 取消订阅消息频道
    /// </summary>
    /// <param name="channel">消息频道</param>
    void Unsubscribe(string channel);

    /// <summary>
    /// 取消订阅所有消息频道
    /// </summary>
    void UnsubscribeAll();

    /// <summary>
    /// 检查插件是否存在并已启用
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    bool IsPluginAvailable(string pluginId);

    /// <summary>
    /// 获取插件的订阅频道列表
    /// </summary>
    /// <param name="pluginId">插件 ID（null 表示当前插件）</param>
    IReadOnlyList<string> GetSubscribedChannels(string? pluginId = null);
}

/// <summary>
/// 插件消息
/// </summary>
public class PluginMessage
{
    /// <summary>
    /// 发送者插件 ID
    /// </summary>
    public string SenderPluginId { get; init; } = string.Empty;

    /// <summary>
    /// 接收者插件 ID（广播时为 "*"）
    /// </summary>
    public string ReceiverPluginId { get; init; } = string.Empty;

    /// <summary>
    /// 消息频道
    /// </summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// 消息数据
    /// </summary>
    public object Data { get; init; } = null!;

    /// <summary>
    /// 消息发送时间
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 是否需要响应
    /// </summary>
    public bool RequireResponse { get; init; }

    /// <summary>
    /// 消息 ID（用于关联请求和响应）
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 回复消息
    /// 如果此消息需要响应，则返回响应消息
    /// </summary>
    public Task<object?> ReplyAsync(IPluginMessenger messenger, object? response)
    {
        if (!RequireResponse)
        {
            return Task.FromResult<object?>(null);
        }

        return Task.FromResult(response);
    }
}

/// <summary>
/// 插件消息事件参数
/// </summary>
public class PluginMessageEventArgs : EventArgs
{
    /// <summary>
    /// 插件消息
    /// </summary>
    public PluginMessage Message { get; init; } = null!;

    /// <summary>
    /// 是否已处理
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 响应数据（如果消息需要响应）
    /// </summary>
    public object? Response { get; set; }
}
