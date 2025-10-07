using NetherGate.API.Events;
using NetherGate.API.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NetherGate.Core.Process;

/// <summary>
/// Minecraft 服务器日志解析器
/// 解析服务器输出日志，提取事件信息
/// </summary>
public class LogParser
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;

    // 日志格式正则表达式
    // 示例: [19:45:30] [Server thread/INFO]: <Player> Hello world
    private static readonly Regex LogLinePattern = new(
        @"^\[(\d{2}:\d{2}:\d{2})\] \[([^\]]+)/([^\]]+)\]: (.+)$",
        RegexOptions.Compiled);

    // 兼容 Paper 等简化日志格式
    // 示例: [19:45:30 INFO]: <Player> Hello world
    private static readonly Regex AlternateLogLinePattern = new(
        @"^\[(\d{2}:\d{2}:\d{2})\s+([A-Z]+)\s*\]:\s+(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 注意: 玩家加入/离开事件由 SMP 协议提供（更可靠、更及时）
    // 此处仅解析 SMP 不支持的事件

    // 玩家聊天: <Player> message
    private static readonly Regex PlayerChatPattern = new(
        @"^<(\w+)> (.+)$",
        RegexOptions.Compiled);

    // 玩家命令: Player issued server command: /command args
    private static readonly Regex PlayerCommandPattern = new(
        @"^(\w+) issued server command: (.+)$",
        RegexOptions.Compiled);

    // 玩家死亡: Player was killed by Zombie
    private static readonly Regex PlayerDeathPattern = new(
        @"^(\w+) (.+)$",
        RegexOptions.Compiled);

    // 玩家成就: Player has made the advancement [Achievement Name]
    private static readonly Regex PlayerAchievementPattern = new(
        @"^(\w+) has made the advancement \[(.+)\]$",
        RegexOptions.Compiled);

    // 服务器启动完成: Done (X.XXXs)! For help, type "help"
    private static readonly Regex ServerReadyPattern = new(
        @"Done \(([\d.]+)s\)!?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // 服务器准备关闭: Stopping server
    private static readonly Regex ServerStoppingPattern = new(
        @"^Stopping server$",
        RegexOptions.Compiled);

    public LogParser(ILogger logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    /// <summary>
    /// 解析日志行
    /// </summary>
    public async Task ParseLineAsync(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        try
        {
            line = line.Trim();
            var match = LogLinePattern.Match(line);
            if (match.Success)
            {
                var time = match.Groups[1].Value;
                var thread = match.Groups[2].Value;
                var level = match.Groups[3].Value;
                var message = match.Groups[4].Value;

                // 发布日志事件
                await PublishLogEventAsync(level, message, thread);

                // 尝试解析特定事件
                await TryParseSpecificEventsAsync(message);
                return;
            }

            // 尝试匹配简化日志格式: [HH:mm:ss LEVEL]: message
            var altMatch = AlternateLogLinePattern.Match(line);
            if (altMatch.Success)
            {
                var time = altMatch.Groups[1].Value;
                var level = altMatch.Groups[2].Value;
                var message = altMatch.Groups[3].Value;

                await PublishLogEventAsync(level, message);
                await TryParseSpecificEventsAsync(message);
                return;
            }

            // 两种格式都不符合：发布原始日志
            await PublishLogEventAsync("INFO", line);

            // 尝试从嵌套的 "[...]: " 包裹中提取最内层消息并解析（例如 Paper 输出或嵌套前缀导致）
            var innerMessage = ExtractInnermostMessage(line);
            if (!string.Equals(innerMessage, line, StringComparison.Ordinal))
            {
                await TryParseSpecificEventsAsync(innerMessage);
                return;
            }

            // 最后尝试在整行中解析（兼容性兜底）
            await TryParseSpecificEventsAsync(line);
        }
        catch (Exception ex)
        {
            _logger.Error($"解析日志行失败: {line}", ex);
        }
    }

    /// <summary>
    /// 尝试解析特定事件
    /// 注意: 玩家加入/离开事件由 SMP 协议提供，此处不再解析
    /// </summary>
    private async Task TryParseSpecificEventsAsync(string message)
    {
        // 调试：记录接收到的消息
        if (message.Contains("Done", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Debug($"[LogParser] 检测到包含 Done 的消息: {message}");
        }
        
        // 服务器启动完成
        var readyMatch = ServerReadyPattern.Match(message);
        if (readyMatch.Success)
        {
            var startupTime = double.Parse(readyMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            
            _logger.Info("========================================");
            _logger.Info($" 检测到服务器启动完成！");
            _logger.Info($" 启动耗时: {startupTime:F3} 秒");
            _logger.Info("========================================");
            
            await _eventBus.PublishAsync(new ServerReadyEvent
            {
                StartupTimeSeconds = startupTime
            });
            return;
        }
        else if (message.Contains("Done", StringComparison.OrdinalIgnoreCase))
        {
            // 调试：正则匹配失败
            _logger.Warning($"[LogParser] Done 关键词存在但正则匹配失败！");
            _logger.Warning($"[LogParser] 消息内容: [{message}]");
            _logger.Warning($"[LogParser] 正则模式: ^Done \\(([\\d.]+)s\\)! For help");
        }

        // 服务器准备关闭
        if (ServerStoppingPattern.IsMatch(message))
        {
            await _eventBus.PublishAsync(new ServerShuttingDownEvent());
            _logger.Info("服务器正在关闭...");
            return;
        }

        // 玩家聊天
        var chatMatch = PlayerChatPattern.Match(message);
        if (chatMatch.Success)
        {
            await _eventBus.PublishAsync(new PlayerChatEvent
            {
                PlayerName = chatMatch.Groups[1].Value,
                Message = chatMatch.Groups[2].Value
            });
            return;
        }

        // 玩家命令
        var commandMatch = PlayerCommandPattern.Match(message);
        if (commandMatch.Success)
        {
            await _eventBus.PublishAsync(new PlayerCommandEvent
            {
                PlayerName = commandMatch.Groups[1].Value,
                Command = commandMatch.Groups[2].Value
            });
            return;
        }

        // 玩家成就
        var achievementMatch = PlayerAchievementPattern.Match(message);
        if (achievementMatch.Success)
        {
            await _eventBus.PublishAsync(new PlayerAchievementEvent
            {
                PlayerName = achievementMatch.Groups[1].Value,
                Achievement = achievementMatch.Groups[2].Value
            });
            return;
        }

        // 玩家死亡（需要更复杂的判断，因为死亡消息格式多样）
        if (IsDeathMessage(message))
        {
            var deathMatch = PlayerDeathPattern.Match(message);
            if (deathMatch.Success)
            {
                await _eventBus.PublishAsync(new PlayerDeathEvent
                {
                    PlayerName = deathMatch.Groups[1].Value,
                    DeathMessage = message
                });
            }
        }
    }

    /// <summary>
    /// 提取嵌套 "[...]: " 结构中的最内层消息
    /// 例如："[00:27:00 INFO ]: [MC] [00:27:00 INFO]: Done (19.532s)! ..." -> "Done (19.532s)! ..."
    /// </summary>
    private static string ExtractInnermostMessage(string line)
    {
        var current = line;
        // 最多剥离 5 层，防止意外的无限循环
        for (int i = 0; i < 5; i++)
        {
            var idx = current.IndexOf("]: ", StringComparison.Ordinal);
            if (idx > 0 && current[0] == '[')
            {
                // 去掉前缀，包括 "]: "
                current = current.Substring(idx + 3);
                continue;
            }
            break;
        }
        return current;
    }

    /// <summary>
    /// 发布日志事件
    /// </summary>
    private async Task PublishLogEventAsync(string level, string message, string? thread = null)
    {
        await _eventBus.PublishAsync(new ServerLogEvent
        {
            LogLevel = level,
            Message = message,
            Thread = thread
        });
    }

    /// <summary>
    /// 判断是否为死亡消息
    /// </summary>
    private bool IsDeathMessage(string message)
    {
        var deathKeywords = new[]
        {
            "was killed", "was slain", "died", "drowned", "burned",
            "fell", "was shot", "blown up", "hit the ground",
            "went up in flames", "walked into fire", "suffocated"
        };

        return deathKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

