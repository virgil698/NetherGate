using System.Globalization;
using System.Text.RegularExpressions;
using NetherGate.API.Events;

namespace NetherGate.Core.Process;

/// <summary>
/// 服务器启动完成匹配器
/// </summary>
internal class ServerReadyMatcher : RegexLogMatcher
{
	public ServerReadyMatcher() : base(@"^Done \(([\d.]+)s\)!?", priority: 100, RegexOptions.Compiled | RegexOptions.IgnoreCase) { }

	protected override ServerEvent? OnMatch(Match match, string level, string? thread)
	{
		var startupTime = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
		return new ServerReadyEvent { StartupTimeSeconds = startupTime };
	}
}

/// <summary>
/// 服务器关闭匹配器
/// </summary>
internal class ServerStoppingMatcher : RegexLogMatcher
{
	public ServerStoppingMatcher() : base(@"^Stopping server$", priority: 100) { }

	protected override ServerEvent? OnMatch(Match match, string level, string? thread)
	{
		return new ServerShuttingDownEvent();
	}
}

/// <summary>
/// 玩家加入匹配器
/// </summary>
internal class PlayerJoinMatcher : RegexLogMatcher
{
	// [10:23:45] [Server thread/INFO]: Steve[/127.0.0.1:12345] logged in with entity id 123 at ([world]1.5, 64.0, 2.5)
	public PlayerJoinMatcher() : base(@"^([^\[]+)\[\/([^\]]+)\] logged in with entity id (\d+)", priority: 50) { }

	protected override ServerEvent? OnMatch(Match match, string level, string? thread)
	{
		return new PlayerJoinedServerEvent
		{
			PlayerName = match.Groups[1].Value.Trim()
		};
	}
}

/// <summary>
/// 玩家离开匹配器
/// </summary>
internal class PlayerLeaveMatcher : RegexLogMatcher
{
	// [10:25:30] [Server thread/INFO]: Steve lost connection: Disconnected
	public PlayerLeaveMatcher() : base(@"^([^\s]+) lost connection: (.+)$", priority: 50) { }

	protected override ServerEvent? OnMatch(Match match, string? level, string? thread)
	{
		return new PlayerLeftServerEvent
		{
			PlayerName = match.Groups[1].Value.Trim()
		};
	}
}

/// <summary>
/// 玩家聊天匹配器
/// </summary>
internal class PlayerChatMatcher : RegexLogMatcher
{
	public PlayerChatMatcher() : base(@"^<(\w+)> (.+)$", priority: 50) { }

	protected override ServerEvent? OnMatch(Match match, string level, string? thread)
	{
		return new PlayerChatEvent
		{
			PlayerName = match.Groups[1].Value,
			Message = match.Groups[2].Value
		};
	}
}

/// <summary>
/// 玩家命令匹配器
/// </summary>
internal class PlayerCommandMatcher : RegexLogMatcher
{
	public PlayerCommandMatcher() : base(@"^(\w+) issued server command: (.+)$", priority: 50) { }

	protected override ServerEvent? OnMatch(Match match, string level, string? thread)
	{
		return new PlayerCommandEvent
		{
			PlayerName = match.Groups[1].Value,
			Command = match.Groups[2].Value
		};
	}
}

/// <summary>
/// 玩家成就匹配器
/// </summary>
internal class PlayerAchievementMatcher : RegexLogMatcher
{
	public PlayerAchievementMatcher() : base(@"^(\w+) has made the advancement \[(.+)\]$", priority: 50) { }

	protected override ServerEvent? OnMatch(Match match, string level, string? thread)
	{
		return new PlayerAchievementEvent
		{
			PlayerName = match.Groups[1].Value,
			Achievement = match.Groups[2].Value
		};
	}
}

/// <summary>
/// 玩家死亡匹配器
/// </summary>
internal class PlayerDeathMatcher : ILogMatcher
{
	private static readonly Regex Pattern = new(@"^(\w+) (.+)$", RegexOptions.Compiled);
	private static readonly string[] DeathKeywords = new[]
	{
		"was killed", "was slain", "died", "drowned", "burned",
		"fell", "was shot", "blown up", "hit the ground",
		"went up in flames", "walked into fire", "suffocated"
	};

	public int Priority => 10; // 低优先级，避免误匹配

	public ServerEvent? TryMatch(string message, string level, string? thread)
	{
		if (!DeathKeywords.Any(kw => message.Contains(kw, StringComparison.OrdinalIgnoreCase)))
			return null;

		var match = Pattern.Match(message);
		if (!match.Success) return null;

		return new PlayerDeathEvent
		{
			PlayerName = match.Groups[1].Value,
			DeathMessage = message
		};
	}
}
