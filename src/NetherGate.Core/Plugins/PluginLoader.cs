using System.Reflection;
using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 插件加载器
/// 负责扫描、加载和卸载插件
/// </summary>
public class PluginLoader
{
    private readonly ILogger _logger;
    private readonly string _pluginsDirectory;
    private readonly string _configDirectory;
    private readonly string _globalLibPath;

    public PluginLoader(ILogger logger, string pluginsDirectory, string configDirectory, string globalLibPath)
    {
        _logger = logger;
        _pluginsDirectory = pluginsDirectory;
        _configDirectory = configDirectory;
        _globalLibPath = globalLibPath;

        // 确保 lib 目录存在
        if (!Directory.Exists(_globalLibPath))
        {
            Directory.CreateDirectory(_globalLibPath);
            _logger.Debug($"创建全局库目录: {_globalLibPath}");
        }
    }

    /// <summary>
    /// 扫描插件目录
    /// </summary>
    public List<PluginContainer> ScanPlugins()
    {
        _logger.Info($"扫描插件目录: {_pluginsDirectory}");

        if (!Directory.Exists(_pluginsDirectory))
        {
            _logger.Warning($"插件目录不存在: {_pluginsDirectory}");
            return new List<PluginContainer>();
        }

        var containers = new List<PluginContainer>();

        // 扫描所有子目录
        var pluginDirs = Directory.GetDirectories(_pluginsDirectory);
        _logger.Info($"发现 {pluginDirs.Length} 个插件目录");

        foreach (var pluginDir in pluginDirs)
        {
            try
            {
                var container = ScanPlugin(pluginDir);
                if (container != null)
                {
                    containers.Add(container);
                    _logger.Info($"  发现插件: {container.Name} v{container.Version}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"扫描插件目录失败: {pluginDir}", ex);
            }
        }

        _logger.Info($"共扫描到 {containers.Count} 个有效插件");
        return containers;
    }

    /// <summary>
    /// 扫描单个插件目录
    /// </summary>
    private PluginContainer? ScanPlugin(string pluginDirectory)
    {
        // 1. 读取 plugin.json
        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        if (!File.Exists(metadataPath))
        {
            _logger.Warning($"插件目录缺少 plugin.json: {pluginDirectory}");
            return null;
        }

        var json = File.ReadAllText(metadataPath);
        var metadata = JsonSerializer.Deserialize<PluginMetadata>(json);

        if (metadata == null)
        {
            _logger.Error($"无法解析 plugin.json: {metadataPath}");
            return null;
        }

        // 2. 验证元数据
        var errors = metadata.Validate();
        if (errors.Count > 0)
        {
            _logger.Error($"插件元数据验证失败: {metadata.Name}");
            foreach (var error in errors)
            {
                _logger.Error($"  - {error}");
            }
            return null;
        }

        // 3. 查找插件 DLL
        var pluginName = Path.GetFileName(pluginDirectory);
        var assemblyPath = Path.Combine(pluginDirectory, $"{pluginName}.dll");

        if (!File.Exists(assemblyPath))
        {
            _logger.Error($"插件 DLL 不存在: {assemblyPath}");
            return null;
        }

        // 4. 创建插件数据目录
        var dataDirectory = Path.Combine(_configDirectory, metadata.Id);
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
            _logger.Debug($"创建插件数据目录: {dataDirectory}");
        }

        // 5. 创建容器
        return new PluginContainer(metadata, pluginDirectory, assemblyPath, dataDirectory);
    }

    /// <summary>
    /// 加载插件
    /// </summary>
    public bool LoadPlugin(PluginContainer container)
    {
        try
        {
            _logger.Info($"加载插件: {container.Name}");

            // 1. 创建加载上下文（支持三层依赖解析）
            var loadContext = new PluginLoadContext(container.AssemblyPath, _globalLibPath, _logger);
            container.LoadContext = loadContext;

            // 2. 加载程序集
            container.Assembly = loadContext.LoadFromAssemblyPath(container.AssemblyPath);
            _logger.Debug($"  程序集已加载: {container.Assembly.FullName}");

            // 3. 查找插件主类
            var mainClass = container.Assembly.GetType(container.Metadata.Main);
            if (mainClass == null)
            {
                container.SetError($"找不到主类: {container.Metadata.Main}");
                _logger.Error($"  {container.Error}");
                return false;
            }

            // 4. 验证主类实现 IPlugin 接口
            if (!typeof(IPlugin).IsAssignableFrom(mainClass))
            {
                container.SetError($"主类未实现 IPlugin 接口: {container.Metadata.Main}");
                _logger.Error($"  {container.Error}");
                return false;
            }

            // 5. 创建插件实例
            container.Instance = (IPlugin?)Activator.CreateInstance(mainClass);
            if (container.Instance == null)
            {
                container.SetError("无法创建插件实例");
                _logger.Error($"  {container.Error}");
                return false;
            }

            // 6. 更新状态
            container.State = PluginState.Loaded;
            container.LoadedAt = DateTime.UtcNow;
            container.ClearError();

            _logger.Info($"  插件加载成功: {container.Name}");
            return true;
        }
        catch (Exception ex)
        {
            container.SetError($"加载插件时发生异常: {ex.Message}", ex);
            _logger.Error($"  加载插件失败: {container.Name}", ex);
            return false;
        }
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public bool UnloadPlugin(PluginContainer container)
    {
        try
        {
            _logger.Info($"卸载插件: {container.Name}");

            // 1. 释放插件实例
            container.Instance = null;

            // 2. 卸载程序集（通过卸载加载上下文）
            if (container.LoadContext is PluginLoadContext loadContext)
            {
                loadContext.UnloadPlugin();
                container.LoadContext = null;
            }

            // 3. 清空程序集引用
            container.Assembly = null;

            // 4. 触发 GC（帮助释放内存）
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // 5. 更新状态
            container.State = PluginState.Unloaded;
            container.LoadedAt = null;
            container.EnabledAt = null;

            _logger.Info($"  插件卸载成功: {container.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"  卸载插件失败: {container.Name}", ex);
            return false;
        }
    }

    /// <summary>
    /// 重载插件
    /// </summary>
    public bool ReloadPlugin(PluginContainer container)
    {
        _logger.Info($"重载插件: {container.Name}");

        if (!UnloadPlugin(container))
        {
            return false;
        }

        return LoadPlugin(container);
    }
}

