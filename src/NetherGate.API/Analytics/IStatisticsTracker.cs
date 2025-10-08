namespace NetherGate.API.Analytics;

/// <summary>
/// 统计数据追踪器接口 - 追踪玩家的游戏统计数据
/// </summary>
public interface IStatisticsTracker
{
    /// <summary>
    /// 获取玩家的统计数据
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <returns>玩家统计数据</returns>
    Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerUuid);

    /// <summary>
    /// 获取所有在线玩家的统计数据
    /// </summary>
    /// <returns>所有玩家的统计数据</returns>
    Task<Dictionary<string, PlayerStatistics>> GetAllPlayerStatisticsAsync();

    /// <summary>
    /// 获取指定统计项的值
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="statistic">统计项（例如：minecraft:mined.minecraft:stone）</param>
    /// <returns>统计值</returns>
    Task<long> GetStatisticValueAsync(string playerUuid, string statistic);

    /// <summary>
    /// 获取玩家的方块收集进度
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <returns>方块收集进度</returns>
    Task<BlockCollectionProgress> GetBlockProgressAsync(string playerUuid);

    /// <summary>
    /// 统计数据更新事件
    /// </summary>
    event EventHandler<StatisticsUpdatedEventArgs>? StatisticsUpdated;

    /// <summary>
    /// 启动统计追踪（监听文件变化）
    /// </summary>
    Task StartTrackingAsync();

    /// <summary>
    /// 停止统计追踪
    /// </summary>
    Task StopTrackingAsync();
}

/// <summary>
/// 玩家统计数据
/// </summary>
public class PlayerStatistics
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public required string PlayerUuid { get; set; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// 游戏时间（tick）
    /// </summary>
    public long PlayTime { get; set; }

    /// <summary>
    /// 游戏时间（TimeSpan）
    /// </summary>
    public TimeSpan PlayTimeSpan => TimeSpan.FromSeconds(PlayTime / 20.0);

    /// <summary>
    /// 移动距离（厘米）
    /// </summary>
    public Dictionary<string, long> TravelDistances { get; set; } = new();

    /// <summary>
    /// 破坏的方块（方块 ID -> 数量）
    /// </summary>
    public Dictionary<string, long> MinedBlocks { get; set; } = new();

    /// <summary>
    /// 使用的物品（物品 ID -> 数量）
    /// </summary>
    public Dictionary<string, long> UsedItems { get; set; } = new();

    /// <summary>
    /// 击杀的生物（生物 ID -> 数量）
    /// </summary>
    public Dictionary<string, long> KilledMobs { get; set; } = new();

    /// <summary>
    /// 被击杀次数（生物 ID -> 次数）
    /// </summary>
    public Dictionary<string, long> DeathsByMob { get; set; } = new();

    /// <summary>
    /// 自定义统计（统计 ID -> 值）
    /// </summary>
    public Dictionary<string, long> CustomStats { get; set; } = new();

    /// <summary>
    /// 总死亡次数
    /// </summary>
    public long TotalDeaths => DeathsByMob.Values.Sum();

    /// <summary>
    /// 总击杀数
    /// </summary>
    public long TotalKills => KilledMobs.Values.Sum();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始统计数据（JSON 格式）
    /// </summary>
    public Dictionary<string, object> RawData { get; set; } = new();
}

/// <summary>
/// 方块收集进度
/// </summary>
public class BlockCollectionProgress
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public required string PlayerUuid { get; set; }

    /// <summary>
    /// 已收集的方块列表
    /// </summary>
    public HashSet<string> CollectedBlocks { get; set; } = new();

    /// <summary>
    /// 总方块数量（根据 Minecraft 版本）
    /// </summary>
    public int TotalBlocks { get; set; }

    /// <summary>
    /// 已收集的方块数量
    /// </summary>
    public int CollectedCount => CollectedBlocks.Count;

    /// <summary>
    /// 收集进度百分比
    /// </summary>
    public double ProgressPercentage => TotalBlocks > 0 
        ? (double)CollectedCount / TotalBlocks * 100 
        : 0;

    /// <summary>
    /// 缺失的方块列表
    /// </summary>
    public HashSet<string> MissingBlocks { get; set; } = new();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 统计数据更新事件参数
/// </summary>
public class StatisticsUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public required string PlayerUuid { get; set; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// 更新的统计项列表
    /// </summary>
    public List<string> UpdatedStatistics { get; set; } = new();

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

