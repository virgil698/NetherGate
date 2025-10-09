using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Python;

/// <summary>
/// Python 插件加载器
/// 扩展核心 PluginLoader 以支持 Python 插件
/// </summary>
public class PythonPluginLoader
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PythonRuntime _pythonRuntime;

    public PythonPluginLoader(
        ILogger logger,
        IServiceProvider serviceProvider,
        PythonRuntime pythonRuntime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _pythonRuntime = pythonRuntime;
    }

    /// <summary>
    /// 检测并加载 Python 插件
    /// </summary>
    public IPlugin? LoadPythonPlugin(string pluginDirectory, PythonPluginMetadata metadata)
    {
        try
        {
            _logger.Info($"加载 Python 插件: {metadata.Name}");

            // 1. 检查 Python 版本
            if (!string.IsNullOrEmpty(metadata.PythonVersion))
            {
                if (!_pythonRuntime.CheckPythonVersion(metadata.PythonVersion))
                {
                    _logger.Error($"Python 版本不兼容: 需要 {metadata.PythonVersion}");
                    return null;
                }
            }

            // 2. 安装 Python 依赖
            if (metadata.PythonDependencies != null && metadata.PythonDependencies.Count > 0)
            {
                InstallPythonDependencies(pluginDirectory, metadata.PythonDependencies);
            }

            // 3. 解析主类
            var mainParts = metadata.Main.Split('.');
            if (mainParts.Length != 2)
            {
                _logger.Error($"无效的 Python 主类格式: {metadata.Main} (应为 'module.ClassName')");
                return null;
            }

            string mainModule = mainParts[0];
            string mainClass = mainParts[1];

            // 4. 创建适配器
            var adapter = new PythonPluginAdapter(
                pluginDirectory,
                mainModule,
                mainClass,
                _serviceProvider,
                _logger,
                _pythonRuntime
            );

            _logger.Info($"Python 插件加载成功: {adapter.Info.Name} v{adapter.Info.Version}");
            return adapter;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载 Python 插件失败: {metadata.Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// 从目录扫描 Python 插件元数据
    /// </summary>
    public static PythonPluginMetadata? ScanPythonPluginMetadata(string pluginDirectory, ILogger logger)
    {
        try
        {
            // 读取 plugin.json
            var metadataPath = Path.Combine(pluginDirectory, "resource", "plugin.json");
            if (!File.Exists(metadataPath))
            {
                return null;
            }

            var json = File.ReadAllText(metadataPath);
            var metadata = JsonSerializer.Deserialize<PythonPluginMetadata>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (metadata == null)
            {
                logger.Error($"无法解析 plugin.json: {metadataPath}");
                return null;
            }

            // 检查是否是 Python 插件
            if (!string.Equals(metadata.Type, "python", StringComparison.OrdinalIgnoreCase))
            {
                return null; // 不是 Python 插件
            }

            // 验证必需字段
            if (string.IsNullOrEmpty(metadata.Id) ||
                string.IsNullOrEmpty(metadata.Name) ||
                string.IsNullOrEmpty(metadata.Version) ||
                string.IsNullOrEmpty(metadata.Main))
            {
                logger.Error($"Python 插件元数据不完整: {metadataPath}");
                return null;
            }

            return metadata;
        }
        catch (Exception ex)
        {
            logger.Error($"读取 Python 插件元数据失败: {pluginDirectory}", ex);
            return null;
        }
    }

    /// <summary>
    /// 安装 Python 依赖
    /// </summary>
    private void InstallPythonDependencies(string pluginDirectory, List<string> dependencies)
    {
        _logger.Info($"检查 Python 依赖 ({dependencies.Count} 个)...");

        try
        {
            // 检查是否有 requirements.txt
            var requirementsPath = Path.Combine(pluginDirectory, "requirements.txt");
            if (File.Exists(requirementsPath))
            {
                _logger.Info("发现 requirements.txt，安装依赖...");
                InstallFromRequirements(requirementsPath);
            }
            else
            {
                // 逐个安装依赖
                foreach (var dep in dependencies)
                {
                    _logger.Info($"安装依赖: {dep}");
                    _pythonRuntime.InstallPackage(dep);
                }
            }

            _logger.Info("Python 依赖安装完成");
        }
        catch (Exception ex)
        {
            _logger.Error("安装 Python 依赖失败", ex);
            throw;
        }
    }

    /// <summary>
    /// 从 requirements.txt 安装依赖
    /// </summary>
    private void InstallFromRequirements(string requirementsPath)
    {
        var dependencies = File.ReadAllLines(requirementsPath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'))
            .ToList();

        foreach (var dep in dependencies)
        {
            _pythonRuntime.InstallPackage(dep);
        }
    }
}

/// <summary>
/// Python 插件元数据
/// </summary>
public class PythonPluginMetadata
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string? Website { get; set; }
    public string Type { get; set; } = "python";
    public string Main { get; set; } = "";
    public string? PythonVersion { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public List<string> SoftDependencies { get; set; } = new();
    public List<string> PythonDependencies { get; set; } = new();
    public int LoadOrder { get; set; } = 100;
}

