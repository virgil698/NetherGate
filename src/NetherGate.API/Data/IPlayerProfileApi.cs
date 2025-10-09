namespace NetherGate.API.Data;

/// <summary>
/// 玩家档案 API 接口
/// 用于获取玩家档案信息，包括 UUID、皮肤纹理等
/// 基于 Minecraft 1.21.9+ 的 /fetchprofile 命令
/// </summary>
public interface IPlayerProfileApi
{
    /// <summary>
    /// 通过玩家名称获取玩家档案
    /// </summary>
    /// <param name="playerName">玩家名称（不区分大小写）</param>
    /// <returns>玩家档案信息，获取失败返回 null</returns>
    Task<PlayerProfile?> FetchProfileByNameAsync(string playerName);

    /// <summary>
    /// 通过玩家 UUID 获取玩家档案
    /// </summary>
    /// <param name="uuid">玩家 UUID</param>
    /// <returns>玩家档案信息，获取失败返回 null</returns>
    Task<PlayerProfile?> FetchProfileByUuidAsync(Guid uuid);

    /// <summary>
    /// 批量获取玩家档案
    /// </summary>
    /// <param name="playerNames">玩家名称列表</param>
    /// <returns>成功获取的玩家档案列表</returns>
    Task<List<PlayerProfile>> FetchProfilesAsync(params string[] playerNames);

    /// <summary>
    /// 获取玩家头颅物品的 NBT 数据
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <returns>玩家头颅的 NBT 数据（SNBT 格式），获取失败返回 null</returns>
    Task<string?> GetPlayerHeadNbtAsync(string playerName);
}

/// <summary>
/// 玩家档案信息
/// </summary>
public class PlayerProfile
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid Uuid { get; init; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 档案属性（包含皮肤纹理等）
    /// </summary>
    public List<ProfileProperty> Properties { get; init; } = new();

    /// <summary>
    /// 皮肤纹理数据（Minecraft 1.21.9+）
    /// </summary>
    public ProfileTexture? Texture { get; set; }

    /// <summary>
    /// 披风纹理数据（Minecraft 1.21.9+）
    /// </summary>
    public ProfileTexture? Cape { get; set; }

    /// <summary>
    /// 鞘翅纹理数据（Minecraft 1.21.9+）
    /// </summary>
    public ProfileTexture? Elytra { get; set; }

    /// <summary>
    /// 玩家模型类型（Minecraft 1.21.9+）
    /// </summary>
    public PlayerModel Model { get; set; } = PlayerModel.Wide;

    /// <summary>
    /// 获取皮肤纹理 URL
    /// </summary>
    public string? GetSkinUrl()
    {
        var texturesProperty = Properties.FirstOrDefault(p => p.Name == "textures");
        if (texturesProperty == null)
            return null;

        try
        {
            // Base64 解码纹理数据
            var json = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(texturesProperty.Value));
            
            // 简单的 JSON 解析提取 URL
            var skinIndex = json.IndexOf("\"SKIN\"");
            if (skinIndex == -1)
                return null;

            var urlIndex = json.IndexOf("\"url\"", skinIndex);
            if (urlIndex == -1)
                return null;

            var urlStart = json.IndexOf("\"http", urlIndex);
            if (urlStart == -1)
                return null;

            var urlEnd = json.IndexOf("\"", urlStart + 1);
            if (urlEnd == -1)
                return null;

            return json.Substring(urlStart + 1, urlEnd - urlStart - 1);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取披风纹理 URL
    /// </summary>
    public string? GetCapeUrl()
    {
        var texturesProperty = Properties.FirstOrDefault(p => p.Name == "textures");
        if (texturesProperty == null)
            return null;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(texturesProperty.Value));
            
            var capeIndex = json.IndexOf("\"CAPE\"");
            if (capeIndex == -1)
                return null;

            var urlIndex = json.IndexOf("\"url\"", capeIndex);
            if (urlIndex == -1)
                return null;

            var urlStart = json.IndexOf("\"http", urlIndex);
            if (urlStart == -1)
                return null;

            var urlEnd = json.IndexOf("\"", urlStart + 1);
            if (urlEnd == -1)
                return null;

            return json.Substring(urlStart + 1, urlEnd - urlStart - 1);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 档案属性
/// </summary>
public class ProfileProperty
{
    /// <summary>
    /// 属性名称（如 "textures"）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 属性值（Base64 编码）
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 签名（可选）
    /// </summary>
    public string? Signature { get; init; }
}

/// <summary>
/// 玩家头颅数据
/// </summary>
public class PlayerHead
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; init; } = string.Empty;

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid Uuid { get; init; }

    /// <summary>
    /// 头颅的 NBT 数据（SNBT 格式）
    /// 可直接用于 /give 命令
    /// </summary>
    public string NbtData { get; init; } = string.Empty;

    /// <summary>
    /// 生成 give 命令
    /// </summary>
    /// <param name="targetPlayer">目标玩家</param>
    /// <param name="count">数量</param>
    /// <returns>完整的 give 命令</returns>
    public string GenerateGiveCommand(string targetPlayer, int count = 1)
    {
        return $"give {targetPlayer} minecraft:player_head{NbtData} {count}";
    }
}

/// <summary>
/// 纹理数据
/// </summary>
public class ProfileTexture
{
    /// <summary>
    /// 纹理 URL
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// 纹理元数据（可选）
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// 玩家模型类型
/// </summary>
public enum PlayerModel
{
    /// <summary>
    /// 标准模型（4像素手臂）
    /// </summary>
    Wide,

    /// <summary>
    /// 纤细模型（3像素手臂，如 Alex）
    /// </summary>
    Slim
}

