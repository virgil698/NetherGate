# RCON 集成

RCON（Remote Console）是 Minecraft 服务器的远程控制台协议，NetherGate 通过 RCON 可以向服务器发送命令并接收响应。

---

## 📋 **目录**

- [什么是 RCON](#什么是-rcon)
- [配置 RCON](#配置-rcon)
- [使用 RCON](#使用-rcon)
- [命令示例](#命令示例)
- [最佳实践](#最佳实践)
- [故障排查](#故障排查)

---

## 🎮 **什么是 RCON**

RCON（Remote Console）是 Minecraft 服务器提供的远程管理协议，允许外部程序：

- ✅ 发送服务器命令
- ✅ 接收命令执行结果
- ✅ 远程管理服务器
- ✅ 自动化任务

### **RCON vs SMP**

| 特性 | RCON | SMP |
|------|------|-----|
| 协议 | 原生 Minecraft | WebSocket + JSON-RPC |
| 安装 | 无需插件 | 需要安装插件 |
| 功能 | 执行命令 | 命令 + 事件 + 状态查询 |
| 实时性 | 低（轮询） | 高（推送） |
| 适用场景 | 基础管理 | 高级功能 |

**推荐：** 同时启用 RCON 和 SMP，获得最佳体验。

---

## ⚙️ **配置 RCON**

### **1. 配置 Minecraft 服务器**

编辑 `server.properties`：

```properties
# 启用 RCON
enable-rcon=true

# RCON 端口
rcon.port=25575

# RCON 密码（强密码）
rcon.password=your_strong_password_here
```

**安全建议：**
- 使用强密码（至少 16 字符）
- 不要使用默认密码
- 限制 RCON 端口访问（防火墙）
- 仅在本地监听（不要公网暴露）

### **2. 配置 NetherGate**

编辑 `nethergate-config.yaml`：

```yaml
rcon:
  # 是否启用 RCON
  enabled: true
  
  # RCON 主机地址
  # 本地: 127.0.0.1 或 localhost
  # 远程: 服务器 IP 地址
  host: "127.0.0.1"
  
  # RCON 端口（与 server.properties 一致）
  port: 25575
  
  # RCON 密码（与 server.properties 一致）
  password: "your_strong_password_here"
  
  # 超时时间（秒）
  timeout_seconds: 5
```

### **3. 验证连接**

启动 NetherGate 后，查看日志：

```
[INFO]: RCON 连接成功: 127.0.0.1:25575
```

如果连接失败：
```
[ERROR]: RCON 连接失败: Connection refused
```

---

## 🔧 **使用 RCON**

### **IRconClient 接口**

```csharp
public interface IRconClient
{
    /// <summary>
    /// 连接到 RCON 服务器
    /// </summary>
    Task<bool> ConnectAsync();
    
    /// <summary>
    /// 断开连接
    /// </summary>
    void Disconnect();
    
    /// <summary>
    /// 发送命令
    /// </summary>
    /// <param name="command">命令（不需要 / 前缀）</param>
    /// <returns>命令执行结果</returns>
    Task<string> SendCommandAsync(string command);
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }
}
```

### **基本用法**

```csharp
using NetherGate.API.Plugins;

public class MyPlugin : PluginBase
{
    public override Task OnEnableAsync()
    {
        // RCON 已由框架自动连接
        if (Rcon.IsConnected)
        {
            Logger.Info("RCON 已就绪");
        }
        else
        {
            Logger.Warning("RCON 未连接，请检查配置");
        }
        
        return Task.CompletedTask;
    }

    public async Task SendCommandExample()
    {
        // 发送命令（不需要 / 前缀）
        var response = await Rcon.SendCommandAsync("list");
        
        Logger.Info($"在线玩家: {response}");
    }
}
```

### **错误处理**

```csharp
using System;
using System.IO;

public async Task<string> SendSafeCommandAsync(string command)
{
    try
    {
        // 检查连接状态
        if (!Rcon.IsConnected)
        {
            Logger.Warning("RCON 未连接");
            return string.Empty;
        }
        
        // 发送命令
        var response = await Rcon.SendCommandAsync(command);
        
        return response;
    }
    catch (TimeoutException)
    {
        Logger.Error("RCON 命令超时");
        return string.Empty;
    }
    catch (IOException ex)
    {
        Logger.Error($"RCON 连接错误: {ex.Message}");
        return string.Empty;
    }
    catch (Exception ex)
    {
        Logger.Error($"RCON 命令失败: {ex.Message}");
        return string.Empty;
    }
}
```

---

## 📝 **命令示例**

### **服务器管理**

```csharp
// 使用 PluginBase（推荐）

// 查看在线玩家
var players = await Rcon.SendCommandAsync("list");
// 输出: "There are 3/20 players online: Steve, Alex, Bob"

// 服务器状态
var seed = await Rcon.SendCommandAsync("seed");
var difficulty = await Rcon.SendCommandAsync("difficulty");

// 保存世界
await Rcon.SendCommandAsync("save-all");
Logger.Info("世界已保存");

// 停止服务器（谨慎使用）
await Rcon.SendCommandAsync("stop");
```

### **玩家管理**

```csharp
// 踢出玩家
await Rcon.SendCommandAsync("kick Steve 违反规则");
Logger.Info("已踢出玩家 Steve");

// 封禁玩家
await Rcon.SendCommandAsync("ban Steve 作弊");
Logger.Info("已封禁玩家 Steve");

// 解封玩家
await Rcon.SendCommandAsync("pardon Steve");
Logger.Info("已解封玩家 Steve");

// 白名单管理
await Rcon.SendCommandAsync("whitelist add Steve");
await Rcon.SendCommandAsync("whitelist remove Steve");
await Rcon.SendCommandAsync("whitelist list");

// OP 权限
await Rcon.SendCommandAsync("op Steve");
await Rcon.SendCommandAsync("deop Steve");
```

### **游戏控制**

```csharp
// 时间控制
await Rcon.SendCommandAsync("time set day");
await Rcon.SendCommandAsync("time set night");
await Rcon.SendCommandAsync("time set 6000");

// 天气控制
await Rcon.SendCommandAsync("weather clear");
await Rcon.SendCommandAsync("weather rain");
await Rcon.SendCommandAsync("weather thunder");

// 游戏规则
await Rcon.SendCommandAsync("gamerule doDaylightCycle false");
await Rcon.SendCommandAsync("gamerule doMobSpawning false");
await Rcon.SendCommandAsync("gamerule keepInventory true");

// 难度
await Rcon.SendCommandAsync("difficulty peaceful");
await Rcon.SendCommandAsync("difficulty easy");
await Rcon.SendCommandAsync("difficulty normal");
await Rcon.SendCommandAsync("difficulty hard");
```

### **玩家操作**

```csharp
// 传送
await Rcon.SendCommandAsync("tp Steve 0 64 0");
await Rcon.SendCommandAsync("tp Steve Alex");

// 给予物品
await Rcon.SendCommandAsync("give Steve diamond 64");
await Rcon.SendCommandAsync("give Steve diamond_sword{Enchantments:[{id:sharpness,lvl:5}]}");

// 清空背包
await Rcon.SendCommandAsync("clear Steve");

// 设置游戏模式
await Rcon.SendCommandAsync("gamemode survival Steve");
await Rcon.SendCommandAsync("gamemode creative Steve");
await Rcon.SendCommandAsync("gamemode adventure Steve");
await Rcon.SendCommandAsync("gamemode spectator Steve");

// 效果
await Rcon.SendCommandAsync("effect give Steve speed 60 1");
await Rcon.SendCommandAsync("effect clear Steve");

// 经验
await Rcon.SendCommandAsync("xp add Steve 100");
await Rcon.SendCommandAsync("xp set Steve 30 levels");
```

### **世界操作**

```csharp
// 填充方块
await Rcon.SendCommandAsync("fill 0 64 0 10 74 10 air");
await Rcon.SendCommandAsync("fill 0 64 0 10 64 10 diamond_block");

// 设置方块
await Rcon.SendCommandAsync("setblock 0 64 0 diamond_block");

// 克隆区域
await Rcon.SendCommandAsync("clone 0 64 0 10 74 10 100 64 100");

// 生成实体
await Rcon.SendCommandAsync("summon zombie 0 64 0");
await Rcon.SendCommandAsync("summon armor_stand 0 65 0 {CustomName:'{\"text\":\"传送点\"}',NoGravity:1b}");

// 击杀实体
await Rcon.SendCommandAsync("kill @e[type=zombie,distance=..10]");
```

### **消息和标题**

```csharp
// 发送消息
await Rcon.SendCommandAsync("say Hello, World!");
await Rcon.SendCommandAsync("tell Steve 你好！");
await Rcon.SendCommandAsync("tellraw @a {\"text\":\"彩色消息\",\"color\":\"gold\"}");

// 显示标题
await Rcon.SendCommandAsync("title Steve title {\"text\":\"欢迎\",\"color\":\"gold\"}");
await Rcon.SendCommandAsync("title Steve subtitle {\"text\":\"来到服务器\"}");
await Rcon.SendCommandAsync("title Steve times 10 70 20");

// ActionBar
await Rcon.SendCommandAsync("title Steve actionbar {\"text\":\"提示消息\"}");

// 注意：推荐使用 GameDisplay API 而非直接 RCON
// await GameDisplay.SendChatMessage("Steve", "你好！");
// await GameDisplay.ShowTitle("Steve", "欢迎", "来到服务器", 10, 70, 20);
```

---

## 💡 **最佳实践**

### **1. 使用 GameDisplayApi 而非直接 RCON**（推荐）

```csharp
// ❌ 不推荐：直接使用 RCON
await Rcon.SendCommandAsync("tp Steve 0 64 0");

// ✅ 推荐：使用封装的 GameDisplay API
await GameDisplay.TeleportPlayer("Steve", 0, 64, 0);

// ❌ 不推荐：直接 RCON 发送消息
await Rcon.SendCommandAsync("tellraw Steve {\"text\":\"Hello\",\"color\":\"gold\"}");

// ✅ 推荐：使用 GameDisplay API
await GameDisplay.SendChatMessage("Steve", "§6Hello");

// ❌ 不推荐：直接 RCON 给予物品
await Rcon.SendCommandAsync("give Steve diamond 64");

// ✅ 推荐：使用 GameDisplay API
await GameDisplay.GiveItem("Steve", "diamond", 64);
```

**原因：**
- ✅ 类型安全，编译时检查
- ✅ 自动错误处理和重试
- ✅ 更易读和维护
- ✅ 参数自动验证和转义
- ✅ 统一的日志记录

### **2. 批量命令优化**

```csharp
// ❌ 不高效：逐个发送
foreach (var player in players)
{
    await Rcon.SendCommandAsync($"give {player} diamond 1");
}

// ✅ 更好：使用选择器
await Rcon.SendCommandAsync("give @a diamond 1");

// ✅ 也可以：使用批量 API（如果支持）
var tasks = players.Select(p => GameDisplay.GiveItem(p, "diamond", 1));
await Task.WhenAll(tasks);
```

### **3. 命令构建器模式**

```csharp
/// <summary>
/// RCON 命令构建器（用于无法使用 GameDisplay API 的场景）
/// </summary>
public class RconCommandBuilder
{
    public static string Give(string player, string item, int count = 1)
    {
        // 验证参数
        if (string.IsNullOrEmpty(player) || string.IsNullOrEmpty(item))
            throw new ArgumentException("玩家名和物品不能为空");
        
        return $"give {player} {item} {count}";
    }

    public static string Teleport(string player, double x, double y, double z)
    {
        return $"tp {player} {x} {y} {z}";
    }

    public static string SetGameRule(string rule, string value)
    {
        return $"gamerule {rule} {value}";
    }
    
    public static string ExecuteFunction(string functionPath)
    {
        return $"function {functionPath}";
    }
}

// 使用示例
await Rcon.SendCommandAsync(
    RconCommandBuilder.Give("Steve", "diamond", 64)
);

await Rcon.SendCommandAsync(
    RconCommandBuilder.ExecuteFunction("mypack:welcome/init")
);
```

### **4. 解析响应**

```csharp
using System.Text.RegularExpressions;

/// <summary>
/// 解析 RCON 命令响应
/// </summary>
public class RconResponseParser
{
    /// <summary>
    /// 解析在线玩家列表
    /// </summary>
    public async Task<List<string>> GetOnlinePlayersAsync()
    {
        var response = await Rcon.SendCommandAsync("list");
        // 输出: "There are 3/20 players online: Steve, Alex, Bob"
        
        var match = Regex.Match(response, @"online:\s*(.+)");
        if (match.Success)
        {
            var playerNames = match.Groups[1].Value.Split(", ");
            return playerNames.ToList();
        }
        
        return new List<string>();
    }

    /// <summary>
    /// 获取在线玩家数量
    /// </summary>
    public async Task<int> GetPlayerCountAsync()
    {
        var response = await Rcon.SendCommandAsync("list");
        var match = Regex.Match(response, @"There are (\d+)/");
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
        {
            return count;
        }
        
        return 0;
    }
    
    /// <summary>
    /// 解析服务器种子
    /// </summary>
    public async Task<long> GetWorldSeedAsync()
    {
        var response = await Rcon.SendCommandAsync("seed");
        // 输出: "Seed: [-1234567890]"
        
        var match = Regex.Match(response, @"Seed:\s*\[(-?\d+)\]");
        if (match.Success && long.TryParse(match.Groups[1].Value, out var seed))
        {
            return seed;
        }
        
        return 0;
    }
}

// 注意：推荐使用 SMP API 获取结构化数据
// var players = await Server.GetOnlinePlayersAsync();
```

### **5. 错误重试机制**

```csharp
/// <summary>
/// 带重试的 RCON 命令执行
/// </summary>
public async Task<string> SendCommandWithRetryAsync(string command, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            if (!Rcon.IsConnected)
            {
                Logger.Warning("RCON 未连接，尝试重新连接...");
                // 框架通常会自动重连，这里只是示例
            }
            
            return await Rcon.SendCommandAsync(command);
        }
        catch (TimeoutException) when (i < maxRetries - 1)
        {
            Logger.Warning($"RCON 超时，重试 {i + 1}/{maxRetries}");
            await Task.Delay(TimeSpan.FromSeconds(1 * (i + 1))); // 指数退避
        }
        catch (IOException ex) when (i < maxRetries - 1)
        {
            Logger.Warning($"RCON 连接错误，重试 {i + 1}/{maxRetries}: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
    
    Logger.Error($"RCON 命令失败，已达到最大重试次数: {command}");
    throw new Exception($"RCON 命令失败: {command}");
}

// 使用示例
try
{
    var result = await SendCommandWithRetryAsync("list");
    Logger.Info($"在线玩家: {result}");
}
catch (Exception ex)
{
    Logger.Error($"无法获取玩家列表: {ex.Message}");
}
```

---

## 🔍 **故障排查**

### **连接失败**

```
[ERROR]: RCON 连接失败: Connection refused
```

**检查清单：**

1. **服务器配置**
   ```properties
   # server.properties
   enable-rcon=true  # 确保启用
   rcon.port=25575
   ```

2. **端口占用**
   ```bash
   # Windows
   netstat -ano | findstr 25575
   
   # Linux
   netstat -tulpn | grep 25575
   ```

3. **防火墙**
   ```bash
   # Linux 开放端口
   sudo ufw allow 25575/tcp
   
   # Windows 防火墙规则
   netsh advfirewall firewall add rule name="RCON" dir=in action=allow protocol=TCP localport=25575
   ```

4. **密码匹配**
   - 确保 `server.properties` 和 `nethergate-config.yaml` 密码一致
   - 密码区分大小写

---

### **认证失败**

```
[ERROR]: RCON 认证失败: Invalid password
```

**解决方案：**
1. 检查密码是否正确
2. 重启 Minecraft 服务器（配置更改后需要重启）
3. 检查密码中是否有特殊字符（可能需要转义）

---

### **超时**

```
[ERROR]: RCON 命令超时
```

**解决方案：**

1. **增加超时时间**
   ```yaml
   rcon:
     timeout_seconds: 10  # 增加到 10 秒
   ```

2. **检查服务器负载**
   - 服务器卡顿会导致响应缓慢

3. **网络延迟**
   - 如果是远程服务器，检查网络延迟

---

### **命令无响应**

**可能原因：**

1. **命令语法错误**
   ```csharp
   // ❌ 错误
   await _context.RconClient.SendCommandAsync("/give Steve diamond 64");
   
   // ✅ 正确（不需要 / 前缀）
   await _context.RconClient.SendCommandAsync("give Steve diamond 64");
   ```

2. **玩家不在线**
   ```csharp
   // 先检查玩家是否在线
   var players = await GetOnlinePlayersAsync();
   if (!players.Contains("Steve"))
   {
       _context.Logger.Warning("玩家 Steve 不在线");
       return;
   }
   ```

---

## 📚 **相关文档**

- [SMP 协议](./SMP协议.md)
- [游戏显示 API](../04-高级功能/游戏显示API.md)
- [配置文件详解](../05-配置和部署/配置文件详解.md)

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05
