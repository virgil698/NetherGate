using Microsoft.Extensions.DependencyInjection;
using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.API.WebSocket;
using NetherGate.Core.Commands;
using NetherGate.Core.Events;
using NetherGate.Core.Logging;
using NetherGate.Core.Plugins;
using NetherGate.Core.Protocol;
using NetherGate.Core.WebSocket;
using NetherGate.Core.Data;
using NetherGate.Core.Monitoring;
using NetherGate.Core.Performance;
using NetherGate.Core.FileSystem;
using NetherGate.Core.Permissions;
using NetherGate.API.Data;
using NetherGate.API.FileSystem;
using NetherGate.API.Monitoring;
using NetherGate.API.Permissions;
using NetherGate.API.Audio;
using NetherGate.Core.Audio;
using NetherGate.API.GameDisplay;
using NetherGate.Core.GameDisplay;
using NetherGate.API.Utilities;
using NetherGate.Core.Utilities;
using NetherGate.API.Leaderboard;
using NetherGate.Core.Leaderboard;
using NetherGate.API.Analytics;
using NetherGate.Core.Analytics;
using NetherGate.API.Plugins;

// 命名空间别名，避免冲突
using ApiLogLevel = NetherGate.API.Logging.LogLevel;
using ApiLoggerFactory = NetherGate.API.Logging.ILoggerFactory;

namespace NetherGate.Host;

/// <summary>
/// DI 容器扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 NetherGate 核心服务
    /// </summary>
    public static IServiceCollection AddNetherGate(
        this IServiceCollection services,
        NetherGateConfig config,
        WebSocketConfig wsConfig)
    {
        // 配置
        services.AddSingleton(config);
        services.AddSingleton(wsConfig);

        // 日志系统
        services.AddLogging(config.Logging);
        
        // 事件总线
        services.AddSingleton<IEventBus, EventBus>();
        
        // 命令系统
        services.AddSingleton<ICommandManager, CommandManager>();
        
        // 协议层
        services.AddProtocol(config);
        
        // 网络层
        services.AddNetwork(config, wsConfig);
        
        // 数据层
        services.AddDataServices(config);
        
        // 文件系统
        services.AddFileSystem(config);
        
        // 监控和性能
        services.AddMonitoring(config);
        
        // 权限系统
        services.AddSingleton<IPermissionManager, PermissionManager>();
        
        // 游戏工具
        services.AddGameUtilities();
        
        // 插件系统
        services.AddPluginSystem(config);
        
        return services;
    }

    /// <summary>
    /// 添加日志系统
    /// </summary>
    private static IServiceCollection AddLogging(
        this IServiceCollection services,
        LoggingConfig config)
    {
        var writers = new List<ILogWriter>();
        
        var minLevel = Enum.TryParse<ApiLogLevel>(config.Level, true, out var level)
            ? level
            : ApiLogLevel.Info;

        if (config.Console.Enabled)
        {
            writers.Add(new ConsoleLogWriter(config.Console.Colored));
        }

        if (config.File.Enabled)
        {
            writers.Add(new FileLogWriter(
                config.File.Path,
                config.File.MaxSize,
                config.File.MaxFiles
            ));
        }

        var loggerFactory = new Core.Logging.LoggerFactory(minLevel, writers);
        services.AddSingleton<ApiLoggerFactory>(loggerFactory);
        services.AddSingleton(sp => sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("NetherGate"));
        
        return services;
    }

    /// <summary>
    /// 添加协议层服务
    /// </summary>
    private static IServiceCollection AddProtocol(
        this IServiceCollection services,
        NetherGateConfig config)
    {
        // SMP 客户端
        services.AddSingleton<SmpClient>(sp => 
            new SmpClient(
                config.ServerConnection,
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("SMP"),
                sp.GetRequiredService<IEventBus>()
            ));
        services.AddSingleton<ISmpApi>(sp => sp.GetRequiredService<SmpClient>());
        
        // SMP 服务
        services.AddSingleton<SmpService>();
        
        // RCON 客户端
        if (config.Rcon.Enabled)
        {
            services.AddSingleton<RconClient>(sp =>
                new RconClient(
                    config.Rcon.Host,
                    config.Rcon.Port,
                    config.Rcon.Password,
                    config.Rcon.ConnectTimeout,
                    sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("RCON")
                ));
            services.AddSingleton<IRconClient>(sp => sp.GetRequiredService<RconClient>());
            services.AddSingleton<RconService>();
        }
        else
        {
            // 注册空实现
            services.AddSingleton<IRconClient>(sp => null!);
        }
        
        // 命令执行器
        services.AddSingleton<IServerCommandExecutor, ServerCommandExecutor>();
        
        return services;
    }

    /// <summary>
    /// 添加网络层服务
    /// </summary>
    private static IServiceCollection AddNetwork(
        this IServiceCollection services,
        NetherGateConfig config,
        WebSocketConfig wsConfig)
    {
        // WebSocket 消息处理器
        services.AddSingleton<WebSocketMessageHandler>();
        
        // WebSocket 服务器
        services.AddSingleton<WebSocketServer>();
        services.AddSingleton<IWebSocketServer>(sp => sp.GetRequiredService<WebSocketServer>());
        
        // WebSocket 事件桥接
        services.AddSingleton<EventBridge>();
        
        // 日志监听器
        if (config.LogListener.Enabled)
        {
            services.AddSingleton<LogListener>();
        }
        
        return services;
    }

    /// <summary>
    /// 添加数据服务
    /// </summary>
    private static IServiceCollection AddDataServices(
        this IServiceCollection services,
        NetherGateConfig config)
    {
        var workingDir = config.ServerProcess.Server.WorkingDirectory;
        
        services.AddSingleton<PlayerDataReader>(sp =>
            new PlayerDataReader(workingDir, sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("PlayerData")));
        services.AddSingleton<IPlayerDataReader>(sp => sp.GetRequiredService<PlayerDataReader>());
        
        services.AddSingleton<INbtDataWriter>(sp =>
            new NbtDataWriter(workingDir, sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("NbtWriter")));
        
        services.AddSingleton<ItemComponentReader>(sp =>
            new ItemComponentReader(
                workingDir,
                sp.GetRequiredService<IRconClient>(),
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("ItemReader")));
        services.AddSingleton<IItemComponentReader>(sp => sp.GetRequiredService<ItemComponentReader>());
        
        services.AddSingleton<IItemComponentWriter>(sp =>
            new ItemComponentWriter(
                sp.GetRequiredService<IRconClient>(),
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("ItemWriter"),
                sp.GetRequiredService<ItemComponentReader>()));
        
        services.AddSingleton<IItemComponentConverter>(sp =>
            new ItemComponentConverter(
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("ItemConverter"),
                sp.GetRequiredService<ItemComponentReader>(),
                sp.GetRequiredService<PlayerDataReader>()));
        
        services.AddSingleton<WorldDataReader>(sp =>
            new WorldDataReader(workingDir, sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("WorldData")));
        services.AddSingleton<IWorldDataReader>(sp => sp.GetRequiredService<WorldDataReader>());
        
        services.AddSingleton<BlockDataReader>(sp =>
            new BlockDataReader(
                workingDir,
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("BlockReader"),
                sp.GetRequiredService<WorldDataReader>()));
        services.AddSingleton<IBlockDataReader>(sp => sp.GetRequiredService<BlockDataReader>());
        
        services.AddSingleton<IBlockDataWriter>(sp =>
            new BlockDataWriter(
                sp.GetRequiredService<IRconClient>(),
                sp.GetRequiredService<BlockDataReader>(),
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("BlockWriter")));
        
        // 成就和统计追踪
        services.AddSingleton<IAdvancementTracker>(sp =>
            new AdvancementTracker(
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("Advancement"),
                workingDir,
                sp.GetRequiredService<IFileWatcher>()));
        
        services.AddSingleton<IStatisticsTracker>(sp =>
            new StatisticsTracker(
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("Statistics"),
                workingDir,
                sp.GetRequiredService<IFileWatcher>()));
        
        // 排行榜系统
        services.AddSingleton<ILeaderboardSystem>(sp =>
            new LeaderboardSystem(
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("Leaderboard"),
                Path.Combine(workingDir, "leaderboards")));
        
        return services;
    }

    /// <summary>
    /// 添加文件系统服务
    /// </summary>
    private static IServiceCollection AddFileServices(
        this IServiceCollection services,
        NetherGateConfig config)
    {
        var workingDir = config.ServerProcess.Server.WorkingDirectory;
        
        services.AddSingleton<IFileWatcher>(sp =>
            new FileWatcher(
                workingDir,
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("FileWatcher")));
        
        services.AddSingleton<IServerFileAccess>(sp =>
            new ServerFileAccess(workingDir, sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("FileAccess")));
        
        services.AddSingleton<IBackupManager>(sp =>
            new BackupManager(
                workingDir,
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("Backup"),
                Path.Combine(workingDir, "backups")));
        
        return services;
    }

    /// <summary>
    /// 添加文件系统服务（简化版本名称）
    /// </summary>
    private static IServiceCollection AddFileSystem(
        this IServiceCollection services,
        NetherGateConfig config)
    {
        return services.AddFileServices(config);
    }

    /// <summary>
    /// 添加监控服务
    /// </summary>
    private static IServiceCollection AddMonitoring(
        this IServiceCollection services,
        NetherGateConfig config)
    {
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        
        // Spark Agent
        if (config.Spark.Enabled)
        {
            services.AddSingleton<SparkAgentManager>(sp =>
                new SparkAgentManager(
                    config.Spark,
                    config.ServerProcess.Server.WorkingDirectory,
                    sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("SparkAgent")));
        }
        
        // 服务器进程管理
        if (config.ServerProcess.Enabled)
        {
            services.AddSingleton<NetherGate.Core.Process.ServerProcessManager>();
        }
        
        // 健康检查服务
        services.AddSingleton<HealthService>();
        
        return services;
    }

    /// <summary>
    /// 添加游戏工具
    /// </summary>
    private static IServiceCollection AddGameUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IGameDisplayApi, GameDisplayApi>();
        services.AddSingleton<IGameUtilities, GameUtilities>();
        services.AddSingleton<IMusicPlayer, MusicPlayer>();
        
        return services;
    }

    /// <summary>
    /// 添加插件系统
    /// </summary>
    private static IServiceCollection AddPluginSystem(
        this IServiceCollection services,
        NetherGateConfig config)
    {
        // 分布式插件总线
        services.AddSingleton<DistributedPluginBus>(sp =>
            new DistributedPluginBus(
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("DistributedBus"),
                sp.GetService<WebSocketServer>()
            ));
        
        // 连接管理器
        services.AddSingleton<NetherGate.Core.Network.ConnectionManager>(sp =>
            new NetherGate.Core.Network.ConnectionManager(
                sp.GetRequiredService<ApiLoggerFactory>().CreateLogger("ConnMgr")
            ));
        
        services.AddSingleton<PluginManager>(sp =>
        {
            var pm = new PluginManager(
                sp.GetRequiredService<ApiLoggerFactory>(),
                sp.GetRequiredService<IEventBus>(),
                sp.GetRequiredService<SmpClient>(),
                sp.GetRequiredService<ICommandManager>(),
                sp.GetService<RconClient>(),
                config.Plugins.Directory,
                "config",
                "lib",
                config.ServerProcess.Server.WorkingDirectory,
                sp.GetRequiredService<IServerCommandExecutor>()
            );
            
            // 设置服务提供者（用于插件构造函数注入）
            pm.SetServiceProvider(sp);
            
            return pm;
        });
        
        return services;
    }
}

