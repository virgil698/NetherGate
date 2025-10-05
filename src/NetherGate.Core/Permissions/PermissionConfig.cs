using System.Text.Json.Serialization;

namespace NetherGate.Core.Permissions;

/// <summary>
/// 权限级别
/// </summary>
public enum PermissionLevel
{
    Guest = 0,      // 访客
    Member = 1,     // 普通成员
    Moderator = 2,  // 管理员
    Admin = 3,      // 超级管理员
    Owner = 4       // 所有者
}

/// <summary>
/// 权限组配置
/// </summary>
public class PermissionGroup
{
    /// <summary>
    /// 组名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 优先级（数字越大权限越高）
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// 权限节点列表
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// 继承的组列表
    /// </summary>
    [JsonPropertyName("inherit_from")]
    public List<string> InheritFrom { get; set; } = new();

    /// <summary>
    /// 是否为默认组
    /// </summary>
    [JsonPropertyName("default")]
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// 权限配置文件
/// </summary>
public class PermissionConfig
{
    /// <summary>
    /// 权限组列表
    /// </summary>
    [JsonPropertyName("groups")]
    public Dictionary<string, PermissionGroup> Groups { get; set; } = new();

    /// <summary>
    /// 玩家-组映射
    /// </summary>
    [JsonPropertyName("players")]
    public Dictionary<string, List<string>> Players { get; set; } = new();

    /// <summary>
    /// 玩家特定权限（覆盖组权限）
    /// </summary>
    [JsonPropertyName("player_permissions")]
    public Dictionary<string, List<string>> PlayerPermissions { get; set; } = new();

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static PermissionConfig CreateDefault()
    {
        return new PermissionConfig
        {
            Groups = new Dictionary<string, PermissionGroup>
            {
                ["guest"] = new PermissionGroup
                {
                    Name = "guest",
                    Priority = 0,
                    Permissions = new List<string>
                    {
                        "nethergate.help",
                        "nethergate.list"
                    },
                    IsDefault = true
                },
                ["member"] = new PermissionGroup
                {
                    Name = "member",
                    Priority = 10,
                    InheritFrom = new List<string> { "guest" },
                    Permissions = new List<string>
                    {
                        "nethergate.tpa",
                        "nethergate.home",
                        "nethergate.back"
                    }
                },
                ["moderator"] = new PermissionGroup
                {
                    Name = "moderator",
                    Priority = 50,
                    InheritFrom = new List<string> { "member" },
                    Permissions = new List<string>
                    {
                        "nethergate.kick",
                        "nethergate.mute",
                        "nethergate.warn",
                        "nethergate.ban"
                    }
                },
                ["admin"] = new PermissionGroup
                {
                    Name = "admin",
                    Priority = 100,
                    InheritFrom = new List<string> { "moderator" },
                    Permissions = new List<string>
                    {
                        "nethergate.*"  // 所有权限
                    }
                }
            },
            Players = new Dictionary<string, List<string>>(),
            PlayerPermissions = new Dictionary<string, List<string>>()
        };
    }
}
