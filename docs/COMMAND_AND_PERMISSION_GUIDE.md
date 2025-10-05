# NetherGate 命令系统和权限系统完整指南

> 本文档介绍 NetherGate 增强的命令系统和权限系统

---

## 📋 目录

- [命令系统增强](#命令系统增强)
  - [命令别名](#命令别名)
  - [Tab 自动补全](#tab-自动补全)
  - [使用示例](#命令系统使用示例)
- [权限系统增强](#权限系统增强)
  - [权限组](#权限组)
  - [权限继承](#权限继承)
  - [配置文件](#权限配置文件)
  - [使用示例](#权限系统使用示例)

---

## 命令系统增强

### 命令别名

NetherGate 支持为每个命令注册多个别名，让用户可以使用不同的命令名执行同一功能。

#### 定义命令别名

```csharp
public class BackupCommand : ICommand
{
    public string Name => "backup";
    public string Description => "创建服务器备份";
    public string Usage => "backup [name]";
    
    // 定义别名
    public List<string> Aliases => new() { "bak", "save", "保存" };
    
    public string PluginId => "my-plugin";
    public string? Permission => "nethergate.backup.create";
    
    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        var name = args.Length > 0 ? args[0] : DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // 执行备份逻辑...
        return CommandResult.Ok($"备份 '{name}' 创建成功");
    }
}
```

#### 注册命令

```csharp
public class MyPlugin : IPlugin
{
    public async Task OnEnableAsync()
    {
        // 注册命令（自动注册所有别名）
        Context.CommandManager.RegisterCommand(new BackupCommand());
    }
}
```

#### 使用命令

用户可以使用以下任何方式执行命令：

```bash
backup myworld      # 主命令
bak myworld         # 别名 1
save myworld        # 别名 2
保存 myworld        # 中文别名
```

---

### Tab 自动补全

NetherGate 支持命令和参数的 Tab 自动补全功能。

#### 实现 Tab 补全

```csharp
public class TeleportCommand : ICommand
{
    private readonly IServerMonitor _serverMonitor;
    
    public string Name => "tp";
    public List<string> Aliases => new() { "teleport", "传送" };
    
    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        // 执行传送逻辑...
        return CommandResult.Ok($"已传送到 {args[0]}");
    }
    
    // Tab 补全实现
    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            // 第一个参数：补全玩家名称
            var onlinePlayers = await _serverMonitor.GetOnlinePlayersAsync();
            var prefix = args[0].ToLower();
            
            return onlinePlayers
                .Where(p => p.ToLower().StartsWith(prefix))
                .ToList();
        }
        
        return new List<string>();
    }
}
```

#### 自动补全场景

1. **命令名补全**
   ```
   输入: bac<Tab>
   补全: backup
   ```

2. **别名补全**
   ```
   输入: ba<Tab>
   补全: backup, bak
   ```

3. **参数补全**
   ```
   输入: tp Pla<Tab>
   补全: Player1, Player2, Player123
   ```

---

### 命令系统使用示例

#### 示例1：带补全的备份命令

```csharp
public class BackupCommand : ICommand
{
    private readonly IBackupManager _backupManager;
    
    public string Name => "backup";
    public List<string> Aliases => new() { "bak", "save" };
    public string Usage => "backup [restore|list|create] [name]";
    
    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 0)
        {
            // 创建备份
            var name = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            await _backupManager.CreateBackupAsync(name);
            return CommandResult.Ok($"✓ 备份已创建: {name}");
        }
        
        var action = args[0].ToLower();
        
        switch (action)
        {
            case "list":
                var backups = _backupManager.ListBackups();
                return CommandResult.Ok($"可用备份:\n{string.Join("\n", backups)}");
                
            case "restore":
                if (args.Length < 2)
                    return CommandResult.Fail("用法: backup restore <name>");
                    
                await _backupManager.RestoreBackupAsync(args[1]);
                return CommandResult.Ok($"✓ 备份已恢复: {args[1]}");
                
            case "create":
                var backupName = args.Length > 1 ? args[1] : DateTime.Now.ToString("yyyyMMdd_HHmmss");
                await _backupManager.CreateBackupAsync(backupName);
                return CommandResult.Ok($"✓ 备份已创建: {backupName}");
                
            default:
                return CommandResult.Fail($"未知操作: {action}");
        }
    }
    
    // Tab 补全
    public async Task<List<string>> TabCompleteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length == 1)
        {
            // 补全子命令
            var actions = new[] { "create", "restore", "list" };
            return actions.Where(a => a.StartsWith(args[0].ToLower())).ToList();
        }
        
        if (args.Length == 2 && args[0].ToLower() == "restore")
        {
            // 补全备份名称
            var backups = _backupManager.ListBackups();
            return backups.Where(b => b.StartsWith(args[1])).ToList();
        }
        
        return new List<string>();
    }
}
```

使用：
```bash
backup list                    # 列出所有备份
backup create myworld          # 创建名为 myworld 的备份
backup restore <Tab>           # Tab 补全显示可用备份
bak <Tab>                      # 显示 create, restore, list
```

---

## 权限系统增强

### 权限组

NetherGate 支持基于组的权限管理，每个组有自己的优先级和权限列表。

#### 权限组结构

```yaml
groups:
  guest:
    priority: 0              # 优先级（数字越大权限越高）
    permissions:
      - "nethergate.help"
      - "nethergate.list"
    default: true            # 默认组

  member:
    priority: 10
    inherit_from:            # 继承其他组
      - "guest"
    permissions:
      - "nethergate.tpa"
      - "nethergate.home"

  moderator:
    priority: 50
    inherit_from:
      - "member"
    permissions:
      - "nethergate.kick"
      - "nethergate.mute"

  admin:
    priority: 100
    inherit_from:
      - "moderator"
    permissions:
      - "nethergate.*"      # 通配符：所有权限
```

### 权限继承

权限组支持多级继承，子组自动获得父组的所有权限。

#### 继承示例

```yaml
groups:
  guest:
    priority: 0
    permissions:
      - "nethergate.help"

  member:
    priority: 10
    inherit_from: ["guest"]  # 继承 guest 的权限
    permissions:
      - "nethergate.tpa"

  vip:
    priority: 20
    inherit_from: ["member"]  # 继承 member 的所有权限（包括 guest 的）
    permissions:
      - "nethergate.fly"
```

**实际权限效果：**
- `guest`: `nethergate.help`
- `member`: `nethergate.help`, `nethergate.tpa`
- `vip`: `nethergate.help`, `nethergate.tpa`, `nethergate.fly`

---

### 权限配置文件

#### 完整配置示例（permissions.yaml）

```yaml
# NetherGate 权限配置文件
# 支持权限组、权限继承、玩家特定权限

groups:
  # 访客组（默认）
  guest:
    priority: 0
    permissions:
      - "nethergate.help"
      - "nethergate.list"
      - "nethergate.info"
    default: true

  # 普通成员
  member:
    priority: 10
    inherit_from:
      - "guest"
    permissions:
      - "nethergate.tpa"
      - "nethergate.tpahere"
      - "nethergate.home"
      - "nethergate.sethome"
      - "nethergate.back"

  # VIP 会员
  vip:
    priority: 20
    inherit_from:
      - "member"
    permissions:
      - "nethergate.fly"
      - "nethergate.speed"
      - "nethergate.nickname"

  # 管理员
  moderator:
    priority: 50
    inherit_from:
      - "vip"
    permissions:
      - "nethergate.kick"
      - "nethergate.mute"
      - "nethergate.warn"
      - "nethergate.tempban"
      - "nethergate.spectate"

  # 超级管理员
  admin:
    priority: 100
    inherit_from:
      - "moderator"
    permissions:
      - "nethergate.*"  # 所有权限

# 玩家-组映射
players:
  "player1": ["member"]
  "player2": ["vip"]
  "moderator_user": ["moderator"]
  "admin_user": ["admin"]

# 玩家特定权限（覆盖组权限）
player_permissions:
  "special_player":
    - "nethergate.special.feature"
    - "-nethergate.fly"  # 否定权限（移除飞行权限）
```

---

### 权限系统使用示例

#### 示例1：在命令中检查权限

```csharp
public class KickCommand : ICommand
{
    public string Name => "kick";
    public string Permission => "nethergate.kick";  // 需要的权限
    
    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        // 权限自动由 CommandManager 检查
        // 如果没有权限，命令不会执行到这里
        
        if (args.Length == 0)
            return CommandResult.Fail("用法: kick <player> [reason]");
        
        var player = args[0];
        var reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "未指定原因";
        
        // 执行踢出逻辑...
        return CommandResult.Ok($"已踢出玩家 {player}，原因: {reason}");
    }
}
```

#### 示例2：手动检查权限

```csharp
public class MyPlugin : IPlugin
{
    private IPermissionManager _permissionManager;
    
    public async Task OnEnableAsync()
    {
        _permissionManager = /* 获取权限管理器 */;
        
        // 检查玩家是否有权限
        if (_permissionManager.HasPermission("player1", "nethergate.fly"))
        {
            Logger.Info("player1 有飞行权限");
        }
        
        // 获取权限级别
        var level = _permissionManager.GetPermissionLevel("player1");
        if (level >= PermissionLevel.Moderator)
        {
            Logger.Info("player1 是管理员");
        }
    }
}
```

#### 示例3：运行时修改权限

```csharp
// 将玩家添加到组
_permissionManager.AddUserToGroup("player1", "vip");

// 授予玩家特定权限
_permissionManager.GrantPermission("player1", "nethergate.special");

// 撤销权限
_permissionManager.RevokePermission("player1", "nethergate.fly");

// 保存配置
await _permissionManager.SaveAsync();
```

---

## 权限节点规范

### 权限节点命名

推荐使用分层命名：

```
nethergate.category.action
nethergate.plugin.feature.action
```

### 示例权限节点

```
nethergate.help                    # 查看帮助
nethergate.plugins.list            # 列出插件
nethergate.backup.create           # 创建备份
nethergate.backup.restore          # 恢复备份
nethergate.backup.*                # 所有备份权限
nethergate.*                       # 所有 NetherGate 权限
```

### 通配符权限

- `*` - 所有权限
- `nethergate.*` - 所有 nethergate 开头的权限
- `nethergate.backup.*` - 所有 backup 子权限

### 否定权限

使用 `-` 前缀移除权限：

```yaml
player_permissions:
  "player1":
    - "nethergate.*"        # 所有权限
    - "-nethergate.admin.*" # 但不包括 admin 权限
```

---

## 🎯 实际使用场景

### 场景1：设置服务器管理员

```yaml
players:
  "admin_user": ["admin"]
```

管理员自动获得所有权限（`nethergate.*`）

### 场景2：创建 VIP 系统

```yaml
groups:
  vip:
    priority: 20
    inherit_from: ["member"]
    permissions:
      - "nethergate.fly"
      - "nethergate.speed"
      - "nethergate.nickname"

players:
  "vip_player1": ["vip"]
  "vip_player2": ["vip"]
```

### 场景3：临时授权

```csharp
// 临时给玩家管理权限（重启后失效）
_permissionManager.AddUserToGroup("player1", "moderator");

// 保存到配置文件（永久生效）
await _permissionManager.SaveAsync();
```

---

## 📊 功能对比

| 功能 | 基础版本 | 增强版本 |
|------|---------|---------|
| 命令别名 | ❌ | ✅ |
| Tab 补全 | ❌ | ✅ |
| 权限组 | ⚠️ 基础 | ✅ 完整 |
| 权限继承 | ❌ | ✅ |
| 优先级系统 | ❌ | ✅ |
| 通配符权限 | ✅ | ✅ |
| 否定权限 | ❌ | ✅ |
| YAML 配置 | ❌ | ✅ |
| 运行时修改 | ⚠️ 部分 | ✅ 完整 |

---

## 📚 相关文档

- [NEW_FEATURES_GUIDE.md](NEW_FEATURES_GUIDE.md) - 新功能完整指南
- [PLUGIN_PROJECT_STRUCTURE.md](PLUGIN_PROJECT_STRUCTURE.md) - 插件开发指南
- [API_DESIGN.md](API_DESIGN.md) - API 设计文档
- [COMMAND_SYSTEM.md](COMMAND_SYSTEM.md) - 命令系统详细文档

---

## ✅ 快速总结

**命令系统增强：**
- ✅ 命令别名支持
- ✅ Tab 自动补全
- ✅ 更好的帮助系统

**权限系统增强：**
- ✅ 权限组系统
- ✅ 权限继承
- ✅ 优先级管理
- ✅ YAML 配置
- ✅ 通配符和否定权限

**立即可用！** 所有功能已编译通过并可以使用。
