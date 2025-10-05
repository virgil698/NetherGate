# NetherGate 自动依赖管理

## 🎯 功能概述

NetherGate 提供了完整的自动依赖管理系统，无需手动下载和管理 DLL 文件。

### 核心特性

1. **🚀 自动下载**：从 NuGet 自动下载插件所需的依赖
2. **🔧 版本冲突解决**：智能检测并解决不同插件之间的版本冲突
3. **📦 多源支持**：支持多个 NuGet 源，包括官方源和国内镜像
4. **🛡️ 安全验证**：可选的哈希验证，确保下载文件的完整性
5. **📊 详细报告**：显示冲突检测和解决过程的详细日志

---

## ⚙️ 配置

在 `nethergate-config.yaml` 中配置：

```yaml
plugins:
  dependency_management:
    # 是否启用依赖管理
    enabled: true
    
    # 是否自动下载缺失的依赖（从 NuGet）
    auto_download: true
    
    # NuGet 源列表（按顺序尝试）
    nuget_sources:
      - "https://api.nuget.org/v3/index.json"          # 官方源
      # - "https://nuget.cdn.azure.cn/v3/index.json"  # 国内镜像
    
    # 下载超时时间（秒）
    download_timeout: 60
    
    # 验证下载文件的哈希值
    verify_hash: true
    
    # 版本冲突解决策略
    # - highest: 使用最高版本（推荐）
    # - lowest: 使用最低兼容版本
    # - fail: 报错，让用户手动处理
    conflict_resolution: highest
    
    # 是否显示冲突报告
    show_conflict_report: true
```

---

## 📝 插件依赖声明

在 `plugin.json` 中声明依赖：

```json
{
  "id": "my-awesome-plugin",
  "name": "My Awesome Plugin",
  "version": "1.0.0",
  "main": "MyPlugin.MyPluginMain",
  
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.3",
      "location": "lib",
      "optional": false
    },
    {
      "name": "Serilog",
      "version": ">=3.0.0",
      "location": "lib",
      "optional": false
    }
  ]
}
```

### 字段说明

| 字段 | 说明 | 示例 |
|------|------|------|
| `name` | 依赖库名称（不含 `.dll`） | `"Newtonsoft.Json"` |
| `version` | 版本要求 | `"13.0.3"` 或 `">=13.0.0"` |
| `location` | 依赖位置（`lib` 或 `local`） | `"lib"` |
| `optional` | 是否为可选依赖 | `false` |

### 版本要求格式

| 格式 | 说明 | 示例 |
|------|------|------|
| 精确版本 | 完全匹配 | `"13.0.3"` |
| 最低版本 | 大于等于 | `">=13.0.0"` |
| 版本范围 | 区间匹配 | `"[13.0.0, 14.0.0)"` |
| 任意版本 | 使用最新版本 | `""` 或不设置 |

---

## 🔄 工作流程

### 1. 插件加载时

```
开始加载插件
========================================
扫描插件...
  - PluginA (v1.0.0)
  - PluginB (v2.0.0)

检查插件依赖...
依赖冲突报告:
====================

依赖: Newtonsoft.Json
冲突的版本要求:
  - PluginA: 13.0.3
  - PluginB: 13.0.5
解决方案: 使用版本 13.0.5

✓ 解决方案: 使用版本 13.0.5
需要检查 3 个依赖
正在下载 NuGet 包: Newtonsoft.Json 13.0.5
✓ 下载成功: Newtonsoft.Json
依赖已存在: Serilog
依赖已存在: MySql.Data
✓ 所有依赖都已就绪

加载插件程序集...
  ✓ PluginA
  ✓ PluginB

初始化插件...
  ✓ PluginA initialized
  ✓ PluginB initialized

启用插件...
  ✓ PluginA enabled
  ✓ PluginB enabled

========================================
插件加载完成，共 2 个插件
========================================
```

### 2. 版本冲突解决

#### 策略：`highest`（推荐）

使用最高版本，确保兼容性：

```
PluginA 需要: Newtonsoft.Json 13.0.3
PluginB 需要: Newtonsoft.Json 13.0.5
结果: 使用 13.0.5
```

#### 策略：`lowest`

使用最低兼容版本：

```
PluginA 需要: Newtonsoft.Json >=13.0.0
PluginB 需要: Newtonsoft.Json >=13.0.3
结果: 使用 13.0.3
```

#### 策略：`fail`

遇到冲突直接报错，让用户手动处理：

```
错误：检测到依赖冲突
依赖: Newtonsoft.Json
  - PluginA 需要: 13.0.3
  - PluginB 需要: 13.0.5
请手动解决冲突或更改配置
```

---

## 🌐 NuGet 源配置

### 官方源（默认）

```yaml
nuget_sources:
  - "https://api.nuget.org/v3/index.json"
```

### 添加国内镜像

```yaml
nuget_sources:
  - "https://api.nuget.org/v3/index.json"
  - "https://nuget.cdn.azure.cn/v3/index.json"  # Azure 中国镜像
```

### 企业私有源

```yaml
nuget_sources:
  - "https://your-company.com/nuget/v3/index.json"
  - "https://api.nuget.org/v3/index.json"  # 备用源
```

---

## 📚 最佳实践

### ✅ 推荐做法

#### 1. 启用自动依赖管理

让 NetherGate 自动处理依赖，简化插件开发：

```yaml
dependency_management:
  enabled: true
  auto_download: true
```

#### 2. 使用宽松版本要求

使用 `>=` 格式，允许自动升级：

```json
{
  "name": "Newtonsoft.Json",
  "version": ">=13.0.0",
  "location": "lib"
}
```

#### 3. 共享依赖放 `lib`

将多个插件共享的依赖声明为 `"location": "lib"`：

```json
{
  "name": "MySql.Data",
  "location": "lib"  // 所有插件共享
}
```

#### 4. 添加国内镜像

如果在中国大陆，添加国内镜像加速下载：

```yaml
nuget_sources:
  - "https://nuget.cdn.azure.cn/v3/index.json"  # 优先国内镜像
  - "https://api.nuget.org/v3/index.json"
```

### ❌ 不推荐做法

#### 1. 精确版本要求

除非必要，避免使用精确版本：

```json
// ❌ 不推荐
{"version": "13.0.3"}

// ✅ 推荐
{"version": ">=13.0.0"}
```

#### 2. 禁用自动下载

禁用自动下载需要手动管理依赖，增加维护成本：

```yaml
# ❌ 不推荐
auto_download: false
```

#### 3. 使用 `fail` 策略

除非你有特殊需求，否则 `fail` 策略会增加配置难度：

```yaml
# ❌ 不推荐
conflict_resolution: fail

# ✅ 推荐
conflict_resolution: highest
```

---

## 🐛 故障排除

### 问题 1: 依赖下载失败

**现象**：
```
✗ 所有源都无法下载依赖: Newtonsoft.Json
```

**解决方案**：

1. 检查网络连接
2. 尝试添加国内镜像
3. 增加超时时间：
   ```yaml
   download_timeout: 120
   ```
4. 手动下载 DLL 并放入 `lib/` 目录

### 问题 2: 版本冲突无法解决

**现象**：
```
依赖管理失败，部分插件可能无法加载
```

**解决方案**：

1. 查看冲突报告，了解具体冲突
2. 调整插件的版本要求，使用 `>=` 格式
3. 如果无法自动解决，使用 `conflict_resolution: fail` 查看详细错误

### 问题 3: 插件加载失败

**现象**：
```
无法加载插件: PluginA
```

**解决方案**：

1. 检查插件的 `library_dependencies` 是否正确
2. 确保依赖的 `location` 设置正确
3. 查看日志中的详细错误信息
4. 尝试手动下载依赖到 `lib/` 或插件目录

---

## 📖 相关文档

- [插件依赖管理系统](PLUGIN_DEPENDENCIES.md) - 完整的依赖管理文档
- [插件项目结构](PLUGIN_PROJECT_STRUCTURE.md) - 插件开发指南
- [配置文件说明](CONFIGURATION.md) - 核心配置文档

---

## 🤝 贡献

如果你在使用自动依赖管理时遇到问题或有改进建议，欢迎：

1. 提交 [Issue](https://github.com/your-repo/NetherGate/issues)
2. 创建 [Pull Request](https://github.com/your-repo/NetherGate/pulls)
3. 参与 [讨论](https://github.com/your-repo/NetherGate/discussions)
