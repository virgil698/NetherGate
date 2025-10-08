using NetherGate.API.Configuration;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetherGate.Core.Configuration;

/// <summary>
/// 配置加载器（支持 JSON 和 YAML 格式）
/// </summary>
public class ConfigurationLoader
{
    private const string ConfigFileNameYaml = "nethergate-config.yaml";
    private const string ConfigFileNameYml = "nethergate-config.yml";
    private const string ConfigFileNameJson = "nethergate-config.json";

    /// <summary>
    /// 获取配置文件路径（如果存在）
    /// </summary>
    public static string GetConfigPath()
    {
        if (File.Exists(ConfigFileNameYaml))
            return ConfigFileNameYaml;
        if (File.Exists(ConfigFileNameYml))
            return ConfigFileNameYml;
        if (File.Exists(ConfigFileNameJson))
            return ConfigFileNameJson;
        
        // 默认返回 YAML 路径（即使不存在）
        return ConfigFileNameYaml;
    }

    /// <summary>
    /// 加载配置（支持 YAML 和 JSON 格式，优先使用 YAML）
    /// </summary>
    public static NetherGateConfig Load()
    {
        return Load(null);
    }

    /// <summary>
    /// 加载配置（支持 YAML 和 JSON 格式，优先使用 YAML）
    /// </summary>
    public static NetherGateConfig Load(API.Logging.ILogger? logger)
    {
        // 按优先级查找配置文件：YAML (.yaml) > YAML (.yml) > JSON
        string? configPath = null;
        ConfigFormat format = ConfigFormat.Yaml;

        if (File.Exists(ConfigFileNameYaml))
        {
            configPath = ConfigFileNameYaml;
            format = ConfigFormat.Yaml;
        }
        else if (File.Exists(ConfigFileNameYml))
        {
            configPath = ConfigFileNameYml;
            format = ConfigFormat.Yaml;
        }
        else if (File.Exists(ConfigFileNameJson))
        {
            configPath = ConfigFileNameJson;
            format = ConfigFormat.Json;
        }

        // 首次运行：创建默认配置
        if (configPath == null)
        {
            Console.WriteLine("[NetherGate] 配置文件不存在，创建默认配置...");
            
            var defaultConfig = CreateDefaultConfig();
            
            // 默认生成 YAML 格式
            configPath = ConfigFileNameYaml;
            SaveConfig(configPath, defaultConfig, ConfigFormat.Yaml);
            
            Console.WriteLine($"[NetherGate] 已创建默认配置文件: {configPath} (YAML 格式)");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  首次启动提醒：");
            Console.WriteLine("   程序将使用默认配置运行，建议稍后根据实际情况修改配置文件");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("重要配置项：");
            Console.WriteLine("  1. server_connection.secret - 修改为 40 字符的密钥");
            Console.WriteLine("  2. rcon.password - 修改 RCON 密码");
            Console.WriteLine("  3. server_process.server.working_directory - Minecraft 服务器目录");
            Console.WriteLine();
            
            // 不再强制退出，使用默认配置继续运行
            return defaultConfig;
        }

        // 加载配置
        try
        {
            var formatName = format == ConfigFormat.Yaml ? "YAML" : "JSON";
            Console.WriteLine($"[NetherGate] 加载配置文件: {configPath} ({formatName} 格式)");
            
            var content = File.ReadAllText(configPath);
            var config = DeserializeConfig(content, format);

            if (config == null)
            {
                Console.WriteLine("[NetherGate] 配置文件解析失败，使用默认配置");
                return CreateDefaultConfig();
            }

            // 迁移配置（如果需要）
            if (logger != null)
            {
                var migrator = new ConfigMigrator(logger);
                if (migrator.NeedsMigration(config))
                {
                    config = migrator.Migrate(config);
                    
                    // 保存迁移后的配置
                    try
                    {
                        SaveConfig(configPath, config, format);
                        logger.Info($"已保存迁移后的配置到: {configPath}");
                    }
                    catch (Exception ex)
                    {
                        logger.Warning($"保存迁移后的配置失败: {ex.Message}");
                    }
                }
            }

            // 验证配置
            ValidateConfig(config);

            Console.WriteLine("[NetherGate] 配置加载成功");
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetherGate] 加载配置文件失败: {ex.Message}");
            Console.WriteLine("[NetherGate] 使用默认配置");
            return CreateDefaultConfig();
        }
    }

    /// <summary>
    /// 保存配置（自动识别格式）
    /// </summary>
    public static void SaveConfig(string path, NetherGateConfig config)
    {
        var format = GetFormatFromPath(path);
        SaveConfig(path, config, format);
    }

    /// <summary>
    /// 保存配置（指定格式）
    /// </summary>
    public static void SaveConfig(string path, NetherGateConfig config, ConfigFormat format)
    {
        string content;
        
        // 如果是默认配置且是 YAML 格式，使用带注释的模板
        if (format == ConfigFormat.Yaml && IsDefaultConfig(config))
        {
            content = GetDefaultYamlTemplate();
        }
        else
        {
            content = SerializeConfig(config, format);
        }
        
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    private static NetherGateConfig CreateDefaultConfig()
    {
        // 注意：所有配置类都已经在定义中设置了默认值
        // 这里只需要覆盖需要特别设置的值
        var config = new NetherGateConfig
        {
            ConfigVersion = ConfigMigrator.LatestVersion
        };
        
        // 设置必须修改的密钥和密码
        config.ServerConnection.Secret = "your-40-character-secret-token-here-change-this";
        config.Rcon.Password = "your-rcon-password-change-this";
        
        // 设置默认的 JVM 参数
        config.ServerProcess.Arguments.JvmMiddle = new List<string>
        {
            "-XX:+UseG1GC",
            "-XX:MaxGCPauseMillis=200",
            "-Dfile.encoding=UTF-8"
        };
        config.ServerProcess.Arguments.Server = new List<string> { "--nogui" };
        
        return config;
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    private static void ValidateConfig(NetherGateConfig config)
    {
        var errors = new List<string>();

        // 验证 SMP 配置
        if (config.ServerConnection.Port <= 0 || config.ServerConnection.Port > 65535)
            errors.Add("server_connection.port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(config.ServerConnection.Secret))
            errors.Add("server_connection.secret cannot be empty");
        else if (config.ServerConnection.Secret.Length != 40 && 
                 config.ServerConnection.Secret != "your-40-character-secret-token-here-change-this")
            errors.Add("server_connection.secret should be 40 characters");

        // 验证 RCON 配置
        if (config.Rcon.Enabled)
        {
            if (config.Rcon.Port <= 0 || config.Rcon.Port > 65535)
                errors.Add("rcon.port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(config.Rcon.Password))
                errors.Add("rcon.password cannot be empty when RCON is enabled");
        }

        // 验证服务器进程配置
        if (config.ServerProcess.Enabled)
        {
            if (config.ServerProcess.Memory.Min <= 0)
                errors.Add("server_process.memory.min must be greater than 0");

            if (config.ServerProcess.Memory.Max < config.ServerProcess.Memory.Min)
                errors.Add("server_process.memory.max must be greater than or equal to min");
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("[NetherGate] 配置验证警告:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    }

    /// <summary>
    /// 根据文件路径判断格式
    /// </summary>
    private static ConfigFormat GetFormatFromPath(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".yaml" || extension == ".yml" 
            ? ConfigFormat.Yaml 
            : ConfigFormat.Json;
    }

    /// <summary>
    /// 反序列化配置
    /// </summary>
    private static NetherGateConfig? DeserializeConfig(string content, ConfigFormat format)
    {
        if (format == ConfigFormat.Yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            return deserializer.Deserialize<NetherGateConfig>(content);
        }
        else
        {
            return JsonSerializer.Deserialize<NetherGateConfig>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
    }

    /// <summary>
    /// 序列化配置
    /// </summary>
    private static string SerializeConfig(NetherGateConfig config, ConfigFormat format)
    {
        if (format == ConfigFormat.Yaml)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();
            return serializer.Serialize(config);
        }
        else
        {
            return JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }

    /// <summary>
    /// 判断是否为默认配置
    /// </summary>
    private static bool IsDefaultConfig(NetherGateConfig config)
    {
        return config.ServerConnection.Secret == "your-40-character-secret-token-here-change-this" &&
               config.Rcon.Password == "your-rcon-password-change-this";
    }

    /// <summary>
    /// 获取带注释的默认 YAML 模板
    /// </summary>
    private static string GetDefaultYamlTemplate()
    {
        return @"# ============================================
# NetherGate 配置文件
# 
# 完整文档：https://github.com/your-repo/NetherGate/docs/CONFIGURATION.md
# ============================================

# ============================================
# 服务器进程管理配置
# ============================================
server_process:
  # 是否启用服务器进程管理
  # true: 由 NetherGate 管理服务器进程
  # false: 服务器由其他方式管理，NetherGate 仅作为客户端连接
  enabled: true
  
  # 启动方式：java | script | external
  # - java: 使用 Java 命令直接启动 server.jar（推荐新手）
  # - script: 使用现有的启动脚本（适合已有启动脚本的用户）
  # - external: 服务器已经在运行，NetherGate 仅连接（适合托管服务器）
  launch_method: java
  
  # Java 配置（仅当 launch_method=java 时使用）
  java:
    path: java  # Java 可执行文件路径
    version_check: true  # 启动时检查 Java 版本
  
  # 服务器配置
  server:
    jar: server.jar  # 服务器 JAR 文件名（仅当 launch_method=java 时使用）
    # 服务器工作目录（所有模式都需要）
    # - java/script 模式：服务器的启动目录
    # - external 模式：用于文件访问、日志读取、备份等功能
    working_directory: ./minecraft_server
  
  # 内存配置（仅当 launch_method=java 时使用，单位：MB）
  memory:
    min: 2048  # 最小内存（-Xms）
    max: 4096  # 最大内存（-Xmx）
  
  # JVM 参数（仅当 launch_method=java 时使用）
  arguments:
    jvm_prefix: []  # JVM 前置参数（-Xms 之前）
    jvm_middle:  # JVM 中间参数（-Xms 和 -Xmx 之间）
      - ""-XX:+UseG1GC""
      - ""-XX:MaxGCPauseMillis=200""
      - ""-Dfile.encoding=UTF-8""
    server:  # 服务器参数（-jar server.jar 之后）
      - ""--nogui""
  
  # 脚本启动配置（仅当 launch_method=script 时使用）
  script:
    path: ./start.sh  # 脚本路径（支持 .sh, .bat, .cmd, .exe）
    arguments: []  # 脚本参数
    working_directory: ./minecraft_server  # 工作目录
    use_shell: false  # 是否使用 Shell 执行（Linux/macOS）
  
  # 进程监控配置
  monitoring:
    startup_timeout: 300  # 启动超时时间（秒）
    show_server_output: true  # 显示服务器输出
    server_output_prefix: ""[MC] ""  # 服务器输出前缀
    
    # 启动检测
    startup_detection:
      enabled: true
      keywords:  # 启动完成的关键词
        - ""Done (""
    
    # 崩溃检测
    crash_detection:
      enabled: true
      keywords:  # 崩溃关键词
        - ""Exception in server tick loop""
  
  # 自动重启配置
  auto_restart:
    enabled: true  # 启用自动重启
    delay_seconds: 5  # 重启延迟（秒）
    max_retries: 3  # 最大重试次数
    retry_delay: 5000  # 重试延迟（毫秒）
    reset_timer: 600000  # 重置计时器（毫秒，10分钟）

# ============================================
# SMP 服务器连接配置
# ============================================
# 服务端管理协议（Server Management Protocol）
# 需要在 server.properties 中启用：
#   management-server-enabled=true
#   management-server-port=40745
#   management-server-secret=<40字符密钥>
server_connection:
  host: localhost  # 服务器地址
  port: 40745  # SMP 端口
  
  # ⚠️ 重要：修改为 40 字符的随机密钥
  # 必须与 server.properties 中的 management-server-secret 一致
  secret: your-40-character-secret-token-here-change-this
  
  use_tls: false  # 是否使用 TLS 加密
  tls_certificate: null  # TLS 证书路径（PFX 格式）
  tls_password: null  # TLS 证书密码
  
  reconnect_interval: 5000  # 重连间隔（毫秒）
  heartbeat_timeout: 30000  # 心跳超时（毫秒）
  auto_connect: true  # 自动连接
  connect_delay: 3000  # 连接延迟（毫秒）
  wait_for_server_ready: true  # 等待服务器就绪后再连接

# ============================================
# RCON 客户端配置
# ============================================
# 用于执行游戏内命令，需要在 server.properties 中启用：
#   enable-rcon=true
#   rcon.port=25566
#   rcon.password=<密码>
rcon:
  enabled: true  # 是否启用 RCON
  host: localhost  # RCON 服务器地址
  port: 25566  # RCON 端口
  
  # ⚠️ 重要：修改为实际的 RCON 密码
  # 必须与 server.properties 中的 rcon.password 一致
  password: your-rcon-password-change-this
  
  connect_timeout: 5000  # 连接超时（毫秒）
  command_timeout: 10000  # 命令超时（毫秒）
  auto_connect: true  # 自动连接
  connect_delay: 3000  # 连接延迟（毫秒）
  
  # 重连配置
  reconnect:
    enabled: true  # 启用自动重连
    interval: 5000  # 重连间隔（毫秒）
    max_retries: 5  # 最大重试次数

# ============================================
# spark 性能监控配置
# ============================================
spark:
  enabled: false  # 是否启用 spark 性能监控
  
  # spark 类型：standalone | plugin
  # - standalone: 独立代理版（通过 -javaagent 注入）
  # - plugin: 插件/模组版（需要手动安装 spark 插件）
  type: standalone
  
  force_enable_for_script_mode: false  # 脚本模式下是否强制启用
  auto_download: true  # 自动下载 spark agent
  agent_jar: null  # spark agent JAR 路径（留空自动下载）
  
  ssh_port: 2222  # SSH 监听端口
  ssh_password: null  # SSH 密码（留空随机生成）
  auto_start_profiling: false  # 启动时自动开始性能分析
  
  version: null  # spark 版本（留空使用最新版）
  download_url: ""https://spark.lucko.me/download/stable""  # 下载地址

# ============================================
# 日志监听器配置
# ============================================
log_listener:
  enabled: true  # 是否启用日志监听
  
  # 日志解析配置
  parsing:
    parse_chat: true  # 解析玩家聊天
    parse_commands: true  # 解析玩家命令
    parse_player_events: true  # 解析玩家加入/离开
    parse_errors: true  # 解析错误日志
  
  # 过滤器配置
  filters:
    ignore_patterns:  # 忽略的日志模式
      - ""Can't keep up!""
      - ""moved too quickly!""
      - ""moved wrongly!""
    log_levels:  # 处理的日志级别
      - ""INFO""
      - ""WARN""
      - ""ERROR""
  
  # 缓冲区配置
  buffer:
    size: 1000  # 缓冲区大小
    drop_old: true  # 缓冲区满时丢弃旧数据

# ============================================
# 插件管理配置
# ============================================
plugins:
  directory: plugins  # 插件目录
  auto_load: true  # 自动加载插件
  hot_reload: true  # 热重载支持（实验性）
  
  enabled_plugins:  # 启用的插件列表
    - ""*""  # ""*"" 表示启用所有插件
  
  disabled_plugins: []  # 禁用的插件列表（插件 ID）
  
  load_after_server_ready: true  # 服务器就绪后再加载插件
  load_timeout: 30  # 插件加载超时（秒）
  
  # 依赖管理配置
  dependency_management:
    enabled: true  # 启用依赖管理
    auto_download: true  # 自动下载缺失的依赖（从 NuGet）
    
    # NuGet 源列表（按顺序尝试）
    nuget_sources:
      - ""https://api.nuget.org/v3/index.json""  # 官方源
      # - ""https://nuget.cdn.azure.cn/v3/index.json""  # 国内镜像（可选）
    
    download_timeout: 60  # 下载超时时间（秒）
    verify_hash: true  # 验证下载文件的哈希值
    
    # 版本冲突解决策略：
    # - highest: 使用最高版本（推荐）
    # - lowest: 使用最低兼容版本
    # - fail: 报错，让用户手动处理
    conflict_resolution: highest
    
    show_conflict_report: true  # 显示冲突报告

# ============================================
# 日志系统配置
# ============================================
logging:
  # 日志级别：Trace | Debug | Info | Warning | Error | Fatal
  level: Info
  
  # 控制台日志
  console:
    enabled: true  # 启用控制台输出
    colored: true  # 彩色输出
    show_server_output: true  # 显示服务器输出
    server_output_prefix: ""[MC] ""  # 服务器输出前缀
  
  # 文件日志
  file:
    enabled: true  # 启用文件日志
    path: logs/latest.log  # 日志文件路径
    max_size: 10485760  # 单个文件最大大小（字节，10MB）
    max_files: 10  # 保留的日志文件数量
    rolling: true  # 启用日志滚动
    encoding: UTF-8  # 文件编码
  
  # 高级日志选项
  advanced:
    log_plugin_loading: true  # 记录插件加载日志
    log_smp_communication: false  # 记录 SMP 通信日志（调试用）
    log_rcon_communication: false  # 记录 RCON 通信日志（调试用）
    log_performance: false  # 记录性能日志（调试用）

# ============================================
# 高级配置
# ============================================
advanced:
  # 性能监控
  performance:
    enabled: false  # 启用性能监控
    report_interval: 60  # 报告间隔（秒）
  
  # 安全选项
  security:
    hide_secrets_in_logs: true  # 隐藏日志中的敏感信息
    plugin_sandbox: false  # 插件沙箱（实验性）
  
  # 实验性功能
  experimental:
    enabled: false  # 启用实验性功能
    hot_reload: false  # 热重载支持
    web_interface: false  # Web 管理界面
";
    }
}

/// <summary>
/// 配置文件格式
/// </summary>
public enum ConfigFormat
{
    Json,
    Yaml
}

