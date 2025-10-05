using NetherGate.API.Configuration;
using NetherGate.API.Logging;
using System.Net.Http;
using System.Security.Cryptography;

namespace NetherGate.Core.Performance;

/// <summary>
/// spark Standalone Agent 管理器
/// 负责下载、安装和管理 spark 性能监控代理
/// 参考: https://spark.lucko.me/docs/Standalone-Agent
/// </summary>
public class SparkAgentManager
{
    private readonly ILogger _logger;
    private readonly SparkConfig _config;
    private readonly string _serverWorkingDir;
    private readonly HttpClient _httpClient;
    private string? _agentJarPath;
    private string? _sshPassword;

    public SparkAgentManager(SparkConfig config, string serverWorkingDir, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _serverWorkingDir = serverWorkingDir;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// 检查是否应该为指定的启动模式启用 spark standalone agent
    /// </summary>
    /// <param name="isJavaMode">是否为 Java 启动模式</param>
    public bool ShouldEnableForLaunchMode(bool isJavaMode)
    {
        if (!_config.Enabled)
        {
            return false;
        }

        // 检查 spark 类型
        if (_config.Type != "standalone")
        {
            _logger.Debug($"spark 类型为 '{_config.Type}'，跳过 standalone agent 初始化");
            return false;
        }

        // Java 模式总是可以启用
        if (isJavaMode)
        {
            return true;
        }

        // 脚本模式需要强制启用标志
        if (_config.ForceEnableForScriptMode)
        {
            _logger.Warning("脚本启动模式下强制启用 spark standalone agent (force_enable_for_script_mode = true)");
            _logger.Warning("请确保在启动脚本中包含 -javaagent 参数");
            return true;
        }

        _logger.Info("spark standalone agent 已启用但当前为脚本模式，已跳过 (设置 force_enable_for_script_mode = true 以强制启用)");
        return false;
    }

    /// <summary>
    /// 确保 spark agent 已就绪（下载/验证）
    /// </summary>
    public async Task<bool> EnsureAgentReadyAsync()
    {
        if (!_config.Enabled)
        {
            return false;
        }

        try
        {
            // 确保服务器工作目录存在
            Directory.CreateDirectory(_serverWorkingDir);

            // 使用配置的路径或自动下载
            if (!string.IsNullOrEmpty(_config.AgentJar))
            {
                // 用户指定了路径
                var agentPath = Path.IsPathRooted(_config.AgentJar) 
                    ? _config.AgentJar 
                    : Path.Combine(_serverWorkingDir, _config.AgentJar);

                if (File.Exists(agentPath))
                {
                    _agentJarPath = agentPath;
                    _logger.Info($"使用指定的 spark agent: {_agentJarPath}");
                }
                else
                {
                    _logger.Error($"指定的 spark agent 不存在: {agentPath}");
                    return false;
                }
            }
            else if (_config.AutoDownload)
            {
                // 自动下载到服务器工作目录
                _logger.Info("未指定 spark agent 路径，准备自动下载到服务器目录...");
                _agentJarPath = await DownloadSparkAgentAsync();
                if (_agentJarPath == null)
                {
                    _logger.Error("spark agent 自动下载失败");
                    return false;
                }
            }
            else
            {
                // 既没有指定路径，也禁用了自动下载
                _logger.Error("spark agent 未配置，且自动下载已禁用 (auto_download = false)");
                _logger.Info("请设置 agent_jar 路径或启用 auto_download");
                return false;
            }

            // 生成或使用配置的 SSH 密码
            _sshPassword = _config.SshPassword ?? GenerateRandomPassword();

            _logger.Info($"spark agent 已就绪: {Path.GetFileName(_agentJarPath)}");
            _logger.Info($"spark agent 位置: {_agentJarPath}");
            _logger.Info($"spark SSH 端口: {_config.SshPort}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"准备 spark agent 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 获取 spark agent 的 JVM 参数
    /// </summary>
    public string? GetAgentJvmArgument()
    {
        if (_agentJarPath == null || !_config.Enabled)
        {
            return null;
        }

        var args = new List<string> { $"port={_config.SshPort}" };

        if (_config.AutoStartProfiling)
        {
            args.Add("start");
        }

        var agentArg = $"-javaagent:{Path.GetFullPath(_agentJarPath)}";
        if (args.Count > 0)
        {
            agentArg += $"={string.Join(",", args)}";
        }

        return agentArg;
    }

    /// <summary>
    /// 获取 SSH 连接信息
    /// </summary>
    public (int port, string password)? GetSshConnectionInfo()
    {
        if (!_config.Enabled || _sshPassword == null)
        {
            return null;
        }

        return (_config.SshPort, _sshPassword);
    }

    /// <summary>
    /// 打印 SSH 连接指令
    /// </summary>
    /// <param name="isJavaMode">是否为 Java 启动模式</param>
    public void PrintConnectionInstructions(bool isJavaMode)
    {
        if (!_config.Enabled || _sshPassword == null)
        {
            return;
        }

        _logger.Info("=".PadRight(60, '='));
        _logger.Info("spark Standalone Agent 已启动");
        _logger.Info("=".PadRight(60, '='));
        
        // 如果是脚本模式且强制启用，显示警告
        if (!isJavaMode && _config.ForceEnableForScriptMode)
        {
            _logger.Warning("注意: 当前为脚本启动模式");
            _logger.Warning("需要在启动脚本中手动添加以下 JVM 参数:");
            _logger.Warning($"  {GetAgentJvmArgument()}");
            _logger.Info("");
        }
        
        _logger.Info($"SSH 端口: {_config.SshPort}");
        _logger.Info($"SSH 密码: {_sshPassword}");
        _logger.Info("");
        _logger.Info("连接命令:");
        _logger.Info($"  ssh -p {_config.SshPort} spark@localhost");
        _logger.Info("");
        _logger.Info("可用命令:");
        _logger.Info("  /spark profiler start        - 开始性能分析");
        _logger.Info("  /spark profiler stop         - 停止并生成报告");
        _logger.Info("  /spark tps                   - 查看 TPS");
        _logger.Info("  /spark health                - 查看服务器健康状况");
        _logger.Info("  exit                         - 断开连接");
        _logger.Info("=".PadRight(60, '='));
    }

    /// <summary>
    /// 下载 spark agent 到服务器工作目录
    /// </summary>
    private async Task<string?> DownloadSparkAgentAsync()
    {
        try
        {
            var version = _config.Version ?? "latest";
            var fileName = version == "latest" 
                ? "spark-standalone-agent.jar" 
                : $"spark-{version}-standalone-agent.jar";
            var targetPath = Path.Combine(_serverWorkingDir, fileName);

            // 如果文件已存在，跳过下载
            if (File.Exists(targetPath))
            {
                _logger.Info($"spark agent 已存在于服务器目录: {fileName}");
                return targetPath;
            }

            _logger.Info($"正在下载 spark agent ({version}) 到服务器目录...");

            // 构建下载 URL
            var downloadUrl = _config.DownloadUrl;
            if (!downloadUrl.Contains("spark"))
            {
                // 如果是自定义 URL 模板
                downloadUrl = downloadUrl.Replace("{version}", version);
            }

            // 下载文件
            var response = await _httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(targetPath, content);

            _logger.Info($"spark agent 下载完成: {fileName} ({content.Length / 1024} KB)");
            _logger.Info($"保存位置: {targetPath}");
            return targetPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"下载 spark agent 失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 生成随机密码
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }

        return new string(random.Select(b => chars[b % chars.Length]).ToArray());
    }
}

