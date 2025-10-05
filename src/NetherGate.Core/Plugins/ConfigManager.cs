using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Plugins;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 配置管理器实现
/// 支持 JSON 和 YAML 格式的配置文件
/// </summary>
public class ConfigManager : IConfigManager
{
    private readonly string _dataDirectory;
    private readonly ILogger _logger;
    private readonly Dictionary<string, object> _configCache = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public ConfigManager(string dataDirectory, ILogger logger)
    {
        _dataDirectory = dataDirectory;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        // 配置 YAML 序列化器
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 加载配置文件（支持 JSON 和 YAML 格式）
    /// </summary>
    public async Task<T> LoadConfigAsync<T>(string fileName = "config.json") where T : class, new()
    {
        var filePath = Path.Combine(_dataDirectory, fileName);
        var cacheKey = $"{typeof(T).FullName}_{fileName}";

        // 1. 检查缓存
        if (_configCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.Trace($"从缓存加载配置: {fileName}");
            return (T)cached;
        }

        // 2. 如果文件不存在，创建默认配置
        if (!File.Exists(filePath))
        {
            _logger.Info($"配置文件不存在，创建默认配置: {filePath}");
            var defaultConfig = new T();
            await SaveConfigAsync(defaultConfig, fileName);
            _configCache[cacheKey] = defaultConfig;
            return defaultConfig;
        }

        try
        {
            // 3. 读取并解析配置文件
            _logger.Debug($"加载配置文件: {filePath}");
            var content = await File.ReadAllTextAsync(filePath);
            var config = DeserializeConfig<T>(content, fileName);

            if (config == null)
            {
                _logger.Warning($"配置文件解析为 null，使用默认配置: {filePath}");
                config = new T();
            }

            // 4. 缓存配置
            _configCache[cacheKey] = config;

            return config;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载配置文件失败: {filePath}", ex);
            _logger.Warning("使用默认配置");
            var defaultConfig = new T();
            _configCache[cacheKey] = defaultConfig;
            return defaultConfig;
        }
    }

    /// <summary>
    /// 保存配置文件（支持 JSON 和 YAML 格式）
    /// </summary>
    public async Task SaveConfigAsync<T>(T config, string fileName = "config.json") where T : class
    {
        var filePath = Path.Combine(_dataDirectory, fileName);

        try
        {
            // 1. 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 2. 序列化配置
            var content = SerializeConfig(config, fileName);

            // 3. 写入文件
            await File.WriteAllTextAsync(filePath, content);

            _logger.Debug($"保存配置文件: {filePath}");

            // 4. 更新缓存
            var cacheKey = $"{typeof(T).FullName}_{fileName}";
            _configCache[cacheKey] = config;
        }
        catch (Exception ex)
        {
            _logger.Error($"保存配置文件失败: {filePath}", ex);
            throw;
        }
    }

    /// <summary>
    /// 重载配置文件
    /// </summary>
    public async Task<T> ReloadConfigAsync<T>(string fileName = "config.json") where T : class, new()
    {
        _logger.Info($"重载配置文件: {fileName}");

        // 清除缓存
        var cacheKey = $"{typeof(T).FullName}_{fileName}";
        _configCache.Remove(cacheKey);

        // 重新加载
        return await LoadConfigAsync<T>(fileName);
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearCache()
    {
        _configCache.Clear();
        _logger.Debug("配置缓存已清空");
    }

    /// <summary>
    /// 判断是否为 YAML 文件
    /// </summary>
    private bool IsYamlFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".yaml" || extension == ".yml";
    }

    /// <summary>
    /// 反序列化配置（根据文件类型自动选择格式）
    /// </summary>
    private T? DeserializeConfig<T>(string content, string fileName) where T : class
    {
        if (IsYamlFile(fileName))
        {
            _logger.Trace($"使用 YAML 格式解析配置文件: {fileName}");
            return _yamlDeserializer.Deserialize<T>(content);
        }
        else
        {
            _logger.Trace($"使用 JSON 格式解析配置文件: {fileName}");
            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }
    }

    /// <summary>
    /// 序列化配置（根据文件类型自动选择格式）
    /// </summary>
    private string SerializeConfig<T>(T config, string fileName) where T : class
    {
        if (IsYamlFile(fileName))
        {
            _logger.Trace($"使用 YAML 格式保存配置文件: {fileName}");
            return _yamlSerializer.Serialize(config);
        }
        else
        {
            _logger.Trace($"使用 JSON 格式保存配置文件: {fileName}");
            return JsonSerializer.Serialize(config, _jsonOptions);
        }
    }
}

