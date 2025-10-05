# NetherGate 项目结构详解

本文档详细说明 NetherGate 项目的目录结构和各模块职责。

---

## 📁 源码仓库结构

**注意**：以下是源码仓库（Git）的目录结构，不包含运行时生成的文件。

```
NetherGate/ (源码仓库)
├── src/
│   ├── NetherGate.Core/              # 核心库
│   │   ├── Process/                  # 服务器进程管理
│   │   │   ├── ServerProcessManager.cs
│   │   │   ├── ProcessLauncher.cs
│   │   │   ├── OutputMonitor.cs
│   │   │   ├── StateDetector.cs
│   │   │   ├── CrashHandler.cs
│   │   │   ├── CommandBuilder.cs
│   │   │   └── Events/
│   │   │       ├── ServerOutputEventArgs.cs
│   │   │       ├── ServerStartedEventArgs.cs
│   │   │       ├── ServerStoppedEventArgs.cs
│   │   │       └── ServerCrashedEventArgs.cs
│   │   │
│   │   ├── Protocol/                 # 协议层
│   │   │   ├── WebSocket/
│   │   │   │   ├── ServerConnection.cs
│   │   │   │   ├── ConnectionManager.cs
│   │   │   │   ├── ReconnectionStrategy.cs
│   │   │   │   └── WebSocketConfig.cs
│   │   │   ├── JsonRpc/
│   │   │   │   ├── JsonRpcHandler.cs
│   │   │   │   ├── JsonRpcRequest.cs
│   │   │   │   ├── JsonRpcResponse.cs
│   │   │   │   ├── JsonRpcNotification.cs
│   │   │   │   ├── JsonRpcError.cs
│   │   │   │   └── JsonRpcBatch.cs
│   │   │   ├── Management/
│   │   │   │   ├── MinecraftServerApi.cs
│   │   │   │   ├── DTOs/
│   │   │   │   │   ├── PlayerDto.cs
│   │   │   │   │   ├── UserBanDto.cs
│   │   │   │   │   ├── IpBanDto.cs
│   │   │   │   │   ├── OperatorDto.cs
│   │   │   │   │   ├── ServerState.cs
│   │   │   │   │   └── TypedRule.cs
│   │   │   │   ├── Methods/
│   │   │   │   │   ├── AllowlistMethods.cs
│   │   │   │   │   ├── BanMethods.cs
│   │   │   │   │   ├── PlayerMethods.cs
│   │   │   │   │   ├── OperatorMethods.cs
│   │   │   │   │   ├── ServerMethods.cs
│   │   │   │   │   └── GameRuleMethods.cs
│   │   │   │   └── Notifications/
│   │   │   │       ├── ServerNotifications.cs
│   │   │   │       ├── PlayerNotifications.cs
│   │   │   │       ├── AllowlistNotifications.cs
│   │   │   │       ├── BanNotifications.cs
│   │   │   │       └── GameRuleNotifications.cs
│   │   │   └── Security/
│   │   │       ├── AuthenticationHandler.cs
│   │   │       ├── TlsConfiguration.cs
│   │   │       └── SecretManager.cs
│   │   │
│   │   ├── Plugin/                   # 插件系统
│   │   │   ├── IPlugin.cs
│   │   │   ├── PluginBase.cs
│   │   │   ├── PluginMetadata.cs
│   │   │   ├── PluginState.cs
│   │   │   ├── PluginLoader.cs
│   │   │   ├── PluginManager.cs
│   │   │   ├── PluginContext.cs
│   │   │   ├── PluginAssemblyLoadContext.cs
│   │   │   ├── Dependency/
│   │   │   │   ├── DependencyResolver.cs
│   │   │   │   ├── DependencyGraph.cs
│   │   │   │   ├── VersionRequirement.cs
│   │   │   │   └── CircularDependencyException.cs
│   │   │   └── Attributes/
│   │   │       ├── PluginAttribute.cs
│   │   │       ├── PluginDependencyAttribute.cs
│   │   │       └── PluginPermissionAttribute.cs
│   │   │
│   │   ├── Event/                    # 事件系统
│   │   │   ├── EventBus.cs
│   │   │   ├── EventPriority.cs
│   │   │   ├── EventHandler.cs
│   │   │   ├── EventSubscription.cs
│   │   │   ├── CancellableEvent.cs
│   │   │   ├── Events/
│   │   │   │   ├── Server/
│   │   │   │   │   ├── ServerStartedEvent.cs
│   │   │   │   │   ├── ServerStoppedEvent.cs
│   │   │   │   │   ├── ServerStatusChangedEvent.cs
│   │   │   │   │   └── ServerHeartbeatEvent.cs
│   │   │   │   ├── Player/
│   │   │   │   │   ├── PlayerJoinedEvent.cs
│   │   │   │   │   ├── PlayerLeftEvent.cs
│   │   │   │   │   └── PlayerKickedEvent.cs
│   │   │   │   ├── Admin/
│   │   │   │   │   ├── OperatorAddedEvent.cs
│   │   │   │   │   ├── OperatorRemovedEvent.cs
│   │   │   │   │   ├── AllowlistChangedEvent.cs
│   │   │   │   │   ├── BanAddedEvent.cs
│   │   │   │   │   └── BanRemovedEvent.cs
│   │   │   │   └── GameRule/
│   │   │   │       └── GameRuleUpdatedEvent.cs
│   │   │   └── Dispatcher/
│   │   │       ├── EventDispatcher.cs
│   │   │       └── AsyncEventDispatcher.cs
│   │   │
│   │   ├── Command/                  # 命令系统
│   │   │   ├── CommandManager.cs
│   │   │   ├── CommandDefinition.cs
│   │   │   ├── CommandContext.cs
│   │   │   ├── CommandResult.cs
│   │   │   ├── CommandParser.cs
│   │   │   ├── CommandExecutor.cs
│   │   │   ├── Permission/
│   │   │   │   ├── PermissionManager.cs
│   │   │   │   ├── Permission.cs
│   │   │   │   └── PermissionNode.cs
│   │   │   └── Built-in/
│   │   │       ├── PluginCommands.cs
│   │   │       ├── StatusCommands.cs
│   │   │       ├── PlayerCommands.cs
│   │   │       └── HelpCommands.cs
│   │   │
│   │   ├── Config/                   # 配置管理
│   │   │   ├── IConfiguration.cs
│   │   │   ├── ConfigurationManager.cs
│   │   │   ├── JsonConfigProvider.cs
│   │   │   ├── YamlConfigProvider.cs
│   │   │   ├── NetherGateConfig.cs
│   │   │   ├── PluginConfig.cs
│   │   │   └── Validation/
│   │   │       ├── ConfigValidator.cs
│   │   │       └── ValidationRules.cs
│   │   │
│   │   ├── Logging/                  # 日志系统
│   │   │   ├── ILogger.cs
│   │   │   ├── LoggerFactory.cs
│   │   │   ├── LogLevel.cs
│   │   │   ├── Loggers/
│   │   │   │   ├── ConsoleLogger.cs
│   │   │   │   ├── FileLogger.cs
│   │   │   │   ├── CompositeLogger.cs
│   │   │   │   └── PluginLogger.cs
│   │   │   └── Formatting/
│   │   │       ├── LogFormatter.cs
│   │   │       └── ColorFormatter.cs
│   │   │
│   │   ├── Util/                     # 工具类
│   │   │   ├── AsyncHelper.cs
│   │   │   ├── JsonHelper.cs
│   │   │   ├── FileSystemHelper.cs
│   │   │   ├── VersionHelper.cs
│   │   │   └── TaskScheduler.cs
│   │   │
│   │   └── Exceptions/               # 异常定义
│   │       ├── NetherGateException.cs
│   │       ├── PluginException.cs
│   │       ├── ProtocolException.cs
│   │       ├── ConfigurationException.cs
│   │       └── CommandException.cs
│   │
│   ├── NetherGate.API/               # 公共 API 接口
│   │   ├── IPluginApi.cs
│   │   ├── IServerApi.cs
│   │   ├── IEventApi.cs
│   │   ├── ICommandApi.cs
│   │   ├── IConfigApi.cs
│   │   ├── ILoggerApi.cs
│   │   └── Models/
│   │       ├── Player.cs
│   │       ├── Server.cs
│   │       └── GameRule.cs
│   │
│   └── NetherGate.Host/              # 主程序
│       ├── Program.cs
│       ├── Application.cs
│       ├── Startup.cs
│       ├── ConsoleInterface.cs
│       └── ServiceConfiguration.cs
│
├── tests/                            # 测试项目
│   ├── NetherGate.Core.Tests/
│   │   ├── Protocol/
│   │   │   ├── JsonRpcTests.cs
│   │   │   └── ServerConnectionTests.cs
│   │   ├── Plugin/
│   │   │   ├── PluginLoaderTests.cs
│   │   │   └── DependencyResolverTests.cs
│   │   ├── Event/
│   │   │   └── EventBusTests.cs
│   │   └── Command/
│   │       └── CommandParserTests.cs
│   │
│   ├── NetherGate.Integration.Tests/
│   │   ├── FullStackTests.cs
│   │   ├── PluginLifecycleTests.cs
│   │   └── ProtocolIntegrationTests.cs
│   │
│   └── NetherGate.Performance.Tests/
│       ├── EventDispatchBenchmark.cs
│       ├── PluginLoadBenchmark.cs
│       └── JsonRpcBenchmark.cs
│
├── docs/                             # 文档目录
│   ├── API.md                        # API 参考
│   ├── PluginGuide.md                # 插件开发指南
│   ├── Architecture.md               # 架构设计
│   ├── Protocol.md                   # 协议说明
│   ├── Examples.md                   # 示例代码
│   ├── FAQ.md                        # 常见问题
│   ├── Changelog.md                  # 更新日志
│   └── images/                       # 文档图片
│       ├── architecture.png
│       └── workflow.png
│
├── tools/                            # 开发工具
│   ├── PluginGenerator/              # 插件项目生成器
│   │   ├── PluginTemplate/
│   │   └── generator.ps1
│   └── TokenGenerator/               # 认证令牌生成器
│       └── generate-token.ps1
│
├── .github/                          # GitHub 配置
│   ├── workflows/
│   │   ├── build.yml
│   │   ├── test.yml
│   │   └── release.yml
│   ├── ISSUE_TEMPLATE/
│   └── PULL_REQUEST_TEMPLATE.md
│
├── config.example.json               # 配置模板（用于首次启动生成配置）
├── NetherGate.sln                    # 解决方案文件
├── Directory.Build.props             # 构建属性
├── .gitignore                        # Git 忽略规则（排除运行时文件）
├── .editorconfig
├── LICENSE
├── README.md
├── DEVELOPMENT.md
└── CONTRIBUTING.md
```

**注意**：
- ✅ 源码仓库只包含源代码、文档和配置模板
- ❌ 不包含运行时生成的文件（config/、plugins/、logs/）
- ✅ .gitignore 已配置忽略运行时目录

---

## 📁 运行时目录结构

**注意**：以下是运行 NetherGate 后自动生成的目录结构（不在 Git 仓库中）。

```
NetherGate/ (运行时)
├── NetherGate.exe                    # 编译后的主程序
├── NetherGate.dll
├── NetherGate.API.dll
├── 其他依赖DLL...
│
├── config/                           # 配置目录（首次启动自动创建）
│   ├── nethergate.json              # 主程序配置
│   ├── example-plugin/              # 插件配置
│   │   └── config.json
│   └── another-plugin/
│       └── config.json
│
├── plugins/                          # 插件目录（首次启动自动创建）
│   ├── example-plugin/
│   │   ├── plugin.json              # 插件元数据
│   │   ├── ExamplePlugin.dll        # 插件主DLL
│   │   ├── Newtonsoft.Json.dll      # 插件依赖
│   │   └── data/                    # 插件数据目录（可选）
│   │       └── database.db
│   ├── another-plugin/
│   │   ├── plugin.json
│   │   └── AnotherPlugin.dll
│   └── README.md                    # 插件使用说明（自动生成）
│
├── logs/                             # 日志目录（自动创建）
│   ├── latest.log                   # 当前日志文件
│   ├── 2025-10-04-1.log.gz         # 归档日志（自动压缩）
│   ├── 2025-10-04-2.log.gz
│   └── 2025-10-03-1.log.gz
│
├── shared-libs/                      # 共享依赖库（可选）
│   ├── Newtonsoft.Json.dll
│   └── Serilog.dll
│
└── minecraft_server/                 # MC 服务器目录（可选）
    ├── server.jar
    ├── server.properties
    ├── eula.txt
    └── world/
```

**目录说明**：

### config/（配置目录）
- 首次启动时自动创建
- 主程序配置：`config/nethergate.json`（从 `config.example.json` 复制）
- 插件配置：`config/<plugin-id>/config.json`
- 配置和代码分离，更新插件不影响配置

### plugins/（插件目录）
- 首次启动时自动创建
- 每个插件一个子目录
- 包含插件 DLL 和依赖
- 可选的 `data/` 子目录存储插件数据

### logs/（日志目录）
- 自动创建
- 按日期滚动日志文件
- 可配置日志大小和保留数量

### shared-libs/（共享依赖库，可选）
- 如果使用共享依赖模式，手动创建此目录
- 存放多个插件共用的 DLL

---

## 📋 首次启动流程

```
1. 运行 NetherGate.exe
   ↓
2. 检测目录结构
   ├─ config/ 不存在？创建并从 config.example.json 生成配置
   ├─ plugins/ 不存在？创建空目录
   └─ logs/ 不存在？创建空目录
   ↓
3. 读取 config/nethergate.json
   ↓
4. 启动 MC 服务器（如果配置启用）
   ↓
5. 扫描 plugins/ 目录，加载插件
   ↓
6. 系统就绪
```

---

## 📦 核心模块详解

### 1. NetherGate.Core

核心功能库，包含所有基础设施代码。

#### 1.0 Process 层

**职责**：管理 Minecraft 服务器进程

- **ServerProcessManager**: 进程管理器主类
- **ProcessLauncher**: 启动服务器进程
- **OutputMonitor**: 监听标准输出/错误
- **StateDetector**: 检测服务器状态（启动完成、崩溃等）
- **CrashHandler**: 处理崩溃和自动重启
- **CommandBuilder**: 构建 Java 启动命令

详细设计见 [SERVER_PROCESS.md](SERVER_PROCESS.md)

#### 1.1 Protocol 层

**职责**：处理与 Minecraft 服务器的通信

- **WebSocket**: 管理 WebSocket 连接，包括认证、TLS、重连
- **JsonRpc**: 实现 JSON-RPC 2.0 协议，处理请求/响应/通知
- **Management**: 封装服务端管理协议的所有方法
- **Security**: 认证和安全相关功能

#### 1.2 Plugin 层

**职责**：插件生命周期管理

- **PluginLoader**: 从 DLL 加载插件
- **PluginManager**: 管理所有插件的状态
- **Dependency**: 解析和验证插件依赖关系
- **Attributes**: 插件元数据标注

#### 1.3 Event 层

**职责**：事件总线和分发

- **EventBus**: 中心事件总线
- **Events**: 所有事件类型定义
- **Dispatcher**: 异步事件分发器

#### 1.4 Command 层

**职责**：命令注册和执行

- **CommandManager**: 命令管理器
- **CommandParser**: 命令解析
- **Permission**: 权限管理
- **Built-in**: 内置命令

#### 1.5 Config 层

**职责**：配置文件管理

- **ConfigurationManager**: 配置管理器
- **Providers**: JSON/YAML 配置提供者
- **Validation**: 配置验证

#### 1.6 Logging 层

**职责**：日志记录

- **Loggers**: 各类日志记录器
- **Formatting**: 日志格式化

---

### 2. NetherGate.API

**职责**：定义插件开发的公共接口

这个项目只包含接口定义和数据模型，插件开发者只需引用这个项目，不需要引用整个 Core。

---

### 3. NetherGate.Host

**职责**：主程序入口

包含应用启动、依赖注入配置、控制台界面等。

---

### 4. 示例插件项目（独立仓库）

**仓库**: `NetherGate-Samples` (独立项目)

示例插件代码单独管理在一个独立的仓库/文件夹中，不包含在 NetherGate 主项目内。

**项目结构**：
```
NetherGate-Samples/
├── README.md                        # 示例说明总览
├── HelloWorld/                      # 最简单的插件示例
│   ├── HelloWorldPlugin.csproj
│   ├── HelloWorldPlugin.cs
│   ├── plugin.json
│   └── README.md
├── PlayerWelcome/                   # 玩家欢迎插件
│   ├── PlayerWelcomePlugin.csproj
│   ├── PlayerWelcomePlugin.cs
│   ├── WelcomeConfig.cs
│   ├── plugin.json
│   └── README.md
└── AdminTools/                      # 管理工具插件
    ├── AdminToolsPlugin.csproj
    ├── AdminToolsPlugin.cs
    ├── Commands/
    │   ├── BanCommand.cs
    │   └── KickCommand.cs
    ├── plugin.json
    └── README.md
```

**特点**：
- 独立管理，不会混入主项目
- 包含完整的 `.csproj` 项目文件和源代码
- 每个示例都有详细的 `README.md` 说明
- 可以单独克隆和构建

**使用方式**：
1. 开发者可以单独克隆示例仓库学习
2. 可以复制示例代码作为新插件的起点
3. 编译后的 DLL 可以放到 NetherGate 的 `plugins/` 目录使用

**包含的示例**：
- **HelloWorld**: 最简单的插件示例，演示基本结构和生命周期
- **PlayerWelcome**: 玩家欢迎插件，演示事件监听和配置管理
- **AdminTools**: 管理工具插件，演示命令注册和服务器 API 调用

**链接**：
- 主项目: `https://github.com/YourName/NetherGate`
- 示例项目: `https://github.com/YourName/NetherGate-Samples`
- 示例项目详细说明: [SAMPLES_PROJECT.md](SAMPLES_PROJECT.md)

---

## 🔧 关键文件说明

### 配置文件

#### `config/nethergate.json` - NetherGate 主配置

**位置**: `config/nethergate.json`（首次启动从 `config.example.json` 复制）

```json
{
    "server_process": {
        "enabled": true,
        "java": {
            "path": "java",
            "version_check": true
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
                "-XX:+ParallelRefProcEnabled",
                "-XX:MaxGCPauseMillis=200",
                "-Dfile.encoding=UTF-8"
            ],
            "server": ["--nogui"]
        },
        "monitoring": {
            "startup_timeout": 300,
            "startup_detection": {
                "enabled": true,
                "keywords": ["Done (", "For help, type \"help\""]
            }
        },
        "auto_restart": {
            "enabled": true,
            "max_retries": 3,
            "retry_delay": 5000,
            "reset_timer": 600000
        }
    },
    "server_connection": {
        "host": "localhost",
        "port": 40745,
        "secret": "your-40-character-secret-token-here",
        "use_tls": false,
        "tls_certificate": null,
        "tls_password": null,
        "reconnect_interval": 5000,
        "heartbeat_timeout": 30000,
        "auto_connect": true,
        "connect_delay": 3000,
        "wait_for_server_ready": true
    },
    "plugins": {
        "directory": "plugins",
        "auto_load": true,
        "hot_reload": true,
        "enabled_plugins": ["*"],
        "disabled_plugins": [],
        "load_after_server_ready": true
    },
    "logging": {
        "level": "Info",
        "console": {
            "enabled": true,
            "colored": true,
            "show_server_output": true
        },
        "file": {
            "enabled": true,
            "path": "logs/latest.log",
            "max_size": 10485760,
            "max_files": 10,
            "rolling": true
        }
    }
}
```

#### `plugin.json` - 插件描述文件

```json
{
    "id": "example-plugin",
    "name": "Example Plugin",
    "version": "1.0.0",
    "author": "Your Name",
    "description": "An example plugin for NetherGate",
    "website": "https://github.com/user/example-plugin",
    "repository": "https://github.com/user/example-plugin",
    "license": "MIT",
    
    "main": "ExamplePlugin.dll",
    "entry_class": "ExamplePlugin.ExamplePlugin",
    
    "dependencies": {
        "nethergate_plugins": [
            {
                "id": "core-library",
                "version": ">=1.0.0 <2.0.0",
                "required": true
            }
        ],
        "assemblies": [
            {
                "name": "Newtonsoft.Json",
                "version": "13.0.1",
                "location": "local",
                "required": true
            }
        ]
    },
    
    "requirements": {
        "min_nethergate_version": "1.0.0",
        "max_nethergate_version": null,
        "min_minecraft_version": "1.21.9",
        "dotnet_version": "9.0"
    },
    
    "permissions": [
        "nethergate.player.kick",
        "nethergate.player.ban",
        "nethergate.server.stop"
    ],
    
    "commands": [
        {
            "name": "example",
            "description": "Example command",
            "aliases": ["ex", "e"],
            "permission": "example.command"
        }
    ],
    
    "metadata": {
        "tags": ["admin", "utility"],
        "keywords": ["player", "management"]
    }
}
```

**详细的依赖管理说明**: 查看 [PLUGIN_DEPENDENCIES.md](PLUGIN_DEPENDENCIES.md)

---

## 🏗️ 项目依赖关系

```
NetherGate.Host
    ├── NetherGate.Core
    │   └── NetherGate.API
    └── Microsoft.Extensions.*

Plugin Projects
    └── NetherGate.API
```

插件只需引用 `NetherGate.API`，保持轻量级。

---

## 📝 命名约定

### 命名空间

- 核心: `NetherGate.Core.*`
- API: `NetherGate.API.*`
- 主程序: `NetherGate.Host`
- 插件: `<PluginName>.*`

### 文件命名

- 接口: `I*.cs`
- 抽象类: `*Base.cs`
- DTO: `*Dto.cs`
- 事件: `*Event.cs`
- 异常: `*Exception.cs`
- 配置: `*Config.cs`

### 项目命名

- 核心库: `NetherGate.Core.csproj`
- API 库: `NetherGate.API.csproj`
- 主程序: `NetherGate.Host.csproj`
- 测试: `NetherGate.*.Tests.csproj`
- 插件: `<PluginName>.csproj`

---

## 🔄 构建和部署

### 开发构建

```bash
dotnet build
```

### 发布构建

```bash
# 标准发布
dotnet publish -c Release -o publish/

# AOT 发布（更高性能）
dotnet publish -c Release -r win-x64 --self-contained -o publish/
```

### 插件打包

```bash
cd samples/HelloWorld
dotnet build -c Release
mkdir -p ../../plugins/hello-world
cp bin/Release/net9.0/HelloWorldPlugin.dll ../../plugins/hello-world/
cp plugin.json ../../plugins/hello-world/
```

---

## 📊 开发工作流

```
1. 克隆项目
   ↓
2. 安装 .NET 9.0 SDK
   ↓
3. 恢复依赖: dotnet restore
   ↓
4. 开发功能
   ↓
5. 运行测试: dotnet test
   ↓
6. 构建: dotnet build
   ↓
7. 运行: dotnet run --project src/NetherGate.Host
   ↓
8. 提交代码
```

---

## 🎯 下一步

参考 [DEVELOPMENT.md](../DEVELOPMENT.md) 查看详细的开发计划和路线图。

