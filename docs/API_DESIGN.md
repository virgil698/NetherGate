# NetherGate API 设计文档

本文档详细描述 NetherGate 的核心 API 设计和使用方法。

---

## 📋 目录

- [协议层 API](#协议层-api)
- [插件 API](#插件-api)
- [事件 API](#事件-api)
- [命令 API](#命令-api)
- [配置 API](#配置-api)
- [日志 API](#日志-api)

---

## 🌐 协议层 API

### ServerConnection

管理与 Minecraft 服务器的 WebSocket 连接。

```csharp
namespace NetherGate.Core.Protocol.WebSocket
{
    public class ServerConnection : IDisposable
    {
        public ConnectionState State { get; }
        
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task<bool> ConnectAsync(ServerConnectionConfig config)
        {
            // 1. 建立 WebSocket 连接
            // 2. 发送认证令牌
            // 3. 等待连接确认
            // 4. 启动心跳检测
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            // 1. 停止心跳
            // 2. 关闭 WebSocket
            // 3. 清理资源
        }
        
        /// <summary>
        /// 发送原始 JSON-RPC 请求
        /// </summary>
        public async Task<string> SendRawAsync(string json)
        
        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// 接收到消息事件
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
    }
    
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Reconnecting,
        Failed
    }
}
```

### JsonRpcHandler

处理 JSON-RPC 2.0 协议。

```csharp
namespace NetherGate.Core.Protocol.JsonRpc
{
    public class JsonRpcHandler
    {
        /// <summary>
        /// 调用 JSON-RPC 方法
        /// </summary>
        public async Task<TResponse> InvokeAsync<TResponse>(
            string method, 
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var request = new JsonRpcRequest
            {
                Id = GenerateId(),
                Method = method,
                Params = parameters
            };
            
            var response = await SendAndWaitAsync(request, cancellationToken);
            
            if (response.Error != null)
            {
                throw new JsonRpcException(response.Error);
            }
            
            return JsonSerializer.Deserialize<TResponse>(response.Result);
        }
        
        /// <summary>
        /// 批量调用
        /// </summary>
        public async Task<List<JsonRpcResponse>> InvokeBatchAsync(
            List<JsonRpcRequest> requests,
            CancellationToken cancellationToken = default)
        
        /// <summary>
        /// 注册通知处理器
        /// </summary>
        public void RegisterNotificationHandler(
            string method, 
            Func<JsonRpcNotification, Task> handler)
        
        /// <summary>
        /// 取消注册通知处理器
        /// </summary>
        public void UnregisterNotificationHandler(string method)
    }
    
    // JSON-RPC 数据模型
    public record JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = "2.0";
        
        [JsonPropertyName("id")]
        public long? Id { get; init; }
        
        [JsonPropertyName("method")]
        public required string Method { get; init; }
        
        [JsonPropertyName("params")]
        public object? Params { get; init; }
    }
    
    public record JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = "2.0";
        
        [JsonPropertyName("id")]
        public long? Id { get; init; }
        
        [JsonPropertyName("result")]
        public JsonElement? Result { get; init; }
        
        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; init; }
    }
    
    public record JsonRpcNotification
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; init; } = "2.0";
        
        [JsonPropertyName("method")]
        public required string Method { get; init; }
        
        [JsonPropertyName("params")]
        public JsonElement? Params { get; init; }
    }
    
    public record JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }
        
        [JsonPropertyName("message")]
        public string Message { get; init; } = "";
        
        [JsonPropertyName("data")]
        public object? Data { get; init; }
    }
}
```

### MinecraftServerApi

封装服务端管理协议的所有方法。

```csharp
namespace NetherGate.Core.Protocol.Management
{
    public class MinecraftServerApi
    {
        private readonly JsonRpcHandler _rpcHandler;
        
        public MinecraftServerApi(JsonRpcHandler rpcHandler)
        {
            _rpcHandler = rpcHandler;
        }
        
        #region 白名单管理
        
        /// <summary>
        /// 获取白名单
        /// </summary>
        public async Task<List<PlayerDto>> GetAllowlistAsync()
        {
            return await _rpcHandler.InvokeAsync<List<PlayerDto>>("allowlist");
        }
        
        /// <summary>
        /// 设置白名单（替换整个列表）
        /// </summary>
        public async Task SetAllowlistAsync(List<PlayerDto> players)
        {
            await _rpcHandler.InvokeAsync<object>("allowlist/set", new { list = players });
        }
        
        /// <summary>
        /// 添加玩家到白名单
        /// </summary>
        public async Task<bool> AddToAllowlistAsync(PlayerDto player)
        {
            return await _rpcHandler.InvokeAsync<bool>("allowlist/add", player);
        }
        
        /// <summary>
        /// 从白名单移除玩家
        /// </summary>
        public async Task<bool> RemoveFromAllowlistAsync(PlayerDto player)
        {
            return await _rpcHandler.InvokeAsync<bool>("allowlist/remove", player);
        }
        
        /// <summary>
        /// 清空白名单
        /// </summary>
        public async Task ClearAllowlistAsync()
        {
            await _rpcHandler.InvokeAsync<object>("allowlist/clear");
        }
        
        #endregion
        
        #region 封禁管理
        
        /// <summary>
        /// 获取玩家封禁列表
        /// </summary>
        public async Task<List<UserBanDto>> GetBansAsync()
        {
            return await _rpcHandler.InvokeAsync<List<UserBanDto>>("bans");
        }
        
        /// <summary>
        /// 封禁玩家
        /// </summary>
        public async Task<bool> AddBanAsync(UserBanDto ban)
        {
            return await _rpcHandler.InvokeAsync<bool>("bans/add", ban);
        }
        
        /// <summary>
        /// 解封玩家
        /// </summary>
        public async Task<bool> RemoveBanAsync(PlayerDto player)
        {
            return await _rpcHandler.InvokeAsync<bool>("bans/remove", player);
        }
        
        /// <summary>
        /// 获取 IP 封禁列表
        /// </summary>
        public async Task<List<IpBanDto>> GetIpBansAsync()
        {
            return await _rpcHandler.InvokeAsync<List<IpBanDto>>("ip_bans");
        }
        
        /// <summary>
        /// 封禁 IP
        /// </summary>
        public async Task<bool> AddIpBanAsync(IpBanDto ipBan)
        {
            return await _rpcHandler.InvokeAsync<bool>("ip_bans/add", ipBan);
        }
        
        /// <summary>
        /// 解封 IP
        /// </summary>
        public async Task<bool> RemoveIpBanAsync(string ip)
        {
            return await _rpcHandler.InvokeAsync<bool>("ip_bans/remove", new { ip });
        }
        
        #endregion
        
        #region 玩家管理
        
        /// <summary>
        /// 获取在线玩家列表
        /// </summary>
        public async Task<List<PlayerDto>> GetPlayersAsync()
        {
            return await _rpcHandler.InvokeAsync<List<PlayerDto>>("players");
        }
        
        /// <summary>
        /// 踢出玩家
        /// </summary>
        public async Task<bool> KickPlayerAsync(PlayerDto player, string? reason = null)
        {
            return await _rpcHandler.InvokeAsync<bool>("players/kick", new 
            { 
                player, 
                reason 
            });
        }
        
        #endregion
        
        #region 管理员管理
        
        /// <summary>
        /// 获取管理员列表
        /// </summary>
        public async Task<List<OperatorDto>> GetOperatorsAsync()
        {
            return await _rpcHandler.InvokeAsync<List<OperatorDto>>("operators");
        }
        
        /// <summary>
        /// 添加管理员
        /// </summary>
        public async Task<bool> AddOperatorAsync(OperatorDto op)
        {
            return await _rpcHandler.InvokeAsync<bool>("operators/add", op);
        }
        
        /// <summary>
        /// 移除管理员
        /// </summary>
        public async Task<bool> RemoveOperatorAsync(PlayerDto player)
        {
            return await _rpcHandler.InvokeAsync<bool>("operators/remove", player);
        }
        
        #endregion
        
        #region 服务器管理
        
        /// <summary>
        /// 获取服务器状态
        /// </summary>
        public async Task<ServerState> GetServerStatusAsync()
        {
            return await _rpcHandler.InvokeAsync<ServerState>("server/status");
        }
        
        /// <summary>
        /// 保存世界
        /// </summary>
        public async Task SaveWorldAsync()
        {
            await _rpcHandler.InvokeAsync<object>("server/save");
        }
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        public async Task StopServerAsync()
        {
            await _rpcHandler.InvokeAsync<object>("server/stop");
        }
        
        /// <summary>
        /// 发送系统消息
        /// </summary>
        public async Task SendSystemMessageAsync(string message)
        {
            await _rpcHandler.InvokeAsync<object>("server/system_message", new { message });
        }
        
        #endregion
        
        #region 游戏规则管理
        
        /// <summary>
        /// 获取所有游戏规则
        /// </summary>
        public async Task<Dictionary<string, TypedRule>> GetGameRulesAsync()
        {
            return await _rpcHandler.InvokeAsync<Dictionary<string, TypedRule>>("gamerules");
        }
        
        /// <summary>
        /// 更新游戏规则
        /// </summary>
        public async Task<bool> UpdateGameRuleAsync(string rule, object value)
        {
            return await _rpcHandler.InvokeAsync<bool>("gamerules/update", new 
            { 
                rule, 
                value 
            });
        }
        
        #endregion
        
        #region 服务器设置（只读）
        
        /// <summary>
        /// 获取服务器设置
        /// </summary>
        public async Task<T> GetServerSettingAsync<T>(string settingName)
        {
            return await _rpcHandler.InvokeAsync<T>($"serversettings/{settingName}");
        }
        
        /// <summary>
        /// 设置服务器设置
        /// </summary>
        public async Task<bool> SetServerSettingAsync(string settingName, object value)
        {
            return await _rpcHandler.InvokeAsync<bool>($"serversettings/{settingName}/set", value);
        }
        
        #endregion
    }
    
    // DTO 定义
    public record PlayerDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        
        [JsonPropertyName("uuid")]
        public required Guid Uuid { get; init; }
    }
    
    public record UserBanDto
    {
        [JsonPropertyName("player")]
        public required PlayerDto Player { get; init; }
        
        [JsonPropertyName("reason")]
        public string? Reason { get; init; }
        
        [JsonPropertyName("expires")]
        public DateTime? Expires { get; init; }
        
        [JsonPropertyName("source")]
        public string? Source { get; init; }
    }
    
    public record IpBanDto
    {
        [JsonPropertyName("ip")]
        public required string Ip { get; init; }
        
        [JsonPropertyName("reason")]
        public string? Reason { get; init; }
        
        [JsonPropertyName("expires")]
        public DateTime? Expires { get; init; }
        
        [JsonPropertyName("source")]
        public string? Source { get; init; }
    }
    
    public record OperatorDto
    {
        [JsonPropertyName("player")]
        public required PlayerDto Player { get; init; }
        
        [JsonPropertyName("level")]
        public int Level { get; init; } = 4;
        
        [JsonPropertyName("bypassPlayerLimit")]
        public bool BypassPlayerLimit { get; init; }
    }
    
    public record ServerState
    {
        [JsonPropertyName("started")]
        public bool Started { get; init; }
        
        [JsonPropertyName("version")]
        public required VersionInfo Version { get; init; }
    }
    
    public record VersionInfo
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        
        [JsonPropertyName("protocol")]
        public int Protocol { get; init; }
    }
    
    public record TypedRule
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }
        
        [JsonPropertyName("value")]
        public required object Value { get; init; }
    }
}
```

---

## 🔌 插件 API

### IPlugin 接口

所有插件必须实现的接口。

```csharp
namespace NetherGate.API
{
    public interface IPlugin
    {
        /// <summary>
        /// 插件元数据
        /// </summary>
        PluginMetadata Metadata { get; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        PluginState State { get; }
        
        /// <summary>
        /// 插件加载时调用（仅一次）
        /// </summary>
        Task OnLoadAsync();
        
        /// <summary>
        /// 插件启用时调用
        /// </summary>
        Task OnEnableAsync();
        
        /// <summary>
        /// 插件禁用时调用
        /// </summary>
        Task OnDisableAsync();
        
        /// <summary>
        /// 插件卸载时调用（仅一次）
        /// </summary>
        Task OnUnloadAsync();
    }
    
    public enum PluginState
    {
        Unloaded,
        Loaded,
        Enabled,
        Disabled,
        Failed
    }
}
```

### PluginBase 抽象类

插件的推荐基类，提供常用功能。

```csharp
namespace NetherGate.API
{
    public abstract class PluginBase : IPlugin
    {
        /// <summary>
        /// 插件元数据
        /// </summary>
        public PluginMetadata Metadata { get; internal set; } = null!;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public PluginState State { get; internal set; }
        
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; private set; } = null!;
        
        /// <summary>
        /// 配置管理
        /// </summary>
        protected IPluginConfig Config { get; private set; } = null!;
        
        /// <summary>
        /// 服务器 API
        /// </summary>
        protected IServerApi Server { get; private set; } = null!;
        
        /// <summary>
        /// 事件总线
        /// </summary>
        protected IEventBus Events { get; private set; } = null!;
        
        /// <summary>
        /// 命令管理器
        /// </summary>
        protected ICommandManager Commands { get; private set; } = null!;
        
        /// <summary>
        /// 数据目录（用于存储插件数据文件，如数据库等）
        /// 位置：plugins/<plugin-id>/data/
        /// </summary>
        protected string DataDirectory { get; private set; } = null!;
        
        /// <summary>
        /// 配置目录（插件配置文件存放位置）
        /// 位置：config/<plugin-id>/
        /// 注意：通常使用 Config 属性访问配置，无需直接操作此目录
        /// </summary>
        protected string ConfigDirectory { get; private set; } = null!;
        
        // 生命周期方法（子类可重写）
        public virtual Task OnLoadAsync() => Task.CompletedTask;
        public virtual Task OnEnableAsync() => Task.CompletedTask;
        public virtual Task OnDisableAsync() => Task.CompletedTask;
        public virtual Task OnUnloadAsync() => Task.CompletedTask;
        
        /// <summary>
        /// 获取其他插件的实例
        /// </summary>
        protected T? GetPlugin<T>() where T : class, IPlugin
        {
            return PluginManager.GetPlugin<T>();
        }
        
        /// <summary>
        /// 检查插件是否已加载
        /// </summary>
        protected bool IsPluginLoaded(string pluginId)
        {
            return PluginManager.IsPluginLoaded(pluginId);
        }
    }
}
```

### PluginMetadata

插件元数据。

```csharp
namespace NetherGate.API
{
    public record PluginMetadata
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Version { get; init; }
        public required string Author { get; init; }
        public string? Description { get; init; }
        public string? Website { get; init; }
        public string? Repository { get; init; }
        public string? License { get; init; }
        public List<PluginDependency> Dependencies { get; init; } = new();
        public List<string> Permissions { get; init; } = new();
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
    
    public record PluginDependency
    {
        public required string Id { get; init; }
        public required string Version { get; init; }
        public bool Required { get; init; } = true;
    }
}
```

---

## 🎯 事件 API

### IEventBus 接口

```csharp
namespace NetherGate.API
{
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<TEvent>(
            IPlugin plugin, 
            EventPriority priority, 
            EventHandler<TEvent> handler) 
            where TEvent : class;
        
        /// <summary>
        /// 订阅事件（异步）
        /// </summary>
        void Subscribe<TEvent>(
            IPlugin plugin, 
            EventPriority priority, 
            AsyncEventHandler<TEvent> handler) 
            where TEvent : class;
        
        /// <summary>
        /// 取消订阅特定事件
        /// </summary>
        void Unsubscribe<TEvent>(IPlugin plugin) 
            where TEvent : class;
        
        /// <summary>
        /// 取消订阅所有事件
        /// </summary>
        void UnsubscribeAll(IPlugin plugin);
        
        /// <summary>
        /// 分发事件
        /// </summary>
        Task DispatchAsync<TEvent>(TEvent eventData) 
            where TEvent : class;
    }
    
    public enum EventPriority
    {
        Lowest = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Highest = 4,
        Monitor = 5  // 仅监听，不应修改事件
    }
    
    public delegate Task AsyncEventHandler<TEvent>(object? sender, TEvent e) 
        where TEvent : class;
}
```

### 事件基类

```csharp
namespace NetherGate.API.Events
{
    public abstract class EventBase
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
    
    public abstract class CancellableEvent : EventBase
    {
        public bool IsCancelled { get; set; }
        
        public void Cancel() => IsCancelled = true;
    }
}
```

### 服务器事件

```csharp
namespace NetherGate.API.Events
{
    /// <summary>
    /// 服务器启动事件
    /// </summary>
    public class ServerStartedEvent : EventBase
    {
        public required ServerState State { get; init; }
    }
    
    /// <summary>
    /// 服务器停止事件
    /// </summary>
    public class ServerStoppedEvent : EventBase
    {
        public required string Reason { get; init; }
    }
    
    /// <summary>
    /// 服务器状态变化事件
    /// </summary>
    public class ServerStatusChangedEvent : EventBase
    {
        public required ServerState OldState { get; init; }
        public required ServerState NewState { get; init; }
    }
}
```

### 玩家事件

```csharp
namespace NetherGate.API.Events
{
    /// <summary>
    /// 玩家加入事件
    /// </summary>
    public class PlayerJoinedEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
    }
    
    /// <summary>
    /// 玩家离开事件
    /// </summary>
    public class PlayerLeftEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
    }
    
    /// <summary>
    /// 玩家被踢出事件
    /// </summary>
    public class PlayerKickedEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
        public string? Reason { get; init; }
    }
}
```

### 管理员事件

```csharp
namespace NetherGate.API.Events
{
    /// <summary>
    /// 管理员添加事件
    /// </summary>
    public class OperatorAddedEvent : EventBase
    {
        public required OperatorDto Operator { get; init; }
    }
    
    /// <summary>
    /// 管理员移除事件
    /// </summary>
    public class OperatorRemovedEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
    }
}
```

### 白名单事件

```csharp
namespace NetherGate.API.Events
{
    /// <summary>
    /// 玩家添加到白名单事件
    /// </summary>
    public class AllowlistAddedEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
    }
    
    /// <summary>
    /// 玩家从白名单移除事件
    /// </summary>
    public class AllowlistRemovedEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
    }
}
```

### 封禁事件

```csharp
namespace NetherGate.API.Events
{
    /// <summary>
    /// 玩家封禁事件
    /// </summary>
    public class BanAddedEvent : EventBase
    {
        public required UserBanDto Ban { get; init; }
    }
    
    /// <summary>
    /// 玩家解封事件
    /// </summary>
    public class BanRemovedEvent : EventBase
    {
        public required PlayerDto Player { get; init; }
    }
    
    /// <summary>
    /// IP 封禁事件
    /// </summary>
    public class IpBanAddedEvent : EventBase
    {
        public required IpBanDto IpBan { get; init; }
    }
    
    /// <summary>
    /// IP 解封事件
    /// </summary>
    public class IpBanRemovedEvent : EventBase
    {
        public required string Ip { get; init; }
    }
}
```

### 游戏规则事件

```csharp
namespace NetherGate.API.Events
{
    /// <summary>
    /// 游戏规则更新事件
    /// </summary>
    public class GameRuleUpdatedEvent : EventBase
    {
        public required string RuleName { get; init; }
        public required TypedRule NewValue { get; init; }
    }
}
```

---

## ⌨️ 命令 API

### ICommandManager 接口

```csharp
namespace NetherGate.API
{
    public interface ICommandManager
    {
        /// <summary>
        /// 注册命令
        /// </summary>
        void Register(IPlugin plugin, CommandDefinition command);
        
        /// <summary>
        /// 取消注册命令
        /// </summary>
        void Unregister(string commandName);
        
        /// <summary>
        /// 取消注册插件的所有命令
        /// </summary>
        void UnregisterAll(IPlugin plugin);
        
        /// <summary>
        /// 执行命令
        /// </summary>
        Task<CommandResult> ExecuteAsync(string commandLine, CommandSource source);
        
        /// <summary>
        /// 获取命令建议
        /// </summary>
        List<string> GetSuggestions(string partial);
        
        /// <summary>
        /// 检查命令是否存在
        /// </summary>
        bool CommandExists(string commandName);
    }
}
```

### CommandDefinition

```csharp
namespace NetherGate.API
{
    public record CommandDefinition
    {
        public required string Name { get; init; }
        public string Description { get; init; } = "";
        public List<string> Aliases { get; init; } = new();
        public string? Permission { get; init; }
        public required Func<CommandContext, Task<CommandResult>> Handler { get; init; }
        public List<CommandParameter> Parameters { get; init; } = new();
    }
    
    public record CommandParameter
    {
        public required string Name { get; init; }
        public Type Type { get; init; } = typeof(string);
        public bool Required { get; init; } = true;
        public object? DefaultValue { get; init; }
        public string Description { get; init; } = "";
    }
}
```

### CommandContext 和 CommandResult

```csharp
namespace NetherGate.API
{
    public class CommandContext
    {
        public required string CommandName { get; init; }
        public required List<string> Args { get; init; }
        public required CommandSource Source { get; init; }
        public IServerApi Server { get; init; } = null!;
        
        /// <summary>
        /// 获取参数
        /// </summary>
        public T GetArg<T>(int index, T defaultValue = default!)
        {
            if (index >= Args.Count)
                return defaultValue;
            
            return (T)Convert.ChangeType(Args[index], typeof(T));
        }
        
        /// <summary>
        /// 发送消息给命令执行者
        /// </summary>
        public async Task ReplyAsync(string message)
        {
            if (Source.Type == CommandSourceType.Server)
            {
                await Server.SendSystemMessageAsync(message);
            }
        }
    }
    
    public record CommandSource
    {
        public CommandSourceType Type { get; init; }
        public PlayerDto? Player { get; init; }
    }
    
    public enum CommandSourceType
    {
        Console,
        Server,
        Player
    }
    
    public record CommandResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public object? Data { get; init; }
        
        public static CommandResult Ok(string? message = null, object? data = null)
            => new() { Success = true, Message = message, Data = data };
        
        public static CommandResult Error(string message)
            => new() { Success = false, Message = message };
    }
}
```

---

## ⚙️ 配置 API

### IPluginConfig 接口

```csharp
namespace NetherGate.API
{
    public interface IPluginConfig
    {
        /// <summary>
        /// 获取配置值
        /// </summary>
        T Get<T>(string key, T defaultValue = default!);
        
        /// <summary>
        /// 设置配置值
        /// </summary>
        void Set<T>(string key, T value);
        
        /// <summary>
        /// 检查配置键是否存在
        /// </summary>
        bool Has(string key);
        
        /// <summary>
        /// 删除配置键
        /// </summary>
        void Remove(string key);
        
        /// <summary>
        /// 保存配置
        /// </summary>
        Task SaveAsync();
        
        /// <summary>
        /// 重载配置
        /// </summary>
        Task ReloadAsync();
        
        /// <summary>
        /// 获取配置对象
        /// </summary>
        T GetSection<T>(string section) where T : class, new();
        
        /// <summary>
        /// 设置配置对象
        /// </summary>
        void SetSection<T>(string section, T value) where T : class;
    }
}
```

使用示例：

```csharp
public class MyPlugin : PluginBase
{
    private MyPluginConfig _config = null!;
    
    public override async Task OnLoadAsync()
    {
        // 从配置文件加载
        _config = Config.GetSection<MyPluginConfig>("settings");
        
        // 或者逐个读取
        var welcomeMessage = Config.Get<string>("welcome_message", "Welcome!");
        var maxPlayers = Config.Get<int>("max_players", 100);
    }
    
    public override async Task OnDisableAsync()
    {
        // 保存配置
        Config.SetSection("settings", _config);
        await Config.SaveAsync();
    }
}

public class MyPluginConfig
{
    public string WelcomeMessage { get; set; } = "Welcome!";
    public int MaxPlayers { get; set; } = 100;
    public List<string> AllowedCommands { get; set; } = new();
}
```

---

## 📝 日志 API

### ILogger 接口

```csharp
namespace NetherGate.API
{
    public interface ILogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
        void Fatal(string message, Exception? exception = null);
        
        void Log(LogLevel level, string message, Exception? exception = null);
        
        bool IsEnabled(LogLevel level);
    }
    
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }
}
```

使用示例：

```csharp
public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        Logger.Info("插件正在启动...");
        
        try
        {
            await DoSomething();
        }
        catch (Exception ex)
        {
            Logger.Error("发生错误", ex);
        }
        
        Logger.Debug("调试信息");
    }
}
```

---

## 📦 完整插件示例

```csharp
using NetherGate.API;
using NetherGate.API.Events;

namespace ExamplePlugin
{
    public class ExamplePlugin : PluginBase
    {
        private ExampleConfig _config = null!;
        
        public override async Task OnLoadAsync()
        {
            Logger.Info("ExamplePlugin 正在加载...");
            
            // 加载配置
            _config = Config.GetSection<ExampleConfig>("config") ?? new();
            Logger.Info($"欢迎消息: {_config.WelcomeMessage}");
        }
        
        public override async Task OnEnableAsync()
        {
            Logger.Info("ExamplePlugin 正在启用...");
            
            // 订阅事件
            Events.Subscribe<PlayerJoinedEvent>(this, EventPriority.Normal, OnPlayerJoin);
            Events.Subscribe<PlayerLeftEvent>(this, EventPriority.Normal, OnPlayerLeave);
            
            // 注册命令
            Commands.Register(this, new CommandDefinition
            {
                Name = "example",
                Description = "示例命令",
                Aliases = new List<string> { "ex" },
                Handler = HandleExampleCommand
            });
            
            Logger.Info("ExamplePlugin 已启用!");
        }
        
        private async Task OnPlayerJoin(object? sender, PlayerJoinedEvent e)
        {
            Logger.Info($"玩家 {e.Player.Name} 加入了服务器");
            
            if (_config.SendWelcomeMessage)
            {
                await Server.SendSystemMessageAsync(
                    _config.WelcomeMessage.Replace("{player}", e.Player.Name));
            }
        }
        
        private async Task OnPlayerLeave(object? sender, PlayerLeftEvent e)
        {
            Logger.Info($"玩家 {e.Player.Name} 离开了服务器");
        }
        
        private async Task<CommandResult> HandleExampleCommand(CommandContext ctx)
        {
            var action = ctx.Args.FirstOrDefault();
            
            switch (action)
            {
                case "status":
                    var state = await Server.GetStatusAsync();
                    await ctx.ReplyAsync($"服务器版本: {state.Version.Name}");
                    return CommandResult.Ok();
                    
                case "players":
                    var players = await Server.GetPlayersAsync();
                    await ctx.ReplyAsync($"在线玩家: {players.Count}");
                    return CommandResult.Ok();
                    
                default:
                    return CommandResult.Error("未知操作");
            }
        }
        
        public override async Task OnDisableAsync()
        {
            Logger.Info("ExamplePlugin 正在禁用...");
            
            // 取消所有订阅
            Events.UnsubscribeAll(this);
            Commands.UnregisterAll(this);
            
            // 保存配置
            Config.SetSection("config", _config);
            await Config.SaveAsync();
        }
        
        public override async Task OnUnloadAsync()
        {
            Logger.Info("ExamplePlugin 已卸载");
        }
    }
    
    public class ExampleConfig
    {
        public bool SendWelcomeMessage { get; set; } = true;
        public string WelcomeMessage { get; set; } = "欢迎 {player} 加入服务器!";
    }
}
```

---

## 🔗 服务器 API 完整接口

```csharp
namespace NetherGate.API
{
    public interface IServerApi
    {
        // 白名单
        Task<List<PlayerDto>> GetAllowlistAsync();
        Task SetAllowlistAsync(List<PlayerDto> players);
        Task<bool> AddToAllowlistAsync(PlayerDto player);
        Task<bool> RemoveFromAllowlistAsync(PlayerDto player);
        Task ClearAllowlistAsync();
        
        // 封禁
        Task<List<UserBanDto>> GetBansAsync();
        Task SetBansAsync(List<UserBanDto> bans);
        Task<bool> AddBanAsync(UserBanDto ban);
        Task<bool> RemoveBanAsync(PlayerDto player);
        Task ClearBansAsync();
        Task<List<IpBanDto>> GetIpBansAsync();
        Task SetIpBansAsync(List<IpBanDto> ipBans);
        Task<bool> AddIpBanAsync(IpBanDto ipBan);
        Task<bool> RemoveIpBanAsync(string ip);
        Task ClearIpBansAsync();
        
        // 玩家
        Task<List<PlayerDto>> GetPlayersAsync();
        Task<bool> KickPlayerAsync(PlayerDto player, string? reason = null);
        
        // 管理员
        Task<List<OperatorDto>> GetOperatorsAsync();
        Task SetOperatorsAsync(List<OperatorDto> operators);
        Task<bool> AddOperatorAsync(OperatorDto op);
        Task<bool> RemoveOperatorAsync(PlayerDto player);
        Task ClearOperatorsAsync();
        
        // 服务器
        Task<ServerState> GetStatusAsync();
        Task SaveWorldAsync();
        Task StopServerAsync();
        Task SendSystemMessageAsync(string message);
        
        // 游戏规则
        Task<Dictionary<string, TypedRule>> GetGameRulesAsync();
        Task<bool> UpdateGameRuleAsync(string rule, object value);
        
        // 服务器设置
        Task<T> GetServerSettingAsync<T>(string settingName);
        Task<bool> SetServerSettingAsync(string settingName, object value);
    }
}
```

---

这个 API 设计文档提供了 NetherGate 核心 API 的详细说明和使用示例。插件开发者可以参考这些接口来开发自己的插件。

