# SMP 协议（Server Management Protocol）

SMP（Server Management Protocol）是 NetherGate 的高级服务器管理协议，基于 WebSocket 和 JSON-RPC 2.0，提供实时事件推送和双向通信。

---

## 📋 **目录**

- [什么是 SMP](#什么是-smp)
- [安装 SMP 插件](#安装-smp-插件)
- [配置 SMP](#配置-smp)
- [使用 SMP API](#使用-smp-api)
- [事件系统](#事件系统)
- [最佳实践](#最佳实践)

---

## 🌐 **什么是 SMP**

SMP（Server Management Protocol）是专为 NetherGate 设计的服务器管理协议。

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
| 协议类型 | Minecraft 原生 | WebSocket + JSON-RPC |
| 连接方式 | TCP | WebSocket (持久连接) |
| 事件推送 | ❌ 不支持 | ✅ 支持 |
| 实时性 | 低（需轮询） | 高（主动推送） |
| 批量操作 | ❌ 不支持 | ✅ 支持 |
| 服务器状态 | 有限 | 详细（TPS、内存、玩家列表等） |
| 安装要求 | 无 | 需要安装插件 |

**推荐策略：**
- **RCON：** 用于基础命令执行
- **SMP：** 用于事件监听和高级功能
- **最佳：** 同时启用，互补使用

---

## 📦 **安装 SMP 插件**

### **支持的服务器类型**

- ✅ **Bukkit / Spigot / Paper** (推荐)
- ✅ **Fabric** (使用 Fabric API)
- ✅ **Forge** (使用 Forge API)

### **安装步骤**

#### **1. 下载插件**

从 [GitHub Releases](https://github.com/your-org/NetherGate-SMP/releases) 下载对应版本：

- `NetherGate-SMP-Bukkit-1.0.0.jar` - Bukkit/Spigot/Paper
- `NetherGate-SMP-Fabric-1.0.0.jar` - Fabric
- `NetherGate-SMP-Forge-1.0.0.jar` - Forge

#### **2. 安装插件**

**Bukkit/Spigot/Paper:**
```bash
# 复制到 plugins 目录
cp NetherGate-SMP-Bukkit-1.0.0.jar server/plugins/

# 重启服务器
```

**Fabric:**
```bash
# 复制到 mods 目录
cp NetherGate-SMP-Fabric-1.0.0.jar server/mods/

# 确保已安装 Fabric API
```

**Forge:**
```bash
# 复制到 mods 目录
cp NetherGate-SMP-Forge-1.0.0.jar server/mods/
```

#### **3. 配置插件**

首次启动后会生成配置文件：

**Bukkit/Spigot/Paper:**  
`plugins/NetherGate-SMP/config.yml`

**Fabric/Forge:**  
`config/nethergate-smp.toml`

**配置示例（YAML）：**
```yaml
# WebSocket 服务器配置
websocket:
  # 监听端口
  port: 25580
  
  # 监听地址（0.0.0.0 = 所有接口，127.0.0.1 = 仅本地）
  host: "0.0.0.0"
  
  # 认证密钥（必须与 NetherGate 配置一致）
  auth_key: "your_secret_key_here"

# 心跳配置
heartbeat:
  # 心跳间隔（秒）
  interval: 1
  
  # 是否启用
  enabled: true

# 事件配置
events:
  # 是否推送玩家加入事件
  player_join: true
  
  # 是否推送玩家离开事件
  player_leave: true
  
  # 是否推送聊天事件
  player_chat: true
  
  # 是否推送死亡事件
  player_death: true
  
  # 是否推送成就事件
  player_advancement: true

# 日志配置
logging:
  # 日志级别：DEBUG, INFO, WARNING, ERROR
  level: "INFO"
  
  # 是否记录所有 RPC 调用
  log_rpc_calls: false
```

#### **4. 重启服务器**

```bash
# 重启 Minecraft 服务器
stop
# 等待服务器完全停止
# 再次启动
```

#### **5. 验证安装**

在服务器控制台查看：

```
[NetherGate-SMP] WebSocket 服务器已启动: 0.0.0.0:25580
[NetherGate-SMP] 等待 NetherGate 连接...
```

启动 NetherGate 后：

```
[NetherGate-SMP] NetherGate 已连接
[INFO]: SMP 连接成功: localhost:25580
```

---

## ⚙️ **配置 SMP**

### **NetherGate 配置**

编辑 `nethergate-config.yaml`：

```yaml
smp:
  # 是否启用 SMP
  enabled: true
  
  # WebSocket 连接地址
  websocket_url: "ws://127.0.0.1:25580"
  # 或者明确指定端口：
  websocket_port: 25580
  
  # 认证密钥（必须与 SMP 插件配置一致）
  auth_key: "your_secret_key_here"
  
  # 重连配置
  reconnect_interval_seconds: 5
  max_reconnect_attempts: 10
  
  # 是否启用心跳
  enable_heartbeat: true
```

### **安全建议**

1. **使用强密钥**
   ```bash
   # 生成随机密钥
   openssl rand -base64 32
   ```

2. **限制访问**
   ```yaml
   # 仅本地访问
   host: "127.0.0.1"
   
   # 或使用防火墙
   # sudo ufw deny 25580/tcp
   ```

3. **不同环境使用不同密钥**
   - 开发环境：简单密钥
   - 生产环境：强随机密钥

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

- [RCON 集成](./RCON集成.md)
- [事件系统](./事件系统.md)
- [事件列表](../08-参考/事件列表.md)
- [API 参考](../08-参考/API参考.md)

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05
