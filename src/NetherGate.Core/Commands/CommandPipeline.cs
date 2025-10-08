using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Core.Commands;

/// <summary>
/// 命令管道 - 支持命令链式执行（类似 Linux 管道）
/// </summary>
public class CommandPipeline
{
    private readonly ICommandManager _commandManager;
    private readonly ILogger _logger;
    private readonly List<PipelineStage> _stages = new();

    public CommandPipeline(ICommandManager commandManager, ILogger logger)
    {
        _commandManager = commandManager;
        _logger = logger;
    }

    /// <summary>
    /// 添加管道阶段
    /// </summary>
    public CommandPipeline Pipe(string command)
    {
        _stages.Add(new PipelineStage { Command = command });
        return this;
    }

    /// <summary>
    /// 添加带过滤器的管道阶段
    /// </summary>
    public CommandPipeline Pipe(string command, Func<string, bool> filter)
    {
        _stages.Add(new PipelineStage { Command = command, Filter = filter });
        return this;
    }

    /// <summary>
    /// 执行管道
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(ICommandSender sender)
    {
        if (_stages.Count == 0)
        {
            return CommandResult.Fail("管道为空");
        }

        string? previousOutput = null;

        for (int i = 0; i < _stages.Count; i++)
        {
            var stage = _stages[i];
            var command = stage.Command;

            // 替换占位符 (使用 $input 表示上一个命令的输出)
            if (previousOutput != null && command.Contains("$input"))
            {
                command = command.Replace("$input", previousOutput);
            }

            // 执行命令
            var result = await _commandManager.ExecuteCommandAsync(command, sender);

            if (!result.Success)
            {
                _logger.Warning($"管道阶段 {i + 1} 失败: {result.Error}");
                return CommandResult.Fail($"管道在阶段 {i + 1} 失败: {result.Error}");
            }

            // 应用过滤器
            if (stage.Filter != null && !string.IsNullOrEmpty(result.Message))
            {
                var lines = result.Message.Split('\n');
                var filtered = lines.Where(stage.Filter);
                previousOutput = string.Join("\n", filtered);
            }
            else
            {
                previousOutput = result.Message;
            }
        }

        return CommandResult.Ok(previousOutput ?? string.Empty);
    }

    /// <summary>
    /// 解析管道命令字符串（支持 | 分隔符）
    /// </summary>
    public static CommandPipeline Parse(string pipelineCommand, ICommandManager commandManager, ILogger logger)
    {
        var pipeline = new CommandPipeline(commandManager, logger);
        
        // 按 | 分割命令
        var commands = pipelineCommand.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var cmd in commands)
        {
            pipeline.Pipe(cmd);
        }

        return pipeline;
    }

    private class PipelineStage
    {
        public string Command { get; set; } = string.Empty;
        public Func<string, bool>? Filter { get; set; }
    }
}

/// <summary>
/// 命令管道扩展方法
/// </summary>
public static class CommandPipelineExtensions
{
    /// <summary>
    /// 创建命令管道
    /// </summary>
    public static CommandPipeline CreatePipeline(this ICommandManager commandManager, ILogger logger)
    {
        return new CommandPipeline(commandManager, logger);
    }

    /// <summary>
    /// 执行管道命令
    /// </summary>
    public static async Task<CommandResult> ExecutePipelineAsync(
        this ICommandManager commandManager, 
        string pipelineCommand, 
        ICommandSender sender,
        ILogger logger)
    {
        var pipeline = CommandPipeline.Parse(pipelineCommand, commandManager, logger);
        return await pipeline.ExecuteAsync(sender);
    }
}

