using System.Collections.Concurrent;
using NetherGate.API.FileSystem;
using NetherGate.API.Logging;

namespace NetherGate.Core.FileSystem;

/// <summary>
/// 文件监听服务实现
/// </summary>
public class FileWatcher : IFileWatcher, IDisposable
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, WatcherInfo> _watchers;
    private int _nextWatcherId;

    public FileWatcher(string serverDirectory, ILogger logger)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
        _watchers = new ConcurrentDictionary<string, WatcherInfo>();
        _nextWatcherId = 1;
    }

    public string WatchFile(string filePath, Action<FileChangeEvent> callback)
    {
        var fullPath = GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException($"无效的文件路径: {filePath}");
        }

        var watcherId = GenerateWatcherId();
        var fsWatcher = new FileSystemWatcher(directory)
        {
            Filter = fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        SetupFileSystemWatcher(fsWatcher, callback);

        var watcherInfo = new WatcherInfo
        {
            Id = watcherId,
            Watcher = fsWatcher,
            Path = filePath,
            Type = WatcherType.File
        };

        _watchers[watcherId] = watcherInfo;
        _logger.Debug($"开始监听文件: {filePath} (ID: {watcherId})");

        return watcherId;
    }

    public string WatchDirectory(string directoryPath, string pattern, bool includeSubdirectories, 
                                 Action<FileChangeEvent> callback)
    {
        var fullPath = GetFullPath(directoryPath);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"目录不存在: {directoryPath}");
        }

        var watcherId = GenerateWatcherId();
        var fsWatcher = new FileSystemWatcher(fullPath)
        {
            Filter = pattern,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            IncludeSubdirectories = includeSubdirectories,
            EnableRaisingEvents = true
        };

        SetupFileSystemWatcher(fsWatcher, callback);

        var watcherInfo = new WatcherInfo
        {
            Id = watcherId,
            Watcher = fsWatcher,
            Path = directoryPath,
            Type = WatcherType.Directory
        };

        _watchers[watcherId] = watcherInfo;
        _logger.Debug($"开始监听目录: {directoryPath} (模式: {pattern}, ID: {watcherId})");

        return watcherId;
    }

    public void StopWatching(string watcherId)
    {
        if (_watchers.TryRemove(watcherId, out var watcherInfo))
        {
            watcherInfo.Watcher.EnableRaisingEvents = false;
            watcherInfo.Watcher.Dispose();
            _logger.Debug($"停止监听: {watcherInfo.Path} (ID: {watcherId})");
        }
    }

    public void StopAll()
    {
        foreach (var watcherId in _watchers.Keys.ToList())
        {
            StopWatching(watcherId);
        }
        _logger.Info("已停止所有文件监听");
    }

    public void Dispose()
    {
        StopAll();
    }

    private void SetupFileSystemWatcher(FileSystemWatcher fsWatcher, Action<FileChangeEvent> callback)
    {
        // 防抖动 - 避免同一文件的多次快速事件触发
        var lastEvents = new ConcurrentDictionary<string, DateTime>();
        var debounceInterval = TimeSpan.FromMilliseconds(100);

        Action<FileChangeType, string, string?> handleChange = (changeType, fullPath, oldPath) =>
        {
            var relativePath = Path.GetRelativePath(_serverDirectory, fullPath);
            var now = DateTime.Now;

            // 防抖动检查
            if (lastEvents.TryGetValue(fullPath, out var lastTime))
            {
                if (now - lastTime < debounceInterval)
                {
                    return; // 忽略重复事件
                }
            }
            lastEvents[fullPath] = now;

            try
            {
                var changeEvent = new FileChangeEvent
                {
                    ChangeType = changeType,
                    FilePath = relativePath,
                    OldFilePath = oldPath != null ? Path.GetRelativePath(_serverDirectory, oldPath) : null,
                    Timestamp = now
                };

                callback(changeEvent);
            }
            catch (Exception ex)
            {
                _logger.Error($"文件变更回调执行失败: {relativePath}", ex);
            }
        };

        fsWatcher.Created += (sender, e) => handleChange(FileChangeType.Created, e.FullPath, null);
        fsWatcher.Changed += (sender, e) => handleChange(FileChangeType.Modified, e.FullPath, null);
        fsWatcher.Deleted += (sender, e) => handleChange(FileChangeType.Deleted, e.FullPath, null);
        fsWatcher.Renamed += (sender, e) => handleChange(FileChangeType.Renamed, e.FullPath, e.OldFullPath);

        fsWatcher.Error += (sender, e) =>
        {
            _logger.Error($"文件监听器错误: {e.GetException()?.Message}", e.GetException());
        };
    }

    private string GetFullPath(string relativePath)
    {
        var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar)
                                         .Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_serverDirectory, normalizedPath));
    }

    private string GenerateWatcherId()
    {
        return $"watcher-{Interlocked.Increment(ref _nextWatcherId)}";
    }

    private class WatcherInfo
    {
        public string Id { get; init; } = string.Empty;
        public FileSystemWatcher Watcher { get; init; } = null!;
        public string Path { get; init; } = string.Empty;
        public WatcherType Type { get; init; }
    }

    private enum WatcherType
    {
        File,
        Directory
    }
}

