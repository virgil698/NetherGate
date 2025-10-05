using NetherGate.API.Configuration;
using NetherGate.API.Plugins;
using NetherGate.Core.Configuration;
using NetherGate.Core.Plugins;

namespace NetherGate.Host.Cli;

/// <summary>
/// CLI 命令处理器
/// </summary>
public class CliCommandHandler
{
    /// <summary>
    /// 执行 CLI 命令
    /// </summary>
    public static async Task<int> ExecuteAsync(CliArguments args)
    {
        try
        {
            return args.Command switch
            {
                CliCommand.PluginList => await PluginListAsync(args),
                CliCommand.PluginInfo => await PluginInfoAsync(args),
                CliCommand.PluginEnable => await PluginEnableAsync(args),
                CliCommand.PluginDisable => await PluginDisableAsync(args),
                CliCommand.PluginReload => await PluginReloadAsync(args),
                CliCommand.ConfigValidate => await ConfigValidateAsync(args),
                CliCommand.ConfigExport => await ConfigExportAsync(args),
                CliCommand.ConfigImport => await ConfigImportAsync(args),
                CliCommand.Diagnose => await DiagnoseAsync(args),
                CliCommand.CheckDeps => await CheckDepsAsync(args),
                CliCommand.Setup => await SetupAsync(args),
                CliCommand.Version => ShowVersion(),
                CliCommand.Help => ShowHelp(),
                _ => 0 // 交互模式
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ 错误: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
    
    // ========== 插件管理命令 ==========
    
    static async Task<int> PluginListAsync(CliArguments args)
    {
        Console.WriteLine("已安装的插件:");
        Console.WriteLine("====================");
        
        var pluginsDir = "plugins";
        if (!Directory.Exists(pluginsDir))
        {
            Console.WriteLine("(无插件)");
            return 0;
        }
        
        var pluginDirs = Directory.GetDirectories(pluginsDir);
        foreach (var dir in pluginDirs)
        {
            var pluginJsonPath = Path.Combine(dir, "plugin.json");
            if (File.Exists(pluginJsonPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(pluginJsonPath);
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<PluginMetadata>(json);
                    if (metadata != null)
                    {
                        Console.WriteLine($"  ✓ {metadata.Name} (v{metadata.Version}) - {metadata.Description}");
                        Console.WriteLine($"      ID: {metadata.Id}");
                        Console.WriteLine($"      作者: {metadata.Author ?? "未知"}");
                        Console.WriteLine();
                    }
                }
                catch
                {
                    Console.WriteLine($"  ✗ {Path.GetFileName(dir)} (无法加载元数据)");
                }
            }
        }
        
        Console.WriteLine($"共 {pluginDirs.Length} 个插件");
        return 0;
    }
    
    static async Task<int> PluginInfoAsync(CliArguments args)
    {
        if (args.Parameters.Count == 0)
        {
            Console.WriteLine("用法: NetherGate.exe plugin info <plugin-id>");
            return 1;
        }
        
        var pluginId = args.Parameters[0];
        var pluginsDir = "plugins";
        
        // 查找插件
        foreach (var dir in Directory.GetDirectories(pluginsDir))
        {
            var pluginJsonPath = Path.Combine(dir, "plugin.json");
            if (!File.Exists(pluginJsonPath)) continue;
            
            var json = await File.ReadAllTextAsync(pluginJsonPath);
            var metadata = System.Text.Json.JsonSerializer.Deserialize<PluginMetadata>(json);
            
            if (metadata?.Id == pluginId)
            {
                Console.WriteLine($"插件信息: {metadata.Name}");
                Console.WriteLine("====================");
                Console.WriteLine($"ID: {metadata.Id}");
                Console.WriteLine($"版本: {metadata.Version}");
                Console.WriteLine($"描述: {metadata.Description}");
                Console.WriteLine($"作者: {metadata.Author ?? "未知"}");
                
                if (!string.IsNullOrEmpty(metadata.Website))
                    Console.WriteLine($"主页: {metadata.Website}");
                
                if (!string.IsNullOrEmpty(metadata.Repository))
                    Console.WriteLine($"仓库: {metadata.Repository}");
                
                if (metadata.Dependencies != null && metadata.Dependencies.Count > 0)
                {
                    Console.WriteLine($"依赖:");
                    foreach (var dep in metadata.Dependencies)
                        Console.WriteLine($"  - {dep}");
                }
                
                if (metadata.LibraryDependencies != null && metadata.LibraryDependencies.Count > 0)
                {
                    Console.WriteLine($"库依赖:");
                    foreach (var dep in metadata.LibraryDependencies)
                        Console.WriteLine($"  - {dep.Name} ({dep.Version ?? "any"})");
                }
                
                return 0;
            }
        }
        
        Console.WriteLine($"✗ 未找到插件: {pluginId}");
        return 1;
    }
    
    static Task<int> PluginEnableAsync(CliArguments args)
    {
        if (args.Parameters.Count == 0)
        {
            Console.WriteLine("用法: NetherGate.exe plugin enable <plugin-id>");
            return Task.FromResult(1);
        }
        
        Console.WriteLine("✓ 插件启用功能需要在运行时通过命令行执行");
        Console.WriteLine("  使用: plugins enable <plugin-id>");
        return Task.FromResult(0);
    }
    
    static Task<int> PluginDisableAsync(CliArguments args)
    {
        Console.WriteLine("✓ 插件禁用功能需要在运行时通过命令行执行");
        Console.WriteLine("  使用: plugins disable <plugin-id>");
        return Task.FromResult(0);
    }
    
    static Task<int> PluginReloadAsync(CliArguments args)
    {
        Console.WriteLine("✓ 插件重载功能需要在运行时通过命令行执行");
        Console.WriteLine("  使用: plugins reload <plugin-id>");
        return Task.FromResult(0);
    }
    
    // ========== 配置管理命令 ==========
    
    static Task<int> ConfigValidateAsync(CliArguments args)
    {
        Console.WriteLine("验证配置文件...");
        Console.WriteLine("====================");
        
        try
        {
            var config = ConfigurationLoader.Load();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ 配置文件有效");
            Console.ResetColor();
            
            Console.WriteLine();
            Console.WriteLine("配置概要:");
            Console.WriteLine($"  服务器进程: {(config.ServerProcess.Enabled ? "启用" : "禁用")}");
            Console.WriteLine($"  SMP 连接: {config.ServerConnection.Host}:{config.ServerConnection.Port}");
            Console.WriteLine($"  RCON: {(config.Rcon.Enabled ? "启用" : "禁用")}");
            Console.WriteLine($"  日志级别: {config.Logging.Level}");
            Console.WriteLine($"  插件目录: {config.Plugins.Directory}");
            
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ 配置文件无效: {ex.Message}");
            Console.ResetColor();
            return Task.FromResult(1);
        }
    }
    
    static Task<int> ConfigExportAsync(CliArguments args)
    {
        var outputFile = args.Parameters.Count > 0 ? args.Parameters[0] : "nethergate-config.backup.yaml";
        
        try
        {
            var config = ConfigurationLoader.Load();
            var configPath = ConfigurationLoader.GetConfigPath();
            
            if (File.Exists(configPath))
            {
                File.Copy(configPath, outputFile, overwrite: true);
                Console.WriteLine($"✓ 配置已导出到: {outputFile}");
                return Task.FromResult(0);
            }
            else
            {
                Console.WriteLine("✗ 配置文件不存在");
                return Task.FromResult(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 导出失败: {ex.Message}");
            return Task.FromResult(1);
        }
    }
    
    static Task<int> ConfigImportAsync(CliArguments args)
    {
        if (args.Parameters.Count == 0)
        {
            Console.WriteLine("用法: NetherGate.exe config import <file>");
            return Task.FromResult(1);
        }
        
        var inputFile = args.Parameters[0];
        
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"✗ 文件不存在: {inputFile}");
            return Task.FromResult(1);
        }
        
        try
        {
            var configPath = ConfigurationLoader.GetConfigPath();
            File.Copy(inputFile, configPath, overwrite: true);
            
            // 验证导入的配置
            var config = ConfigurationLoader.Load();
            
            Console.WriteLine($"✓ 配置已导入");
            Console.WriteLine("请重启 NetherGate 以应用新配置");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 导入失败: {ex.Message}");
            return Task.FromResult(1);
        }
    }
    
    // ========== 诊断工具 ==========
    
    static Task<int> DiagnoseAsync(CliArguments args)
    {
        Console.WriteLine("NetherGate 诊断报告");
        Console.WriteLine("====================");
        Console.WriteLine();
        
        // 系统信息
        Console.WriteLine("系统信息:");
        Console.WriteLine($"  OS: {Environment.OSVersion}");
        Console.WriteLine($"  .NET: {Environment.Version}");
        Console.WriteLine($"  工作目录: {Environment.CurrentDirectory}");
        Console.WriteLine();
        
        // 配置文件
        Console.WriteLine("配置文件:");
        var configPath = ConfigurationLoader.GetConfigPath();
        if (File.Exists(configPath))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ 存在: {configPath}");
            Console.ResetColor();
            
            try
            {
                var config = ConfigurationLoader.Load();
                Console.WriteLine($"  ✓ 有效");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ 无效: {ex.Message}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ 不存在: {configPath}");
            Console.ResetColor();
        }
        Console.WriteLine();
        
        // 目录结构
        Console.WriteLine("目录结构:");
        CheckDirectory("plugins", "插件目录");
        CheckDirectory("config", "配置目录");
        CheckDirectory("lib", "库目录");
        CheckDirectory("logs", "日志目录");
        Console.WriteLine();
        
        // 插件
        Console.WriteLine("插件:");
        if (Directory.Exists("plugins"))
        {
            var pluginDirs = Directory.GetDirectories("plugins");
            Console.WriteLine($"  发现 {pluginDirs.Length} 个插件目录");
            
            foreach (var dir in pluginDirs)
            {
                var name = Path.GetFileName(dir);
                var pluginJsonPath = Path.Combine(dir, "plugin.json");
                
                if (File.Exists(pluginJsonPath))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ {name}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ {name} (缺少 plugin.json)");
                    Console.ResetColor();
                }
            }
        }
        else
        {
            Console.WriteLine("  插件目录不存在");
        }
        
        return Task.FromResult(0);
    }
    
    static async Task<int> CheckDepsAsync(CliArguments args)
    {
        Console.WriteLine("检查依赖...");
        Console.WriteLine("====================");
        
        // 检查 lib 目录
        if (Directory.Exists("lib"))
        {
            var dlls = Directory.GetFiles("lib", "*.dll");
            Console.WriteLine($"lib/ 目录中的依赖 ({dlls.Length}):");
            foreach (var dll in dlls)
            {
                Console.WriteLine($"  ✓ {Path.GetFileName(dll)}");
            }
        }
        else
        {
            Console.WriteLine("lib/ 目录不存在");
        }
        
        Console.WriteLine();
        
        // 检查插件依赖
        Console.WriteLine("插件依赖:");
        if (Directory.Exists("plugins"))
        {
            foreach (var dir in Directory.GetDirectories("plugins"))
            {
                var pluginJsonPath = Path.Combine(dir, "plugin.json");
                if (!File.Exists(pluginJsonPath)) continue;
                
                var json = await File.ReadAllTextAsync(pluginJsonPath);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<PluginMetadata>(json);
                
                if (metadata?.LibraryDependencies != null && metadata.LibraryDependencies.Count > 0)
                {
                    Console.WriteLine($"  {metadata.Name}:");
                    foreach (var dep in metadata.LibraryDependencies)
                    {
                        var depPath = dep.Location == "local" 
                            ? Path.Combine(dir, $"{dep.Name}.dll")
                            : Path.Combine("lib", $"{dep.Name}.dll");
                        
                        if (File.Exists(depPath))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"    ✓ {dep.Name} ({dep.Version ?? "any"})");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"    ✗ {dep.Name} ({dep.Version ?? "any"}) - 缺失");
                            Console.ResetColor();
                        }
                    }
                }
            }
        }
        
        return 0;
    }
    
    // ========== 向导 ==========
    
    static async Task<int> SetupAsync(CliArguments args)
    {
        return await ConfigurationWizard.RunAsync();
    }
    
    // ========== 其他命令 ==========
    
    static int ShowVersion()
    {
        Console.WriteLine("NetherGate v0.1.0-alpha");
        Console.WriteLine(".NET 9.0");
        Console.WriteLine("https://github.com/BlockBridge/NetherGate");
        return 0;
    }
    
    static int ShowHelp()
    {
        Console.WriteLine(@"NetherGate - Minecraft 服务器插件加载器

用法: NetherGate.exe [命令] [选项]

命令:
  plugin list               列出所有插件
  plugin info <id>          查看插件详情
  plugin enable <id>        启用插件 (运行时)
  plugin disable <id>       禁用插件 (运行时)
  plugin reload <id>        重载插件 (运行时)

  config validate           验证配置文件
  config export [file]      导出配置
  config import <file>      导入配置

  diagnose                  运行诊断
  check-deps                检查依赖

  setup, --setup, -s        运行配置向导
  version, --version, -v    显示版本
  help, --help, -h          显示帮助

示例:
  NetherGate.exe                      # 启动交互模式
  NetherGate.exe --setup              # 运行配置向导
  NetherGate.exe plugin list          # 列出插件
  NetherGate.exe config validate      # 验证配置
  NetherGate.exe diagnose             # 运行诊断
");
        return 0;
    }
    
    // ========== 辅助方法 ==========
    
    static void CheckDirectory(string path, string name)
    {
        if (Directory.Exists(path))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {name}: {path}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ {name}: {path} (不存在)");
            Console.ResetColor();
        }
    }
}
