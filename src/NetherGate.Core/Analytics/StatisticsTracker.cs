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
    private int? _cachedTotalBlockCount;
    private HashSet<string>? _cachedAllBlocks;

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

    public Task StartTrackingAsync()
    {
        if (_isTracking)
        {
            return Task.CompletedTask;
        }

        _isTracking = true;
        var statsDir = Path.Combine(_worldPath, "stats");

        if (!Directory.Exists(statsDir))
        {
            Directory.CreateDirectory(statsDir);
        }

        // 监听统计文件变化
        _fileWatcher.WatchDirectory(statsDir, "*.json", false, e =>
        {
            if (e.ChangeType == FileChangeType.Modified || e.ChangeType == FileChangeType.Created)
            {
                var playerUuid = Path.GetFileNameWithoutExtension(e.FilePath);
                _ = OnStatisticsFileChanged(playerUuid, e.FilePath);
            }
        });

        _logger.Info("Statistics tracking started");
        return Task.CompletedTask;
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
        if (_cachedTotalBlockCount.HasValue)
            return _cachedTotalBlockCount.Value;

        var allBlocks = GetAllBlocks();
        _cachedTotalBlockCount = allBlocks.Count;
        return _cachedTotalBlockCount.Value;
    }

    private bool IsCollectableBlock(string blockId)
    {
        // 检查是否是可收集的方块
        // 排除空气、虚空等不可收集的方块
        if (blockId.Contains("air") || blockId.Contains("void") || blockId.Contains("barrier"))
            return false;

        // 检查是否在已知方块列表中
        var allBlocks = GetAllBlocks();
        return allBlocks.Contains(blockId);
    }

    private HashSet<string> GetAllBlocks()
    {
        // 使用缓存
        if (_cachedAllBlocks != null)
            return _cachedAllBlocks;

        var blocks = new HashSet<string>();

        try
        {
            var serverDirectory = Path.GetFullPath(Path.Combine(_worldPath, ".."));
            
            // 方法1：从方块战利品表加载（最可靠的方法）
            var lootTablesPath = Path.Combine(serverDirectory, "data", "minecraft", "loot_tables", "blocks");
            if (Directory.Exists(lootTablesPath))
            {
                var lootTableFiles = Directory.GetFiles(lootTablesPath, "*.json", SearchOption.AllDirectories);
                foreach (var file in lootTableFiles)
                {
                    var relativePath = Path.GetRelativePath(lootTablesPath, file);
                    var blockId = "minecraft:" + Path.ChangeExtension(relativePath, null).Replace('\\', '/');
                    blocks.Add(blockId);
                }
                
                _logger.Debug($"从战利品表加载了 {blocks.Count} 个方块");
            }

            // 方法2：从方块标签补充（可能包含更多方块）
            var blockTagsPath = Path.Combine(serverDirectory, "data", "minecraft", "tags", "blocks");
            if (Directory.Exists(blockTagsPath))
            {
                try
                {
                    // 读取 mineable 标签组
                    var mineableCategories = new[] { "pickaxe", "axe", "shovel", "hoe" };
                    foreach (var category in mineableCategories)
                    {
                        var tagFile = Path.Combine(blockTagsPath, "mineable", $"{category}.json");
                        if (File.Exists(tagFile))
                        {
                            var json = File.ReadAllText(tagFile);
                            var tag = JsonSerializer.Deserialize<TagDefinition>(json);
                            if (tag?.Values != null)
                            {
                                foreach (var value in tag.Values)
                                {
                                    if (!value.StartsWith("#")) // 忽略标签引用
                                    {
                                        blocks.Add(value.Contains(":") ? value : $"minecraft:{value}");
                                    }
                                }
                            }
                        }
                    }
                    
                    _logger.Debug($"从方块标签补充后共 {blocks.Count} 个方块");
                }
                catch (Exception ex)
                {
                    _logger.Warning($"从方块标签加载失败: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"动态加载方块列表失败: {ex.Message}");
        }

        // 如果加载失败或数量太少，使用默认的常见方块列表
        if (blocks.Count < 100)
        {
            _logger.Warning($"动态加载的方块数量过少({blocks.Count})，使用默认列表");
            blocks = GetDefaultBlocks();
        }

        _cachedAllBlocks = blocks;
        return blocks;
    }

    private HashSet<string> GetDefaultBlocks()
    {
        // 默认的常见方块列表（作为降级方案）
        return new HashSet<string>
        {
            "minecraft:stone", "minecraft:granite", "minecraft:polished_granite",
            "minecraft:diorite", "minecraft:polished_diorite", "minecraft:andesite",
            "minecraft:polished_andesite", "minecraft:deepslate", "minecraft:cobbled_deepslate",
            "minecraft:grass_block", "minecraft:dirt", "minecraft:coarse_dirt",
            "minecraft:podzol", "minecraft:rooted_dirt", "minecraft:mud",
            "minecraft:cobblestone", "minecraft:oak_planks", "minecraft:spruce_planks",
            "minecraft:birch_planks", "minecraft:jungle_planks", "minecraft:acacia_planks",
            "minecraft:dark_oak_planks", "minecraft:mangrove_planks", "minecraft:cherry_planks",
            "minecraft:bedrock", "minecraft:sand", "minecraft:red_sand",
            "minecraft:gravel", "minecraft:coal_ore", "minecraft:deepslate_coal_ore",
            "minecraft:iron_ore", "minecraft:deepslate_iron_ore", "minecraft:copper_ore",
            "minecraft:deepslate_copper_ore", "minecraft:gold_ore", "minecraft:deepslate_gold_ore",
            "minecraft:redstone_ore", "minecraft:deepslate_redstone_ore",
            "minecraft:emerald_ore", "minecraft:deepslate_emerald_ore",
            "minecraft:lapis_ore", "minecraft:deepslate_lapis_ore",
            "minecraft:diamond_ore", "minecraft:deepslate_diamond_ore",
            "minecraft:nether_gold_ore", "minecraft:nether_quartz_ore"
            // 总计1200+方块（这里简化为默认值）
        };
    }

    /// <summary>
    /// 标签定义（用于JSON反序列化）
    /// </summary>
    private class TagDefinition
    {
        public List<string>? Values { get; set; }
        public bool Replace { get; set; }
    }
}

