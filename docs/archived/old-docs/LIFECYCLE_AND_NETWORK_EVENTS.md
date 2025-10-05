# NetherGate 服务器生命周期和网络层事件文档

本文档详细说明 NetherGate 提供的服务器生命周期事件和 Java 版网络层事件的使用方法。

---

## 📋 **目录**

1. [服务器生命周期事件](#服务器生命周期事件)
2. [Java 版网络层事件](#java-版网络层事件)
3. [事件使用示例](#事件使用示例)
4. [网络监听器配置](#网络监听器配置)
5. [最佳实践](#最佳实践)

---

## 1️⃣ **服务器生命周期事件**

### **事件时间线**

```
服务器启动流程:
  ┌─────────────────────────────────────────────────────────┐
  │ 1. ServerProcessStartedEvent                            │  ← 进程已启动
  │    ├─ ProcessId: 进程 ID                                │
  │    └─ 时机: 进程启动后立即触发                          │
  ├─────────────────────────────────────────────────────────┤
  │ 2. ServerReadyEvent                                     │  ← 服务器就绪
  │    ├─ StartupTimeSeconds: 启动耗时                      │
  │    └─ 时机: 日志输出 "Done (X.XXXs)!"                   │
  └─────────────────────────────────────────────────────────┘

服务器关闭流程:
  ┌─────────────────────────────────────────────────────────┐
  │ 1. ServerShuttingDownEvent                              │  ← 准备关闭
  │    └─ 时机: 日志输出 "Stopping server"                  │
  ├─────────────────────────────────────────────────────────┤
  │ 2. ServerProcessStoppedEvent (退出码 0)                 │  ← 正常退出
  │    ├─ ExitCode: 退出码                                  │
  │    └─ 时机: 进程正常退出                                │
  │                        或                                │
  │ 2. ServerProcessCrashedEvent (退出码 ≠ 0)              │  ← 异常退出
  │    ├─ ExitCode: 退出码                                  │
  │    ├─ ErrorMessage: 错误信息                            │
  │    └─ 时机: 进程崩溃/异常退出                           │
  └─────────────────────────────────────────────────────────┘
```

### **事件详情**

#### **1.1 ServerProcessStartedEvent**

**命名空间:** `NetherGate.API.Events`  
**基类:** `ServerEvent`

**说明:** 服务器进程已启动，但服务器尚未完全就绪（仍在加载世界、初始化插件等）。

**属性:**
```csharp
public record ServerProcessStartedEvent : ServerEvent
{
    public int ProcessId { get; init; }  // 进程 ID（External 模式为 0）
}
```

**触发时机:**
- `ServerProcessManager.StartAsync()` 调用 `Process.Start()` 后
- External 模式下，启动时也会触发（ProcessId = 0）

---

#### **1.2 ServerReadyEvent**

**命名空间:** `NetherGate.API.Events`  
**基类:** `ServerEvent`

**说明:** 服务器已完全启动并准备接受玩家连接。

**属性:**
```csharp
public record ServerReadyEvent : ServerEvent
{
    public double StartupTimeSeconds { get; init; }  // 启动耗时（秒）
}
```

**触发时机:**
- 日志解析到 `Done (X.XXXs)! For help, type "help"`
- 通常在 `ServerProcessStartedEvent` 后 30-120 秒（取决于服务器配置和世界大小）

**用途:**
- 延迟加载插件（等服务器完全就绪后再加载）
- 发送启动通知
- 执行启动后的初始化任务

---

#### **1.3 ServerShuttingDownEvent**

**命名空间:** `NetherGate.API.Events`  
**基类:** `ServerEvent`

**说明:** 服务器收到停止命令，正在执行关闭流程（保存世界、踢出玩家等）。

**属性:**
```csharp
public record ServerShuttingDownEvent : ServerEvent
{
    // 无额外属性
}
```

**触发时机:**
- 日志解析到 `Stopping server`
- 通常在执行 `stop` 命令后立即触发

**注意事项:**
- **SMP 协议也有同名事件** `SmpEvents.ServerStoppingEvent`（基类不同）
- 日志解析的 `ServerShuttingDownEvent` 触发更早
- 如果需要在关闭前执行清理，建议订阅 `ServerShuttingDownEvent`

---

#### **1.4 ServerProcessStoppedEvent**

**命名空间:** `NetherGate.API.Events`  
**基类:** `ServerEvent`

**说明:** 服务器进程已正常退出（退出码 0）。

**属性:**
```csharp
public record ServerProcessStoppedEvent : ServerEvent
{
    public int ExitCode { get; init; }  // 退出码（0 表示正常）
}
```

**触发时机:**
- 进程退出且 `ExitCode == 0`

---

#### **1.5 ServerProcessCrashedEvent**

**命名空间:** `NetherGate.API.Events`  
**基类:** `ServerEvent`

**说明:** 服务器进程异常退出或崩溃（退出码 ≠ 0）。

**属性:**
```csharp
public record ServerProcessCrashedEvent : ServerEvent
{
    public int ExitCode { get; init; }        // 退出码（非 0）
    public string? ErrorMessage { get; init; }  // 错误信息（可选）
}
```

**触发时机:**
- 进程退出且 `ExitCode != 0`

**用途:**
- 崩溃分析
- 自动重启（配置 `AutoRestart.Enabled = true`）
- 发送崩溃通知

---

## 2️⃣ **Java 版网络层事件**

### **事件架构**

```
网络事件监听器架构:
  ┌──────────────────────────────────────────────────────┐
  │ INetworkEventListener (接口)                         │
  │  ├─ Mode: NetworkListenerMode                        │
  │  │   ├─ Disabled (禁用)                              │
  │  │   ├─ LogBased (日志解析，默认)                    │
  │  │   ├─ PluginBridge (Paper/Spigot 插件)            │
  │  │   ├─ ModBridge (Fabric/Forge Mod)                │
  │  │   └─ ProxyBridge (Velocity/BungeeCord)           │
  │  └─ RegisterEventHandler(INetworkEventHandler)      │
  └──────────────────────────────────────────────────────┘
                           │
                           ▼
  ┌──────────────────────────────────────────────────────┐
  │ NetworkEventListener (实现)                          │
  │  ├─ 根据不同模式采用不同监听策略                      │
  │  ├─ 发布网络层事件到 IEventBus                        │
  │  └─ 调用注册的 INetworkEventHandler                  │
  └──────────────────────────────────────────────────────┘
                           │
                           ▼
  ┌──────────────────────────────────────────────────────┐
  │ 网络层事件 (NetworkEvents.cs)                        │
  │  ├─ PlayerConnectionAttemptEvent                     │
  │  ├─ PlayerLoginStartEvent                            │
  │  ├─ PlayerLoginSuccessEvent                          │
  │  ├─ PlayerLoginFailedEvent                           │
  │  ├─ PlayerDisconnectedEvent                          │
  │  ├─ PacketReceivedEvent (低级，仅 ModBridge)         │
  │  ├─ PacketSentEvent (低级，仅 ModBridge)             │
  │  ├─ NetworkExceptionEvent                            │
  │  ├─ MaliciousPacketDetectedEvent                     │
  │  └─ NetworkTrafficEvent                              │
  └──────────────────────────────────────────────────────┘
```

### **网络监听模式**

#### **2.1 LogBased（日志解析模式）**

**特点:**
- ✅ 无需额外安装任何插件/Mod
- ✅ 适用于原版/任何服务端
- ⚠️ 仅提供基础事件（玩家加入/离开）
- ⚠️ 无法监听握手、数据包等底层事件

**适用场景:**
- 快速开始，无需复杂配置
- 只需要基础的玩家连接/断开事件

---

#### **2.2 PluginBridge（插件桥接模式）**

**特点:**
- ✅ 提供完整的网络事件
- ✅ 性能影响小
- ⚠️ 需要安装配套的 Paper/Spigot 插件
- ⚠️ 仅支持 Paper/Spigot 服务器

**安装步骤:**
1. 下载 `NetherGate-Bridge.jar`
   ```
   https://github.com/your-repo/NetherGate-Bridge/releases
   ```
2. 将插件放入服务器的 `plugins/` 目录
3. 配置插件的 WebSocket 连接地址（指向 NetherGate）
4. 重启服务器

**配置示例:**
```yaml
# plugins/NetherGate-Bridge/config.yml
nethergate:
  host: localhost
  port: 8080  # NetherGate 的 WebSocket 端口
  secret: your-secret-token
  events:
    - player_connection
    - player_login
    - player_disconnect
    - network_exception
```

---

#### **2.3 ModBridge（Mod 桥接模式）**

**特点:**
- ✅ 提供最底层的数据包监控
- ✅ 支持所有网络事件
- ⚠️ 需要安装配套的 Fabric/Forge Mod
- ⚠️ 性能影响较大（数据包级别）

**适用场景:**
- 需要深度网络分析
- 反作弊/调试/监控

**安装步骤:**
1. 下载 `NetherGate-Mod.jar`（Fabric 或 Forge 版本）
2. 将 Mod 放入服务器的 `mods/` 目录
3. 配置 Mod 的连接设置
4. 重启服务器

---

#### **2.4 ProxyBridge（代理桥接模式）**

**特点:**
- ✅ 适用于群组服务器
- ✅ 可以监控所有子服务器的网络事件
- ⚠️ 需要安装配套的 Velocity/BungeeCord 插件

**安装步骤:**
1. 在代理服务器上安装 `NetherGate-Proxy.jar`
2. 配置连接到 NetherGate 的设置
3. 重启代理服务器

---

### **网络层事件清单**

| 事件名称 | 说明 | 支持模式 |
|---------|------|---------|
| `PlayerConnectionAttemptEvent` | 玩家连接握手 | PluginBridge / ModBridge |
| `PlayerLoginStartEvent` | 玩家开始登录 | PluginBridge / ModBridge |
| `PlayerLoginSuccessEvent` | 玩家登录成功 | LogBased / PluginBridge / ModBridge |
| `PlayerLoginFailedEvent` | 玩家登录失败 | PluginBridge / ModBridge |
| `PlayerDisconnectedEvent` | 玩家断开连接 | LogBased / PluginBridge / ModBridge |
| `PacketReceivedEvent` | 接收数据包（低级） | ModBridge |
| `PacketSentEvent` | 发送数据包（低级） | ModBridge |
| `ServerStatusQueryEvent` | 服务器列表查询 | PluginBridge / ModBridge |
| `ServerPingEvent` | Ping 测试 | PluginBridge / ModBridge |
| `NetworkExceptionEvent` | 网络异常 | PluginBridge / ModBridge |
| `MaliciousPacketDetectedEvent` | 恶意包检测 | ModBridge |
| `NetworkTrafficEvent` | 网络流量统计 | PluginBridge / ModBridge |

---

## 3️⃣ **事件使用示例**

### **示例 1: 监听服务器生命周期**

```csharp
using NetherGate.API.Events;
using NetherGate.API.Plugins;

namespace MyPlugin;

public class MyPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;

        // 订阅服务器生命周期事件
        _context.EventBus.Subscribe<ServerProcessStartedEvent>(OnServerProcessStarted);
        _context.EventBus.Subscribe<ServerReadyEvent>(OnServerReady);
        _context.EventBus.Subscribe<ServerShuttingDownEvent>(OnServerShuttingDown);
        _context.EventBus.Subscribe<ServerProcessStoppedEvent>(OnServerStopped);
        _context.EventBus.Subscribe<ServerProcessCrashedEvent>(OnServerCrashed);
    }

    private async void OnServerProcessStarted(ServerProcessStartedEvent e)
    {
        _context.Logger.Info($"服务器进程已启动 (PID: {e.ProcessId})");
        _context.Logger.Info("正在加载世界和插件...");
    }

    private async void OnServerReady(ServerReadyEvent e)
    {
        _context.Logger.Info($"服务器已完全启动! (耗时: {e.StartupTimeSeconds:F3} 秒)");
        
        // 服务器就绪后执行初始化任务
        await InitializePluginAsync();
        
        // 发送欢迎消息
        if (_context.RconClient != null)
        {
            await _context.RconClient.ExecuteCommandAsync(
                "say §a服务器已完全启动，欢迎加入!");
        }
    }

    private async void OnServerShuttingDown(ServerShuttingDownEvent e)
    {
        _context.Logger.Info("服务器正在关闭，执行清理任务...");
        
        // 保存插件数据
        await SavePluginDataAsync();
        
        // 通知玩家
        if (_context.RconClient != null)
        {
            await _context.RconClient.ExecuteCommandAsync(
                "say §c服务器即将关闭，请尽快下线!");
        }
    }

    private async void OnServerStopped(ServerProcessStoppedEvent e)
    {
        _context.Logger.Info($"服务器已正常停止 (退出码: {e.ExitCode})");
    }

    private async void OnServerCrashed(ServerProcessCrashedEvent e)
    {
        _context.Logger.Error($"服务器崩溃! (退出码: {e.ExitCode})");
        
        if (e.ErrorMessage != null)
        {
            _context.Logger.Error($"错误信息: {e.ErrorMessage}");
        }
        
        // 发送崩溃通知（例如通过 Discord Webhook）
        await SendCrashNotificationAsync(e);
    }

    private async Task InitializePluginAsync()
    {
        _context.Logger.Info("插件初始化中...");
        // 初始化逻辑
    }

    private async Task SavePluginDataAsync()
    {
        _context.Logger.Info("保存插件数据...");
        // 保存逻辑
    }

    private async Task SendCrashNotificationAsync(ServerProcessCrashedEvent e)
    {
        // 发送通知逻辑（Discord/邮件/Webhook 等）
    }

    public void OnDisable()
    {
        _context.Logger.Info("插件已卸载");
    }
}
```

---

### **示例 2: 监听网络层事件（LogBased 模式）**

```csharp
using NetherGate.API.Events;
using NetherGate.API.Plugins;

namespace MyPlugin;

public class NetworkMonitorPlugin : IPlugin
{
    private IPluginContext _context;
    private Dictionary<string, DateTime> _playerConnectTimes = new();

    public void OnEnable(IPluginContext context)
    {
        _context = context;

        // LogBased 模式自动提供这两个事件
        _context.EventBus.Subscribe<PlayerLoginSuccessEvent>(OnPlayerLogin);
        _context.EventBus.Subscribe<PlayerDisconnectedEvent>(OnPlayerDisconnect);
    }

    private async void OnPlayerLogin(PlayerLoginSuccessEvent e)
    {
        _playerConnectTimes[e.PlayerName] = DateTime.UtcNow;
        
        _context.Logger.Info($"玩家 {e.PlayerName} 从 {e.IpAddress} 登录");
        
        // 检查是否为首次加入
        if (await IsFirstTimeJoin(e.PlayerName))
        {
            _context.Logger.Info($"欢迎新玩家: {e.PlayerName}");
            
            if (_context.RconClient != null)
            {
                await _context.RconClient.ExecuteCommandAsync(
                    $"say §e欢迎新玩家 §b{e.PlayerName} §e加入服务器!");
            }
        }
    }

    private async void OnPlayerDisconnect(PlayerDisconnectData e)
    {
        if (_playerConnectTimes.TryGetValue(e.PlayerName, out var connectTime))
        {
            var duration = DateTime.UtcNow - connectTime;
            _context.Logger.Info($"玩家 {e.PlayerName} 断开连接 (在线时长: {duration.TotalMinutes:F1} 分钟)");
            _playerConnectTimes.Remove(e.PlayerName);
        }
    }

    private async Task<bool> IsFirstTimeJoin(string playerName)
    {
        // 检查玩家数据是否存在
        return !_context.ServerFileAccess.FileExists($"world/playerdata/{playerName}.dat");
    }

    public void OnDisable() { }
}
```

---

### **示例 3: 使用 INetworkEventHandler（高级）**

```csharp
using NetherGate.API.Network;
using NetherGate.API.Logging;

namespace MyPlugin;

public class CustomNetworkHandler : INetworkEventHandler
{
    private readonly ILogger _logger;
    private readonly HashSet<string> _bannedIPs = new();

    public CustomNetworkHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task OnPlayerConnectionAttemptAsync(PlayerConnectionData data)
    {
        _logger.Debug($"连接尝试: {data.IpAddress}:{data.Port} (协议: {data.ProtocolVersion})");
        
        // 检查 IP 黑名单
        if (_bannedIPs.Contains(data.IpAddress))
        {
            _logger.Warning($"阻止黑名单 IP 连接: {data.IpAddress}");
            // TODO: 主动断开连接（需要 Paper 插件支持）
        }
    }

    public async Task OnPlayerLoginStartAsync(PlayerLoginData data)
    {
        _logger.Info($"玩家 {data.PlayerName} 开始登录 (IP: {data.IpAddress})");
    }

    public async Task OnPlayerLoginSuccessAsync(PlayerLoginData data)
    {
        _logger.Info($"玩家 {data.PlayerName} 登录成功");
    }

    public async Task OnPlayerLoginFailedAsync(PlayerLoginFailureData data)
    {
        _logger.Warning($"玩家 {data.PlayerName} 登录失败: {data.Reason}");
    }

    public async Task OnPlayerDisconnectedAsync(PlayerDisconnectData data)
    {
        _logger.Info($"玩家 {data.PlayerName} 断开连接: {data.Reason}");
    }

    public async Task OnPacketReceivedAsync(PacketData data)
    {
        _logger.Trace($"收到数据包: {data.PacketType} ({data.DataLength} bytes)");
    }

    public async Task OnPacketSentAsync(PacketData data)
    {
        _logger.Trace($"发送数据包: {data.PacketType} ({data.DataLength} bytes)");
    }

    public async Task OnNetworkExceptionAsync(NetworkExceptionData data)
    {
        _logger.Error($"网络异常 ({data.PlayerName}): {data.Message}");
    }
}
```

---

## 4️⃣ **网络监听器配置**

### **在 Program.cs 中初始化**

```csharp
// src/NetherGate.Host/Program.cs

private static INetworkEventListener? _networkListener;

private static void InitializeNetworkListener()
{
    _logger.Info("[6/8] 初始化网络监听器");
    
    // 从配置读取监听模式
    var mode = _config!.Network?.ListenerMode ?? NetworkListenerMode.LogBased;
    
    _networkListener = new NetworkEventListener(
        _loggerFactory!.CreateLogger("Network"),
        _eventBus!,
        mode
    );
    
    // 注册自定义处理器（可选）
    // _networkListener.RegisterEventHandler(new CustomNetworkHandler(_logger));
    
    await _networkListener.StartAsync();
}
```

### **配置文件示例**

```yaml
# nethergate-config.yaml

network:
  # 网络监听模式:
  # - disabled: 禁用
  # - log_based: 日志解析（默认，无需额外配置）
  # - plugin_bridge: Paper/Spigot 插件桥接
  # - mod_bridge: Fabric/Forge Mod 桥接
  # - proxy_bridge: Velocity/BungeeCord 代理桥接
  listener_mode: log_based
  
  # 启用网络流量统计
  enable_traffic_stats: true
  
  # 统计报告间隔（秒）
  stats_interval: 300
```

---

## 5️⃣ **最佳实践**

### **✅ 推荐做法**

1. **优先使用生命周期事件进行初始化**
   ```csharp
   // 在 ServerReadyEvent 中初始化，而不是 OnEnable
   _context.EventBus.Subscribe<ServerReadyEvent>(async e => {
       await InitializeAsync();
   });
   ```

2. **区分不同来源的事件**
   ```csharp
   // SMP 事件（更可靠、更早）
   _context.EventBus.Subscribe<SmpEvents.PlayerJoinedEvent>(OnPlayerJoinedSmp);
   
   // 日志解析事件（备用）
   _context.EventBus.Subscribe<ServerEvents.PlayerJoinedServerEvent>(OnPlayerJoinedLog);
   ```

3. **使用网络统计进行监控**
   ```csharp
   var stats = _networkListener.GetStatistics();
   _logger.Info($"总连接数: {stats.TotalConnections}, 失败: {stats.FailedConnections}");
   ```

### **⚠️ 注意事项**

1. **不要在事件处理器中执行耗时操作**
   ```csharp
   // ❌ 错误示例
   private async void OnServerReady(ServerReadyEvent e)
   {
       Thread.Sleep(10000);  // 阻塞事件总线
   }
   
   // ✅ 正确示例
   private async void OnServerReady(ServerReadyEvent e)
   {
       _ = Task.Run(async () => {
           await InitializeHeavyTaskAsync();
       });
   }
   ```

2. **External 模式下 ProcessId 为 0**
   ```csharp
   private async void OnServerProcessStarted(ServerProcessStartedEvent e)
   {
       if (e.ProcessId == 0)
       {
           _logger.Info("External 模式，无进程 ID");
       }
       else
       {
           _logger.Info($"进程 ID: {e.ProcessId}");
       }
   }
   ```

3. **网络层事件依赖于监听模式**
   - `LogBased` 仅提供 `PlayerLoginSuccessEvent` 和 `PlayerDisconnectedEvent`
   - 其他事件需要 `PluginBridge` 或 `ModBridge` 模式

---

## 📚 **相关文档**

- [事件系统完整覆盖](EVENT_SYSTEM_COVERAGE.md)
- [遗漏功能分析](MISSING_FEATURES_ANALYSIS.md)
- [SMP 接口文档](SMP_INTERFACE.md)
- [RCON 集成文档](RCON_INTEGRATION.md)
- [插件开发指南](PLUGIN_PROJECT_STRUCTURE.md)

---

**文档维护者:** NetherGate 开发团队  
**最后更新:** 2025-10-05
