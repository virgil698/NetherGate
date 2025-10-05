# NetherGate 新功能使用指南

> 本文档介绍 NetherGate 最新实现的5大功能特性

---

## 📋 新功能概览

本次更新实现了以下功能：

1. ✅ **插件元数据增强** - 更丰富的插件信息和依赖管理
2. ✅ **配置模板自动生成** - 从 C# 类自动生成带注释的配置
3. ✅ **命令系统增强** - 命令别名和自动补全支持（API层完成）
4. ✅ **权限系统增强** - 权限组和继承机制（API层完成）
5. ✅ **崩溃分析系统** - 智能识别崩溃类型并提供建议

---

## 1. 插件元数据增强

### 1.1 扩展的 plugin.json 格式

现在支持更丰富的元数据字段：

```json
{
  "id": "com.example.myplugin",
  "name": "My Awesome Plugin",
  "version": "1.0.0",
  "description": "一个功能强大的插件",
  "author": "Your Name",
  "authors": ["Author1", "Author2"],
  "website": "https://example.com",
  "repository": "https://github.com/yourname/myplugin",
  "license": "MIT",
  "main": "MyPlugin.MyPluginMain",
  
  "category": "utility",
  "tags": ["backup", "management", "automation"],
  
  "min_nethergate_version": "1.0.0",
  "max_nethergate_version": "2.0.0",
  "minecraft_version": "1.21.9",
  
  "plugin_dependencies": [
    {
      "id": "permission-manager",
      "version": ">=1.0.0",
      "optional": false
    },
    {
      "id": "database-helper",
      "version": ">=2.0.0",
      "optional": true
    }
  ],
  
  "conflicts": [
    {
      "id": "old-backup-plugin",
      "reason": "功能冲突，使用相同的命令"
    }
  ],
  
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": ">=13.0.0",
      "location": "lib"
    }
  ]
}
```

### 1.2 新增字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `category` | string | 插件分类（utility, management, fun等） |
| `tags` | string[] | 插件标签，方便搜索 |
| `min_nethergate_version` | string | 最低NetherGate版本要求 |
| `max_nethergate_version` | string | 最高兼容版本 |
| `plugin_dependencies` | array | 插件间依赖（详细格式） |
| `conflicts` | array | 冲突的插件列表 |

### 1.3 插件依赖验证

NetherGate 会自动验证：

- ✅ 必需依赖是否存在
- ✅ 依赖版本是否满足要求
- ✅ 是否有冲突插件
- ✅ 是否有循环依赖
- ✅ NetherGate 版本是否兼容

**验证示例输出：**

```
插件依赖验证报告
====================

错误 (2):
  ✗ 插件 'MyPlugin' 依赖的插件未找到: permission-manager
  ✗ 插件冲突: 'MyPlugin' 与 'old-backup-plugin' 冲突 - 功能冲突

警告 (1):
  ⚠ 插件 'MyPlugin' 的可选依赖未找到: database-helper

建议: 安装缺失的插件或移除冲突插件
```

---

## 2. 配置模板自动生成

### 2.1 使用 C# 特性定义配置

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class MyPluginConfig
{
    [Description("是否启用自动备份")]
    [DefaultValue(true)]
    public bool AutoBackup { get; set; } = true;
    
    [Description("备份间隔（分钟）")]
    [DefaultValue(30)]
    [Range(5, 1440)]
    public int BackupInterval { get; set; } = 30;
    
    [Description("最大备份数量")]
    [DefaultValue(10)]
    [Required]
    public int MaxBackups { get; set; } = 10;
    
    [Description("备份目录")]
    [DefaultValue("./backups")]
    public string BackupDirectory { get; set; } = "./backups";
    
    [Description("排除的文件夹")]
    public List<string> ExcludeFolders { get; set; } = new() { "cache", "logs" };
}
```

### 2.2 生成 YAML 配置模板

```csharp
using NetherGate.Core.Plugins;

// 生成配置模板
var yamlTemplate = ConfigSchemaGenerator.GenerateYamlTemplate<MyPluginConfig>();
Console.WriteLine(yamlTemplate);
```

**生成的 YAML：**

```yaml
# MyPluginConfig 配置文件
# 自动生成时间: 2025-10-05 12:00:00

# 是否启用自动备份
# 默认值: true
autoBackup: true

# 备份间隔（分钟）
# 默认值: 30
# 范围: 5 - 1440
backupInterval: 30

# 最大备份数量
# 默认值: 10
# 必填
maxBackups: 10

# 备份目录
# 默认值: ./backups
backupDirectory: ./backups

# 排除的文件夹
excludeFolders:
  - cache
  - logs
```

### 2.3 在插件中使用

```csharp
public class MyPlugin : IPlugin
{
    private MyPluginConfig _config;
    
    public void OnLoad(IPluginContext context)
    {
        // 加载配置
        _config = context.ConfigManager.Load<MyPluginConfig>("my-plugin.yaml");
        
        // 如果配置文件不存在，生成模板
        if (_config == null)
        {
            var template = ConfigSchemaGenerator.GenerateYamlTemplate<MyPluginConfig>();
            File.WriteAllText("config/my-plugin.yaml", template);
            _config = new MyPluginConfig();
        }
    }
}
```

---

## 3. 命令系统增强（API 层）

### 3.1 命令别名

```csharp
// API 已扩展，待在 CommandManager 中实现
public interface ICommandBuilder
{
    ICommandBuilder WithAlias(params string[] aliases);
    ICommandBuilder WithParameter(string name, ParameterType type, 
        bool required = true,
        Func<string, IEnumerable<string>>? completer = null);
}

// 使用示例（待实现）
commandManager.RegisterCommand(
    CommandBuilder.Create("backup")
        .WithAlias("bak", "save", "保存")  // 多个别名
        .WithDescription("创建服务器备份")
        .WithParameter("name", ParameterType.String, false,
            completer: _ => backupManager.ListBackups())  // Tab 补全
        .Build()
);
```

### 3.2 自动补全

```csharp
// 参数自动补全（待实现）
.WithParameter("player", ParameterType.String, true,
    completer: input => server.GetOnlinePlayers()
        .Where(p => p.StartsWith(input))
        .ToList()
)
```

---

## 4. 权限系统增强（API 层）

### 4.1 权限组配置

```yaml
# config/permissions.yaml
groups:
  guest:
    priority: 0
    permissions:
      - "nethergate.help"
      - "nethergate.list"
  
  member:
    priority: 10
    inherit_from: ["guest"]
    permissions:
      - "nethergate.tpa"
      - "nethergate.home"
      - "nethergate.back"
  
  moderator:
    priority: 50
    inherit_from: ["member"]
    permissions:
      - "nethergate.kick"
      - "nethergate.mute"
      - "nethergate.warn"
  
  admin:
    priority: 100
    inherit_from: ["moderator"]
    permissions:
      - "nethergate.*"

players:
  "player1": ["member"]
  "player2": ["moderator"]
  "admin_user": ["admin"]
```

### 4.2 权限检查（API 已定义）

```csharp
// 检查权限
if (permissionManager.HasPermission(playerName, "nethergate.backup.create"))
{
    // 允许操作
}

// 获取权限级别
var level = permissionManager.GetPermissionLevel(playerName);
if (level >= PermissionLevel.Moderator)
{
    // 管理员操作
}
```

---

## 5. 崩溃分析系统

### 5.1 自动崩溃检测

NetherGate 会自动检测以下崩溃类型：

| 崩溃类型 | 检测关键词 | 解决建议 |
|---------|-----------|---------|
| `OutOfMemory` | OutOfMemoryError | 增加内存配置 |
| `PortInUse` | BindException | 更改端口或关闭占用程序 |
| `ModConflict` | ClassNotFoundException | 检查模组兼容性 |
| `CorruptedWorld` | Corrupt chunk | 从备份恢复 |
| `NetworkError` | Connection refused | 检查网络设置 |
| `PermissionDenied` | Permission denied | 以管理员运行 |
| `MissingDependency` | Could not find main class | 安装依赖 |

### 5.2 崩溃报告示例

```
====================================
       NetherGate 崩溃报告
====================================

时间: 2025-10-05 12:30:45
崩溃类型: OutOfMemory

解决建议:
------------------------------------
内存不足。建议：
1. 增加服务器内存配置（-Xmx 参数）
2. 减少加载的区块数量
3. 优化模组配置
4. 检查是否有内存泄漏

相关日志行:
------------------------------------
[ERROR] java.lang.OutOfMemoryError: Java heap space
[ERROR] at java.base/java.util.Arrays.copyOf
[ERROR] at net.minecraft.server.world.ChunkHolder

完整日志:
====================================
[详细日志内容...]
```

### 5.3 崩溃报告位置

崩溃报告自动保存到：`logs/crashes/crash-YYYYMMDD-HHmmss.txt`

### 5.4 查看崩溃历史

```bash
# 查看最近的崩溃报告
ls logs/crashes/

# 输出示例：
crash-20251005-123045.txt
crash-20251004-180230.txt
crash-20251003-093012.txt
```

---

## 🎯 实际使用场景

### 场景1：开发新插件

```csharp
// 1. 定义配置类
public class MyConfig
{
    [Description("功能开关")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}

// 2. 在 OnLoad 中生成配置模板
public void OnLoad(IPluginContext context)
{
    var configPath = "config/my-plugin.yaml";
    
    if (!File.Exists(configPath))
    {
        var template = ConfigSchemaGenerator.GenerateYamlTemplate<MyConfig>();
        await File.WriteAllTextAsync(configPath, template);
    }
    
    _config = context.ConfigManager.Load<MyConfig>(configPath);
}
```

### 场景2：声明插件依赖

```json
{
  "id": "advanced-backup",
  "plugin_dependencies": [
    {
      "id": "permission-manager",
      "version": ">=1.0.0",
      "optional": false
    }
  ]
}
```

NetherGate 会自动：
- 检查 `permission-manager` 是否安装
- 验证版本是否满足 `>=1.0.0`
- 在加载顺序中确保先加载依赖插件

### 场景3：处理服务器崩溃

当服务器崩溃时，NetherGate 自动：

1. ✅ 捕获崩溃日志
2. ✅ 分析崩溃原因
3. ✅ 生成详细报告
4. ✅ 提供解决建议
5. ✅ 保存到 `logs/crashes/`

管理员只需：
```bash
# 查看最新崩溃报告
cat logs/crashes/crash-20251005-123045.txt

# 根据建议修复问题
```

---

## 📊 功能完成度

| 功能 | 完成度 | 说明 |
|------|--------|------|
| 插件元数据增强 | ✅ 100% | 已完成并测试 |
| 配置模板生成 | ✅ 100% | 已完成并测试 |
| 命令系统增强 | ⚠️ 50% | API 完成，待实现 |
| 权限系统增强 | ⚠️ 50% | API 完成，待实现 |
| 崩溃分析系统 | ✅ 100% | 已完成并测试 |

---

## 🚀 下一步计划

1. **命令系统实现** - 完善 CommandManager 的别名和补全功能
2. **权限系统实现** - 实现权限组加载和继承逻辑
3. **插件仓库** - 构建插件市场系统
4. **Web 管理面板** - 提供图形化管理界面

---

## 📚 相关文档

- [插件依赖管理](PLUGIN_DEPENDENCIES.md)
- [自动依赖管理](AUTO_DEPENDENCY_MANAGEMENT.md)
- [MCDR 功能对比](MCDR_COMPARISON.md)
- [功能路线图](ROADMAP_MCDR_INSPIRED.md)
