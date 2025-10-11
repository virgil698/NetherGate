# JavaScript 插件接口兼容性分析

## 📊 接口覆盖率统计

### 当前状态

| 类别 | C# 接口数 | JavaScript SDK 已实现 | 覆盖率 |
|------|----------|----------------|--------|
| **核心功能** | 7 | 2 | ⚠️ 29% |
| **服务器交互** | 5 | 0 | ❌ 0% |
| **数据操作** | 10 | 0 | ❌ 0% |
| **游戏功能** | 8 | 0 | ❌ 0% |
| **文件系统** | 3 | 0 | ❌ 0% |
| **高级功能** | 9 | 0 | ❌ 0% |
| **总计** | **42** | **2** | **5%** |

---

## ✅ 已实现的接口（核心功能）

### 1. **基础框架**
- ✅ `Plugin` - 插件基类
- ✅ `PluginInfo` - 插件元数据

### 2. **日志系统**
- ✅ `Logger` - 日志记录器（封装到 `console`）
- ✅ `LogLevel` - 日志级别

### 3. **事件系统**
- ✅ `EventBus` - 事件总线
- ⚠️ 事件类型 - 需要 JavaScript 定义

### 4. **命令系统**
- ❌ `CommandRegistry` - 未实现
- ❌ `CommandContext` - 未实现

### 5. **RCON**
- ❌ `RconClient` - 未实现
- ❌ `RconResult` - 未实现

### 6. **调度器**
- ❌ `Scheduler` - 未实现

### 7. **配置管理**
- ❌ `ConfigManager` - 未实现

---

## ⚠️ 部分实现的接口（服务器交互）

| C# 接口 | JavaScript SDK | 状态 | 优先级 |
|---------|-----------|------|--------|
| `IRconClient` | ❌ 未实现 | 缺失 | **高** |
| `IServerCommandExecutor` | ❌ 未实现 | 缺失 | 高 |
| `ISmpApi` | ❌ 未实现 | 缺失 | **高** |
| `IWebSocketServer` | ❌ 未实现 | 缺失 | **中** |
| `INetworkEventListener` | ❌ 未实现 | 缺失 | 低 |

---

## ❌ 未实现的接口（数据操作）

### 数据读取
- ❌ `IPlayerDataReader` - 玩家数据读取
- ❌ `IWorldDataReader` - 世界数据读取
- ❌ `IBlockDataReader` - 方块数据读取
- ❌ `IPlayerProfileApi` - 玩家档案 API
- ❌ `ITagApi` - 标签系统 API

### 数据写入
- ❌ `INbtDataWriter` - NBT 数据写入
- ❌ `IBlockDataWriter` - 方块数据写入
- ❌ `IItemComponentWriter` - 物品组件写入
- ❌ `IItemComponentReader` - 物品组件读取
- ❌ `IItemComponentConverter` - 物品组件转换

**影响**: 无法读取玩家数据、世界数据、操作 NBT

---

## ❌ 未实现的接口（游戏功能）

### 游戏显示
- ❌ `IGameDisplayApi` - BossBar/Title/ActionBar
- ❌ `IScoreboardApi` - 计分板系统

### 权限和安全
- ❌ `IPermissionManager` - 权限管理器

### 游戏工具
- ❌ `IGameUtilities` - 游戏工具类
- ❌ `ICommandSequence` - 命令序列
- ❌ `IFireworkBuilder` - 烟花构建器

### 音频
- ❌ `IMusicPlayer` - 音乐播放器

### 国际化
- ❌ `II18nService` - 国际化服务

**影响**: 无法使用游戏内 UI、权限系统、高级游戏功能

---

## ❌ 未实现的接口（文件系统）

- ❌ `IFileWatcher` - 文件监视器
- ❌ `IServerFileAccess` - 服务器文件访问
- ❌ `IBackupManager` - 备份管理器

**影响**: 无法监视文件变化、管理备份

---

## ❌ 未实现的接口（高级功能）

### 分析和追踪
- ❌ `IStatisticsTracker` - 统计追踪器
- ❌ `IAdvancementTracker` - 成就追踪器
- ❌ `ILeaderboardSystem` - 排行榜系统

### 插件通信
- ❌ `IPluginMessenger` - 插件消息传递
- ❌ `IPluginContext` - 插件上下文
- ❌ `DistributedPluginBus` - 分布式插件总线

### 监控
- ❌ `IPerformanceMonitor` - 性能监控
- ❌ `IServerMonitor` - 服务器监控
- ❌ `IServerQuery` - 服务器查询

**影响**: 无法使用高级监控、跨插件通信功能

---

## 🎯 推荐实现优先级

### 🔴 优先级 1 - 核心服务接口（必需）

这些接口是大多数插件的基础需求：

```typescript
// src/NetherGate.Script/Wrappers/RconClientWrapper.cs
public class RconClientWrapper
{
    /// <summary>
    /// 执行 RCON 命令
    /// </summary>
    public Task<RconResult> Execute(string command) { }
    
    /// <summary>
    /// 批量执行命令
    /// </summary>
    public Task<RconResult[]> ExecuteBatch(string[] commands) { }
    
    /// <summary>
    /// 检查连接状态
    /// </summary>
    public bool IsConnected() { }
}

// src/NetherGate.Script/Wrappers/CommandRegistryWrapper.cs
public class CommandRegistryWrapper
{
    /// <summary>
    /// 注册命令
    /// </summary>
    public void Register(string name, object options, Func<object, Task> handler) { }
    
    /// <summary>
    /// 取消注册命令
    /// </summary>
    public void Unregister(string name) { }
}

// src/NetherGate.Script/Wrappers/SchedulerWrapper.cs
public class SchedulerWrapper
{
    /// <summary>
    /// 延迟执行任务
    /// </summary>
    public string ScheduleDelayed(int delayMs, Action callback) { }
    
    /// <summary>
    /// 周期执行任务
    /// </summary>
    public string ScheduleRepeating(int intervalMs, Action callback) { }
    
    /// <summary>
    /// 取消任务
    /// </summary>
    public void Cancel(string taskId) { }
}
```

### 🟠 优先级 2 - 常用游戏功能（重要）

大多数游戏插件需要的功能：

```typescript
// src/NetherGate.Script/Wrappers/PlayerDataReaderWrapper.cs
public class PlayerDataReaderWrapper
{
    /// <summary>
    /// 读取玩家数据
    /// </summary>
    public Task<object> ReadPlayerData(string playerUuid) { }
    
    /// <summary>
    /// 列出所有玩家
    /// </summary>
    public string[] ListPlayers() { }
}

// src/NetherGate.Script/Wrappers/ScoreboardApiWrapper.cs
public class ScoreboardApiWrapper
{
    /// <summary>
    /// 创建目标
    /// </summary>
    public Task CreateObjective(string name, string criterion) { }
    
    /// <summary>
    /// 设置分数
    /// </summary>
    public Task SetScore(string objective, string target, int score) { }
}

// src/NetherGate.Script/Wrappers/PermissionManagerWrapper.cs
public class PermissionManagerWrapper
{
    /// <summary>
    /// 检查权限
    /// </summary>
    public bool HasPermission(string player, string permission) { }
    
    /// <summary>
    /// 授予权限
    /// </summary>
    public Task GrantPermission(string player, string permission) { }
}
```

### 🟡 优先级 3 - SMP 和高级功能（可选）

特定用途的高级功能：

```typescript
// src/NetherGate.Script/Wrappers/SmpApiWrapper.cs
public class SmpApiWrapper
{
    /// <summary>
    /// 获取在线玩家
    /// </summary>
    public Task<object[]> GetPlayers() { }
    
    /// <summary>
    /// 执行命令
    /// </summary>
    public Task ExecuteCommand(string command) { }
    
    /// <summary>
    /// 获取服务器状态
    /// </summary>
    public Task<string> GetServerStatus() { }
}

// src/NetherGate.Script/Wrappers/WebSocketServerWrapper.cs
public class WebSocketServerWrapper
{
    /// <summary>
    /// 广播消息
    /// </summary>
    public Task Broadcast(object data) { }
    
    /// <summary>
    /// 发送给特定客户端
    /// </summary>
    public Task Send(string clientId, object data) { }
}

// src/NetherGate.Script/Wrappers/ConfigManagerWrapper.cs
public class ConfigManagerWrapper
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    public object Get(string key) { }
    
    /// <summary>
    /// 设置配置值
    /// </summary>
    public void Set(string key, object value) { }
    
    /// <summary>
    /// 保存配置
    /// </summary>
    public Task Save() { }
}
```

### 🟢 优先级 4 - 专用工具（低优先级）

特殊场景的工具类：

```typescript
// src/NetherGate.Script/Wrappers/GameDisplayApiWrapper.cs
public class GameDisplayApiWrapper
{
    /// <summary>
    /// 显示标题
    /// </summary>
    public Task ShowTitle(string player, string title, string subtitle) { }
    
    /// <summary>
    /// 显示动作栏
    /// </summary>
    public Task ShowActionBar(string player, string text) { }
}

// src/NetherGate.Script/Wrappers/NbtDataWriterWrapper.cs
public class NbtDataWriterWrapper
{
    /// <summary>
    /// 写入 NBT 数据
    /// </summary>
    public Task Write(string path, object data) { }
}
```

---

## 💡 实施建议

### 方案 A：渐进式实现（推荐）

**优点**:
- 快速推出可用版本
- 根据实际需求迭代
- 降低初期维护成本

**实施步骤**:
1. **v1.0**: 基础框架 + Logger + EventBus（已完成 ✅）
2. **v1.1**: 添加优先级 1 接口（RCON、命令、调度器）
3. **v1.2**: 添加优先级 2 接口（数据读取、游戏功能）
4. **v2.0**: 添加优先级 3-4 接口（完整兼容）

### 方案 B：完全兼容

**优点**:
- JavaScript 插件功能与 C# 完全对等
- 开发者体验一致

**缺点**:
- 开发和测试工作量大
- 某些接口在 JavaScript 中使用率可能很低
- 维护成本高

---

## 🔍 特殊考虑

### 1. **接口适配性**

某些 C# 接口需要特殊适配才能在 JavaScript 中使用：

```csharp
// C# - 使用泛型和复杂类型
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : Event;
    Task PublishAsync<T>(T @event) where T : Event;
}

// JavaScript Wrapper - 需要简化
public class EventBusWrapper
{
    // 使用 object 类型和类型名称字符串
    public void Subscribe(string eventType, Action<object> handler) { }
    public Task Publish(object eventData) { }
}
```

### 2. **Jint 引擎限制**

- Jint 3.x 对某些 .NET 特性支持有限（如复杂的泛型、ref/out 参数）
- 性能敏感的接口可能不适合 JavaScript（如循环密集型操作）
- 异步操作需要特殊处理（Promise 桥接）
- 某些 .NET 类型在 JavaScript 中需要转换（如 DateTime、Guid）

### 3. **类型转换挑战**

JavaScript 和 C# 之间的数据转换需要特别注意：

```typescript
// JavaScript 中的对象
const event = {
    playerName: "Steve",
    timestamp: Date.now()
};

// 需要转换为 C# 对象
// C# Wrapper 中需要处理 JsValue -> CLR 类型的转换
```

### 4. **使用频率预测**

基于 C# 和 Python 插件使用统计，预计 JavaScript 插件的接口使用率：

| 接口 | 预计使用率 | JavaScript 优先级 |
|------|--------|--------------|
| Logger | 100% | ✅ 已实现 |
| EventBus | 95% | ✅ 已实现 |
| RconClient | 85% | 🔴 高 |
| CommandRegistry | 80% | 🔴 高 |
| Scheduler | 75% | 🔴 高 |
| PlayerDataReader | 70% | 🟠 中 |
| ScoreboardApi | 60% | 🟠 中 |
| ConfigManager | 55% | 🟡 中 |
| GameDisplayApi | 50% | 🟠 中 |
| SmpApi | 40% | 🟡 中低 |
| PermissionManager | 35% | 🟠 中 |
| WebSocketServer | 30% | 🟡 低 |
| 其他 | <30% | 🟢 低 |

---

## 📝 结论和建议

### 当前状态
⚠️ **基础框架已就绪** - JavaScript 插件可以实现基本的日志记录和事件监听，但缺少与服务器交互的核心功能

### 短期建议（v1.1 - 2-3周内）
🔴 **补充核心服务接口** - 实现 `RconClient`、`CommandRegistry`、`Scheduler`，满足 70% 插件需求

### 中期建议（v1.2 - 1-2个月内）
🟠 **添加数据和游戏功能** - 实现 `PlayerDataReader`、`ScoreboardApi`、`PermissionManager`、`ConfigManager`，覆盖 90% 需求

### 长期建议（v2.0 - 3-6个月内）
🟡 **完整接口兼容** - 根据用户反馈实现剩余接口，达到与 Python 插件相似的功能覆盖率

### 不推荐实现
- `ICommandInterceptor` - 底层接口，JavaScript 性能不适合
- `IPerformanceMonitor` - 性能敏感，C# 更合适
- 某些内部 API - 仅供 NetherGate 核心使用
- 深度依赖 .NET 反射的接口 - Jint 限制

---

## 📊 实现成本估算

| 阶段 | 新增接口数 | 代码量（估算） | 测试工作量 | 预估时间 |
|------|----------|--------|-----------|---------|
| **当前** | 2 | ~500 行 | 基础测试 | ✅ 已完成 |
| **v1.1** | +3 | ~800 行 | 中等 | 2-3 周 |
| **v1.2** | +4 | ~1000 行 | 中等 | 3-4 周 |
| **v2.0** | +5 | ~800 行 | 高 | 4-6 周 |
| **总计** | 14 | ~3100 行 | - | 9-13 周 |

**注意**: JavaScript 插件支持的目标不是 100% 功能对等，而是提供**快速原型开发**和**轻量级脚本**的能力。

---

## 🎯 最终建议

### 推荐方案：**渐进式实现 + 明确定位**

**理由**:
1. 当前 5% 覆盖率只能满足**非常基础的日志和事件处理**
2. 补充优先级 1 接口后可达到 **24% 覆盖率**，满足 **50% 的轻量级脚本需求**
3. 补充优先级 1-2 接口后可达到 **43% 覆盖率**，满足 **80% 的常见使用场景**
4. JavaScript 插件定位是"快速开发、轻量级脚本"，不需要所有 C# 的企业级功能

### JavaScript vs Python vs C# 插件定位

| 特性 | JavaScript | Python | C# |
|------|-----------|--------|-----|
| **目标用户** | Web 开发者 | 脚本编写者 | 专业开发者 |
| **开发速度** | 快 | 快 | 中 |
| **性能** | 中 | 中 | 高 |
| **类型安全** | TypeScript 支持 | 有限 | 完全 |
| **生态系统** | npm 包（受限） | PyPI 包（受限） | 完整 .NET 生态 |
| **功能覆盖** | 目标 40-50% | 目标 60-70% | 100% |
| **使用场景** | 快速脚本、Web 集成 | 数据处理、自动化 | 生产级应用 |

### 下一步行动
1. ✅ 发布 v1.0（当前版本）- 基础框架
2. 🔴 实现优先级 1 接口（RCON、命令、调度器）
3. 🟠 收集用户反馈，优化 API 设计
4. 🟡 根据实际需求添加更多接口

### 文档策略
- ✅ 明确标注 JavaScript SDK 与 C# API 的差异
- ✅ 提供接口映射表和类型定义（TypeScript `.d.ts`）
- ⚠️ 为未实现的功能提供替代方案或说明
- ⚠️ 提供迁移指南（JavaScript/TypeScript → C#）

---

**最后更新**: 2025-10-11  
**维护者**: NetherGate Team  
**反馈**: https://github.com/your-org/NetherGate/issues


