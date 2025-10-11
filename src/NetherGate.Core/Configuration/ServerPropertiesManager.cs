using NetherGate.API.Configuration;
using NetherGate.API.Logging;
using System.Text;

namespace NetherGate.Core.Configuration;

/// <summary>
/// Minecraft server.properties 文件管理器
/// 负责读取、修改和验证 server.properties 配置
/// </summary>
public class ServerPropertiesManager
{
    private readonly ILogger _logger;
    private readonly string _propertiesPath;
    private Dictionary<string, string> _properties = new();

    public ServerPropertiesManager(ILogger logger, string serverDirectory)
    {
        _logger = logger;
        _propertiesPath = Path.Combine(serverDirectory, "server.properties");
    }

    /// <summary>
    /// 加载 server.properties 文件
    /// </summary>
    public bool Load()
    {
        try
        {
            if (!File.Exists(_propertiesPath))
            {
                _logger.Warning($"server.properties 文件不存在: {_propertiesPath}");
                return false;
            }

            _properties.Clear();
            var lines = File.ReadAllLines(_propertiesPath);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // 跳过注释和空行
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var equalIndex = trimmed.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = trimmed.Substring(0, equalIndex).Trim();
                    var value = trimmed.Substring(equalIndex + 1).Trim();
                    _properties[key] = value;
                }
            }

            _logger.Debug($"已加载 server.properties，共 {_properties.Count} 个配置项");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载 server.properties 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 保存 server.properties 文件
    /// </summary>
    public bool Save()
    {
        try
        {
            // 读取原始文件以保留注释和格式
            var lines = File.Exists(_propertiesPath) 
                ? File.ReadAllLines(_propertiesPath).ToList() 
                : new List<string>();

            // 更新已存在的配置项
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                
                // 跳过注释和空行
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var equalIndex = line.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = line.Substring(0, equalIndex).Trim();
                    
                    if (_properties.ContainsKey(key))
                    {
                        // 保留原始缩进
                        var indent = lines[i].TakeWhile(char.IsWhiteSpace).Count();
                        lines[i] = new string(' ', indent) + $"{key}={_properties[key]}";
                    }
                }
            }

            File.WriteAllLines(_propertiesPath, lines, Encoding.UTF8);
            _logger.Info($"已保存 server.properties: {_propertiesPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"保存 server.properties 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    public string? GetValue(string key)
    {
        return _properties.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetValue(string key, string value)
    {
        _properties[key] = value;
    }

    /// <summary>
    /// 获取布尔值
    /// </summary>
    public bool GetBoolean(string key, bool defaultValue = false)
    {
        var value = GetValue(key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取整数值
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        var value = GetValue(key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 检查并自动配置 RCON 和 SMP
    /// </summary>
    /// <returns>如果进行了修改返回 true</returns>
    public bool EnsureRconAndSmpEnabled()
    {
        bool modified = false;

        // 检查 RCON
        if (!GetBoolean("enable-rcon", false))
        {
            _logger.Warning("检测到 RCON 未启用，正在自动启用...");
            SetValue("enable-rcon", "true");
            modified = true;
        }

        // 确保 RCON 端口存在
        var rconPort = GetInt("rcon.port", 0);
        if (rconPort == 0)
        {
            _logger.Info("设置默认 RCON 端口: 25575");
            SetValue("rcon.port", "25575");
            modified = true;
        }

        // 确保 RCON 密码存在
        var rconPassword = GetValue("rcon.password");
        if (string.IsNullOrEmpty(rconPassword))
        {
            _logger.Warning("RCON 密码为空，设置默认密码");
            SetValue("rcon.password", GenerateRandomPassword(16));
            modified = true;
        }

        // 检查 SMP (Management Server)
        if (!GetBoolean("management-server-enabled", false))
        {
            _logger.Warning("检测到 SMP (Management Server) 未启用，正在自动启用...");
            SetValue("management-server-enabled", "true");
            modified = true;
        }

        // 确保 SMP 配置存在
        var smpHost = GetValue("management-server-host");
        if (string.IsNullOrEmpty(smpHost))
        {
            _logger.Info("设置默认 SMP Host: localhost");
            SetValue("management-server-host", "localhost");
            modified = true;
        }

        var smpPort = GetInt("management-server-port", 0);
        if (smpPort == 0)
        {
            _logger.Info("设置默认 SMP Port: 40745");
            SetValue("management-server-port", "40745");
            modified = true;
        }

        var smpSecret = GetValue("management-server-secret");
        if (string.IsNullOrEmpty(smpSecret))
        {
            _logger.Warning("SMP Secret 为空，生成随机密钥");
            SetValue("management-server-secret", GenerateRandomSecret(48));
            modified = true;
        }

        if (modified)
        {
            _logger.Info("已自动配置 RCON 和 SMP，请重启 Minecraft 服务器使配置生效");
        }

        return modified;
    }

    /// <summary>
    /// 同步配置到 NetherGate 配置
    /// </summary>
    public bool SyncToNetherGateConfig(NetherGateConfig config)
    {
        bool modified = false;

        // 同步 RCON 配置
        var rconEnabled = GetBoolean("enable-rcon", false);
        var rconPort = GetInt("rcon.port", 25575);
        var rconPassword = GetValue("rcon.password") ?? "";

        if (config.Rcon.Enabled != rconEnabled)
        {
            config.Rcon.Enabled = rconEnabled;
            modified = true;
            _logger.Info($"已同步 RCON 启用状态: {rconEnabled}");
        }

        if (config.Rcon.Port != rconPort)
        {
            config.Rcon.Port = rconPort;
            modified = true;
            _logger.Info($"已同步 RCON 端口: {rconPort}");
        }

        if (config.Rcon.Password != rconPassword)
        {
            config.Rcon.Password = rconPassword;
            modified = true;
            _logger.Info("已同步 RCON 密码");
        }

        // 同步 SMP 配置
        var smpEnabled = GetBoolean("management-server-enabled", false);
        var smpHost = GetValue("management-server-host") ?? "localhost";
        var smpPort = GetInt("management-server-port", 40745);
        var smpSecret = GetValue("management-server-secret") ?? "";

        if (smpEnabled)
        {
            if (config.ServerConnection.Host != smpHost)
            {
                config.ServerConnection.Host = smpHost;
                modified = true;
                _logger.Info($"已同步 SMP Host: {smpHost}");
            }

            if (config.ServerConnection.Port != smpPort)
            {
                config.ServerConnection.Port = smpPort;
                modified = true;
                _logger.Info($"已同步 SMP Port: {smpPort}");
            }

            if (config.ServerConnection.Secret != smpSecret)
            {
                config.ServerConnection.Secret = smpSecret;
                modified = true;
                _logger.Info("已同步 SMP Secret");
            }
        }

        return modified;
    }

    /// <summary>
    /// 生成随机密码
    /// </summary>
    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// 生成随机密钥（Base64 风格）
    /// </summary>
    private string GenerateRandomSecret(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// 获取所有配置信息
    /// </summary>
    public ServerPropertiesInfo GetInfo()
    {
        return new ServerPropertiesInfo
        {
            RconEnabled = GetBoolean("enable-rcon", false),
            RconPort = GetInt("rcon.port", 25575),
            RconPassword = GetValue("rcon.password") ?? "",
            SmpEnabled = GetBoolean("management-server-enabled", false),
            SmpHost = GetValue("management-server-host") ?? "localhost",
            SmpPort = GetInt("management-server-port", 40745),
            SmpSecret = GetValue("management-server-secret") ?? "",
            ServerPort = GetInt("server-port", 25565)
        };
    }
}

/// <summary>
/// server.properties 配置信息
/// </summary>
public class ServerPropertiesInfo
{
    public bool RconEnabled { get; set; }
    public int RconPort { get; set; }
    public string RconPassword { get; set; } = "";
    
    public bool SmpEnabled { get; set; }
    public string SmpHost { get; set; } = "localhost";
    public int SmpPort { get; set; }
    public string SmpSecret { get; set; } = "";
    
    public int ServerPort { get; set; }

    public override string ToString()
    {
        return $"RCON: {(RconEnabled ? "启用" : "禁用")} (:{RconPort}), " +
               $"SMP: {(SmpEnabled ? "启用" : "禁用")} ({SmpHost}:{SmpPort}), " +
               $"Server: :{ServerPort}";
    }
}

