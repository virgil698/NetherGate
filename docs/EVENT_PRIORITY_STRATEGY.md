# NetherGate 事件监听优先级策略

## 📋 优先级原则

NetherGate 采用**三层事件监听策略**，按优先级从高到低依次为：

```
1️⃣ SMP 协议（Server Management Protocol） - 最高优先级
    ↓ 如果 SMP 不支持该事件
2️⃣ RCON 协议（Remote Console） - 次优先级
    ↓ 如果 RCON 不支持该事件
3️⃣ 日志解析（Log Parsing） - 最低优先级
```

### 为什么采用这个优先级？

| 协议/方式 | 优势 | 劣势 | 适用场景 |
|----------|------|------|---------|
| **SMP** | ✅ 结构化数据<br>✅ 实时推送<br>✅ 包含 UUID<br>✅ 官方支持 | ❌ 覆盖范围有限<br>❌ 需要 MC 1.21.9+ | 管理操作、玩家加入/离开 |
| **RCON** | ✅ 命令执行<br>✅ 广泛支持 | ❌ 无事件监听<br>❌ 单向通信 | 执行游戏命令 |
| **日志解析** | ✅ 覆盖所有事件<br>✅ 无版本限制 | ❌ 不可靠（格式变更）<br>❌ 缺少结构化数据<br>❌ 有延迟 | SMP/RCON 不支持的事件 |

---

## 🟢 第一层：SMP 协议（已实现）

根据 [Minecraft Wiki SMP 文档](https://zh.minecraft.wiki/w/%E6%9C%8D%E5%8A%A1%E7%AB%AF%E7%AE%A1%E7%90%86%E5%8D%8F%E8%AE%AE?variant=zh-cn)，SMP 支持以下实时通知：

### 玩家事件
- ✅ **玩家加入** (`players/joined`) → `PlayerJoinedEvent`
  - 包含：UUID、玩家名、IP 地址
- ✅ **玩家离开** (`players/left`) → `PlayerLeftEvent`
  - 包含：UUID、玩家名

### 管理操作事件
- ✅ **管理员添加** (`operators/added`) → `OperatorAddedEvent`
- ✅ **管理员移除** (`operators/removed`) → `OperatorRemovedEvent`
- ✅ **白名单添加** (`allowlist/added`) → `AllowlistChangedEvent`
- ✅ **白名单移除** (`allowlist/removed`) → `AllowlistChangedEvent`
- ✅ **玩家封禁** (`bans/added`) → `PlayerBannedEvent`
- ✅ **玩家解封** (`bans/removed`) → `PlayerUnbannedEvent`
- ✅ **IP 封禁** (`ip_bans/added`) → `IpBannedEvent`
- ✅ **IP 解封** (`ip_bans/removed`) → `IpUnbannedEvent`

### 服务器事件
- ✅ **游戏规则更新** (`gamerules/updated`) → `GameRuleChangedEvent`
- ✅ **服务器状态心跳** (`server/status`) → `ServerStartedEvent`

**实现文件**：
- `src/NetherGate.Core/Protocol/SmpClient.cs` (719 行)
- `src/NetherGate.API/Events/SmpEvents.cs` (235 行)

---

## 🟡 第二层：RCON 协议（已实现）

### 支持的功能
- ✅ **执行游戏命令** (`SendCommandAsync()`)
  - 示例：`/say`, `/give`, `/tp`, `/gamemode`, `/effect` 等

### 不支持的功能
- ❌ **事件监听**（RCON 是单向通信协议，只能发送命令，无法接收事件推送）

**实现文件**：
- `src/NetherGate.Core/Protocol/RconClient.cs` (268 行)
- `src/NetherGate.API/Protocol/IRconClient.cs` (42 行)

**使用场景**：
- 插件需要执行游戏内命令
- 配合日志解析实现命令响应

---

## 🔴 第三层：日志解析（已实现）

用于监听 SMP 和 RCON 都不支持的事件。

### 支持的事件
- ✅ **玩家聊天** → `PlayerChatEvent`
  - 解析：`<PlayerName> message`
- ✅ **玩家命令** → `PlayerCommandEvent`
  - 解析：`PlayerName issued server command: /command`
- ✅ **玩家死亡** → `PlayerDeathEvent`
  - 解析：关键词匹配（was killed, died, drowned 等）
- ✅ **玩家成就** → `PlayerAchievementEvent`
  - 解析：`PlayerName has made the advancement [Achievement]`
- ✅ **通用日志** → `ServerLogEvent`
  - 所有服务器输出

### 服务器进程监控
- ✅ **进程启动** → `ServerProcessStartedEvent`
- ✅ **进程停止** → `ServerProcessStoppedEvent`
- ✅ **进程崩溃** → `ServerProcessCrashedEvent`

**实现文件**：
- `src/NetherGate.Core/Process/LogParser.cs` (182 行)
- `src/NetherGate.Core/Process/ServerProcessManager.cs` (406 行)
- `src/NetherGate.API/Events/ServerEvents.cs` (101 行)

**注意事项**：
- 日志格式可能因 Minecraft 版本、语言、MOD 而变化
- 不如 SMP 可靠，建议仅用于补充

---

## 📊 事件覆盖矩阵

| 事件类型 | SMP | RCON | 日志解析 | 当前实现 |
|---------|-----|------|---------|---------|
| 玩家加入 | ✅ | ❌ | ✅ | **SMP** |
| 玩家离开 | ✅ | ❌ | ✅ | **SMP** |
| 玩家聊天 | ❌ | ❌ | ✅ | **日志解析** |
| 玩家命令 | ❌ | ❌ | ✅ | **日志解析** |
| 玩家死亡 | ❌ | ❌ | ✅ | **日志解析** |
| 玩家成就 | ❌ | ❌ | ✅ | **日志解析** |
| 管理员操作 | ✅ | ❌ | ❌ | **SMP** |
| 白名单操作 | ✅ | ❌ | ❌ | **SMP** |
| 封禁操作 | ✅ | ❌ | ❌ | **SMP** |
| 游戏规则变更 | ✅ | ❌ | ❌ | **SMP** |
| 执行命令 | ❌ | ✅ | ❌ | **RCON** |
| 服务器进程 | ❌ | ❌ | ✅ | **进程监控** |

---

## 🎯 代码实现验证

### 避免重复监听

`LogParser.cs` 中已明确注释：

```csharp
// 注意: 玩家加入/离开事件由 SMP 协议提供（更可靠、更及时）
// 此处仅解析 SMP 不支持的事件
```

**已移除的日志解析**：
- ~~玩家加入~~ → 改用 SMP `players/joined`
- ~~玩家离开~~ → 改用 SMP `players/left`

**保留的日志解析**（SMP 不支持）：
- ✅ 玩家聊天
- ✅ 玩家命令
- ✅ 玩家死亡
- ✅ 玩家成就

---

## 🚀 未来扩展

### 如果 Mojang 增加新的 SMP 通知

**步骤**：
1. 在 `SmpClient.cs` 中注册新通知处理器
2. 在 `SmpEvents.cs` 中定义新事件类
3. 如果日志解析中有重复，移除日志解析逻辑
4. 更新本文档

### 示例：假设 SMP 未来支持聊天事件

```csharp
// 1. 注册通知处理器
_rpcHandler.RegisterNotificationHandler("players/chat", HandlePlayerChat);

// 2. 实现处理方法
private async void HandlePlayerChat(JsonRpcNotification notification)
{
    var data = notification.GetParams<ChatMessage>();
    await _eventBus.PublishAsync(new PlayerChatEvent 
    { 
        PlayerName = data.Player.Name,
        Message = data.Message 
    });
}

// 3. 移除 LogParser.cs 中的聊天解析
```

---

## 📚 相关文档

- [SMP API 文档](./SMP_API.md) - 完整的 SMP 接口说明
- [RCON 使用指南](./RCON.md) - RCON 命令执行
- [日志解析说明](./LOG_PARSING.md) - 日志解析规则
- [插件开发指南](./PLUGIN_GUIDE.md) - 如何订阅事件

---

## 总结

✅ **NetherGate 采用智能三层策略**，优先使用官方、可靠的 SMP 协议，在其不足时才退化到其他方案。

✅ **当前实现已完全符合此策略**，无重复监听，最大化可靠性。

✅ **未来可扩展**，当 Mojang 增强 SMP 功能时，可轻松迁移。

