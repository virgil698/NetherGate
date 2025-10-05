# NetherGate

<div align="center">

**🌐 现代化的 .NET Minecraft 服务器插件加载器 🌐**

[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dot.net)
[![Minecraft](https://img.shields.io/badge/Minecraft-Java%201.20--1.21-brightgreen?logo=minecraft)](https://minecraft.net)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Coverage](https://img.shields.io/badge/Coverage-100%25-success)](docs/功能覆盖率报告.md)

</div>

---

## ✨ **特性一览**

### **🎯 100% 功能覆盖率**

| 核心能力 | 状态 | 说明 |
|---------|------|------|
| **SMP 协议** | ✅ 100% | 35 个方法 + 22 个通知处理 |
| **RCON 命令** | ✅ 100% | 完整的游戏命令支持（60+ 命令） |
| **日志监听** | ✅ 100% | 服务器生命周期 + 玩家事件 |
| **网络事件** | ✅ 100% | 4 种监听模式（LogBased 立即可用） |
| **NBT 数据** | ✅ 100% | 读取 + 写入（玩家/世界数据） |
| **文件操作** | ✅ 100% | 读写/监听/备份 |
| **性能监控** | ✅ 100% | CPU/内存/TPS |

**详细报告：** [功能覆盖率文档](docs/功能覆盖率报告.md)

### **🚀 核心优势**

- ⚡ **高性能**：原生 .NET 编译，执行效率高
- 🔒 **插件隔离**：AssemblyLoadContext 机制，插件互不干扰
- 🎨 **现代设计**：异步 API、事件驱动、依赖注入
- 📦 **依赖管理**：自动 NuGet 下载，智能版本冲突解决
- 🔥 **热重载**：无需重启服务器即可更新插件
- 🌐 **跨平台**：Windows、Linux、macOS 全支持
- 📚 **文档完善**：覆盖所有功能的中文文档

---

## 📥 **快速开始**

### **1. 下载**

从 [Releases](../../releases) 下载最新版本，或自行编译：

```bash
git clone https://github.com/your-org/NetherGate.git
cd NetherGate
dotnet build -c Release
```

### **2. 配置**

首次运行会自动生成配置文件：

```bash
cd bin/Release
./NetherGate.Host
```

或使用交互式配置向导：

```bash
./NetherGate.Host --wizard
```

**配置文件示例：** `nethergate-config.yaml`

```yaml
server_process:
  launch_method: java  # java | script | external
  working_directory: ./
  
  java:
    jar_path: server.jar
    min_memory: 2G
    max_memory: 4G
```

**详细配置：** [配置文件详解](docs/05-配置和部署/配置文件详解.md)

### **3. 启动**

```bash
./NetherGate.Host
```

**三种启动模式：**
- **Java 模式**：NetherGate 直接启动 Minecraft 服务器
- **Script 模式**：使用现有启动脚本
- **External 模式**：连接到已运行的服务器（仅管理功能）

---

## 🔌 **插件开发**

### **最简单的插件**

创建一个新的 .NET 类库项目：

```bash
dotnet new classlib -n MyPlugin
cd MyPlugin
dotnet add reference ../NetherGate.API/NetherGate.API.csproj
```

编写插件代码：

```csharp
using NetherGate.API.Plugins;
using NetherGate.API.Events;

namespace MyPlugin;

public class MyPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        _context.Logger.Info("MyPlugin 已启用！");
        
        // 订阅玩家加入事件
        _context.EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
    }

    private void OnPlayerJoined(PlayerJoinedEvent e)
    {
        _context.Logger.Info($"玩家 {e.PlayerName} 加入了服务器！");
        
        // 发送欢迎消息
        _context.GameDisplay.SendChatMessageAsync(
            "@a", 
            $"欢迎 {e.PlayerName} 加入服务器！"
        );
    }

    public void OnDisable()
    {
        _context.Logger.Info("MyPlugin 已禁用");
    }
}
```

编译并放入 `plugins/` 文件夹：

```bash
dotnet build
cp bin/Debug/net9.0/MyPlugin.dll ../NetherGate/plugins/
```

**完整教程：** [插件开发指南](docs/03-插件开发/插件开发指南.md)

---

## 📖 **文档导航**

### **🎓 新手入门**
- [安装和配置](docs/01-快速开始/安装和配置.md)
- [第一个插件](docs/01-快速开始/第一个插件.md)
- [常见问题 (FAQ)](docs/01-快速开始/FAQ.md)

### **📘 核心功能**
- [SMP 协议](docs/02-核心功能/SMP协议.md) - Server Management Protocol
- [RCON 集成](docs/02-核心功能/RCON集成.md) - 远程命令执行
- [NBT 数据操作](docs/02-核心功能/NBT数据操作.md) - 玩家/世界数据读写
- [事件系统](docs/03-插件开发/事件系统.md) - 事件订阅和发布

### **🔧 插件开发**
- [插件开发指南](docs/03-插件开发/插件开发指南.md)
- [API 参考](docs/03-插件开发/API参考.md)
- [插件依赖管理](docs/03-插件开发/插件依赖管理.md)
- [热重载](docs/03-插件开发/热重载.md)

### **⚙️ 配置和部署**
- [配置文件详解](docs/05-配置和部署/配置文件详解.md)
- [服务器启动方式](docs/05-配置和部署/服务器启动方式.md)
- [CLI 命令](docs/05-配置和部署/CLI命令.md)

**完整文档索引：** [docs/README.md](docs/README.md)

---

## 🎯 **功能演示**

### **SMP 协议管理**

```csharp
// 获取在线玩家
var players = await context.SmpApi.GetPlayersAsync();
foreach (var player in players)
{
    context.Logger.Info($"玩家：{player.Name}，等级：{player.Level}");
}

// 添加白名单
await context.SmpApi.AddToAllowlistAsync("Player123");

// 设置游戏规则
await context.SmpApi.UpdateGameRuleAsync("doDaylightCycle", "false");
```

### **游戏显示控制**

```csharp
// 显示 BossBar
await context.GameDisplay.ShowBossBarAsync(
    "my_boss", 
    "欢迎来到服务器", 
    1.0f, 
    BossBarColor.Green
);

// 显示 Title
await context.GameDisplay.ShowTitleAsync(
    "@a", 
    "§6欢迎！", 
    "§e开始你的冒险", 
    10, 70, 20
);

// 发送彩色消息
await context.GameDisplay.SendColoredMessageAsync(
    "@a", 
    "这是一条红色消息", 
    TextColor.Red, 
    bold: true
);
```

### **NBT 数据操作**

```csharp
// 读取玩家数据
var playerData = await context.PlayerDataReader.ReadPlayerDataAsync(playerUuid);
context.Logger.Info($"生命值：{playerData.Health}，等级：{playerData.XpLevel}");

// 修改玩家位置
await context.NbtDataWriter.UpdatePlayerPositionAsync(
    playerUuid, 
    x: 0, y: 100, z: 0, 
    dimension: "minecraft:overworld"
);

// 给予物品（附魔钻石剑）
await context.NbtDataWriter.AddItemToInventoryAsync(
    playerUuid,
    new ItemStack
    {
        Id = "minecraft:diamond_sword",
        Count = 1,
        Enchantments = new List<Enchantment>
        {
            new() { Id = "minecraft:sharpness", Level = 5 },
            new() { Id = "minecraft:unbreaking", Level = 3 }
        },
        CustomName = "神剑"
    }
);
```

### **插件间通信**

```csharp
// 插件 A：发送消息
var response = await context.Messenger.SendMessageAsync(
    targetPluginId: "PluginB",
    channel: "economy.transfer",
    data: new { from = "Player1", to = "Player2", amount = 100 },
    requireResponse: true
);

// 插件 B：接收消息
context.Messenger.SubscribeWithResponse("economy.transfer", async msg => 
{
    var data = msg.Data;
    // 处理转账逻辑...
    return new { success = true, newBalance = 500 };
});
```

---

## 🏗️ **项目架构**

```
NetherGate/
├── src/
│   ├── NetherGate.API/          # 插件 API 接口
│   ├── NetherGate.Core/         # 核心实现
│   └── NetherGate.Host/         # 主程序
├── docs/                        # 完整文档
├── scripts/                     # 构建脚本
└── bin/Release/                 # 编译输出
    ├── NetherGate.Host.exe      # 主程序
    ├── plugins/                 # 插件目录
    ├── config/                  # 配置目录
    └── logs/                    # 日志目录
```

**详细架构：** [项目架构文档](docs/06-架构和设计/项目架构.md)

---

## 🤝 **贡献**

欢迎贡献代码、文档或建议！

- **报告 Bug**：[提交 Issue](../../issues)
- **功能建议**：[参与讨论](../../discussions)
- **代码贡献**：[提交 Pull Request](../../pulls)

**贡献指南：** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📊 **对比其他框架**

| 特性 | NetherGate | MCDReforged | BungeeCord |
|------|-----------|-------------|------------|
| **语言** | C# (.NET 9) | Python 3 | Java |
| **性能** | ⚡⚡⚡ 高 | 🐌 中等 | ⚡⚡ 高 |
| **插件隔离** | ✅ 完整 | ❌ 无 | ✅ 完整 |
| **SMP 协议** | ✅ 完整 | ❌ 无 | ⏳ 部分 |
| **NBT 操作** | ✅ 读写 | ⏳ 仅读 | ❌ 无 |
| **热重载** | ✅ 支持 | ✅ 支持 | ❌ 不支持 |
| **依赖管理** | ✅ 自动 | ❌ 手动 | ⏳ Maven |
| **跨平台** | ✅ 全平台 | ✅ 全平台 | ✅ 全平台 |
| **文档** | ✅ 完整中文 | ✅ 中英文 | ✅ 英文 |

---

## 📄 **许可证**

本项目采用 [MIT License](LICENSE) 开源协议。

---

## 💬 **社区和支持**

- 📖 **文档中心**：[docs/README.md](docs/README.md)
- 🐛 **问题反馈**：[GitHub Issues](../../issues)
- 💡 **功能讨论**：[GitHub Discussions](../../discussions)
- 📧 **联系方式**：查看 [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🌟 **致谢**

感谢以下开源项目：

- [fNbt](https://github.com/mstefarov/fNbt) - NBT 数据处理
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YAML 配置支持
- [Minecraft Wiki](https://zh.minecraft.wiki) - 技术文档参考

---

<div align="center">

**⭐ 如果觉得有用，请给个 Star！⭐**

Made with ❤️ by NetherGate Team

[功能覆盖率](docs/功能覆盖率报告.md) • [文档中心](docs/README.md) • [API 参考](docs/03-插件开发/API参考.md)

</div>