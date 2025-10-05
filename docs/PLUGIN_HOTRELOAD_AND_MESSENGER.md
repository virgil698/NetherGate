# 插件热重载与插件间通信

本文档介绍 NetherGate 的两个高级功能：**插件热重载**和**插件间通信 API**。

---

## 📦 插件热重载

### 功能概述

插件热重载允许在不重启 NetherGate 的情况下更新插件代码，提升开发和维护效率。

### 使用方法

#### 通过命令行重载插件

```bash
# 在 NetherGate 控制台中执行
plugin reload <plugin-id>
```

#### 通过 API 重载插件

```csharp
// 在其他插件中调用
var pluginManager = context.GetService<PluginManager>();
await pluginManager.ReloadPluginAsync("my-plugin");
```

### 热重载流程

1. **保存状态** - 如果插件实现了状态保存方法，会自动保存当前状态
2. **禁用插件** - 调用 `OnDisableAsync()`
3. **卸载插件** - 调用 `OnUnloadAsync()`
4. **清理资源** - 自动清理命令注册、事件订阅、消息订阅
5. **重新加载** - 重新加载插件程序集
6. **初始化插件** - 调用 `OnLoadAsync()` 和 `OnEnableAsync()`
7. **恢复状态** - 如果之前保存了状态，会自动恢复

### 插件状态保持（可选）

插件可以选择实现状态保存和恢复，以便热重载时保持数据：

```csharp
public class MyPlugin : IPlugin
{
    private MyPluginState _state = new();
    
    // 可选：实现状态保存
    public Task<object> SaveStateAsync()
    {
        return Task.FromResult<object>(_state);
    }
    
    // 可选：实现状态恢复
    public Task RestoreStateAsync(object state)
    {
        if (state is MyPluginState savedState)
        {
            _state = savedState;
        }
        return Task.CompletedTask;
    }
    
    // 插件状态类
    [Serializable]
    public class MyPluginState
    {
        public int Counter { get; set; }
        public Dictionary<string, string> Data { get; set; } = new();
    }
}
```

### 注意事项

⚠️ **当前实现限制：**
- 完整的热重载需要修改 `PluginLoader` 以支持单个插件的重载
- 当前版本会卸载插件，但需要重启 NetherGate 才能完全重载
- 建议在开发环境中使用，生产环境建议重启整个应用

🔮 **未来改进：**
- 真正的热重载支持（无需重启）
- 插件依赖关系的级联重载
- 更智能的状态迁移机制

---

## 💬 插件间通信 API

### 功能概述

插件间通信 API 允许插件之间安全地发送和接收消息，实现插件协作。

### 核心接口

#### IPluginMessenger

```csharp
public interface IPluginMessenger
{
    // 向指定插件发送消息
    Task<object?> SendMessageAsync(string targetPluginId, string channel, object data, bool requireResponse = false);
    
    // 广播消息给所有插件
    Task BroadcastMessageAsync(string channel, object data, bool excludeSelf = true);
    
    // 订阅消息频道（带响应）
    void Subscribe(string channel, Func<PluginMessage, Task<object?>> handler);
    
    // 订阅消息频道（无响应）
    void Subscribe(string channel, Func<PluginMessage, Task> handler);
    
    // 取消订阅
    void Unsubscribe(string channel);
    void UnsubscribeAll();
    
    // 检查插件可用性
    bool IsPluginAvailable(string pluginId);
    
    // 获取订阅频道列表
    IReadOnlyList<string> GetSubscribedChannels(string? pluginId = null);
}
```

#### PluginMessage

```csharp
public class PluginMessage
{
    public string SenderPluginId { get; init; }      // 发送者插件 ID
    public string ReceiverPluginId { get; init; }    // 接收者插件 ID
    public string Channel { get; init; }             // 消息频道
    public object Data { get; init; }                // 消息数据
    public DateTime Timestamp { get; init; }         // 发送时间
    public bool RequireResponse { get; init; }       // 是否需要响应
    public string MessageId { get; init; }           // 消息 ID
}
```

### 使用示例

#### 示例 1：简单的点对点通信

**插件 A（发送者）：**
```csharp
public class PluginA : IPlugin
{
    private IPluginContext _context;
    
    public async Task OnEnableAsync()
    {
        // 发送消息给插件 B
        await _context.Messenger.SendMessageAsync(
            targetPluginId: "plugin-b",
            channel: "greeting",
            data: new { Message = "Hello from Plugin A!" }
        );
    }
}
```

**插件 B（接收者）：**
```csharp
public class PluginB : IPlugin
{
    private IPluginContext _context;
    
    public async Task OnEnableAsync()
    {
        // 订阅 greeting 频道
        _context.Messenger.Subscribe("greeting", async (message) =>
        {
            _context.Logger.Info($"收到来自 {message.SenderPluginId} 的消息");
            
            // 处理消息数据
            if (message.Data is IDictionary<string, object> data)
            {
                var msg = data["Message"]?.ToString();
                _context.Logger.Info($"消息内容: {msg}");
            }
        });
    }
}
```

#### 示例 2：请求-响应模式

**插件 A（请求者）：**
```csharp
public async Task<int> GetPlayerCountAsync()
{
    // 向玩家管理插件请求在线玩家数
    var response = await _context.Messenger.SendMessageAsync(
        targetPluginId: "player-manager",
        channel: "player-count",
        data: null,
        requireResponse: true  // 需要响应
    );
    
    return response is int count ? count : 0;
}
```

**插件 B（响应者）：**
```csharp
public async Task OnEnableAsync()
{
    // 订阅 player-count 频道，并返回响应
    _context.Messenger.Subscribe("player-count", async (message) =>
    {
        // 查询在线玩家数
        var players = await _context.SmpApi.GetPlayersAsync();
        return players.Count;  // 返回响应数据
    });
}
```

#### 示例 3：广播消息

**经济插件（广播交易事件）：**
```csharp
public async Task ProcessTransactionAsync(string player, decimal amount)
{
    // 处理交易逻辑
    // ...
    
    // 广播交易事件给所有插件
    await _context.Messenger.BroadcastMessageAsync(
        channel: "economy.transaction",
        data: new
        {
            Player = player,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        }
    );
}
```

**统计插件（接收广播）：**
```csharp
public async Task OnEnableAsync()
{
    // 订阅经济交易事件
    _context.Messenger.Subscribe("economy.transaction", async (message) =>
    {
        // 记录交易统计
        var data = message.Data as dynamic;
        await RecordTransactionAsync(data.Player, data.Amount);
    });
}
```

#### 示例 4：类型安全的消息传递

定义消息数据类：

```csharp
// 共享的消息数据定义
public class PlayerKillEvent
{
    public string Killer { get; set; }
    public string Victim { get; set; }
    public string Weapon { get; set; }
    public DateTime Time { get; set; }
}
```

**PvP 插件（发送者）：**
```csharp
public async Task OnPlayerKillAsync(string killer, string victim, string weapon)
{
    var killEvent = new PlayerKillEvent
    {
        Killer = killer,
        Victim = victim,
        Weapon = weapon,
        Time = DateTime.UtcNow
    };
    
    await _context.Messenger.BroadcastMessageAsync(
        channel: "pvp.player-kill",
        data: killEvent
    );
}
```

**统计插件（接收者）：**
```csharp
public async Task OnEnableAsync()
{
    _context.Messenger.Subscribe("pvp.player-kill", async (message) =>
    {
        if (message.Data is PlayerKillEvent killEvent)
        {
            await UpdateKillStatsAsync(killEvent);
        }
    });
}
```

### 最佳实践

#### 1. 频道命名规范

建议使用分层命名格式：

```
<plugin-name>.<category>.<action>

示例：
- economy.balance.changed
- pvp.player-kill
- shop.item.purchased
- backup.world.saved
```

#### 2. 检查插件可用性

在发送消息前检查目标插件是否在线：

```csharp
if (_context.Messenger.IsPluginAvailable("target-plugin"))
{
    await _context.Messenger.SendMessageAsync(...);
}
```

#### 3. 使用强类型数据

定义明确的数据类型，避免使用匿名对象：

```csharp
// ✅ 推荐
public class TransactionData
{
    public string Player { get; set; }
    public decimal Amount { get; set; }
}

// ❌ 不推荐
var data = new { Player = "John", Amount = 100 };
```

#### 4. 错误处理

消息处理器中应该捕获异常：

```csharp
_context.Messenger.Subscribe("my-channel", async (message) =>
{
    try
    {
        // 处理消息
        await ProcessMessageAsync(message);
    }
    catch (Exception ex)
    {
        _context.Logger.Error($"处理消息失败: {ex.Message}");
    }
});
```

#### 5. 清理订阅

在插件禁用时，自动清理所有订阅（框架已自动处理）：

```csharp
public async Task OnDisableAsync()
{
    // 不需要手动调用 UnsubscribeAll()
    // 框架会自动清理
}
```

### 消息流程

```
发送者插件                   NetherGate 核心                   接收者插件
    |                              |                              |
    |-- SendMessageAsync() ------->|                              |
    |                              |                              |
    |                              |-- 检查目标插件是否可用 ------>|
    |                              |                              |
    |                              |<-- 插件可用 ------------------|
    |                              |                              |
    |                              |-- DeliverMessage() --------->|
    |                              |                              |
    |                              |                      处理消息 |
    |                              |                              |
    |                              |<-- 返回响应（可选）-----------|
    |                              |                              |
    |<-- 返回响应 ------------------|                              |
    |                              |                              |
```

### 性能考虑

- **异步设计**：所有消息传递都是异步的，不会阻塞调用者
- **并发广播**：广播消息时，并发向所有插件投递
- **超时保护**：可以配置消息处理超时
- **错误隔离**：单个插件的错误不会影响其他插件

### 调试技巧

#### 查看插件订阅的频道

```csharp
var channels = _context.Messenger.GetSubscribedChannels();
_context.Logger.Info($"当前订阅的频道: {string.Join(", ", channels)}");
```

#### 查看其他插件订阅的频道

```csharp
var channels = _context.Messenger.GetSubscribedChannels("target-plugin-id");
```

#### 启用消息日志

消息传递器会自动记录所有消息的发送和接收日志（Debug 级别）。

---

## 🔗 相关文档

- [插件开发指南](API_DESIGN.md)
- [事件系统](../DEVELOPMENT.md#事件系统)
- [插件项目结构](PLUGIN_PROJECT_STRUCTURE.md)

---

## 📝 总结

### 插件热重载

✅ **已实现：**
- 插件禁用和卸载
- 资源自动清理（命令、消息订阅）
- 状态保存和恢复接口（可选）

⚠️ **限制：**
- 需要重启才能完全重载

### 插件间通信

✅ **已实现：**
- 点对点消息传递
- 广播消息
- 请求-响应模式
- 订阅/取消订阅
- 插件可用性检查

🎯 **推荐用法：**
- 使用频道隔离不同类型的消息
- 使用强类型数据类进行通信
- 检查目标插件可用性
- 做好错误处理

---

**更新日期：** 2025-10-05  
**版本：** 1.0.0
