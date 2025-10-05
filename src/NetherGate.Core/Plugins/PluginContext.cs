using NetherGate.API.Data;
using NetherGate.API.Events;
using NetherGate.API.FileSystem;
using NetherGate.API.Logging;
using NetherGate.API.Monitoring;
using NetherGate.API.Network;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 插件上下文实现
/// </summary>
internal class PluginContext : IPluginContext, IPluginContextInternal
{
    private readonly PluginManager _pluginManager;
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ISmpApi _smpApi;
    private readonly ICommandManager _commandManager;
    private readonly IConfigManager _configManager;
    private readonly IRconClient? _rconClient;
    private readonly IFileWatcher _fileWatcher;
    private readonly IServerFileAccess _serverFileAccess;
    private readonly IBackupManager _backupManager;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IPlayerDataReader _playerDataReader;
    private readonly IWorldDataReader _worldDataReader;
    private readonly PluginMessenger _messenger;

    public PluginInfo PluginInfo { get; }
    public string DataDirectory { get; }
    public ILogger Logger => _logger;
    public IEventBus EventBus => _eventBus;
    public ISmpApi SmpApi => _smpApi;
    public ICommandManager CommandManager => _commandManager;
    public IConfigManager ConfigManager => _configManager;

    // 已实现的功能
    public IRconClient? RconClient => _rconClient;
    public IFileWatcher FileWatcher => _fileWatcher;
    public IServerFileAccess ServerFileAccess => _serverFileAccess;
    public IBackupManager BackupManager => _backupManager;
    public IPerformanceMonitor PerformanceMonitor => _performanceMonitor;
    public IPlayerDataReader PlayerDataReader => _playerDataReader;
    public IWorldDataReader WorldDataReader => _worldDataReader;
    public IPluginMessenger Messenger => _messenger;
    
    // 待实现的功能
    public IServerQuery ServerQuery => throw new NotImplementedException("服务器查询功能将在后续版本实现（基于 MC 网络协议）");
    public IServerMonitor ServerMonitor => throw new NotImplementedException("服务器监控功能将在后续版本实现（基于 MC 网络协议）");
    
    // 内部接口实现（用于 PluginMessenger 访问）
    PluginMessenger? IPluginContextInternal.Messenger => _messenger;


    public PluginContext(
        PluginContainer container,
        PluginManager pluginManager,
        ILoggerFactory loggerFactory,
        IEventBus eventBus,
        ISmpApi smpApi,
        ICommandManager commandManager,
        IRconClient? rconClient,
        IFileWatcher fileWatcher,
        IServerFileAccess serverFileAccess,
        IBackupManager backupManager,
        IPerformanceMonitor performanceMonitor,
        IPlayerDataReader playerDataReader,
        IWorldDataReader worldDataReader)
    {
        _pluginManager = pluginManager;
        _eventBus = eventBus;
        _smpApi = smpApi;
        _commandManager = commandManager;
        _rconClient = rconClient;
        _fileWatcher = fileWatcher;
        _serverFileAccess = serverFileAccess;
        _backupManager = backupManager;
        _performanceMonitor = performanceMonitor;
        _playerDataReader = playerDataReader;
        _worldDataReader = worldDataReader;

        PluginInfo = container.Metadata.ToPluginInfo();
        DataDirectory = container.DataDirectory;

        // 为插件创建专用的日志记录器
        _logger = loggerFactory.CreateLogger($"Plugin:{container.Name}");

        // 为插件创建专用的配置管理器
        _configManager = new ConfigManager(container.DataDirectory, _logger);

        // 为插件创建专用的消息传递器
        _messenger = new PluginMessenger(
            container.Metadata.Id,
            _logger,
            pluginManager.GetPlugin,
            pluginManager.GetPluginState,
            pluginManager.GetAllPlugins
        );
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        return _pluginManager.GetPlugin(pluginId);
    }

    public IReadOnlyList<IPlugin> GetAllPlugins()
    {
        return _pluginManager.GetAllPlugins();
    }
}

