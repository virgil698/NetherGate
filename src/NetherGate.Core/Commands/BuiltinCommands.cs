using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;
using NetherGate.Core.Plugins;

namespace NetherGate.Core.Commands;

/// <summary>
/// Help 命令
/// </summary>
public class HelpCommand : ICommand
{
    private readonly ICommandManager _commandManager;

    public string Name => "help";
    public string Description => "显示所有可用命令";
    public string Usage => "help [命令名]";
    public List<string> Aliases => new() { "?" };
    public string PluginId => "nethergate";
    public string? Permission => null; // 无需权限

    public HelpCommand(ICommandManager commandManager)
    {
        _commandManager = commandManager;
    }

    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        // 补全命令名
        if (args.Length <= 1)
        {
            var commands = _commandManager.GetAllCommands();
            var prefix = args.Length == 1 ? args[0].ToLower() : "";
            
            return commands.Keys
                .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x)
                .ToList();
        }

        return await Task.FromResult(new List<string>());
    }

    public Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            // 显示所有命令
            var commands = _commandManager.GetAllCommands();
            var message = "可用命令:\n";

            foreach (var (name, cmd) in commands.OrderBy(c => c.Key))
            {
                message += $"  {name,-15} - {cmd.Description}\n";
            }

            message += "\n输入 'help <命令>' 查看详细用法";
            return Task.FromResult(CommandResult.Ok(message));
        }
        else
        {
            // 显示特定命令的帮助
            var commandName = args[0].ToLower();
            var commands = _commandManager.GetAllCommands();

            if (commands.TryGetValue(commandName, out var command))
            {
                var message = $"命令: {command.Name}\n" +
                             $"描述: {command.Description}\n" +
                             $"用法: {command.Usage}\n";

                if (command.Aliases.Count > 0)
                {
                    message += $"别名: {string.Join(", ", command.Aliases)}\n";
                }

                message += $"插件: {command.PluginId}";

                return Task.FromResult(CommandResult.Ok(message));
            }
            else
            {
                return Task.FromResult(CommandResult.Fail($"未知命令: {commandName}"));
            }
        }
    }
}

/// <summary>
/// Plugins 命令
/// </summary>
public class PluginsCommand : ICommand
{
    private readonly PluginManager _pluginManager;

    public string Name => "plugins";
    public string Description => "显示已加载的插件列表";
    public string Usage => "plugins";
    public List<string> Aliases => new() { "pl" };
    public string PluginId => "nethergate";
    public string? Permission => "nethergate.plugins.list";

    public PluginsCommand(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        var plugins = _pluginManager.GetAllPluginContainers();

        if (plugins.Count == 0)
        {
            return Task.FromResult(CommandResult.Ok("没有已加载的插件"));
        }

        var message = $"已加载的插件 ({plugins.Count}):\n";

        foreach (var plugin in plugins)
        {
            var state = plugin.State == PluginState.Enabled ? "[已启用]" : "[已禁用]";
            var author = plugin.Metadata.Author ?? "Unknown";
            message += $"  {state} {plugin.Name} v{plugin.Version} by {author}\n";
            if (!string.IsNullOrEmpty(plugin.Metadata.Description))
            {
                message += $"     {plugin.Metadata.Description}\n";
            }
        }

        return Task.FromResult(CommandResult.Ok(message));
    }
}

/// <summary>
/// Stop 命令
/// </summary>
public class StopCommand : ICommand
{
    public string Name => "stop";
    public string Description => "停止 NetherGate";
    public string Usage => "stop";
    public List<string> Aliases => new() { "exit", "quit" };
    public string PluginId => "nethergate";
    public string? Permission => "nethergate.admin.stop";

    public Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        // 这个命令由 Program.cs 中的命令循环处理
        return Task.FromResult(CommandResult.Ok("正在停止 NetherGate..."));
    }
}

/// <summary>
/// Version 命令
/// </summary>
public class VersionCommand : ICommand
{
    public string Name => "version";
    public string Description => "显示 NetherGate 版本信息";
    public string Usage => "version";
    public List<string> Aliases => new() { "ver" };
    public string PluginId => "nethergate";
    public string? Permission => null; // 无需权限

    public Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        var message = "NetherGate v0.1.0-alpha\n" +
                     $".NET 版本: {Environment.Version}\n" +
                     "项目主页: https://github.com/BlockBridge/NetherGate";

        return Task.FromResult(CommandResult.Ok(message));
    }
}

/// <summary>
/// Status 命令
/// </summary>
public class StatusCommand : ICommand
{
    private readonly ISmpApi _smpApi;
    private readonly ILogger _logger;

    public string Name => "status";
    public string Description => "显示服务器状态";
    public string Usage => "status";
    public List<string> Aliases => new() { "stat" };
    public string PluginId => "nethergate";
    public string? Permission => "nethergate.status";

    public StatusCommand(ISmpApi smpApi, ILogger logger)
    {
        _smpApi = smpApi;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        try
        {
            var status = await _smpApi.GetServerStatusAsync();
            var players = await _smpApi.GetPlayersAsync();

            var message = $"服务器状态:\n" +
                         $"  版本: {status.Version.Name} (Protocol {status.Version.Protocol})\n" +
                         $"  状态: {(status.Started ? "运行中" : "已停止")}\n" +
                         $"  在线玩家: {players.Count}\n";

            if (players.Count > 0)
            {
                message += "  玩家列表:\n";
                foreach (var player in players)
                {
                    message += $"    - {player.Name}\n";
                }
            }

            return CommandResult.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.Error("获取服务器状态失败", ex);
            return CommandResult.Fail($"获取服务器状态失败: {ex.Message}");
        }
    }
}

