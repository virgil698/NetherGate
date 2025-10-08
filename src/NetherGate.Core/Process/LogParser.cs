using NetherGate.API.Events;
using NetherGate.API.Logging;
using System.Text.RegularExpressions;

namespace NetherGate.Core.Process;

/// <summary>
/// Minecraft 服务器日志解析器
/// 解析服务器输出日志，提取事件信息，支持插件注册自定义匹配器
/// </summary>
public class LogParser
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly List<ILogMatcher> _matchers = new();
    private readonly object _matcherLock = new();

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

    public LogParser(ILogger logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;

        // 注册内置匹配器
        RegisterMatcher(new ServerReadyMatcher());
        RegisterMatcher(new ServerStoppingMatcher());
        RegisterMatcher(new PlayerJoinMatcher());
        RegisterMatcher(new PlayerLeaveMatcher());
        RegisterMatcher(new PlayerChatMatcher());
        RegisterMatcher(new PlayerCommandMatcher());
        RegisterMatcher(new PlayerAchievementMatcher());
        RegisterMatcher(new PlayerDeathMatcher());
    }

    /// <summary>
    /// 注册自定义日志匹配器
    /// </summary>
    public void RegisterMatcher(ILogMatcher matcher)
    {
        lock (_matcherLock)
        {
            _matchers.Add(matcher);
            // 按优先级降序排序
            _matchers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
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
                await TryParseSpecificEventsAsync(message, level, thread);
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
                await TryParseSpecificEventsAsync(message, level);
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
    /// 尝试解析特定事件（使用匹配器管线）
    /// </summary>
    private async Task TryParseSpecificEventsAsync(string message, string level = "INFO", string? thread = null)
    {
        ILogMatcher[] matchers;
        lock (_matcherLock)
        {
            matchers = _matchers.ToArray();
        }

        foreach (var matcher in matchers)
        {
            try
            {
                var evt = matcher.TryMatch(message, level, thread);
                if (evt != null)
                {
                    // 特殊处理 ServerReadyEvent 的日志输出
                    if (evt is ServerReadyEvent readyEvent)
                    {
                        _logger.Info("========================================");
                        _logger.Info($" 检测到服务器启动完成！");
                        _logger.Info($" 启动耗时: {readyEvent.StartupTimeSeconds:F3} 秒");
                        _logger.Info("========================================");
                    }
                    else if (evt is ServerShuttingDownEvent)
                    {
                        _logger.Info("服务器正在关闭...");
                    }

                    await _eventBus.PublishAsync(evt);
                    return; // 匹配成功，停止后续匹配
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"日志匹配器 {matcher.GetType().Name} 执行失败", ex);
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

}

