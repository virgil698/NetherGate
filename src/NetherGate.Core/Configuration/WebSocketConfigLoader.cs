using NetherGate.API.Configuration;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetherGate.Core.Configuration;

/// <summary>
/// WebSocket 配置加载器
/// </summary>
public class WebSocketConfigLoader
{
    private const string ConfigFileNameYaml = "websocket-config.yaml";
    private const string ConfigFileNameJson = "websocket-config.json";

    public static WebSocketConfig Load()
    {
        // 查找配置文件
        string? configPath = null;
        
        if (File.Exists(ConfigFileNameYaml))
        {
            configPath = ConfigFileNameYaml;
        }
        else if (File.Exists(ConfigFileNameJson))
        {
            configPath = ConfigFileNameJson;
        }

        // 如果不存在配置文件，创建默认配置
        if (configPath == null)
        {
            Console.WriteLine("[NetherGate] WebSocket 配置文件不存在，创建默认配置...");
            var defaultConfig = CreateDefaultConfig();
            SaveConfig(defaultConfig, ConfigFileNameYaml);
            return defaultConfig;
        }

        try
        {
            var content = File.ReadAllText(configPath);
            
            if (configPath.EndsWith(".yaml") || configPath.EndsWith(".yml"))
            {
                return LoadFromYaml(content);
            }
            else
            {
                return LoadFromJson(content);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetherGate] 加载 WebSocket 配置失败: {ex.Message}");
            Console.WriteLine("[NetherGate] 使用默认配置");
            return CreateDefaultConfig();
        }
    }

    private static WebSocketConfig LoadFromYaml(string content)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        
        return deserializer.Deserialize<WebSocketConfig>(content);
    }

    private static WebSocketConfig LoadFromJson(string content)
    {
        return JsonSerializer.Deserialize<WebSocketConfig>(content) ?? CreateDefaultConfig();
    }

    public static void SaveConfig(WebSocketConfig config, string path)
    {
        try
        {
            string content;
            
            if (path.EndsWith(".yaml") || path.EndsWith(".yml"))
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                
                var template = GetYamlTemplate();
                File.WriteAllText(path, template);
            }
            else
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                content = JsonSerializer.Serialize(config, options);
                File.WriteAllText(path, content);
            }
            
            Console.WriteLine($"[NetherGate] WebSocket 配置已保存: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetherGate] 保存 WebSocket 配置失败: {ex.Message}");
        }
    }

    private static WebSocketConfig CreateDefaultConfig()
    {
        return new WebSocketConfig
        {
            Enabled = true,
            Host = "localhost",
            Port = 8766,
            MaxConnections = 100,
            Authentication = new WebSocketAuthConfig
            {
                Enabled = true,
                Token = GenerateSecureToken(),
                TokenExpiryHours = 24,
                AllowedIps = new List<string>(),
                RequireTls = false
            },
            Cors = new WebSocketCorsConfig
            {
                Enabled = true,
                AllowedOrigins = new List<string> { "*" }
            },
            Heartbeat = new WebSocketHeartbeatConfig
            {
                Enabled = true,
                IntervalSeconds = 30,
                TimeoutSeconds = 60
            },
            Buffer = new WebSocketBufferConfig
            {
                ReceiveBufferSize = 4096,
                SendBufferSize = 4096
            }
        };
    }

    private static string GenerateSecureToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

    private static string GetYamlTemplate()
    {
        var token = GenerateSecureToken();
        
        return $@"# ============================================
# NetherGate WebSocket 配置文件
# ============================================

# 是否启用 WebSocket 服务器
enabled: true

# 监听地址（localhost 或 0.0.0.0）
host: localhost

# 监听端口
port: 8766

# 最大连接数
max_connections: 100

# ============================================
# 认证配置
# ============================================
authentication:
  # 是否启用认证
  enabled: true
  
  # 认证令牌（首次生成后请妥善保管）
  # 客户端需要使用此令牌进行认证
  token: ""{token}""
  
  # 令牌过期时间（小时）
  token_expiry_hours: 24
  
  # 允许的 IP 地址列表（空则允许所有）
  allowed_ips: []
  # 示例:
  # - ""127.0.0.1""
  # - ""192.168.1.100""
  
  # 是否要求 TLS（生产环境建议启用）
  require_tls: false

# ============================================
# CORS 配置
# ============================================
cors:
  # 是否启用 CORS
  enabled: true
  
  # 允许的源（空则允许所有）
  allowed_origins:
    - ""*""
  # 生产环境建议指定具体的域名:
  # - ""https://your-dashboard.com""

# ============================================
# 心跳配置
# ============================================
heartbeat:
  # 是否启用心跳检查
  enabled: true
  
  # 心跳间隔（秒）
  interval_seconds: 30
  
  # 超时时间（秒）
  timeout_seconds: 60

# ============================================
# 缓冲区配置
# ============================================
buffer:
  # 接收缓冲区大小（字节）
  receive_buffer_size: 4096
  
  # 发送缓冲区大小（字节）
  send_buffer_size: 4096

# ============================================
# 使用说明
# ============================================
# 1. 首次运行时会自动生成认证令牌
# 2. 请妥善保管令牌，客户端需要使用它连接
# 3. 如需重新生成令牌，删除此文件后重启服务器
# 4. 生产环境建议:
#    - 启用认证 (authentication.enabled: true)
#    - 限制 IP 白名单 (authentication.allowed_ips)
#    - 限制 CORS 源 (cors.allowed_origins)
#    - 使用 TLS (authentication.require_tls: true)
";
    }
}
