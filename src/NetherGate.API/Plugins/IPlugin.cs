namespace NetherGate.API.Plugins;

/// <summary>
/// 插件接口
/// 所有 NetherGate 插件必须实现此接口
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件信息
    /// </summary>
    PluginInfo Info { get; }

    /// <summary>
    /// 插件加载时调用
    /// 此时插件被加载到内存，但尚未启用
    /// 用于初始化插件的基本配置和资源
    /// </summary>
    Task OnLoadAsync();

    /// <summary>
    /// 插件启用时调用
    /// 此时插件开始正式工作，可以注册事件、命令等
    /// </summary>
    Task OnEnableAsync();

    /// <summary>
    /// 插件禁用时调用
    /// 此时插件停止工作，应该注销所有事件、命令等
    /// </summary>
    Task OnDisableAsync();

    /// <summary>
    /// 插件卸载时调用
    /// 此时插件将从内存中移除，应该释放所有资源
    /// </summary>
    Task OnUnloadAsync();
}

/// <summary>
/// 插件信息
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// 插件 ID（唯一标识符，推荐使用反向域名格式，如 com.example.myplugin）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 插件名称（显示名称）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 插件版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 插件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 插件作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 插件主页
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// 插件依赖项（其他插件的 ID）
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 可选依赖项（如果存在则加载，不存在也不影响）
    /// </summary>
    public List<string> SoftDependencies { get; set; } = new();

    /// <summary>
    /// 加载顺序（值越小越先加载，默认 100）
    /// </summary>
    public int LoadOrder { get; set; } = 100;

    /// <summary>
    /// 返回插件信息的字符串表示形式
    /// </summary>
    public override string ToString() => $"{Name} v{Version} by {Author}";
}

/// <summary>
/// 插件状态
/// </summary>
public enum PluginState
{
    /// <summary>
    /// 未加载
    /// </summary>
    Unloaded,

    /// <summary>
    /// 已加载但未启用
    /// </summary>
    Loaded,

    /// <summary>
    /// 已启用（正在运行）
    /// </summary>
    Enabled,

    /// <summary>
    /// 已禁用
    /// </summary>
    Disabled,

    /// <summary>
    /// 加载失败
    /// </summary>
    Error
}

