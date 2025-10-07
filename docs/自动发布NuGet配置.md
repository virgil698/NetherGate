# NetherGate.API 自动发布 NuGet 包配置指南

## 概述

NetherGate 已配置 GitHub Actions 自动发布 NuGet 包，支持以下触发方式：

1. **定期发布** - 每周一自动发布
2. **Release 发布** - 创建 GitHub Release 时自动发布
3. **手动发布** - 通过 GitHub Actions 界面手动触发

---

## 触发方式详解

### 1. 定期自动发布 ⏰

**触发时间**：每周一凌晨 2:00 (UTC) / 北京时间 10:00

**行为**：
- 自动从 `src/NetherGate.API/NetherGate.API.csproj` 读取版本号
- 构建并发布到 NuGet.org 和 GitHub Packages
- 如果版本已存在，会跳过（`--skip-duplicate`）

**适用场景**：
- 确保最新代码定期同步到 NuGet
- 无需手动干预的持续发布

**修改定时计划**：

编辑 `.github/workflows/publish-nuget.yml`：

```yaml
schedule:
  - cron: '0 2 * * 1'  # 每周一 02:00 UTC
```

常用 cron 表达式：
- `0 2 * * 1` - 每周一 02:00
- `0 2 * * *` - 每天 02:00
- `0 2 1 * *` - 每月 1 号 02:00
- `0 2 1 */3 *` - 每季度 1 号 02:00

### 2. Release 自动发布 🚀

**触发条件**：在 GitHub 创建新的 Release

**步骤**：

1. 进入仓库的 [Releases 页面](https://github.com/virgil698/NetherGate/releases)
2. 点击 "Draft a new release"
3. 填写信息：
   - **Tag**: `v1.0.1` 或 `1.0.1`（会自动去掉 `v` 前缀）
   - **Title**: `NetherGate.API v1.0.1`
   - **Description**: 版本更新说明
4. 点击 "Publish release"

**行为**：
- 使用 Release Tag 作为 NuGet 包版本号
- 自动构建并发布

**示例**：
```
Tag: v1.2.0
→ 发布 NetherGate.API 1.2.0
```

### 3. 手动触发发布 🔧

**适用场景**：
- 紧急发布修复版本
- 测试发布流程
- 自定义版本号发布

**步骤**：

1. 进入 [Actions 页面](https://github.com/virgil698/NetherGate/actions/workflows/publish-nuget.yml)
2. 点击 "Run workflow"
3. 填写参数：
   - **version** (可选): 自定义版本号（如 `1.0.2`）
     - 留空则使用 `.csproj` 中的版本号
   - **force_publish** (可选): 是否强制发布
     - `false` (默认): 跳过已存在的版本
     - `true`: 强制覆盖（需要 NuGet 包未列出）
4. 点击 "Run workflow"

---

## 配置 NuGet API Key

### 步骤 1: 获取 NuGet API Key

1. 访问 [NuGet.org API Keys](https://www.nuget.org/account/apikeys)
2. 创建新 Key：
   - **Key Name**: `NetherGate`
   - **Expires In**: `365 days`
   - **Scopes**: Push new packages and package versions
   - **Glob Pattern**: `NetherGate.*`

### 步骤 2: 配置 GitHub Secret

1. 进入仓库 [Settings > Secrets and variables > Actions](https://github.com/virgil698/NetherGate/settings/secrets/actions)
2. 点击 "New repository secret"
3. 填写：
   - **Name**: `NUGET_API_KEY`
   - **Value**: 粘贴你的 NuGet API Key
4. 点击 "Add secret"

### 步骤 3: 验证配置

手动触发一次 workflow 测试是否正常工作。

---

## 版本管理策略

### 自动版本号（推荐）

在 `src/NetherGate.API/NetherGate.API.csproj` 中维护版本号：

```xml
<Version>1.0.0</Version>
```

每次需要发布新版本时：
1. 更新 `.csproj` 中的版本号
2. 提交并推送代码
3. 等待定期发布，或手动触发/创建 Release

### 语义化版本规范

遵循 [SemVer 2.0.0](https://semver.org/)：

- **主版本号 (Major)**: 不兼容的 API 修改
  - `1.0.0` → `2.0.0`
  - 示例：删除接口、重命名方法
  
- **次版本号 (Minor)**: 向下兼容的功能性新增
  - `1.0.0` → `1.1.0`
  - 示例：添加新事件、新接口
  
- **修订号 (Patch)**: 向下兼容的问题修正
  - `1.0.0` → `1.0.1`
  - 示例：修复 Bug、文档更新

### 预发布版本

测试新功能时使用预发布版本：

```xml
<Version>1.1.0-beta.1</Version>
<Version>1.1.0-rc.1</Version>
```

---

## 发布到 GitHub Packages

除了 NuGet.org，包也会自动发布到 GitHub Packages。

### 安装来自 GitHub Packages 的包

1. **配置 NuGet 源**：
   ```bash
   dotnet nuget add source \
     --name github \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_GITHUB_TOKEN \
     --store-password-in-clear-text \
     https://nuget.pkg.github.com/virgil698/index.json
   ```

2. **安装包**：
   ```bash
   dotnet add package NetherGate.API --source github
   ```

---

## 工作流程概览

```
┌─────────────────────────────────────────────────────────┐
│                   触发发布                                │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  1. 定期触发 (每周一)                                     │
│     └─> 读取 .csproj 版本号                               │
│                                                           │
│  2. 创建 Release                                          │
│     └─> 使用 Release Tag 作为版本号                        │
│                                                           │
│  3. 手动触发                                              │
│     └─> 使用输入版本号或 .csproj 版本号                     │
│                                                           │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│                   构建流程                                │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  1. Checkout 代码                                         │
│  2. 设置 .NET 9.0                                         │
│  3. 提取版本号                                            │
│  4. Restore 依赖                                          │
│  5. Build 项目                                            │
│  6. Pack NuGet 包                                         │
│                                                           │
└─────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│                   发布流程                                │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  1. 发布到 NuGet.org                                      │
│     └─> 使用 NUGET_API_KEY Secret                         │
│                                                           │
│  2. 发布到 GitHub Packages                                │
│     └─> 使用 GITHUB_TOKEN                                 │
│                                                           │
│  3. 上传构建产物                                          │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 监控和调试

### 查看发布历史

访问 [Actions 页面](https://github.com/virgil698/NetherGate/actions/workflows/publish-nuget.yml) 查看所有发布记录。

### 查看发布摘要

每次成功发布后，会在 Actions 运行详情中生成摘要，包含：
- 包名称和版本号
- 触发方式
- NuGet.org 和 GitHub Packages 链接

### 常见问题排查

#### 问题 1: 发布失败 - "Package already exists"

**原因**：版本号已在 NuGet.org 存在

**解决方案**：
1. 更新 `.csproj` 中的版本号
2. 重新触发发布

#### 问题 2: 发布失败 - "Unauthorized"

**原因**：NUGET_API_KEY 未配置或已过期

**解决方案**：
1. 在 NuGet.org 重新生成 API Key
2. 更新 GitHub Secret

#### 问题 3: 定期发布未触发

**原因**：
- GitHub Actions 可能有延迟（最多 5-10 分钟）
- 仓库可能被归档或禁用

**解决方案**：
1. 检查仓库设置中 Actions 是否启用
2. 手动触发一次测试

---

## 禁用定期发布

如果不需要定期自动发布，可以编辑 `.github/workflows/publish-nuget.yml`，注释掉或删除 `schedule` 部分：

```yaml
on:
  release:
    types: [published]
  
  # schedule:  # 注释掉定期发布
  #   - cron: '0 2 * * 1'
  
  workflow_dispatch:
    # ...
```

---

## 最佳实践

1. **版本号管理**
   - 始终在 `.csproj` 中维护正确的版本号
   - 每次合并 PR 前检查版本号是否需要更新

2. **发布前测试**
   - 在本地运行 `scripts/pack-nuget.bat` 确保能正常构建
   - 检查生成的 `.nupkg` 包内容

3. **文档同步**
   - 更新版本号时同步更新 CHANGELOG.md
   - Release 说明要清晰列出变更内容

4. **API Key 安全**
   - 定期轮换 NuGet API Key
   - 不要将 API Key 提交到代码仓库

5. **监控发布**
   - 订阅 Actions 失败通知
   - 定期检查 NuGet.org 包页面

---

## 相关链接

- [NuGet.org 包页面](https://www.nuget.org/packages/NetherGate.API/)
- [GitHub Packages](https://github.com/virgil698/NetherGate/packages)
- [Actions 工作流](https://github.com/virgil698/NetherGate/actions/workflows/publish-nuget.yml)
- [发布 NuGet 包指南](./发布NuGet包指南.md)

