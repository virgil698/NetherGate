# NetherGate 插件依赖管理系统

## 📦 概述

NetherGate 提供了智能的插件依赖管理系统，支持：

1. **🚀 自动依赖下载**：从 NuGet 自动下载插件所需的依赖
2. **🔧 版本冲突解决**：自动检测并解决不同插件之间的版本冲突
3. **📂 三层依赖解析**：灵活的依赖解析策略，避免 DLL Hell 问题

---

## 🏗️ 三层依赖解析策略

NetherGate 使用以下优先级顺序来加载程序集：

```
┌─────────────────────────────────────┐
│  第一层: lib/ (全局共享依赖)         │  ← 最高优先级
│  - 所有插件共享                      │
│  - 版本统一管理                      │
└─────────────────────────────────────┘
              ↓ 未找到则继续
┌─────────────────────────────────────┐
│  第二层: plugins/plugin-name/        │  ← 中等优先级
│  - 插件私有依赖                      │
│  - 独立隔离                          │
└─────────────────────────────────────┘
              ↓ 未找到则继续
┌─────────────────────────────────────┐
│  第三层: 默认 .NET 程序集            │  ← 最低优先级
│  - 系统库                            │
│  - NetherGate 核心 API              │
└─────────────────────────────────────┘
```

---

##目录结构

```
NetherGate/
├── NetherGate.exe              # 主程序
├── NetherGate.API.dll         # 核心 API
├── NetherGate.Core.dll        # 核心实现
│
├── lib/                        # 全局共享依赖库（第一层）
│   ├── Newtonsoft.Json.dll    # 示例：JSON 库
│   ├── Serilog.dll            # 示例：日志库
│   └── MySql.Data.dll         # 示例：数据库驱动
│
├── plugins/                    # 插件目录
│   ├── example-plugin/         # 插件 A
│   │   ├── plugin.json         # 插件元数据
│   │   ├── ExamplePlugin.dll   # 插件主程序
│   │   ├── SpecificLib.dll     # 插件 A 私有依赖（第二层）
│   │   └── AnotherDep.dll
│   │
│   └── another-plugin/         # 插件 B
│       ├── plugin.json
│       ├── AnotherPlugin.dll
│       └── PrivateDep.dll      # 插件 B 私有依赖（第二层）
│
└── config/                     # 配置目录
    ├── example-plugin.json
    └── another-plugin.json
```

---

## 📝 在 plugin.json 中声明依赖

### 基本格式

```json
{
  "id": "com.example.myplugin",
  "name": "My Plugin",
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
      "name": "MySql.Data",
      "version": ">=8.0.0",
      "location": "auto",
      "optional": true
    },
    {
      "name": "MyPrivateLib",
      "location": "local",
      "optional": false
    }
  ]
}
```

### 依赖项字段说明

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | ✅ | 程序集名称（不含 `.dll` 扩展名） |
| `version` | string | ❌ | 版本要求（支持 `13.0.0` 或 `>=8.0.0`）|
| `location` | string | ❌ | 加载位置：`lib`、`local`、`auto`（默认 `auto`）|
| `optional` | boolean | ❌ | 是否为可选依赖（默认 `false`）|

### location 参数详解

| 值 | 说明 | 使用场景 |
|-----|------|----------|
| `lib` | 只从全局 `lib/` 文件夹加载 | 共享依赖，所有插件都使用 |
| `local` | 只从插件自己的文件夹加载 | 插件私有依赖，版本特殊 |
| `auto` | 先 `lib/`，再 `local/`（推荐）| 大多数情况 |

---

## 🔧 依赖冲突处理规则

### 规则 1: lib/ 优先级最高

如果依赖同时存在于 `lib/` 和插件文件夹中，**始终使用 `lib/` 中的版本**。

**示例**:
```
lib/
  └── Newtonsoft.Json.dll  (v13.0.3)
plugins/my-plugin/
  └── Newtonsoft.Json.dll  (v12.0.0)
```
**结果**: 插件将加载 v13.0.3（来自 `lib/`）

### 规则 2: 多插件相同依赖

如果两个插件都在自己的文件夹中包含相同依赖，**每个插件加载自己的版本**（隔离）。

**示例**:
```
plugins/plugin-a/
  └── SpecialLib.dll  (v1.0.0)
plugins/plugin-b/
  └── SpecialLib.dll  (v2.0.0)
```
**结果**: 
- Plugin A 使用 v1.0.0
- Plugin B 使用 v2.0.0
- 互不影响（得益于 `AssemblyLoadContext` 隔离）

### 规则 3: 共享 lib/ 中的依赖

如果依赖放在 `lib/` 中，**所有插件共享同一版本**。

**示例**:
```
lib/
  └── MySql.Data.dll  (v8.0.33)
```
**结果**: 所有需要 `MySql.Data` 的插件都使用 v8.0.33

---

## 🚀 自动依赖管理（新功能）

### 配置自动依赖管理

在 `nethergate-config.yaml` 中启用：

```yaml
plugins:
  dependency_management:
    enabled: true                # 启用依赖管理
    auto_download: true          # 自动下载缺失的依赖（从 NuGet）
    
    # NuGet 源列表（按顺序尝试）
    nuget_sources:
      - "https://api.nuget.org/v3/index.json"          # 官方源
      # - "https://nuget.cdn.azure.cn/v3/index.json"  # 国内镜像（可选）
    
    download_timeout: 60         # 下载超时时间（秒）
    verify_hash: true            # 验证下载文件的哈希值
    
    # 版本冲突解决策略：
    # - highest: 使用最高版本（推荐）
    # - lowest: 使用最低兼容版本
    # - fail: 报错，让用户手动处理
    conflict_resolution: highest
    
    show_conflict_report: true   # 显示冲突报告
```

### 自动下载流程

当 NetherGate 启动并加载插件时：

1. **扫描插件**：读取所有插件的 `plugin.json` 中的 `library_dependencies`
2. **冲突检测**：检测不同插件之间的版本冲突
3. **冲突解决**：根据配置的策略自动解决冲突
4. **下载依赖**：从 NuGet 下载缺失的依赖到 `lib/` 目录
5. **加载插件**：使用统一的依赖版本加载所有插件

### 版本冲突解决示例

假设有两个插件：

**PluginA** 的 `plugin.json`：
```json
{
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.3",
      "location": "lib"
    }
  ]
}
```

**PluginB** 的 `plugin.json`：
```json
{
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.5",
      "location": "lib"
    }
  ]
}
```

**冲突解决过程**（策略：`highest`）：

```
检查插件依赖...
依赖冲突报告:
====================

依赖: Newtonsoft.Json
冲突的版本要求:
  - PluginA: 13.0.3
  - PluginB: 13.0.5
解决方案: 使用版本 13.0.5

✓ 解决方案: 使用版本 13.0.5
正在下载 NuGet 包: Newtonsoft.Json 13.0.5
✓ 下载成功: Newtonsoft.Json
✓ 所有依赖都已就绪
```

**最终结果**：
- NetherGate 从 NuGet 下载 `Newtonsoft.Json 13.0.5` 到 `lib/` 目录
- PluginA 和 PluginB 都使用这个版本

### 版本要求格式

在 `plugin.json` 中，`version` 字段支持多种格式：

| 格式 | 说明 | 示例 |
|------|------|------|
| 精确版本 | 完全匹配 | `"13.0.3"` |
| 最低版本 | 大于等于 | `">=13.0.0"` |
| 版本范围 | 区间匹配 | `"[13.0.0, 14.0.0)"` |
| 任意版本 | 使用最新版本 | `""` 或不设置 |

### 手动依赖管理

如果你不想使用自动依赖管理，可以禁用它：

```yaml
plugins:
  dependency_management:
    enabled: false
```

然后手动将 DLL 文件放入 `lib/` 或插件目录。

---

## 📚 最佳实践

### ✅ 推荐做法

#### 1. **使用自动依赖管理（推荐）**

在 `plugin.json` 中声明依赖，让 NetherGate 自动下载：

```json
{
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": ">=13.0.0",
      "location": "lib"
    }
  ]
}
```

**优点**:
- 无需手动下载 DLL
- 自动解决版本冲突
- 简化插件分发

#### 2. **常用库放 lib/**

将多个插件都需要的库放在 `lib/` 中，避免重复：

```
lib/
├── Newtonsoft.Json.dll     # 所有插件共享（自动下载）
├── Serilog.dll
└── MySql.Data.dll
```

**优点**:
- 节省磁盘空间
- 统一版本管理
- 减少加载时间

#### 3. **特殊版本放插件目录**

如果插件需要特定版本的库，放在插件自己的文件夹中：

```
plugins/legacy-plugin/
├── plugin.json
├── LegacyPlugin.dll
└── OldLibrary.dll    # 旧版本库
```

#### 3. **声明依赖到 plugin.json**

始终在 `plugin.json` 中声明依赖，方便管理：

```json
{
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.3",
      "location": "lib"
    }
  ]
}
```

### ❌ 不推荐的做法

#### 1. **不要在插件中包含 NetherGate.API.dll**

❌ **错误**:
```
plugins/my-plugin/
├── plugin.json
├── MyPlugin.dll
└── NetherGate.API.dll    # 不要这样做！
```

✅ **正确**: NetherGate 核心 API 会自动共享，无需包含。

#### 2. **避免在 lib/ 中放置特定版本**

如果只有一个插件需要某个库，不要放在 `lib/` 中。

#### 3. **不要混用不同版本的 .NET 库**

确保所有依赖都针对相同的 .NET 目标框架（如 `net9.0`）。

---

## 🛠️ 开发工作流

### 步骤 1: 确定依赖

创建插件时，确定需要哪些外部库：

```csharp
using Newtonsoft.Json;  // 需要 JSON 库
using MySql.Data.MySqlClient;  // 需要 MySQL 驱动
```

### 步骤 2: 选择放置位置

- 如果是常用库（JSON、日志等）→ 放 `lib/`
- 如果是插件特有库 → 放插件文件夹

### 步骤 3: 配置 .csproj

在插件项目中配置依赖复制：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <!-- 确保依赖 DLL 被复制到输出目录 -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <!-- NuGet 包引用 -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="MySql.Data" Version="8.0.33" />
    
    <!-- NetherGate API 引用（不要复制到输出） -->
    <Reference Include="NetherGate.API">
      <HintPath>..\..\NetherGate.API.dll</HintPath>
      <Private>false</Private>  <!-- 重要：不复制 API DLL -->
    </Reference>
  </ItemGroup>
</Project>
```

### 步骤 4: 更新 plugin.json

```json
{
  "id": "com.example.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "main": "MyPlugin.MyPluginMain",
  
  "library_dependencies": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.3",
      "location": "lib"
    },
    {
      "name": "MySql.Data",
      "version": "8.0.33",
      "location": "auto"
    }
  ]
}
```

### 步骤 5: 组织文件

**如果使用 lib/**:
```
NetherGate/
├── lib/
│   ├── Newtonsoft.Json.dll
│   └── MySql.Data.dll
└── plugins/my-plugin/
    ├── plugin.json
    └── MyPlugin.dll
```

**如果使用本地依赖**:
```
NetherGate/
└── plugins/my-plugin/
    ├── plugin.json
    ├── MyPlugin.dll
    ├── Newtonsoft.Json.dll
    └── MySql.Data.dll
```

---

## 🔍 调试和诊断

### 启用依赖加载日志

在 `nethergate-config.json` 中设置日志级别为 `Trace`:

```json
{
  "logging": {
    "level": "Trace"
  }
}
```

**日志输出示例**:
```
[TRACE] [PluginLoadContext] [层1] 从全局 lib/ 加载: Newtonsoft.Json
[TRACE] [PluginLoadContext] [层2] 从插件目录加载: SpecialLib
[TRACE] [PluginLoadContext] [层3] 使用默认加载器: System.Text.Json
```

### 常见问题

#### 问题 1: 找不到依赖 DLL

**错误信息**:
```
Could not load file or assembly 'Newtonsoft.Json, Version=13.0.0.0'
```

**解决方法**:
1. 检查 DLL 是否存在于 `lib/` 或插件文件夹
2. 检查 `plugin.json` 中是否声明了依赖
3. 确认 DLL 版本匹配

#### 问题 2: 版本冲突

**错误信息**:
```
The located assembly's manifest definition does not match the assembly reference
```

**解决方法**:
- 将冲突的依赖移到 `lib/` 统一管理
- 或者在 `plugin.json` 中设置 `location: "local"` 使用插件自己的版本

#### 问题 3: 加载了错误的版本

**排查步骤**:
1. 启用 `Trace` 日志查看加载路径
2. 检查是否在 `lib/` 和插件文件夹中都有同名 DLL
3. 记住：`lib/` 中的 DLL **始终优先**

---

## 📖 示例场景

### 场景 1: 多个插件使用 JSON 库

**目标**: 3 个插件都需要 Newtonsoft.Json

**最佳方案**:
```
lib/
  └── Newtonsoft.Json.dll  (v13.0.3)

plugins/
├── plugin-a/
│   ├── plugin.json  → library_dependencies: [{name: "Newtonsoft.Json", location: "lib"}]
│   └── PluginA.dll
├── plugin-b/
│   ├── plugin.json  → library_dependencies: [{name: "Newtonsoft.Json", location: "lib"}]
│   └── PluginB.dll
└── plugin-c/
    ├── plugin.json  → library_dependencies: [{name: "Newtonsoft.Json", location: "lib"}]
    └── PluginC.dll
```

**结果**: 所有插件共享 `lib/` 中的单个 DLL，节省空间和加载时间。

### 场景 2: 插件需要特定版本的库

**目标**: 插件 A 需要 MySql.Data v8.0，插件 B 需要 v8.2

**最佳方案**:
```
plugins/
├── plugin-a/
│   ├── plugin.json  → library_dependencies: [{name: "MySql.Data", version: "8.0.33", location: "local"}]
│   ├── PluginA.dll
│   └── MySql.Data.dll  (v8.0.33)
│
└── plugin-b/
    ├── plugin.json  → library_dependencies: [{name: "MySql.Data", version: "8.2.0", location: "local"}]
    ├── PluginB.dll
    └── MySql.Data.dll  (v8.2.0)
```

**结果**: 每个插件使用自己的版本，互不冲突。

### 场景 3: 混合使用 lib/ 和本地依赖

**目标**: 插件需要 2 个库，1 个共享，1 个私有

**最佳方案**:
```
lib/
  └── Newtonsoft.Json.dll    # 共享

plugins/my-plugin/
├── plugin.json
│   → library_dependencies: [
│       {name: "Newtonsoft.Json", location: "lib"},
│       {name: "PrivateLib", location: "local"}
│     ]
├── MyPlugin.dll
└── PrivateLib.dll            # 私有
```

**结果**: JSON 库共享，PrivateLib 独立。

---

## 🚀 高级主题

### 自定义依赖加载逻辑

如果你需要更复杂的依赖加载逻辑，可以在插件中实现：

```csharp
public class MyPlugin : IPlugin
{
    public Task OnLoadAsync()
    {
        // 注册自定义程序集解析
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            // 自定义加载逻辑
            return null;
        };
        
        return Task.CompletedTask;
    }
}
```

### 延迟加载依赖

对于大型依赖库，可以延迟加载：

```csharp
private Assembly? _heavyLibrary;

public void UseHeavyLibrary()
{
    if (_heavyLibrary == null)
    {
        _heavyLibrary = Assembly.LoadFrom("path/to/HeavyLibrary.dll");
    }
    
    // 使用 _heavyLibrary
}
```

---

## 📚 相关文档

- [插件开发指南](./PLUGIN_DEVELOPMENT.md)
- [项目结构说明](./PROJECT_STRUCTURE.md)
- [配置文件参考](./CONFIGURATION.md)

---

**最后更新**: 2025-10-04
