namespace NetherGate.API.FileSystem;

/// <summary>
/// 文件监听服务接口
/// 用于监听服务器文件变更
/// </summary>
public interface IFileWatcher
{
    /// <summary>
    /// 监听指定文件
    /// </summary>
    /// <param name="filePath">文件路径（相对于服务器目录）</param>
    /// <param name="callback">文件变更回调</param>
    /// <returns>监听器 ID</returns>
    string WatchFile(string filePath, Action<FileChangeEvent> callback);

    /// <summary>
    /// 监听指定目录
    /// </summary>
    /// <param name="directoryPath">目录路径（相对于服务器目录）</param>
    /// <param name="pattern">文件匹配模式（如 "*.json"）</param>
    /// <param name="includeSubdirectories">是否包含子目录</param>
    /// <param name="callback">文件变更回调</param>
    /// <returns>监听器 ID</returns>
    string WatchDirectory(string directoryPath, string pattern, bool includeSubdirectories, Action<FileChangeEvent> callback);

    /// <summary>
    /// 停止监听
    /// </summary>
    /// <param name="watcherId">监听器 ID</param>
    void StopWatching(string watcherId);

    /// <summary>
    /// 停止所有监听
    /// </summary>
    void StopAll();
}

/// <summary>
/// 文件变更事件
/// </summary>
public class FileChangeEvent
{
    /// <summary>
    /// 变更类型
    /// </summary>
    public FileChangeType ChangeType { get; init; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 旧文件路径（仅重命名时有效）
    /// </summary>
    public string? OldFilePath { get; init; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// 文件变更类型
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// 创建
    /// </summary>
    Created,

    /// <summary>
    /// 修改
    /// </summary>
    Modified,

    /// <summary>
    /// 删除
    /// </summary>
    Deleted,

    /// <summary>
    /// 重命名
    /// </summary>
    Renamed
}

