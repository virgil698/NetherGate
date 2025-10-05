using fNbt;
using NetherGate.API.Data;
using NetherGate.API.Logging;

namespace NetherGate.Core.Data;

/// <summary>
/// 世界数据读取器实现
/// 基于 https://zh.minecraft.wiki/w/NBT%E6%A0%BC%E5%BC%8F
/// </summary>
public class WorldDataReader : IWorldDataReader
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;

    public WorldDataReader(string serverDirectory, ILogger logger)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
    }

    public async Task<WorldInfo?> ReadWorldInfoAsync(string? worldName = null)
    {
        var world = worldName ?? "world";
        var levelFile = GetLevelDatPath(world);

        if (!File.Exists(levelFile))
        {
            _logger.Warning($"世界数据文件不存在: {world}");
            return null;
        }

        try
        {
            return await Task.Run(() =>
            {
                var nbtFile = new NbtFile();
                nbtFile.LoadFromFile(levelFile);

                return ParseWorldInfo(nbtFile.RootTag, world);
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"读取世界数据失败: {world}", ex);
            return null;
        }
    }

    public List<string> ListWorlds()
    {
        try
        {
            return Directory.GetDirectories(_serverDirectory)
                .Where(dir =>
                {
                    var dirName = Path.GetFileName(dir);
                    return dirName == "world" || dirName.StartsWith("world_");
                })
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error("列出世界失败", ex);
            return new List<string>();
        }
    }

    public long GetWorldSize(string worldName)
    {
        var worldPath = Path.Combine(_serverDirectory, worldName);

        if (!Directory.Exists(worldPath))
        {
            return 0;
        }

        try
        {
            var files = Directory.GetFiles(worldPath, "*", SearchOption.AllDirectories);
            var totalSize = files.Sum(file => new FileInfo(file).Length);

            return totalSize / 1024 / 1024; // 转换为 MB
        }
        catch (Exception ex)
        {
            _logger.Error($"获取世界大小失败: {worldName}", ex);
            return 0;
        }
    }

    public async Task<long?> GetWorldSeedAsync(string worldName)
    {
        var worldInfo = await ReadWorldInfoAsync(worldName);
        return worldInfo?.Seed;
    }

    public async Task<SpawnPoint?> GetSpawnPointAsync(string worldName)
    {
        var worldInfo = await ReadWorldInfoAsync(worldName);
        return worldInfo?.SpawnPoint;
    }

    public bool WorldExists(string worldName)
    {
        var worldPath = Path.Combine(_serverDirectory, worldName);
        var levelFile = Path.Combine(worldPath, "level.dat");

        return Directory.Exists(worldPath) && File.Exists(levelFile);
    }

    private WorldInfo ParseWorldInfo(NbtCompound root, string worldName)
    {
        var data = root["Data"] as NbtCompound ?? root;

        var info = new WorldInfo
        {
            Name = worldName,
            Path = Path.Combine(_serverDirectory, worldName),
            Seed = data["WorldGenSettings"]?["seed"]?.LongValue ??
                   data["RandomSeed"]?.LongValue ?? 0,
            Version = data["Version"]?["Name"]?.StringValue ?? "Unknown",
            LevelType = data["generatorName"]?.StringValue ?? "default",
            GameTime = data["Time"]?.LongValue ?? 0,
            DayTime = data["DayTime"]?.LongValue ?? 0,
            AllowCommands = (data["allowCommands"]?.ByteValue ?? 0) == 1,
            Difficulty = (Difficulty)(data["Difficulty"]?.ByteValue ?? 2),
            DifficultyLocked = (data["DifficultyLocked"]?.ByteValue ?? 0) == 1,
            SizeMB = GetWorldSize(worldName),
            Raining = (data["raining"]?.ByteValue ?? 0) == 1,
            Thundering = (data["thundering"]?.ByteValue ?? 0) == 1
        };

        // 解析出生点
        var spawnPoint = new SpawnPoint
        {
            X = data["SpawnX"]?.IntValue ?? 0,
            Y = data["SpawnY"]?.IntValue ?? 64,
            Z = data["SpawnZ"]?.IntValue ?? 0,
            Angle = data["SpawnAngle"]?.FloatValue ?? 0
        };

        // 解析世界边界
        var worldBorder = info.WorldBorder;
        if (data["WorldBorder"] is NbtCompound borderData)
        {
            worldBorder = new WorldBorder
            {
                CenterX = borderData["CenterX"]?.DoubleValue ?? 0,
                CenterZ = borderData["CenterZ"]?.DoubleValue ?? 0,
                Size = borderData["Size"]?.DoubleValue ?? 60000000,
                SizeLerpTarget = borderData["SizeLerpTarget"]?.DoubleValue ?? 60000000,
                DamagePerBlock = borderData["DamagePerBlock"]?.DoubleValue ?? 0.2,
                WarningBlocks = borderData["WarningBlocks"]?.DoubleValue ?? 5
            };
        }

        // 解析游戏规则
        var gameRules = info.GameRules;
        if (data["GameRules"] is NbtCompound gameRulesData)
        {
            gameRules = new Dictionary<string, string>();

            foreach (var tag in gameRulesData.Tags)
            {
                if (!string.IsNullOrEmpty(tag.Name))
                {
                    gameRules[tag.Name] = tag.StringValue ?? tag.ToString();
                }
            }
        }

        // 解析数据包
        var enabledDataPacks = info.EnabledDataPacks;
        if (data["DataPacks"]?["Enabled"] is NbtList enabledPacksList)
        {
            enabledDataPacks = enabledPacksList
                .Select(t => t.StringValue)
                .Where(s => !string.IsNullOrEmpty(s))
                .Cast<string>()
                .ToList();
        }

        // 解析最后游玩时间
        DateTime? lastPlayed = info.LastPlayed;
        if (data["LastPlayed"]?.LongValue is long lastPlayedTicks)
        {
            try
            {
                lastPlayed = DateTimeOffset.FromUnixTimeMilliseconds(lastPlayedTicks).DateTime;
            }
            catch
            {
                // 忽略时间解析错误
            }
        }

        // 创建最终的 WorldInfo 对象
        info = new WorldInfo
        {
            Name = info.Name,
            Path = info.Path,
            Seed = info.Seed,
            Version = info.Version,
            LevelType = info.LevelType,
            GameTime = info.GameTime,
            DayTime = info.DayTime,
            AllowCommands = info.AllowCommands,
            Difficulty = info.Difficulty,
            DifficultyLocked = info.DifficultyLocked,
            SizeMB = info.SizeMB,
            Raining = info.Raining,
            Thundering = info.Thundering,
            SpawnPoint = spawnPoint,
            WorldBorder = worldBorder,
            GameRules = gameRules,
            EnabledDataPacks = enabledDataPacks,
            LastPlayed = lastPlayed
        };

        return info;
    }

    private string GetLevelDatPath(string worldName)
    {
        return Path.Combine(_serverDirectory, worldName, "level.dat");
    }
}

