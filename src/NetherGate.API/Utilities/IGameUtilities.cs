using NetherGate.API.Data.Models;

namespace NetherGate.API.Utilities;

/// <summary>
/// 游戏实用工具接口
/// 提供高层便捷的游戏操作 API
/// </summary>
public interface IGameUtilities
{
    // ========== 时间便捷操作 ==========
    
    /// <summary>
    /// 设置为白天
    /// </summary>
    Task SetDayAsync();
    
    /// <summary>
    /// 设置为夜晚
    /// </summary>
    Task SetNightAsync();
    
    /// <summary>
    /// 设置为正午
    /// </summary>
    Task SetNoonAsync();
    
    /// <summary>
    /// 设置为午夜
    /// </summary>
    Task SetMidnightAsync();
    
    /// <summary>
    /// 设置为日出
    /// </summary>
    Task SetSunriseAsync();
    
    /// <summary>
    /// 设置为日落
    /// </summary>
    Task SetSunsetAsync();
    
    // ========== 天气便捷操作 ==========
    
    /// <summary>
    /// 设置晴天
    /// </summary>
    Task SetClearWeatherAsync(int? durationSeconds = null);
    
    /// <summary>
    /// 设置下雨
    /// </summary>
    Task SetRainAsync(int? durationSeconds = null);
    
    /// <summary>
    /// 设置雷暴
    /// </summary>
    Task SetThunderAsync(int? durationSeconds = null);
    
    // ========== 烟花操作 ==========
    
    /// <summary>
    /// 发射烟花
    /// </summary>
    /// <param name="pos">发射位置</param>
    /// <param name="type">烟花类型</param>
    /// <param name="colors">烟花颜色</param>
    /// <param name="fadeColors">淡出颜色</param>
    /// <param name="flightDuration">飞行时间（1-3）</param>
    /// <param name="hasTrail">是否有轨迹</param>
    /// <param name="hasTwinkle">是否闪烁</param>
    Task LaunchFireworkAsync(
        Position pos,
        FireworkType type = FireworkType.LargeBall,
        List<FireworkColor>? colors = null,
        List<FireworkColor>? fadeColors = null,
        int flightDuration = 2,
        bool hasTrail = false,
        bool hasTwinkle = false);
    
    /// <summary>
    /// 发射随机烟花
    /// </summary>
    Task LaunchRandomFireworkAsync(Position pos, int flightDuration = 2);
    
    /// <summary>
    /// 发射烟花秀（多个烟花）
    /// </summary>
    /// <param name="centerPos">中心位置</param>
    /// <param name="count">烟花数量</param>
    /// <param name="radius">发射半径</param>
    /// <param name="delayMs">每个烟花之间的延迟（毫秒）</param>
    Task LaunchFireworkShowAsync(Position centerPos, int count = 10, double radius = 5.0, int delayMs = 200);
    
    /// <summary>
    /// 使用烟花构建器创建自定义烟花
    /// </summary>
    Task LaunchCustomFireworkAsync(Position pos, Action<IFireworkBuilder> configure);
    
    // ========== 声音便捷操作 ==========
    
    /// <summary>
    /// 播放预设声音
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="sound">声音类型</param>
    /// <param name="pos">位置（可选）</param>
    /// <param name="volume">音量</param>
    /// <param name="pitch">音调</param>
    Task PlaySoundAsync(
        string targets,
        MinecraftSound sound,
        Position? pos = null,
        double volume = 1.0,
        double pitch = 1.0);
    
    /// <summary>
    /// 播放音符
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="note">音符</param>
    /// <param name="instrument">乐器</param>
    /// <param name="pos">位置（可选）</param>
    Task PlayNoteAsync(
        string targets,
        Note note,
        Instrument instrument = Instrument.Piano,
        Position? pos = null);
    
    /// <summary>
    /// 播放旋律序列
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="melody">音符序列（音符，延迟毫秒）</param>
    /// <param name="instrument">乐器</param>
    /// <param name="pos">位置（可选）</param>
    Task PlayMelodyAsync(
        string targets,
        IEnumerable<(Note note, int delayMs)> melody,
        Instrument instrument = Instrument.Piano,
        Position? pos = null);
    
    /// <summary>
    /// 播放预设旋律
    /// </summary>
    Task PlayPredefinedMelodyAsync(
        string targets,
        PredefinedMelody melody,
        Position? pos = null);
    
    // ========== 区域操作 ==========
    
    /// <summary>
    /// 清空区域（填充空气）
    /// </summary>
    Task ClearAreaAsync(Position from, Position to);
    
    /// <summary>
    /// 填充区域
    /// </summary>
    Task FillAreaAsync(Position from, Position to, string block);
    
    /// <summary>
    /// 克隆区域
    /// </summary>
    Task CloneAreaAsync(Position from, Position to, Position destination, CloneMode mode = CloneMode.Replace);
    
    /// <summary>
    /// 创建空心立方体
    /// </summary>
    Task CreateHollowCubeAsync(Position from, Position to, string block);
    
    /// <summary>
    /// 创建球体
    /// </summary>
    Task CreateSphereAsync(Position center, double radius, string block, bool hollow = false);
    
    /// <summary>
    /// 创建圆柱体
    /// </summary>
    Task CreateCylinderAsync(Position base1, Position base2, double radius, string block, bool hollow = false);
    
    // ========== 粒子效果 ==========
    
    /// <summary>
    /// 在两点之间创建粒子线
    /// </summary>
    Task CreateParticleLineAsync(Position from, Position to, string particle, int density = 10);
    
    /// <summary>
    /// 创建粒子圆圈
    /// </summary>
    Task CreateParticleCircleAsync(Position center, double radius, string particle, int points = 20);
    
    /// <summary>
    /// 创建粒子球体
    /// </summary>
    Task CreateParticleSphereAsync(Position center, double radius, string particle, int density = 50);
    
    /// <summary>
    /// 创建粒子螺旋
    /// </summary>
    Task CreateParticleSpiralAsync(Position start, double height, double radius, string particle, int turns = 3, int pointsPerTurn = 20);
    
    // ========== 传送便捷操作 ==========
    
    /// <summary>
    /// 传送到世界出生点
    /// </summary>
    Task TeleportToSpawnAsync(string targets);
    
    /// <summary>
    /// 传送到玩家床位置
    /// </summary>
    Task TeleportToBedAsync(string targets);
    
    /// <summary>
    /// 随机传送（在指定范围内）
    /// </summary>
    Task RandomTeleportAsync(string targets, Position center, double radius);
    
    /// <summary>
    /// 将所有玩家传送到一起
    /// </summary>
    Task GatherPlayersAsync(Position location);
    
    // ========== 批量玩家操作 ==========
    
    /// <summary>
    /// 治疗所有玩家
    /// </summary>
    Task HealAllPlayersAsync();
    
    /// <summary>
    /// 喂饱所有玩家
    /// </summary>
    Task FeedAllPlayersAsync();
    
    /// <summary>
    /// 给所有玩家清除所有效果
    /// </summary>
    Task ClearAllEffectsAsync(string targets = "@a");
    
    /// <summary>
    /// 给所有玩家完整装备
    /// </summary>
    Task GiveFullArmorAsync(string targets, ArmorMaterial material = ArmorMaterial.Diamond);
    
    // ========== 实用功能 ==========
    
    /// <summary>
    /// 创建命令序列（流式 API）
    /// </summary>
    ICommandSequence CreateSequence();
    
    /// <summary>
    /// 延迟执行（异步等待）
    /// </summary>
    /// <param name="milliseconds">延迟毫秒数</param>
    Task DelayAsync(int milliseconds);
    
    /// <summary>
    /// 延迟执行（按游戏刻）
    /// </summary>
    /// <param name="ticks">游戏刻数（1 tick = 50ms）</param>
    Task DelayTicksAsync(int ticks);
    
    /// <summary>
    /// 获取随机位置（在区域内）
    /// </summary>
    Position GetRandomPosition(Position min, Position max);
    
    /// <summary>
    /// 获取随机颜色
    /// </summary>
    FireworkColor GetRandomColor();
}

/// <summary>
/// 克隆模式
/// </summary>
public enum CloneMode
{
    /// <summary>
    /// 替换（包括空气）
    /// </summary>
    Replace,
    
    /// <summary>
    /// 仅遮罩（不复制空气）
    /// </summary>
    Masked,
    
    /// <summary>
    /// 移动（删除源区域）
    /// </summary>
    Move
}

/// <summary>
/// 盔甲材质
/// </summary>
public enum ArmorMaterial
{
    /// <summary>
    /// 皮革
    /// </summary>
    Leather,
    
    /// <summary>
    /// 锁链
    /// </summary>
    Chainmail,
    
    /// <summary>
    /// 铁
    /// </summary>
    Iron,
    
    /// <summary>
    /// 金
    /// </summary>
    Gold,
    
    /// <summary>
    /// 钻石
    /// </summary>
    Diamond,
    
    /// <summary>
    /// 下界合金
    /// </summary>
    Netherite
}

