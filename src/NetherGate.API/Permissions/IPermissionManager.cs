namespace NetherGate.API.Permissions;

/// <summary>
/// 权限管理器接口
/// NetherGate 核心只提供此接口，具体实现由权限管理插件提供
/// </summary>
public interface IPermissionManager
{
    // ========== 权限检查（核心功能）==========
    
    /// <summary>
    /// 检查玩家是否拥有权限
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <param name="permission">权限节点</param>
    /// <returns>是否拥有权限</returns>
    Task<bool> HasPermissionAsync(string playerName, string permission);
    
    /// <summary>
    /// 获取玩家的所有权限
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <returns>权限列表</returns>
    Task<List<string>> GetUserPermissionsAsync(string playerName);
    
    // ========== 用户组管理（由权限插件实现）==========
    
    /// <summary>
    /// 将玩家添加到组
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <param name="groupName">组名</param>
    Task AddUserToGroupAsync(string playerName, string groupName);
    
    /// <summary>
    /// 从组移除玩家
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <param name="groupName">组名</param>
    Task RemoveUserFromGroupAsync(string playerName, string groupName);
    
    /// <summary>
    /// 获取玩家所在的组
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <returns>组名列表</returns>
    Task<List<string>> GetUserGroupsAsync(string playerName);
    
    // ========== 权限管理（由权限插件实现）==========
    
    /// <summary>
    /// 给予玩家权限
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <param name="permission">权限节点</param>
    Task GrantPermissionAsync(string playerName, string permission);
    
    /// <summary>
    /// 移除玩家权限
    /// </summary>
    /// <param name="playerName">玩家名</param>
    /// <param name="permission">权限节点</param>
    Task RevokePermissionAsync(string playerName, string permission);
    
    /// <summary>
    /// 创建权限组
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="priority">优先级</param>
    Task CreateGroupAsync(string groupName, int priority = 0);
    
    /// <summary>
    /// 删除权限组
    /// </summary>
    /// <param name="groupName">组名</param>
    Task DeleteGroupAsync(string groupName);
    
    /// <summary>
    /// 给予组权限
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="permission">权限节点</param>
    Task GrantGroupPermissionAsync(string groupName, string permission);
    
    /// <summary>
    /// 移除组权限
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="permission">权限节点</param>
    Task RevokeGroupPermissionAsync(string groupName, string permission);
    
    /// <summary>
    /// 获取组的所有权限
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <returns>权限列表</returns>
    Task<List<string>> GetGroupPermissionsAsync(string groupName);
    
    /// <summary>
    /// 重新加载权限配置
    /// </summary>
    Task ReloadAsync();
    
    /// <summary>
    /// 保存权限配置
    /// </summary>
    Task SaveAsync();
}

/// <summary>
/// 权限组
/// </summary>
public class PermissionGroup
{
    /// <summary>
    /// 组名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限列表
    /// </summary>
    public HashSet<string> Permissions { get; set; } = new();

    /// <summary>
    /// 继承的组
    /// </summary>
    public HashSet<string> InheritedGroups { get; set; } = new();
}

/// <summary>
/// 用户权限
/// </summary>
public class UserPermissions
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// 直接授予的权限
    /// </summary>
    public HashSet<string> Permissions { get; set; } = new();

    /// <summary>
    /// 所属的权限组
    /// </summary>
    public HashSet<string> Groups { get; set; } = new();
}

