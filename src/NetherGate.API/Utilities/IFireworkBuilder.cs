namespace NetherGate.API.Utilities;

/// <summary>
/// 烟花构建器接口
/// 用于创建自定义烟花
/// </summary>
public interface IFireworkBuilder
{
    /// <summary>
    /// 设置烟花类型
    /// </summary>
    IFireworkBuilder WithType(FireworkType type);
    
    /// <summary>
    /// 添加颜色
    /// </summary>
    IFireworkBuilder WithColor(FireworkColor color);
    
    /// <summary>
    /// 添加多个颜色
    /// </summary>
    IFireworkBuilder WithColors(params FireworkColor[] colors);
    
    /// <summary>
    /// 设置淡出颜色
    /// </summary>
    IFireworkBuilder WithFadeColor(FireworkColor color);
    
    /// <summary>
    /// 设置多个淡出颜色
    /// </summary>
    IFireworkBuilder WithFadeColors(params FireworkColor[] colors);
    
    /// <summary>
    /// 设置飞行时间（1-3）
    /// </summary>
    IFireworkBuilder WithFlightDuration(int duration);
    
    /// <summary>
    /// 启用轨迹效果
    /// </summary>
    IFireworkBuilder WithTrail(bool hasTrail = true);
    
    /// <summary>
    /// 启用闪烁效果
    /// </summary>
    IFireworkBuilder WithTwinkle(bool hasTwinkle = true);
    
    /// <summary>
    /// 添加爆炸效果
    /// </summary>
    IFireworkBuilder AddExplosion(Action<IFireworkExplosionBuilder> configure);
    
    /// <summary>
    /// 使用随机颜色
    /// </summary>
    IFireworkBuilder WithRandomColors(int count = 3);
    
    /// <summary>
    /// 使用彩虹色
    /// </summary>
    IFireworkBuilder WithRainbowColors();
    
    /// <summary>
    /// 构建烟花 NBT 数据
    /// </summary>
    string Build();
}

/// <summary>
/// 烟花爆炸效果构建器
/// </summary>
public interface IFireworkExplosionBuilder
{
    /// <summary>
    /// 设置爆炸类型
    /// </summary>
    IFireworkExplosionBuilder WithType(FireworkType type);
    
    /// <summary>
    /// 添加颜色
    /// </summary>
    IFireworkExplosionBuilder WithColor(FireworkColor color);
    
    /// <summary>
    /// 添加多个颜色
    /// </summary>
    IFireworkExplosionBuilder WithColors(params FireworkColor[] colors);
    
    /// <summary>
    /// 设置淡出颜色
    /// </summary>
    IFireworkExplosionBuilder WithFadeColor(FireworkColor color);
    
    /// <summary>
    /// 设置多个淡出颜色
    /// </summary>
    IFireworkExplosionBuilder WithFadeColors(params FireworkColor[] colors);
    
    /// <summary>
    /// 启用轨迹
    /// </summary>
    IFireworkExplosionBuilder WithTrail(bool hasTrail = true);
    
    /// <summary>
    /// 启用闪烁
    /// </summary>
    IFireworkExplosionBuilder WithTwinkle(bool hasTwinkle = true);
}

