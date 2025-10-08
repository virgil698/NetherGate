namespace NetherGate.API.Scheduling;

/// <summary>
/// 调度器接口，用于延时/周期任务
/// </summary>
public interface IScheduler
{
	/// <summary>
	/// 延时执行任务
	/// </summary>
	/// <param name="action">要执行的动作</param>
	/// <param name="delay">延时时间</param>
	/// <returns>可取消的任务句柄</returns>
	IScheduledTask CallLater(Action action, TimeSpan delay);

	/// <summary>
	/// 周期性执行任务
	/// </summary>
	/// <param name="action">要执行的动作</param>
	/// <param name="period">周期间隔</param>
	/// <param name="initialDelay">首次执行延时（可选）</param>
	/// <returns>可取消的任务句柄</returns>
	IScheduledTask CallPeriodic(Action action, TimeSpan period, TimeSpan? initialDelay = null);

	/// <summary>
	/// 异步延时执行
	/// </summary>
	IScheduledTask CallLaterAsync(Func<Task> asyncAction, TimeSpan delay);

	/// <summary>
	/// 异步周期执行
	/// </summary>
	IScheduledTask CallPeriodicAsync(Func<Task> asyncAction, TimeSpan period, TimeSpan? initialDelay = null);
}

/// <summary>
/// 已调度任务句柄
/// </summary>
public interface IScheduledTask : IDisposable
{
	/// <summary>
	/// 任务 ID
	/// </summary>
	string Id { get; }

	/// <summary>
	/// 是否已取消
	/// </summary>
	bool IsCancelled { get; }

	/// <summary>
	/// 取消任务
	/// </summary>
	void Cancel();
}
