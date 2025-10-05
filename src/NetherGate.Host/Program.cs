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
    private static WebSocketServer? _wsServer;

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
            Console.WriteLine("[NetherGate] [1/8] 加载配置...");
            
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
            Console.WriteLine("[NetherGate] [2/8] 初始化日志系统...");
            _loggerFactory = InitializeLogging(_config.Logging);
            _logger = _loggerFactory.CreateLogger("NetherGate");

            _logger.Info("NetherGate 正在启动...");
            _logger.Info($"版本: 0.1.0-alpha");
            _logger.Info($".NET 版本: {Environment.Version}");

            // 3. 初始化事件总线
            _logger.Info("[3/8] 初始化事件总线...");
            _eventBus = InitializeEventBus();

            // 4. 初始化目录结构
            _logger.Info("[4/8] 初始化目录结构...");
            InitializeDirectories(_config);

            // 5. 初始化命令系统
            _logger.Info("[5/8] 初始化命令系统...");
            _commandManager = InitializeCommandManager();

            // 6. 初始化 SMP 客户端
            _logger.Info("[6/8] 初始化 SMP 客户端...");
            _smpClient = InitializeSmpClient(_config);

            // 7. 初始化插件管理器（WebSocket 服务器依赖它）
            _logger.Info("[7/8] 初始化插件管理器...");
            _pluginManager = new PluginManager(
                _loggerFactory!,
                _eventBus!,
                _smpClient!,
                _commandManager!,
                null, // RconClient - 暂时传 null，未来如果需要可以初始化
                _config!.Plugins.Directory,
                "config"
            );

            // 8. 初始化 WebSocket 服务器（需要 PluginManager）
            _logger.Info("[8/8] 初始化 WebSocket 服务器...");
            _wsServer = InitializeWebSocketServer(_wsConfig, _pluginManager);

            // 显示配置摘要
            _logger.Info("配置摘要:");
            DisplayConfigSummary(_config);

            _logger.Info("========================================");
            _logger.Info("NetherGate 基础框架启动完成");
            _logger.Info("========================================");

            // 从这里开始计时（基础框架初始化完成，准备启动服务器和加载插件）
            startTime = DateTime.Now;

            // 测试 SMP 连接（如果配置启用）
            if (_config.ServerConnection.AutoConnect)
            {
                _logger.Info("");
                _logger.Info("连接到 SMP 服务器...");
                await ConnectSmpAsync();
            }

            // 启动 WebSocket 服务器（如果配置启用）
            if (_wsConfig!.Enabled)
            {
                _logger.Info("");
                _logger.Info("启动 WebSocket 服务器...");
                await StartWebSocketServerAsync();
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

        // 订阅一些测试事件
        eventBus.Subscribe<SmpConnectedEvent>(OnSmpConnected);
        eventBus.Subscribe<SmpDisconnectedEvent>(OnSmpDisconnected);

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
            _logger?.Info($"    - Java: {config.ServerProcess.Java.Path}");
            _logger?.Info($"    - JAR: {config.ServerProcess.Server.Jar}");
            _logger?.Info($"    - 内存: {config.ServerProcess.Memory.Min}MB - {config.ServerProcess.Memory.Max}MB");
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
            _logger?.Info("停止 WebSocket 服务器...");
            await _wsServer.StopAsync();
        }

        // 2. 卸载插件
        if (_pluginManager != null)
        {
            _logger?.Info("卸载插件...");
            await _pluginManager.UnloadAllPluginsAsync();
        }

        // 3. 断开 SMP 连接
        if (_smpClient != null)
        {
            _logger?.Info("断开 SMP 连接...");
            await _smpClient.DisconnectAsync();
            _smpClient.Dispose();
        }

        // 4. 清理事件总线
        if (_eventBus != null)
        {
            _eventBus.ClearAllSubscriptions();
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
