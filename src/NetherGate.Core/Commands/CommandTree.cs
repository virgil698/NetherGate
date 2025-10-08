using NetherGate.API.Plugins;

namespace NetherGate.Core.Commands;

/// <summary>
/// 标注命令实现携带结构化命令树，用于通用的帮助与补全
/// </summary>
internal interface IHasCommandTree
{
	CommandTree CommandTree { get; }
}

/// <summary>
/// 命令树，包含子命令与参数提示
/// </summary>
internal class CommandTree
{
	public CommandNode Root { get; }
    private readonly Dictionary<string, List<string>> _usageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (List<string> suggestions, DateTime cachedAt)> _completionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);

	public CommandTree(string name, string? description = null, string? permission = null)
	{
		Root = new CommandNode(name, description, permission);
	}

	/// <summary>
	/// 为 Tab 补全提供建议（不含命令名本身），按权限过滤，带缓存
	/// </summary>
	public async Task<List<string>> SuggestAsync(ICommandSender sender, string[] args)
	{
		// 生成缓存键
		var cacheKey = $"{sender.Name}:{string.Join(" ", args)}";
		
		// 检查缓存
		if (_completionCache.TryGetValue(cacheKey, out var cached))
		{
			if (DateTime.UtcNow - cached.cachedAt < _cacheExpiration)
			{
				return cached.suggestions;
			}
			// 过期则删除
			_completionCache.Remove(cacheKey);
		}

		var (node, matchedCount) = TraverseToDeepestNode(Root, args);
		var remaining = args.Length - matchedCount;

		List<string> suggestions;

		// 没有更多已匹配的 token，提示子命令
		if (remaining == 0)
		{
			suggestions = node.Children
				.Where(kv => HasPermission(sender, kv.Value))
				.Select(kv => kv.Key)
				.OrderBy(x => x)
				.ToList();
		}
		// 存在一个部分 token，尝试以前缀匹配子命令
		else if (remaining == 1)
		{
			var prefix = args[^1];
			var subMatches = node.Children
				.Where(kv => HasPermission(sender, kv.Value) && kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				.Select(kv => kv.Key)
				.ToList();

			if (subMatches.Count > 0)
			{
				suggestions = subMatches.OrderBy(x => x).ToList();
			}
			else
			{
				// 处理参数级提示
				var targetArgIndex = remaining - 1;
				if (targetArgIndex < 0) targetArgIndex = 0;

				if (node.ArgumentProviders.TryGetValue(targetArgIndex, out var provider))
				{
					var providerSuggestions = await provider(sender, args.Skip(matchedCount).ToArray());
					var lastToken = args[^1];
					suggestions = providerSuggestions
						.Where(s => string.IsNullOrEmpty(lastToken) || s.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
						.Distinct()
						.OrderBy(x => x)
						.ToList();
				}
				else
				{
					suggestions = new List<string>();
				}
			}
		}
		else
		{
			// 处理参数级提示
			var targetArgIndex = remaining - 1;
			if (targetArgIndex < 0) targetArgIndex = 0;

			if (node.ArgumentProviders.TryGetValue(targetArgIndex, out var provider))
			{
				var providerSuggestions = await provider(sender, args.Skip(matchedCount).ToArray());
				var lastToken = args[^1];
				suggestions = providerSuggestions
					.Where(s => string.IsNullOrEmpty(lastToken) || s.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
					.Distinct()
					.OrderBy(x => x)
					.ToList();
			}
			else
			{
				suggestions = new List<string>();
			}
		}

		// 缓存结果（限制缓存大小）
		if (_completionCache.Count > 1000)
		{
			// 清理过期缓存
			var expired = _completionCache
				.Where(kv => DateTime.UtcNow - kv.Value.cachedAt >= _cacheExpiration)
				.Select(kv => kv.Key)
				.ToList();
			foreach (var key in expired)
			{
				_completionCache.Remove(key);
			}
		}

		_completionCache[cacheKey] = (suggestions, DateTime.UtcNow);
		return suggestions;
	}

	/// <summary>
	/// 解析参数，返回匹配到的最深节点与已匹配的子命令数量
	/// </summary>
	public (CommandNode node, int matchedCount) ResolveNode(string[] args)
	{
		return TraverseToDeepestNode(Root, args);
	}

	/// <summary>
	/// 解析子命令路径，返回已匹配的子命令令牌列表与最终节点
	/// </summary>
	public (List<string> matchedTokens, CommandNode node) ResolvePath(string[] args)
	{
		var node = Root;
		var matchedTokens = new List<string>();
		var idx = 0;
		while (idx < args.Length)
		{
			var token = args[idx].ToLower();
			if (node.Children.TryGetValue(token, out var child))
			{
				node = child;
				matchedTokens.Add(token);
				idx++;
			}
			else
			{
				break;
			}
		}
		return (matchedTokens, node);
	}

    /// <summary>
    /// 生成用法行（仅展示 Root 的直系子命令），含参数占位
    /// </summary>
    public List<string> GenerateUsageLines(string rootCommandName)
    {
        if (_usageCache.TryGetValue(rootCommandName, out var cached))
        {
            return cached;
        }

        var usages = new List<string>();
        foreach (var (name, child) in Root.Children.OrderBy(kv => kv.Key))
        {
            var argText = CommandNode.FormatArgs(child.ArgSpecs);
            usages.Add(string.IsNullOrEmpty(argText)
                ? $"{rootCommandName} {name}"
                : $"{rootCommandName} {name} {argText}");
        }
        // 如果 Root 自身定义了参数（无子命令的情况），也输出一行
        if (Root.Children.Count == 0 && Root.ArgSpecs.Count > 0)
        {
            var argText = CommandNode.FormatArgs(Root.ArgSpecs);
            usages.Add($"{rootCommandName} {argText}");
        }
        _usageCache[rootCommandName] = usages;
        return usages;
    }

    /// <summary>
    /// 生成格式化的帮助信息（包含描述和示例）
    /// </summary>
    public string GenerateFormattedHelp(string rootCommandName, bool useColors = true)
    {
        var help = new System.Text.StringBuilder();
        
        // 标题
        var titleColor = useColors ? "§6" : "";
        var resetColor = useColors ? "§f" : "";
        var descColor = useColors ? "§7" : "";
        var exampleColor = useColors ? "§a" : "";
        var argColor = useColors ? "§e" : "";
        
        help.AppendLine($"{titleColor}=== {rootCommandName} 命令帮助 ==={resetColor}");
        
        if (!string.IsNullOrEmpty(Root.Description))
        {
            help.AppendLine($"{descColor}{Root.Description}{resetColor}");
        }
        
        help.AppendLine();
        help.AppendLine($"{titleColor}用法:{resetColor}");
        
        // 生成所有用法
        if (Root.Children.Count > 0)
        {
            foreach (var (name, child) in Root.Children.OrderBy(kv => kv.Key))
            {
                var argText = CommandNode.FormatArgs(child.ArgSpecs);
                var usage = string.IsNullOrEmpty(argText)
                    ? $"  {argColor}{rootCommandName} {name}{resetColor}"
                    : $"  {argColor}{rootCommandName} {name} {argText}{resetColor}";
                
                help.Append(usage);
                
                if (!string.IsNullOrEmpty(child.Description))
                {
                    help.Append($" {descColor}- {child.Description}{resetColor}");
                }
                help.AppendLine();
                
                // 显示参数说明
                if (child.ArgSpecs.Count > 0)
                {
                    foreach (var spec in child.ArgSpecs)
                    {
                        if (!string.IsNullOrEmpty(spec.Description))
                        {
                            help.AppendLine($"      {descColor}{spec.Name}: {spec.Description}{resetColor}");
                        }
                    }
                }
            }
        }
        else if (Root.ArgSpecs.Count > 0)
        {
            var argText = CommandNode.FormatArgs(Root.ArgSpecs);
            help.AppendLine($"  {argColor}{rootCommandName} {argText}{resetColor}");
            
            // 显示参数说明
            foreach (var spec in Root.ArgSpecs)
            {
                if (!string.IsNullOrEmpty(spec.Description))
                {
                    help.AppendLine($"    {descColor}{spec.Name}: {spec.Description}{resetColor}");
                }
            }
        }
        
        // 显示示例
        if (Root.Examples != null && Root.Examples.Count > 0)
        {
            help.AppendLine();
            help.AppendLine($"{titleColor}示例:{resetColor}");
            foreach (var example in Root.Examples)
            {
                help.AppendLine($"  {exampleColor}{example}{resetColor}");
            }
        }
        
        return help.ToString().TrimEnd();
    }

    /// <summary>
    /// 生成简洁的命令提示（单行）
    /// </summary>
    public string GenerateQuickHelp(string rootCommandName)
    {
        var usages = GenerateUsageLines(rootCommandName);
        if (usages.Count == 0) return $"用法: {rootCommandName}";
        if (usages.Count == 1) return $"用法: {usages[0]}";
        return $"用法: {usages[0]} (还有 {usages.Count - 1} 种用法，使用 'help {rootCommandName}' 查看详细信息)";
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        _usageCache.Clear();
        _completionCache.Clear();
    }

	private static (CommandNode node, int matchedCount) TraverseToDeepestNode(CommandNode start, string[] args)
	{
		var node = start;
		var idx = 0;
		while (idx < args.Length)
		{
			var token = args[idx].ToLower();
			if (node.Children.TryGetValue(token, out var child))
			{
				node = child;
				idx++;
			}
			else
			{
				break;
			}
		}
		return (node, idx);
	}

	private static bool HasPermission(ICommandSender sender, CommandNode node)
	{
		return string.IsNullOrEmpty(node.Permission) || sender.HasPermission(node.Permission!);
	}
}

internal class CommandNode
{
	public string Name { get; }
	public string? Description { get; }
	public string? Permission { get; }
	public List<string>? Examples { get; set; }

	public Dictionary<string, CommandNode> Children { get; } = new();
	public Dictionary<int, Func<ICommandSender, string[], Task<IEnumerable<string>>>> ArgumentProviders { get; } = new();
    public List<CommandArgSpec> ArgSpecs { get; } = new();

	public CommandNode(string name, string? description = null, string? permission = null)
	{
		Name = name;
		Description = description;
		Permission = permission;
	}

	public CommandNode Sub(string name, string? description = null, string? permission = null)
	{
		var node = new CommandNode(name.ToLower(), description, permission ?? Permission);
		Children[name.ToLower()] = node;
		return node;
	}

	public CommandNode Arg(int index, Func<ICommandSender, string[], Task<IEnumerable<string>>> suggestionProvider)
	{
		ArgumentProviders[index] = suggestionProvider;
		return this;
	}

    public CommandNode ArgSpec(string name, CommandArgType type = CommandArgType.String, bool required = true, string? description = null, IEnumerable<string>? enumValues = null)
    {
        ArgSpecs.Add(new CommandArgSpec
        {
            Name = name,
            Type = type,
            Required = required,
            Description = description,
            EnumValues = enumValues?.ToList()
        });
        return this;
    }

    public CommandNode WithExamples(params string[] examples)
    {
        Examples = examples.ToList();
        return this;
    }

    public static string FormatArgs(IReadOnlyList<CommandArgSpec> specs)
    {
        if (specs.Count == 0) return string.Empty;
        var parts = new List<string>();
        foreach (var spec in specs)
        {
            var core = spec.Type == CommandArgType.Enum && spec.EnumValues is { Count: >0 }
                ? string.Join("|", spec.EnumValues)
                : spec.Name;
            parts.Add(spec.Required ? $"<{core}>" : $"[{core}]");
        }
        return string.Join(" ", parts);
    }
}

internal enum CommandArgType
{
    String,
    Integer,
    Float,
    Boolean,
    Enum,
    Uuid,           // UUID 参数
    TimeSpan,       // 时间间隔（如 "1h30m", "5d", "30s"）
    FilePath,       // 文件路径
    Regex,          // 正则表达式
    JsonObject,     // JSON 对象
    Url,            // URL 地址
    IpAddress,      // IP 地址
    Long            // 长整型
}

internal sealed class CommandArgSpec
{
    public string Name { get; set; } = string.Empty;
    public CommandArgType Type { get; set; } = CommandArgType.String;
    public bool Required { get; set; } = true;
    public string? Description { get; set; }
    public List<string>? EnumValues { get; set; }
}


