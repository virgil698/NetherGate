using NetherGate.API.Events;
using System.Collections.Concurrent;

namespace NetherGate.Script.Wrappers;

/// <summary>
/// EventBus 的 JavaScript 包装器
/// </summary>
public class EventBusWrapper
{
    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<string, List<Action<object>>> _handlers = new();

    public EventBusWrapper(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public void subscribe(string eventName, Action<object> handler)
    {
        // 存储处理器引用（用于后续取消订阅）
        if (!_handlers.ContainsKey(eventName))
        {
            _handlers[eventName] = new List<Action<object>>();
        }
        _handlers[eventName].Add(handler);

        // 订阅到 C# EventBus (使用 object 作为事件类型)
        _eventBus.Subscribe<object>(handler);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    public void unsubscribe(string eventName, Action<object> handler)
    {
        if (_handlers.TryGetValue(eventName, out var handlers))
        {
            handlers.Remove(handler);
        }

        _eventBus.Unsubscribe<object>(handler);
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public async void publish(string eventName, object? eventData = null)
    {
        await _eventBus.PublishAsync(eventData ?? new { });
    }

    /// <summary>
    /// 清理所有事件处理器
    /// </summary>
    public void clearAll()
    {
        foreach (var (_, handlers) in _handlers)
        {
            foreach (var handler in handlers)
            {
                _eventBus.Unsubscribe<object>(handler);
            }
        }
        _handlers.Clear();
    }
}

