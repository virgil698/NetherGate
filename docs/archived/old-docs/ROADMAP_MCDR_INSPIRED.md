# NetherGate 功能路线图（MCDR 启发）

> 基于 MCDReforged 的功能分析，制定的 NetherGate 改进路线图

---

## 🚀 阶段一：核心功能增强（1-2 周）

### 1.1 插件元数据增强 ⭐⭐⭐

**目标**：让 `plugin.json` 更丰富，支持分类、标签、插件间依赖

**任务清单**：
- [ ] 扩展 `PluginMetadata` 类，添加新字段
- [ ] 实现插件间依赖检查（不只是库依赖）
- [ ] 实现冲突插件检测
- [ ] 更新插件加载器以验证元数据
- [ ] 更新文档和示例

**预计时间**：2-3 天

**代码位置**：
- `src/NetherGate.API/Plugins/PluginMetadata.cs`
- `src/NetherGate.Core/Plugins/PluginLoader.cs`

---

### 1.2 命令系统增强 ⭐⭐⭐

**目标**：添加命令别名、自动补全、更好的帮助系统

**任务清单**：
- [ ] 在 `ICommandManager` 中添加别名支持
- [ ] 实现 Tab 自动补全接口
- [ ] 自动生成命令帮助文档
- [ ] 添加命令使用统计

**预计时间**：3-4 天

**代码位置**：
- `src/NetherGate.API/Plugins/ICommandManager.cs`
- `src/NetherGate.Core/Commands/CommandManager.cs`

**示例代码**：
```csharp
// 命令别名支持
commandManager.RegisterCommand(
    CommandBuilder.Create("backup")
        .WithAliases("bak", "save", "保存")
        .WithDescription("创建服务器备份")
        .WithParameter("name", ParameterType.String, 
            completer: _ => backupManager.ListBackups())
        .Build()
);
```

---

### 1.3 配置模板自动生成 ⭐⭐

**目标**：从 C# 类自动生成带注释的 YAML 配置

**任务清单**：
- [ ] 创建 `ConfigSchemaGenerator` 类
- [ ] 支持 Description、DefaultValue 等特性
- [ ] 自动生成插件配置模板
- [ ] 集成到 ConfigManager

**预计时间**：2 天

**代码位置**：
- `src/NetherGate.Core/Plugins/ConfigSchemaGenerator.cs`
- `src/NetherGate.Core/Plugins/ConfigManager.cs`

**示例**：
```csharp
// 插件配置类
public class MyPluginConfig
{
    [Description("是否启用自动备份")]
    [DefaultValue(true)]
    public bool AutoBackup { get; set; } = true;
}

// 自动生成：
// # 是否启用自动备份
// # 默认值: true
// autoBackup: true
```

---

## 🔧 阶段二：高级功能（2-3 周）

### 2.1 插件仓库系统 ⭐⭐⭐

**目标**：构建插件市场，支持搜索、安装、更新

**任务清单**：
- [ ] 设计插件仓库 API 规范
- [ ] 实现 `PluginRepository` 客户端
- [ ] 添加插件搜索功能
- [ ] 实现插件下载和安装
- [ ] 实现自动更新检查
- [ ] 添加 CLI 命令支持

**预计时间**：5-7 天

**CLI 命令**：
```bash
NetherGate.exe plugin search backup
NetherGate.exe plugin install auto-backup
NetherGate.exe plugin update
```

---

### 2.2 权限系统增强 ⭐⭐

**目标**：添加权限组、权限继承、多级权限

**任务清单**：
- [ ] 扩展 `IPermissionManager` 接口
- [ ] 实现权限组系统
- [ ] 实现权限继承
- [ ] 添加预定义权限级别
- [ ] 创建权限配置文件

**预计时间**：3-4 天

**配置示例**：
```yaml
groups:
  admin:
    priority: 100
    permissions:
      - "nethergate.*"
  moderator:
    priority: 50
    inherit_from: ["member"]
    permissions:
      - "nethergate.kick"
      - "nethergate.mute"
```

---

### 2.3 崩溃分析系统 ⭐⭐

**目标**：智能识别崩溃类型并提供建议

**任务清单**：
- [ ] 创建 `CrashAnalyzer` 类
- [ ] 定义常见崩溃类型
- [ ] 实现崩溃模式匹配
- [ ] 生成崩溃报告
- [ ] 提供解决建议

**预计时间**：2-3 天

**支持的崩溃类型**：
- OutOfMemory（内存不足）
- PortInUse（端口占用）
- ModConflict（模组冲突）
- CorruptedWorld（世界损坏）
- NetworkError（网络错误）

---

## 📊 阶段三：监控和管理（3-4 周）

### 3.1 日志系统增强 ⭐

**目标**：插件独立日志、结构化日志、彩色输出

**任务清单**：
- [ ] 支持插件创建独立日志文件
- [ ] 实现结构化日志
- [ ] 添加彩色控制台输出
- [ ] 日志级别动态调整

**预计时间**：2 天

---

### 3.2 统计收集系统 ⭐

**目标**：收集服务器和插件使用统计

**任务清单**：
- [ ] 创建 `StatisticsCollector`
- [ ] 收集运行时间、玩家数等
- [ ] 记录命令使用情况
- [ ] 记录插件调用次数
- [ ] 生成统计报告

**预计时间**：2-3 天

---

### 3.3 Web 管理面板 ⭐

**目标**：提供简单的 Web API 和管理界面

**任务清单**：
- [ ] 实现 REST API
- [ ] 提供服务器状态查询
- [ ] 提供插件管理接口
- [ ] 提供命令执行接口
- [ ] 添加认证和授权

**预计时间**：5-7 天

**API 端点**：
```
GET  /api/status       - 服务器状态
GET  /api/plugins      - 插件列表
POST /api/plugins/{id}/reload - 重载插件
GET  /api/stats        - 统计信息
POST /api/command      - 执行命令
```

---

## 🎨 阶段四：用户体验优化（1-2 周）

### 4.1 交互式配置向导

**任务清单**：
- [ ] 实现首次运行检测
- [ ] 创建交互式向导
- [ ] 自动生成配置文件
- [ ] 提供配置验证

**预计时间**：2 天

---

### 4.2 CLI 命令增强

**任务清单**：
- [ ] 添加更多管理命令
- [ ] 实现配置验证命令
- [ ] 实现诊断工具
- [ ] 添加依赖检查命令

**预计时间**：2-3 天

---

## 📋 功能对比表

| 功能 | MCDR | NetherGate (当前) | 计划实施 |
|------|------|------------------|---------|
| 插件热重载 | ✅ | ✅ | - |
| 命令系统 | ✅ | ✅ | 增强中 |
| 事件系统 | ✅ | ✅ | - |
| 依赖管理 | ✅ | ✅ (更强大) | - |
| **插件仓库** | ✅ | ❌ | 阶段二 |
| **命令别名** | ✅ | ❌ | 阶段一 |
| **Tab 补全** | ✅ | ❌ | 阶段一 |
| **权限组** | ✅ | ⚠️ (基础) | 阶段二 |
| **崩溃分析** | ✅ | ❌ | 阶段二 |
| **配置模板** | ✅ | ❌ | 阶段一 |
| **统计系统** | ✅ | ❌ | 阶段三 |
| **Web 面板** | ⚠️ (简单) | ❌ | 阶段三 |
| SMP 支持 | ❌ | ✅ | - |
| 强类型 | ❌ | ✅ | - |
| NuGet 自动下载 | ❌ | ✅ | - |

---

## 🎯 优先级评分

| 功能 | 重要性 | 难度 | 优先级 | 预计时间 |
|------|--------|------|--------|---------|
| 插件元数据增强 | ⭐⭐⭐ | 低 | 🔥 高 | 2-3 天 |
| 命令系统增强 | ⭐⭐⭐ | 中 | 🔥 高 | 3-4 天 |
| 配置模板生成 | ⭐⭐ | 低 | 🔥 高 | 2 天 |
| 插件仓库 | ⭐⭐⭐ | 高 | ⏳ 中 | 5-7 天 |
| 权限系统增强 | ⭐⭐ | 中 | ⏳ 中 | 3-4 天 |
| 崩溃分析 | ⭐⭐ | 中 | ⏳ 中 | 2-3 天 |
| 统计系统 | ⭐ | 低 | 📅 低 | 2-3 天 |
| Web 面板 | ⭐ | 高 | 📅 低 | 5-7 天 |

---

## 🚦 实施建议

### 本周可以完成（快速见效）：
1. ✅ 插件元数据增强（2-3 天）
2. ✅ 配置模板自动生成（2 天）

### 下周可以完成：
3. ✅ 命令系统增强（3-4 天）
4. ✅ 崩溃分析系统（2-3 天）

### 月内完成：
5. ⏳ 插件仓库系统（5-7 天）
6. ⏳ 权限系统增强（3-4 天）

### 长期规划：
7. 📅 统计和监控（阶段三）
8. 📅 Web 管理面板（阶段三）
9. 📅 用户体验优化（阶段四）

---

## 📝 总结

NetherGate 已经在核心技术上超越了 MCDR（SMP 协议、强类型、NuGet 依赖管理），现在需要在**用户体验**和**生态系统**上继续完善。

优先实施：
1. 🎯 **插件元数据增强** - 让插件系统更完善
2. 🎯 **命令系统增强** - 提升开发者体验
3. 🎯 **配置模板生成** - 简化插件开发

这些功能实施后，NetherGate 将在功能完整性上全面超越 MCDR！
