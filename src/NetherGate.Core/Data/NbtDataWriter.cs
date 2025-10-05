using fNbt;
using NetherGate.API.Data;
using NetherGate.API.Logging;

namespace NetherGate.Core.Data;

/// <summary>
/// NBT 数据写入器实现
/// ⚠️ 警告：修改 NBT 数据需要谨慎，建议在服务器停止时操作
/// </summary>
public class NbtDataWriter : INbtDataWriter
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;
    private readonly string _backupDirectory;

    public NbtDataWriter(string serverDirectory, ILogger logger)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
        _backupDirectory = Path.Combine(_serverDirectory, "backups", "nbt");
        
        // 确保备份目录存在
        Directory.CreateDirectory(_backupDirectory);
    }

    // ========== 玩家数据写入 ==========

    public async Task UpdatePlayerPositionAsync(Guid playerUuid, double x, double y, double z, string? dimension = null)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            // 更新位置
            var posList = root.Get<NbtList>("Pos") ?? new NbtList("Pos", NbtTagType.Double);
            posList.Clear();
            posList.Add(new NbtDouble(x));
            posList.Add(new NbtDouble(y));
            posList.Add(new NbtDouble(z));
            root["Pos"] = posList;

            // 更新维度（如果提供）
            if (dimension != null)
            {
                root["Dimension"] = new NbtString("Dimension", dimension);
            }

            _logger.Info($"已更新玩家 {playerUuid} 位置: ({x:F2}, {y:F2}, {z:F2})");
        });
    }

    public async Task UpdatePlayerHealthAsync(Guid playerUuid, float health)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            root["Health"] = new NbtFloat("Health", health);
            _logger.Info($"已更新玩家 {playerUuid} 生命值: {health}");
        });
    }

    public async Task UpdatePlayerFoodLevelAsync(Guid playerUuid, int foodLevel, float saturation)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            root["foodLevel"] = new NbtInt("foodLevel", foodLevel);
            root["foodSaturationLevel"] = new NbtFloat("foodSaturationLevel", saturation);
            _logger.Info($"已更新玩家 {playerUuid} 饱食度: {foodLevel}, 饱和度: {saturation:F2}");
        });
    }

    public async Task UpdatePlayerExperienceAsync(Guid playerUuid, int level, float progress, int total)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            root["XpLevel"] = new NbtInt("XpLevel", level);
            root["XpP"] = new NbtFloat("XpP", progress);
            root["XpTotal"] = new NbtInt("XpTotal", total);
            _logger.Info($"已更新玩家 {playerUuid} 经验: 等级 {level}, 总经验 {total}");
        });
    }

    public async Task UpdatePlayerGameModeAsync(Guid playerUuid, GameMode gameMode)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            root["playerGameType"] = new NbtInt("playerGameType", (int)gameMode);
            _logger.Info($"已更新玩家 {playerUuid} 游戏模式: {gameMode}");
        });
    }

    public async Task AddItemToInventoryAsync(Guid playerUuid, ItemStack item, int slot = -1)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            var inventory = root.Get<NbtList>("Inventory") ?? new NbtList("Inventory", NbtTagType.Compound);

            // 如果 slot 是 -1，自动寻找空位
            if (slot == -1)
            {
                slot = FindEmptyInventorySlot(inventory);
                if (slot == -1)
                {
                    _logger.Warning($"玩家 {playerUuid} 背包已满，无法添加物品");
                    return;
                }
            }

            // 创建物品 NBT
            var itemNbt = CreateItemNbt(item.Id, item.Count, item.Enchantments, item.CustomName, null, null);
            itemNbt.Add(new NbtByte("Slot", (byte)slot));

            // 移除旧的同槽位物品
            RemoveItemAtSlot(inventory, slot);

            // 添加新物品
            inventory.Add(itemNbt);
            root["Inventory"] = inventory;

            _logger.Info($"已添加物品 {item.Id} x{item.Count} 到玩家 {playerUuid} 背包槽位 {slot}");
        });
    }

    public async Task RemoveItemFromInventoryAsync(Guid playerUuid, int slot)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            var inventory = root.Get<NbtList>("Inventory");
            if (inventory == null)
            {
                _logger.Warning($"玩家 {playerUuid} 没有背包数据");
                return;
            }

            RemoveItemAtSlot(inventory, slot);
            _logger.Info($"已从玩家 {playerUuid} 背包移除槽位 {slot} 的物品");
        });
    }

    public async Task UpdateInventoryItemAsync(Guid playerUuid, int slot, ItemStack item)
    {
        await RemoveItemFromInventoryAsync(playerUuid, slot);
        await AddItemToInventoryAsync(playerUuid, item, slot);
    }

    public async Task ClearPlayerInventoryAsync(Guid playerUuid)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            root["Inventory"] = new NbtList("Inventory", NbtTagType.Compound);
            _logger.Info($"已清空玩家 {playerUuid} 的背包");
        });
    }

    public async Task AddStatusEffectAsync(Guid playerUuid, StatusEffect effect)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            var effects = root.Get<NbtList>("ActiveEffects") ?? root.Get<NbtList>("active_effects") 
                ?? new NbtList("active_effects", NbtTagType.Compound);

            // 尝试将效果 ID 字符串解析为数值（假设 ID 为 "minecraft:speed" 等形式）
            // 如果无法解析，使用 1 作为默认值
            byte effectIdByte = 1;
            if (int.TryParse(effect.Id.Split(':').LastOrDefault() ?? "1", out int parsedId))
            {
                effectIdByte = (byte)parsedId;
            }

            var effectNbt = new NbtCompound
            {
                new NbtByte("Id", effectIdByte),
                new NbtInt("Duration", effect.Duration),
                new NbtByte("Amplifier", (byte)effect.Amplifier),
                new NbtByte("Ambient", 0),
                new NbtByte("ShowParticles", 1)
            };

            effects.Add(effectNbt);
            root["active_effects"] = effects;

            _logger.Info($"已给予玩家 {playerUuid} 状态效果 {effect.Id}");
        });
    }

    public async Task RemoveStatusEffectAsync(Guid playerUuid, int effectId)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            var effects = root.Get<NbtList>("active_effects") ?? root.Get<NbtList>("ActiveEffects");
            if (effects == null) return;

            var toRemove = effects.Cast<NbtCompound>()
                .FirstOrDefault(e => e.Get<NbtByte>("Id")?.Value == effectId);

            if (toRemove != null)
            {
                effects.Remove(toRemove);
                _logger.Info($"已移除玩家 {playerUuid} 的状态效果 {effectId}");
            }
        });
    }

    public async Task UpdatePlayerArmorAsync(Guid playerUuid, PlayerArmor armor)
    {
        await ModifyPlayerNbtAsync(playerUuid, root =>
        {
            var inventory = root.Get<NbtList>("Inventory") ?? new NbtList("Inventory", NbtTagType.Compound);

            // 移除旧盔甲
            for (int slot = 100; slot <= 103; slot++)
            {
                RemoveItemAtSlot(inventory, slot);
            }

            // 添加新盔甲
            if (armor.Helmet != null)
                AddArmorPiece(inventory, armor.Helmet, 103);
            if (armor.Chestplate != null)
                AddArmorPiece(inventory, armor.Chestplate, 102);
            if (armor.Leggings != null)
                AddArmorPiece(inventory, armor.Leggings, 101);
            if (armor.Boots != null)
                AddArmorPiece(inventory, armor.Boots, 100);

            root["Inventory"] = inventory;
            _logger.Info($"已更新玩家 {playerUuid} 的盔甲");
        });
    }

    public async Task ModifyPlayerNbtAsync(Guid playerUuid, Action<NbtCompound> nbtModifier)
    {
        var playerFile = GetPlayerDataFilePath(playerUuid);

        if (!File.Exists(playerFile))
        {
            _logger.Error($"玩家数据文件不存在: {playerUuid}");
            throw new FileNotFoundException($"Player data not found: {playerUuid}");
        }

        await Task.Run(() =>
        {
            // 备份原文件
            BackupFile(playerFile);

            // 读取 NBT
            var nbtFile = new NbtFile();
            nbtFile.LoadFromFile(playerFile);

            if (nbtFile.RootTag == null)
            {
                throw new InvalidDataException("Invalid NBT file: no root tag");
            }

            // 修改 NBT
            nbtModifier(nbtFile.RootTag);

            // 写回文件
            nbtFile.SaveToFile(playerFile, NbtCompression.GZip);

            _logger.Debug($"已写入玩家 NBT 数据: {playerUuid}");
        });
    }

    // ========== 世界数据写入 ==========

    public async Task UpdateWorldSpawnAsync(string worldName, int x, int y, int z)
    {
        await ModifyWorldNbtAsync(worldName, root =>
        {
            var data = root.Get<NbtCompound>("Data") ?? new NbtCompound("Data");
            
            data["SpawnX"] = new NbtInt("SpawnX", x);
            data["SpawnY"] = new NbtInt("SpawnY", y);
            data["SpawnZ"] = new NbtInt("SpawnZ", z);

            root["Data"] = data;
            _logger.Info($"已更新世界 {worldName} 出生点: ({x}, {y}, {z})");
        });
    }

    public async Task UpdateWorldBorderAsync(string worldName, double centerX, double centerZ, double size, double damagePerBlock, double warningDistance)
    {
        await ModifyWorldNbtAsync(worldName, root =>
        {
            var data = root.Get<NbtCompound>("Data") ?? new NbtCompound("Data");
            var border = data.Get<NbtCompound>("WorldBorder") ?? new NbtCompound("WorldBorder");

            border["CenterX"] = new NbtDouble("CenterX", centerX);
            border["CenterZ"] = new NbtDouble("CenterZ", centerZ);
            border["Size"] = new NbtDouble("Size", size);
            border["DamagePerBlock"] = new NbtDouble("DamagePerBlock", damagePerBlock);
            border["WarningDistance"] = new NbtDouble("WarningDistance", warningDistance);

            data["WorldBorder"] = border;
            root["Data"] = data;

            _logger.Info($"已更新世界 {worldName} 边界");
        });
    }

    public async Task UpdateGameRuleAsync(string worldName, string ruleName, string value)
    {
        await ModifyWorldNbtAsync(worldName, root =>
        {
            var data = root.Get<NbtCompound>("Data") ?? new NbtCompound("Data");
            var gameRules = data.Get<NbtCompound>("GameRules") ?? new NbtCompound("GameRules");

            gameRules[ruleName] = new NbtString(ruleName, value);

            data["GameRules"] = gameRules;
            root["Data"] = data;

            _logger.Info($"已更新世界 {worldName} 游戏规则: {ruleName} = {value}");
        });
    }

    public async Task UpdateWorldTimeAsync(string worldName, long dayTime, long gameTime)
    {
        await ModifyWorldNbtAsync(worldName, root =>
        {
            var data = root.Get<NbtCompound>("Data") ?? new NbtCompound("Data");

            data["DayTime"] = new NbtLong("DayTime", dayTime);
            data["Time"] = new NbtLong("Time", gameTime);

            root["Data"] = data;
            _logger.Info($"已更新世界 {worldName} 时间: Day={dayTime}, Game={gameTime}");
        });
    }

    public async Task UpdateWorldWeatherAsync(string worldName, bool raining, int rainTime, bool thundering, int thunderTime)
    {
        await ModifyWorldNbtAsync(worldName, root =>
        {
            var data = root.Get<NbtCompound>("Data") ?? new NbtCompound("Data");

            data["raining"] = new NbtByte("raining", (byte)(raining ? 1 : 0));
            data["rainTime"] = new NbtInt("rainTime", rainTime);
            data["thundering"] = new NbtByte("thundering", (byte)(thundering ? 1 : 0));
            data["thunderTime"] = new NbtInt("thunderTime", thunderTime);

            root["Data"] = data;
            _logger.Info($"已更新世界 {worldName} 天气");
        });
    }

    public async Task ModifyWorldNbtAsync(string worldName, Action<NbtCompound> nbtModifier)
    {
        var worldFile = GetWorldDataFilePath(worldName);

        if (!File.Exists(worldFile))
        {
            _logger.Error($"世界数据文件不存在: {worldName}");
            throw new FileNotFoundException($"World data not found: {worldName}");
        }

        await Task.Run(() =>
        {
            // 备份原文件
            BackupFile(worldFile);

            // 读取 NBT
            var nbtFile = new NbtFile();
            nbtFile.LoadFromFile(worldFile);

            if (nbtFile.RootTag == null)
            {
                throw new InvalidDataException("Invalid NBT file: no root tag");
            }

            // 修改 NBT
            nbtModifier(nbtFile.RootTag);

            // 写回文件
            nbtFile.SaveToFile(worldFile, NbtCompression.GZip);

            _logger.Debug($"已写入世界 NBT 数据: {worldName}");
        });
    }

    // ========== 实体/物品 NBT 创建 ==========

    public NbtCompound CreateEntityNbt(string entityType, double x, double y, double z, NbtCompound? customNbt = null)
    {
        var entity = new NbtCompound
        {
            new NbtString("id", entityType),
            new NbtList("Pos", NbtTagType.Double)
            {
                new NbtDouble(x),
                new NbtDouble(y),
                new NbtDouble(z)
            },
            new NbtList("Motion", NbtTagType.Double)
            {
                new NbtDouble(0),
                new NbtDouble(0),
                new NbtDouble(0)
            },
            new NbtList("Rotation", NbtTagType.Float)
            {
                new NbtFloat(0),
                new NbtFloat(0)
            }
        };

        // 合并自定义 NBT
        if (customNbt != null)
        {
            foreach (var tag in customNbt)
            {
                if (tag.Name != null)
                {
                    entity[tag.Name] = tag;
                }
            }
        }

        return entity;
    }

    public NbtCompound CreateItemNbt(
        string itemId, 
        int count = 1, 
        List<Enchantment>? enchantments = null,
        string? customName = null,
        List<string>? lore = null,
        NbtCompound? customNbt = null)
    {
        var item = new NbtCompound
        {
            new NbtString("id", itemId),
            new NbtByte("Count", (byte)count)
        };

        // 添加标签数据
        if (enchantments != null || customName != null || lore != null || customNbt != null)
        {
            var tag = new NbtCompound("tag");

            // 附魔
            if (enchantments != null && enchantments.Count > 0)
            {
                var enchList = new NbtList("Enchantments", NbtTagType.Compound);
                foreach (var ench in enchantments)
                {
                    enchList.Add(new NbtCompound
                    {
                        new NbtString("id", ench.Id),
                        new NbtInt("lvl", ench.Level)
                    });
                }
                tag.Add(enchList);
            }

            // 显示信息
            if (customName != null || lore != null)
            {
                var display = new NbtCompound("display");
                
                if (customName != null)
                {
                    display.Add(new NbtString("Name", $"{{\"text\":\"{customName}\"}}"));
                }

                if (lore != null && lore.Count > 0)
                {
                    var loreList = new NbtList("Lore", NbtTagType.String);
                    foreach (var line in lore)
                    {
                        loreList.Add(new NbtString($"{{\"text\":\"{line}\"}}"));
                    }
                    display.Add(loreList);
                }

                tag.Add(display);
            }

            // 合并自定义 NBT
            if (customNbt != null)
            {
                foreach (var customTag in customNbt)
                {
                    if (customTag.Name != null)
                    {
                        tag[customTag.Name] = customTag;
                    }
                }
            }

            item.Add(tag);
        }

        return item;
    }

    // ========== 通用 NBT 操作 ==========

    public async Task<NbtCompound?> ReadNbtFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.Debug($"NBT 文件不存在: {filePath}");
            return null;
        }

        return await Task.Run(() =>
        {
            try
            {
                var nbtFile = new NbtFile();
                nbtFile.LoadFromFile(filePath);
                return nbtFile.RootTag;
            }
            catch (Exception ex)
            {
                _logger.Error($"读取 NBT 文件失败: {filePath}", ex);
                return null;
            }
        });
    }

    public async Task WriteNbtFileAsync(string filePath, NbtCompound nbt, bool backup = true)
    {
        await Task.Run(() =>
        {
            if (backup && File.Exists(filePath))
            {
                BackupFile(filePath);
            }

            var nbtFile = new NbtFile(nbt);
            nbtFile.SaveToFile(filePath, NbtCompression.GZip);

            _logger.Debug($"已写入 NBT 文件: {filePath}");
        });
    }

    public bool ValidateNbt(NbtCompound nbt, string expectedRootTag)
    {
        return nbt.Name == expectedRootTag || nbt.Get<NbtCompound>(expectedRootTag) != null;
    }

    // ========== 辅助方法 ==========

    private string GetPlayerDataFilePath(Guid playerUuid)
    {
        var uuidStr = playerUuid.ToString();
        return Path.Combine(_serverDirectory, "world", "playerdata", $"{uuidStr}.dat");
    }

    private string GetWorldDataFilePath(string worldName)
    {
        return Path.Combine(_serverDirectory, worldName, "level.dat");
    }

    private void BackupFile(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(_backupDirectory, $"{fileName}.{timestamp}.backup");

            File.Copy(filePath, backupPath, overwrite: true);
            _logger.Debug($"已备份文件: {backupPath}");
        }
        catch (Exception)
        {
            _logger.Warning($"备份文件失败: {filePath}");
        }
    }

    private int FindEmptyInventorySlot(NbtList inventory)
    {
        var usedSlots = inventory.Cast<NbtCompound>()
            .Select(item => item.Get<NbtByte>("Slot")?.Value ?? -1)
            .ToHashSet();

        // 查找 0-35 范围内的空槽位（主背包和快捷栏）
        for (byte slot = 0; slot < 36; slot++)
        {
            if (!usedSlots.Contains(slot))
            {
                return slot;
            }
        }

        return -1; // 背包已满
    }

    private void RemoveItemAtSlot(NbtList inventory, int slot)
    {
        var toRemove = inventory.Cast<NbtCompound>()
            .FirstOrDefault(item => item.Get<NbtByte>("Slot")?.Value == slot);

        if (toRemove != null)
        {
            inventory.Remove(toRemove);
        }
    }

    private void AddArmorPiece(NbtList inventory, ItemStack armor, int slot)
    {
        var armorNbt = CreateItemNbt(armor.Id, armor.Count, armor.Enchantments, armor.CustomName, null, null);
        armorNbt.Add(new NbtByte("Slot", (byte)slot));
        inventory.Add(armorNbt);
    }
}
