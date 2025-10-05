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
            message += "\n注意: 游戏内执行命令需要加 # 前缀（例如: #help）";
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
/// Plugin 命令 - 管理插件
/// </summary>
public class PluginCommand : ICommand
{
    private readonly PluginManager _pluginManager;
    private readonly ILogger _logger;

    public string Name => "plugin";
    public string Description => "管理插件（list/reload/enable/disable/info）";
    public string Usage => "plugin <list|reload|enable|disable|info> [插件ID]";
    public List<string> Aliases => new() { "pl" };
    public string PluginId => "nethergate";
    public string? Permission => "nethergate.plugins.manage";

    public PluginCommand(PluginManager pluginManager, ILogger logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;
    }

    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            // 补全子命令
            var subcommands = new[] { "list", "reload", "enable", "disable", "info", "load", "unload" };
            var prefix = args[0].ToLower();
            return subcommands.Where(s => s.StartsWith(prefix)).ToList();
        }

        if (args.Length == 2)
        {
            var subcommand = args[0].ToLower();
            if (subcommand is "reload" or "enable" or "disable" or "info" or "unload")
            {
                // 补全插件 ID
                var plugins = _pluginManager.GetAllPluginContainers();
                var prefix = args[1].ToLower();
                
                var suggestions = plugins
                    .Select(p => p.Metadata.Id)
                    .Where(id => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // 对于 reload，添加 "all" 选项
                if (subcommand == "reload" && "all".StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Insert(0, "all");
                }

                return suggestions;
            }
        }

        return await Task.FromResult(new List<string>());
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Fail($"用法: {Usage}\n子命令:\n" +
                "  list           - 显示所有插件\n" +
                "  reload <id>    - 重载指定插件\n" +
                "  reload all     - 重载所有插件\n" +
                "  enable <id>    - 启用插件\n" +
                "  disable <id>   - 禁用插件\n" +
                "  info <id>      - 查看插件详情\n" +
                "  load <id>      - 加载新插件\n" +
                "  unload <id>    - 卸载插件");
        }

        var subcommand = args[0].ToLower();

        return subcommand switch
        {
            "list" or "ls" => await ListPluginsAsync(),
            "reload" or "rl" => await ReloadPluginAsync(args),
            "enable" => await EnablePluginAsync(args),
            "disable" => await DisablePluginAsync(args),
            "info" => await InfoPluginAsync(args),
            "load" => await LoadPluginAsync(args),
            "unload" => await UnloadPluginAsync(args),
            _ => CommandResult.Fail($"未知子命令: {subcommand}\n使用 'plugin' 查看帮助")
        };
    }

    private Task<CommandResult> ListPluginsAsync()
    {
        var plugins = _pluginManager.GetAllPluginContainers();

        if (plugins.Count == 0)
        {
            return Task.FromResult(CommandResult.Ok("没有已加载的插件"));
        }

        var message = $"已加载的插件 ({plugins.Count}):\n";

        foreach (var plugin in plugins.OrderBy(p => p.Metadata.Id))
        {
            var stateColor = plugin.State switch
            {
                PluginState.Enabled => "✓",
                PluginState.Disabled => "✗",
                PluginState.Error => "!",
                _ => "?"
            };

            var author = plugin.Metadata.Author ?? "Unknown";
            message += $"  {stateColor} {plugin.Metadata.Id} v{plugin.Version} by {author}\n";
        }

        message += "\n使用 'plugin info <id>' 查看详情";
        return Task.FromResult(CommandResult.Ok(message));
    }

    private async Task<CommandResult> ReloadPluginAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Fail("用法: plugin reload <插件ID> 或 plugin reload all");
        }

        var pluginId = args[1].ToLower();

        // 重载所有插件
        if (pluginId == "all")
        {
            _logger.Info("开始重载所有插件...");
            var plugins = _pluginManager.GetAllPluginContainers()
                .Where(p => p.State == PluginState.Enabled)
                .ToList();

            var successCount = 0;
            var failCount = 0;

            foreach (var plugin in plugins)
            {
                try
                {
                    _logger.Info($"重载插件: {plugin.Metadata.Id}");
                    await _pluginManager.ReloadPluginAsync(plugin.Metadata.Id);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.Error($"重载插件 {plugin.Metadata.Id} 失败", ex);
                    failCount++;
                }
            }

            return CommandResult.Ok($"插件重载完成: 成功 {successCount} 个，失败 {failCount} 个");
        }

        // 重载单个插件
        var targetPlugin = _pluginManager.GetAllPluginContainers()
            .FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));

        if (targetPlugin == null)
        {
            return CommandResult.Fail($"未找到插件: {pluginId}");
        }

        try
        {
            _logger.Info($"重载插件: {targetPlugin.Metadata.Id}");
            await _pluginManager.ReloadPluginAsync(targetPlugin.Metadata.Id);
            return CommandResult.Ok($"插件 {targetPlugin.Metadata.Id} 重载成功");
        }
        catch (Exception ex)
        {
            _logger.Error($"重载插件失败", ex);
            return CommandResult.Fail($"重载插件失败: {ex.Message}");
        }
    }

    private async Task<CommandResult> EnablePluginAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Fail("用法: plugin enable <插件ID>");
        }

        var pluginId = args[1];
        var plugin = _pluginManager.GetAllPluginContainers()
            .FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));

        if (plugin == null)
        {
            return CommandResult.Fail($"未找到插件: {pluginId}");
        }

        if (plugin.State == PluginState.Enabled)
        {
            return CommandResult.Ok($"插件 {plugin.Metadata.Id} 已经是启用状态");
        }

        try
        {
            await _pluginManager.EnablePluginAsync(plugin.Metadata.Id);
            return CommandResult.Ok($"插件 {plugin.Metadata.Id} 已启用");
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"启用插件失败: {ex.Message}");
        }
    }

    private async Task<CommandResult> DisablePluginAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Fail("用法: plugin disable <插件ID>");
        }

        var pluginId = args[1];
        var plugin = _pluginManager.GetAllPluginContainers()
            .FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));

        if (plugin == null)
        {
            return CommandResult.Fail($"未找到插件: {pluginId}");
        }

        if (plugin.State == PluginState.Disabled)
        {
            return CommandResult.Ok($"插件 {plugin.Metadata.Id} 已经是禁用状态");
        }

        try
        {
            await _pluginManager.DisablePluginAsync(plugin.Metadata.Id);
            return CommandResult.Ok($"插件 {plugin.Metadata.Id} 已禁用");
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"禁用插件失败: {ex.Message}");
        }
    }

    private Task<CommandResult> InfoPluginAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return Task.FromResult(CommandResult.Fail("用法: plugin info <插件ID>"));
        }

        var pluginId = args[1];
        var plugin = _pluginManager.GetAllPluginContainers()
            .FirstOrDefault(p => p.Metadata.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));

        if (plugin == null)
        {
            return Task.FromResult(CommandResult.Fail($"未找到插件: {pluginId}"));
        }

        var meta = plugin.Metadata;
        var message = $"插件信息:\n" +
                     $"  ID: {meta.Id}\n" +
                     $"  名称: {meta.Name}\n" +
                     $"  版本: {meta.Version}\n" +
                     $"  作者: {meta.Author ?? "未知"}\n" +
                     $"  描述: {meta.Description ?? "无"}\n" +
                     $"  状态: {GetStateText(plugin.State)}\n";

        if (meta.Dependencies?.Count > 0)
        {
            message += $"  NuGet依赖:\n";
            foreach (var dep in meta.Dependencies)
            {
                message += $"    - {dep}\n";
            }
        }

        if (meta.LibraryDependencies?.Count > 0)
        {
            message += $"  库依赖:\n";
            foreach (var dep in meta.LibraryDependencies)
            {
                message += $"    - {dep.Name} {dep.Version}\n";
            }
        }

        if (meta.PluginDependencies?.Count > 0)
        {
            message += $"  插件依赖:\n";
            foreach (var dep in meta.PluginDependencies)
            {
                var optional = dep.Optional ? " (可选)" : "";
                message += $"    - {dep.Id} {dep.Version}{optional}\n";
            }
        }

        if (!string.IsNullOrEmpty(meta.Website))
        {
            message += $"  网站: {meta.Website}\n";
        }

        return Task.FromResult(CommandResult.Ok(message));
    }

    private Task<CommandResult> LoadPluginAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return Task.FromResult(CommandResult.Fail("用法: plugin load <插件ID>"));
        }

        // 这个功能需要扫描 plugins 目录并加载新插件
        return Task.FromResult(CommandResult.Fail("load 功能暂未实现，请重启 NetherGate 以加载新插件"));
    }

    private Task<CommandResult> UnloadPluginAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return Task.FromResult(CommandResult.Fail("用法: plugin unload <插件ID>"));
        }

        var pluginId = args[1];
        return Task.FromResult(CommandResult.Fail($"unload 功能暂未实现，请使用 'plugin disable {pluginId}' 代替"));
    }

    private static string GetStateText(PluginState state) => state switch
    {
        PluginState.Loaded => "已加载",
        PluginState.Enabled => "已启用",
        PluginState.Disabled => "已禁用",
        PluginState.Error => "错误",
        _ => "未知"
    };
}

/// <summary>
/// Plugins 命令 - 快捷方式（兼容旧命令）
/// </summary>
public class PluginsCommand : ICommand
{
    private readonly PluginCommand _pluginCommand;

    public string Name => "plugins";
    public string Description => "显示已加载的插件列表（快捷方式）";
    public string Usage => "plugins";
    public List<string> Aliases => new();
    public string PluginId => "nethergate";
    public string? Permission => "nethergate.plugins.manage";

    public PluginsCommand(PluginCommand pluginCommand)
    {
        _pluginCommand = pluginCommand;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        // 转发到 plugin list
        return await _pluginCommand.ExecuteAsync(sender, new[] { "list" });
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

