using System.Text.RegularExpressions;

namespace NetherGate.API.Events;

/// <summary>
/// 日志匹配器接口，用于将日志行转为强类型事件
/// </summary>
public interface ILogMatcher
{
	/// <summary>
	/// 匹配器优先级（数字越大越先执行）
	/// </summary>
	int Priority { get; }

	/// <summary>
	/// 尝试匹配日志消息并产生事件
	/// </summary>
	/// <param name="message">日志消息（已剥离时间戳/线程/级别）</param>
	/// <param name="level">日志级别</param>
	/// <param name="thread">线程名（可空）</param>
	/// <returns>匹配成功返回事件，失败返回 null</returns>
	ServerEvent? TryMatch(string message, string level, string? thread);
}

/// <summary>
/// 基于正则的日志匹配器基类
/// </summary>
public abstract class RegexLogMatcher : ILogMatcher
{
	/// <summary>
	/// 正则表达式模式
	/// </summary>
	protected Regex Pattern { get; }
	
	/// <summary>
	/// 匹配器优先级（数字越大越先执行）
	/// </summary>
	public int Priority { get; }

	/// <summary>
	/// 构造函数
	/// </summary>
	/// <param name="pattern">正则表达式模式字符串</param>
	/// <param name="priority">优先级，默认为 0</param>
	/// <param name="options">正则表达式选项，默认为已编译</param>
	protected RegexLogMatcher(string pattern, int priority = 0, RegexOptions options = RegexOptions.Compiled)
	{
		Pattern = new Regex(pattern, options);
		Priority = priority;
	}

	/// <summary>
	/// 尝试匹配日志消息并产生事件
	/// </summary>
	/// <param name="message">日志消息</param>
	/// <param name="level">日志级别</param>
	/// <param name="thread">线程名</param>
	/// <returns>匹配成功返回事件，失败返回 null</returns>
	public ServerEvent? TryMatch(string message, string level, string? thread)
	{
		var match = Pattern.Match(message);
		return match.Success ? OnMatch(match, level, thread) : null;
	}

	/// <summary>
	/// 当正则匹配成功时调用，由子类实现以生成具体事件
	/// </summary>
	/// <param name="match">正则匹配结果</param>
	/// <param name="level">日志级别</param>
	/// <param name="thread">线程名</param>
	/// <returns>生成的服务器事件</returns>
	protected abstract ServerEvent? OnMatch(Match match, string level, string? thread);
}
