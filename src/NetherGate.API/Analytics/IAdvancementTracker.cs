namespace NetherGate.API.Analytics;

/// <summary>
/// 成就追踪器接口 - 追踪玩家的成就进度
/// </summary>
public interface IAdvancementTracker
{
    /// <summary>
    /// 获取玩家的成就数据
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <returns>玩家成就数据</returns>
    Task<PlayerAdvancementData?> GetPlayerAdvancementsAsync(string playerUuid);

    /// <summary>
    /// 获取所有在线玩家的成就数据
    /// </summary>
    /// <returns>所有玩家的成就数据</returns>
    Task<Dictionary<string, PlayerAdvancementData>> GetAllPlayerAdvancementsAsync();

    /// <summary>
    /// 检查玩家是否完成了指定成就
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="advancementId">成就 ID（例如：minecraft:story/mine_stone）</param>
    /// <returns>是否完成</returns>
    Task<bool> HasCompletedAdvancementAsync(string playerUuid, string advancementId);

    /// <summary>
    /// 获取玩家的成就完成进度（百分比）
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="category">成就类别（可选，如 "story", "nether", "end"）</param>
    /// <returns>完成百分比（0-100）</returns>
    Task<double> GetAdvancementProgressAsync(string playerUuid, string? category = null);

    /// <summary>
    /// 成就完成事件
    /// </summary>
    event EventHandler<AdvancementCompletedEventArgs>? AdvancementCompleted;

    /// <summary>
    /// 启动成就追踪（监听文件变化）
    /// </summary>
    Task StartTrackingAsync();

    /// <summary>
    /// 停止成就追踪
    /// </summary>
    Task StopTrackingAsync();
}

/// <summary>
/// 玩家成就数据
/// </summary>
public class PlayerAdvancementData
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
    /// 已完成的成就列表（成就 ID -> 完成时间）
    /// </summary>
    public Dictionary<string, DateTime> CompletedAdvancements { get; set; } = new();

    /// <summary>
    /// 进行中的成就（成就 ID -> 完成的条件）
    /// </summary>
    public Dictionary<string, Dictionary<string, bool>> InProgressAdvancements { get; set; } = new();

    /// <summary>
    /// 总成就数量
    /// </summary>
    public int TotalAdvancements { get; set; }

    /// <summary>
    /// 已完成的成就数量
    /// </summary>
    public int CompletedCount => CompletedAdvancements.Count;

    /// <summary>
    /// 完成百分比
    /// </summary>
    public double ProgressPercentage => TotalAdvancements > 0 
        ? (double)CompletedCount / TotalAdvancements * 100 
        : 0;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 成就完成事件参数
/// </summary>
public class AdvancementCompletedEventArgs : EventArgs
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
    /// 成就 ID
    /// </summary>
    public required string AdvancementId { get; set; }

    /// <summary>
    /// 成就名称
    /// </summary>
    public string? AdvancementName { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

