using System.Text.Json.Serialization;

namespace NetherGate.API.Plugins;

/// <summary>
/// 库依赖项定义
/// </summary>
public class LibraryDependency
{
    /// <summary>
    /// 程序集名称（不含 .dll 扩展名）
    /// 例如: "Newtonsoft.Json"
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本要求（可选）
    /// 例如: "13.0.0" 或 ">=13.0.0"
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// 依赖位置：
    /// - "lib": 从全局 lib/ 文件夹加载
    /// - "local": 从插件自己的文件夹加载
    /// - "auto": 自动查找（先 lib/，再 local/）
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; set; } = "auto";

    /// <summary>
    /// 是否为可选依赖（缺失时不报错）
    /// </summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; set; } = false;
}

/// <summary>
/// 插件依赖项定义（插件级别的依赖）
/// </summary>
public class PluginDependency
{
    /// <summary>
    /// 依赖的插件 ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 版本要求
    /// 例如: "1.0.0", ">=1.0.0", "[1.0.0, 2.0.0)"
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// 是否为可选依赖
    /// </summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; set; } = false;
}

/// <summary>
/// 冲突插件定义
/// </summary>
public class PluginConflict
{
    /// <summary>
    /// 冲突的插件 ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 冲突原因说明
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// 插件元数据（对应 plugin.json 文件）
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// 插件 ID（唯一标识符，推荐使用反向域名格式）
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 插件名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 插件版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 插件描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 插件作者
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// 插件作者列表
    /// </summary>
    [JsonPropertyName("authors")]
    public List<string>? Authors { get; set; }

    /// <summary>
    /// 插件主页
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>
    /// 源代码仓库
    /// </summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; set; }

    /// <summary>
    /// 许可证
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; set; }

    /// <summary>
    /// 主类全名（包含命名空间）
    /// 例如: "MyPlugin.MyPluginMain"
    /// </summary>
    [JsonPropertyName("main")]
    public string Main { get; set; } = string.Empty;

    /// <summary>
    /// 插件分类
    /// 例如: "utility", "management", "fun", "protection" 等
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// 插件依赖项（其他插件的 ID）- 简化格式
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }

    /// <summary>
    /// 可选依赖项（其他插件的 ID）- 简化格式
    /// </summary>
    [JsonPropertyName("soft_dependencies")]
    public List<string>? SoftDependencies { get; set; }

    /// <summary>
    /// 插件依赖项（详细格式，包含版本要求）
    /// </summary>
    [JsonPropertyName("plugin_dependencies")]
    public List<PluginDependency>? PluginDependencies { get; set; }

    /// <summary>
    /// 冲突插件列表
    /// </summary>
    [JsonPropertyName("conflicts")]
    public List<PluginConflict>? Conflicts { get; set; }

    /// <summary>
    /// 库依赖项（程序集名称列表）
    /// 这些依赖应该放在 lib/ 文件夹或插件自己的文件夹中
    /// </summary>
    [JsonPropertyName("library_dependencies")]
    public List<LibraryDependency>? LibraryDependencies { get; set; }

    /// <summary>
    /// 加载顺序（越小越先加载，默认 100）
    /// </summary>
    [JsonPropertyName("load_order")]
    public int LoadOrder { get; set; } = 100;

    /// <summary>
    /// NetherGate 最低版本要求
    /// </summary>
    [JsonPropertyName("min_nethergate_version")]
    public string? MinNetherGateVersion { get; set; }

    /// <summary>
    /// NetherGate 最高兼容版本
    /// </summary>
    [JsonPropertyName("max_nethergate_version")]
    public string? MaxNetherGateVersion { get; set; }

    /// <summary>
    /// NetherGate 版本要求（兼容旧格式）
    /// </summary>
    [JsonPropertyName("nethergate_version")]
    public string? NetherGateVersion { get; set; }

    /// <summary>
    /// Minecraft 版本要求
    /// </summary>
    [JsonPropertyName("minecraft_version")]
    public string? MinecraftVersion { get; set; }

    /// <summary>
    /// .NET 目标框架
    /// </summary>
    [JsonPropertyName("target_framework")]
    public string? TargetFramework { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 自定义属性（扩展用）
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// 转换为 PluginInfo
    /// </summary>
    public PluginInfo ToPluginInfo()
    {
        return new PluginInfo
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Description = Description ?? string.Empty,
            Author = GetAuthorString(),
            Website = Website,
            Dependencies = Dependencies ?? new List<string>(),
            SoftDependencies = SoftDependencies ?? new List<string>(),
            LoadOrder = LoadOrder
        };
    }

    private string GetAuthorString()
    {
        if (!string.IsNullOrEmpty(Author))
            return Author;

        if (Authors != null && Authors.Count > 0)
            return string.Join(", ", Authors);

        return "Unknown";
    }

    /// <summary>
    /// 获取所有依赖的插件 ID（合并简化格式和详细格式）
    /// </summary>
    public List<string> GetAllDependencies()
    {
        var result = new HashSet<string>();

        // 简化格式
        if (Dependencies != null)
            foreach (var dep in Dependencies)
                result.Add(dep);

        // 详细格式
        if (PluginDependencies != null)
            foreach (var dep in PluginDependencies.Where(d => !d.Optional))
                result.Add(dep.Id);

        return result.ToList();
    }

    /// <summary>
    /// 获取所有可选依赖的插件 ID
    /// </summary>
    public List<string> GetAllSoftDependencies()
    {
        var result = new HashSet<string>();

        // 简化格式
        if (SoftDependencies != null)
            foreach (var dep in SoftDependencies)
                result.Add(dep);

        // 详细格式
        if (PluginDependencies != null)
            foreach (var dep in PluginDependencies.Where(d => d.Optional))
                result.Add(dep.Id);

        return result.ToList();
    }

    /// <summary>
    /// 验证元数据是否有效
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("插件 ID 不能为空");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("插件名称不能为空");

        if (string.IsNullOrWhiteSpace(Version))
            errors.Add("插件版本不能为空");

        if (string.IsNullOrWhiteSpace(Main))
            errors.Add("主类名称不能为空");

        // 验证 ID 格式（推荐使用反向域名，但不强制）
        if (!string.IsNullOrWhiteSpace(Id))
        {
            if (Id.Contains(" "))
                errors.Add("插件 ID 不能包含空格");

            if (!IsValidPluginId(Id))
                errors.Add("插件 ID 格式无效（只能包含字母、数字、点、下划线和连字符）");
        }

        // 验证版本格式
        if (!string.IsNullOrWhiteSpace(Version) && !IsValidVersion(Version))
            errors.Add($"插件版本格式无效: {Version}");

        // 验证依赖格式
        if (PluginDependencies != null)
        {
            foreach (var dep in PluginDependencies)
            {
                if (string.IsNullOrWhiteSpace(dep.Id))
                    errors.Add("插件依赖的 ID 不能为空");

                if (!string.IsNullOrWhiteSpace(dep.Version) && !IsValidVersionRequirement(dep.Version))
                    errors.Add($"依赖版本要求格式无效: {dep.Id} - {dep.Version}");
            }
        }

        // 验证冲突插件
        if (Conflicts != null)
        {
            foreach (var conflict in Conflicts)
            {
                if (string.IsNullOrWhiteSpace(conflict.Id))
                    errors.Add("冲突插件的 ID 不能为空");
            }
        }

        return errors;
    }

    private static bool IsValidPluginId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        foreach (char c in id)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-')
                return false;
        }

        return true;
    }

    private static bool IsValidVersion(string version)
    {
        // 简单验证：主要检查是否符合 SemVer 格式
        if (string.IsNullOrWhiteSpace(version))
            return false;

        // 支持 1.0.0、1.0、1.0.0-beta 等格式
        var parts = version.Split(new[] { '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        // 至少第一部分应该是数字
        return int.TryParse(parts[0], out _);
    }

    private static bool IsValidVersionRequirement(string versionReq)
    {
        if (string.IsNullOrWhiteSpace(versionReq))
            return false;

        // 支持：1.0.0、>=1.0.0、[1.0.0, 2.0.0) 等格式
        // 简化验证，只要不是空的就认为有效
        return true;
    }
}

