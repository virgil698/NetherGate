# NetherGate

<div align="center">

**🌐 现代化的 .NET Minecraft 服务器插件加载器 🌐**

[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dot.net)
[![Minecraft](https://img.shields.io/badge/Minecraft-Java%201.21.9+-brightgreen?logo=minecraft)](https://minecraft.net)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Coverage](https://img.shields.io/badge/Coverage-100%25-success)](docs/功能覆盖率报告.md)

</div>

---

## ✨ **为什么选择 NetherGate？**

### **🎯 易于使用，功能强大**

NetherGate 让插件开发变得**简单而强大**：

- 🎨 **从入门到精通**：无论是简单的签到插件、礼包系统，还是复杂的 QQ 机器人、备份插件、经济插件，都能轻松实现
- ⚡ **开箱即用**：完整的 API 设计，无需学习复杂概念即可上手
- 🔥 **真正的热重载**：插件更新无需重启服务器，保留运行状态，支持跨版本迁移
- 📦 **智能依赖管理**：自动下载 NuGet 包，智能解决版本冲突，告别依赖地狱
- 🎭 **完全隔离**：每个插件独立运行环境，互不干扰，崩溃不影响其他插件

### **🚀 100% 功能覆盖率**

| 核心能力 | 状态 | 说明 |
|---------|------|------|
| **SMP 协议** | ✅ 100% | 35 个方法 + 22 个通知处理 |
| **RCON 命令** | ✅ 100% | 完整的游戏命令支持（60+ 命令） |
| **日志监听** | ✅ 100% | 服务器生命周期 + 玩家事件 |
| **网络事件** | ✅ 100% | 4 种监听模式（LogBased 立即可用） |
| **NBT 数据** | ✅ 100% | 读取 + 写入（玩家/世界数据） |
| **数据组件** | ✅ 100% | 1.20.5+ 物品组件系统支持 |
| **文件操作** | ✅ 100% | 读写/监听/备份 |
| **性能监控** | ✅ 100% | CPU/内存/TPS |

**详细报告：** [功能覆盖率文档](docs/功能覆盖率报告.md)

### **💡 核心优势**

- ⚡ **高性能**：原生 .NET 编译，执行效率远超解释型语言
- 🛡️ **安全隔离**：AssemblyLoadContext 机制，插件完全隔离
- 🎨 **现代设计**：异步 API、事件驱动、依赖注入
- 🔥 **先进的热重载**：
  - ✅ 保留插件状态（数据不丢失）
  - ✅ 支持代码修改后立即生效
  - ✅ 自动卸载旧版本，加载新版本
  - ✅ 插件间依赖自动重载
- 🌐 **跨平台**：支持 8 种平台架构组合
  - **Windows**: x64, x86, ARM64
  - **Linux**: x64, ARM, ARM64
  - **macOS**: Intel x64, Apple Silicon ARM64
- 📚 **文档完善**：覆盖所有功能的中文文档，丰富示例

---

## 🎮 **兼容性说明**

### **支持的 Minecraft 版本**
- ✅ **Java Edition 1.21.9+**（及以上版本）

### **支持的服务端类型**

NetherGate 支持所有开放 **RCON + SMP 协议**的 Minecraft 服务端，包括但不限于：

- **Vanilla** (原版)
- **Paper**
- **Purpur**
- **Leaves**
- **Leaf**
- **Forge**
- **Fabric**
- **NeoForge**

**前置要求：**
- ✅ 服务端需开启 **RCON** 功能（用于命令执行）
- ✅ 服务端需支持 **SMP 协议**（用于服务器管理）

---

## 📥 **快速开始**

### **1. 下载**

从 [Releases](../../releases) 下载适合您系统的版本：

| 操作系统 | 架构 | 文件名 |
|---------|------|--------|
| **Windows** | x64 (推荐) | `nethergate-nightly-win-x64.zip` |
| Windows | x86 | `nethergate-nightly-win-x86.zip` |
| Windows | ARM64 | `nethergate-nightly-win-arm64.zip` |
| **Linux** | x64 (推荐) | `nethergate-nightly-linux-x64.tar.gz` |
| Linux | ARM | `nethergate-nightly-linux-arm.tar.gz` |
| Linux | ARM64 | `nethergate-nightly-linux-arm64.tar.gz` |
| **macOS** | Intel x64 | `nethergate-nightly-osx-x64.tar.gz` |
| **macOS** | Apple Silicon | `nethergate-nightly-osx-arm64.tar.gz` |

或自行编译：

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

**详细配置：** [配置文件详解](docs/01-快速开始/配置文件详解.md)

### **3. 启动**

```bash
./NetherGate.Host
```

**三种启动模式：**
- **Java 模式**：NetherGate 直接启动 Minecraft 服务器
- **Script 模式**：使用现有启动脚本
- **External 模式**：连接到已运行的服务器（仅管理功能）

---

## 🔌 **5 分钟创建第一个插件**

### **1. 安装 NuGet 包**

NetherGate.API 已发布到 NuGet.org，可以直接安装：

```bash
dotnet new classlib -n MyPlugin
cd MyPlugin
dotnet add package NetherGate.API
```

[![NuGet](https://img.shields.io/nuget/v/NetherGate.API.svg)](https://www.nuget.org/packages/NetherGate.API/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetherGate.API.svg)](https://www.nuget.org/packages/NetherGate.API/)

### **2. 编写代码**

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

    private async void OnPlayerJoined(PlayerJoinedEvent e)
    {
        // 发送欢迎消息
        await _context.GameDisplay.SendChatMessageAsync(
            e.PlayerName, 
            $"§a欢迎 {e.PlayerName} 加入服务器！"
        );
    }

    public void OnDisable()
    {
        _context.Logger.Info("MyPlugin 已禁用");
    }
}
```

### **3. 构建并加载**

```bash
dotnet build
# 将生成的 DLL 复制到 plugins/ 目录
# 使用 `plugin load MyPlugin` 命令加载，或重启 NetherGate
```

**完整教程：** [第一个插件](docs/01-快速开始/第一个插件.md) | [插件开发指南](docs/03-插件开发/插件开发指南.md)

---

## 📚 **丰富的功能 API**

NetherGate 提供了完整的 API，让你能实现**任何想法**：

### **🎮 游戏交互**
```csharp
// BossBar、Title、ActionBar、聊天消息
await context.GameDisplay.ShowBossBarAsync("my_boss", "欢迎！", 1.0f);
await context.GameDisplay.ShowTitleAsync("@a", "§6标题", "§e副标题");
```

### **📦 数据操作**
```csharp
// NBT 数据（1.20.4-）、数据组件（1.20.5+）
var playerData = await context.PlayerDataReader.ReadPlayerDataAsync(uuid);
await context.NbtDataWriter.UpdatePlayerHealthAsync(uuid, 20.0f);

// 物品组件系统
var item = await context.ItemComponentReader.ReadInventorySlotAsync(player, slot);
await context.ItemComponentWriter.UpdateComponentAsync(player, slot, "custom_name", "神剑");
```

### **🎯 SMP 协议**
```csharp
// 完整的服务器管理
var players = await context.SmpApi.GetPlayersAsync();
await context.SmpApi.AddToAllowlistAsync("Player123");
await context.SmpApi.UpdateGameRuleAsync("doDaylightCycle", "false");
```

### **🔧 RCON 命令**
```csharp
// 执行任意 Minecraft 命令
var result = await context.RconClient.ExecuteCommandAsync("give @a diamond 64");
```

### **💬 插件间通信**
```csharp
// 跨插件消息传递（适用于模块化开发）
var response = await context.Messenger.SendMessageAsync(
    "EconomyPlugin", "transfer", 
    new { from = "Player1", to = "Player2", amount = 100 }
);
```

### **⚙️ 更多功能**
- 📁 **文件系统**：读写服务器文件、监听变化
- 🔒 **权限系统**：组、继承、通配符
- ⏱️ **性能监控**：CPU、内存、TPS
- 🎭 **事件系统**：30+ 事件类型，支持优先级

**完整 API 文档：** [API 参考](docs/08-参考/API参考.md)

---

## 📖 **文档导航**

### **🎓 新手入门**
- [安装和配置](docs/01-快速开始/安装和配置.md)
- [第一个插件](docs/01-快速开始/第一个插件.md) - 完整的签到插件示例
- [常见问题 (FAQ)](docs/08-参考/FAQ.md)

### **📘 核心功能**
- [SMP 协议](docs/02-核心功能/SMP协议.md) - Server Management Protocol
- [RCON 集成](docs/02-核心功能/RCON集成.md) - 远程命令执行
- [NBT 数据操作](docs/02-核心功能/NBT数据操作.md) - 玩家/世界数据读写 (1.20.4-)
- [物品组件系统](docs/02-核心功能/物品组件系统.md) - 数据组件操作 (1.20.5+)
- [事件系统](docs/02-核心功能/事件系统.md) - 事件订阅和发布
- [权限系统](docs/02-核心功能/权限系统.md) - 权限管理
- [命令系统](docs/02-核心功能/命令系统.md) - 命令注册

### **🔧 插件开发**
- [插件开发指南](docs/03-插件开发/插件开发指南.md)
- [配置文件](docs/03-插件开发/配置文件.md)
- [调试技巧](docs/03-插件开发/调试技巧.md)
- [发布流程](docs/03-插件开发/发布流程.md)

### **⚙️ 高级功能**
- [游戏显示 API](docs/04-高级功能/游戏显示API.md)
- [插件热重载](docs/04-高级功能/插件热重载.md)
- [插件间通信](docs/04-高级功能/插件间通信.md)
- [文件系统](docs/04-高级功能/文件系统.md)
- [性能监控](docs/04-高级功能/性能监控.md)

**完整文档索引：** [docs/README.md](docs/README.md)

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

---

## 🤝 **贡献**

欢迎贡献代码、文档或建议！

- **报告 Bug**：[提交 Issue](../../issues)
- **功能建议**：[参与讨论](../../discussions)
- **代码贡献**：[提交 Pull Request](../../pulls)

**贡献指南：** [CONTRIBUTING.md](CONTRIBUTING.md)

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

### **特别感谢**

感谢 [**MCDReforged**](https://github.com/Fallen-Breath/MCDReforged) 为 NetherGate 提供了优秀的设计思路和开发理念！MCDReforged 在 Python 生态中为 Minecraft 服务器管理树立了标杆，NetherGate 在继承这些优秀理念的基础上，结合 .NET 的高性能和现代化特性，为插件开发者带来更强大、更灵活的开发体验。

### **开源项目**

感谢以下优秀的开源项目：

- [MCDReforged](https://github.com/Fallen-Breath/MCDReforged) - 设计理念和灵感来源
- [fNbt](https://github.com/mstefarov/fNbt) - NBT 数据处理
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YAML 配置支持
- [Minecraft Wiki](https://zh.minecraft.wiki) - 技术文档参考

---

<div align="center">

**⭐ 如果觉得有用，请给个 Star！⭐**

Made with ❤️ by NetherGate Team

[功能覆盖率](docs/功能覆盖率报告.md) • [文档中心](docs/README.md) • [API 参考](docs/08-参考/API参考.md)

</div>
