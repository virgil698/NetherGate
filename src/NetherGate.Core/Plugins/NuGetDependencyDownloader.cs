using System.IO.Compression;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NetherGate.Core.Plugins;

/// <summary>
/// NuGet 依赖下载器
/// 负责从 NuGet 源自动下载插件所需的依赖
/// </summary>
public class NuGetDependencyDownloader
{
    private readonly NetherGate.API.Logging.ILogger _logger;
    private readonly string _libPath;
    private readonly List<string> _nugetSources;
    private readonly int _timeoutSeconds;
    private readonly bool _verifyHash;
    
    private readonly SourceCacheContext _cacheContext;
    private readonly List<SourceRepository> _repositories;

    public NuGetDependencyDownloader(
        string libPath,
        NetherGate.API.Logging.ILogger logger,
        List<string> nugetSources,
        int timeoutSeconds = 60,
        bool verifyHash = true)
    {
        _libPath = libPath;
        _logger = logger;
        _nugetSources = nugetSources;
        _timeoutSeconds = timeoutSeconds;
        _verifyHash = verifyHash;

        // 初始化 NuGet 组件
        _cacheContext = new SourceCacheContext();
        _repositories = new List<SourceRepository>();
        
        foreach (var source in nugetSources)
        {
            var packageSource = new PackageSource(source);
            var repository = Repository.Factory.GetCoreV3(packageSource);
            _repositories.Add(repository);
        }
    }

    /// <summary>
    /// 检查依赖是否已存在
    /// </summary>
    public bool DependencyExists(LibraryDependency dependency)
    {
        var dllPath = Path.Combine(_libPath, $"{dependency.Name}.dll");
        return File.Exists(dllPath);
    }

    /// <summary>
    /// 下载依赖
    /// </summary>
    public async Task<bool> DownloadDependencyAsync(LibraryDependency dependency)
    {
        if (DependencyExists(dependency))
        {
            _logger.Debug($"依赖已存在: {dependency.Name}");
            return true;
        }

        _logger.Info($"正在下载 NuGet 包: {dependency.Name} {dependency.Version ?? "latest"}");

        try
        {
            // 1. 解析版本
            var versionRange = ParseVersionRange(dependency.Version);
            
            // 2. 从多个源尝试下载
            foreach (var repository in _repositories)
            {
                try
                {
                    var success = await TryDownloadFromRepositoryAsync(
                        repository, 
                        dependency.Name, 
                        versionRange);
                    
                    if (success)
                    {
                        _logger.Info($"✓ 下载成功: {dependency.Name}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"从 {repository.PackageSource.Source} 下载失败: {ex.Message}");
                }
            }

            _logger.Error($"✗ 所有源都无法下载依赖: {dependency.Name}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"下载依赖失败: {dependency.Name}", ex);
            return false;
        }
    }

    /// <summary>
    /// 批量下载依赖
    /// </summary>
    public async Task<Dictionary<string, bool>> DownloadDependenciesAsync(
        List<LibraryDependency> dependencies)
    {
        var results = new Dictionary<string, bool>();

        foreach (var dependency in dependencies)
        {
            if (dependency.Optional)
            {
                _logger.Debug($"跳过可选依赖: {dependency.Name}");
                results[dependency.Name] = true;
                continue;
            }

            var success = await DownloadDependencyAsync(dependency);
            results[dependency.Name] = success;
        }

        return results;
    }

    /// <summary>
    /// 从指定仓库下载包
    /// </summary>
    private async Task<bool> TryDownloadFromRepositoryAsync(
        SourceRepository repository,
        string packageId,
        VersionRange versionRange)
    {
        _logger.Debug($"尝试从 {repository.PackageSource.Source} 下载 {packageId}");

        // 1. 查找包
        var findResource = await repository.GetResourceAsync<FindPackageByIdResource>();
        if (findResource == null)
        {
            _logger.Warning("无法获取 FindPackageByIdResource");
            return false;
        }

        // 2. 获取所有版本
        var versions = await findResource.GetAllVersionsAsync(
            packageId,
            _cacheContext,
            NullLogger.Instance,
            CancellationToken.None);

        if (versions == null || !versions.Any())
        {
            _logger.Debug($"未找到包: {packageId}");
            return false;
        }

        // 3. 选择合适的版本
        var selectedVersion = SelectVersion(versions.ToList(), versionRange);
        if (selectedVersion == null)
        {
            _logger.Warning($"未找到符合版本要求的包: {packageId} {versionRange}");
            return false;
        }

        _logger.Debug($"选择版本: {selectedVersion}");

        // 4. 下载包
        var tempPath = Path.Combine(Path.GetTempPath(), $"{packageId}.{selectedVersion}.nupkg");
        
        using (var packageStream = File.Create(tempPath))
        {
            var downloaded = await findResource.CopyNupkgToStreamAsync(
                packageId,
                selectedVersion,
                packageStream,
                _cacheContext,
                NullLogger.Instance,
                CancellationToken.None);

            if (!downloaded)
            {
                _logger.Warning("下载包失败");
                return false;
            }
        }

        // 5. 解压并提取 DLL
        var extractResult = ExtractDllsFromPackage(tempPath, packageId);

        // 6. 清理临时文件
        try
        {
            File.Delete(tempPath);
        }
        catch { }

        return extractResult;
    }

    /// <summary>
    /// 从 NuGet 包中提取 DLL
    /// </summary>
    private bool ExtractDllsFromPackage(string packagePath, string packageId)
    {
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            
            // 查找合适的 DLL（优先 net9.0, net8.0, netstandard2.1, netstandard2.0）
            var targetFrameworks = new[] { 
                "net9.0", "net8.0", "net7.0", "net6.0", 
                "netstandard2.1", "netstandard2.0", 
                "netcoreapp3.1", "netcoreapp3.0" 
            };

            ZipArchiveEntry? selectedEntry = null;
            
            foreach (var framework in targetFrameworks)
            {
                var entry = archive.Entries.FirstOrDefault(e => 
                    e.FullName.StartsWith($"lib/{framework}/", StringComparison.OrdinalIgnoreCase) &&
                    e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileNameWithoutExtension(e.Name).Equals(packageId, StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    selectedEntry = entry;
                    _logger.Debug($"找到 DLL: {entry.FullName}");
                    break;
                }
            }

            // 如果没找到，尝试查找任意 lib/ 下的 DLL
            if (selectedEntry == null)
            {
                selectedEntry = archive.Entries.FirstOrDefault(e =>
                    e.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
                    e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileNameWithoutExtension(e.Name).Equals(packageId, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedEntry == null)
            {
                _logger.Warning($"NuGet 包中未找到主 DLL: {packageId}");
                return false;
            }

            // 确保目标目录存在
            if (!Directory.Exists(_libPath))
            {
                Directory.CreateDirectory(_libPath);
            }

            // 提取 DLL
            var targetPath = Path.Combine(_libPath, $"{packageId}.dll");
            selectedEntry.ExtractToFile(targetPath, overwrite: true);
            
            _logger.Debug($"提取 DLL 到: {targetPath}");

            // 提取依赖的 DLL（可选）
            ExtractDependentDlls(archive, targetFrameworks);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"提取 DLL 失败: {packageId}", ex);
            return false;
        }
    }

    /// <summary>
    /// 提取包中的依赖 DLL
    /// </summary>
    private void ExtractDependentDlls(ZipArchive archive, string[] targetFrameworks)
    {
        try
        {
            foreach (var framework in targetFrameworks)
            {
                var entries = archive.Entries.Where(e =>
                    e.FullName.StartsWith($"lib/{framework}/", StringComparison.OrdinalIgnoreCase) &&
                    e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                foreach (var entry in entries)
                {
                    var fileName = Path.GetFileName(entry.Name);
                    var targetPath = Path.Combine(_libPath, fileName);
                    
                    // 如果已存在，跳过
                    if (File.Exists(targetPath))
                        continue;

                    try
                    {
                        entry.ExtractToFile(targetPath, overwrite: false);
                        _logger.Trace($"提取依赖 DLL: {fileName}");
                    }
                    catch
                    {
                        // 忽略提取失败的依赖
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"提取依赖 DLL 时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析版本范围
    /// </summary>
    private VersionRange ParseVersionRange(string? versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            // 未指定版本，使用任意最新版本
            return VersionRange.All;
        }

        try
        {
            // 支持格式：
            // - "13.0.3" -> 精确版本
            // - ">=13.0.0" -> 最低版本
            // - "[13.0.0, 14.0.0)" -> 版本范围
            return VersionRange.Parse(versionString);
        }
        catch
        {
            _logger.Warning($"无法解析版本字符串: {versionString}，使用任意版本");
            return VersionRange.All;
        }
    }

    /// <summary>
    /// 选择合适的版本
    /// </summary>
    private NuGetVersion? SelectVersion(List<NuGetVersion> versions, VersionRange versionRange)
    {
        // 筛选符合要求的版本
        var matchingVersions = versions
            .Where(v => versionRange.Satisfies(v))
            .OrderByDescending(v => v)
            .ToList();

        if (matchingVersions.Count == 0)
            return null;

        // 返回最新的稳定版本（如果有），否则返回最新版本
        var stableVersion = matchingVersions.FirstOrDefault(v => !v.IsPrerelease);
        return stableVersion ?? matchingVersions.First();
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _cacheContext?.Dispose();
    }
}
