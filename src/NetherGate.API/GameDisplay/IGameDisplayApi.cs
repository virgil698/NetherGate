namespace NetherGate.API.GameDisplay;

/// <summary>
/// 游戏内显示 API 接口
/// 用于在游戏中显示 BossBar、Title、ActionBar、计分板等内容
/// </summary>
public interface IGameDisplayApi
{
    // ========== Title 相关 ==========
    
    /// <summary>
    /// 显示标题
    /// </summary>
    /// <param name="targets">目标玩家（支持 @a, @p, 玩家名等）</param>
    /// <param name="title">主标题文本（JSON 格式或纯文本）</param>
    /// <param name="subtitle">副标题文本（可选）</param>
    /// <param name="fadeIn">淡入时间（tick，1秒=20tick）</param>
    /// <param name="stay">停留时间（tick）</param>
    /// <param name="fadeOut">淡出时间（tick）</param>
    Task ShowTitleAsync(string targets, string title, string? subtitle = null, int fadeIn = 10, int stay = 70, int fadeOut = 20);

    /// <summary>
    /// 清除标题
    /// </summary>
    Task ClearTitleAsync(string targets);

    /// <summary>
    /// 重置标题时间设置
    /// </summary>
    Task ResetTitleAsync(string targets);

    // ========== ActionBar 相关 ==========
    
    /// <summary>
    /// 显示操作栏消息（物品栏上方）
    /// </summary>
    Task ShowActionBarAsync(string targets, string message);

    // ========== BossBar 相关 ==========
    
    /// <summary>
    /// 创建 BossBar
    /// </summary>
    /// <param name="id">BossBar ID（命名空间格式，如 "custom:progress"）</param>
    /// <param name="name">显示名称（JSON 格式或纯文本）</param>
    /// <param name="color">颜色（blue/green/pink/purple/red/white/yellow）</param>
    /// <param name="style">样式（notched_6/notched_10/notched_12/notched_20/progress）</param>
    Task CreateBossBarAsync(string id, string name, BossBarColor color = BossBarColor.Purple, BossBarStyle style = BossBarStyle.Progress);

    /// <summary>
    /// 设置 BossBar 显示的玩家
    /// </summary>
    Task SetBossBarPlayersAsync(string id, string targets);

    /// <summary>
    /// 设置 BossBar 名称
    /// </summary>
    Task SetBossBarNameAsync(string id, string name);

    /// <summary>
    /// 设置 BossBar 颜色
    /// </summary>
    Task SetBossBarColorAsync(string id, BossBarColor color);

    /// <summary>
    /// 设置 BossBar 样式
    /// </summary>
    Task SetBossBarStyleAsync(string id, BossBarStyle style);

    /// <summary>
    /// 设置 BossBar 数值（0-100）
    /// </summary>
    Task SetBossBarValueAsync(string id, int value);

    /// <summary>
    /// 设置 BossBar 最大值
    /// </summary>
    Task SetBossBarMaxAsync(string id, int max);

    /// <summary>
    /// 设置 BossBar 可见性
    /// </summary>
    Task SetBossBarVisibleAsync(string id, bool visible);

    /// <summary>
    /// 移除 BossBar
    /// </summary>
    Task RemoveBossBarAsync(string id);

    /// <summary>
    /// 获取所有 BossBar ID
    /// </summary>
    Task<List<string>> ListBossBarsAsync();

    // ========== 计分板相关 ==========
    
    /// <summary>
    /// 创建计分板目标
    /// </summary>
    /// <param name="name">目标名称</param>
    /// <param name="criteria">标准（dummy/health/playerKillCount等）</param>
    /// <param name="displayName">显示名称（可选）</param>
    Task CreateScoreboardObjectiveAsync(string name, string criteria = "dummy", string? displayName = null);

    /// <summary>
    /// 移除计分板目标
    /// </summary>
    Task RemoveScoreboardObjectiveAsync(string name);

    /// <summary>
    /// 设置计分板显示位置
    /// </summary>
    /// <param name="slot">位置（list/sidebar/belowName）</param>
    /// <param name="objective">目标名称</param>
    Task SetScoreboardDisplayAsync(string slot, string objective);

    /// <summary>
    /// 设置玩家分数
    /// </summary>
    Task SetScoreAsync(string target, string objective, int score);

    /// <summary>
    /// 增加玩家分数
    /// </summary>
    Task AddScoreAsync(string target, string objective, int amount);

    /// <summary>
    /// 减少玩家分数
    /// </summary>
    Task RemoveScoreAsync(string target, string objective, int amount);

    /// <summary>
    /// 重置玩家分数
    /// </summary>
    Task ResetScoreAsync(string target, string? objective = null);

    /// <summary>
    /// 获取玩家分数
    /// </summary>
    Task<int?> GetScoreAsync(string target, string objective);

    // ========== 粒子效果相关 ==========
    
    /// <summary>
    /// 播放粒子效果
    /// </summary>
    /// <param name="particle">粒子类型（如 "minecraft:heart", "flame"）</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="deltaX">X 偏移范围</param>
    /// <param name="deltaY">Y 偏移范围</param>
    /// <param name="deltaZ">Z 偏移范围</param>
    /// <param name="speed">粒子速度</param>
    /// <param name="count">粒子数量</param>
    Task SpawnParticleAsync(string particle, double x, double y, double z, 
        double deltaX = 0, double deltaY = 0, double deltaZ = 0, 
        double speed = 1.0, int count = 1);

    // ========== 声音相关 ==========
    
    /// <summary>
    /// 播放声音
    /// </summary>
    /// <param name="sound">声音 ID（如 "minecraft:entity.player.levelup"）</param>
    /// <param name="source">声音来源（master/music/record/weather/block/hostile/neutral/player/ambient/voice）</param>
    /// <param name="targets">目标玩家</param>
    /// <param name="x">X 坐标（可选）</param>
    /// <param name="y">Y 坐标（可选）</param>
    /// <param name="z">Z 坐标（可选）</param>
    /// <param name="volume">音量（默认 1.0）</param>
    /// <param name="pitch">音调（默认 1.0，范围 0.0-2.0）</param>
    Task PlaySoundAsync(string sound, string source, string targets, 
        double? x = null, double? y = null, double? z = null, 
        double volume = 1.0, double pitch = 1.0);

    /// <summary>
    /// 停止声音
    /// </summary>
    Task StopSoundAsync(string targets, string? sound = null, string? source = null);

    // ========== 聊天消息相关 ==========
    
    /// <summary>
    /// 发送简单聊天消息
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="message">消息内容</param>
    Task SendChatMessageAsync(string targets, string message);

    /// <summary>
    /// 发送格式化的聊天消息（JSON 格式）
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="jsonText">JSON 文本组件</param>
    Task SendFormattedMessageAsync(string targets, string jsonText);

    /// <summary>
    /// 发送彩色聊天消息（简化版）
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="message">消息内容</param>
    /// <param name="color">文本颜色</param>
    /// <param name="bold">是否加粗</param>
    /// <param name="italic">是否斜体</param>
    /// <param name="underlined">是否下划线</param>
    Task SendColoredMessageAsync(string targets, string message, TextColor color = TextColor.White, bool bold = false, bool italic = false, bool underlined = false);

    // ========== 对话框相关 (Minecraft 1.21.6+) ==========
    
    /// <summary>
    /// 显示对话框（自定义 SNBT 格式）
    /// </summary>
    /// <param name="targets">目标玩家（支持 @a, @p, 玩家名等）</param>
    /// <param name="dialogSnbt">对话框 SNBT 数据</param>
    Task ShowDialogAsync(string targets, string dialogSnbt);

    /// <summary>
    /// 显示通知对话框（简化版）
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="title">标题</param>
    /// <param name="body">正文</param>
    Task ShowNoticeDialogAsync(string targets, string title, string body);

    /// <summary>
    /// 显示数据包定义的对话框
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="dialogId">对话框 ID（命名空间格式，如 "custom:example/test"）</param>
    Task ShowDialogByIdAsync(string targets, string dialogId);

    /// <summary>
    /// 清除对话框
    /// </summary>
    /// <param name="targets">目标玩家</param>
    Task ClearDialogAsync(string targets);

    // ========== 玩家控制相关 ==========
    
    /// <summary>
    /// 给予玩家物品
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="item">物品 ID（如 "minecraft:diamond"）</param>
    /// <param name="count">数量（默认 1）</param>
    Task GiveItemAsync(string targets, string item, int count = 1);

    /// <summary>
    /// 传送玩家
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    Task TeleportAsync(string targets, double x, double y, double z);

    /// <summary>
    /// 传送玩家到另一个玩家
    /// </summary>
    Task TeleportToPlayerAsync(string targets, string destination);

    /// <summary>
    /// 设置玩家游戏模式
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="gamemode">游戏模式（survival/creative/adventure/spectator）</param>
    Task SetGameModeAsync(string targets, string gamemode);

    /// <summary>
    /// 给予或移除玩家经验值
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="amount">经验值数量（正数给予，负数移除）</param>
    /// <param name="isLevels">是否为等级（false 为经验点）</param>
    Task GiveExperienceAsync(string targets, int amount, bool isLevels = false);

    /// <summary>
    /// 清除玩家物品
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="item">物品 ID（可选，null 则清除所有）</param>
    /// <param name="maxCount">最大清除数量（可选）</param>
    Task ClearInventoryAsync(string targets, string? item = null, int? maxCount = null);

    // ========== 实体控制相关 ==========
    
    /// <summary>
    /// 召唤实体
    /// </summary>
    /// <param name="entity">实体 ID（如 "minecraft:zombie"）</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="nbt">NBT 数据（可选）</param>
    Task SummonEntityAsync(string entity, double x, double y, double z, string? nbt = null);

    /// <summary>
    /// 给予玩家药水效果
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="effect">效果 ID（如 "minecraft:speed"）</param>
    /// <param name="seconds">持续时间（秒）</param>
    /// <param name="amplifier">效果等级（0 为 I 级）</param>
    /// <param name="hideParticles">是否隐藏粒子</param>
    Task GiveEffectAsync(string targets, string effect, int seconds = 30, int amplifier = 0, bool hideParticles = false);

    /// <summary>
    /// 清除玩家药水效果
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="effect">效果 ID（可选，null 则清除所有）</param>
    Task ClearEffectAsync(string targets, string? effect = null);

    // ========== 世界控制相关 ==========
    
    /// <summary>
    /// 设置方块
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="block">方块 ID（如 "minecraft:stone"）</param>
    Task SetBlockAsync(int x, int y, int z, string block);

    /// <summary>
    /// 填充方块
    /// </summary>
    /// <param name="x1">起始 X</param>
    /// <param name="y1">起始 Y</param>
    /// <param name="z1">起始 Z</param>
    /// <param name="x2">结束 X</param>
    /// <param name="y2">结束 Y</param>
    /// <param name="z2">结束 Z</param>
    /// <param name="block">方块 ID</param>
    /// <param name="mode">填充模式（replace/keep/outline/hollow/destroy）</param>
    Task FillBlocksAsync(int x1, int y1, int z1, int x2, int y2, int z2, string block, string mode = "replace");

    /// <summary>
    /// 设置天气
    /// </summary>
    /// <param name="weather">天气类型（clear/rain/thunder）</param>
    /// <param name="duration">持续时间（秒，可选）</param>
    Task SetWeatherAsync(string weather, int? duration = null);

    /// <summary>
    /// 设置时间
    /// </summary>
    /// <param name="time">时间值（day/night/noon/midnight 或 tick 数）</param>
    Task SetTimeAsync(string time);

    /// <summary>
    /// 设置难度
    /// </summary>
    /// <param name="difficulty">难度（peaceful/easy/normal/hard）</param>
    Task SetDifficultyAsync(string difficulty);

    /// <summary>
    /// 设置世界出生点
    /// </summary>
    Task SetWorldSpawnAsync(int x, int y, int z);

    /// <summary>
    /// 设置玩家重生点
    /// </summary>
    Task SetPlayerSpawnAsync(string targets, int x, int y, int z);

    /// <summary>
    /// 设置世界边界中心
    /// </summary>
    Task SetWorldBorderCenterAsync(double x, double z);

    /// <summary>
    /// 设置世界边界大小
    /// </summary>
    /// <param name="distance">边界直径（方块数）</param>
    /// <param name="time">变化时间（秒，可选）</param>
    Task SetWorldBorderSizeAsync(double distance, int? time = null);

    // ========== 进度和配方相关 ==========
    
    /// <summary>
    /// 授予玩家进度
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="advancement">进度 ID</param>
    Task GrantAdvancementAsync(string targets, string advancement);

    /// <summary>
    /// 撤销玩家进度
    /// </summary>
    Task RevokeAdvancementAsync(string targets, string advancement);

    /// <summary>
    /// 解锁配方
    /// </summary>
    Task GrantRecipeAsync(string targets, string recipe);

    /// <summary>
    /// 锁定配方
    /// </summary>
    Task RevokeRecipeAsync(string targets, string recipe);

    // ========== 队伍管理相关 ==========
    
    /// <summary>
    /// 创建队伍
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    Task CreateTeamAsync(string teamName);

    /// <summary>
    /// 删除队伍
    /// </summary>
    Task RemoveTeamAsync(string teamName);

    /// <summary>
    /// 将玩家加入队伍
    /// </summary>
    Task AddToTeamAsync(string teamName, string members);

    /// <summary>
    /// 将玩家从队伍移除
    /// </summary>
    Task RemoveFromTeamAsync(string teamName, string members);

    /// <summary>
    /// 设置队伍选项
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="option">选项名（color/friendlyFire/seeFriendlyInvisibles等）</param>
    /// <param name="value">选项值</param>
    Task SetTeamOptionAsync(string teamName, string option, string value);

    // ========== 实用功能 ==========
    
    /// <summary>
    /// 执行函数
    /// </summary>
    /// <param name="functionId">函数 ID（命名空间格式）</param>
    Task ExecuteFunctionAsync(string functionId);

    /// <summary>
    /// 为实体添加标签
    /// </summary>
    Task AddTagAsync(string targets, string tag);

    /// <summary>
    /// 为实体移除标签
    /// </summary>
    Task RemoveTagAsync(string targets, string tag);

    /// <summary>
    /// 获取方块数据（返回 SNBT 格式）
    /// </summary>
    Task<string> GetBlockDataAsync(int x, int y, int z);

    /// <summary>
    /// 获取实体数据（返回 SNBT 格式）
    /// </summary>
    Task<string> GetEntityDataAsync(string target);

    // ========== 高级功能 ==========
    
    /// <summary>
    /// 执行自定义游戏命令
    /// </summary>
    Task<string> ExecuteCommandAsync(string command);
}

/// <summary>
/// BossBar 颜色
/// </summary>
public enum BossBarColor
{
    /// <summary>
    /// 蓝色
    /// </summary>
    Blue,
    
    /// <summary>
    /// 绿色
    /// </summary>
    Green,
    
    /// <summary>
    /// 粉色
    /// </summary>
    Pink,
    
    /// <summary>
    /// 紫色
    /// </summary>
    Purple,
    
    /// <summary>
    /// 红色
    /// </summary>
    Red,
    
    /// <summary>
    /// 白色
    /// </summary>
    White,
    
    /// <summary>
    /// 黄色
    /// </summary>
    Yellow
}

/// <summary>
/// BossBar 样式
/// </summary>
public enum BossBarStyle
{
    /// <summary>
    /// 无分段
    /// </summary>
    Progress,
    
    /// <summary>
    /// 6 个分段
    /// </summary>
    Notched_6,
    
    /// <summary>
    /// 10 个分段
    /// </summary>
    Notched_10,
    
    /// <summary>
    /// 12 个分段
    /// </summary>
    Notched_12,
    
    /// <summary>
    /// 20 个分段
    /// </summary>
    Notched_20
}

/// <summary>
/// Minecraft 文本颜色
/// </summary>
public enum TextColor
{
    /// <summary>
    /// 黑色
    /// </summary>
    Black,
    
    /// <summary>
    /// 深蓝色
    /// </summary>
    DarkBlue,
    
    /// <summary>
    /// 深绿色
    /// </summary>
    DarkGreen,
    
    /// <summary>
    /// 深青色
    /// </summary>
    DarkAqua,
    
    /// <summary>
    /// 深红色
    /// </summary>
    DarkRed,
    
    /// <summary>
    /// 深紫色
    /// </summary>
    DarkPurple,
    
    /// <summary>
    /// 金色
    /// </summary>
    Gold,
    
    /// <summary>
    /// 灰色
    /// </summary>
    Gray,
    
    /// <summary>
    /// 深灰色
    /// </summary>
    DarkGray,
    
    /// <summary>
    /// 蓝色
    /// </summary>
    Blue,
    
    /// <summary>
    /// 绿色
    /// </summary>
    Green,
    
    /// <summary>
    /// 青色
    /// </summary>
    Aqua,
    
    /// <summary>
    /// 红色
    /// </summary>
    Red,
    
    /// <summary>
    /// 浅紫色
    /// </summary>
    LightPurple,
    
    /// <summary>
    /// 黄色
    /// </summary>
    Yellow,
    
    /// <summary>
    /// 白色
    /// </summary>
    White
}
