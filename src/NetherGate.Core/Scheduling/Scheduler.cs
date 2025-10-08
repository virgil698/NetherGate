using NetherGate.API.Logging;
using NetherGate.API.Scheduling;

namespace NetherGate.Core.Scheduling;

/// <summary>
/// 调度器实现
/// </summary>
public class Scheduler : IScheduler, IDisposable
{
	private readonly ILogger _logger;
	private readonly List<ScheduledTask> _tasks = new();
	private readonly object _lock = new();
	private bool _disposed;

	public Scheduler(ILogger logger)
	{
		_logger = logger;
	}

	public IScheduledTask CallLater(Action action, TimeSpan delay)
	{
		return Schedule(action, delay, isPeriodic: false);
	}

	public IScheduledTask CallPeriodic(Action action, TimeSpan period, TimeSpan? initialDelay = null)
	{
		return Schedule(action, initialDelay ?? period, isPeriodic: true, period);
	}

	public IScheduledTask CallLaterAsync(Func<Task> asyncAction, TimeSpan delay)
	{
		return Schedule(asyncAction, delay, isPeriodic: false);
	}

	public IScheduledTask CallPeriodicAsync(Func<Task> asyncAction, TimeSpan period, TimeSpan? initialDelay = null)
	{
		return Schedule(asyncAction, initialDelay ?? period, isPeriodic: true, period);
	}

	private IScheduledTask Schedule(Delegate action, TimeSpan delay, bool isPeriodic, TimeSpan? period = null)
	{
		if (_disposed) throw new ObjectDisposedException(nameof(Scheduler));

		var task = new ScheduledTask(action, delay, isPeriodic, period, _logger, OnTaskCompleted);
		lock (_lock)
		{
			_tasks.Add(task);
		}
		task.Start();
		return task;
	}

	private void OnTaskCompleted(ScheduledTask task)
	{
		lock (_lock)
		{
			_tasks.Remove(task);
		}
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		lock (_lock)
		{
			foreach (var task in _tasks.ToArray())
			{
				task.Cancel();
			}
			_tasks.Clear();
		}
	}
}

internal class ScheduledTask : IScheduledTask
{
	private readonly Delegate _action;
	private readonly bool _isPeriodic;
	private readonly TimeSpan? _period;
	private readonly ILogger _logger;
	private readonly Action<ScheduledTask> _onCompleted;
	private CancellationTokenSource? _cts;
	private Task? _runningTask;

	public string Id { get; } = Guid.NewGuid().ToString("N");
	public bool IsCancelled { get; private set; }

	public ScheduledTask(Delegate action, TimeSpan delay, bool isPeriodic, TimeSpan? period, ILogger logger, Action<ScheduledTask> onCompleted)
	{
		_action = action;
		_isPeriodic = isPeriodic;
		_period = period;
		_logger = logger;
		_onCompleted = onCompleted;
		_cts = new CancellationTokenSource();

		// 首次延时
		_runningTask = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(delay, _cts.Token);
				await ExecuteActionAsync();

				if (_isPeriodic && _period.HasValue && !_cts.Token.IsCancellationRequested)
				{
					await RunPeriodicAsync(_period.Value);
				}
			}
			catch (OperationCanceledException)
			{
				// 正常取消
			}
			catch (Exception ex)
			{
				_logger.Error($"调度任务 {Id} 执行失败", ex);
			}
			finally
			{
				_onCompleted(this);
			}
		}, _cts.Token);
	}

	public void Start()
	{
		// Task 已在构造函数中启动
	}

	private async Task RunPeriodicAsync(TimeSpan period)
	{
		while (!_cts!.Token.IsCancellationRequested)
		{
			await Task.Delay(period, _cts.Token);
			await ExecuteActionAsync();
		}
	}

	private async Task ExecuteActionAsync()
	{
		try
		{
			if (_action is Action syncAction)
			{
				syncAction();
			}
			else if (_action is Func<Task> asyncAction)
			{
				await asyncAction();
			}
		}
		catch (Exception ex)
		{
			_logger.Error($"调度任务 {Id} 回调执行失败", ex);
		}
	}

	public void Cancel()
	{
		if (IsCancelled) return;
		IsCancelled = true;
		_cts?.Cancel();
	}

	public void Dispose()
	{
		Cancel();
		_cts?.Dispose();
		_cts = null;
	}
}
