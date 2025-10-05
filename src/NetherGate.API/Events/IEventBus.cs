namespace NetherGate.API.Events;

/// <summary>
/// 事件总线接口
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    /// <param name="priority">优先级（越大越优先，默认 0）</param>
    void Subscribe<TEvent>(Action<TEvent> handler, int priority = 0);

    /// <summary>
    /// 订阅事件（异步）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    /// <param name="priority">优先级（越大越优先，默认 0）</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0);

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    void Unsubscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// 取消订阅事件（异步）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler);

    /// <summary>
    /// 发布事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    Task PublishAsync<TEvent>(TEvent @event);

    /// <summary>
    /// 清空所有订阅
    /// </summary>
    void ClearAllSubscriptions();
}

