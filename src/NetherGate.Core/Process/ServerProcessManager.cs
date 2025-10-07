using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.Core.Performance;
using System.Diagnostics;
using System.Text;

namespace NetherGate.Core.Process;

/// <summary>
/// Minecraft 服务器进程管理器
/// 负责启动、停止、监听服务器进程
/// </summary>
public class ServerProcessManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ServerProcessConfig _config;
    private readonly LogParser _logParser;
    private readonly SparkAgentManager? _sparkAgent;

    private System.Diagnostics.Process? _process;
    private StreamWriter? _inputWriter;
    private bool _disposed;
    private bool _isRunning;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<int>? ProcessExited;

    public bool IsRunning => _isRunning && _process != null && !_process.HasExited;
    public int? ProcessId => _process?.Id;

    public ServerProcessManager(
        ServerProcessConfig config,
        ILogger logger,
        IEventBus eventBus,
        SparkAgentManager? sparkAgent = null)
    {
        _config = config;
        _logger = logger;
        _eventBus = eventBus;
        _logParser = new LogParser(logger, eventBus);
        _sparkAgent = sparkAgent;
    }

    /// <summary>
    /// 启动服务器
    /// </summary>
    public async Task<bool> StartAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServerProcessManager));

        if (IsRunning)
        {
            _logger.Warning("服务器已在运行中");
            return false;
        }

        // External 模式：服务器已经在运行，不需要启动
        if (_config.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info("External 模式：跳过服务器进程启动，将直接连接到已运行的服务器");
            _logger.Info($"服务器工作目录: {_config.Server.WorkingDirectory}");
            
            _isRunning = true;  // 标记为运行状态，以便其他功能可以正常工作
            
            // 发布启动事件（虽然实际没有启动进程）
            await _eventBus.PublishAsync(new ServerProcessStartedEvent
            {
                ProcessId = 0  // External 模式没有进程 ID
            });
            
            return true;
        }

        try
        {
_logger.Info("正在启动 Minecraft 服务器...");

            var startInfo = CreateProcessStartInfo();
            
            _process = new System.Diagnostics.Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            // 订阅事件
            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
            _process.Exited += OnProcessExited;

            // 启动进程
            if (!_process.Start())
            {
                _logger.Error("无法启动服务器进程");
                return false;
            }

            _isRunning = true;
            _inputWriter = _process.StandardInput;

            // 开始异步读取输出
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _logger.Info($"服务器进程已启动 (PID: {_process.Id})");

            // 显示 spark 连接信息（如果启用）
            var isJavaMode = _config.LaunchMethod == "java";
            _sparkAgent?.PrintConnectionInstructions(isJavaMode);

            // 发布启动事件
            await _eventBus.PublishAsync(new ServerProcessStartedEvent
            {
                ProcessId = _process.Id
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"启动服务器失败: {ex.Message}", ex);
            _isRunning = false;
            return false;
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public async Task<bool> StopAsync(int timeoutMs = 60000)
    {
        if (!IsRunning)
        {
            _logger.Warning("服务器未运行");
            return false;
        }

        // External 模式：不管理进程，仅更新状态
        if (_config.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info("External 模式：不停止服务器进程（由外部管理）");
            _isRunning = false;
            return true;
        }

        if (_process == null)
        {
            _logger.Warning("服务器进程不存在");
            return false;
        }

        try
        {
            _logger.Info("========================================");
            _logger.Info("正在停止 Minecraft 服务器...");
            _logger.Info("========================================");

            // 检查进程是否已经退出
            if (_process.HasExited)
            {
                _logger.Info("服务器进程已退出");
                _isRunning = false;
                return true;
            }

            // 尝试发送 stop 命令（优雅关闭）
            bool stopCommandSent = false;
            try
            {
                _logger.Info("发送 stop 命令到服务器...");
                await SendCommandAsync("stop");
                stopCommandSent = true;
                _logger.Info("stop 命令已发送，等待服务器保存数据并退出...");
            }
            catch (Exception ex)
            {
                _logger.Warning($"无法发送 stop 命令: {ex.Message}");
                _logger.Warning("将尝试等待进程自然退出或强制终止");
            }

            // 等待进程正常退出（给予足够的时间）
            _logger.Info($"等待服务器退出（最多 {timeoutMs/1000} 秒）...");
            var exited = _process.WaitForExit(timeoutMs);

            if (exited)
            {
                _logger.Info("✓ 服务器已正常退出");
                _isRunning = false;
                return true;
            }

            // 超时处理
            _logger.Warning($"⚠ 服务器未在 {timeoutMs/1000} 秒内退出");
            
            if (stopCommandSent)
            {
                // 如果已经发送了 stop 命令，再给一些额外时间
                _logger.Warning("正在保存世界数据，再等待 30 秒...");
                exited = _process.WaitForExit(30000);
                
                if (exited)
                {
                    _logger.Info("✓ 服务器已退出");
                    _isRunning = false;
                    return true;
                }
            }

            // 仍未退出，强制终止
            _logger.Warning("⚠ 服务器仍未退出，将强制终止进程");
            _logger.Warning("⚠ 注意：强制终止可能导致数据损坏或文件锁定");
            
            try
            {
                _process.Kill(entireProcessTree: true); // 终止整个进程树
                _process.WaitForExit(5000);
                _logger.Warning("✓ 服务器进程已强制终止");
            }
            catch (Exception killEx)
            {
                _logger.Error($"强制终止失败: {killEx.Message}");
            }

            _isRunning = false;
            
            // 清理可能遗留的锁文件
            await CleanupServerLocksAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"停止服务器时发生错误: {ex.Message}", ex);
            _isRunning = false;
            
            // 尝试清理锁文件
            await CleanupServerLocksAsync();
            
            return false;
        }
    }

    /// <summary>
    /// 清理服务器锁文件
    /// </summary>
    private async Task CleanupServerLocksAsync()
    {
        try
        {
            var workingDir = _config.Server.WorkingDirectory;
            
            // 清理主世界的 session.lock
            var sessionLockPath = Path.Combine(workingDir, "world", "session.lock");
            if (File.Exists(sessionLockPath))
            {
                _logger.Info($"清理锁文件: {sessionLockPath}");
                await Task.Run(() => File.Delete(sessionLockPath));
            }

            // 清理下界的 session.lock
            var netherLockPath = Path.Combine(workingDir, "world_nether", "session.lock");
            if (File.Exists(netherLockPath))
            {
                _logger.Info($"清理锁文件: {netherLockPath}");
                await Task.Run(() => File.Delete(netherLockPath));
            }

            // 清理末地的 session.lock
            var endLockPath = Path.Combine(workingDir, "world_the_end", "session.lock");
            if (File.Exists(endLockPath))
            {
                _logger.Info($"清理锁文件: {endLockPath}");
                await Task.Run(() => File.Delete(endLockPath));
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"清理锁文件时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 向服务器发送命令
    /// </summary>
    public async Task SendCommandAsync(string command)
    {
        if (!IsRunning)
            throw new InvalidOperationException("服务器未运行");

        // External 模式：不支持直接发送命令到标准输入
        if (_config.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("External 模式：无法通过标准输入发送命令，请使用 RCON");
            throw new InvalidOperationException("External 模式不支持通过标准输入发送命令，请使用 RCON");
        }

        if (_inputWriter == null)
            throw new InvalidOperationException("标准输入未就绪");

        try
        {
            await _inputWriter.WriteLineAsync(command);
            await _inputWriter.FlushAsync();
            _logger.Trace($"发送命令: {command}");
        }
        catch (Exception ex)
        {
            _logger.Error($"发送命令失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 创建进程启动信息
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo()
    {
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = _config.Server.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // 根据启动方式配置
        if (_config.LaunchMethod.Equals("java", StringComparison.OrdinalIgnoreCase))
        {
            // 构建 Java 命令
            startInfo.FileName = ResolveJavaPath();
            startInfo.Arguments = BuildJavaArguments();
        }
        else if (_config.LaunchMethod.Equals("script", StringComparison.OrdinalIgnoreCase))
        {
            // 使用脚本启动
            ConfigureScriptLaunch(startInfo);
        }
        else
        {
            throw new InvalidOperationException($"不支持的启动方式: {_config.LaunchMethod}。支持的方式：java, script, external");
        }

        _logger.Debug($"启动命令: {startInfo.FileName} {startInfo.Arguments}");

        return startInfo;
    }

    /// <summary>
    /// 解析 Java 路径
    /// </summary>
    private string ResolveJavaPath()
    {
        var javaPath = _config.Java.Path;

        // 如果是相对路径或环境变量
        if (!Path.IsPathRooted(javaPath))
        {
            // 尝试从环境变量获取
            var envJava = Environment.GetEnvironmentVariable(javaPath);
            if (!string.IsNullOrEmpty(envJava))
                return envJava;
        }

        return javaPath;
    }

    /// <summary>
    /// 构建 Java 启动参数
    /// </summary>
    private string BuildJavaArguments()
    {
        var args = new List<string>();

        // JVM 前缀参数
        args.AddRange(_config.Arguments.JvmPrefix);

        // spark agent 参数（如果启用且适用于当前启动模式）
        if (_sparkAgent != null && _sparkAgent.ShouldEnableForLaunchMode(isJavaMode: true))
        {
            var sparkAgentArg = _sparkAgent.GetAgentJvmArgument();
            if (!string.IsNullOrEmpty(sparkAgentArg))
            {
                args.Add(sparkAgentArg);
                _logger.Info("已启用 spark 性能监控 agent");
            }
        }

        // 内存参数
        args.Add($"-Xms{_config.Memory.Min}M");
        args.Add($"-Xmx{_config.Memory.Max}M");

        // JVM 中间参数
        args.AddRange(_config.Arguments.JvmMiddle);

        // JAR 文件
        args.Add("-Dfile.encoding=UTF-8");
        args.Add("-Dsun.stdout.encoding=UTF-8");
        args.Add("-Dsun.stderr.encoding=UTF-8");
        args.Add("-jar");
        // 引号包裹 JAR 路径，避免包含空格时解析错误
        var jarArg = _config.Server.Jar.Contains(' ') ? $"\"{_config.Server.Jar}\"" : _config.Server.Jar;
        args.Add(jarArg);

        // 服务器参数
        args.AddRange(_config.Arguments.Server);

        return string.Join(" ", args);
    }

    /// <summary>
    /// 配置脚本启动
    /// </summary>
    private void ConfigureScriptLaunch(ProcessStartInfo startInfo)
    {
        var scriptPath = _config.Script.Path;
        var extension = Path.GetExtension(scriptPath).ToLowerInvariant();

        // 解析脚本路径（处理相对路径）
        var resolvedScriptPath = ResolveScriptPath(scriptPath);

        startInfo.WorkingDirectory = _config.Script.WorkingDirectory;

        if (_config.Script.UseShell)
        {
            startInfo.UseShellExecute = true;
            startInfo.FileName = resolvedScriptPath;
        }
        else
        {
            // 根据脚本类型选择解释器
            switch (extension)
            {
                case ".bat":
                case ".cmd":
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c \"{resolvedScriptPath}\" {string.Join(" ", _config.Script.Arguments)}";
                    break;

                case ".sh":
                    startInfo.FileName = "bash";
                    startInfo.Arguments = $"\"{resolvedScriptPath}\" {string.Join(" ", _config.Script.Arguments)}";
                    break;

                case ".exe":
                    startInfo.FileName = resolvedScriptPath;
                    startInfo.Arguments = string.Join(" ", _config.Script.Arguments);
                    break;

                default:
                    throw new InvalidOperationException($"不支持的脚本类型: {extension}");
            }
        }
    }

    /// <summary>
    /// 解析脚本路径（将相对路径转换为绝对路径）
    /// </summary>
    private string ResolveScriptPath(string scriptPath)
    {
        // 如果已经是绝对路径，直接返回
        if (Path.IsPathRooted(scriptPath))
            return scriptPath;

        // 相对路径：基于当前工作目录解析
        var basePath = Directory.GetCurrentDirectory();
        var fullPath = Path.GetFullPath(Path.Combine(basePath, scriptPath));

        _logger.Debug($"脚本路径解析: {scriptPath} -> {fullPath}");

        // 检查文件是否存在
        if (!File.Exists(fullPath))
        {
            _logger.Warning($"脚本文件不存在: {fullPath}");
            _logger.Warning($"请确保脚本文件路径正确");
        }

        return fullPath;
    }

    /// <summary>
    /// 处理标准输出
    /// </summary>
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        try
        {
            // 触发输出事件
            OutputReceived?.Invoke(this, e.Data);

            // 如果配置了显示服务器输出，记录到日志
            if (_config.Monitoring.ShowServerOutput)
            {
                var prefix = _config.Monitoring.ServerOutputPrefix ?? "[Server] ";
                _logger.Info($"{prefix}{e.Data}");
            }

            // 解析日志
            _ = _logParser.ParseLineAsync(e.Data);
        }
        catch (Exception ex)
        {
            _logger.Error($"处理服务器输出失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理错误输出
    /// </summary>
    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        try
        {
            ErrorReceived?.Invoke(this, e.Data);
            _logger.Error($"[Server Error] {e.Data}");

            // 将错误输出也交给日志解析器，部分实现会在 stderr 打印关键事件
            _ = _logParser.ParseLineAsync(e.Data);
        }
        catch (Exception ex)
        {
            _logger.Error($"处理服务器错误输出失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理进程退出
    /// </summary>
    private async void OnProcessExited(object? sender, EventArgs e)
    {
        if (_process == null)
            return;

        var exitCode = _process.ExitCode;
        _isRunning = false;

        _logger.Info($"服务器进程已退出 (退出码: {exitCode})");

        ProcessExited?.Invoke(this, exitCode);

        // 发布退出事件
        if (exitCode == 0)
        {
            await _eventBus.PublishAsync(new ServerProcessStoppedEvent { ExitCode = exitCode });
        }
        else
        {
            await _eventBus.PublishAsync(new ServerProcessCrashedEvent { ExitCode = exitCode });
        }

        // 自动重启
        if (_config.AutoRestart.Enabled && exitCode != 0)
        {
            _logger.Info($"服务器崩溃，将在 {_config.AutoRestart.DelaySeconds} 秒后重启");
            await Task.Delay(_config.AutoRestart.DelaySeconds * 1000);

            if (!_disposed)
            {
                await StartAsync();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (IsRunning)
            {
                StopAsync().Wait();
            }

            _inputWriter?.Dispose();
            _process?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error($"释放 ServerProcessManager 失败: {ex.Message}", ex);
        }

        _logger.Debug("ServerProcessManager 已释放");
    }
}

