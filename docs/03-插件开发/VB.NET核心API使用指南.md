# VB.NET 核心 API 使用指南

本文档介绍如何在 VB.NET 插件中使用 NetherGate 的核心功能。

> **💡 关于 VB.NET 插件**
> 
> VB.NET 插件与 C# 插件完全等价，因为它们都编译成相同的 .NET IL 代码。VB.NET 插件：
> - ✅ 拥有与 C# 相同的性能和功能
> - ✅ 可以访问所有 NetherGate API
> - ✅ 支持构造函数注入和 PluginBase 基类
> - ✅ 完全的类型安全和编译时检查
> 
> **本文档仅展示 VB.NET 语法示例。关于 API 详情、接口定义、高级功能等，请参考：**
> - [插件开发指南](./插件开发指南.md) - 完整的插件功能和接口
> - [命令系统](../02-核心功能/命令系统.md) - 命令注册和处理
> - [事件系统](../02-核心功能/事件系统.md) - 事件订阅和发布
> - [API 参考](../08-参考/API参考.md) - 完整的 API 文档

---

## 📋 **目录**

- [Logger（日志系统）](#logger日志系统)
- [Events（事件系统）](#events事件系统)
- [RCON 客户端](#rcon-客户端)
- [SMP API（服务器管理）](#smp-api服务器管理)
- [GameDisplay（游戏显示）](#gamedisplay游戏显示)
- [配置管理](#配置管理)
- [玩家数据读取](#玩家数据读取)
- [调度器](#调度器)

---

## 📝 **Logger（日志系统）**

Logger 提供了标准的日志记录功能，支持不同的日志级别。

### **基础用法**

```vb
Imports NetherGate.API.Plugins
Imports NetherGate.API.Logging

Public Class MyPlugin
    Inherits PluginBase

    Public Overrides Function OnEnableAsync() As Task
        ' 不同级别的日志
        Logger.Trace("详细的追踪信息（通常不显示）")
        Logger.Debug("调试信息（开发时使用）")
        Logger.Info("一般信息（正常运行状态）")
        Logger.Warning("警告信息（可能的问题）")
        Logger.Error("错误信息（需要注意的问题）")
        
        Return Task.CompletedTask
    End Function

End Class
```

### **日志级别**

| 级别 | 方法 | 用途 |
|------|------|------|
| **Trace** | `Logger.Trace()` | 非常详细的调试信息，通常不显示 |
| **Debug** | `Logger.Debug()` | 调试信息，仅在开发时使用 |
| **Info** | `Logger.Info()` | 一般性信息，记录重要的状态变化 |
| **Warning** | `Logger.Warning()` | 警告信息，可能的问题但不影响运行 |
| **Error** | `Logger.Error()` | 错误信息，需要注意的问题 |

### **实用示例**

```vb
Public Class LogExamplePlugin
    Inherits PluginBase

    Public Overrides Async Function OnEnableAsync() As Task
        ' 记录配置信息
        Dim config = Await Config.LoadConfigAsync(Of MyConfig)("config")
        Logger.Info($"插件配置已加载: MaxPlayers={config.MaxPlayers}")
        
        ' 记录调试信息
        Logger.Debug($"数据目录: {DataDirectory}")
        
        ' 警告检查
        If Not config.Enabled Then
            Logger.Warning("插件功能已在配置中禁用")
        End If
        
        ' 错误处理
        Try
            Await SomeRiskyOperationAsync()
        Catch ex As Exception
            Logger.Error($"操作失败: {ex.Message}", ex)
        End Try
    End Function

    Private Async Function SomeRiskyOperationAsync() As Task
        ' 可能抛出异常的操作
        Await Task.CompletedTask
    End Function

End Class
```

---

## 🔔 **Events（事件系统）**

事件系统允许插件响应游戏内发生的各种事件。

### **订阅事件**

```vb
Imports NetherGate.API.Plugins
Imports NetherGate.API.Events

Public Class EventExamplePlugin
    Inherits PluginBase

    Public Overrides Function OnEnableAsync() As Task
        ' 订阅玩家加入事件
        Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)
        
        ' 订阅玩家离开事件
        Events.Subscribe(Of PlayerLeftEvent)(AddressOf OnPlayerLeft, EventPriority.Normal)
        
        ' 订阅服务器启动事件（高优先级）
        Events.Subscribe(Of ServerStartedEvent)(AddressOf OnServerStarted, EventPriority.High)
        
        Return Task.CompletedTask
    End Function

    Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
        Logger.Info($"玩家 {e.Player.Name} 加入了服务器")
        
        ' 发送欢迎消息
        Await GameDisplay.SendChatMessage(
            e.Player.Name,
            "§a欢迎来到服务器！"
        )
    End Function

    Private Function OnPlayerLeft(e As PlayerLeftEvent) As Task
        Logger.Info($"玩家 {e.Player.Name} 离开了服务器")
        Return Task.CompletedTask
    End Function

    Private Function OnServerStarted(e As ServerStartedEvent) As Task
        Logger.Info("服务器已启动！")
        Return Task.CompletedTask
    End Function

End Class
```

### **事件优先级**

```vb
' 事件优先级（执行顺序）
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Lowest)   ' 最后执行
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Low)      ' 较后执行
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)   ' 正常顺序（默认）
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.High)     ' 较早执行
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Highest)  ' 最先执行
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Monitor)  ' 监控（只读，最后执行）
```

### **取消事件**

某些事件可以被取消：

```vb
Private Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
    ' 检查玩家是否被封禁
    If IsPlayerBanned(e.Player.Name) Then
        e.Cancelled = True  ' 取消事件
        e.CancelReason = "你已被服务器封禁"
        Logger.Info($"阻止被封禁玩家 {e.Player.Name} 加入")
    End If
    
    Return Task.CompletedTask
End Function

Private Function IsPlayerBanned(playerName As String) As Boolean
    ' 检查封禁逻辑
    Return False
End Function
```

### **常用事件列表**

完整的事件列表请参考 [事件列表](../08-参考/事件列表.md)。

---

## 🎮 **RCON 客户端**

RCON 客户端提供了与 Minecraft 服务器通信的能力，可以执行游戏命令。

### **基础用法**

```vb
Public Class RconExamplePlugin
    Inherits PluginBase

    Public Overrides Async Function OnEnableAsync() As Task
        ' 执行单条命令
        Dim result = Await Rcon.SendCommandAsync("say Hello from VB.NET!")
        Logger.Info($"命令结果: {result}")
    End Function

End Class
```

### **便捷方法**

```vb
' 广播消息
Await Rcon.Say("服务器公告")

' 向特定玩家发送消息
Await Rcon.Tell("PlayerName", "这是私人消息")

' 给予物品
Await Rcon.Give("PlayerName", "diamond", 64)

' 传送玩家
Await Rcon.Teleport("PlayerName", 100, 64, 200)

' 更改游戏模式
Await Rcon.Gamemode("PlayerName", "creative")

' 踢出玩家
Await Rcon.Kick("BadPlayer", "违反服务器规则")

' 封禁玩家
Await Rcon.Ban("Cheater", "使用作弊客户端")

' 解封玩家
Await Rcon.Pardon("ReformedPlayer")

' 管理员权限
Await Rcon.Op("TrustedPlayer")
Await Rcon.Deop("FormerAdmin")
```

### **白名单管理**

```vb
' 添加到白名单
Await Rcon.WhitelistAdd("NewPlayer")

' 从白名单移除
Await Rcon.WhitelistRemove("OldPlayer")

' 开启白名单
Await Rcon.WhitelistOn()

' 关闭白名单
Await Rcon.WhitelistOff()

' 重新加载白名单
Await Rcon.WhitelistReload()
```

### **批量执行命令**

```vb
Public Async Function BroadcastMultipleMessages() As Task
    Dim commands As New List(Of String) From {
        "say 欢迎来到服务器！",
        "say 输入 /help 查看命令",
        "say 祝你玩得开心！"
    }
    
    For Each cmd In commands
        Dim result = Await Rcon.SendCommandAsync(cmd)
        Logger.Debug($"执行命令: {cmd}, 结果: {result}")
    Next
End Function
```

---

## 🌐 **SMP API（服务器管理）**

SMP API 提供了服务器管理协议的接口，可以获取服务器状态和在线玩家信息。

### **获取在线玩家**

```vb
Public Async Function ListOnlinePlayers() As Task
    Dim players = Await Server.GetOnlinePlayersAsync()
    
    Logger.Info($"当前在线玩家数: {players.Count}")
    
    For Each player In players
        Logger.Info($"- {player.Name} (UUID: {player.Uuid})")
    Next
End Function
```

### **获取服务器状态**

```vb
Public Async Function CheckServerStatus() As Task
    Dim status = Await Server.GetServerStatusAsync()
    
    Logger.Info($"服务器版本: {status.Version}")
    Logger.Info($"在线玩家: {status.OnlinePlayers}/{status.MaxPlayers}")
    Logger.Info($"MOTD: {status.Motd}")
End Function
```

### **实用示例**

```vb
Public Class ServerMonitorPlugin
    Inherits PluginBase

    Public Overrides Function OnEnableAsync() As Task
        ' 每分钟检查一次服务器状态
        Scheduler.RunRepeating(AddressOf CheckServerStatus, 
            TimeSpan.Zero, 
            TimeSpan.FromMinutes(1))
        
        Return Task.CompletedTask
    End Function

    Private Async Sub CheckServerStatus()
        Try
            Dim players = Await Server.GetOnlinePlayersAsync()
            Logger.Info($"服务器健康检查: {players.Count} 玩家在线")
            
            ' 如果玩家数量过多，发送警告
            If players.Count > 90 Then
                Logger.Warning($"服务器玩家数接近上限: {players.Count}/100")
                Await Rcon.Say("§c服务器玩家数接近上限！")
            End If
        Catch ex As Exception
            Logger.Error($"服务器健康检查失败: {ex.Message}")
        End Try
    End Sub

End Class
```

---

## 🎨 **GameDisplay（游戏显示）**

GameDisplay API 提供了在游戏内显示各种信息的功能。

### **发送聊天消息**

```vb
' 发送简单消息
Await GameDisplay.SendChatMessage("PlayerName", "Hello!")

' 发送彩色消息
Await GameDisplay.SendChatMessage("PlayerName", "§aGreen §bBlue §cRed")

' 发送给所有玩家
Dim players = Await Server.GetOnlinePlayersAsync()
For Each player In players
    Await GameDisplay.SendChatMessage(player.Name, "§e服务器公告")
Next
```

### **显示标题**

```vb
' 显示标题和副标题
Await GameDisplay.ShowTitle(
    "PlayerName",
    "§e欢迎回来！",           ' 主标题
    "§7上次登录: 昨天",        ' 副标题
    10,                        ' 淡入时间（tick，1秒=20tick）
    70,                        ' 停留时间（tick）
    20                         ' 淡出时间（tick）
)
```

### **显示 ActionBar**

```vb
' 在屏幕底部显示消息
Await GameDisplay.ShowActionBar("PlayerName", "§6你获得了 10 金币！")
```

### **播放音效**

```vb
' 播放音效
Await GameDisplay.PlaySound(
    "PlayerName",
    "entity.experience_orb.pickup",  ' 音效名称
    1.0F,                             ' 音量
    1.0F                              ' 音调
)
```

### **实用示例：欢迎系统**

```vb
Public Class WelcomePlugin
    Inherits PluginBase

    Public Overrides Function OnEnableAsync() As Task
        Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)
        Return Task.CompletedTask
    End Function

    Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
        Dim player = e.Player
        
        ' 显示欢迎标题
        Await GameDisplay.ShowTitle(
            player.Name,
            "§e欢迎来到服务器！",
            "§7祝你玩得开心",
            10, 60, 10
        )
        
        ' 发送欢迎消息
        Await GameDisplay.SendChatMessage(player.Name, "§a━━━━━━━━━━━━━━━━━━━━━")
        Await GameDisplay.SendChatMessage(player.Name, "§e  欢迎来到我们的服务器！")
        Await GameDisplay.SendChatMessage(player.Name, "§7  输入 §f/help §7查看帮助")
        Await GameDisplay.SendChatMessage(player.Name, "§a━━━━━━━━━━━━━━━━━━━━━")
        
        ' 播放欢迎音效
        Await GameDisplay.PlaySound(
            player.Name,
            "entity.player.levelup",
            1.0F,
            1.0F
        )
        
        ' 显示 ActionBar
        Await GameDisplay.ShowActionBar(player.Name, "§6欢迎！当前在线玩家: " & e.OnlinePlayerCount)
    End Function

End Class
```

---

## ⚙️ **配置管理**

配置管理器支持 JSON 和 YAML 格式的配置文件。

### **定义配置类**

```vb
Public Class MyConfig
    Public Property Enabled As Boolean = True
    Public Property MaxPlayers As Integer = 100
    Public Property WelcomeMessage As String = "Welcome!"
    Public Property AllowedWorlds As List(Of String) = New List(Of String) From {"world", "world_nether", "world_the_end"}
End Class
```

### **加载配置**

```vb
Public Async Function LoadConfigExample() As Task
    ' 加载配置（自动创建默认配置）
    Dim config = Await Config.LoadConfigAsync(Of MyConfig)("config")
    
    Logger.Info($"Enabled: {config.Enabled}")
    Logger.Info($"MaxPlayers: {config.MaxPlayers}")
    Logger.Info($"WelcomeMessage: {config.WelcomeMessage}")
    
    For Each world In config.AllowedWorlds
        Logger.Info($"  - {world}")
    Next
End Function
```

### **保存配置**

```vb
Public Async Function SaveConfigExample() As Task
    ' 加载现有配置
    Dim config = Await Config.LoadConfigAsync(Of MyConfig)("config")
    
    ' 修改配置
    config.MaxPlayers = 200
    config.WelcomeMessage = "Updated welcome message"
    
    ' 保存配置
    Await Config.SaveConfigAsync("config", config)
    
    Logger.Info("配置已保存")
End Function
```

### **完整示例**

```vb
Public Class ConfigurablePlugin
    Inherits PluginBase

    Private _config As MyConfig

    Public Overrides Async Function OnEnableAsync() As Task
        ' 加载配置
        _config = Await Config.LoadConfigAsync(Of MyConfig)("config")
        
        If Not _config.Enabled Then
            Logger.Warning("插件已在配置中禁用")
            Return
        End If
        
        Logger.Info($"最大玩家数: {_config.MaxPlayers}")
        
        ' 订阅事件
        Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)
    End Function

    Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
        ' 使用配置中的欢迎消息
        Await GameDisplay.SendChatMessage(e.Player.Name, _config.WelcomeMessage)
    End Function

End Class
```

---

## 📊 **玩家数据读取**

玩家数据读取器允许你读取玩家的 NBT 数据。

### **读取玩家数据**

```vb
Public Async Function ReadPlayerDataExample() As Task
    ' 读取玩家数据
    Dim data = Await PlayerData.ReadPlayerDataAsync("Steve")
    
    ' 基本信息
    Logger.Info($"玩家: {data.Name}")
    Logger.Info($"UUID: {data.Uuid}")
    Logger.Info($"维度: {data.Dimension}")
    
    ' 位置信息
    Logger.Info($"位置: X={data.Position.X}, Y={data.Position.Y}, Z={data.Position.Z}")
    
    ' 生存数据
    Logger.Info($"生命值: {data.Health}/{data.MaxHealth}")
    Logger.Info($"饥饿值: {data.FoodLevel}")
    Logger.Info($"经验等级: {data.Level}")
    
    ' 游戏模式
    Logger.Info($"游戏模式: {data.GameMode}")
End Function
```

### **读取物品栏**

```vb
Public Async Function ReadInventoryExample() As Task
    Dim data = Await PlayerData.ReadPlayerDataAsync("Steve")
    
    Logger.Info("玩家物品栏:")
    
    For Each item In data.Inventory
        If Not String.IsNullOrEmpty(item.Id) Then
            Logger.Info($"  槽位 {item.Slot}: {item.Id} x{item.Count}")
            
            ' 检查附魔
            If item.Enchantments IsNot Nothing AndAlso item.Enchantments.Count > 0 Then
                For Each enchant In item.Enchantments
                    Logger.Info($"    附魔: {enchant.Id} 等级 {enchant.Level}")
                Next
            End If
        End If
    Next
End Function
```

### **实用示例：统计系统**

```vb
Public Class PlayerStatsPlugin
    Inherits PluginBase

    Public Overrides Function OnEnableAsync() As Task
        ' 注册命令
        Commands.RegisterCommand(New StatsCommand(Me))
        Return Task.CompletedTask
    End Function

    Public Async Function ShowPlayerStats(playerName As String) As Task
        Try
            Dim data = Await PlayerData.ReadPlayerDataAsync(playerName)
            
            ' 构建统计信息
            Dim stats = New List(Of String) From {
                "§e━━━━━ 玩家统计 ━━━━━",
                $"§7玩家: §f{data.Name}",
                $"§7等级: §a{data.Level} §7(经验: {data.Experience})",
                $"§7生命值: §c{data.Health}§7/§c{data.MaxHealth}",
                $"§7位置: §f{CInt(data.Position.X)}, {CInt(data.Position.Y)}, {CInt(data.Position.Z)}",
                $"§7维度: §f{data.Dimension}",
                "§e━━━━━━━━━━━━━━━━━━"
            }
            
            ' 发送统计信息
            For Each line In stats
                Await GameDisplay.SendChatMessage(playerName, line)
            Next
            
        Catch ex As Exception
            Logger.Error($"无法读取玩家数据: {ex.Message}")
            Await GameDisplay.SendChatMessage(playerName, "§c无法读取玩家数据")
        End Try
    End Function

End Class
```

---

## ⏱️ **调度器**

调度器允许你执行延迟任务和定时任务。

### **延迟执行**

```vb
' 5秒后执行
Scheduler.RunDelayed(Sub()
    Logger.Info("5秒后执行的任务")
End Sub, TimeSpan.FromSeconds(5))

' 异步任务
Scheduler.RunDelayed(Async Function()
    Dim players = Await Server.GetOnlinePlayersAsync()
    Logger.Info($"当前在线: {players.Count} 人")
End Function, TimeSpan.FromSeconds(5))
```

### **重复执行**

```vb
' 每30秒执行一次
Scheduler.RunRepeating(Sub()
    Logger.Info("定时任务执行")
End Sub, TimeSpan.Zero, TimeSpan.FromSeconds(30))

' 首次延迟10秒，然后每30秒执行
Scheduler.RunRepeating(Sub()
    Logger.Info("定时任务执行")
End Sub, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30))
```

### **异步任务**

```vb
' 在后台线程执行异步任务
Scheduler.RunAsync(Async Function()
    Dim players = Await Server.GetOnlinePlayersAsync()
    
    For Each player In players
        Dim data = Await PlayerData.ReadPlayerDataAsync(player.Name)
        Logger.Debug($"{player.Name}: Level {data.Level}")
    Next
End Function)
```

### **实用示例：自动保存**

```vb
Public Class AutoSavePlugin
    Inherits PluginBase

    Private _saveInterval As TimeSpan = TimeSpan.FromMinutes(5)

    Public Overrides Function OnEnableAsync() As Task
        ' 每5分钟自动保存
        Scheduler.RunRepeating(AddressOf AutoSave, _saveInterval, _saveInterval)
        
        Logger.Info($"自动保存已启用，间隔: {_saveInterval.TotalMinutes} 分钟")
        
        Return Task.CompletedTask
    End Function

    Private Async Sub AutoSave()
        Try
            Logger.Info("执行自动保存...")
            
            ' 广播消息
            Await Rcon.Say("§e服务器正在保存...")
            
            ' 执行保存命令
            Await Rcon.SendCommandAsync("save-all flush")
            
            ' 等待保存完成
            Await Task.Delay(2000)
            
            Logger.Info("自动保存完成")
            Await Rcon.Say("§a保存完成！")
            
        Catch ex As Exception
            Logger.Error($"自动保存失败: {ex.Message}")
        End Try
    End Sub

End Class
```

---

## 📚 **相关文档**

完整的 API 详情和高级功能，请参考：

- **[插件开发指南](./插件开发指南.md)** - 完整的插件开发流程
- **[VB.NET插件开发指南](./VB.NET插件开发指南.md)** - VB.NET 语法示例
- **[命令系统](../02-核心功能/命令系统.md)** - 命令注册和处理
- **[事件系统](../02-核心功能/事件系统.md)** - 事件订阅和发布
- **[RCON 集成](../02-核心功能/RCON集成.md)** - RCON 详细文档
- **[SMP 协议](../02-核心功能/SMP协议.md)** - SMP API 详细文档
- **[API 参考](../08-参考/API参考.md)** - 完整的 API 文档

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-12

