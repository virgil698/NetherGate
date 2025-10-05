# NetherGate 命令前缀系统

本文档详细说明 NetherGate 的命令前缀约定和使用方法。

---

## 命令前缀概述

NetherGate 使用 **`#` (井号)** 作为框架命令和插件命令的前缀，以区分 Minecraft 游戏原生命令（使用 `/` 前缀）。

### 为什么使用 `#` 前缀？

1. **避免冲突**: Minecraft 游戏原生命令使用 `/` 前缀（如 `/give`, `/tp`）
2. **明确区分**: 清楚地区分游戏命令和框架/插件命令
3. **技术限制**: SMP 等技术目前尚不支持向游戏内注册自定义命令
4. **统一标准**: 为所有 NetherGate 插件提供统一的命令标识

---

## 命令前缀规则

### 1. 控制台输入（NetherGate 控制台）

在 NetherGate 控制台中，**可以不使用前缀**：

```bash
> help                    # ✅ 正确
> plugin list            # ✅ 正确
> status                 # ✅ 正确

> #help                  # ✅ 也正确（带前缀也可以）
> #plugin list           # ✅ 也正确
```

**控制台输入特点**:
- 前缀是**可选的**
- 带或不带 `#` 前缀都能正常执行
- 推荐不使用前缀以保持简洁

---

### 2. 游戏内输入（通过聊天框）

在 Minecraft 游戏聊天框中，**必须使用 `#` 前缀**：

```
# 游戏聊天框
#help                    # ✅ 正确
#plugin list             # ✅ 正确
#status                  # ✅ 正确

help                     # ❌ 错误 - 会被忽略或识别为聊天消息
plugin list              # ❌ 错误 - 会被忽略或识别为聊天消息
```

**游戏内输入特点**:
- `#` 前缀是**必需的**
- 没有前缀的消息会被当作普通聊天消息
- 使用 `/` 前缀会执行游戏原生命令

---

### 3. 游戏原生命令（`/` 前缀）

如果尝试在 NetherGate 中执行游戏原生命令（以 `/` 开头），会收到提示：

```bash
> /give @p diamond 1
❌ 游戏原生命令请直接在游戏内执行，NetherGate 命令请使用 # 前缀
```

**处理方式**:
- NetherGate 不处理 `/` 开头的命令
- 游戏原生命令应在游戏内聊天框直接执行
- 如需通过 NetherGate 执行游戏命令，使用 `server` 或 `rcon` 相关命令

---

## 命令示例

### 控制台命令示例

```bash
# NetherGate 控制台
> help
可用命令:
  help            - 显示所有可用命令
  plugin          - 管理插件（list/reload/enable/disable/info）
  status          - 显示服务器状态
  ...

注意: 游戏内执行命令需要加 # 前缀（例如: #help）

> plugin list
已加载的插件 (3):
  ✓ backup-manager v1.0.0 by Author1
  ✓ world-editor v2.1.0 by Author2
  ...

> status
服务器状态:
  版本: 1.20.1 (Protocol 763)
  在线玩家: 5
  ...
```

---

### 游戏内命令示例

玩家在 Minecraft 聊天框输入：

```
#help
#plugin list
#status
#plugin info backup-manager
```

玩家会在聊天框看到命令执行结果（如果插件支持返回消息到游戏内）。

---

## Tab 自动补全

### 控制台 Tab 补全

在控制台中，按 `Tab` 键可以自动补全命令：

```bash
> hel[Tab]        → help
> plugin re[Tab]  → plugin reload
> plugin reload b[Tab] → plugin reload backup-manager
```

**注意**: 控制台 Tab 补全不需要 `#` 前缀。

---

### 游戏内 Tab 补全（未来支持）

游戏内 Tab 补全功能取决于 SMP 或其他协议的支持，当前版本**暂不支持**。

**计划功能**:
```
#help[Tab]        → #help, #history, #...
#plugin [Tab]     → #plugin list, #plugin reload, #...
```

---

## 命令类型对比

| 命令类型 | 前缀 | 执行环境 | 示例 |
|---------|------|---------|------|
| **游戏原生命令** | `/` | 游戏内 | `/give @p diamond 1` |
| **NetherGate 命令（控制台）** | 无 或 `#` | 控制台 | `help` 或 `#help` |
| **NetherGate 命令（游戏内）** | `#` | 游戏聊天框 | `#help` |
| **插件命令（控制台）** | 无 或 `#` | 控制台 | `backup create` |
| **插件命令（游戏内）** | `#` | 游戏聊天框 | `#backup create` |

---

## 常见问题 (FAQ)

### Q1: 为什么不使用 `/` 前缀？

**A**: Minecraft 游戏原生命令已经使用 `/` 前缀，如果 NetherGate 也使用相同前缀会导致：
- 命令冲突（例如自定义 `/help` 会和游戏的 `/help` 冲突）
- 无法区分是游戏命令还是插件命令
- SMP 等技术暂不支持向游戏注册自定义 `/` 命令

---

### Q2: 我可以更改命令前缀吗？

**A**: 当前版本中，`#` 前缀是硬编码的，无法通过配置更改。

**未来计划**: 在后续版本中可能会支持自定义前缀配置：
```yaml
commands:
  prefix: "#"       # 可自定义为 "!", ".", "@" 等
  allow_no_prefix: true  # 控制台是否允许无前缀
```

---

### Q3: 如果我在控制台输入 `#help` 会怎样？

**A**: 完全正常！控制台会自动去掉 `#` 前缀并执行 `help` 命令。

控制台同时支持：
- `help` ✅ 
- `#help` ✅

两种方式效果完全相同。

---

### Q4: 插件开发者需要关心前缀吗？

**A**: **不需要**！插件开发者只需注册命令名称（不带前缀）：

```csharp
public class MyCommand : ICommand
{
    public string Name => "mycommand";  // 不需要加 # 前缀
    // ...
}
```

NetherGate 会自动处理 `#` 前缀的解析。

用户可以通过以下方式调用：
- 控制台: `mycommand` 或 `#mycommand`
- 游戏内: `#mycommand`

---

### Q5: 如何在游戏内执行 NetherGate 命令？

**A**: 当前版本中，游戏内执行 NetherGate 命令需要：

1. **确保 SMP 已启用并正常连接**
2. **在游戏聊天框输入 `#` 前缀命令**
3. **等待命令执行结果**（可能显示在聊天框或由插件自定义处理）

**注意**: 不同插件对游戏内命令的支持程度可能不同，某些命令可能仅限控制台使用。

---

### Q6: 我可以在插件中自定义命令前缀吗？

**A**: 插件注册的命令会自动使用 NetherGate 的统一前缀 `#`。

如果您的插件需要特殊的命令格式（例如正则表达式匹配聊天消息），可以：
1. 监听 `ChatMessageEvent`
2. 自行解析消息内容
3. 执行自定义逻辑

但**不推荐**这种做法，因为会破坏统一的命令体验。

---

## 设计理念

NetherGate 的命令前缀系统遵循以下设计原则：

### 1. **简洁性**
- 控制台无需前缀，操作简单直接
- 游戏内使用单字符前缀 `#`，输入快速

### 2. **一致性**
- 所有 NetherGate 和插件命令使用相同前缀
- 用户学习成本低

### 3. **明确性**
- `#` 前缀清晰标识这是 NetherGate 命令
- 不会与游戏原生命令冲突

### 4. **灵活性**
- 控制台支持带或不带前缀
- 为未来的配置化预留扩展空间

---

## 实现细节

### 命令解析流程

```
用户输入 → 去除空白 → 检测前缀
    ↓
是 # 前缀? 
    ├─ 是 → 去掉 # → 解析命令
    └─ 否 → 直接解析命令
    ↓
是 / 前缀?
    ├─ 是 → 返回错误提示
    └─ 否 → 继续执行
    ↓
查找命令 → 权限检查 → 执行
```

### 代码实现（参考）

```csharp
public async Task<CommandResult> ExecuteCommandAsync(string commandLine, ICommandSender? sender = null)
{
    // 处理 # 前缀（框架命令前缀）
    commandLine = commandLine.TrimStart();
    if (commandLine.StartsWith("#"))
    {
        commandLine = commandLine.Substring(1).TrimStart();
    }

    // 如果是游戏原生命令（以 / 开头），不处理
    if (commandLine.StartsWith("/"))
    {
        return CommandResult.Fail("游戏原生命令请直接在游戏内执行，NetherGate 命令请使用 # 前缀");
    }

    // ... 解析并执行命令
}
```

---

## 最佳实践

### 1. 控制台使用

**推荐**: 不使用前缀
```bash
✅ help
✅ plugin list
✅ status
```

**不推荐但可用**: 使用前缀
```bash
⚠️ #help        # 可以用，但不够简洁
⚠️ #plugin list
```

---

### 2. 游戏内使用

**必须**: 使用 `#` 前缀
```
✅ #help
✅ #plugin list
✅ #backup create world1
```

**错误示例**:
```
❌ help          # 会被当作聊天消息
❌ /help         # 会执行游戏原生 /help 命令
```

---

### 3. 插件开发

**命令注册**:
```csharp
public class BackupCommand : ICommand
{
    public string Name => "backup";  // 不要加前缀
    public string Usage => "backup <create|restore|list>";
    // ...
}
```

**文档说明**:
```markdown
## 命令使用

- 控制台: `backup create world1`
- 游戏内: `#backup create world1`
```

---

## 相关文档

- [命令系统详解](COMMAND_AND_PERMISSION_GUIDE.md)
- [插件管理命令](PLUGIN_MANAGEMENT_COMMANDS.md)
- [内置命令列表](../README.md#内置命令)
- [插件开发指南](PLUGIN_PROJECT_STRUCTURE.md)

---

## 未来改进计划

### 短期（v0.2.0）
- [ ] 改进游戏内命令反馈机制
- [ ] 支持游戏内 Tab 补全（取决于 SMP 支持）
- [ ] 完善命令错误提示

### 中期（v0.3.0）
- [ ] 支持自定义命令前缀配置
- [ ] 支持多种前缀格式（如 `@`, `!`, `.`）
- [ ] 命令前缀冲突检测

### 长期（v1.0.0）
- [ ] 支持动态注册游戏内命令（需要 SMP 或插件支持）
- [ ] 命令别名前缀（不同插件可使用不同前缀）
- [ ] 可视化命令配置界面

---

## 反馈与贡献

如果您对命令前缀系统有任何建议或遇到问题：

1. **GitHub Issues**: [https://github.com/BlockBridge/NetherGate/issues](https://github.com/BlockBridge/NetherGate/issues)
2. **Discord 社区**: 加入我们的讨论频道
3. **提交 PR**: 欢迎贡献代码改进

---

**最后更新**: 2025-01-XX  
**版本**: v0.1.0  
**作者**: NetherGate Team
