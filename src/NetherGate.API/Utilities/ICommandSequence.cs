namespace NetherGate.API.Utilities;

/// <summary>
/// 命令序列接口
/// 提供流式 API 用于创建命令执行序列
/// </summary>
public interface ICommandSequence
{
    /// <summary>
    /// 执行异步操作
    /// </summary>
    ICommandSequence Execute(Func<Task> action);
    
    /// <summary>
    /// 执行同步操作
    /// </summary>
    ICommandSequence Execute(Action action);
    
    /// <summary>
    /// 等待指定毫秒数
    /// </summary>
    ICommandSequence Wait(int milliseconds);
    
    /// <summary>
    /// 等待指定游戏刻数（1 tick = 50ms）
    /// </summary>
    ICommandSequence WaitTicks(int ticks);
    
    /// <summary>
    /// 等待指定秒数
    /// </summary>
    ICommandSequence WaitSeconds(double seconds);
    
    /// <summary>
    /// 重复前面的操作指定次数
    /// </summary>
    /// <param name="times">重复次数</param>
    ICommandSequence Repeat(int times);
    
    /// <summary>
    /// 重复整个序列指定次数
    /// </summary>
    ICommandSequence RepeatSequence(int times);
    
    /// <summary>
    /// 条件执行
    /// </summary>
    ICommandSequence If(Func<bool> condition, Action<ICommandSequence> thenBranch);
    
    /// <summary>
    /// 条件执行（含 else 分支）
    /// </summary>
    ICommandSequence If(Func<bool> condition, Action<ICommandSequence> thenBranch, Action<ICommandSequence> elseBranch);
    
    /// <summary>
    /// 异步条件执行
    /// </summary>
    ICommandSequence IfAsync(Func<Task<bool>> condition, Action<ICommandSequence> thenBranch);
    
    /// <summary>
    /// 并行执行多个操作
    /// </summary>
    ICommandSequence Parallel(params Func<Task>[] actions);
    
    /// <summary>
    /// 循环执行直到条件为真
    /// </summary>
    ICommandSequence While(Func<bool> condition, Action<ICommandSequence> body);
    
    /// <summary>
    /// For 循环
    /// </summary>
    ICommandSequence For(int start, int end, Action<ICommandSequence, int> body);
    
    /// <summary>
    /// ForEach 循环
    /// </summary>
    ICommandSequence ForEach<T>(IEnumerable<T> items, Action<ICommandSequence, T> body);
    
    /// <summary>
    /// 添加日志输出
    /// </summary>
    ICommandSequence Log(string message);
    
    /// <summary>
    /// 捕获异常
    /// </summary>
    ICommandSequence Catch(Action<Exception> handler);
    
    /// <summary>
    /// 最终执行（无论是否发生异常）
    /// </summary>
    ICommandSequence Finally(Action finallyAction);
    
    /// <summary>
    /// 运行序列
    /// </summary>
    Task RunAsync();
    
    /// <summary>
    /// 运行序列并返回结果
    /// </summary>
    Task<T> RunAsync<T>(Func<T> resultSelector);
    
    /// <summary>
    /// 在后台运行序列（不等待完成）
    /// </summary>
    Task RunInBackgroundAsync();
    
    /// <summary>
    /// 取消序列执行
    /// </summary>
    void Cancel();
}

