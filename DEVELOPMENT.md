# NetherGate 开发文档

> **📖 文档定位说明**
> 
> 本文档是 **NetherGate 的架构设计总览**，面向核心开发者和贡献者。
> 
> - 📐 **本文档内容**：整体架构、核心模块设计、技术选型、开发路线
> - 📚 **详细文档**：具体实现细节请查看 [docs/](docs/) 目录下的专题文档
> - 🔌 **插件开发**：插件开发者请查看 [API_DESIGN.md](docs/API_DESIGN.md) 和 [SMP_INTERFACE.md](docs/SMP_INTERFACE.md)
> - 🚀 **快速开始**：用户请查看 [README.md](README.md)

---

## 📋 项目概述

**NetherGate** 是一个基于 C# .NET 9.0 开发的 Minecraft Java 版服务器插件加载器系统，类似于 MCDR（MCDReforged），但采用更现代化的架构和更强大的功能。

### 核心特性

- **基于服务端管理协议**：使用 Minecraft 1.21.9+ 引入的服务端管理协议（Server Management Protocol），比 RCON 提供更丰富的服务器控制能力
- **DLL 动态插件系统**：插件以 .NET DLL 形式编译和加载，支持热加载/热卸载
- **强类型插件 API**：利用 C# 的强类型特性，提供更安全、更易维护的插件开发体验
- **高性能异步架构**：基于 async/await 模式，充分利用现代 .NET 性能优势
- **丰富的事件系统**：监听服务器事件（玩家加入/离开、服务器状态变化等）并分发给插件
- **插件依赖管理**：支持插件间依赖关系和版本管理
- **配置管理系统**：统一的配置文件管理，支持 JSON/YAML 格式

---

## 🏗️ 系统架构设计

### 整体架构图

```
                           ┌─────────────────────┐
                           │   NetherGate Core   │
                           └──────────┬──────────┘
                                      │ 启动并管理
                                      ↓
┌─────────────────────────────────────────────────────────────┐
│                    Minecraft Java Server                    │
│                     (Managed Process)                        │
│                  (Server Management Protocol)               │
└──────────────────────────┬──────────────────────────────────┘
                           │ WebSocket + JSON-RPC 2.0
                           │ (TLS Optional)
                           │
                           │ stdout/stderr (监控输出)
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                      NetherGate Core                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │           Server Process Manager                      │  │
│  │  - Launch/Stop  - Monitor Output  - Auto Restart     │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌────────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │ Protocol Layer │  │ Event System │  │ Plugin Manager  │ │
│  │  - WebSocket   │  │  - Listener  │  │  - Load/Unload  │ │
│  │  - JSON-RPC    │  │  - Dispatch  │  │  - Dependency   │ │
│  │  - Auth/TLS    │  │  - Priority  │  │  - Lifecycle    │ │
│  └────────────────┘  └──────────────┘  └─────────────────┘ │
│  ┌────────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │ Command System │  │ Config Mgr   │  │ Logger System   │ │
│  │  - Parser      │  │  - Load/Save │  │  - Multi-Level  │ │
│  │  - Permission  │  │  - Validate  │  │  - File/Console │ │
│  └────────────────┘  └──────────────┘  └─────────────────┘ │
└──────────────────────────┬──────────────────────────────────┘
                           │ Plugin API (Interface)
┌──────────────────────────┴──────────────────────────────────┐
│                      Plugin Instances                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ Plugin A │  │ Plugin B │  │ Plugin C │  │ Plugin D │    │
│  │  (.dll)  │  │  (.dll)  │  │  (.dll)  │  │  (.dll)  │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 核心模块设计

### 0. 服务器进程管理 (Server Process Manager)

**详细文档**: [SERVER_PROCESS.md](docs/SERVER_PROCESS.md)

#### 0.1 职责

- 启动和停止 Minecraft 服务器进程
- 监听并显示服务器标准输出/错误
- 检测服务器启动完成状态
- 处理服务器崩溃和自动重启
- 灵活配置启动参数

#### 0.2 核心类

```csharp
public class ServerProcessManager
{
    public ServerProcessState State { get; }
    public bool IsRunning { get; }
    
    - Task<bool> StartAsync()
    - Task StopAsync(bool force = false, int timeout = 30000)
    - Task RestartAsync()
    - Task SendCommandAsync(string command)
    - void Kill()
    
    // 事件
    - event EventHandler<ServerOutputEventArgs> OutputReceived
    - event EventHandler<ServerStartedEventArgs> ServerStarted
    - event EventHandler<ServerStoppedEventArgs> ServerStopped
    - event EventHandler<ServerCrashedEventArgs> ServerCrashed
}
```

#### 0.3 启动流程

```
NetherGate 启动
    ↓
首次启动初始化（检查并创建目录结构）
  - 检查 config/ 目录
    ├─ 如果不存在，创建并从 config.example.json 生成 nethergate.json
  - 创建 plugins/ 目录（如果不存在）
  - 创建 logs/ 目录（如果不存在）
    ↓
读取配置（config/nethergate.json）
  - Java路径、内存、JVM参数等
    ↓
构建启动命令: java [jvm_prefix] -Xms -Xmx [jvm_middle] -jar server.jar [server_args]
    ↓
启动 MC 服务器进程
    ↓
监听 stdout/stderr ──→ 转发到 NetherGate 控制台
    ↓
检测启动完成（关键字: "Done ("）
    ↓
连接服务端管理协议（WebSocket）
    ↓
扫描 plugins/ 目录，加载插件
  - 为每个插件创建 config/<plugin-id>/ 目录
  - 加载插件配置
  - 初始化插件
```

**运行时目录结构**（首次启动自动生成）：
```
NetherGate/
├── NetherGate.exe              # 编译后的主程序
├── config/                      # 配置目录（自动创建）
│   ├── nethergate.json         # 主程序配置
│   ├── plugin-a/               # 插件A配置
│   │   └── config.json
│   └── plugin-b/               # 插件B配置
│       └── config.json
├── plugins/                     # 插件代码（自动创建）
│   ├── plugin-a/
│   │   ├── plugin.json
│   │   ├── PluginA.dll
│   │   └── Newtonsoft.Json.dll
│   └── plugin-b/
│       ├── plugin.json
│       └── PluginB.dll
└── logs/                        # 日志（自动创建）
    ├── latest.log                  # 当前日志
    └── 2025-10-04-1.log.gz        # 归档日志（自动压缩）
```

**注意**：
- ✅ `config/`、`plugins/`、`logs/` 目录运行时自动创建
- ❌ 这些目录不在源码仓库中（.gitignore 已排除）
- ✅ 源码仓库只包含源代码和 `config.example.json` 模板

#### 0.4 配置示例

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
            "max_retries": 3,
            "retry_delay": 5000
        }
    }
}
```

---

### 1. 协议层 (Protocol Layer)

**详细文档**: [SMP_INTERFACE.md](docs/SMP_INTERFACE.md) - 完整的服务端管理协议接口封装

#### 核心架构：三位一体

NetherGate 采用 **SMP + RCON + 日志监听** 三位一体架构：

```
┌─────────────────────────────────────────────────────────────┐
│                      Protocol Layer                          │
├──────────────────┬──────────────────┬────────────────────────┤
│  SMP Client      │  RCON Client     │  Log Listener          │
│  (WebSocket)     │  (TCP)           │  (Process Monitor)     │
│                  │                  │                        │
│  结构化管理      │  游戏命令执行    │  事件捕获              │
│  - 白名单/封禁   │  - /give         │  - 玩家聊天            │
│  - 游戏规则      │  - /tp           │  - 命令执行            │
│  - 实时通知      │  - /tellraw      │  - 服务器日志          │
└──────────────────┴──────────────────┴────────────────────────┘
                             ↓
                    ┌────────────────────┐
                    │   IServerApi       │  统一接口
                    │   (插件使用)       │
                    └────────────────────┘
```

**设计原则：**
- ✅ 模块化设计，每个协议客户端独立
- ✅ 统一接口封装，插件无需关心底层实现
- ✅ 可扩展架构，未来可轻松添加新的协议支持

**未来扩展性：**
- 🔮 当Mojang扩展SMP功能时，只需更新SmpClient
- 🔮 如果出现新的管理协议，可以添加新的Client实现
- 🔮 插件API保持稳定，插件无需修改

#### 1.1 SMP Client - WebSocket 连接管理

**职责**：
- 建立和维护与 Minecraft 服务器的 WebSocket 连接
- 处理连接认证（Bearer Token）
- TLS/SSL 支持
- 连接断线重连机制
- 心跳检测

**核心类**：
```csharp
public class ServerConnection
{
    - Task<bool> ConnectAsync(ServerConfig config)
    - Task DisconnectAsync()
    - Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request)
    - void SendNotification(JsonRpcNotification notification)
    - event EventHandler<ConnectionStateChangedEventArgs> StateChanged
}
```

#### 1.2 JSON-RPC 2.0 处理器

**职责**：
- 序列化/反序列化 JSON-RPC 消息
- 请求-响应匹配（ID 管理）
- 批处理请求支持
- 错误处理

**核心类**：
```csharp
public class JsonRpcHandler
{
    - Task<TResponse> InvokeMethodAsync<TResponse>(string method, object? params)
    - void RegisterNotificationHandler(string method, Action<JsonRpcNotification> handler)
    - Task<List<JsonRpcResponse>> InvokeBatchAsync(List<JsonRpcRequest> requests)
}
```

#### 1.3 服务端管理协议封装

**职责**：
- 封装所有服务端管理协议的方法调用
- 提供强类型的 API 接口

**支持的协议方法**（参考 Wiki）：

**白名单管理**：
- `allowlist` - 获取白名单
- `allowlist/set` - 设置白名单
- `allowlist/add` - 添加玩家到白名单
- `allowlist/remove` - 从白名单移除玩家
- `allowlist/clear` - 清空白名单

**封禁管理**：
- `bans`, `bans/set`, `bans/add`, `bans/remove`, `bans/clear` - 玩家封禁
- `ip_bans`, `ip_bans/set`, `ip_bans/add`, `ip_bans/remove`, `ip_bans/clear` - IP 封禁

**玩家管理**：
- `players` - 获取在线玩家列表
- `players/kick` - 踢出玩家

**管理员管理**：
- `operators`, `operators/set`, `operators/add`, `operators/remove`, `operators/clear` - OP 管理

**服务器管理**：
- `server/status` - 获取服务器状态
- `server/save` - 保存世界
- `server/stop` - 停止服务器
- `server/system_message` - 发送系统消息

**设置管理**：
- `serversettings/*` - 读取服务器设置
- `serversettings/*/set` - 修改服务器设置

**游戏规则管理**：
- `gamerules` - 获取游戏规则
- `gamerules/update` - 更新游戏规则

**核心类**：
```csharp
public class MinecraftServerApi
{
    // 白名单
    - Task<List<PlayerDto>> GetAllowlistAsync()
    - Task SetAllowlistAsync(List<PlayerDto> players)
    - Task AddToAllowlistAsync(PlayerDto player)
    - Task RemoveFromAllowlistAsync(PlayerDto player)
    - Task ClearAllowlistAsync()
    
    // 玩家
    - Task<List<PlayerDto>> GetPlayersAsync()
    - Task KickPlayerAsync(string playerName, string? reason)
    
    // 管理员
    - Task<List<OperatorDto>> GetOperatorsAsync()
    - Task AddOperatorAsync(OperatorDto operator)
    - Task RemoveOperatorAsync(PlayerDto player)
    
    // 服务器
    - Task<ServerState> GetServerStatusAsync()
    - Task SaveWorldAsync()
    - Task StopServerAsync()
    - Task SendSystemMessageAsync(string message)
    
    // 游戏规则
    - Task<Dictionary<string, TypedRule>> GetGameRulesAsync()
    - Task UpdateGameRuleAsync(string rule, object value)
    
    // 封禁
    - Task<List<UserBanDto>> GetBansAsync()
    - Task AddBanAsync(UserBanDto ban)
    - Task<List<IpBanDto>> GetIpBansAsync()
    - Task AddIpBanAsync(IpBanDto ipBan)
}
```

**DTO 数据结构**（参考协议文档）：
```csharp
public record PlayerDto(string Name, Guid Uuid);
public record UserBanDto(PlayerDto Player, string? Reason, DateTime? Expires, string? Source);
public record IpBanDto(string Ip, string? Reason, DateTime? Expires, string? Source);
public record OperatorDto(PlayerDto Player, int Level, bool BypassPlayerLimit);
public record ServerState(bool Started, VersionInfo Version);
public record VersionInfo(string Name, int Protocol);
public record TypedRule(string Type, object Value);
```

**详细文档**: [SMP_INTERFACE.md](docs/SMP_INTERFACE.md)

---

#### 1.4 RCON Client - 游戏命令执行

**核心定位：** 执行任意Minecraft游戏命令

**职责**：
- 建立和维护与服务器的RCON连接（TCP）
- 处理RCON协议认证
- 执行游戏命令并获取响应
- 支持批量命令执行

**核心类**：

```csharp
public class RconClient
{
    public bool IsConnected { get; }
    
    public async Task<bool> ConnectAsync(string host, int port, string password);
    public async Task DisconnectAsync();
    
    public async Task<RconResponse> ExecuteCommandAsync(string command);
    public async Task<List<RconResponse>> ExecuteCommandsAsync(params string[] commands);
}

public record RconResponse
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public string RawResponse { get; init; }
}
```

**使用场景**：
- 给予物品：`give <player> <item> <count>`
- 传送玩家：`tp <player> <x> <y> <z>`
- 施加效果：`effect give <player> <effect>`
- 发送消息：`tellraw <player> <json>`

**详细文档**: [RCON_INTEGRATION.md](docs/RCON_INTEGRATION.md)

---

#### 1.5 Log Listener - 服务器日志监听

**核心定位：** 捕获服务器输出，解析游戏事件

**职责**：
- 监听服务器进程的stdout/stderr输出
- 实时解析日志内容
- 识别事件类型
- 将日志事件转发给事件系统

**核心类**：

```csharp
public class LogListener
{
    public void Subscribe(Action<ServerLogEntry> handler);
    public void Unsubscribe(Action<ServerLogEntry> handler);
}

public record ServerLogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Thread { get; init; }
    public string Message { get; init; }
    public LogEntryType Type { get; init; }
}

public enum LogEntryType
{
    Unknown,
    PlayerChat,      // <Player> message
    PlayerCommand,   // Player issued server command: /cmd
    PlayerJoin,      // Player joined the game
    PlayerLeave,     // Player left the game
    ServerStarted,   // Done (5.123s)!
    Error            // [ERROR] ...
}
```

**使用场景**：
- 监听玩家聊天内容
- 捕获玩家执行的命令
- 检测服务器启动完成
- 监控错误日志

---

#### 1.6 协议层统一接口 (IServerApi)

**设计理念：** 将三种技术统一封装，插件无需关心底层实现

```csharp
public interface IServerApi
{
    // SMP 提供的结构化管理
    Task<List<PlayerDto>> GetPlayersAsync();
    Task AddToAllowlistAsync(PlayerDto player);
    
    // RCON 提供的游戏命令（内部封装）
    Task GiveItemAsync(string player, string item, int count);
    Task TeleportPlayerAsync(string player, int x, int y, int z);
    
    // 日志监听
    void SubscribeToServerLog(Action<ServerLogEntry> handler);
}
```

**智能路由**：当多个协议都支持某功能时，自动选择最优实现。

---

### 2. 事件系统 (Event System)

#### 2.1 事件类型

**服务器事件**：
- `ServerStartedEvent` - 服务器启动
- `ServerStoppedEvent` - 服务器停止
- `ServerStatusChangedEvent` - 服务器状态变化
- `ServerHeartbeatEvent` - 服务器心跳

**玩家事件**：
- `PlayerJoinedEvent` - 玩家加入
- `PlayerLeftEvent` - 玩家离开
- `PlayerKickedEvent` - 玩家被踢出

**管理事件**：
- `OperatorAddedEvent` - 添加管理员
- `OperatorRemovedEvent` - 移除管理员
- `AllowlistChangedEvent` - 白名单变化
- `BanAddedEvent` - 添加封禁
- `BanRemovedEvent` - 移除封禁

**游戏规则事件**：
- `GameRuleUpdatedEvent` - 游戏规则更新

#### 2.2 事件处理器

**核心类**：
```csharp
public class EventBus
{
    - void Subscribe<TEvent>(IPlugin plugin, EventPriority priority, EventHandler<TEvent> handler)
    - void Unsubscribe<TEvent>(IPlugin plugin)
    - Task DispatchAsync<TEvent>(TEvent eventData)
    - void SetEventCancellable<TEvent>(bool cancellable)
}

public enum EventPriority
{
    Lowest = 0,
    Low = 1,
    Normal = 2,
    High = 3,
    Highest = 4,
    Monitor = 5  // 仅监听，不修改
}
```

#### 2.3 通知监听器

**职责**：监听服务端推送的 JSON-RPC 通知，转换为事件

**支持的通知**（参考协议文档）：
- `server/status` - 服务器状态
- `players/joined` - 玩家加入
- `players/left` - 玩家离开
- `operators/added` - 添加 OP
- `operators/removed` - 移除 OP
- `allowlist/added` - 添加白名单
- `allowlist/removed` - 移除白名单
- `ip_bans/added` - 添加 IP 封禁
- `ip_bans/removed` - 移除 IP 封禁
- `bans/added` - 添加玩家封禁
- `bans/removed` - 移除玩家封禁
- `gamerules/updated` - 游戏规则更新

---

### 3. 插件管理器 (Plugin Manager)

#### 3.1 插件生命周期

```
[未加载] → Load() → [已加载] → Enable() → [已启用]
                         ↓                      ↓
                    [加载失败]           Disable() → [已禁用]
                                                ↓
                                          Unload() → [已卸载]
```

#### 3.2 插件元数据

**插件描述文件** (`plugin.json`)：
```json
{
    "id": "example-plugin",
    "name": "Example Plugin",
    "version": "1.0.0",
    "author": "Author Name",
    "description": "Plugin description",
    "main": "ExamplePlugin.dll",
    "dependencies": [
        {
            "id": "core-library",
            "version": ">=1.0.0"
        }
    ],
    "min_nethergate_version": "1.0.0",
    "repository": "https://github.com/user/plugin",
    "license": "MIT"
}
```

#### 3.3 插件基类和接口

```csharp
public interface IPlugin
{
    PluginMetadata Metadata { get; }
    PluginState State { get; }
    
    Task OnLoadAsync();
    Task OnEnableAsync();
    Task OnDisableAsync();
    Task OnUnloadAsync();
}

public abstract class PluginBase : IPlugin
{
    public PluginMetadata Metadata { get; }
    public ILogger Logger { get; }
    public IPluginConfig Config { get; }
    public MinecraftServerApi Server { get; }
    public EventBus Events { get; }
    
    public virtual Task OnLoadAsync() => Task.CompletedTask;
    public virtual Task OnEnableAsync() => Task.CompletedTask;
    public virtual Task OnDisableAsync() => Task.CompletedTask;
    public virtual Task OnUnloadAsync() => Task.CompletedTask;
}
```

#### 3.4 插件加载器

**核心类**：
```csharp
public class PluginLoader
{
    - Task<IPlugin> LoadPluginAsync(string pluginPath)
    - Task UnloadPluginAsync(IPlugin plugin)
    - Task ReloadPluginAsync(string pluginId)
    - Task<bool> ValidatePluginAsync(string pluginPath)
    - Task<List<IPlugin>> LoadAllPluginsAsync(string pluginsDirectory)
}
```

**加载流程**：
1. 扫描插件目录
2. 读取并验证 `plugin.json`
3. 检查依赖关系
4. 按依赖顺序加载 DLL
5. 实例化插件类
6. 调用 `OnLoadAsync()`
7. 调用 `OnEnableAsync()`

#### 3.5 依赖管理

**职责**：
- 解析插件间依赖关系
- 管理外部程序集依赖（NuGet 包等）
- 处理依赖版本冲突
- 使用 AssemblyLoadContext 实现依赖隔离

**核心类**：
```csharp
public class DependencyResolver
{
    // 插件间依赖
    - List<IPlugin> ResolveDependencies(List<IPlugin> plugins)
    - bool CheckCircularDependency(IPlugin plugin)
    - bool ValidateVersion(string version, string requirement)
    
    // 外部依赖
    - Task<bool> ValidateAssemblyDependenciesAsync(PluginMetadata metadata)
    - Assembly LoadDependencyAssembly(string assemblyName, string pluginDirectory)
}

public class PluginAssemblyLoadContext : AssemblyLoadContext
{
    // 为每个插件创建独立的加载上下文
    // 实现依赖隔离，避免版本冲突
}
```

**依赖管理详细设计**：查看 [插件依赖管理文档](docs/PLUGIN_DEPENDENCIES.md)

**依赖隔离架构**：
```
Plugin A (独立 AssemblyLoadContext)
  ├── PluginA.dll
  └── Newtonsoft.Json v13.0.1

Plugin B (独立 AssemblyLoadContext)
  ├── PluginB.dll
  └── Newtonsoft.Json v12.0.3  ← 不冲突
```

---

### 4. 命令系统 (Command System)

#### 4.1 命令注册与解析

虽然服务端管理协议不直接支持命令执行，但可以通过以下方式实现：
- 在 NetherGate 控制台中执行管理命令
- 通过 `server/system_message` 发送消息模拟命令提示

**核心类**：
```csharp
public class CommandManager
{
    - void RegisterCommand(IPlugin plugin, CommandDefinition command)
    - void UnregisterCommand(string commandName)
    - Task<CommandResult> ExecuteCommandAsync(string commandLine)
    - List<string> GetCommandSuggestions(string partial)
}

public record CommandDefinition(
    string Name,
    string Description,
    List<string> Aliases,
    Func<CommandContext, Task<CommandResult>> Handler,
    string? Permission = null
);
```

#### 4.2 内置命令

- `plugins` - 列出所有插件
- `plugin <id> enable/disable/reload` - 插件管理
- `status` - 显示服务器状态
- `players` - 列出在线玩家
- `stop` - 停止服务器
- `reload` - 重载 NetherGate
- `help` - 帮助信息

---

### 5. 配置管理 (Configuration Management)

#### 5.1 全局配置

**NetherGate 配置文件** (`config.json`)：
```json
{
    "server": {
        "host": "localhost",
        "port": 40745,
        "secret": "your-40-character-secret-token-here",
        "use_tls": true,
        "tls_certificate": "path/to/cert.pfx",
        "tls_password": "cert-password",
        "reconnect_interval": 5000,
        "heartbeat_timeout": 30000
    },
    "plugins": {
        "directory": "plugins",
        "auto_load": true,
        "hot_reload": true
    },
    "logging": {
        "level": "Info",
        "console": true,
        "file": true,
        "file_path": "logs/latest.log",
        "max_file_size": 10485760,
        "max_files": 10
    }
}
```

#### 5.2 插件配置

每个插件有独立的配置文件：
- 位置：`config/<plugin-id>/config.json` 或 `config.yaml`
- 自动加载和保存
- 支持热重载
- 配置和代码分离，便于管理

**目录结构**：
```
NetherGate/
├── config/                    # 配置目录（统一管理）
│   ├── nethergate.json       # 主程序配置
│   └── my-plugin/            # 插件配置
│       └── config.json
└── plugins/                   # 插件代码
    └── my-plugin/
        ├── plugin.json
        └── MyPlugin.dll
```

**核心接口**：
```csharp
public interface IPluginConfig
{
    T Get<T>(string key, T defaultValue = default);
    void Set<T>(string key, T value);
    Task SaveAsync();
    Task ReloadAsync();
}
```

**配置文件路径**：
- 插件加载时，NetherGate 自动为每个插件创建 `config/<plugin-id>/` 目录
- 插件通过 `Config` 属性访问配置，无需关心实际路径
- 配置文件独立于插件代码，更新插件不影响配置

---

### 6. 日志系统 (Logging System)

#### 6.1 日志级别

```csharp
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
```

#### 6.2 日志接口

```csharp
public interface ILogger
{
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Fatal(string message, Exception? exception = null);
}
```

#### 6.3 日志格式

```
[2025-10-04 12:00:00.123] [INFO] [PluginName] Message content
[2025-10-04 12:00:01.456] [ERROR] [Core] Error message
    Exception details...
```

---

## 🔌 插件开发指南

### 示例插件结构

```
MyPlugin/
├── MyPlugin.csproj
├── plugin.json
├── MyPlugin.cs
├── Commands/
│   └── MyCommand.cs
├── Events/
│   └── PlayerEventHandler.cs
└── Config/
    └── config.json
```

### 示例插件代码

```csharp
using NetherGate.Plugin;
using NetherGate.Events;

public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        Logger.Info("MyPlugin is starting...");
        
        // 注册事件监听
        Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoin);
        
        // 注册命令
        Commands.Register(new CommandDefinition(
            Name: "hello",
            Description: "Say hello to player",
            Aliases: new List<string> { "hi" },
            Handler: HandleHelloCommand
        ));
        
        Logger.Info("MyPlugin started successfully!");
    }
    
    private async Task OnPlayerJoin(object? sender, PlayerJoinedEvent e)
    {
        Logger.Info($"Player {e.Player.Name} joined the server!");
        await Server.SendSystemMessageAsync($"Welcome {e.Player.Name}!");
    }
    
    private async Task<CommandResult> HandleHelloCommand(CommandContext ctx)
    {
        var playerName = ctx.Args.FirstOrDefault();
        if (string.IsNullOrEmpty(playerName))
        {
            return CommandResult.Error("Please specify a player name");
        }
        
        await Server.SendSystemMessageAsync($"Hello {playerName}!");
        return CommandResult.Success($"Sent hello to {playerName}");
    }
    
    public override async Task OnDisableAsync()
    {
        Logger.Info("MyPlugin is stopping...");
        Events.UnsubscribeAll(this);
    }
}
```

---

## 🚀 开发路线图

### Phase 1: 基础框架 (Week 1-2) ✅
- [x] 项目结构搭建
- [x] WebSocket 连接管理
- [x] JSON-RPC 2.0 处理器
- [x] 基础日志系统（带颜色、日志归档）
- [x] 配置文件加载（JSON & YAML）

### Phase 2: 协议实现 (Week 3-4) ✅
- [x] 服务端管理协议 API 封装（完整 SMP）
- [x] 认证和 TLS 支持
- [x] 请求-响应处理
- [x] 通知监听和解析
- [x] 错误处理和重连机制
- [x] RCON 协议实现（额外功能）
- [x] 日志监听系统（额外功能）

### Phase 3: 插件系统 (Week 5-6) ✅
- [x] 插件接口定义（IPlugin, PluginBase）
- [x] 插件加载器实现（AssemblyLoadContext 隔离）
- [x] 插件生命周期管理（Load/Enable/Disable/Unload）
- [x] 依赖解析系统（三层优先级：lib/ > plugin/ > core）
- [x] 插件配置管理（独立 config/ 目录）
- [x] 插件元数据系统（plugin.json）

### Phase 4: 事件系统 (Week 7-8) ✅
- [x] 事件总线实现（IEventBus）
- [x] 事件优先级处理（6 个优先级）
- [x] 事件取消机制
- [x] SMP 事件封装（玩家、服务器、管理等）
- [x] RCON 事件支持
- [x] 日志事件监听
- [x] 三重监听策略（SMP > RCON > Log）

### Phase 5: 命令系统 (Week 9-10)
- [x] 命令注册管理
- [x] 命令解析器
- [x] 内置命令实现
- [x] 命令权限系统
- [x] 自动补全支持

### Phase 6: 高级特性 (Week 11-12) 🟢 80%
- [ ] 热重载支持（部分完成）
- [ ] 插件间通信 API
- [x] 性能监控（PerformanceMonitor）
- [x] RCON 性能优化
  - [x] Fire-and-Forget 执行模式
  - [x] 批量命令执行（顺序/并行）
  - [x] 命令执行统计和监控
- [x] spark 集成（Standalone + Plugin 版本）
- [x] TPS/MSPT 监控
- [x] CPU/内存监控（Windows）
- [x] 文件系统功能（IFileWatcher, IServerFiles, IBackupManager）
- [x] NBT 数据读取（IPlayerData, IWorldData）
- [x] NBT 数据写入（INbtDataWriter - 完整实现）
- [x] 数据组件系统（1.20.5+ Item Components）
- [x] 服务器查询（IServerQuery, IServerMonitor）
- [ ] Web 管理界面（可选）
- [ ] 插件市场（可选）

### Phase 7: 测试与优化 (Week 13-14) 🟡 30%
- [ ] 单元测试
- [ ] 集成测试
- [x] RCON 性能优化
- [x] DI 容器错误修复（ServiceCollectionExtensions）
- [x] 文档优化（删除争议性内容）
- [x] 最佳实践文档（RCON 性能优化示例）
- [ ] 示例插件项目（NetherGate-Samples 独立仓库）

### Phase 8: 发布准备 (Week 15-16) 🟡 20%
- [x] 打包和发布脚本（publish.bat/sh）
- [x] 用户文档（README.md + docs/）
- [x] 开发者文档（DEVELOPMENT.md + API 文档）
- [ ] CI/CD 配置（GitHub Actions）
- [ ] 1.0.0 版本发布
- [ ] 发布示例插件项目

---

## 🆚 与 MCDR 的对比优势

| 特性 | MCDR (Python) | NetherGate (C#) |
|------|---------------|-----------------|
| **协议支持** | RCON / 标准输入输出 | 服务端管理协议 (更强大) |
| **类型安全** | 动态类型 | 强类型 |
| **性能** | 解释执行 | JIT/AOT 编译，性能更高 |
| **插件格式** | .py 文件 | .dll 编译文件 |
| **异步支持** | asyncio | native async/await |
| **IDE 支持** | 一般 | Visual Studio / Rider (优秀) |
| **依赖管理** | pip | NuGet |
| **调试体验** | print 调试为主 | 完整调试器支持 |
| **控制能力** | 有限 | 丰富（白名单、OP、封禁等） |

---

## 📚 技术栈

- **.NET 9.0** - 最新的 .NET 平台
- **C# 13** - 现代 C# 语言特性
- **System.Net.WebSockets** - WebSocket 客户端
- **System.Text.Json** - JSON 序列化
- **Microsoft.Extensions.Logging** - 日志框架
- **Microsoft.Extensions.Configuration** - 配置管理
- **System.Reflection** - 插件动态加载
- **YamlDotNet** - YAML 配置支持（可选）

---

## 🔒 安全考虑

1. **认证令牌管理**：
   - 令牌存储加密
   - 支持令牌轮换
   - 避免日志中泄露令牌

2. **TLS 证书验证**：
   - 支持自签名证书
   - 证书有效期检查
   - 可配置的验证策略

3. **插件沙箱**：
   - 插件权限系统
   - API 访问控制
   - 资源使用限制

4. **输入验证**：
   - 所有外部输入验证
   - 防止注入攻击
   - 参数类型检查

---

## 📖 参考资料

- [Minecraft 服务端管理协议 Wiki](https://zh.minecraft.wiki/w/%E6%9C%8D%E5%8A%A1%E7%AB%AF%E7%AE%A1%E7%90%86%E5%8D%8F%E8%AE%AE)
- [JSON-RPC 2.0 规范](https://www.jsonrpc.org/specification)
- [MCDReforged 文档](https://mcdreforged.readthedocs.io/)
- [.NET 插件系统设计](https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
- [WebSocket 协议](https://tools.ietf.org/html/rfc6455)

---

## 🤝 贡献指南

欢迎参与 NetherGate 的开发！

详细的贡献指南请查看：[CONTRIBUTING.md](CONTRIBUTING.md)

主要内容包括：
- 如何报告 Bug
- 如何提出功能建议
- 代码规范和提交规范
- Pull Request 流程
- 测试指南

---

## 📄 许可证

（待定）

---

**项目状态**: 🚧 开发中

**当前版本**: 0.1.0-alpha

**目标版本**: 1.0.0

---

*本文档随项目进展持续更新*

