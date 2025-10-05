using System.Diagnostics;
using System.Text.Json;
using NetherGate.API.Configuration;
using NetherGate.API.Data;
using NetherGate.API.Events;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using NetherGate.API.Protocol;
using NetherGate.API.WebSocket;

namespace NetherGate.Core.WebSocket;

/// <summary>
/// WebSocket 消息处理器
/// </summary>
public class WebSocketMessageHandler
{
    private readonly WebSocketConfig _config;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEventBus _eventBus;
    private readonly ISmpApi? _smpApi;
    private readonly IPlayerDataReader _playerDataReader;
    private readonly IWorldDataReader _worldDataReader;
    private readonly Func<IReadOnlyList<PluginContainer>> _getPlugins;
    private readonly WebSocketServer _server;

    public WebSocketMessageHandler(
        WebSocketConfig config,
        ILogger logger,
        ILoggerFactory loggerFactory,
        IEventBus eventBus,
        ISmpApi? smpApi,
        IPlayerDataReader playerDataReader,
        IWorldDataReader worldDataReader,
        Func<IReadOnlyList<PluginContainer>> getPlugins,
        WebSocketServer server)
    {
        _config = config;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _eventBus = eventBus;
        _smpApi = smpApi;
        _playerDataReader = playerDataReader;
        _worldDataReader = worldDataReader;
        _getPlugins = getPlugins;
        _server = server;
    }

    public async Task HandleMessageAsync(WebSocketClient client, WebSocketMessage message)
    {
        _logger.Debug($"收到客户端 {client.Id} 消息: {message.Type}");

        try
        {
            switch (message.Type)
            {
                case "auth":
                    await HandleAuthAsync(client, message);
                    break;

                case "ping":
                    await HandlePingAsync(client, message);
                    break;

                case "subscribe":
                    await HandleSubscribeAsync(client, message);
                    break;

                case "unsubscribe":
                    await HandleUnsubscribeAsync(client, message);
                    break;

                // 服务器信息
                case "server.info":
                    await HandleServerInfoAsync(client, message);
                    break;

                case "server.status":
                    await HandleServerStatusAsync(client, message);
                    break;

                // 玩家数据
                case "players.list":
                    await HandlePlayersListAsync(client, message);
                    break;

                case "players.get":
                    await HandlePlayerGetAsync(client, message);
                    break;

                case "players.stats":
                    await HandlePlayerStatsAsync(client, message);
                    break;

                // 世界数据
                case "world.info":
                    await HandleWorldInfoAsync(client, message);
                    break;

                case "world.list":
                    await HandleWorldListAsync(client, message);
                    break;

                // 插件管理
                case "plugins.list":
                    await HandlePluginsListAsync(client, message);
                    break;

                case "plugins.info":
                    await HandlePluginInfoAsync(client, message);
                    break;

                // 日志
                case "logs.subscribe":
                    await HandleLogsSubscribeAsync(client, message);
                    break;

                default:
                    await SendResponseAsync(client, WebSocketResponse.Fail($"未知的消息类型: {message.Type}", message.RequestId));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"处理消息 {message.Type} 时出错", ex);
            await SendResponseAsync(client, WebSocketResponse.Fail($"处理失败: {ex.Message}", message.RequestId));
        }
    }

    private async Task HandleAuthAsync(WebSocketClient client, WebSocketMessage message)
    {
        if (!_config.Authentication.Enabled)
        {
            client.IsAuthenticated = true;
            await SendResponseAsync(client, WebSocketResponse.Ok(new { authenticated = true }, message.RequestId));
            return;
        }

        var data = JsonSerializer.SerializeToElement(message.Data);
        
        if (!data.TryGetProperty("token", out var tokenElement))
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("缺少认证令牌", message.RequestId));
            return;
        }

        var token = tokenElement.GetString();

        if (string.IsNullOrEmpty(token) || token != _config.Authentication.Token)
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("认证令牌无效", message.RequestId));
            return;
        }

        client.IsAuthenticated = true;
        _logger.Info($"客户端 {client.Id} 认证成功");
        
        await SendResponseAsync(client, WebSocketResponse.Ok(new { authenticated = true }, message.RequestId));
    }

    private async Task HandlePingAsync(WebSocketClient client, WebSocketMessage message)
    {
        await SendResponseAsync(client, WebSocketResponse.Ok(new { pong = true }, message.RequestId));
    }

    private async Task HandleSubscribeAsync(WebSocketClient client, WebSocketMessage message)
    {
        // TODO: 实现事件订阅
        await SendResponseAsync(client, WebSocketResponse.Ok(new { subscribed = true }, message.RequestId));
    }

    private async Task HandleUnsubscribeAsync(WebSocketClient client, WebSocketMessage message)
    {
        // TODO: 实现取消订阅
        await SendResponseAsync(client, WebSocketResponse.Ok(new { unsubscribed = true }, message.RequestId));
    }

    private async Task HandleServerInfoAsync(WebSocketClient client, WebSocketMessage message)
    {
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        
        var info = new
        {
            version = "0.1.0-alpha",
            netVersion = Environment.Version.ToString(),
            os = Environment.OSVersion.ToString(),
            uptime = new
            {
                days = uptime.Days,
                hours = uptime.Hours,
                minutes = uptime.Minutes,
                seconds = uptime.Seconds
            },
            connections = _server.ConnectionCount
        };

        await SendResponseAsync(client, WebSocketResponse.Ok(info, message.RequestId));
    }

    private async Task HandleServerStatusAsync(WebSocketClient client, WebSocketMessage message)
    {
        var status = new
        {
            running = true,
            smpConnected = false, // TODO: 添加 IsConnected 属性到 ISmpApi
            playerCount = 0, // TODO: 从 SMP 获取
            tps = 20.0 // TODO: 从性能监控获取
        };

        await SendResponseAsync(client, WebSocketResponse.Ok(status, message.RequestId));
    }

    private async Task HandlePlayersListAsync(WebSocketClient client, WebSocketMessage message)
    {
        var playerUuids = _playerDataReader.ListPlayers();
        
        var players = new List<object>();
        foreach (var uuid in playerUuids)
        {
            var data = await _playerDataReader.ReadPlayerDataAsync(uuid);
            if (data != null)
            {
                players.Add(new
                {
                    uuid = data.Uuid,
                    name = data.Name,
                    level = data.XpLevel,
                    health = data.Health,
                    gameMode = data.GameMode.ToString()
                });
            }
        }

        await SendResponseAsync(client, WebSocketResponse.Ok(new { players, count = players.Count }, message.RequestId));
    }

    private async Task HandlePlayerGetAsync(WebSocketClient client, WebSocketMessage message)
    {
        var data = JsonSerializer.SerializeToElement(message.Data);
        
        if (!data.TryGetProperty("uuid", out var uuidElement))
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("缺少玩家 UUID", message.RequestId));
            return;
        }

        if (!Guid.TryParse(uuidElement.GetString(), out var playerUuid))
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("无效的玩家 UUID", message.RequestId));
            return;
        }

        var playerData = await _playerDataReader.ReadPlayerDataAsync(playerUuid);
        
        if (playerData == null)
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("玩家不存在", message.RequestId));
            return;
        }

        await SendResponseAsync(client, WebSocketResponse.Ok(playerData, message.RequestId));
    }

    private async Task HandlePlayerStatsAsync(WebSocketClient client, WebSocketMessage message)
    {
        var data = JsonSerializer.SerializeToElement(message.Data);
        
        if (!data.TryGetProperty("uuid", out var uuidElement))
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("缺少玩家 UUID", message.RequestId));
            return;
        }

        if (!Guid.TryParse(uuidElement.GetString(), out var playerUuid))
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("无效的玩家 UUID", message.RequestId));
            return;
        }

        var stats = await _playerDataReader.ReadPlayerStatsAsync(playerUuid);
        
        if (stats == null)
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("统计数据不存在", message.RequestId));
            return;
        }

        await SendResponseAsync(client, WebSocketResponse.Ok(stats, message.RequestId));
    }

    private async Task HandleWorldInfoAsync(WebSocketClient client, WebSocketMessage message)
    {
        var worldInfo = await _worldDataReader.ReadWorldInfoAsync();
        
        if (worldInfo == null)
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("世界数据不存在", message.RequestId));
            return;
        }

        await SendResponseAsync(client, WebSocketResponse.Ok(worldInfo, message.RequestId));
    }

    private async Task HandleWorldListAsync(WebSocketClient client, WebSocketMessage message)
    {
        var worlds = _worldDataReader.ListWorlds();
        await SendResponseAsync(client, WebSocketResponse.Ok(new { worlds }, message.RequestId));
    }

    private async Task HandlePluginsListAsync(WebSocketClient client, WebSocketMessage message)
    {
        var plugins = _getPlugins();
        
        var pluginList = plugins.Select(p => new
        {
            id = p.Metadata.Id,
            name = p.Metadata.Name,
            version = p.Metadata.Version,
            author = p.Metadata.Author,
            description = p.Metadata.Description
        }).ToList();

        await SendResponseAsync(client, WebSocketResponse.Ok(new { plugins = pluginList, count = pluginList.Count }, message.RequestId));
    }

    private async Task HandlePluginInfoAsync(WebSocketClient client, WebSocketMessage message)
    {
        var data = JsonSerializer.SerializeToElement(message.Data);
        
        if (!data.TryGetProperty("id", out var idElement))
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("缺少插件 ID", message.RequestId));
            return;
        }

        var pluginId = idElement.GetString();
        var plugin = _getPlugins().FirstOrDefault(p => p.Metadata.Id == pluginId);

        if (plugin == null)
        {
            await SendResponseAsync(client, WebSocketResponse.Fail("插件不存在", message.RequestId));
            return;
        }

        var pluginInfo = new
        {
            id = plugin.Metadata.Id,
            name = plugin.Metadata.Name,
            version = plugin.Metadata.Version,
            author = plugin.Metadata.Author,
            description = plugin.Metadata.Description,
            website = plugin.Metadata.Website,
            dependencies = plugin.Metadata.Dependencies,
            isLoaded = plugin.IsLoaded,
            loadedAt = plugin.LoadedAt
        };

        await SendResponseAsync(client, WebSocketResponse.Ok(pluginInfo, message.RequestId));
    }

    private async Task HandleLogsSubscribeAsync(WebSocketClient client, WebSocketMessage message)
    {
        // TODO: 实现日志订阅
        await SendResponseAsync(client, WebSocketResponse.Ok(new { subscribed = true }, message.RequestId));
    }

    private async Task SendResponseAsync(WebSocketClient client, WebSocketResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        await _server.SendToClientAsync(client.Id, json);
    }
}
