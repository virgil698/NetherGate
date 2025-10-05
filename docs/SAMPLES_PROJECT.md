# NetherGate-Samples 示例插件项目

本文档说明 NetherGate-Samples 独立示例插件项目的结构和用途。

---

## 📋 项目概述

**NetherGate-Samples** 是一个独立的仓库/项目，包含 NetherGate 插件系统的完整示例代码。

**独立管理的原因**：
- ✅ 保持主项目代码简洁
- ✅ 示例代码可以单独更新和维护
- ✅ 用户可以选择性下载学习
- ✅ 便于示例代码的版本管理

---

## 📦 项目结构

### 仓库结构

```
NetherGate-Samples/
├── README.md                        # 项目说明
├── .gitignore                       # Git 忽略文件
├── Directory.Build.props            # 共享构建属性
│
├── HelloWorld/                      # 示例 1: Hello World
│   ├── HelloWorldPlugin.csproj
│   ├── src/                         # 源代码（类似 src/main/java）
│   │   └── HelloWorldPlugin.cs
│   ├── resources/                   # 资源文件（类似 src/main/resources）
│   │   ├── plugin.json
│   │   └── lang/
│   │       ├── en_US.json
│   │       └── zh_CN.json
│   └── README.md
│
├── PlayerWelcome/                   # 示例 2: 玩家欢迎
│   ├── PlayerWelcomePlugin.csproj
│   ├── src/                         # 源代码
│   │   ├── PlayerWelcomePlugin.cs
│   │   ├── Events/
│   │   │   └── PlayerEventListener.cs
│   │   └── Models/
│   │       └── WelcomeConfig.cs
│   ├── resources/                   # 资源文件
│   │   ├── plugin.json
│   │   ├── config.json              # 默认配置模板
│   │   └── lang/
│   │       ├── en_US.json
│   │       └── zh_CN.json
│   └── README.md
│
├── AdminTools/                      # 示例 3: 管理工具
│   ├── AdminToolsPlugin.csproj
│   ├── src/                         # 源代码
│   │   ├── AdminToolsPlugin.cs
│   │   └── Commands/
│   │       ├── BanCommand.cs
│   │       ├── KickCommand.cs
│   │       └── WhitelistCommand.cs
│   ├── resources/                   # 资源文件
│   │   ├── plugin.json
│   │   └── lang/
│   │       ├── en_US.json
│   │       └── zh_CN.json
│   └── README.md
│
└── Common/                          # 共享代码（可选）
    ├── Common.csproj
    └── src/
        └── Utilities.cs
```

### 项目结构说明

每个示例插件都采用类似 **Maven/Gradle** 的目录布局：

- **src/**：源代码目录（类似 Java 的 `src/main/java`）
  - 按功能组织：Commands/、Events/、Services/ 等
  
- **resources/**：资源文件目录（类似 Java 的 `src/main/resources`）
  - `plugin.json`：插件元数据（必需）
  - `config.json`：默认配置模板（可选）
  - `lang/`：多语言文件目录

详细的项目结构指南请参考：[插件项目结构文档](PLUGIN_PROJECT_STRUCTURE.md)

---

## 📚 示例插件说明

### 1. HelloWorld

**难度**: ⭐

**目的**: 演示最基本的插件结构

**功能**:
- 插件加载和卸载
- 基本的日志输出
- 简单的命令注册

**适合**: 
- 第一次接触 NetherGate 的开发者
- 了解插件生命周期

**代码片段**:
```csharp
public class HelloWorldPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        Logger.Info("Hello, NetherGate!");
    }
}
```

---

### 2. PlayerWelcome

**难度**: ⭐⭐

**目的**: 演示事件监听和配置管理

**功能**:
- 监听玩家加入/离开事件
- 发送欢迎消息
- 读取和保存配置文件
- 自定义欢迎消息模板

**适合**:
- 需要处理服务器事件的插件
- 需要配置文件管理的插件

**学习要点**:
- 如何订阅事件
- 如何使用配置系统
- 如何调用服务器 API

---

### 3. AdminTools

**难度**: ⭐⭐⭐

**目的**: 演示完整的管理工具插件

**功能**:
- 多个管理命令（ban、kick、whitelist）
- 命令参数解析
- 权限检查
- 调用服务端管理协议 API
- 错误处理和用户反馈

**适合**:
- 开发复杂功能的插件
- 需要多个命令的插件
- 需要调用服务器管理功能

**学习要点**:
- 命令系统的高级用法
- 如何组织多个命令
- 如何使用服务端管理协议 API
- 错误处理最佳实践

---

## 🚀 快速开始

### 克隆示例项目

```bash
# 克隆示例项目
git clone https://github.com/YourName/NetherGate-Samples.git
cd NetherGate-Samples
```

### 构建示例

```bash
# 构建所有示例
dotnet build

# 构建特定示例
dotnet build HelloWorld/HelloWorldPlugin.csproj
```

### 测试示例

```bash
# 复制编译后的插件到 NetherGate
cp HelloWorld/bin/Debug/net9.0/HelloWorldPlugin.dll ../NetherGate/plugins/hello-world/
cp HelloWorld/plugin.json ../NetherGate/plugins/hello-world/

# 启动 NetherGate 测试
cd ../NetherGate
dotnet run
```

---

## 📖 开发指南

### 创建新插件（基于示例）

1. **复制示例项目**:
   ```bash
   cp -r HelloWorld MyPlugin
   cd MyPlugin
   ```

2. **修改项目文件**:
   - 重命名 `.csproj` 文件
   - 更新命名空间
   - 修改 `plugin.json`

3. **开发功能**:
   - 实现插件逻辑
   - 添加命令和事件处理
   - 编写配置文件

4. **测试**:
   ```bash
   dotnet build
   cp bin/Debug/net9.0/*.dll ../NetherGate/plugins/my-plugin/
   ```

---

## 🔗 依赖说明

### NetherGate API 依赖

所有示例插件都依赖 `NetherGate.API`:

```xml
<ItemGroup>
    <PackageReference Include="NetherGate.API" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
</ItemGroup>
```

**注意**: 
- ✅ 仅需引用 `NetherGate.API`
- ❌ 不要引用 `NetherGate.Core`
- ✅ 使用 `PrivateAssets` 和 `ExcludeAssets` 避免打包 API DLL

### 外部依赖处理

如果示例插件使用了外部库（如 Newtonsoft.Json），需要：

```xml
<PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <!-- 关键：复制所有依赖到输出目录 -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="NetherGate.API" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
    
    <!-- 外部依赖会自动复制 -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
</ItemGroup>
```

**详细的依赖管理**: 查看 [插件依赖管理文档](PLUGIN_DEPENDENCIES.md)

---

## 📝 每个示例的 README

每个示例插件都包含独立的 `README.md`，包含：

- 插件功能详细说明
- 配置文件示例
- 使用方法
- API 调用示例
- 常见问题

---

## 🤝 贡献示例

欢迎提交新的示例插件！

**示例要求**:
- 代码清晰，注释完整
- 包含完整的 `README.md`
- 演示特定的功能或最佳实践
- 可以独立编译和运行

**提交流程**:
1. Fork 示例项目
2. 创建新的示例目录
3. 编写代码和文档
4. 提交 Pull Request

---

## 📄 许可证

与 NetherGate 主项目相同（待定）

---

## 🔗 相关链接

- **主项目**: [NetherGate](https://github.com/YourName/NetherGate)
- **文档**: [开发文档](https://github.com/YourName/NetherGate/blob/main/DEVELOPMENT.md)
- **API 参考**: [API 设计](https://github.com/YourName/NetherGate/blob/main/docs/API_DESIGN.md)
- **讨论**: [GitHub Discussions](https://github.com/YourName/NetherGate/discussions)

---

**项目状态**: 🚧 开发中

**当前版本**: 与 NetherGate 主项目同步

