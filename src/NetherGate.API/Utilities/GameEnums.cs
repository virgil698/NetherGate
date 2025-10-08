namespace NetherGate.API.Utilities;

/// <summary>
/// Minecraft 声音枚举
/// 包含常用的游戏内声音效果
/// </summary>
public enum MinecraftSound
{
    // ========== 音符方块 ==========
    
    /// <summary>
    /// 钟声
    /// </summary>
    Bell,
    
    /// <summary>
    /// 低音鼓
    /// </summary>
    BaseDrum,
    
    /// <summary>
    /// 小军鼓
    /// </summary>
    Snare,
    
    /// <summary>
    /// 击鼓边
    /// </summary>
    Hat,
    
    /// <summary>
    /// 牛铃
    /// </summary>
    CowBell,
    
    /// <summary>
    /// 钢琴
    /// </summary>
    Piano,
    
    /// <summary>
    /// 低音提琴
    /// </summary>
    Bass,
    
    /// <summary>
    /// 吉他
    /// </summary>
    Guitar,
    
    /// <summary>
    /// 长笛
    /// </summary>
    Flute,
    
    /// <summary>
    /// 钟琴
    /// </summary>
    Chime,
    
    /// <summary>
    /// 木琴
    /// </summary>
    Xylophone,
    
    // ========== 环境音效 ==========
    
    /// <summary>
    /// 玩家升级
    /// </summary>
    PlayerLevelUp,
    
    /// <summary>
    /// 经验球收集
    /// </summary>
    ExperienceOrb,
    
    /// <summary>
    /// 铁砧放置
    /// </summary>
    AnvilPlace,
    
    /// <summary>
    /// 铁砧使用
    /// </summary>
    AnvilUse,
    
    /// <summary>
    /// 铁砧损坏
    /// </summary>
    AnvilBreak,
    
    /// <summary>
    /// 箱子打开
    /// </summary>
    ChestOpen,
    
    /// <summary>
    /// 箱子关闭
    /// </summary>
    ChestClose,
    
    /// <summary>
    /// 门开启
    /// </summary>
    DoorOpen,
    
    /// <summary>
    /// 门关闭
    /// </summary>
    DoorClose,
    
    /// <summary>
    /// 按钮点击
    /// </summary>
    ButtonClick,
    
    /// <summary>
    /// 拉杆切换
    /// </summary>
    LeverClick,
    
    // ========== 实体音效 ==========
    
    /// <summary>
    /// 末影龙死亡
    /// </summary>
    EnderDragonDeath,
    
    /// <summary>
    /// 凋灵生成
    /// </summary>
    WitherSpawn,
    
    /// <summary>
    /// 凋灵死亡
    /// </summary>
    WitherDeath,
    
    /// <summary>
    /// 村民交易
    /// </summary>
    VillagerYes,
    
    /// <summary>
    /// 村民拒绝
    /// </summary>
    VillagerNo,
    
    /// <summary>
    /// 僵尸呻吟
    /// </summary>
    ZombieAmbient,
    
    /// <summary>
    /// 骷髅射箭
    /// </summary>
    SkeletonShoot,
    
    /// <summary>
    /// 苦力怕嘶嘶声
    /// </summary>
    CreeperPrimed,
    
    /// <summary>
    /// 末影人传送
    /// </summary>
    EndermanTeleport,
    
    // ========== 物品音效 ==========
    
    /// <summary>
    /// 烟花发射
    /// </summary>
    FireworkLaunch,
    
    /// <summary>
    /// 烟花爆炸
    /// </summary>
    FireworkBlast,
    
    /// <summary>
    /// 附魔
    /// </summary>
    EnchantmentTable,
    
    /// <summary>
    /// 药水饮用
    /// </summary>
    DrinkPotion,
    
    /// <summary>
    /// 吃食物
    /// </summary>
    Eat,
    
    /// <summary>
    /// 打嗝
    /// </summary>
    Burp,
    
    // ========== 天气音效 ==========
    
    /// <summary>
    /// 雷声
    /// </summary>
    Thunder,
    
    /// <summary>
    /// 雨声
    /// </summary>
    Rain
}

/// <summary>
/// 烟花类型
/// </summary>
public enum FireworkType
{
    /// <summary>
    /// 小型球状
    /// </summary>
    SmallBall = 0,
    
    /// <summary>
    /// 大型球状
    /// </summary>
    LargeBall = 1,
    
    /// <summary>
    /// 星形
    /// </summary>
    Star = 2,
    
    /// <summary>
    /// 苦力怕形状
    /// </summary>
    Creeper = 3,
    
    /// <summary>
    /// 爆裂
    /// </summary>
    Burst = 4
}

/// <summary>
/// 烟花颜色
/// </summary>
public enum FireworkColor
{
    /// <summary>
    /// 黑色
    /// </summary>
    Black = 0,
    
    /// <summary>
    /// 红色
    /// </summary>
    Red = 1,
    
    /// <summary>
    /// 绿色
    /// </summary>
    Green = 2,
    
    /// <summary>
    /// 棕色
    /// </summary>
    Brown = 3,
    
    /// <summary>
    /// 蓝色
    /// </summary>
    Blue = 4,
    
    /// <summary>
    /// 紫色
    /// </summary>
    Purple = 5,
    
    /// <summary>
    /// 青色
    /// </summary>
    Cyan = 6,
    
    /// <summary>
    /// 淡灰色
    /// </summary>
    LightGray = 7,
    
    /// <summary>
    /// 灰色
    /// </summary>
    Gray = 8,
    
    /// <summary>
    /// 粉红色
    /// </summary>
    Pink = 9,
    
    /// <summary>
    /// 黄绿色
    /// </summary>
    Lime = 10,
    
    /// <summary>
    /// 黄色
    /// </summary>
    Yellow = 11,
    
    /// <summary>
    /// 淡蓝色
    /// </summary>
    LightBlue = 12,
    
    /// <summary>
    /// 品红色
    /// </summary>
    Magenta = 13,
    
    /// <summary>
    /// 橙色
    /// </summary>
    Orange = 14,
    
    /// <summary>
    /// 白色
    /// </summary>
    White = 15
}

/// <summary>
/// 音符
/// </summary>
public enum Note
{
    /// <summary>
    /// F# (升F)
    /// </summary>
    FSharp = 0,
    
    /// <summary>
    /// G (G)
    /// </summary>
    G = 1,
    
    /// <summary>
    /// G# (升G)
    /// </summary>
    GSharp = 2,
    
    /// <summary>
    /// A (A)
    /// </summary>
    A = 3,
    
    /// <summary>
    /// A# (升A)
    /// </summary>
    ASharp = 4,
    
    /// <summary>
    /// B (B)
    /// </summary>
    B = 5,
    
    /// <summary>
    /// C (C)
    /// </summary>
    C = 6,
    
    /// <summary>
    /// C# (升C)
    /// </summary>
    CSharp = 7,
    
    /// <summary>
    /// D (D)
    /// </summary>
    D = 8,
    
    /// <summary>
    /// D# (升D)
    /// </summary>
    DSharp = 9,
    
    /// <summary>
    /// E (E)
    /// </summary>
    E = 10,
    
    /// <summary>
    /// F (F)
    /// </summary>
    F = 11,
    
    /// <summary>
    /// F# 高八度 (升F)
    /// </summary>
    FSharpHigh = 12,
    
    /// <summary>
    /// G 高八度 (G)
    /// </summary>
    GHigh = 13,
    
    /// <summary>
    /// G# 高八度 (升G)
    /// </summary>
    GSharpHigh = 14,
    
    /// <summary>
    /// A 高八度 (A)
    /// </summary>
    AHigh = 15,
    
    /// <summary>
    /// A# 高八度 (升A)
    /// </summary>
    ASharpHigh = 16,
    
    /// <summary>
    /// B 高八度 (B)
    /// </summary>
    BHigh = 17,
    
    /// <summary>
    /// C 高八度 (C)
    /// </summary>
    CHigh = 18,
    
    /// <summary>
    /// C# 高八度 (升C)
    /// </summary>
    CSharpHigh = 19,
    
    /// <summary>
    /// D 高八度 (D)
    /// </summary>
    DHigh = 20,
    
    /// <summary>
    /// D# 高八度 (升D)
    /// </summary>
    DSharpHigh = 21,
    
    /// <summary>
    /// E 高八度 (E)
    /// </summary>
    EHigh = 22,
    
    /// <summary>
    /// F 高八度 (F)
    /// </summary>
    FHigh = 23,
    
    /// <summary>
    /// F# 最高八度 (升F)
    /// </summary>
    FSharpHighest = 24
}

/// <summary>
/// 乐器类型
/// </summary>
public enum Instrument
{
    /// <summary>
    /// 钢琴（音符方块下方为任何方块）
    /// </summary>
    Piano,
    
    /// <summary>
    /// 低音提琴（音符方块下方为木头）
    /// </summary>
    Bass,
    
    /// <summary>
    /// 低音鼓（音符方块下方为石头）
    /// </summary>
    BaseDrum,
    
    /// <summary>
    /// 小军鼓（音符方块下方为沙子）
    /// </summary>
    Snare,
    
    /// <summary>
    /// 击鼓边（音符方块下方为玻璃）
    /// </summary>
    Hat,
    
    /// <summary>
    /// 吉他（音符方块下方为羊毛）
    /// </summary>
    Guitar,
    
    /// <summary>
    /// 长笛（音符方块下方为粘土）
    /// </summary>
    Flute,
    
    /// <summary>
    /// 钟（音符方块下方为金块）
    /// </summary>
    Bell,
    
    /// <summary>
    /// 钟琴（音符方块下方为冰）
    /// </summary>
    Chime,
    
    /// <summary>
    /// 木琴（音符方块下方为骨块）
    /// </summary>
    Xylophone,
    
    /// <summary>
    /// 铁木琴（音符方块下方为铁块）
    /// </summary>
    IronXylophone,
    
    /// <summary>
    /// 牛铃（音符方块下方为灵魂沙）
    /// </summary>
    CowBell,
    
    /// <summary>
    /// 迪吉里杜管（音符方块下方为南瓜）
    /// </summary>
    Didgeridoo,
    
    /// <summary>
    /// 比特（音符方块下方为绿宝石块）
    /// </summary>
    Bit,
    
    /// <summary>
    /// 班卓琴（音符方块下方为干草块）
    /// </summary>
    Banjo,
    
    /// <summary>
    /// 电钢琴（音符方块下方为荧石）
    /// </summary>
    Pling
}

/// <summary>
/// 物品排序模式
/// </summary>
public enum ItemSortMode
{
    /// <summary>
    /// 按 ID 排序
    /// </summary>
    ById,
    
    /// <summary>
    /// 按数量排序
    /// </summary>
    ByCount,
    
    /// <summary>
    /// 按自定义名称排序
    /// </summary>
    ByCustomName,
    
    /// <summary>
    /// 按槽位排序
    /// </summary>
    BySlot
}

/// <summary>
/// 预设旋律
/// </summary>
public enum PredefinedMelody
{
    /// <summary>
    /// 胜利音效
    /// </summary>
    Victory,
    
    /// <summary>
    /// 失败音效
    /// </summary>
    Defeat,
    
    /// <summary>
    /// 欢迎音效
    /// </summary>
    Welcome,
    
    /// <summary>
    /// 通知音效
    /// </summary>
    Notification,
    
    /// <summary>
    /// 警告音效
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误音效
    /// </summary>
    Error,
    
    /// <summary>
    /// 成功音效
    /// </summary>
    Success
}

