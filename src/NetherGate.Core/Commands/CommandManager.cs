using NetherGate.API.Logging;
using NetherGate.API.Permissions;
using NetherGate.API.Plugins;

namespace NetherGate.Core.Commands;

/// <summary>
/// 命令管理器实现
/// </summary>
public class CommandManager : ICommandManager
{
    private readonly ILogger _logger;
    private readonly IPermissionManager? _permissionManager;
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly Dictionary<string, string> _aliases = new(); // alias -> commandName
    private readonly object _lock = new();

    public CommandManager(ILogger logger, IPermissionManager? permissionManager = null)
    {
        _logger = logger;
        _permissionManager = permissionManager;
    }

    /// <summary>
    /// 注册命令
    /// </summary>
    public void RegisterCommand(ICommand command)
    {
        lock (_lock)
        {
            // 检查命令名称是否已存在
            if (_commands.ContainsKey(command.Name.ToLower()))
            {
                _logger.Warning($"命令 '{command.Name}' 已存在，将被覆盖");
            }

            // 注册命令
            _commands[command.Name.ToLower()] = command;
            _logger.Debug($"注册命令: {command.Name} (插件: {command.PluginId})");

            // 注册别名
            foreach (var alias in command.Aliases)
            {
                if (_aliases.ContainsKey(alias.ToLower()))
                {
                    _logger.Warning($"别名 '{alias}' 已存在，将被覆盖");
                }

                _aliases[alias.ToLower()] = command.Name.ToLower();
                _logger.Trace($"  别名: {alias} -> {command.Name}");
            }
        }
    }

    /// <summary>
    /// 注销命令
    /// </summary>
    public void UnregisterCommand(string commandName)
    {
        lock (_lock)
        {
            var lowerName = commandName.ToLower();

            if (!_commands.TryGetValue(lowerName, out var command))
            {
                _logger.Warning($"命令不存在: {commandName}");
                return;
            }

            // 移除命令
            _commands.Remove(lowerName);
            _logger.Debug($"注销命令: {commandName}");

            // 移除别名
            var aliasesToRemove = _aliases
                .Where(kvp => kvp.Value == lowerName)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var alias in aliasesToRemove)
            {
                _aliases.Remove(alias);
            }
        }
    }

    /// <summary>
    /// 注销插件的所有命令
    /// </summary>
    public void UnregisterPluginCommands(string pluginId)
    {
        lock (_lock)
        {
            var pluginCommands = _commands.Values
                .Where(c => c.PluginId == pluginId)
                .ToList();

            foreach (var command in pluginCommands)
            {
                UnregisterCommand(command.Name);
            }

            if (pluginCommands.Count > 0)
            {
                _logger.Info($"注销插件 '{pluginId}' 的 {pluginCommands.Count} 个命令");
            }
        }
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(string commandLine, ICommandSender? sender = null)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return CommandResult.Fail("命令不能为空");
        }

        // 处理 # 前缀（框架命令前缀）
        // 支持在控制台直接输入（不带前缀）或在游戏内输入（带 # 前缀）
        commandLine = commandLine.TrimStart();
        if (commandLine.StartsWith("#"))
        {
            commandLine = commandLine.Substring(1).TrimStart();
        }

        // 如果是游戏原生命令（以 / 开头），不处理
        if (commandLine.StartsWith("/"))
        {
            return CommandResult.Fail("游戏原生命令请直接在游戏内执行，NetherGate 命令请使用 # 前缀");
        }

        // 解析命令行
        var parts = ParseCommandLine(commandLine);
        if (parts.Length == 0)
        {
            return CommandResult.Fail("命令不能为空");
        }

        var commandName = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();

        // 查找命令
        ICommand? command;
        lock (_lock)
        {
            // 先查找命令名
            if (!_commands.TryGetValue(commandName, out command))
            {
                // 再查找别名
                if (_aliases.TryGetValue(commandName, out var actualName))
                {
                    _commands.TryGetValue(actualName, out command);
                }
            }
        }

        if (command == null)
        {
            return CommandResult.Fail($"未知命令: {commandName}");
        }

        // 默认使用控制台发送者
        sender ??= new ConsoleSender(_permissionManager);

        // 权限检查
        if (!string.IsNullOrEmpty(command.Permission) && !sender.HasPermission(command.Permission))
        {
            _logger.Warning($"用户 '{sender.Name}' 尝试执行命令 '{commandName}' 但权限不足 (需要: {command.Permission})");
            return CommandResult.Fail($"权限不足: 需要权限 '{command.Permission}'");
        }

        // 执行命令
        try
        {
            _logger.Debug($"执行命令: {commandName} {string.Join(" ", args)} (by {sender.Name})");
            return await command.ExecuteAsync(sender, args);
        }
        catch (Exception ex)
        {
            _logger.Error($"命令执行失败: {commandName}", ex);
            return CommandResult.Fail($"命令执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析命令行（支持引号）
    /// </summary>
    private string[] ParseCommandLine(string commandLine)
    {
        var parts = new List<string>();
        var currentPart = "";
        var inQuotes = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (!string.IsNullOrEmpty(currentPart))
                {
                    parts.Add(currentPart);
                    currentPart = "";
                }
            }
            else
            {
                currentPart += c;
            }
        }

        if (!string.IsNullOrEmpty(currentPart))
        {
            parts.Add(currentPart);
        }

        return parts.ToArray();
    }

    /// <summary>
    /// 获取所有命令
    /// </summary>
    public IReadOnlyDictionary<string, ICommand> GetAllCommands()
    {
        lock (_lock)
        {
            return new Dictionary<string, ICommand>(_commands);
        }
    }

    /// <summary>
    /// 获取命令数量
    /// </summary>
    public int GetCommandCount()
    {
        lock (_lock)
        {
            return _commands.Count;
        }
    }

    /// <summary>
    /// Tab 补全
    /// </summary>
    public async Task<List<string>> TabCompleteAsync(string partialCommandLine, ICommandSender? sender = null)
    {
        sender ??= new ConsoleSender(_permissionManager);

        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(partialCommandLine))
        {
            // 返回所有有权限的命令
            lock (_lock)
            {
                foreach (var command in _commands.Values)
                {
                    if (string.IsNullOrEmpty(command.Permission) || sender.HasPermission(command.Permission))
                    {
                        results.Add(command.Name);
                    }
                }
            }
            return results;
        }

        // 处理 # 前缀
        var hasPrefix = partialCommandLine.TrimStart().StartsWith("#");
        if (hasPrefix)
        {
            partialCommandLine = partialCommandLine.TrimStart().Substring(1).TrimStart();
        }

        var parts = ParseCommandLine(partialCommandLine);
        
        if (parts.Length == 0)
            return results;

        // 如果只有一个部分且没有空格结尾，补全命令名
        if (parts.Length == 1 && !partialCommandLine.EndsWith(" "))
        {
            var prefix = parts[0].ToLower();
            lock (_lock)
            {
                // 匹配命令名
                foreach (var command in _commands.Values)
                {
                    if (command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(command.Permission) || sender.HasPermission(command.Permission))
                        {
                            results.Add(command.Name);
                        }
                    }
                }

                // 匹配别名
                foreach (var (alias, actualCommandName) in _aliases)
                {
                    if (alias.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_commands.TryGetValue(actualCommandName, out var command))
                        {
                            if (string.IsNullOrEmpty(command.Permission) || sender.HasPermission(command.Permission))
                            {
                                results.Add(alias);
                            }
                        }
                    }
                }
            }

            return results.Distinct().OrderBy(x => x).ToList();
        }

        // 补全命令参数
        var commandName = parts[0].ToLower();
        ICommand? targetCommand = null;

        lock (_lock)
        {
            if (!_commands.TryGetValue(commandName, out targetCommand))
            {
                if (_aliases.TryGetValue(commandName, out var actualName))
                {
                    _commands.TryGetValue(actualName, out targetCommand);
                }
            }
        }

        if (targetCommand != null)
        {
            // 权限检查
            if (!string.IsNullOrEmpty(targetCommand.Permission) && !sender.HasPermission(targetCommand.Permission))
            {
                return results;
            }

            // 调用命令的 TabComplete 方法
            var args = parts.Skip(1).ToArray();
            try
            {
                var commandResults = await targetCommand.TabCompleteAsync(sender, args);
                results.AddRange(commandResults);
            }
            catch (Exception ex)
            {
                _logger.Debug($"命令 '{commandName}' 的 Tab 补全失败: {ex.Message}");
            }
        }

        return results.Distinct().OrderBy(x => x).ToList();
    }
}

/// <summary>
/// 控制台命令发送者
/// </summary>
public class ConsoleSender : ICommandSender
{
    private readonly IPermissionManager? _permissionManager;

    public ConsoleSender(IPermissionManager? permissionManager = null)
    {
        _permissionManager = permissionManager;
    }

    public string Name => "Console";
    public bool IsConsole => true;

    public void SendMessage(string message)
    {
        Console.WriteLine(message);
    }

    public bool HasPermission(string permission)
    {
        // 控制台拥有所有权限
        if (_permissionManager != null)
        {
            return _permissionManager.HasPermission("Console", permission);
        }
        return true;
    }
}

