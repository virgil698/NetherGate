using System.Text.Json.Serialization;

namespace NetherGate.API.Protocol;

/// <summary>
/// 玩家数据传输对象
/// </summary>
public record PlayerDto
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => $"{Name} ({Uuid})";
}

/// <summary>
/// 用户封禁数据传输对象
/// </summary>
public record UserBanDto
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    [JsonPropertyName("player")]
    public PlayerDto Player { get; init; } = new();

    /// <summary>
    /// 封禁原因
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTime? Expires { get; init; }

    /// <summary>
    /// 封禁来源
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => 
        $"Ban: {Player.Name} - {Reason ?? "No reason"}" + 
        (Expires.HasValue ? $" (until {Expires.Value:yyyy-MM-dd})" : " (永久)");
}

/// <summary>
/// IP 封禁数据传输对象
/// </summary>
public record IpBanDto
{
    /// <summary>
    /// IP 地址
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; init; } = string.Empty;

    /// <summary>
    /// 封禁原因
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTime? Expires { get; init; }

    /// <summary>
    /// 封禁来源
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => 
        $"IP Ban: {Ip} - {Reason ?? "No reason"}" + 
        (Expires.HasValue ? $" (until {Expires.Value:yyyy-MM-dd})" : " (永久)");
}

/// <summary>
/// 管理员数据传输对象
/// </summary>
public record OperatorDto
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    [JsonPropertyName("player")]
    public PlayerDto Player { get; init; } = new();

    /// <summary>
    /// 权限级别
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; init; }

    /// <summary>
    /// 是否可绕过玩家限制
    /// </summary>
    [JsonPropertyName("bypassPlayerLimit")]
    public bool BypassPlayerLimit { get; init; }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => $"OP: {Player.Name} (Level {Level})";
}

/// <summary>
/// 服务器状态
/// </summary>
public record ServerState
{
    /// <summary>
    /// 是否已启动
    /// </summary>
    [JsonPropertyName("started")]
    public bool Started { get; init; }

    /// <summary>
    /// 版本信息
    /// </summary>
    [JsonPropertyName("version")]
    public VersionInfo Version { get; init; } = new();

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => 
        $"Server: {(Started ? "运行中" : "已停止")} - {Version}";
}

/// <summary>
/// 版本信息
/// </summary>
public record VersionInfo
{
    /// <summary>
    /// 版本名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 协议版本号
    /// </summary>
    [JsonPropertyName("protocol")]
    public int Protocol { get; init; }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => $"{Name} (Protocol {Protocol})";
}

/// <summary>
/// 类型化规则
/// </summary>
public record TypedRule
{
    /// <summary>
    /// 规则类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 规则值
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => $"{Type}: {Value}";
}

/// <summary>
/// 服务器设置
/// </summary>
public record ServerSetting
{
    /// <summary>
    /// 设置键名
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// 设置值
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    /// <summary>
    /// 设置类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString() => $"{Key}: {Value} ({Type})";
}

