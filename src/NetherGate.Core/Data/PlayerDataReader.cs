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
            Armor = data.Armor,
            Effects = data.Effects,
            LastPlayed = data.LastPlayed,
            IsOnline = data.IsOnline
        };

        // 解析装备（目前简化处理）
        // TODO: 完整实现装备解析

        return data;
    }

    private List<ItemStack> ParseItemList(NbtList itemList)
    {
        var items = new List<ItemStack>();

        foreach (var itemTag in itemList.OfType<NbtCompound>())
        {
            try
            {
                var item = new ItemStack
                {
                    Id = itemTag["id"]?.StringValue ?? "minecraft:air",
                    Count = itemTag["Count"]?.ByteValue ?? 1,
                    Slot = itemTag["Slot"]?.ByteValue ?? 0
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
        // 简化实现 - 实际需要解析完整的 JSON 统计数据
        // 参考: https://minecraft.fandom.com/wiki/Statistics
        var stats = new PlayerStats
        {
            Uuid = playerUuid
        };

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("stats", out var statsObj))
            {
                // 解析统计数据
                // TODO: 完整实现统计解析
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"解析玩家统计失败: {ex.Message}");
        }

        return stats;
    }

    private PlayerAdvancements ParsePlayerAdvancements(string json, Guid playerUuid)
    {
        // 简化实现 - 实际需要解析完整的成就数据
        var advancements = new PlayerAdvancements
        {
            Uuid = playerUuid
        };

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // TODO: 完整实现成就解析
        }
        catch (Exception ex)
        {
            _logger.Warning($"解析玩家成就失败: {ex.Message}");
        }

        return advancements;
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
}

