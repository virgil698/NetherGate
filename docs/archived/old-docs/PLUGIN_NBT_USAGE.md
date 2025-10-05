# 插件使用 NBT 读取器指南

NetherGate 为插件提供了强大的 NBT 数据读取功能，插件可以通过 `IPluginContext` 访问玩家数据和世界数据。

## 目录

- [访问 NBT 读取器](#访问-nbt-读取器)
- [读取玩家数据](#读取玩家数据)
- [读取世界数据](#读取世界数据)
- [实战示例](#实战示例)
- [最佳实践](#最佳实践)

---

## 访问 NBT 读取器

通过插件上下文，你可以访问两个核心的 NBT 读取器：

```csharp
public class MyPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;

        // 玩家数据读取器
        var playerDataReader = context.PlayerDataReader;

        // 世界数据读取器
        var worldDataReader = context.WorldDataReader;
    }
}
```

---

## 读取玩家数据

### 基本玩家信息

```csharp
// 读取玩家基本数据
var playerUuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null)
{
    _context.Logger.Info($"玩家: {playerData.Name}");
    _context.Logger.Info($"等级: {playerData.XpLevel}");
    _context.Logger.Info($"生命值: {playerData.Health}/20");
    _context.Logger.Info($"饱食度: {playerData.FoodLevel}/20");
    _context.Logger.Info($"游戏模式: {playerData.GameMode}");
    
    // 位置信息
    var pos = playerData.Position;
    _context.Logger.Info($"位置: ({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");
    _context.Logger.Info($"维度: {pos.Dimension}");
}
```

### 读取背包物品

```csharp
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null)
{
    _context.Logger.Info($"背包物品数量: {playerData.Inventory.Count}");
    
    foreach (var item in playerData.Inventory)
    {
        _context.Logger.Info($"槽位 {item.Slot}: {item.Id} x{item.Count}");
        
        // 检查附魔
        if (item.Enchantments.Count > 0)
        {
            _context.Logger.Info("  附魔:");
            foreach (var ench in item.Enchantments)
            {
                _context.Logger.Info($"    - {ench.Id} Lv.{ench.Level}");
            }
        }
        
        // 检查自定义名称
        if (!string.IsNullOrEmpty(item.CustomName))
        {
            _context.Logger.Info($"  自定义名称: {item.CustomName}");
        }
    }
}
```

### 读取装备

```csharp
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null)
{
    var armor = playerData.Armor;
    
    _context.Logger.Info("玩家装备:");
    _context.Logger.Info($"  头盔: {armor.Helmet?.Id ?? "无"}");
    _context.Logger.Info($"  胸甲: {armor.Chestplate?.Id ?? "无"}");
    _context.Logger.Info($"  护腿: {armor.Leggings?.Id ?? "无"}");
    _context.Logger.Info($"  靴子: {armor.Boots?.Id ?? "无"}");
    
    // 检查头盔附魔
    if (armor.Helmet?.Enchantments.Count > 0)
    {
        _context.Logger.Info("  头盔附魔:");
        foreach (var ench in armor.Helmet.Enchantments)
        {
            _context.Logger.Info($"    - {ench.Id} Lv.{ench.Level}");
        }
    }
}
```

### 读取末影箱

```csharp
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null)
{
    _context.Logger.Info($"末影箱物品数量: {playerData.EnderChest.Count}");
    
    foreach (var item in playerData.EnderChest)
    {
        _context.Logger.Info($"槽位 {item.Slot}: {item.Id} x{item.Count}");
    }
}
```

### 读取状态效果

```csharp
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null && playerData.Effects.Count > 0)
{
    _context.Logger.Info("玩家状态效果:");
    
    foreach (var effect in playerData.Effects)
    {
        _context.Logger.Info($"  {effect.Id} Lv.{effect.Amplifier} ({effect.Duration}秒)");
    }
}
```

### 读取玩家统计

```csharp
var stats = await _context.PlayerDataReader.ReadPlayerStatsAsync(playerUuid);

if (stats != null)
{
    _context.Logger.Info("玩家统计:");
    _context.Logger.Info($"  游玩时间: {stats.PlayTimeMinutes} 分钟");
    _context.Logger.Info($"  死亡次数: {stats.Deaths}");
    _context.Logger.Info($"  跳跃次数: {stats.Jumps}");
    _context.Logger.Info($"  行走距离: {stats.DistanceWalked:F2} 米");
    _context.Logger.Info($"  飞行距离: {stats.DistanceFlown:F2} 米");
    
    // 击杀统计
    if (stats.MobKills.Count > 0)
    {
        _context.Logger.Info("  击杀生物:");
        foreach (var (mob, count) in stats.MobKills)
        {
            _context.Logger.Info($"    {mob}: {count}");
        }
    }
    
    // 挖掘统计
    if (stats.BlocksMined.Count > 0)
    {
        _context.Logger.Info("  挖掘方块:");
        foreach (var (block, count) in stats.BlocksMined.OrderByDescending(x => x.Value).Take(5))
        {
            _context.Logger.Info($"    {block}: {count}");
        }
    }
}
```

### 读取玩家成就

```csharp
var advancements = await _context.PlayerDataReader.ReadPlayerAdvancementsAsync(playerUuid);

if (advancements != null)
{
    _context.Logger.Info($"成就完成度: {advancements.CompletionPercent:F1}%");
    _context.Logger.Info($"已完成: {advancements.Completed.Count} 个");
    _context.Logger.Info($"进行中: {advancements.InProgress.Count} 个");
    
    // 显示最近完成的成就
    var recentAdvancements = advancements.Completed
        .OrderByDescending(a => a.CompletedAt)
        .Take(5);
    
    _context.Logger.Info("最近完成的成就:");
    foreach (var adv in recentAdvancements)
    {
        _context.Logger.Info($"  {adv.Id} - {adv.CompletedAt:yyyy-MM-dd HH:mm:ss}");
    }
}
```

### 列出所有玩家

```csharp
// 列出主世界的所有玩家
var playerUuids = _context.PlayerDataReader.ListPlayers();

_context.Logger.Info($"找到 {playerUuids.Count} 个玩家:");

foreach (var uuid in playerUuids)
{
    var data = await _context.PlayerDataReader.ReadPlayerDataAsync(uuid);
    if (data != null)
    {
        _context.Logger.Info($"  {uuid}: Lv.{data.XpLevel}");
    }
}
```

---

## 读取世界数据

### 基本世界信息

```csharp
var worldInfo = await _context.WorldDataReader.ReadWorldInfoAsync();

if (worldInfo != null)
{
    _context.Logger.Info($"世界名称: {worldInfo.Name}");
    _context.Logger.Info($"世界种子: {worldInfo.Seed}");
    _context.Logger.Info($"版本: {worldInfo.Version}");
    _context.Logger.Info($"大小: {worldInfo.SizeMB} MB");
    _context.Logger.Info($"游戏时间: {worldInfo.GameTime} ticks");
    _context.Logger.Info($"游戏难度: {worldInfo.Difficulty}");
    _context.Logger.Info($"允许命令: {worldInfo.AllowCommands}");
    
    // 天气信息
    _context.Logger.Info($"下雨: {worldInfo.Raining}");
    _context.Logger.Info($"雷暴: {worldInfo.Thundering}");
}
```

### 出生点信息

```csharp
var spawnPoint = await _context.WorldDataReader.GetSpawnPointAsync("world");

if (spawnPoint != null)
{
    _context.Logger.Info($"出生点: ({spawnPoint.X}, {spawnPoint.Y}, {spawnPoint.Z})");
    _context.Logger.Info($"角度: {spawnPoint.Angle}");
}
```

### 世界边界

```csharp
var worldInfo = await _context.WorldDataReader.ReadWorldInfoAsync();

if (worldInfo != null)
{
    var border = worldInfo.WorldBorder;
    _context.Logger.Info("世界边界:");
    _context.Logger.Info($"  中心: ({border.CenterX}, {border.CenterZ})");
    _context.Logger.Info($"  大小: {border.Size} 方块");
    _context.Logger.Info($"  伤害: {border.DamagePerBlock}/方块");
}
```

### 游戏规则

```csharp
var worldInfo = await _context.WorldDataReader.ReadWorldInfoAsync();

if (worldInfo != null)
{
    _context.Logger.Info("游戏规则:");
    foreach (var (rule, value) in worldInfo.GameRules)
    {
        _context.Logger.Info($"  {rule}: {value}");
    }
}
```

### 列出所有世界

```csharp
var worlds = _context.WorldDataReader.ListWorlds();

_context.Logger.Info($"找到 {worlds.Count} 个世界:");
foreach (var world in worlds)
{
    _context.Logger.Info($"  - {world}");
    
    var size = _context.WorldDataReader.GetWorldSize(world);
    _context.Logger.Info($"    大小: {size} MB");
}
```

---

## 实战示例

### 示例 1：玩家统计命令插件

```csharp
using NetherGate.API.Plugins;
using NetherGate.API.Events;

namespace MyPlugin;

public class PlayerStatsPlugin : IPlugin
{
    private IPluginContext _context = null!;

    public void OnLoad(IPluginContext context)
    {
        _context = context;
    }

    public void OnEnable(IPluginContext context)
    {
        // 注册命令
        _context.CommandManager.RegisterCommand(new PlayerStatsCommand(_context));
        
        _context.Logger.Info("玩家统计插件已启用");
    }

    public void OnDisable()
    {
        _context.Logger.Info("玩家统计插件已禁用");
    }
}

public class PlayerStatsCommand : ICommand
{
    private readonly IPluginContext _context;

    public string Name => "pstats";
    public string Description => "查看玩家统计信息";
    public string Usage => "pstats <player-uuid>";
    public List<string> Aliases => new() { "playerstats" };
    public string PluginId => "player-stats";
    public string? Permission => "playerstats.view";

    public PlayerStatsCommand(IPluginContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Fail("用法: pstats <player-uuid>");
        }

        if (!Guid.TryParse(args[0], out var playerUuid))
        {
            return CommandResult.Fail("无效的玩家 UUID");
        }

        try
        {
            // 读取玩家数据
            var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);
            if (playerData == null)
            {
                return CommandResult.Fail("找不到玩家数据");
            }

            // 读取统计数据
            var stats = await _context.PlayerDataReader.ReadPlayerStatsAsync(playerUuid);
            
            // 格式化输出
            var message = $@"
玩家统计 - {playerData.Name ?? playerUuid.ToString()}
========================================
等级: {playerData.XpLevel} | 总经验: {playerData.XpTotal}
生命值: {playerData.Health}/20 | 饱食度: {playerData.FoodLevel}/20
位置: ({playerData.Position.X:F2}, {playerData.Position.Y:F2}, {playerData.Position.Z:F2})
维度: {playerData.Position.Dimension}

背包物品: {playerData.Inventory.Count}
末影箱: {playerData.EnderChest.Count}
状态效果: {playerData.Effects.Count}
";

            if (stats != null)
            {
                message += $@"
统计数据:
  游玩时间: {stats.PlayTimeMinutes} 分钟 ({stats.PlayTimeMinutes / 60.0:F1} 小时)
  死亡次数: {stats.Deaths}
  跳跃次数: {stats.Jumps:N0}
  行走距离: {stats.DistanceWalked:F2} 米 ({stats.DistanceWalked / 1000:F2} km)
  飞行距离: {stats.DistanceFlown:F2} 米 ({stats.DistanceFlown / 1000:F2} km)
  击杀生物: {stats.MobKills.Values.Sum()}
  挖掘方块: {stats.BlocksMined.Values.Sum():N0}
";
            }

            sender.SendMessage(message);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            _context.Logger.Error($"读取玩家数据失败", ex);
            return CommandResult.Fail($"读取失败: {ex.Message}");
        }
    }
}
```

### 示例 2：背包检查插件

```csharp
public class InventoryCheckerPlugin : IPlugin
{
    private IPluginContext _context = null!;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        
        // 订阅玩家加入事件（示例）
        _context.EventBus.Subscribe<PlayerJoinEvent>(OnPlayerJoin);
    }

    private async void OnPlayerJoin(PlayerJoinEvent e)
    {
        try
        {
            // 读取玩家数据
            var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(e.PlayerUuid);
            
            if (playerData == null) return;

            // 检查是否有非法物品
            var illegalItems = playerData.Inventory
                .Where(item => IsIllegalItem(item.Id))
                .ToList();

            if (illegalItems.Count > 0)
            {
                _context.Logger.Warning($"玩家 {playerData.Name} 背包中有 {illegalItems.Count} 个非法物品");
                
                foreach (var item in illegalItems)
                {
                    _context.Logger.Warning($"  - {item.Id} x{item.Count} (槽位 {item.Slot})");
                }
                
                // 可以通过 RCON 踢出玩家或清除物品
                // await _context.RconClient?.SendCommandAsync($"kick {playerData.Name} 检测到非法物品");
            }

            // 检查是否有超级附魔
            var superEnchantedItems = playerData.Inventory
                .Where(item => item.Enchantments.Any(e => e.Level > 10))
                .ToList();

            if (superEnchantedItems.Count > 0)
            {
                _context.Logger.Warning($"玩家 {playerData.Name} 有超级附魔物品");
            }
        }
        catch (Exception ex)
        {
            _context.Logger.Error("检查背包失败", ex);
        }
    }

    private bool IsIllegalItem(string itemId)
    {
        // 定义非法物品列表
        var illegalItems = new[]
        {
            "minecraft:bedrock",
            "minecraft:barrier",
            "minecraft:command_block"
        };

        return illegalItems.Contains(itemId);
    }

    public void OnDisable()
    {
        // 清理
    }
}
```

### 示例 3：世界信息面板插件

```csharp
public class WorldInfoPlugin : IPlugin
{
    private IPluginContext _context = null!;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        _context.CommandManager.RegisterCommand(new WorldInfoCommand(_context));
    }

    public void OnDisable() { }
}

public class WorldInfoCommand : ICommand
{
    private readonly IPluginContext _context;

    public string Name => "worldinfo";
    public string Description => "显示世界信息";
    public string Usage => "worldinfo [world-name]";
    public List<string> Aliases => new() { "winfo" };
    public string PluginId => "world-info";
    public string? Permission => null;

    public WorldInfoCommand(IPluginContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        var worldName = args.Length > 0 ? args[0] : "world";

        try
        {
            var worldInfo = await _context.WorldDataReader.ReadWorldInfoAsync(worldName);
            
            if (worldInfo == null)
            {
                return CommandResult.Fail($"找不到世界: {worldName}");
            }

            var dayTime = worldInfo.DayTime % 24000;
            var timeOfDay = dayTime switch
            {
                >= 0 and < 6000 => "早晨",
                >= 6000 and < 12000 => "白天",
                >= 12000 and < 18000 => "傍晚",
                _ => "夜晚"
            };

            var message = $@"
世界信息 - {worldInfo.Name}
========================================
种子: {worldInfo.Seed}
版本: {worldInfo.Version}
类型: {worldInfo.LevelType}
大小: {worldInfo.SizeMB} MB

游戏时间: {worldInfo.GameTime} ticks ({timeOfDay})
难度: {worldInfo.Difficulty}
允许命令: {worldInfo.AllowCommands}

天气: {(worldInfo.Raining ? "下雨" : "晴朗")} {(worldInfo.Thundering ? "⚡雷暴" : "")}

出生点: ({worldInfo.SpawnPoint.X}, {worldInfo.SpawnPoint.Y}, {worldInfo.SpawnPoint.Z})
世界边界: {worldInfo.WorldBorder.Size} 方块

已启用的数据包 ({worldInfo.EnabledDataPacks.Count}):
{string.Join("\n", worldInfo.EnabledDataPacks.Select(dp => $"  - {dp}"))}
";

            sender.SendMessage(message);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            _context.Logger.Error("读取世界信息失败", ex);
            return CommandResult.Fail($"读取失败: {ex.Message}");
        }
    }
}
```

---

## 最佳实践

### 1. 异常处理

始终使用 try-catch 处理 NBT 读取操作：

```csharp
try
{
    var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);
    // 处理数据
}
catch (Exception ex)
{
    _context.Logger.Error($"读取玩家数据失败: {playerUuid}", ex);
}
```

### 2. 检查 null 值

NBT 读取器在文件不存在时返回 `null`：

```csharp
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData == null)
{
    _context.Logger.Warning($"玩家数据不存在: {playerUuid}");
    return;
}

// 安全地使用 playerData
```

### 3. 异步操作

NBT 读取是异步操作，避免阻塞：

```csharp
// ❌ 不要这样做
public void OnSomeEvent(SomeEvent e)
{
    var playerData = _context.PlayerDataReader.ReadPlayerDataAsync(e.PlayerUuid).Result; // 阻塞!
}

// ✅ 应该这样做
public async void OnSomeEvent(SomeEvent e)
{
    var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(e.PlayerUuid);
}
```

### 4. 缓存数据

对于频繁访问的数据，考虑缓存：

```csharp
private readonly Dictionary<Guid, PlayerData> _playerCache = new();
private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

public async Task<PlayerData?> GetPlayerDataAsync(Guid playerUuid)
{
    // 检查缓存
    if (_playerCache.TryGetValue(playerUuid, out var cached))
    {
        return cached;
    }

    // 从文件读取
    var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);
    
    if (playerData != null)
    {
        _playerCache[playerUuid] = playerData;
    }

    return playerData;
}
```

### 5. 使用日志

记录重要操作以便调试：

```csharp
_context.Logger.Debug($"正在读取玩家数据: {playerUuid}");
var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);

if (playerData != null)
{
    _context.Logger.Debug($"成功读取玩家 {playerData.Name} 的数据");
}
else
{
    _context.Logger.Warning($"玩家数据不存在: {playerUuid}");
}
```

### 6. 性能考虑

- NBT 文件读取是 I/O 密集型操作
- 避免在主循环中频繁读取
- 考虑使用后台任务处理大量数据

```csharp
// 批量处理玩家数据
public async Task ProcessAllPlayersAsync()
{
    var playerUuids = _context.PlayerDataReader.ListPlayers();
    
    _context.Logger.Info($"开始处理 {playerUuids.Count} 个玩家");

    // 并发处理（注意控制并发数）
    var tasks = playerUuids.Select(async uuid =>
    {
        try
        {
            var data = await _context.PlayerDataReader.ReadPlayerDataAsync(uuid);
            // 处理数据...
        }
        catch (Exception ex)
        {
            _context.Logger.Error($"处理玩家失败: {uuid}", ex);
        }
    });

    await Task.WhenAll(tasks);
    
    _context.Logger.Info("所有玩家处理完成");
}
```

---

## 常见问题

### Q: 如何获取玩家的 UUID？

A: 可以通过事件、RCON 或 SMP API 获取：

```csharp
// 方式 1: 从事件获取
_context.EventBus.Subscribe<PlayerJoinEvent>(e =>
{
    var uuid = e.PlayerUuid;
});

// 方式 2: 列出所有玩家
var allPlayers = _context.PlayerDataReader.ListPlayers();
```

### Q: 读取数据会影响服务器性能吗？

A: NBT 读取是异步且独立的操作，不会直接影响服务器性能。但大量并发读取可能影响磁盘 I/O。

### Q: 数据是实时的吗？

A: NBT 文件在服务器保存数据时更新（如玩家退出、定时保存等）。要获取实时数据，需要使用 SMP 或 RCON。

### Q: 可以修改 NBT 数据吗？

A: 当前版本的 NBT 读取器仅支持读取。修改功能将在未来版本中添加。

---

## 相关文档

- [插件开发指南](PLUGIN_PROJECT_STRUCTURE.md)
- [API 设计文档](API_DESIGN.md)
- [事件系统](EVENT_PRIORITY_STRATEGY.md)
- [命令系统](COMMAND_SYSTEM.md)
