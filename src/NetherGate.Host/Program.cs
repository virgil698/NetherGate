using System.Reflection;
using System.Runtime.Loader;
using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NetherGate.Core.Commands;
using NetherGate.Core.Configuration;
using NetherGate.Core.Events;
using NetherGate.Core.Logging;
using NetherGate.Core.Plugins;
using NetherGate.Core.Protocol;
using NetherGate.API.Protocol;
using NetherGate.Core.WebSocket;
using NetherGate.Host.Cli;

namespace NetherGate.Host;

class Program
{
    private static NetherGateConfig? _config;
    private static WebSocketConfig? _wsConfig;
    private static ILogger? _logger;
    private static ILoggerFactory? _loggerFactory;
    private static IEventBus? _eventBus;
    private static ICommandManager? _commandManager;
    private static PluginManager? _pluginManager;
    private static SmpClient? _smpClient;
    private static SmpService? _smpService;
    private static WebSocketServer? _wsServer;
    private static LogListener? _logListener;
    private static RconClient? _rconClient;
    private static RconService? _rconService;
    private static IServerCommandExecutor? _serverCommandExecutor;
    private static NetherGate.Core.Process.ServerProcessManager? _serverProcessManager;
    private static TaskCompletionSource<bool>? _serverReadySignal;
    private static NetherGate.Core.Monitoring.HealthService? _healthService;
    private static NetherGate.Core.WebSocket.EventBridge? _wsEventBridge;

    static async Task<int> Main(string[] args)
    {
        // 注册 lib 文件夹的程序集解析器（必须在最开始）
        RegisterLibAssemblyResolver();
        
        // 解析 CLI 参数
        var cliArgs = CliArgumentParser.Parse(args);
        
        // 处理 CLI 命令（非交互模式）
        if (!cliArgs.IsInteractive)
        {
            return await CliCommandHandler.ExecuteAsync(cliArgs);
        }
        
        DateTime startTime = DateTime.MinValue; // 稍后在基础框架启动完成后设置
        
        try
        {
            // 显示欢迎信息
            PrintBanner();

            Console.WriteLine("[NetherGate] 正在启动...");
            Console.WriteLine($"[NetherGate] 版本: 0.1.0-alpha");
            Console.WriteLine($"[NetherGate] .NET 版本: {Environment.Version}");
            Console.WriteLine();

            // 1. 加载配置
            Console.WriteLine("[NetherGate] [1/9] 加载配置...");
            
            // 检查配置文件是否存在
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
            
            // 加载 WebSocket 配置（首次运行会自动生成 websocket-config.yaml）
            _wsConfig = WebSocketConfigLoader.Load();

            // 2. 初始化日志系统
            Console.WriteLine("[NetherGate] [2/9] 初始化日志系统...");
            _loggerFactory = InitializeLogging(_config.Logging);
            _logger = _loggerFactory.CreateLogger("NetherGate");

            _logger.Info("NetherGate 正在启动...");
            _logger.Info($"版本: 0.1.0-alpha");
            _logger.Info($".NET 版本: {Environment.Version}");

            // 3. 初始化事件总线（必须在启动服务器之前！）
            _logger.Info("[3/9] 初始化事件总线...");
            _eventBus = InitializeEventBus();

            // 4. 启动 Minecraft 服务端（如果配置启用且为 java/script 模式）
            int currentStep = 4;
            if (_config.ServerProcess.Enabled)
            {
                var launchMethod = _config.ServerProcess.LaunchMethod.ToLower();
                if (launchMethod == "java" || launchMethod == "script")
                {
                    _logger.Info($"[{currentStep}/9] 启动 Minecraft 服务端...");
                    await StartServerProcessAsync(_config, isInitPhase: true);
                    currentStep++;
                }
            }

            // 5. 初始化目录结构
            _logger.Info($"[{currentStep}/9] 初始化目录结构...");
            InitializeDirectories(_config);
            currentStep++;

            // 6. 初始化命令系统
            _logger.Info($"[{currentStep}/9] 初始化命令系统...");
            _commandManager = InitializeCommandManager();
            currentStep++;

            // 7. 初始化 SMP 客户端
            _logger.Info($"[{currentStep}/9] 初始化 SMP 客户端...");
            _smpClient = InitializeSmpClient(_config);
            _smpService = new SmpService(_config.ServerConnection, _smpClient, _eventBus!, _loggerFactory!.CreateLogger("SMPService"));
            await _smpService.StartAsync();
            currentStep++;

            // 8. 初始化插件管理器（WebSocket 服务器依赖它）
            _logger.Info($"[{currentStep}/9] 初始化插件管理器...");
            _pluginManager = new PluginManager(
                _loggerFactory!,
                _eventBus!,
                _smpClient!,
                _commandManager!,
                null, // RconClient - 暂时传 null，未来如果需要可以初始化
                _config!.Plugins.Directory,
                "config",
                "lib",
                _config.ServerProcess.Server.WorkingDirectory,
                _serverCommandExecutor
            );
            currentStep++;

            // 9. 初始化 WebSocket 服务器（需要 PluginManager）
            _logger.Info($"[{currentStep}/9] 初始化 WebSocket 服务器...");
            _wsServer = InitializeWebSocketServer(_wsConfig, _pluginManager);

            // 启动日志监听器（若启用）
            if (_config.LogListener.Enabled)
            {
                var logListenerLogger = _loggerFactory!.CreateLogger("LogListener");
                _logListener = new LogListener(logListenerLogger, _eventBus!, _config.LogListener);
                await _logListener.StartAsync();
            }

            // 初始化 RCON（若启用）
            if (_config.Rcon.Enabled)
            {
                var rconLogger = _loggerFactory!.CreateLogger("RCON");
                _rconClient = new RconClient(
                    _config.Rcon.Host,
                    _config.Rcon.Port,
                    _config.Rcon.Password,
                    _config.Rcon.ConnectTimeout,
                    rconLogger
                );
                _rconService = new RconService(_config.Rcon, _rconClient, _eventBus!, rconLogger);
                await _rconService.StartAsync();
            }

            // 初始化统一命令执行器
            _serverCommandExecutor = new ServerCommandExecutor(_config, _loggerFactory!.CreateLogger("CmdExec"), _serverProcessManager, _rconClient, _eventBus);

            // 显示配置摘要
            _logger.Info("配置摘要:");
            DisplayConfigSummary(_config);

            _logger.Info("========================================");
            _logger.Info("NetherGate 基础框架启动完成");
            _logger.Info("========================================");

            // 从这里开始计时（基础框架初始化完成，准备启动服务器和加载插件）
            startTime = DateTime.Now;

            // 等待服务器启动完成（如果在初始化阶段已启动）
            if (_serverProcessManager != null && _serverReadySignal != null)
            {
                _logger.Info("");
                _logger.Info("========================================");
                _logger.Info("等待 Minecraft 服务器完全启动...");
                _logger.Info("等待检测关键词: 'Done (X.XXXs)!'");
                _logger.Info("========================================");
                
                var timeout = TimeSpan.FromSeconds(_config.ServerProcess.Monitoring.StartupTimeout);
                var waitTask = _serverReadySignal.Task;
                
                if (await Task.WhenAny(waitTask, Task.Delay(timeout)) == waitTask)
                {
                    _logger.Info("");
                    _logger.Info("========================================");
                    _logger.Info("✓ Minecraft 服务器启动完成！");
                    _logger.Info("✓ 继续初始化 NetherGate 组件...");
                    _logger.Info("========================================");
                }
                else
                {
                    _logger.Info("");
                    _logger.Warning("========================================");
                    _logger.Warning($"⚠ 服务器未在 {timeout.TotalSeconds} 秒内启动完成");
                    _logger.Warning("⚠ 将继续执行，但某些功能可能不可用");
                    _logger.Warning("========================================");
                }
            }
            
            // External 模式：在此处启动（不需要等待）
            if (_config.ServerProcess.Enabled && 
                _config.ServerProcess.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info("");
                _logger.Info("服务器进程管理（External 模式）...");
                await StartServerProcessAsync(_config, isInitPhase: false);
            }

            // 启动 WebSocket 服务器（如果配置启用）
            if (_wsConfig!.Enabled)
            {
                _logger.Info("");
                _logger.Info("启动 WebSocket 服务器...");
                await StartWebSocketServerAsync();
                // 启动 WS 事件桥接
                _wsEventBridge = new NetherGate.Core.WebSocket.EventBridge(_eventBus!, _wsServer!, _loggerFactory!.CreateLogger("WSEvent"));
                await _wsEventBridge.StartAsync();
            }

            // 启动健康检查服务（发布 SystemHealthEvent）
            _healthService = new NetherGate.Core.Monitoring.HealthService(
                _eventBus!,
                _loggerFactory!.CreateLogger("Health"),
                _serverProcessManager,
                _rconClient,
                _smpClient,
                _wsServer,
                () => _pluginManager?.GetAllPluginContainers().Count ?? 0,
                TimeSpan.FromSeconds(5)
            );
            await _healthService.StartAsync();

            // 连接 SMP 服务器（如果配置启用）
            if (_config.ServerConnection.AutoConnect)
            {
                _logger.Info("");
                _logger.Info("连接到 SMP 服务器...");
                await ConnectSmpAsync();
            }

            // 加载插件
            _logger.Info("");
            await LoadPluginsAsync();

            // 计算启动耗时（从基础框架启动完成到全部加载完毕）
            var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
            _logger.Info("");
            _logger.Info($"NetherGate 启动完毕（耗时: {elapsedSeconds:F3} 秒）");
            _logger.Info("控制台输入 'help' 或游戏内输入 '#help' 查看可用命令");

            // 启动命令循环
            await RunCommandLoopAsync();

            _logger.Info("NetherGate 正在关闭...");
            
            // 清理资源
            await ShutdownAsync();

            _logger.Info("已安全退出");
            
            // 等待用户按键后关闭（方便查看输出和错误信息）
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("按任意键关闭窗口...");
            Console.ResetColor();
            Console.ReadKey(true);
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetherGate] 启动失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            
            // 等待用户按键后关闭（方便查看错误信息）
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("按任意键关闭窗口...");
            Console.ResetColor();
            Console.ReadKey(true);
            
            return 1;
        }
    }

    /// <summary>
    /// 打印欢迎横幅
    /// </summary>
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

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    static ILoggerFactory InitializeLogging(LoggingConfig config)
    {
        var writers = new List<ILogWriter>();

        // 解析日志级别
        var minLevel = Enum.TryParse<LogLevel>(config.Level, true, out var level)
            ? level
            : LogLevel.Info;

        // 控制台日志
        if (config.Console.Enabled)
        {
            writers.Add(new ConsoleLogWriter(config.Console.Colored));
        }

        // 文件日志
        if (config.File.Enabled)
        {
            writers.Add(new FileLogWriter(
                config.File.Path,
                config.File.MaxSize,
                config.File.MaxFiles
            ));
        }

        return new LoggerFactory(minLevel, writers);
    }

    /// <summary>
    /// 初始化事件总线
    /// </summary>
    static IEventBus InitializeEventBus()
    {
        var eventBus = new EventBus(_loggerFactory!.CreateLogger("EventBus"));

        // 订阅事件
        eventBus.Subscribe<SmpConnectedEvent>(OnSmpConnected);
        eventBus.Subscribe<SmpDisconnectedEvent>(OnSmpDisconnected);
        eventBus.Subscribe<ServerReadyEvent>(OnServerReady);

        _logger?.Debug("事件总线初始化完成");
        return eventBus;
    }

    /// <summary>
    /// 初始化命令管理器
    /// </summary>
    static ICommandManager InitializeCommandManager()
    {
        var commandManager = new CommandManager(_loggerFactory!.CreateLogger("CommandManager"));

        // 注册内置命令
        RegisterBuiltinCommands(commandManager);

        _logger?.Debug("命令系统初始化完成");
        return commandManager;
    }

    /// <summary>
    /// 初始化 SMP 客户端
    /// </summary>
    static SmpClient InitializeSmpClient(NetherGateConfig config)
    {
        var smpLogger = _loggerFactory!.CreateLogger("SMP");
        var smpClient = new SmpClient(config.ServerConnection, smpLogger, _eventBus);

        _logger?.Debug("SMP 客户端初始化完成");
        return smpClient;
    }

    /// <summary>
    /// 初始化 WebSocket 服务器（需要在插件管理器初始化之后调用）
    /// </summary>
    static WebSocketServer InitializeWebSocketServer(WebSocketConfig config, PluginManager pluginManager)
    {
        var wsLogger = _loggerFactory!.CreateLogger("WebSocket");
        
        // 注意：WebSocketServer 会在构造函数中创建 WebSocketMessageHandler
        // 但我们需要先创建一个临时的服务器引用，然后在构造 MessageHandler 时传入
        // 这里采用延迟初始化的方式
        WebSocketServer? wsServer = null;
        
        var playerDataReader = new NetherGate.Core.Data.PlayerDataReader(_config!.ServerProcess.Server.WorkingDirectory, wsLogger);
        var worldDataReader = new NetherGate.Core.Data.WorldDataReader(_config!.ServerProcess.Server.WorkingDirectory, wsLogger);
        
        // 创建临时占位的 WebSocketServer（后面会替换）
        var tempServer = new WebSocketServer(
            config,
            wsLogger,
            new WebSocketMessageHandler(
                config,
                wsLogger,
                _loggerFactory!,
                _eventBus!,
                _smpClient,
                playerDataReader,
                worldDataReader,
                () => pluginManager.GetAllPluginContainers(),
                _serverCommandExecutor,
                null! // 临时传 null，创建后会设置
            )
        );
        
        // 设置 MessageHandler 的 Server 引用
        var messageHandlerField = typeof(WebSocketMessageHandler).GetField("_server", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (messageHandlerField != null)
        {
            var messageHandler = typeof(WebSocketServer).GetField("_messageHandler",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(tempServer);
            messageHandlerField.SetValue(messageHandler, tempServer);
        }

        wsServer = tempServer;

        _logger?.Debug("WebSocket 服务器初始化完成");
        
        // 如果配置启用，则启动服务器
        if (config.Enabled)
        {
            _logger?.Info($"WebSocket 服务器将在端口 {config.Port} 上启动");
        }
        else
        {
            _logger?.Info("WebSocket 服务器已禁用");
        }

        return wsServer;
    }

    /// <summary>
    /// 初始化目录结构
    /// </summary>
    static void InitializeDirectories(NetherGateConfig config)
    {
        // 创建插件目录
        if (!Directory.Exists(config.Plugins.Directory))
        {
            Directory.CreateDirectory(config.Plugins.Directory);
            _logger?.Info($"创建插件目录: {config.Plugins.Directory}");
        }

        // 创建插件配置目录
        const string configDir = "config";
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            _logger?.Info($"创建插件配置目录: {configDir}");
        }

        // 创建全局库目录
        const string libDir = "lib";
        if (!Directory.Exists(libDir))
        {
            Directory.CreateDirectory(libDir);
            _logger?.Info($"创建全局库目录: {libDir}");
        }

        // 创建日志目录
        var logDir = Path.GetDirectoryName(config.Logging.File.Path);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
            _logger?.Info($"创建日志目录: {logDir}");
        }

        // 创建服务器工作目录（如果启用进程管理）
        if (config.ServerProcess.Enabled && !Directory.Exists(config.ServerProcess.Server.WorkingDirectory))
        {
            _logger?.Warning($"服务器工作目录不存在: {config.ServerProcess.Server.WorkingDirectory}");
            _logger?.Warning("请确保 Minecraft 服务器已正确配置");
        }
    }

    /// <summary>
    /// 显示配置摘要
    /// </summary>
    static void DisplayConfigSummary(NetherGateConfig config)
    {
        _logger?.Info($"  服务器进程管理: {(config.ServerProcess.Enabled ? "[启用]" : "[禁用]")}");
        
        if (config.ServerProcess.Enabled)
        {
            _logger?.Info($"    - 启动方式: {config.ServerProcess.LaunchMethod}");
            _logger?.Info($"    - 工作目录: {config.ServerProcess.Server.WorkingDirectory}");
            
            if (config.ServerProcess.LaunchMethod.Equals("java", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.Info($"    - Java: {config.ServerProcess.Java.Path}");
                _logger?.Info($"    - JAR: {config.ServerProcess.Server.Jar}");
                _logger?.Info($"    - 内存: {config.ServerProcess.Memory.Min}MB - {config.ServerProcess.Memory.Max}MB");
            }
            else if (config.ServerProcess.LaunchMethod.Equals("script", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.Info($"    - 脚本路径: {config.ServerProcess.Script.Path}");
                _logger?.Info($"    - 脚本参数: {string.Join(" ", config.ServerProcess.Script.Arguments)}");
            }
            else if (config.ServerProcess.LaunchMethod.Equals("external", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.Info($"    - 外部管理模式（服务器由其他方式启动）");
            }
        }

        _logger?.Info($"  SMP 连接: {config.ServerConnection.Host}:{config.ServerConnection.Port}");
        _logger?.Info($"    - TLS: {(config.ServerConnection.UseTls ? "启用" : "禁用")}");
        _logger?.Info($"    - 自动连接: {(config.ServerConnection.AutoConnect ? "是" : "否")}");

        _logger?.Info($"  RCON 客户端: {(config.Rcon.Enabled ? "[启用]" : "[禁用]")}");
        if (config.Rcon.Enabled)
        {
            _logger?.Info($"    - 端口: {config.Rcon.Port}");
            _logger?.Info($"    - 自动连接: {(config.Rcon.AutoConnect ? "是" : "否")}");
        }

        _logger?.Info($"  日志监听器: {(config.LogListener.Enabled ? "[启用]" : "[禁用]")}");
        _logger?.Info($"  插件目录: {config.Plugins.Directory}");
        _logger?.Info($"  日志级别: {config.Logging.Level}");
        
        if (_wsConfig != null)
        {
            _logger?.Info($"  WebSocket 服务器: {(_wsConfig.Enabled ? "[启用]" : "[禁用]")}");
            if (_wsConfig.Enabled)
            {
                _logger?.Info($"    - 端口: {_wsConfig.Port}");
                _logger?.Info($"    - 认证: {(_wsConfig.Authentication.Enabled ? "启用" : "禁用")}");
            }
        }
    }

    /// <summary>
    /// 注册内置命令
    /// </summary>
    static void RegisterBuiltinCommands(ICommandManager commandManager)
    {
        // Help 命令
        commandManager.RegisterCommand(new HelpCommand(commandManager));
        
        // Version 命令
        commandManager.RegisterCommand(new VersionCommand());
        
        // Stop 命令
        commandManager.RegisterCommand(new StopCommand());
        
        _logger?.Debug("内置命令注册完成");
    }

    /// <summary>
    /// 启动服务器进程
    /// </summary>
    /// <param name="config">配置</param>
    /// <param name="isInitPhase">是否在初始化阶段（true=初始化阶段，false=后续启动）</param>
    static async Task StartServerProcessAsync(NetherGateConfig config, bool isInitPhase = false)
    {
        try
        {
            // 初始化 spark agent（如果启用）
            NetherGate.Core.Performance.SparkAgentManager? sparkAgent = null;
            if (config.Spark.Enabled)
            {
                sparkAgent = new NetherGate.Core.Performance.SparkAgentManager(
                    config.Spark,
                    config.ServerProcess.Server.WorkingDirectory,
                    _loggerFactory!.CreateLogger("SparkAgent")
                );
            }

            // 创建服务器进程管理器
            _serverProcessManager = new NetherGate.Core.Process.ServerProcessManager(
                config.ServerProcess,
                _loggerFactory!.CreateLogger("ServerProcess"),
                _eventBus!,
                sparkAgent
            );

            // 启动服务器
            var started = await _serverProcessManager.StartAsync();
            
            if (started)
            {
                var launchMethod = config.ServerProcess.LaunchMethod.ToLower();
                
                // java 和 script 模式需要设置等待信号
                if (isInitPhase && (launchMethod == "java" || launchMethod == "script"))
                {
                    _serverReadySignal = new TaskCompletionSource<bool>();
                    _logger?.Info("服务器进程已启动，将在后续步骤等待启动完成");
                }
                else
                {
                    _logger?.Info("服务器进程管理已初始化");
                }
            }
            else
            {
                _logger?.Error("启动 Minecraft 服务器进程失败");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"启动服务器进程时发生错误: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 连接到 SMP 服务器
    /// </summary>
    static async Task ConnectSmpAsync()
    {
        if (_smpClient == null)
        {
            _logger?.Error("SMP 客户端未初始化");
            return;
        }

        try
        {
            _logger?.Info($"尝试连接到 {_config!.ServerConnection.Host}:{_config.ServerConnection.Port}");
            _logger?.Info("注意: 需要 Minecraft 服务器运行并启用 SMP");
            
            var connected = await _smpClient.ConnectAsync();

            if (connected)
            {
                _logger?.Info("SMP 连接成功");
            }
            else
            {
                _logger?.Warning("SMP 连接失败（这是正常的，如果服务器未运行）");
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"SMP 连接失败: {ex.Message}");
            _logger?.Info("这是正常的，因为 Minecraft 服务器可能未运行");
        }
    }

    /// <summary>
    /// 加载插件
    /// </summary>
    static async Task LoadPluginsAsync()
    {
        try
        {
            // 插件管理器已在初始化阶段创建，这里直接加载插件
            if (_pluginManager == null)
            {
                _logger?.Error("插件管理器未初始化");
                return;
            }

            // 加载所有插件
            await _pluginManager.LoadAllPluginsAsync();

            // 注册需要 PluginManager 的内置命令
            var pluginCommandLogger = _loggerFactory!.CreateLogger("PluginCommand");
            var pluginCommand = new PluginCommand(_pluginManager, pluginCommandLogger);
            _commandManager!.RegisterCommand(pluginCommand);
            _commandManager.RegisterCommand(new PluginsCommand(pluginCommand));
            
            // 注册需要 SMP API 的内置命令
            var statusLogger = _loggerFactory!.CreateLogger("StatusCommand");
            _commandManager.RegisterCommand(new StatusCommand(_smpClient!, statusLogger));
        }
        catch (Exception ex)
        {
            _logger?.Error("加载插件时发生错误", ex);
        }
    }

    /// <summary>
    /// 运行命令循环
    /// </summary>
    static async Task RunCommandLoopAsync()
    {
        var running = true;

        // 处理 Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            running = false;
        };

        while (running)
        {
            try
            {
                // 读取用户输入
                Console.Write("> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // 以 / 开头的命令视为 Minecraft 服务端命令（去掉前导 / 后发送）
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
                    break;
                }

                // 执行命令
                var result = await _commandManager!.ExecuteCommandAsync(input);

                // 显示结果
                if (result.Success)
                {
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        _logger?.Info(result.Message);
                    }
                }
                else
                {
                    _logger?.Error(result.Error ?? "命令执行失败");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("命令执行时发生异常", ex);
            }
        }
    }

    /// <summary>
    /// 发送 Minecraft 服务端命令
    /// </summary>
    static async Task TryExecuteMinecraftCommandAsync(string serverCommand)
    {
        if (string.IsNullOrWhiteSpace(serverCommand))
            return;

        try
        {
            if (_serverCommandExecutor == null)
            {
                _logger?.Warning("命令执行器未初始化");
                return;
            }

            var ok = await _serverCommandExecutor.TryExecuteAsync(serverCommand);
            if (ok)
                _logger?.Info($"已发送到 MC 服务器: {serverCommand}");
            else
                _logger?.Warning("没有可用的命令通道或发送失败");
        }
        catch (Exception ex)
        {
            _logger?.Error($"发送 Minecraft 命令失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 启动 WebSocket 服务器
    /// </summary>
    static async Task StartWebSocketServerAsync()
    {
        if (_wsServer == null)
        {
            _logger?.Error("WebSocket 服务器未初始化");
            return;
        }

        try
        {
            await _wsServer.StartAsync();
            _logger?.Info($"WebSocket 服务器已启动: {_wsConfig!.Host}:{_wsConfig.Port}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"启动 WebSocket 服务器失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭并清理资源
    /// </summary>
    static async Task ShutdownAsync()
    {
        // 1. 停止 WebSocket 服务器
        if (_wsServer != null)
        {
            if (_wsEventBridge != null)
            {
                await _wsEventBridge.StopAsync();
                _wsEventBridge.Dispose();
            }
            _logger?.Info("停止 WebSocket 服务器...");
            await _wsServer.StopAsync();
        }

        // 1.5 停止日志监听器
        if (_logListener != null)
        {
            _logger?.Info("停止日志监听器...");
            await _logListener.StopAsync();
            _logListener.Dispose();
        }

        // 2. 卸载插件
        if (_pluginManager != null)
        {
            _logger?.Info("卸载插件...");
            await _pluginManager.UnloadAllPluginsAsync();
        }

        // 2.5 停止 RCON 服务
        if (_rconService != null)
        {
            _logger?.Info("停止 RCON 服务...");
            await _rconService.StopAsync();
            _rconService.Dispose();
        }

        // 3. 断开 SMP 连接
        if (_smpClient != null)
        {
            _logger?.Info("断开 SMP 连接...");
            await _smpClient.DisconnectAsync();
            _smpClient.Dispose();
        }
        if (_smpService != null)
        {
            await _smpService.StopAsync();
            _smpService.Dispose();
        }

        // 4. 停止服务器进程
        if (_serverProcessManager != null)
        {
            _logger?.Info("停止 Minecraft 服务器...");
            await _serverProcessManager.StopAsync();
            _serverProcessManager.Dispose();
        }

        // 5. 清理事件总线
        if (_eventBus != null)
        {
            _eventBus.ClearAllSubscriptions();
        }

        // 6. 停止健康检查
        if (_healthService != null)
        {
            await _healthService.StopAsync();
            _healthService.Dispose();
        }
    }

    // ========== 事件处理 ==========

    /// <summary>
    /// SMP 连接成功事件
    /// </summary>
    static void OnSmpConnected(SmpConnectedEvent @event)
    {
        _logger?.Info($"[事件] SMP 连接已建立 ({@event.Timestamp:HH:mm:ss})");
    }

    /// <summary>
    /// SMP 连接断开事件
    /// </summary>
    static void OnSmpDisconnected(SmpDisconnectedEvent @event)
    {
        var reason = string.IsNullOrEmpty(@event.Reason) ? "未知原因" : @event.Reason;
        _logger?.Warning($"[事件] SMP 连接已断开 ({@event.Timestamp:HH:mm:ss}) - {reason}");
    }

    /// <summary>
    /// 服务器启动完成事件
    /// </summary>
    static void OnServerReady(ServerReadyEvent @event)
    {
        _logger?.Info($"[事件] 服务器启动完成 (耗时: {@event.StartupTimeSeconds:F3} 秒)");
        
        // 通知等待的任务
        _serverReadySignal?.TrySetResult(true);
    }

    /// <summary>
    /// 注册 lib 文件夹的程序集解析器
    /// </summary>
    static void RegisterLibAssemblyResolver()
    {
        // 获取程序所在目录
        var baseDirectory = AppContext.BaseDirectory;
        var libDirectory = Path.Combine(baseDirectory, "lib");

        // 如果 lib 目录不存在，直接返回
        if (!Directory.Exists(libDirectory))
        {
            return;
        }

        // 注册程序集解析事件
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            // 尝试从 lib 文件夹加载程序集
            var assemblyPath = Path.Combine(libDirectory, $"{assemblyName.Name}.dll");

            if (File.Exists(assemblyPath))
            {
                try
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }
                catch
                {
                    // 加载失败，返回 null 让默认加载器处理
                    return null;
                }
            }

            return null;
        };
    }
    
    /// <summary>
    /// 询问用户是或否
    /// </summary>
    static bool AskYesNo(string prompt, bool defaultYes = true)
    {
        var defaultText = defaultYes ? "Y/n" : "y/N";
        Console.Write($"{prompt} [{defaultText}] ");
        
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (string.IsNullOrEmpty(input))
            return defaultYes;
        
        return input == "y" || input == "yes";
    }
}
