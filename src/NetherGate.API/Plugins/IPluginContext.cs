using NetherGate.API.Data;
using NetherGate.API.Events;
using NetherGate.API.FileSystem;
using NetherGate.API.GameDisplay;
using NetherGate.API.Logging;
using NetherGate.API.Monitoring;
using NetherGate.API.Network;
using NetherGate.API.Protocol;

namespace NetherGate.API.Plugins;

/// <summary>
/// 插件上下文
/// 为插件提供访问 NetherGate 核心功能的接口
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// 插件信息
    /// </summary>
    PluginInfo PluginInfo { get; }

    /// <summary>
    /// 插件数据目录
    /// 每个插件有独立的数据目录，用于存储配置、数据文件等
    /// </summary>
    string DataDirectory { get; }

    /// <summary>
    /// 日志记录器
    /// 自动带有插件名称前缀
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// 事件总线
    /// 用于订阅和发布事件
    /// </summary>
    IEventBus EventBus { get; }

    /// <summary>
    /// SMP API
    /// 用于与 Minecraft 服务器通信
    /// </summary>
    ISmpApi SmpApi { get; }

    /// <summary>
    /// 配置管理器
    /// 用于加载和保存插件配置
    /// </summary>
    IConfigManager ConfigManager { get; }

    /// <summary>
    /// 命令管理器
    /// 用于注册插件命令
    /// </summary>
    ICommandManager CommandManager { get; }

    /// <summary>
    /// RCON 客户端
    /// 用于执行游戏命令
    /// </summary>
    IRconClient? RconClient { get; }

    /// <summary>
    /// 文件监听服务
    /// 用于监听服务器文件变更
    /// </summary>
    IFileWatcher FileWatcher { get; }

    /// <summary>
    /// 服务器文件访问
    /// 用于读写服务器配置和数据文件
    /// </summary>
    IServerFileAccess ServerFileAccess { get; }

    /// <summary>
    /// 备份管理器
    /// 用于创建和恢复服务器备份
    /// </summary>
    IBackupManager BackupManager { get; }

    /// <summary>
    /// 性能监控器
    /// 用于监控服务器性能指标
    /// </summary>
    IPerformanceMonitor PerformanceMonitor { get; }

    /// <summary>
    /// 玩家数据读取器
    /// 用于读取玩家 NBT 数据
    /// </summary>
    IPlayerDataReader PlayerDataReader { get; }

    /// <summary>
    /// 世界数据读取器
    /// 用于读取世界信息
    /// </summary>
    IWorldDataReader WorldDataReader { get; }

    /// <summary>
    /// 服务器查询
    /// 用于查询其他 Minecraft 服务器的状态（Server List Ping）
    /// </summary>
    IServerQuery ServerQuery { get; }

    /// <summary>
    /// 服务器监控
    /// 用于持续监控多个 Minecraft 服务器的状态
    /// </summary>
    IServerMonitor ServerMonitor { get; }

    /// <summary>
    /// 插件消息传递器
    /// 用于插件间通信
    /// </summary>
    IPluginMessenger Messenger { get; }

    /// <summary>
    /// 游戏显示 API
    /// 用于在游戏中显示 BossBar、Title、ActionBar、计分板等
    /// </summary>
    IGameDisplayApi GameDisplay { get; }

    /// <summary>
    /// NBT 数据写入器
    /// 用于编辑和写入玩家/世界 NBT 数据
    /// ⚠️ 警告：修改 NBT 需要谨慎，建议在服务器停止时操作
    /// </summary>
    INbtDataWriter NbtDataWriter { get; }

    /// <summary>
    /// 网络事件监听器
    /// 用于监听 Minecraft 服务器的网络层事件
    /// </summary>
    INetworkEventListener NetworkEventListener { get; }

    /// <summary>
    /// 获取其他插件
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    /// <returns>插件实例，如果不存在则返回 null</returns>
    IPlugin? GetPlugin(string pluginId);

    /// <summary>
    /// 获取所有已加载的插件
    /// </summary>
    IReadOnlyList<IPlugin> GetAllPlugins();
}

/// <summary>
/// 配置管理器接口
/// </summary>
public interface IConfigManager
{
    /// <summary>
    /// 加载配置文件
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="fileName">配置文件名（相对于插件数据目录）</param>
    /// <returns>配置对象</returns>
    Task<T> LoadConfigAsync<T>(string fileName = "config.json") where T : class, new();

    /// <summary>
    /// 保存配置文件
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="config">配置对象</param>
    /// <param name="fileName">配置文件名（相对于插件数据目录）</param>
    Task SaveConfigAsync<T>(T config, string fileName = "config.json") where T : class;

    /// <summary>
    /// 重载配置文件
    /// </summary>
    /// <typeparam name="T">配置类型</typeparam>
    /// <param name="fileName">配置文件名</param>
    Task<T> ReloadConfigAsync<T>(string fileName = "config.json") where T : class, new();
}

/// <summary>
/// 命令管理器接口
/// </summary>
public interface ICommandManager
{
    /// <summary>
    /// 注册命令
    /// </summary>
    /// <param name="command">命令</param>
    void RegisterCommand(ICommand command);

    /// <summary>
    /// 注销命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    void UnregisterCommand(string commandName);

    /// <summary>
    /// 注销插件的所有命令
    /// </summary>
    /// <param name="pluginId">插件 ID</param>
    void UnregisterPluginCommands(string pluginId);

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="commandLine">命令行</param>
    /// <param name="sender">命令发送者（可选，默认为控制台）</param>
    Task<CommandResult> ExecuteCommandAsync(string commandLine, ICommandSender? sender = null);

    /// <summary>
    /// 获取所有命令
    /// </summary>
    IReadOnlyDictionary<string, ICommand> GetAllCommands();

    /// <summary>
    /// Tab 补全命令
    /// </summary>
    /// <param name="partialCommandLine">部分命令行</param>
    /// <param name="sender">命令发送者（可选，默认为控制台）</param>
    /// <returns>补全建议列表</returns>
    Task<List<string>> TabCompleteAsync(string partialCommandLine, ICommandSender? sender = null);
}

/// <summary>
/// 命令接口
/// </summary>
public interface ICommand
{
    /// <summary>
    /// 命令名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 命令描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 命令用法
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// 命令别名
    /// </summary>
    List<string> Aliases { get; }

    /// <summary>
    /// 所属插件 ID
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// 所需权限节点（null 表示无需权限）
    /// </summary>
    string? Permission { get; }

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="sender">命令发送者</param>
    /// <param name="args">参数</param>
    Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args);

    /// <summary>
    /// Tab 补全
    /// </summary>
    /// <param name="sender">命令发送者</param>
    /// <param name="args">当前已输入的参数</param>
    /// <returns>补全建议列表</returns>
    Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        return Task.FromResult(new List<string>());
    }
}

/// <summary>
/// 命令发送者接口
/// </summary>
public interface ICommandSender
{
    /// <summary>
    /// 发送者名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否是控制台
    /// </summary>
    bool IsConsole { get; }

    /// <summary>
    /// 发送消息给命令发送者
    /// </summary>
    /// <param name="message">消息</param>
    void SendMessage(string message);

    /// <summary>
    /// 检查权限
    /// </summary>
    /// <param name="permission">权限节点</param>
    bool HasPermission(string permission);
}

/// <summary>
/// 命令执行结果
/// </summary>
public record CommandResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; init; }

    public static CommandResult Ok(string message = "") => new() { Success = true, Message = message };
    public static CommandResult Fail(string error) => new() { Success = false, Error = error };
}

