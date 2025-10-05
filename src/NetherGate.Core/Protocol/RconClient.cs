using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using System.Net.Sockets;
using System.Text;

namespace NetherGate.Core.Protocol;

/// <summary>
/// RCON 数据包类型
/// </summary>
internal enum RconPacketType
{
    Auth = 3,
    AuthResponse = 2,
    Command = 2,
    CommandResponse = 0
}

/// <summary>
/// RCON 客户端实现
/// 基于 Source RCON 协议
/// </summary>
public class RconClient : IRconClient, IRconPerformance
{
    private readonly ILogger _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _password;
    private readonly int _timeout;
    
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private int _requestId;
    private bool _disposed;
    private readonly object _lock = new();

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public RconClient(string host, int port, string password, int timeout, ILogger logger)
    {
        _host = host;
        _port = port;
        _password = password;
        _timeout = timeout;
        _logger = logger;
        _requestId = 0;
    }

    /// <summary>
    /// 连接到 RCON 服务器
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RconClient));

        if (IsConnected)
        {
            _logger.Warning("RCON 已连接");
            return true;
        }

        try
        {
            _logger.Info($"正在连接 RCON: {_host}:{_port}");

            _tcpClient = new TcpClient
            {
                ReceiveTimeout = _timeout,
                SendTimeout = _timeout
            };

            await _tcpClient.ConnectAsync(_host, _port);
            _stream = _tcpClient.GetStream();

            // 发送认证请求
            var authSuccess = await AuthenticateAsync();
            
            if (authSuccess)
            {
                _logger.Info("RCON 连接成功");
                ConnectionStateChanged?.Invoke(this, true);
                return true;
            }
            else
            {
                _logger.Error("RCON 认证失败");
                await DisconnectAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"RCON 连接失败: {ex.Message}", ex);
            ErrorOccurred?.Invoke(this, ex);
            await DisconnectAsync();
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public Task DisconnectAsync()
    {
        if (!IsConnected)
            return Task.CompletedTask;

        try
        {
            _logger.Info("正在断开 RCON 连接");
            
            _stream?.Close();
            _tcpClient?.Close();
            
            _stream = null;
            _tcpClient = null;

            ConnectionStateChanged?.Invoke(this, false);
            _logger.Info("RCON 已断开");
        }
        catch (Exception ex)
        {
            _logger.Error($"断开 RCON 连接时出错: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    public Task<string> ExecuteCommandAsync(string command)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RconClient));

        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("RCON 未连接");

        return Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    _logger.Trace($"执行 RCON 命令: {command}");

                    // 发送命令数据包
                    var requestId = Interlocked.Increment(ref _requestId);
                    var packet = CreatePacket(requestId, RconPacketType.Command, command);
                    _stream.Write(packet, 0, packet.Length);

                    // 读取响应
                    var response = ReadPacket(_stream);
                    
                    if (response.requestId == -1)
                    {
                        throw new InvalidOperationException("RCON 命令执行失败：认证失败");
                    }

                    _logger.Trace($"RCON 响应: {response.payload}");
                    return response.payload;
                }
                catch (Exception ex)
                {
                    _logger.Error($"执行 RCON 命令失败: {ex.Message}", ex);
                    ErrorOccurred?.Invoke(this, ex);
                    throw;
                }
            }
        });
    }

    /// <summary>
    /// 认证
    /// </summary>
    private async Task<bool> AuthenticateAsync()
    {
        if (_stream == null)
            return false;

        try
        {
            // 发送认证数据包
            var requestId = Interlocked.Increment(ref _requestId);
            var packet = CreatePacket(requestId, RconPacketType.Auth, _password);
            
            await _stream.WriteAsync(packet, 0, packet.Length);

            // 读取认证响应
            var response = ReadPacket(_stream);
            
            // 如果返回的 requestId 为 -1，表示认证失败
            return response.requestId != -1;
        }
        catch (Exception ex)
        {
            _logger.Error($"RCON 认证失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 创建 RCON 数据包
    /// </summary>
    private byte[] CreatePacket(int requestId, RconPacketType type, string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var packetSize = sizeof(int) + sizeof(int) + payloadBytes.Length + 2; // +2 for null terminators

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(packetSize);
        writer.Write(requestId);
        writer.Write((int)type);
        writer.Write(payloadBytes);
        writer.Write((byte)0); // Null terminator for string
        writer.Write((byte)0); // Null terminator for packet

        return ms.ToArray();
    }

    /// <summary>
    /// 读取 RCON 数据包
    /// </summary>
    private (int requestId, int type, string payload) ReadPacket(NetworkStream stream)
    {
        // 读取数据包大小
        var sizeBytes = new byte[sizeof(int)];
        ReadExactly(stream, sizeBytes, 0, sizeof(int));
        var size = BitConverter.ToInt32(sizeBytes, 0);

        // 读取数据包内容
        var dataBytes = new byte[size];
        ReadExactly(stream, dataBytes, 0, size);

        // 解析数据包
        var requestId = BitConverter.ToInt32(dataBytes, 0);
        var type = BitConverter.ToInt32(dataBytes, sizeof(int));
        var payload = Encoding.UTF8.GetString(dataBytes, sizeof(int) * 2, size - sizeof(int) * 2 - 2);

        return (requestId, type, payload);
    }

    /// <summary>
    /// 确保从流中读取指定数量的字节
    /// </summary>
    private static void ReadExactly(NetworkStream stream, byte[] buffer, int offset, int count)
    {
        var bytesRead = 0;
        while (bytesRead < count)
        {
            var read = stream.Read(buffer, offset + bytesRead, count - bytesRead);
            if (read == 0)
                throw new IOException("连接已关闭");
            bytesRead += read;
        }
    }

    // ========== IRconPerformance 实现 ==========

    /// <summary>
    /// 获取 TPS（仅支持 Paper/Purpur）
    /// </summary>
    public async Task<TpsData?> GetTpsAsync()
    {
        try
        {
            var response = await ExecuteCommandAsync("tps");
            
            // Paper 输出格式: "TPS from last 1m, 5m, 15m: 20.0, 20.0, 20.0"
            // Purpur 类似
            var match = System.Text.RegularExpressions.Regex.Match(response, 
                @"TPS from last [^:]+:\s*([0-9.]+),\s*([0-9.]+),\s*([0-9.]+)");
            
            if (match.Success)
            {
                return new TpsData
                {
                    Tps1m = double.Parse(match.Groups[1].Value),
                    Tps5m = double.Parse(match.Groups[2].Value),
                    Tps15m = double.Parse(match.Groups[3].Value)
                };
            }

            _logger.Warning("无法解析 TPS 数据，服务器可能不支持 /tps 命令");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取 TPS 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取 MSPT（仅支持 Paper）
    /// </summary>
    public async Task<MsptData?> GetMsptAsync()
    {
        try
        {
            var response = await ExecuteCommandAsync("mspt");
            
            // Paper 输出格式: "Server tick times (avg/min/max) from last 5s: 45.2/41.0/50.0"
            var match = System.Text.RegularExpressions.Regex.Match(response,
                @"tick times.*?:\s*([0-9.]+)/([0-9.]+)/([0-9.]+)");
            
            if (match.Success)
            {
                return new MsptData
                {
                    Average = double.Parse(match.Groups[1].Value),
                    Min = double.Parse(match.Groups[2].Value),
                    Max = double.Parse(match.Groups[3].Value)
                };
            }

            _logger.Warning("无法解析 MSPT 数据，服务器可能不支持 /mspt 命令");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取 MSPT 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检测服务器类型
    /// </summary>
    public async Task<ServerType> DetectServerTypeAsync()
    {
        try
        {
            // 尝试 Paper /tps 命令
            var tpsResponse = await ExecuteCommandAsync("tps");
            if (!string.IsNullOrEmpty(tpsResponse) && tpsResponse.Contains("TPS from last"))
            {
                // 进一步检测 Purpur
                var versionResponse = await ExecuteCommandAsync("version");
                if (versionResponse.Contains("Purpur"))
                    return ServerType.Purpur;
                
                return ServerType.Paper;
            }

            // 检测 Spigot
            var versionResponse2 = await ExecuteCommandAsync("version");
            if (versionResponse2.Contains("Spigot"))
                return ServerType.Spigot;
            if (versionResponse2.Contains("Paper"))
                return ServerType.Paper;
            if (versionResponse2.Contains("Purpur"))
                return ServerType.Purpur;
            if (versionResponse2.Contains("Forge"))
                return ServerType.Forge;
            if (versionResponse2.Contains("Fabric"))
                return ServerType.Fabric;

            return ServerType.Vanilla;
        }
        catch
        {
            return ServerType.Unknown;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _stream?.Dispose();
        _tcpClient?.Dispose();

        _logger.Debug("RconClient 已释放");
    }
}

