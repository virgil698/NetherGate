# NetherGate 服务端管理协议（SMP）接口文档

本文档详细说明了 NetherGate 对 Minecraft 服务端管理协议（Server Management Protocol）的完整封装，供插件开发者使用。

---

## 📋 目录

- [概述](#概述)
- [三种技术的能力边界与组合使用](#三种技术的能力边界与组合使用)
- [核心接口 IServerApi](#核心接口-iserverapi)
- [数据传输对象 (DTOs)](#数据传输对象-dtos)
- [白名单管理](#白名单管理)
- [封禁玩家管理](#封禁玩家管理)
- [封禁IP管理](#封禁ip管理)
- [在线玩家管理](#在线玩家管理)
- [管理员管理](#管理员管理)
- [服务器状态管理](#服务器状态管理)
- [游戏规则管理](#游戏规则管理)
- [服务器设置管理](#服务器设置管理)
- [服务器事件（通知）](#服务器事件通知)
- [完整使用示例](#完整使用示例)

---

## 概述

服务端管理协议（SMP）是 Minecraft 1.21.9+ 版本引入的基于 WebSocket 和 JSON-RPC 2.0 的服务器管理接口，提供结构化的服务器管理能力。

NetherGate 将所有 SMP 方法封装为易用的 C# 接口，并通过事件系统实时推送服务器通知。

### 特性

✅ **完整功能覆盖** - 支持所有 SMP 方法  
✅ **强类型安全** - 所有参数和返回值都有明确类型  
✅ **异步设计** - 基于 async/await，性能优异  
✅ **事件驱动** - 服务器通知自动转为事件分发  
✅ **错误处理** - 完善的异常处理机制  

### SMP能力边界

**SMP专注于服务器管理，以下功能需要配合其他方式实现：**

| 功能需求 | SMP支持 | 替代方案 |
|---------|---------|---------|
| 白名单/封禁管理 | ✅ 支持 | - |
| 玩家列表/踢出 | ✅ 支持 | - |
| 游戏规则/服务器设置 | ✅ 支持 | - |
| **执行任意游戏命令** | ❌ 不支持 | **RCON** |
| **游戏内自定义命令** | ❌ 不支持 | **日志监听 + RCON** |
| **监听玩家聊天** | ❌ 不支持 | **日志监听** |
| **监听命令执行** | ❌ 不支持 | **日志监听** |
| **给予物品/传送玩家** | ❌ 不支持 | **RCON** |

> 💡 **提示**：NetherGate提供 **SMP + RCON + 日志监听** 三位一体的完整解决方案！  
> 详见：[RCON集成文档](./RCON_INTEGRATION.md)

---

## 三种技术的能力边界与组合使用

NetherGate 通过三种技术的协同工作，提供完整的服务器管理和游戏交互能力。

### 1. SMP（服务端管理协议）- 结构化管理

**核心定位：** 结构化的服务器管理，提供类型安全的API

#### ✅ 能做什么

| 功能类别 | 具体功能 | 返回数据 |
|---------|---------|---------|
| **白名单管理** | 查询/添加/移除/清空 | 结构化玩家列表 |
| **封禁管理** | 玩家封禁、IP封禁 | 带过期时间、原因等完整信息 |
| **玩家管理** | 获取在线列表、踢出玩家 | 玩家UUID、名称 |
| **管理员管理** | OP列表增删改查 | 带权限等级的管理员信息 |
| **服务器控制** | 状态查询、保存世界、停止服务器 | 服务器版本、协议号等 |
| **游戏规则** | 读取和修改所有游戏规则 | 类型化的规则值 |
| **实时通知** | WebSocket推送服务器事件 | 结构化事件数据 |

#### ❌ 不能做什么

- ❌ 执行任意游戏命令（如 `/give`、`/tp`）
- ❌ 监听玩家聊天内容
- ❌ 监听玩家执行的命令
- ❌ 给予物品、传送玩家
- ❌ 修改玩家背包、生命值
- ❌ 执行数据包函数

#### 💡 优势

```csharp
// ✅ 类型安全
var players = await Server.GetPlayersAsync();  // 返回 List<PlayerDto>
foreach (var player in players)
{
    Logger.Info($"{player.Name} - {player.Uuid}");  // 强类型访问
}

// ✅ 实时通知
Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, e => {
    // 玩家加入立即收到通知
});
```

---

### 2. RCON - 游戏命令执行

**核心定位：** 执行任意Minecraft游戏命令，提供灵活的游戏控制

#### ✅ 能做什么

| 功能类别 | 示例命令 | 说明 |
|---------|---------|------|
| **物品管理** | `/give`, `/clear` | 给予/清空物品 |
| **玩家传送** | `/tp`, `/teleport` | 传送玩家 |
| **效果管理** | `/effect` | 施加/清除药水效果 |
| **游戏模式** | `/gamemode` | 切换游戏模式 |
| **世界管理** | `/time`, `/weather` | 修改时间、天气 |
| **消息发送** | `/say`, `/tell`, `/tellraw` | 发送文本/富文本消息 |
| **函数执行** | `/function` | 执行数据包函数 |
| **任意命令** | 任何原版或MOD命令 | 完全灵活 |

#### ❌ 不能做什么

- ❌ 获取结构化数据（返回的是文本字符串）
- ❌ 实时事件通知
- ❌ 类型安全保证

#### 💡 优势

```csharp
// ✅ 灵活性极高
await Rcon.ExecuteCommandAsync("give Steve diamond 64");
await Rcon.ExecuteCommandAsync("tp Steve 0 100 0");
await Rcon.ExecuteCommandAsync("effect give Steve speed 60 2");

// ✅ 支持复杂的tellraw
var json = @"{""text"":""点击领取"",""clickEvent"":{""action"":""run_command"",""value"":""/claim""}}";
await Rcon.ExecuteCommandAsync($"tellraw Steve {json}");
```

---

### 3. 日志监听 - 事件捕获

**核心定位：** 监听服务器输出，捕获游戏内事件

#### ✅ 能做什么

| 事件类型 | 日志示例 | 捕获内容 |
|---------|---------|---------|
| **玩家命令** | `Steve issued server command: /tp 0 64 0` | 玩家名、命令内容 |
| **玩家聊天** | `<Steve> Hello World` | 玩家名、消息内容 |
| **玩家加入** | `Steve joined the game` | 玩家名 |
| **玩家离开** | `Steve left the game` | 玩家名、离开原因 |
| **服务器状态** | `Done (5.123s)!` | 启动完成时间 |
| **错误日志** | `[ERROR] ...` | 错误信息 |

#### ❌ 不能做什么

- ❌ 获取结构化数据（需要正则解析）
- ❌ 主动查询信息
- ❌ 修改游戏状态

#### 💡 优势

```csharp
// ✅ 捕获SMP无法捕获的事件
Server.SubscribeToServerLog(entry => {
    if (entry.Type == LogEntryType.PlayerChat)
    {
        // 捕获玩家聊天
        var match = Regex.Match(entry.Message, @"<(\w+)> (.+)");
        var player = match.Groups[1].Value;
        var message = match.Groups[2].Value;
        
        // 可以实现聊天过滤、关键词触发等功能
    }
});
```

---

### 组合使用模式

#### 模式1：游戏内自定义命令 ⭐

**场景：** 玩家在游戏中输入 `/myplugin give diamond`，插件给予钻石

```csharp
public class CustomCommandPlugin : PluginBase
{
    public override Task OnEnableAsync()
    {
        // 1️⃣ 日志监听 - 捕获命令
        Server.SubscribeToServerLog(OnServerLog);
        return Task.CompletedTask;
    }
    
    private async void OnServerLog(ServerLogEntry entry)
    {
        // 检测玩家命令
        var match = Regex.Match(entry.Message, 
            @"(\w+) issued server command: /myplugin (.+)");
        
        if (!match.Success) return;
        
        var playerName = match.Groups[1].Value;
        var args = match.Groups[2].Value.Split(' ');
        
        // 2️⃣ RCON - 执行游戏命令
        if (args[0] == "give" && args.Length >= 2)
        {
            await Rcon.ExecuteCommandAsync(
                $"give {playerName} {args[1]} 1");
            
            await Rcon.ExecuteCommandAsync(
                $"tell {playerName} §a已给予 {args[1]}");
        }
    }
}
```

**流程图：**
```
玩家输入: /myplugin give diamond
        ↓
日志监听器捕获（LogEntry）
        ↓
插件解析命令参数
        ↓
通过RCON执行: give Steve diamond 1
        ↓
玩家获得钻石 ✅
```

---

#### 模式2：VIP欢迎系统 ⭐

**场景：** VIP玩家加入时，发送欢迎消息并给予特权

```csharp
public class VipWelcome : PluginBase
{
    private HashSet<Guid> _vipList = new();
    
    public override Task OnEnableAsync()
    {
        // 1️⃣ SMP事件 - 监听玩家加入
        Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoined);
        return Task.CompletedTask;
    }
    
    private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
    {
        // 2️⃣ 检查VIP状态
        if (!_vipList.Contains(e.Player.Uuid)) return;
        
        Logger.Info($"VIP玩家 {e.Player.Name} 加入");
        
        // 3️⃣ SMP - 发送系统消息
        await Server.SendSystemMessageAsync(
            $"§6[VIP] §e欢迎尊贵的 {e.Player.Name}！");
        
        // 4️⃣ RCON - 给予游戏内特权
        if (Rcon?.IsConnected == true)
        {
            await Rcon.ExecuteCommandsAsync(
                $"effect give {e.Player.Name} speed 60 0",
                $"effect give {e.Player.Name} night_vision 300 0",
                $"give {e.Player.Name} diamond 5"
            );
        }
    }
}
```

**流程图：**
```
玩家加入服务器
        ↓
SMP推送PlayerJoinedEvent（结构化数据：UUID、Name）
        ↓
插件检查VIP列表
        ↓
通过SMP发送欢迎消息
        ↓
通过RCON给予速度、夜视、钻石
        ↓
VIP玩家获得特权 ✅
```

---

#### 模式3：聊天关键词触发 ⭐

**场景：** 玩家在聊天中输入 "help"，自动显示帮助菜单

```csharp
public class ChatTrigger : PluginBase
{
    public override Task OnEnableAsync()
    {
        // 1️⃣ 日志监听 - 捕获聊天
        Server.SubscribeToServerLog(OnServerLog);
        return Task.CompletedTask;
    }
    
    private async void OnServerLog(ServerLogEntry entry)
    {
        if (entry.Type != LogEntryType.PlayerChat) return;
        
        // 解析聊天消息
        var match = Regex.Match(entry.Message, @"<(\w+)> (.+)");
        if (!match.Success) return;
        
        var playerName = match.Groups[1].Value;
        var message = match.Groups[2].Value.ToLower();
        
        // 检测关键词
        if (message.Contains("help"))
        {
            // 2️⃣ 通过SMP获取玩家信息（可选）
            var players = await Server.GetPlayersAsync();
            var player = players.FirstOrDefault(p => p.Name == playerName);
            
            // 3️⃣ 通过RCON发送可点击的帮助菜单
            if (Rcon?.IsConnected == true)
            {
                var json = @"[
                    {""text"":""=== 帮助菜单 ==="",""color"":""gold""},
                    ""\n"",
                    {
                        ""text"":""[查看规则]"",
                        ""color"":""green"",
                        ""clickEvent"":{""action"":""run_command"",""value"":""/rules""}
                    }
                ]";
                
                await Rcon.ExecuteCommandAsync($"tellraw {playerName} {json}");
            }
        }
    }
}
```

**流程图：**
```
玩家聊天: "I need help"
        ↓
日志监听捕获聊天内容
        ↓
插件检测到关键词 "help"
        ↓
（可选）通过SMP获取玩家完整信息
        ↓
通过RCON发送可点击菜单（tellraw）
        ↓
玩家点击菜单项 ✅
```

---

#### 模式4：完整的经济系统 ⭐⭐⭐

**场景：** 结合三种技术实现完整的游戏内经济

```csharp
public class EconomySystem : PluginBase
{
    private Dictionary<Guid, decimal> _balances = new();
    
    public override Task OnEnableAsync()
    {
        // 1️⃣ SMP事件 - 玩家加入时加载余额
        Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoined);
        
        // 2️⃣ 日志监听 - 捕获交易命令
        Server.SubscribeToServerLog(OnServerLog);
        
        return Task.CompletedTask;
    }
    
    private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
    {
        // SMP提供结构化的玩家数据
        if (!_balances.ContainsKey(e.Player.Uuid))
        {
            _balances[e.Player.Uuid] = 1000m; // 新玩家初始金额
        }
        
        // RCON发送余额提示
        if (Rcon?.IsConnected == true)
        {
            await Rcon.ExecuteCommandAsync(
                $"tell {e.Player.Name} §a你的余额: §e${_balances[e.Player.Uuid]}");
        }
    }
    
    private async void OnServerLog(ServerLogEntry entry)
    {
        // 日志监听捕获交易命令
        var match = Regex.Match(entry.Message, 
            @"(\w+) issued server command: /money pay (\w+) (\d+)");
        
        if (!match.Success) return;
        
        var fromName = match.Groups[1].Value;
        var toName = match.Groups[2].Value;
        var amount = decimal.Parse(match.Groups[3].Value);
        
        // SMP获取玩家UUID（结构化数据）
        var players = await Server.GetPlayersAsync();
        var fromPlayer = players.FirstOrDefault(p => p.Name == fromName);
        var toPlayer = players.FirstOrDefault(p => p.Name == toName);
        
        if (fromPlayer == null || toPlayer == null) return;
        
        // 检查余额
        if (_balances[fromPlayer.Uuid] < amount)
        {
            // RCON发送错误提示
            if (Rcon?.IsConnected == true)
            {
                await Rcon.ExecuteCommandAsync(
                    $"tell {fromName} §c余额不足");
            }
            return;
        }
        
        // 执行转账
        _balances[fromPlayer.Uuid] -= amount;
        _balances[toPlayer.Uuid] = _balances.GetValueOrDefault(toPlayer.Uuid) + amount;
        
        // RCON通知双方
        if (Rcon?.IsConnected == true)
        {
            await Rcon.ExecuteCommandsAsync(
                $"tell {fromName} §a成功转账 ${amount} 给 {toName}，余额: §e${_balances[fromPlayer.Uuid]}",
                $"tell {toName} §a收到来自 {fromName} 的 ${amount}，余额: §e${_balances[toPlayer.Uuid]}"
            );
        }
        
        Logger.Info($"转账: {fromName}({fromPlayer.Uuid}) -> {toName}({toPlayer.Uuid}): ${amount}");
    }
}
```

**流程图：**
```
玩家输入: /money pay Alex 100
        ↓
日志监听捕获命令
        ↓
通过SMP获取双方UUID（结构化）
        ↓
检查余额、执行转账
        ↓
通过RCON通知双方
        ↓
交易完成 ✅
```

---

### 技术选择指南

#### 何时使用 SMP？

✅ **管理类操作**
- 白名单、封禁、OP管理
- 需要获取结构化数据（玩家列表、游戏规则等）
- 需要实时事件通知（玩家加入/离开等）

```csharp
// 推荐：使用SMP的结构化API
var players = await Server.GetPlayersAsync();
await Server.AddToAllowlistAsync(player);
```

#### 何时使用 RCON？

✅ **游戏内操作**
- 给予物品、传送玩家
- 施加效果、改变游戏模式
- 发送富文本消息（tellraw）
- 执行数据包函数
- **实现游戏内命令**

```csharp
// 推荐：使用RCON执行游戏命令
await Rcon.ExecuteCommandAsync("give Steve diamond 10");
await Rcon.ExecuteCommandAsync("tp Steve 0 100 0");
```

#### 何时使用日志监听？

✅ **事件捕获**
- 监听玩家聊天内容
- 捕获玩家执行的命令
- 实现聊天过滤、关键词触发
- **捕获游戏内自定义命令**

```csharp
// 推荐：监听日志捕获事件
Server.SubscribeToServerLog(entry => {
    if (entry.Type == LogEntryType.PlayerChat)
    {
        // 处理聊天消息
    }
});
```

---

### 最佳实践总结

#### 1. 优先使用 SMP

对于SMP支持的功能，始终优先使用SMP：

```csharp
// ✅ 好：使用SMP
await Server.AddToAllowlistAsync(player);
await Server.GetPlayersAsync();

// ❌ 不好：使用RCON
await Rcon.ExecuteCommandAsync($"whitelist add {playerName}");
```

**原因：**
- 返回结构化数据，易于处理
- 类型安全，编译时检查
- 错误处理更完善

#### 2. RCON 专注游戏操作

RCON用于SMP不支持的游戏内操作：

```csharp
// ✅ 适合用RCON
await Rcon.ExecuteCommandAsync($"give {player} diamond 10");
await Rcon.ExecuteCommandAsync($"effect give {player} speed 60 1");
```

#### 3. 日志监听捕获事件

日志监听用于SMP无法提供的事件捕获：

```csharp
// ✅ 适合用日志监听
Server.SubscribeToServerLog(entry => {
    if (entry.Message.Contains("issued server command: /mycommand"))
    {
        // 处理自定义命令
    }
});
```

#### 4. 组合使用发挥最大威力

```csharp
// 🌟 三位一体的完美组合
public override Task OnEnableAsync()
{
    // SMP: 监听结构化事件
    Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoined);
    
    // 日志监听: 捕获自定义命令
    Server.SubscribeToServerLog(OnServerLog);
    
    return Task.CompletedTask;
}

private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
{
    // SMP: 获取结构化玩家数据
    Logger.Info($"玩家 {e.Player.Name} (UUID: {e.Player.Uuid}) 加入");
    
    // RCON: 执行游戏内欢迎
    if (Rcon?.IsConnected == true)
    {
        await Rcon.ExecuteCommandAsync($"give {e.Player.Name} diamond 1");
    }
}
```

---

## 核心接口 IServerApi

所有插件通过 `Server` 属性访问此接口：

```csharp
public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        // 通过 Server 属性访问所有 SMP 功能
        var players = await Server.GetPlayersAsync();
        await Server.SendSystemMessageAsync("插件已启用!");
    }
}
```

完整接口定义：

```csharp
namespace NetherGate.API
{
    /// <summary>
    /// 服务器 API 接口，封装所有服务端管理协议功能
    /// </summary>
    public interface IServerApi
    {
        #region 白名单管理
        
        /// <summary>
        /// 获取白名单列表
        /// </summary>
        /// <returns>白名单中的所有玩家</returns>
        Task<List<PlayerDto>> GetAllowlistAsync();
        
        /// <summary>
        /// 设置白名单（完全替换）
        /// </summary>
        /// <param name="players">新的白名单列表</param>
        Task SetAllowlistAsync(List<PlayerDto> players);
        
        /// <summary>
        /// 添加玩家到白名单
        /// </summary>
        /// <param name="player">要添加的玩家</param>
        /// <returns>是否添加成功</returns>
        Task<bool> AddToAllowlistAsync(PlayerDto player);
        
        /// <summary>
        /// 从白名单移除玩家
        /// </summary>
        /// <param name="player">要移除的玩家</param>
        /// <returns>是否移除成功</returns>
        Task<bool> RemoveFromAllowlistAsync(PlayerDto player);
        
        /// <summary>
        /// 清空白名单
        /// </summary>
        Task ClearAllowlistAsync();
        
        #endregion
        
        #region 封禁玩家管理
        
        /// <summary>
        /// 获取封禁玩家列表
        /// </summary>
        /// <returns>所有被封禁的玩家</returns>
        Task<List<UserBanDto>> GetBansAsync();
        
        /// <summary>
        /// 设置封禁列表（完全替换）
        /// </summary>
        /// <param name="bans">新的封禁列表</param>
        Task SetBansAsync(List<UserBanDto> bans);
        
        /// <summary>
        /// 封禁玩家
        /// </summary>
        /// <param name="ban">封禁信息</param>
        /// <returns>是否封禁成功</returns>
        Task<bool> AddBanAsync(UserBanDto ban);
        
        /// <summary>
        /// 解封玩家
        /// </summary>
        /// <param name="player">要解封的玩家</param>
        /// <returns>是否解封成功</returns>
        Task<bool> RemoveBanAsync(PlayerDto player);
        
        /// <summary>
        /// 清空封禁列表
        /// </summary>
        Task ClearBansAsync();
        
        #endregion
        
        #region 封禁IP管理
        
        /// <summary>
        /// 获取封禁IP列表
        /// </summary>
        /// <returns>所有被封禁的IP</returns>
        Task<List<IpBanDto>> GetIpBansAsync();
        
        /// <summary>
        /// 设置IP封禁列表（完全替换）
        /// </summary>
        /// <param name="ipBans">新的IP封禁列表</param>
        Task SetIpBansAsync(List<IpBanDto> ipBans);
        
        /// <summary>
        /// 封禁IP
        /// </summary>
        /// <param name="ipBan">IP封禁信息</param>
        /// <returns>是否封禁成功</returns>
        Task<bool> AddIpBanAsync(IpBanDto ipBan);
        
        /// <summary>
        /// 解封IP
        /// </summary>
        /// <param name="ip">要解封的IP地址</param>
        /// <returns>是否解封成功</returns>
        Task<bool> RemoveIpBanAsync(string ip);
        
        /// <summary>
        /// 清空IP封禁列表
        /// </summary>
        Task ClearIpBansAsync();
        
        #endregion
        
        #region 在线玩家管理
        
        /// <summary>
        /// 获取在线玩家列表
        /// </summary>
        /// <returns>当前在线的所有玩家</returns>
        Task<List<PlayerDto>> GetPlayersAsync();
        
        /// <summary>
        /// 踢出玩家
        /// </summary>
        /// <param name="player">要踢出的玩家</param>
        /// <param name="reason">踢出原因（可选）</param>
        /// <returns>是否踢出成功</returns>
        Task<bool> KickPlayerAsync(PlayerDto player, string? reason = null);
        
        #endregion
        
        #region 管理员管理
        
        /// <summary>
        /// 获取管理员列表
        /// </summary>
        /// <returns>所有管理员</returns>
        Task<List<OperatorDto>> GetOperatorsAsync();
        
        /// <summary>
        /// 设置管理员列表（完全替换）
        /// </summary>
        /// <param name="operators">新的管理员列表</param>
        Task SetOperatorsAsync(List<OperatorDto> operators);
        
        /// <summary>
        /// 添加管理员
        /// </summary>
        /// <param name="op">管理员信息</param>
        /// <returns>是否添加成功</returns>
        Task<bool> AddOperatorAsync(OperatorDto op);
        
        /// <summary>
        /// 移除管理员
        /// </summary>
        /// <param name="player">要移除的管理员</param>
        /// <returns>是否移除成功</returns>
        Task<bool> RemoveOperatorAsync(PlayerDto player);
        
        /// <summary>
        /// 清空管理员列表
        /// </summary>
        Task ClearOperatorsAsync();
        
        #endregion
        
        #region 服务器状态管理
        
        /// <summary>
        /// 获取服务器状态
        /// </summary>
        /// <returns>服务器状态信息</returns>
        Task<ServerState> GetStatusAsync();
        
        /// <summary>
        /// 保存世界
        /// </summary>
        Task SaveWorldAsync();
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        Task StopServerAsync();
        
        /// <summary>
        /// 发送系统消息
        /// </summary>
        /// <param name="message">要发送的消息</param>
        Task SendSystemMessageAsync(string message);
        
        #endregion
        
        #region 游戏规则管理
        
        /// <summary>
        /// 获取所有游戏规则
        /// </summary>
        /// <returns>游戏规则字典</returns>
        Task<Dictionary<string, TypedRule>> GetGameRulesAsync();
        
        /// <summary>
        /// 更新游戏规则
        /// </summary>
        /// <param name="rule">规则名称</param>
        /// <param name="value">规则值</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateGameRuleAsync(string rule, object value);
        
        #endregion
        
        #region 服务器设置管理
        
        /// <summary>
        /// 获取服务器设置
        /// </summary>
        /// <typeparam name="T">设置值的类型</typeparam>
        /// <param name="settingName">设置名称</param>
        /// <returns>设置值</returns>
        Task<T> GetServerSettingAsync<T>(string settingName);
        
        /// <summary>
        /// 设置服务器设置
        /// </summary>
        /// <param name="settingName">设置名称</param>
        /// <param name="value">设置值</param>
        /// <returns>是否设置成功</returns>
        Task<bool> SetServerSettingAsync(string settingName, object value);
        
        #endregion
    }
}
```

---

## 数据传输对象 (DTOs)

所有与 SMP 交互的数据结构。

### PlayerDto - 玩家信息

```csharp
namespace NetherGate.API.Protocol
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    public record PlayerDto
    {
        /// <summary>
        /// 玩家名称
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        
        /// <summary>
        /// 玩家 UUID
        /// </summary>
        [JsonPropertyName("uuid")]
        public required Guid Uuid { get; init; }
    }
}
```

**使用示例：**

```csharp
var player = new PlayerDto
{
    Name = "Steve",
    Uuid = Guid.Parse("069a79f4-44e9-4726-a5be-fca90e38aaf5")
};

await Server.AddToAllowlistAsync(player);
```

---

### UserBanDto - 玩家封禁信息

```csharp
namespace NetherGate.API.Protocol
{
    /// <summary>
    /// 玩家封禁信息
    /// </summary>
    public record UserBanDto
    {
        /// <summary>
        /// 被封禁的玩家
        /// </summary>
        [JsonPropertyName("player")]
        public required PlayerDto Player { get; init; }
        
        /// <summary>
        /// 封禁原因
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; init; }
        
        /// <summary>
        /// 封禁过期时间（null 表示永久）
        /// </summary>
        [JsonPropertyName("expires")]
        public DateTime? Expires { get; init; }
        
        /// <summary>
        /// 封禁来源（执行封禁的管理员或系统）
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; init; }
    }
}
```

**使用示例：**

```csharp
// 永久封禁
var permanentBan = new UserBanDto
{
    Player = new PlayerDto { Name = "Griefer", Uuid = someUuid },
    Reason = "破坏服务器",
    Source = "Admin"
};
await Server.AddBanAsync(permanentBan);

// 临时封禁（7天）
var temporaryBan = new UserBanDto
{
    Player = new PlayerDto { Name = "BadBehavior", Uuid = someUuid },
    Reason = "违反规则",
    Expires = DateTime.UtcNow.AddDays(7),
    Source = "AutoMod"
};
await Server.AddBanAsync(temporaryBan);
```

---

### IpBanDto - IP封禁信息

```csharp
namespace NetherGate.API.Protocol
{
    /// <summary>
    /// IP 封禁信息
    /// </summary>
    public record IpBanDto
    {
        /// <summary>
        /// 被封禁的 IP 地址
        /// </summary>
        [JsonPropertyName("ip")]
        public required string Ip { get; init; }
        
        /// <summary>
        /// 封禁原因
        /// </summary>
        [JsonPropertyName("reason")]
        public string? Reason { get; init; }
        
        /// <summary>
        /// 封禁过期时间（null 表示永久）
        /// </summary>
        [JsonPropertyName("expires")]
        public DateTime? Expires { get; init; }
        
        /// <summary>
        /// 封禁来源
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; init; }
    }
}
```

**使用示例：**

```csharp
var ipBan = new IpBanDto
{
    Ip = "192.168.1.100",
    Reason = "多次违规",
    Expires = DateTime.UtcNow.AddHours(24),
    Source = "AntiCheat"
};
await Server.AddIpBanAsync(ipBan);
```

---

### OperatorDto - 管理员信息

```csharp
namespace NetherGate.API.Protocol
{
    /// <summary>
    /// 管理员信息
    /// </summary>
    public record OperatorDto
    {
        /// <summary>
        /// 管理员玩家信息
        /// </summary>
        [JsonPropertyName("player")]
        public required PlayerDto Player { get; init; }
        
        /// <summary>
        /// 管理员等级（1-4，4 为最高）
        /// </summary>
        [JsonPropertyName("level")]
        public int Level { get; init; } = 4;
        
        /// <summary>
        /// 是否可以绕过玩家数量限制
        /// </summary>
        [JsonPropertyName("bypassPlayerLimit")]
        public bool BypassPlayerLimit { get; init; }
    }
}
```

**管理员等级说明：**

| 等级 | 权限描述 |
|------|----------|
| 1    | 可以绕过出生点保护 |
| 2    | 可以使用作弊命令（/give, /tp 等） |
| 3    | 可以使用管理命令（/ban, /kick 等） |
| 4    | 可以使用所有命令（/stop, /op 等） |

**使用示例：**

```csharp
var op = new OperatorDto
{
    Player = new PlayerDto { Name = "Admin", Uuid = adminUuid },
    Level = 4,
    BypassPlayerLimit = true
};
await Server.AddOperatorAsync(op);
```

---

### ServerState - 服务器状态

```csharp
namespace NetherGate.API.Protocol
{
    /// <summary>
    /// 服务器状态
    /// </summary>
    public record ServerState
    {
        /// <summary>
        /// 服务器是否已启动
        /// </summary>
        [JsonPropertyName("started")]
        public bool Started { get; init; }
        
        /// <summary>
        /// 服务器版本信息
        /// </summary>
        [JsonPropertyName("version")]
        public required VersionInfo Version { get; init; }
    }
    
    /// <summary>
    /// 版本信息
    /// </summary>
    public record VersionInfo
    {
        /// <summary>
        /// 版本名称（如 "1.21.9"）
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        
        /// <summary>
        /// 协议版本号
        /// </summary>
        [JsonPropertyName("protocol")]
        public int Protocol { get; init; }
    }
}
```

**使用示例：**

```csharp
var state = await Server.GetStatusAsync();
Logger.Info($"服务器版本: {state.Version.Name}");
Logger.Info($"协议版本: {state.Version.Protocol}");
Logger.Info($"运行状态: {(state.Started ? "运行中" : "已停止")}");
```

---

### TypedRule - 游戏规则

```csharp
namespace NetherGate.API.Protocol
{
    /// <summary>
    /// 游戏规则
    /// </summary>
    public record TypedRule
    {
        /// <summary>
        /// 规则类型（"boolean", "integer" 等）
        /// </summary>
        [JsonPropertyName("type")]
        public required string Type { get; init; }
        
        /// <summary>
        /// 规则值
        /// </summary>
        [JsonPropertyName("value")]
        public required object Value { get; init; }
    }
}
```

**使用示例：**

```csharp
var rules = await Server.GetGameRulesAsync();

// 读取规则
if (rules.TryGetValue("keepInventory", out var keepInvRule))
{
    bool keepInventory = (bool)keepInvRule.Value;
    Logger.Info($"死亡保留物品: {keepInventory}");
}

// 修改规则
await Server.UpdateGameRuleAsync("keepInventory", true);
await Server.UpdateGameRuleAsync("randomTickSpeed", 10);
```

---

## 白名单管理

### 获取白名单

```csharp
public override async Task OnEnableAsync()
{
    var allowlist = await Server.GetAllowlistAsync();
    
    Logger.Info($"白名单中有 {allowlist.Count} 个玩家:");
    foreach (var player in allowlist)
    {
        Logger.Info($"  - {player.Name} ({player.Uuid})");
    }
}
```

### 添加/移除玩家

```csharp
// 添加到白名单
var newPlayer = new PlayerDto
{
    Name = "NewPlayer",
    Uuid = Guid.Parse("069a79f4-44e9-4726-a5be-fca90e38aaf5")
};

if (await Server.AddToAllowlistAsync(newPlayer))
{
    Logger.Info($"已添加 {newPlayer.Name} 到白名单");
}

// 从白名单移除
if (await Server.RemoveFromAllowlistAsync(newPlayer))
{
    Logger.Info($"已从白名单移除 {newPlayer.Name}");
}
```

### 批量设置白名单

```csharp
// 完全替换白名单
var newAllowlist = new List<PlayerDto>
{
    new() { Name = "Player1", Uuid = uuid1 },
    new() { Name = "Player2", Uuid = uuid2 },
    new() { Name = "Player3", Uuid = uuid3 }
};

await Server.SetAllowlistAsync(newAllowlist);
Logger.Info("白名单已更新");
```

### 清空白名单

```csharp
await Server.ClearAllowlistAsync();
Logger.Info("白名单已清空");
```

---

## 封禁玩家管理

### 查看封禁列表

```csharp
var bans = await Server.GetBansAsync();

foreach (var ban in bans)
{
    Logger.Info($"封禁玩家: {ban.Player.Name}");
    Logger.Info($"  原因: {ban.Reason ?? "无"}");
    Logger.Info($"  来源: {ban.Source ?? "未知"}");
    
    if (ban.Expires.HasValue)
    {
        var remaining = ban.Expires.Value - DateTime.UtcNow;
        Logger.Info($"  剩余时间: {remaining.TotalHours:F1} 小时");
    }
    else
    {
        Logger.Info($"  类型: 永久封禁");
    }
}
```

### 封禁玩家

```csharp
// 永久封禁
var ban = new UserBanDto
{
    Player = targetPlayer,
    Reason = "严重违规",
    Source = Metadata.Name  // 使用插件名称作为来源
};
await Server.AddBanAsync(ban);

// 临时封禁（3天）
var tempBan = new UserBanDto
{
    Player = targetPlayer,
    Reason = "首次警告",
    Expires = DateTime.UtcNow.AddDays(3),
    Source = Metadata.Name
};
await Server.AddBanAsync(tempBan);
```

### 解封玩家

```csharp
await Server.RemoveBanAsync(targetPlayer);
Logger.Info($"已解封 {targetPlayer.Name}");
```

---

## 封禁IP管理

### 查看IP封禁列表

```csharp
var ipBans = await Server.GetIpBansAsync();

foreach (var ipBan in ipBans)
{
    Logger.Info($"封禁IP: {ipBan.Ip}");
    Logger.Info($"  原因: {ipBan.Reason ?? "无"}");
    
    if (ipBan.Expires.HasValue)
    {
        Logger.Info($"  过期时间: {ipBan.Expires.Value:yyyy-MM-dd HH:mm:ss}");
    }
}
```

### 封禁IP

```csharp
var ipBan = new IpBanDto
{
    Ip = "192.168.1.100",
    Reason = "恶意攻击",
    Expires = DateTime.UtcNow.AddDays(7),  // 7天后过期
    Source = "SecuritySystem"
};

if (await Server.AddIpBanAsync(ipBan))
{
    Logger.Info($"已封禁IP: {ipBan.Ip}");
}
```

### 解封IP

```csharp
if (await Server.RemoveIpBanAsync("192.168.1.100"))
{
    Logger.Info("IP已解封");
}
```

---

## 在线玩家管理

### 获取在线玩家

```csharp
var players = await Server.GetPlayersAsync();

Logger.Info($"当前在线玩家数: {players.Count}");
foreach (var player in players)
{
    Logger.Info($"  - {player.Name}");
}
```

### 踢出玩家

```csharp
// 无理由踢出
await Server.KickPlayerAsync(targetPlayer);

// 带理由踢出
await Server.KickPlayerAsync(targetPlayer, "AFK时间过长");
```

### 实用示例：AFK检测

```csharp
public class AfkKicker : PluginBase
{
    private Dictionary<Guid, DateTime> _lastActivity = new();
    private const int AfkTimeoutMinutes = 10;
    
    public override async Task OnEnableAsync()
    {
        // 启动定时检查
        _ = Task.Run(CheckAfkPlayersAsync);
    }
    
    private async Task CheckAfkPlayersAsync()
    {
        while (State == PluginState.Enabled)
        {
            var players = await Server.GetPlayersAsync();
            var now = DateTime.UtcNow;
            
            foreach (var player in players)
            {
                if (_lastActivity.TryGetValue(player.Uuid, out var lastTime))
                {
                    var afkDuration = now - lastTime;
                    
                    if (afkDuration.TotalMinutes >= AfkTimeoutMinutes)
                    {
                        await Server.KickPlayerAsync(
                            player, 
                            $"AFK超过{AfkTimeoutMinutes}分钟");
                        
                        _lastActivity.Remove(player.Uuid);
                    }
                }
                else
                {
                    _lastActivity[player.Uuid] = now;
                }
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
    
    // 监听玩家活动事件来更新 _lastActivity
}
```

---

## 管理员管理

### 获取管理员列表

```csharp
var operators = await Server.GetOperatorsAsync();

foreach (var op in operators)
{
    Logger.Info($"管理员: {op.Player.Name}");
    Logger.Info($"  等级: {op.Level}");
    Logger.Info($"  可绕过玩家限制: {op.BypassPlayerLimit}");
}
```

### 添加管理员

```csharp
var newOp = new OperatorDto
{
    Player = targetPlayer,
    Level = 3,  // 管理员等级
    BypassPlayerLimit = false
};

if (await Server.AddOperatorAsync(newOp))
{
    Logger.Info($"已授予 {targetPlayer.Name} 管理员权限");
}
```

### 移除管理员

```csharp
if (await Server.RemoveOperatorAsync(targetPlayer))
{
    Logger.Info($"已移除 {targetPlayer.Name} 的管理员权限");
}
```

---

## 服务器状态管理

### 获取服务器信息

```csharp
var state = await Server.GetStatusAsync();

Logger.Info($"服务器状态: {(state.Started ? "运行中" : "已停止")}");
Logger.Info($"Minecraft 版本: {state.Version.Name}");
Logger.Info($"协议版本: {state.Version.Protocol}");
```

### 保存世界

```csharp
Logger.Info("开始保存世界...");
await Server.SaveWorldAsync();
Logger.Info("世界保存完成!");
```

### 发送系统消息

```csharp
// 向所有玩家发送消息
await Server.SendSystemMessageAsync("服务器将在5分钟后重启");

// 发送欢迎消息
await Server.SendSystemMessageAsync($"欢迎 {player.Name} 加入服务器!");

// 发送广播
await Server.SendSystemMessageAsync("§6[公告] §f今日签到活动开始!");
```

### 停止服务器

```csharp
// 警告：这会停止整个服务器
Logger.Warning("正在停止服务器...");
await Server.StopServerAsync();
```

### 实用示例：定时保存

```csharp
public class AutoSave : PluginBase
{
    private Timer? _saveTimer;
    
    public override Task OnEnableAsync()
    {
        // 每30分钟自动保存
        _saveTimer = new Timer(async _ =>
        {
            Logger.Info("执行自动保存...");
            await Server.SendSystemMessageAsync("§e[自动保存] 正在保存世界...");
            await Server.SaveWorldAsync();
            await Server.SendSystemMessageAsync("§a[自动保存] 保存完成!");
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        
        return Task.CompletedTask;
    }
    
    public override Task OnDisableAsync()
    {
        _saveTimer?.Dispose();
        return Task.CompletedTask;
    }
}
```

---

## 游戏规则管理

### 获取所有游戏规则

```csharp
var rules = await Server.GetGameRulesAsync();

foreach (var (name, rule) in rules)
{
    Logger.Info($"{name} ({rule.Type}): {rule.Value}");
}
```

### 修改游戏规则

```csharp
// 布尔类型规则
await Server.UpdateGameRuleAsync("keepInventory", true);
await Server.UpdateGameRuleAsync("mobGriefing", false);
await Server.UpdateGameRuleAsync("doDaylightCycle", false);

// 整数类型规则
await Server.UpdateGameRuleAsync("randomTickSpeed", 10);
await Server.UpdateGameRuleAsync("maxEntityCramming", 50);
await Server.UpdateGameRuleAsync("spawnRadius", 20);
```

### 常用游戏规则

| 规则名称 | 类型 | 默认值 | 说明 |
|---------|------|--------|------|
| `keepInventory` | boolean | false | 死亡保留物品 |
| `mobGriefing` | boolean | true | 生物是否破坏方块 |
| `doDaylightCycle` | boolean | true | 日夜交替 |
| `doWeatherCycle` | boolean | true | 天气变化 |
| `randomTickSpeed` | integer | 3 | 随机刻速度 |
| `maxEntityCramming` | integer | 24 | 最大实体挤压数 |
| `spawnRadius` | integer | 10 | 出生点半径 |
| `commandBlockOutput` | boolean | true | 命令方块输出 |

### 实用示例：游戏模式切换

```csharp
public class GameModeManager : PluginBase
{
    public async Task SetCreativeModeAsync()
    {
        await Server.UpdateGameRuleAsync("keepInventory", true);
        await Server.UpdateGameRuleAsync("mobGriefing", false);
        await Server.SendSystemMessageAsync("§6已切换到创造模式设置");
    }
    
    public async Task SetSurvivalModeAsync()
    {
        await Server.UpdateGameRuleAsync("keepInventory", false);
        await Server.UpdateGameRuleAsync("mobGriefing", true);
        await Server.SendSystemMessageAsync("§6已切换到生存模式设置");
    }
    
    public async Task SetPvpModeAsync()
    {
        await Server.UpdateGameRuleAsync("keepInventory", false);
        await Server.UpdateGameRuleAsync("naturalRegeneration", false);
        await Server.SendSystemMessageAsync("§c已切换到PVP模式");
    }
}
```

---

## 服务器设置管理

### 读取服务器设置

```csharp
// 读取各种服务器设置
var difficulty = await Server.GetServerSettingAsync<string>("difficulty");
var pvpEnabled = await Server.GetServerSettingAsync<bool>("pvp");
var viewDistance = await Server.GetServerSettingAsync<int>("view-distance");
var motd = await Server.GetServerSettingAsync<string>("motd");

Logger.Info($"难度: {difficulty}");
Logger.Info($"PVP: {pvpEnabled}");
Logger.Info($"视距: {viewDistance}");
Logger.Info($"MOTD: {motd}");
```

### 修改服务器设置

```csharp
// 修改设置
await Server.SetServerSettingAsync("pvp", true);
await Server.SetServerSettingAsync("difficulty", "hard");
await Server.SetServerSettingAsync("view-distance", 12);

Logger.Info("服务器设置已更新");
```

### 常用服务器设置

| 设置名称 | 类型 | 说明 |
|---------|------|------|
| `difficulty` | string | 难度（peaceful/easy/normal/hard） |
| `pvp` | boolean | 是否允许PVP |
| `view-distance` | integer | 视距（区块） |
| `max-players` | integer | 最大玩家数 |
| `white-list` | boolean | 是否启用白名单 |
| `spawn-protection` | integer | 出生点保护范围 |
| `motd` | string | 服务器描述 |

**注意：** 某些设置可能需要重启服务器才能生效。

---

## 服务器事件（通知）

SMP 通过 WebSocket 推送实时通知，NetherGate 将这些通知转换为事件，插件通过事件系统订阅。

### 服务器状态事件

```csharp
public override Task OnEnableAsync()
{
    // 服务器启动
    Events.Subscribe<ServerStartedEvent>(
        this, 
        EventPriority.Normal, 
        OnServerStarted);
    
    // 服务器状态变化（心跳）
    Events.Subscribe<ServerStatusChangedEvent>(
        this, 
        EventPriority.Normal, 
        OnServerStatusChanged);
    
    return Task.CompletedTask;
}

private async Task OnServerStarted(object? sender, ServerStartedEvent e)
{
    Logger.Info($"服务器已启动: {e.State.Version.Name}");
}

private async Task OnServerStatusChanged(object? sender, ServerStatusChangedEvent e)
{
    Logger.Debug("服务器状态更新（心跳）");
}
```

---

### 玩家事件

```csharp
public override Task OnEnableAsync()
{
    // 玩家加入
    Events.Subscribe<PlayerJoinedEvent>(
        this, 
        EventPriority.Normal, 
        OnPlayerJoined);
    
    // 玩家离开
    Events.Subscribe<PlayerLeftEvent>(
        this, 
        EventPriority.Normal, 
        OnPlayerLeft);
    
    return Task.CompletedTask;
}

private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
{
    Logger.Info($"玩家 {e.Player.Name} 加入了服务器");
    
    // 发送欢迎消息
    await Server.SendSystemMessageAsync($"欢迎 {e.Player.Name}!");
    
    // 检查是否是VIP
    if (IsVip(e.Player))
    {
        await Server.SendSystemMessageAsync($"§6VIP玩家 {e.Player.Name} 加入了服务器!");
    }
}

private async Task OnPlayerLeft(object? sender, PlayerLeftEvent e)
{
    Logger.Info($"玩家 {e.Player.Name} 离开了服务器");
}
```

---

### 管理员事件

```csharp
public override Task OnEnableAsync()
{
    // 管理员添加
    Events.Subscribe<OperatorAddedEvent>(
        this, 
        EventPriority.Normal, 
        OnOperatorAdded);
    
    // 管理员移除
    Events.Subscribe<OperatorRemovedEvent>(
        this, 
        EventPriority.Normal, 
        OnOperatorRemoved);
    
    return Task.CompletedTask;
}

private async Task OnOperatorAdded(object? sender, OperatorAddedEvent e)
{
    Logger.Info($"{e.Operator.Player.Name} 被授予了管理员权限（等级 {e.Operator.Level}）");
    
    // 记录到日志文件
    await LogOperatorChangeAsync("ADD", e.Operator);
}

private async Task OnOperatorRemoved(object? sender, OperatorRemovedEvent e)
{
    Logger.Info($"{e.Player.Name} 的管理员权限被移除");
    
    // 记录到日志文件
    await LogOperatorChangeAsync("REMOVE", e.Player);
}
```

---

### 白名单事件

```csharp
public override Task OnEnableAsync()
{
    Events.Subscribe<AllowlistAddedEvent>(
        this, 
        EventPriority.Normal, 
        OnAllowlistAdded);
    
    Events.Subscribe<AllowlistRemovedEvent>(
        this, 
        EventPriority.Normal, 
        OnAllowlistRemoved);
    
    return Task.CompletedTask;
}

private async Task OnAllowlistAdded(object? sender, AllowlistAddedEvent e)
{
    Logger.Info($"{e.Player.Name} 被添加到白名单");
}

private async Task OnAllowlistRemoved(object? sender, AllowlistRemovedEvent e)
{
    Logger.Info($"{e.Player.Name} 从白名单中移除");
}
```

---

### 封禁事件

```csharp
public override Task OnEnableAsync()
{
    // 玩家封禁
    Events.Subscribe<BanAddedEvent>(
        this, 
        EventPriority.Normal, 
        OnBanAdded);
    
    Events.Subscribe<BanRemovedEvent>(
        this, 
        EventPriority.Normal, 
        OnBanRemoved);
    
    // IP封禁
    Events.Subscribe<IpBanAddedEvent>(
        this, 
        EventPriority.Normal, 
        OnIpBanAdded);
    
    Events.Subscribe<IpBanRemovedEvent>(
        this, 
        EventPriority.Normal, 
        OnIpBanRemoved);
    
    return Task.CompletedTask;
}

private async Task OnBanAdded(object? sender, BanAddedEvent e)
{
    var banType = e.Ban.Expires.HasValue ? "临时" : "永久";
    Logger.Info($"{e.Ban.Player.Name} 被{banType}封禁，原因: {e.Ban.Reason}");
}

private async Task OnIpBanAdded(object? sender, IpBanAddedEvent e)
{
    Logger.Info($"IP {e.IpBan.Ip} 被封禁，原因: {e.IpBan.Reason}");
}
```

---

### 游戏规则事件

```csharp
public override Task OnEnableAsync()
{
    Events.Subscribe<GameRuleUpdatedEvent>(
        this, 
        EventPriority.Normal, 
        OnGameRuleUpdated);
    
    return Task.CompletedTask;
}

private async Task OnGameRuleUpdated(object? sender, GameRuleUpdatedEvent e)
{
    Logger.Info($"游戏规则 {e.RuleName} 已更新为: {e.NewValue.Value}");
    
    // 特殊处理某些规则
    if (e.RuleName == "keepInventory" && (bool)e.NewValue.Value == true)
    {
        await Server.SendSystemMessageAsync("§e死亡保留物品已开启!");
    }
}
```

---

## 完整使用示例

### 示例1：多功能管理插件

```csharp
using NetherGate.API;
using NetherGate.API.Events;
using NetherGate.API.Protocol;

namespace AdminTools
{
    public class AdminTools : PluginBase
    {
        private AdminConfig _config = null!;
        private Dictionary<Guid, int> _warnings = new();
        
        public override async Task OnLoadAsync()
        {
            Logger.Info("AdminTools 加载中...");
            _config = Config.GetSection<AdminConfig>("config") ?? new();
        }
        
        public override async Task OnEnableAsync()
        {
            Logger.Info("AdminTools 启动中...");
            
            // 订阅事件
            Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoined);
            Events.Subscribe<PlayerLeftEvent>(this, EventPriority.Normal, OnPlayerLeft);
            
            // 注册命令
            RegisterCommands();
            
            Logger.Info("AdminTools 已启用!");
        }
        
        private void RegisterCommands()
        {
            // /warn 命令
            Commands.Register(this, new CommandDefinition
            {
                Name = "warn",
                Description = "警告玩家",
                Handler = async ctx =>
                {
                    if (ctx.Args.Count < 2)
                        return CommandResult.Error("用法: /warn <玩家> <原因>");
                    
                    var playerName = ctx.Args[0];
                    var reason = string.Join(" ", ctx.Args.Skip(1));
                    
                    var players = await Server.GetPlayersAsync();
                    var target = players.FirstOrDefault(p => 
                        p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                    
                    if (target == null)
                        return CommandResult.Error("玩家不在线");
                    
                    // 增加警告计数
                    if (!_warnings.ContainsKey(target.Uuid))
                        _warnings[target.Uuid] = 0;
                    
                    _warnings[target.Uuid]++;
                    var warningCount = _warnings[target.Uuid];
                    
                    // 发送警告消息
                    await Server.SendSystemMessageAsync(
                        $"§c[警告] {target.Name}: {reason} (警告 {warningCount}/{_config.MaxWarnings})");
                    
                    // 达到上限则踢出
                    if (warningCount >= _config.MaxWarnings)
                    {
                        await Server.KickPlayerAsync(target, $"警告次数过多: {reason}");
                        _warnings.Remove(target.Uuid);
                    }
                    
                    return CommandResult.Ok($"已警告 {target.Name}");
                }
            });
            
            // /tempban 命令
            Commands.Register(this, new CommandDefinition
            {
                Name = "tempban",
                Description = "临时封禁玩家",
                Handler = async ctx =>
                {
                    if (ctx.Args.Count < 3)
                        return CommandResult.Error("用法: /tempban <玩家> <小时> <原因>");
                    
                    var playerName = ctx.Args[0];
                    if (!int.TryParse(ctx.Args[1], out var hours))
                        return CommandResult.Error("小时数必须是整数");
                    
                    var reason = string.Join(" ", ctx.Args.Skip(2));
                    
                    var players = await Server.GetPlayersAsync();
                    var target = players.FirstOrDefault(p => 
                        p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                    
                    if (target == null)
                        return CommandResult.Error("玩家不在线");
                    
                    var ban = new UserBanDto
                    {
                        Player = target,
                        Reason = reason,
                        Expires = DateTime.UtcNow.AddHours(hours),
                        Source = Metadata.Name
                    };
                    
                    if (await Server.AddBanAsync(ban))
                    {
                        await Server.SendSystemMessageAsync(
                            $"§c{target.Name} 被临时封禁 {hours} 小时，原因: {reason}");
                        return CommandResult.Ok();
                    }
                    
                    return CommandResult.Error("封禁失败");
                }
            });
        }
        
        private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
        {
            Logger.Info($"{e.Player.Name} 加入服务器");
            
            // 发送欢迎消息
            if (_config.SendWelcomeMessage)
            {
                await Server.SendSystemMessageAsync(
                    _config.WelcomeMessage.Replace("{player}", e.Player.Name));
            }
            
            // 检查是否有警告记录
            if (_warnings.TryGetValue(e.Player.Uuid, out var warnings))
            {
                await Server.SendSystemMessageAsync(
                    $"§e{e.Player.Name} 有 {warnings} 次警告记录");
            }
        }
        
        private async Task OnPlayerLeft(object? sender, PlayerLeftEvent e)
        {
            Logger.Info($"{e.Player.Name} 离开服务器");
        }
        
        public override async Task OnDisableAsync()
        {
            Events.UnsubscribeAll(this);
            Commands.UnregisterAll(this);
            
            // 保存配置
            Config.SetSection("config", _config);
            await Config.SaveAsync();
            
            Logger.Info("AdminTools 已禁用");
        }
    }
    
    public class AdminConfig
    {
        public bool SendWelcomeMessage { get; set; } = true;
        public string WelcomeMessage { get; set; } = "欢迎 {player}!";
        public int MaxWarnings { get; set; } = 3;
    }
}
```

---

### 示例2：自动备份插件

```csharp
using NetherGate.API;
using NetherGate.API.Events;

namespace AutoBackup
{
    public class AutoBackup : PluginBase
    {
        private Timer? _backupTimer;
        private BackupConfig _config = null!;
        
        public override async Task OnLoadAsync()
        {
            _config = Config.GetSection<BackupConfig>("config") ?? new();
        }
        
        public override Task OnEnableAsync()
        {
            Logger.Info("自动备份插件启动");
            
            // 设置定时备份
            var interval = TimeSpan.FromMinutes(_config.IntervalMinutes);
            _backupTimer = new Timer(async _ => await PerformBackupAsync(), 
                null, interval, interval);
            
            return Task.CompletedTask;
        }
        
        private async Task PerformBackupAsync()
        {
            try
            {
                Logger.Info("开始自动备份...");
                
                // 通知玩家
                if (_config.AnnounceBackup)
                {
                    await Server.SendSystemMessageAsync("§e[备份] 开始保存世界...");
                }
                
                // 保存世界
                await Server.SaveWorldAsync();
                
                // 这里可以添加复制世界文件的逻辑
                // ...
                
                Logger.Info("自动备份完成");
                
                if (_config.AnnounceBackup)
                {
                    await Server.SendSystemMessageAsync("§a[备份] 备份完成!");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("备份失败", ex);
            }
        }
        
        public override Task OnDisableAsync()
        {
            _backupTimer?.Dispose();
            Logger.Info("自动备份插件已停止");
            return Task.CompletedTask;
        }
    }
    
    public class BackupConfig
    {
        public int IntervalMinutes { get; set; } = 30;
        public bool AnnounceBackup { get; set; } = true;
    }
}
```

---

### 示例3：服务器监控插件

```csharp
using NetherGate.API;
using NetherGate.API.Events;
using NetherGate.API.Protocol;

namespace ServerMonitor
{
    public class ServerMonitor : PluginBase
    {
        private int _totalJoins = 0;
        private int _totalLeaves = 0;
        private DateTime _startTime;
        
        public override Task OnLoadAsync()
        {
            _startTime = DateTime.UtcNow;
            return Task.CompletedTask;
        }
        
        public override Task OnEnableAsync()
        {
            // 订阅所有事件进行监控
            Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Monitor, OnPlayerJoined);
            Events.Subscribe<PlayerLeftEvent>(this, EventPriority.Monitor, OnPlayerLeft);
            Events.Subscribe<BanAddedEvent>(this, EventPriority.Monitor, OnBanAdded);
            Events.Subscribe<GameRuleUpdatedEvent>(this, EventPriority.Monitor, OnGameRuleUpdated);
            
            // 注册统计命令
            Commands.Register(this, new CommandDefinition
            {
                Name = "serverstats",
                Description = "查看服务器统计",
                Handler = async ctx =>
                {
                    var state = await Server.GetStatusAsync();
                    var players = await Server.GetPlayersAsync();
                    var uptime = DateTime.UtcNow - _startTime;
                    
                    await ctx.ReplyAsync("=== 服务器统计 ===");
                    await ctx.ReplyAsync($"版本: {state.Version.Name}");
                    await ctx.ReplyAsync($"在线玩家: {players.Count}");
                    await ctx.ReplyAsync($"运行时间: {uptime.TotalHours:F1} 小时");
                    await ctx.ReplyAsync($"总加入次数: {_totalJoins}");
                    await ctx.ReplyAsync($"总离开次数: {_totalLeaves}");
                    
                    return CommandResult.Ok();
                }
            });
            
            Logger.Info("服务器监控插件已启用");
            return Task.CompletedTask;
        }
        
        private async Task OnPlayerJoined(object? sender, PlayerJoinedEvent e)
        {
            _totalJoins++;
            Logger.Debug($"玩家加入: {e.Player.Name} (总计: {_totalJoins})");
        }
        
        private async Task OnPlayerLeft(object? sender, PlayerLeftEvent e)
        {
            _totalLeaves++;
            Logger.Debug($"玩家离开: {e.Player.Name} (总计: {_totalLeaves})");
        }
        
        private async Task OnBanAdded(object? sender, BanAddedEvent e)
        {
            Logger.Warning($"封禁: {e.Ban.Player.Name}, 原因: {e.Ban.Reason}");
        }
        
        private async Task OnGameRuleUpdated(object? sender, GameRuleUpdatedEvent e)
        {
            Logger.Info($"游戏规则变更: {e.RuleName} = {e.NewValue.Value}");
        }
        
        public override Task OnDisableAsync()
        {
            Events.UnsubscribeAll(this);
            Commands.UnregisterAll(this);
            
            Logger.Info($"监控统计 - 总加入: {_totalJoins}, 总离开: {_totalLeaves}");
            return Task.CompletedTask;
        }
    }
}
```

---

## 事件完整列表

### 服务器事件

| 事件 | 说明 | 属性 |
|------|------|------|
| `ServerStartedEvent` | 服务器启动完成 | `ServerState State` |
| `ServerStoppedEvent` | 服务器停止 | `string Reason` |
| `ServerStatusChangedEvent` | 服务器状态变化（心跳） | `ServerState OldState, NewState` |

### 玩家事件

| 事件 | 说明 | 属性 |
|------|------|------|
| `PlayerJoinedEvent` | 玩家加入 | `PlayerDto Player` |
| `PlayerLeftEvent` | 玩家离开 | `PlayerDto Player` |
| `PlayerKickedEvent` | 玩家被踢出 | `PlayerDto Player, string? Reason` |

### 管理员事件

| 事件 | 说明 | 属性 |
|------|------|------|
| `OperatorAddedEvent` | 添加管理员 | `OperatorDto Operator` |
| `OperatorRemovedEvent` | 移除管理员 | `PlayerDto Player` |

### 白名单事件

| 事件 | 说明 | 属性 |
|------|------|------|
| `AllowlistAddedEvent` | 添加到白名单 | `PlayerDto Player` |
| `AllowlistRemovedEvent` | 从白名单移除 | `PlayerDto Player` |

### 封禁事件

| 事件 | 说明 | 属性 |
|------|------|------|
| `BanAddedEvent` | 封禁玩家 | `UserBanDto Ban` |
| `BanRemovedEvent` | 解封玩家 | `PlayerDto Player` |
| `IpBanAddedEvent` | 封禁IP | `IpBanDto IpBan` |
| `IpBanRemovedEvent` | 解封IP | `string Ip` |

### 游戏规则事件

| 事件 | 说明 | 属性 |
|------|------|------|
| `GameRuleUpdatedEvent` | 游戏规则更新 | `string RuleName, TypedRule NewValue` |

---

## 最佳实践

### 1. 异常处理

```csharp
try
{
    await Server.AddToAllowlistAsync(player);
}
catch (JsonRpcException ex)
{
    // SMP 协议错误
    Logger.Error($"协议错误: {ex.Message}", ex);
}
catch (TimeoutException)
{
    // 请求超时
    Logger.Error("请求超时，服务器可能未响应");
}
catch (Exception ex)
{
    // 其他错误
    Logger.Error("操作失败", ex);
}
```

### 2. 性能优化

```csharp
// ❌ 不好：频繁查询
foreach (var player in playerList)
{
    var players = await Server.GetPlayersAsync();  // 每次循环都查询
    // ...
}

// ✅ 好：缓存查询结果
var players = await Server.GetPlayersAsync();
foreach (var player in playerList)
{
    // 使用缓存的 players
}
```

### 3. 事件优先级

```csharp
// Monitor 优先级：仅监听，不修改事件
Events.Subscribe<PlayerJoinedEvent>(
    this, 
    EventPriority.Monitor,  // 最后执行，用于日志记录
    OnPlayerJoinedLogging);

// Normal 优先级：正常业务逻辑
Events.Subscribe<PlayerJoinedEvent>(
    this, 
    EventPriority.Normal,   // 中等优先级
    OnPlayerJoinedWelcome);

// Highest 优先级：安全检查
Events.Subscribe<PlayerJoinedEvent>(
    this, 
    EventPriority.Highest,  // 最先执行
    OnPlayerJoinedSecurityCheck);
```

### 4. 资源清理

```csharp
public override async Task OnDisableAsync()
{
    // 1. 取消所有事件订阅
    Events.UnsubscribeAll(this);
    
    // 2. 取消所有命令注册
    Commands.UnregisterAll(this);
    
    // 3. 停止定时器
    _timer?.Dispose();
    
    // 4. 保存数据
    await SaveDataAsync();
    
    // 5. 保存配置
    await Config.SaveAsync();
}
```

---

## 参考资料

- [Minecraft Wiki - 服务端管理协议](https://zh.minecraft.wiki/w/%E6%9C%8D%E5%8A%A1%E7%AB%AF%E7%AE%A1%E7%90%86%E5%8D%8F%E8%AE%AE)
- [JSON-RPC 2.0 规范](https://www.jsonrpc.org/specification)
- [NetherGate API 设计文档](./API_DESIGN.md)
- [NetherGate 开发文档](../DEVELOPMENT.md)

---

## 总结

NetherGate 的 SMP 接口封装提供了：

✅ **完整功能** - 覆盖所有 Minecraft 服务端管理协议  
✅ **类型安全** - 强类型 C# API，编译时错误检查  
✅ **易于使用** - 简洁的接口，清晰的文档  
✅ **事件驱动** - 实时响应服务器状态变化  
✅ **高性能** - 异步设计，支持高并发  

开始使用 NetherGate 开发你的 Minecraft 服务器插件吧！🚀

