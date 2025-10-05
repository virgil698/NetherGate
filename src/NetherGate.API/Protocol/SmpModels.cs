using System.Text.Json.Serialization;

namespace NetherGate.API.Protocol;

/// <summary>
/// 玩家数据传输对象
/// </summary>
public record PlayerDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    public override string ToString() => $"{Name} ({Uuid})";
}

/// <summary>
/// 用户封禁数据传输对象
/// </summary>
public record UserBanDto
{
    [JsonPropertyName("player")]
    public PlayerDto Player { get; init; } = new();

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    public override string ToString() => 
        $"Ban: {Player.Name} - {Reason ?? "No reason"}" + 
        (Expires.HasValue ? $" (until {Expires.Value:yyyy-MM-dd})" : " (永久)");
}

/// <summary>
/// IP 封禁数据传输对象
/// </summary>
public record IpBanDto
{
    [JsonPropertyName("ip")]
    public string Ip { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    public override string ToString() => 
        $"IP Ban: {Ip} - {Reason ?? "No reason"}" + 
        (Expires.HasValue ? $" (until {Expires.Value:yyyy-MM-dd})" : " (永久)");
}

/// <summary>
/// 管理员数据传输对象
/// </summary>
public record OperatorDto
{
    [JsonPropertyName("player")]
    public PlayerDto Player { get; init; } = new();

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("bypassPlayerLimit")]
    public bool BypassPlayerLimit { get; init; }

    public override string ToString() => $"OP: {Player.Name} (Level {Level})";
}

/// <summary>
/// 服务器状态
/// </summary>
public record ServerState
{
    [JsonPropertyName("started")]
    public bool Started { get; init; }

    [JsonPropertyName("version")]
    public VersionInfo Version { get; init; } = new();

    public override string ToString() => 
        $"Server: {(Started ? "运行中" : "已停止")} - {Version}";
}

/// <summary>
/// 版本信息
/// </summary>
public record VersionInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("protocol")]
    public int Protocol { get; init; }

    public override string ToString() => $"{Name} (Protocol {Protocol})";
}

/// <summary>
/// 类型化规则
/// </summary>
public record TypedRule
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; init; }

    public override string ToString() => $"{Type}: {Value}";
}

/// <summary>
/// 服务器设置
/// </summary>
public record ServerSetting
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    public override string ToString() => $"{Key}: {Value} ({Type})";
}

