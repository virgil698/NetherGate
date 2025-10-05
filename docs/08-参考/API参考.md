# NetherGate API 参考

本文档提供 NetherGate 所有公开 API 的完整参考。

---

## 📋 **目录**

- [IPluginContext](#iplugincontext)
- [ILogger](#ilogger)
- [IEventBus](#ieventbus)
- [ICommandManager](#icommandmanager)
- [IConfigManager](#iconfigmanager)
- [IPermissionManager](#ipermissionmanager)
- [IGameDisplayApi](#igamedisplayapi)
- [IRconClient](#irconclient)
- [ISmpApi](#ismpapi)
- [IPlayerDataReader](#iplayerdatareader)
- [IWorldDataReader](#iworlddatareader)
- [INbtDataWriter](#inbtdatawriter)
- [IPluginMessenger](#ipluginmessenger)

---

## 🔌 **IPluginContext**

插件上下文，提供所有框架服务的访问入口。

```csharp
public interface IPluginContext
{
    // 核心服务
    ILogger Logger { get; }
    IEventBus EventBus { get; }
    ICommandManager CommandManager { get; }
    IConfigManager ConfigManager { get; }
    IPermissionManager PermissionManager { get; }
    
    // 插件信息
    PluginMetadata Metadata { get; }
    string DataDirectory { get; }
    IPluginManager PluginManager { get; }
    
    // 服务器交互
    IGameDisplayApi GameDisplayApi { get; }
    IRconClient RconClient { get; }
    ISmpApi SmpApi { get; }
    
    // 数据访问
    IPlayerDataReader PlayerDataReader { get; }
    IWorldDataReader WorldDataReader { get; }
    INbtDataWriter NbtDataWriter { get; }
    
    // 高级功能
    IFileWatcher FileWatcher { get; }
    IBackupManager BackupManager { get; }
    IPerformanceMonitor PerformanceMonitor { get; }
    INetworkEventListener NetworkEventListener { get; }
    IPluginMessenger Messenger { get; }
}
```

---

## 📝 **ILogger**

日志记录器，用于输出日志信息。

### **方法**

```csharp
public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
```

### **使用示例**

```csharp
// 使用 PluginBase（推荐）
Logger.Debug("调试信息 - 开发时使用");
Logger.Info("一般信息 - 正常日志");
Logger.Warning("警告信息 - 需要注意但不影响运行");
Logger.Error("错误信息 - 出现了错误");

// 使用 IPluginContext
_context.Logger.Info($"玩家 {playerName} 执行了操作");

// 日志示例
Logger.Info("插件启动完成");
Logger.Debug($"加载配置: {config}");
Logger.Warning("配置文件缺少某些选项，使用默认值");
Logger.Error($"无法连接到数据库: {ex.Message}");
```

---

## 📡 **IEventBus**

事件总线，用于发布和订阅事件。

### **接口定义**

```csharp
namespace NetherGate.API.Events
{
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理器（异步）</param>
        /// <param name="priority">事件优先级（默认 Normal）</param>
        void Subscribe<TEvent>(Func<TEvent, Task> handler, EventPriority priority = EventPriority.Normal) 
            where TEvent : class;
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Unsubscribe<TEvent>(Func<TEvent, Task> handler) 
            where TEvent : class;
        
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="event">事件对象</param>
        Task PublishAsync<TEvent>(TEvent @event) 
            where TEvent : class;
    }
}
```

### **EventPriority 枚举**

```csharp
public enum EventPriority
{
    Lowest = 0,    // 最低优先级（最先执行）
    Low = 1,
    Normal = 2,    // 默认
    High = 3,
    Highest = 4,   // 最高优先级（最后执行）
    Monitor = 5    // 监视器（仅监听，不修改）
}
```

### **使用示例**

```csharp
// 订阅事件（使用 PluginBase）
Events.Subscribe<PlayerJoinedEvent>(OnPlayerJoined, EventPriority.Normal);
Events.Subscribe<ServerReadyEvent>(OnServerReady, EventPriority.High);

private async Task OnPlayerJoined(PlayerJoinedEvent e)
{
    Logger.Info($"{e.Player.Name} 加入服务器");
    await GameDisplay.SendChatMessage(e.Player.Name, "§a欢迎！");
}

// 使用 Lambda 表达式
Events.Subscribe<PlayerLeftEvent>(e =>
{
    Logger.Info($"{e.Player.Name} 离开服务器");
    return Task.CompletedTask;
}, EventPriority.Normal);

// 异步 Lambda
Events.Subscribe<PlayerJoinedEvent>(async e =>
{
    var data = await LoadPlayerDataAsync(e.Player.Uuid);
    await SendWelcomeMessage(e.Player.Name, data);
}, EventPriority.Normal);

// 取消订阅
Events.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);

// 发布自定义事件
await Events.PublishAsync(new CustomEvent { Data = "test" });
```

---

## ⚙️ **ICommandManager**

命令管理器，用于注册和管理命令。

### **方法**

```csharp
public interface ICommandManager
{
    /// <summary>
    /// 注册命令
    /// </summary>
    void RegisterCommand(ICommand command);
    
    /// <summary>
    /// 注销命令
    /// </summary>
    void UnregisterCommand(string commandName);
    
    /// <summary>
    /// 注册命令别名
    /// </summary>
    void RegisterAlias(string commandName, string alias);
    
    /// <summary>
    /// 执行命令
    /// </summary>
    Task<CommandResult> ExecuteCommandAsync(string input, ICommandSender? sender = null);
}
```

### **ICommand 接口**

```csharp
namespace NetherGate.API.Commands
{
    public interface ICommand
    {
        /// <summary>命令名称</summary>
        string Name { get; }
        
        /// <summary>命令描述</summary>
        string Description { get; }
        
        /// <summary>使用方法提示</summary>
        string Usage { get; }
        
        /// <summary>命令别名</summary>
        string[] Aliases { get; }
        
        /// <summary>所属插件 ID</summary>
        string PluginId { get; }
        
        /// <summary>所需权限（null 表示无需权限）</summary>
        string? Permission { get; }
        
        /// <summary>执行命令</summary>
        /// <param name="sender">命令发送者</param>
        /// <param name="args">命令参数</param>
        Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args);
        
        /// <summary>Tab 补全</summary>
        /// <param name="sender">命令发送者</param>
        /// <param name="args">当前输入的参数</param>
        Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args);
    }
}
```

### **ICommandSender 接口**

```csharp
namespace NetherGate.API.Commands
{
    public interface ICommandSender
    {
        /// <summary>发送者名称</summary>
        string Name { get; }
        
        /// <summary>是否是控制台</summary>
        bool IsConsole { get; }
        
        /// <summary>发送消息给命令发送者</summary>
        void SendMessage(string message);
        
        /// <summary>检查权限</summary>
        bool HasPermission(string permission);
    }
}
```

### **CommandResult 记录**

```csharp
namespace NetherGate.API.Commands
{
    public record CommandResult
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        
        // 静态工厂方法
        public static CommandResult Ok(string message) 
            => new CommandResult { Success = true, Message = message };
        
        public static CommandResult Fail(string message) 
            => new CommandResult { Success = false, Message = message };
    }
}
```

### **命令实现示例**

```csharp
using NetherGate.API.Commands;
using NetherGate.API.Plugins;

public class MyCommand : ICommand
{
    private readonly PluginBase _plugin;
    
    public string Name => "mycommand";
    public string Description => "我的自定义命令";
    public string Usage => "#mycommand <arg>";
    public string[] Aliases => new[] { "mc", "mycmd" };
    public string PluginId => _plugin.Metadata.Id;
    public string? Permission => "myplugin.mycommand";
    
    public MyCommand(PluginBase plugin)
    {
        _plugin = plugin;
    }
    
    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (sender.IsConsole)
        {
            _plugin.Logger.Info("从控制台执行");
            return CommandResult.Ok("命令执行成功");
        }
        
        if (args.Length == 0)
        {
            return CommandResult.Fail($"用法: {Usage}");
        }
        
        // 执行命令逻辑
        await _plugin.GameDisplay.SendChatMessage(sender.Name, $"§a你输入了: {args[0]}");
        return CommandResult.Ok("成功");
    }
    
    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            return new List<string> { "option1", "option2", "option3" };
        }
        return new List<string>();
    }
}

// 注册命令（在 OnEnableAsync 中）
Commands.RegisterCommand(new MyCommand(this));

// 注销命令（在 OnDisableAsync 中，通常会自动注销）
Commands.UnregisterCommand("mycommand");
```

---

## 📄 **IConfigManager**

配置管理器，用于加载和保存配置文件。

### **方法**

```csharp
public interface IConfigManager
{
    /// <summary>
    /// 加载配置（不存在则创建默认配置）
    /// </summary>
    Task<T> LoadConfigAsync<T>(string fileName) where T : class, new();
    
    /// <summary>
    /// 保存配置
    /// </summary>
    Task SaveConfigAsync<T>(string fileName, T config) where T : class;
    
    /// <summary>
    /// 重新加载配置
    /// </summary>
    Task<T> ReloadConfigAsync<T>(string fileName) where T : class, new();
    
    /// <summary>
    /// 检查配置是否存在
    /// </summary>
    bool ConfigExists(string fileName);
    
    /// <summary>
    /// 删除配置
    /// </summary>
    Task DeleteConfigAsync(string fileName);
}
```

### **配置类示例**

```csharp
public class MyConfig
{
    public string ServerName { get; set; } = "My Server";
    public int MaxPlayers { get; set; } = 20;
    public bool EnableFeature { get; set; } = true;
    public List<string> AllowedWorlds { get; set; } = new() { "world", "world_nether", "world_the_end" };
}
```

### **使用示例**

```csharp
// 使用 PluginBase（推荐）
public override async Task OnEnableAsync()
{
    // 加载配置（不存在则创建默认配置）
    var config = await Config.LoadConfigAsync<MyConfig>("config");
    Logger.Info($"服务器名称: {config.ServerName}");
    
    // 修改配置
    config.MaxPlayers = 30;
    
    // 保存配置
    await Config.SaveConfigAsync("config", config);
    
    // 重新加载配置
    config = await Config.ReloadConfigAsync<MyConfig>("config");
    
    // 检查配置文件是否存在
    if (Config.ConfigExists("backup-config"))
    {
        var backupConfig = await Config.LoadConfigAsync<MyConfig>("backup-config");
    }
    
    // 删除配置
    await Config.DeleteConfigAsync("old-config");
}

// 使用 IPluginContext
var config = await _context.ConfigManager.LoadConfigAsync<MyConfig>("config");
```

---

## 🔒 **IPermissionManager**

权限管理器，用于检查和管理权限。

### **方法**

```csharp
public interface IPermissionManager
{
    // 权限检查
    Task<bool> HasPermissionAsync(string playerName, string permission);
    Task<PermissionLevel> GetPermissionLevelAsync(string playerName);
    
    // 用户权限
    Task GrantPermissionAsync(string playerName, string permission);
    Task RevokePermissionAsync(string playerName, string permission);
    Task<List<string>> GetUserPermissionsAsync(string playerName);
    
    // 用户组
    Task AddUserToGroupAsync(string playerName, string groupName);
    Task RemoveUserFromGroupAsync(string playerName, string groupName);
    Task<List<string>> GetUserGroupsAsync(string playerName);
    
    // 权限组
    Task CreateGroupAsync(string groupName, int priority = 0);
    Task DeleteGroupAsync(string groupName);
    Task GrantGroupPermissionAsync(string groupName, string permission);
    Task RevokeGroupPermissionAsync(string groupName, string permission);
    Task<List<string>> GetGroupPermissionsAsync(string groupName);
    
    // 配置
    Task ReloadAsync();
    Task SaveAsync();
}
```

### **PermissionLevel 枚举**

```csharp
public enum PermissionLevel
{
    User,       // 普通用户
    Moderator,  // 版主
    Admin,      // 管理员
    Owner       // 服主
}
```

### **使用示例**

```csharp
// 使用 PluginBase（推荐）
// 权限检查（命令系统会自动检查，这里是手动检查示例）
public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
{
    // 方式1：使用 sender.HasPermission（推荐）
    if (!sender.HasPermission("myplugin.admin"))
    {
        return CommandResult.Fail("§c你没有权限执行此命令");
    }
    
    // 方式2：使用 PermissionManager（用于检查其他玩家）
    bool hasVip = await _context.PermissionManager.HasPermissionAsync("Steve", "vip.features");
    if (hasVip)
    {
        // 给予 VIP 特权
    }
    
    // 获取玩家权限等级
    var level = await _context.PermissionManager.GetPermissionLevelAsync("Steve");
    if (level >= PermissionLevel.Admin)
    {
        // 管理员操作
    }
    
    return CommandResult.Ok("成功");
}

// 权限管理示例
public async Task ManagePermissions()
{
    // 创建权限组
    await _context.PermissionManager.CreateGroupAsync("vip", priority: 10);
    await _context.PermissionManager.CreateGroupAsync("admin", priority: 100);
    
    // 给权限组添加权限
    await _context.PermissionManager.GrantGroupPermissionAsync("vip", "essentials.home");
    await _context.PermissionManager.GrantGroupPermissionAsync("vip", "essentials.tpa");
    await _context.PermissionManager.GrantGroupPermissionAsync("admin", "essentials.*");
    
    // 添加用户到组
    await _context.PermissionManager.AddUserToGroupAsync("Steve", "vip");
    await _context.PermissionManager.AddUserToGroupAsync("Alex", "admin");
    
    // 给用户直接授予权限
    await _context.PermissionManager.GrantPermissionAsync("Steve", "special.feature");
    
    // 查询用户权限和组
    var userGroups = await _context.PermissionManager.GetUserGroupsAsync("Steve");
    var userPermissions = await _context.PermissionManager.GetUserPermissionsAsync("Steve");
    
    Logger.Info($"Steve 所在组: {string.Join(", ", userGroups)}");
    Logger.Info($"Steve 的权限: {string.Join(", ", userPermissions)}");
    
    // 移除权限
    await _context.PermissionManager.RevokePermissionAsync("Steve", "special.feature");
    await _context.PermissionManager.RemoveUserFromGroupAsync("Steve", "vip");
    
    // 重新加载权限配置
    await _context.PermissionManager.ReloadAsync();
}
```

---

## 🎮 **IGameDisplayApi**

游戏显示 API，用于与 Minecraft 游戏交互。

### **BossBar**

```csharp
Task ShowBossBar(string playerName, string id, string title, BossBarColor color, 
    BossBarStyle style, float progress = 1.0f, bool darkenScreen = false, 
    bool createFog = false, bool playBossMusic = false);
Task UpdateBossBarValue(string playerName, string id, float value);
Task UpdateBossBarName(string playerName, string id, string name);
Task UpdateBossBarColor(string playerName, string id, BossBarColor color);
Task UpdateBossBarStyle(string playerName, string id, BossBarStyle style);
Task RemoveBossBar(string playerName, string id);
```

### **记分板**

```csharp
Task CreateScoreboardObjective(string name, string criterion, string displayName);
Task RemoveScoreboardObjective(string name);
Task SetScoreboardDisplay(DisplaySlot slot, string objectiveName);
Task SetScoreboardValue(string objectiveName, string targetName, int value);
Task AddScoreboardValue(string objectiveName, string targetName, int value);
Task RemoveScoreboardValue(string objectiveName, string targetName, int value);
Task ResetScoreboardScore(string objectiveName, string targetName);
```

### **标题和消息**

```csharp
Task ShowTitle(string playerName, string title, string subtitle = "", 
    int fadeIn = 10, int stay = 70, int fadeOut = 20);
Task ShowTitleOnly(string playerName, string title, int fadeIn = 10, 
    int stay = 70, int fadeOut = 20);
Task ShowSubtitleOnly(string playerName, string subtitle, int fadeIn = 10, 
    int stay = 70, int fadeOut = 20);
Task ShowActionBar(string playerName, string message);
Task ClearTitle(string playerName);
Task ResetTitleTimes(string playerName);
```

### **聊天**

```csharp
Task SendChatMessage(string playerName, string message);
Task SendJsonMessage(string playerName, string jsonMessage);
Task BroadcastMessage(string message);
```

### **对话框**

```csharp
Task ShowDialog(string playerName, string title, string author, string[] pages);
```

### **玩家控制**

```csharp
Task TeleportPlayer(string playerName, double x, double y, double z, 
    float yaw = 0, float pitch = 0);
Task TeleportPlayerToPlayer(string playerName, string targetPlayer);
Task SetPlayerHealth(string playerName, float health);
Task SetPlayerHunger(string playerName, int hunger);
Task SetPlayerLevel(string playerName, int level);
Task SetGameMode(string playerName, GameMode mode);
Task GiveEffect(string playerName, string effect, int duration, int amplifier = 0);
Task ClearEffect(string playerName, string effect);
Task ClearAllEffects(string playerName);
Task GiveItem(string playerName, string item, int count = 1, string? nbt = null);
Task ClearInventory(string playerName);
Task ClearItem(string playerName, string item);
```

### **实体和世界**

```csharp
Task SummonEntity(string entityType, double x, double y, double z, string? nbt = null);
Task KillEntity(string selector);
Task SetWorldTime(string time);
Task SetWeather(string weather);
Task SetGameRule(string rule, string value);
Task SetBlock(int x, int y, int z, string blockType);
Task FillBlocks(int x1, int y1, int z1, int x2, int y2, int z2, string blockType);
```

### **粒子和音效**

```csharp
Task PlayParticle(string particleType, double x, double y, double z, 
    int count = 1, float speed = 0.0f);
Task PlaySound(string playerName, string sound, float volume = 1.0f, float pitch = 1.0f);
Task PlaySound(string selector, string sound, double x, double y, double z, 
    float volume = 1.0f, float pitch = 1.0f);
Task StopSound(string playerName, string sound);
```

---

## 🌐 **IRconClient**

RCON 客户端，用于发送原生 Minecraft 命令。

### **方法**

```csharp
public interface IRconClient
{
    Task<bool> ConnectAsync();
    void Disconnect();
    Task<string> SendCommandAsync(string command);
    bool IsConnected { get; }
}
```

### **使用示例**

```csharp
// 使用 PluginBase（推荐）
// 发送 RCON 命令
var response = await Rcon.SendCommandAsync("list");
Logger.Info($"在线玩家: {response}");

// 执行游戏命令
await Rcon.SendCommandAsync("say §aHello from NetherGate!");
await Rcon.SendCommandAsync("give Steve diamond 64");
await Rcon.SendCommandAsync("tp Steve 0 64 0");
await Rcon.SendCommandAsync("gamemode creative Alex");

// 检查连接状态
if (Rcon.IsConnected)
{
    var time = await Rcon.SendCommandAsync("time query daytime");
    Logger.Info($"当前时间: {time}");
}
```

---

## 📊 **ISmpApi**

SMP（Server Management Protocol）客户端，用于与服务器插件通信。

### **方法**

```csharp
public interface ISmpApi
{
    // 连接
    Task<bool> ConnectAsync();
    void Disconnect();
    bool IsConnected { get; }
    
    // 玩家管理
    Task<List<PlayerInfo>> GetOnlinePlayersAsync();
    Task KickPlayerAsync(string playerName, string reason);
    Task BanPlayerAsync(string playerName, string reason, DateTime? expires = null);
    Task UnbanPlayerAsync(string playerName);
    
    // 白名单
    Task<List<string>> GetAllowlistAsync();
    Task AddToAllowlistAsync(string playerName);
    Task RemoveFromAllowlistAsync(string playerName);
    
    // 服务器状态
    Task<ServerState> GetServerStatusAsync();
}
```

### **使用示例**

```csharp
// 使用 PluginBase（推荐）
// 获取在线玩家
var players = await Server.GetOnlinePlayersAsync();
foreach (var player in players)
{
    Logger.Info($"玩家: {player.Name} (UUID: {player.Uuid})");
}

// 踢出玩家
await Server.KickPlayerAsync("Steve", "违反服务器规则");

// 封禁玩家（临时封禁）
await Server.BanPlayerAsync("Hacker", "使用作弊工具", 
    expires: DateTime.Now.AddDays(7));

// 封禁玩家（永久封禁）
await Server.BanPlayerAsync("Cheater", "严重违规", expires: null);

// 解封玩家
await Server.UnbanPlayerAsync("Steve");

// 白名单管理
var allowlist = await Server.GetAllowlistAsync();
Logger.Info($"白名单玩家: {string.Join(", ", allowlist)}");

await Server.AddToAllowlistAsync("NewPlayer");
await Server.RemoveFromAllowlistAsync("OldPlayer");

// 获取服务器状态
var status = await Server.GetServerStatusAsync();
Logger.Info($"在线玩家数: {status.OnlinePlayers}/{status.MaxPlayers}");
Logger.Info($"服务器版本: {status.Version}");
Logger.Info($"TPS: {status.Tps:F2}");

// 检查连接状态
if (Server.IsConnected)
{
    Logger.Info("SMP 协议已连接");
}
```

---

## 📖 **IPlayerDataReader**

玩家数据读取器，用于读取玩家 NBT 数据。

### **方法**

```csharp
public interface IPlayerDataReader
{
    Task<PlayerData?> ReadPlayerDataAsync(string playerName);
    Task<List<ItemStack>> GetPlayerInventoryAsync(string playerName);
    Task<Dictionary<string, int>> GetPlayerStatsAsync(string playerName);
    Task<List<string>> GetPlayerAdvancementsAsync(string playerName);
}
```

### **使用示例**

```csharp
// 读取玩家数据
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync("Steve");
if (playerData != null)
{
    _context.Logger.Info($"玩家位置: {playerData.Position}");
    _context.Logger.Info($"生命值: {playerData.Health}");
}

// 获取背包
var inventory = await _context.PlayerDataReader.GetPlayerInventoryAsync("Steve");
```

---

## 🌍 **IWorldDataReader**

世界数据读取器，用于读取世界 NBT 数据。

### **方法**

```csharp
public interface IWorldDataReader
{
    Task<WorldData?> ReadWorldDataAsync(string worldName);
    Task<List<string>> GetLoadedChunksAsync(string worldName);
}
```

---

## ✏️ **INbtDataWriter**

NBT 数据写入器，用于修改玩家和世界数据。

### **方法**

```csharp
public interface INbtDataWriter
{
    // 玩家数据
    Task UpdatePlayerPositionAsync(string playerName, double x, double y, double z);
    Task UpdatePlayerHealthAsync(string playerName, float health);
    Task UpdatePlayerHungerAsync(string playerName, int foodLevel);
    Task AddItemToInventoryAsync(string playerName, ItemStack item);
    Task RemoveItemFromInventoryAsync(string playerName, int slot);
    Task UpdateInventorySlotAsync(string playerName, int slot, ItemStack item);
    Task ClearInventoryAsync(string playerName);
    
    // 世界数据
    Task UpdateWorldSpawnAsync(string worldName, int x, int y, int z);
    Task UpdateWorldTimeAsync(string worldName, long time);
    Task UpdateGameRuleAsync(string worldName, string rule, string value);
}
```

---

## 📬 **IPluginMessenger**

插件间通信接口，用于插件之间传递消息。

### **方法**

```csharp
public interface IPluginMessenger
{
    // 发送消息
    Task SendMessageAsync(string targetPlugin, string channel, object? data = null);
    Task<TResponse?> SendRequestAsync<TResponse>(string targetPlugin, 
        string channel, object? data = null, int timeoutMs = 5000);
    Task BroadcastMessageAsync(string channel, object? data = null);
    
    // 接收消息
    void Subscribe(string channel, Action<PluginMessage> handler);
    void SubscribeRequest<TRequest, TResponse>(string channel, 
        Func<TRequest, Task<TResponse>> handler);
    void Unsubscribe(string channel, Action<PluginMessage> handler);
}
```

### **PluginMessage 类**

```csharp
public class PluginMessage
{
    public string SourcePlugin { get; set; }
    public string Channel { get; set; }
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### **使用示例**

```csharp
// 发送消息
await _context.Messenger.SendMessageAsync("EconomyPlugin", "balance", 
    new { PlayerName = "Steve" });

// 发送请求并等待响应
var balance = await _context.Messenger.SendRequestAsync<int>(
    "EconomyPlugin", "get_balance", new { PlayerName = "Steve" });

// 订阅消息
_context.Messenger.Subscribe("balance", msg =>
{
    _context.Logger.Info($"收到消息: {msg.Data}");
});

// 订阅请求
_context.Messenger.SubscribeRequest<BalanceRequest, int>("get_balance", 
    async req => await GetPlayerBalance(req.PlayerName));
```

---

## 📚 **相关文档**

- [插件开发指南](../03-插件开发/插件开发指南.md)
- [事件列表](./事件列表.md)
- [命令系统](../02-核心功能/命令系统.md)

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05
