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
}


