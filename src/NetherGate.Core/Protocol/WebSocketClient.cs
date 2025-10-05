using NetherGate.API.Logging;
using System.Net.WebSockets;
using System.Text;

namespace NetherGate.Core.Protocol;

/// <summary>
/// WebSocket 连接状态
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting,
    Reconnecting
}

/// <summary>
/// WebSocket 客户端
/// </summary>
public class WebSocketClient : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _uri;
    private readonly Dictionary<string, string> _headers;
    private readonly int _reconnectInterval;
    private readonly CancellationTokenSource _cts;
    
    private ClientWebSocket? _webSocket;
    private Task? _receiveTask;
    private ConnectionState _state;
    private bool _disposed;
    private int _reconnectAttempts;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<ConnectionState>? StateChanged;
    public event EventHandler<Exception>? ErrorOccurred;

    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                StateChanged?.Invoke(this, value);
                _logger.Debug($"WebSocket 状态变更: {value}");
            }
        }
    }

    public bool IsConnected => State == ConnectionState.Connected;

    public WebSocketClient(string uri, Dictionary<string, string>? headers, int reconnectInterval, ILogger logger)
    {
        _uri = uri;
        _headers = headers ?? new Dictionary<string, string>();
        _reconnectInterval = reconnectInterval;
        _logger = logger;
        _cts = new CancellationTokenSource();
        _state = ConnectionState.Disconnected;
    }

    /// <summary>
    /// 连接到 WebSocket 服务器
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WebSocketClient));

        if (State == ConnectionState.Connected || State == ConnectionState.Connecting)
        {
            _logger.Warning("已连接或正在连接中");
            return State == ConnectionState.Connected;
        }

        try
        {
            State = ConnectionState.Connecting;
            _logger.Info($"正在连接 WebSocket: {_uri}");

            _webSocket = new ClientWebSocket();

            // 添加请求头
            foreach (var header in _headers)
            {
                _webSocket.Options.SetRequestHeader(header.Key, header.Value);
            }

            await _webSocket.ConnectAsync(new Uri(_uri), _cts.Token);

            State = ConnectionState.Connected;
            _logger.Info("WebSocket 连接成功");
            _reconnectAttempts = 0;

            // 启动接收任务
            _receiveTask = Task.Run(ReceiveLoopAsync, _cts.Token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"WebSocket 连接失败: {ex.Message}", ex);
            State = ConnectionState.Disconnected;
            ErrorOccurred?.Invoke(this, ex);
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_disposed || State == ConnectionState.Disconnected)
            return;

        try
        {
            State = ConnectionState.Disconnecting;
            _logger.Info("正在断开 WebSocket 连接...");

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "关闭连接",
                    CancellationToken.None);
            }

            State = ConnectionState.Disconnected;
            _logger.Info("WebSocket 已断开");
        }
        catch (Exception ex)
        {
            _logger.Error($"断开连接时出错: {ex.Message}", ex);
            State = ConnectionState.Disconnected;
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendAsync(string message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WebSocketClient));

        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket 未连接");
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(bytes);

            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token);
            _logger.Trace($"发送消息: {message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"发送消息失败: {ex.Message}", ex);
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    /// <summary>
    /// 接收循环
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[8192];

        try
        {
            while (!_cts.Token.IsCancellationRequested && _webSocket != null)
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    _logger.Warning("WebSocket 连接已关闭");
                    break;
                }

                try
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await _webSocket.ReceiveAsync(segment, _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.Info($"收到关闭消息: {result.CloseStatusDescription}");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.Trace($"收到消息: {message}");
                        MessageReceived?.Invoke(this, message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    _logger.Warning("连接被远程主机关闭");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error($"接收消息时出错: {ex.Message}", ex);
                    ErrorOccurred?.Invoke(this, ex);
                    break;
                }
            }
        }
        finally
        {
            State = ConnectionState.Disconnected;

            // 自动重连
            if (!_disposed && !_cts.Token.IsCancellationRequested)
            {
                _ = Task.Run(async () =>
                {
                    _reconnectAttempts++;
                    _logger.Info($"尝试重连 (第 {_reconnectAttempts} 次)...");
                    
                    await Task.Delay(_reconnectInterval);
                    
                    if (!_disposed && !_cts.Token.IsCancellationRequested)
                    {
                        State = ConnectionState.Reconnecting;
                        await ConnectAsync();
                    }
                });
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();

        _webSocket?.Dispose();
        _cts.Dispose();

        _logger.Debug("WebSocketClient 已释放");
    }
}

