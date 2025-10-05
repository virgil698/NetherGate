# NetherGate 文档结构指南

本文档说明 NetherGate 项目的文档组织方式和阅读顺序。

---

## 📚 文档层次结构

```
NetherGate/
├── README.md              ⭐ 入口：项目简介、快速开始
├── DEVELOPMENT.md         🏗️ 架构：整体设计、核心模块
├── CONTRIBUTING.md        🤝 贡献：参与开发指南
│
└── docs/                  📖 详细文档（按主题分类）
    ├── README.md          📚 文档导航中心
    │
    ├── API_DESIGN.md      🔌 插件 API 完整设计
    ├── SMP_INTERFACE.md   🌐 SMP 协议详细封装
    ├── RCON_INTEGRATION.md 🎮 RCON 游戏命令集成
    │
    ├── PLUGIN_PROJECT_STRUCTURE.md  📁 插件项目结构
    ├── PLUGIN_DEPENDENCIES.md       📦 插件依赖管理
    ├── SAMPLES_PROJECT.md           💡 示例插件说明
    │
    ├── SERVER_PROCESS.md           🖥️ 服务器进程管理
    ├── PROJECT_STRUCTURE.md        🗂️ 项目目录结构
    ├── FUTURE_EXTENSIBILITY.md     🔮 未来扩展性设计
    └── FAQ.md                      ❓ 常见问题解答
```

---

## 🎯 文档定位与职责

### 根目录文档（核心入口）

#### 1. README.md
- **定位**：项目门户
- **受众**：所有人（用户、开发者、贡献者）
- **内容**：
  - 项目简介和特性
  - 快速开始（5分钟上手）
  - 功能概览
  - 文档索引

#### 2. DEVELOPMENT.md
- **定位**：架构总览
- **受众**：核心开发者、系统架构师、深度贡献者
- **内容**：
  - 整体架构设计
  - 核心模块概览（每个模块简要说明）
  - 技术选型和原因
  - 开发路线图
  - 与 MCDR 的对比
- **特点**：
  - ✅ 高度概括，提供"地图"而非"路线"
  - ✅ 每个模块只讲核心类和职责
  - ✅ 详细实现链接到 docs/ 相应文档
  - ❌ 不包含具体实现代码
  - ❌ 不重复 docs/ 的详细内容

#### 3. CONTRIBUTING.md
- **定位**：贡献指南
- **受众**：想要参与项目的开发者
- **内容**：
  - 如何报告 Bug
  - 如何提出功能建议
  - 代码规范
  - Pull Request 流程
  - 测试指南

---

### docs/ 目录（详细专题）

#### 核心 API 文档

**API_DESIGN.md**
- **定位**：插件开发者的 API 参考
- **内容**：完整的接口定义、DTO、使用示例
- **特点**：详细、可执行的代码示例

**SMP_INTERFACE.md**
- **定位**：服务端管理协议完整封装
- **内容**：所有 SMP 方法、事件、DTO、三种技术组合使用
- **特点**：最详细的协议文档，包含大量实战示例

**RCON_INTEGRATION.md**
- **定位**：RCON 集成和游戏内命令实现
- **内容**：RCON vs SMP、游戏内命令实现、完整示例

#### 插件开发文档

**PLUGIN_PROJECT_STRUCTURE.md**
- **定位**：插件项目结构指南
- **内容**：Maven/Gradle 风格的项目布局、.NET 特性应用

**PLUGIN_DEPENDENCIES.md**
- **定位**：插件依赖管理详解
- **内容**：AssemblyLoadContext、依赖隔离、NuGet 包处理

**SAMPLES_PROJECT.md**
- **定位**：示例插件说明
- **内容**：示例项目结构、如何使用示例

#### 运维和管理文档

**SERVER_PROCESS.md**
- **定位**：服务器进程管理详解
- **内容**：启动流程、配置详解、监控和日志

**PROJECT_STRUCTURE.md**
- **定位**：项目目录结构说明
- **内容**：源码结构、运行时结构、文件说明

#### 高级主题

**FUTURE_EXTENSIBILITY.md**
- **定位**：架构扩展性设计
- **内容**：如何应对 SMP 未来更新、新协议集成

**FAQ.md**
- **定位**：常见问题解答
- **内容**：故障排查、性能优化、最佳实践

**DOCUMENTATION_GUIDE.md**（本文档）
- **定位**：文档结构说明
- **内容**：文档组织方式、阅读顺序

---

## 🎓 推荐阅读路径

### 路径 1：我是新用户

```
1. README.md（了解项目）
   ↓
2. 快速开始部分（5分钟安装）
   ↓
3. docs/FAQ.md（遇到问题查这里）
```

### 路径 2：我想开发插件

```
1. README.md（了解项目）
   ↓
2. docs/API_DESIGN.md（学习 API）
   ↓
3. docs/PLUGIN_PROJECT_STRUCTURE.md（创建项目）
   ↓
4. docs/SMP_INTERFACE.md（掌握 SMP）
   ↓
5. docs/RCON_INTEGRATION.md（实现游戏内命令）
   ↓
6. docs/PLUGIN_DEPENDENCIES.md（处理依赖）
```

### 路径 3：我想了解架构

```
1. README.md（项目概览）
   ↓
2. DEVELOPMENT.md（架构总览）⭐
   ↓
3. docs/PROJECT_STRUCTURE.md（项目结构）
   ↓
4. docs/FUTURE_EXTENSIBILITY.md（扩展性设计）
```

### 路径 4：我想贡献代码

```
1. CONTRIBUTING.md（贡献指南）⭐
   ↓
2. DEVELOPMENT.md（理解架构）
   ↓
3. 选择感兴趣的模块
   ↓ 
4. 查看对应的 docs/ 详细文档
   ↓
5. 开始编码！
```

---

## 🔄 文档维护原则

### 1. 单一职责原则

每个文档只负责一个主题，避免内容重复：

- ✅ **好**: `PLUGIN_DEPENDENCIES.md` 只讲依赖管理
- ❌ **不好**: 在多个文档中重复讲依赖管理

### 2. 层次分明原则

文档有清晰的层次关系：

```
README.md        (概览 - 1分钟了解)
    ↓
DEVELOPMENT.md   (架构 - 30分钟理解)
    ↓
docs/*.md        (详细 - 深入学习)
```

### 3. 交叉引用原则

文档之间应该相互链接：

- DEVELOPMENT.md 中每个模块链接到详细文档
- 详细文档中引用相关的其他文档
- 使用相对路径：`[链接](docs/FILE.md)` 或 `[链接](../DEVELOPMENT.md)`

### 4. 保持同步原则

- 架构变化时：同步更新 DEVELOPMENT.md 和相关的 docs/
- 新增功能时：先确定属于哪个文档
- 定期检查：确保没有过时内容

---

## 📝 文档更新检查清单

当你修改代码时，需要更新哪些文档？

### 添加新功能

- [ ] 更新 README.md 的功能列表
- [ ] 在 DEVELOPMENT.md 添加模块概述
- [ ] 在 docs/ 创建或更新详细文档
- [ ] 更新 docs/README.md 的文档索引
- [ ] 如果影响插件 API，更新 API_DESIGN.md

### 修改架构

- [ ] 更新 DEVELOPMENT.md 的架构图
- [ ] 更新相关的 docs/ 详细文档
- [ ] 检查并更新交叉引用
- [ ] 更新 FUTURE_EXTENSIBILITY.md（如果涉及）

### 修复 Bug

- [ ] 在 docs/FAQ.md 添加相关问题（如果常见）
- [ ] 更新受影响的文档示例代码
- [ ] 在 CHANGELOG.md 记录修复

### 重构代码

- [ ] 检查所有提到旧代码的文档
- [ ] 更新代码示例
- [ ] 保持文档与代码一致

---

## 🆚 DEVELOPMENT.md vs docs/ 的区别

### DEVELOPMENT.md 应该包含：

✅ 整体架构图  
✅ 核心模块列表和职责  
✅ 技术选型和原因  
✅ 开发路线图  
✅ 模块间的关系  

❌ 详细的 API 定义  
❌ 完整的配置说明  
❌ 具体的实现代码  
❌ 详细的使用教程  

### docs/ 文档应该包含：

✅ 详细的 API 定义  
✅ 完整的配置说明  
✅ 具体的实现示例  
✅ 详细的使用教程  
✅ 常见问题解答  

❌ 不应该重复 DEVELOPMENT.md 的架构概述  

---

## 💡 写文档的最佳实践

### 1. 从用户角度出发

- 用户想知道"如何做"而不是"内部实现"
- 提供可执行的代码示例
- 解释"为什么"而不只是"是什么"

### 2. 使用清晰的结构

```markdown
# 标题

简短介绍（一两句话）

## 概述

详细说明

## 快速开始

立即可用的示例

## 详细说明

深入的内容

## 常见问题

Q&A
```

### 3. 代码示例规范

```csharp
// ❌ 不好：没有上下文
Server.GetPlayersAsync();

// ✅ 好：完整示例
public class MyPlugin : PluginBase
{
    public override async Task OnEnableAsync()
    {
        var players = await Server.GetPlayersAsync();
        Logger.Info($"当前在线 {players.Count} 名玩家");
    }
}
```

### 4. 使用图表和图示

- 架构图帮助理解整体
- 流程图说明步骤
- 表格对比差异

---

## 🔗 相关资源

- [Markdown 语法指南](https://www.markdownguide.org/)
- [技术文档写作最佳实践](https://github.com/google/styleguide/tree/gh-pages/docguide)
- [Write the Docs](https://www.writethedocs.org/)

---

**记住：好的文档就像好的代码一样重要！** 📖✨

