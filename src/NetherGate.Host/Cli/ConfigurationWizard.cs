using NetherGate.API.Configuration;
using NetherGate.Core.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetherGate.Host.Cli;

/// <summary>
/// 交互式配置向导
/// </summary>
public class ConfigurationWizard
{
    private static NetherGateConfig _config = new();
    
    /// <summary>
    /// 运行配置向导
    /// </summary>
    public static async Task<int> RunAsync()
    {
        Console.Clear();
        PrintWelcome();
        
        // 检查是否已有配置文件
        var configPath = ConfigurationLoader.GetConfigPath();
        if (File.Exists(configPath))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ 检测到现有配置文件");
            Console.ResetColor();
            Console.WriteLine($"位置: {configPath}");
            Console.WriteLine();
            
            if (!AskYesNo("是否要覆盖现有配置？", defaultYes: false))
            {
                Console.WriteLine("已取消配置向导");
                return 0;
            }
            
            Console.WriteLine();
        }
        
        // 步骤 1: 服务器进程配置
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  步骤 1/5: 服务器进程管理");
        Console.WriteLine("========================================");
        Console.ResetColor();
        Console.WriteLine();
        
        await ConfigureServerProcessAsync();
        
        // 步骤 2: SMP 连接配置
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  步骤 2/5: SMP (Server Management Protocol)");
        Console.WriteLine("========================================");
        Console.ResetColor();
        Console.WriteLine();
        
        await ConfigureSmpAsync();
        
        // 步骤 3: RCON 配置
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  步骤 3/5: RCON (Remote Console)");
        Console.WriteLine("========================================");
        Console.ResetColor();
        Console.WriteLine();
        
        await ConfigureRconAsync();
        
        // 步骤 4: 日志系统配置
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  步骤 4/5: 日志系统");
        Console.WriteLine("========================================");
        Console.ResetColor();
        Console.WriteLine();
        
        await ConfigureLoggingAsync();
        
        // 步骤 5: 插件系统配置
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  步骤 5/5: 插件系统");
        Console.WriteLine("========================================");
        Console.ResetColor();
        Console.WriteLine();
        
        await ConfigurePluginsAsync();
        
        // 保存配置
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("========================================");
        Console.WriteLine("  配置完成");
        Console.WriteLine("========================================");
        Console.ResetColor();
        Console.WriteLine();
        
        DisplayConfigSummary();
        
        Console.WriteLine();
        if (AskYesNo("确认保存配置？", defaultYes: true))
        {
            try
            {
                var savePath = ConfigurationLoader.GetConfigPath();
                ConfigurationLoader.SaveConfig(savePath, _config);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine("✓ 配置已保存！");
                Console.ResetColor();
                Console.WriteLine($"位置: {savePath}");
                Console.WriteLine();
                Console.WriteLine("提示: 你可以随时手动编辑配置文件");
                Console.WriteLine("运行 'NetherGate.exe config validate' 来验证配置");
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ 保存配置失败: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }
        else
        {
            Console.WriteLine("已取消保存");
            return 0;
        }
    }
    
    // ========== 配置步骤 ==========
    
    static Task ConfigureServerProcessAsync()
    {
        return Task.Run(() =>
        {
        Console.WriteLine("NetherGate 可以管理 Minecraft 服务器进程，或连接到外部运行的服务器。");
        Console.WriteLine();
        
        var launchMethod = AskChoice(
            "选择服务器启动方式:",
            new[] {
                ("java", "使用 Java 直接启动 (推荐)"),
                ("script", "使用脚本启动"),
                ("external", "连接到外部服务器")
            },
            defaultChoice: "java"
        );
        
        _config.ServerProcess.LaunchMethod = launchMethod;
        
        if (launchMethod == "external")
        {
            Console.WriteLine();
            Console.WriteLine("外部模式下，NetherGate 不会管理服务器进程。");
            Console.WriteLine("请确保服务器已启用 SMP/RCON 支持。");
            
            _config.ServerProcess.Enabled = false;
            _config.ServerProcess.Server.WorkingDirectory = AskString(
                "服务器工作目录:",
                "server"
            );
        }
        else
        {
            _config.ServerProcess.Enabled = true;
            
            Console.WriteLine();
            _config.ServerProcess.Server.WorkingDirectory = AskString(
                "服务器工作目录:",
                "server"
            );
            
            if (launchMethod == "java")
            {
                Console.WriteLine();
                _config.ServerProcess.Java.Path = AskString(
                    "Java 可执行文件路径:",
                    "java"
                );
                
                _config.ServerProcess.Server.Jar = AskString(
                    "服务器 JAR 文件名:",
                    "server.jar"
                );
                
                Console.WriteLine();
                Console.WriteLine("内存配置 (MB):");
                _config.ServerProcess.Memory.Min = AskInt(
                    "  最小内存:",
                    1024,
                    min: 512
                );
                
                _config.ServerProcess.Memory.Max = AskInt(
                    "  最大内存:",
                    2048,
                    min: _config.ServerProcess.Memory.Min
                );
                
                Console.WriteLine();
                if (AskYesNo("是否需要自定义 JVM 参数？", defaultYes: false))
                {
                    Console.WriteLine("输入 JVM 参数 (每行一个，输入空行结束):");
                    var jvmArgs = new List<string>();
                    while (true)
                    {
                        Console.Write("  ");
                        var line = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) break;
                        jvmArgs.Add(line);
                    }
                    _config.ServerProcess.Arguments.JvmMiddle = jvmArgs;
                }
            }
            else if (launchMethod == "script")
            {
                Console.WriteLine();
                _config.ServerProcess.Script.Path = AskString(
                    "启动脚本路径:",
                    "start.sh"
                );
            }
            
            Console.WriteLine();
            var autoRestart = AskYesNo(
                "服务器崩溃时自动重启？",
                defaultYes: true
            );
            
            _config.ServerProcess.AutoRestart.Enabled = autoRestart;
            
            if (autoRestart)
            {
                _config.ServerProcess.AutoRestart.DelaySeconds = AskInt(
                    "重启延迟 (秒):",
                    5,
                    min: 0
                );
            }
        }
        });
    }
    
    static Task ConfigureSmpAsync()
    {
        return Task.Run(() =>
        {
        Console.WriteLine("SMP (Server Management Protocol) 用于与 Minecraft 服务器通信。");
        Console.WriteLine("需要服务器安装支持 SMP 的插件或 Mod。");
        Console.WriteLine();
        
        _config.ServerConnection.Host = AskString(
            "SMP 服务器地址:",
            "localhost"
        );
        
        _config.ServerConnection.Port = AskInt(
            "SMP 端口:",
            8765,
            min: 1,
            max: 65535
        );
        
        Console.WriteLine();
        _config.ServerConnection.UseTls = AskYesNo(
            "使用 TLS 加密连接？",
            defaultYes: false
        );
        
        if (_config.ServerConnection.UseTls)
        {
            _config.ServerConnection.Secret = AskPassword(
                "TLS 密钥 (留空则自动生成):"
            );
            
            if (string.IsNullOrEmpty(_config.ServerConnection.Secret))
            {
                _config.ServerConnection.Secret = Guid.NewGuid().ToString("N");
            }
        }
        
        Console.WriteLine();
        _config.ServerConnection.AutoConnect = AskYesNo(
            "启动时自动连接？",
            defaultYes: true
        );
        });
    }
    
    static Task ConfigureRconAsync()
    {
        return Task.Run(() =>
        {
        Console.WriteLine("RCON (Remote Console) 用于向服务器发送命令。");
        Console.WriteLine("需要在 server.properties 中启用 RCON。");
        Console.WriteLine();
        
        _config.Rcon.Enabled = AskYesNo(
            "启用 RCON 支持？",
            defaultYes: true
        );
        
        if (_config.Rcon.Enabled)
        {
            Console.WriteLine();
            _config.Rcon.Host = AskString(
                "RCON 地址:",
                "localhost"
            );
            
            _config.Rcon.Port = AskInt(
                "RCON 端口:",
                25575,
                min: 1,
                max: 65535
            );
            
            _config.Rcon.Password = AskPassword(
                "RCON 密码:"
            );
            
            if (string.IsNullOrEmpty(_config.Rcon.Password))
            {
                _config.Rcon.Password = Guid.NewGuid().ToString("N").Substring(0, 16);
                Console.WriteLine($"已生成随机密码: {_config.Rcon.Password}");
                Console.WriteLine("请在 server.properties 中设置相同的密码");
            }
            
            Console.WriteLine();
            _config.Rcon.AutoConnect = AskYesNo(
                "启动时自动连接？",
                defaultYes: true
            );
        }
        });
    }
    
    static Task ConfigureLoggingAsync()
    {
        return Task.Run(() =>
        {
        var level = AskChoice(
            "日志级别:",
            new[] {
                ("Debug", "调试 (最详细)"),
                ("Info", "信息 (推荐)"),
                ("Warning", "警告"),
                ("Error", "错误 (最少)")
            },
            defaultChoice: "Info"
        );
        
        _config.Logging.Level = level;
        
        Console.WriteLine();
        _config.Logging.Console.Enabled = AskYesNo(
            "启用控制台日志？",
            defaultYes: true
        );
        
        if (_config.Logging.Console.Enabled)
        {
            _config.Logging.Console.Colored = AskYesNo(
                "使用彩色输出？",
                defaultYes: true
            );
        }
        
        Console.WriteLine();
        _config.Logging.File.Enabled = AskYesNo(
            "启用文件日志？",
            defaultYes: true
        );
        
        if (_config.Logging.File.Enabled)
        {
            _config.Logging.File.Path = AskString(
                "日志文件路径:",
                "logs/latest.log"
            );
            
            _config.Logging.File.MaxSize = AskInt(
                "单文件最大大小 (MB):",
                10,
                min: 1
            );
            
            _config.Logging.File.MaxFiles = AskInt(
                "保留文件数量:",
                5,
                min: 1
            );
        }
        });
    }
    
    static Task ConfigurePluginsAsync()
    {
        return Task.Run(() =>
        {
        _config.Plugins.Directory = AskString(
            "插件目录:",
            "plugins"
        );
        
        Console.WriteLine();
        _config.Plugins.AutoLoad = AskYesNo(
            "自动加载所有插件？",
            defaultYes: true
        );
        
        _config.Plugins.HotReload = AskYesNo(
            "启用热重载 (实验性)？",
            defaultYes: true
        );
        
        Console.WriteLine();
        Console.WriteLine("依赖管理:");
        _config.Plugins.DependencyManagement.Enabled = AskYesNo(
            "  启用自动依赖管理？",
            defaultYes: true
        );
        
        if (_config.Plugins.DependencyManagement.Enabled)
        {
            _config.Plugins.DependencyManagement.AutoDownload = AskYesNo(
                "  自动下载缺失的依赖？",
                defaultYes: true
            );
            
            var strategy = AskChoice(
                "  版本冲突解决策略:",
                new[] {
                    ("highest", "使用最高版本 (推荐)"),
                    ("lowest", "使用最低兼容版本"),
                    ("fail", "报错，手动处理")
                },
                defaultChoice: "highest"
            );
            
            _config.Plugins.DependencyManagement.ConflictResolution = strategy;
        }
        });
    }
    
    // ========== UI 辅助方法 ==========
    
    static void PrintWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║          NetherGate 配置向导                           ║
║                                                        ║
║  此向导将帮助你快速配置 NetherGate                     ║
║  你可以随时手动编辑配置文件                            ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
");
        Console.ResetColor();
    }
    
    static void DisplayConfigSummary()
    {
        Console.WriteLine("配置摘要:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        Console.WriteLine($"服务器进程:");
        Console.WriteLine($"  启动方式: {_config.ServerProcess.LaunchMethod}");
        Console.WriteLine($"  工作目录: {_config.ServerProcess.Server.WorkingDirectory}");
        if (_config.ServerProcess.Enabled && _config.ServerProcess.LaunchMethod == "java")
        {
            Console.WriteLine($"  Java: {_config.ServerProcess.Java.Path}");
            Console.WriteLine($"  JAR: {_config.ServerProcess.Server.Jar}");
            Console.WriteLine($"  内存: {_config.ServerProcess.Memory.Min}MB - {_config.ServerProcess.Memory.Max}MB");
        }
        
        Console.WriteLine();
        Console.WriteLine($"SMP 连接:");
        Console.WriteLine($"  地址: {_config.ServerConnection.Host}:{_config.ServerConnection.Port}");
        Console.WriteLine($"  TLS: {(_config.ServerConnection.UseTls ? "启用" : "禁用")}");
        Console.WriteLine($"  自动连接: {(_config.ServerConnection.AutoConnect ? "是" : "否")}");
        
        Console.WriteLine();
        Console.WriteLine($"RCON:");
        Console.WriteLine($"  状态: {(_config.Rcon.Enabled ? "启用" : "禁用")}");
        if (_config.Rcon.Enabled)
        {
            Console.WriteLine($"  地址: {_config.Rcon.Host}:{_config.Rcon.Port}");
            Console.WriteLine($"  自动连接: {(_config.Rcon.AutoConnect ? "是" : "否")}");
        }
        
        Console.WriteLine();
        Console.WriteLine($"日志:");
        Console.WriteLine($"  级别: {_config.Logging.Level}");
        Console.WriteLine($"  控制台: {(_config.Logging.Console.Enabled ? "启用" : "禁用")}");
        Console.WriteLine($"  文件: {(_config.Logging.File.Enabled ? "启用" : "禁用")}");
        
        Console.WriteLine();
        Console.WriteLine($"插件:");
        Console.WriteLine($"  目录: {_config.Plugins.Directory}");
        Console.WriteLine($"  自动加载: {(_config.Plugins.AutoLoad ? "是" : "否")}");
        Console.WriteLine($"  依赖管理: {(_config.Plugins.DependencyManagement.Enabled ? "启用" : "禁用")}");
    }
    
    static bool AskYesNo(string prompt, bool defaultYes = true)
    {
        var defaultText = defaultYes ? "Y/n" : "y/N";
        Console.Write($"{prompt} [{defaultText}] ");
        
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (string.IsNullOrEmpty(input))
            return defaultYes;
        
        return input == "y" || input == "yes";
    }
    
    static string AskString(string prompt, string defaultValue = "")
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            Console.Write($"{prompt} [{defaultValue}] ");
        }
        else
        {
            Console.Write($"{prompt} ");
        }
        
        var input = Console.ReadLine()?.Trim();
        
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }
    
    static string AskPassword(string prompt)
    {
        Console.Write($"{prompt} ");
        
        var password = "";
        ConsoleKeyInfo key;
        
        do
        {
            key = Console.ReadKey(intercept: true);
            
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write("*");
            }
        }
        while (key.Key != ConsoleKey.Enter);
        
        Console.WriteLine();
        return password;
    }
    
    static int AskInt(string prompt, int defaultValue, int? min = null, int? max = null)
    {
        while (true)
        {
            Console.Write($"{prompt} [{defaultValue}] ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
                return defaultValue;
            
            if (int.TryParse(input, out var value))
            {
                if (min.HasValue && value < min.Value)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ 值不能小于 {min.Value}");
                    Console.ResetColor();
                    continue;
                }
                
                if (max.HasValue && value > max.Value)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ 值不能大于 {max.Value}");
                    Console.ResetColor();
                    continue;
                }
                
                return value;
            }
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ 请输入有效的数字");
            Console.ResetColor();
        }
    }
    
    static string AskChoice(string prompt, (string value, string description)[] choices, string defaultChoice)
    {
        Console.WriteLine(prompt);
        
        for (int i = 0; i < choices.Length; i++)
        {
            var marker = choices[i].value == defaultChoice ? "*" : " ";
            Console.WriteLine($"  {marker} [{i + 1}] {choices[i].description}");
        }
        
        while (true)
        {
            Console.Write($"选择 [1-{choices.Length}] (默认: {Array.FindIndex(choices, c => c.value == defaultChoice) + 1}): ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
                return defaultChoice;
            
            if (int.TryParse(input, out var index) && index >= 1 && index <= choices.Length)
            {
                return choices[index - 1].value;
            }
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ 请输入 1 到 {choices.Length} 之间的数字");
            Console.ResetColor();
        }
    }
}
