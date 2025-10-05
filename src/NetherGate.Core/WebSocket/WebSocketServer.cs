using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NetherGate.API.Configuration;
using NetherGate.API.Logging;
using NetherGate.API.WebSocket;

namespace NetherGate.Core.WebSocket;

/// <summary>
/// WebSocket 服务器实现
/// </summary>
public class WebSocketServer : IWebSocketServer, IDisposable
{
    private readonly WebSocketConfig _config;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, WebSocketClient> _clients = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private HttpListener? _httpListener;
    private bool _isRunning;
    private readonly WebSocketMessageHandler _messageHandler;

    public bool IsRunning => _isRunning;
    public int ConnectionCount => _clients.Count;

    public WebSocketServer(WebSocketConfig config, ILogger logger, WebSocketMessageHandler messageHandler)
    {
        _config = config;
        _logger = logger;
        _messageHandler = messageHandler;
    }

    public Task StartAsync()
    {
        if (_isRunning)
        {
            _logger.Warning("WebSocket 服务器已在运行");
            return Task.CompletedTask;
        }

        try
        {
            _httpListener = new HttpListener();
            var prefix = $"http://{_config.Host}:{_config.Port}/";
            _httpListener.Prefixes.Add(prefix);
            _httpListener.Start();
            
            _isRunning = true;
            _logger.Info($"WebSocket 服务器已启动: {prefix}");

            // 开始接受连接
            _ = Task.Run(AcceptConnectionsAsync, _cancellationTokenSource.Token);

            // 启动心跳检查
            if (_config.Heartbeat.Enabled)
            {
                _ = Task.Run(HeartbeatCheckAsync, _cancellationTokenSource.Token);
            }
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error("启动 WebSocket 服务器失败", ex);
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.Info("正在停止 WebSocket 服务器...");
        _isRunning = false;

        // 取消所有任务
        _cancellationTokenSource.Cancel();

        // 关闭所有客户端连接
        var closeTasks = _clients.Values.Select(client => CloseClientAsync(client, "服务器关闭"));
        await Task.WhenAll(closeTasks);

        // 停止 HTTP 监听器
        _httpListener?.Stop();
        _httpListener?.Close();

        _logger.Info("WebSocket 服务器已停止");
    }

    private async Task AcceptConnectionsAsync()
    {
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener!.GetContextAsync();
                
                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => HandleWebSocketConnectionAsync(context));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex) when (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _logger.Error("接受 WebSocket 连接时出错", ex);
            }
        }
    }

    private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
    {
        WebSocketContext? webSocketContext = null;
        
        try
        {
            // 检查连接数限制
            if (_clients.Count >= _config.MaxConnections)
            {
                _logger.Warning($"连接数已达上限 ({_config.MaxConnections})");
                context.Response.StatusCode = 503;
                context.Response.Close();
                return;
            }

            // 检查 IP 限制
            var clientIp = context.Request.RemoteEndPoint?.Address.ToString() ?? "";
            if (_config.Authentication.Enabled && _config.Authentication.AllowedIps.Count > 0)
            {
                if (!_config.Authentication.AllowedIps.Contains(clientIp))
                {
                    _logger.Warning($"拒绝来自 {clientIp} 的连接：IP 不在白名单中");
                    context.Response.StatusCode = 403;
                    context.Response.Close();
                    return;
                }
            }

            // 接受 WebSocket 连接
            webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;

            var clientId = Guid.NewGuid().ToString();
            var client = new WebSocketClient
            {
                Id = clientId,
                WebSocket = webSocket,
                ConnectedAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                IpAddress = clientIp,
                IsAuthenticated = !_config.Authentication.Enabled // 如果不需要认证则默认已认证
            };

            _clients.TryAdd(clientId, client);
            _logger.Info($"新客户端已连接: {clientId} ({clientIp})，当前连接数: {_clients.Count}");

            // 发送欢迎消息
            await SendToClientAsync(clientId, JsonSerializer.Serialize(new WebSocketMessage
            {
                Type = "welcome",
                Data = new
                {
                    clientId,
                    serverVersion = "0.1.0-alpha",
                    requiresAuth = _config.Authentication.Enabled
                }
            }));

            // 开始接收消息
            await ReceiveMessagesAsync(client);
        }
        catch (Exception ex)
        {
            _logger.Error("处理 WebSocket 连接时出错", ex);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocketClient client)
    {
        var buffer = new byte[_config.Buffer.ReceiveBufferSize];
        var messageBuffer = new List<byte>();

        try
        {
            while (client.WebSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await client.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseClientAsync(client, "客户端关闭连接");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuffer.AddRange(buffer.Take(result.Count));

                    if (result.EndOfMessage)
                    {
                        var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        messageBuffer.Clear();

                        // 更新心跳时间
                        client.LastHeartbeat = DateTime.UtcNow;

                        // 处理消息
                        await HandleMessageAsync(client, message);
                    }
                }
            }
        }
        catch (WebSocketException ex)
        {
            _logger.Debug($"客户端 {client.Id} WebSocket 异常: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"接收客户端 {client.Id} 消息时出错", ex);
        }
        finally
        {
            await RemoveClientAsync(client);
        }
    }

    private async Task HandleMessageAsync(WebSocketClient client, string message)
    {
        try
        {
            var wsMessage = JsonSerializer.Deserialize<WebSocketMessage>(message);
            
            if (wsMessage == null)
            {
                await SendErrorToClientAsync(client.Id, "无效的消息格式", null);
                return;
            }

            // 检查认证
            if (_config.Authentication.Enabled && !client.IsAuthenticated && wsMessage.Type != "auth")
            {
                await SendErrorToClientAsync(client.Id, "未认证", wsMessage.RequestId);
                return;
            }

            // 处理消息
            await _messageHandler.HandleMessageAsync(client, wsMessage);
        }
        catch (JsonException ex)
        {
            _logger.Warning($"解析客户端 {client.Id} 消息失败: {ex.Message}");
            await SendErrorToClientAsync(client.Id, "JSON 解析失败", null);
        }
        catch (Exception ex)
        {
            _logger.Error($"处理客户端 {client.Id} 消息时出错", ex);
            await SendErrorToClientAsync(client.Id, "处理消息失败", null);
        }
    }

    private async Task HeartbeatCheckAsync()
    {
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.Heartbeat.IntervalSeconds), _cancellationTokenSource.Token);

                var now = DateTime.UtcNow;
                var timeout = TimeSpan.FromSeconds(_config.Heartbeat.TimeoutSeconds);

                var timeoutClients = _clients.Values
                    .Where(c => now - c.LastHeartbeat > timeout)
                    .ToList();

                foreach (var client in timeoutClients)
                {
                    _logger.Warning($"客户端 {client.Id} 心跳超时，断开连接");
                    await CloseClientAsync(client, "心跳超时");
                }
            }
            catch (TaskCanceledException)
            {
                // 正常取消，忽略
            }
            catch (Exception ex)
            {
                _logger.Error("心跳检查时出错", ex);
            }
        }
    }

    public async Task BroadcastAsync(string message)
    {
        var tasks = _clients.Values.Select(client => SendToClientAsync(client.Id, message));
        await Task.WhenAll(tasks);
    }

    public async Task SendToClientAsync(string clientId, string message)
    {
        if (!_clients.TryGetValue(clientId, out var client))
        {
            return;
        }

        try
        {
            if (client.WebSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await client.WebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"发送消息给客户端 {clientId} 失败", ex);
            await RemoveClientAsync(client);
        }
    }

    private async Task SendErrorToClientAsync(string clientId, string error, string? requestId)
    {
        var response = WebSocketResponse.Fail(error, requestId);
        await SendToClientAsync(clientId, JsonSerializer.Serialize(response));
    }

    private async Task CloseClientAsync(WebSocketClient client, string reason)
    {
        try
        {
            if (client.WebSocket.State == WebSocketState.Open)
            {
                await client.WebSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"关闭客户端连接时出错: {ex.Message}");
        }
        finally
        {
            await RemoveClientAsync(client);
        }
    }

    private async Task RemoveClientAsync(WebSocketClient client)
    {
        if (_clients.TryRemove(client.Id, out _))
        {
            _logger.Info($"客户端已断开: {client.Id}，当前连接数: {_clients.Count}");
            client.WebSocket.Dispose();
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _cancellationTokenSource.Dispose();
        _httpListener?.Close();
    }
}

/// <summary>
/// WebSocket 客户端
/// </summary>
public class WebSocketClient
{
    public string Id { get; set; } = string.Empty;
    public System.Net.WebSockets.WebSocket WebSocket { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
}
