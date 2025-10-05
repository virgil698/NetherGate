# NetherGate 常见问题 (FAQ)

本文档回答了使用 NetherGate 时最常见的问题。

---

## 📋 **目录**

- [基础问题](#基础问题)
- [安装和配置](#安装和配置)
- [插件开发](#插件开发)
- [命令和权限](#命令和权限)
- [协议相关](#协议相关)
- [事件系统](#事件系统)
- [性能和优化](#性能和优化)
- [故障排查](#故障排查)

---

## 🌟 **基础问题**

### **Q: NetherGate 是什么？**

**A:** NetherGate 是一个 Minecraft Java 版服务器的外部插件加载器，使用 C# 开发，提供强大的插件开发能力和服务器管理功能。

**主要特性**：
- ✅ 基于 .NET 9.0 的高性能架构
- ✅ 支持 SMP、RCON、日志解析三层协议
- ✅ 完整的插件热重载支持
- ✅ 丰富的 API 和事件系统
- ✅ 跨平台支持（Windows/Linux/macOS）

---

### **Q: NetherGate 与 MCDR 的主要区别？**

**A:** 

| 特性 | MCDR | NetherGate |
|------|------|-----------|
| **语言** | Python | C# (.NET 9.0) |
| **协议** | RCON + 日志 | **SMP + RCON + 日志** |
| **性能** | 解释执行 | JIT/AOT 编译 |
| **类型安全** | 动态类型 | 强类型 |
| **游戏内命令** | 有限支持 | **完整支持** |
| **插件热重载** | 支持 | **支持（带状态保存）** |
| **NBT 操作** | 有限 | **完整支持** |
| **网络监控** | 不支持 | **支持（可选）** |

---

### **Q: NetherGate 支持哪些 Minecraft 版本？**

**A:** NetherGate 支持 **Minecraft Java Edition 1.20.x 及以上**。

**SMP 协议要求**：
- Minecraft 1.21.9+ 原生支持
- 或安装 SMP 桥接插件（支持 1.20.x）

**RCON 要求**：
- 几乎所有现代 Minecraft 版本都支持

---

### **Q: NetherGate 可以与 Bukkit/Spigot/Paper 插件一起使用吗？**

**A:** **可以！** NetherGate 是外部插件加载器，不会与服务端插件冲突。

你可以同时使用：
- ✅ NetherGate 插件（外部管理）
- ✅ Bukkit/Spigot/Paper 插件（服务端内部）
- ✅ Mod（如 Fabric/Forge）

它们各司其职，互不干扰。

---

## 🔧 **安装和配置**

### **Q: 需要什么版本的 .NET？**

**A:** NetherGate 需要 **.NET 9.0** 或更高版本。

**安装步骤**：

```bash
# 1. 下载 .NET 9.0
https://dotnet.microsoft.com/download/dotnet/9.0

# 2. 验证安装
dotnet --version
# 应输出: 9.0.x
```

**Linux (Ubuntu/Debian)**：
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-runtime-9.0
```

---

### **Q: 需要什么系统要求？**

**A:**

**最低要求**：
- .NET 9.0 Runtime
- 512MB RAM (NetherGate 本身)
- 支持的操作系统：Windows 10/11, Linux, macOS

**推荐配置**：
- .NET 9.0 SDK (用于插件开发)
- 1GB+ RAM (取决于插件数量)
- SSD 存储

---

### **Q: 配置文件在哪里？**

**A:** 首次运行后会自动生成：

```
NetherGate/
├── nethergate-config.yaml      # 核心配置
├── websocket-config.yaml        # WebSocket 配置
├── plugins/                     # 插件目录
├── config/                      # 插件配置目录
└── logs/                        # 日志目录
```

---

### **Q: 如何重置配置？**

**A:**

**方法 1：使用命令行**
```bash
# 备份当前配置
NetherGate.exe config export backup.yaml

# 删除配置
rm nethergate-config.yaml websocket-config.yaml

# 重新运行配置向导
NetherGate.exe --setup
```

**方法 2：手动删除**
```bash
# 删除配置文件
rm nethergate-config.yaml
rm websocket-config.yaml

# 重新启动 NetherGate，会提示运行配置向导
NetherGate.exe
```

---

### **Q: 如何配置 SMP？**

**A:**

**1. 在 server.properties 中配置**：
```properties
# Minecraft 1.21.9+ 原生支持
management-server-enabled=true
management-server-port=40745
management-server-secret=<your-40-character-token>
management-server-tls-enabled=false
```

**2. 在 nethergate-config.yaml 中配置**：
```yaml
server_connection:
  host: localhost
  port: 40745
  secret: <same-token-as-server>
  use_tls: false
  auto_connect: true
```

**3. 生成认证令牌**：
```bash
# Linux/macOS
openssl rand -base64 30 | tr -d '+/=' | cut -c1-40

# Windows PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 40 | % {[char]$_})
```

---

### **Q: 如何配置 RCON？**

**A:**

**1. 在 server.properties 中配置**：
```properties
enable-rcon=true
rcon.port=25575
rcon.password=your_strong_password
```

**2. 在 nethergate-config.yaml 中配置**：
```yaml
rcon:
  enabled: true
  host: localhost
  port: 25575
  password: your_strong_password
  auto_connect: true
```

**安全提示**：
- ⚠️ 使用强密码
- ⚠️ 不要在公网暴露 RCON 端口
- ⚠️ 定期更换密码

---

## 💻 **插件开发**

### **Q: 如何开始开发插件？**

**A:**

**1. 创建项目**：
```bash
dotnet new classlib -n MyPlugin
cd MyPlugin
```

**2. 添加 NuGet 引用**：
```bash
dotnet add package NetherGate.API
```

**3. 创建插件类**：
```csharp
using NetherGate.API.Plugins;

public class MyPlugin : PluginBase
{
    public override Task OnLoadAsync()
    {
        Logger.Info("插件加载中...");
        return Task.CompletedTask;
    }

    public override Task OnEnableAsync()
    {
        Logger.Info("插件已启用！");
        return Task.CompletedTask;
    }
}
```

**4. 创建 plugin.json**：
```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "YourName",
  "main": "MyPlugin.dll"
}
```

**详细指南**：[插件开发指南](../03-插件开发/插件开发指南.md)

---

### **Q: 插件可以做什么？**

**A:** 插件可以访问以下功能：

**✅ 通过 SMP（Server Management Protocol）**：
- 管理白名单、封禁、OP
- 查询在线玩家和服务器状态
- 修改游戏规则
- 保存世界、重启服务器

**✅ 通过 RCON（Remote Console）**：
- 执行任意游戏命令
- 给予物品、传送玩家
- 发送富文本消息
- 操作 BossBar、Scoreboard、Title

**✅ 通过事件系统**：
- 监听玩家加入/离开/聊天/死亡
- 监听服务器启动/停止/崩溃
- 监听网络事件（可选）
- 创建自定义事件

**✅ 通过 NBT 操作**：
- 读取玩家数据（背包、统计、进度）
- 读取世界数据（种子、出生点）
- 修改玩家数据（位置、生命、经验）
- 修改世界数据（规则、时间、天气）

---

### **Q: 如何实现游戏内命令？**

**A:**

**使用命令系统**：

```csharp
using NetherGate.API.Commands;

public class TeleportCommand : ICommand
{
    public string Name => "tp";
    public string Description => "传送玩家";
    public string Usage => "/tp <玩家> <x> <y> <z>";
    public string[] Aliases => new[] { "teleport" };
    public string? Permission => "myplugin.tp";
    public string PluginId => "my-plugin";

    private readonly IGameDisplayApi _gameDisplay;

    public TeleportCommand(IGameDisplayApi gameDisplay)
    {
        _gameDisplay = gameDisplay;
    }

    public async Task<CommandResult> ExecuteAsync(
        ICommandSender sender, 
        string[] args)
    {
        if (args.Length < 4)
            return CommandResult.Fail($"用法: {Usage}");

        string player = args[0];
        if (!double.TryParse(args[1], out double x) ||
            !double.TryParse(args[2], out double y) ||
            !double.TryParse(args[3], out double z))
        {
            return CommandResult.Fail("坐标必须是数字！");
        }

        await _gameDisplay.TeleportPlayer(player, x, y, z);
        return CommandResult.Ok($"已传送 {player} 到 {x} {y} {z}");
    }

    public Task<List<string>> TabCompleteAsync(
        ICommandSender sender, 
        string[] args)
    {
        // 返回玩家列表作为补全
        if (args.Length == 1)
            return Task.FromResult(GetOnlinePlayerNames());
        
        return Task.FromResult(new List<string>());
    }
}
```

**注册命令**：
```csharp
public override Task OnEnableAsync()
{
    Commands.Register(new TeleportCommand(GameDisplay));
    return Task.CompletedTask;
}
```

---

### **Q: 插件如何持久化数据？**

**A:** 多种方式：

**1. 使用配置文件（推荐用于设置）**：
```csharp
// 读取
var setting = Config.Get<string>("my_setting", "default");

// 写入
Config.Set("my_setting", "new_value");
await Config.SaveAsync();
```

**2. 使用 JSON 文件（推荐用于数据）**：
```csharp
var dataFile = Path.Combine(DataDirectory, "data.json");
var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
{ 
    WriteIndented = true 
});
await File.WriteAllTextAsync(dataFile, json);
```

**3. 使用数据库（大量数据）**：
```csharp
// SQLite
using var db = new SqliteConnection(
    $"Data Source={Path.Combine(DataDirectory, "data.db")}");

// 使用 EF Core 或 Dapper
```

---

### **Q: 插件之间如何通信？**

**A:** 使用插件通信 API：

**发送消息**：
```csharp
// 发送给特定插件
await Messenger.SendAsync("target-plugin", "Hello!", priority: 1);

// 广播给所有插件
await Messenger.BroadcastAsync("Announcement", priority: 0);
```

**接收消息**：
```csharp
public override Task OnEnableAsync()
{
    Messenger.Subscribe("my-topic", OnMessageReceived);
    return Task.CompletedTask;
}

private async Task OnMessageReceived(PluginMessage message)
{
    Logger.Info($"收到来自 {message.SourcePluginId} 的消息: {message.Content}");
    
    // 回复消息
    if (message.ExpectsReply)
    {
        await Messenger.ReplyAsync(message, "Got it!");
    }
}
```

---

### **Q: 如何调试插件？**

**A:**

**方法 1：使用 Visual Studio/Rider（推荐）**
```csharp
1. 在插件代码中设置断点
2. Attach to Process...
3. 选择 NetherGate.exe
4. F5 开始调试
```

**方法 2：使用日志**
```csharp
Logger.Debug("调试信息");
Logger.Info("重要信息");
Logger.Warning("警告信息");
Logger.Error("错误信息", exception);
```

**方法 3：使用单元测试**
```csharp
[Fact]
public async Task TestPluginFunction()
{
    // 模拟插件上下文
    var mockContext = new MockPluginContext();
    var plugin = new MyPlugin();
    plugin.Initialize(mockContext);
    
    await plugin.OnLoadAsync();
    Assert.True(plugin.IsInitialized);
}
```

**详细指南**：[调试技巧](../03-插件开发/调试技巧.md)

---

## ⚙️ **命令和权限**

### **Q: 如何给玩家授予权限？**

**A:**

**在游戏内**：
```
# 授予权限
#permission user <玩家名> add <权限节点>
#permission user Steve add myplugin.admin

# 撤销权限
#permission user <玩家名> remove <权限节点>
#permission user Steve remove myplugin.admin

# 查看玩家权限
#permission user <玩家名> list
#permission user Steve list
```

**通过配置文件** (`config/permissions.yaml`):
```yaml
users:
  Steve:
    permissions:
      - myplugin.admin
      - myplugin.teleport
```

---

### **Q: 如何创建权限组？**

**A:**

**在游戏内**：
```
# 创建组
#permission group create <组名>
#permission group create moderator

# 设置组权限
#permission group moderator add myplugin.kick
#permission group moderator add myplugin.ban

# 将玩家添加到组
#permission group moderator add user Steve
```

**通过配置文件**：
```yaml
groups:
  moderator:
    priority: 50
    permissions:
      - myplugin.kick
      - myplugin.ban
      - myplugin.mute

users:
  Steve:
    groups:
      - moderator
```

---

### **Q: 游戏内命令为什么要用 `#` 前缀？**

**A:** 为了区分 NetherGate 命令和 Minecraft 原生命令：

- `/` 前缀 - Minecraft 原生命令（如 `/give`, `/tp`）
- `#` 前缀 - NetherGate 框架命令（如 `#plugin list`, `#help`）

**在控制台**：
- 可以不使用前缀直接输入命令
- `plugin list` 和 `#plugin list` 效果相同

---

## 📡 **协议相关**

### **Q: SMP、RCON、日志监听有什么区别？**

**A:**

| 技术 | 用途 | 优势 | 限制 |
|------|------|------|------|
| **SMP** | 结构化管理 | • 类型安全<br>• 实时通知<br>• 包含 UUID | • 功能有限<br>• 需要 MC 1.21.9+ |
| **RCON** | 游戏命令执行 | • 灵活强大<br>• 广泛支持 | • 返回文本<br>• 无事件监听 |
| **日志监听** | 事件捕获 | • 捕获更多事件<br>• 无版本限制 | • 不可靠<br>• 需要解析<br>• 有延迟 |

**最佳实践**：结合使用三种技术，发挥各自优势。

**详细说明**：[事件系统 - 三层监听策略](../02-核心功能/事件系统.md#三层事件监听策略)

---

### **Q: 为什么不只用 RCON？**

**A:**

**RCON 的局限性**：
- ❌ 返回非结构化文本，需要解析
- ❌ 无实时事件通知
- ❌ 无类型安全
- ❌ 某些操作返回值不完整

**SMP 的优势**：
- ✅ 返回结构化 JSON 数据
- ✅ WebSocket 实时推送事件
- ✅ 强类型 API
- ✅ 更完整的信息

**结论**：组合使用 SMP + RCON 可以获得最佳效果。

---

## 📊 **事件系统**

### **Q: 如何订阅事件？**

**A:**

```csharp
public override Task OnEnableAsync()
{
    // 订阅玩家加入事件
    Events.Subscribe<PlayerJoinedEvent>(OnPlayerJoined, EventPriority.Normal);
    
    // 订阅玩家离开事件
    Events.Subscribe<PlayerLeftEvent>(OnPlayerLeft, EventPriority.Normal);
    
    return Task.CompletedTask;
}

private async Task OnPlayerJoined(PlayerJoinedEvent e)
{
    Logger.Info($"{e.Player.Name} 加入了服务器");
    await GameDisplay.SendChatMessage(e.Player.Name, "§a欢迎！");
}

private Task OnPlayerLeft(PlayerLeftEvent e)
{
    Logger.Info($"{e.Player.Name} 离开了服务器");
    return Task.CompletedTask;
}
```

**详细说明**：[事件系统](../02-核心功能/事件系统.md)

---

### **Q: 事件优先级有什么用？**

**A:** 事件优先级决定处理顺序：

```
Lowest (0) → Low (1) → Normal (2) → High (3) → Highest (4) → Monitor (5)
```

**使用场景**：

- `Lowest` - 最先执行，用于预处理
- `Normal` - 默认优先级，常规处理
- `Highest` - 最后执行，用于后处理
- `Monitor` - 仅监听，不应修改数据

**示例**：
```csharp
// 安全插件：高优先级，最后检查
Events.Subscribe<PlayerJoinedEvent>(CheckPlayerSafety, EventPriority.Highest);

// 日志插件：Monitor 优先级，仅记录
Events.Subscribe<PlayerJoinedEvent>(LogPlayerJoin, EventPriority.Monitor);
```

---

## ⚡ **性能和优化**

### **Q: NetherGate 占用多少资源？**

**A:**

**典型资源占用**：
- NetherGate 核心：50-100MB 内存
- 每个插件：10-50MB 内存（取决于功能）
- CPU：几乎可以忽略不计（事件驱动）

**与其他系统对比**：
- MCDR (Python)：100-200MB 内存
- Bukkit 服务端：1-4GB 内存（Minecraft 服务器本身）

---

### **Q: 如何优化插件性能？**

**A:**

**1. 使用异步操作**
```csharp
// ❌ 不好：阻塞
Thread.Sleep(1000);

// ✅ 好：异步
await Task.Delay(1000);
```

**2. 缓存查询结果**
```csharp
// ❌ 不好：每次都查询
foreach (var item in items)
{
    var players = await SmpApi.GetPlayersAsync();
}

// ✅ 好：缓存结果
var players = await SmpApi.GetPlayersAsync();
foreach (var item in items)
{
    // 使用缓存的 players
}
```

**3. 使用批量操作**
```csharp
// ❌ 不好：逐个执行
foreach (var player in players)
{
    await GameDisplay.GiveItem(player, "diamond", 1);
}

// ✅ 好：使用 @a 选择器
await Rcon.SendCommandAsync("give @a diamond 1");
```

**详细指南**：[性能优化](../07-示例和最佳实践/性能优化.md)

---

### **Q: 多少个插件算多？**

**A:** 没有硬性限制，取决于插件质量：

- **小型服务器**：5-10 个插件
- **中型服务器**：10-20 个插件
- **大型服务器**：20-50 个插件

**关键是插件质量，而不是数量**。一个写得很差的插件可能比 10 个优秀插件消耗更多资源。

---

## 🔧 **故障排查**

### **Q: NetherGate 启动失败怎么办？**

**A:** 按以下步骤排查：

**1. 检查 .NET Runtime**
```bash
dotnet --version
# 应该是 9.0.x 或更高
```

**2. 检查配置文件**
```bash
# 验证配置文件语法
NetherGate.exe config validate
```

**3. 查看日志**
```bash
# 查看最新日志
tail -f logs/latest.log
```

**4. 运行诊断**
```bash
# 全面诊断
NetherGate.exe diagnose
```

**详细指南**：[故障排查](../05-配置和部署/故障排查.md)

---

### **Q: 连接失败怎么办？**

**A:**

**检查清单**：

**1. SMP 连接**
```yaml
# 确认配置一致
server.properties:
  management-server-enabled=true
  management-server-port=40745
  management-server-secret=<token>

nethergate-config.yaml:
  server_connection:
    port: 40745
    secret: <same-token>
```

**2. RCON 连接**
```yaml
# 确认配置一致
server.properties:
  enable-rcon=true
  rcon.port=25575
  rcon.password=<password>

nethergate-config.yaml:
  rcon:
    port: 25575
    password: <same-password>
```

**3. 防火墙**
```bash
# 允许相应端口
# Windows
netsh advfirewall firewall add rule name="NetherGate-SMP" dir=in action=allow protocol=TCP localport=40745

# Linux (ufw)
sudo ufw allow 40745/tcp
sudo ufw allow 25575/tcp
```

---

### **Q: 插件加载失败怎么办？**

**A:**

**常见原因**：

**1. 缺少 plugin.json**
```
plugins/
└── my-plugin/
    ├── MyPlugin.dll        ✅
    ├── plugin.json         ❌ 缺失！
    └── dependencies/
```

**解决方案**：创建 `plugin.json`
```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "YourName",
  "main": "MyPlugin.dll"
}
```

**2. 依赖缺失**
```bash
# 检查依赖
NetherGate.exe check-deps
```

**解决方案**：将缺失的 DLL 放入插件目录：
```
plugins/
└── my-plugin/
    ├── MyPlugin.dll
    ├── plugin.json
    ├── Newtonsoft.Json.dll     ✅ 依赖
    └── SomeOtherLib.dll        ✅ 依赖
```

**3. API 版本不兼容**

**解决方案**：重新编译插件，使用最新的 `NetherGate.API` 版本。

---

### **Q: 内存泄漏或性能问题怎么办？**

**A:**

**检查事件订阅**：
```csharp
// ❌ 忘记取消订阅
public override Task OnDisableAsync()
{
    // 没有取消订阅！
    return Task.CompletedTask;
}

// ✅ 正确做法
public override Task OnDisableAsync()
{
    Events.UnsubscribeAll(this);
    Commands.UnregisterAll(this);
    return Task.CompletedTask;
}
```

**检查定时器**：
```csharp
private Timer? _timer;

public override Task OnDisableAsync()
{
    _timer?.Dispose();  // 必须释放！
    return Task.CompletedTask;
}
```

**使用性能分析工具**：
- Visual Studio Profiler
- dotMemory
- PerfView

---

## 📚 **相关文档**

- [快速开始](../01-快速开始/安装和配置.md)
- [插件开发指南](../03-插件开发/插件开发指南.md)
- [故障排查](../05-配置和部署/故障排查.md)
- [API 参考](./API参考.md)

---

**问题没有得到解答？** 

欢迎在 [GitHub Issues](https://github.com/BlockBridge/NetherGate/issues) 提问！

---

**最后更新**: 2025-10-05  
**NetherGate 版本**: v0.1.0

