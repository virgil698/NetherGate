namespace NetherGate.Host.Cli;

/// <summary>
/// CLI 命令枚举
/// </summary>
public enum CliCommand
{
    None,
    
    // 插件管理
    PluginList,
    PluginInfo,
    PluginEnable,
    PluginDisable,
    PluginReload,
    PluginSearch,
    PluginInstall,
    PluginUpdate,
    
    // 配置管理
    ConfigValidate,
    ConfigExport,
    ConfigImport,
    
    // 诊断工具
    Diagnose,
    CheckDeps,
    
    // 向导
    Setup,
    
    // 其他
    Version,
    Help
}

/// <summary>
/// CLI 命令参数
/// </summary>
public class CliArguments
{
    public CliCommand Command { get; set; }
    public List<string> Parameters { get; set; } = new();
    public Dictionary<string, string> Options { get; set; } = new();
    
    /// <summary>
    /// 是否为交互模式
    /// </summary>
    public bool IsInteractive => Command == CliCommand.None || Command == CliCommand.Setup;
}

/// <summary>
/// CLI 参数解析器
/// </summary>
public class CliArgumentParser
{
    /// <summary>
    /// 解析命令行参数
    /// </summary>
    public static CliArguments Parse(string[] args)
    {
        var result = new CliArguments();
        
        if (args.Length == 0)
        {
            result.Command = CliCommand.None;
            return result;
        }
        
        var commandStr = args[0].ToLower();
        
        // 解析命令
        result.Command = commandStr switch
        {
            "plugin" when args.Length > 1 => ParsePluginCommand(args[1]),
            "config" when args.Length > 1 => ParseConfigCommand(args[1]),
            "diagnose" => CliCommand.Diagnose,
            "check-deps" => CliCommand.CheckDeps,
            "setup" or "--setup" or "-s" => CliCommand.Setup,
            "version" or "--version" or "-v" => CliCommand.Version,
            "help" or "--help" or "-h" => CliCommand.Help,
            _ => CliCommand.None
        };
        
        // 解析参数和选项
        for (int i = GetParameterStartIndex(args); i < args.Length; i++)
        {
            var arg = args[i];
            
            if (arg.StartsWith("--"))
            {
                // 长选项：--option=value 或 --option value
                var parts = arg.Substring(2).Split('=', 2);
                var key = parts[0];
                var value = parts.Length > 1 ? parts[1] : 
                           (i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[++i] : "true");
                result.Options[key] = value;
            }
            else if (arg.StartsWith("-"))
            {
                // 短选项：-o value
                var key = arg.Substring(1);
                var value = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[++i] : "true";
                result.Options[key] = value;
            }
            else
            {
                // 位置参数
                result.Parameters.Add(arg);
            }
        }
        
        return result;
    }
    
    private static CliCommand ParsePluginCommand(string subCommand)
    {
        return subCommand.ToLower() switch
        {
            "list" or "ls" => CliCommand.PluginList,
            "info" => CliCommand.PluginInfo,
            "enable" => CliCommand.PluginEnable,
            "disable" => CliCommand.PluginDisable,
            "reload" => CliCommand.PluginReload,
            "search" => CliCommand.PluginSearch,
            "install" => CliCommand.PluginInstall,
            "update" => CliCommand.PluginUpdate,
            _ => CliCommand.Help
        };
    }
    
    private static CliCommand ParseConfigCommand(string subCommand)
    {
        return subCommand.ToLower() switch
        {
            "validate" => CliCommand.ConfigValidate,
            "export" => CliCommand.ConfigExport,
            "import" => CliCommand.ConfigImport,
            _ => CliCommand.Help
        };
    }
    
    private static int GetParameterStartIndex(string[] args)
    {
        if (args.Length == 0) return 0;
        if (args[0] == "plugin" || args[0] == "config")
            return args.Length > 1 ? 2 : 1;
        return 1;
    }
}
