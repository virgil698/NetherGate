using NetherGate.API.Configuration;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Performance;

/// <summary>
/// spark 插件/模组客户端
/// 通过 RCON 与服务器上已安装的 spark 插件交互
/// </summary>
public class SparkPluginClient
{
    private readonly ILogger _logger;
    private readonly SparkConfig _config;
    private readonly IRconClient? _rconClient;
    private bool _isSparkInstalled;
    private string? _sparkVersion;

    public SparkPluginClient(SparkConfig config, IRconClient? rconClient, ILogger logger)
    {
        _config = config;
        _rconClient = rconClient;
        _logger = logger;
        _isSparkInstalled = false;
    }

    /// <summary>
    /// 检测服务器是否安装了 spark 插件/模组
    /// </summary>
    public async Task<bool> DetectSparkAsync()
    {
        if (_rconClient == null || !_rconClient.IsConnected)
        {
            _logger.Warning("RCON 未连接，无法检测 spark 插件");
            return false;
        }

        try
        {
            _logger.Info("正在检测 spark 插件/模组...");

            // 尝试执行 spark 命令
            var response = await _rconClient.ExecuteCommandAsync("spark");
            
            // 检查响应是否包含 spark 特征
            if (string.IsNullOrEmpty(response))
            {
                _logger.Info("服务器未安装 spark 插件/模组");
                _isSparkInstalled = false;
                return false;
            }

            // 检查是否为错误响应（未知命令）
            if (response.Contains("Unknown command") || 
                response.Contains("未知的命令") ||
                response.Contains("Unknown or incomplete command"))
            {
                _logger.Info("服务器未安装 spark 插件/模组");
                _isSparkInstalled = false;
                return false;
            }

            // 如果响应包含 spark 相关信息，说明已安装
            if (response.Contains("spark") || 
                response.Contains("profiler") || 
                response.Contains("Usage:"))
            {
                _isSparkInstalled = true;
                _logger.Info("检测到 spark 插件/模组已安装");
                
                // 尝试获取版本信息
                await GetSparkVersionAsync();
                
                return true;
            }

            _logger.Info("服务器未安装 spark 插件/模组");
            _isSparkInstalled = false;
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"检测 spark 插件失败: {ex.Message}", ex);
            _isSparkInstalled = false;
            return false;
        }
    }

    /// <summary>
    /// 获取 spark 版本信息
    /// </summary>
    private async Task GetSparkVersionAsync()
    {
        if (_rconClient == null || !_rconClient.IsConnected)
            return;

        try
        {
            var response = await _rconClient.ExecuteCommandAsync("spark version");
            if (!string.IsNullOrEmpty(response) && response.Contains("spark"))
            {
                // 解析版本信息（格式可能为: "spark v1.10.53"）
                var match = System.Text.RegularExpressions.Regex.Match(response, @"v?(\d+\.\d+\.\d+)");
                if (match.Success)
                {
                    _sparkVersion = match.Groups[1].Value;
                    _logger.Info($"spark 版本: {_sparkVersion}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"获取 spark 版本失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 通过 RCON 获取 TPS
    /// </summary>
    public async Task<TpsData?> GetTpsAsync()
    {
        if (!_isSparkInstalled || _rconClient == null || !_rconClient.IsConnected)
        {
            return null;
        }

        try
        {
            var response = await _rconClient.ExecuteCommandAsync("spark tps");
            
            // 解析 TPS 输出
            // 格式: "TPS from last 5s, 10s, 1m, 5m, 15m: ▃▅▇ 20.0, 20.0, 19.98, 19.95, 19.92"
            var match = System.Text.RegularExpressions.Regex.Match(response,
                @"1m.*?(\d+\.\d+).*?5m.*?(\d+\.\d+).*?15m.*?(\d+\.\d+)");
            
            if (match.Success)
            {
                return new TpsData
                {
                    Tps1m = double.Parse(match.Groups[1].Value),
                    Tps5m = double.Parse(match.Groups[2].Value),
                    Tps15m = double.Parse(match.Groups[3].Value)
                };
            }

            _logger.Warning("无法解析 spark TPS 输出");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取 TPS 失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 通过 RCON 获取服务器健康状况
    /// </summary>
    public async Task<string?> GetHealthAsync()
    {
        if (!_isSparkInstalled || _rconClient == null || !_rconClient.IsConnected)
        {
            return null;
        }

        try
        {
            return await _rconClient.ExecuteCommandAsync("spark health");
        }
        catch (Exception ex)
        {
            _logger.Error($"获取服务器健康状况失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 开始性能分析
    /// </summary>
    public async Task<string?> StartProfilingAsync(int durationSeconds = 30)
    {
        if (!_isSparkInstalled || _rconClient == null || !_rconClient.IsConnected)
        {
            return null;
        }

        try
        {
            return await _rconClient.ExecuteCommandAsync($"spark profiler start --timeout {durationSeconds}");
        }
        catch (Exception ex)
        {
            _logger.Error($"开始性能分析失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 停止性能分析
    /// </summary>
    public async Task<string?> StopProfilingAsync()
    {
        if (!_isSparkInstalled || _rconClient == null || !_rconClient.IsConnected)
        {
            return null;
        }

        try
        {
            return await _rconClient.ExecuteCommandAsync("spark profiler stop");
        }
        catch (Exception ex)
        {
            _logger.Error($"停止性能分析失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 打印 spark 插件信息
    /// </summary>
    public void PrintSparkPluginInfo()
    {
        if (!_isSparkInstalled)
        {
            return;
        }

        _logger.Info("=".PadRight(60, '='));
        _logger.Info("spark 插件/模组已检测到");
        _logger.Info("=".PadRight(60, '='));
        
        if (!string.IsNullOrEmpty(_sparkVersion))
        {
            _logger.Info($"版本: {_sparkVersion}");
        }
        
        _logger.Info("交互方式: RCON 命令");
        _logger.Info("");
        _logger.Info("可用命令（通过 RCON 执行）:");
        _logger.Info("  spark tps                    - 查看 TPS");
        _logger.Info("  spark health                 - 查看服务器健康状况");
        _logger.Info("  spark profiler start         - 开始性能分析");
        _logger.Info("  spark profiler stop          - 停止并生成报告");
        _logger.Info("  spark heapsummary            - 查看堆内存摘要");
        _logger.Info("=".PadRight(60, '='));
    }

    /// <summary>
    /// spark 是否已安装
    /// </summary>
    public bool IsSparkInstalled => _isSparkInstalled;

    /// <summary>
    /// spark 版本
    /// </summary>
    public string? SparkVersion => _sparkVersion;
}

