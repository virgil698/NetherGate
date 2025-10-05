using NetherGate.API.Logging;

namespace NetherGate.Core.Process;

/// <summary>
/// 崩溃类型
/// </summary>
public enum CrashType
{
    Unknown,
    OutOfMemory,
    PortInUse,
    ModConflict,
    CorruptedWorld,
    NetworkError,
    PermissionDenied,
    MissingDependency
}

/// <summary>
/// 崩溃报告
/// </summary>
public class CrashReport
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public CrashType Type { get; set; } = CrashType.Unknown;
    public string Suggestion { get; set; } = string.Empty;
    public string LogContent { get; set; } = string.Empty;
    public List<string> RelevantLines { get; set; } = new();
}

/// <summary>
/// 崩溃分析器
/// 分析服务器崩溃日志并提供解决建议
/// </summary>
public class CrashAnalyzer
{
    private readonly ILogger _logger;
    private readonly string _crashLogDir;

    public CrashAnalyzer(ILogger logger, string? crashLogDir = null)
    {
        _logger = logger;
        _crashLogDir = crashLogDir ?? Path.Combine("logs", "crashes");

        // 确保崩溃日志目录存在
        if (!Directory.Exists(_crashLogDir))
        {
            Directory.CreateDirectory(_crashLogDir);
        }
    }

    /// <summary>
    /// 分析崩溃日志
    /// </summary>
    public CrashReport AnalyzeCrash(string logContent)
    {
        var report = new CrashReport
        {
            Timestamp = DateTime.Now,
            LogContent = logContent
        };

        // 检测崩溃类型
        report.Type = DetectCrashType(logContent, report.RelevantLines);
        report.Suggestion = GetSuggestion(report.Type);

        return report;
    }

    /// <summary>
    /// 检测崩溃类型
    /// </summary>
    private CrashType DetectCrashType(string logContent, List<string> relevantLines)
    {
        var lines = logContent.Split('\n');

        // 内存不足
        if (ContainsPattern(lines, relevantLines, "OutOfMemoryError", "java.lang.OutOfMemoryError"))
        {
            return CrashType.OutOfMemory;
        }

        // 端口被占用
        if (ContainsPattern(lines, relevantLines, "BindException", "Address already in use"))
        {
            return CrashType.PortInUse;
        }

        // 模组/插件冲突
        if (ContainsPattern(lines, relevantLines, "ClassNotFoundException", "NoClassDefFoundError", "IncompatibleClassChangeError"))
        {
            return CrashType.ModConflict;
        }

        // 世界文件损坏
        if (ContainsPattern(lines, relevantLines, "Failed to load world", "Corrupt chunk", "region file"))
        {
            return CrashType.CorruptedWorld;
        }

        // 网络错误
        if (ContainsPattern(lines, relevantLines, "Connection refused", "Connection reset", "SocketException"))
        {
            return CrashType.NetworkError;
        }

        // 权限问题
        if (ContainsPattern(lines, relevantLines, "Permission denied", "Access is denied", "Cannot create directory"))
        {
            return CrashType.PermissionDenied;
        }

        // 缺少依赖
        if (ContainsPattern(lines, relevantLines, "Could not find or load main class", "ClassNotFoundException"))
        {
            return CrashType.MissingDependency;
        }

        return CrashType.Unknown;
    }

    /// <summary>
    /// 检查日志是否包含特定模式
    /// </summary>
    private bool ContainsPattern(string[] lines, List<string> relevantLines, params string[] patterns)
    {
        foreach (var line in lines)
        {
            foreach (var pattern in patterns)
            {
                if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    relevantLines.Add(line.Trim());
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取解决建议
    /// </summary>
    private string GetSuggestion(CrashType crashType)
    {
        return crashType switch
        {
            CrashType.OutOfMemory => 
                "内存不足。建议：\n" +
                "1. 增加服务器内存配置（-Xmx 参数）\n" +
                "2. 减少加载的区块数量\n" +
                "3. 优化模组配置\n" +
                "4. 检查是否有内存泄漏",

            CrashType.PortInUse => 
                "端口被占用。建议：\n" +
                "1. 检查是否有其他服务器实例正在运行\n" +
                "2. 更改 server.properties 中的端口号\n" +
                "3. 使用 netstat/ss 命令查看端口占用情况\n" +
                "4. 重启系统释放端口",

            CrashType.ModConflict => 
                "模组/插件冲突或缺少依赖。建议：\n" +
                "1. 检查模组兼容性\n" +
                "2. 确保所有依赖都已安装\n" +
                "3. 更新模组到最新版本\n" +
                "4. 逐个禁用模组以找出冲突源",

            CrashType.CorruptedWorld => 
                "世界文件损坏。建议：\n" +
                "1. 从备份恢复世界\n" +
                "2. 删除损坏的区块文件\n" +
                "3. 使用 MCEdit 等工具修复世界\n" +
                "4. 如果无法修复，考虑重新生成世界",

            CrashType.NetworkError => 
                "网络错误。建议：\n" +
                "1. 检查网络连接\n" +
                "2. 检查防火墙设置\n" +
                "3. 验证 RCON/SMP 配置\n" +
                "4. 检查代理或 VPN 设置",

            CrashType.PermissionDenied => 
                "权限不足。建议：\n" +
                "1. 以管理员身份运行\n" +
                "2. 检查文件和目录权限\n" +
                "3. 确保有写入权限\n" +
                "4. 检查 SELinux/AppArmor 设置",

            CrashType.MissingDependency => 
                "缺少依赖。建议：\n" +
                "1. 安装正确的 Java 版本\n" +
                "2. 检查 server.jar 文件是否完整\n" +
                "3. 重新下载服务器文件\n" +
                "4. 检查环境变量配置",

            _ => 
                "未知崩溃类型。建议：\n" +
                "1. 查看完整的崩溃日志\n" +
                "2. 搜索错误信息\n" +
                "3. 在社区寻求帮助\n" +
                "4. 检查服务器和模组的更新日志"
        };
    }

    /// <summary>
    /// 保存崩溃报告
    /// </summary>
    public async Task<string> SaveCrashReportAsync(CrashReport report)
    {
        var filename = $"crash-{report.Timestamp:yyyyMMdd-HHmmss}.txt";
        var path = Path.Combine(_crashLogDir, filename);

        var content = GenerateReportContent(report);
        await File.WriteAllTextAsync(path, content);

        _logger.Error($"服务器崩溃，报告已保存至: {path}");
        _logger.Warning($"崩溃类型: {report.Type}");
        _logger.Info($"建议:\n{report.Suggestion}");

        return path;
    }

    /// <summary>
    /// 生成报告内容
    /// </summary>
    private string GenerateReportContent(CrashReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("====================================");
        sb.AppendLine("       NetherGate 崩溃报告");
        sb.AppendLine("====================================");
        sb.AppendLine();
        sb.AppendLine($"时间: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"崩溃类型: {report.Type}");
        sb.AppendLine();
        sb.AppendLine("解决建议:");
        sb.AppendLine("------------------------------------");
        sb.AppendLine(report.Suggestion);
        sb.AppendLine();

        if (report.RelevantLines.Count > 0)
        {
            sb.AppendLine("相关日志行:");
            sb.AppendLine("------------------------------------");
            foreach (var line in report.RelevantLines)
            {
                sb.AppendLine(line);
            }
            sb.AppendLine();
        }

        sb.AppendLine("完整日志:");
        sb.AppendLine("====================================");
        sb.AppendLine(report.LogContent);

        return sb.ToString();
    }

    /// <summary>
    /// 获取最近的崩溃报告列表
    /// </summary>
    public List<FileInfo> GetRecentCrashReports(int count = 10)
    {
        if (!Directory.Exists(_crashLogDir))
            return new List<FileInfo>();

        var dir = new DirectoryInfo(_crashLogDir);
        return dir.GetFiles("crash-*.txt")
            .OrderByDescending(f => f.LastWriteTime)
            .Take(count)
            .ToList();
    }
}
