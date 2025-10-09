using System.Collections.Concurrent;
using System.Text.Json;
using NetherGate.API.Leaderboard;
using NetherGate.API.Logging;
using LeaderboardModel = NetherGate.API.Leaderboard.Leaderboard;

namespace NetherGate.Core.Leaderboard;

/// <summary>
/// 排行榜系统实现
/// </summary>
public class LeaderboardSystem : ILeaderboardSystem
{
    private readonly ILogger _logger;
    private readonly string _dataPath;
    private readonly ConcurrentDictionary<string, LeaderboardModel> _leaderboards = new();

    public event EventHandler<ScoreUpdatedEventArgs>? ScoreUpdated;
    public event EventHandler<RankChangedEventArgs>? RankChanged;

    public LeaderboardSystem(ILogger logger, string dataPath)
    {
        _logger = logger;
        _dataPath = dataPath;

        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }
    }

    public async Task<LeaderboardModel> CreateLeaderboardAsync(string name, LeaderboardType type, string? displayName = null)
    {
        if (_leaderboards.ContainsKey(name))
        {
            throw new InvalidOperationException($"Leaderboard '{name}' already exists");
        }

        var leaderboard = new LeaderboardModel
        {
            Name = name,
            DisplayName = displayName ?? name,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _leaderboards[name] = leaderboard;
        await SaveLeaderboardAsync(leaderboard);
        _logger.Info($"Created leaderboard: {name} ({type})");

        return leaderboard;
    }

    public Task<bool> DeleteLeaderboardAsync(string name)
    {
        if (!_leaderboards.TryRemove(name, out _))
        {
            return Task.FromResult(false);
        }

        var filePath = GetLeaderboardFilePath(name);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        _logger.Info($"Deleted leaderboard: {name}");
        return Task.FromResult(true);
    }

    public Task<LeaderboardModel?> GetLeaderboardAsync(string name)
    {
        return Task.FromResult(_leaderboards.TryGetValue(name, out var leaderboard) ? leaderboard : null);
    }

    public Task<List<LeaderboardModel>> GetAllLeaderboardsAsync()
    {
        return Task.FromResult(_leaderboards.Values.ToList());
    }

    public async Task UpdateScoreAsync(string leaderboardName, string playerUuid, double score, string? playerName = null)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        if (leaderboard == null)
        {
            throw new InvalidOperationException($"Leaderboard '{leaderboardName}' not found");
        }

        var existingEntry = leaderboard.Entries.FirstOrDefault(e => e.PlayerUuid == playerUuid);
        var oldScore = existingEntry?.Score ?? 0;
        var oldRank = existingEntry?.Rank ?? 0;

        if (existingEntry != null)
        {
            existingEntry.Score = score;
            existingEntry.PlayerName = playerName ?? existingEntry.PlayerName;
            existingEntry.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            leaderboard.Entries.Add(new LeaderboardEntry
            {
                PlayerUuid = playerUuid,
                PlayerName = playerName,
                Score = score,
                Rank = 0, // 将在重新排序后更新
                LastUpdated = DateTime.UtcNow
            });
        }

        // 重新排序
        SortLeaderboard(leaderboard);
        leaderboard.LastUpdated = DateTime.UtcNow;
        await SaveLeaderboardAsync(leaderboard);

        // 触发事件
        ScoreUpdated?.Invoke(this, new ScoreUpdatedEventArgs
        {
            LeaderboardName = leaderboardName,
            PlayerUuid = playerUuid,
            PlayerName = playerName,
            OldScore = oldScore,
            NewScore = score
        });

        var newEntry = leaderboard.Entries.First(e => e.PlayerUuid == playerUuid);
        if (oldRank != newEntry.Rank)
        {
            RankChanged?.Invoke(this, new RankChangedEventArgs
            {
                LeaderboardName = leaderboardName,
                PlayerUuid = playerUuid,
                PlayerName = playerName,
                OldRank = oldRank,
                NewRank = newEntry.Rank
            });
        }
    }

    public async Task IncrementScoreAsync(string leaderboardName, string playerUuid, double increment, string? playerName = null)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        if (leaderboard == null)
        {
            throw new InvalidOperationException($"Leaderboard '{leaderboardName}' not found");
        }

        var existingEntry = leaderboard.Entries.FirstOrDefault(e => e.PlayerUuid == playerUuid);
        var currentScore = existingEntry?.Score ?? 0;
        var newScore = currentScore + increment;

        await UpdateScoreAsync(leaderboardName, playerUuid, newScore, playerName);
    }

    public async Task<double> GetPlayerScoreAsync(string leaderboardName, string playerUuid)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        var entry = leaderboard?.Entries.FirstOrDefault(e => e.PlayerUuid == playerUuid);
        return entry?.Score ?? 0;
    }

    public async Task<int> GetPlayerRankAsync(string leaderboardName, string playerUuid)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        var entry = leaderboard?.Entries.FirstOrDefault(e => e.PlayerUuid == playerUuid);
        return entry?.Rank ?? 0;
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(string leaderboardName, int count = 10)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        if (leaderboard == null)
        {
            return new List<LeaderboardEntry>();
        }

        return leaderboard.Entries.Take(count).ToList();
    }

    public async Task<List<LeaderboardEntry>> GetAllEntriesAsync(string leaderboardName)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        return leaderboard?.Entries ?? new List<LeaderboardEntry>();
    }

    public async Task ResetLeaderboardAsync(string leaderboardName)
    {
        var leaderboard = await GetLeaderboardAsync(leaderboardName);
        if (leaderboard == null)
        {
            throw new InvalidOperationException($"Leaderboard '{leaderboardName}' not found");
        }

        leaderboard.Entries.Clear();
        leaderboard.LastUpdated = DateTime.UtcNow;
        await SaveLeaderboardAsync(leaderboard);

        _logger.Info($"Reset leaderboard: {leaderboardName}");
    }

    public async Task LoadAllLeaderboardsAsync()
    {
        var files = Directory.GetFiles(_dataPath, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var leaderboard = JsonSerializer.Deserialize<LeaderboardModel>(json);
                if (leaderboard != null)
                {
                    _leaderboards[leaderboard.Name] = leaderboard;
                    _logger.Info($"Loaded leaderboard: {leaderboard.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load leaderboard from {file}: {ex.Message}");
            }
        }
    }

    private void SortLeaderboard(LeaderboardModel leaderboard)
    {
        // 根据类型排序
        switch (leaderboard.Type)
        {
            case LeaderboardType.HighestScore:
                leaderboard.Entries = leaderboard.Entries.OrderByDescending(e => e.Score).ToList();
                break;
            case LeaderboardType.LowestScore:
                leaderboard.Entries = leaderboard.Entries.OrderBy(e => e.Score).ToList();
                break;
            case LeaderboardType.Accumulative:
                leaderboard.Entries = leaderboard.Entries.OrderByDescending(e => e.Score).ToList();
                break;
        }

        // 更新排名
        for (int i = 0; i < leaderboard.Entries.Count; i++)
        {
            leaderboard.Entries[i].Rank = i + 1;
        }
    }

    private async Task SaveLeaderboardAsync(LeaderboardModel leaderboard)
    {
        var filePath = GetLeaderboardFilePath(leaderboard.Name);
        var json = JsonSerializer.Serialize(leaderboard, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(filePath, json);
    }

    private string GetLeaderboardFilePath(string name)
    {
        return Path.Combine(_dataPath, $"{name}.json");
    }
}

