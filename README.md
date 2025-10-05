# NetherGate

<div align="center">

**🌉 A Modern Plugin Loader for Minecraft Java Edition Servers**

[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![C# Version](https://img.shields.io/badge/C%23-13-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Minecraft Version](https://img.shields.io/badge/Minecraft-1.21.9%2B-green.svg)](https://www.minecraft.net/)
[![License](https://img.shields.io/badge/license-TBD-yellow.svg)](LICENSE)

[English](#) | [简体中文](#)

</div>

---

## ✨ 特性

- 🎮 **服务器进程管理** - 自动启动和管理 MC 服务器，监听输出，支持崩溃重启
- 🚀 **三位一体架构** - SMP（结构化管理）+ RCON（命令执行）+ 日志监听（事件捕获）
- 🎯 **游戏内命令支持** - 插件可注册游戏内命令，玩家直接在游戏中使用
- 🔌 **DLL 插件系统** - 插件编译为 .NET DLL，支持热加载
- 💪 **强类型 API** - 利用 C# 的类型安全特性，更易维护
- ⚡ **高性能异步** - 基于 async/await，充分利用现代 .NET 性能
- 📡 **丰富的事件系统** - 监听服务器各类事件并分发给插件
- 🔗 **依赖管理** - 智能处理插件依赖关系
- ⚙️ **灵活的启动配置** - 完全自定义 Java 启动参数、内存设置
- 🛠️ **易于开发** - 完善的 API 和良好的 IDE 支持

---

## 🎯 为什么选择 NetherGate？

### vs MCDR (MCDReforged)

| 特性 | MCDR | NetherGate |
|------|------|-----------|
| 协议 | RCON / 标准输入 | **服务端管理协议** ✨ |
| 语言 | Python | **C#** |
| 性能 | 解释执行 | **JIT/AOT 编译** 🚀 |
| 类型安全 | 动态类型 | **强类型** ✅ |
| 插件格式 | .py | **.dll** (编译) |
| 控制能力 | 有限 | **丰富** (白名单/OP/封禁等) |

---

## 📋 功能概览

### 三位一体的服务器控制

NetherGate 结合三种技术，提供完整的服务器管理能力：

#### 1️⃣ 服务端管理协议 (SMP) - 结构化管理

- ✅ **白名单管理** - 添加/删除/查询白名单
- ✅ **封禁管理** - 玩家封禁和 IP 封禁
- ✅ **玩家管理** - 查询在线玩家、踢出玩家
- ✅ **管理员管理** - OP 列表管理
- ✅ **服务器控制** - 状态查询、保存世界、停止服务器
- ✅ **游戏规则** - 读取和修改游戏规则
- ✅ **实时通知** - 监听服务器事件推送

#### 2️⃣ RCON - 游戏命令执行

- ✅ **任意命令** - 执行任何 Minecraft 命令
- ✅ **物品给予** - `/give` 给予玩家物品
- ✅ **玩家传送** - `/tp` 传送玩家
- ✅ **效果管理** - `/effect` 施加药水效果
- ✅ **富文本消息** - `/tellraw` 发送可点击消息
- ✅ **游戏内命令** - 玩家输入 `/myplugin` 触发插件功能

#### 3️⃣ 日志监听 - 事件捕获

- ✅ **玩家聊天** - 捕获玩家发送的消息
- ✅ **命令执行** - 监听玩家输入的命令
- ✅ **游戏事件** - 解析服务器日志获取游戏事件

> 💡 **组合使用示例**：玩家在游戏中输入 `/myplugin give diamond` → 日志监听捕获 → 插件处理 → 通过RCON执行 `/give` → 玩家获得钻石  
> 详见：[RCON集成文档](docs/RCON_INTEGRATION.md)

---

## 🚀 快速开始

### 前置要求

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- Minecraft Java Edition 服务器 1.21.9+
- 服务器已启用服务端管理协议

### 配置 Minecraft 服务器

编辑 `server.properties`，添加以下配置：

```properties
# 启用服务端管理协议 (SMP)
management-server-enabled=true
management-server-host=localhost
management-server-port=40745
management-server-secret=<你的40位认证令牌>
management-server-tls-enabled=false  # 开发环境可关闭 TLS

# 启用 RCON（用于执行游戏命令）
enable-rcon=true
rcon.port=25566
rcon.password=<你的RCON密码>
```

生成认证令牌（40 位字母数字）：
```bash
# Linux/Mac
openssl rand -base64 30 | tr -d '+/=' | cut -c1-40

# Windows PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 40 | ForEach-Object {[char]$_})
```

### 安装 NetherGate

```bash
# 克隆仓库
git clone https://github.com/YourName/NetherGate.git
cd NetherGate

# 构建项目
dotnet build

# 运行
dotnet run
```

### 配置 NetherGate

首次启动后，编辑 `config/nethergate.json`：

```json
{
    "server_process": {
        "enabled": true,
        "java": {
            "path": "java"
        },
        "server": {
            "jar": "server.jar",
            "working_directory": "./minecraft_server"
        },
        "memory": {
            "min": 2048,
            "max": 4096
        },
        "arguments": {
            "jvm_prefix": [],
            "jvm_middle": [
                "-XX:+UseG1GC",
                "-XX:MaxGCPauseMillis=200",
                "-Dfile.encoding=UTF-8"
            ],
            "server": ["--nogui"]
        },
        "auto_restart": {
            "enabled": true,
            "max_retries": 3
        }
    },
    "server_connection": {
        "host": "localhost",
        "port": 40745,
        "secret": "你的服务器认证令牌",
        "use_tls": false,
        "auto_connect": true
    },
    "plugins": {
        "directory": "plugins",
        "auto_load": true
    }
}
```

**注意**：
- 设置 `server_process.enabled = true` 让 NetherGate 自动启动 MC 服务器
- 确保 `working_directory` 和 `jar` 路径正确
- `arguments.jvm_middle` 可以自定义任何 JVM 参数（G1GC、ZGC 等）

---

## 🔌 开发插件

### 项目结构（类似 Maven/Gradle）

NetherGate 插件采用类似 Java 的目录布局，让 Bukkit/Spigot 开发者更容易上手：

```
MyPlugin/
├── MyPlugin.csproj              # 项目文件（类似 pom.xml）
├── src/                         # 源代码（类似 src/main/java）
│   ├── MyPlugin.cs
│   ├── Commands/
│   └── Events/
└── resources/                   # 资源文件（类似 src/main/resources）
    ├── plugin.json              # 插件元数据（类似 plugin.yml）
    ├── config.json              # 默认配置
    └── lang/                    # 多语言文件
        ├── en_US.json
        └── zh_CN.json
```

详细指南：[插件项目结构文档](docs/PLUGIN_PROJECT_STRUCTURE.md)

### 快速创建插件

使用脚本快速创建插件项目（包含完整目录结构）：

```bash
# Linux/macOS
./tools/create-plugin.sh MyPlugin

# Windows PowerShell
.\tools\create-plugin.ps1 MyPlugin
```

### 插件示例

**src/MyPlugin.cs**:
```csharp
using NetherGate.API;
using NetherGate.API.Events;

namespace MyPlugin;

public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        Logger.Info("MyPlugin 正在启动...");
        
        // 监听玩家加入事件
        Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoin);
        
        // 注册命令
        Commands.Register(this, new CommandDefinition
        {
            Name = "welcome",
            Description = "欢迎玩家",
            Handler = HandleWelcomeCommand
        });
    }
    
    private async Task OnPlayerJoin(object? sender, PlayerJoinedEvent e)
    {
        Logger.Info($"玩家 {e.Player.Name} 加入了服务器");
        await Server.SendSystemMessageAsync($"欢迎 {e.Player.Name}!");
    }
    
    private async Task<CommandResult> HandleWelcomeCommand(CommandContext ctx)
    {
        await Server.SendSystemMessageAsync("欢迎来到服务器！");
        return CommandResult.Success();
    }
}
```

**resources/plugin.json**:
```json
{
    "id": "my-plugin",
    "name": "My Plugin",
    "version": "1.0.0",
    "author": "Your Name",
    "description": "My awesome plugin",
    "main": "MyPlugin.dll"
}
```

### 编译和部署

```bash
# 编译插件
dotnet build -c Release

# 部署到 NetherGate
# 复制整个输出目录到 plugins/my-plugin/
cp -r bin/Release/net9.0/* ../../NetherGate/plugins/my-plugin/
```

---

## 📚 文档

### 核心文档
- [开发文档](DEVELOPMENT.md) - 详细的架构和设计文档
- [配置指南](docs/CONFIGURATION.md) - 完整的配置说明 ⚙️
- [项目结构](docs/PROJECT_STRUCTURE.md) - 项目目录结构说明
- [API 设计](docs/API_DESIGN.md) - 完整的 API 设计文档
- [SMP 接口封装](docs/SMP_INTERFACE.md) - 服务端管理协议完整封装 ⭐
- [RCON 集成](docs/RCON_INTEGRATION.md) - 游戏内命令和RCON使用 ⭐

### 专题文档
- [服务器进程管理](docs/SERVER_PROCESS.md) - 服务器启动和管理详解
- [插件依赖管理](docs/PLUGIN_DEPENDENCIES.md) - 处理插件的外部依赖
- [插件项目结构](docs/PLUGIN_PROJECT_STRUCTURE.md) - Java 风格的项目布局 ⭐
- [示例插件项目](docs/SAMPLES_PROJECT.md) - 示例插件说明
- [未来扩展性设计](docs/FUTURE_EXTENSIBILITY.md) - 架构演进与未来规划 🔮
- [文档结构指南](docs/DOCUMENTATION_GUIDE.md) - 文档组织和阅读路径 📖
- [常见问题 (FAQ)](docs/FAQ.md) - 问题排查与解答 💡

### 开发参考
- [贡献指南](CONTRIBUTING.md) - 如何参与项目开发
- [服务端管理协议](https://zh.minecraft.wiki/w/%E6%9C%8D%E5%8A%A1%E7%AB%AF%E7%AE%A1%E7%90%86%E5%8D%8F%E8%AE%AE) - 官方协议文档

## 📦 相关项目

- **[NetherGate-Samples](https://github.com/YourName/NetherGate-Samples)** - 示例插件集合（独立仓库）
  - HelloWorld - 最简单的插件示例 ⭐
  - PlayerWelcome - 玩家欢迎插件 ⭐⭐
  - AdminTools - 管理工具插件 ⭐⭐⭐
  - [查看示例项目说明](docs/SAMPLES_PROJECT.md)

---

## 🗺️ 路线图

- [x] 项目初始化
- [ ] **Phase 1**: WebSocket 连接和 JSON-RPC 实现
- [ ] **Phase 2**: 服务端管理协议 API 封装
- [ ] **Phase 3**: 插件加载系统
- [ ] **Phase 4**: 事件系统
- [ ] **Phase 5**: 命令系统
- [ ] **Phase 6**: 高级特性（热重载、插件市场）
- [ ] **Phase 7**: 测试和文档
- [ ] **Phase 8**: 1.0.0 版本发布

查看 [DEVELOPMENT.md](DEVELOPMENT.md) 了解详细计划。

---

## 🏗️ 架构

### 源码结构
```
NetherGate/ (源码仓库)
├── src/
│   ├── NetherGate.Core/    # 核心功能
│   │   ├── Process/        # 服务器进程管理
│   │   ├── Protocol/       # 协议层 (WebSocket, JSON-RPC)
│   │   ├── Plugin/         # 插件管理
│   │   ├── Event/          # 事件系统
│   │   ├── Command/        # 命令系统
│   │   └── Config/         # 配置管理
│   ├── NetherGate.API/     # 公共 API 接口
│   └── NetherGate.Host/    # 主程序
├── docs/                    # 文档
└── config.example.json      # 配置模板
```

### 运行时结构（自动生成）
```
NetherGate/ (运行时)
├── NetherGate.exe
├── config/                  # 配置目录（自动创建）
│   ├── nethergate.json
│   └── <plugin-id>/
│       └── config.json
├── plugins/                 # 插件目录（自动创建）
│   └── <plugin-id>/
│       ├── plugin.json
│       └── *.dll
└── logs/                    # 日志目录（自动创建）
```

---

## 🤝 贡献

我们欢迎各种形式的贡献！

- 🐛 报告 Bug
- 💡 提出新功能建议
- 📝 改进文档
- 🔧 提交代码

请查看 [贡献指南](CONTRIBUTING.md) 了解详情

---

## 📜 许可证

（待定）

---

## 🙏 致谢

- [MCDReforged](https://github.com/Fallen-Breath/MCDReforged) - 设计灵感
- [Minecraft Wiki](https://zh.minecraft.wiki/) - 协议文档
- [.NET Foundation](https://dotnet.foundation/) - 优秀的开发平台

---

## 📞 联系方式

- **Issues**: [GitHub Issues](https://github.com/YourName/NetherGate/issues)
- **Discussions**: [GitHub Discussions](https://github.com/YourName/NetherGate/discussions)

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给个 Star！⭐**

Made with ❤️ by NetherGate Team

</div>

