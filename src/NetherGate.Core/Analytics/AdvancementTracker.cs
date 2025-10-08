using System.Collections.Concurrent;
using System.Text.Json;
using NetherGate.API.Analytics;
using NetherGate.API.FileSystem;
using NetherGate.API.Logging;

namespace NetherGate.Core.Analytics;

/// <summary>
/// 成就追踪器实现
/// </summary>
public class AdvancementTracker : IAdvancementTracker
{
    private readonly ILogger _logger;
    private readonly string _worldPath;
    private readonly IFileWatcher _fileWatcher;
    private readonly ConcurrentDictionary<string, PlayerAdvancementData> _cache = new();
    private bool _isTracking;

    public event EventHandler<AdvancementCompletedEventArgs>? AdvancementCompleted;

    public AdvancementTracker(ILogger logger, string worldPath, IFileWatcher fileWatcher)
    {
        _logger = logger;
        _worldPath = worldPath;
        _fileWatcher = fileWatcher;
    }

    public async Task<PlayerAdvancementData?> GetPlayerAdvancementsAsync(string playerUuid)
    {
        if (_cache.TryGetValue(playerUuid, out var cached))
        {
            return cached;
        }

        var advancementPath = Path.Combine(_worldPath, "advancements", $"{playerUuid}.json");
        if (!File.Exists(advancementPath))
        {
            return null;
        }

        try
        {
            var data = await LoadAdvancementDataAsync(advancementPath, playerUuid);
            if (data != null)
            {
                _cache[playerUuid] = data;
            }
            return data;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load advancement data for {playerUuid}: {ex.Message}");
            return null;
        }
    }

    public async Task<Dictionary<string, PlayerAdvancementData>> GetAllPlayerAdvancementsAsync()
    {
        var result = new Dictionary<string, PlayerAdvancementData>();
        var advancementDir = Path.Combine(_worldPath, "advancements");

        if (!Directory.Exists(advancementDir))
        {
            return result;
        }

        var files = Directory.GetFiles(advancementDir, "*.json");
        foreach (var file in files)
        {
            var playerUuid = Path.GetFileNameWithoutExtension(file);
            var data = await GetPlayerAdvancementsAsync(playerUuid);
            if (data != null)
            {
                result[playerUuid] = data;
            }
        }

        return result;
    }

    public async Task<bool> HasCompletedAdvancementAsync(string playerUuid, string advancementId)
    {
        var data = await GetPlayerAdvancementsAsync(playerUuid);
        return data?.CompletedAdvancements.ContainsKey(advancementId) ?? false;
    }

    public async Task<double> GetAdvancementProgressAsync(string playerUuid, string? category = null)
    {
        var data = await GetPlayerAdvancementsAsync(playerUuid);
        if (data == null)
        {
            return 0;
        }

        if (string.IsNullOrEmpty(category))
        {
            return data.ProgressPercentage;
        }

        // 按类别过滤
        var categoryAdvancements = data.CompletedAdvancements
            .Where(kvp => kvp.Key.StartsWith($"minecraft:{category}/"))
            .Count();

        var totalCategoryAdvancements = GetTotalAdvancementsForCategory(category);
        return totalCategoryAdvancements > 0 
            ? (double)categoryAdvancements / totalCategoryAdvancements * 100 
            : 0;
    }

    public async Task StartTrackingAsync()
    {
        if (_isTracking)
        {
            return;
        }

        _isTracking = true;
        var advancementDir = Path.Combine(_worldPath, "advancements");

        if (!Directory.Exists(advancementDir))
        {
            Directory.CreateDirectory(advancementDir);
        }

        // 监听成就文件变化
        _fileWatcher.Watch(advancementDir, "*.json", async (filePath, changeType) =>
        {
            if (changeType == WatcherChangeTypes.Changed || changeType == WatcherChangeTypes.Created)
            {
                var playerUuid = Path.GetFileNameWithoutExtension(filePath);
                await OnAdvancementFileChanged(playerUuid, filePath);
            }
        });

        _logger.Info("Advancement tracking started");
    }

    public Task StopTrackingAsync()
    {
        _isTracking = false;
        _cache.Clear();
        _logger.Info("Advancement tracking stopped");
        return Task.CompletedTask;
    }

    private async Task OnAdvancementFileChanged(string playerUuid, string filePath)
    {
        try
        {
            var oldData = _cache.TryGetValue(playerUuid, out var cached) ? cached : null;
            var newData = await LoadAdvancementDataAsync(filePath, playerUuid);

            if (newData == null)
            {
                return;
            }

            _cache[playerUuid] = newData;

            // 检测新完成的成就
            if (oldData != null)
            {
                foreach (var (advId, completedTime) in newData.CompletedAdvancements)
                {
                    if (!oldData.CompletedAdvancements.ContainsKey(advId))
                    {
                        // 触发成就完成事件
                        AdvancementCompleted?.Invoke(this, new AdvancementCompletedEventArgs
                        {
                            PlayerUuid = playerUuid,
                            PlayerName = newData.PlayerName,
                            AdvancementId = advId,
                            AdvancementName = GetAdvancementDisplayName(advId),
                            CompletedAt = completedTime
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to process advancement file change for {playerUuid}: {ex.Message}");
        }
    }

    private async Task<PlayerAdvancementData?> LoadAdvancementDataAsync(string filePath, string playerUuid)
    {
        try
        {
            // 使用优化的文件读取
            var json = await SafeFileReader.ReadTextAsync(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var data = new PlayerAdvancementData
            {
                PlayerUuid = playerUuid,
                CompletedAdvancements = new Dictionary<string, DateTime>(),
                InProgressAdvancements = new Dictionary<string, Dictionary<string, bool>>()
            };

            foreach (var property in root.EnumerateObject())
            {
                var advancementId = property.Name;
                var advData = property.Value;

                if (advData.TryGetProperty("done", out var done) && done.GetBoolean())
                {
                    // 已完成的成就
                    var completedTime = DateTime.UtcNow;
                    if (advData.TryGetProperty("done_time", out var doneTime))
                    {
                        completedTime = DateTimeOffset.FromUnixTimeMilliseconds(doneTime.GetInt64()).UtcDateTime;
                    }
                    data.CompletedAdvancements[advancementId] = completedTime;
                }
                else if (advData.TryGetProperty("criteria", out var criteria))
                {
                    // 进行中的成就
                    var criteriaDict = new Dictionary<string, bool>();
                    foreach (var criterion in criteria.EnumerateObject())
                    {
                        criteriaDict[criterion.Name] = criterion.Value.ValueKind != JsonValueKind.Null;
                    }
                    data.InProgressAdvancements[advancementId] = criteriaDict;
                }
            }

            data.TotalAdvancements = GetTotalAdvancements();
            data.LastUpdated = DateTime.UtcNow;

            return data;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load advancement data from {filePath}: {ex.Message}");
            return null;
        }
    }

    private int GetTotalAdvancements()
    {
        // Minecraft 1.21 大约有 120+ 个成就
        // 这个数字应该根据版本动态获取
        return 122;
    }

    private int GetTotalAdvancementsForCategory(string category)
    {
        // 各类别的成就数量（示例数据，应该根据实际版本调整）
        return category.ToLower() switch
        {
            "story" => 12,
            "nether" => 9,
            "end" => 5,
            "adventure" => 19,
            "husbandry" => 10,
            _ => 0
        };
    }

    private string GetAdvancementDisplayName(string advancementId)
    {
        // 可以从语言文件加载显示名称
        // 这里简化处理，返回 ID 的最后一部分
        var parts = advancementId.Split('/');
        return parts.Length > 0 ? parts[^1].Replace('_', ' ') : advancementId;
    }
}

