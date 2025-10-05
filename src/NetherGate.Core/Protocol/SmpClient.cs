using NetherGate.API.Configuration;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Protocol;

/// <summary>
/// 服务端管理协议 (SMP) 客户端
/// </summary>
public class SmpClient : ISmpApi, IDisposable
{
    private readonly ServerConnectionConfig _config;
    private readonly ILogger _logger;
    private readonly JsonRpcHandler _rpcHandler;
    private readonly IEventBus? _eventBus;
    private WebSocketClient? _webSocket;
    private bool _disposed;

    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public bool IsConnected => _webSocket?.IsConnected ?? false;
    public ConnectionState State => _webSocket?.State ?? ConnectionState.Disconnected;

    public SmpClient(ServerConnectionConfig config, ILogger logger, IEventBus? eventBus = null)
    {
        _config = config;
        _logger = logger;
        _rpcHandler = new JsonRpcHandler(logger);
        _eventBus = eventBus;
    }

    /// <summary>
    /// 连接到 SMP 服务器
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SmpClient));

        if (IsConnected)
        {
            _logger.Warning("SMP 已连接");
            return true;
        }

        try
        {
            // 构建 WebSocket URL
            var protocol = _config.UseTls ? "wss" : "ws";
            var uri = $"{protocol}://{_config.Host}:{_config.Port}";

            _logger.Info($"正在连接 SMP 服务器: {uri}");

            // 创建 WebSocket 客户端
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_config.Secret}"
            };

            _webSocket = new WebSocketClient(
                uri,
                headers,
                _config.ReconnectInterval,
                _logger);

            // 订阅事件
            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.StateChanged += OnStateChanged;
            _webSocket.ErrorOccurred += OnErrorOccurred;

            // 注册 SMP 通知处理器
            RegisterNotificationHandlers();

            // 连接
            var connected = await _webSocket.ConnectAsync();

            if (connected)
            {
                _logger.Info("SMP 连接成功");
            }

            return connected;
        }
        catch (Exception ex)
        {
            _logger.Error($"SMP 连接失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_webSocket != null)
        {
            _logger.Info("正在断开 SMP 连接...");
            await _webSocket.DisconnectAsync();
        }
    }

    /// <summary>
    /// 调用 SMP 方法
    /// </summary>
    public async Task<TResponse?> InvokeAsync<TResponse>(string method, object? parameters = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SmpClient));

        if (!IsConnected)
            throw new InvalidOperationException("SMP 未连接");

        return await _rpcHandler.InvokeAsync<TResponse>(
            method,
            parameters,
            async (json) => await _webSocket!.SendAsync(json),
            TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 发送通知
    /// </summary>
    public async Task SendNotificationAsync(string method, object? parameters = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SmpClient));

        if (!IsConnected)
            throw new InvalidOperationException("SMP 未连接");

        await _rpcHandler.SendNotificationAsync(
            method,
            parameters,
            async (json) => await _webSocket!.SendAsync(json));
    }

    /// <summary>
    /// 注册通知处理器
    /// </summary>
    public void RegisterNotificationHandler(string method, Action<JsonRpcNotification> handler)
    {
        _rpcHandler.RegisterNotificationHandler(method, handler);
    }

    /// <summary>
    /// 注销通知处理器
    /// </summary>
    public void UnregisterNotificationHandler(string method, Action<JsonRpcNotification> handler)
    {
        _rpcHandler.UnregisterNotificationHandler(method, handler);
    }

    /// <summary>
    /// 处理收到的消息
    /// </summary>
    private void OnMessageReceived(object? sender, string message)
    {
        _rpcHandler.HandleMessage(message);
    }

    /// <summary>
    /// 处理状态变更
    /// </summary>
    private async void OnStateChanged(object? sender, ConnectionState state)
    {
        _logger.Info($"SMP 状态变更: {state}");
        ConnectionStateChanged?.Invoke(this, state);

        // 发布事件
        if (_eventBus != null)
        {
            try
            {
                switch (state)
                {
                    case ConnectionState.Connected:
                        await _eventBus.PublishAsync(new SmpConnectedEvent());
                        break;
                    case ConnectionState.Disconnected:
                        await _eventBus.PublishAsync(new SmpDisconnectedEvent());
                        break;
                    case ConnectionState.Reconnecting:
                        // 重连事件会在 WebSocketClient 中处理
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"发布 SMP 事件失败: {ex.Message}", ex);
            }
        }

        if (state == ConnectionState.Disconnected)
        {
            _rpcHandler.ClearPendingRequests();
        }
    }

    /// <summary>
    /// 处理错误
    /// </summary>
    private void OnErrorOccurred(object? sender, Exception ex)
    {
        _logger.Error($"SMP 错误: {ex.Message}", ex);
    }

    // ========== ISmpApi 实现 ==========

    #region 白名单管理

    public async Task<List<PlayerDto>> GetAllowlistAsync()
    {
        return await InvokeAsync<List<PlayerDto>>("allowlist") ?? new List<PlayerDto>();
    }

    public async Task SetAllowlistAsync(List<PlayerDto> players)
    {
        await InvokeAsync<object>("allowlist/set", players);
    }

    public async Task AddToAllowlistAsync(PlayerDto player)
    {
        await InvokeAsync<object>("allowlist/add", player);
    }

    public async Task RemoveFromAllowlistAsync(PlayerDto player)
    {
        await InvokeAsync<object>("allowlist/remove", player);
    }

    public async Task ClearAllowlistAsync()
    {
        await InvokeAsync<object>("allowlist/clear");
    }

    #endregion

    #region 封禁管理

    public async Task<List<UserBanDto>> GetBansAsync()
    {
        return await InvokeAsync<List<UserBanDto>>("bans") ?? new List<UserBanDto>();
    }

    public async Task SetBansAsync(List<UserBanDto> bans)
    {
        await InvokeAsync<object>("bans/set", bans);
    }

    public async Task AddBanAsync(UserBanDto ban)
    {
        await InvokeAsync<object>("bans/add", ban);
    }

    public async Task RemoveBanAsync(PlayerDto player)
    {
        await InvokeAsync<object>("bans/remove", player);
    }

    public async Task ClearBansAsync()
    {
        await InvokeAsync<object>("bans/clear");
    }

    #endregion

    #region IP 封禁管理

    public async Task<List<IpBanDto>> GetIpBansAsync()
    {
        return await InvokeAsync<List<IpBanDto>>("ip_bans") ?? new List<IpBanDto>();
    }

    public async Task SetIpBansAsync(List<IpBanDto> bans)
    {
        await InvokeAsync<object>("ip_bans/set", bans);
    }

    public async Task AddIpBanAsync(IpBanDto ban)
    {
        await InvokeAsync<object>("ip_bans/add", ban);
    }

    public async Task RemoveIpBanAsync(string ip)
    {
        await InvokeAsync<object>("ip_bans/remove", new { ip });
    }

    public async Task ClearIpBansAsync()
    {
        await InvokeAsync<object>("ip_bans/clear");
    }

    #endregion

    #region 玩家管理

    public async Task<List<PlayerDto>> GetPlayersAsync()
    {
        return await InvokeAsync<List<PlayerDto>>("players") ?? new List<PlayerDto>();
    }

    public async Task KickPlayerAsync(string playerName, string? reason = null)
    {
        object parameters = reason == null
            ? (object)new { playerName }
            : new { playerName, reason };

        await InvokeAsync<object>("players/kick", parameters);
    }

    #endregion

    #region 管理员管理

    public async Task<List<OperatorDto>> GetOperatorsAsync()
    {
        return await InvokeAsync<List<OperatorDto>>("operators") ?? new List<OperatorDto>();
    }

    public async Task SetOperatorsAsync(List<OperatorDto> operators)
    {
        await InvokeAsync<object>("operators/set", operators);
    }

    public async Task AddOperatorAsync(OperatorDto op)
    {
        await InvokeAsync<object>("operators/add", op);
    }

    public async Task RemoveOperatorAsync(PlayerDto player)
    {
        await InvokeAsync<object>("operators/remove", player);
    }

    public async Task ClearOperatorsAsync()
    {
        await InvokeAsync<object>("operators/clear");
    }

    #endregion

    #region 服务器管理

    public async Task<ServerState> GetServerStatusAsync()
    {
        return await InvokeAsync<ServerState>("server/status") ?? new ServerState();
    }

    public async Task SaveWorldAsync()
    {
        await InvokeAsync<object>("server/save");
    }

    public async Task StopServerAsync()
    {
        await InvokeAsync<object>("server/stop");
    }

    public async Task SendSystemMessageAsync(string message)
    {
        await InvokeAsync<object>("server/system_message", new { message });
    }

    #endregion

    #region 游戏规则管理

    public async Task<Dictionary<string, TypedRule>> GetGameRulesAsync()
    {
        return await InvokeAsync<Dictionary<string, TypedRule>>("gamerules") 
            ?? new Dictionary<string, TypedRule>();
    }

    public async Task UpdateGameRuleAsync(string rule, object value)
    {
        await InvokeAsync<object>("gamerules/update", new { rule, value });
    }

    #endregion

    #region 服务器设置

    public async Task<Dictionary<string, object?>> GetServerSettingsAsync()
    {
        return await InvokeAsync<Dictionary<string, object?>>("serversettings") 
            ?? new Dictionary<string, object?>();
    }

    public async Task<object?> GetServerSettingAsync(string key)
    {
        return await InvokeAsync<object>($"serversettings/{key}");
    }

    public async Task SetServerSettingAsync(string key, object value)
    {
        await InvokeAsync<object>($"serversettings/{key}/set", value);
    }

    #endregion

    #region SMP 通知处理

    /// <summary>
    /// 注册所有 SMP 通知处理器
    /// </summary>
    private void RegisterNotificationHandlers()
    {
        // 玩家事件
        _rpcHandler.RegisterNotificationHandler("players/joined", HandlePlayerJoined);
        _rpcHandler.RegisterNotificationHandler("players/left", HandlePlayerLeft);
        
        // 管理员事件
        _rpcHandler.RegisterNotificationHandler("operators/added", HandleOperatorAdded);
        _rpcHandler.RegisterNotificationHandler("operators/removed", HandleOperatorRemoved);
        
        // 白名单事件
        _rpcHandler.RegisterNotificationHandler("allowlist/added", HandleAllowlistAdded);
        _rpcHandler.RegisterNotificationHandler("allowlist/removed", HandleAllowlistRemoved);
        
        // 封禁事件
        _rpcHandler.RegisterNotificationHandler("bans/added", HandleBanAdded);
        _rpcHandler.RegisterNotificationHandler("bans/removed", HandleBanRemoved);
        
        // IP 封禁事件
        _rpcHandler.RegisterNotificationHandler("ip_bans/added", HandleIpBanAdded);
        _rpcHandler.RegisterNotificationHandler("ip_bans/removed", HandleIpBanRemoved);
        
        // 游戏规则事件
        _rpcHandler.RegisterNotificationHandler("gamerules/updated", HandleGameRuleUpdated);
        
        // 服务器状态心跳
        _rpcHandler.RegisterNotificationHandler("server/status", HandleServerStatus);
        
        _logger.Debug("已注册所有 SMP 通知处理器");
    }

    /// <summary>
    /// 处理玩家加入通知
    /// </summary>
    private async void HandlePlayerJoined(JsonRpcNotification notification)
    {
        try
        {
            var player = notification.GetParams<PlayerDto>();
            if (player != null && _eventBus != null)
            {
                _logger.Debug($"玩家加入: {player.Name}");
                await _eventBus.PublishAsync(new PlayerJoinedEvent { Player = player });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理玩家加入通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理玩家离开通知
    /// </summary>
    private async void HandlePlayerLeft(JsonRpcNotification notification)
    {
        try
        {
            var player = notification.GetParams<PlayerDto>();
            if (player != null && _eventBus != null)
            {
                _logger.Debug($"玩家离开: {player.Name}");
                await _eventBus.PublishAsync(new PlayerLeftEvent { Player = player });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理玩家离开通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理管理员添加通知
    /// </summary>
    private async void HandleOperatorAdded(JsonRpcNotification notification)
    {
        try
        {
            var op = notification.GetParams<OperatorDto>();
            if (op != null && _eventBus != null)
            {
                _logger.Debug($"管理员添加: {op.Player.Name}");
                await _eventBus.PublishAsync(new OperatorAddedEvent { Operator = op });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理管理员添加通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理管理员移除通知
    /// </summary>
    private async void HandleOperatorRemoved(JsonRpcNotification notification)
    {
        try
        {
            var op = notification.GetParams<OperatorDto>();
            if (op != null && _eventBus != null)
            {
                _logger.Debug($"管理员移除: {op.Player.Name}");
                await _eventBus.PublishAsync(new OperatorRemovedEvent { Player = op.Player });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理管理员移除通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理白名单添加通知
    /// </summary>
    private async void HandleAllowlistAdded(JsonRpcNotification notification)
    {
        try
        {
            var player = notification.GetParams<PlayerDto>();
            if (player != null && _eventBus != null)
            {
                _logger.Debug($"白名单添加: {player.Name}");
                await _eventBus.PublishAsync(new AllowlistChangedEvent 
                { 
                    Players = new List<PlayerDto> { player },
                    Operation = "added"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理白名单添加通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理白名单移除通知
    /// </summary>
    private async void HandleAllowlistRemoved(JsonRpcNotification notification)
    {
        try
        {
            var player = notification.GetParams<PlayerDto>();
            if (player != null && _eventBus != null)
            {
                _logger.Debug($"白名单移除: {player.Name}");
                await _eventBus.PublishAsync(new AllowlistChangedEvent 
                { 
                    Players = new List<PlayerDto> { player },
                    Operation = "removed"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理白名单移除通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理封禁添加通知
    /// </summary>
    private async void HandleBanAdded(JsonRpcNotification notification)
    {
        try
        {
            var ban = notification.GetParams<UserBanDto>();
            if (ban != null && _eventBus != null)
            {
                _logger.Debug($"玩家封禁添加: {ban.Player.Name}");
                await _eventBus.PublishAsync(new PlayerBannedEvent { Ban = ban });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理封禁添加通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理封禁移除通知
    /// </summary>
    private async void HandleBanRemoved(JsonRpcNotification notification)
    {
        try
        {
            var player = notification.GetParams<PlayerDto>();
            if (player != null && _eventBus != null)
            {
                _logger.Debug($"玩家封禁移除: {player.Name}");
                await _eventBus.PublishAsync(new PlayerUnbannedEvent { Player = player });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理封禁移除通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理 IP 封禁添加通知
    /// </summary>
    private async void HandleIpBanAdded(JsonRpcNotification notification)
    {
        try
        {
            var ban = notification.GetParams<IpBanDto>();
            if (ban != null && _eventBus != null)
            {
                _logger.Debug($"IP 封禁添加: {ban.Ip}");
                await _eventBus.PublishAsync(new IpBannedEvent { Ban = ban });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理 IP 封禁添加通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理 IP 封禁移除通知
    /// </summary>
    private async void HandleIpBanRemoved(JsonRpcNotification notification)
    {
        try
        {
            var ban = notification.GetParams<IpBanDto>();
            if (ban != null && _eventBus != null)
            {
                _logger.Debug($"IP 封禁移除: {ban.Ip}");
                await _eventBus.PublishAsync(new IpUnbannedEvent { Ip = ban.Ip });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理 IP 封禁移除通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理游戏规则更新通知
    /// </summary>
    private async void HandleGameRuleUpdated(JsonRpcNotification notification)
    {
        try
        {
            // 通知参数包含规则名称和新值
            var data = notification.GetParams<Dictionary<string, object>>();
            if (data != null && _eventBus != null)
            {
                var ruleName = data.ContainsKey("name") ? data["name"]?.ToString() : "unknown";
                _logger.Debug($"游戏规则更新: {ruleName}");
                await _eventBus.PublishAsync(new GameRuleChangedEvent 
                { 
                    Rule = ruleName ?? "unknown",
                    NewValue = data.ContainsKey("value") ? data["value"] : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理游戏规则更新通知失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 处理服务器状态心跳通知
    /// </summary>
    private async void HandleServerStatus(JsonRpcNotification notification)
    {
        try
        {
            var status = notification.GetParams<ServerState>();
            if (status != null && _eventBus != null)
            {
                _logger.Trace($"服务器状态心跳: Started={status.Started}");
                // 发布服务器状态变更事件
                if (status.Started)
                {
                    await _eventBus.PublishAsync(new ServerStartedEvent());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理服务器状态心跳通知失败: {ex.Message}", ex);
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_webSocket != null)
        {
            _webSocket.MessageReceived -= OnMessageReceived;
            _webSocket.StateChanged -= OnStateChanged;
            _webSocket.ErrorOccurred -= OnErrorOccurred;
            _webSocket.Dispose();
        }

        _logger.Debug("SmpClient 已释放");
    }
}

