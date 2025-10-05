# NetherGate 未来扩展性设计

本文档说明 NetherGate 的架构如何支持未来的功能扩展。

---

## 🔮 设计理念

NetherGate 的架构设计遵循以下原则，确保能够轻松适应未来的变化：

### 1. 协议抽象层

**当前实现：**
```
插件 API (IServerApi)
      ↓
├── SMP Client (WebSocket + JSON-RPC)
├── RCON Client (TCP + RCON Protocol)
└── Log Listener (Process Monitor)
```

**未来扩展：**
```
插件 API (IServerApi)  ← 保持稳定，插件无需修改
      ↓
├── SMP Client v2 (增强功能)  ← Mojang更新SMP时
├── RCON Client
├── Log Listener
└── ??? Client (新协议)  ← 未来可能出现的新协议
```

---

## 🚀 预期的未来扩展场景

### 场景1：Mojang扩展SMP功能

**可能性：极高 ⭐⭐⭐⭐⭐**

Mojang很可能在未来版本中为SMP添加更多功能，例如：

#### 潜在的新功能

```csharp
// 🔮 可能在未来版本中添加
public interface IServerApi
{
    // 现有SMP功能
    Task<List<PlayerDto>> GetPlayersAsync();
    Task AddToAllowlistAsync(PlayerDto player);
    
    // 🔮 未来可能的SMP扩展
    Task<PlayerInventory> GetPlayerInventoryAsync(PlayerDto player);
    Task SetPlayerHealthAsync(PlayerDto player, int health);
    Task<List<Entity>> GetWorldEntitiesAsync();
    Task ExecuteCommandAsync(string command);  // SMP原生支持命令执行！
    Task<WorldData> GetWorldDataAsync();
    Task SubscribeToPlayerChatAsync(Action<ChatMessage> handler);
}
```

#### 扩展实现步骤

1. **更新 SmpClient**
```csharp
// NetherGate.Core/Protocol/Smp/SmpClient.cs
public class SmpClient
{
    // 添加新的JSON-RPC方法调用
    public async Task<PlayerInventory> GetPlayerInventoryAsync(PlayerDto player)
    {
        return await _rpcHandler.InvokeAsync<PlayerInventory>(
            "player/inventory",
            new { player = player.Uuid }
        );
    }
}
```

2. **更新 IServerApi 接口**
```csharp
// NetherGate.API/IServerApi.cs
public interface IServerApi
{
    // 新方法自动可用
    Task<PlayerInventory> GetPlayerInventoryAsync(PlayerDto player);
}
```

3. **插件自动支持** ✅
```csharp
// 插件无需修改，直接使用新功能！
public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        var inventory = await Server.GetPlayerInventoryAsync(player);
        // 新功能立即可用！
    }
}
```

---

### 场景2：更好的插件注入方式

**可能性：中等 ⭐⭐⭐**

未来可能出现比当前方案更好的插件集成方式。

#### 2.1 官方插件API（Mojang原生支持）

如果Mojang推出官方插件API：

```csharp
// 新的 OfficialPluginApi Client
public class OfficialPluginClient
{
    // 使用官方API实现
    public async Task RegisterCommandAsync(string command, CommandHandler handler)
    {
        // 官方API可能支持真正的命令注册！
    }
}

// NetherGate适配
public class ServerApiAdapter : IServerApi
{
    private SmpClient _smp;
    private RconClient _rcon;
    private OfficialPluginClient _official;  // 新增
    
    // 智能路由：优先使用官方API
    public async Task RegisterCommandAsync(string command)
    {
        if (_official?.IsConnected == true)
        {
            await _official.RegisterCommandAsync(command, handler);
        }
        else
        {
            // 降级到RCON + 日志监听方案
            await FallbackToRconAsync(command);
        }
    }
}
```

**优势：**
- ✅ 新老服务器都支持
- ✅ 自动选择最佳实现
- ✅ 插件代码无需修改

#### 2.2 Mod/Fabric/Forge 集成

支持服务端Mod框架的直接集成：

```csharp
public class FabricBridgeClient
{
    // 通过Fabric Mod直接与服务器交互
    public async Task<T> InvokeModMethodAsync<T>(string modId, string method, params object[] args)
    {
        // 更深层次的服务器集成
    }
}
```

---

### 场景3：数据包（Data Pack）集成

**可能性：高 ⭐⭐⭐⭐**

数据包是Minecraft的官方扩展方式，NetherGate可以提供更好的集成：

```csharp
public interface IServerApi
{
    // 🔮 数据包管理
    Task<List<DataPack>> GetLoadedDataPacksAsync();
    Task LoadDataPackAsync(string packName);
    Task UnloadDataPackAsync(string packName);
    
    // 🔮 函数执行（增强版）
    Task<FunctionResult> ExecuteFunctionAsync(string function, ExecutionContext context);
    
    // 🔮 谓词评估
    Task<bool> EvaluatePredicateAsync(string predicate, PredicateContext context);
}
```

---

## 🏗️ 架构扩展指南

### 添加新协议客户端

#### 第1步：定义客户端接口

```csharp
// NetherGate.Core/Protocol/NewProtocol/INewProtocolClient.cs
public interface INewProtocolClient
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(NewProtocolConfig config);
    Task DisconnectAsync();
    
    // 协议特定方法
    Task<TResult> ExecuteAsync<TResult>(string method, object? parameters);
}
```

#### 第2步：实现客户端

```csharp
// NetherGate.Core/Protocol/NewProtocol/NewProtocolClient.cs
public class NewProtocolClient : INewProtocolClient
{
    private readonly ILogger _logger;
    
    public bool IsConnected { get; private set; }
    
    public async Task<bool> ConnectAsync(NewProtocolConfig config)
    {
        // 实现连接逻辑
    }
    
    // ... 实现其他方法
}
```

#### 第3步：集成到 ServerApi

```csharp
// NetherGate.Core/Server/ServerApiImpl.cs
public class ServerApiImpl : IServerApi
{
    private readonly SmpClient _smp;
    private readonly RconClient _rcon;
    private readonly LogListener _log;
    private readonly INewProtocolClient? _newProtocol;  // 新增
    
    // 智能路由
    public async Task<PlayerData> GetPlayerDataAsync(PlayerDto player)
    {
        // 优先使用最强大的协议
        if (_newProtocol?.IsConnected == true)
        {
            return await _newProtocol.GetPlayerDataAsync(player);
        }
        else if (_smp.IsConnected)
        {
            return await _smp.GetPlayerDataAsync(player);
        }
        else
        {
            throw new InvalidOperationException("No protocol available");
        }
    }
}
```

#### 第4步：更新配置

```json
// config/nethergate.json
{
  "server_connection": {
    "smp": { "enabled": true, "port": 25575 },
    "rcon": { "enabled": true, "port": 25566 },
    "new_protocol": { "enabled": true, "port": 25577 }  // 新协议
  }
}
```

---

## 📊 版本兼容性策略

### API版本控制

```csharp
// NetherGate.API/IServerApi.cs
[ApiVersion("1.0")]
public interface IServerApi
{
    // v1.0 方法
}

[ApiVersion("2.0")]
public interface IServerApiV2 : IServerApi
{
    // v2.0 新增方法
    Task<NewFeature> GetNewFeatureAsync();
}
```

### 插件兼容性

```csharp
// plugin.json
{
  "id": "my-plugin",
  "api_version": "1.0",  // 插件使用的API版本
  "min_nethergate_version": "1.0.0"
}
```

**NetherGate 保证：**
- ✅ API v1.0 永远可用
- ✅ 新版本向后兼容
- ✅ 插件只需声明最低版本

---

## 🎯 实际扩展案例

### 案例1：SMP添加命令执行支持

**假设场景：** Minecraft 1.22 的SMP添加了 `command/execute` 方法

#### 当前实现（使用RCON）
```csharp
public async Task GiveItemAsync(string player, string item)
{
    if (Rcon?.IsConnected == true)
    {
        await Rcon.ExecuteCommandAsync($"give {player} {item}");
    }
}
```

#### 未来实现（SMP原生支持）
```csharp
public async Task GiveItemAsync(string player, string item)
{
    // NetherGate 自动选择最佳方式
    if (_smp.SupportsCommands)  // SMP v2支持
    {
        var result = await _smp.ExecuteCommandAsync($"give {player} {item}");
        // 结构化的命令结果！
        if (!result.Success)
        {
            Logger.Warning($"命令执行失败: {result.Error}");
        }
    }
    else if (Rcon?.IsConnected == true)  // 降级到RCON
    {
        await Rcon.ExecuteCommandAsync($"give {player} {item}");
    }
}
```

**插件视角：**
```csharp
// 插件代码完全不变！
await Server.GiveItemAsync(player.Name, "diamond");
// NetherGate 内部自动选择最佳实现
```

---

### 案例2：新的事件通知机制

**假设场景：** 未来出现更强大的事件系统

```csharp
// 当前：通过日志监听
Server.SubscribeToServerLog(entry => {
    if (entry.Type == LogEntryType.PlayerChat)
    {
        // 需要正则解析
        var match = Regex.Match(entry.Message, @"<(\w+)> (.+)");
    }
});

// 未来：SMP原生事件
Events.Subscribe<PlayerChatEvent>(this, EventPriority.Normal, e => {
    // 结构化数据，直接可用！
    Logger.Info($"{e.Player.Name}: {e.Message}");
    
    // 甚至可以取消消息
    if (e.Message.Contains("禁词"))
    {
        e.Cancel();
    }
});
```

---

## 🔧 开发者指南

### 为未来编写代码

#### ✅ 好的做法

```csharp
// 使用抽象接口
public async Task WelcomePlayerAsync(PlayerDto player)
{
    await Server.SendSystemMessageAsync($"Welcome {player.Name}!");
    // NetherGate 会选择最佳实现（SMP/RCON）
}

// 检查功能可用性
if (Server.SupportsFeature("inventory_management"))
{
    var inventory = await Server.GetPlayerInventoryAsync(player);
}
```

#### ❌ 避免的做法

```csharp
// 不要直接依赖特定协议
if (Rcon != null)  // ❌ 硬编码协议依赖
{
    await Rcon.ExecuteCommandAsync("...");
}

// 应该使用抽象方法
await Server.ExecuteCommandAsync("...");  // ✅ 让NetherGate选择
```

---

## 📈 路线图

### 近期（当前版本）
- ✅ SMP + RCON + 日志监听三位一体
- ✅ 完整的插件API
- ✅ 模块化协议层

### 中期（未来6-12个月）
- 🔄 跟踪Mojang的SMP更新
- 🔄 支持新的SMP功能
- 🔄 优化日志解析器

### 远期（1年+）
- 🔮 官方插件API支持（如果Mojang推出）
- 🔮 Mod框架集成
- 🔮 更深层次的服务器集成

---

## 💡 总结

NetherGate的架构设计确保：

1. **当前最优** - SMP+RCON+日志监听是目前最完整的方案
2. **面向未来** - 模块化设计，轻松添加新协议
3. **向后兼容** - 插件API稳定，插件无需修改
4. **智能路由** - 自动选择最佳实现方式
5. **逐步演进** - 平滑过渡到新技术

**你的判断完全正确** 🎯：
- Mojang很可能扩展SMP功能
- 可能会出现更好的集成方式
- NetherGate已经做好了准备！

让我们一起期待 Minecraft 和 NetherGate 的未来！🚀

