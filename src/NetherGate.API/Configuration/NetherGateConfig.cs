using System.Text.Json.Serialization;

namespace NetherGate.API.Configuration;

/// <summary>
/// NetherGate 主配置
/// </summary>
public class NetherGateConfig
{
    /// <summary>
    /// 配置文件版本（用于迁移）
    /// </summary>
    [JsonPropertyName("config_version")]
    public int ConfigVersion { get; set; } = 1;

    /// <summary>
    /// 服务器进程配置
    /// </summary>
    [JsonPropertyName("server_process")]
    public ServerProcessConfig ServerProcess { get; set; } = new();

    /// <summary>
    /// 服务器连接配置 (SMP)
    /// </summary>
    [JsonPropertyName("server_connection")]
    public ServerConnectionConfig ServerConnection { get; set; } = new();

    /// <summary>
    /// RCON 客户端配置
    /// </summary>
    [JsonPropertyName("rcon")]
    public RconConfig Rcon { get; set; } = new();

    /// <summary>
    /// spark 性能监控配置
    /// </summary>
    [JsonPropertyName("spark")]
    public SparkConfig Spark { get; set; } = new();

    /// <summary>
    /// 日志监听器配置
    /// </summary>
    [JsonPropertyName("log_listener")]
    public LogListenerConfig LogListener { get; set; } = new();

    /// <summary>
    /// 插件管理配置
    /// </summary>
    [JsonPropertyName("plugins")]
    public PluginConfig Plugins { get; set; } = new();

    /// <summary>
    /// 日志系统配置
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// 高级配置
    /// </summary>
    [JsonPropertyName("advanced")]
    public AdvancedConfig Advanced { get; set; } = new();
}

/// <summary>
/// 服务器进程配置
/// </summary>
public class ServerProcessConfig
{
    /// <summary>
    /// 是否启用服务器进程管理
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 启动方式: "java" 或 "script"
    /// java - 由 NetherGate 构建 Java 启动命令
    /// script - 使用现有的启动脚本（支持 .bat, .sh, .cmd, .exe）
    /// </summary>
    [JsonPropertyName("launch_method")]
    public string LaunchMethod { get; set; } = "java";

    /// <summary>
    /// Java 启动配置（launch_method = "java" 时使用）
    /// </summary>
    [JsonPropertyName("java")]
    public JavaConfig Java { get; set; } = new();

    /// <summary>
    /// 服务器 JAR 配置（launch_method = "java" 时使用）
    /// </summary>
    [JsonPropertyName("server")]
    public ServerJarConfig Server { get; set; } = new();

    /// <summary>
    /// 内存配置（launch_method = "java" 时使用）
    /// </summary>
    [JsonPropertyName("memory")]
    public MemoryConfig Memory { get; set; } = new();

    /// <summary>
    /// JVM 参数配置（launch_method = "java" 时使用）
    /// </summary>
    [JsonPropertyName("arguments")]
    public ArgumentsConfig Arguments { get; set; } = new();

    /// <summary>
    /// 脚本启动配置（launch_method = "script" 时使用）
    /// </summary>
    [JsonPropertyName("script")]
    public ScriptConfig Script { get; set; } = new();

    /// <summary>
    /// 进程监控配置
    /// </summary>
    [JsonPropertyName("monitoring")]
    public MonitoringConfig Monitoring { get; set; } = new();

    /// <summary>
    /// 自动重启配置
    /// </summary>
    [JsonPropertyName("auto_restart")]
    public AutoRestartConfig AutoRestart { get; set; } = new();
}

/// <summary>
/// Java 配置
/// </summary>
public class JavaConfig
{
    /// <summary>
    /// Java 可执行文件路径
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = "java";

    /// <summary>
    /// 是否检查 Java 版本
    /// </summary>
    [JsonPropertyName("version_check")]
    public bool VersionCheck { get; set; } = true;
}

/// <summary>
/// 服务器 JAR 配置
/// </summary>
public class ServerJarConfig
{
    /// <summary>
    /// 服务器 JAR 文件名
    /// </summary>
    [JsonPropertyName("jar")]
    public string Jar { get; set; } = "server.jar";

    /// <summary>
    /// 服务器工作目录
    /// </summary>
    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = "./minecraft_server";
}

/// <summary>
/// 内存配置
/// </summary>
public class MemoryConfig
{
    /// <summary>
    /// 最小内存 (MB)
    /// </summary>
    [JsonPropertyName("min")]
    public int Min { get; set; } = 2048;

    /// <summary>
    /// 最大内存 (MB)
    /// </summary>
    [JsonPropertyName("max")]
    public int Max { get; set; } = 4096;
}

/// <summary>
/// JVM 和服务器参数配置
/// </summary>
public class ArgumentsConfig
{
    /// <summary>
    /// JVM 前缀参数（在 -Xms/-Xmx 之前）
    /// </summary>
    [JsonPropertyName("jvm_prefix")]
    public List<string> JvmPrefix { get; set; } = new();

    /// <summary>
    /// JVM 中间参数（在 -Xms/-Xmx 之后，-jar 之前）
    /// </summary>
    [JsonPropertyName("jvm_middle")]
    public List<string> JvmMiddle { get; set; } = new();

    /// <summary>
    /// 服务器参数（在 JAR 文件名之后）
    /// </summary>
    [JsonPropertyName("server")]
    public List<string> Server { get; set; } = new();
}

/// <summary>
/// 脚本启动配置
/// </summary>
public class ScriptConfig
{
    /// <summary>
    /// 脚本文件路径（支持 .bat, .sh, .cmd, .exe）
    /// 相对于服务器工作目录或绝对路径
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = "./start.sh";

    /// <summary>
    /// 脚本参数
    /// </summary>
    [JsonPropertyName("arguments")]
    public List<string> Arguments { get; set; } = new();

    /// <summary>
    /// 工作目录（脚本执行的目录）
    /// </summary>
    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = "./minecraft_server";

    /// <summary>
    /// 是否使用 Shell 执行（Linux/macOS）
    /// true: 使用 sh -c "script"
    /// false: 直接执行脚本
    /// </summary>
    [JsonPropertyName("use_shell")]
    public bool UseShell { get; set; } = false;
}

/// <summary>
/// 监控配置
/// </summary>
public class MonitoringConfig
{
    /// <summary>
    /// 启动超时时间（秒）
    /// </summary>
    [JsonPropertyName("startup_timeout")]
    public int StartupTimeout { get; set; } = 300;

    /// <summary>
    /// 是否显示服务器输出
    /// </summary>
    [JsonPropertyName("show_server_output")]
    public bool ShowServerOutput { get; set; } = true;

    /// <summary>
    /// 服务器输出前缀
    /// </summary>
    [JsonPropertyName("server_output_prefix")]
    public string ServerOutputPrefix { get; set; } = "[MC] ";

    /// <summary>
    /// 启动检测配置
    /// </summary>
    [JsonPropertyName("startup_detection")]
    public StartupDetectionConfig StartupDetection { get; set; } = new();

    /// <summary>
    /// 崩溃检测配置
    /// </summary>
    [JsonPropertyName("crash_detection")]
    public CrashDetectionConfig CrashDetection { get; set; } = new();
}

/// <summary>
/// 启动检测配置
/// </summary>
public class StartupDetectionConfig
{
    /// <summary>
    /// 是否启用启动检测
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 启动关键字列表
    /// </summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new() { "Done (" };
}

/// <summary>
/// 崩溃检测配置
/// </summary>
public class CrashDetectionConfig
{
    /// <summary>
    /// 是否启用崩溃检测
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 崩溃关键字列表
    /// </summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new() { "Exception in server tick loop" };
}

/// <summary>
/// 自动重启配置
/// </summary>
public class AutoRestartConfig
{
    /// <summary>
    /// 是否启用自动重启
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 重启延迟（秒）
    /// </summary>
    [JsonPropertyName("delay_seconds")]
    public int DelaySeconds { get; set; } = 5;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [JsonPropertyName("max_retries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    [JsonPropertyName("retry_delay")]
    public int RetryDelay { get; set; } = 5000;

    /// <summary>
    /// 重置计时器（毫秒）
    /// </summary>
    [JsonPropertyName("reset_timer")]
    public int ResetTimer { get; set; } = 600000;
}

/// <summary>
/// 服务器连接配置 (SMP)
/// </summary>
public class ServerConnectionConfig
{
    /// <summary>
    /// 服务器主机地址
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 服务器端口
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 40745;

    /// <summary>
    /// 连接密钥
    /// </summary>
    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 是否使用 TLS
    /// </summary>
    [JsonPropertyName("use_tls")]
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// TLS 证书路径
    /// </summary>
    [JsonPropertyName("tls_certificate")]
    public string? TlsCertificate { get; set; }

    /// <summary>
    /// TLS 证书密码
    /// </summary>
    [JsonPropertyName("tls_password")]
    public string? TlsPassword { get; set; }

    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    [JsonPropertyName("reconnect_interval")]
    public int ReconnectInterval { get; set; } = 5000;

    /// <summary>
    /// 心跳超时（毫秒）
    /// </summary>
    [JsonPropertyName("heartbeat_timeout")]
    public int HeartbeatTimeout { get; set; } = 30000;

    /// <summary>
    /// 是否自动连接
    /// </summary>
    [JsonPropertyName("auto_connect")]
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// 连接延迟（毫秒）
    /// </summary>
    [JsonPropertyName("connect_delay")]
    public int ConnectDelay { get; set; } = 3000;

    /// <summary>
    /// 是否等待服务器就绪
    /// </summary>
    [JsonPropertyName("wait_for_server_ready")]
    public bool WaitForServerReady { get; set; } = true;
}

/// <summary>
/// RCON 配置
/// </summary>
public class RconConfig
{
    /// <summary>
    /// 是否启用 RCON
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// RCON 主机地址
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RCON 端口
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 25566;

    /// <summary>
    /// RCON 密码
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 连接超时（毫秒）
    /// </summary>
    [JsonPropertyName("connect_timeout")]
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 命令超时（毫秒）
    /// </summary>
    [JsonPropertyName("command_timeout")]
    public int CommandTimeout { get; set; } = 10000;

    /// <summary>
    /// 是否自动连接
    /// </summary>
    [JsonPropertyName("auto_connect")]
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// 连接延迟（毫秒）
    /// </summary>
    [JsonPropertyName("connect_delay")]
    public int ConnectDelay { get; set; } = 3000;

    /// <summary>
    /// 重连配置
    /// </summary>
    [JsonPropertyName("reconnect")]
    public ReconnectConfig Reconnect { get; set; } = new();
}

/// <summary>
/// 重连配置
/// </summary>
public class ReconnectConfig
{
    /// <summary>
    /// 是否启用重连
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 5000;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [JsonPropertyName("max_retries")]
    public int MaxRetries { get; set; } = 5;
}

/// <summary>
/// 日志监听器配置
/// </summary>
public class LogListenerConfig
{
    /// <summary>
    /// 是否启用日志监听器
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 解析配置
    /// </summary>
    [JsonPropertyName("parsing")]
    public ParsingConfig Parsing { get; set; } = new();

    /// <summary>
    /// 过滤配置
    /// </summary>
    [JsonPropertyName("filters")]
    public FiltersConfig Filters { get; set; } = new();

    /// <summary>
    /// 缓冲配置
    /// </summary>
    [JsonPropertyName("buffer")]
    public BufferConfig Buffer { get; set; } = new();
}

/// <summary>
/// 日志解析配置
/// </summary>
public class ParsingConfig
{
    /// <summary>
    /// 是否解析聊天消息
    /// </summary>
    [JsonPropertyName("parse_chat")]
    public bool ParseChat { get; set; } = true;

    /// <summary>
    /// 是否解析命令
    /// </summary>
    [JsonPropertyName("parse_commands")]
    public bool ParseCommands { get; set; } = true;

    /// <summary>
    /// 是否解析玩家事件
    /// </summary>
    [JsonPropertyName("parse_player_events")]
    public bool ParsePlayerEvents { get; set; } = true;

    /// <summary>
    /// 是否解析错误
    /// </summary>
    [JsonPropertyName("parse_errors")]
    public bool ParseErrors { get; set; } = true;
}

/// <summary>
/// 日志过滤配置
/// </summary>
public class FiltersConfig
{
    /// <summary>
    /// 忽略模式列表
    /// </summary>
    [JsonPropertyName("ignore_patterns")]
    public List<string> IgnorePatterns { get; set; } = new();

    /// <summary>
    /// 日志级别列表
    /// </summary>
    [JsonPropertyName("log_levels")]
    public List<string> LogLevels { get; set; } = new() { "INFO", "WARN", "ERROR" };
}

/// <summary>
/// 日志缓冲配置
/// </summary>
public class BufferConfig
{
    /// <summary>
    /// 缓冲区大小
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; } = 1000;

    /// <summary>
    /// 是否丢弃旧数据
    /// </summary>
    [JsonPropertyName("drop_old")]
    public bool DropOld { get; set; } = true;
}

/// <summary>
/// 插件配置
/// </summary>
public class PluginConfig
{
    /// <summary>
    /// 插件目录
    /// </summary>
    [JsonPropertyName("directory")]
    public string Directory { get; set; } = "plugins";

    /// <summary>
    /// 是否自动加载插件
    /// </summary>
    [JsonPropertyName("auto_load")]
    public bool AutoLoad { get; set; } = true;

    /// <summary>
    /// 是否支持热重载
    /// </summary>
    [JsonPropertyName("hot_reload")]
    public bool HotReload { get; set; } = true;

    /// <summary>
    /// 启用的插件列表
    /// </summary>
    [JsonPropertyName("enabled_plugins")]
    public List<string> EnabledPlugins { get; set; } = new() { "*" };

    /// <summary>
    /// 禁用的插件列表
    /// </summary>
    [JsonPropertyName("disabled_plugins")]
    public List<string> DisabledPlugins { get; set; } = new();

    /// <summary>
    /// 是否在服务器就绪后加载插件
    /// </summary>
    [JsonPropertyName("load_after_server_ready")]
    public bool LoadAfterServerReady { get; set; } = true;

    /// <summary>
    /// 插件加载超时（秒）
    /// </summary>
    [JsonPropertyName("load_timeout")]
    public int LoadTimeout { get; set; } = 30;

    /// <summary>
    /// 依赖管理配置
    /// </summary>
    [JsonPropertyName("dependency_management")]
    public DependencyManagementConfig DependencyManagement { get; set; } = new();
}

/// <summary>
/// 依赖管理配置
/// </summary>
public class DependencyManagementConfig
{
    /// <summary>
    /// 是否启用依赖管理
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否自动下载缺失的依赖
    /// </summary>
    [JsonPropertyName("auto_download")]
    public bool AutoDownload { get; set; } = true;

    /// <summary>
    /// NuGet 源列表
    /// </summary>
    [JsonPropertyName("nuget_sources")]
    public List<string> NugetSources { get; set; } = new()
    {
        "https://api.nuget.org/v3/index.json"
    };

    /// <summary>
    /// 下载超时时间（秒）
    /// </summary>
    [JsonPropertyName("download_timeout")]
    public int DownloadTimeout { get; set; } = 60;

    /// <summary>
    /// 是否验证文件哈希
    /// </summary>
    [JsonPropertyName("verify_hash")]
    public bool VerifyHash { get; set; } = true;

    /// <summary>
    /// 冲突解决策略：highest | lowest | fail
    /// </summary>
    [JsonPropertyName("conflict_resolution")]
    public string ConflictResolution { get; set; } = "highest";

    /// <summary>
    /// 是否在加载前显示依赖冲突报告
    /// </summary>
    [JsonPropertyName("show_conflict_report")]
    public bool ShowConflictReport { get; set; } = true;
}

/// <summary>
/// 日志系统配置
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// 日志级别
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; set; } = "Info";

    /// <summary>
    /// 控制台日志配置
    /// </summary>
    [JsonPropertyName("console")]
    public ConsoleLogConfig Console { get; set; } = new();

    /// <summary>
    /// 文件日志配置
    /// </summary>
    [JsonPropertyName("file")]
    public FileLogConfig File { get; set; } = new();

    /// <summary>
    /// 高级日志配置
    /// </summary>
    [JsonPropertyName("advanced")]
    public LoggingAdvancedConfig Advanced { get; set; } = new();
}

/// <summary>
/// 控制台日志配置
/// </summary>
public class ConsoleLogConfig
{
    /// <summary>
    /// 是否启用控制台日志
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否彩色输出
    /// </summary>
    [JsonPropertyName("colored")]
    public bool Colored { get; set; } = true;

    /// <summary>
    /// 是否显示服务器输出
    /// </summary>
    [JsonPropertyName("show_server_output")]
    public bool ShowServerOutput { get; set; } = true;

    /// <summary>
    /// 服务器输出前缀
    /// </summary>
    [JsonPropertyName("server_output_prefix")]
    public string ServerOutputPrefix { get; set; } = "[MC] ";
}

/// <summary>
/// 文件日志配置
/// </summary>
public class FileLogConfig
{
    /// <summary>
    /// 是否启用文件日志
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = "logs/latest.log";

    /// <summary>
    /// 最大文件大小（字节）
    /// </summary>
    [JsonPropertyName("max_size")]
    public int MaxSize { get; set; } = 10485760;

    /// <summary>
    /// 最大文件数量
    /// </summary>
    [JsonPropertyName("max_files")]
    public int MaxFiles { get; set; } = 10;

    /// <summary>
    /// 是否滚动日志
    /// </summary>
    [JsonPropertyName("rolling")]
    public bool Rolling { get; set; } = true;

    /// <summary>
    /// 文件编码
    /// </summary>
    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "UTF-8";
}

/// <summary>
/// 高级日志配置
/// </summary>
public class LoggingAdvancedConfig
{
    /// <summary>
    /// 是否记录插件加载日志
    /// </summary>
    [JsonPropertyName("log_plugin_loading")]
    public bool LogPluginLoading { get; set; } = true;

    /// <summary>
    /// 是否记录 SMP 通信日志
    /// </summary>
    [JsonPropertyName("log_smp_communication")]
    public bool LogSmpCommunication { get; set; } = false;

    /// <summary>
    /// 是否记录 RCON 通信日志
    /// </summary>
    [JsonPropertyName("log_rcon_communication")]
    public bool LogRconCommunication { get; set; } = false;

    /// <summary>
    /// 是否记录性能日志
    /// </summary>
    [JsonPropertyName("log_performance")]
    public bool LogPerformance { get; set; } = false;
}

/// <summary>
/// 高级配置
/// </summary>
public class AdvancedConfig
{
    /// <summary>
    /// 性能配置
    /// </summary>
    [JsonPropertyName("performance")]
    public PerformanceConfig Performance { get; set; } = new();

    /// <summary>
    /// 安全配置
    /// </summary>
    [JsonPropertyName("security")]
    public SecurityConfig Security { get; set; } = new();

    /// <summary>
    /// 实验性功能配置
    /// </summary>
    [JsonPropertyName("experimental")]
    public ExperimentalConfig Experimental { get; set; } = new();
}

/// <summary>
/// 性能配置
/// </summary>
public class PerformanceConfig
{
    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 报告间隔（秒）
    /// </summary>
    [JsonPropertyName("report_interval")]
    public int ReportInterval { get; set; } = 60;
}

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfig
{
    /// <summary>
    /// 是否在日志中隐藏密钥
    /// </summary>
    [JsonPropertyName("hide_secrets_in_logs")]
    public bool HideSecretsInLogs { get; set; } = true;

    /// <summary>
    /// 是否启用插件沙箱
    /// </summary>
    [JsonPropertyName("plugin_sandbox")]
    public bool PluginSandbox { get; set; } = false;
}

/// <summary>
/// 实验性功能配置
/// </summary>
public class ExperimentalConfig
{
    /// <summary>
    /// 是否启用实验性功能
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 是否启用热重载
    /// </summary>
    [JsonPropertyName("hot_reload")]
    public bool HotReload { get; set; } = false;

    /// <summary>
    /// 是否启用 Web 界面
    /// </summary>
    [JsonPropertyName("web_interface")]
    public bool WebInterface { get; set; } = false;
}

/// <summary>
/// spark 性能监控配置
/// </summary>
public class SparkConfig
{
    /// <summary>
    /// 是否启用 spark
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// spark 类型: "standalone" 或 "plugin"
    /// standalone: 独立代理版（通过 -javaagent 注入）
    /// plugin: 插件/模组版（通过 RCON 交互）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "standalone";

    /// <summary>
    /// 是否强制在非 java 启动模式下也尝试启用 spark
    /// 仅对 type="standalone" 有效
    /// 警告：脚本模式需要自行在脚本中添加 -javaagent 参数
    /// </summary>
    [JsonPropertyName("force_enable_for_script_mode")]
    public bool ForceEnableForScriptMode { get; set; } = false;

    /// <summary>
    /// 是否自动下载 spark agent
    /// 仅对 type="standalone" 有效
    /// false: 必须手动提供 agent_jar 路径
    /// true: 如果 agent_jar 为空，自动下载
    /// </summary>
    [JsonPropertyName("auto_download")]
    public bool AutoDownload { get; set; } = true;

    /// <summary>
    /// spark agent jar 文件路径
    /// 仅对 type="standalone" 有效
    /// 如果为空且 auto_download = true，将自动下载
    /// </summary>
    [JsonPropertyName("agent_jar")]
    public string? AgentJar { get; set; }

    /// <summary>
    /// spark SSH 监听端口
    /// </summary>
    [JsonPropertyName("ssh_port")]
    public int SshPort { get; set; } = 2222;

    /// <summary>
    /// SSH 密码（如果为空则使用随机生成的密码）
    /// </summary>
    [JsonPropertyName("ssh_password")]
    public string? SshPassword { get; set; }

    /// <summary>
    /// 启动时自动开始性能分析
    /// </summary>
    [JsonPropertyName("auto_start_profiling")]
    public bool AutoStartProfiling { get; set; } = false;

    /// <summary>
    /// 自动下载 spark 的版本
    /// 例如: "1.10.53"，留空使用最新版本
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// spark 下载镜像 URL
    /// 默认使用官方 GitHub Releases
    /// </summary>
    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = "https://spark.lucko.me/download/stable";
}

