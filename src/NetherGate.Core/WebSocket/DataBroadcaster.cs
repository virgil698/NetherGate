using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.WebSocket;

namespace NetherGate.Core.WebSocket;

/// <summary>
/// 数据广播器实现
/// </summary>
public class DataBroadcaster : IDataBroadcaster
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, List<WebSocketClient>> _channels = new();
    private readonly ConcurrentDictionary<string, DataSource> _dataSources = new();
    private readonly ConcurrentDictionary<string, WebSocketClient> _clients = new();

    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;

    public DataBroadcaster(ILogger logger)
    {
        _logger = logger;
    }

    public async Task BroadcastAsync<T>(string channel, T data)
    {
        if (!_channels.TryGetValue(channel, out var clients) || clients.Count == 0)
        {
            return;
        }

        var message = new WebSocketMessage<T>
        {
            Channel = channel,
            Data = data,
            Type = "data"
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        var tasks = clients
            .Where(c => c.WebSocket.State == WebSocketState.Open)
            .Select(c => SendToWebSocketAsync(c.WebSocket, bytes));

        await Task.WhenAll(tasks);
        _logger.Debug($"Broadcasted to {clients.Count} clients on channel '{channel}'");
    }

    public async Task SendToClientAsync<T>(string channel, string clientId, T data)
    {
        if (!_clients.TryGetValue(clientId, out var client))
        {
            throw new InvalidOperationException($"Client '{clientId}' not found");
        }

        if (client.WebSocket.State != WebSocketState.Open)
        {
            return;
        }

        var message = new WebSocketMessage<T>
        {
            Channel = channel,
            Data = data,
            Type = "data"
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await SendToWebSocketAsync(client.WebSocket, bytes);
    }

    public Task RegisterDataSourceAsync<T>(string channel, Func<Task<T>> dataProvider, TimeSpan interval)
    {
        if (_dataSources.ContainsKey(channel))
        {
            throw new InvalidOperationException($"Data source for channel '{channel}' already registered");
        }

        var cts = new CancellationTokenSource();
        var dataSource = new DataSource
        {
            Channel = channel,
            Interval = interval,
            CancellationTokenSource = cts
        };

        _dataSources[channel] = dataSource;

        // 启动数据推送任务
        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var data = await dataProvider();
                    await BroadcastAsync(channel, data);
                    await Task.Delay(interval, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error in data source for channel '{channel}': {ex.Message}");
                    await Task.Delay(interval, cts.Token);
                }
            }
        }, cts.Token);

        _logger.Info($"Registered data source for channel '{channel}' with interval {interval.TotalSeconds}s");
        return Task.CompletedTask;
    }

    public Task UnregisterDataSourceAsync(string channel)
    {
        if (_dataSources.TryRemove(channel, out var dataSource))
        {
            dataSource.CancellationTokenSource.Cancel();
            dataSource.CancellationTokenSource.Dispose();
            _logger.Info($"Unregistered data source for channel '{channel}'");
        }
        return Task.CompletedTask;
    }

    public int GetSubscriberCount(string channel)
    {
        return _channels.TryGetValue(channel, out var clients) ? clients.Count : 0;
    }

    public List<string> GetActiveChannels()
    {
        return _channels.Keys.ToList();
    }

    public Task AddClientAsync(System.Net.WebSockets.WebSocket webSocket, string channel, string? ipAddress = null)
    {
        var clientId = Guid.NewGuid().ToString();
        var client = new WebSocketClient
        {
            Id = clientId,
            WebSocket = webSocket,
            Channel = channel,
            ConnectedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _clients[clientId] = client;

        if (!_channels.ContainsKey(channel))
        {
            _channels[channel] = new List<WebSocketClient>();
        }
        _channels[channel].Add(client);

        ClientConnected?.Invoke(this, new ClientConnectedEventArgs
        {
            ClientId = clientId,
            Channel = channel,
            IpAddress = ipAddress
        });

        _logger.Info($"Client {clientId} connected to channel '{channel}'");

        // 监听客户端断开
        _ = Task.Run(async () =>
        {
            try
            {
                var buffer = new byte[1024];
                while (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"WebSocket 接收循环异常 (客户端 {clientId}): {ex.Message}");
            }
            finally
            {
                await RemoveClientAsync(clientId, "Connection closed");
            }
        });
        
        return Task.CompletedTask;
    }

    public async Task RemoveClientAsync(string clientId, string? reason = null)
    {
        if (!_clients.TryRemove(clientId, out var client))
        {
            return;
        }

        if (_channels.TryGetValue(client.Channel, out var clients))
        {
            clients.Remove(client);
            if (clients.Count == 0)
            {
                _channels.TryRemove(client.Channel, out _);
            }
        }

        if (client.WebSocket.State == WebSocketState.Open)
        {
            await client.WebSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                reason ?? "Disconnected",
                CancellationToken.None
            );
        }

        ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs
        {
            ClientId = clientId,
            Channel = client.Channel,
            Reason = reason
        });

        _logger.Info($"Client {clientId} disconnected from channel '{client.Channel}': {reason}");
    }

    private async Task SendToWebSocketAsync(System.Net.WebSockets.WebSocket webSocket, byte[] data)
    {
        try
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to send WebSocket message: {ex.Message}");
        }
    }

    private class WebSocketClient
    {
        public required string Id { get; set; }
        public required System.Net.WebSockets.WebSocket WebSocket { get; set; }
        public required string Channel { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string? IpAddress { get; set; }
    }

    private class DataSource
    {
        public required string Channel { get; set; }
        public TimeSpan Interval { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
    }
}

