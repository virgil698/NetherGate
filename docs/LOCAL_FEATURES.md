# NetherGate 本地功能与网络查询接口

本文档介绍 NetherGate 提供的**本地文件系统、监控功能和网络查询功能**。

---

## 📋 功能概览

| 功能模块 | 接口 | 状态 | 描述 |
|---------|------|------|------|
| 文件监听 | `IFileWatcher` | 🟡 待实现 | 监听服务器文件变更 |
| 文件访问 | `IServerFileAccess` | 🟡 待实现 | 读写服务器配置和数据文件 |
| 备份管理 | `IBackupManager` | 🟡 待实现 | 自动备份和恢复 |
| 性能监控 | `IPerformanceMonitor` | 🟡 待实现 | CPU、内存、磁盘监控 |
| 玩家数据 | `IPlayerDataReader` | 🟡 待实现 | 读取玩家 NBT 数据 |
| 世界数据 | `IWorldDataReader` | 🟡 待实现 | 读取世界信息 |
| **服务器查询** | `IServerQuery` | 🟡 待实现 | 查询其他 MC 服务器状态 |
| **服务器监控** | `IServerMonitor` | 🟡 待实现 | 监控多个 MC 服务器 |

---

## 🔔 1. 文件监听服务 (`IFileWatcher`)

监听服务器文件和目录的变更事件。

### 接口定义

```csharp
public interface IFileWatcher
{
    // 监听单个文件
    string WatchFile(string filePath, Action<FileChangeEvent> callback);
    
    // 监听目录
    string WatchDirectory(string directoryPath, string pattern, 
                         bool includeSubdirectories, Action<FileChangeEvent> callback);
    
    // 停止监听
    void StopWatching(string watcherId);
    void StopAll();
}
```

### 使用示例

```csharp
public class MyPlugin : IPlugin
{
    public async Task OnEnableAsync(IPluginContext context)
    {
        // 监听服务器配置文件变更
        context.FileWatcher.WatchFile("server.properties", e =>
        {
            context.Logger.Info($"服务器配置已更改: {e.ChangeType}");
            // 重新加载配置
        });

        // 监听所有 JSON 配置文件
        context.FileWatcher.WatchDirectory("config", "*.json", true, e =>
        {
            context.Logger.Info($"配置文件 {e.FilePath} 已更改");
        });
    }
}
```

### 支持的变更类型

- `Created` - 文件创建
- `Modified` - 文件修改
- `Deleted` - 文件删除
- `Renamed` - 文件重命名

---

## 📁 2. 服务器文件访问 (`IServerFileAccess`)

安全地读写服务器文件，支持自动备份。

### 接口定义

```csharp
public interface IServerFileAccess
{
    string ServerDirectory { get; }
    
    // 文本文件操作
    Task<string> ReadTextFileAsync(string relativePath);
    Task WriteTextFileAsync(string relativePath, string content, bool backup = true);
    
    // JSON 文件操作
    Task<T?> ReadJsonFileAsync<T>(string relativePath) where T : class;
    Task WriteJsonFileAsync<T>(string relativePath, T data, bool backup = true) where T : class;
    
    // server.properties 操作
    Task<Dictionary<string, string>> ReadServerPropertiesAsync();
    Task WriteServerPropertiesAsync(Dictionary<string, string> properties, bool backup = true);
    
    // 文件管理
    bool FileExists(string relativePath);
    bool DirectoryExists(string relativePath);
    void CreateDirectory(string relativePath);
    Task DeleteFileAsync(string relativePath, bool backup = true);
    List<string> ListFiles(string relativePath, string pattern = "*", bool recursive = false);
    FileInfo GetFileInfo(string relativePath);
}
```

### 使用示例

```csharp
// 读取服务器配置
var props = await context.ServerFileAccess.ReadServerPropertiesAsync();
context.Logger.Info($"服务器端口: {props["server-port"]}");

// 修改配置（自动备份）
props["max-players"] = "100";
await context.ServerFileAccess.WriteServerPropertiesAsync(props);

// 读取自定义 JSON 配置
var config = await context.ServerFileAccess.ReadJsonFileAsync<MyConfig>("config/custom.json");

// 列出世界文件
var worldFiles = context.ServerFileAccess.ListFiles("world", "*", recursive: true);
```

---

## 💾 3. 备份管理 (`IBackupManager`)

自动创建和管理服务器备份。

### 接口定义

```csharp
public interface IBackupManager
{
    string BackupDirectory { get; }
    
    // 创建备份
    Task<string> CreateBackupAsync(string? backupName = null, 
                                   bool includeWorlds = true,
                                   bool includeConfigs = true,
                                   bool includePlugins = false);
    
    Task<string> CreateWorldBackupAsync(string? worldName = null, string? backupName = null);
    
    // 恢复备份
    Task RestoreBackupAsync(string backupPath, bool createBackupBeforeRestore = true);
    
    // 管理备份
    List<BackupInfo> ListBackups();
    Task DeleteBackupAsync(string backupPath);
    Task CleanupBackupsAsync(int keepCount = 10, int olderThanDays = 30);
    
    // 自动备份
    void EnableAutoBackup(int intervalMinutes = 60, int maxBackups = 10);
    void DisableAutoBackup();
}
```

### 使用示例

```csharp
// 创建完整备份
var backupPath = await context.BackupManager.CreateBackupAsync("pre-update");
context.Logger.Info($"备份已创建: {backupPath}");

// 创建世界备份
await context.BackupManager.CreateWorldBackupAsync("world", "before-reset");

// 列出所有备份
var backups = context.BackupManager.ListBackups();
foreach (var backup in backups)
{
    context.Logger.Info($"{backup.Name} - {backup.FormattedSize} - {backup.CreatedAt}");
}

// 启用自动备份（每小时）
context.BackupManager.EnableAutoBackup(intervalMinutes: 60, maxBackups: 24);

// 清理旧备份（保留最新10个，删除30天前的）
await context.BackupManager.CleanupBackupsAsync(keepCount: 10, olderThanDays: 30);
```

---

## 📊 4. 性能监控 (`IPerformanceMonitor`)

监控服务器性能指标。

### 接口定义

```csharp
public interface IPerformanceMonitor
{
    bool IsMonitoring { get; }
    
    // 获取性能数据
    PerformanceSnapshot GetSnapshot();
    List<PerformanceSnapshot> GetHistory(int minutes = 60);
    
    // 控制监控
    void Start(int intervalSeconds = 10);
    void Stop();
    
    // 性能警告事件
    event EventHandler<PerformanceWarningEvent>? PerformanceWarning;
}
```

### 使用示例

```csharp
// 启动性能监控
context.PerformanceMonitor.Start(intervalSeconds: 10);

// 订阅性能警告
context.PerformanceMonitor.PerformanceWarning += (sender, e) =>
{
    context.Logger.Warning($"性能警告: {e.Message}");
    context.Logger.Warning($"当前值: {e.CurrentValue}, 阈值: {e.Threshold}");
};

// 获取当前性能快照
var snapshot = context.PerformanceMonitor.GetSnapshot();
context.Logger.Info($"CPU: {snapshot.CpuUsage:F1}%");
context.Logger.Info($"内存: {snapshot.Memory.UsedMB}/{snapshot.Memory.TotalMB} MB ({snapshot.Memory.UsagePercent:F1}%)");
context.Logger.Info($"磁盘: {snapshot.Disk.UsedGB}/{snapshot.Disk.TotalGB} GB");

// 获取最近1小时的性能历史
var history = context.PerformanceMonitor.GetHistory(minutes: 60);
var avgCpu = history.Average(s => s.CpuUsage);
context.Logger.Info($"平均 CPU 使用率: {avgCpu:F1}%");
```

---

## 👤 5. 玩家数据读取 (`IPlayerDataReader`)

读取玩家的 NBT 数据文件。

### 接口定义

```csharp
public interface IPlayerDataReader
{
    Task<PlayerData?> ReadPlayerDataAsync(Guid playerUuid);
    Task<PlayerStats?> ReadPlayerStatsAsync(Guid playerUuid);
    Task<PlayerAdvancements?> ReadPlayerAdvancementsAsync(Guid playerUuid);
    
    List<Guid> ListPlayers(string? worldName = null);
    List<PlayerData> GetOnlinePlayers();
    bool PlayerDataExists(Guid playerUuid);
}
```

### 使用示例

```csharp
// 读取玩家数据
var playerUuid = Guid.Parse("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
var playerData = await context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null)
{
    context.Logger.Info($"玩家: {playerData.Name}");
    context.Logger.Info($"生命值: {playerData.Health}");
    context.Logger.Info($"等级: {playerData.XpLevel}");
    context.Logger.Info($"位置: ({playerData.Position.X}, {playerData.Position.Y}, {playerData.Position.Z})");
    
    // 背包物品
    foreach (var item in playerData.Inventory)
    {
        context.Logger.Info($"槽位 {item.Slot}: {item.Id} x{item.Count}");
    }
}

// 读取玩家统计
var stats = await context.PlayerDataReader.ReadPlayerStatsAsync(playerUuid);
context.Logger.Info($"游玩时间: {stats.PlayTimeMinutes} 分钟");
context.Logger.Info($"击杀生物: {stats.MobKills.Sum(kvp => kvp.Value)}");
context.Logger.Info($"行走距离: {stats.DistanceWalked:F2} 米");

// 读取玩家成就
var advancements = await context.PlayerDataReader.ReadPlayerAdvancementsAsync(playerUuid);
context.Logger.Info($"完成成就: {advancements.Completed.Count}");
context.Logger.Info($"完成率: {advancements.CompletionPercent:F1}%");

// 列出所有玩家
var allPlayers = context.PlayerDataReader.ListPlayers();
context.Logger.Info($"服务器共有 {allPlayers.Count} 个玩家");
```

---

## 🌍 6. 世界数据读取 (`IWorldDataReader`)

读取 Minecraft 世界信息。

### 接口定义

```csharp
public interface IWorldDataReader
{
    Task<WorldInfo?> ReadWorldInfoAsync(string? worldName = null);
    List<string> ListWorlds();
    long GetWorldSize(string worldName);
    Task<long?> GetWorldSeedAsync(string worldName);
    Task<SpawnPoint?> GetSpawnPointAsync(string worldName);
    bool WorldExists(string worldName);
}
```

### 使用示例

```csharp
// 读取主世界信息
var worldInfo = await context.WorldDataReader.ReadWorldInfoAsync();

if (worldInfo != null)
{
    context.Logger.Info($"世界: {worldInfo.Name}");
    context.Logger.Info($"种子: {worldInfo.Seed}");
    context.Logger.Info($"版本: {worldInfo.Version}");
    context.Logger.Info($"难度: {worldInfo.Difficulty}");
    context.Logger.Info($"游戏时间: {worldInfo.GameTime} 刻");
    context.Logger.Info($"世界大小: {worldInfo.SizeMB} MB");
    context.Logger.Info($"出生点: ({worldInfo.SpawnPoint.X}, {worldInfo.SpawnPoint.Y}, {worldInfo.SpawnPoint.Z})");
    
    // 游戏规则
    foreach (var (rule, value) in worldInfo.GameRules)
    {
        context.Logger.Info($"游戏规则 {rule}: {value}");
    }
}

// 列出所有世界
var worlds = context.WorldDataReader.ListWorlds();
foreach (var world in worlds)
{
    var size = context.WorldDataReader.GetWorldSize(world);
    context.Logger.Info($"世界 {world}: {size} MB");
}

// 获取世界种子
var seed = await context.WorldDataReader.GetWorldSeedAsync("world");
context.Logger.Info($"世界种子: {seed}");
```

---

## 🎯 实现状态

### 当前状态
✅ **接口定义完成** - 所有 API 接口已定义  
🟡 **实现进行中** - 功能实现将在后续版本完成

### 实现优先级

1. **高优先级**（常用功能）
   - `IServerFileAccess` - 文件读写
   - `IBackupManager` - 备份管理
   - `IFileWatcher` - 文件监听

2. **中优先级**（监控功能）
   - `IPerformanceMonitor` - 性能监控

3. **低优先级**（高级功能）
   - `IPlayerDataReader` - 玩家数据（需要 NBT 解析库）
   - `IWorldDataReader` - 世界数据（需要 NBT 解析库）

### 依赖库

- **fNbt** - NBT 数据解析（用于玩家和世界数据）
- **System.IO.Compression** - 备份压缩（已内置）
- **System.Diagnostics** - 性能监控（已内置）

---

## 🌐 7. 服务器查询 (`IServerQuery`)

**基于 [Minecraft Java 版网络协议](https://zh.minecraft.wiki/w/Java%E7%89%88%E7%BD%91%E7%BB%9C%E5%8D%8F%E8%AE%AE)** 实现，查询其他 Minecraft 服务器的状态。

### 接口定义

```csharp
public interface IServerQuery
{
    // 查询服务器状态（Server List Ping）
    Task<ServerStatusResponse?> QueryAsync(string host, int port = 25565, int timeout = 5000);
    
    // Ping 服务器（仅获取延迟）
    Task<long> PingAsync(string host, int port = 25565, int timeout = 5000);
    
    // 批量查询多个服务器
    Task<Dictionary<string, ServerStatusResponse?>> QueryBatchAsync(
        List<ServerAddress> servers, int timeout = 5000);
}
```

### 使用示例

```csharp
// 查询单个服务器
var status = await context.ServerQuery.QueryAsync("mc.hypixel.net");
if (status != null && status.IsOnline)
{
    context.Logger.Info($"服务器: {status.Address}");
    context.Logger.Info($"版本: {status.Version.Name}");
    context.Logger.Info($"在线玩家: {status.Players.Online}/{status.Players.Max}");
    context.Logger.Info($"延迟: {status.Latency}ms");
    context.Logger.Info($"MOTD: {status.Description}");
}

// Ping 服务器
var latency = await context.ServerQuery.PingAsync("mc.hypixel.net");
context.Logger.Info($"延迟: {latency}ms");

// 批量查询
var servers = new List<ServerAddress>
{
    new() { Host = "mc.hypixel.net", FriendlyName = "Hypixel" },
    new() { Host = "play.cubecraft.net", FriendlyName = "CubeCraft" },
    new() { Host = "mc.mineplex.com", FriendlyName = "Mineplex" }
};

var results = await context.ServerQuery.QueryBatchAsync(servers);
foreach (var (address, status) in results)
{
    if (status != null && status.IsOnline)
    {
        context.Logger.Info($"{address}: {status.Players.Online} 玩家在线");
    }
    else
    {
        context.Logger.Warning($"{address}: 离线");
    }
}
```

### 返回的数据

- ✅ 服务器版本信息
- ✅ 在线玩家数 / 最大玩家数
- ✅ 玩家列表样本（部分服务器提供）
- ✅ MOTD（服务器描述）
- ✅ 服务器图标（Base64 编码）
- ✅ Forge Mod 列表（如果是 Forge 服务器）
- ✅ 延迟（Ping）

### 与现有功能的关系

| 功能 | 对象 | 用途 |
|------|------|------|
| **SMP** | 本地服务器 | 管理本地服务器（白名单、封禁等） |
| **RCON** | 本地服务器 | 执行本地服务器命令 |
| **Server Query** | 远程服务器 | 查询远程服务器状态 |

✅ **完全不冲突**，各司其职！

---

## 📊 8. 服务器监控 (`IServerMonitor`)

持续监控多个 Minecraft 服务器的状态，并在状态变化时触发事件。

### 接口定义

```csharp
public interface IServerMonitor
{
    bool IsMonitoring { get; }
    
    // 添加/移除监控
    void AddServer(ServerAddress address, int checkInterval = 60);
    void RemoveServer(string host, int port = 25565);
    
    // 获取状态
    List<MonitoredServer> GetMonitoredServers();
    ServerStatusResponse? GetServerStatus(string host, int port = 25565);
    
    // 刷新
    Task RefreshAllAsync();
    Task RefreshServerAsync(string host, int port = 25565);
    
    // 控制
    void Start();
    void Stop();
    
    // 事件
    event EventHandler<ServerStatusChangedEvent>? StatusChanged;
    event EventHandler<ServerEvent>? ServerOnline;
    event EventHandler<ServerEvent>? ServerOffline;
    event EventHandler<PlayerCountChangedEvent>? PlayerCountChanged;
}
```

### 使用示例

```csharp
public class ServerMonitorPlugin : IPlugin
{
    public async Task OnEnableAsync(IPluginContext context)
    {
        var monitor = context.ServerMonitor;
        
        // 订阅事件
        monitor.ServerOnline += (sender, e) =>
        {
            context.Logger.Info($"✅ {e.Address} 已上线");
            // 可以通过 RCON 向本地服务器广播消息
            context.RconClient?.SendCommandAsync($"say {e.Address} 子服已上线！");
        };
        
        monitor.ServerOffline += (sender, e) =>
        {
            context.Logger.Warning($"❌ {e.Address} 已下线");
            context.RconClient?.SendCommandAsync($"say {e.Address} 子服已离线！");
        };
        
        monitor.PlayerCountChanged += (sender, e) =>
        {
            context.Logger.Info($"👥 {e.Address} 玩家数变化: {e.OldCount} -> {e.NewCount}");
        };
        
        // 添加要监控的服务器
        monitor.AddServer(new ServerAddress 
        { 
            Host = "lobby.mynetwork.com", 
            Port = 25565,
            FriendlyName = "大厅服"
        }, checkInterval: 30); // 每 30 秒检查一次
        
        monitor.AddServer(new ServerAddress 
        { 
            Host = "survival.mynetwork.com", 
            FriendlyName = "生存服"
        }, checkInterval: 60);
        
        // 启动监控
        monitor.Start();
        
        context.Logger.Info("服务器监控已启动");
    }
    
    public async Task OnDisableAsync(IPluginContext context)
    {
        context.ServerMonitor.Stop();
        context.Logger.Info("服务器监控已停止");
    }
}
```

### 实用场景

#### 1. **群组服（BungeeCord/Velocity）监控**
```csharp
// 监控所有子服务器状态
monitor.AddServer(new() { Host = "lobby-1", Port = 25566 });
monitor.AddServer(new() { Host = "survival-1", Port = 25567 });
monitor.AddServer(new() { Host = "creative-1", Port = 25568 });
```

#### 2. **负载均衡**
```csharp
monitor.PlayerCountChanged += async (sender, e) =>
{
    var servers = monitor.GetMonitoredServers()
        .Where(s => s.IsOnline)
        .OrderBy(s => s.CurrentStatus?.Players.Online ?? 0)
        .ToList();
    
    // 将新玩家引导到人少的服务器
    if (servers.Any())
    {
        var leastBusyServer = servers.First();
        context.Logger.Info($"推荐服务器: {leastBusyServer.Address}");
    }
};
```

#### 3. **自动故障转移**
```csharp
monitor.ServerOffline += async (sender, e) =>
{
    context.Logger.Warning($"{e.Address} 下线，启动备用服务器...");
    // 通过 RCON 或其他方式启动备用服务器
};
```

#### 4. **状态页面生成**
```csharp
var servers = monitor.GetMonitoredServers();
var statusHtml = GenerateStatusPage(servers);
File.WriteAllText("status.html", statusHtml);
```

---

## 📚 相关文档

- [事件监听策略](./EVENT_PRIORITY_STRATEGY.md) - SMP/RCON/日志优先级
- [API 设计](./API_DESIGN.md) - 完整 API 接口
- [插件开发指南](./PLUGIN_GUIDE.md) - 如何开发插件
- [Minecraft Java 版网络协议](https://zh.minecraft.wiki/w/Java%E7%89%88%E7%BD%91%E7%BB%9C%E5%8D%8F%E8%AE%AE) - 官方协议文档

---

## 总结

NetherGate 提供了完整的**本地功能和网络查询能力**，让插件开发者可以：

### 本地功能
✅ 监听服务器文件变更  
✅ 安全地读写配置文件  
✅ 创建和管理备份  
✅ 监控服务器性能  
✅ 读取玩家和世界数据  

### 网络查询（新增）
✅ 查询任意 MC 服务器状态  
✅ 持续监控多个服务器  
✅ 实时获取玩家数和延迟  
✅ 支持群组服和服务器网络  

这些功能与 **SMP/RCON/日志解析** 相结合，为插件开发者提供了**史上最完整的 Minecraft 服务器管理和监控能力**！

