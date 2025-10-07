using NetherGate.API.Configuration;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.Core.Process;
using NetherGate.API.Events;

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
        // 优先 RCON（如开启且已连接）
        if (_config.Rcon.Enabled && _rcon != null && _rcon.IsConnected)
        {
            try
            {
                var resp = await _rcon.ExecuteCommandAsync(command);
                if (_eventBus != null)
                {
                    try { await _eventBus.PublishAsync(new ServerCommandResponseEvent { Command = command, Success = true, Response = resp, Channel = "rcon" }); } catch { }
                }
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
                    try { await _eventBus.PublishAsync(new ServerCommandResponseEvent { Command = command, Success = true, Response = null, Channel = "stdin" }); } catch { }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"STDIN 发送命令失败: {ex.Message}");
                return false;
            }
        }

        _logger.Warning("没有可用的命令通道（RCON 未连接且 STDIN 不可用）");
        return false;
    }

    public async Task<(bool Success, string? Response)> TryExecuteWithResponseAsync(string command, CancellationToken cancellationToken = default)
    {
        // 只有 RCON 能稳定拿到响应
        if (_config.Rcon.Enabled && _rcon != null && _rcon.IsConnected)
        {
            try
            {
                var resp = await _rcon.ExecuteCommandAsync(command);
                if (_eventBus != null)
                {
                    try { await _eventBus.PublishAsync(new ServerCommandResponseEvent { Command = command, Success = true, Response = resp, Channel = "rcon" }); } catch { }
                }
                return (true, resp);
            }
            catch (Exception ex)
            {
                _logger.Error($"RCON 执行命令获取响应失败: {ex.Message}");
                return (false, null);
            }
        }

        // 回退：尝试 STDIN，但无法提供响应
        var ok = await TryExecuteAsync(command, cancellationToken);
        return (ok, ok ? null : null);
    }
}


