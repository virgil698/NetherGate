using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NuGet.Versioning;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 插件依赖验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 插件依赖验证器
/// 负责验证插件间的依赖关系和冲突
/// </summary>
public class PluginDependencyValidator
{
    private readonly ILogger _logger;

    public PluginDependencyValidator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证所有插件的依赖关系
    /// </summary>
    public ValidationResult ValidateAll(List<PluginContainer> containers)
    {
        var result = new ValidationResult { IsValid = true };

        // 1. 检查插件间依赖
        ValidatePluginDependencies(containers, result);

        // 2. 检查冲突插件
        ValidateConflicts(containers, result);

        // 3. 检查 NetherGate 版本要求
        ValidateNetherGateVersion(containers, result);

        // 4. 检查循环依赖
        ValidateCircularDependencies(containers, result);

        return result;
    }

    /// <summary>
    /// 验证插件间依赖关系
    /// </summary>
    private void ValidatePluginDependencies(List<PluginContainer> containers, ValidationResult result)
    {
        var pluginMap = containers.ToDictionary(c => c.Id, c => c);

        foreach (var container in containers)
        {
            var metadata = container.Metadata;

            // 检查必需依赖
            var requiredDeps = metadata.GetAllDependencies();
            foreach (var depId in requiredDeps)
            {
                if (!pluginMap.ContainsKey(depId))
                {
                    result.IsValid = false;
                    result.Errors.Add($"插件 '{container.Name}' 依赖的插件未找到: {depId}");
                    continue;
                }

                // 检查版本要求
                var depContainer = pluginMap[depId];
                var versionReq = GetPluginDependencyVersion(metadata, depId);

                if (!string.IsNullOrEmpty(versionReq))
                {
                    if (!IsVersionSatisfied(depContainer.Version, versionReq))
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"插件 '{container.Name}' 需要 '{depId}' 版本 {versionReq}，" +
                            $"但当前版本为 {depContainer.Version}");
                    }
                }
            }

            // 检查可选依赖
            var optionalDeps = metadata.GetAllSoftDependencies();
            foreach (var depId in optionalDeps)
            {
                if (!pluginMap.ContainsKey(depId))
                {
                    result.Warnings.Add($"插件 '{container.Name}' 的可选依赖未找到: {depId}");
                    continue;
                }

                var depContainer = pluginMap[depId];
                var versionReq = GetPluginDependencyVersion(metadata, depId);

                if (!string.IsNullOrEmpty(versionReq))
                {
                    if (!IsVersionSatisfied(depContainer.Version, versionReq))
                    {
                        result.Warnings.Add(
                            $"插件 '{container.Name}' 推荐 '{depId}' 版本 {versionReq}，" +
                            $"当前版本为 {depContainer.Version}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 验证冲突插件
    /// </summary>
    private void ValidateConflicts(List<PluginContainer> containers, ValidationResult result)
    {
        var pluginMap = containers.ToDictionary(c => c.Id, c => c);

        foreach (var container in containers)
        {
            if (container.Metadata.Conflicts == null)
                continue;

            foreach (var conflict in container.Metadata.Conflicts)
            {
                if (pluginMap.ContainsKey(conflict.Id))
                {
                    result.IsValid = false;
                    var reason = string.IsNullOrEmpty(conflict.Reason)
                        ? "未知原因"
                        : conflict.Reason;

                    result.Errors.Add(
                        $"插件冲突: '{container.Name}' 与 '{conflict.Id}' 冲突 - {reason}");
                }
            }
        }
    }

    /// <summary>
    /// 验证 NetherGate 版本要求
    /// </summary>
    private void ValidateNetherGateVersion(List<PluginContainer> containers, ValidationResult result)
    {
        var currentVersion = GetNetherGateVersion();

        foreach (var container in containers)
        {
            var metadata = container.Metadata;

            // 检查最低版本要求
            if (!string.IsNullOrEmpty(metadata.MinNetherGateVersion))
            {
                if (!IsVersionSatisfied(currentVersion, $">={metadata.MinNetherGateVersion}"))
                {
                    result.IsValid = false;
                    result.Errors.Add(
                        $"插件 '{container.Name}' 需要 NetherGate 版本 >= {metadata.MinNetherGateVersion}，" +
                        $"当前版本为 {currentVersion}");
                }
            }

            // 检查最高版本要求
            if (!string.IsNullOrEmpty(metadata.MaxNetherGateVersion))
            {
                if (!IsVersionSatisfied(currentVersion, $"<={metadata.MaxNetherGateVersion}"))
                {
                    result.Warnings.Add(
                        $"插件 '{container.Name}' 建议 NetherGate 版本 <= {metadata.MaxNetherGateVersion}，" +
                        $"当前版本为 {currentVersion}，可能不兼容");
                }
            }

            // 兼容旧格式
            if (!string.IsNullOrEmpty(metadata.NetherGateVersion))
            {
                if (!IsVersionSatisfied(currentVersion, metadata.NetherGateVersion))
                {
                    result.Warnings.Add(
                        $"插件 '{container.Name}' 指定 NetherGate 版本 {metadata.NetherGateVersion}，" +
                        $"当前版本为 {currentVersion}");
                }
            }
        }
    }

    /// <summary>
    /// 检查循环依赖
    /// </summary>
    private void ValidateCircularDependencies(List<PluginContainer> containers, ValidationResult result)
    {
        var pluginMap = containers.ToDictionary(c => c.Id, c => c);
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var container in containers)
        {
            if (!visited.Contains(container.Id))
            {
                if (HasCircularDependency(container.Id, pluginMap, visited, recursionStack, out var cycle))
                {
                    result.IsValid = false;
                    result.Errors.Add($"检测到循环依赖: {string.Join(" -> ", cycle)}");
                }
            }
        }
    }

    /// <summary>
    /// 递归检查循环依赖
    /// </summary>
    private bool HasCircularDependency(
        string pluginId,
        Dictionary<string, PluginContainer> pluginMap,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        out List<string> cycle)
    {
        cycle = new List<string>();

        if (!pluginMap.ContainsKey(pluginId))
            return false;

        visited.Add(pluginId);
        recursionStack.Add(pluginId);

        var container = pluginMap[pluginId];
        var dependencies = container.Metadata.GetAllDependencies();

        foreach (var depId in dependencies)
        {
            if (!pluginMap.ContainsKey(depId))
                continue;

            if (!visited.Contains(depId))
            {
                if (HasCircularDependency(depId, pluginMap, visited, recursionStack, out cycle))
                {
                    cycle.Insert(0, pluginId);
                    return true;
                }
            }
            else if (recursionStack.Contains(depId))
            {
                // 找到循环
                cycle.Add(pluginId);
                cycle.Add(depId);
                return true;
            }
        }

        recursionStack.Remove(pluginId);
        return false;
    }

    /// <summary>
    /// 获取插件依赖的版本要求
    /// </summary>
    private string? GetPluginDependencyVersion(PluginMetadata metadata, string pluginId)
    {
        if (metadata.PluginDependencies != null)
        {
            var dep = metadata.PluginDependencies.FirstOrDefault(d => d.Id == pluginId);
            if (dep != null)
                return dep.Version;
        }

        return null;
    }

    /// <summary>
    /// 检查版本是否满足要求
    /// </summary>
    private bool IsVersionSatisfied(string actualVersion, string requirement)
    {
        try
        {
            // 尝试解析为 NuGet 版本
            if (!NuGetVersion.TryParse(actualVersion, out var version))
                return true; // 无法解析，跳过检查

            var range = VersionRange.Parse(requirement);
            return range.Satisfies(version);
        }
        catch
        {
            // 解析失败，跳过检查
            return true;
        }
    }

    /// <summary>
    /// 获取 NetherGate 版本
    /// </summary>
    private string GetNetherGateVersion()
    {
        // 从程序集版本获取
        var assembly = typeof(PluginDependencyValidator).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// 生成验证报告
    /// </summary>
    public string GenerateReport(ValidationResult result)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("插件依赖验证报告");
        sb.AppendLine("====================");
        sb.AppendLine();

        if (result.Errors.Count == 0 && result.Warnings.Count == 0)
        {
            sb.AppendLine("✓ 所有插件依赖检查通过");
        }
        else
        {
            if (result.Errors.Count > 0)
            {
                sb.AppendLine($"错误 ({result.Errors.Count}):");
                foreach (var error in result.Errors)
                {
                    sb.AppendLine($"  ✗ {error}");
                }
                sb.AppendLine();
            }

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine($"警告 ({result.Warnings.Count}):");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($"  ⚠ {warning}");
                }
            }
        }

        return sb.ToString();
    }
}
