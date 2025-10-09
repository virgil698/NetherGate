using NetherGate.API.GameDisplay;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.Core.Utilities;
using System.Text.RegularExpressions;

namespace NetherGate.Core.GameDisplay;

/// <summary>
/// 游戏内显示 API 实现
/// 通过 RCON 执行 Minecraft 命令来实现各种显示效果
/// </summary>
public class GameDisplayApi : IGameDisplayApi
{
    private readonly IRconClient _rconClient;
    private readonly ILogger _logger;

    public GameDisplayApi(IRconClient rconClient, ILogger logger)
    {
        _rconClient = rconClient;
        _logger = logger;
    }

    // ========== Title 相关 ==========

    public async Task ShowTitleAsync(string targets, string title, string? subtitle = null, int fadeIn = 10, int stay = 70, int fadeOut = 20)
    {
        // 设置时间
        await ExecuteCommandAsync($"title {targets} times {fadeIn} {stay} {fadeOut}");
        
        // 显示主标题
        var titleJson = StringEscapeUtils.EscapeJson(title);
        await ExecuteCommandAsync($"title {targets} title {titleJson}");
        
        // 显示副标题（如果有）
        if (!string.IsNullOrEmpty(subtitle))
        {
            var subtitleJson = StringEscapeUtils.EscapeJson(subtitle);
            await ExecuteCommandAsync($"title {targets} subtitle {subtitleJson}");
        }
    }

    public async Task ClearTitleAsync(string targets)
    {
        await ExecuteCommandAsync($"title {targets} clear");
    }

    public async Task ResetTitleAsync(string targets)
    {
        await ExecuteCommandAsync($"title {targets} reset");
    }

    // ========== ActionBar 相关 ==========

    public async Task ShowActionBarAsync(string targets, string message)
    {
        var messageJson = StringEscapeUtils.EscapeJson(message);
        await ExecuteCommandAsync($"title {targets} actionbar {messageJson}");
    }

    // ========== BossBar 相关 ==========

    public async Task CreateBossBarAsync(string id, string name, BossBarColor color = BossBarColor.Purple, BossBarStyle style = BossBarStyle.Progress)
    {
        var nameJson = StringEscapeUtils.EscapeJson(name);
        var colorStr = color.ToString().ToLower();
        var styleStr = ConvertBossBarStyle(style);
        
        await ExecuteCommandAsync($"bossbar add {id} {nameJson}");
        await SetBossBarColorAsync(id, color);
        await SetBossBarStyleAsync(id, style);
    }

    public async Task SetBossBarPlayersAsync(string id, string targets)
    {
        await ExecuteCommandAsync($"bossbar set {id} players {targets}");
    }

    public async Task SetBossBarNameAsync(string id, string name)
    {
        var nameJson = StringEscapeUtils.EscapeJson(name);
        await ExecuteCommandAsync($"bossbar set {id} name {nameJson}");
    }

    public async Task SetBossBarColorAsync(string id, BossBarColor color)
    {
        var colorStr = color.ToString().ToLower();
        await ExecuteCommandAsync($"bossbar set {id} color {colorStr}");
    }

    public async Task SetBossBarStyleAsync(string id, BossBarStyle style)
    {
        var styleStr = ConvertBossBarStyle(style);
        await ExecuteCommandAsync($"bossbar set {id} style {styleStr}");
    }

    public async Task SetBossBarValueAsync(string id, int value)
    {
        await ExecuteCommandAsync($"bossbar set {id} value {value}");
    }

    public async Task SetBossBarMaxAsync(string id, int max)
    {
        await ExecuteCommandAsync($"bossbar set {id} max {max}");
    }

    public async Task SetBossBarVisibleAsync(string id, bool visible)
    {
        var visibleStr = visible ? "true" : "false";
        await ExecuteCommandAsync($"bossbar set {id} visible {visibleStr}");
    }

    public async Task RemoveBossBarAsync(string id)
    {
        await ExecuteCommandAsync($"bossbar remove {id}");
    }

    public async Task<List<string>> ListBossBarsAsync()
    {
        var result = await ExecuteCommandAsync("bossbar list");
        
        // 解析返回的 BossBar 列表
        var bossbars = new List<string>();
        if (!string.IsNullOrEmpty(result))
        {
            // 匹配格式: "There are 2 custom bossbars: [id1], [id2]"
            var match = Regex.Match(result, @"\[(.*?)\]", RegexOptions.IgnoreCase);
            while (match.Success)
            {
                bossbars.Add(match.Groups[1].Value);
                match = match.NextMatch();
            }
        }
        
        return bossbars;
    }

    // ========== 计分板相关 ==========

    public async Task CreateScoreboardObjectiveAsync(string name, string criteria = "dummy", string? displayName = null)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            await ExecuteCommandAsync($"scoreboard objectives add {name} {criteria}");
        }
        else
        {
            var displayJson = StringEscapeUtils.EscapeJson(displayName);
            await ExecuteCommandAsync($"scoreboard objectives add {name} {criteria} {displayJson}");
        }
    }

    public async Task RemoveScoreboardObjectiveAsync(string name)
    {
        await ExecuteCommandAsync($"scoreboard objectives remove {name}");
    }

    public async Task SetScoreboardDisplayAsync(string slot, string objective)
    {
        await ExecuteCommandAsync($"scoreboard objectives setdisplay {slot} {objective}");
    }

    public async Task SetScoreAsync(string target, string objective, int score)
    {
        await ExecuteCommandAsync($"scoreboard players set {target} {objective} {score}");
    }

    public async Task AddScoreAsync(string target, string objective, int amount)
    {
        await ExecuteCommandAsync($"scoreboard players add {target} {objective} {amount}");
    }

    public async Task RemoveScoreAsync(string target, string objective, int amount)
    {
        await ExecuteCommandAsync($"scoreboard players remove {target} {objective} {amount}");
    }

    public async Task ResetScoreAsync(string target, string? objective = null)
    {
        if (string.IsNullOrEmpty(objective))
        {
            await ExecuteCommandAsync($"scoreboard players reset {target}");
        }
        else
        {
            await ExecuteCommandAsync($"scoreboard players reset {target} {objective}");
        }
    }

    public async Task<int?> GetScoreAsync(string target, string objective)
    {
        var result = await ExecuteCommandAsync($"scoreboard players get {target} {objective}");
        
        // 解析返回的分数
        // 格式: "[PlayerName] has [score] [objective]"
        var match = Regex.Match(result, @"has (\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
        {
            return score;
        }
        
        return null;
    }

    // ========== 粒子效果相关 ==========

    public async Task SpawnParticleAsync(string particle, double x, double y, double z, 
        double deltaX = 0, double deltaY = 0, double deltaZ = 0, 
        double speed = 1.0, int count = 1)
    {
        await ExecuteCommandAsync($"particle {particle} {x} {y} {z} {deltaX} {deltaY} {deltaZ} {speed} {count}");
    }

    // ========== 声音相关 ==========

    public async Task PlaySoundAsync(string sound, string source, string targets, 
        double? x = null, double? y = null, double? z = null, 
        double volume = 1.0, double pitch = 1.0)
    {
        string command;
        if (x.HasValue && y.HasValue && z.HasValue)
        {
            command = $"playsound {sound} {source} {targets} {x.Value} {y.Value} {z.Value} {volume} {pitch}";
        }
        else
        {
            command = $"playsound {sound} {source} {targets}";
        }
        
        await ExecuteCommandAsync(command);
    }

    public async Task StopSoundAsync(string targets, string? sound = null, string? source = null)
    {
        if (!string.IsNullOrEmpty(sound) && !string.IsNullOrEmpty(source))
        {
            await ExecuteCommandAsync($"stopsound {targets} {source} {sound}");
        }
        else if (!string.IsNullOrEmpty(source))
        {
            await ExecuteCommandAsync($"stopsound {targets} {source}");
        }
        else
        {
            await ExecuteCommandAsync($"stopsound {targets}");
        }
    }

    // ========== 聊天消息相关 ==========

    public async Task SendChatMessageAsync(string targets, string message)
    {
        var jsonText = StringEscapeUtils.EscapeJson(message);
        await ExecuteCommandAsync($"tellraw {targets} {jsonText}");
    }

    public async Task SendFormattedMessageAsync(string targets, string jsonText)
    {
        await ExecuteCommandAsync($"tellraw {targets} {jsonText}");
    }

    public async Task SendColoredMessageAsync(string targets, string message, TextColor color = TextColor.White, bool bold = false, bool italic = false, bool underlined = false)
    {
        var colorName = GetColorName(color);
        var escapedMessage = message.Replace("\\", "\\\\").Replace("\"", "\\\"");
        
        var jsonParts = new List<string>
        {
            $"\"text\":\"{escapedMessage}\"",
            $"\"color\":\"{colorName}\""
        };

        if (bold) jsonParts.Add("\"bold\":true");
        if (italic) jsonParts.Add("\"italic\":true");
        if (underlined) jsonParts.Add("\"underlined\":true");

        var jsonText = "{" + string.Join(",", jsonParts) + "}";
        await ExecuteCommandAsync($"tellraw {targets} {jsonText}");
    }

    // ========== 对话框相关 (Minecraft 1.21.6+) ==========

    public async Task ShowDialogAsync(string targets, string dialogSnbt)
    {
        await ExecuteCommandAsync($"dialog show {targets} {dialogSnbt}");
    }

    public async Task ShowNoticeDialogAsync(string targets, string title, string body)
    {
        // 转义特殊字符
        var escapedTitle = StringEscapeUtils.EscapeSnbt(title);
        var escapedBody = StringEscapeUtils.EscapeSnbt(body);
        
        // 构建 SNBT 格式的对话框数据
        var dialogSnbt = $"{{type:\"minecraft:notice\",title:\"{escapedTitle}\",body:[{{type:\"minecraft:plain_message\",contents:\"{escapedBody}\"}}]}}";
        
        await ExecuteCommandAsync($"dialog show {targets} {dialogSnbt}");
    }

    public async Task ShowDialogByIdAsync(string targets, string dialogId)
    {
        await ExecuteCommandAsync($"dialog show {targets} {dialogId}");
    }

    public async Task ClearDialogAsync(string targets)
    {
        await ExecuteCommandAsync($"dialog clear {targets}");
    }

    // ========== 玩家控制相关 ==========

    public async Task GiveItemAsync(string targets, string item, int count = 1)
    {
        await ExecuteCommandAsync($"give {targets} {item} {count}");
    }

    public async Task TeleportAsync(string targets, double x, double y, double z)
    {
        await ExecuteCommandAsync($"tp {targets} {x} {y} {z}");
    }

    public async Task TeleportToPlayerAsync(string targets, string destination)
    {
        await ExecuteCommandAsync($"tp {targets} {destination}");
    }

    public async Task SetGameModeAsync(string targets, string gamemode)
    {
        await ExecuteCommandAsync($"gamemode {gamemode} {targets}");
    }

    public async Task GiveExperienceAsync(string targets, int amount, bool isLevels = false)
    {
        var unit = isLevels ? "levels" : "points";
        await ExecuteCommandAsync($"experience add {targets} {amount} {unit}");
    }

    public async Task ClearInventoryAsync(string targets, string? item = null, int? maxCount = null)
    {
        if (string.IsNullOrEmpty(item))
        {
            await ExecuteCommandAsync($"clear {targets}");
        }
        else if (maxCount.HasValue)
        {
            await ExecuteCommandAsync($"clear {targets} {item} {maxCount.Value}");
        }
        else
        {
            await ExecuteCommandAsync($"clear {targets} {item}");
        }
    }

    // ========== 实体控制相关 ==========

    public async Task SummonEntityAsync(string entity, double x, double y, double z, string? nbt = null)
    {
        if (string.IsNullOrEmpty(nbt))
        {
            await ExecuteCommandAsync($"summon {entity} {x} {y} {z}");
        }
        else
        {
            await ExecuteCommandAsync($"summon {entity} {x} {y} {z} {nbt}");
        }
    }

    public async Task GiveEffectAsync(string targets, string effect, int seconds = 30, int amplifier = 0, bool hideParticles = false)
    {
        var hide = hideParticles ? "true" : "false";
        await ExecuteCommandAsync($"effect give {targets} {effect} {seconds} {amplifier} {hide}");
    }

    public async Task ClearEffectAsync(string targets, string? effect = null)
    {
        if (string.IsNullOrEmpty(effect))
        {
            await ExecuteCommandAsync($"effect clear {targets}");
        }
        else
        {
            await ExecuteCommandAsync($"effect clear {targets} {effect}");
        }
    }

    // ========== 世界控制相关 ==========

    public async Task SetBlockAsync(int x, int y, int z, string block)
    {
        await ExecuteCommandAsync($"setblock {x} {y} {z} {block}");
    }

    public async Task FillBlocksAsync(int x1, int y1, int z1, int x2, int y2, int z2, string block, string mode = "replace")
    {
        await ExecuteCommandAsync($"fill {x1} {y1} {z1} {x2} {y2} {z2} {block} {mode}");
    }

    public async Task SetWeatherAsync(string weather, int? duration = null)
    {
        if (duration.HasValue)
        {
            await ExecuteCommandAsync($"weather {weather} {duration.Value}");
        }
        else
        {
            await ExecuteCommandAsync($"weather {weather}");
        }
    }

    public async Task SetTimeAsync(string time)
    {
        await ExecuteCommandAsync($"time set {time}");
    }

    public async Task SetDifficultyAsync(string difficulty)
    {
        await ExecuteCommandAsync($"difficulty {difficulty}");
    }

    public async Task SetWorldSpawnAsync(int x, int y, int z)
    {
        await ExecuteCommandAsync($"setworldspawn {x} {y} {z}");
    }

    public async Task SetPlayerSpawnAsync(string targets, int x, int y, int z)
    {
        await ExecuteCommandAsync($"spawnpoint {targets} {x} {y} {z}");
    }

    public async Task SetWorldBorderCenterAsync(double x, double z)
    {
        await ExecuteCommandAsync($"worldborder center {x} {z}");
    }

    public async Task SetWorldBorderSizeAsync(double distance, int? time = null)
    {
        if (time.HasValue)
        {
            await ExecuteCommandAsync($"worldborder set {distance} {time.Value}");
        }
        else
        {
            await ExecuteCommandAsync($"worldborder set {distance}");
        }
    }

    // ========== 进度和配方相关 ==========

    public async Task GrantAdvancementAsync(string targets, string advancement)
    {
        await ExecuteCommandAsync($"advancement grant {targets} only {advancement}");
    }

    public async Task RevokeAdvancementAsync(string targets, string advancement)
    {
        await ExecuteCommandAsync($"advancement revoke {targets} only {advancement}");
    }

    public async Task GrantRecipeAsync(string targets, string recipe)
    {
        await ExecuteCommandAsync($"recipe give {targets} {recipe}");
    }

    public async Task RevokeRecipeAsync(string targets, string recipe)
    {
        await ExecuteCommandAsync($"recipe take {targets} {recipe}");
    }

    // ========== 队伍管理相关 ==========

    public async Task CreateTeamAsync(string teamName)
    {
        await ExecuteCommandAsync($"team add {teamName}");
    }

    public async Task RemoveTeamAsync(string teamName)
    {
        await ExecuteCommandAsync($"team remove {teamName}");
    }

    public async Task AddToTeamAsync(string teamName, string members)
    {
        await ExecuteCommandAsync($"team join {teamName} {members}");
    }

    public async Task RemoveFromTeamAsync(string teamName, string members)
    {
        await ExecuteCommandAsync($"team leave {members}");
    }

    public async Task SetTeamOptionAsync(string teamName, string option, string value)
    {
        await ExecuteCommandAsync($"team modify {teamName} {option} {value}");
    }

    // ========== 实用功能 ==========

    public async Task ExecuteFunctionAsync(string functionId)
    {
        await ExecuteCommandAsync($"function {functionId}");
    }

    public async Task AddTagAsync(string targets, string tag)
    {
        await ExecuteCommandAsync($"tag {targets} add {tag}");
    }

    public async Task RemoveTagAsync(string targets, string tag)
    {
        await ExecuteCommandAsync($"tag {targets} remove {tag}");
    }

    public async Task<string> GetBlockDataAsync(int x, int y, int z)
    {
        return await ExecuteCommandAsync($"data get block {x} {y} {z}");
    }

    public async Task<string> GetEntityDataAsync(string target)
    {
        return await ExecuteCommandAsync($"data get entity {target}");
    }

    // ========== 辅助方法 ==========

    public async Task<string> ExecuteCommandAsync(string command)
    {
        try
        {
            if (!_rconClient.IsConnected)
            {
                _logger.Warning("RCON 未连接，无法执行游戏命令");
                return string.Empty;
            }

            _logger.Debug($"执行游戏命令: {command}");
            var result = await _rconClient.ExecuteCommandAsync(command);
            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error($"执行游戏命令失败: {command}", ex);
            throw;
        }
    }

    /// <summary>
    /// 转换 BossBar 样式枚举到 Minecraft 命令格式
    /// </summary>
    private string ConvertBossBarStyle(BossBarStyle style)
    {
        return style switch
        {
            BossBarStyle.Progress => "progress",
            BossBarStyle.Notched_6 => "notched_6",
            BossBarStyle.Notched_10 => "notched_10",
            BossBarStyle.Notched_12 => "notched_12",
            BossBarStyle.Notched_20 => "notched_20",
            _ => "progress"
        };
    }

    /// <summary>
    /// 将 TextColor 枚举转换为 Minecraft 颜色名称
    /// </summary>
    private string GetColorName(TextColor color)
    {
        return color switch
        {
            TextColor.Black => "black",
            TextColor.DarkBlue => "dark_blue",
            TextColor.DarkGreen => "dark_green",
            TextColor.DarkAqua => "dark_aqua",
            TextColor.DarkRed => "dark_red",
            TextColor.DarkPurple => "dark_purple",
            TextColor.Gold => "gold",
            TextColor.Gray => "gray",
            TextColor.DarkGray => "dark_gray",
            TextColor.Blue => "blue",
            TextColor.Green => "green",
            TextColor.Aqua => "aqua",
            TextColor.Red => "red",
            TextColor.LightPurple => "light_purple",
            TextColor.Yellow => "yellow",
            TextColor.White => "white",
            _ => "white"
        };
    }
}
