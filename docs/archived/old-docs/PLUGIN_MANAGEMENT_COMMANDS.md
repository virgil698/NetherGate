# NetherGate 插件管理命令

本文档详细介绍 NetherGate 的插件管理命令系统。

---

## 命令列表

### 1. `plugin list` - 查看插件列表

**别名**: `pl list`, `pl ls`, `plugins`

**用法**:
```
plugin list
```

**描述**: 显示所有已加载的插件，包括状态、版本和作者信息。

**示例输出**:
```
已加载的插件 (3):
  ✓ backup-manager v1.0.0 by ExampleAuthor
  ✗ world-editor v2.1.0 by AnotherDev
  ! buggy-plugin v0.1.0 by TestUser

使用 'plugin info <id>' 查看详情
```

**状态图标**:
- `✓` - 已启用
- `✗` - 已禁用
- `!` - 错误状态
- `?` - 未知状态

---

### 2. `plugin reload` - 重载插件

**别名**: `pl reload`, `pl rl`

**用法**:
```
plugin reload <插件ID>      # 重载单个插件
plugin reload all           # 重载所有插件
```

**描述**: 
- 重载单个插件：禁用、卸载、重新加载并启用指定插件
- 重载所有插件：批量重载所有已启用的插件

**功能**:
1. 保存插件状态（如果插件实现了 `SaveStateAsync`）
2. 调用插件的 `OnDisable`
3. 卸载插件程序集
4. 重新加载插件
5. 调用插件的 `OnLoad` 和 `OnEnable`
6. 恢复插件状态（如果插件实现了 `RestoreStateAsync`）

**示例**:
```
> plugin reload backup-manager
[14:23:45 INFO]: 重载插件: backup-manager
[14:23:45 INFO]: 插件 backup-manager 重载成功

> plugin reload all
[14:24:10 INFO]: 开始重载所有插件...
[14:24:10 INFO]: 重载插件: backup-manager
[14:24:11 INFO]: 重载插件: world-editor
[14:24:12 INFO]: 插件重载完成: 成功 2 个，失败 0 个
```

**注意事项**:
- 重载过程中，插件的所有事件订阅和命令会被清理
- 插件的配置文件不会重新加载，除非插件自行实现
- 如果重载失败，插件会保持禁用状态

---

### 3. `plugin enable` - 启用插件

**别名**: `pl enable`

**用法**:
```
plugin enable <插件ID>
```

**描述**: 启用已禁用的插件。

**示例**:
```
> plugin enable backup-manager
[14:25:00 INFO]: 插件 backup-manager 已启用
```

**错误处理**:
- 如果插件不存在：`未找到插件: <id>`
- 如果插件已启用：`插件 <id> 已经是启用状态`
- 如果启用失败：`启用插件失败: <错误信息>`

---

### 4. `plugin disable` - 禁用插件

**别名**: `pl disable`

**用法**:
```
plugin disable <插件ID>
```

**描述**: 禁用已启用的插件。禁用后，插件的所有功能将停止，包括：
- 事件处理器停止响应
- 命令将不再可用
- 定时任务将被取消

**示例**:
```
> plugin disable backup-manager
[14:26:00 INFO]: 插件 backup-manager 已禁用
```

**错误处理**:
- 如果插件不存在：`未找到插件: <id>`
- 如果插件已禁用：`插件 <id> 已经是禁用状态`
- 如果禁用失败：`禁用插件失败: <错误信息>`

---

### 5. `plugin info` - 查看插件详情

**别名**: `pl info`

**用法**:
```
plugin info <插件ID>
```

**描述**: 显示插件的详细信息，包括元数据、依赖关系和状态。

**示例**:
```
> plugin info backup-manager
插件信息:
  ID: backup-manager
  名称: Backup Manager
  版本: 1.0.0
  作者: ExampleAuthor
  描述: 自动备份服务器世界
  状态: 已启用
  NuGet依赖:
    - Newtonsoft.Json
    - SharpZipLib
  库依赖:
    - fNbt >= 1.0.0
  插件依赖:
    - world-api >= 2.0.0
  网站: https://github.com/example/backup-manager
```

**显示内容**:
- 基本信息：ID、名称、版本、作者、描述
- 当前状态
- NuGet 依赖包
- 库依赖（Library Dependencies）
- 插件间依赖（包括版本要求和可选标记）
- 官方网站（如果提供）

---

### 6. `plugin load` - 加载新插件

**别名**: `pl load`

**用法**:
```
plugin load <插件ID>
```

**描述**: 从 `plugins/` 目录加载新添加的插件（不需要重启 NetherGate）。

**状态**: ⚠️ **暂未实现**

**当前行为**: 提示用户重启 NetherGate 以加载新插件。

**计划功能**:
- 扫描 `plugins/` 目录
- 发现新插件
- 加载并启用新插件
- 自动解决依赖关系

---

### 7. `plugin unload` - 卸载插件

**别名**: `pl unload`

**用法**:
```
plugin unload <插件ID>
```

**描述**: 完全卸载插件，包括从内存中移除。

**状态**: ⚠️ **暂未实现**

**当前替代方案**: 使用 `plugin disable <id>` 来禁用插件。

**计划功能**:
- 禁用插件
- 清理插件资源
- 卸载插件程序集
- 从插件列表中移除

---

## Tab 自动补全

`plugin` 命令支持完整的 Tab 自动补全功能：

### 第一级补全 - 子命令
```
> plugin [Tab]
list    reload    enable    disable    info    load    unload
```

### 第二级补全 - 插件 ID
```
> plugin reload [Tab]
all    backup-manager    world-editor    ...

> plugin enable [Tab]
backup-manager    world-editor    ...

> plugin info [Tab]
backup-manager    world-editor    ...
```

---

## 权限要求

所有 `plugin` 命令需要以下权限：

```
nethergate.plugins.manage
```

如果您没有此权限，将收到错误提示。

---

## 兼容性 - `plugins` 命令

为了向后兼容，传统的 `plugins` 命令仍然可用，它等同于 `plugin list`：

```
> plugins
已加载的插件 (3):
  ✓ backup-manager v1.0.0 by ExampleAuthor
  ...
```

**推荐**: 使用新的 `plugin` 命令系统以获得完整功能。

---

## 使用场景

### 场景 1: 开发插件时快速测试修改

```bash
# 1. 编辑插件代码
# 2. 重新编译插件
# 3. 将新的 DLL 复制到 plugins/my-plugin/
> plugin reload my-plugin
```

这样就能立即测试新代码，无需重启 NetherGate。

---

### 场景 2: 临时禁用问题插件

```bash
> plugin list
  ✓ backup-manager v1.0.0 by Author1
  ! buggy-plugin v0.1.0 by Author2    # 这个插件有问题

> plugin disable buggy-plugin
[15:00:00 INFO]: 插件 buggy-plugin 已禁用
```

服务器可以继续运行，问题插件已被隔离。

---

### 场景 3: 批量重载所有插件（更新配置后）

```bash
# 修改了多个插件的配置文件后
> plugin reload all
[15:05:00 INFO]: 开始重载所有插件...
[15:05:01 INFO]: 插件重载完成: 成功 5 个，失败 0 个
```

所有插件重新加载配置，无需重启服务器。

---

### 场景 4: 检查插件依赖关系

```bash
> plugin info backup-manager
插件信息:
  ID: backup-manager
  ...
  插件依赖:
    - world-api >= 2.0.0

# 检查 world-api 是否已安装
> plugin list
  ✓ world-api v2.1.0 by WorldDev    # ✅ 已安装且版本符合
```

---

## 插件状态说明

插件可以处于以下状态之一：

| 状态 | 说明 | 图标 |
|------|------|------|
| `Loaded` | 已加载但未启用 | `?` |
| `Enabled` | 已启用且正常运行 | `✓` |
| `Disabled` | 已禁用 | `✗` |
| `Error` | 加载或启用时发生错误 | `!` |

---

## 错误处理

### 常见错误及解决方法

#### 1. `未找到插件: <id>`

**原因**: 插件 ID 拼写错误或插件未加载。

**解决方法**:
```bash
> plugin list                    # 查看可用的插件 ID
> plugin info correct-plugin-id  # 使用正确的 ID
```

#### 2. `重载插件失败: <错误信息>`

**可能原因**:
- 插件文件被锁定或损坏
- 插件依赖缺失
- 插件代码有错误

**解决方法**:
1. 检查日志文件 `logs/latest.log` 获取详细错误信息
2. 确保插件的所有依赖都在 `plugins/plugin-name/` 或 `lib/` 目录
3. 检查插件是否与当前 NetherGate 版本兼容

#### 3. `插件重载完成: 成功 X 个，失败 Y 个`

**解决方法**:
1. 查看日志确定哪些插件重载失败
2. 单独重载失败的插件以获取详细错误：
   ```bash
   > plugin reload failed-plugin-id
   ```

---

## 最佳实践

### 1. 开发时使用热重载

在开发插件时，使用 `plugin reload` 可以大大加快测试速度：

```bash
# 开发循环
1. 修改代码
2. 编译插件
3. plugin reload my-plugin
4. 测试
5. 重复
```

### 2. 生产环境谨慎重载

在生产服务器上重载插件前：
- ✅ 在测试环境验证
- ✅ 备份重要数据
- ✅ 在低峰时段操作
- ✅ 准备回滚计划

### 3. 使用 Tab 补全避免错误

按 `Tab` 键自动补全命令和插件 ID，避免拼写错误：

```bash
> plugin rel[Tab] → plugin reload
> plugin reload back[Tab] → plugin reload backup-manager
```

### 4. 定期检查插件状态

```bash
> plugin list
```

确保所有关键插件都是 `✓` 已启用状态。

---

## 与旧版本的区别

| 功能 | 旧版 (`plugins`) | 新版 (`plugin`) |
|------|------------------|-----------------|
| 列出插件 | ✅ `plugins` | ✅ `plugin list` |
| 重载插件 | ❌ 不支持 | ✅ `plugin reload <id>` |
| 批量重载 | ❌ 不支持 | ✅ `plugin reload all` |
| 启用/禁用 | ❌ 不支持 | ✅ `plugin enable/disable <id>` |
| 插件详情 | ❌ 不支持 | ✅ `plugin info <id>` |
| Tab 补全 | ❌ 不支持 | ✅ 完整支持 |

---

## 日志格式

插件管理操作会生成以下格式的日志：

```
[14:23:45 INFO]: 重载插件: backup-manager
[14:23:45 INFO]: 插件 backup-manager 重载成功
```

新的日志格式更简洁：`[HH:mm:ss LEVEL]: 消息`

---

## 相关文档

- [插件热重载与状态保存](PLUGIN_HOTRELOAD_AND_MESSENGER.md) - 详细的热重载机制
- [插件开发指南](PLUGIN_PROJECT_STRUCTURE.md) - 如何开发支持热重载的插件
- [权限系统](COMMAND_AND_PERMISSION_GUIDE.md) - 权限配置和管理

---

## 反馈与贡献

如果您在使用插件管理命令时遇到问题或有改进建议，请：

1. 查看 `logs/latest.log` 获取详细错误信息
2. 在 GitHub Issues 提交 Bug 报告或功能请求
3. 参与社区讨论

**项目地址**: [https://github.com/BlockBridge/NetherGate](https://github.com/BlockBridge/NetherGate)
