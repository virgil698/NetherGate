# NetherGate 常见问题 (FAQ)

本文档收集了 NetherGate 使用过程中的常见问题和解答。

---

## 📋 目录

- [基础问题](#基础问题)
- [安装与配置](#安装与配置)
- [插件开发](#插件开发)
- [协议相关](#协议相关)
- [故障排查](#故障排查)
- [性能与优化](#性能与优化)

---

## 基础问题

### Q: NetherGate 是什么？

**A:** NetherGate 是一个 Minecraft Java 版服务器的外部插件加载器，类似于 MCDR，但使用 C# 开发，提供更强大的功能和更好的性能。

### Q: NetherGate 与 MCDR 的主要区别是什么？

**A:** 主要区别包括：

| 特性 | MCDR | NetherGate |
|------|------|-----------|
| 语言 | Python | C# (.NET 9.0) |
| 协议 | RCON + 日志 | **SMP + RCON + 日志** |
| 性能 | 解释执行 | JIT/AOT 编译 |
| 类型安全 | 动态类型 | 强类型 |
| 游戏内命令 | 有限 | **完整支持** |

### Q: NetherGate 支持哪些 Minecraft 版本？

**A:** NetherGate 需要 Minecraft Java Edition 1.21.9+ 版本，因为服务端管理协议（SMP）是在这个版本引入的。

### Q: NetherGate 可以与 Bukkit/Spigot/Paper 插件一起使用吗？

**A:** 可以！NetherGate 是外部插件加载器，不会与服务端插件冲突。你可以同时使用：
- NetherGate 插件（外部管理）
- Bukkit/Spigot/Paper 插件（服务端内部）

---

## 安装与配置

### Q: 如何安装 NetherGate？

**A:** 

1. 安装 .NET 9.0 Runtime
2. 下载 NetherGate 可执行文件
3. 配置 Minecraft 服务器的 `server.properties`：
   ```properties
   # 启用 SMP
   management-server-enabled=true
   management-server-port=25575
   management-server-secret=your-token-here
   
   # 启用 RCON
   enable-rcon=true
   rcon.port=25566
   rcon.password=your-rcon-password
   ```
4. 编辑 `config/nethergate.json`
5. 运行 `NetherGate.exe`

详见：[README.md](../README.md#快速开始)

### Q: NetherGate 需要什么系统要求？

**A:**

**最低要求：**
- .NET 9.0 Runtime
- Windows 10/11, Linux, macOS
- 内存：512MB (NetherGate本身)
- Minecraft 服务器正常运行所需资源

**推荐配置：**
- .NET 9.0 SDK (用于插件开发)
- 内存：1GB+ (取决于插件数量)
- SSD 存储

### Q: 如何生成 SMP 认证令牌？

**A:**

```bash
# Linux/macOS
openssl rand -base64 30 | tr -d '+/=' | cut -c1-40

# Windows PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 40 | % {[char]$_})
```

或使用在线工具生成 40 位字母数字字符串。

### Q: 为什么连接失败？

**A:** 检查以下几点：

1. **SMP 配置**
   - 确认 `management-server-enabled=true`
   - 检查端口是否被占用
   - 验证令牌是否正确

2. **RCON 配置**
   - 确认 `enable-rcon=true`
   - 检查密码是否正确
   - 验证端口不冲突

3. **防火墙**
   - 允许相应端口的连接

4. **日志检查**
   - 查看 `logs/latest.log`
   - 查看服务器日志

---

## 插件开发

### Q: 如何开始开发 NetherGate 插件？

**A:**

1. 创建 .NET 类库项目
2. 引用 `NetherGate.API` NuGet 包
3. 创建插件类继承 `PluginBase`
4. 实现生命周期方法
5. 添加 `plugin.json` 元数据

详见：[插件项目结构文档](PLUGIN_PROJECT_STRUCTURE.md)

### Q: 插件可以做什么？

**A:** 插件可以：

✅ **通过 SMP：**
- 管理白名单、封禁、OP
- 查询在线玩家
- 修改游戏规则
- 保存世界、停止服务器

✅ **通过 RCON：**
- 执行任意游戏命令
- 给予物品、传送玩家
- 发送富文本消息

✅ **通过日志监听：**
- 监听玩家聊天
- 捕获命令执行
- 实现游戏内自定义命令

详见：[SMP 接口文档](SMP_INTERFACE.md)

### Q: 如何实现游戏内命令？

**A:** 通过日志监听 + RCON 组合：

```csharp
public override Task OnEnableAsync()
{
    Server.SubscribeToServerLog(OnServerLog);
    return Task.CompletedTask;
}

private async void OnServerLog(ServerLogEntry entry)
{
    // 捕获玩家命令
    var match = Regex.Match(entry.Message, 
        @"(\w+) issued server command: /myplugin (.+)");
    
    if (match.Success)
    {
        var playerName = match.Groups[1].Value;
        var args = match.Groups[2].Value.Split(' ');
        
        // 通过 RCON 执行游戏命令
        await Rcon.ExecuteCommandAsync($"give {playerName} diamond");
    }
}
```

详见：[RCON 集成文档](RCON_INTEGRATION.md)

### Q: 插件如何持久化数据？

**A:** 多种方式：

**1. 配置文件（推荐用于设置）：**
```csharp
Config.Set("key", value);
await Config.SaveAsync();
```

**2. JSON 文件（推荐用于数据）：**
```csharp
var dataFile = Path.Combine(DataDirectory, "data.json");
var json = JsonSerializer.Serialize(data);
await File.WriteAllTextAsync(dataFile, json);
```

**3. 数据库（大量数据）：**
```csharp
using var db = new SqliteConnection($"Data Source={DataDirectory}/data.db");
// 使用 EF Core 或 Dapper
```

### Q: 插件之间如何通信？

**A:**

**方式1：获取插件实例**
```csharp
var otherPlugin = GetPlugin<OtherPlugin>();
if (otherPlugin != null)
{
    otherPlugin.DoSomething();
}
```

**方式2：通过事件**
```csharp
// 插件 A 触发自定义事件
Events.Dispatch(new MyCustomEvent { Data = "..." });

// 插件 B 订阅事件
Events.Subscribe<MyCustomEvent>(this, EventPriority.Normal, OnMyEvent);
```

### Q: 如何调试插件？

**A:**

1. **使用 Visual Studio/Rider**
   - 在插件代码中设置断点
   - Attach 到 NetherGate 进程
   - F5 开始调试

2. **使用日志**
   ```csharp
   Logger.Debug("调试信息");
   Logger.Info("重要信息");
   Logger.Error("错误信息", exception);
   ```

3. **使用测试**
   ```csharp
   [Fact]
   public async Task TestPluginFunction()
   {
       var plugin = new MyPlugin();
       await plugin.OnLoadAsync();
       Assert.True(plugin.IsInitialized);
   }
   ```

---

## 协议相关

### Q: SMP、RCON、日志监听有什么区别？

**A:**

| 技术 | 用途 | 优势 | 限制 |
|------|------|------|------|
| **SMP** | 结构化管理 | 类型安全、实时通知 | 功能有限 |
| **RCON** | 游戏命令执行 | 灵活强大 | 返回文本 |
| **日志监听** | 事件捕获 | 捕获更多事件 | 需要解析 |

详见：[SMP接口文档 - 三种技术的能力边界](SMP_INTERFACE.md#三种技术的能力边界与组合使用)

### Q: 为什么不只用 RCON？

**A:** 虽然 RCON 很灵活，但：

❌ **RCON 的问题：**
- 返回非结构化文本，需要解析
- 无实时事件通知
- 无类型安全
- 某些操作返回值不完整

✅ **SMP 的优势：**
- 返回结构化 JSON 数据
- WebSocket 实时推送事件
- 强类型 API
- 更完整的信息

**最佳实践：** 结合使用三种技术，发挥各自优势。

### Q: SMP 未来会有更多功能吗？

**A:** 很可能！Mojang 在 1.21.9 才引入 SMP，功能还在完善中。NetherGate 的架构设计确保：
- 当 SMP 扩展时，只需更新协议层
- 插件代码无需修改
- 自动享受新功能

详见：[未来扩展性设计](FUTURE_EXTENSIBILITY.md)

---

## 故障排查

### Q: NetherGate 启动失败

**A:** 检查：

1. **.NET Runtime 版本**
   ```bash
   dotnet --version  # 应该是 9.0+
   ```

2. **配置文件语法**
   - 验证 `config/nethergate.json` 是否有效 JSON
   - 使用 JSON 验证工具检查

3. **端口冲突**
   ```bash
   # Windows
   netstat -ano | findstr :25575
   
   # Linux
   lsof -i :25575
   ```

4. **日志文件**
   - 查看 `logs/latest.log`
   - 查看详细错误信息

### Q: 插件加载失败

**A:** 常见原因：

1. **缺少 plugin.json**
   ```json
   {
     "id": "my-plugin",
     "name": "My Plugin",
     "version": "1.0.0",
     "author": "YourName",
     "main": "MyPlugin.dll"
   }
   ```

2. **依赖问题**
   - 检查插件的 DLL 依赖是否都在目录中
   - 查看 `plugin.json` 中的 `dependencies` 配置

3. **API 版本不兼容**
   - 确认插件使用的 `NetherGate.API` 版本
   - 重新编译插件

4. **权限问题**
   - 确保插件目录有读取权限
   - Windows: 检查是否被安全软件阻止

### Q: 命令不响应

**A:**

1. **检查命令注册**
   ```csharp
   Commands.Register(this, new CommandDefinition
   {
       Name = "mycommand",
       Handler = HandleCommand
   });
   ```

2. **检查权限**
   - 确认命令源有执行权限

3. **检查日志**
   - 是否有命令解析错误
   - 是否有处理器异常

### Q: 内存泄漏或性能问题

**A:**

1. **检查事件订阅**
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

2. **检查定时器**
   ```csharp
   private Timer? _timer;
   
   public override Task OnDisableAsync()
   {
       _timer?.Dispose();  // 必须释放！
       return Task.CompletedTask;
   }
   ```

3. **使用性能分析工具**
   - Visual Studio Profiler
   - dotMemory
   - PerfView

---

## 性能与优化

### Q: NetherGate 占用多少资源？

**A:**

**典型使用场景：**
- NetherGate 核心：50-100MB 内存
- 每个插件：10-50MB 内存（取决于插件功能）
- CPU：几乎可以忽略不计（事件驱动）

**优化建议：**
- 只加载需要的插件
- 避免频繁的网络请求
- 使用异步操作
- 合理使用缓存

### Q: 如何优化插件性能？

**A:**

**1. 避免阻塞操作**
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
    var players = await Server.GetPlayersAsync();
}

// ✅ 好：缓存结果
var players = await Server.GetPlayersAsync();
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
    await Rcon.ExecuteCommandAsync($"give {player} diamond");
}

// ✅ 好：批量执行
var commands = players.Select(p => $"give {p} diamond").ToArray();
await Rcon.ExecuteCommandsAsync(commands);
```

### Q: 多少个插件算多？

**A:** 没有硬性限制，但建议：
- **小型服务器**：5-10 个插件
- **中型服务器**：10-20 个插件
- **大型服务器**：20-50 个插件

关键是插件质量，而不是数量。

---

## 其他问题

### Q: NetherGate 是开源的吗？

**A:** （根据实际情况填写）

### Q: 可以用于生产环境吗？

**A:** 当前版本 (0.1.0-alpha) 处于开发阶段，建议：
- ✅ 测试环境：完全可以
- ⚠️ 生产环境：等待稳定版本

### Q: 如何获取帮助？

**A:**

- 📖 查看[文档](../README.md#文档)
- 💬 [GitHub Discussions](https://github.com/YourName/NetherGate/discussions)
- 🐛 [GitHub Issues](https://github.com/YourName/NetherGate/issues)
- 📧 联系维护者

### Q: 如何保持更新？

**A:**

1. **Watch GitHub 仓库**
   - 获取新版本通知

2. **订阅 Release**
   - GitHub Releases RSS

3. **加入社区**
   - Discord/QQ群（如果有）

---

## 🔗 相关资源

- [README](../README.md) - 项目介绍
- [开发文档](../DEVELOPMENT.md) - 架构设计
- [SMP接口](SMP_INTERFACE.md) - 协议接口
- [RCON集成](RCON_INTEGRATION.md) - 命令执行
- [贡献指南](../CONTRIBUTING.md) - 如何贡献

---

**问题没有得到解答？** 

欢迎在 [GitHub Discussions](https://github.com/YourName/NetherGate/discussions) 提问！

