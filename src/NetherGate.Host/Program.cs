using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;
using NetherGate.Core.Commands;
using NetherGate.Core.Configuration;
using NetherGate.Core.Events;
using NetherGate.Core.Logging;
using NetherGate.Core.Plugins;
using NetherGate.Core.Protocol;
using NetherGate.Core.WebSocket;
using NetherGate.Host.Cli;

namespace NetherGate.Host;

/// <summary>
/// 基于 .NET Generic Host 和 DI 的入口程序
/// </summary>
class Program
{
    private static NetherGateConfig? _config;
    private static WebSocketConfig? _wsConfig;
    
    static async Task<int> Main(string[] args)
    {
        // 解析 CLI 参数
        var cliArgs = CliArgumentParser.Parse(args);
        
        // 处理 CLI 命令（非交互模式）
        if (!cliArgs.IsInteractive)
        {
            return await CliCommandHandler.ExecuteAsync(cliArgs);
        }
        
        try
        {
            // 显示欢迎信息
            PrintBanner();
            Console.WriteLine("[NetherGate] 正在启动...");
            Console.WriteLine($"[NetherGate] 版本: 0.1.0-alpha");
            Console.WriteLine($"[NetherGate] .NET 版本: {Environment.Version}");
            Console.WriteLine();

            // 加载配置
            Console.WriteLine("[NetherGate] [1/3] 加载配置...");
            var configPath = ConfigurationLoader.GetConfigPath();
            if (!File.Exists(configPath))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ 未找到配置文件！");
                Console.ResetColor();
                Console.WriteLine();
                
                if (AskYesNo("是否运行配置向导？", defaultYes: true))
                {
                    Console.WriteLine();
                    var wizardResult = await ConfigurationWizard.RunAsync();
                    
                    if (wizardResult != 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("配置向导未完成。正在使用默认配置启动...");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("正在使用默认配置启动...");
                    Console.WriteLine();
                }
            }
            
            _config = ConfigurationLoader.Load();
            _wsConfig = WebSocketConfigLoader.Load();

            // 检查并自动配置 server.properties
            Console.WriteLine("[NetherGate] 检查服务器配置...");
            await CheckAndFixServerPropertiesAsync(_config);

            // 构建主机
            Console.WriteLine("[NetherGate] [2/3] 初始化服务...");
            var host = CreateHostBuilder(args).Build();

            // 启动服务
            Console.WriteLine("[NetherGate] [3/3] 启动 NetherGate...");
            await host.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetherGate] 启动失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("按任意键关闭窗口...");
            Console.ResetColor();
            Console.ReadKey(true);
            
            return 1;
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // 添加 NetherGate 核心服务
                services.AddNetherGate(_config!, _wsConfig!);
                
                // 添加托管服务
                services.AddHostedService<NetherGateHostedService>();
            })
            .UseConsoleLifetime();

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
    ███╗   ██╗███████╗████████╗██╗  ██╗███████╗██████╗  ██████╗  █████╗ ████████╗███████╗
    ████╗  ██║██╔════╝╚══██╔══╝██║  ██║██╔════╝██╔══██╗██╔════╝ ██╔══██╗╚══██╔══╝██╔════╝
    ██╔██╗ ██║█████╗     ██║   ███████║█████╗  ██████╔╝██║  ███╗███████║   ██║   ███████╗
    ██║╚██╗██║██╔══╝     ██║   ██╔══██║██╔══╝  ██╔══██╗██║   ██║██╔══██║   ██║   ╚════██║
    ██║ ╚████║███████╗   ██║   ██║  ██║███████╗██║  ██║╚██████╔╝██║  ██║   ██║   ███████║
    ╚═╝  ╚═══╝╚══════╝   ╚═╝   ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝
");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("    ═══════════════════════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("                    🌐 Minecraft Server Plugin Loader for .NET 🌐");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("    ═══════════════════════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
    }
    
    static bool AskYesNo(string prompt, bool defaultYes = true)
    {
        var defaultText = defaultYes ? "Y/n" : "y/N";
        Console.Write($"{prompt} [{defaultText}] ");
        
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (string.IsNullOrEmpty(input))
            return defaultYes;
        
        return input == "y" || input == "yes";
    }

    /// <summary>
    /// 检查并自动修正 server.properties 配置
    /// </summary>
    static Task CheckAndFixServerPropertiesAsync(NetherGateConfig config)
    {
        try
        {
            var serverDir = config.ServerProcess.Server.WorkingDirectory;
            var propertiesPath = Path.Combine(serverDir, "server.properties");

            // 检查文件是否存在
            if (!File.Exists(propertiesPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ 未找到 server.properties 文件: {propertiesPath}");
                Console.ResetColor();
                Console.WriteLine("  跳过配置检查，将使用 NetherGate 配置启动");
                return Task.CompletedTask;
            }

            // 创建临时日志记录器用于配置管理
            var tempLogger = new ConsoleLogger("ConfigCheck");
            var propsManager = new ServerPropertiesManager(tempLogger, serverDir);

            // 加载 server.properties
            if (!propsManager.Load())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ 无法读取 server.properties 文件");
                Console.ResetColor();
                return Task.CompletedTask;
            }

            // 检查并自动启用 RCON 和 SMP
            var modified = propsManager.EnsureRconAndSmpEnabled();
            
            // 同步配置到 NetherGate
            var configModified = propsManager.SyncToNetherGateConfig(config);

            // 显示配置信息
            var info = propsManager.GetInfo();
            Console.WriteLine($"  RCON: {(info.RconEnabled ? "✓ 已启用" : "✗ 禁用")} (端口: {info.RconPort})");
            Console.WriteLine($"  SMP:  {(info.SmpEnabled ? "✓ 已启用" : "✗ 禁用")} (端口: {info.SmpPort})");

            // 如果修改了 server.properties，保存并提示
            if (modified)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ 已自动修改 server.properties 以启用 RCON 和 SMP");
                Console.ResetColor();
                
                if (propsManager.Save())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ server.properties 已保存");
                    Console.ResetColor();
                    
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("⚠ 重要：请重启 Minecraft 服务器以应用新配置！");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            // 如果同步了配置，保存 NetherGate 配置
            if (configModified)
            {
                Console.WriteLine();
                Console.WriteLine("同步配置到 NetherGate...");
                
                var configPath = ConfigurationLoader.GetConfigPath();
                var format = configPath.EndsWith(".yaml") || configPath.EndsWith(".yml") 
                    ? ConfigFormat.Yaml 
                    : ConfigFormat.Json;
                
                ConfigurationLoader.SaveConfig(configPath, config, format);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ NetherGate 配置已更新并保存到: {configPath}");
                Console.ResetColor();
                Console.WriteLine();
            }
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ 配置检查失败: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("  将继续使用现有配置启动");
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// 简单的控制台日志记录器
/// </summary>
class ConsoleLogger : ILogger
{
    private readonly string _name;

    public ConsoleLogger(string name)
    {
        _name = name;
    }

    public bool IsEnabled(API.Logging.LogLevel level) => true;

    public void Trace(string message) { }
    public void Debug(string message) { }
    
    public void Info(string message)
    {
        Console.WriteLine($"  [{_name}] {message}");
    }
    
    public void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  [{_name}] ⚠ {message}");
        Console.ResetColor();
    }
    
    public void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [{_name}] ✗ {message}");
        Console.ResetColor();
    }
    
    public void Error(string message, Exception? exception = null)
    {
        if (exception != null)
            Error($"{message}: {exception.Message}");
        else
            Error(message);
    }
    
    public void Fatal(string message, Exception? exception = null)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"  [{_name}] ✗✗ FATAL: {message}");
        if (exception != null)
            Console.WriteLine($"      {exception.Message}");
        Console.ResetColor();
    }
}

/// <summary>
/// NetherGate 托管服务
/// </summary>
public class NetherGateHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly SmpService _smpService;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly RconService? _rconService;
    private readonly WebSocketServer? _wsServer;
    private readonly EventBridge? _wsEventBridge;
    private readonly LogListener? _logListener;
    private readonly PluginManager _pluginManager;
    private readonly ICommandManager _commandManager;
    private readonly SmpClient _smpClient;
    private readonly IServerCommandExecutor _serverCommandExecutor;
    private readonly NetherGateConfig _config;
    private readonly WebSocketConfig _wsConfig;
    private readonly NetherGate.Core.Monitoring.HealthService _healthService;
    private readonly NetherGate.Core.Process.ServerProcessManager? _serverProcessManager;

    public NetherGateHostedService(
        ILoggerFactory loggerFactory,
        IEventBus eventBus,
        SmpService smpService,
        IServiceProvider serviceProvider,
        PluginManager pluginManager,
        ICommandManager commandManager,
        SmpClient smpClient,
        IServerCommandExecutor serverCommandExecutor,
        NetherGateConfig config,
        WebSocketConfig wsConfig,
        NetherGate.Core.Monitoring.HealthService healthService,
        IHostApplicationLifetime appLifetime)
    {
        _logger = loggerFactory.CreateLogger("NetherGate");
        _eventBus = eventBus;
        _smpService = smpService;
        _appLifetime = appLifetime;
        _pluginManager = pluginManager;
        _commandManager = commandManager;
        _smpClient = smpClient;
        _serverCommandExecutor = serverCommandExecutor;
        _config = config;
        _wsConfig = wsConfig;
        _healthService = healthService;
        
        // 可选服务
        _rconService = serviceProvider.GetService<RconService>();
        _wsServer = serviceProvider.GetService<WebSocketServer>();
        _wsEventBridge = serviceProvider.GetService<EventBridge>();
        _logListener = serviceProvider.GetService<LogListener>();
        _serverProcessManager = serviceProvider.GetService<NetherGate.Core.Process.ServerProcessManager>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("NetherGate 正在启动...");
        _logger.Info($"版本: 0.1.0-alpha");
        _logger.Info($".NET 版本: {Environment.Version}");

        // 初始化目录
        InitializeDirectories();

        // 注册插件相关命令（提前注册以便使用）
        RegisterPluginCommands();

        // 启动日志监听器（用于监控服务器日志）
        if (_logListener != null && _config.LogListener.Enabled)
        {
            _logger.Info("启动日志监听器...");
            await _logListener.StartAsync();
        }

        // 启动 WebSocket 服务器（独立服务，不依赖 MC 服务器）
        if (_wsServer != null && _wsConfig.Enabled)
        {
            _logger.Info("启动 WebSocket 服务器...");
            await _wsServer.StartAsync();
            
            if (_wsEventBridge != null)
            {
                await _wsEventBridge.StartAsync();
            }
        }

        // 启动健康检查
        await _healthService.StartAsync();

        // 注册事件处理器
        _eventBus.Subscribe<SmpConnectedEvent>(OnSmpConnected);
        _eventBus.Subscribe<SmpDisconnectedEvent>(OnSmpDisconnected);

        // 启动服务器进程并等待就绪
        bool serverReady = false;
        
        if (_serverProcessManager != null && _config.ServerProcess.Enabled)
        {
            var launchMethod = _config.ServerProcess.LaunchMethod.ToLower();
            if (launchMethod == "java" || launchMethod == "script")
            {
                // 创建服务器就绪等待信号
                var serverReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                
                // 使用临时变量来捕获事件
                Action<ServerReadyEvent>? tempHandler = null;
                tempHandler = evt => {
                    _logger.Info($"✓ 检测到服务器启动完成信号（耗时: {evt.StartupTimeSeconds:F3} 秒）");
                    OnServerReady(evt);
                    serverReadyTcs.TrySetResult(true);
                    // 触发后立即取消订阅，避免重复触发
                    if (tempHandler != null)
                        _eventBus.Unsubscribe(tempHandler);
                };
                
                // 订阅服务器就绪事件
                _eventBus.Subscribe<ServerReadyEvent>(tempHandler);
                
                _logger.Info("启动 Minecraft 服务端...");
                await _serverProcessManager.StartAsync();
                
                _logger.Info("等待服务器启动完成（最多等待 5 分钟）...");
                
                // 等待服务器就绪（最多等待 5 分钟）
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(5));
                
                try
                {
                    await serverReadyTcs.Task.WaitAsync(cts.Token);
                    serverReady = true;
                    _logger.Info("✓ 服务器已就绪，开始连接服务");
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning("等待服务器启动超时（5 分钟），将继续启动连接服务");
                    // 清理订阅
                    if (tempHandler != null)
                        _eventBus.Unsubscribe(tempHandler);
                }
                
                // 额外等待 1 秒，确保服务器完全稳定
                if (serverReady)
                {
                    _logger.Info("等待 1 秒让服务器完全稳定...");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        else
        {
            _logger.Info("服务器进程管理未启用，假定服务器已运行");
        }

        // 服务器就绪后，启动 SMP 和 RCON 连接
        _logger.Info("启动 SMP 服务...");
        await _smpService.StartAsync();

        if (_config.ServerConnection.AutoConnect)
        {
            _logger.Info("连接到 SMP 服务器...");
            await _smpClient.ConnectAsync();
        }

        if (_rconService != null && _config.Rcon.Enabled)
        {
            _logger.Info("启动 RCON 服务...");
            await _rconService.StartAsync();
        }

        // 最后加载插件
        _logger.Info("加载插件...");
        await _pluginManager.LoadAllPluginsAsync();
        
        _logger.Info("========================================");
        _logger.Info("NetherGate 启动完成！");
        _logger.Info("========================================");
        _logger.Info("控制台输入 'help' 或游戏内输入 '#help' 查看可用命令");

        // 启动命令循环（在后台线程）
        _ = Task.Run(async () => await RunCommandLoopAsync());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Info("NetherGate 正在关闭...");

        // 停止 WebSocket
        if (_wsServer != null)
        {
            if (_wsEventBridge != null)
            {
                await _wsEventBridge.StopAsync();
            }
            await _wsServer.StopAsync();
        }

        // 停止日志监听器
        if (_logListener != null)
        {
            await _logListener.StopAsync();
        }

        // 卸载插件
        await _pluginManager.UnloadAllPluginsAsync();

        // 停止 RCON
        if (_rconService != null)
        {
            await _rconService.StopAsync();
        }

        // 断开 SMP
        await _smpClient.DisconnectAsync();
        await _smpService.StopAsync();

        // 停止服务器进程
        if (_serverProcessManager != null)
        {
            await _serverProcessManager.StopAsync();
        }

        // 停止健康检查
        await _healthService.StopAsync();

        _logger.Info("已安全退出");
    }

    private async Task ShutdownAsync()
    {
        _logger.Info("收到 stop 指令，正在请求 MC 有序关闭...");

        // 1) 优先尝试通过 STDIN 发送 stop 给 MC（原生行为）
        try
        {
            var preferStdin = _serverProcessManager != null
                && _serverProcessManager.IsRunning
                && !_config.ServerProcess.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase);

            if (preferStdin)
            {
                await _serverProcessManager!.SendCommandAsync("stop");
            }
            else
            {
                await _serverCommandExecutor.TryExecuteAsync("stop");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"发送 stop 到 MC 失败: {ex.Message}");
        }

        // 2) 等待几秒让 MC 正常落盘关闭
        await Task.Delay(3000);

        // 3) 触发宿主应用的停止
        _appLifetime.StopApplication();
    }

    private void InitializeDirectories()
    {
        var directories = new[]
        {
            _config.Plugins.Directory,
            "config",
            "lib"
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                _logger.Info($"创建目录: {dir}");
            }
        }
    }

    private void RegisterPluginCommands()
    {
        // 注册内置命令
        _commandManager.RegisterCommand(new HelpCommand(_commandManager));
        _commandManager.RegisterCommand(new VersionCommand());
        _commandManager.RegisterCommand(new StopCommand());
        
        // 注册插件命令
        var pluginCommand = new PluginCommand(_pluginManager, _logger);
        _commandManager.RegisterCommand(pluginCommand);
        _commandManager.RegisterCommand(new PluginsCommand(pluginCommand));
        
        // 注册状态命令
        _commandManager.RegisterCommand(new StatusCommand(_smpClient, _logger));
    }

    private async Task RunCommandLoopAsync()
    {
        var running = true;

        while (running)
        {
            try
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // 以 / 开头的命令视为 Minecraft 服务端命令
                var trimmed = input.Trim();
                if (trimmed.StartsWith("/"))
                {
                    var serverCommand = trimmed.Substring(1).TrimStart();
                    await TryExecuteMinecraftCommandAsync(serverCommand);
                    continue;
                }

                // 处理 stop 命令
                if (input.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    await ShutdownAsync();
                    break;
                }

                // 执行命令
                var result = await _commandManager.ExecuteCommandAsync(input);

                if (result.Success)
                {
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        _logger.Info(result.Message);
                    }
                }
                else
                {
                    _logger.Error(result.Error ?? "命令执行失败");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("命令执行时发生异常", ex);
            }
        }
    }

    private async Task TryExecuteMinecraftCommandAsync(string serverCommand)
    {
        if (string.IsNullOrWhiteSpace(serverCommand))
            return;

        try
        {
            // 前台交互：优先直接写入 STDIN，保持与原版控制台一致（RCON 仅后台使用）
            var preferStdin = _serverProcessManager != null
                && _serverProcessManager.IsRunning
                && !_config.ServerProcess.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase);

            if (preferStdin)
            {
                await _serverProcessManager!.SendCommandAsync(serverCommand);
                _logger.Info($"已发送到 MC 服务器: {serverCommand}");
                return;
            }

            // 无法使用 STDIN（例如 external 模式），回退到统一执行器（可能使用 RCON）
            var ok = await _serverCommandExecutor.TryExecuteAsync(serverCommand);
            if (ok)
                _logger.Info($"已发送到 MC 服务器: {serverCommand}");
            else
                _logger.Warning("没有可用的命令通道或发送失败");
        }
        catch (Exception ex)
        {
            _logger.Error($"发送 Minecraft 命令失败: {ex.Message}", ex);
        }
    }

    private void OnSmpConnected(SmpConnectedEvent @event)
    {
        _logger.Info($"[事件] SMP 连接已建立 ({@event.Timestamp:HH:mm:ss})");
    }

    private void OnSmpDisconnected(SmpDisconnectedEvent @event)
    {
        var reason = string.IsNullOrEmpty(@event.Reason) ? "未知原因" : @event.Reason;
        _logger.Warning($"[事件] SMP 连接已断开 ({@event.Timestamp:HH:mm:ss}) - {reason}");
    }

    private void OnServerReady(ServerReadyEvent @event)
    {
        _logger.Info($"[事件] 服务器启动完成 (耗时: {@event.StartupTimeSeconds:F3} 秒)");
    }
}

