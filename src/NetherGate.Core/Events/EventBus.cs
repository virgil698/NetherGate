using System.Collections.Concurrent;
using NetherGate.API.Events;
using NetherGate.API.Logging;

namespace NetherGate.Core.Events;

/// <summary>
/// 事件处理器包装
/// </summary>
internal class EventHandlerWrapper
{
    public int Priority { get; }
    public Delegate Handler { get; }

    public EventHandlerWrapper(Delegate handler, int priority)
    {
        Handler = handler;
        Priority = priority;
    }

    public async Task InvokeAsync(object @event)
    {
        if (Handler is Func<object, Task> asyncHandler)
        {
            await asyncHandler(@event);
        }
        else if (Handler is Action<object> syncHandler)
        {
            syncHandler(@event);
        }
    }
}

/// <summary>
/// 事件总线实现
/// </summary>
public class EventBus : IEventBus
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Type, List<EventHandlerWrapper>> _handlers = new();
    private readonly object _lock = new();

    public EventBus(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 订阅事件（同步）
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler, int priority = 0)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var wrapper = new EventHandlerWrapper(
            new Action<object>(e => handler((TEvent)e)),
            priority
        );

        AddHandler(eventType, wrapper);
        _logger.Debug($"订阅事件: {eventType.Name}, 优先级: {priority}");
    }

    /// <summary>
    /// 订阅事件（异步）
    /// </summary>
    public void Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var wrapper = new EventHandlerWrapper(
            new Func<object, Task>(e => handler((TEvent)e)),
            priority
        );

        AddHandler(eventType, wrapper);
        _logger.Debug($"订阅事件（异步）: {eventType.Name}, 优先级: {priority}");
    }

    /// <summary>
    /// 取消订阅事件（同步）
    /// </summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        RemoveHandler(eventType, h => 
            h.Handler is Action<object> action && 
            action.Method == handler.Method && 
            action.Target == handler.Target
        );

        _logger.Debug($"取消订阅事件: {eventType.Name}");
    }

    /// <summary>
    /// 取消订阅事件（异步）
    /// </summary>
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        RemoveHandler(eventType, h => 
            h.Handler is Func<object, Task> func && 
            func.Method == handler.Method && 
            func.Target == handler.Target
        );

        _logger.Debug($"取消订阅事件（异步）: {eventType.Name}");
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        var handlers = GetHandlers(eventType);

        if (handlers.Count == 0)
        {
            _logger.Trace($"事件 {eventType.Name} 没有订阅者");
            return;
        }

        _logger.Trace($"发布事件: {eventType.Name}, 订阅者数量: {handlers.Count}");

        // 按优先级排序（从高到低）
        var sortedHandlers = handlers.OrderByDescending(h => h.Priority).ToList();

        // 依次调用处理器
        foreach (var wrapper in sortedHandlers)
        {
            try
            {
                await wrapper.InvokeAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.Error($"事件处理器执行失败: {eventType.Name}", ex);
                // 继续执行其他处理器
            }
        }
    }

    /// <summary>
    /// 清空所有订阅
    /// </summary>
    public void ClearAllSubscriptions()
    {
        lock (_lock)
        {
            _handlers.Clear();
        }
        _logger.Info("已清空所有事件订阅");
    }

    // ========== 私有方法 ==========

    private void AddHandler(Type eventType, EventHandlerWrapper wrapper)
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<EventHandlerWrapper>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(wrapper);
        }
    }

    private void RemoveHandler(Type eventType, Func<EventHandlerWrapper, bool> predicate)
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.RemoveAll(h => predicate(h));

                // 如果列表为空，移除该类型
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                }
            }
        }
    }

    private List<EventHandlerWrapper> GetHandlers(Type eventType)
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                return new List<EventHandlerWrapper>(handlers);
            }

            return new List<EventHandlerWrapper>();
        }
    }
}

