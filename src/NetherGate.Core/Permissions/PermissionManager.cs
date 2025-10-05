using System.Text.Json;
using NetherGate.API.Logging;
using NetherGate.API.Permissions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetherGate.Core.Permissions;

/// <summary>
/// 权限管理器实现
/// </summary>
public class PermissionManager : IPermissionManager
{
    private readonly ILogger _logger;
    private readonly string _configPath;
    private readonly Dictionary<string, PermissionGroupInternal> _groups = new();
    private readonly Dictionary<string, HashSet<string>> _playerGroups = new();  // player -> groups
    private readonly Dictionary<string, HashSet<string>> _playerPermissions = new();  // player -> permissions
    private readonly object _lock = new();

    public PermissionManager(ILogger logger, string configPath = "config/permissions.yaml")
    {
        _logger = logger;
        _configPath = configPath;
    }

    /// <summary>
    /// 初始化（异步加载配置）
    /// </summary>
    public async Task InitializeAsync()
    {
        await ReloadAsync();
    }

    /// <summary>
    /// 从配置创建权限组
    /// </summary>
    private void LoadFromConfig(PermissionConfig config)
    {
        lock (_lock)
        {
            _groups.Clear();
            _playerGroups.Clear();
            _playerPermissions.Clear();

            // 加载组
            foreach (var (groupName, group) in config.Groups)
            {
                var internalGroup = new PermissionGroupInternal
                {
                    Name = group.Name,
                    Priority = group.Priority,
                    Permissions = new HashSet<string>(group.Permissions),
                    InheritFrom = new HashSet<string>(group.InheritFrom),
                    IsDefault = group.IsDefault
                };
                _groups[groupName] = internalGroup;
            }

            // 加载玩家-组映射
            foreach (var (player, groups) in config.Players)
            {
                _playerGroups[player] = new HashSet<string>(groups);
            }

            // 加载玩家特定权限
            if (config.PlayerPermissions != null)
            {
                foreach (var (player, permissions) in config.PlayerPermissions)
                {
                    _playerPermissions[player] = new HashSet<string>(permissions);
                }
            }

            _logger.Info($"已加载 {_groups.Count} 个权限组，{_playerGroups.Count} 个玩家配置");
        }
    }

    /// <summary>
    /// 检查权限
    /// </summary>
    public bool HasPermission(string user, string permission)
    {
        lock (_lock)
        {
            // 控制台拥有所有权限
            if (user == "Console" || user == "CONSOLE")
                return true;

            // 1. 检查玩家特定权限
            if (_playerPermissions.TryGetValue(user, out var playerPerms))
            {
                if (CheckPermissionNode(playerPerms, permission))
                    return true;
            }

            // 2. 检查玩家所在组的权限
            if (_playerGroups.TryGetValue(user, out var groups))
            {
                foreach (var groupName in groups)
                {
                    if (HasGroupPermission(groupName, permission, new HashSet<string>()))
                        return true;
                }
            }

            // 3. 检查默认组
            var defaultGroup = _groups.Values.FirstOrDefault(g => g.IsDefault);
            if (defaultGroup != null)
            {
                return HasGroupPermission(defaultGroup.Name, permission, new HashSet<string>());
            }

            return false;
        }
    }

    /// <summary>
    /// 检查组是否有权限（包括继承）
    /// </summary>
    private bool HasGroupPermission(string groupName, string permission, HashSet<string> visited)
    {
        if (!_groups.TryGetValue(groupName, out var group))
            return false;

        if (visited.Contains(groupName))
            return false;

        visited.Add(groupName);

        // 检查组的直接权限
        if (CheckPermissionNode(group.Permissions, permission))
            return true;

        // 检查继承的组
        foreach (var inheritedGroupName in group.InheritFrom)
        {
            if (HasGroupPermission(inheritedGroupName, permission, visited))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检查权限节点
    /// </summary>
    private bool CheckPermissionNode(HashSet<string> permissions, string permission)
    {
        // 通配符权限
        if (permissions.Contains("*"))
            return true;

        // 精确匹配
        if (permissions.Contains(permission))
            return true;

        // 否定权限（以 - 开头）
        if (permissions.Contains($"-{permission}"))
            return false;

        // 通配符匹配（例如：nethergate.* 匹配 nethergate.plugins）
        var parts = permission.Split('.');
        for (int i = parts.Length; i > 0; i--)
        {
            var wildcard = string.Join(".", parts.Take(i)) + ".*";
            if (permissions.Contains(wildcard))
                return true;
            
            // 检查否定通配符
            if (permissions.Contains($"-{wildcard}"))
                return false;
        }

        return false;
    }

    /// <summary>
    /// 授予用户权限
    /// </summary>
    public void GrantPermission(string user, string permission)
    {
        lock (_lock)
        {
            if (!_playerPermissions.ContainsKey(user))
            {
                _playerPermissions[user] = new HashSet<string>();
            }

            _playerPermissions[user].Add(permission);
            _logger.Info($"授予用户 '{user}' 权限: {permission}");
        }
    }

    /// <summary>
    /// 撤销用户权限
    /// </summary>
    public void RevokePermission(string user, string permission)
    {
        lock (_lock)
        {
            if (_playerPermissions.TryGetValue(user, out var perms))
            {
                perms.Remove(permission);
                _logger.Info($"撤销用户 '{user}' 权限: {permission}");
            }
        }
    }

    /// <summary>
    /// 获取用户权限
    /// </summary>
    public List<string> GetPermissions(string user)
    {
        lock (_lock)
        {
            var allPermissions = new HashSet<string>();

            // 玩家特定权限
            if (_playerPermissions.TryGetValue(user, out var playerPerms))
            {
                allPermissions.UnionWith(playerPerms);
            }

            // 组权限
            if (_playerGroups.TryGetValue(user, out var groups))
            {
                foreach (var groupName in groups)
                {
                    CollectGroupPermissions(groupName, allPermissions, new HashSet<string>());
                }
            }

            return allPermissions.ToList();
        }
    }

    /// <summary>
    /// 收集组权限（包括继承）
    /// </summary>
    private void CollectGroupPermissions(string groupName, HashSet<string> permissions, HashSet<string> visited)
    {
        if (!_groups.TryGetValue(groupName, out var group))
            return;

        if (visited.Contains(groupName))
            return;

        visited.Add(groupName);

        permissions.UnionWith(group.Permissions);

        foreach (var inheritedGroupName in group.InheritFrom)
        {
            CollectGroupPermissions(inheritedGroupName, permissions, visited);
        }
    }

    /// <summary>
    /// 将用户添加到组
    /// </summary>
    public void AddUserToGroup(string user, string groupName)
    {
        lock (_lock)
        {
            if (!_groups.ContainsKey(groupName))
            {
                _logger.Warning($"权限组不存在: {groupName}");
                return;
            }

            if (!_playerGroups.ContainsKey(user))
            {
                _playerGroups[user] = new HashSet<string>();
            }

            _playerGroups[user].Add(groupName);
            _logger.Info($"将用户 '{user}' 添加到组: {groupName}");
        }
    }

    /// <summary>
    /// 将用户从组中移除
    /// </summary>
    public void RemoveUserFromGroup(string user, string groupName)
    {
        lock (_lock)
        {
            if (_playerGroups.TryGetValue(user, out var groups))
            {
                groups.Remove(groupName);
                _logger.Info($"将用户 '{user}' 从组移除: {groupName}");
            }
        }
    }

    /// <summary>
    /// 获取用户所在的所有组
    /// </summary>
    public List<string> GetUserGroups(string user)
    {
        lock (_lock)
        {
            if (_playerGroups.TryGetValue(user, out var groups))
            {
                return groups.ToList();
            }

            return new List<string>();
        }
    }

    /// <summary>
    /// 重新加载权限配置
    /// </summary>
    public async Task ReloadAsync()
    {
        try
        {
            // 检查配置文件是否存在
            if (!File.Exists(_configPath))
            {
                _logger.Info("权限配置文件不存在，创建默认配置");
                var defaultConfig = PermissionConfig.CreateDefault();
                await SaveConfigAsync(defaultConfig);
                LoadFromConfig(defaultConfig);
                return;
            }

            // 根据文件扩展名选择解析器
            PermissionConfig? config = null;

            if (_configPath.EndsWith(".yaml") || _configPath.EndsWith(".yml"))
            {
                // YAML 格式
                var yaml = await File.ReadAllTextAsync(_configPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                config = deserializer.Deserialize<PermissionConfig>(yaml);
            }
            else
            {
                // JSON 格式
                var json = await File.ReadAllTextAsync(_configPath);
                config = JsonSerializer.Deserialize<PermissionConfig>(json);
            }

            if (config != null)
            {
                LoadFromConfig(config);
                _logger.Info($"权限配置已重新加载: {_configPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"重新加载权限配置失败: {ex.Message}", ex);
            // 加载默认配置
            LoadFromConfig(PermissionConfig.CreateDefault());
        }
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    private async Task SaveConfigAsync(PermissionConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (_configPath.EndsWith(".yaml") || _configPath.EndsWith(".yml"))
            {
                // YAML 格式
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                var yaml = serializer.Serialize(config);
                await File.WriteAllTextAsync(_configPath, yaml);
            }
            else
            {
                // JSON 格式
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_configPath, json);
            }

            _logger.Info($"权限配置已保存: {_configPath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存权限配置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取权限级别
    /// </summary>
    public PermissionLevel GetPermissionLevel(string user)
    {
        lock (_lock)
        {
            if (user == "Console" || user == "CONSOLE")
                return PermissionLevel.Owner;

            if (!_playerGroups.TryGetValue(user, out var groups))
                return PermissionLevel.Guest;

            // 返回最高优先级组的级别
            int maxPriority = 0;
            foreach (var groupName in groups)
            {
                if (_groups.TryGetValue(groupName, out var group))
                {
                    if (group.Priority > maxPriority)
                        maxPriority = group.Priority;
                }
            }

            // 根据优先级映射到权限级别
            return maxPriority switch
            {
                >= 100 => PermissionLevel.Admin,
                >= 50 => PermissionLevel.Moderator,
                >= 10 => PermissionLevel.Member,
                _ => PermissionLevel.Guest
            };
        }
    }

    /// <summary>
    /// 保存权限配置
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            // 构建配置对象
            var config = new PermissionConfig
            {
                Groups = _groups.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new PermissionGroup
                    {
                        Name = kvp.Value.Name,
                        Priority = kvp.Value.Priority,
                        Permissions = kvp.Value.Permissions.ToList(),
                        InheritFrom = kvp.Value.InheritFrom.ToList(),
                        IsDefault = kvp.Value.IsDefault
                    }
                ),
                Players = _playerGroups.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                ),
                PlayerPermissions = _playerPermissions.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                )
            };

            await SaveConfigAsync(config);
        }
        catch (Exception ex)
        {
            _logger.Error($"保存权限配置失败: {ex.Message}", ex);
        }
    }

    // 其他接口方法的空实现（如果 IPermissionManager 有其他方法）
    public void CreateGroup(string groupName) => throw new NotImplementedException();
    public void DeleteGroup(string groupName) => throw new NotImplementedException();
    public void GrantGroupPermission(string groupName, string permission) => throw new NotImplementedException();
    public void RevokeGroupPermission(string groupName, string permission) => throw new NotImplementedException();
    public List<string> GetGroupPermissions(string groupName) => throw new NotImplementedException();
}