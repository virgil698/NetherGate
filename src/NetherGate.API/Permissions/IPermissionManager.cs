namespace NetherGate.API.Permissions;

/// <summary>
/// 权限管理器接口
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// 检查用户是否有权限
    /// </summary>
    /// <param name="user">用户名</param>
    /// <param name="permission">权限节点</param>
    bool HasPermission(string user, string permission);

    /// <summary>
    /// 授予用户权限
    /// </summary>
    /// <param name="user">用户名</param>
    /// <param name="permission">权限节点</param>
    void GrantPermission(string user, string permission);

    /// <summary>
    /// 撤销用户权限
    /// </summary>
    /// <param name="user">用户名</param>
    /// <param name="permission">权限节点</param>
    void RevokePermission(string user, string permission);

    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    /// <param name="user">用户名</param>
    List<string> GetPermissions(string user);

    /// <summary>
    /// 创建权限组
    /// </summary>
    /// <param name="groupName">组名</param>
    void CreateGroup(string groupName);

    /// <summary>
    /// 删除权限组
    /// </summary>
    /// <param name="groupName">组名</param>
    void DeleteGroup(string groupName);

    /// <summary>
    /// 将用户添加到权限组
    /// </summary>
    /// <param name="user">用户名</param>
    /// <param name="groupName">组名</param>
    void AddUserToGroup(string user, string groupName);

    /// <summary>
    /// 将用户从权限组移除
    /// </summary>
    /// <param name="user">用户名</param>
    /// <param name="groupName">组名</param>
    void RemoveUserFromGroup(string user, string groupName);

    /// <summary>
    /// 授予权限组权限
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="permission">权限节点</param>
    void GrantGroupPermission(string groupName, string permission);

    /// <summary>
    /// 撤销权限组权限
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="permission">权限节点</param>
    void RevokeGroupPermission(string groupName, string permission);

    /// <summary>
    /// 获取用户所在的所有组
    /// </summary>
    /// <param name="user">用户名</param>
    List<string> GetUserGroups(string user);

    /// <summary>
    /// 获取权限组的所有权限
    /// </summary>
    /// <param name="groupName">组名</param>
    List<string> GetGroupPermissions(string groupName);

    /// <summary>
    /// 保存权限配置
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// 重新加载权限配置
    /// </summary>
    Task ReloadAsync();
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

