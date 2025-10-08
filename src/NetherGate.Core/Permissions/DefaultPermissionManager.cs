using NetherGate.API.Logging;
using NetherGate.API.Permissions;

namespace NetherGate.Core.Permissions;

/// <summary>
/// 默认权限管理器 - 允许所有操作
/// 这是一个占位实现，真正的权限管理由插件提供
/// </summary>
public class DefaultPermissionManager : IPermissionManager
{
    private readonly ILogger _logger;

    public DefaultPermissionManager(ILogger logger)
    {
        _logger = logger;
        _logger.Warning("使用默认权限管理器 - 所有权限检查将返回 true");
        _logger.Warning("建议安装权限管理插件以启用完整的权限控制");
    }

    public Task<bool> HasPermissionAsync(string playerName, string permission)
    {
        // 默认允许所有权限
        return Task.FromResult(true);
    }

    public Task<List<string>> GetUserPermissionsAsync(string playerName)
    {
        return Task.FromResult(new List<string> { "*" });
    }

    public Task AddUserToGroupAsync(string playerName, string groupName)
    {
        _logger.Warning($"默认权限管理器不支持组管理 - AddUserToGroup({playerName}, {groupName})");
        return Task.CompletedTask;
    }

    public Task RemoveUserFromGroupAsync(string playerName, string groupName)
    {
        _logger.Warning($"默认权限管理器不支持组管理 - RemoveUserFromGroup({playerName}, {groupName})");
        return Task.CompletedTask;
    }

    public Task<List<string>> GetUserGroupsAsync(string playerName)
    {
        return Task.FromResult(new List<string>());
    }

    public Task GrantPermissionAsync(string playerName, string permission)
    {
        _logger.Warning($"默认权限管理器不支持权限管理 - GrantPermission({playerName}, {permission})");
        return Task.CompletedTask;
    }

    public Task RevokePermissionAsync(string playerName, string permission)
    {
        _logger.Warning($"默认权限管理器不支持权限管理 - RevokePermission({playerName}, {permission})");
        return Task.CompletedTask;
    }

    public Task CreateGroupAsync(string groupName, int priority = 0)
    {
        _logger.Warning($"默认权限管理器不支持组管理 - CreateGroup({groupName}, {priority})");
        return Task.CompletedTask;
    }

    public Task DeleteGroupAsync(string groupName)
    {
        _logger.Warning($"默认权限管理器不支持组管理 - DeleteGroup({groupName})");
        return Task.CompletedTask;
    }

    public Task GrantGroupPermissionAsync(string groupName, string permission)
    {
        _logger.Warning($"默认权限管理器不支持组管理 - GrantGroupPermission({groupName}, {permission})");
        return Task.CompletedTask;
    }

    public Task RevokeGroupPermissionAsync(string groupName, string permission)
    {
        _logger.Warning($"默认权限管理器不支持组管理 - RevokeGroupPermission({groupName}, {permission})");
        return Task.CompletedTask;
    }

    public Task<List<string>> GetGroupPermissionsAsync(string groupName)
    {
        return Task.FromResult(new List<string>());
    }

    public Task ReloadAsync()
    {
        _logger.Info("默认权限管理器 - 无配置可重载");
        return Task.CompletedTask;
    }

    public Task SaveAsync()
    {
        _logger.Info("默认权限管理器 - 无配置可保存");
        return Task.CompletedTask;
    }
}

