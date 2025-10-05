# NBT 数据操作

NBT（Named Binary Tag）是 Minecraft 用于存储游戏数据的格式。NetherGate 提供了完整的 NBT 读写功能，允许插件直接访问和修改玩家、世界等数据。

---

## 📋 **目录**

- [什么是 NBT](#什么是-nbt)
- [读取 NBT 数据](#读取-nbt-数据)
- [写入 NBT 数据](#写入-nbt-数据)
- [安全性考虑](#安全性考虑)
- [最佳实践](#最佳实践)
- [常见场景](#常见场景)

---

## 🏷️ **什么是 NBT**

NBT（Named Binary Tag）是 Minecraft 使用的数据存储格式，类似于 JSON 但是二进制的。

### **NBT 存储的数据**

| 数据类型 | 文件位置 | 内容 |
|---------|---------|------|
| 玩家数据 | `world/playerdata/*.dat` | 位置、生命值、背包、经验等 |
| 世界数据 | `world/level.dat` | 种子、出生点、游戏规则等 |
| 区块数据 | `world/region/*.mca` | 方块、实体、TileEntity |
| 结构数据 | `world/structures/*.nbt` | 建筑结构 |

### **常见 NBT 标签**

- **Byte** - 字节（-128 到 127）
- **Short** - 短整型
- **Int** - 整型
- **Long** - 长整型
- **Float** - 浮点数
- **Double** - 双精度浮点数
- **String** - 字符串
- **List** - 列表
- **Compound** - 复合标签（键值对集合）

---

## 📖 **读取 NBT 数据**

### **IPlayerDataReader 接口**

```csharp
namespace NetherGate.API.Data
{
    /// <summary>
    /// 玩家数据读取接口
    /// 读取 Minecraft 玩家数据文件（NBT 格式）
    /// </summary>
    public interface IPlayerDataReader
    {
        /// <summary>
        /// 读取玩家数据
        /// </summary>
        /// <param name="playerUuid">玩家 UUID</param>
        Task<PlayerData?> ReadPlayerDataAsync(Guid playerUuid);

        /// <summary>
        /// 读取玩家统计数据
        /// </summary>
        /// <param name="playerUuid">玩家 UUID</param>
        Task<PlayerStats?> ReadPlayerStatsAsync(Guid playerUuid);

        /// <summary>
        /// 读取玩家成就进度
        /// </summary>
        /// <param name="playerUuid">玩家 UUID</param>
        Task<PlayerAdvancements?> ReadPlayerAdvancementsAsync(Guid playerUuid);

        /// <summary>
        /// 列出所有玩家
        /// </summary>
        /// <param name="worldName">世界名称（默认为主世界）</param>
        List<Guid> ListPlayers(string? worldName = null);

        /// <summary>
        /// 获取在线玩家数据
        /// </summary>
        List<PlayerData> GetOnlinePlayers();

        /// <summary>
        /// 检查玩家数据是否存在
        /// </summary>
        /// <param name="playerUuid">玩家 UUID</param>
        bool PlayerDataExists(Guid playerUuid);
    }
}
```

### **读取玩家数据**

```csharp
using NetherGate.API.Plugins;
using NetherGate.API.Data;

public class NbtReaderPlugin : PluginBase
{
    public async Task ReadPlayerInfoAsync(Guid playerUuid)
    {
        // 使用 PlayerDataReader 属性（由 PluginBase 提供）
        var playerData = await PlayerDataReader.ReadPlayerDataAsync(playerUuid);
        
        if (playerData == null)
        {
            Logger.Warning($"玩家数据未找到: {playerUuid}");
            return;
        }
        
        // 基本信息
        Logger.Info($"玩家: {playerData.Name}");
        Logger.Info($"UUID: {playerData.Uuid}");
        
        // 位置信息
        var pos = playerData.Position;
        Logger.Info($"位置: X={pos.X:F2}, Y={pos.Y:F2}, Z={pos.Z:F2}");
        Logger.Info($"维度: {pos.Dimension}");
        Logger.Info($"朝向: Yaw={pos.Yaw:F2}, Pitch={pos.Pitch:F2}");
        
        // 生命值和饥饿度
        Logger.Info($"生命值: {playerData.Health:F1}/20");
        Logger.Info($"饥饿值: {playerData.FoodLevel}/20");
        
        // 经验
        Logger.Info($"经验等级: {playerData.XpLevel}");
        Logger.Info($"总经验: {playerData.XpTotal}");
        
        // 游戏模式
        Logger.Info($"游戏模式: {playerData.GameMode}");
        
        // 在线状态
        Logger.Info($"在线状态: {(playerData.IsOnline ? "在线" : "离线")}");
        
        if (playerData.LastPlayed.HasValue)
        {
            Logger.Info($"最后上线: {playerData.LastPlayed.Value:yyyy-MM-dd HH:mm:ss}");
        }
    }
}
```

### **PlayerData 结构**

```csharp
namespace NetherGate.API.Data
{
    /// <summary>
    /// 玩家数据
    /// </summary>
    public class PlayerData
    {
        // 基本信息
        public Guid Uuid { get; init; }
        public string Name { get; init; } = string.Empty;
        
        // 位置
        public PlayerPosition Position { get; init; } = new();
        
        // 生命和饥饿
        public float Health { get; init; }
        public int FoodLevel { get; init; }
        
        // 经验
        public int XpLevel { get; init; }
        public int XpTotal { get; init; }
        
        // 游戏模式
        public GameMode GameMode { get; init; }
        
        // 背包（0-35：主背包，100-103：装备）
        public List<ItemStack> Inventory { get; init; } = new();
        
        // 末影箱（0-26）
        public List<ItemStack> EnderChest { get; init; } = new();
        
        // 装备
        public PlayerArmor Armor { get; init; } = new();
        
        // 效果
        public List<StatusEffect> Effects { get; init; } = new();
        
        // 时间
        public DateTime? LastPlayed { get; init; }
        public bool IsOnline { get; init; }
    }
    
    /// <summary>
    /// 玩家位置
    /// </summary>
    public class PlayerPosition
    {
        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
        public string Dimension { get; init; } = "minecraft:overworld";
        public float Yaw { get; init; }
        public float Pitch { get; init; }
    }
    
    /// <summary>
    /// 物品堆
    /// </summary>
    public class ItemStack
    {
        public string Id { get; init; } = string.Empty;
        public int Count { get; init; }
        public int Slot { get; init; }
        public List<Enchantment> Enchantments { get; init; } = new();
        public string? CustomName { get; init; }
    }
    
    /// <summary>
    /// 附魔
    /// </summary>
    public class Enchantment
    {
        public string Id { get; init; } = string.Empty;
        public int Level { get; init; }
    }
    
    /// <summary>
    /// 玩家护甲
    /// </summary>
    public class PlayerArmor
    {
        public ItemStack? Helmet { get; init; }
        public ItemStack? Chestplate { get; init; }
        public ItemStack? Leggings { get; init; }
        public ItemStack? Boots { get; init; }
    }
    
    /// <summary>
    /// 状态效果
    /// </summary>
    public class StatusEffect
    {
        public string Id { get; init; } = string.Empty;
        public int Amplifier { get; init; }
        public int Duration { get; init; }  // 剩余时间（秒）
    }
    
    /// <summary>
    /// 游戏模式
    /// </summary>
    public enum GameMode
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }
}
```

### **读取背包**

```csharp
public async Task ShowInventoryAsync(Guid playerUuid)
{
    // 读取玩家数据（包含背包）
    var playerData = await PlayerDataReader.ReadPlayerDataAsync(playerUuid);
    
    if (playerData == null)
    {
        Logger.Warning($"玩家数据未找到: {playerUuid}");
        return;
    }
    
    Logger.Info($"=== {playerData.Name} 的背包 ===");
    
    // 背包物品
    foreach (var item in playerData.Inventory)
    {
        var enchInfo = item.Enchantments.Count > 0 
            ? $" (附魔: {string.Join(", ", item.Enchantments.Select(e => $"{e.Id} Lv.{e.Level}"))})"
            : "";
        
        var nameInfo = !string.IsNullOrEmpty(item.CustomName) 
            ? $" [{item.CustomName}]" 
            : "";
        
        Logger.Info($"槽位 {item.Slot}: {item.Id} x{item.Count}{nameInfo}{enchInfo}");
    }
    
    // 末影箱
    if (playerData.EnderChest.Count > 0)
    {
        Logger.Info($"\n=== 末影箱（{playerData.EnderChest.Count} 件物品）===");
        foreach (var item in playerData.EnderChest)
        {
            Logger.Info($"槽位 {item.Slot}: {item.Id} x{item.Count}");
        }
    }
    
    // 装备
    Logger.Info("\n=== 装备 ===");
    var armor = playerData.Armor;
    if (armor.Helmet != null) Logger.Info($"头盔: {armor.Helmet.Id}");
    if (armor.Chestplate != null) Logger.Info($"胸甲: {armor.Chestplate.Id}");
    if (armor.Leggings != null) Logger.Info($"护腿: {armor.Leggings.Id}");
    if (armor.Boots != null) Logger.Info($"靴子: {armor.Boots.Id}");
}
```

### **读取世界数据**

```csharp
public async Task ReadWorldInfoAsync(string? worldName = null)
{
    // 读取世界信息（默认主世界）
    var worldInfo = await WorldDataReader.ReadWorldInfoAsync(worldName);
    
    if (worldInfo == null)
    {
        Logger.Warning($"世界数据未找到: {worldName ?? "world"}");
        return;
    }
    
    Logger.Info($"=== 世界信息: {worldInfo.Name} ===");
    
    // 基本信息
    Logger.Info($"种子: {worldInfo.Seed}");
    Logger.Info($"版本: {worldInfo.Version}");
    Logger.Info($"世界类型: {worldInfo.LevelType}");
    Logger.Info($"难度: {worldInfo.Difficulty}");
    Logger.Info($"世界大小: {worldInfo.SizeMB} MB");
    
    // 出生点
    var spawn = worldInfo.SpawnPoint;
    Logger.Info($"出生点: X={spawn.X}, Y={spawn.Y}, Z={spawn.Z}, 角度={spawn.Angle}");
    
    // 时间
    Logger.Info($"游戏时间: {worldInfo.GameTime} 刻");
    Logger.Info($"日期时间: {worldInfo.DayTime} 刻");
    
    // 天气
    Logger.Info($"天气: {(worldInfo.Raining ? "下雨" : "晴天")}, {(worldInfo.Thundering ? "打雷" : "无雷")}");
    
    // 世界边界
    var border = worldInfo.WorldBorder;
    Logger.Info($"世界边界: 中心({border.CenterX}, {border.CenterZ}), 大小={border.Size}");
    
    // 游戏规则（显示部分重要规则）
    Logger.Info("\n=== 游戏规则 ===");
    var importantRules = new[] { 
        "doDaylightCycle", "doMobSpawning", "keepInventory",
        "mobGriefing", "doFireTick", "naturalRegeneration" 
    };
    
    foreach (var ruleName in importantRules)
    {
        if (worldInfo.GameRules.TryGetValue(ruleName, out var value))
        {
            Logger.Info($"  {ruleName}: {value}");
        }
    }
    
    // 数据包
    if (worldInfo.EnabledDataPacks.Count > 0)
    {
        Logger.Info($"\n启用的数据包: {string.Join(", ", worldInfo.EnabledDataPacks)}");
    }
}
```

---

## ✏️ **写入 NBT 数据**

### **⚠️ 重要警告**

NBT 数据写入是**高风险操作**：

- ⚠️ **数据损坏：** 错误的写入可能损坏存档
- ⚠️ **服务器崩溃：** 无效数据可能导致崩溃
- ⚠️ **玩家数据丢失：** 错误操作可能导致玩家数据丢失

**安全建议：**
1. ✅ 始终在**离线状态**下修改玩家数据
2. ✅ 修改前自动**备份**
3. ✅ 使用**验证**确保数据正确
4. ✅ 在**测试服务器**上先测试

### **INbtDataWriter 接口**

```csharp
using fNbt;

namespace NetherGate.API.Data
{
    public interface INbtDataWriter
    {
        // ========== 玩家数据写入 ==========
        
        /// <summary>
        /// 更新玩家位置
        /// </summary>
        Task UpdatePlayerPositionAsync(Guid playerUuid, double x, double y, double z, string? dimension = null);
        
        /// <summary>
        /// 更新玩家生命值
        /// </summary>
        Task UpdatePlayerHealthAsync(Guid playerUuid, float health);
        
        /// <summary>
        /// 更新玩家饱食度
        /// </summary>
        Task UpdatePlayerFoodLevelAsync(Guid playerUuid, int foodLevel, float saturation);
        
        /// <summary>
        /// 更新玩家经验
        /// </summary>
        Task UpdatePlayerExperienceAsync(Guid playerUuid, int level, float progress, int total);
        
        /// <summary>
        /// 更新玩家游戏模式
        /// </summary>
        Task UpdatePlayerGameModeAsync(Guid playerUuid, GameMode gameMode);
        
        // ========== 背包操作 ==========
        
        /// <summary>
        /// 添加物品到玩家背包
        /// </summary>
        Task AddItemToInventoryAsync(Guid playerUuid, ItemStack item, int slot = -1);
        
        /// <summary>
        /// 从玩家背包移除物品
        /// </summary>
        Task RemoveItemFromInventoryAsync(Guid playerUuid, int slot);
        
        /// <summary>
        /// 更新玩家背包物品
        /// </summary>
        Task UpdateInventoryItemAsync(Guid playerUuid, int slot, ItemStack item);
        
        /// <summary>
        /// 清空玩家背包
        /// </summary>
        Task ClearPlayerInventoryAsync(Guid playerUuid);
        
        // ========== 状态效果 ==========
        
        /// <summary>
        /// 给予玩家状态效果
        /// </summary>
        Task AddStatusEffectAsync(Guid playerUuid, StatusEffect effect);
        
        /// <summary>
        /// 移除玩家状态效果
        /// </summary>
        Task RemoveStatusEffectAsync(Guid playerUuid, int effectId);
        
        /// <summary>
        /// 更新玩家盔甲
        /// </summary>
        Task UpdatePlayerArmorAsync(Guid playerUuid, PlayerArmor armor);
        
        // ========== 世界数据写入 ==========
        
        /// <summary>
        /// 更新世界出生点
        /// </summary>
        Task UpdateWorldSpawnAsync(string worldName, int x, int y, int z);
        
        /// <summary>
        /// 更新世界边界
        /// </summary>
        Task UpdateWorldBorderAsync(string worldName, double centerX, double centerZ, double size, double damagePerBlock, double warningDistance);
        
        /// <summary>
        /// 更新游戏规则
        /// </summary>
        Task UpdateGameRuleAsync(string worldName, string ruleName, string value);
        
        /// <summary>
        /// 更新世界时间
        /// </summary>
        Task UpdateWorldTimeAsync(string worldName, long dayTime, long gameTime);
        
        /// <summary>
        /// 更新世界天气
        /// </summary>
        Task UpdateWorldWeatherAsync(string worldName, bool raining, int rainTime, bool thundering, int thunderTime);
        
        // ========== 高级 NBT 操作 ==========
        
        /// <summary>
        /// 直接修改玩家 NBT 数据
        /// </summary>
        Task ModifyPlayerNbtAsync(Guid playerUuid, Action<NbtCompound> nbtModifier);
        
        /// <summary>
        /// 直接修改世界 NBT 数据
        /// </summary>
        Task ModifyWorldNbtAsync(string worldName, Action<NbtCompound> nbtModifier);
        
        /// <summary>
        /// 创建实体 NBT
        /// </summary>
        NbtCompound CreateEntityNbt(string entityType, double x, double y, double z, NbtCompound? customNbt = null);
        
        /// <summary>
        /// 创建物品 NBT
        /// </summary>
        NbtCompound CreateItemNbt(string itemId, int count = 1, List<Enchantment>? enchantments = null, string? customName = null, List<string>? lore = null, NbtCompound? customNbt = null);
        
        // ========== 通用 NBT 操作 ==========
        
        /// <summary>
        /// 从文件读取 NBT 数据
        /// </summary>
        Task<NbtCompound?> ReadNbtFileAsync(string filePath);
        
        /// <summary>
        /// 写入 NBT 数据到文件
        /// </summary>
        Task WriteNbtFileAsync(string filePath, NbtCompound nbt, bool backup = true);
        
        /// <summary>
        /// 验证 NBT 数据结构
        /// </summary>
        bool ValidateNbt(NbtCompound nbt, string expectedRootTag);
    }
}
```

### **修改玩家数据**

```csharp
using NetherGate.API.Plugins;
using NetherGate.API.Data;

public class NbtWriterPlugin : PluginBase
{
    public async Task HealPlayerAsync(Guid playerUuid)
    {
        try
        {
            // 确保玩家离线（重要！）
            var playerData = await PlayerDataReader.ReadPlayerDataAsync(playerUuid);
            if (playerData == null)
            {
                Logger.Warning($"玩家数据未找到: {playerUuid}");
                return;
            }
            
            if (playerData.IsOnline)
            {
                Logger.Warning($"{playerData.Name} 在线，无法修改数据！请等待玩家离线。");
                return;
            }
            
            // 修改生命值（满血）
            await NbtDataWriter.UpdatePlayerHealthAsync(playerUuid, 20.0f);
            
            // 修改饥饿值（满食）
            await NbtDataWriter.UpdatePlayerFoodLevelAsync(playerUuid, 20, 5.0f);
            
            Logger.Info($"已恢复 {playerData.Name} 的生命值和饥饿值");
        }
        catch (Exception ex)
        {
            Logger.Error($"修改玩家数据失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 传送玩家到指定位置
    /// </summary>
    public async Task TeleportPlayerAsync(Guid playerUuid, double x, double y, double z, string dimension = "minecraft:overworld")
    {
        try
        {
            await NbtDataWriter.UpdatePlayerPositionAsync(playerUuid, x, y, z, dimension);
            Logger.Info($"已将玩家传送到 ({x}, {y}, {z}) 维度: {dimension}");
        }
        catch (Exception ex)
        {
            Logger.Error($"传送失败: {ex.Message}");
        }
    }
}
```

### **修改背包**

```csharp
public async Task GiveRewardAsync(Guid playerUuid)
{
    // 创建附魔钻石剑
    var diamondSword = new ItemStack
    {
        Id = "minecraft:diamond_sword",
        Count = 1,
        Slot = -1, // -1 表示自动寻找空位
        Enchantments = new List<Enchantment>
        {
            new Enchantment { Id = "minecraft:sharpness", Level = 5 },
            new Enchantment { Id = "minecraft:unbreaking", Level = 3 },
            new Enchantment { Id = "minecraft:fire_aspect", Level = 2 }
        },
        CustomName = "§6§l传奇之剑"
    };
    
    try
    {
        // 确保玩家离线
        var playerData = await PlayerDataReader.ReadPlayerDataAsync(playerUuid);
        if (playerData?.IsOnline == true)
        {
            Logger.Warning("玩家在线，无法修改背包");
            return;
        }
        
        // 添加物品到背包
        await NbtDataWriter.AddItemToInventoryAsync(playerUuid, diamondSword);
        Logger.Info($"已给予玩家传奇之剑");
    }
    catch (Exception ex)
    {
        Logger.Error($"添加物品失败: {ex.Message}");
    }
}

/// <summary>
/// 清空玩家背包
/// </summary>
public async Task ClearPlayerInventoryAsync(Guid playerUuid)
{
    try
    {
        await NbtDataWriter.ClearPlayerInventoryAsync(playerUuid);
        Logger.Info("已清空玩家背包");
    }
    catch (Exception ex)
    {
        Logger.Error($"清空背包失败: {ex.Message}");
    }
}
```

### **自动备份**

```csharp
public class NbtDataWriter : INbtDataWriter
{
    private readonly string _backupDirectory;

    public async Task UpdatePlayerHealthAsync(string playerName, float health)
    {
        var playerFile = GetPlayerDataPath(playerName);
        
        // 自动备份
        await BackupFileAsync(playerFile);
        
        try
        {
            // 读取 NBT
            var nbt = await ReadNbtFileAsync(playerFile);
            
            // 修改数据
            nbt["Health"] = new NbtFloat(health);
            
            // 验证
            if (!ValidatePlayerNbt(nbt))
            {
                throw new InvalidDataException("NBT 数据验证失败");
            }
            
            // 写入
            await WriteNbtFileAsync(playerFile, nbt);
            
            _logger.Info($"已更新 {playerName} 的生命值为 {health}");
        }
        catch (Exception ex)
        {
            // 恢复备份
            await RestoreBackupAsync(playerFile);
            _logger.Error($"修改失败，已恢复备份: {ex.Message}");
            throw;
        }
    }

    private async Task BackupFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return;
        
        var backupPath = Path.Combine(
            _backupDirectory,
            $"{Path.GetFileName(filePath)}.backup.{DateTime.Now:yyyyMMdd_HHmmss}"
        );
        
        await Task.Run(() => File.Copy(filePath, backupPath, overwrite: true));
        _logger.Debug($"已备份: {backupPath}");
    }

    private async Task RestoreBackupAsync(string filePath)
    {
        var backupPath = Directory.GetFiles(_backupDirectory, 
            $"{Path.GetFileName(filePath)}.backup.*")
            .OrderByDescending(f => f)
            .FirstOrDefault();
        
        if (backupPath != null && File.Exists(backupPath))
        {
            await Task.Run(() => File.Copy(backupPath, filePath, overwrite: true));
            _logger.Info($"已恢复备份: {backupPath}");
        }
    }
}
```

---

## 🔒 **安全性考虑**

### **1. 玩家必须离线**

```csharp
public async Task<bool> CanModifyPlayerDataAsync(string playerName)
{
    // 检查玩家是否在线
    var onlinePlayers = await _context.SmpApi.GetOnlinePlayersAsync();
    
    if (onlinePlayers.Any(p => p.Name == playerName))
    {
        _context.Logger.Warning($"无法修改在线玩家 {playerName} 的数据");
        return false;
    }
    
    return true;
}

public async Task SafeModifyPlayerDataAsync(string playerName)
{
    if (!await CanModifyPlayerDataAsync(playerName))
    {
        throw new InvalidOperationException("玩家在线，无法修改数据");
    }
    
    // 执行修改...
}
```

### **2. 数据验证**

```csharp
public bool ValidatePlayerNbt(NbtCompound nbt)
{
    // 必需字段
    if (!nbt.Contains("Health") || !nbt.Contains("foodLevel"))
    {
        return false;
    }
    
    // 数值范围
    var health = nbt.Get<NbtFloat>("Health")?.Value ?? 0;
    if (health < 0 || health > 1024) // 允许超过 20
    {
        return false;
    }
    
    var foodLevel = nbt.Get<NbtInt>("foodLevel")?.Value ?? 0;
    if (foodLevel < 0 || foodLevel > 20)
    {
        return false;
    }
    
    // 背包验证
    if (nbt.Contains("Inventory"))
    {
        var inventory = nbt.Get<NbtList>("Inventory");
        if (inventory != null && inventory.Count > 41) // 36背包 + 4盔甲 + 1副手
        {
            return false;
        }
    }
    
    return true;
}
```

### **3. 事务性操作**

```csharp
public async Task TransactionalUpdateAsync(string playerName, Action<NbtCompound> modifier)
{
    var playerFile = GetPlayerDataPath(playerName);
    var backupPath = await BackupFileAsync(playerFile);
    
    try
    {
        // 读取
        var nbt = await ReadNbtFileAsync(playerFile);
        
        // 修改
        modifier(nbt);
        
        // 验证
        if (!ValidatePlayerNbt(nbt))
        {
            throw new InvalidDataException("数据验证失败");
        }
        
        // 写入
        await WriteNbtFileAsync(playerFile, nbt);
        
        _logger.Info("数据修改成功");
    }
    catch (Exception ex)
    {
        // 回滚
        if (File.Exists(backupPath))
        {
            File.Copy(backupPath, playerFile, overwrite: true);
            _logger.Warning($"操作失败，已回滚: {ex.Message}");
        }
        throw;
    }
    finally
    {
        // 清理备份（可选）
        // File.Delete(backupPath);
    }
}
```

---

## 💡 **最佳实践**

### **1. 使用游戏 API 优先**

```csharp
// ❌ 不推荐：直接修改 NBT
await _context.NbtDataWriter.UpdatePlayerHealthAsync("Steve", 20.0f);

// ✅ 推荐：使用游戏 API
await _context.GameDisplayApi.GiveEffect("Steve", "instant_health", 1, 255);
```

**原因：** 游戏 API 更安全、无需玩家离线。

### **2. 批量操作**

```csharp
public async Task BatchHealPlayersAsync(List<string> playerNames)
{
    foreach (var playerName in playerNames)
    {
        if (!await CanModifyPlayerDataAsync(playerName))
        {
            continue;
        }
        
        await _context.NbtDataWriter.UpdatePlayerHealthAsync(playerName, 20.0f);
        await _context.NbtDataWriter.UpdatePlayerHungerAsync(playerName, 20);
    }
}
```

### **3. 记录所有修改**

```csharp
public class AuditedNbtWriter
{
    public async Task UpdatePlayerHealthAsync(string playerName, float health)
    {
        // 记录修改
        _logger.Info($"[NBT修改] 玩家: {playerName}, 操作: 修改生命值, 新值: {health}");
        
        // 写入审计日志
        await LogAuditAsync(new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            PlayerName = playerName,
            Operation = "UpdateHealth",
            OldValue = await GetCurrentHealthAsync(playerName),
            NewValue = health
        });
        
        // 执行修改
        await _nbtWriter.UpdatePlayerHealthAsync(playerName, health);
    }
}
```

---

## 📚 **常见场景**

### **场景1：离线奖励系统**

```csharp
public async Task GiveOfflineRewardAsync(string playerName, Reward reward)
{
    if (!await CanModifyPlayerDataAsync(playerName))
    {
        // 记录到待发放队列，等玩家上线
        _pendingRewards.Add(playerName, reward);
        return;
    }
    
    // 添加物品
    foreach (var item in reward.Items)
    {
        await _context.NbtDataWriter.AddItemToInventoryAsync(playerName, item);
    }
    
    // 添加经验
    var playerData = await _context.PlayerDataReader.ReadPlayerDataAsync(playerName);
    if (playerData != null)
    {
        await _context.NbtDataWriter.UpdatePlayerExperienceAsync(
            playerName,
            playerData.XpLevel + reward.Experience,
            playerData.XpProgress
        );
    }
    
    _context.Logger.Info($"已发放离线奖励给 {playerName}");
}
```

### **场景2：数据修复工具**

```csharp
public async Task RepairCorruptedPlayerDataAsync(string playerName)
{
    try
    {
        var nbt = await _context.NbtDataWriter.ReadNbtFileAsync(
            GetPlayerDataPath(playerName));
        
        // 修复异常数值
        if (nbt.Get<NbtFloat>("Health")?.Value < 0)
        {
            nbt["Health"] = new NbtFloat(20);
            _context.Logger.Info($"修复了 {playerName} 的异常生命值");
        }
        
        if (nbt.Get<NbtInt>("foodLevel")?.Value < 0)
        {
            nbt["foodLevel"] = new NbtInt(20);
            _context.Logger.Info($"修复了 {playerName} 的异常饥饿值");
        }
        
        // 写回
        await _context.NbtDataWriter.WriteNbtFileAsync(
            GetPlayerDataPath(playerName), nbt);
        
        _context.Logger.Info($"玩家 {playerName} 数据修复完成");
    }
    catch (Exception ex)
    {
        _context.Logger.Error($"数据修复失败: {ex.Message}");
    }
}
```

### **场景3：数据迁移**

```csharp
public async Task MigratePlayerDataAsync(string oldServer, string newServer)
{
    var oldPlayerFiles = Directory.GetFiles(
        Path.Combine(oldServer, "world/playerdata"), "*.dat");
    
    foreach (var oldFile in oldPlayerFiles)
    {
        var playerUuid = Path.GetFileNameWithoutExtension(oldFile);
        var newFile = Path.Combine(newServer, "world/playerdata", $"{playerUuid}.dat");
        
        // 读取旧数据
        var nbt = await _context.NbtDataWriter.ReadNbtFileAsync(oldFile);
        
        // 数据转换（如果需要）
        TransformNbtData(nbt);
        
        // 写入新服务器
        await _context.NbtDataWriter.WriteNbtFileAsync(newFile, nbt);
        
        _context.Logger.Info($"已迁移玩家数据: {playerUuid}");
    }
}
```

---

## 📚 **相关文档**

- [游戏显示 API](../04-高级功能/游戏显示API.md)
- [事件系统](./事件系统.md)
- [API 参考](../08-参考/API参考.md)

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05
