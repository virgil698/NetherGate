using System.Text.Json;
using NetherGate.API.FileSystem;
using NetherGate.API.Logging;

namespace NetherGate.Core.FileSystem;

/// <summary>
/// 服务器文件访问实现
/// 提供安全的服务器文件读写功能
/// </summary>
public class ServerFileAccess : IServerFileAccess
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;
    private readonly string _backupDirectory;

    public string ServerDirectory => _serverDirectory;

    public ServerFileAccess(string serverDirectory, ILogger logger)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
        _backupDirectory = Path.Combine(_serverDirectory, ".nethergate-backups");

        // 确保备份目录存在
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    public async Task<string> ReadTextFileAsync(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        ValidatePath(fullPath);

        try
        {
            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.Error($"读取文件失败: {relativePath}", ex);
            throw;
        }
    }

    public async Task WriteTextFileAsync(string relativePath, string content, bool backup = true)
    {
        var fullPath = GetFullPath(relativePath);
        ValidatePath(fullPath);

        try
        {
            // 备份原文件
            if (backup && File.Exists(fullPath))
            {
                await BackupFileAsync(fullPath);
            }

            // 确保目录存在
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);
            _logger.Debug($"文件已写入: {relativePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"写入文件失败: {relativePath}", ex);
            throw;
        }
    }

    public async Task<T?> ReadJsonFileAsync<T>(string relativePath) where T : class
    {
        var content = await ReadTextFileAsync(relativePath);
        try
        {
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (JsonException ex)
        {
            _logger.Error($"JSON 解析失败: {relativePath}", ex);
            throw;
        }
    }

    public async Task WriteJsonFileAsync<T>(string relativePath, T data, bool backup = true) where T : class
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var content = JsonSerializer.Serialize(data, options);
        await WriteTextFileAsync(relativePath, content, backup);
    }

    public async Task<Dictionary<string, string>> ReadServerPropertiesAsync()
    {
        var properties = new Dictionary<string, string>();
        var fullPath = GetFullPath("server.properties");

        if (!File.Exists(fullPath))
        {
            _logger.Warning("server.properties 文件不存在");
            return properties;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(fullPath);
            foreach (var line in lines)
            {
                // 跳过注释和空行
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    properties[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("读取 server.properties 失败", ex);
            throw;
        }

        return properties;
    }

    public async Task WriteServerPropertiesAsync(Dictionary<string, string> properties, bool backup = true)
    {
        var fullPath = GetFullPath("server.properties");

        try
        {
            // 备份原文件
            if (backup && File.Exists(fullPath))
            {
                await BackupFileAsync(fullPath);
            }

            // 读取原文件以保留注释
            var lines = new List<string>();
            if (File.Exists(fullPath))
            {
                lines.AddRange(await File.ReadAllLinesAsync(fullPath));
            }

            // 更新属性值
            var updatedLines = new List<string>();
            var processedKeys = new HashSet<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                {
                    updatedLines.Add(line);
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    if (properties.ContainsKey(key))
                    {
                        updatedLines.Add($"{key}={properties[key]}");
                        processedKeys.Add(key);
                    }
                    else
                    {
                        updatedLines.Add(line);
                    }
                }
            }

            // 添加新属性
            foreach (var (key, value) in properties)
            {
                if (!processedKeys.Contains(key))
                {
                    updatedLines.Add($"{key}={value}");
                }
            }

            await File.WriteAllLinesAsync(fullPath, updatedLines);
            _logger.Info("server.properties 已更新");
        }
        catch (Exception ex)
        {
            _logger.Error("写入 server.properties 失败", ex);
            throw;
        }
    }

    public bool FileExists(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        return File.Exists(fullPath);
    }

    public bool DirectoryExists(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        return Directory.Exists(fullPath);
    }

    public void CreateDirectory(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        ValidatePath(fullPath);

        try
        {
            Directory.CreateDirectory(fullPath);
            _logger.Debug($"目录已创建: {relativePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"创建目录失败: {relativePath}", ex);
            throw;
        }
    }

    public async Task DeleteFileAsync(string relativePath, bool backup = true)
    {
        var fullPath = GetFullPath(relativePath);
        ValidatePath(fullPath);

        if (!File.Exists(fullPath))
        {
            _logger.Warning($"文件不存在: {relativePath}");
            return;
        }

        try
        {
            if (backup)
            {
                await BackupFileAsync(fullPath);
            }

            File.Delete(fullPath);
            _logger.Info($"文件已删除: {relativePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"删除文件失败: {relativePath}", ex);
            throw;
        }
    }

    public List<string> ListFiles(string relativePath, string pattern = "*", bool recursive = false)
    {
        var fullPath = GetFullPath(relativePath);
        ValidatePath(fullPath);

        if (!Directory.Exists(fullPath))
        {
            return new List<string>();
        }

        try
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(fullPath, pattern, searchOption);

            // 转换为相对路径
            return files.Select(f => Path.GetRelativePath(_serverDirectory, f)).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"列出文件失败: {relativePath}", ex);
            throw;
        }
    }

    public FileInfo GetFileInfo(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        ValidatePath(fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }

        return new FileInfo(fullPath);
    }

    private string GetFullPath(string relativePath)
    {
        // 规范化路径
        var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar)
                                         .Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFullPath(Path.Combine(_serverDirectory, normalizedPath));
    }

    private void ValidatePath(string fullPath)
    {
        // 确保路径在服务器目录内（防止路径遍历攻击）
        if (!fullPath.StartsWith(_serverDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"禁止访问服务器目录外的文件: {fullPath}");
        }
    }

    private async Task BackupFileAsync(string fullPath)
    {
        try
        {
            var fileName = Path.GetFileName(fullPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupFileName = $"{fileName}.{timestamp}.bak";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            await Task.Run(() => File.Copy(fullPath, backupPath, overwrite: true));
            _logger.Trace($"文件已备份: {backupFileName}");
        }
        catch (Exception ex)
        {
            _logger.Warning($"备份文件失败: {ex.Message}");
            // 备份失败不抛出异常，继续执行
        }
    }
}

