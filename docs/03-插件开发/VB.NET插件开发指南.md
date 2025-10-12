# NetherGate VB.NET 插件开发指南

本指南将带你使用 Visual Basic .NET 创建 NetherGate 插件。

> **💡 关于 VB.NET 插件支持**
> 
> NetherGate 原生支持所有 .NET 语言编写的插件，包括 VB.NET、C#、F# 等。这是因为：
> - 所有 .NET 语言都编译成相同的 IL（中间语言）代码
> - NetherGate 通过 `AssemblyLoadContext` 加载 .NET 程序集，与源语言无关
> - 只要实现 `IPlugin` 接口，任何 .NET 语言都能无缝集成
> 
> **本文档仅展示 VB.NET 特有的语法示例。关于插件接口、API 详情、配置文件等内容，请参考：**
> - [插件开发指南](./插件开发指南.md) - 完整的插件开发流程和最佳实践
> - [配置文件](./配置文件.md) - plugin.json 配置详解
> - [API 参考](../08-参考/API参考.md) - 完整的 API 文档

---

## 📋 **目录**

- [环境准备](#环境准备)
- [创建第一个 VB.NET 插件](#创建第一个-vbnet-插件)
- [插件生命周期](#插件生命周期)
- [使用 PluginBase 基类](#使用-pluginbase-基类)
- [常用功能示例](#常用功能示例)
- [VB.NET 与 C# 语法对照](#vbnet-与-c-语法对照)

---

## 🔧 **环境准备**

### **1. 安装工具**

- **.NET 9.0 SDK** 或更高版本
  - 下载：https://dotnet.microsoft.com/download
  - 验证：`dotnet --version`

- **IDE**（选择一个）
  - Visual Studio 2022（推荐，对 VB.NET 支持最好）
  - JetBrains Rider
  - Visual Studio Code

### **2. 获取 NetherGate.API**

#### **方式 1：从源码引用**

```bash
git clone https://github.com/your-org/NetherGate.git
# 然后在插件项目中引用 NetherGate.API.csproj
```

#### **方式 2：从 NuGet 安装**（如果已发布）

```bash
dotnet add package NetherGate.API
```

---

## 🚀 **创建第一个 VB.NET 插件**

### **1. 创建项目**

```bash
# 创建新的 VB.NET 类库项目
dotnet new classlib -lang VB -n HelloWorldPlugin
cd HelloWorldPlugin

# 添加 NetherGate.API 引用
dotnet add reference ../NetherGate/src/NetherGate.API/NetherGate.API.csproj
```

### **2. 配置项目文件**

编辑 `HelloWorldPlugin.vbproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- 目标框架 -->
    <TargetFramework>net9.0</TargetFramework>
    
    <!-- 启用动态加载（必须） -->
    <EnableDynamicLoading>true</EnableDynamicLoading>
    
    <!-- VB.NET 根命名空间 -->
    <RootNamespace>HelloWorldPlugin</RootNamespace>
    
    <!-- 插件版本 -->
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <!-- NetherGate API 引用 -->
    <PackageReference Include="NetherGate.API" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>

  <!-- plugin.json 配置 -->
  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <!-- 构建后自动复制到 plugins 目录（可选） -->
  <Target Name="CopyToPlugins" AfterTargets="Build">
    <ItemGroup>
      <PluginFiles Include="$(OutputPath)**\*.*" />
    </ItemGroup>
    <Copy 
      SourceFiles="@(PluginFiles)" 
      DestinationFolder="$(SolutionDir)..\NetherGate\bin\$(Configuration)\plugins\$(ProjectName)\%(RecursiveDir)" 
      SkipUnchangedFiles="true" />
  </Target>
</Project>
```

### **3. 创建插件主类**

删除 `Class1.vb`，创建 `HelloWorldPlugin.vb`：

```vb
Imports NetherGate.API.Plugins
Imports NetherGate.API.Events
Imports NetherGate.API.Logging
Imports System.Threading.Tasks

Namespace HelloWorldPlugin

    Public Class HelloWorldPlugin
        Inherits PluginBase

        ' 插件加载时调用
        Public Overrides Function OnLoadAsync() As Task
            Logger.Info("Hello World 插件正在加载...")
            Return Task.CompletedTask
        End Function

        ' 插件启用时调用
        Public Overrides Function OnEnableAsync() As Task
            Logger.Info("Hello World 插件已启用！")
            
            ' 订阅玩家加入事件
            Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)
            
            ' 注册命令
            Commands.RegisterCommand(New HelloCommand(Me))
            
            Return Task.CompletedTask
        End Function

        ' 插件禁用时调用
        Public Overrides Function OnDisableAsync() As Task
            Logger.Info("Hello World 插件已禁用")
            Return Task.CompletedTask
        End Function

        ' 处理玩家加入事件
        Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
            Logger.Info($"玩家 {e.Player.Name} 加入了服务器")
            
            ' 发送欢迎消息
            Await GameDisplay.SendChatMessage(
                e.Player.Name, 
                "§a欢迎来到服务器！这是来自 VB.NET 插件的问候。"
            )
        End Function

    End Class

End Namespace
```

### **4. 创建命令**

创建 `HelloCommand.vb`：

```vb
Imports NetherGate.API.Commands
Imports NetherGate.API.Plugins
Imports System.Threading.Tasks

Namespace HelloWorldPlugin

    Public Class HelloCommand
        Implements ICommand

        Private ReadOnly _plugin As HelloWorldPlugin

        Public Sub New(plugin As HelloWorldPlugin)
            _plugin = plugin
        End Sub

        ' 命令属性
        Public ReadOnly Property Name As String Implements ICommand.Name
            Get
                Return "hello"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ICommand.Description
            Get
                Return "向玩家打招呼"
            End Get
        End Property

        Public ReadOnly Property Usage As String Implements ICommand.Usage
            Get
                Return "/hello [name]"
            End Get
        End Property

        Public ReadOnly Property Aliases As String() Implements ICommand.Aliases
            Get
                Return {"hi", "greet"}
            End Get
        End Property

        Public ReadOnly Property Permission As String Implements ICommand.Permission
            Get
                Return "helloworld.use"
            End Get
        End Property

        Public ReadOnly Property PluginId As String Implements ICommand.PluginId
            Get
                Return _plugin.Metadata.Id
            End Get
        End Property

        ' 执行命令
        Public Async Function ExecuteAsync(sender As ICommandSender, args As String()) As Task(Of CommandResult) Implements ICommand.ExecuteAsync
            If sender.IsConsole Then
                ' 控制台执行
                _plugin.Logger.Info("Hello from console!")
                Return CommandResult.Ok("Hello from console!")
            End If

            ' 游戏内执行
            Dim playerName As String = sender.Name
            Dim greeting As String

            If args.Length > 0 Then
                greeting = $"§aHello, {args(0)}! From {playerName}."
            Else
                greeting = $"§aHello, {playerName}!"
            End If

            Await _plugin.GameDisplay.SendChatMessage(playerName, greeting)

            Return CommandResult.Ok("Greeting sent!")
        End Function

        ' Tab 补全
        Public Async Function TabCompleteAsync(sender As ICommandSender, args As String()) As Task(Of List(Of String)) Implements ICommand.TabCompleteAsync
            If args.Length = 1 Then
                ' 提供在线玩家列表
                Dim players = Await _plugin.Server.GetOnlinePlayersAsync()
                Return players _
                    .Select(Function(p) p.Name) _
                    .Where(Function(name) name.StartsWith(args(0), StringComparison.OrdinalIgnoreCase)) _
                    .ToList()
            End If

            Return New List(Of String)()
        End Function

    End Class

End Namespace
```

### **5. 创建 plugin.json**

在项目根目录创建 `plugin.json`：

```json
{
  "id": "com.example.helloworldvb",
  "name": "HelloWorldPlugin",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "My first VB.NET NetherGate plugin",
  "main": "HelloWorldPlugin.HelloWorldPlugin",
  "type": "csharp",
  "dependencies": [],
  "nethergate_version": ">=1.0.0"
}
```

> **注意**：`type` 字段应设置为 `"csharp"`，因为 VB.NET 和 C# 都是 .NET 语言插件。

### **6. 构建和运行**

```bash
# 构建插件
dotnet build

# 输出位置：bin/Debug/net9.0/

# 复制到 NetherGate plugins 目录
# （如果配置了 CopyToPlugins，会自动复制）

# 运行 NetherGate
cd ../NetherGate/bin/Debug
dotnet NetherGate.Host.dll
```

### **7. 测试插件**

```bash
# 在 NetherGate 控制台
plugin list          # 查看插件列表
hello                # 执行命令（控制台）

# 在 Minecraft 游戏内
/hello               # 执行命令（游戏内）
/hi                  # 使用别名
```

---

## 🔄 **插件生命周期**

VB.NET 插件的生命周期与 C# 完全相同：

```
1. OnLoadAsync()    ← 插件加载（静态初始化）
2. OnEnableAsync()  ← 插件启用（注册事件、命令等）
3. [运行中]         ← 响应事件、处理命令
4. OnDisableAsync() ← 插件禁用（清理资源）
5. OnUnloadAsync()  ← 插件卸载（最终清理）
```

### **实现示例**

```vb
Imports NetherGate.API.Plugins
Imports NetherGate.API.Events
Imports System.Threading.Tasks

Public Class MyPlugin
    Inherits PluginBase

    Private _timer As Timer
    Private _database As DatabaseConnection

    ' 1. 加载阶段
    Public Overrides Function OnLoadAsync() As Task
        Logger.Info("插件加载中...")
        ' 初始化静态资源
        Return Task.CompletedTask
    End Function

    ' 2. 启用阶段
    Public Overrides Async Function OnEnableAsync() As Task
        Logger.Info("插件启用中...")

        ' 加载配置
        Dim config = Await Config.LoadConfigAsync(Of MyConfig)("config")

        ' 连接数据库
        _database = New DatabaseConnection(config.DatabaseUrl)
        Await _database.ConnectAsync()

        ' 订阅事件
        Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)

        ' 注册命令
        Commands.RegisterCommand(New MyCommand(Me))

        ' 启动定时任务
        _timer = New Timer(AddressOf OnHeartbeat, Nothing, TimeSpan.Zero, TimeSpan.FromMinutes(1))

        Logger.Info("插件启用成功")
    End Function

    ' 3. 禁用阶段
    Public Overrides Async Function OnDisableAsync() As Task
        Logger.Info("插件禁用中...")

        ' 停止定时任务
        If _timer IsNot Nothing Then
            _timer.Dispose()
        End If

        ' 保存数据
        Await SaveAllDataAsync()

        ' 断开数据库
        If _database IsNot Nothing Then
            Await _database.DisconnectAsync()
        End If

        Logger.Info("插件已禁用")
    End Function

    ' 4. 卸载阶段
    Public Overrides Function OnUnloadAsync() As Task
        _database = Nothing
        Return Task.CompletedTask
    End Function

    ' 事件处理
    Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
        Logger.Info($"玩家 {e.Player.Name} 加入了服务器")
        Await GameDisplay.SendChatMessage(e.Player.Name, "§a欢迎！")
    End Function

    ' 定时任务
    Private Sub OnHeartbeat(state As Object)
        Logger.Debug("Heartbeat")
    End Sub

    ' 数据保存
    Private Async Function SaveAllDataAsync() As Task
        ' 保存逻辑
        Await Task.CompletedTask
    End Function

End Class
```

---

## 📚 **使用 PluginBase 基类**

`PluginBase` 是推荐的插件基类，它提供了常用服务的便捷访问：

### **可用属性**

```vb
Public MustInherit Class PluginBase
    Inherits IPlugin

    ' 核心服务（自动注入）
    Protected Logger As ILogger              ' 日志记录器
    Protected Config As IConfigManager        ' 配置管理器
    Protected Events As IEventBus            ' 事件总线
    Protected Commands As ICommandManager     ' 命令管理器
    Protected Permissions As IPermissionManager ' 权限管理器
    
    ' 服务器交互
    Protected Server As ISmpApi              ' SMP API
    Protected Rcon As IRconClient            ' RCON 客户端
    Protected GameDisplay As IGameDisplayApi  ' 游戏显示 API
    
    ' 数据访问
    Protected PlayerData As IPlayerDataReader  ' 玩家数据读取器
    Protected WorldData As IWorldDataReader   ' 世界数据读取器
    Protected NbtWriter As INbtDataWriter     ' NBT 数据写入器
    
    ' 高级功能
    Protected Scheduler As IScheduler        ' 调度器
    Protected Messenger As IPluginMessenger  ' 插件间通信
    Protected FileSystem As IFileSystemApi   ' 文件系统 API
    
    ' 插件信息
    Protected DataDirectory As String        ' 插件数据目录
    Protected Metadata As PluginMetadata     ' 插件元数据
    
End Class
```

### **使用示例**

```vb
Imports NetherGate.API.Plugins
Imports NetherGate.API.Events

Public Class MyPlugin
    Inherits PluginBase

    Public Overrides Async Function OnEnableAsync() As Task
        ' 直接使用 Logger
        Logger.Info("插件启用")
        
        ' 直接使用 Events
        Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)
        
        ' 直接使用 Server (SMP API)
        Dim players = Await Server.GetOnlinePlayersAsync()
        Logger.Info($"当前在线玩家: {players.Count}")
        
        ' 直接使用 GameDisplay
        For Each player In players
            Await GameDisplay.SendChatMessage(player.Name, "§a插件已重新加载！")
        Next
        
        ' 直接使用 DataDirectory
        Dim configPath = Path.Combine(DataDirectory, "config.json")
        Logger.Debug($"配置文件路径: {configPath}")
    End Function

    Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
        ' 读取玩家数据
        Dim data = Await PlayerData.ReadPlayerDataAsync(e.Player.Uuid)
        
        ' 显示欢迎消息
        Await GameDisplay.ShowTitle(
            e.Player.Name,
            "§e欢迎回来！",
            $"§7上次游玩时间: {data.LastPlayed}",
            10, 70, 20
        )
    End Function

End Class
```

---

## 💡 **常用功能示例**

### **1. 事件订阅**

```vb
' 订阅事件（使用 AddressOf）
Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)

' 事件处理器
Private Async Function OnPlayerJoined(e As PlayerJoinedEvent) As Task
    Logger.Info($"{e.Player.Name} joined")
    Await Task.CompletedTask
End Function
```

### **2. 执行 RCON 命令**

```vb
' 执行单条命令
Dim result = Await Rcon.SendCommandAsync("say Hello!")
Logger.Info($"命令结果: {result}")

' 便捷方法
Await Rcon.Say("服务器公告")
Await Rcon.Tell("PlayerName", "私人消息")
Await Rcon.Give("PlayerName", "diamond", 64)
```

### **3. 显示游戏内容**

```vb
' 发送聊天消息
Await GameDisplay.SendChatMessage("PlayerName", "§aHello!")

' 显示标题
Await GameDisplay.ShowTitle(
    "PlayerName",
    "§e主标题",
    "§7副标题",
    10,  ' 淡入时间（tick）
    70,  ' 停留时间（tick）
    20   ' 淡出时间（tick）
)

' 显示 ActionBar
Await GameDisplay.ShowActionBar("PlayerName", "§6ActionBar 消息")
```

### **4. 配置文件管理**

```vb
' 定义配置类
Public Class MyConfig
    Public Property Enabled As Boolean = True
    Public Property MaxPlayers As Integer = 100
    Public Property WelcomeMessage As String = "Welcome!"
End Class

' 加载配置
Dim config = Await Config.LoadConfigAsync(Of MyConfig)("config")

' 保存配置
config.MaxPlayers = 200
Await Config.SaveConfigAsync("config", config)
```

### **5. 玩家数据读取**

```vb
' 读取玩家数据
Dim data = Await PlayerData.ReadPlayerDataAsync("Steve")

' 访问玩家信息
Logger.Info($"玩家: {data.Name}")
Logger.Info($"UUID: {data.Uuid}")
Logger.Info($"位置: {data.Position.X}, {data.Position.Y}, {data.Position.Z}")
Logger.Info($"生命值: {data.Health}")
Logger.Info($"经验等级: {data.Level}")

' 遍历物品栏
For Each item In data.Inventory
    Logger.Debug($"物品: {item.Id} x{item.Count}")
Next
```

### **6. 定时任务**

```vb
' 延迟执行（5秒后）
Scheduler.RunDelayed(Sub()
    Logger.Info("5秒后执行")
End Sub, TimeSpan.FromSeconds(5))

' 重复执行（每30秒）
Scheduler.RunRepeating(Sub()
    Logger.Info("每30秒执行一次")
End Sub, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30))

' 异步任务
Scheduler.RunAsync(Async Function()
    Dim players = Await Server.GetOnlinePlayersAsync()
    Logger.Info($"在线玩家数: {players.Count}")
End Function)
```

---

## 🔀 **VB.NET 与 C# 语法对照**

### **命名空间和导入**

| C# | VB.NET |
|-----|--------|
| `using System;` | `Imports System` |
| `namespace MyPlugin` | `Namespace MyPlugin` |

### **类定义**

| C# | VB.NET |
|-----|--------|
| `public class MyPlugin` | `Public Class MyPlugin` |
| `public class MyPlugin : PluginBase` | `Public Class MyPlugin`<br>`    Inherits PluginBase` |
| `public class MyCommand : ICommand` | `Public Class MyCommand`<br>`    Implements ICommand` |

### **方法定义**

| C# | VB.NET |
|-----|--------|
| `public async Task OnEnableAsync()` | `Public Async Function OnEnableAsync() As Task` |
| `private void DoSomething()` | `Private Sub DoSomething()` |
| `private async Task<string> GetDataAsync()` | `Private Async Function GetDataAsync() As Task(Of String)` |

### **属性**

| C# | VB.NET |
|-----|--------|
| `public string Name { get; set; }` | `Public Property Name As String` |
| `public string Name => "Test";` | `Public ReadOnly Property Name As String`<br>`    Get`<br>`        Return "Test"`<br>`    End Get`<br>`End Property` |

### **Lambda 表达式**

| C# | VB.NET |
|-----|--------|
| `x => x.Name` | `Function(x) x.Name` |
| `() => DoSomething()` | `Sub() DoSomething()` |
| `async () => await DoAsync()` | `Async Function() Await DoAsync()` |

### **泛型**

| C# | VB.NET |
|-----|--------|
| `List<string>` | `List(Of String)` |
| `Task<int>` | `Task(Of Integer)` |
| `Dictionary<string, int>` | `Dictionary(Of String, Integer)` |

### **字符串插值**

| C# | VB.NET |
|-----|--------|
| `$"Hello {name}"` | `$"Hello {name}"` |

### **空值检查**

| C# | VB.NET |
|-----|--------|
| `if (obj != null)` | `If obj IsNot Nothing Then` |
| `if (obj == null)` | `If obj Is Nothing Then` |

### **委托（事件处理）**

| C# | VB.NET |
|-----|--------|
| `Events.Subscribe<PlayerJoinedEvent>(OnPlayerJoined)` | `Events.Subscribe(Of PlayerJoinedEvent)(AddressOf OnPlayerJoined, EventPriority.Normal)` |

---

## 📚 **相关文档**

完整的插件开发信息，请参考：

- **[插件开发指南](./插件开发指南.md)** - 完整的 C# 插件开发流程（API 相同）
- **[配置文件](./配置文件.md)** - plugin.json 配置详解
- **[命令系统](../02-核心功能/命令系统.md)** - 命令注册和处理
- **[事件系统](../02-核心功能/事件系统.md)** - 事件订阅和发布
- **[权限系统](../02-核心功能/权限系统.md)** - 权限管理
- **[API 参考](../08-参考/API参考.md)** - 完整的 API 文档

---

## 🎯 **最佳实践**

1. **使用 PluginBase** - 简化代码，自动注入常用服务
2. **异步编程** - 正确使用 `Async Function` 和 `Await`
3. **错误处理** - 使用 `Try...Catch` 捕获异常
4. **资源清理** - 在 `OnDisableAsync` 中释放资源
5. **日志记录** - 合理使用不同级别的日志
6. **代码组织** - 按功能分类文件和类

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-12

