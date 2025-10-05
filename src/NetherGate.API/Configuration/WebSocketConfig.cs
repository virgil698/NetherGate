using System.Text.Json.Serialization;

namespace NetherGate.API.Configuration;

/// <summary>
/// WebSocket 服务器配置
/// </summary>
public class WebSocketConfig
{
    /// <summary>
    /// 是否启用 WebSocket 服务器
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 监听地址
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 监听端口
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 8766;

    /// <summary>
    /// 最大连接数
    /// </summary>
    [JsonPropertyName("max_connections")]
    public int MaxConnections { get; set; } = 100;

    /// <summary>
    /// 认证配置
    /// </summary>
    [JsonPropertyName("authentication")]
    public WebSocketAuthConfig Authentication { get; set; } = new();

    /// <summary>
    /// CORS 配置
    /// </summary>
    [JsonPropertyName("cors")]
    public WebSocketCorsConfig Cors { get; set; } = new();

    /// <summary>
    /// 心跳配置
    /// </summary>
    [JsonPropertyName("heartbeat")]
    public WebSocketHeartbeatConfig Heartbeat { get; set; } = new();

    /// <summary>
    /// 缓冲区配置
    /// </summary>
    [JsonPropertyName("buffer")]
    public WebSocketBufferConfig Buffer { get; set; } = new();
}

/// <summary>
/// WebSocket 认证配置
/// </summary>
public class WebSocketAuthConfig
{
    /// <summary>
    /// 是否启用认证
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 认证令牌（留空则自动生成）
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 令牌过期时间（小时）
    /// </summary>
    [JsonPropertyName("token_expiry_hours")]
    public int TokenExpiryHours { get; set; } = 24;

    /// <summary>
    /// 允许的 IP 地址列表（空则允许所有）
    /// </summary>
    [JsonPropertyName("allowed_ips")]
    public List<string> AllowedIps { get; set; } = new();

    /// <summary>
    /// 是否要求 TLS
    /// </summary>
    [JsonPropertyName("require_tls")]
    public bool RequireTls { get; set; } = false;
}

/// <summary>
/// WebSocket CORS 配置
/// </summary>
public class WebSocketCorsConfig
{
    /// <summary>
    /// 是否启用 CORS
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 允许的源（空则允许所有）
    /// </summary>
    [JsonPropertyName("allowed_origins")]
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
}

/// <summary>
/// WebSocket 心跳配置
/// </summary>
public class WebSocketHeartbeatConfig
{
    /// <summary>
    /// 是否启用心跳
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 心跳间隔（秒）
    /// </summary>
    [JsonPropertyName("interval_seconds")]
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    [JsonPropertyName("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// WebSocket 缓冲区配置
/// </summary>
public class WebSocketBufferConfig
{
    /// <summary>
    /// 接收缓冲区大小（字节）
    /// </summary>
    [JsonPropertyName("receive_buffer_size")]
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// 发送缓冲区大小（字节）
    /// </summary>
    [JsonPropertyName("send_buffer_size")]
    public int SendBufferSize { get; set; } = 4096;
}
