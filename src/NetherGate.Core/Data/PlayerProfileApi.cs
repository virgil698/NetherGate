using System.Text.RegularExpressions;
using NetherGate.API.Data;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Data;

/// <summary>
/// 玩家档案 API 实现
/// 基于 Minecraft 1.21.9+ 的 /fetchprofile 命令
/// </summary>
public class PlayerProfileApi : IPlayerProfileApi
{
    private readonly IRconClient? _rconClient;
    private readonly ILogger _logger;
    private readonly Dictionary<string, PlayerProfile> _profileCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private readonly Dictionary<string, DateTime> _cacheTimestamps = new();

    public PlayerProfileApi(IRconClient? rconClient, ILogger logger)
    {
        _rconClient = rconClient;
        _logger = logger;
    }

    public async Task<PlayerProfile?> FetchProfileByNameAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            _logger.Warning("玩家名称不能为空");
            return null;
        }

        // 检查缓存
        var cacheKey = playerName.ToLowerInvariant();
        if (_profileCache.TryGetValue(cacheKey, out var cachedProfile) &&
            _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
            DateTime.UtcNow - timestamp < _cacheExpiration)
        {
            _logger.Debug($"从缓存获取玩家档案: {playerName}");
            return cachedProfile;
        }

        if (_rconClient == null)
        {
            _logger.Warning("RCON 客户端未配置，无法获取玩家档案");
            return null;
        }

        try
        {
            _logger.Debug($"获取玩家档案: {playerName}");
            
            // 执行 fetchprofile 命令
            var response = await _rconClient.ExecuteCommandAsync($"fetchprofile name {playerName}");

            if (string.IsNullOrEmpty(response))
            {
                _logger.Warning($"获取玩家档案失败: {playerName} - 无响应");
                return null;
            }

            // 解析响应
            var profile = ParseProfileResponse(response);
            
            if (profile != null)
            {
                // 更新缓存
                _profileCache[cacheKey] = profile;
                _cacheTimestamps[cacheKey] = DateTime.UtcNow;
                
                _logger.Debug($"成功获取玩家档案: {profile.Name} ({profile.Uuid})");
            }
            else
            {
                _logger.Warning($"解析玩家档案失败: {playerName}");
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取玩家档案时发生错误: {playerName}", ex);
            return null;
        }
    }

    public async Task<PlayerProfile?> FetchProfileByUuidAsync(Guid uuid)
    {
        if (uuid == Guid.Empty)
        {
            _logger.Warning("UUID 不能为空");
            return null;
        }

        // 检查缓存
        var cacheKey = uuid.ToString();
        if (_profileCache.TryGetValue(cacheKey, out var cachedProfile) &&
            _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
            DateTime.UtcNow - timestamp < _cacheExpiration)
        {
            _logger.Debug($"从缓存获取玩家档案: {uuid}");
            return cachedProfile;
        }

        if (_rconClient == null)
        {
            _logger.Warning("RCON 客户端未配置，无法获取玩家档案");
            return null;
        }

        try
        {
            _logger.Debug($"通过 UUID 获取玩家档案: {uuid}");
            
            // 执行 fetchprofile 命令（UUID 格式需要带连字符）
            var uuidString = uuid.ToString();
            var response = await _rconClient.ExecuteCommandAsync($"fetchprofile id {uuidString}");

            if (string.IsNullOrEmpty(response))
            {
                _logger.Warning($"获取玩家档案失败: {uuid} - 无响应");
                return null;
            }

            // 解析响应
            var profile = ParseProfileResponse(response);
            
            if (profile != null)
            {
                // 更新缓存
                _profileCache[cacheKey] = profile;
                _profileCache[profile.Name.ToLowerInvariant()] = profile;
                _cacheTimestamps[cacheKey] = DateTime.UtcNow;
                _cacheTimestamps[profile.Name.ToLowerInvariant()] = DateTime.UtcNow;
                
                _logger.Debug($"成功获取玩家档案: {profile.Name} ({profile.Uuid})");
            }
            else
            {
                _logger.Warning($"解析玩家档案失败: {uuid}");
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取玩家档案时发生错误: {uuid}", ex);
            return null;
        }
    }

    public async Task<List<PlayerProfile>> FetchProfilesAsync(params string[] playerNames)
    {
        var profiles = new List<PlayerProfile>();
        
        foreach (var name in playerNames)
        {
            var profile = await FetchProfileByNameAsync(name);
            if (profile != null)
            {
                profiles.Add(profile);
            }
        }
        
        return profiles;
    }

    public async Task<string?> GetPlayerHeadNbtAsync(string playerName)
    {
        var profile = await FetchProfileByNameAsync(playerName);
        if (profile == null)
        {
            return null;
        }

        // 构建玩家头颅的 NBT 数据
        var textureProperty = profile.Properties.FirstOrDefault(p => p.Name == "textures");
        if (textureProperty == null)
        {
            return null;
        }

        // 生成 SNBT 格式的 NBT 数据
        var nbt = $"{{profile:{{id:[I;{FormatUuidAsIntArray(profile.Uuid)}]," +
                  $"name:\"{profile.Name}\"," +
                  $"properties:[{{name:\"textures\",value:\"{textureProperty.Value}\"";
        
        if (!string.IsNullOrEmpty(textureProperty.Signature))
        {
            nbt += $",signature:\"{textureProperty.Signature}\"";
        }
        
        nbt += "}}]}}";
        
        return nbt;
    }

    /// <summary>
    /// 解析 fetchprofile 命令的响应
    /// </summary>
    private PlayerProfile? ParseProfileResponse(string response)
    {
        try
        {
            // fetchprofile 命令会在成功时返回包含 profile 组件的消息
            // 由于是异步命令，返回值为 1，实际数据在消息中
            
            // 提取 UUID
            var uuidMatch = Regex.Match(response, @"id:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");
            if (!uuidMatch.Success)
            {
                return null;
            }

            var uuid = ParseUuidFromIntArray(
                int.Parse(uuidMatch.Groups[1].Value),
                int.Parse(uuidMatch.Groups[2].Value),
                int.Parse(uuidMatch.Groups[3].Value),
                int.Parse(uuidMatch.Groups[4].Value));

            // 提取玩家名称
            var nameMatch = Regex.Match(response, @"name:\s*""([^""]+)""");
            if (!nameMatch.Success)
            {
                return null;
            }
            var name = nameMatch.Groups[1].Value;

            // 提取属性
            var properties = new List<ProfileProperty>();
            var propertiesMatch = Regex.Match(response, @"properties:\[(.*?)\]", RegexOptions.Singleline);
            
            if (propertiesMatch.Success)
            {
                var propertiesContent = propertiesMatch.Groups[1].Value;
                
                // 提取 textures 属性
                var textureValueMatch = Regex.Match(propertiesContent, @"name:\s*""textures"",\s*value:\s*""([^""]+)""");
                if (textureValueMatch.Success)
                {
                    var textureValue = textureValueMatch.Groups[1].Value;
                    
                    // 提取可选的 signature
                    var signatureMatch = Regex.Match(propertiesContent, @"signature:\s*""([^""]+)""");
                    var signature = signatureMatch.Success ? signatureMatch.Groups[1].Value : null;
                    
                    properties.Add(new ProfileProperty
                    {
                        Name = "textures",
                        Value = textureValue,
                        Signature = signature
                    });
                }
            }

            return new PlayerProfile
            {
                Uuid = uuid,
                Name = name,
                Properties = properties
            };
        }
        catch (Exception ex)
        {
            _logger.Error("解析 profile 响应失败", ex);
            return null;
        }
    }

    /// <summary>
    /// 从 int 数组解析 UUID
    /// </summary>
    private static Guid ParseUuidFromIntArray(int i0, int i1, int i2, int i3)
    {
        var bytes = new byte[16];
        
        // 将 4 个 int 转换为 16 字节
        BitConverter.GetBytes(i0).CopyTo(bytes, 0);
        BitConverter.GetBytes(i1).CopyTo(bytes, 4);
        BitConverter.GetBytes(i2).CopyTo(bytes, 8);
        BitConverter.GetBytes(i3).CopyTo(bytes, 12);
        
        // 处理字节序（Minecraft 使用大端序）
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 4);
            Array.Reverse(bytes, 8, 4);
            Array.Reverse(bytes, 12, 4);
        }
        
        return new Guid(bytes);
    }

    /// <summary>
    /// 将 UUID 格式化为 int 数组
    /// </summary>
    private static string FormatUuidAsIntArray(Guid uuid)
    {
        var bytes = uuid.ToByteArray();
        
        // 处理字节序
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 4);
            Array.Reverse(bytes, 8, 4);
            Array.Reverse(bytes, 12, 4);
        }
        
        var i0 = BitConverter.ToInt32(bytes, 0);
        var i1 = BitConverter.ToInt32(bytes, 4);
        var i2 = BitConverter.ToInt32(bytes, 8);
        var i3 = BitConverter.ToInt32(bytes, 12);
        
        return $"{i0},{i1},{i2},{i3}";
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    public void CleanupCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cacheTimestamps
            .Where(kvp => now - kvp.Value >= _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _profileCache.Remove(key);
            _cacheTimestamps.Remove(key);
        }

        if (expiredKeys.Any())
        {
            _logger.Debug($"清理了 {expiredKeys.Count} 个过期的档案缓存");
        }
    }
}

