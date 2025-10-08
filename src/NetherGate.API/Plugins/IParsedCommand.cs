namespace NetherGate.API.Plugins;

/// <summary>
/// 可选接口：命令支持接收预解析的强类型参数
/// </summary>
public interface IParsedCommand
{
	/// <summary>
	/// 执行命令（参数已按命令树规则解析为强类型）
	/// </summary>
	/// <param name="sender">命令发送者</param>
	/// <param name="positionalArgs">位置参数列表</param>
	/// <param name="namedArgs">按参数名索引的参数映射</param>
	Task<CommandResult> ExecuteParsedAsync(ICommandSender sender, IReadOnlyList<object?> positionalArgs, IReadOnlyDictionary<string, object?> namedArgs);
}


