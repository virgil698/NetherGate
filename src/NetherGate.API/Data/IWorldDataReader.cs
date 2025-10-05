namespace NetherGate.API.Data;

/// <summary>
/// 世界数据读取接口
/// 读取 Minecraft 世界信息
/// </summary>
public interface IWorldDataReader
{
    /// <summary>
    /// 读取世界信息
    /// </summary>
    /// <param name="worldName">世界名称（默认为主世界）</param>
    Task<WorldInfo?> ReadWorldInfoAsync(string? worldName = null);

    /// <summary>
    /// 列出所有世界
    /// </summary>
    List<string> ListWorlds();

    /// <summary>
    /// 获取世界大小
    /// </summary>
    /// <param name="worldName">世界名称</param>
    /// <returns>世界大小（MB）</returns>
    long GetWorldSize(string worldName);

    /// <summary>
    /// 读取世界种子
    /// </summary>
    /// <param name="worldName">世界名称</param>
    Task<long?> GetWorldSeedAsync(string worldName);

    /// <summary>
    /// 读取世界出生点
    /// </summary>
    /// <param name="worldName">世界名称</param>
    Task<SpawnPoint?> GetSpawnPointAsync(string worldName);

    /// <summary>
    /// 检查世界是否存在
    /// </summary>
    /// <param name="worldName">世界名称</param>
    bool WorldExists(string worldName);
}

/// <summary>
/// 世界信息
/// </summary>
public class WorldInfo
{
    /// <summary>
    /// 世界名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 世界路径
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 世界种子
    /// </summary>
    public long Seed { get; init; }

    /// <summary>
    /// 游戏版本
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// 世界类型
    /// </summary>
    public string LevelType { get; init; } = "default";

    /// <summary>
    /// 出生点
    /// </summary>
    public SpawnPoint SpawnPoint { get; init; } = new();

    /// <summary>
    /// 游戏时间（游戏内刻）
    /// </summary>
    public long GameTime { get; init; }

    /// <summary>
    /// 日期时间（游戏内刻）
    /// </summary>
    public long DayTime { get; init; }

    /// <summary>
    /// 是否允许作弊
    /// </summary>
    public bool AllowCommands { get; init; }

    /// <summary>
    /// 难度
    /// </summary>
    public Difficulty Difficulty { get; init; }

    /// <summary>
    /// 是否锁定难度
    /// </summary>
    public bool DifficultyLocked { get; init; }

    /// <summary>
    /// 世界大小（MB）
    /// </summary>
    public long SizeMB { get; init; }

    /// <summary>
    /// 世界边界中心
    /// </summary>
    public WorldBorder WorldBorder { get; init; } = new();

    /// <summary>
    /// 游戏规则
    /// </summary>
    public Dictionary<string, string> GameRules { get; init; } = new();

    /// <summary>
    /// 数据包
    /// </summary>
    public List<string> EnabledDataPacks { get; init; } = new();

    /// <summary>
    /// 最后游玩时间
    /// </summary>
    public DateTime? LastPlayed { get; init; }

    /// <summary>
    /// 是否下雨
    /// </summary>
    public bool Raining { get; init; }

    /// <summary>
    /// 是否打雷
    /// </summary>
    public bool Thundering { get; init; }
}

/// <summary>
/// 出生点
/// </summary>
public class SpawnPoint
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Z 坐标
    /// </summary>
    public int Z { get; init; }

    /// <summary>
    /// 出生角度
    /// </summary>
    public float Angle { get; init; }
}

/// <summary>
/// 世界边界
/// </summary>
public class WorldBorder
{
    /// <summary>
    /// 中心 X 坐标
    /// </summary>
    public double CenterX { get; init; }

    /// <summary>
    /// 中心 Z 坐标
    /// </summary>
    public double CenterZ { get; init; }

    /// <summary>
    /// 当前大小
    /// </summary>
    public double Size { get; init; }

    /// <summary>
    /// 目标大小
    /// </summary>
    public double SizeLerpTarget { get; init; }

    /// <summary>
    /// 伤害距离
    /// </summary>
    public double DamagePerBlock { get; init; }

    /// <summary>
    /// 警告距离
    /// </summary>
    public double WarningBlocks { get; init; }
}

/// <summary>
/// 难度
/// </summary>
public enum Difficulty
{
    /// <summary>
    /// 和平
    /// </summary>
    Peaceful = 0,

    /// <summary>
    /// 简单
    /// </summary>
    Easy = 1,

    /// <summary>
    /// 普通
    /// </summary>
    Normal = 2,

    /// <summary>
    /// 困难
    /// </summary>
    Hard = 3
}

