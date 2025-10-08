using NetherGate.API.Audio;
using NetherGate.API.Data;
using NetherGate.API.Events;
using NetherGate.API.FileSystem;
using NetherGate.API.GameDisplay;
using NetherGate.API.Logging;
using NetherGate.API.Localization;
using NetherGate.API.Monitoring;
using NetherGate.API.Network;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;
using NetherGate.API.Scheduling;
using NetherGate.API.Utilities;

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
    private readonly INbtDataWriter _nbtDataWriter;
    private readonly IItemComponentReader _itemComponentReader;
    private readonly IItemComponentWriter _itemComponentWriter;
    private readonly IItemComponentConverter _itemComponentConverter;
    private readonly INetworkEventListener _networkEventListener;
    private readonly PluginMessenger _messenger;
    private readonly IGameDisplayApi _gameDisplay;
    private readonly IServerCommandExecutor _commandExecutor;
    private readonly II18nService _i18n;
    private readonly IScheduler _scheduler;
    private readonly IBlockDataReader _blockDataReader;
    private readonly IBlockDataWriter? _blockDataWriter;
    private readonly IGameUtilities? _gameUtilities;
    private readonly IMusicPlayer _musicPlayer;

    public PluginInfo PluginInfo { get; }
    public string DataDirectory { get; }
    public ILogger Logger => _logger;
    public IEventBus EventBus => _eventBus;
    public ISmpApi SmpApi => _smpApi;
    public IServerCommandExecutor CommandExecutor => _commandExecutor;
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
    public INbtDataWriter NbtDataWriter => _nbtDataWriter;
    public IItemComponentReader ItemComponentReader => _itemComponentReader;
    public IItemComponentWriter ItemComponentWriter => _itemComponentWriter;
    public IItemComponentConverter ItemComponentConverter => _itemComponentConverter;
    public INetworkEventListener NetworkEventListener => _networkEventListener;
    public IPluginMessenger Messenger => _messenger;
    public IGameDisplayApi GameDisplay => _gameDisplay;
    public II18nService I18n => _i18n;
    public IScheduler Scheduler => _scheduler;
    public IBlockDataReader BlockDataReader => _blockDataReader;
    public IBlockDataWriter BlockDataWriter => _blockDataWriter ?? throw new InvalidOperationException("RCON 未启用，无法使用方块数据写入功能");
    public IGameUtilities GameUtilities => _gameUtilities ?? throw new InvalidOperationException("RCON 未启用，无法使用游戏实用工具");
    public IMusicPlayer MusicPlayer => _musicPlayer;
    
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
        IWorldDataReader worldDataReader,
        INbtDataWriter nbtDataWriter,
        IItemComponentReader itemComponentReader,
        IItemComponentWriter itemComponentWriter,
        IItemComponentConverter itemComponentConverter,
        INetworkEventListener networkEventListener,
        IGameDisplayApi gameDisplay,
        IServerCommandExecutor commandExecutor,
        IBlockDataReader blockDataReader,
        IBlockDataWriter? blockDataWriter,
        IGameUtilities? gameUtilities,
        IMusicPlayer musicPlayer)
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
        _nbtDataWriter = nbtDataWriter;
        _itemComponentReader = itemComponentReader;
        _itemComponentWriter = itemComponentWriter;
        _itemComponentConverter = itemComponentConverter;
        _networkEventListener = networkEventListener;
        _gameDisplay = gameDisplay;
        _commandExecutor = commandExecutor;
        _blockDataReader = blockDataReader;
        _blockDataWriter = blockDataWriter;
        _gameUtilities = gameUtilities;
        _musicPlayer = musicPlayer;

        PluginInfo = container.Metadata.ToPluginInfo();
        DataDirectory = container.DataDirectory;

        // 为插件创建专用的日志记录器
        _logger = loggerFactory.CreateLogger($"Plugin:{container.Name}");

        // 为插件创建专用的配置管理器
        _configManager = new ConfigManager(container.DataDirectory, _logger);

        // 创建 i18n 服务（默认使用系统 locale 或环境变量指定）
        _i18n = new NetherGate.Core.Localization.I18nService(
            Path.Combine(container.DataDirectory, "lang"),
            null,
            _logger
        );

        // 创建调度器
        _scheduler = new NetherGate.Core.Scheduling.Scheduler(_logger);

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

