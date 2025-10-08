using NetherGate.API.Audio;
using NetherGate.API.Configuration;
using NetherGate.API.Data;
using NetherGate.API.Events;
using NetherGate.API.FileSystem;
using NetherGate.API.GameDisplay;
using NetherGate.API.Logging;
using NetherGate.API.Monitoring;
using NetherGate.API.Network;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;
using NetherGate.API.Utilities;
using NetherGate.Core.Audio;
using NetherGate.Core.Data;
using NetherGate.Core.FileSystem;
using NetherGate.Core.GameDisplay;
using NetherGate.Core.Monitoring;
using NetherGate.Core.Protocol;
using NetherGate.Core.Utilities;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 插件管理器
/// 负责插件的生命周期管理、依赖解析、加载顺序等
/// </summary>
public class PluginManager
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEventBus _eventBus;
    private readonly ISmpApi _smpApi;
    private readonly ICommandManager _commandManager;
    private readonly PluginLoader _pluginLoader;
    private readonly Dictionary<string, PluginContainer> _plugins = new();
    private readonly object _lock = new();
    private readonly DependencyManagementConfig _depConfig = new();
    private readonly IRconClient? _rconClient;
    
    // 本地功能服务（通过懒加载初始化）
    private IFileWatcher? _fileWatcher;
    private IServerFileAccess? _serverFileAccess;
    private IBackupManager? _backupManager;
    private IPerformanceMonitor? _performanceMonitor;
    private IPlayerDataReader? _playerDataReader;
    private IWorldDataReader? _worldDataReader;
    private INbtDataWriter? _nbtDataWriter;
    private IItemComponentReader? _itemComponentReader;
    private IItemComponentWriter? _itemComponentWriter;
    private IItemComponentConverter? _itemComponentConverter;
    private INetworkEventListener? _networkEventListener;
    private IGameDisplayApi? _gameDisplayApi;
    private readonly string _serverDirectory;
    private readonly IServerCommandExecutor? _commandExecutor;
    
    // 新增的实用工具服务
    private IBlockDataReader? _blockDataReader;
    private IBlockDataWriter? _blockDataWriter;
    private IGameUtilities? _gameUtilities;
    private IMusicPlayer? _musicPlayer;

    public PluginManager(
        ILoggerFactory loggerFactory,
        IEventBus eventBus,
        ISmpApi smpApi,
        ICommandManager commandManager,
        IRconClient? rconClient,
        string pluginsDirectory,
        string configDirectory,
        string globalLibPath = "lib",
        string serverDirectory = ".",
        IServerCommandExecutor? commandExecutor = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger("PluginManager");
        _eventBus = eventBus;
        _smpApi = smpApi;
        _commandManager = commandManager;
        _rconClient = rconClient;
        _pluginLoader = new PluginLoader(_logger, pluginsDirectory, configDirectory, globalLibPath);
        _serverDirectory = serverDirectory;
        _commandExecutor = commandExecutor;
    }
    
    /// <summary>
    /// 获取或创建文件监听服务
    /// </summary>
    private IFileWatcher GetFileWatcher()
    {
        return _fileWatcher ??= new FileWatcher(_serverDirectory, _logger);
    }
    
    /// <summary>
    /// 获取或创建服务器文件访问服务
    /// </summary>
    private IServerFileAccess GetServerFileAccess()
    {
        return _serverFileAccess ??= new ServerFileAccess(_serverDirectory, _logger);
    }
    
    /// <summary>
    /// 获取或创建备份管理器
    /// </summary>
    private IBackupManager GetBackupManager()
    {
        return _backupManager ??= new BackupManager(_serverDirectory, _logger);
    }
    
    /// <summary>
    /// 获取或创建性能监控器
    /// </summary>
    private IPerformanceMonitor GetPerformanceMonitor()
    {
        return _performanceMonitor ??= new PerformanceMonitor(_logger);
    }
    
    /// <summary>
    /// 获取或创建玩家数据读取器
    /// </summary>
    private IPlayerDataReader GetPlayerDataReader()
    {
        return _playerDataReader ??= new PlayerDataReader(_serverDirectory, _logger);
    }
    
    /// <summary>
    /// 获取或创建世界数据读取器
    /// </summary>
    private IWorldDataReader GetWorldDataReader()
    {
        return _worldDataReader ??= new WorldDataReader(_serverDirectory, _logger);
    }
    
    /// <summary>
    /// 获取或创建 NBT 数据写入器
    /// </summary>
    private INbtDataWriter GetNbtDataWriter()
    {
        return _nbtDataWriter ??= new NbtDataWriter(_serverDirectory, _logger);
    }
    
    /// <summary>
    /// 获取或创建网络事件监听器
    /// </summary>
    private INetworkEventListener GetNetworkEventListener()
    {
        return _networkEventListener ??= new Network.NetworkEventListener(_logger, _eventBus);
    }
    
    /// <summary>
    /// 获取或创建游戏显示 API
    /// </summary>
    private IGameDisplayApi GetGameDisplayApi()
    {
        if (_gameDisplayApi == null && _rconClient != null)
        {
            _gameDisplayApi = new GameDisplayApi(_rconClient, _logger);
        }
        
        // 如果 RCON 未启用，返回一个占位实现
        return _gameDisplayApi ?? new GameDisplayApi(null!, _logger);
    }
    
    /// <summary>
    /// 获取或创建方块数据读取器
    /// </summary>
    private IBlockDataReader GetBlockDataReader()
    {
        return _blockDataReader ??= new NetherGate.Core.Data.BlockDataReader(
            _serverDirectory,
            _logger,
            (WorldDataReader)GetWorldDataReader()
        );
    }
    
    /// <summary>
    /// 获取或创建方块数据写入器
    /// </summary>
    private IBlockDataWriter GetBlockDataWriter()
    {
        if (_blockDataWriter == null && _rconClient != null)
        {
            _blockDataWriter = new NetherGate.Core.Data.BlockDataWriter(
                _rconClient,
                GetBlockDataReader(),
                _logger
            );
        }
        
        return _blockDataWriter ?? throw new InvalidOperationException("RCON 未启用，无法使用方块数据写入功能");
    }
    
    /// <summary>
    /// 获取或创建游戏实用工具
    /// </summary>
    private IGameUtilities? GetGameUtilities()
    {
        if (_gameUtilities == null && _rconClient != null)
        {
            _gameUtilities = new GameUtilities(
                _rconClient!,
                GetGameDisplayApi(),
                _logger
            );
        }
        
        return _gameUtilities;
    }
    
    /// <summary>
    /// 获取或创建音乐播放器
    /// </summary>
    private IMusicPlayer GetMusicPlayer()
    {
        if (_musicPlayer == null)
        {
            _musicPlayer = new MusicPlayer(
                GetGameDisplayApi(),
                _logger
            );
        }
        return _musicPlayer;
    }
    
    /// <summary>
    /// 获取或创建物品组件读取器
    /// </summary>
    private IItemComponentReader GetItemComponentReader()
    {
        return _itemComponentReader ??= new ItemComponentReader(_serverDirectory, _rconClient, _logger);
    }
    
    /// <summary>
    /// 获取或创建物品组件写入器
    /// </summary>
    private IItemComponentWriter GetItemComponentWriter()
    {
        return _itemComponentWriter ??= new ItemComponentWriter(_rconClient!, _logger, (ItemComponentReader)GetItemComponentReader());
    }
    
    /// <summary>
    /// 获取或创建物品组件转换器
    /// </summary>
    private IItemComponentConverter GetItemComponentConverter()
    {
        return _itemComponentConverter ??= new ItemComponentConverter(_logger, (ItemComponentReader)GetItemComponentReader(), (PlayerDataReader)GetPlayerDataReader());
    }

    /// <summary>
    /// 加载所有插件
    /// </summary>
    public async Task LoadAllPluginsAsync()
    {
        _logger.Info("========================================");
        _logger.Info("开始加载插件");
        _logger.Info("========================================");

        // 1. 扫描插件
        var containers = _pluginLoader.ScanPlugins();
        if (containers.Count == 0)
        {
            _logger.Warning("未发现任何插件");
            return;
        }

        // 2. 按加载顺序和依赖关系排序
        var sortedContainers = SortByDependencies(containers);

        // 3. 依赖管理：检查冲突和自动下载
        if (_depConfig.Enabled)
        {
            _logger.Info("");
            _logger.Info("检查插件依赖...");
            
            var dependenciesOk = await ManageDependenciesAsync(sortedContainers);
            if (!dependenciesOk)
            {
                _logger.Error("依赖管理失败，部分插件可能无法加载");
            }
        }

        // 4. 加载插件程序集
        _logger.Info("");
        _logger.Info("加载插件程序集...");
        foreach (var container in sortedContainers)
        {
            if (_pluginLoader.LoadPlugin(container))
            {
                lock (_lock)
                {
                    _plugins[container.Id] = container;
                }
            }
        }

        // 5. 调用插件的 OnLoad 生命周期
        _logger.Info("");
        _logger.Info("初始化插件...");
        foreach (var container in sortedContainers)
        {
            if (container.IsLoaded && !container.HasError)
            {
                await InitializePluginAsync(container);
            }
        }

        // 6. 启用插件
        _logger.Info("");
        _logger.Info("启用插件...");
        foreach (var container in sortedContainers)
        {
            if (container.IsLoaded && !container.HasError)
            {
                await EnablePluginAsync(container);
            }
        }

        _logger.Info("");
        _logger.Info("========================================");
        _logger.Info($"插件加载完成，共 {_plugins.Count} 个插件");
        _logger.Info("========================================");
    }

    /// <summary>
    /// 初始化插件（调用 OnLoad）
    /// </summary>
    private async Task InitializePluginAsync(PluginContainer container)
    {
        try
        {
            if (container.Instance == null)
            {
                container.SetError("插件实例为 null");
                return;
            }

            _logger.Info($"初始化: {container.Name}");
            await container.Instance.OnLoadAsync();
            _logger.Debug($"  {container.Name} 初始化成功");
        }
        catch (Exception ex)
        {
            container.SetError($"初始化失败: {ex.Message}", ex);
            _logger.Error($"  {container.Name} 初始化失败", ex);
        }
    }

    /// <summary>
    /// 启用插件（调用 OnEnable）
    /// </summary>
    private async Task EnablePluginAsync(PluginContainer container)
    {
        try
        {
            if (container.Instance == null)
            {
                container.SetError("插件实例为 null");
                return;
            }

            _logger.Info($"启用: {container.Name}");

            // 创建插件上下文
            var context = new PluginContext(
                container,
                this,
                _loggerFactory,
                _eventBus,
                _smpApi,
                _commandManager,
                _rconClient,
                GetFileWatcher(),
                GetServerFileAccess(),
                GetBackupManager(),
                GetPerformanceMonitor(),
                GetPlayerDataReader(),
                GetWorldDataReader(),
                GetNbtDataWriter(),
                GetItemComponentReader(),
                GetItemComponentWriter(),
                GetItemComponentConverter(),
                GetNetworkEventListener(),
                GetGameDisplayApi(),
                _commandExecutor ?? new ServerCommandExecutor(new NetherGate.API.Configuration.NetherGateConfig(), _loggerFactory.CreateLogger("CmdExec"), null, _rconClient, _eventBus),
                GetBlockDataReader(),
                _rconClient != null ? GetBlockDataWriter() : null!,
                _rconClient != null ? GetGameUtilities() : null!,
                GetMusicPlayer()
            );

            // 通过反射设置 Context 属性（如果存在）
            SetPluginContext(container.Instance, context);

            // 调用 OnEnable
            await container.Instance.OnEnableAsync();

            // 更新状态
            container.State = PluginState.Enabled;
            container.EnabledAt = DateTime.UtcNow;

            _logger.Info($"  {container.Name} 已启用");
        }
        catch (Exception ex)
        {
            container.SetError($"启用失败: {ex.Message}", ex);
            _logger.Error($"  {container.Name} 启用失败", ex);
        }
    }

    /// <summary>
    /// 设置插件上下文（通过反射）
    /// </summary>
    private void SetPluginContext(IPlugin plugin, IPluginContext context)
    {
        var type = plugin.GetType();
        var contextProperty = type.GetProperty("Context");

        if (contextProperty != null && contextProperty.CanWrite)
        {
            contextProperty.SetValue(plugin, context);
            _logger.Trace($"  注入插件上下文: {type.Name}.Context");
        }
    }

    /// <summary>
    /// 禁用所有插件
    /// </summary>
    public async Task DisableAllPluginsAsync()
    {
        _logger.Info("========================================");
        _logger.Info("禁用所有插件");
        _logger.Info("========================================");

        var enabledPlugins = GetEnabledPlugins();
        enabledPlugins.Reverse();

        foreach (var container in enabledPlugins)
        {
            await DisablePluginAsync(container);
        }

        _logger.Info("所有插件已禁用");
    }

    /// <summary>
    /// 禁用插件
    /// </summary>
    private async Task DisablePluginAsync(PluginContainer container)
    {
        if (container.Instance == null || container.State != PluginState.Enabled)
            return;

        try
        {
            _logger.Info($"禁用: {container.Name}");

            // 调用 OnDisable
            await container.Instance.OnDisableAsync();

            // 注销插件的所有命令
            _commandManager.UnregisterPluginCommands(container.Id);

            // 取消所有消息订阅（如果支持）
            try
            {
                var context = GetPluginContext(container);
                if (context?.Messenger != null)
                {
                    context.Messenger.UnsubscribeAll();
                }
            }
            catch { /* 忽略错误 */ }

            // 更新状态
            container.State = PluginState.Disabled;
            container.EnabledAt = null;

            _logger.Info($"  {container.Name} 已禁用");
        }
        catch (Exception ex)
        {
            _logger.Error($"  {container.Name} 禁用失败", ex);
        }
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public async Task UnloadAllPluginsAsync()
    {
        _logger.Info("========================================");
        _logger.Info("卸载所有插件");
        _logger.Info("========================================");

        // 1. 先禁用所有插件
        await DisableAllPluginsAsync();

        // 2. 调用 OnUnload
        var loadedPlugins = GetLoadedPlugins();
        loadedPlugins.Reverse();
        foreach (var container in loadedPlugins)
        {
            await UnloadPluginAsync(container);
        }

        // 3. 清空插件列表
        lock (_lock)
        {
            _plugins.Clear();
        }

        _logger.Info("所有插件已卸载");
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    private async Task UnloadPluginAsync(PluginContainer container)
    {
        try
        {
            if (container.Instance == null)
                return;

            _logger.Info($"卸载: {container.Name}");

            // 调用 OnUnload
            await container.Instance.OnUnloadAsync();

            // 卸载插件程序集
            _pluginLoader.UnloadPlugin(container);

            _logger.Info($"  {container.Name} 已卸载");
        }
        catch (Exception ex)
        {
            _logger.Error($"  {container.Name} 卸载失败", ex);
        }
    }

    /// <summary>
    /// 获取插件
    /// </summary>
    public IPlugin? GetPlugin(string pluginId)
    {
        lock (_lock)
        {
            if (_plugins.TryGetValue(pluginId, out var container))
            {
                return container.Instance;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取插件状态
    /// </summary>
    public PluginState GetPluginState(string pluginId)
    {
        lock (_lock)
        {
            if (_plugins.TryGetValue(pluginId, out var container))
            {
                return container.State;
            }
        }

        return PluginState.Unloaded;
    }

    /// <summary>
    /// 重载插件（完整热重载实现）
    /// </summary>
    public async Task<bool> ReloadPluginAsync(string pluginId)
    {
        PluginContainer? container;
        
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out container))
            {
                _logger.Warning($"插件不存在: {pluginId}");
                return false;
            }
        }

        _logger.Info($"正在重载插件: {container.Name}");

        try
        {
            // 1. 保存插件状态（如果插件支持）
            var state = await SavePluginStateAsync(container);

            // 2. 禁用插件
            if (container.State == PluginState.Enabled)
            {
                await DisablePluginAsync(container);
            }

            // 3. 卸载插件程序集
            await UnloadPluginAsync(container);

            // 4. 使用 PluginLoader 重新加载插件（会重新扫描元数据）
            if (!_pluginLoader.ReloadPlugin(container))
            {
                _logger.Error($"重新加载插件失败: {container.Name}");
                return false;
            }

            // 5. 调用 OnLoad 生命周期
            var context = new PluginContext(
                container,
                this,
                _loggerFactory,
                _eventBus,
                _smpApi,
                _commandManager,
                _rconClient,
                GetFileWatcher(),
                GetServerFileAccess(),
                GetBackupManager(),
                GetPerformanceMonitor(),
                GetPlayerDataReader(),
                GetWorldDataReader(),
                GetNbtDataWriter(),
                GetItemComponentReader(),
                GetItemComponentWriter(),
                GetItemComponentConverter(),
                GetNetworkEventListener(),
                GetGameDisplayApi(),
                _commandExecutor ?? new ServerCommandExecutor(new NetherGate.API.Configuration.NetherGateConfig(), _loggerFactory.CreateLogger("CmdExec"), null, _rconClient, _eventBus),
                GetBlockDataReader(),
                _rconClient != null ? GetBlockDataWriter() : null!,
                _rconClient != null ? GetGameUtilities() : null!,
                GetMusicPlayer()
            );

            try
            {
                if (container.Instance == null)
                {
                    _logger.Error("插件实例为空");
                    return false;
                }
                
                await container.Instance.OnLoadAsync();
                _logger.Info($"  OnLoad 完成: {container.Name}");
            }
            catch (Exception ex)
            {
                container.SetError($"OnLoad 失败: {ex.Message}", ex);
                _logger.Error($"  OnLoad 失败: {container.Name}", ex);
                return false;
            }

            // 6. 调用 OnEnable 生命周期（如果之前是启用状态）
            if (state != null)
            {
                await EnablePluginAsync(container);
                
                // 恢复插件状态（如果插件支持）
                await RestorePluginStateAsync(container, state);
            }

            _logger.Info($"插件重载成功: {container.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"插件重载失败: {container.Name}", ex);
            container.SetError($"重载失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 启用插件（公共 API）
    /// </summary>
    public async Task<bool> EnablePluginAsync(string pluginId)
    {
        PluginContainer? container;
        
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out container))
            {
                _logger.Warning($"插件不存在: {pluginId}");
                return false;
            }
        }

        if (container.State == PluginState.Enabled)
        {
            _logger.Warning($"插件已启用: {container.Name}");
            return true;
        }

        try
        {
            await EnablePluginAsync(container);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"启用插件失败: {container.Name}", ex);
            return false;
        }
    }

    /// <summary>
    /// 禁用插件（公共 API）
    /// </summary>
    public async Task<bool> DisablePluginAsync(string pluginId)
    {
        PluginContainer? container;
        
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out container))
            {
                _logger.Warning($"插件不存在: {pluginId}");
                return false;
            }
        }

        if (container.State != PluginState.Enabled)
        {
            _logger.Warning($"插件未启用: {container.Name}");
            return true;
        }

        try
        {
            await DisablePluginAsync(container);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"禁用插件失败: {container.Name}", ex);
            return false;
        }
    }

    /// <summary>
    /// 保存插件状态
    /// </summary>
    private async Task<object?> SavePluginStateAsync(PluginContainer container)
    {
        try
        {
            // 尝试调用插件的 SaveState 方法（如果存在）
            var saveStateMethod = container.Instance?.GetType().GetMethod("SaveStateAsync");
            if (saveStateMethod != null)
            {
                var task = saveStateMethod.Invoke(container.Instance, null) as Task<object>;
                if (task != null)
                {
                    return await task;
                }
            }
        }
        catch
        {
            _logger.Warning($"保存插件状态失败: {container.Name}");
        }

        return null;
    }

    /// <summary>
    /// 恢复插件状态
    /// </summary>
    private async Task RestorePluginStateAsync(PluginContainer container, object state)
    {
        try
        {
            // 尝试调用插件的 RestoreState 方法（如果存在）
            var restoreStateMethod = container.Instance?.GetType().GetMethod("RestoreStateAsync");
            if (restoreStateMethod != null)
            {
                var task = restoreStateMethod.Invoke(container.Instance, new[] { state }) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        }
        catch
        {
            _logger.Warning($"恢复插件状态失败: {container.Name}");
        }
    }

    /// <summary>
    /// 获取插件上下文
    /// </summary>
    private IPluginContextInternal? GetPluginContext(PluginContainer container)
    {
        var contextProperty = container.Instance?.GetType().GetProperty("Context");
        return contextProperty?.GetValue(container.Instance) as IPluginContextInternal;
    }

    /// <summary>
    /// 获取所有插件
    /// </summary>
    public IReadOnlyList<IPlugin> GetAllPlugins()
    {
        lock (_lock)
        {
            return _plugins.Values
                .Where(c => c.Instance != null)
                .Select(c => c.Instance!)
                .ToList();
        }
    }

    /// <summary>
    /// 获取所有插件容器（用于显示详细信息）
    /// </summary>
    public IReadOnlyList<PluginContainer> GetAllPluginContainers()
    {
        lock (_lock)
        {
            return _plugins.Values.ToList();
        }
    }

    /// <summary>
    /// 获取所有已启用的插件
    /// </summary>
    private List<PluginContainer> GetEnabledPlugins()
    {
        lock (_lock)
        {
            return _plugins.Values
                .Where(c => c.IsEnabled)
                .ToList();
        }
    }

    /// <summary>
    /// 获取所有已加载的插件
    /// </summary>
    private List<PluginContainer> GetLoadedPlugins()
    {
        lock (_lock)
        {
            return _plugins.Values
                .Where(c => c.IsLoaded)
                .ToList();
        }
    }

    /// <summary>
    /// 按依赖关系和加载顺序排序
    /// </summary>
    private List<PluginContainer> SortByDependencies(List<PluginContainer> containers)
    {
        var sorted = new List<PluginContainer>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(PluginContainer container)
        {
            if (visited.Contains(container.Id))
                return;

            if (visiting.Contains(container.Id))
            {
                _logger.Warning($"检测到循环依赖: {container.Id}");
                return;
            }

            visiting.Add(container.Id);

            // 先加载依赖项
            foreach (var depId in container.Metadata.Dependencies ?? new List<string>())
            {
                var dep = containers.FirstOrDefault(c => c.Id == depId);
                if (dep != null)
                {
                    Visit(dep);
                }
                else
                {
                    _logger.Warning($"{container.Name} 依赖的插件未找到: {depId}");
                }
            }

            visiting.Remove(container.Id);
            visited.Add(container.Id);
            sorted.Add(container);
        }

        // 先按 LoadOrder 排序
        var orderedContainers = containers.OrderBy(c => c.Metadata.LoadOrder).ToList();

        // 再按依赖关系排序
        foreach (var container in orderedContainers)
        {
            Visit(container);
        }

        return sorted;
    }

    /// <summary>
    /// 依赖管理：检测冲突 + 自动下载
    /// </summary>
    private async Task<bool> ManageDependenciesAsync(List<PluginContainer> containers)
    {
        try
        {
            // 1. 检测版本冲突
            var strategy = ParseConflictResolutionStrategy(_depConfig.ConflictResolution);
            var resolver = new DependencyConflictResolver(_logger, strategy);
            var conflicts = resolver.AnalyzeDependencies(containers);

            if (conflicts.Count > 0)
            {
                if (_depConfig.ShowConflictReport)
                {
                    _logger.Warning(resolver.GenerateConflictReport(conflicts));
                }

                // 解决冲突
                if (!resolver.ResolveConflicts(conflicts))
                {
                    _logger.Error("无法解决依赖冲突");
                    return false;
                }

                // 更新插件依赖版本为解决后的版本
                UpdateResolvedVersions(containers, conflicts);
            }

            // 2. 自动下载缺失的依赖
            if (_depConfig.AutoDownload)
            {
                var globalLibPath = Path.Combine(AppContext.BaseDirectory, "lib");
                var downloader = new NuGetDependencyDownloader(
                    globalLibPath,
                    _logger,
                    _depConfig.NugetSources,
                    _depConfig.DownloadTimeout,
                    _depConfig.VerifyHash);

                // 收集所有需要下载的依赖
                var allDependencies = new List<LibraryDependency>();
                foreach (var container in containers)
                {
                    if (container.Metadata.LibraryDependencies != null)
                    {
                        // 只下载 lib 类型的依赖
                        var libDependencies = container.Metadata.LibraryDependencies
                            .Where(d => d.Location != "local")
                            .ToList();
                        allDependencies.AddRange(libDependencies);
                    }
                }

                // 去重
                allDependencies = allDependencies
                    .GroupBy(d => d.Name)
                    .Select(g => g.First())
                    .ToList();

                if (allDependencies.Count > 0)
                {
                    _logger.Info($"需要检查 {allDependencies.Count} 个依赖");
                    var results = await downloader.DownloadDependenciesAsync(allDependencies);

                    var failed = results.Where(r => !r.Value).ToList();
                    if (failed.Count > 0)
                    {
                        _logger.Error($"以下依赖下载失败:");
                        foreach (var fail in failed)
                        {
                            _logger.Error($"  - {fail.Key}");
                        }
                        return false;
                    }

                    _logger.Info("所有依赖都已就绪");
                }

                downloader.Dispose();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("依赖管理过程出错", ex);
            return false;
        }
    }

    /// <summary>
    /// 解析冲突解决策略
    /// </summary>
    private ConflictResolutionStrategy ParseConflictResolutionStrategy(string strategy)
    {
        return strategy.ToLowerInvariant() switch
        {
            "highest" => ConflictResolutionStrategy.UseHighest,
            "lowest" => ConflictResolutionStrategy.UseLowest,
            "fail" => ConflictResolutionStrategy.Fail,
            _ => ConflictResolutionStrategy.UseHighest
        };
    }

    /// <summary>
    /// 更新依赖的解决版本
    /// </summary>
    private void UpdateResolvedVersions(
        List<PluginContainer> containers,
        List<DependencyConflict> conflicts)
    {
        var resolutionMap = conflicts.ToDictionary(
            c => c.DependencyName,
            c => c.ResolvedVersion ?? "latest");

        foreach (var container in containers)
        {
            if (container.Metadata.LibraryDependencies == null)
                continue;

            foreach (var dep in container.Metadata.LibraryDependencies)
            {
                if (resolutionMap.TryGetValue(dep.Name, out var resolved))
                {
                    _logger.Debug($"更新 {container.Name} 的依赖 {dep.Name}: {dep.Version} -> {resolved}");
                    dep.Version = resolved;
                }
            }
        }
    }
}

