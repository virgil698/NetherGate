using System.IO.Compression;
using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Script;

/// <summary>
/// JavaScript 插件加载器
/// 负责扫描和加载 JavaScript 插件（支持目录和 .ngplugin 文件）
/// </summary>
public class JavaScriptPluginLoader
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JavaScriptRuntime _jsRuntime;
    private readonly string _tempDirectory;

    public JavaScriptPluginLoader(
        ILogger logger,
        IServiceProvider serviceProvider,
        JavaScriptRuntime jsRuntime,
        string pluginsDirectory)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _jsRuntime = jsRuntime;
        _tempDirectory = Path.Combine(pluginsDirectory, ".temp");

        // 确保临时目录存在
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
            _logger.Debug($"创建临时目录: {_tempDirectory}");
        }
    }

    /// <summary>
    /// 加载 JavaScript 插件
    /// </summary>
    public IPlugin? LoadJavaScriptPlugin(string pluginPath, PluginMetadata metadata)
    {
        try
        {
            _logger.Info($"加载 JavaScript 插件: {metadata.Name}");

            // 1. 判断是目录还是 .ngplugin 文件
            string pluginDirectory;

            if (File.Exists(pluginPath) && pluginPath.EndsWith(".ngplugin", StringComparison.OrdinalIgnoreCase))
            {
                // 解压 .ngplugin 文件
                pluginDirectory = ExtractNgPlugin(pluginPath, metadata.Id);
                _logger.Debug($"解压打包插件到: {pluginDirectory}");
            }
            else if (Directory.Exists(pluginPath))
            {
                // 直接使用目录
                pluginDirectory = pluginPath;
                _logger.Debug($"使用插件目录: {pluginDirectory}");
            }
            else
            {
                _logger.Error($"无效的插件路径: {pluginPath}");
                return null;
            }

            // 2. 读取主文件
            var mainFilePath = Path.Combine(pluginDirectory, metadata.Main);
            if (!File.Exists(mainFilePath))
            {
                _logger.Error($"找不到主文件: {mainFilePath}");
                return null;
            }

            var mainFileContent = File.ReadAllText(mainFilePath);
            _logger.Debug($"已读取主文件: {mainFilePath} ({mainFileContent.Length} 字符)");

            // 3. 创建适配器
            var adapter = new JavaScriptPluginAdapter(
                pluginDirectory,
                mainFileContent,
                mainFilePath,
                _serviceProvider,
                _logger,
                _jsRuntime
            );

            _logger.Info($"JavaScript 插件加载成功: {adapter.Info.Name} v{adapter.Info.Version}");
            return adapter;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载 JavaScript 插件失败: {metadata.Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// 解压 .ngplugin 文件
    /// </summary>
    private string ExtractNgPlugin(string ngPluginPath, string pluginId)
    {
        var extractPath = Path.Combine(_tempDirectory, pluginId);

        try
        {
            // 如果已存在，先删除
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
                _logger.Debug($"清理旧的临时目录: {extractPath}");
            }

            // 解压
            ZipFile.ExtractToDirectory(ngPluginPath, extractPath);
            _logger.Debug($"已解压 .ngplugin 文件到: {extractPath}");

            return extractPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"解压 .ngplugin 文件失败: {ngPluginPath}", ex);
            throw;
        }
    }

    /// <summary>
    /// 检查路径是否是 JavaScript 插件
    /// </summary>
    public static bool IsJavaScriptPlugin(string pluginPath, ILogger logger, out PluginMetadata? metadata)
    {
        metadata = null;

        try
        {
            // 检查 .ngplugin 文件
            if (File.Exists(pluginPath) && pluginPath.EndsWith(".ngplugin", StringComparison.OrdinalIgnoreCase))
            {
                return IsJavaScriptPluginFromZip(pluginPath, logger, out metadata);
            }
            // 检查目录
            else if (Directory.Exists(pluginPath))
            {
                return IsJavaScriptPluginFromDirectory(pluginPath, logger, out metadata);
            }
        }
        catch (Exception ex)
        {
            logger.Debug($"检查 JavaScript 插件时出错: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 从 ZIP 文件检查是否是 JavaScript 插件
    /// </summary>
    private static bool IsJavaScriptPluginFromZip(string zipPath, ILogger logger, out PluginMetadata? metadata)
    {
        metadata = null;

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var pluginJsonEntry = archive.GetEntry("resource/plugin.json");

            if (pluginJsonEntry == null)
            {
                return false;
            }

            using var stream = pluginJsonEntry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            metadata = JsonSerializer.Deserialize<PluginMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return metadata?.Type?.Equals("javascript", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception ex)
        {
            logger.Debug($"读取 ZIP 中的 plugin.json 失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从目录检查是否是 JavaScript 插件
    /// </summary>
    private static bool IsJavaScriptPluginFromDirectory(string pluginDirectory, ILogger logger, out PluginMetadata? metadata)
    {
        metadata = null;

        try
        {
            var metadataPath = Path.Combine(pluginDirectory, "resource", "plugin.json");
            if (!File.Exists(metadataPath))
            {
                return false;
            }

            var json = File.ReadAllText(metadataPath);
            metadata = JsonSerializer.Deserialize<PluginMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return metadata?.Type?.Equals("javascript", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception ex)
        {
            logger.Debug($"读取目录中的 plugin.json 失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 清理临时目录
    /// </summary>
    public void CleanupTempDirectory()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
                _logger.Info($"已清理临时目录: {_tempDirectory}");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"清理临时目录失败: {ex.Message}");
        }
    }
}

