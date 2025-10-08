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

	public CommandTree(string name, string? description = null, string? permission = null)
	{
		Root = new CommandNode(name, description, permission);
	}

	/// <summary>
	/// 为 Tab 补全提供建议（不含命令名本身），按权限过滤
	/// </summary>
	public async Task<List<string>> SuggestAsync(ICommandSender sender, string[] args)
	{
		var (node, matchedCount) = TraverseToDeepestNode(Root, args);
		var remaining = args.Length - matchedCount;

		// 没有更多已匹配的 token，提示子命令
		if (remaining == 0)
		{
			return node.Children
				.Where(kv => HasPermission(sender, kv.Value))
				.Select(kv => kv.Key)
				.OrderBy(x => x)
				.ToList();
		}

		// 存在一个部分 token，尝试以前缀匹配子命令
		if (remaining == 1)
		{
			var prefix = args[^1];
			var subMatches = node.Children
				.Where(kv => HasPermission(sender, kv.Value) && kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				.Select(kv => kv.Key)
				.ToList();

			if (subMatches.Count > 0)
			{
				return subMatches.OrderBy(x => x).ToList();
			}
		}

		// 处理参数级提示
		var targetArgIndex = remaining - 1; // 当前正在输入的参数索引（基于剩余）
		if (targetArgIndex < 0) targetArgIndex = 0;

		// 定位到存在参数定义的节点
		if (node.ArgumentProviders.TryGetValue(targetArgIndex, out var provider))
		{
			var suggestions = await provider(sender, args.Skip(matchedCount).ToArray());
			var lastToken = args[^1];
			return suggestions
				.Where(s => string.IsNullOrEmpty(lastToken) || s.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
				.Distinct()
				.OrderBy(x => x)
				.ToList();
		}

		return new List<string>();
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
    Enum
}

internal sealed class CommandArgSpec
{
    public string Name { get; set; } = string.Empty;
    public CommandArgType Type { get; set; } = CommandArgType.String;
    public bool Required { get; set; } = true;
    public string? Description { get; set; }
    public List<string>? EnumValues { get; set; }
}


