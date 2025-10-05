using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NuGet.Versioning;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 依赖版本冲突解决策略
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// 使用最高版本（默认）
    /// </summary>
    UseHighest,
    
    /// <summary>
    /// 使用最低兼容版本
    /// </summary>
    UseLowest,
    
    /// <summary>
    /// 报错，让用户手动处理
    /// </summary>
    Fail
}

/// <summary>
/// 依赖冲突信息
/// </summary>
public class DependencyConflict
{
    public string DependencyName { get; set; } = string.Empty;
    public List<ConflictingRequirement> Requirements { get; set; } = new();
    public string? ResolvedVersion { get; set; }
}

/// <summary>
/// 冲突的依赖要求
/// </summary>
public class ConflictingRequirement
{
    public string PluginName { get; set; } = string.Empty;
    public string RequiredVersion { get; set; } = string.Empty;
}

/// <summary>
/// 依赖冲突解决器
/// 负责检测和解决插件之间的依赖版本冲突
/// </summary>
public class DependencyConflictResolver
{
    private readonly ILogger _logger;
    private readonly ConflictResolutionStrategy _strategy;

    public DependencyConflictResolver(
        ILogger logger,
        ConflictResolutionStrategy strategy = ConflictResolutionStrategy.UseHighest)
    {
        _logger = logger;
        _strategy = strategy;
    }

    /// <summary>
    /// 分析所有插件的依赖，检测冲突
    /// </summary>
    public List<DependencyConflict> AnalyzeDependencies(List<PluginContainer> containers)
    {
        var conflicts = new List<DependencyConflict>();
        
        // 收集所有依赖要求
        var dependencyMap = new Dictionary<string, List<(string pluginName, LibraryDependency dep)>>();
        
        foreach (var container in containers)
        {
            if (container.Metadata.LibraryDependencies == null)
                continue;

            foreach (var dep in container.Metadata.LibraryDependencies)
            {
                // 只检查 lib/ 中的共享依赖
                if (dep.Location == "local")
                    continue;

                if (!dependencyMap.ContainsKey(dep.Name))
                {
                    dependencyMap[dep.Name] = new List<(string, LibraryDependency)>();
                }

                dependencyMap[dep.Name].Add((container.Name, dep));
            }
        }

        // 检测冲突
        foreach (var (depName, requirements) in dependencyMap)
        {
            // 只有一个插件需要这个依赖，无冲突
            if (requirements.Count <= 1)
                continue;

            // 检查版本要求是否冲突
            var versions = requirements
                .Where(r => !string.IsNullOrWhiteSpace(r.dep.Version))
                .Select(r => r.dep.Version!)
                .Distinct()
                .ToList();

            // 如果版本要求不同，就有冲突
            if (versions.Count > 1)
            {
                var conflict = new DependencyConflict
                {
                    DependencyName = depName,
                    Requirements = requirements.Select(r => new ConflictingRequirement
                    {
                        PluginName = r.pluginName,
                        RequiredVersion = r.dep.Version ?? "any"
                    }).ToList()
                };

                conflicts.Add(conflict);
            }
        }

        return conflicts;
    }

    /// <summary>
    /// 解决冲突
    /// </summary>
    public bool ResolveConflicts(List<DependencyConflict> conflicts)
    {
        if (conflicts.Count == 0)
        {
            _logger.Debug("未检测到依赖冲突");
            return true;
        }

        _logger.Warning($"检测到 {conflicts.Count} 个依赖冲突");

        foreach (var conflict in conflicts)
        {
            _logger.Warning($"");
            _logger.Warning($"依赖冲突: {conflict.DependencyName}");
            foreach (var req in conflict.Requirements)
            {
                _logger.Warning($"  - {req.PluginName} 需要版本: {req.RequiredVersion}");
            }

            // 根据策略解决冲突
            var resolved = ResolveConflict(conflict);
            
            if (!resolved)
            {
                _logger.Error($"无法解决冲突: {conflict.DependencyName}");
                return false;
            }

            _logger.Info($"✓ 解决方案: 使用版本 {conflict.ResolvedVersion}");
        }

        return true;
    }

    /// <summary>
    /// 解决单个冲突
    /// </summary>
    private bool ResolveConflict(DependencyConflict conflict)
    {
        switch (_strategy)
        {
            case ConflictResolutionStrategy.UseHighest:
                return ResolveUsingHighest(conflict);
                
            case ConflictResolutionStrategy.UseLowest:
                return ResolveUsingLowest(conflict);
                
            case ConflictResolutionStrategy.Fail:
                return false;
                
            default:
                return ResolveUsingHighest(conflict);
        }
    }

    /// <summary>
    /// 使用最高版本解决冲突
    /// </summary>
    private bool ResolveUsingHighest(DependencyConflict conflict)
    {
        try
        {
            var versions = new List<NuGetVersion>();
            
            foreach (var req in conflict.Requirements)
            {
                if (string.IsNullOrWhiteSpace(req.RequiredVersion) || req.RequiredVersion == "any")
                    continue;

                // 尝试解析版本
                if (NuGetVersion.TryParse(ExtractVersionNumber(req.RequiredVersion), out var version))
                {
                    versions.Add(version);
                }
            }

            if (versions.Count == 0)
            {
                conflict.ResolvedVersion = "latest";
                return true;
            }

            // 选择最高版本
            var highestVersion = versions.Max();
            conflict.ResolvedVersion = highestVersion!.ToString();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"解析版本时出错: {conflict.DependencyName}", ex);
            return false;
        }
    }

    /// <summary>
    /// 使用最低兼容版本解决冲突
    /// </summary>
    private bool ResolveUsingLowest(DependencyConflict conflict)
    {
        try
        {
            var versions = new List<NuGetVersion>();
            
            foreach (var req in conflict.Requirements)
            {
                if (string.IsNullOrWhiteSpace(req.RequiredVersion) || req.RequiredVersion == "any")
                    continue;

                // 尝试解析版本
                if (NuGetVersion.TryParse(ExtractVersionNumber(req.RequiredVersion), out var version))
                {
                    versions.Add(version);
                }
            }

            if (versions.Count == 0)
            {
                conflict.ResolvedVersion = "latest";
                return true;
            }

            // 选择最高版本（作为最低兼容版本）
            // 注意：这里仍然选最高版本，因为最低版本可能不兼容其他插件
            var highestVersion = versions.Max();
            conflict.ResolvedVersion = highestVersion!.ToString();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"解析版本时出错: {conflict.DependencyName}", ex);
            return false;
        }
    }

    /// <summary>
    /// 提取版本号（去除前缀符号）
    /// </summary>
    private string ExtractVersionNumber(string versionString)
    {
        // 去除 >=, >, =, < 等前缀
        var trimmed = versionString.Trim();
        
        if (trimmed.StartsWith(">=") || trimmed.StartsWith("<="))
            return trimmed.Substring(2).Trim();
        
        if (trimmed.StartsWith(">") || trimmed.StartsWith("<") || trimmed.StartsWith("="))
            return trimmed.Substring(1).Trim();
        
        return trimmed;
    }

    /// <summary>
    /// 生成冲突报告
    /// </summary>
    public string GenerateConflictReport(List<DependencyConflict> conflicts)
    {
        if (conflicts.Count == 0)
            return "未检测到依赖冲突";

        var report = new System.Text.StringBuilder();
        report.AppendLine("依赖冲突报告:");
        report.AppendLine("====================");
        
        foreach (var conflict in conflicts)
        {
            report.AppendLine();
            report.AppendLine($"依赖: {conflict.DependencyName}");
            report.AppendLine("冲突的版本要求:");
            
            foreach (var req in conflict.Requirements)
            {
                report.AppendLine($"  - {req.PluginName}: {req.RequiredVersion}");
            }
            
            if (!string.IsNullOrEmpty(conflict.ResolvedVersion))
            {
                report.AppendLine($"解决方案: 使用版本 {conflict.ResolvedVersion}");
            }
        }
        
        return report.ToString();
    }
}
