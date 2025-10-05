namespace NetherGate.API.Data;

/// <summary>
/// 玩家数据读取接口
/// 读取 Minecraft 玩家数据文件（NBT 格式）
/// </summary>
public interface IPlayerDataReader
{
    /// <summary>
    /// 读取玩家数据
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    Task<PlayerData?> ReadPlayerDataAsync(Guid playerUuid);

    /// <summary>
    /// 读取玩家统计数据
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    Task<PlayerStats?> ReadPlayerStatsAsync(Guid playerUuid);

    /// <summary>
    /// 读取玩家成就进度
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    Task<PlayerAdvancements?> ReadPlayerAdvancementsAsync(Guid playerUuid);

    /// <summary>
    /// 列出所有玩家
    /// </summary>
    /// <param name="worldName">世界名称（默认为主世界）</param>
    List<Guid> ListPlayers(string? worldName = null);

    /// <summary>
    /// 获取在线玩家数据
    /// </summary>
    List<PlayerData> GetOnlinePlayers();

    /// <summary>
    /// 检查玩家数据是否存在
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    bool PlayerDataExists(Guid playerUuid);
}

/// <summary>
/// 玩家数据
/// </summary>
public class PlayerData
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid Uuid { get; init; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 生命值
    /// </summary>
    public float Health { get; init; }

    /// <summary>
    /// 饱食度
    /// </summary>
    public int FoodLevel { get; init; }

    /// <summary>
    /// 经验等级
    /// </summary>
    public int XpLevel { get; init; }

    /// <summary>
    /// 总经验
    /// </summary>
    public int XpTotal { get; init; }

    /// <summary>
    /// 游戏模式
    /// </summary>
    public GameMode GameMode { get; init; }

    /// <summary>
    /// 位置
    /// </summary>
    public PlayerPosition Position { get; init; } = new();

    /// <summary>
    /// 背包物品
    /// </summary>
    public List<ItemStack> Inventory { get; init; } = new();

    /// <summary>
    /// 末影箱物品
    /// </summary>
    public List<ItemStack> EnderChest { get; init; } = new();

    /// <summary>
    /// 装备物品
    /// </summary>
    public PlayerArmor Armor { get; init; } = new();

    /// <summary>
    /// 玩家效果
    /// </summary>
    public List<StatusEffect> Effects { get; init; } = new();

    /// <summary>
    /// 最后上线时间
    /// </summary>
    public DateTime? LastPlayed { get; init; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; init; }
}

/// <summary>
/// 玩家位置
/// </summary>
public class PlayerPosition
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Z 坐标
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// 维度
    /// </summary>
    public string Dimension { get; init; } = "minecraft:overworld";

    /// <summary>
    /// 偏航角
    /// </summary>
    public float Yaw { get; init; }

    /// <summary>
    /// 俯仰角
    /// </summary>
    public float Pitch { get; init; }
}

/// <summary>
/// 物品堆
/// </summary>
public class ItemStack
{
    /// <summary>
    /// 物品 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 数量
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// 槽位
    /// </summary>
    public int Slot { get; init; }

    /// <summary>
    /// 附魔
    /// </summary>
    public List<Enchantment> Enchantments { get; init; } = new();

    /// <summary>
    /// 自定义名称
    /// </summary>
    public string? CustomName { get; init; }
}

/// <summary>
/// 附魔
/// </summary>
public class Enchantment
{
    /// <summary>
    /// 附魔 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 等级
    /// </summary>
    public int Level { get; init; }
}

/// <summary>
/// 玩家护甲
/// </summary>
public class PlayerArmor
{
    /// <summary>
    /// 头盔
    /// </summary>
    public ItemStack? Helmet { get; init; }

    /// <summary>
    /// 胸甲
    /// </summary>
    public ItemStack? Chestplate { get; init; }

    /// <summary>
    /// 护腿
    /// </summary>
    public ItemStack? Leggings { get; init; }

    /// <summary>
    /// 靴子
    /// </summary>
    public ItemStack? Boots { get; init; }
}

/// <summary>
/// 状态效果
/// </summary>
public class StatusEffect
{
    /// <summary>
    /// 效果 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 等级
    /// </summary>
    public int Amplifier { get; init; }

    /// <summary>
    /// 剩余时间（秒）
    /// </summary>
    public int Duration { get; init; }
}

/// <summary>
/// 玩家统计数据
/// </summary>
public class PlayerStats
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid Uuid { get; init; }

    /// <summary>
    /// 游玩时间（分钟）
    /// </summary>
    public int PlayTimeMinutes { get; init; }

    /// <summary>
    /// 击杀生物数
    /// </summary>
    public Dictionary<string, int> MobKills { get; init; } = new();

    /// <summary>
    /// 死亡次数
    /// </summary>
    public int Deaths { get; init; }

    /// <summary>
    /// 跳跃次数
    /// </summary>
    public int Jumps { get; init; }

    /// <summary>
    /// 行走距离（米）
    /// </summary>
    public double DistanceWalked { get; init; }

    /// <summary>
    /// 飞行距离（米）
    /// </summary>
    public double DistanceFlown { get; init; }

    /// <summary>
    /// 破坏方块数
    /// </summary>
    public Dictionary<string, int> BlocksMined { get; init; } = new();

    /// <summary>
    /// 使用物品数
    /// </summary>
    public Dictionary<string, int> ItemsUsed { get; init; } = new();

    /// <summary>
    /// 自定义统计
    /// </summary>
    public Dictionary<string, int> CustomStats { get; init; } = new();
}

/// <summary>
/// 玩家成就进度
/// </summary>
public class PlayerAdvancements
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid Uuid { get; init; }

    /// <summary>
    /// 已完成的成就
    /// </summary>
    public List<CompletedAdvancement> Completed { get; init; } = new();

    /// <summary>
    /// 进行中的成就
    /// </summary>
    public List<AdvancementProgress> InProgress { get; init; } = new();

    /// <summary>
    /// 完成百分比
    /// </summary>
    public double CompletionPercent { get; init; }
}

/// <summary>
/// 已完成的成就
/// </summary>
public class CompletedAdvancement
{
    /// <summary>
    /// 成就 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// 成就进度
/// </summary>
public class AdvancementProgress
{
    /// <summary>
    /// 成就 ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 已完成的标准
    /// </summary>
    public List<string> CompletedCriteria { get; init; } = new();

    /// <summary>
    /// 总标准数
    /// </summary>
    public int TotalCriteria { get; init; }

    /// <summary>
    /// 完成百分比
    /// </summary>
    public double ProgressPercent => TotalCriteria > 0 
        ? (double)CompletedCriteria.Count / TotalCriteria * 100 
        : 0;
}

/// <summary>
/// 游戏模式
/// </summary>
public enum GameMode
{
    /// <summary>
    /// 生存模式
    /// </summary>
    Survival = 0,

    /// <summary>
    /// 创造模式
    /// </summary>
    Creative = 1,

    /// <summary>
    /// 冒险模式
    /// </summary>
    Adventure = 2,

    /// <summary>
    /// 旁观模式
    /// </summary>
    Spectator = 3
}

