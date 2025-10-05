using fNbt;
using NetherGate.API.Data;
using NetherGate.API.Logging;

namespace NetherGate.Core.Data;

/// <summary>
/// 玩家数据读取器实现
/// 基于 https://zh.minecraft.wiki/w/NBT%E6%A0%BC%E5%BC%8F
/// </summary>
public class PlayerDataReader : IPlayerDataReader
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;

    public PlayerDataReader(string serverDirectory, ILogger logger)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
    }

    public async Task<PlayerData?> ReadPlayerDataAsync(Guid playerUuid)
    {
        var playerFile = GetPlayerDataFilePath(playerUuid);

        if (!File.Exists(playerFile))
        {
            _logger.Debug($"玩家数据文件不存在: {playerUuid}");
            return null;
        }

        try
        {
            return await Task.Run(() =>
            {
                var nbtFile = new NbtFile();
                nbtFile.LoadFromFile(playerFile);

                return ParsePlayerData(nbtFile.RootTag, playerUuid);
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"读取玩家数据失败: {playerUuid}", ex);
            return null;
        }
    }

    public async Task<PlayerStats?> ReadPlayerStatsAsync(Guid playerUuid)
    {
        var statsFile = GetPlayerStatsFilePath(playerUuid);

        if (!File.Exists(statsFile))
        {
            _logger.Debug($"玩家统计文件不存在: {playerUuid}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(statsFile);
            return ParsePlayerStats(json, playerUuid);
        }
        catch (Exception ex)
        {
            _logger.Error($"读取玩家统计失败: {playerUuid}", ex);
            return null;
        }
    }

    public async Task<PlayerAdvancements?> ReadPlayerAdvancementsAsync(Guid playerUuid)
    {
        var advFile = GetPlayerAdvancementsFilePath(playerUuid);

        if (!File.Exists(advFile))
        {
            _logger.Debug($"玩家成就文件不存在: {playerUuid}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(advFile);
            return ParsePlayerAdvancements(json, playerUuid);
        }
        catch (Exception ex)
        {
            _logger.Error($"读取玩家成就失败: {playerUuid}", ex);
            return null;
        }
    }

    public List<Guid> ListPlayers(string? worldName = null)
    {
        var world = worldName ?? "world";
        var playerDataDir = Path.Combine(_serverDirectory, world, "playerdata");

        if (!Directory.Exists(playerDataDir))
        {
            return new List<Guid>();
        }

        try
        {
            return Directory.GetFiles(playerDataDir, "*.dat")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(f => Guid.TryParse(f, out _))
                .Select(Guid.Parse)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"列出玩家失败: {world}", ex);
            return new List<Guid>();
        }
    }

    public List<PlayerData> GetOnlinePlayers()
    {
        // TODO: 通过 SMP 或 RCON 获取在线玩家列表，然后读取数据
        // 目前返回空列表
        _logger.Warning("GetOnlinePlayers 功能尚未完全实现，需要与 SMP/RCON 集成");
        return new List<PlayerData>();
    }

    public bool PlayerDataExists(Guid playerUuid)
    {
        return File.Exists(GetPlayerDataFilePath(playerUuid));
    }

    private PlayerData ParsePlayerData(NbtCompound root, Guid playerUuid)
    {
        var data = new PlayerData
        {
            Uuid = playerUuid,
            Health = root["Health"]?.FloatValue ?? 20.0f,
            FoodLevel = root["foodLevel"]?.IntValue ?? 20,
            XpLevel = root["XpLevel"]?.IntValue ?? 0,
            XpTotal = root["XpTotal"]?.IntValue ?? 0,
            GameMode = (GameMode)(root["playerGameType"]?.IntValue ?? 0)
        };

        // 解析位置
        var position = data.Position;
        if (root["Pos"] is NbtList posList && posList.Count >= 3)
        {
            position = new PlayerPosition
            {
                X = posList[0].DoubleValue,
                Y = posList[1].DoubleValue,
                Z = posList[2].DoubleValue,
                Dimension = root["Dimension"]?.StringValue ?? "minecraft:overworld"
            };
        }

        // 解析旋转
        if (root["Rotation"] is NbtList rotList && rotList.Count >= 2)
        {
            position = new PlayerPosition
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z,
                Dimension = position.Dimension,
                Yaw = rotList[0].FloatValue,
                Pitch = rotList[1].FloatValue
            };
        }

        // 解析装备
        var armor = ParseArmor(root);

        // 解析状态效果
        var effects = ParseStatusEffects(root);

        data = new PlayerData
        {
            Uuid = data.Uuid,
            Name = data.Name,
            Health = data.Health,
            FoodLevel = data.FoodLevel,
            XpLevel = data.XpLevel,
            XpTotal = data.XpTotal,
            GameMode = data.GameMode,
            Position = position,
            Inventory = root["Inventory"] is NbtList inventory ? ParseItemList(inventory) : data.Inventory,
            EnderChest = root["EnderItems"] is NbtList enderItems ? ParseItemList(enderItems) : data.EnderChest,
            Armor = armor,
            Effects = effects,
            LastPlayed = data.LastPlayed,
            IsOnline = data.IsOnline
        };

        return data;
    }

    private List<ItemStack> ParseItemList(NbtList itemList)
    {
        var items = new List<ItemStack>();

        foreach (var itemTag in itemList.OfType<NbtCompound>())
        {
            try
            {
                var enchantments = new List<Enchantment>();
                string? customName = null;

                // 解析物品 NBT 数据
                if (itemTag["tag"] is NbtCompound tag)
                {
                    // 解析附魔
                    if (tag["Enchantments"] is NbtList enchList)
                    {
                        foreach (var enchTag in enchList.OfType<NbtCompound>())
                        {
                            enchantments.Add(new Enchantment
                            {
                                Id = enchTag["id"]?.StringValue ?? "",
                                Level = enchTag["lvl"]?.ShortValue ?? 1
                            });
                        }
                    }

                    // 解析自定义名称
                    if (tag["display"] is NbtCompound display)
                    {
                        customName = display["Name"]?.StringValue;
                    }
                }

                var item = new ItemStack
                {
                    Id = itemTag["id"]?.StringValue ?? "minecraft:air",
                    Count = itemTag["Count"]?.ByteValue ?? 1,
                    Slot = itemTag["Slot"]?.ByteValue ?? 0,
                    Enchantments = enchantments,
                    CustomName = customName
                };

                items.Add(item);
            }
            catch (Exception ex)
            {
                _logger.Trace($"解析物品失败: {ex.Message}");
            }
        }

        return items;
    }

    private PlayerStats ParsePlayerStats(string json, Guid playerUuid)
    {
        var stats = new PlayerStats
        {
            Uuid = playerUuid
        };

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("stats", out var statsObj))
                return stats;

            var mobKills = new Dictionary<string, int>();
            var blocksMined = new Dictionary<string, int>();
            var itemsUsed = new Dictionary<string, int>();
            var customStats = new Dictionary<string, int>();

            int deaths = 0;
            int jumps = 0;
            double distanceWalked = 0;
            double distanceFlown = 0;
            int playTime = 0;

            // 解析自定义统计
            if (statsObj.TryGetProperty("minecraft:custom", out var customObj))
            {
                foreach (var prop in customObj.EnumerateObject())
                {
                    var value = prop.Value.GetInt32();
                    customStats[prop.Name] = value;

                    // 提取常用统计
                    switch (prop.Name)
                    {
                        case "minecraft:deaths":
                            deaths = value;
                            break;
                        case "minecraft:jump":
                            jumps = value;
                            break;
                        case "minecraft:walk_one_cm":
                            distanceWalked = value / 100.0; // 转换为米
                            break;
                        case "minecraft:fly_one_cm":
                            distanceFlown = value / 100.0;
                            break;
                        case "minecraft:play_time":
                        case "minecraft:play_one_minute":
                            playTime = value / 20 / 60; // ticks -> 分钟
                            break;
                    }
                }
            }

            // 解析击杀统计
            if (statsObj.TryGetProperty("minecraft:killed", out var killedObj))
            {
                foreach (var prop in killedObj.EnumerateObject())
                {
                    mobKills[prop.Name] = prop.Value.GetInt32();
                }
            }

            // 解析挖掘统计
            if (statsObj.TryGetProperty("minecraft:mined", out var minedObj))
            {
                foreach (var prop in minedObj.EnumerateObject())
                {
                    blocksMined[prop.Name] = prop.Value.GetInt32();
                }
            }

            // 解析使用物品统计
            if (statsObj.TryGetProperty("minecraft:used", out var usedObj))
            {
                foreach (var prop in usedObj.EnumerateObject())
                {
                    itemsUsed[prop.Name] = prop.Value.GetInt32();
                }
            }

            stats = new PlayerStats
            {
                Uuid = playerUuid,
                PlayTimeMinutes = playTime,
                MobKills = mobKills,
                Deaths = deaths,
                Jumps = jumps,
                DistanceWalked = distanceWalked,
                DistanceFlown = distanceFlown,
                BlocksMined = blocksMined,
                ItemsUsed = itemsUsed,
                CustomStats = customStats
            };
        }
        catch (Exception ex)
        {
            _logger.Warning($"解析玩家统计失败: {ex.Message}");
        }

        return stats;
    }

    private PlayerAdvancements ParsePlayerAdvancements(string json, Guid playerUuid)
    {
        var completed = new List<CompletedAdvancement>();
        var inProgress = new List<AdvancementProgress>();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var advancement in root.EnumerateObject())
            {
                var advId = advancement.Name;
                var advData = advancement.Value;

                if (!advData.TryGetProperty("done", out var doneElement))
                    continue;

                bool isDone = doneElement.GetBoolean();

                if (isDone)
                {
                    // 完全完成的成就
                    DateTime completedAt = DateTime.MinValue;

                    if (advData.TryGetProperty("done", out var doneTimeElement) && 
                        doneTimeElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        // 尝试解析时间戳
                        if (DateTime.TryParse(doneTimeElement.GetString(), out var parsedTime))
                        {
                            completedAt = parsedTime;
                        }
                    }
                    else if (advData.TryGetProperty("criteria", out var criteriaObj))
                    {
                        // 从标准中获取最新完成时间
                        foreach (var criterion in criteriaObj.EnumerateObject())
                        {
                            if (DateTime.TryParse(criterion.Value.GetString(), out var criterionTime))
                            {
                                if (criterionTime > completedAt)
                                    completedAt = criterionTime;
                            }
                        }
                    }

                    completed.Add(new CompletedAdvancement
                    {
                        Id = advId,
                        CompletedAt = completedAt
                    });
                }
                else if (advData.TryGetProperty("criteria", out var criteriaObj))
                {
                    // 进行中的成就
                    var completedCriteria = new List<string>();

                    foreach (var criterion in criteriaObj.EnumerateObject())
                    {
                        completedCriteria.Add(criterion.Name);
                    }

                    if (completedCriteria.Count > 0)
                    {
                        inProgress.Add(new AdvancementProgress
                        {
                            Id = advId,
                            CompletedCriteria = completedCriteria,
                            TotalCriteria = completedCriteria.Count // 实际总数需要从成就定义中获取
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"解析玩家成就失败: {ex.Message}");
        }

        // 计算完成百分比（粗略估算）
        double completionPercent = 0;
        int totalAdvancements = completed.Count + inProgress.Count;
        if (totalAdvancements > 0)
        {
            completionPercent = (double)completed.Count / totalAdvancements * 100;
        }

        return new PlayerAdvancements
        {
            Uuid = playerUuid,
            Completed = completed,
            InProgress = inProgress,
            CompletionPercent = completionPercent
        };
    }

    private string GetPlayerDataFilePath(Guid playerUuid)
    {
        return Path.Combine(_serverDirectory, "world", "playerdata", $"{playerUuid}.dat");
    }

    private string GetPlayerStatsFilePath(Guid playerUuid)
    {
        return Path.Combine(_serverDirectory, "world", "stats", $"{playerUuid}.json");
    }

    private string GetPlayerAdvancementsFilePath(Guid playerUuid)
    {
        return Path.Combine(_serverDirectory, "world", "advancements", $"{playerUuid}.json");
    }

    private PlayerArmor ParseArmor(NbtCompound root)
    {
        PlayerArmor armor = new();

        try
        {
            // 尝试从 ArmorItems 列表解析（较新版本）
            if (root["ArmorItems"] is NbtList armorList && armorList.Count >= 4)
            {
                // ArmorItems 顺序: [feet, legs, chest, head]
                var boots = armorList[0] as NbtCompound;
                var leggings = armorList[1] as NbtCompound;
                var chestplate = armorList[2] as NbtCompound;
                var helmet = armorList[3] as NbtCompound;

                armor = new PlayerArmor
                {
                    Helmet = ParseSingleItem(helmet),
                    Chestplate = ParseSingleItem(chestplate),
                    Leggings = ParseSingleItem(leggings),
                    Boots = ParseSingleItem(boots)
                };
            }
            // 尝试从 Inventory 中查找装备槽位（旧版本或备用方法）
            else if (root["Inventory"] is NbtList inventory)
            {
                foreach (var itemTag in inventory.OfType<NbtCompound>())
                {
                    var slot = itemTag["Slot"]?.ByteValue ?? 0;
                    
                    // 装备槽位编号: 100=feet, 101=legs, 102=chest, 103=head
                    var item = ParseSingleItem(itemTag);
                    if (item == null) continue;

                    armor = slot switch
                    {
                        100 => new PlayerArmor { Helmet = armor.Helmet, Chestplate = armor.Chestplate, Leggings = armor.Leggings, Boots = item },
                        101 => new PlayerArmor { Helmet = armor.Helmet, Chestplate = armor.Chestplate, Leggings = item, Boots = armor.Boots },
                        102 => new PlayerArmor { Helmet = armor.Helmet, Chestplate = item, Leggings = armor.Leggings, Boots = armor.Boots },
                        103 => new PlayerArmor { Helmet = item, Chestplate = armor.Chestplate, Leggings = armor.Leggings, Boots = armor.Boots },
                        _ => armor
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Trace($"解析装备失败: {ex.Message}");
        }

        return armor;
    }

    private ItemStack? ParseSingleItem(NbtCompound? itemTag)
    {
        if (itemTag == null) return null;

        var id = itemTag["id"]?.StringValue;
        if (string.IsNullOrEmpty(id) || id == "minecraft:air")
            return null;

        var enchantments = new List<Enchantment>();
        string? customName = null;

        // 解析物品 NBT 数据
        if (itemTag["tag"] is NbtCompound tag)
        {
            // 解析附魔
            if (tag["Enchantments"] is NbtList enchList)
            {
                foreach (var enchTag in enchList.OfType<NbtCompound>())
                {
                    enchantments.Add(new Enchantment
                    {
                        Id = enchTag["id"]?.StringValue ?? "",
                        Level = enchTag["lvl"]?.ShortValue ?? 1
                    });
                }
            }

            // 解析自定义名称
            if (tag["display"] is NbtCompound display)
            {
                customName = display["Name"]?.StringValue;
            }
        }

        return new ItemStack
        {
            Id = id,
            Count = itemTag["Count"]?.ByteValue ?? 1,
            Slot = itemTag["Slot"]?.ByteValue ?? 0,
            Enchantments = enchantments,
            CustomName = customName
        };
    }

    private List<StatusEffect> ParseStatusEffects(NbtCompound root)
    {
        var effects = new List<StatusEffect>();

        try
        {
            // 尝试两种可能的标签名
            var effectsList = root["ActiveEffects"] as NbtList ?? root["active_effects"] as NbtList;

            if (effectsList != null)
            {
                foreach (var effectTag in effectsList.OfType<NbtCompound>())
                {
                    var effect = new StatusEffect
                    {
                        Id = effectTag["Id"]?.ByteValue.ToString() ?? 
                             effectTag["id"]?.StringValue ?? "",
                        Amplifier = effectTag["Amplifier"]?.ByteValue ?? 0,
                        Duration = (effectTag["Duration"]?.IntValue ?? 0) / 20 // ticks -> 秒
                    };

                    effects.Add(effect);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Trace($"解析状态效果失败: {ex.Message}");
        }

        return effects;
    }
}

