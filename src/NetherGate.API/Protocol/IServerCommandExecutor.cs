namespace NetherGate.API.Protocol;

/// <summary>
/// 统一的服务器命令执行器
/// 根据实际能力选择使用标准输入或 RCON 执行命令
/// </summary>
public interface IServerCommandExecutor
{
    /// <summary>
    /// 尝试执行命令（不关心响应）
    /// </summary>
    Task<bool> TryExecuteAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试执行命令并获取响应（如果底层支持，例如 RCON）
    /// </summary>
    Task<(bool Success, string? Response)> TryExecuteWithResponseAsync(string command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fire-and-Forget 模式：执行命令但不等待结果（用于不关心结果的场景）
    /// </summary>
    void ExecuteFireAndForget(string command);
    
    /// <summary>
    /// 批量执行命令（按顺序执行）
    /// </summary>
    /// <param name="commands">命令列表</param>
    /// <param name="stopOnError">遇到错误是否停止（默认 false，继续执行）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>每个命令的执行结果</returns>
    Task<List<(string Command, bool Success, string? Response)>> ExecuteBatchAsync(
        IEnumerable<string> commands, 
        bool stopOnError = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 并行执行多个命令（适用于互不依赖的命令）
    /// </summary>
    /// <param name="commands">命令列表</param>
    /// <param name="maxDegreeOfParallelism">最大并行度（默认 4）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>每个命令的执行结果</returns>
    Task<List<(string Command, bool Success, string? Response)>> ExecuteParallelAsync(
        IEnumerable<string> commands,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取命令执行统计信息
    /// </summary>
    CommandExecutionStats GetStats();
}

/// <summary>
/// 命令执行统计信息
/// </summary>
public record CommandExecutionStats
{
    /// <summary>
    /// 总执行次数
    /// </summary>
    public long TotalExecutions { get; init; }
    
    /// <summary>
    /// 成功次数
    /// </summary>
    public long SuccessCount { get; init; }
    
    /// <summary>
    /// 失败次数
    /// </summary>
    public long FailureCount { get; init; }
    
    /// <summary>
    /// 平均执行时间（毫秒）
    /// </summary>
    public double AverageExecutionTimeMs { get; init; }
    
    /// <summary>
    /// 最慢执行时间（毫秒）
    /// </summary>
    public double MaxExecutionTimeMs { get; init; }
    
    /// <summary>
    /// 最近的执行时间（毫秒）
    /// </summary>
    public double LastExecutionTimeMs { get; init; }
    
    /// <summary>
    /// 当前正在执行的命令数
    /// </summary>
    public int CurrentExecuting { get; init; }
}


