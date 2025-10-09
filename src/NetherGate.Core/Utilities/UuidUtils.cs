using System.Text.Json;

namespace NetherGate.Core.Utilities;

/// <summary>
/// UUID 工具类
/// 提供 UUID 的解析、转换和格式化功能
/// </summary>
public static class UuidUtils
{
    /// <summary>
    /// 从 Minecraft 的 int 数组格式解析 UUID
    /// Minecraft 在 NBT 中使用 [I;a,b,c,d] 格式存储 UUID
    /// </summary>
    /// <param name="intArray">包含 4 个 int 的数组</param>
    /// <returns>解析后的 UUID，如果解析失败则返回 Guid.Empty</returns>
    public static Guid ParseUuidFromIntArray(int[] intArray)
    {
        if (intArray.Length != 4)
            return Guid.Empty;

        try
        {
            var bytes = new byte[16];
            
            // 将 4 个 int32 值转换为 16 字节的 UUID
            Buffer.BlockCopy(BitConverter.GetBytes(intArray[0]), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(intArray[1]), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(intArray[2]), 0, bytes, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(intArray[3]), 0, bytes, 12, 4);
            
            // 修正字节顺序
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, 4);
                Array.Reverse(bytes, 4, 4);
                Array.Reverse(bytes, 8, 4);
                Array.Reverse(bytes, 12, 4);
            }
            
            return new Guid(bytes);
        }
        catch
        {
            return Guid.Empty;
        }
    }

    /// <summary>
    /// 将 UUID 格式化为 Minecraft 的 int 数组格式
    /// 返回格式：[I;a,b,c,d]
    /// </summary>
    /// <param name="uuid">要格式化的 UUID</param>
    /// <returns>Minecraft int 数组格式的字符串</returns>
    public static string FormatUuidAsIntArray(Guid uuid)
    {
        var bytes = uuid.ToByteArray();
        
        // 修正字节顺序
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 4);
            Array.Reverse(bytes, 8, 4);
            Array.Reverse(bytes, 12, 4);
        }
        
        var intArray = new int[4];
        intArray[0] = BitConverter.ToInt32(bytes, 0);
        intArray[1] = BitConverter.ToInt32(bytes, 4);
        intArray[2] = BitConverter.ToInt32(bytes, 8);
        intArray[3] = BitConverter.ToInt32(bytes, 12);
        
        return $"[I;{intArray[0]},{intArray[1]},{intArray[2]},{intArray[3]}]";
    }

    /// <summary>
    /// 从 usercache.json 中读取玩家 UUID
    /// </summary>
    /// <param name="serverDirectory">服务器目录</param>
    /// <param name="playerName">玩家名称</param>
    /// <returns>玩家的 UUID，如果未找到则返回 Guid.Empty</returns>
    public static async Task<Guid> GetPlayerUuidFromCacheAsync(string serverDirectory, string playerName)
    {
        try
        {
            var usercachePath = Path.Combine(serverDirectory, "usercache.json");
            if (!File.Exists(usercachePath))
                return Guid.Empty;

            var json = await File.ReadAllTextAsync(usercachePath);
            var entries = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
            
            if (entries == null)
                return Guid.Empty;

            var entry = entries.FirstOrDefault(e =>
                e.TryGetValue("name", out var nameElement) &&
                nameElement.GetString()?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

            if (entry != null && entry.TryGetValue("uuid", out var uuidElement))
            {
                var uuidString = uuidElement.GetString()?.Replace("-", "");
                if (Guid.TryParse(uuidString, out var uuid))
                {
                    return uuid;
                }
            }
        }
        catch
        {
            // 静默失败，返回空 UUID
        }

        return Guid.Empty;
    }

    /// <summary>
    /// 格式化 UUID 为标准格式（带连字符）
    /// </summary>
    /// <param name="uuid">UUID</param>
    /// <returns>格式化后的 UUID 字符串（例如：xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx）</returns>
    public static string FormatUuid(Guid uuid)
    {
        return uuid.ToString();
    }

    /// <summary>
    /// 格式化 UUID 为无连字符格式
    /// </summary>
    /// <param name="uuid">UUID</param>
    /// <returns>无连字符的 UUID 字符串</returns>
    public static string FormatUuidWithoutDashes(Guid uuid)
    {
        return uuid.ToString("N");
    }

    /// <summary>
    /// 解析 UUID 字符串（支持带或不带连字符）
    /// </summary>
    /// <param name="uuidString">UUID 字符串</param>
    /// <returns>解析后的 UUID，如果解析失败则返回 null</returns>
    public static Guid? ParseUuid(string uuidString)
    {
        if (string.IsNullOrWhiteSpace(uuidString))
            return null;

        // 移除连字符（如果有）
        var cleanedString = uuidString.Replace("-", "");
        
        if (Guid.TryParse(cleanedString, out var uuid))
            return uuid;

        return null;
    }

    /// <summary>
    /// 验证 UUID 字符串是否有效
    /// </summary>
    /// <param name="uuidString">UUID 字符串</param>
    /// <returns>如果是有效的 UUID 格式则返回 true</returns>
    public static bool IsValidUuid(string uuidString)
    {
        return ParseUuid(uuidString) != null;
    }

    /// <summary>
    /// 获取玩家数据文件路径
    /// </summary>
    /// <param name="serverDirectory">服务器目录</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="worldName">世界名称（默认为 "world"）</param>
    /// <returns>玩家数据文件的完整路径</returns>
    public static string GetPlayerDataFilePath(string serverDirectory, Guid playerUuid, string worldName = "world")
    {
        return Path.Combine(serverDirectory, worldName, "playerdata", $"{playerUuid}.dat");
    }

    /// <summary>
    /// 获取玩家统计文件路径
    /// </summary>
    /// <param name="serverDirectory">服务器目录</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="worldName">世界名称（默认为 "world"）</param>
    /// <returns>玩家统计文件的完整路径</returns>
    public static string GetPlayerStatsFilePath(string serverDirectory, Guid playerUuid, string worldName = "world")
    {
        return Path.Combine(serverDirectory, worldName, "stats", $"{playerUuid}.json");
    }

    /// <summary>
    /// 获取玩家成就文件路径
    /// </summary>
    /// <param name="serverDirectory">服务器目录</param>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="worldName">世界名称（默认为 "world"）</param>
    /// <returns>玩家成就文件的完整路径</returns>
    public static string GetPlayerAdvancementsFilePath(string serverDirectory, Guid playerUuid, string worldName = "world")
    {
        return Path.Combine(serverDirectory, worldName, "advancements", $"{playerUuid}.json");
    }
}

