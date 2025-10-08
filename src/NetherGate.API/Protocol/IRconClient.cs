namespace NetherGate.API.Protocol;

/// <summary>
/// RCON 客户端接口
/// 用于执行 Minecraft 服务器命令
/// </summary>
public interface IRconClient : IDisposable
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接到 RCON 服务器
    /// </summary>
    Task<bool> ConnectAsync();

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="command">要执行的命令</param>
    /// <returns>命令执行结果</returns>
    Task<string> ExecuteCommandAsync(string command);

    /// <summary>
    /// 连接状态变更事件
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// 错误发生事件
    /// </summary>
    event EventHandler<Exception>? ErrorOccurred;
}

/// <summary>
/// RCON 性能数据扩展接口
/// 用于获取服务器性能指标（TPS/MSPT）
/// </summary>
public interface IRconPerformance
{
    /// <summary>
    /// 获取 TPS（需要 Paper/Purpur 服务器）
    /// </summary>
    Task<TpsData?> GetTpsAsync();

    /// <summary>
    /// 获取 MSPT（需要 Paper 服务器）
    /// </summary>
    Task<MsptData?> GetMsptAsync();

    /// <summary>
    /// 检测服务器类型
    /// </summary>
    Task<ServerType> DetectServerTypeAsync();
}

/// <summary>
/// TPS 数据
/// </summary>
public record TpsData
{
    /// <summary>
    /// 最近 1 分钟 TPS
    /// </summary>
    public double Tps1m { get; init; }

    /// <summary>
    /// 最近 5 分钟 TPS
    /// </summary>
    public double Tps5m { get; init; }

    /// <summary>
    /// 最近 15 分钟 TPS
    /// </summary>
    public double Tps15m { get; init; }

    /// <summary>
    /// 获取平均 TPS
    /// </summary>
    public double Average => (Tps1m + Tps5m + Tps15m) / 3.0;

    public override string ToString() => $"TPS: {Tps1m:F1} / {Tps5m:F1} / {Tps15m:F1}";
}

/// <summary>
/// MSPT 数据
/// </summary>
public record MsptData
{
    /// <summary>
    /// 平均 MSPT
    /// </summary>
    public double Average { get; init; }

    /// <summary>
    /// 最小 MSPT
    /// </summary>
    public double Min { get; init; }

    /// <summary>
    /// 最大 MSPT
    /// </summary>
    public double Max { get; init; }

    public override string ToString() => $"MSPT: {Average:F1}ms (min: {Min:F1}, max: {Max:F1})";
}

/// <summary>
/// 服务器类型
/// </summary>
public enum ServerType
{
    /// <summary>
    /// 未知服务器类型
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 原版服务器
    /// </summary>
    Vanilla,
    
    /// <summary>
    /// Paper 服务器
    /// </summary>
    Paper,
    
    /// <summary>
    /// Purpur 服务器
    /// </summary>
    Purpur,
    
    /// <summary>
    /// Spigot 服务器
    /// </summary>
    Spigot,
    
    /// <summary>
    /// Forge 服务器
    /// </summary>
    Forge,
    
    /// <summary>
    /// Fabric 服务器
    /// </summary>
    Fabric
}

