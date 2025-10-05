# SMP 协议（Server Management Protocol）

SMP（Server Management Protocol）是 **Minecraft 官方在 Java 版 1.21.9 (快照 25w35a) 中引入的原生服务器管理协议**，基于 WebSocket 和 JSON-RPC 2.0，提供实时事件推送和双向通信。

---

## 📋 **目录**

- [什么是 SMP](#什么是-smp)
- [启用 SMP 协议](#启用-smp-协议)
- [配置 NetherGate](#配置-nethergate)
- [使用 SMP API](#使用-smp-api)
- [事件系统](#事件系统)
- [最佳实践](#最佳实践)

---

## 🌐 **什么是 SMP**

SMP（Server Management Protocol）是 **Minecraft 官方的服务器管理协议**，自 **Java 版 1.21.9 (快照 25w35a)** 开始提供。

> **重要**: SMP 是 Minecraft 原生协议，**无需安装任何插件或模组**！

### **特性**

- ✅ **实时事件推送** - 服务器主动推送事件（玩家加入、离开、聊天等）
- ✅ **双向通信** - 支持请求-响应模式
- ✅ **丰富的 API** - 玩家管理、服务器状态查询、批量操作
- ✅ **WebSocket** - 持久连接，低延迟
- ✅ **JSON-RPC 2.0** - 标准化的 RPC 协议
- ✅ **认证机制** - 密钥认证，安全可靠

### **SMP vs RCON**

| 特性 | RCON | SMP |
|------|------|-----|
| 协议类型 | Minecraft 原生（传统） | Minecraft 原生（现代） |
| 连接方式 | TCP | WebSocket (持久连接) |
| 事件推送 | ❌ 不支持 | ✅ 支持 |
| 实时性 | 低（需轮询） | 高（主动推送） |
| 批量操作 | ❌ 不支持 | ✅ 支持 |
| 服务器状态 | 有限 | 详细（TPS、内存、玩家列表等） |
| 最低版本 | 1.9+ | 1.21.9+ (25w35a) |
| 安装要求 | ✅ 无需插件 | ✅ 无需插件 |

**推荐策略：**
- **RCON：** 用于基础命令执行
- **SMP：** 用于事件监听和高级功能
- **最佳：** 同时启用，互补使用

### **版本兼容性**

| Minecraft 版本 | SMP 支持 | 说明 |
|----------------|---------|------|
| 1.21.8 及更早 | ❌ 不支持 | SMP 尚未引入 |
| 1.21.9 快照 25w35a | ✅ 支持 | SMP 首次引入 |
| 1.21.9+ | ✅ 支持 | 完整支持 SMP |

> **注意**: SMP 是 **Minecraft 官方协议**，不是第三方扩展！  
> 官方文档：[Minecraft Wiki - 服务端管理协议](https://zh.minecraft.wiki/w/%E6%9C%8D%E5%8A%A1%E7%AB%AF%E7%AE%A1%E7%90%86%E5%8D%8F%E8%AE%AE)

---

## 📦 **启用 SMP 协议**

### **系统要求**

- ✅ **Minecraft Java 版 1.21.9+** (快照 25w35a 或更高版本)
- ✅ **原版服务器** 或 **兼容的服务端** (Paper, Spigot 等)
- ❌ **无需安装任何插件或模组**

### **配置步骤**

#### **1. 编辑 server.properties**

在 Minecraft 服务器的 `server.properties` 文件中添加或修改以下配置：

```properties
# 启用 SMP 协议
management-server-enabled=true

# 监听地址（localhost = 仅本地，0.0.0.0 = 所有接口）
management-server-host=localhost

# 监听端口（0 = 自动选择可用端口）
management-server-port=25580

# 认证令牌（40位字符，包含大小写字母和数字）
management-server-secret=n2pQcIG1OQ92jot2xG1M0aw0ZWnrh4F3Z3jw8qRP

# 是否启用 TLS（推荐生产环境启用）
management-server-tls-enabled=false

# TLS 证书配置（启用 TLS 时需要）
# management-server-tls-keystore=keystore.p12
# management-server-tls-keystore-password=your_password

# 心跳间隔（秒，0 = 禁用心跳）
status-heartbeat-interval=1
```

#### **2. 生成认证令牌**

认证令牌必须是 **40 位**，只包含大小写字母和数字（`^[a-zA-Z0-9]{40}$`）。

**生成方法：**

```bash
# 方式 1：使用 openssl
openssl rand -base64 30 | tr -d '/+=' | head -c 40

# 方式 2：使用 Python
python3 -c "import random, string; print(''.join(random.choices(string.ascii_letters + string.digits, k=40)))"

# 方式 3：在线生成
# 访问 https://www.random.org/strings/
```

**示例令牌：**
```
n2pQcIG1OQ92jot2xG1M0aw0ZWnrh4F3Z3jw8qRP
```

#### **3. 重启 Minecraft 服务器**

```bash
stop
# 等待服务器完全停止后重新启动
```

#### **4. 验证 SMP 已启动**

在服务器启动日志中查看：

```
[Server thread/INFO]: Starting SMP management server on localhost:25580
[Server thread/INFO]: Management server is ready to accept connections
```

如果配置的端口已被占用，服务器将**启动失败**并报错。

#### **5. 测试连接**

使用 `websocat` 或其他 WebSocket 客户端测试：

```bash
# 使用正确的认证令牌
$ websocat ws://localhost:25580 -H 'Authorization: Bearer n2pQcIG1OQ92jot2xG1M0aw0ZWnrh4F3Z3jw8qRP'

# 发送测试请求
{"jsonrpc":"2.0","method":"server/status","id":0}

# 应该收到响应
{"jsonrpc":"2.0","id":0,"result":{"started":true,"version":{"name":"1.21.9","protocol":...}}}
```

**如果令牌错误：**
```bash
$ websocat ws://localhost:25580
websocat: WebSocketError: Received unexpected status code (401 Unauthorized)
```

---

## ⚙️ **配置 NetherGate**

### **编辑 nethergate-config.yaml**

在 NetherGate 配置文件中设置 SMP 连接参数：

```yaml
server_connection:
  # SMP 协议配置
  smp:
    # 是否启用 SMP
    enabled: true
    
    # WebSocket 连接地址（必须与 server.properties 中的 management-server-host 和 management-server-port 匹配）
    host: "127.0.0.1"
    port: 25580
    
    # 是否使用 TLS
    use_tls: false
    
    # 认证令牌（必须与 server.properties 中的 management-server-secret 完全一致）
    auth_token: "n2pQcIG1OQ92jot2xG1M0aw0ZWnrh4F3Z3jw8qRP"
    
    # 连接超时（秒）
    connection_timeout: 10
    
    # 重连配置
    reconnect:
      enabled: true
      interval_seconds: 5
      max_attempts: 10
    
    # 心跳配置
    heartbeat:
      enabled: true
      timeout_seconds: 30
```

### **配置说明**

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|-------|------|
| `enabled` | bool | `true` | 是否启用 SMP 连接 |
| `host` | string | `127.0.0.1` | Minecraft 服务器的 SMP 监听地址 |
| `port` | int | `25580` | Minecraft 服务器的 SMP 监听端口 |
| `use_tls` | bool | `false` | 是否使用 TLS 加密连接 |
| `auth_token` | string | - | 认证令牌，**必须与 server.properties 中的完全一致** |
| `connection_timeout` | int | `10` | 连接超时时间（秒） |
| `reconnect.enabled` | bool | `true` | 是否启用自动重连 |
| `reconnect.interval_seconds` | int | `5` | 重连间隔（秒） |
| `reconnect.max_attempts` | int | `10` | 最大重连次数（0 = 无限重试） |
| `heartbeat.enabled` | bool | `true` | 是否启用心跳检测 |
| `heartbeat.timeout_seconds` | int | `30` | 心跳超时时间（秒） |

### **安全建议**

#### **1. 使用强认证令牌**

```bash
# 生成安全的 40 位令牌
openssl rand -base64 30 | tr -d '/+=' | head -c 40
```

**不要使用：**
- ❌ 简单字符串（如 `admin123`）
- ❌ 短令牌（少于 40 位）
- ❌ 包含特殊字符的令牌

#### **2. 限制网络访问**

**仅本地访问（推荐）：**
```properties
# server.properties
management-server-host=localhost
```

**使用防火墙：**
```bash
# Linux (iptables)
sudo iptables -A INPUT -p tcp --dport 25580 -s 127.0.0.1 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 25580 -j DROP

# Linux (ufw)
sudo ufw deny 25580/tcp
sudo ufw allow from 127.0.0.1 to any port 25580

# Windows 防火墙
New-NetFirewallRule -DisplayName "Block SMP" -Direction Inbound -LocalPort 25580 -Protocol TCP -Action Block
```

#### **3. 生产环境启用 TLS**

```properties
# server.properties
management-server-tls-enabled=true
management-server-tls-keystore=config/keystore.p12
management-server-tls-keystore-password=your_keystore_password
```

生成自签名证书：
```bash
# 生成 PKCS#12 格式的 KeyStore
keytool -genkeypair -alias minecraft -keyalg RSA -keysize 2048 \
  -validity 365 -keystore keystore.p12 -storetype PKCS12 \
  -storepass your_keystore_password \
  -dname "CN=minecraft.local, OU=Server, O=MyServer, L=City, ST=State, C=US"
```

NetherGate 配置：
```yaml
server_connection:
  smp:
    use_tls: true
    # 如果使用自签名证书，可能需要禁用证书验证（仅开发环境）
    verify_certificate: false
```

#### **4. 不同环境使用不同配置**

| 环境 | 建议配置 |
|------|----------|
| **开发** | `host: localhost`, `use_tls: false`, 简单令牌 |
| **测试** | `host: localhost`, `use_tls: false`, 中等强度令牌 |
| **生产** | `host: localhost`, `use_tls: true`, 强随机令牌, 防火墙限制 |

---

## 🔧 **使用 SMP API**

### **ISmpApi 接口**

```csharp
public interface ISmpApi
{
    // 连接管理
    Task<bool> ConnectAsync();
    void Disconnect();
    bool IsConnected { get; }
    
    // 玩家管理
    Task<List<PlayerInfo>> GetOnlinePlayersAsync();
    Task<PlayerInfo?> GetPlayerInfoAsync(string playerName);
    Task KickPlayerAsync(string playerName, string reason);
    Task BanPlayerAsync(string playerName, string reason, DateTime? expires = null);
    Task UnbanPlayerAsync(string playerName);
    
    // 白名单
    Task<List<string>> GetAllowlistAsync();
    Task AddToAllowlistAsync(string playerName);
    Task RemoveFromAllowlistAsync(string playerName);
    Task SetAllowlistAsync(List<string> playerNames);
    Task ClearAllowlistAsync();
    
    // 封禁管理
    Task<List<BanInfo>> GetBanListAsync();
    Task<List<string>> GetIpBanListAsync();
    Task IpBanAsync(string ipAddress, string reason, DateTime? expires = null);
    Task IpUnbanAsync(string ipAddress);
    
    // OP 管理
    Task<List<string>> GetOperatorsAsync();
    Task AddOperatorAsync(string playerName, int level = 4);
    Task RemoveOperatorAsync(string playerName);
    
    // 服务器状态
    Task<ServerState> GetServerStatusAsync();
    
    // 批量操作
    Task BatchKickAsync(List<string> playerNames, string reason);
    Task BatchBanAsync(List<string> playerNames, string reason);
}
```

### **基本用法**

```csharp
using NetherGate.API.Plugins;

public class MyPlugin : PluginBase
{

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        
        // SMP 已由框架自动连接
        if (_context.SmpApi.IsConnected)
        {
            _context.Logger.Info("SMP 已就绪");
        }
    }

    public async Task GetServerInfoAsync()
    {
        // 获取服务器状态
        var status = await _context.SmpApi.GetServerStatusAsync();
        
        _context.Logger.Info($"在线玩家: {status.PlayerCount}/{status.MaxPlayers}");
        _context.Logger.Info($"TPS: {status.Tps:F1}");
        _context.Logger.Info($"内存: {status.MemoryUsed}/{status.MemoryMax} MB");
    }
}
```

### **玩家管理**

```csharp
// 获取在线玩家列表
var players = await _context.SmpApi.GetOnlinePlayersAsync();
foreach (var player in players)
{
    _context.Logger.Info($"{player.Name} - {player.Uuid}");
}

// 获取特定玩家信息
var playerInfo = await _context.SmpApi.GetPlayerInfoAsync("Steve");
if (playerInfo != null)
{
    _context.Logger.Info($"玩家: {playerInfo.Name}");
    _context.Logger.Info($"UUID: {playerInfo.Uuid}");
    _context.Logger.Info($"IP: {playerInfo.IpAddress}");
}

// 踢出玩家
await _context.SmpApi.KickPlayerAsync("Steve", "违反规则");

// 封禁玩家
await _context.SmpApi.BanPlayerAsync("Steve", "作弊", expires: DateTime.UtcNow.AddDays(7));

// 解封玩家
await _context.SmpApi.UnbanPlayerAsync("Steve");

// 批量踢出
await _context.SmpApi.BatchKickAsync(
    new List<string> { "Player1", "Player2", "Player3" },
    "服务器维护"
);
```

### **白名单管理**

```csharp
// 获取白名单
var allowlist = await _context.SmpApi.GetAllowlistAsync();
_context.Logger.Info($"白名单玩家: {string.Join(", ", allowlist)}");

// 添加到白名单
await _context.SmpApi.AddToAllowlistAsync("Steve");

// 从白名单移除
await _context.SmpApi.RemoveFromAllowlistAsync("Steve");

// 批量设置白名单
await _context.SmpApi.SetAllowlistAsync(new List<string>
{
    "Steve", "Alex", "Bob"
});

// 清空白名单
await _context.SmpApi.ClearAllowlistAsync();
```

### **服务器状态**

```csharp
public class ServerMonitorPlugin : IPlugin
{
    private Timer? _monitorTimer;

    public void OnEnable(IPluginContext context)
    {
        // 每30秒检查一次服务器状态
        _monitorTimer = new Timer(async _ => await MonitorServerAsync(), 
            null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private async Task MonitorServerAsync()
    {
        var status = await _context.SmpApi.GetServerStatusAsync();
        
        // TPS 告警
        if (status.Tps < 15)
        {
            _context.Logger.Warning($"服务器 TPS 过低: {status.Tps:F1}");
        }
        
        // 内存告警
        var memoryUsagePercent = (double)status.MemoryUsed / status.MemoryMax * 100;
        if (memoryUsagePercent > 90)
        {
            _context.Logger.Warning($"服务器内存使用过高: {memoryUsagePercent:F1}%");
        }
        
        // 玩家数量
        _context.Logger.Debug($"在线: {status.PlayerCount}, TPS: {status.Tps:F1}");
    }

    public void OnDisable()
    {
        _monitorTimer?.Dispose();
    }
}
```

---

## 📡 **事件系统**

SMP 提供实时事件推送，无需轮询。

### **订阅事件**

```csharp
public void OnEnable(IPluginContext context)
{
    // 订阅玩家加入事件
    _context.EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
    
    // 订阅玩家离开事件
    _context.EventBus.Subscribe<PlayerLeftEvent>(OnPlayerLeft);
    
    // 订阅聊天事件
    _context.EventBus.Subscribe<PlayerChatEvent>(OnPlayerChat);
    
    // 订阅服务器心跳
    _context.EventBus.Subscribe<ServerHeartbeatEvent>(OnHeartbeat);
}

private async void OnPlayerJoined(PlayerJoinedEvent e)
{
    _context.Logger.Info($"{e.Player.Name} 加入了服务器");
    
    // 发送欢迎消息
    await _context.GameDisplayApi.SendChatMessage(
        e.Player.Name,
        "§a欢迎来到服务器！"
    );
}

private void OnPlayerLeft(PlayerLeftEvent e)
{
    _context.Logger.Info($"{e.Player.Name} 离开了服务器");
}

private void OnPlayerChat(PlayerChatEvent e)
{
    _context.Logger.Debug($"[聊天] {e.PlayerName}: {e.Message}");
}

private void OnHeartbeat(ServerHeartbeatEvent e)
{
    _context.Logger.Debug($"TPS: {e.State.Tps:F1}, 玩家: {e.State.PlayerCount}");
}
```

### **可用事件**

完整事件列表请参考：[事件列表](../08-参考/事件列表.md)

**核心事件：**
- `PlayerJoinedEvent` - 玩家加入
- `PlayerLeftEvent` - 玩家离开
- `PlayerChatEvent` - 玩家聊天
- `PlayerDeathEvent` - 玩家死亡
- `PlayerAdvancementEvent` - 玩家成就
- `ServerHeartbeatEvent` - 服务器心跳
- `AllowlistAddedEvent` - 白名单添加
- `BanAddedEvent` - 玩家封禁
- `OperatorAddedEvent` - OP 添加

---

## 💡 **最佳实践**

### **1. 优雅处理断线重连**

```csharp
public void OnEnable(IPluginContext context)
{
    _context = context;
    
    // 订阅 SMP 断线事件
    _context.EventBus.Subscribe<SmpDisconnectedEvent>(OnSmpDisconnected);
    _context.EventBus.Subscribe<SmpConnectedEvent>(OnSmpConnected);
}

private void OnSmpDisconnected(SmpDisconnectedEvent e)
{
    _context.Logger.Warning($"SMP 连接断开: {e.Reason}");
    
    // 切换到降级模式（仅使用 RCON）
    _useFallbackMode = true;
}

private void OnSmpConnected(SmpConnectedEvent e)
{
    _context.Logger.Info("SMP 重新连接成功");
    
    // 恢复正常模式
    _useFallbackMode = false;
    
    // 重新同步状态
    _ = ResyncStateAsync();
}
```

### **2. 缓存服务器状态**

```csharp
public class ServerStateCache
{
    private ServerState? _cachedState;
    private DateTime _lastUpdate;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

    public async Task<ServerState> GetStateAsync(ISmpApi smpApi)
    {
        if (_cachedState != null && DateTime.UtcNow - _lastUpdate < _cacheDuration)
        {
            return _cachedState;
        }
        
        _cachedState = await smpApi.GetServerStatusAsync();
        _lastUpdate = DateTime.UtcNow;
        
        return _cachedState;
    }

    public void Invalidate()
    {
        _cachedState = null;
    }
}
```

### **3. 批量操作**

```csharp
// ❌ 不高效：逐个操作
foreach (var player in playersToKick)
{
    await _context.SmpApi.KickPlayerAsync(player, "维护");
}

// ✅ 高效：批量操作
await _context.SmpApi.BatchKickAsync(playersToKick, "维护");
```

### **4. 错误处理**

```csharp
public async Task<List<PlayerInfo>> GetOnlinePlayersSafeAsync()
{
    try
    {
        if (!_context.SmpApi.IsConnected)
        {
            _context.Logger.Warning("SMP 未连接，使用降级方案");
            return await GetPlayersViaRconAsync();
        }
        
        return await _context.SmpApi.GetOnlinePlayersAsync();
    }
    catch (TimeoutException)
    {
        _context.Logger.Error("SMP 请求超时");
        return new List<PlayerInfo>();
    }
    catch (Exception ex)
    {
        _context.Logger.Error($"获取玩家列表失败: {ex.Message}");
        return new List<PlayerInfo>();
    }
}

private async Task<List<PlayerInfo>> GetPlayersViaRconAsync()
{
    // 降级到 RCON
    var response = await _context.RconClient.SendCommandAsync("list");
    // 解析响应...
    return new List<PlayerInfo>();
}
```

---

## 📚 **相关文档**

### **NetherGate 文档**
- [RCON 集成](./RCON集成.md) - RCON 协议使用指南
- [事件系统](./事件系统.md) - 事件订阅和发布
- [事件列表](../08-参考/事件列表.md) - 所有可用事件
- [API 参考](../08-参考/API参考.md) - ISmpApi 完整接口

### **Minecraft 官方文档**
- [Minecraft Wiki - 服务端管理协议](https://zh.minecraft.wiki/w/%E6%9C%8D%E5%8A%A1%E7%AB%AF%E7%AE%A1%E7%90%86%E5%8D%8F%E8%AE%AE) - SMP 官方文档
- [JSON-RPC 2.0 规范](https://www.jsonrpc.org/specification) - 协议标准
- [WebSocket 协议](https://datatracker.ietf.org/doc/html/rfc6455) - WebSocket RFC 6455

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05  
**基于：** Minecraft Java 版 1.21.9 (快照 25w35a)
