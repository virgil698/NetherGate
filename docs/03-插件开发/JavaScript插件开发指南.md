# JavaScript/TypeScript 插件开发指南

> **注意**: JavaScript/TypeScript 插件支持需要安装 `NetherGate.Script` 扩展模块

本指南介绍如何使用 JavaScript 或 TypeScript 开发 NetherGate 插件。JavaScript 插件通过 `NetherGate.Script` 桥接层与主系统交互，提供与 C# 插件相同的功能。

> **⚠️ 功能限制与性能说明**
>
> JavaScript 插件基于桥接层实现，适合快速开发和原型验证，但请注意以下限制：
>
> - **API 覆盖范围**：JavaScript SDK 主要封装核心功能，部分高级 API 可能未完全实现
> - **性能影响**：跨语言调用会产生额外开销，不适合高频率调用或性能敏感场景
> - **运行环境**：基于嵌入式 JavaScript 引擎（Jint），不支持完整的 Node.js 生态系统
> - **调试体验**：跨语言调试相对复杂，错误堆栈可能跨越 JavaScript 和 .NET
>
> **何时选择 JavaScript 插件**：
> - ✅ 快速原型和概念验证
> - ✅ 简单的游戏逻辑和脚本
> - ✅ 熟悉 JavaScript/TypeScript 的开发者
> - ✅ 轻量级工具和实用功能
>
> **何时选择 .NET 插件**：
> - ✅ 生产环境和企业级应用
> - ✅ 高性能要求（如实时数据处理）
> - ✅ 复杂的业务逻辑和架构
> - ✅ 需要完整的 API 访问和类型安全
> - ✅ 深度集成 .NET 生态系统
>
> 如需最佳性能和完整功能，请参考 [C# 插件开发指南](./插件开发指南.md)。

---

## 📋 目录

- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [项目结构](#项目结构)
- [插件元数据](#插件元数据)
- [基础插件类](#基础插件类)
- [生命周期](#生命周期)
- [API 使用](#api-使用)
- [TypeScript 支持](#typescript-支持)
- [打包发布](#打包发布)
- [调试技巧](#调试技巧)
- [最佳实践](#最佳实践)

---

## 环境要求

### 必需组件

- **Node.js**: 18.0 或更高版本（用于开发，TypeScript 编译等）
- **NetherGate**: 最新版本
- **NetherGate.Script**: JavaScript/TypeScript 桥接扩展

### 可选工具

- **IDE**: VS Code, WebStorm, 或其他 JavaScript/TypeScript IDE
- **TypeScript**: 用于类型检查和 TypeScript 开发
- **ESLint**: 代码规范检查
- **Prettier**: 代码格式化

### 安装 NetherGate.Script

确保 NetherGate.Script 模块已正确安装在服务器中。

---

## 快速开始

### 1. 创建插件项目

```bash
# 创建插件目录
mkdir MyJavaScriptPlugin
cd MyJavaScriptPlugin

# 创建标准目录结构
mkdir src
mkdir resource

# 初始化 package.json（可选，用于开发依赖）
npm init -y

# 安装 TypeScript（如果使用 TS）
npm install --save-dev typescript @types/node

# 创建主文件
touch src/index.js
touch resource/plugin.json
```

### 2. 编写插件元数据

创建 `resource/plugin.json`:

```json
{
  "id": "com.example.myplugin",
  "name": "My JavaScript Plugin",
  "version": "1.0.0",
  "description": "我的第一个 JavaScript 插件",
  "author": "Your Name",
  "website": "https://github.com/yourname/myplugin",
  
  "type": "javascript",
  "main": "src/index.js",
  
  "dependencies": [],
  "soft_dependencies": [],
  "load_order": 100
}
```

### 3. 编写插件代码（JavaScript）

创建 `src/index.js`:

```javascript
/**
 * 我的第一个 NetherGate JavaScript 插件
 */
class MyPlugin {
    /**
     * 构造函数 - 支持依赖注入
     * @param {Logger} logger - 日志记录器
     * @param {EventBus} eventBus - 事件总线
     */
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
        
        this.info = {
            id: "com.example.myplugin",
            name: "My JavaScript Plugin",
            version: "1.0.0",
            description: "我的第一个 JavaScript 插件",
            author: "Your Name"
        };
    }
    
    /**
     * 插件加载时调用
     */
    async onLoad() {
        this.logger.info(`${this.info.name} 正在加载...`);
    }
    
    /**
     * 插件启用时调用
     */
    async onEnable() {
        this.logger.info(`${this.info.name} 已启用!`);
        
        // 注册事件监听器
        this.eventBus.subscribe("ServerStartedEvent", this.onServerStarted.bind(this));
    }
    
    /**
     * 插件禁用时调用
     */
    async onDisable() {
        this.logger.info(`${this.info.name} 已禁用`);
        
        // 注销事件监听器
        this.eventBus.unsubscribe("ServerStartedEvent", this.onServerStarted.bind(this));
    }
    
    /**
     * 插件卸载时调用
     */
    async onUnload() {
        this.logger.info(`${this.info.name} 已卸载`);
    }
    
    /**
     * 服务器启动事件处理
     * @param {Object} event - 事件对象
     */
    async onServerStarted(event) {
        this.logger.info("检测到服务器启动!");
    }
}

// 导出插件类
module.exports = MyPlugin;
```

### 4. 部署插件

#### 方式1: 直接部署目录

将插件复制到 NetherGate 的 `plugins` 目录：

```
NetherGate/
  plugins/
    MyJavaScriptPlugin/
      src/
        index.js
      resource/
        plugin.json
        config.yaml  (可选)
```

#### 方式2: 打包为 .ngplugin 文件（推荐）

```bash
# 在插件根目录执行
npm run pack
# 或使用 zip 命令
zip -r MyJavaScriptPlugin.ngplugin src/ resource/ README.md LICENSE

# 将 .ngplugin 文件放入 plugins 目录
cp MyJavaScriptPlugin.ngplugin ../NetherGate/plugins/
```

---

## 项目结构

### 标准目录结构（JavaScript）

```
MyJavaScriptPlugin/
├── src/                    # JavaScript 源代码目录
│   ├── index.js           # 插件主类（必需）
│   ├── commands.js        # 命令处理
│   ├── events.js          # 事件处理
│   └── utils.js           # 工具函数
│
├── resource/              # 资源文件目录
│   ├── plugin.json        # 插件元数据（必需）
│   ├── config.yaml        # 默认配置文件
│   └── lang/              # 国际化文件
│       ├── en_US.yaml
│       └── zh_CN.yaml
│
├── tests/                 # 测试文件（可选）
│   └── test_index.js
│
├── package.json           # NPM 包配置（开发用）
├── README.md             # 插件说明
└── LICENSE               # 开源协议
```

### 标准目录结构（TypeScript）

```
MyTypeScriptPlugin/
├── src/                    # TypeScript 源代码目录
│   ├── index.ts           # 插件主类（必需）
│   ├── commands.ts        # 命令处理
│   ├── events.ts          # 事件处理
│   ├── types.ts           # 类型定义
│   └── utils.ts           # 工具函数
│
├── dist/                  # 编译后的 JavaScript（发布用）
│   ├── index.js
│   ├── commands.js
│   └── ...
│
├── resource/              # 资源文件目录
│   ├── plugin.json        # 插件元数据（必需）
│   ├── config.yaml        # 默认配置文件
│   └── lang/              # 国际化文件
│
├── tests/                 # 测试文件（可选）
│   └── test_index.spec.ts
│
├── tsconfig.json          # TypeScript 配置
├── package.json           # NPM 包配置
├── README.md             # 插件说明
└── LICENSE               # 开源协议
```

### 关键文件说明

| 文件 | 必需 | 说明 |
|------|------|------|
| `src/index.js` 或 `src/index.ts` | ✅ | 插件主类，必须导出一个实现插件接口的类 |
| `resource/plugin.json` | ✅ | 插件元数据，描述插件信息 |
| `dist/` (TypeScript) | ✅ | TypeScript 编译后的输出目录 |
| `package.json` | ❌ | NPM 依赖声明（用于开发，不会在运行时使用） |
| `resource/config.yaml` | ❌ | 默认配置文件 |
| `README.md` | ❌ | 插件文档（建议提供） |

---

## 插件元数据

### plugin.json 完整示例

```json
{
  "id": "com.example.advanced_plugin",
  "name": "Advanced JavaScript Plugin",
  "version": "2.1.0",
  "description": "一个功能丰富的 JavaScript 插件示例",
  "author": "Your Name",
  "authors": ["Developer 1", "Developer 2"],
  "website": "https://github.com/yourname/advanced-plugin",
  "repository": "https://github.com/yourname/advanced-plugin",
  "license": "MIT",
  
  "type": "javascript",
  "main": "dist/index.js",
  
  "dependencies": [
    "com.example.base_plugin"
  ],
  "soft_dependencies": [
    "com.example.optional_plugin"
  ],
  "load_order": 100,
  
  "permissions": [
    "nethergate.command.register",
    "nethergate.rcon.execute"
  ],
  
  "min_nethergate_version": "1.0.0",
  "category": "utility",
  "tags": ["teleport", "management", "utility"]
}
```

### 字段说明

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 插件唯一标识符（推荐反向域名格式） |
| `name` | string | ✅ | 插件显示名称 |
| `version` | string | ✅ | 插件版本（建议使用语义化版本） |
| `description` | string | ✅ | 插件简短描述 |
| `author` | string | ✅ | 插件作者 |
| `website` | string | ❌ | 插件主页 |
| `type` | string | ✅ | 插件类型，JavaScript 插件为 `"javascript"` |
| `main` | string | ✅ | 插件入口文件路径，相对于插件根目录 |
| `dependencies` | array | ❌ | 硬依赖插件列表 |
| `soft_dependencies` | array | ❌ | 软依赖插件列表 |
| `load_order` | number | ❌ | 加载顺序（默认 100） |
| `permissions` | array | ❌ | 插件需要的权限列表 |

---

## 基础插件类

### 插件类结构

所有 JavaScript 插件必须导出一个包含以下方法的类：

```javascript
class MyPlugin {
    /**
     * 构造函数（可选参数，支持依赖注入）
     */
    constructor(logger, eventBus, rcon, scheduler, /* ... */) {
        // 初始化
        this.info = {
            id: "com.example.myplugin",
            name: "My Plugin",
            version: "1.0.0"
        };
    }
    
    /**
     * 插件加载（可选）
     */
    async onLoad() {
        // 加载配置、初始化资源
    }
    
    /**
     * 插件启用（可选）
     */
    async onEnable() {
        // 注册命令、事件监听器等
    }
    
    /**
     * 插件禁用（可选）
     */
    async onDisable() {
        // 清理资源、注销监听器
    }
    
    /**
     * 插件卸载（可选）
     */
    async onUnload() {
        // 最终清理
    }
}

module.exports = MyPlugin;
```

### 构造函数注入（推荐）

插件支持通过构造函数进行依赖注入：

```javascript
class MyPlugin {
    constructor(
        logger,              // 日志记录器
        eventBus,           // 事件总线
        commandRegistry,    // 命令注册器
        rcon,              // RCON 客户端
        scheduler,         // 调度器
        configManager,     // 配置管理器
        playerDataReader,  // 玩家数据读取器
        scoreboardApi      // 计分板 API
    ) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.commands = commandRegistry;
        this.rcon = rcon;
        this.scheduler = scheduler;
        this.config = configManager;
        this.playerData = playerDataReader;
        this.scoreboard = scoreboardApi;
        
        this.info = {
            id: "com.example.myplugin",
            name: "My Plugin",
            version: "1.0.0"
        };
    }
}
```

### 可注入的服务

| 服务名称（参数名） | 说明 |
|-------------------|------|
| `logger` | 日志记录器 |
| `eventBus` | 事件总线 |
| `commandRegistry` | 命令注册器 |
| `rcon` | RCON 客户端 |
| `scheduler` | 任务调度器 |
| `configManager` | 配置管理器 |
| `permissionManager` | 权限管理器 |
| `scoreboardApi` | 计分板 API |
| `playerDataReader` | 玩家数据读取器 |
| `webSocketServer` | WebSocket 服务器 |

---

## 生命周期

### 生命周期方法

JavaScript 插件的生命周期与 C# 插件保持一致：

```javascript
class MyPlugin {
    /**
     * 插件加载阶段
     * 
     * 时机: 插件被加载到内存，但尚未启用
     * 用途: 
     *   - 初始化基本配置
     *   - 加载资源文件
     *   - 验证依赖项
     * 注意: 此时不应注册事件或命令
     */
    async onLoad() {
        this.logger.info("插件正在加载...");
    }

    /**
     * 插件启用阶段
     * 
     * 时机: 插件开始正式工作
     * 用途:
     *   - 注册事件监听器
     *   - 注册命令
     *   - 启动定时任务
     *   - 连接外部服务
     */
    async onEnable() {
        this.logger.info("插件已启用");
    }

    /**
     * 插件禁用阶段
     * 
     * 时机: 插件停止工作
     * 用途:
     *   - 注销事件监听器
     *   - 注销命令
     *   - 停止定时任务
     *   - 断开外部连接
     * 注意: 应该清理所有注册的资源
     */
    async onDisable() {
        this.logger.info("插件已禁用");
    }

    /**
     * 插件卸载阶段
     * 
     * 时机: 插件从内存中移除
     * 用途:
     *   - 释放所有资源
     *   - 保存持久化数据
     *   - 关闭文件句柄
     */
    async onUnload() {
        this.logger.info("插件已卸载");
    }
}
```

### 生命周期流程图

```
[未加载] 
   ↓ (onLoad)
[已加载]
   ↓ (onEnable)
[已启用] ←→ (热重载)
   ↓ (onDisable)
[已禁用]
   ↓ (onUnload)
[已卸载]
```

---

## API 使用

### 日志记录

```javascript
class MyPlugin {
    constructor(logger) {
        this.logger = logger;
    }
    
    async onEnable() {
        // 不同级别的日志
        this.logger.trace("详细的调试信息");
        this.logger.debug("调试信息");
        this.logger.info("一般信息");
        this.logger.warning("警告信息");
        this.logger.error("错误信息");
        
        // 带变量的日志
        const playerName = "Steve";
        this.logger.info(`玩家 ${playerName} 完成了操作`);
        
        // 带异常的日志（如果支持）
        try {
            riskyOperation();
        } catch (error) {
            this.logger.error(`操作失败: ${error.message}`);
        }
    }
}
```

### 事件系统

```javascript
class MyPlugin {
    constructor(eventBus) {
        this.eventBus = eventBus;
        // 存储绑定的处理函数，用于后续注销
        this.boundHandlers = {};
    }
    
    async onEnable() {
        // 绑定 this 上下文
        this.boundHandlers.serverStarted = this.onServerStarted.bind(this);
        this.boundHandlers.playerJoin = this.onPlayerJoin.bind(this);
        
        // 订阅事件
        this.eventBus.subscribe("ServerStartedEvent", this.boundHandlers.serverStarted);
        this.eventBus.subscribe("PlayerJoinEvent", this.boundHandlers.playerJoin);
    }
    
    async onDisable() {
        // 取消订阅
        this.eventBus.unsubscribe("ServerStartedEvent", this.boundHandlers.serverStarted);
        this.eventBus.unsubscribe("PlayerJoinEvent", this.boundHandlers.playerJoin);
    }
    
    async onServerStarted(event) {
        this.logger.info("服务器已启动");
    }
    
    async onPlayerJoin(event) {
        this.logger.info(`玩家 ${event.playerName} 加入了游戏`);
        
        // 发送欢迎消息
        await this.rcon.execute(
            `tellraw ${event.playerName} {"text":"欢迎回来!","color":"green"}`
        );
    }
}
```

### 命令注册

```javascript
class MyPlugin {
    constructor(commandRegistry, rcon, logger) {
        this.commands = commandRegistry;
        this.rcon = rcon;
        this.logger = logger;
    }
    
    async onEnable() {
        // 注册命令
        this.commands.register({
            name: "hello",
            callback: this.cmdHello.bind(this),
            description: "打招呼命令",
            usage: "/hello [name]",
            permission: "myplugin.command.hello"
        });
    }
    
    async onDisable() {
        // 注销命令
        this.commands.unregister("hello");
    }
    
    async cmdHello(context) {
        const name = context.args.length > 0 ? context.args[0] : "World";
        await context.reply(`Hello, ${name}!`);
        this.logger.info(`Hello 命令被执行: ${name}`);
    }
}
```

### RCON 执行

```javascript
class MyPlugin {
    constructor(rcon, logger) {
        this.rcon = rcon;
        this.logger = logger;
    }
    
    async teleportPlayer(player, x, y, z) {
        const result = await this.rcon.execute(`tp ${player} ${x} ${y} ${z}`);
        if (result.success) {
            this.logger.info(`已传送 ${player} 到 (${x}, ${y}, ${z})`);
        } else {
            this.logger.error(`传送失败: ${result.response}`);
        }
    }
    
    async giveItem(player, item, count = 1) {
        await this.rcon.execute(`give ${player} ${item} ${count}`);
    }
    
    async getOnlinePlayers() {
        const result = await this.rcon.execute("list");
        if (result.success) {
            // 解析在线玩家列表
            return this.parsePlayerList(result.response);
        }
        return [];
    }
}
```

### 定时任务

```javascript
class MyPlugin {
    constructor(scheduler, logger) {
        this.scheduler = scheduler;
        this.logger = logger;
        this.taskIds = [];
    }
    
    async onEnable() {
        // 延迟执行（5秒后）
        const delayedId = this.scheduler.runDelayed(
            this.delayedTask.bind(this),
            5000  // 毫秒
        );
        this.taskIds.push(delayedId);
        
        // 定时重复执行（每10秒）
        const repeatingId = this.scheduler.runRepeating(
            this.repeatingTask.bind(this),
            10000,  // 间隔（毫秒）
            0       // 初始延迟（毫秒）
        );
        this.taskIds.push(repeatingId);
    }
    
    async onDisable() {
        // 取消所有定时任务
        for (const taskId of this.taskIds) {
            this.scheduler.cancel(taskId);
        }
        this.taskIds = [];
    }
    
    async delayedTask() {
        this.logger.info("延迟任务执行");
    }
    
    async repeatingTask() {
        this.logger.info("定时任务执行");
    }
}
```

### 配置管理

```javascript
class MyPlugin {
    constructor(configManager, logger) {
        this.config = configManager;
        this.logger = logger;
    }
    
    async onLoad() {
        // 加载配置（自动创建默认配置）
        await this.config.load("config.yaml", {
            maxPlayers: 20,
            welcomeMessage: "Welcome!",
            features: {
                teleport: true,
                kits: false
            }
        });
    }
    
    async onEnable() {
        // 读取配置
        const maxPlayers = this.config.get("maxPlayers", 10);
        const message = this.config.get("welcomeMessage");
        const teleportEnabled = this.config.get("features.teleport");  // 支持点号路径
        
        this.logger.info(`最大玩家数: ${maxPlayers}`);
        
        // 修改配置
        this.config.set("maxPlayers", 25);
        
        // 保存配置
        await this.config.save("config.yaml");
    }
}
```

### 玩家数据读取

```javascript
class MyPlugin {
    constructor(playerDataReader, logger) {
        this.playerData = playerDataReader;
        this.logger = logger;
    }
    
    async getPlayerStats(playerName) {
        try {
            // 读取玩家 NBT 数据
            const data = await this.playerData.read(playerName);
            
            if (data) {
                const pos = data.Pos;
                const health = data.Health;
                const inventory = data.Inventory;
                
                this.logger.info(`${playerName}: 位置=${pos}, 生命值=${health}`);
                return {
                    position: pos,
                    health: health,
                    inventoryCount: inventory ? inventory.length : 0
                };
            }
        } catch (error) {
            this.logger.error(`读取玩家数据失败: ${error.message}`);
        }
        return null;
    }
}
```

### 计分板操作

```javascript
class MyPlugin {
    constructor(scoreboardApi, logger) {
        this.scoreboard = scoreboardApi;
        this.logger = logger;
    }
    
    async createLeaderboard() {
        // 创建计分板
        await this.scoreboard.createObjective(
            "kills",
            "playerKillCount",
            "击杀排行"
        );
        
        // 设置显示位置
        await this.scoreboard.setDisplay("sidebar", "kills");
    }
    
    async addScore(player, points) {
        await this.scoreboard.addScore("kills", player, points);
    }
    
    async getTopPlayers(limit = 10) {
        const scores = await this.scoreboard.getScores("kills");
        // 排序并返回前N名
        const sorted = Object.entries(scores)
            .sort(([, a], [, b]) => b - a)
            .slice(0, limit);
        return sorted;
    }
}
```

### WebSocket 推送

```javascript
class MyPlugin {
    constructor(webSocketServer, logger) {
        this.ws = webSocketServer;
        this.logger = logger;
    }
    
    async broadcastEvent(eventType, data) {
        await this.ws.broadcast({
            type: eventType,
            plugin: "myplugin",
            data: data,
            timestamp: new Date().toISOString()
        });
    }
    
    async onPlayerJoin(event) {
        await this.broadcastEvent("player_join", {
            player: event.playerName,
            uuid: event.playerUuid
        });
    }
}
```

---

## TypeScript 支持

### 设置 TypeScript 项目

1. **安装 TypeScript**

```bash
npm install --save-dev typescript @types/node
```

2. **创建 `tsconfig.json`**

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "resolveJsonModule": true,
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist", "tests"]
}
```

3. **创建类型定义文件** `src/nethergate.d.ts`

```typescript
// NetherGate JavaScript API 类型定义

declare module 'nethergate' {
    export interface PluginInfo {
        id: string;
        name: string;
        version: string;
        description?: string;
        author?: string;
    }

    export interface Logger {
        trace(message: string): void;
        debug(message: string): void;
        info(message: string): void;
        warning(message: string): void;
        error(message: string): void;
    }

    export interface EventBus {
        subscribe(eventName: string, handler: (event: any) => void | Promise<void>): void;
        unsubscribe(eventName: string, handler: Function): void;
        publish(eventName: string, event: any): void;
    }

    export interface CommandContext {
        sender: string;
        args: string[];
        reply(message: string): Promise<void>;
    }

    export interface CommandRegistry {
        register(options: {
            name: string;
            callback: (context: CommandContext) => void | Promise<void>;
            description?: string;
            usage?: string;
            permission?: string;
        }): void;
        unregister(name: string): void;
    }

    export interface RconResult {
        success: boolean;
        response: string;
    }

    export interface RconClient {
        execute(command: string): Promise<RconResult>;
        executeBatch(commands: string[]): Promise<RconResult[]>;
    }

    export interface Scheduler {
        runDelayed(callback: () => void | Promise<void>, delayMs: number): string;
        runRepeating(callback: () => void | Promise<void>, intervalMs: number, initialDelayMs?: number): string;
        cancel(taskId: string): void;
    }

    export interface ConfigManager {
        load(filename: string, defaults?: any): Promise<any>;
        get(path: string, defaultValue?: any): any;
        set(path: string, value: any): void;
        save(filename: string): Promise<void>;
    }

    export interface PlayerDataReader {
        read(playerName: string): Promise<any>;
        exists(playerName: string): Promise<boolean>;
    }

    export interface ScoreboardApi {
        createObjective(name: string, criterion: string, displayName: string): Promise<void>;
        setDisplay(slot: string, objective: string): Promise<void>;
        addScore(objective: string, player: string, points: number): Promise<void>;
        getScores(objective: string): Promise<Record<string, number>>;
    }

    export interface WebSocketServer {
        broadcast(message: any): Promise<void>;
        send(clientId: string, message: any): Promise<void>;
    }

    export interface Plugin {
        info: PluginInfo;
        onLoad?(): Promise<void> | void;
        onEnable?(): Promise<void> | void;
        onDisable?(): Promise<void> | void;
        onUnload?(): Promise<void> | void;
    }
}
```

### TypeScript 插件示例

创建 `src/index.ts`:

```typescript
import type {
    Plugin,
    PluginInfo,
    Logger,
    EventBus,
    CommandRegistry,
    CommandContext,
    RconClient
} from 'nethergate';

/**
 * 我的第一个 TypeScript 插件
 */
class MyPlugin implements Plugin {
    public readonly info: PluginInfo;
    
    private logger: Logger;
    private eventBus: EventBus;
    private commands: CommandRegistry;
    private rcon: RconClient;
    
    /**
     * 构造函数 - 依赖注入
     */
    constructor(
        logger: Logger,
        eventBus: EventBus,
        commandRegistry: CommandRegistry,
        rcon: RconClient
    ) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.commands = commandRegistry;
        this.rcon = rcon;
        
        this.info = {
            id: "com.example.myplugin",
            name: "My TypeScript Plugin",
            version: "1.0.0",
            description: "我的第一个 TypeScript 插件",
            author: "Your Name"
        };
    }
    
    async onLoad(): Promise<void> {
        this.logger.info(`${this.info.name} 正在加载...`);
    }
    
    async onEnable(): Promise<void> {
        this.logger.info(`${this.info.name} 已启用!`);
        
        // 注册命令
        this.commands.register({
            name: "hello",
            callback: this.cmdHello.bind(this),
            description: "打招呼命令",
            usage: "/hello [name]"
        });
        
        // 注册事件
        this.eventBus.subscribe("ServerStartedEvent", this.onServerStarted.bind(this));
    }
    
    async onDisable(): Promise<void> {
        this.logger.info(`${this.info.name} 已禁用`);
        this.commands.unregister("hello");
    }
    
    async onUnload(): Promise<void> {
        this.logger.info(`${this.info.name} 已卸载`);
    }
    
    private async onServerStarted(event: any): Promise<void> {
        this.logger.info("服务器已启动!");
    }
    
    private async cmdHello(ctx: CommandContext): Promise<void> {
        const name = ctx.args.length > 0 ? ctx.args[0] : "World";
        await ctx.reply(`Hello, ${name}!`);
    }
}

export = MyPlugin;
```

### 编译 TypeScript

在 `package.json` 中添加脚本：

```json
{
  "scripts": {
    "build": "tsc",
    "watch": "tsc --watch",
    "clean": "rm -rf dist"
  }
}
```

编译：

```bash
npm run build
```

编译后的文件在 `dist/` 目录，记得在 `plugin.json` 中将 `main` 字段指向 `dist/index.js`。

---

## 打包发布

### 准备发布

1. **检查代码**

```bash
# 如果是 TypeScript，先编译
npm run build

# 代码格式检查
npm run lint  # 如果配置了 ESLint

# 运行测试
npm test  # 如果有测试
```

2. **更新文档**

- 更新 `README.md`
- 更新 `CHANGELOG.md`
- 确保 `plugin.json` 版本正确

3. **清理不必要的文件**

确保 `.gitignore` 或打包脚本排除：
- `node_modules/`
- `.git/`
- `tests/`
- `src/` (如果是 TypeScript，只保留 `dist/`)

### 打包为 .ngplugin

**方式1: 使用 npm 脚本**

在 `package.json` 中添加：

```json
{
  "scripts": {
    "pack": "node scripts/pack.js"
  }
}
```

创建 `scripts/pack.js`:

```javascript
const fs = require('fs');
const archiver = require('archiver');
const path = require('path');

const pluginName = "MyJavaScriptPlugin";
const outputFile = `${pluginName}.ngplugin`;

const output = fs.createWriteStream(outputFile);
const archive = archiver('zip', { zlib: { level: 9 } });

output.on('close', () => {
    console.log(`✅ 插件已打包: ${outputFile} (${archive.pointer()} bytes)`);
});

archive.on('error', (err) => {
    throw err;
});

archive.pipe(output);

// 添加文件
if (fs.existsSync('dist')) {
    archive.directory('dist/', 'dist');
} else {
    archive.directory('src/', 'src');
}
archive.directory('resource/', 'resource');

// 添加文档
['README.md', 'LICENSE', 'CHANGELOG.md'].forEach(file => {
    if (fs.existsSync(file)) {
        archive.file(file, { name: file });
    }
});

archive.finalize();
```

安装依赖：

```bash
npm install --save-dev archiver
```

打包：

```bash
npm run pack
```

**方式2: 使用 zip 命令**

```bash
# Linux/macOS
zip -r MyJavaScriptPlugin.ngplugin dist/ resource/ README.md LICENSE

# Windows (PowerShell)
Compress-Archive -Path dist\,resource\,README.md,LICENSE -DestinationPath MyJavaScriptPlugin.ngplugin
```

### 打包后的文件结构

```
MyJavaScriptPlugin.ngplugin (ZIP 格式)
├── dist/              # (或 src/)
│   ├── index.js
│   └── ...
├── resource/
│   ├── plugin.json
│   ├── config.yaml
│   └── ...
├── README.md
└── LICENSE
```

### 发布到 GitHub Releases

```bash
# 1. 创建 Git 标签
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# 2. 上传 .ngplugin 文件到 GitHub Releases
gh release create v1.0.0 MyJavaScriptPlugin.ngplugin --title "v1.0.0" --notes "首次发布"
```

---

## 调试技巧

### 控制台日志调试

```javascript
class MyPlugin {
    constructor(logger) {
        this.logger = logger;
        this.debugMode = true;  // 开发时设为 true
    }
    
    debug(message) {
        if (this.debugMode) {
            this.logger.debug(`[DEBUG] ${message}`);
        }
    }
    
    async someFunction() {
        this.debug("进入 someFunction");
        
        const result = await this.doSomething();
        this.debug(`结果: ${JSON.stringify(result)}`);
        
        this.debug("退出 someFunction");
    }
}
```

### 错误处理

```javascript
class MyPlugin {
    async safeOperation() {
        try {
            await this.riskyOperation();
        } catch (error) {
            this.logger.error(`操作失败: ${error.message}`);
            this.logger.error(`堆栈: ${error.stack}`);
            // 可以选择重新抛出
            // throw error;
        }
    }
    
    async riskyOperation() {
        // 可能抛出异常的代码
    }
}
```

### 性能计时

```javascript
class MyPlugin {
    async timedOperation(operationName, callback) {
        const start = Date.now();
        try {
            const result = await callback();
            const elapsed = Date.now() - start;
            this.logger.debug(`${operationName} 耗时: ${elapsed}ms`);
            return result;
        } catch (error) {
            const elapsed = Date.now() - start;
            this.logger.error(`${operationName} 失败 (耗时: ${elapsed}ms): ${error.message}`);
            throw error;
        }
    }
    
    async expensiveOperation() {
        return await this.timedOperation("expensiveOperation", async () => {
            // 耗时操作
        });
    }
}
```

---

## 最佳实践

### 1. 代码组织

```javascript
// ✅ 推荐：模块化设计
// src/index.js - 主插件类
// src/commands.js - 命令处理
// src/events.js - 事件处理
// src/config.js - 配置管理
// src/utils.js - 工具函数

// 主文件导入其他模块
const Commands = require('./commands');
const Events = require('./events');

class MyPlugin {
    constructor(logger, eventBus, commandRegistry) {
        this.commands = new Commands(commandRegistry, logger);
        this.events = new Events(eventBus, logger);
    }
}
```

### 2. 异步编程

```javascript
// ✅ 推荐：使用 async/await
async onEnable() {
    const result = await this.rcon.execute("list");
    await this.processResult(result);
}

// ❌ 不推荐：不处理 Promise
onEnable() {
    this.rcon.execute("list");  // Promise 未等待
    this.processResult(result); // result 未定义
}
```

### 3. 资源清理

```javascript
class MyPlugin {
    constructor(eventBus, scheduler) {
        this.eventBus = eventBus;
        this.scheduler = scheduler;
        this.handlers = {};
        this.taskIds = [];
    }
    
    async onEnable() {
        // 注册资源时记录
        this.handlers.playerJoin = this.onPlayerJoin.bind(this);
        this.eventBus.subscribe("PlayerJoinEvent", this.handlers.playerJoin);
        
        const taskId = this.scheduler.runRepeating(this.task.bind(this), 10000);
        this.taskIds.push(taskId);
    }
    
    async onDisable() {
        // ✅ 务必清理所有资源
        this.eventBus.unsubscribe("PlayerJoinEvent", this.handlers.playerJoin);
        
        for (const taskId of this.taskIds) {
            this.scheduler.cancel(taskId);
        }
        this.taskIds = [];
    }
}
```

### 4. 配置验证

```javascript
async onLoad() {
    const config = await this.config.load("config.yaml", DEFAULT_CONFIG);
    
    // ✅ 验证配置
    const maxPlayers = config.maxPlayers;
    if (maxPlayers < 1 || maxPlayers > 100) {
        throw new Error("maxPlayers 必须在 1-100 之间");
    }
    
    if (!config.serverAddress) {
        throw new Error("缺少必需的 serverAddress 配置");
    }
}
```

### 5. 错误处理

```javascript
// ✅ 推荐：具体的异常处理
async processPlayer(playerName) {
    try {
        const data = await this.playerData.read(playerName);
        return data;
    } catch (error) {
        if (error.message.includes("not found")) {
            this.logger.warning(`玩家 ${playerName} 数据不存在`);
        } else if (error.message.includes("permission")) {
            this.logger.error(`无权限读取 ${playerName} 数据`);
        } else {
            this.logger.error(`读取失败: ${error.message}`);
        }
        return null;
    }
}

// ❌ 不推荐：吞掉所有异常
async processPlayer(playerName) {
    try {
        return await this.playerData.read(playerName);
    } catch {
        return null;  // 丢失了错误信息
    }
}
```

### 6. 性能优化

```javascript
// ✅ 推荐：批量操作
async teleportPlayers(players, x, y, z) {
    const commands = players.map(p => `tp ${p} ${x} ${y} ${z}`);
    await this.rcon.executeBatch(commands);
}

// ❌ 不推荐：逐个操作
async teleportPlayers(players, x, y, z) {
    for (const p of players) {
        await this.rcon.execute(`tp ${p} ${x} ${y} ${z}`);  // 多次网络请求
    }
}
```

### 7. 使用 TypeScript（强烈推荐）

TypeScript 提供类型安全和更好的 IDE 支持：

```typescript
// ✅ TypeScript: 编译时类型检查
async teleportPlayer(player: string, x: number, y: number, z: number): Promise<void> {
    await this.rcon.execute(`tp ${player} ${x} ${y} ${z}`);
}

// ❌ JavaScript: 运行时才发现错误
async teleportPlayer(player, x, y, z) {
    await this.rcon.execute(`tp ${player} ${x} ${y} ${z}`);
}
```

### 8. 国际化支持

```javascript
class MyPlugin {
    constructor(logger) {
        this.logger = logger;
        this.messages = {
            "en_US": {
                "welcome": "Welcome to the server!",
                "goodbye": "See you next time!"
            },
            "zh_CN": {
                "welcome": "欢迎来到服务器！",
                "goodbye": "下次再见！"
            }
        };
        this.locale = "zh_CN";
    }
    
    t(key) {
        return this.messages[this.locale][key] || key;
    }
    
    async onPlayerJoin(event) {
        const message = this.t("welcome");
        await this.rcon.execute(
            `tellraw ${event.playerName} {"text":"${message}"}`
        );
    }
}
```

---

## 完整示例

### 传送插件示例

```javascript
/**
 * 传送插件 - 提供玩家传送功能
 */
class TeleportPlugin {
    constructor(logger, commandRegistry, configManager, rcon) {
        this.logger = logger;
        this.commands = commandRegistry;
        this.config = configManager;
        this.rcon = rcon;
        
        this.info = {
            id: "com.example.teleport",
            name: "Teleport Plugin",
            version: "1.0.0",
            description: "玩家传送插件",
            author: "Your Name"
        };
        
        // 传送点存储
        this.warps = new Map();
    }
    
    async onLoad() {
        this.logger.info("正在加载传送插件...");
        
        // 加载配置
        const config = await this.config.load("config.yaml", {
            maxWarps: 10,
            teleportDelay: 3,
            requirePermission: true
        });
        
        // 加载传送点
        const warpsData = await this.config.load("warps.yaml", {});
        for (const [name, pos] of Object.entries(warpsData)) {
            this.warps.set(name, pos);
        }
        
        this.logger.info(`已加载 ${this.warps.size} 个传送点`);
    }
    
    async onEnable() {
        this.logger.info("正在启用传送插件...");
        
        // 注册命令
        this.commands.register({
            name: "setwarp",
            callback: this.cmdSetWarp.bind(this),
            description: "设置传送点",
            usage: "/setwarp <name> <x> <y> <z>",
            permission: "teleport.setwarp"
        });
        
        this.commands.register({
            name: "warp",
            callback: this.cmdWarp.bind(this),
            description: "传送到传送点",
            usage: "/warp <name>",
            permission: "teleport.warp"
        });
        
        this.commands.register({
            name: "delwarp",
            callback: this.cmdDelWarp.bind(this),
            description: "删除传送点",
            usage: "/delwarp <name>",
            permission: "teleport.delwarp"
        });
        
        this.commands.register({
            name: "listwarps",
            callback: this.cmdListWarps.bind(this),
            description: "列出所有传送点",
            usage: "/listwarps"
        });
        
        this.logger.info("传送插件已启用");
    }
    
    async onDisable() {
        this.logger.info("正在禁用传送插件...");
        
        // 保存传送点
        const warpsData = Object.fromEntries(this.warps);
        await this.config.save("warps.yaml", warpsData);
        
        // 注销命令
        this.commands.unregister("setwarp");
        this.commands.unregister("warp");
        this.commands.unregister("delwarp");
        this.commands.unregister("listwarps");
        
        this.logger.info("传送插件已禁用");
    }
    
    async onUnload() {
        this.logger.info("传送插件已卸载");
    }
    
    // 命令处理函数
    
    async cmdSetWarp(ctx) {
        if (ctx.args.length < 4) {
            await ctx.reply("用法: /setwarp <name> <x> <y> <z>");
            return;
        }
        
        const name = ctx.args[0];
        const x = parseInt(ctx.args[1]);
        const y = parseInt(ctx.args[2]);
        const z = parseInt(ctx.args[3]);
        
        if (isNaN(x) || isNaN(y) || isNaN(z)) {
            await ctx.reply("坐标必须是整数");
            return;
        }
        
        // 检查数量限制
        const maxWarps = this.config.get("maxWarps", 10);
        if (this.warps.size >= maxWarps && !this.warps.has(name)) {
            await ctx.reply(`传送点数量已达上限 (${maxWarps})`);
            return;
        }
        
        this.warps.set(name, [x, y, z]);
        await ctx.reply(`✅ 传送点 '${name}' 已设置为 (${x}, ${y}, ${z})`);
        this.logger.info(`创建传送点: ${name} -> (${x}, ${y}, ${z})`);
    }
    
    async cmdWarp(ctx) {
        if (ctx.args.length < 1) {
            await ctx.reply("用法: /warp <name>");
            return;
        }
        
        const name = ctx.args[0];
        if (!this.warps.has(name)) {
            await ctx.reply(`❌ 传送点 '${name}' 不存在`);
            await ctx.reply(`使用 /listwarps 查看所有传送点`);
            return;
        }
        
        const [x, y, z] = this.warps.get(name);
        const player = ctx.sender;
        
        // 执行传送
        const result = await this.rcon.execute(`tp ${player} ${x} ${y} ${z}`);
        if (result.success) {
            await ctx.reply(`✅ 已传送到 '${name}'`);
            this.logger.info(`${player} 传送到 ${name}`);
        } else {
            await ctx.reply(`❌ 传送失败`);
            this.logger.error(`传送失败: ${result.response}`);
        }
    }
    
    async cmdDelWarp(ctx) {
        if (ctx.args.length < 1) {
            await ctx.reply("用法: /delwarp <name>");
            return;
        }
        
        const name = ctx.args[0];
        if (!this.warps.has(name)) {
            await ctx.reply(`❌ 传送点 '${name}' 不存在`);
            return;
        }
        
        this.warps.delete(name);
        await ctx.reply(`✅ 传送点 '${name}' 已删除`);
        this.logger.info(`删除传送点: ${name}`);
    }
    
    async cmdListWarps(ctx) {
        if (this.warps.size === 0) {
            await ctx.reply("当前没有传送点");
            return;
        }
        
        await ctx.reply(`📍 传送点列表 (${this.warps.size}):`);
        for (const [name, [x, y, z]] of this.warps) {
            await ctx.reply(`  - ${name}: (${x}, ${y}, ${z})`);
        }
    }
}

module.exports = TeleportPlugin;
```

---

## 下一步

- 阅读 [JavaScript API 参考](../08-参考/JavaScript_API参考.md)
- 查看 [JavaScript 示例插件集](../07-示例和最佳实践/JavaScript示例插件集.md)
- 了解 [插件发布流程](发布流程.md)
- 加入社区交流

---

## 相关文档

- [C# 插件开发指南](插件开发指南.md)
- [Python 插件开发指南](Python插件开发指南.md)
- [配置文件](配置文件.md)
- [调试技巧](调试技巧.md)
- [发布流程](发布流程.md)

---

**注意**: JavaScript/TypeScript 插件功能目前处于开发阶段，API 可能会有变化。建议关注项目更新。

