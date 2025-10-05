using System.Reflection;
using System.Runtime.Loader;
using NetherGate.API.Logging;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 插件加载上下文
/// 使用 AssemblyLoadContext 实现插件隔离，防止 DLL 冲突
/// 支持三层依赖解析策略：
/// 1. lib/ (全局共享依赖) - 最高优先级
/// 2. plugins/plugin-name/ (插件私有依赖) - 中等优先级
/// 3. 默认程序集 - 最低优先级
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly ILogger _logger;
    private readonly string _pluginPath;
    private readonly string _pluginDirectory;
    private readonly string _globalLibPath;

    public PluginLoadContext(string pluginPath, string globalLibPath, ILogger logger) 
        : base(name: $"Plugin_{Path.GetFileNameWithoutExtension(pluginPath)}", isCollectible: true)
    {
        _pluginPath = pluginPath;
        _pluginDirectory = Path.GetDirectoryName(pluginPath) ?? string.Empty;
        _globalLibPath = globalLibPath;
        _logger = logger;
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <summary>
    /// 加载程序集（三层依赖解析策略）
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrEmpty(name))
            return null;

        // 对于 NetherGate.API 和 NetherGate.Core，始终从主程序域加载（共享 API）
        if (name.StartsWith("NetherGate.API") || name.StartsWith("NetherGate.Core"))
        {
            _logger.Trace($"从主程序域加载共享程序集: {name}");
            return Default.LoadFromAssemblyName(assemblyName);
        }

        // 三层依赖解析策略
        Assembly? assembly;

        // 第一层: 尝试从全局 lib/ 文件夹加载（最高优先级）
        assembly = TryLoadFromGlobalLib(name);
        if (assembly != null)
        {
            _logger.Trace($"[层1] 从全局 lib/ 加载: {name}");
            return assembly;
        }

        // 第二层: 尝试从插件目录加载（中等优先级）
        assembly = TryLoadFromPluginDirectory(assemblyName);
        if (assembly != null)
        {
            _logger.Trace($"[层2] 从插件目录加载: {name}");
            return assembly;
        }

        // 第三层: 返回 null，让默认上下文处理（最低优先级）
        _logger.Trace($"[层3] 使用默认加载器: {name}");
        return null;
    }

    /// <summary>
    /// 尝试从全局 lib/ 文件夹加载程序集
    /// </summary>
    private Assembly? TryLoadFromGlobalLib(string assemblyName)
    {
        if (!Directory.Exists(_globalLibPath))
            return null;

        var dllPath = Path.Combine(_globalLibPath, $"{assemblyName}.dll");
        if (File.Exists(dllPath))
        {
            try
            {
                return LoadFromAssemblyPath(dllPath);
            }
            catch (Exception ex)
            {
                _logger.Warning($"无法从 lib/ 加载程序集 {assemblyName}: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 尝试从插件目录加载程序集
    /// </summary>
    private Assembly? TryLoadFromPluginDirectory(AssemblyName assemblyName)
    {
        // 1. 使用 AssemblyDependencyResolver 解析
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null && File.Exists(assemblyPath))
        {
            try
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.Warning($"无法从插件目录加载程序集 {assemblyName.Name}: {ex.Message}");
            }
        }

        // 2. 手动尝试从插件目录加载
        if (!string.IsNullOrEmpty(_pluginDirectory))
        {
            var dllPath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(dllPath))
            {
                try
                {
                    return LoadFromAssemblyPath(dllPath);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"无法手动加载程序集 {assemblyName.Name}: {ex.Message}");
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 加载非托管库
    /// </summary>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            _logger.Trace($"加载非托管库: {unmanagedDllName} -> {libraryPath}");
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    public void UnloadPlugin()
    {
        _logger.Debug($"卸载插件上下文: {Name}");
        Unload();
    }
}

