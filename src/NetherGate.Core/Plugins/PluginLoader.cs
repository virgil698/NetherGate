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
    private IServiceProvider? _serviceProvider;

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
    /// 设置服务提供者（用于构造函数注入）
    /// </summary>
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
        // 1. 读取元数据
        var metadata = ScanPluginMetadataFromDirectory(pluginDirectory);
        if (metadata == null)
        {
            return null;
        }

        string assemblyPath;

        // 2. 根据插件类型确定程序集路径
        if (string.Equals(metadata.Type, "python", StringComparison.OrdinalIgnoreCase))
        {
            // Python 插件：使用插件目录本身作为"程序集路径"
            assemblyPath = pluginDirectory;
            _logger.Debug($"检测到 Python 插件: {metadata.Name}");
        }
        else
        {
            // C# 插件（默认）：查找 DLL
            var pluginName = Path.GetFileName(pluginDirectory);
            assemblyPath = Path.Combine(pluginDirectory, $"{pluginName}.dll");

            if (!File.Exists(assemblyPath))
            {
                _logger.Error($"插件 DLL 不存在: {assemblyPath}");
                return null;
            }
        }

        // 3. 创建插件数据目录
        var dataDirectory = Path.Combine(_configDirectory, metadata.Id);
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
            _logger.Debug($"创建插件数据目录: {dataDirectory}");
        }

        // 4. 创建容器
        return new PluginContainer(metadata, pluginDirectory, assemblyPath, dataDirectory);
    }

    /// <summary>
    /// 从插件目录扫描元数据
    /// </summary>
    private PluginMetadata? ScanPluginMetadataFromDirectory(string pluginDirectory)
    {
        // 优先查找 resource/plugin.json（Python 插件标准位置）
        var resourceMetadataPath = Path.Combine(pluginDirectory, "resource", "plugin.json");
        if (File.Exists(resourceMetadataPath))
        {
            return ScanPluginMetadata(resourceMetadataPath);
        }

        // 其次查找根目录的 plugin.json（C# 插件标准位置）
        var metadataPath = Path.Combine(pluginDirectory, "plugin.json");
        if (File.Exists(metadataPath))
        {
            return ScanPluginMetadata(metadataPath);
        }

        _logger.Warning($"插件目录缺少 plugin.json: {pluginDirectory}");
        return null;
    }

    /// <summary>
    /// 从 plugin.json 文件读取元数据
    /// </summary>
    private PluginMetadata? ScanPluginMetadata(string metadataPath)
    {
        try
        {
            var json = File.ReadAllText(metadataPath);
            var metadata = JsonSerializer.Deserialize<PluginMetadata>(json);

            if (metadata == null)
            {
                _logger.Error($"无法解析 plugin.json: {metadataPath}");
                return null;
            }

            // 验证元数据
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

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取插件元数据失败: {metadataPath}", ex);
            return null;
        }
    }

    /// <summary>
    /// 加载插件
    /// </summary>
    public bool LoadPlugin(PluginContainer container)
    {
        try
        {
            _logger.Info($"加载插件: {container.Name}");

            // 检查是否是 Python 插件
            if (string.Equals(container.Metadata.Type, "python", StringComparison.OrdinalIgnoreCase))
            {
                return LoadPythonPlugin(container);
            }

            // C# 插件加载逻辑（原有逻辑）
            return LoadCSharpPlugin(container);
        }
        catch (Exception ex)
        {
            container.SetError($"加载插件时发生异常: {ex.Message}", ex);
            _logger.Error($"  加载插件失败: {container.Name}", ex);
            return false;
        }
    }

    /// <summary>
    /// 加载 C# 插件
    /// </summary>
    private bool LoadCSharpPlugin(PluginContainer container)
    {
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

        // 5. 创建插件实例（支持构造函数注入）
        container.Instance = (IPlugin?)CreatePluginInstance(mainClass);
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

        _logger.Info($"  C# 插件加载成功: {container.Name}");
        return true;
    }

    /// <summary>
    /// 加载 Python 插件
    /// </summary>
    private bool LoadPythonPlugin(PluginContainer container)
    {
        if (_serviceProvider == null)
        {
            _logger.Error("服务提供者未设置，无法加载 Python 插件");
            return false;
        }

        // 尝试获取 Python 插件加载器
        var pythonLoaderType = Type.GetType("NetherGate.Python.PythonPluginLoader, NetherGate.Python");
        if (pythonLoaderType == null)
        {
            _logger.Error("未找到 NetherGate.Python 程序集，请确保已安装 Python 插件支持");
            _logger.Info("提示：运行 'dotnet add package NetherGate.Python' 安装 Python 插件支持");
            return false;
        }

        var pythonLoader = _serviceProvider.GetService(pythonLoaderType);
        if (pythonLoader == null)
        {
            _logger.Error("无法获取 Python 插件加载器实例");
            _logger.Info("提示：请在 Program.cs 中调用 services.AddPythonPluginSupport()");
            return false;
        }

        // 转换元数据为 Python 元数据
        var pythonMetadataType = Type.GetType("NetherGate.Python.PythonPluginMetadata, NetherGate.Python");
        if (pythonMetadataType == null)
        {
            _logger.Error("无法找到 PythonPluginMetadata 类型");
            return false;
        }

        var pythonMetadata = Activator.CreateInstance(pythonMetadataType);
        if (pythonMetadata == null)
        {
            _logger.Error("无法创建 Python 元数据实例");
            return false;
        }

        // 复制元数据属性
        CopyMetadataProperties(container.Metadata, pythonMetadata);

        // 调用 Python 加载器的 LoadPythonPlugin 方法
        var loadMethod = pythonLoaderType.GetMethod("LoadPythonPlugin");
        if (loadMethod == null)
        {
            _logger.Error("无法找到 LoadPythonPlugin 方法");
            return false;
        }

        var pluginInstance = loadMethod.Invoke(pythonLoader, new[] { container.PluginDirectory, pythonMetadata });
        if (pluginInstance is IPlugin plugin)
        {
            container.Instance = plugin;
            container.State = PluginState.Loaded;
            container.LoadedAt = DateTime.UtcNow;
            container.ClearError();

            _logger.Info($"  Python 插件加载成功: {container.Name}");
            return true;
        }

        _logger.Error("Python 插件加载失败：返回的实例无效");
        return false;
    }

    /// <summary>
    /// 复制元数据属性（从 PluginMetadata 到 PythonPluginMetadata）
    /// </summary>
    private void CopyMetadataProperties(PluginMetadata source, object target)
    {
        var targetType = target.GetType();

        // 使用反射复制属性
        var properties = new Dictionary<string, object?>
        {
            ["Id"] = source.Id,
            ["Name"] = source.Name,
            ["Version"] = source.Version,
            ["Description"] = source.Description,
            ["Author"] = source.Author,
            ["Website"] = source.Website,
            ["Type"] = source.Type,
            ["Main"] = source.Main,
            ["PythonVersion"] = source.PythonVersion,
            ["Dependencies"] = source.Dependencies,
            ["SoftDependencies"] = source.SoftDependencies,
            ["PythonDependencies"] = source.PythonDependencies,
            ["LoadOrder"] = source.LoadOrder
        };

        foreach (var (propName, value) in properties)
        {
            var prop = targetType.GetProperty(propName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
            }
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
    /// 重载插件（完整流程：卸载 → 重新扫描 → 加载）
    /// </summary>
    public bool ReloadPlugin(PluginContainer container)
    {
        _logger.Info($"重载插件: {container.Name}");

        // 1. 卸载旧插件
        if (!UnloadPlugin(container))
        {
            return false;
        }

        // 2. 重新扫描插件目录，获取最新的元数据
        var assemblyPath = container.AssemblyPath;
        if (!File.Exists(assemblyPath))
        {
            _logger.Error($"插件文件不存在: {assemblyPath}");
            return false;
        }

        try
        {
            // 重新读取元数据（从插件目录的 plugin.json）
            var pluginDirectory = Path.GetDirectoryName(assemblyPath);
            if (string.IsNullOrEmpty(pluginDirectory))
            {
                _logger.Error($"无法获取插件目录: {assemblyPath}");
                return false;
            }

            var metadata = ScanPluginMetadataFromDirectory(pluginDirectory);
            if (metadata == null)
            {
                _logger.Error($"无法读取插件元数据: {pluginDirectory}");
                return false;
            }

            // 使用反射更新容器的 Metadata 字段（因为它是只读属性）
            var metadataField = typeof(PluginContainer).GetField("<Metadata>k__BackingField", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (metadataField != null)
            {
                metadataField.SetValue(container, metadata);
                _logger.Info($"  已更新插件元数据: {metadata.Name} v{metadata.Version}");
            }
            else
            {
                _logger.Warning("  无法更新元数据（反射失败），使用旧元数据继续");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"  重新扫描插件元数据失败: {ex.Message}", ex);
            return false;
        }

        // 3. 加载新插件
        return LoadPlugin(container);
    }
    
    /// <summary>
    /// 创建插件实例（支持构造函数注入）
    /// </summary>
    private object? CreatePluginInstance(Type pluginType)
    {
        try
        {
            // 如果有服务提供者，尝试使用构造函数注入
            if (_serviceProvider != null)
            {
                // 获取所有构造函数，按参数数量排序（从多到少）
                var constructors = pluginType.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length)
                    .ToArray();
                
                foreach (var constructor in constructors)
                {
                    try
                    {
                        var parameters = constructor.GetParameters();
                        var args = new object[parameters.Length];
                        
                        // 尝试从服务提供者解析所有参数
                        bool allResolved = true;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var service = _serviceProvider.GetService(parameters[i].ParameterType);
                            if (service == null && !parameters[i].HasDefaultValue)
                            {
                                allResolved = false;
                                break;
                            }
                            args[i] = service ?? parameters[i].DefaultValue!;
                        }
                        
                        if (allResolved)
                        {
                            _logger.Debug($"  使用构造函数注入创建插件实例: {constructor.GetParameters().Length} 个参数");
                            return constructor.Invoke(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"  构造函数注入失败，尝试下一个构造函数: {ex.Message}");
                        continue;
                    }
                }
            }
            
            // 回退到无参构造函数
            _logger.Debug($"  使用无参构造函数创建插件实例");
            return Activator.CreateInstance(pluginType);
        }
        catch (Exception ex)
        {
            _logger.Error($"创建插件实例失败: {ex.Message}", ex);
            return null;
        }
    }
}

