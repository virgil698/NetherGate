# 贡献指南

感谢你对 NetherGate 项目的关注！我们欢迎各种形式的贡献。

---

## 🤝 如何贡献

### 报告 Bug

如果你发现了 Bug，请：

1. 检查 [Issues](https://github.com/YourName/NetherGate/issues) 确认是否已被报告
2. 如果没有，创建新 Issue 并提供：
   - 问题描述
   - 重现步骤
   - 期望行为
   - 实际行为
   - 环境信息（.NET版本、Minecraft版本等）
   - 相关日志

### 提出功能建议

欢迎提出新功能想法！请：

1. 在 [Issues](https://github.com/YourName/NetherGate/issues) 或 [Discussions](https://github.com/YourName/NetherGate/discussions) 中描述你的想法
2. 说明：
   - 功能用途
   - 使用场景
   - 预期效果
   - 可能的实现方式（可选）

### 改进文档

文档改进包括：
- 修正错别字
- 改善表述
- 补充示例
- 翻译文档
- 添加新章节

直接提交 Pull Request 即可！

### 提交代码

#### 开发环境设置

```bash
# 1. Fork 并 clone 仓库
git clone https://github.com/YourName/NetherGate.git
cd NetherGate

# 2. 安装 .NET 9.0 SDK
# 下载地址: https://dotnet.microsoft.com/download

# 3. 恢复依赖
dotnet restore

# 4. 构建项目
dotnet build

# 5. 运行测试
dotnet test
```

#### 代码规范

**C# 编码规范：**
- 遵循 [C# 编码约定](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 4 个空格缩进
- 使用 PascalCase 命名公共成员
- 使用 camelCase 命名私有成员
- 添加 XML 文档注释

**示例：**

```csharp
/// <summary>
/// 表示一个插件实例
/// </summary>
public class Plugin : IPlugin
{
    private readonly ILogger _logger;
    
    /// <summary>
    /// 初始化插件
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public Plugin(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 加载插件
    /// </summary>
    public async Task OnLoadAsync()
    {
        _logger.Info("插件正在加载");
        await Task.CompletedTask;
    }
}
```

#### 提交规范

**Commit Message 格式：**

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type 类型：**
- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式（不影响功能）
- `refactor`: 重构
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建/工具相关

**示例：**

```
feat(plugin): 添加插件热重载功能

- 实现插件卸载逻辑
- 添加插件重载命令
- 更新插件管理器

Closes #123
```

#### Pull Request 流程

1. **创建分支**
```bash
git checkout -b feature/your-feature-name
```

2. **开发并提交**
```bash
git add .
git commit -m "feat: 你的功能描述"
```

3. **保持同步**
```bash
git fetch upstream
git rebase upstream/main
```

4. **推送分支**
```bash
git push origin feature/your-feature-name
```

5. **创建 Pull Request**
   - 填写 PR 模板
   - 描述改动内容
   - 关联相关 Issue
   - 等待 Code Review

#### Code Review 标准

你的 PR 需要：
- ✅ 通过所有测试
- ✅ 遵循代码规范
- ✅ 包含必要的文档
- ✅ 不引入新的警告
- ✅ 有清晰的 commit history

---

## 🧪 测试指南

### 单元测试

```csharp
[Fact]
public async Task OnLoadAsync_ShouldInitializePlugin()
{
    // Arrange
    var logger = new MockLogger();
    var plugin = new MyPlugin(logger);
    
    // Act
    await plugin.OnLoadAsync();
    
    // Assert
    Assert.True(plugin.IsLoaded);
}
```

### 集成测试

```csharp
[Fact]
public async Task SmpClient_ShouldConnectToServer()
{
    // Arrange
    var config = new SmpConfig
    {
        Host = "localhost",
        Port = 25575,
        Secret = "test-token"
    };
    var client = new SmpClient();
    
    // Act
    var connected = await client.ConnectAsync(config);
    
    // Assert
    Assert.True(connected);
}
```

---

## 📝 文档指南

### 文档结构

```
docs/
├── API_DESIGN.md              # API 设计
├── SMP_INTERFACE.md           # SMP 接口
├── RCON_INTEGRATION.md        # RCON 集成
├── PLUGIN_DEPENDENCIES.md     # 插件依赖
├── PLUGIN_PROJECT_STRUCTURE.md# 插件结构
├── SERVER_PROCESS.md          # 服务器进程
├── PROJECT_STRUCTURE.md       # 项目结构
├── SAMPLES_PROJECT.md         # 示例项目
└── FUTURE_EXTENSIBILITY.md    # 未来扩展
```

### 文档规范

- 使用 Markdown 格式
- 添加目录导航
- 包含代码示例
- 使用清晰的标题层级
- 添加必要的图表

---

## 🏷️ 发布流程

### 版本号规范

遵循 [语义化版本](https://semver.org/lang/zh-CN/)：

- `MAJOR.MINOR.PATCH`
- `1.0.0` - 正式版
- `1.1.0` - 添加新功能
- `1.1.1` - Bug 修复
- `2.0.0` - 破坏性变更

### 发布清单

- [ ] 更新版本号
- [ ] 更新 CHANGELOG
- [ ] 运行所有测试
- [ ] 更新文档
- [ ] 创建 Git Tag
- [ ] 发布 Release
- [ ] 发布 NuGet 包（NetherGate.API）

---

## 💬 社区

### 行为准则

- 尊重他人
- 建设性反馈
- 包容多元观点
- 专业的交流方式

### 获取帮助

- 💬 [GitHub Discussions](https://github.com/YourName/NetherGate/discussions) - 提问讨论
- 🐛 [GitHub Issues](https://github.com/YourName/NetherGate/issues) - 报告问题
- 📧 Email - 私密问题

---

## 🎯 开发优先级

当前开发重点（按优先级）：

### P0 - 核心功能
- [ ] SMP 客户端实现
- [ ] RCON 客户端实现
- [ ] 日志监听器
- [ ] 插件加载系统

### P1 - 基础功能
- [ ] 事件系统
- [ ] 命令系统
- [ ] 配置管理
- [ ] 日志系统

### P2 - 增强功能
- [ ] 插件热重载
- [ ] 依赖管理
- [ ] 性能监控
- [ ] 插件市场

### P3 - 优化
- [ ] 性能优化
- [ ] 文档完善
- [ ] 示例插件
- [ ] 国际化支持

---

## 📊 项目状态

- **当前版本**: 0.1.0-alpha
- **开发阶段**: Phase 1 - 基础框架
- **下一里程碑**: Phase 2 - 协议实现

---

## 🙏 致谢

感谢所有贡献者！

特别感谢：
- MCDReforged 项目的设计灵感
- .NET 社区的技术支持
- 所有提供反馈的用户

---

**记住：每一个贡献都很重要，无论大小！** ❤️

