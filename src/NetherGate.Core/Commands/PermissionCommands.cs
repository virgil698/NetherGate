using NetherGate.API.Permissions;
using NetherGate.API.Plugins;

namespace NetherGate.Core.Commands;

/// <summary>
/// Permission 命令 - 权限管理
/// </summary>
public class PermissionCommand : ICommand
{
    private readonly IPermissionManager _permissionManager;

    public string Name => "permission";
    public string Description => "权限管理命令";
    public string Usage => "permission <user|group> <add|remove|list> [参数...]";
    public List<string> Aliases => new() { "perm", "perms" };
    public string PluginId => "nethergate";
    public string? Permission => "nethergate.admin.permission";

    public PermissionCommand(IPermissionManager permissionManager)
    {
        _permissionManager = permissionManager;
    }

    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            return new List<string> { "user", "group" }
                .Where(x => x.StartsWith(args[0], StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (args.Length == 2 && (args[0] == "user" || args[0] == "group"))
        {
            return new List<string> { "add", "remove", "list", "info" }
                .Where(x => x.StartsWith(args[1], StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return await Task.FromResult(new List<string>());
    }

    public Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length < 2)
        {
            return Task.FromResult(CommandResult.Fail(
                "用法: permission <user|group> <add|remove|list> [参数...]\n" +
                "示例:\n" +
                "  permission user add <用户> <权限>\n" +
                "  permission user remove <用户> <权限>\n" +
                "  permission user list <用户>\n" +
                "  permission user info <用户>\n" +
                "  permission group create <组名>\n" +
                "  permission group delete <组名>\n" +
                "  permission group add <组名> <权限>\n" +
                "  permission group remove <组名> <权限>\n" +
                "  permission group list [组名]\n" +
                "  permission group adduser <用户> <组名>\n" +
                "  permission group removeuser <用户> <组名>"
            ));
        }

        var type = args[0].ToLower();
        var action = args[1].ToLower();

        try
        {
            if (type == "user")
            {
                return Task.FromResult(HandleUserCommand(action, args.Skip(2).ToArray()));
            }
            else if (type == "group")
            {
                return Task.FromResult(HandleGroupCommand(action, args.Skip(2).ToArray()));
            }
            else
            {
                return Task.FromResult(CommandResult.Fail($"未知类型: {type}，应为 user 或 group"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandResult.Fail($"执行失败: {ex.Message}"));
        }
    }

    private CommandResult HandleUserCommand(string action, string[] args)
    {
        switch (action)
        {
            case "add":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: permission user add <用户> <权限>");
                
                _permissionManager.GrantPermission(args[0], args[1]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已授予用户 '{args[0]}' 权限: {args[1]}");

            case "remove":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: permission user remove <用户> <权限>");
                
                _permissionManager.RevokePermission(args[0], args[1]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已撤销用户 '{args[0]}' 权限: {args[1]}");

            case "list":
                if (args.Length < 1)
                    return CommandResult.Fail("用法: permission user list <用户>");
                
                var permissions = _permissionManager.GetPermissions(args[0]);
                if (permissions.Count == 0)
                    return CommandResult.Ok($"用户 '{args[0]}' 没有任何权限");
                
                return CommandResult.Ok($"用户 '{args[0]}' 的权限:\n  " + string.Join("\n  ", permissions));

            case "info":
                if (args.Length < 1)
                    return CommandResult.Fail("用法: permission user info <用户>");
                
                var userGroups = _permissionManager.GetUserGroups(args[0]);
                var userPerms = _permissionManager.GetPermissions(args[0]);
                
                var message = $"用户 '{args[0]}' 信息:\n";
                message += $"所属组: {(userGroups.Count > 0 ? string.Join(", ", userGroups) : "无")}\n";
                message += $"权限数量: {userPerms.Count}\n";
                if (userPerms.Count > 0)
                {
                    message += "权限列表:\n  " + string.Join("\n  ", userPerms);
                }
                
                return CommandResult.Ok(message);

            default:
                return CommandResult.Fail($"未知操作: {action}");
        }
    }

    private CommandResult HandleGroupCommand(string action, string[] args)
    {
        switch (action)
        {
            case "create":
                if (args.Length < 1)
                    return CommandResult.Fail("用法: permission group create <组名>");
                
                _permissionManager.CreateGroup(args[0]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已创建权限组: {args[0]}");

            case "delete":
                if (args.Length < 1)
                    return CommandResult.Fail("用法: permission group delete <组名>");
                
                _permissionManager.DeleteGroup(args[0]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已删除权限组: {args[0]}");

            case "add":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: permission group add <组名> <权限>");
                
                _permissionManager.GrantGroupPermission(args[0], args[1]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已授予组 '{args[0]}' 权限: {args[1]}");

            case "remove":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: permission group remove <组名> <权限>");
                
                _permissionManager.RevokeGroupPermission(args[0], args[1]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已撤销组 '{args[0]}' 权限: {args[1]}");

            case "list":
                if (args.Length == 0)
                {
                    // 列出所有组（需要额外接口，这里简化处理）
                    return CommandResult.Ok("列出所有组功能待实现");
                }
                
                var groupPerms = _permissionManager.GetGroupPermissions(args[0]);
                if (groupPerms.Count == 0)
                    return CommandResult.Ok($"权限组 '{args[0]}' 没有任何权限");
                
                return CommandResult.Ok($"权限组 '{args[0]}' 的权限:\n  " + string.Join("\n  ", groupPerms));

            case "adduser":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: permission group adduser <用户> <组名>");
                
                _permissionManager.AddUserToGroup(args[0], args[1]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已将用户 '{args[0]}' 添加到组: {args[1]}");

            case "removeuser":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: permission group removeuser <用户> <组名>");
                
                _permissionManager.RemoveUserFromGroup(args[0], args[1]);
                _ = _permissionManager.SaveAsync();
                return CommandResult.Ok($"已将用户 '{args[0]}' 从组移除: {args[1]}");

            default:
                return CommandResult.Fail($"未知操作: {action}");
        }
    }
}

