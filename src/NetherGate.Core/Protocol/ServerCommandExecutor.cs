using NetherGate.API.Configuration;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.Core.Process;
using NetherGate.API.Events;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace NetherGate.Core.Protocol;

/// <summary>
/// 统一服务器命令执行器：优先使用 RCON（如可用），否则使用 STDIN（java/script 模式）
/// </summary>
public class ServerCommandExecutor : IServerCommandExecutor
{
    private readonly ILogger _logger;
    private readonly ServerProcessManager? _processManager;
    private readonly IRconClient? _rcon;
    private readonly NetherGateConfig _config;
    private readonly IEventBus? _eventBus;
    
    // 性能统计
    private long _totalExecutions;
    private long _successCount;
    private long _failureCount;
    private double _totalExecutionTimeMs;
    private double _maxExecutionTimeMs;
    private double _lastExecutionTimeMs;
    private int _currentExecuting;
    private readonly object _statsLock = new();

    public ServerCommandExecutor(
        NetherGateConfig config,
        ILogger logger,
        ServerProcessManager? processManager,
        IRconClient? rcon,
        IEventBus? eventBus = null)
    {
        _config = config;
        _logger = logger;
        _processManager = processManager;
        _rcon = rcon;
        _eventBus = eventBus;
    }

    public async Task<bool> TryExecuteAsync(string command, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        Interlocked.Increment(ref _currentExecuting);
        
        try
        {
            // 优先 RCON（如开启且已连接）
            if (_config.Rcon.Enabled && _rcon != null && _rcon.IsConnected)
            {
                try
                {
                    var resp = await _rcon.ExecuteCommandAsync(command);
                    if (_eventBus != null)
                    {
                        try 
                        { 
                            await _eventBus.PublishAsync(new ServerCommandResponseEvent { Command = command, Success = true, Response = resp, Channel = "rcon" }); 
                        } 
                        catch (Exception ex) 
                        { 
                            _logger.Debug($"发布 RCON 命令响应事件失败: {ex.Message}"); 
                        }
                    }
                    RecordStats(true, sw.Elapsed.TotalMilliseconds);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"RCON 执行失败，回退到 STDIN: {ex.Message}");
                }
            }

            // STDIN（仅限非 external 且进程运行中）
            if (_processManager != null && _processManager.IsRunning &&
                !_config.ServerProcess.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _processManager.SendCommandAsync(command);
                    if (_eventBus != null)
                    {
                        try 
                        { 
                            await _eventBus.PublishAsync(new ServerCommandResponseEvent { Command = command, Success = true, Response = null, Channel = "stdin" }); 
                        } 
                        catch (Exception ex) 
                        { 
                            _logger.Debug($"发布 STDIN 命令响应事件失败: {ex.Message}"); 
                        }
                    }
                    RecordStats(true, sw.Elapsed.TotalMilliseconds);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"STDIN 发送命令失败: {ex.Message}");
                    RecordStats(false, sw.Elapsed.TotalMilliseconds);
                    return false;
                }
            }

            _logger.Warning("没有可用的命令通道（RCON 未连接且 STDIN 不可用）");
            RecordStats(false, sw.Elapsed.TotalMilliseconds);
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _currentExecuting);
        }
    }

    public async Task<(bool Success, string? Response)> TryExecuteWithResponseAsync(string command, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        Interlocked.Increment(ref _currentExecuting);
        
        try
        {
            // 只有 RCON 能稳定拿到响应
            if (_config.Rcon.Enabled && _rcon != null && _rcon.IsConnected)
            {
                try
                {
                    var resp = await _rcon.ExecuteCommandAsync(command);
                    if (_eventBus != null)
                    {
                        try 
                        { 
                            await _eventBus.PublishAsync(new ServerCommandResponseEvent { Command = command, Success = true, Response = resp, Channel = "rcon" }); 
                        } 
                        catch (Exception ex) 
                        { 
                            _logger.Debug($"发布 RCON 命令响应事件失败 (带响应): {ex.Message}"); 
                        }
                    }
                    RecordStats(true, sw.Elapsed.TotalMilliseconds);
                    return (true, resp);
                }
                catch (Exception ex)
                {
                    _logger.Error($"RCON 执行命令获取响应失败: {ex.Message}");
                    RecordStats(false, sw.Elapsed.TotalMilliseconds);
                    return (false, null);
                }
            }

            // 回退：尝试 STDIN，但无法提供响应
            var ok = await TryExecuteAsync(command, cancellationToken);
            return (ok, ok ? null : null);
        }
        finally
        {
            Interlocked.Decrement(ref _currentExecuting);
        }
    }
    
    /// <summary>
    /// Fire-and-Forget 模式：执行命令但不等待结果
    /// </summary>
    public void ExecuteFireAndForget(string command)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await TryExecuteAsync(command);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fire-and-Forget 命令执行异常: {ex.Message}");
            }
        });
    }
    
    /// <summary>
    /// 批量执行命令（按顺序）
    /// </summary>
    public async Task<List<(string Command, bool Success, string? Response)>> ExecuteBatchAsync(
        IEnumerable<string> commands,
        bool stopOnError = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(string Command, bool Success, string? Response)>();
        var commandList = commands.ToList();
        
        _logger.Info($"开始批量执行 {commandList.Count} 个命令（顺序执行，stopOnError={stopOnError}）");
        var sw = Stopwatch.StartNew();
        
        foreach (var command in commandList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Warning("批量命令执行被取消");
                break;
            }
            
            var (success, response) = await TryExecuteWithResponseAsync(command, cancellationToken);
            results.Add((command, success, response));
            
            if (!success && stopOnError)
            {
                _logger.Warning($"命令执行失败，停止批量执行: {command}");
                break;
            }
        }
        
        sw.Stop();
        _logger.Info($"批量执行完成，耗时 {sw.ElapsedMilliseconds}ms，成功 {results.Count(r => r.Success)}/{results.Count}");
        
        return results;
    }
    
    /// <summary>
    /// 并行执行多个命令
    /// </summary>
    public async Task<List<(string Command, bool Success, string? Response)>> ExecuteParallelAsync(
        IEnumerable<string> commands,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default)
    {
        var commandList = commands.ToList();
        var results = new ConcurrentBag<(string Command, bool Success, string? Response)>();
        
        _logger.Info($"开始并行执行 {commandList.Count} 个命令（并行度={maxDegreeOfParallelism}）");
        var sw = Stopwatch.StartNew();
        
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };
        
        try
        {
            await Parallel.ForEachAsync(commandList, options, async (command, ct) =>
            {
                var (success, response) = await TryExecuteWithResponseAsync(command, ct);
                results.Add((command, success, response));
            });
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("并行命令执行被取消");
        }
        
        sw.Stop();
        var resultList = results.ToList();
        _logger.Info($"并行执行完成，耗时 {sw.ElapsedMilliseconds}ms，成功 {resultList.Count(r => r.Success)}/{resultList.Count}");
        
        return resultList;
    }
    
    /// <summary>
    /// 获取统计信息
    /// </summary>
    public CommandExecutionStats GetStats()
    {
        lock (_statsLock)
        {
            return new CommandExecutionStats
            {
                TotalExecutions = _totalExecutions,
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                AverageExecutionTimeMs = _totalExecutions > 0 ? _totalExecutionTimeMs / _totalExecutions : 0,
                MaxExecutionTimeMs = _maxExecutionTimeMs,
                LastExecutionTimeMs = _lastExecutionTimeMs,
                CurrentExecuting = _currentExecuting
            };
        }
    }
    
    /// <summary>
    /// 记录统计信息
    /// </summary>
    private void RecordStats(bool success, double executionTimeMs)
    {
        lock (_statsLock)
        {
            _totalExecutions++;
            _totalExecutionTimeMs += executionTimeMs;
            _lastExecutionTimeMs = executionTimeMs;
            
            if (executionTimeMs > _maxExecutionTimeMs)
            {
                _maxExecutionTimeMs = executionTimeMs;
            }
            
            if (success)
            {
                _successCount++;
            }
            else
            {
                _failureCount++;
            }
            
            // 每1000次执行记录一次性能日志
            if (_totalExecutions % 1000 == 0)
            {
                var avgTime = _totalExecutionTimeMs / _totalExecutions;
                _logger.Info($"命令执行统计：总计 {_totalExecutions} 次，成功率 {(_successCount * 100.0 / _totalExecutions):F2}%，平均耗时 {avgTime:F2}ms，最大耗时 {_maxExecutionTimeMs:F2}ms");
            }
        }
    }
}


