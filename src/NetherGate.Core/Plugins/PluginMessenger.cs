using System.Collections.Concurrent;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 插件间消息传递实现
/// </summary>
public class PluginMessenger : IPluginMessenger
{
    private readonly string _pluginId;
    private readonly ILogger _logger;
    private readonly Func<string, IPlugin?> _pluginResolver;
    private readonly Func<string, PluginState> _pluginStateResolver;
    private readonly Func<IReadOnlyList<IPlugin>> _allPluginsResolver;
    
    // 存储每个频道的订阅者（支持响应）
    private readonly ConcurrentDictionary<string, Func<PluginMessage, Task<object?>>> _responseHandlers = new();
    
    // 存储每个频道的订阅者（无响应）
    private readonly ConcurrentDictionary<string, Func<PluginMessage, Task>> _simpleHandlers = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="pluginId">当前插件 ID</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="pluginResolver">插件解析器（用于获取其他插件实例）</param>
    /// <param name="pluginStateResolver">插件状态解析器</param>
    /// <param name="allPluginsResolver">所有插件解析器</param>
    public PluginMessenger(
        string pluginId,
        ILogger logger,
        Func<string, IPlugin?> pluginResolver,
        Func<string, PluginState> pluginStateResolver,
        Func<IReadOnlyList<IPlugin>> allPluginsResolver)
    {
        _pluginId = pluginId;
        _logger = logger;
        _pluginResolver = pluginResolver;
        _pluginStateResolver = pluginStateResolver;
        _allPluginsResolver = allPluginsResolver;
    }

    /// <inheritdoc/>
    public async Task<object?> SendMessageAsync(string targetPluginId, string channel, object data, bool requireResponse = false)
    {
        if (string.IsNullOrWhiteSpace(targetPluginId))
            throw new ArgumentException("Target plugin ID cannot be null or empty", nameof(targetPluginId));

        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

        // 检查目标插件是否可用
        if (!IsPluginAvailable(targetPluginId))
        {
            _logger.Warning($"目标插件不可用: {targetPluginId}");
            return null;
        }

        var plugin = _pluginResolver(targetPluginId);
        if (plugin == null)
        {
            _logger.Warning($"未找到插件: {targetPluginId}");
            return null;
        }

        var message = new PluginMessage
        {
            SenderPluginId = _pluginId,
            ReceiverPluginId = targetPluginId,
            Channel = channel,
            Data = data,
            RequireResponse = requireResponse,
            Timestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString()
        };

        try
        {
            // 获取目标插件的 Messenger
            var targetContext = GetPluginContext(plugin);
            if (targetContext?.Messenger == null)
            {
                _logger.Warning($"无法获取插件 {targetPluginId} 的消息传递器");
                return null;
            }

            // 投递消息
            var response = await targetContext.Messenger.DeliverMessageAsync(message);

            _logger.Debug($"消息已发送: {_pluginId} -> {targetPluginId} [{channel}]");

            return response;
        }
        catch (Exception ex)
        {
            _logger.Error($"发送消息失败: {_pluginId} -> {targetPluginId} [{channel}]", ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task BroadcastMessageAsync(string channel, object data, bool excludeSelf = true)
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

        var message = new PluginMessage
        {
            SenderPluginId = _pluginId,
            ReceiverPluginId = "*", // 广播标记
            Channel = channel,
            Data = data,
            RequireResponse = false,
            Timestamp = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString()
        };

        var tasks = new List<Task>();

        // 获取所有插件
        var allPlugins = GetAllPlugins();
        foreach (var plugin in allPlugins)
        {
            // 跳过自己（如果需要）
            if (excludeSelf && plugin.Info.Id == _pluginId)
                continue;

            // 跳过未启用的插件
            if (_pluginStateResolver(plugin.Info.Id) != PluginState.Enabled)
                continue;

            // 获取插件的 Messenger
            var targetContext = GetPluginContext(plugin);
            if (targetContext?.Messenger == null)
                continue;

            // 异步投递消息
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await targetContext.Messenger.DeliverMessageAsync(message);
                    _logger.Debug($"广播消息已投递: {_pluginId} -> {plugin.Info.Id} [{channel}]");
                }
                catch (Exception ex)
                {
                    _logger.Error($"广播消息投递失败: {_pluginId} -> {plugin.Info.Id} [{channel}]", ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        _logger.Debug($"广播消息已发送: {_pluginId} [{channel}] -> {tasks.Count} 个插件");
    }

    /// <inheritdoc/>
    public void Subscribe(string channel, Func<PluginMessage, Task<object?>> handler)
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _responseHandlers[channel] = handler;
        _logger.Debug($"插件 {_pluginId} 已订阅频道: {channel} (带响应)");
    }

    /// <inheritdoc/>
    public void Subscribe(string channel, Func<PluginMessage, Task> handler)
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", nameof(channel));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _simpleHandlers[channel] = handler;
        _logger.Debug($"插件 {_pluginId} 已订阅频道: {channel}");
    }

    /// <inheritdoc/>
    public void Unsubscribe(string channel)
    {
        var removed1 = _responseHandlers.TryRemove(channel, out _);
        var removed2 = _simpleHandlers.TryRemove(channel, out _);

        if (removed1 || removed2)
        {
            _logger.Debug($"插件 {_pluginId} 已取消订阅频道: {channel}");
        }
    }

    /// <inheritdoc/>
    public void UnsubscribeAll()
    {
        var count = _responseHandlers.Count + _simpleHandlers.Count;
        _responseHandlers.Clear();
        _simpleHandlers.Clear();
        
        _logger.Debug($"插件 {_pluginId} 已取消所有订阅 ({count} 个频道)");
    }

    /// <inheritdoc/>
    public bool IsPluginAvailable(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var plugin = _pluginResolver(pluginId);
        if (plugin == null)
            return false;

        var state = _pluginStateResolver(pluginId);
        return state == PluginState.Enabled;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetSubscribedChannels(string? pluginId = null)
    {
        // 如果指定了插件 ID，则需要获取该插件的订阅频道
        if (!string.IsNullOrEmpty(pluginId) && pluginId != _pluginId)
        {
            var plugin = _pluginResolver(pluginId);
            if (plugin == null)
                return Array.Empty<string>();

            var context = GetPluginContext(plugin);
            if (context?.Messenger == null)
                return Array.Empty<string>();

            return context.Messenger.GetSubscribedChannels();
        }

        // 返回当前插件的订阅频道
        var channels = new List<string>();
        channels.AddRange(_responseHandlers.Keys);
        channels.AddRange(_simpleHandlers.Keys);
        return channels.Distinct().ToList();
    }

    /// <summary>
    /// 投递消息（内部方法，由其他插件调用）
    /// </summary>
    internal async Task<object?> DeliverMessageAsync(PluginMessage message)
    {
        // 优先使用响应处理器
        if (_responseHandlers.TryGetValue(message.Channel, out var responseHandler))
        {
            try
            {
                return await responseHandler(message);
            }
            catch (Exception ex)
            {
                _logger.Error($"处理消息失败: {message.SenderPluginId} -> {_pluginId} [{message.Channel}]", ex);
                return null;
            }
        }

        // 使用简单处理器
        if (_simpleHandlers.TryGetValue(message.Channel, out var simpleHandler))
        {
            try
            {
                await simpleHandler(message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"处理消息失败: {message.SenderPluginId} -> {_pluginId} [{message.Channel}]", ex);
                return null;
            }
        }

        // 没有找到处理器
        _logger.Warning($"未找到频道处理器: {_pluginId} [{message.Channel}]");
        return null;
    }

    /// <summary>
    /// 获取插件上下文（通过反射）
    /// </summary>
    private IPluginContextInternal? GetPluginContext(IPlugin plugin)
    {
        // 尝试通过反射获取插件的 Context 属性
        var contextProperty = plugin.GetType().GetProperty("Context");
        if (contextProperty == null)
            return null;

        return contextProperty.GetValue(plugin) as IPluginContextInternal;
    }

    /// <summary>
    /// 获取所有插件
    /// </summary>
    private List<IPlugin> GetAllPlugins()
    {
        var allPlugins = _allPluginsResolver();
        return allPlugins.ToList();
    }
}

/// <summary>
/// 插件上下文内部接口（用于访问 Messenger）
/// </summary>
internal interface IPluginContextInternal
{
    PluginMessenger? Messenger { get; }
}
