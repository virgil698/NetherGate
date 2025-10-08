using System.Collections.Concurrent;
using System.Text.Json;
using NetherGate.API.Analytics;
using NetherGate.API.FileSystem;
using NetherGate.API.Logging;

namespace NetherGate.Core.Analytics;

/// <summary>
/// 统计数据追踪器实现
/// </summary>
public class StatisticsTracker : IStatisticsTracker
{
    private readonly ILogger _logger;
    private readonly string _worldPath;
    private readonly IFileWatcher _fileWatcher;
    private readonly ConcurrentDictionary<string, PlayerStatistics> _cache = new();
    private bool _isTracking;

    public event EventHandler<StatisticsUpdatedEventArgs>? StatisticsUpdated;

    public StatisticsTracker(ILogger logger, string worldPath, IFileWatcher fileWatcher)
    {
        _logger = logger;
        _worldPath = worldPath;
        _fileWatcher = fileWatcher;
    }

    public async Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerUuid)
    {
        if (_cache.TryGetValue(playerUuid, out var cached))
        {
            return cached;
        }

        var statsPath = Path.Combine(_worldPath, "stats", $"{playerUuid}.json");
        if (!File.Exists(statsPath))
        {
            return null;
        }

        try
        {
            var stats = await LoadStatisticsDataAsync(statsPath, playerUuid);
            if (stats != null)
            {
                _cache[playerUuid] = stats;
            }
            return stats;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load statistics for {playerUuid}: {ex.Message}");
            return null;
        }
    }

    public async Task<Dictionary<string, PlayerStatistics>> GetAllPlayerStatisticsAsync()
    {
        var result = new Dictionary<string, PlayerStatistics>();
        var statsDir = Path.Combine(_worldPath, "stats");

        if (!Directory.Exists(statsDir))
        {
            return result;
        }

        var files = Directory.GetFiles(statsDir, "*.json");
        foreach (var file in files)
        {
            var playerUuid = Path.GetFileNameWithoutExtension(file);
            var stats = await GetPlayerStatisticsAsync(playerUuid);
            if (stats != null)
            {
                result[playerUuid] = stats;
            }
        }

        return result;
    }

    public async Task<long> GetStatisticValueAsync(string playerUuid, string statistic)
    {
        var stats = await GetPlayerStatisticsAsync(playerUuid);
        if (stats == null)
        {
            return 0;
        }

        // 解析统计项路径
        var parts = statistic.Split(':');
        if (parts.Length < 2)
        {
            return 0;
        }

        var category = parts[0];
        var key = string.Join(':', parts.Skip(1));

        return category switch
        {
            "minecraft" when key.StartsWith("mined.") => 
                stats.MinedBlocks.GetValueOrDefault(key.Replace("mined.", ""), 0),
            "minecraft" when key.StartsWith("used.") => 
                stats.UsedItems.GetValueOrDefault(key.Replace("used.", ""), 0),
            "minecraft" when key.StartsWith("killed.") => 
                stats.KilledMobs.GetValueOrDefault(key.Replace("killed.", ""), 0),
            "minecraft" when key.StartsWith("killed_by.") => 
                stats.DeathsByMob.GetValueOrDefault(key.Replace("killed_by.", ""), 0),
            _ => stats.CustomStats.GetValueOrDefault(statistic, 0)
        };
    }

    public async Task<BlockCollectionProgress> GetBlockProgressAsync(string playerUuid)
    {
        var stats = await GetPlayerStatisticsAsync(playerUuid);
        if (stats == null)
        {
            return new BlockCollectionProgress
            {
                PlayerUuid = playerUuid,
                TotalBlocks = GetTotalBlockCount()
            };
        }

        var progress = new BlockCollectionProgress
        {
            PlayerUuid = playerUuid,
            TotalBlocks = GetTotalBlockCount()
        };

        // 从 MinedBlocks 和 UsedItems 中收集方块
        foreach (var block in stats.MinedBlocks.Keys)
        {
            if (IsCollectableBlock(block))
            {
                progress.CollectedBlocks.Add(block);
            }
        }

        foreach (var item in stats.UsedItems.Keys)
        {
            if (IsCollectableBlock(item))
            {
                progress.CollectedBlocks.Add(item);
            }
        }

        // 计算缺失的方块
        var allBlocks = GetAllBlocks();
        progress.MissingBlocks = allBlocks.Except(progress.CollectedBlocks).ToHashSet();
        progress.LastUpdated = DateTime.UtcNow;

        return progress;
    }

    public async Task StartTrackingAsync()
    {
        if (_isTracking)
        {
            return;
        }

        _isTracking = true;
        var statsDir = Path.Combine(_worldPath, "stats");

        if (!Directory.Exists(statsDir))
        {
            Directory.CreateDirectory(statsDir);
        }

        // 监听统计文件变化
        _fileWatcher.Watch(statsDir, "*.json", async (filePath, changeType) =>
        {
            if (changeType == WatcherChangeTypes.Changed || changeType == WatcherChangeTypes.Created)
            {
                var playerUuid = Path.GetFileNameWithoutExtension(filePath);
                await OnStatisticsFileChanged(playerUuid, filePath);
            }
        });

        _logger.Info("Statistics tracking started");
    }

    public Task StopTrackingAsync()
    {
        _isTracking = false;
        _cache.Clear();
        _logger.Info("Statistics tracking stopped");
        return Task.CompletedTask;
    }

    private async Task OnStatisticsFileChanged(string playerUuid, string filePath)
    {
        try
        {
            var oldStats = _cache.TryGetValue(playerUuid, out var cached) ? cached : null;
            var newStats = await LoadStatisticsDataAsync(filePath, playerUuid);

            if (newStats == null)
            {
                return;
            }

            _cache[playerUuid] = newStats;

            // 检测更新的统计项
            var updatedStats = new List<string>();
            if (oldStats != null)
            {
                foreach (var (key, value) in newStats.CustomStats)
                {
                    if (!oldStats.CustomStats.TryGetValue(key, out var oldValue) || oldValue != value)
                    {
                        updatedStats.Add(key);
                    }
                }
            }

            if (updatedStats.Count > 0)
            {
                StatisticsUpdated?.Invoke(this, new StatisticsUpdatedEventArgs
                {
                    PlayerUuid = playerUuid,
                    PlayerName = newStats.PlayerName,
                    UpdatedStatistics = updatedStats
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to process statistics file change for {playerUuid}: {ex.Message}");
        }
    }

    private async Task<PlayerStatistics?> LoadStatisticsDataAsync(string filePath, string playerUuid)
    {
        try
        {
            // 使用优化的文件读取
            var json = await SafeFileReader.ReadTextAsync(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var stats = new PlayerStatistics
            {
                PlayerUuid = playerUuid
            };

            if (root.TryGetProperty("stats", out var statsObj))
            {
                foreach (var category in statsObj.EnumerateObject())
                {
                    var categoryName = category.Name;
                    var categoryData = category.Value;

                    foreach (var stat in categoryData.EnumerateObject())
                    {
                        var statKey = stat.Name;
                        var statValue = stat.Value.GetInt64();

                        switch (categoryName)
                        {
                            case "minecraft:custom":
                                HandleCustomStat(stats, statKey, statValue);
                                break;
                            case "minecraft:mined":
                                stats.MinedBlocks[statKey] = statValue;
                                break;
                            case "minecraft:used":
                                stats.UsedItems[statKey] = statValue;
                                break;
                            case "minecraft:killed":
                                stats.KilledMobs[statKey] = statValue;
                                break;
                            case "minecraft:killed_by":
                                stats.DeathsByMob[statKey] = statValue;
                                break;
                        }

                        // 保存原始数据
                        stats.RawData[$"{categoryName}:{statKey}"] = statValue;
                    }
                }
            }

            stats.LastUpdated = DateTime.UtcNow;
            return stats;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load statistics from {filePath}: {ex.Message}");
            return null;
        }
    }

    private void HandleCustomStat(PlayerStatistics stats, string statKey, long value)
    {
        switch (statKey)
        {
            case "minecraft:play_time":
            case "minecraft:play_one_minute":
                stats.PlayTime = value;
                break;
            case "minecraft:walk_one_cm":
                stats.TravelDistances["walk"] = value;
                break;
            case "minecraft:sprint_one_cm":
                stats.TravelDistances["sprint"] = value;
                break;
            case "minecraft:fly_one_cm":
                stats.TravelDistances["fly"] = value;
                break;
            case "minecraft:boat_one_cm":
                stats.TravelDistances["boat"] = value;
                break;
            case "minecraft:swim_one_cm":
                stats.TravelDistances["swim"] = value;
                break;
            default:
                stats.CustomStats[statKey] = value;
                break;
        }
    }

    private int GetTotalBlockCount()
    {
        // Minecraft 1.21 有约 1000+ 个方块
        return 1200;
    }

    private bool IsCollectableBlock(string blockId)
    {
        // 检查是否是可收集的方块
        // 排除空气、虚空等不可收集的方块
        return !blockId.Contains("air") && !blockId.Contains("void");
    }

    private HashSet<string> GetAllBlocks()
    {
        // 应该从资源文件或数据包加载所有方块列表
        // 这里返回一个示例列表
        return new HashSet<string>
        {
            "minecraft:stone",
            "minecraft:dirt",
            "minecraft:grass_block",
            // ... 更多方块
        };
    }
}

