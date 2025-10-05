# NetherGate 命令系统文档

本文档详细说明 NetherGate 的命令系统，包括命令注册、权限管理和 Tab 补全。

---

## 📋 目录

- [概述](#概述)
- [命令接口](#命令接口)
- [命令注册](#命令注册)
- [权限系统](#权限系统)
- [Tab 补全](#tab-补全)
- [内置命令](#内置命令)
- [插件命令开发](#插件命令开发)
- [最佳实践](#最佳实践)

---

## 概述

NetherGate 提供了一套完整的命令系统，支持：

- ✅ **命令注册与管理** - 动态注册/注销命令
- ✅ **权限系统** - 基于权限节点的访问控制
- ✅ **Tab 补全** - 智能命令和参数补全
- ✅ **命令别名** - 支持多个命令别名
- ✅ **参数解析** - 支持引号字符串参数
- ✅ **异步执行** - 基于 async/await 的命令执行

---

## 命令接口

### ICommand 接口

所有命令都必须实现 `ICommand` 接口：

```csharp
public interface ICommand
{
    /// <summary>命令名称</summary>
    string Name { get; }

    /// <summary>命令描述</summary>
    string Description { get; }

    /// <summary>命令用法</summary>
    string Usage { get; }

    /// <summary>命令别名</summary>
    List<string> Aliases { get; }

    /// <summary>所属插件 ID</summary>
    string PluginId { get; }

    /// <summary>所需权限节点（null 表示无需权限）</summary>
    string? Permission { get; }

    /// <summary>执行命令</summary>
    Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args);

    /// <summary>Tab 补全（可选实现）</summary>
    Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args);
}
```

### ICommandSender 接口

命令发送者接口：

```csharp
public interface ICommandSender
{
    /// <summary>发送者名称</summary>
    string Name { get; }

    /// <summary>是否是控制台</summary>
    bool IsConsole { get; }

    /// <summary>发送消息给命令发送者</summary>
    void SendMessage(string message);

    /// <summary>检查权限</summary>
    bool HasPermission(string permission);
}
```

### CommandResult 记录

命令执行结果：

```csharp
public record CommandResult
{
    public bool Success { get; init; }
    public string Message { get; init; }

    public static CommandResult Ok(string message);
    public static CommandResult Fail(string message);
}
```

---

## 命令注册

### 注册命令

在插件的 `OnEnableAsync()` 方法中注册命令：

```csharp
public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        // 注册命令
        var myCommand = new MyCommand();
        Context.Commands.RegisterCommand(myCommand);

        Logger.Info("命令已注册");
    }
}
```

### 注销命令

在插件禁用时自动注销，或手动注销：

```csharp
// 注销特定命令
Context.Commands.UnregisterCommand("mycommand");

// 注销插件的所有命令（插件禁用时自动调用）
Context.Commands.UnregisterPluginCommands("my-plugin-id");
```

---

## 权限系统

### 权限节点

权限节点采用点号分隔的层级结构：

```
nethergate.admin.stop          - 停止 NetherGate
nethergate.admin.permission    - 权限管理
nethergate.plugins.list        - 查看插件列表
nethergate.status              - 查看服务器状态

myplugin.command1              - 插件自定义权限
myplugin.admin.*               - 插件所有管理权限（通配符）
*                              - 所有权限（超级管理员）
```

### 权限通配符

- `*` - 匹配所有权限
- `nethergate.*` - 匹配所有 `nethergate` 开头的权限
- `-permission` - 否定权限（明确拒绝）

### 权限检查

命令系统会自动检查权限，插件代码中也可以手动检查：

```csharp
if (!sender.HasPermission("myplugin.admin"))
{
    sender.SendMessage("权限不足");
    return CommandResult.Fail("权限不足");
}
```

### 权限管理命令

#### 用户权限管理

```bash
# 授予用户权限
permission user add <用户> <权限>

# 撤销用户权限
permission user remove <用户> <权限>

# 查看用户权限
permission user list <用户>

# 查看用户详细信息
permission user info <用户>
```

#### 权限组管理

```bash
# 创建权限组
permission group create <组名>

# 删除权限组
permission group delete <组名>

# 授予组权限
permission group add <组名> <权限>

# 撤销组权限
permission group remove <组名> <权限>

# 查看组权限
permission group list <组名>

# 将用户添加到组
permission group adduser <用户> <组名>

# 将用户从组移除
permission group removeuser <用户> <组名>
```

### 默认权限组

系统自动创建两个默认权限组：

- **default** - 默认组，无权限
- **admin** - 管理员组，拥有所有权限（`*`）

### 权限配置文件

权限配置保存在 `config/permissions.json`：

```json
{
  "Groups": [
    {
      "Name": "admin",
      "Permissions": ["*"],
      "InheritedGroups": []
    },
    {
      "Name": "moderator",
      "Permissions": [
        "nethergate.plugins.list",
        "nethergate.status"
      ],
      "InheritedGroups": ["default"]
    }
  ],
  "Users": [
    {
      "User": "Steve",
      "Permissions": ["myplugin.special"],
      "Groups": ["moderator"]
    }
  ]
}
```

---

## Tab 补全

### 实现 Tab 补全

命令可以实现 `TabCompleteAsync` 方法提供智能补全：

```csharp
public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
{
    if (args.Length == 1)
    {
        // 补全第一个参数
        return new List<string> { "add", "remove", "list" }
            .Where(x => x.StartsWith(args[0], StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    if (args.Length == 2 && args[0] == "add")
    {
        // 补全第二个参数（例如玩家名）
        var players = await Context.Server.GetPlayersAsync();
        return players.Select(p => p.Name).ToList();
    }

    return new List<string>();
}
```

### Tab 补全示例

**命令名补全**：
```
输入: hel<TAB>
输出: help
```

**参数补全**：
```
输入: permission user a<TAB>
输出: add

输入: permission user add Steve <TAB>
输出: nethergate.admin.stop  nethergate.plugins.list  ...
```

**别名补全**：
```
输入: pl<TAB>
输出: plugins
```

---

## 内置命令

| 命令 | 别名 | 描述 | 权限 |
|------|------|------|------|
| `help` | `?` | 显示所有可用命令 | 无 |
| `version` | `ver` | 显示版本信息 | 无 |
| `plugins` | `pl` | 显示已加载的插件 | `nethergate.plugins.list` |
| `status` | `stat` | 显示服务器状态 | `nethergate.status` |
| `stop` | `exit`, `quit` | 停止 NetherGate | `nethergate.admin.stop` |
| `permission` | `perm`, `perms` | 权限管理 | `nethergate.admin.permission` |

### 命令示例

```bash
# 查看帮助
help

# 查看特定命令的帮助
help plugins

# 查看版本
version

# 查看插件列表
plugins

# 查看服务器状态
status

# 停止 NetherGate
stop
```

---

## 插件命令开发

### 简单命令示例

```csharp
public class HelloCommand : ICommand
{
    public string Name => "hello";
    public string Description => "向玩家问好";
    public string Usage => "hello <玩家名>";
    public List<string> Aliases => new() { "hi" };
    public string PluginId => "my-plugin";
    public string? Permission => "myplugin.hello";

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Fail("用法: hello <玩家名>");
        }

        var playerName = args[0];
        sender.SendMessage($"Hello, {playerName}!");

        return CommandResult.Ok($"已向 {playerName} 问好");
    }

    // 可选：实现 Tab 补全
    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            // 补全在线玩家名
            var context = /* 获取插件上下文 */;
            var players = await context.Server.GetPlayersAsync();
            return players.Select(p => p.Name)
                .Where(name => name.StartsWith(args[0], StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new List<string>();
    }
}
```

### 复杂命令示例（子命令）

```csharp
public class AdminCommand : ICommand
{
    private readonly IPluginContext _context;

    public string Name => "admin";
    public string Description => "管理员命令";
    public string Usage => "admin <kick|ban|mute> <参数...>";
    public List<string> Aliases => new() { "adm" };
    public string PluginId => "my-plugin";
    public string? Permission => "myplugin.admin";

    public AdminCommand(IPluginContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Fail(Usage);
        }

        var subCommand = args[0].ToLower();
        var subArgs = args.Skip(1).ToArray();

        switch (subCommand)
        {
            case "kick":
                return await HandleKick(sender, subArgs);
            
            case "ban":
                return await HandleBan(sender, subArgs);
            
            case "mute":
                return await HandleMute(sender, subArgs);
            
            default:
                return CommandResult.Fail($"未知子命令: {subCommand}");
        }
    }

    private async Task<CommandResult> HandleKick(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            return CommandResult.Fail("用法: admin kick <玩家> [原因]");
        }

        var playerName = args[0];
        var reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "被管理员踢出";

        // 检查子权限
        if (!sender.HasPermission("myplugin.admin.kick"))
        {
            return CommandResult.Fail("权限不足: 需要 myplugin.admin.kick");
        }

        await _context.Server.KickPlayerAsync(playerName, reason);
        return CommandResult.Ok($"已踢出玩家 {playerName}");
    }

    private async Task<CommandResult> HandleBan(ICommandSender sender, string[] args)
    {
        // 类似实现...
        return await Task.FromResult(CommandResult.Ok("Ban 功能待实现"));
    }

    private async Task<CommandResult> HandleMute(ICommandSender sender, string[] args)
    {
        // 类似实现...
        return await Task.FromResult(CommandResult.Ok("Mute 功能待实现"));
    }

    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            // 补全子命令
            return new List<string> { "kick", "ban", "mute" }
                .Where(x => x.StartsWith(args[0], StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (args.Length == 2)
        {
            // 补全玩家名
            var players = await _context.Server.GetPlayersAsync();
            return players.Select(p => p.Name)
                .Where(name => name.StartsWith(args[1], StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new List<string>();
    }
}
```

---

## 最佳实践

### 1. 权限设计

- 使用清晰的权限节点命名：`<插件名>.<功能>.<操作>`
- 为不同级别的功能设置不同权限
- 使用通配符方便管理（如 `myplugin.admin.*`）

```csharp
// 良好的权限设计
"myplugin.user.info"        - 查看信息（普通用户）
"myplugin.admin.kick"       - 踢出玩家（管理员）
"myplugin.admin.ban"        - 封禁玩家（管理员）
"myplugin.admin.*"          - 所有管理功能
```

### 2. 参数验证

始终验证命令参数：

```csharp
public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
{
    // 检查参数数量
    if (args.Length < 2)
    {
        return CommandResult.Fail($"用法: {Usage}");
    }

    // 验证参数格式
    if (!int.TryParse(args[1], out var amount) || amount <= 0)
    {
        return CommandResult.Fail("数量必须是正整数");
    }

    // 执行命令逻辑...
}
```

### 3. 用户反馈

提供清晰的反馈信息：

```csharp
// ✅ 好的反馈
return CommandResult.Ok($"已给予玩家 {playerName} {amount} 个 {itemName}");

// ❌ 不好的反馈
return CommandResult.Ok("OK");
```

### 4. 错误处理

妥善处理异常：

```csharp
try
{
    await _context.Server.SomeOperationAsync();
    return CommandResult.Ok("操作成功");
}
catch (Exception ex)
{
    _context.Logger.Error($"命令执行失败: {ex.Message}", ex);
    return CommandResult.Fail($"操作失败: {ex.Message}");
}
```

### 5. Tab 补全

提供有用的 Tab 补全：

```csharp
// 补全常用值
public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
{
    if (args.Length == 1)
    {
        return new List<string> { "true", "false" };
    }

    if (args.Length == 2)
    {
        return new List<string> { "all", "world", "nether", "end" };
    }

    return new List<string>();
}
```

---

## 权限节点参考

### NetherGate 核心权限

| 权限节点 | 描述 | 默认 |
|---------|------|------|
| `nethergate.admin.*` | 所有管理权限 | OP |
| `nethergate.admin.stop` | 停止 NetherGate | OP |
| `nethergate.admin.permission` | 权限管理 | OP |
| `nethergate.plugins.list` | 查看插件列表 | 所有人 |
| `nethergate.status` | 查看服务器状态 | 所有人 |

### 插件权限建议

```
<插件名>.user.*           - 所有用户功能
<插件名>.admin.*          - 所有管理功能
<插件名>.command.<命令名> - 特定命令权限
```

---

## 常见问题

### Q: 如何让命令无需权限？

A: 将 `Permission` 属性设置为 `null`：

```csharp
public string? Permission => null;
```

### Q: 如何实现子命令？

A: 在 `ExecuteAsync` 中解析第一个参数作为子命令：

```csharp
var subCommand = args[0].ToLower();
switch (subCommand)
{
    case "add": return await HandleAdd(args.Skip(1).ToArray());
    case "remove": return await HandleRemove(args.Skip(1).ToArray());
}
```

### Q: Tab 补全不工作？

A: 确保实现了 `TabCompleteAsync` 方法并返回有效的补全列表。

### Q: 如何检查玩家是否在线？

A: 使用 SMP API：

```csharp
var players = await Context.Server.GetPlayersAsync();
var player = players.FirstOrDefault(p => p.Name == playerName);
if (player == null)
{
    return CommandResult.Fail($"玩家 {playerName} 不在线");
}
```

---

## 示例项目

完整的命令系统示例插件请参考：[NetherGate-Samples](https://github.com/BlockBridge/NetherGate-Samples)

---

**文档版本**: 1.0.0  
**最后更新**: 2025-10-04  
**贡献者**: NetherGate Team

