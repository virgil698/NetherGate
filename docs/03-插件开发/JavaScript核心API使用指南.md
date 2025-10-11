# JavaScript 核心 API 使用指南

本文档介绍 NetherGate JavaScript SDK 中最重要的核心功能的详细使用方法。

> **⚠️ 重要提示**
> 
> JavaScript 插件 API 通过 Jint 引擎桥接层实现，主要提供基础功能和常用操作的封装。虽然 JavaScript/TypeScript 插件开发更加简单快捷，但存在以下限制：
> 
> - **功能覆盖**：JavaScript API 仅封装了最常用的核心功能，某些高级 API 尚未实现
> - **性能开销**：JavaScript 与 .NET 之间的互操作会带来额外的性能开销
> - **引擎限制**：Jint 引擎对某些 ES6+ 特性的支持有限
> - **类型转换**：复杂的 .NET 类型可能无法直接在 JavaScript 中使用
> 
> **推荐场景**：
> - ✅ JavaScript 插件：快速原型、简单的游戏逻辑、脚本化任务、Web 开发者友好
> - ✅ .NET 插件：生产环境、高性能需求、复杂业务逻辑、深度集成、企业级应用
> 
> 如果您需要更强的控制能力、更好的性能和完整的 API 访问，强烈推荐使用 C# 开发 .NET 插件。详见 [插件开发指南](./插件开发指南.md)。

## 目录

- [Logger（日志系统）](#logger日志系统)
- [EventBus（事件系统）](#eventbus事件系统)
- [Console（控制台桥接）](#console控制台桥接)
- [插件生命周期](#插件生命周期)
- [TypeScript 支持](#typescript-支持)

---

## Logger（日志系统）

Logger 提供了标准的日志记录功能，支持不同的日志级别。

### 基础用法

```javascript
// index.js
class MyPlugin {
    constructor(logger) {
        this.logger = logger;
    }
    
    onLoad() {
        this.logger.info("插件加载中...");
    }
    
    onEnable() {
        this.logger.info("插件已启用");
        this.logger.debug("调试信息");
        this.logger.warn("警告信息");
        this.logger.error("错误信息");
    }
}

module.exports = MyPlugin;
```

### 日志级别

Logger 支持以下日志级别（从低到高）：

| 级别 | 方法 | 用途 |
|------|------|------|
| **Debug** | `logger.debug()` | 详细的调试信息，仅在开发时使用 |
| **Info** | `logger.info()` | 一般性信息，记录重要的状态变化 |
| **Warning** | `logger.warn()` | 警告信息，可能的问题但不影响运行 |
| **Error** | `logger.error()` | 错误信息，需要注意的问题 |

### 日志记录示例

```javascript
class LogExamplePlugin {
    constructor(logger) {
        this.logger = logger;
    }
    
    onEnable() {
        // 记录普通信息
        this.logger.info("服务器启动完成");
        
        // 记录调试信息（开发时启用）
        this.logger.debug(`当前配置: ${JSON.stringify(this.config)}`);
        
        // 记录警告
        if (!this.config.enabled) {
            this.logger.warn("插件功能已禁用");
        }
        
        // 记录错误
        try {
            this.someRiskyOperation();
        } catch (error) {
            this.logger.error(`操作失败: ${error.message}`);
        }
    }
    
    someRiskyOperation() {
        throw new Error("示例错误");
    }
}

module.exports = LogExamplePlugin;
```

---

## EventBus（事件系统）

EventBus 是插件间通信和响应服务器事件的核心机制。

### 基础用法

```javascript
class EventExamplePlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
    }
    
    onEnable() {
        // 订阅事件
        this.eventBus.subscribe('PlayerJoinEvent', (eventData) => {
            this.logger.info(`玩家加入: ${eventData.playerName}`);
        });
        
        this.eventBus.subscribe('ServerStartedEvent', (eventData) => {
            this.logger.info("服务器已启动");
        });
    }
    
    onDisable() {
        // 取消订阅所有事件
        this.eventBus.clearAll();
    }
}

module.exports = EventExamplePlugin;
```

### 订阅事件

```javascript
class PlayerEventPlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.handlers = [];
    }
    
    onEnable() {
        // 方法 1: 使用箭头函数（推荐）
        this.eventBus.subscribe('PlayerJoinEvent', (event) => {
            this.onPlayerJoin(event);
        });
        
        // 方法 2: 绑定 this
        const handler = this.onPlayerLeave.bind(this);
        this.eventBus.subscribe('PlayerLeaveEvent', handler);
        this.handlers.push({ event: 'PlayerLeaveEvent', handler });
    }
    
    onPlayerJoin(event) {
        const playerName = event.playerName;
        this.logger.info(`欢迎 ${playerName}!`);
    }
    
    onPlayerLeave(event) {
        const playerName = event.playerName;
        this.logger.info(`${playerName} 离开了游戏`);
    }
    
    onDisable() {
        // 清理事件订阅
        for (const { event, handler } of this.handlers) {
            this.eventBus.unsubscribe(event, handler);
        }
        this.handlers = [];
    }
}

module.exports = PlayerEventPlugin;
```

### 发布自定义事件

```javascript
class CustomEventPlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
    }
    
    onEnable() {
        // 订阅自定义事件
        this.eventBus.subscribe('CustomRewardEvent', (event) => {
            this.logger.info(`${event.playerName} 获得奖励: ${event.rewardType}`);
        });
        
        // 延迟 5 秒后发布事件
        setTimeout(() => {
            this.giveReward('Steve', 'diamond');
        }, 5000);
    }
    
    giveReward(playerName, rewardType) {
        // 发布自定义事件
        const eventData = {
            playerName: playerName,
            rewardType: rewardType,
            timestamp: Date.now()
        };
        
        this.eventBus.publish(eventData);
        this.logger.info(`已发布奖励事件`);
    }
}

module.exports = CustomEventPlugin;
```

### 常用事件类型

以下是 NetherGate 支持的常见事件类型：

| 事件名 | 触发时机 | 事件数据 |
|--------|---------|---------|
| `ServerStartedEvent` | 服务器启动完成 | `{}` |
| `ServerStoppedEvent` | 服务器停止 | `{}` |
| `PlayerJoinEvent` | 玩家加入游戏 | `{ playerName: string }` |
| `PlayerLeaveEvent` | 玩家离开游戏 | `{ playerName: string }` |
| `PlayerChatEvent` | 玩家发送聊天消息 | `{ playerName: string, message: string }` |
| `PlayerDeathEvent` | 玩家死亡 | `{ playerName: string, deathMessage: string }` |
| `PlayerAdvancementEvent` | 玩家获得成就 | `{ playerName: string, advancementId: string }` |

---

## Console（控制台桥接）

JavaScript 插件中的 `console` 对象已桥接到 NetherGate 的日志系统。

### 基础用法

```javascript
class ConsolePlugin {
    onEnable() {
        console.log("普通日志 - 映射到 Info");
        console.info("信息日志 - 映射到 Info");
        console.warn("警告日志 - 映射到 Warning");
        console.error("错误日志 - 映射到 Error");
        console.debug("调试日志 - 映射到 Debug");
    }
}

module.exports = ConsolePlugin;
```

### Console vs Logger

```javascript
class LogComparisonPlugin {
    constructor(logger) {
        this.logger = logger;
    }
    
    onEnable() {
        // 使用 console（简单、熟悉）
        console.log("Hello from console");
        
        // 使用 logger（推荐，更灵活）
        this.logger.info("Hello from logger");
        
        // 两者输出相同，但 logger 提供更好的控制
        // 例如：可以在配置中设置不同插件的日志级别
    }
}

module.exports = LogComparisonPlugin;
```

**推荐**：在插件开发中优先使用 `logger`，因为它提供了更好的配置和控制能力。

---

## 插件生命周期

JavaScript 插件支持标准的生命周期钩子。

### 生命周期方法

```javascript
class LifecyclePlugin {
    constructor(logger, eventBus) {
        // 构造函数 - 依赖注入
        this.logger = logger;
        this.eventBus = eventBus;
        this.timerId = null;
    }
    
    onLoad() {
        // 插件加载时调用（最早阶段）
        this.logger.info("插件加载中...");
        this.loadConfig();
    }
    
    onEnable() {
        // 插件启用时调用（服务器启动后）
        this.logger.info("插件已启用");
        this.registerEvents();
        this.startTasks();
    }
    
    onDisable() {
        // 插件禁用时调用（服务器关闭前）
        this.logger.info("插件正在禁用...");
        this.stopTasks();
        this.cleanup();
    }
    
    onUnload() {
        // 插件卸载时调用（最后阶段）
        this.logger.info("插件已卸载");
    }
    
    // 辅助方法
    loadConfig() {
        this.logger.debug("加载配置...");
    }
    
    registerEvents() {
        this.eventBus.subscribe('PlayerJoinEvent', (event) => {
            this.logger.info(`玩家加入: ${event.playerName}`);
        });
    }
    
    startTasks() {
        // 启动定时任务
        this.timerId = setInterval(() => {
            this.logger.debug("定时任务执行");
        }, 60000); // 每分钟执行一次
    }
    
    stopTasks() {
        // 停止定时任务
        if (this.timerId) {
            clearInterval(this.timerId);
            this.timerId = null;
        }
    }
    
    cleanup() {
        // 清理资源
        this.eventBus.clearAll();
    }
}

module.exports = LifecyclePlugin;
```

### 生命周期顺序

```
插件加载流程:
1. new LifecyclePlugin(logger, eventBus)  // 构造函数
2. onLoad()                                // 加载阶段
3. onEnable()                              // 启用阶段
   ... [插件运行中] ...
4. onDisable()                             // 禁用阶段
5. onUnload()                              // 卸载阶段
```

### 最佳实践

```javascript
class BestPracticePlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
        
        // 初始化状态
        this.isEnabled = false;
        this.resources = [];
    }
    
    onLoad() {
        // ✓ 加载配置
        // ✓ 初始化数据结构
        // ✗ 不要订阅事件（应在 onEnable）
        // ✗ 不要启动任务（应在 onEnable）
        this.logger.info("配置已加载");
    }
    
    onEnable() {
        // ✓ 订阅事件
        // ✓ 启动定时任务
        // ✓ 注册命令
        this.isEnabled = true;
        this.logger.info("插件已启用");
    }
    
    onDisable() {
        // ✓ 取消事件订阅
        // ✓ 停止定时任务
        // ✓ 保存数据
        // ✓ 清理资源
        this.isEnabled = false;
        this.cleanup();
        this.logger.info("插件已禁用");
    }
    
    onUnload() {
        // ✓ 最终清理
        // ✓ 释放所有引用
        this.logger.info("插件已卸载");
    }
    
    cleanup() {
        // 清理所有资源
        this.eventBus.clearAll();
        this.resources.forEach(resource => resource.dispose());
        this.resources = [];
    }
}

module.exports = BestPracticePlugin;
```

---

## TypeScript 支持

NetherGate 提供了完整的 TypeScript 类型定义。

### 设置 TypeScript

1. 安装 TypeScript：

```bash
npm install --save-dev typescript @types/node
```

2. 创建 `tsconfig.json`：

```json
{
  "compilerOptions": {
    "target": "ES2015",
    "module": "commonjs",
    "lib": ["ES2015"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules"]
}
```

### TypeScript 插件示例

```typescript
// src/index.ts
import { Plugin, Logger, EventBus, PluginInfo } from 'nethergate';

class MyPlugin implements Plugin {
    private logger: Logger;
    private eventBus: EventBus;
    
    constructor(logger: Logger, eventBus: EventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
    }
    
    onLoad(): void {
        this.logger.info("插件加载中...");
    }
    
    onEnable(): void {
        this.logger.info("插件已启用");
        
        // TypeScript 提供类型检查
        this.eventBus.subscribe('PlayerJoinEvent', (event) => {
            // event 参数会有类型提示
            this.logger.info(`玩家加入: ${event.playerName}`);
        });
    }
    
    onDisable(): void {
        this.logger.info("插件正在禁用...");
        this.eventBus.clearAll();
    }
    
    onUnload(): void {
        this.logger.info("插件已卸载");
    }
}

export = MyPlugin;
```

### 使用类型定义

```typescript
// src/types.ts
import { Logger, EventBus } from 'nethergate';

// 定义自定义事件接口
interface CustomRewardEvent {
    playerName: string;
    rewardType: string;
    amount: number;
    timestamp: number;
}

// 定义配置接口
interface PluginConfig {
    enabled: boolean;
    rewardInterval: number;
    rewardTypes: string[];
}

class RewardPlugin {
    private logger: Logger;
    private eventBus: EventBus;
    private config: PluginConfig;
    
    constructor(logger: Logger, eventBus: EventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.config = this.loadConfig();
    }
    
    private loadConfig(): PluginConfig {
        // 加载配置（示例）
        return {
            enabled: true,
            rewardInterval: 60000,
            rewardTypes: ['diamond', 'gold', 'iron']
        };
    }
    
    onEnable(): void {
        this.eventBus.subscribe('CustomRewardEvent', (event: CustomRewardEvent) => {
            this.logger.info(
                `${event.playerName} 获得 ${event.amount}x ${event.rewardType}`
            );
        });
    }
    
    giveReward(playerName: string): void {
        const rewardType = this.config.rewardTypes[
            Math.floor(Math.random() * this.config.rewardTypes.length)
        ];
        
        const event: CustomRewardEvent = {
            playerName,
            rewardType,
            amount: 1,
            timestamp: Date.now()
        };
        
        this.eventBus.publish(event);
    }
}

export = RewardPlugin;
```

### 编译 TypeScript

添加到 `package.json`：

```json
{
  "scripts": {
    "build": "tsc",
    "watch": "tsc --watch"
  }
}
```

运行构建：

```bash
npm run build
```

---

## 完整示例

下面是一个综合使用这些 API 的完整示例：

```javascript
/**
 * 玩家欢迎插件
 * 功能：
 * - 玩家加入时发送欢迎消息
 * - 记录玩家加入/离开日志
 * - 定期广播服务器消息
 */
class WelcomePlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.timerId = null;
        this.playerCount = 0;
    }
    
    onLoad() {
        this.logger.info("欢迎插件加载中...");
    }
    
    onEnable() {
        this.logger.info("欢迎插件已启用");
        
        // 订阅玩家加入事件
        this.eventBus.subscribe('PlayerJoinEvent', (event) => {
            this.onPlayerJoin(event);
        });
        
        // 订阅玩家离开事件
        this.eventBus.subscribe('PlayerLeaveEvent', (event) => {
            this.onPlayerLeave(event);
        });
        
        // 启动定时任务：每 5 分钟广播一次消息
        this.startBroadcast();
    }
    
    onDisable() {
        this.logger.info("欢迎插件正在禁用...");
        this.stopBroadcast();
        this.eventBus.clearAll();
    }
    
    onUnload() {
        this.logger.info("欢迎插件已卸载");
    }
    
    onPlayerJoin(event) {
        const playerName = event.playerName;
        this.playerCount++;
        
        this.logger.info(`玩家 ${playerName} 加入了游戏 (当前在线: ${this.playerCount})`);
        
        // 发布自定义欢迎事件
        this.eventBus.publish({
            type: 'WelcomeMessageEvent',
            playerName: playerName,
            message: `欢迎 ${playerName} 加入服务器!`
        });
    }
    
    onPlayerLeave(event) {
        const playerName = event.playerName;
        this.playerCount = Math.max(0, this.playerCount - 1);
        
        this.logger.info(`玩家 ${playerName} 离开了游戏 (当前在线: ${this.playerCount})`);
    }
    
    startBroadcast() {
        this.timerId = setInterval(() => {
            this.logger.info(`定时广播: 当前在线 ${this.playerCount} 名玩家`);
            
            // 发布广播事件
            this.eventBus.publish({
                type: 'ServerBroadcastEvent',
                message: '感谢您的游玩!',
                timestamp: Date.now()
            });
        }, 5 * 60 * 1000); // 5 分钟
    }
    
    stopBroadcast() {
        if (this.timerId) {
            clearInterval(this.timerId);
            this.timerId = null;
        }
    }
}

module.exports = WelcomePlugin;
```

---

## 最佳实践

### 1. 错误处理

始终使用 try-catch 处理可能的错误：

```javascript
onEnable() {
    try {
        this.eventBus.subscribe('PlayerJoinEvent', (event) => {
            this.handlePlayerJoin(event);
        });
    } catch (error) {
        this.logger.error(`订阅事件失败: ${error.message}`);
    }
}
```

### 2. 资源清理

确保在插件卸载时清理所有资源：

```javascript
onDisable() {
    // 清理定时器
    if (this.timerId) {
        clearInterval(this.timerId);
    }
    
    // 取消事件订阅
    this.eventBus.clearAll();
    
    // 清理其他资源
    this.cleanup();
}
```

### 3. 使用箭头函数

使用箭头函数避免 `this` 绑定问题：

```javascript
onEnable() {
    // 推荐 ✓
    this.eventBus.subscribe('PlayerJoinEvent', (event) => {
        this.logger.info(`玩家: ${event.playerName}`);
    });
    
    // 不推荐 ✗ (this 可能未定义)
    this.eventBus.subscribe('PlayerJoinEvent', function(event) {
        this.logger.info(`玩家: ${event.playerName}`); // 错误!
    });
}
```

### 4. 日志级别

使用适当的日志级别：

```javascript
this.logger.debug("调试信息 - 详细的执行流程");
this.logger.info("普通信息 - 重要的状态更新");
this.logger.warn("警告 - 可能的问题");
this.logger.error("错误 - 需要注意的问题");
```

### 5. 事件命名

使用清晰、一致的事件命名：

```javascript
// 推荐 ✓
this.eventBus.publish({
    type: 'PlayerRewardEvent',
    playerName: 'Steve',
    rewardType: 'diamond'
});

// 不推荐 ✗
this.eventBus.publish({
    type: 'reward',
    p: 'Steve',
    r: 'diamond'
});
```

---

## 限制和注意事项

### 1. Jint 引擎限制

- 不支持某些 ES6+ 特性（如 `async/await`、生成器）
- 不支持 `require()` 外部 npm 包
- 某些 JavaScript API 可能不可用

### 2. 性能考虑

- JavaScript 和 C# 之间的调用有性能开销
- 避免在事件处理器中执行耗时操作
- 使用批量操作而非频繁的小操作

### 3. 类型转换

- JavaScript 对象和 C# 对象之间的转换可能有限制
- 复杂的 .NET 类型可能无法直接在 JavaScript 中使用

---

## 下一步

- 查看 [JavaScript API 参考](../../08-参考/JavaScript_API参考.md) 了解完整的 API 文档
- 查看 [JavaScript 插件开发指南](./JavaScript插件开发指南.md) 了解插件结构
- 查看 [TypeScript 类型定义](../../src/NetherGate.Script/JavaScriptSDK/nethergate.d.ts) 了解类型信息

---

**最后更新**: 2025-10-11  
**维护者**: NetherGate Team


