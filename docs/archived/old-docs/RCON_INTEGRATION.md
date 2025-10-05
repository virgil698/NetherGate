# NetherGate RCON 集成文档

本文档说明如何通过RCON实现游戏内命令和更灵活的服务器控制。

---

## 📋 目录

- [概述](#概述)
- [SMP vs RCON 对比](#smp-vs-rcon-对比)
- [组合使用架构](#组合使用架构)
- [RCON API](#rcon-api)
- [游戏内命令实现](#游戏内命令实现)
- [完整使用示例](#完整使用示例)
- [最佳实践](#最佳实践)

---

## 概述

NetherGate 提供三种与Minecraft服务器交互的方式：

| 方式 | 用途 | 优势 |
|------|------|------|
| **SMP** | 结构化服务器管理 | 类型安全、实时通知、现代化 |
| **RCON** | 执行游戏命令 | 功能全面、灵活性高 |
| **日志监听** | 捕获游戏事件 | 实时监控、事件驱动 |

**核心理念：三者配合使用，发挥各自优势**

---

## SMP vs RCON 对比

### 功能对比表

| 功能 | SMP | RCON |
|------|-----|------|
| 白名单管理 | ✅ 结构化API | ✅ `whitelist add/remove` |
| 封禁管理 | ✅ 结构化API | ✅ `ban/pardon` |
| 玩家管理 | ✅ 获取列表、踢出 | ✅ `kick` |
| 游戏规则 | ✅ 结构化API | ✅ `gamerule` |
| **执行任意命令** | ❌ 不支持 | ✅ **支持** |
| **给予物品** | ❌ 不支持 | ✅ `give` |
| **传送玩家** | ❌ 不支持 | ✅ `tp` |
| **发送消息** | ✅ `system_message` | ✅ `say/tell/tellraw` |
| **执行函数** | ❌ 不支持 | ✅ `function` |
| **自定义命令** | ❌ 不支持 | ✅ **通过监听+RCON** |
| 实时通知 | ✅ WebSocket推送 | ❌ 不支持 |
| 返回值类型 | ✅ 结构化JSON | ⚠️ 文本（需解析） |

### 使用建议

**使用SMP的场景：**
- ✅ 白名单、封禁等服务器管理
- ✅ 需要实时通知（玩家加入/离开等）
- ✅ 需要结构化数据（玩家列表、游戏规则等）

**使用RCON的场景：**
- ✅ 给予物品、经验值
- ✅ 传送玩家、改变游戏模式
- ✅ 执行数据包函数
- ✅ 发送富文本消息（tellraw）
- ✅ **实现游戏内自定义命令**

---

## 组合使用架构

```
玩家在游戏中输入: /myplugin give diamond
           ↓
    (1) Minecraft服务器日志输出
           ↓
    (2) NetherGate 日志监听器捕获
           ↓
    (3) 解析命令并触发插件处理器
           ↓
    (4) 插件通过 RCON 执行游戏命令
           ↓
    (5) /give PlayerName diamond 1
           ↓
    (6) 玩家获得钻石
```

---

## RCON API

### IRconClient 接口

```csharp
namespace NetherGate.API
{
    /// <summary>
    /// RCON 客户端接口
    /// </summary>
    public interface IRconClient
    {
        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// 连接到RCON服务器
        /// </summary>
        Task<bool> ConnectAsync(string host, int port, string password);
        
        /// <summary>
        /// 断开连接
        /// </summary>
        Task DisconnectAsync();
        
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">要执行的命令（不需要斜杠）</param>
        /// <returns>命令执行结果</returns>
        Task<RconResponse> ExecuteCommandAsync(string command);
        
        /// <summary>
        /// 批量执行命令
        /// </summary>
        Task<List<RconResponse>> ExecuteCommandsAsync(params string[] commands);
    }
    
    /// <summary>
    /// RCON 响应
    /// </summary>
    public record RconResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; init; }
        
        /// <summary>
        /// 响应消息
        /// </summary>
        public string Message { get; init; } = "";
        
        /// <summary>
        /// 原始响应
        /// </summary>
        public string RawResponse { get; init; } = "";
    }
}
```

### 在插件中使用RCON

```csharp
public abstract class PluginBase : IPlugin
{
    // ... 现有属性 ...
    
    /// <summary>
    /// RCON 客户端（如果配置了RCON）
    /// </summary>
    protected IRconClient? Rcon { get; private set; }
}
```

---

## 游戏内命令实现

### 方案1：命令前缀监听（推荐）

玩家在游戏中输入带有特定前缀的命令，插件捕获并处理。

#### 服务器日志输出示例

```
[12:34:56] [Server thread/INFO]: Steve issued server command: /myplugin give diamond
```

#### 实现代码

```csharp
using NetherGate.API;
using NetherGate.API.Events;

namespace CustomCommands
{
    public class CustomCommandPlugin : PluginBase
    {
        private const string CommandPrefix = "/myplugin";
        
        public override Task OnEnableAsync()
        {
            // 订阅服务器日志输出
            Server.SubscribeToServerLog(OnServerLog);
            
            Logger.Info("游戏内命令插件已启用");
            Logger.Info($"玩家可以在游戏中使用 {CommandPrefix} 命令");
            return Task.CompletedTask;
        }
        
        private async void OnServerLog(ServerLogEntry entry)
        {
            // 检测玩家命令执行
            if (entry.Type == LogEntryType.PlayerCommand)
            {
                var match = Regex.Match(entry.Message, 
                    @"(\w+) issued server command: (.+)");
                
                if (!match.Success) return;
                
                var playerName = match.Groups[1].Value;
                var command = match.Groups[2].Value;
                
                // 检查是否是我们的命令
                if (!command.StartsWith(CommandPrefix)) return;
                
                // 解析命令参数
                var args = command.Substring(CommandPrefix.Length)
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (args.Length == 0) return;
                
                // 处理命令
                await HandleGameCommandAsync(playerName, args);
            }
        }
        
        private async Task HandleGameCommandAsync(string playerName, string[] args)
        {
            try
            {
                switch (args[0].ToLower())
                {
                    case "give":
                        await HandleGiveCommand(playerName, args);
                        break;
                    
                    case "tp":
                        await HandleTeleportCommand(playerName, args);
                        break;
                    
                    case "heal":
                        await HandleHealCommand(playerName);
                        break;
                    
                    default:
                        await SendGameMessage(playerName, 
                            $"§c未知命令: {args[0]}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"处理游戏命令时出错: {ex.Message}", ex);
                await SendGameMessage(playerName, "§c命令执行失败");
            }
        }
        
        private async Task HandleGiveCommand(string playerName, string[] args)
        {
            // 用法: /myplugin give <物品> [数量]
            if (args.Length < 2)
            {
                await SendGameMessage(playerName, 
                    "§c用法: /myplugin give <物品> [数量]");
                return;
            }
            
            var itemName = args[1];
            var count = args.Length >= 3 && int.TryParse(args[2], out var c) ? c : 1;
            
            // 通过RCON执行give命令
            if (Rcon?.IsConnected == true)
            {
                var response = await Rcon.ExecuteCommandAsync(
                    $"give {playerName} {itemName} {count}");
                
                if (response.Success)
                {
                    await SendGameMessage(playerName, 
                        $"§a成功给予 {count} 个 {itemName}");
                    Logger.Info($"给予 {playerName}: {itemName} x{count}");
                }
                else
                {
                    await SendGameMessage(playerName, 
                        $"§c给予失败: {response.Message}");
                }
            }
        }
        
        private async Task HandleTeleportCommand(string playerName, string[] args)
        {
            // 用法: /myplugin tp <x> <y> <z>
            if (args.Length < 4)
            {
                await SendGameMessage(playerName, 
                    "§c用法: /myplugin tp <x> <y> <z>");
                return;
            }
            
            if (Rcon?.IsConnected == true)
            {
                var response = await Rcon.ExecuteCommandAsync(
                    $"tp {playerName} {args[1]} {args[2]} {args[3]}");
                
                if (response.Success)
                {
                    await SendGameMessage(playerName, 
                        $"§a已传送到 ({args[1]}, {args[2]}, {args[3]})");
                }
            }
        }
        
        private async Task HandleHealCommand(string playerName)
        {
            if (Rcon?.IsConnected == true)
            {
                await Rcon.ExecuteCommandsAsync(
                    $"effect clear {playerName}",
                    $"effect give {playerName} instant_health 1 10",
                    $"effect give {playerName} saturation 1 10"
                );
                
                await SendGameMessage(playerName, "§a已治疗");
            }
        }
        
        /// <summary>
        /// 向玩家发送游戏内消息
        /// </summary>
        private async Task SendGameMessage(string playerName, string message)
        {
            if (Rcon?.IsConnected == true)
            {
                await Rcon.ExecuteCommandAsync($"tell {playerName} {message}");
            }
        }
        
        /// <summary>
        /// 向所有玩家广播
        /// </summary>
        private async Task BroadcastMessage(string message)
        {
            if (Rcon?.IsConnected == true)
            {
                await Rcon.ExecuteCommandAsync($"say {message}");
            }
        }
    }
}
```

---

### 方案2：可点击消息（tellraw）

创建玩家可以点击的交互式消息。

```csharp
public class InteractiveMenu : PluginBase
{
    public async Task ShowMenuToPlayer(string playerName)
    {
        if (Rcon?.IsConnected != true) return;
        
        var json = @"[
            {""text"":""========= 插件菜单 ========="",""color"":""gold""},
            ""\n"",
            {
                ""text"":""[领取奖励]"",
                ""color"":""green"",
                ""clickEvent"":{
                    ""action"":""run_command"",
                    ""value"":""/myplugin claim""
                },
                ""hoverEvent"":{
                    ""action"":""show_text"",
                    ""contents"":""点击领取每日奖励""
                }
            },
            "" "",
            {
                ""text"":""[传送回家]"",
                ""color"":""aqua"",
                ""clickEvent"":{
                    ""action"":""run_command"",
                    ""value"":""/myplugin home""
                },
                ""hoverEvent"":{
                    ""action"":""show_text"",
                    ""contents"":""传送到家园""
                }
            }
        ]";
        
        await Rcon.ExecuteCommandAsync($"tellraw {playerName} {json}");
    }
}
```

---

## 完整使用示例

### 示例1：VIP系统

结合SMP（管理）+ RCON（游戏命令）+ 日志监听（事件）

```csharp
using NetherGate.API;
using NetherGate.API.Events;
using NetherGate.API.Protocol;

namespace VipSystem
{
    public class VipSystem : PluginBase
    {
        private HashSet<Guid> _vipPlayers = new();
        
        public override async Task OnLoadAsync()
        {
            // 从配置加载VIP列表
            _vipPlayers = Config.Get<HashSet<Guid>>("vip_players", new());
        }
        
        public override Task OnEnableAsync()
        {
            // 订阅玩家加入事件（通过SMP）
            Events.Subscribe<PlayerJoinedEvent>(
                this, 
                EventPriority.Normal, 
                OnPlayerJoined);
            
            // 订阅服务器日志（监听命令）
            Server.SubscribeToServerLog(OnServerLog);
            
            // 注册NetherGate命令
            Commands.Register(this, new CommandDefinition
            {
                Name = "vip",
                Description = "VIP管理",
                Handler = HandleVipCommand
            });
            
            Logger.Info("VIP系统已启用");
            return Task.CompletedTask;
        }
        
        // SMP事件：玩家加入
        private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
        {
            if (_vipPlayers.Contains(e.Player.Uuid))
            {
                Logger.Info($"VIP玩家 {e.Player.Name} 加入服务器");
                
                // 通过SMP发送欢迎消息
                await Server.SendSystemMessageAsync(
                    $"§6[VIP] §e欢迎尊贵的 {e.Player.Name} 回来！");
                
                // 通过RCON给予特权
                if (Rcon?.IsConnected == true)
                {
                    await Rcon.ExecuteCommandsAsync(
                        $"effect give {e.Player.Name} speed 60 0",
                        $"effect give {e.Player.Name} night_vision 300 0"
                    );
                }
            }
        }
        
        // 日志监听：捕获游戏内命令
        private async void OnServerLog(ServerLogEntry entry)
        {
            if (entry.Type != LogEntryType.PlayerCommand) return;
            
            var match = Regex.Match(entry.Message, 
                @"(\w+) issued server command: /vip (.+)");
            
            if (!match.Success) return;
            
            var playerName = match.Groups[1].Value;
            var subCommand = match.Groups[2].Value.Trim();
            
            // 获取玩家信息（通过SMP）
            var players = await Server.GetPlayersAsync();
            var player = players.FirstOrDefault(p => 
                p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            
            if (player == null) return;
            
            // 检查是否是VIP
            if (!_vipPlayers.Contains(player.Uuid))
            {
                if (Rcon?.IsConnected == true)
                {
                    await Rcon.ExecuteCommandAsync(
                        $"tell {playerName} §c你不是VIP，无法使用此功能");
                }
                return;
            }
            
            // 处理VIP命令
            await HandleVipGameCommand(playerName, subCommand);
        }
        
        private async Task HandleVipGameCommand(string playerName, string command)
        {
            if (Rcon?.IsConnected != true) return;
            
            switch (command.ToLower())
            {
                case "fly":
                    await Rcon.ExecuteCommandAsync(
                        $"gamemode creative {playerName}");
                    await Rcon.ExecuteCommandAsync(
                        $"tell {playerName} §a飞行模式已开启");
                    break;
                
                case "kit":
                    await Rcon.ExecuteCommandsAsync(
                        $"give {playerName} diamond_sword 1",
                        $"give {playerName} diamond_pickaxe 1",
                        $"give {playerName} cooked_beef 64"
                    );
                    await Rcon.ExecuteCommandAsync(
                        $"tell {playerName} §aVIP礼包已发放");
                    break;
                
                case "home":
                    // 从配置读取VIP家园坐标
                    await Rcon.ExecuteCommandAsync(
                        $"tp {playerName} 0 100 0");
                    break;
            }
        }
        
        // NetherGate命令：管理VIP
        private async Task<CommandResult> HandleVipCommand(CommandContext ctx)
        {
            if (ctx.Args.Count < 2)
                return CommandResult.Error("用法: vip <add|remove|list> [玩家]");
            
            var action = ctx.Args[0].ToLower();
            
            switch (action)
            {
                case "add":
                    var playerName = ctx.Args[1];
                    var players = await Server.GetPlayersAsync();
                    var target = players.FirstOrDefault(p => 
                        p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                    
                    if (target == null)
                        return CommandResult.Error("玩家不在线");
                    
                    _vipPlayers.Add(target.Uuid);
                    await Config.SaveAsync();
                    
                    // 通知游戏内玩家
                    if (Rcon?.IsConnected == true)
                    {
                        await Rcon.ExecuteCommandAsync(
                            $"tell {playerName} §a恭喜！你已成为VIP");
                    }
                    
                    return CommandResult.Ok($"已将 {playerName} 添加到VIP");
                
                case "list":
                    return CommandResult.Ok($"VIP数量: {_vipPlayers.Count}");
                
                default:
                    return CommandResult.Error("未知操作");
            }
        }
        
        public override async Task OnDisableAsync()
        {
            // 保存VIP列表
            Config.Set("vip_players", _vipPlayers);
            await Config.SaveAsync();
        }
    }
}
```

---

### 示例2：经济系统

```csharp
public class EconomyPlugin : PluginBase
{
    private Dictionary<Guid, decimal> _balances = new();
    
    public override Task OnEnableAsync()
    {
        Server.SubscribeToServerLog(OnServerLog);
        return Task.CompletedTask;
    }
    
    private async void OnServerLog(ServerLogEntry entry)
    {
        if (entry.Type != LogEntryType.PlayerCommand) return;
        
        var match = Regex.Match(entry.Message, 
            @"(\w+) issued server command: /money (.+)");
        
        if (!match.Success) return;
        
        var playerName = match.Groups[1].Value;
        var args = match.Groups[2].Value.Split(' ');
        
        if (args[0] == "pay" && args.Length >= 3)
        {
            var targetName = args[1];
            if (decimal.TryParse(args[2], out var amount))
            {
                await HandlePayment(playerName, targetName, amount);
            }
        }
        else if (args[0] == "balance")
        {
            await ShowBalance(playerName);
        }
    }
    
    private async Task HandlePayment(string from, string to, decimal amount)
    {
        // 获取玩家UUID（通过SMP）
        var players = await Server.GetPlayersAsync();
        var fromPlayer = players.FirstOrDefault(p => p.Name == from);
        var toPlayer = players.FirstOrDefault(p => p.Name == to);
        
        if (fromPlayer == null || toPlayer == null) return;
        
        // 检查余额
        if (!_balances.ContainsKey(fromPlayer.Uuid) || 
            _balances[fromPlayer.Uuid] < amount)
        {
            if (Rcon?.IsConnected == true)
            {
                await Rcon.ExecuteCommandAsync(
                    $"tell {from} §c余额不足");
            }
            return;
        }
        
        // 转账
        _balances[fromPlayer.Uuid] -= amount;
        _balances[toPlayer.Uuid] = _balances.GetValueOrDefault(toPlayer.Uuid) + amount;
        
        // 通知双方
        if (Rcon?.IsConnected == true)
        {
            await Rcon.ExecuteCommandsAsync(
                $"tell {from} §a成功转账 ${amount} 给 {to}",
                $"tell {to} §a收到来自 {from} 的 ${amount}"
            );
        }
        
        Logger.Info($"转账: {from} -> {to}: ${amount}");
    }
    
    private async Task ShowBalance(string playerName)
    {
        var players = await Server.GetPlayersAsync();
        var player = players.FirstOrDefault(p => p.Name == playerName);
        
        if (player == null) return;
        
        var balance = _balances.GetValueOrDefault(player.Uuid, 0);
        
        if (Rcon?.IsConnected == true)
        {
            await Rcon.ExecuteCommandAsync(
                $"tell {playerName} §a你的余额: §e${balance}");
        }
    }
}
```

---

## 最佳实践

### 1. 优先使用SMP

对于SMP支持的功能，优先使用SMP而不是RCON：

```csharp
// ✅ 好：使用SMP
await Server.AddToAllowlistAsync(player);

// ❌ 不好：使用RCON
await Rcon.ExecuteCommandAsync($"whitelist add {player.Name}");
```

**原因：**
- SMP返回结构化数据，更容易处理
- SMP有类型安全保证
- SMP有实时通知

### 2. RCON用于游戏操作

RCON专注于SMP不支持的游戏操作：

```csharp
// ✅ 适合用RCON
await Rcon.ExecuteCommandAsync($"give {player} diamond 10");
await Rcon.ExecuteCommandAsync($"tp {player} 0 100 0");
await Rcon.ExecuteCommandAsync($"effect give {player} speed 60 1");
await Rcon.ExecuteCommandAsync($"tellraw {player} {{...}}");
```

### 3. 命令前缀规范

为游戏内命令使用明确的前缀，避免冲突：

```csharp
// ✅ 好
const string CommandPrefix = "/myplugin";

// ❌ 不好：容易与原版命令冲突
const string CommandPrefix = "/give";
```

### 4. 错误处理

始终检查RCON连接状态：

```csharp
if (Rcon?.IsConnected == true)
{
    var response = await Rcon.ExecuteCommandAsync(command);
    
    if (!response.Success)
    {
        Logger.Warning($"RCON命令失败: {response.Message}");
    }
}
else
{
    Logger.Warning("RCON未连接，无法执行命令");
}
```

### 5. 性能考虑

避免频繁执行RCON命令：

```csharp
// ❌ 不好：每个玩家单独执行
foreach (var player in players)
{
    await Rcon.ExecuteCommandAsync($"give {player.Name} diamond 1");
}

// ✅ 好：批量执行
var commands = players.Select(p => $"give {p.Name} diamond 1").ToArray();
await Rcon.ExecuteCommandsAsync(commands);
```

---

## 配置示例

### config/nethergate.json

```json
{
  "server_connection": {
    "host": "localhost",
    "port": 25575,
    "secret": "your-smp-token",
    "auto_connect": true
  },
  "rcon": {
    "enabled": true,
    "host": "localhost",
    "port": 25575,
    "password": "your-rcon-password",
    "auto_connect": true,
    "timeout_seconds": 5
  },
  "server_process": {
    "auto_start": true,
    "jar_file": "server.jar"
  }
}
```

### server.properties

```properties
# 启用SMP
management-server-enabled=true
management-server-port=25575
management-server-secret=your-smp-token

# 启用RCON
enable-rcon=true
rcon.port=25566
rcon.password=your-rcon-password
```

---

## 总结

NetherGate通过**SMP + RCON + 日志监听**三位一体的架构，实现了完整的服务器管理和游戏内交互：

✅ **SMP** - 结构化管理、实时通知  
✅ **RCON** - 灵活命令执行、游戏内交互  
✅ **日志监听** - 事件捕获、命令拦截  

这种组合让NetherGate的功能完整性和灵活性**超越MCDR**，同时保持类型安全和现代化的API设计！🚀

