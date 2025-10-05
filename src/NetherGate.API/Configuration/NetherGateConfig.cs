using System.Text.Json.Serialization;

namespace NetherGate.API.Configuration;

/// <summary>
/// NetherGate 主配置
/// </summary>
public class NetherGateConfig
{
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

public class JavaConfig
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "java";

    [JsonPropertyName("version_check")]
    public bool VersionCheck { get; set; } = true;
}

public class ServerJarConfig
{
    [JsonPropertyName("jar")]
    public string Jar { get; set; } = "server.jar";

    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = "./minecraft_server";
}

public class MemoryConfig
{
    [JsonPropertyName("min")]
    public int Min { get; set; } = 2048;

    [JsonPropertyName("max")]
    public int Max { get; set; } = 4096;
}

public class ArgumentsConfig
{
    [JsonPropertyName("jvm_prefix")]
    public List<string> JvmPrefix { get; set; } = new();

    [JsonPropertyName("jvm_middle")]
    public List<string> JvmMiddle { get; set; } = new();

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

public class MonitoringConfig
{
    [JsonPropertyName("startup_timeout")]
    public int StartupTimeout { get; set; } = 300;

    [JsonPropertyName("show_server_output")]
    public bool ShowServerOutput { get; set; } = true;

    [JsonPropertyName("server_output_prefix")]
    public string ServerOutputPrefix { get; set; } = "[MC] ";

    [JsonPropertyName("startup_detection")]
    public StartupDetectionConfig StartupDetection { get; set; } = new();

    [JsonPropertyName("crash_detection")]
    public CrashDetectionConfig CrashDetection { get; set; } = new();
}

public class StartupDetectionConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new() { "Done (" };
}

public class CrashDetectionConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new() { "Exception in server tick loop" };
}

public class AutoRestartConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("delay_seconds")]
    public int DelaySeconds { get; set; } = 5;

    [JsonPropertyName("max_retries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("retry_delay")]
    public int RetryDelay { get; set; } = 5000;

    [JsonPropertyName("reset_timer")]
    public int ResetTimer { get; set; } = 600000;
}

/// <summary>
/// 服务器连接配置 (SMP)
/// </summary>
public class ServerConnectionConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 40745;

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("use_tls")]
    public bool UseTls { get; set; } = false;

    [JsonPropertyName("tls_certificate")]
    public string? TlsCertificate { get; set; }

    [JsonPropertyName("tls_password")]
    public string? TlsPassword { get; set; }

    [JsonPropertyName("reconnect_interval")]
    public int ReconnectInterval { get; set; } = 5000;

    [JsonPropertyName("heartbeat_timeout")]
    public int HeartbeatTimeout { get; set; } = 30000;

    [JsonPropertyName("auto_connect")]
    public bool AutoConnect { get; set; } = true;

    [JsonPropertyName("connect_delay")]
    public int ConnectDelay { get; set; } = 3000;

    [JsonPropertyName("wait_for_server_ready")]
    public bool WaitForServerReady { get; set; } = true;
}

/// <summary>
/// RCON 配置
/// </summary>
public class RconConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 25566;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("connect_timeout")]
    public int ConnectTimeout { get; set; } = 5000;

    [JsonPropertyName("command_timeout")]
    public int CommandTimeout { get; set; } = 10000;

    [JsonPropertyName("auto_connect")]
    public bool AutoConnect { get; set; } = true;

    [JsonPropertyName("connect_delay")]
    public int ConnectDelay { get; set; } = 3000;

    [JsonPropertyName("reconnect")]
    public ReconnectConfig Reconnect { get; set; } = new();
}

public class ReconnectConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 5000;

    [JsonPropertyName("max_retries")]
    public int MaxRetries { get; set; } = 5;
}

/// <summary>
/// 日志监听器配置
/// </summary>
public class LogListenerConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("parsing")]
    public ParsingConfig Parsing { get; set; } = new();

    [JsonPropertyName("filters")]
    public FiltersConfig Filters { get; set; } = new();

    [JsonPropertyName("buffer")]
    public BufferConfig Buffer { get; set; } = new();
}

public class ParsingConfig
{
    [JsonPropertyName("parse_chat")]
    public bool ParseChat { get; set; } = true;

    [JsonPropertyName("parse_commands")]
    public bool ParseCommands { get; set; } = true;

    [JsonPropertyName("parse_player_events")]
    public bool ParsePlayerEvents { get; set; } = true;

    [JsonPropertyName("parse_errors")]
    public bool ParseErrors { get; set; } = true;
}

public class FiltersConfig
{
    [JsonPropertyName("ignore_patterns")]
    public List<string> IgnorePatterns { get; set; } = new();

    [JsonPropertyName("log_levels")]
    public List<string> LogLevels { get; set; } = new() { "INFO", "WARN", "ERROR" };
}

public class BufferConfig
{
    [JsonPropertyName("size")]
    public int Size { get; set; } = 1000;

    [JsonPropertyName("drop_old")]
    public bool DropOld { get; set; } = true;
}

/// <summary>
/// 插件配置
/// </summary>
public class PluginConfig
{
    [JsonPropertyName("directory")]
    public string Directory { get; set; } = "plugins";

    [JsonPropertyName("auto_load")]
    public bool AutoLoad { get; set; } = true;

    [JsonPropertyName("hot_reload")]
    public bool HotReload { get; set; } = true;

    [JsonPropertyName("enabled_plugins")]
    public List<string> EnabledPlugins { get; set; } = new() { "*" };

    [JsonPropertyName("disabled_plugins")]
    public List<string> DisabledPlugins { get; set; } = new();

    [JsonPropertyName("load_after_server_ready")]
    public bool LoadAfterServerReady { get; set; } = true;

    [JsonPropertyName("load_timeout")]
    public int LoadTimeout { get; set; } = 30;

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
    [JsonPropertyName("level")]
    public string Level { get; set; } = "Info";

    [JsonPropertyName("console")]
    public ConsoleLogConfig Console { get; set; } = new();

    [JsonPropertyName("file")]
    public FileLogConfig File { get; set; } = new();

    [JsonPropertyName("advanced")]
    public LoggingAdvancedConfig Advanced { get; set; } = new();
}

public class ConsoleLogConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("colored")]
    public bool Colored { get; set; } = true;

    [JsonPropertyName("show_server_output")]
    public bool ShowServerOutput { get; set; } = true;

    [JsonPropertyName("server_output_prefix")]
    public string ServerOutputPrefix { get; set; } = "[MC] ";
}

public class FileLogConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("path")]
    public string Path { get; set; } = "logs/latest.log";

    [JsonPropertyName("max_size")]
    public int MaxSize { get; set; } = 10485760;

    [JsonPropertyName("max_files")]
    public int MaxFiles { get; set; } = 10;

    [JsonPropertyName("rolling")]
    public bool Rolling { get; set; } = true;

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "UTF-8";
}

public class LoggingAdvancedConfig
{
    [JsonPropertyName("log_plugin_loading")]
    public bool LogPluginLoading { get; set; } = true;

    [JsonPropertyName("log_smp_communication")]
    public bool LogSmpCommunication { get; set; } = false;

    [JsonPropertyName("log_rcon_communication")]
    public bool LogRconCommunication { get; set; } = false;

    [JsonPropertyName("log_performance")]
    public bool LogPerformance { get; set; } = false;
}

/// <summary>
/// 高级配置
/// </summary>
public class AdvancedConfig
{
    [JsonPropertyName("performance")]
    public PerformanceConfig Performance { get; set; } = new();

    [JsonPropertyName("security")]
    public SecurityConfig Security { get; set; } = new();

    [JsonPropertyName("experimental")]
    public ExperimentalConfig Experimental { get; set; } = new();
}

public class PerformanceConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("report_interval")]
    public int ReportInterval { get; set; } = 60;
}

public class SecurityConfig
{
    [JsonPropertyName("hide_secrets_in_logs")]
    public bool HideSecretsInLogs { get; set; } = true;

    [JsonPropertyName("plugin_sandbox")]
    public bool PluginSandbox { get; set; } = false;
}

public class ExperimentalConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("hot_reload")]
    public bool HotReload { get; set; } = false;

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

