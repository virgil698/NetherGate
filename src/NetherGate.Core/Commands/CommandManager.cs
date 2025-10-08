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
    private readonly List<ICommandInterceptor> _interceptors = new();
    private readonly object _lock = new();

    public CommandManager(ILogger logger, IPermissionManager? permissionManager = null)
    {
        _logger = logger;
        _permissionManager = permissionManager;
    }

    /// <summary>
    /// 注册命令拦截器
    /// </summary>
    public void RegisterInterceptor(ICommandInterceptor interceptor)
    {
        lock (_lock)
        {
            _interceptors.Add(interceptor);
            // 按优先级排序（优先级数值越小越先执行）
            _interceptors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _logger.Debug($"注册命令拦截器: {interceptor.GetType().Name} (优先级: {interceptor.Priority})");
        }
    }

    /// <summary>
    /// 注销命令拦截器
    /// </summary>
    public void UnregisterInterceptor(ICommandInterceptor interceptor)
    {
        lock (_lock)
        {
            _interceptors.Remove(interceptor);
            _logger.Debug($"注销命令拦截器: {interceptor.GetType().Name}");
        }
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

        // 检查是否为管道命令（包含 | 符号）
        if (commandLine.Contains('|'))
        {
            var pipeline = CommandPipeline.Parse(commandLine, this, _logger);
            return await pipeline.ExecuteAsync(sender ?? new ConsoleSender(_permissionManager));
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

        List<object?>? parsedArgs = null;
        Dictionary<string, object?>? namedArgs = null;

        // 如果命令提供了命令树，进行基础参数校验与友好用法提示，并尝试预解析
        if (command is IHasCommandTree hasTree)
        {
            var tree = hasTree.CommandTree;
            var (matchedTokens, node) = tree.ResolvePath(args);
            var matched = matchedTokens.Count;

            // 针对匹配到的最终节点执行权限检查（支持子命令独立权限）
            if (!string.IsNullOrEmpty(node.Permission) && !sender.HasPermission(node.Permission))
            {
                return CommandResult.Fail($"权限不足: 需要权限 '{node.Permission}'");
            }

            // 若匹配到子命令但缺少必需参数，返回用法提示
            if (matched == args.Length)
            {
                var requiredCount = node.ArgSpecs.Count(s => s.Required);
                var provided = args.Length - matched;
                if (provided < requiredCount)
                {
                    var usageLines = tree.GenerateUsageLines(command.Name);
                    if (usageLines.Count > 0)
                    {
                        var usageHelp = string.Join("\n  ", usageLines);
                        return CommandResult.Fail($"参数不足。可用用法:\n  {usageHelp}");
                    }
                }
            }

            // 类型校验与枚举校验
            var argTokens = args.Skip(matched).ToArray();
            if (argTokens.Length > node.ArgSpecs.Count)
            {
                var usageLines = tree.GenerateUsageLines(command.Name);
                var usageHelp = usageLines.Count > 0 ? string.Join("\n  ", usageLines) : command.Usage;
                return CommandResult.Fail($"参数过多。可用用法:\n  {usageHelp}");
            }

            parsedArgs = new List<object?>(capacity: Math.Max(0, node.ArgSpecs.Count));
            namedArgs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            // 记录匹配到的最后一级子命令名称，供 IParsedCommand 使用
            if (matched > 0 && matched <= args.Length)
            {
                var subName = args[matched - 1];
                if (!string.IsNullOrEmpty(subName))
                {
                    namedArgs["__subcommand"] = subName.ToLowerInvariant();
                }
                var category = matchedTokens[0];
                if (!string.IsNullOrEmpty(category))
                {
                    namedArgs["__category"] = category.ToLowerInvariant();
                }
            }

            for (int i = 0; i < argTokens.Length && i < node.ArgSpecs.Count; i++)
            {
                var tok = argTokens[i];
                var spec = node.ArgSpecs[i];

                try
                {
                    var (parsedValue, error) = ParseArgumentValue(tok, spec, i + 1);
                    if (error != null)
                    {
                        return CommandResult.Fail(error);
                    }
                    parsedArgs.Add(parsedValue);
                    namedArgs[spec.Name] = parsedValue;
                }
                catch (Exception ex)
                {
                    return CommandResult.Fail($"参数 {i + 1} 解析失败: {ex.Message}");
                }
            }
        }

        // 执行命令（带拦截器）
        return await ExecuteCommandWithInterceptorsAsync(command, sender, args, parsedArgs, namedArgs);
    }

    /// <summary>
    /// 执行命令（带拦截器支持）
    /// </summary>
    private async Task<CommandResult> ExecuteCommandWithInterceptorsAsync(
        ICommand command, 
        ICommandSender sender, 
        string[] args,
        List<object?>? parsedArgs = null,
        Dictionary<string, object?>? namedArgs = null)
    {
        var context = new Dictionary<string, object>();
        List<ICommandInterceptor> interceptors;

        lock (_lock)
        {
            interceptors = _interceptors.ToList();
        }

        try
        {
            // 执行 BeforeExecute 拦截器
            foreach (var interceptor in interceptors)
            {
                var shouldContinue = await interceptor.BeforeExecuteAsync(command, sender, args, context);
                if (!shouldContinue)
                {
                    _logger.Debug($"命令执行被拦截器 {interceptor.GetType().Name} 阻止");
                    return CommandResult.Fail("命令执行被拦截器阻止");
                }
            }

            // 执行命令
            _logger.Debug($"执行命令: {command.Name} {string.Join(" ", args)} (by {sender.Name})");
            CommandResult result;

            if (parsedArgs != null && namedArgs != null && command is IParsedCommand parsed)
            {
                result = await parsed.ExecuteParsedAsync(sender, parsedArgs, namedArgs);
            }
            else
            {
                result = await command.ExecuteAsync(sender, args);
            }

            // 执行 AfterExecute 拦截器（逆序执行）
            for (int i = interceptors.Count - 1; i >= 0; i--)
            {
                result = await interceptors[i].AfterExecuteAsync(command, sender, args, result, context);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"命令执行失败: {command.Name}", ex);

            // 执行异常拦截器
            foreach (var interceptor in interceptors)
            {
                try
                {
                    await interceptor.OnExceptionAsync(command, sender, args, ex, context);
                }
                catch (Exception interceptorEx)
                {
                    _logger.Error($"拦截器 {interceptor.GetType().Name} 处理异常时出错", interceptorEx);
                }
            }

            return CommandResult.Fail($"命令执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析参数值
    /// </summary>
    private static (object? value, string? error) ParseArgumentValue(string token, CommandArgSpec spec, int argIndex)
    {
        switch (spec.Type)
        {
            case CommandArgType.Integer:
                if (!int.TryParse(token, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intVal))
                {
                    return (null, $"参数 {argIndex} 应为整数: '{token}'");
                }
                return (intVal, null);

            case CommandArgType.Long:
                if (!long.TryParse(token, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var longVal))
                {
                    return (null, $"参数 {argIndex} 应为长整数: '{token}'");
                }
                return (longVal, null);

            case CommandArgType.Float:
                if (!double.TryParse(token, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var dblVal))
                {
                    return (null, $"参数 {argIndex} 应为数字: '{token}'");
                }
                return (dblVal, null);

            case CommandArgType.Boolean:
            {
                var t = token.ToLowerInvariant();
                var ok = t is "true" or "false" or "on" or "off" or "yes" or "no" or "1" or "0";
                if (!ok)
                {
                    return (null, $"参数 {argIndex} 应为布尔值(true/false/on/off/yes/no/1/0): '{token}'");
                }
                var boolVal = t is "true" or "on" or "yes" or "1";
                return (boolVal, null);
            }

            case CommandArgType.Enum:
                if (spec.EnumValues is { Count: > 0 })
                {
                    var match = spec.EnumValues.FirstOrDefault(v => v.Equals(token, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        return (null, $"参数 {argIndex} 可选值: {string.Join(", ", spec.EnumValues)} (收到: '{token}')");
                    }
                    return (match, null);
                }
                return (token, null);

            case CommandArgType.Uuid:
                if (!Guid.TryParse(token, out var guidVal))
                {
                    return (null, $"参数 {argIndex} 应为有效的 UUID: '{token}'");
                }
                return (guidVal, null);

            case CommandArgType.TimeSpan:
            {
                var timeSpan = ParseTimeSpanString(token);
                if (timeSpan == null)
                {
                    return (null, $"参数 {argIndex} 应为有效的时间间隔 (如 '1h30m', '5d', '30s'): '{token}'");
                }
                return (timeSpan.Value, null);
            }

            case CommandArgType.FilePath:
                // 简单验证路径格式
                if (string.IsNullOrWhiteSpace(token) || token.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    return (null, $"参数 {argIndex} 应为有效的文件路径: '{token}'");
                }
                return (token, null);

            case CommandArgType.Regex:
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(token);
                    return (regex, null);
                }
                catch (ArgumentException ex)
                {
                    return (null, $"参数 {argIndex} 应为有效的正则表达式: {ex.Message}");
                }

            case CommandArgType.JsonObject:
                try
                {
                    var json = Newtonsoft.Json.Linq.JToken.Parse(token);
                    return (json, null);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    return (null, $"参数 {argIndex} 应为有效的 JSON: {ex.Message}");
                }

            case CommandArgType.Url:
                if (!Uri.TryCreate(token, UriKind.Absolute, out var uri))
                {
                    return (null, $"参数 {argIndex} 应为有效的 URL: '{token}'");
                }
                return (uri, null);

            case CommandArgType.IpAddress:
                if (!System.Net.IPAddress.TryParse(token, out var ipAddress))
                {
                    return (null, $"参数 {argIndex} 应为有效的 IP 地址: '{token}'");
                }
                return (ipAddress, null);

            case CommandArgType.String:
            default:
                return (token, null);
        }
    }

    /// <summary>
    /// 解析时间间隔字符串（如 "1h30m", "5d", "30s"）
    /// </summary>
    private static TimeSpan? ParseTimeSpanString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var regex = new System.Text.RegularExpressions.Regex(@"(\d+)([dhms])");
        var matches = regex.Matches(input.ToLower());
        
        if (matches.Count == 0) return null;

        var totalSeconds = 0.0;
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (!double.TryParse(match.Groups[1].Value, out var value)) continue;
            
            var unit = match.Groups[2].Value;
            totalSeconds += unit switch
            {
                "d" => value * 86400,   // days
                "h" => value * 3600,    // hours
                "m" => value * 60,      // minutes
                "s" => value,           // seconds
                _ => 0
            };
        }

        return totalSeconds > 0 ? TimeSpan.FromSeconds(totalSeconds) : null;
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

            var args = parts.Skip(1).ToArray();

            // 如果命令提供了结构化命令树，优先使用命令树进行建议
            if (targetCommand is IHasCommandTree treeProvider)
            {
                try
                {
                    var treeResults = await treeProvider.CommandTree.SuggestAsync(sender, args);
                    results.AddRange(treeResults);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"命令 '{commandName}' 的命令树补全失败: {ex.Message}");
                }
            }
            else
            {
                // 回退到命令自身的补全方法
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
            return _permissionManager.HasPermissionAsync("Console", permission).GetAwaiter().GetResult();
        }
        return true;
    }
}

