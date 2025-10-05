namespace NetherGate.API.FileSystem;

/// <summary>
/// 备份管理接口
/// 提供自动备份和恢复功能
/// </summary>
public interface IBackupManager
{
    /// <summary>
    /// 创建完整备份
    /// </summary>
    /// <param name="backupName">备份名称（可选，默认使用时间戳）</param>
    /// <param name="includeWorlds">是否包含世界文件</param>
    /// <param name="includeConfigs">是否包含配置文件</param>
    /// <param name="includePlugins">是否包含插件文件</param>
    /// <returns>备份文件路径</returns>
    Task<string> CreateBackupAsync(
        string? backupName = null,
        bool includeWorlds = true,
        bool includeConfigs = true,
        bool includePlugins = false);

    /// <summary>
    /// 创建世界备份
    /// </summary>
    /// <param name="worldName">世界名称（默认为主世界）</param>
    /// <param name="backupName">备份名称（可选）</param>
    /// <returns>备份文件路径</returns>
    Task<string> CreateWorldBackupAsync(string? worldName = null, string? backupName = null);

    /// <summary>
    /// 恢复备份
    /// </summary>
    /// <param name="backupPath">备份文件路径</param>
    /// <param name="createBackupBeforeRestore">恢复前是否创建当前状态的备份</param>
    Task RestoreBackupAsync(string backupPath, bool createBackupBeforeRestore = true);

    /// <summary>
    /// 列出所有备份
    /// </summary>
    /// <returns>备份信息列表</returns>
    List<BackupInfo> ListBackups();

    /// <summary>
    /// 删除备份
    /// </summary>
    /// <param name="backupPath">备份文件路径</param>
    Task DeleteBackupAsync(string backupPath);

    /// <summary>
    /// 清理旧备份
    /// </summary>
    /// <param name="keepCount">保留最新的备份数量</param>
    /// <param name="olderThanDays">删除超过指定天数的备份</param>
    Task CleanupBackupsAsync(int keepCount = 10, int olderThanDays = 30);

    /// <summary>
    /// 启用自动备份
    /// </summary>
    /// <param name="intervalMinutes">备份间隔（分钟）</param>
    /// <param name="maxBackups">最大备份数量</param>
    void EnableAutoBackup(int intervalMinutes = 60, int maxBackups = 10);

    /// <summary>
    /// 禁用自动备份
    /// </summary>
    void DisableAutoBackup();

    /// <summary>
    /// 获取备份目录
    /// </summary>
    string BackupDirectory { get; }
}

/// <summary>
/// 备份信息
/// </summary>
public class BackupInfo
{
    /// <summary>
    /// 备份名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 备份文件路径
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// 备份类型
    /// </summary>
    public BackupType Type { get; init; }

    /// <summary>
    /// 是否包含世界文件
    /// </summary>
    public bool IncludesWorlds { get; init; }

    /// <summary>
    /// 是否包含配置文件
    /// </summary>
    public bool IncludesConfigs { get; init; }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FormattedSize => FormatFileSize(Size);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 备份类型
/// </summary>
public enum BackupType
{
    /// <summary>
    /// 完整备份
    /// </summary>
    Full,

    /// <summary>
    /// 世界备份
    /// </summary>
    World,

    /// <summary>
    /// 配置备份
    /// </summary>
    Config,

    /// <summary>
    /// 自动备份
    /// </summary>
    Auto
}

