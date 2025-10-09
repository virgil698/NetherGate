namespace NetherGate.API.Network;

/// <summary>
/// Minecraft 服务器查询接口
/// 使用 Minecraft 网络协议查询其他服务器的状态（Server List Ping）
/// </summary>
public interface IServerQuery
{
    /// <summary>
    /// 查询服务器状态
    /// </summary>
    /// <param name="host">服务器地址</param>
    /// <param name="port">服务器端口（默认 25565）</param>
    /// <param name="timeout">超时时间（毫秒，默认 5000）</param>
    /// <returns>服务器状态信息</returns>
    Task<ServerStatusResponse?> QueryAsync(string host, int port = 25565, int timeout = 5000);

    /// <summary>
    /// Ping 服务器（获取延迟）
    /// </summary>
    /// <param name="host">服务器地址</param>
    /// <param name="port">服务器端口（默认 25565）</param>
    /// <param name="timeout">超时时间（毫秒，默认 5000）</param>
    /// <returns>延迟（毫秒），-1 表示超时或失败</returns>
    Task<long> PingAsync(string host, int port = 25565, int timeout = 5000);

    /// <summary>
    /// 批量查询多个服务器
    /// </summary>
    /// <param name="servers">服务器列表</param>
    /// <param name="timeout">每个服务器的超时时间（毫秒）</param>
    /// <returns>查询结果字典（key: "host:port", value: 状态）</returns>
    Task<Dictionary<string, ServerStatusResponse?>> QueryBatchAsync(
        List<ServerAddress> servers, 
        int timeout = 5000);
}

/// <summary>
/// 服务器地址
/// </summary>
public record ServerAddress
{
    /// <summary>
    /// 服务器主机名或 IP
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; init; } = 25565;

    /// <summary>
    /// 友好名称（可选）
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// 返回服务器地址的字符串表示形式
    /// </summary>
    public override string ToString() => $"{Host}:{Port}";
}

/// <summary>
/// 服务器状态响应
/// </summary>
public class ServerStatusResponse
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; init; }

    /// <summary>
    /// 延迟（毫秒）
    /// </summary>
    public long Latency { get; init; }

    /// <summary>
    /// 版本信息
    /// </summary>
    public VersionInfo Version { get; init; } = new();

    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayersInfo Players { get; init; } = new();

    /// <summary>
    /// 服务器描述（MOTD）
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 服务器图标（Base64 编码的 PNG）
    /// </summary>
    public string? Favicon { get; init; }

    /// <summary>
    /// 是否启用了 Forge
    /// </summary>
    public bool ModdedForge { get; init; }

    /// <summary>
    /// Forge Mod 列表
    /// </summary>
    public List<ModInfo>? Mods { get; init; }

    /// <summary>
    /// 查询时间
    /// </summary>
    public DateTime QueriedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// 错误信息（如果查询失败）
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// 版本信息
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// 版本名称（如 "1.21.9"）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 协议版本号
    /// </summary>
    public int Protocol { get; init; }
}

/// <summary>
/// 玩家信息
/// </summary>
public class PlayersInfo
{
    /// <summary>
    /// 在线玩家数
    /// </summary>
    public int Online { get; init; }

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int Max { get; init; }

    /// <summary>
    /// 在线玩家列表（可能为空）
    /// </summary>
    public List<PlayerSample> Sample { get; init; } = new();
}

/// <summary>
/// 玩家样本
/// </summary>
public class PlayerSample
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public string Id { get; init; } = string.Empty;
}

/// <summary>
/// Mod 信息
/// </summary>
public class ModInfo
{
    /// <summary>
    /// Mod ID
    /// </summary>
    public string ModId { get; init; } = string.Empty;

    /// <summary>
    /// Mod 版本
    /// </summary>
    public string Version { get; init; } = string.Empty;
}

