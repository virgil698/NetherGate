using System.Reflection;

namespace NetherGate.API.Plugins;

/// <summary>
/// 插件容器
/// 封装插件实例、元数据、状态等信息
/// </summary>
public class PluginContainer
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    public PluginMetadata Metadata { get; }

    /// <summary>
    /// 插件实例
    /// </summary>
    public IPlugin? Instance { get; set; }

    /// <summary>
    /// 插件状态
    /// </summary>
    public PluginState State { get; set; }

    /// <summary>
    /// 插件目录
    /// </summary>
    public string PluginDirectory { get; }

    /// <summary>
    /// 插件 DLL 路径
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// 插件程序集
    /// </summary>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// 加载上下文（用于插件隔离）
    /// </summary>
    public object? LoadContext { get; set; }

    /// <summary>
    /// 插件数据目录
    /// </summary>
    public string DataDirectory { get; }

    /// <summary>
    /// 加载时间
    /// </summary>
    public DateTime? LoadedAt { get; set; }

    /// <summary>
    /// 启用时间
    /// </summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception? Exception { get; set; }

    public PluginContainer(
        PluginMetadata metadata,
        string pluginDirectory,
        string assemblyPath,
        string dataDirectory)
    {
        Metadata = metadata;
        PluginDirectory = pluginDirectory;
        AssemblyPath = assemblyPath;
        DataDirectory = dataDirectory;
        State = PluginState.Unloaded;
    }

    /// <summary>
    /// 插件 ID
    /// </summary>
    public string Id => Metadata.Id;

    /// <summary>
    /// 插件名称
    /// </summary>
    public string Name => Metadata.Name;

    /// <summary>
    /// 插件版本
    /// </summary>
    public string Version => Metadata.Version;

    /// <summary>
    /// 是否已加载
    /// </summary>
    public bool IsLoaded => State != PluginState.Unloaded;

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool IsEnabled => State == PluginState.Enabled;

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasError => State == PluginState.Error;

    /// <summary>
    /// 设置错误状态
    /// </summary>
    public void SetError(string error, Exception? exception = null)
    {
        State = PluginState.Error;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// 清除错误
    /// </summary>
    public void ClearError()
    {
        Error = null;
        Exception = null;
    }

    public override string ToString()
    {
        return $"{Name} v{Version} [{State}]";
    }
}

