using System.IO.Compression;
using NetherGate.API.FileSystem;
using NetherGate.API.Logging;

namespace NetherGate.Core.FileSystem;

/// <summary>
/// 备份管理器实现
/// </summary>
public class BackupManager : IBackupManager
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;
    private readonly string _backupDirectory;
    private System.Threading.Timer? _autoBackupTimer;
    private int _autoBackupMaxBackups;

    public string BackupDirectory => _backupDirectory;

    public BackupManager(string serverDirectory, ILogger logger, string? backupDirectory = null)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
        _backupDirectory = backupDirectory ?? Path.Combine(_serverDirectory, "backups");

        // 确保备份目录存在
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
            _logger.Info($"创建备份目录: {_backupDirectory}");
        }
    }

    public async Task<string> CreateBackupAsync(
        string? backupName = null,
        bool includeWorlds = true,
        bool includeConfigs = true,
        bool includePlugins = false)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var name = backupName ?? $"backup-{timestamp}";
        var backupFileName = $"{name}.zip";
        var backupPath = Path.Combine(_backupDirectory, backupFileName);

        _logger.Info($"开始创建备份: {backupFileName}");

        try
        {
            using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);

            // 备份世界文件
            if (includeWorlds)
            {
                await AddDirectoryToArchiveAsync(archive, "world", "world/");
                await AddDirectoryToArchiveAsync(archive, "world_nether", "world_nether/");
                await AddDirectoryToArchiveAsync(archive, "world_the_end", "world_the_end/");
            }

            // 备份配置文件
            if (includeConfigs)
            {
                AddFileToArchiveIfExists(archive, "server.properties");
                AddFileToArchiveIfExists(archive, "eula.txt");
                AddFileToArchiveIfExists(archive, "ops.json");
                AddFileToArchiveIfExists(archive, "whitelist.json");
                AddFileToArchiveIfExists(archive, "banned-players.json");
                AddFileToArchiveIfExists(archive, "banned-ips.json");
                AddFileToArchiveIfExists(archive, "permissions.yml");
                await AddDirectoryToArchiveAsync(archive, "config", "config/");
            }

            // 备份插件文件
            if (includePlugins)
            {
                await AddDirectoryToArchiveAsync(archive, "plugins", "plugins/");
            }

            var backupSize = new FileInfo(backupPath).Length;
            _logger.Info($"备份创建成功: {backupFileName} ({FormatFileSize(backupSize)})");

            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建备份失败: {backupFileName}", ex);

            // 清理失败的备份文件
            if (File.Exists(backupPath))
            {
                try
                {
                    File.Delete(backupPath);
                }
                catch
                {
                    // 忽略清理错误
                }
            }

            throw;
        }
    }

    public async Task<string> CreateWorldBackupAsync(string? worldName = null, string? backupName = null)
    {
        var world = worldName ?? "world";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var name = backupName ?? $"{world}-backup-{timestamp}";
        var backupFileName = $"{name}.zip";
        var backupPath = Path.Combine(_backupDirectory, backupFileName);

        _logger.Info($"开始创建世界备份: {world}");

        try
        {
            using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);
            await AddDirectoryToArchiveAsync(archive, world, $"{world}/");

            var backupSize = new FileInfo(backupPath).Length;
            _logger.Info($"世界备份创建成功: {backupFileName} ({FormatFileSize(backupSize)})");

            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建世界备份失败: {world}", ex);

            if (File.Exists(backupPath))
            {
                try
                {
                    File.Delete(backupPath);
                }
                catch
                {
                    // 忽略
                }
            }

            throw;
        }
    }

    public async Task RestoreBackupAsync(string backupPath, bool createBackupBeforeRestore = true)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException($"备份文件不存在: {backupPath}");
        }

        _logger.Warning($"开始恢复备份: {Path.GetFileName(backupPath)}");

        try
        {
            // 恢复前创建当前状态的备份
            if (createBackupBeforeRestore)
            {
                _logger.Info("恢复前创建当前状态备份...");
                await CreateBackupAsync("pre-restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            }

            // 解压备份文件
            ZipFile.ExtractToDirectory(backupPath, _serverDirectory, overwriteFiles: true);

            _logger.Info("备份恢复成功");
        }
        catch (Exception ex)
        {
            _logger.Error($"恢复备份失败: {backupPath}", ex);
            throw;
        }
    }

    public List<BackupInfo> ListBackups()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            return new List<BackupInfo>();
        }

        var backupFiles = Directory.GetFiles(_backupDirectory, "*.zip");
        var backups = new List<BackupInfo>();

        foreach (var file in backupFiles)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var backup = new BackupInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    CreatedAt = fileInfo.CreationTime,
                    Size = fileInfo.Length,
                    Type = DetermineBackupType(fileInfo.Name),
                    IncludesWorlds = true, // 需要打开 ZIP 才能精确判断
                    IncludesConfigs = true
                };

                backups.Add(backup);
            }
            catch (Exception ex)
            {
                _logger.Warning($"读取备份信息失败: {file} - {ex.Message}");
            }
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task DeleteBackupAsync(string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            _logger.Warning($"备份文件不存在: {backupPath}");
            return;
        }

        try
        {
            await Task.Run(() => File.Delete(backupPath));
            _logger.Info($"备份已删除: {Path.GetFileName(backupPath)}");
        }
        catch (Exception ex)
        {
            _logger.Error($"删除备份失败: {backupPath}", ex);
            throw;
        }
    }

    public async Task CleanupBackupsAsync(int keepCount = 10, int olderThanDays = 30)
    {
        var backups = ListBackups();
        var cutoffDate = DateTime.Now.AddDays(-olderThanDays);

        var toDelete = backups
            .Where(b => b.CreatedAt < cutoffDate || backups.IndexOf(b) >= keepCount)
            .ToList();

        if (toDelete.Count == 0)
        {
            _logger.Info("没有需要清理的旧备份");
            return;
        }

        _logger.Info($"清理 {toDelete.Count} 个旧备份...");

        foreach (var backup in toDelete)
        {
            try
            {
                await DeleteBackupAsync(backup.FilePath);
            }
            catch (Exception ex)
            {
                _logger.Warning($"清理备份失败: {backup.Name} - {ex.Message}");
            }
        }

        _logger.Info("备份清理完成");
    }

    public void EnableAutoBackup(int intervalMinutes = 60, int maxBackups = 10)
    {
        DisableAutoBackup(); // 先停止现有的自动备份

        _autoBackupMaxBackups = maxBackups;
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _autoBackupTimer = new System.Threading.Timer(async _ =>
        {
            try
            {
                _logger.Info("执行自动备份...");
                await CreateBackupAsync(
                    $"auto-{DateTime.Now:yyyyMMdd-HHmmss}",
                    includeWorlds: true,
                    includeConfigs: true,
                    includePlugins: false);

                // 清理旧的自动备份
                await CleanupBackupsAsync(keepCount: _autoBackupMaxBackups, olderThanDays: 7);
            }
            catch (Exception ex)
            {
                _logger.Error("自动备份失败", ex);
            }
        }, null, interval, interval);

        _logger.Info($"自动备份已启用: 间隔 {intervalMinutes} 分钟, 保留 {maxBackups} 个备份");
    }

    public void DisableAutoBackup()
    {
        if (_autoBackupTimer != null)
        {
            _autoBackupTimer.Dispose();
            _autoBackupTimer = null;
            _logger.Info("自动备份已禁用");
        }
    }

    private async Task AddDirectoryToArchiveAsync(ZipArchive archive, string directoryName, string entryPrefix)
    {
        var directoryPath = Path.Combine(_serverDirectory, directoryName);

        if (!Directory.Exists(directoryPath))
        {
            _logger.Trace($"目录不存在，跳过: {directoryName}");
            return;
        }

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // 跳过锁定文件
                if (file.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relativePath = Path.GetRelativePath(directoryPath, file);
                var entryName = entryPrefix + relativePath.Replace(Path.DirectorySeparatorChar, '/');

                try
                {
                    archive.CreateEntryFromFile(file, entryName);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"添加文件到备份失败: {file} - {ex.Message}");
                }
            }
        });

        _logger.Trace($"已添加到备份: {directoryName}");
    }

    private void AddFileToArchiveIfExists(ZipArchive archive, string fileName)
    {
        var filePath = Path.Combine(_serverDirectory, fileName);

        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            archive.CreateEntryFromFile(filePath, fileName);
            _logger.Trace($"已添加到备份: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.Warning($"添加文件到备份失败: {fileName} - {ex.Message}");
        }
    }

    private static BackupType DetermineBackupType(string fileName)
    {
        if (fileName.Contains("world", StringComparison.OrdinalIgnoreCase))
            return BackupType.World;
        if (fileName.Contains("auto", StringComparison.OrdinalIgnoreCase))
            return BackupType.Auto;
        if (fileName.Contains("config", StringComparison.OrdinalIgnoreCase))
            return BackupType.Config;

        return BackupType.Full;
    }

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

