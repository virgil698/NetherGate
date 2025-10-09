using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.API.Scoreboard;
using System.Text.RegularExpressions;

namespace NetherGate.Core.Scoreboard;

/// <summary>
/// 计分板系统 API 实现
/// 完全基于 RCON /scoreboard 命令
/// </summary>
public class ScoreboardApi : IScoreboardApi
{
    private readonly IRconClient? _rconClient;
    private readonly ILogger _logger;

    public ScoreboardApi(ILogger logger, IRconClient? rconClient = null)
    {
        _logger = logger;
        _rconClient = rconClient;
    }

    // ==================== 目标管理 ====================

    public async Task<bool> CreateObjectiveAsync(string name, ScoreboardCriteria criteria = ScoreboardCriteria.Dummy, string? displayName = null)
    {
        try
        {
            var criteriaStr = GetCriteriaString(criteria);
            
            if (string.IsNullOrEmpty(displayName))
            {
                await ExecuteCommandAsync($"scoreboard objectives add {name} {criteriaStr}");
            }
            else
            {
                var displayJson = EscapeJson(displayName);
                await ExecuteCommandAsync($"scoreboard objectives add {name} {criteriaStr} {displayJson}");
            }
            
            _logger.Info($"创建计分板目标: {name} ({criteriaStr})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建计分板目标失败: {name}", ex);
            return false;
        }
    }

    public async Task<bool> RemoveObjectiveAsync(string name)
    {
        try
        {
            await ExecuteCommandAsync($"scoreboard objectives remove {name}");
            _logger.Info($"删除计分板目标: {name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"删除计分板目标失败: {name}", ex);
            return false;
        }
    }

    public async Task<bool> SetDisplaySlotAsync(DisplaySlot slot, string objectiveName)
    {
        try
        {
            var slotStr = GetDisplaySlotString(slot);
            await ExecuteCommandAsync($"scoreboard objectives setdisplay {slotStr} {objectiveName}");
            _logger.Debug($"设置显示位置: {slotStr} -> {objectiveName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置显示位置失败: {slot}", ex);
            return false;
        }
    }

    public async Task<bool> ClearDisplaySlotAsync(DisplaySlot slot)
    {
        try
        {
            var slotStr = GetDisplaySlotString(slot);
            await ExecuteCommandAsync($"scoreboard objectives setdisplay {slotStr}");
            _logger.Debug($"清除显示位置: {slotStr}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"清除显示位置失败: {slot}", ex);
            return false;
        }
    }

    public async Task<bool> ModifyObjectiveDisplayNameAsync(string objectiveName, string displayName)
    {
        try
        {
            var displayJson = EscapeJson(displayName);
            await ExecuteCommandAsync($"scoreboard objectives modify {objectiveName} displayname {displayJson}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"修改目标显示名称失败: {objectiveName}", ex);
            return false;
        }
    }

    public async Task<bool> ModifyObjectiveRenderTypeAsync(string objectiveName, RenderType renderType)
    {
        try
        {
            var renderTypeStr = renderType == RenderType.Hearts ? "hearts" : "integer";
            await ExecuteCommandAsync($"scoreboard objectives modify {objectiveName} rendertype {renderTypeStr}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"修改目标渲染类型失败: {objectiveName}", ex);
            return false;
        }
    }

    // ==================== 分数操作 ====================

    public async Task<bool> SetScoreAsync(string holder, string objectiveName, int score)
    {
        try
        {
            await ExecuteCommandAsync($"scoreboard players set {holder} {objectiveName} {score}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置分数失败: {holder} {objectiveName} {score}", ex);
            return false;
        }
    }

    public async Task<bool> AddScoreAsync(string holder, string objectiveName, int amount)
    {
        try
        {
            await ExecuteCommandAsync($"scoreboard players add {holder} {objectiveName} {amount}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"增加分数失败: {holder} {objectiveName} {amount}", ex);
            return false;
        }
    }

    public async Task<bool> RemoveScoreAsync(string holder, string objectiveName, int amount)
    {
        try
        {
            await ExecuteCommandAsync($"scoreboard players remove {holder} {objectiveName} {amount}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"减少分数失败: {holder} {objectiveName} {amount}", ex);
            return false;
        }
    }

    public async Task<int?> GetScoreAsync(string holder, string objectiveName)
    {
        try
        {
            var result = await ExecuteCommandAsync($"scoreboard players get {holder} {objectiveName}");
            
            // 解析返回的分数
            // 格式: "[PlayerName] has [score] [objective]"
            var match = Regex.Match(result, @"has (-?\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
            {
                return score;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.Debug($"获取分数失败: {holder} {objectiveName} - {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ResetScoreAsync(string holder, string? objectiveName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(objectiveName))
            {
                await ExecuteCommandAsync($"scoreboard players reset {holder}");
            }
            else
            {
                await ExecuteCommandAsync($"scoreboard players reset {holder} {objectiveName}");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"重置分数失败: {holder}", ex);
            return false;
        }
    }

    public async Task<Dictionary<string, int>> ListScoresAsync(string holder)
    {
        try
        {
            var result = await ExecuteCommandAsync($"scoreboard players list {holder}");
            var scores = new Dictionary<string, int>();
            
            // 解析分数列表
            // 格式: "Player has 5 scores: [objective1: 10, objective2: 20]"
            var matches = Regex.Matches(result, @"(\w+): (-?\d+)");
            foreach (Match match in matches)
            {
                if (match.Success && int.TryParse(match.Groups[2].Value, out var score))
                {
                    scores[match.Groups[1].Value] = score;
                }
            }
            
            return scores;
        }
        catch (Exception ex)
        {
            _logger.Error($"列出分数失败: {holder}", ex);
            return new Dictionary<string, int>();
        }
    }

    // ==================== 分数操作（高级）====================

    public async Task<bool> OperationAsync(string targetHolder, string targetObjective, 
        ScoreboardOperation operation, string sourceHolder, string sourceObjective)
    {
        try
        {
            var operationStr = GetOperationString(operation);
            await ExecuteCommandAsync($"scoreboard players operation {targetHolder} {targetObjective} {operationStr} {sourceHolder} {sourceObjective}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"分数运算失败: {targetHolder} {targetObjective} {operation}", ex);
            return false;
        }
    }

    public async Task<bool> EnableTriggerAsync(string holder, string objectiveName)
    {
        try
        {
            await ExecuteCommandAsync($"scoreboard players enable {holder} {objectiveName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"启用触发器失败: {holder} {objectiveName}", ex);
            return false;
        }
    }

    // ==================== 队伍管理 ====================

    public async Task<bool> CreateTeamAsync(string teamName, string? displayName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(displayName))
            {
                await ExecuteCommandAsync($"team add {teamName}");
            }
            else
            {
                var displayJson = EscapeJson(displayName);
                await ExecuteCommandAsync($"team add {teamName} {displayJson}");
            }
            _logger.Info($"创建队伍: {teamName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建队伍失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> RemoveTeamAsync(string teamName)
    {
        try
        {
            await ExecuteCommandAsync($"team remove {teamName}");
            _logger.Info($"删除队伍: {teamName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"删除队伍失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> EmptyTeamAsync(string teamName)
    {
        try
        {
            await ExecuteCommandAsync($"team empty {teamName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"清空队伍失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> JoinTeamAsync(string teamName, string members)
    {
        try
        {
            await ExecuteCommandAsync($"team join {teamName} {members}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"加入队伍失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> LeaveTeamAsync(string members)
    {
        try
        {
            await ExecuteCommandAsync($"team leave {members}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"离开队伍失败", ex);
            return false;
        }
    }

    public async Task<bool> ModifyTeamOptionAsync(string teamName, TeamOption option, string value)
    {
        try
        {
            var optionStr = GetTeamOptionString(option);
            await ExecuteCommandAsync($"team modify {teamName} {optionStr} {value}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"修改队伍选项失败: {teamName} {option}", ex);
            return false;
        }
    }

    public async Task<bool> SetTeamColorAsync(string teamName, TeamColor color)
    {
        try
        {
            var colorStr = GetTeamColorString(color);
            await ExecuteCommandAsync($"team modify {teamName} color {colorStr}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置队伍颜色失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> SetTeamFriendlyFireAsync(string teamName, bool enabled)
    {
        try
        {
            var value = enabled ? "true" : "false";
            await ExecuteCommandAsync($"team modify {teamName} friendlyFire {value}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置友军伤害失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> SetTeamSeeFriendlyInvisiblesAsync(string teamName, bool enabled)
    {
        try
        {
            var value = enabled ? "true" : "false";
            await ExecuteCommandAsync($"team modify {teamName} seeFriendlyInvisibles {value}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置看见隐身队友失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> SetTeamNameTagVisibilityAsync(string teamName, NameTagVisibility visibility)
    {
        try
        {
            var visibilityStr = GetNameTagVisibilityString(visibility);
            await ExecuteCommandAsync($"team modify {teamName} nametagVisibility {visibilityStr}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置名称标签可见性失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> SetTeamPrefixAsync(string teamName, string prefix)
    {
        try
        {
            var prefixJson = EscapeJson(prefix);
            await ExecuteCommandAsync($"team modify {teamName} prefix {prefixJson}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置队伍前缀失败: {teamName}", ex);
            return false;
        }
    }

    public async Task<bool> SetTeamSuffixAsync(string teamName, string suffix)
    {
        try
        {
            var suffixJson = EscapeJson(suffix);
            await ExecuteCommandAsync($"team modify {teamName} suffix {suffixJson}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置队伍后缀失败: {teamName}", ex);
            return false;
        }
    }

    // ==================== 高级功能 ====================

    public async Task<List<ScoreEntry>> GetTopScoresAsync(string objectiveName, int limit = 10, bool descending = true)
    {
        try
        {
            // 获取所有在线玩家的列表
            var listResult = await ExecuteCommandAsync("list");
            var players = ParsePlayerList(listResult);

            if (!players.Any())
            {
                _logger.Warning("没有在线玩家，无法获取排行榜");
                return new List<ScoreEntry>();
            }

            // 获取每个玩家的分数
            var scoreEntries = new List<ScoreEntry>();
            foreach (var player in players)
            {
                try
                {
                    var score = await GetScoreAsync(player, objectiveName);
                    if (score.HasValue)
                    {
                        scoreEntries.Add(new ScoreEntry
                        {
                            Holder = player,
                            Score = score.Value
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"获取玩家 {player} 的分数失败: {ex.Message}");
                }
            }

            // 排序并限制数量
            var sorted = descending
                ? scoreEntries.OrderByDescending(e => e.Score)
                : scoreEntries.OrderBy(e => e.Score);

            return sorted.Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"获取排行榜失败: {objectiveName}", ex);
            return new List<ScoreEntry>();
        }
    }

    public async Task<int?> GetPlayerRankAsync(string holder, string objectiveName)
    {
        try
        {
            // 获取排行榜
            var topScores = await GetTopScoresAsync(objectiveName, int.MaxValue, true);

            // 查找玩家排名
            for (int i = 0; i < topScores.Count; i++)
            {
                if (topScores[i].Holder.Equals(holder, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1; // 排名从1开始
                }
            }

            _logger.Warning($"玩家 {holder} 在目标 {objectiveName} 中没有分数");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取玩家排名失败: {holder} {objectiveName}", ex);
            return null;
        }
    }

    /// <summary>
    /// 解析玩家列表
    /// </summary>
    private List<string> ParsePlayerList(string listResult)
    {
        var players = new List<string>();
        
        try
        {
            // 解析 "There are X of a max of Y players online: player1, player2, player3"
            var match = Regex.Match(listResult, @"online:\s*(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerNames = match.Groups[1].Value.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p));
                players.AddRange(playerNames);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("解析玩家列表失败", ex);
        }

        return players;
    }

    // ==================== 辅助方法 ====================

    private async Task<string> ExecuteCommandAsync(string command)
    {
        if (_rconClient == null)
        {
            throw new InvalidOperationException("RCON 客户端未初始化");
        }

        if (!_rconClient.IsConnected)
        {
            throw new InvalidOperationException("RCON 未连接");
        }

        var result = await _rconClient.ExecuteCommandAsync(command);
        return result ?? string.Empty;
    }

    private string EscapeJson(string text)
    {
        // 如果已经是 JSON 格式，直接返回
        if (text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("["))
        {
            return text;
        }
        
        // 否则包装为简单的 JSON 文本组件
        var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"{{\"text\":\"{escaped}\"}}";
    }

    private string GetCriteriaString(ScoreboardCriteria criteria)
    {
        return criteria switch
        {
            ScoreboardCriteria.Dummy => "dummy",
            ScoreboardCriteria.Trigger => "trigger",
            ScoreboardCriteria.DeathCount => "deathCount",
            ScoreboardCriteria.PlayerKillCount => "playerKillCount",
            ScoreboardCriteria.TotalKillCount => "totalKillCount",
            ScoreboardCriteria.Health => "health",
            ScoreboardCriteria.Xp => "xp",
            ScoreboardCriteria.Level => "level",
            ScoreboardCriteria.Food => "food",
            ScoreboardCriteria.Air => "air",
            ScoreboardCriteria.Armor => "armor",
            _ => "dummy"
        };
    }

    private string GetDisplaySlotString(DisplaySlot slot)
    {
        return slot switch
        {
            DisplaySlot.Sidebar => "sidebar",
            DisplaySlot.List => "list",
            DisplaySlot.BelowName => "belowName",
            _ => "sidebar"
        };
    }

    private string GetOperationString(ScoreboardOperation operation)
    {
        return operation switch
        {
            ScoreboardOperation.Add => "+=",
            ScoreboardOperation.Subtract => "-=",
            ScoreboardOperation.Multiply => "*=",
            ScoreboardOperation.Divide => "/=",
            ScoreboardOperation.Modulo => "%=",
            ScoreboardOperation.Assign => "=",
            ScoreboardOperation.Min => "<",
            ScoreboardOperation.Max => ">",
            ScoreboardOperation.Swap => "><",
            _ => "="
        };
    }

    private string GetTeamOptionString(TeamOption option)
    {
        return option switch
        {
            TeamOption.Color => "color",
            TeamOption.FriendlyFire => "friendlyFire",
            TeamOption.SeeFriendlyInvisibles => "seeFriendlyInvisibles",
            TeamOption.NameTagVisibility => "nametagVisibility",
            TeamOption.DeathMessageVisibility => "deathMessageVisibility",
            TeamOption.CollisionRule => "collisionRule",
            TeamOption.DisplayName => "displayname",
            TeamOption.Prefix => "prefix",
            TeamOption.Suffix => "suffix",
            _ => "color"
        };
    }

    private string GetTeamColorString(TeamColor color)
    {
        return color switch
        {
            TeamColor.Black => "black",
            TeamColor.DarkBlue => "dark_blue",
            TeamColor.DarkGreen => "dark_green",
            TeamColor.DarkAqua => "dark_aqua",
            TeamColor.DarkRed => "dark_red",
            TeamColor.DarkPurple => "dark_purple",
            TeamColor.Gold => "gold",
            TeamColor.Gray => "gray",
            TeamColor.DarkGray => "dark_gray",
            TeamColor.Blue => "blue",
            TeamColor.Green => "green",
            TeamColor.Aqua => "aqua",
            TeamColor.Red => "red",
            TeamColor.LightPurple => "light_purple",
            TeamColor.Yellow => "yellow",
            TeamColor.White => "white",
            TeamColor.Reset => "reset",
            _ => "white"
        };
    }

    private string GetNameTagVisibilityString(NameTagVisibility visibility)
    {
        return visibility switch
        {
            NameTagVisibility.Always => "always",
            NameTagVisibility.Never => "never",
            NameTagVisibility.HideForOwnTeam => "hideForOwnTeam",
            NameTagVisibility.HideForOtherTeams => "hideForOtherTeams",
            _ => "always"
        };
    }
}

