namespace NetherGate.API.Leaderboard;

/// <summary>
/// 排行榜系统接口
/// </summary>
public interface ILeaderboardSystem
{
    /// <summary>
    /// 创建新的排行榜
    /// </summary>
    /// <param name="name">排行榜名称</param>
    /// <param name="type">排行榜类型</param>
    /// <param name="displayName">显示名称（可选）</param>
    /// <returns>创建的排行榜</returns>
    Task<Leaderboard> CreateLeaderboardAsync(string name, LeaderboardType type, string? displayName = null);

    /// <summary>
    /// 删除排行榜
    /// </summary>
    /// <param name="name">排行榜名称</param>
    Task<bool> DeleteLeaderboardAsync(string name);

    /// <summary>
    /// 获取排行榜
    /// </summary>
    /// <param name="name">排行榜名称</param>
    /// <returns>排行榜对象</returns>
    Task<Leaderboard?> GetLeaderboardAsync(string name);

    /// <summary>
    /// 获取所有排行榜
    /// </summary>
    /// <returns>所有排行榜列表</returns>
    Task<List<Leaderboard>> GetAllLeaderboardsAsync();

    /// <summary>
    /// 更新玩家分数
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="score">分数</param>
    /// <param name="playerName">玩家名称（可选）</param>
    Task UpdateScoreAsync(string leaderboardName, string playerUuid, double score, string? playerName = null);

    /// <summary>
    /// 增加玩家分数
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="increment">增加的分数</param>
    /// <param name="playerName">玩家名称（可选）</param>
    Task IncrementScoreAsync(string leaderboardName, string playerUuid, double increment, string? playerName = null);

    /// <summary>
    /// 获取玩家分数
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <returns>玩家分数</returns>
    Task<double> GetPlayerScoreAsync(string leaderboardName, string playerUuid);

    /// <summary>
    /// 获取玩家排名
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <returns>排名（1-based）</returns>
    Task<int> GetPlayerRankAsync(string leaderboardName, string playerUuid);

    /// <summary>
    /// 获取前 N 名玩家
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    /// <param name="count">数量</param>
    /// <returns>排行榜条目列表</returns>
    Task<List<LeaderboardEntry>> GetTopPlayersAsync(string leaderboardName, int count = 10);

    /// <summary>
    /// 获取排行榜中的所有玩家
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    /// <returns>所有排行榜条目</returns>
    Task<List<LeaderboardEntry>> GetAllEntriesAsync(string leaderboardName);

    /// <summary>
    /// 重置排行榜
    /// </summary>
    /// <param name="leaderboardName">排行榜名称</param>
    Task ResetLeaderboardAsync(string leaderboardName);

    /// <summary>
    /// 分数更新事件
    /// </summary>
    event EventHandler<ScoreUpdatedEventArgs>? ScoreUpdated;

    /// <summary>
    /// 排名变化事件
    /// </summary>
    event EventHandler<RankChangedEventArgs>? RankChanged;
}

/// <summary>
/// 排行榜类型
/// </summary>
public enum LeaderboardType
{
    /// <summary>
    /// 最高分（分数越高越好）
    /// </summary>
    HighestScore,

    /// <summary>
    /// 最低分（分数越低越好，如速通时间）
    /// </summary>
    LowestScore,

    /// <summary>
    /// 累计分数
    /// </summary>
    Accumulative
}

/// <summary>
/// 排行榜
/// </summary>
public class Leaderboard
{
    /// <summary>
    /// 排行榜名称（唯一标识）
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 排行榜类型
    /// </summary>
    public LeaderboardType Type { get; set; }

    /// <summary>
    /// 排行榜条目
    /// </summary>
    public List<LeaderboardEntry> Entries { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 额外元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 排行榜条目
/// </summary>
public class LeaderboardEntry
{
    /// <summary>
    /// 排名（1-based）
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public required string PlayerUuid { get; set; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// 分数
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 额外数据
    /// </summary>
    public Dictionary<string, object> ExtraData { get; set; } = new();
}

/// <summary>
/// 分数更新事件参数
/// </summary>
public class ScoreUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// 排行榜名称
    /// </summary>
    public required string LeaderboardName { get; set; }

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public required string PlayerUuid { get; set; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// 旧分数
    /// </summary>
    public double OldScore { get; set; }

    /// <summary>
    /// 新分数
    /// </summary>
    public double NewScore { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 排名变化事件参数
/// </summary>
public class RankChangedEventArgs : EventArgs
{
    /// <summary>
    /// 排行榜名称
    /// </summary>
    public required string LeaderboardName { get; set; }

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public required string PlayerUuid { get; set; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// 旧排名
    /// </summary>
    public int OldRank { get; set; }

    /// <summary>
    /// 新排名
    /// </summary>
    public int NewRank { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

