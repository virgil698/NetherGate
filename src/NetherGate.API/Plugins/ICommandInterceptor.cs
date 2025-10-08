namespace NetherGate.API.Plugins;

/// <summary>
/// 命令拦截器接口 - 用于在命令执行前后插入自定义逻辑
/// </summary>
public interface ICommandInterceptor
{
    /// <summary>
    /// 拦截器优先级（数值越小优先级越高）
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// 命令执行前调用
    /// </summary>
    /// <param name="command">要执行的命令</param>
    /// <param name="sender">命令发送者</param>
    /// <param name="args">命令参数</param>
    /// <param name="context">上下文数据（可用于在拦截器之间传递数据）</param>
    /// <returns>如果返回 false，则阻止命令执行</returns>
    Task<bool> BeforeExecuteAsync(ICommand command, ICommandSender sender, string[] args, Dictionary<string, object> context);

    /// <summary>
    /// 命令执行后调用
    /// </summary>
    /// <param name="command">已执行的命令</param>
    /// <param name="sender">命令发送者</param>
    /// <param name="args">命令参数</param>
    /// <param name="result">命令执行结果</param>
    /// <param name="context">上下文数据</param>
    /// <returns>可以修改并返回新的结果</returns>
    Task<CommandResult> AfterExecuteAsync(ICommand command, ICommandSender sender, string[] args, CommandResult result, Dictionary<string, object> context);

    /// <summary>
    /// 命令执行异常时调用
    /// </summary>
    /// <param name="command">执行失败的命令</param>
    /// <param name="sender">命令发送者</param>
    /// <param name="args">命令参数</param>
    /// <param name="exception">异常信息</param>
    /// <param name="context">上下文数据</param>
    Task OnExceptionAsync(ICommand command, ICommandSender sender, string[] args, Exception exception, Dictionary<string, object> context)
    {
        // 默认实现：不处理异常
        return Task.CompletedTask;
    }
}

/// <summary>
/// 命令拦截器基类 - 提供默认实现
/// </summary>
public abstract class CommandInterceptorBase : ICommandInterceptor
{
    /// <inheritdoc/>
    public virtual int Priority => 0;

    /// <inheritdoc/>
    public virtual Task<bool> BeforeExecuteAsync(ICommand command, ICommandSender sender, string[] args, Dictionary<string, object> context)
    {
        return Task.FromResult(true); // 默认允许执行
    }

    /// <inheritdoc/>
    public virtual Task<CommandResult> AfterExecuteAsync(ICommand command, ICommandSender sender, string[] args, CommandResult result, Dictionary<string, object> context)
    {
        return Task.FromResult(result); // 默认不修改结果
    }

    /// <inheritdoc/>
    public virtual Task OnExceptionAsync(ICommand command, ICommandSender sender, string[] args, Exception exception, Dictionary<string, object> context)
    {
        return Task.CompletedTask;
    }
}

