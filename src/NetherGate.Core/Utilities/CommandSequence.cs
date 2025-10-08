using NetherGate.API.Logging;
using NetherGate.API.Utilities;
using System.Collections.Concurrent;

namespace NetherGate.Core.Utilities;

/// <summary>
/// 命令序列实现
/// </summary>
public class CommandSequence : ICommandSequence
{
    private readonly List<SequenceStep> _steps = new();
    private readonly ILogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private Action<Exception>? _exceptionHandler;
    private Action? _finallyAction;

    public CommandSequence(ILogger logger)
    {
        _logger = logger;
    }

    public ICommandSequence Execute(Func<Task> action)
    {
        _steps.Add(new SequenceStep { Action = action });
        return this;
    }

    public ICommandSequence Execute(Action action)
    {
        _steps.Add(new SequenceStep { Action = () => { action(); return Task.CompletedTask; } });
        return this;
    }

    public ICommandSequence Wait(int milliseconds)
    {
        _steps.Add(new SequenceStep { Action = async () => await Task.Delay(milliseconds) });
        return this;
    }

    public ICommandSequence WaitTicks(int ticks)
    {
        return Wait(ticks * 50); // 1 tick = 50ms
    }

    public ICommandSequence WaitSeconds(double seconds)
    {
        return Wait((int)(seconds * 1000));
    }

    public ICommandSequence Repeat(int times)
    {
        if (_steps.Count == 0) return this;

        var lastStep = _steps[^1];
        for (int i = 1; i < times; i++)
        {
            _steps.Add(lastStep);
        }
        return this;
    }

    public ICommandSequence RepeatSequence(int times)
    {
        var currentSteps = _steps.ToList();
        for (int i = 1; i < times; i++)
        {
            _steps.AddRange(currentSteps);
        }
        return this;
    }

    public ICommandSequence If(Func<bool> condition, Action<ICommandSequence> thenBranch)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () =>
            {
                if (condition())
                {
                    var branch = new CommandSequence(_logger);
                    thenBranch(branch);
                    await branch.RunAsync();
                }
            }
        });
        return this;
    }

    public ICommandSequence If(Func<bool> condition, Action<ICommandSequence> thenBranch, Action<ICommandSequence> elseBranch)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () =>
            {
                if (condition())
                {
                    var branch = new CommandSequence(_logger);
                    thenBranch(branch);
                    await branch.RunAsync();
                }
                else
                {
                    var branch = new CommandSequence(_logger);
                    elseBranch(branch);
                    await branch.RunAsync();
                }
            }
        });
        return this;
    }

    public ICommandSequence IfAsync(Func<Task<bool>> condition, Action<ICommandSequence> thenBranch)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () =>
            {
                if (await condition())
                {
                    var branch = new CommandSequence(_logger);
                    thenBranch(branch);
                    await branch.RunAsync();
                }
            }
        });
        return this;
    }

    public ICommandSequence Parallel(params Func<Task>[] actions)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () => await Task.WhenAll(actions.Select(a => a()))
        });
        return this;
    }

    public ICommandSequence While(Func<bool> condition, Action<ICommandSequence> body)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () =>
            {
                while (condition())
                {
                    var iteration = new CommandSequence(_logger);
                    body(iteration);
                    await iteration.RunAsync();
                }
            }
        });
        return this;
    }

    public ICommandSequence For(int start, int end, Action<ICommandSequence, int> body)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () =>
            {
                for (int i = start; i < end; i++)
                {
                    var iteration = new CommandSequence(_logger);
                    body(iteration, i);
                    await iteration.RunAsync();
                }
            }
        });
        return this;
    }

    public ICommandSequence ForEach<T>(IEnumerable<T> items, Action<ICommandSequence, T> body)
    {
        _steps.Add(new SequenceStep
        {
            Action = async () =>
            {
                foreach (var item in items)
                {
                    var iteration = new CommandSequence(_logger);
                    body(iteration, item);
                    await iteration.RunAsync();
                }
            }
        });
        return this;
    }

    public ICommandSequence Log(string message)
    {
        _steps.Add(new SequenceStep { Action = () => { _logger.Info(message); return Task.CompletedTask; } });
        return this;
    }

    public ICommandSequence Catch(Action<Exception> handler)
    {
        _exceptionHandler = handler;
        return this;
    }

    public ICommandSequence Finally(Action finallyAction)
    {
        _finallyAction = finallyAction;
        return this;
    }

    public async Task RunAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            foreach (var step in _steps)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.Debug("命令序列已取消");
                    break;
                }

                await step.Action();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"命令序列执行失败: {ex.Message}", ex);
            _exceptionHandler?.Invoke(ex);
            
            if (_exceptionHandler == null)
                throw;
        }
        finally
        {
            _finallyAction?.Invoke();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public async Task<T> RunAsync<T>(Func<T> resultSelector)
    {
        await RunAsync();
        return resultSelector();
    }

    public Task RunInBackgroundAsync()
    {
        _ = Task.Run(async () => await RunAsync());
        return Task.CompletedTask;
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    private class SequenceStep
    {
        public required Func<Task> Action { get; init; }
    }
}

