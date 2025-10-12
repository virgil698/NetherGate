# JavaScript/TypeScript API 参考

NetherGate JavaScript API 提供了与 C# API 对等的功能，允许 JavaScript/TypeScript 开发者使用熟悉的语法开发插件。

> **⚠️ 重要说明**
>
> 本文档描述的 JavaScript API 是 C# API 的桥接封装。虽然我们努力提供完整的功能覆盖，但请注意：
>
> 1. **功能完整性**：部分高级功能或新增 API 可能尚未在 JavaScript 中实现
> 2. **性能考虑**：跨语言调用会引入额外开销，建议避免在热路径中频繁调用
> 3. **运行环境**：基于 Jint 3.x，不支持完整的 Node.js 生态和 ES6+ 高级特性
> 4. **异步支持**：Promise 支持有限，推荐使用简单的 async 方法
>
> 对于性能关键型应用和生产环境，建议使用 [C# API](./API参考.md) 开发插件。

---

## 📋 目录

- [核心类型](#核心类型)
- [日志系统](#日志系统)
- [事件系统](#事件系统)
- [命令系统](#命令系统)
- [RCON 客户端](#rcon-客户端)
- [调度器](#调度器)
- [配置管理](#配置管理)
- [数据读取](#数据读取)
- [计分板](#计分板)
- [权限系统](#权限系统)
- [WebSocket](#websocket)
- [工具类](#工具类)

---

## 核心类型

### Plugin

插件类，所有插件必须实现此接口。

**TypeScript 示例**:

```typescript
import type { Plugin, PluginInfo, Logger, EventBus } from 'nethergate';

class MyPlugin implements Plugin {
    public readonly info: PluginInfo;
    private logger: Logger;
    
    constructor(logger: Logger, eventBus: EventBus) {
        this.logger = logger;
        this.info = {
            id: "com.example.plugin",
            name: "My Plugin",
            version: "1.0.0"
        };
    }
    
    async onLoad(): Promise<void> {}
    async onEnable(): Promise<void> {}
    async onDisable(): Promise<void> {}
    async onUnload(): Promise<void> {}
}

export = MyPlugin;
```

**JavaScript 示例**:

```javascript
class MyPlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.info = {
            id: "com.example.plugin",
            name: "My Plugin",
            version: "1.0.0"
        };
    }
    
    async onLoad() {}
    async onEnable() {}
    async onDisable() {}
    async onUnload() {}
}

module.exports = MyPlugin;
```

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `info` | `PluginInfo` | 插件元数据（必需） |

#### 生命周期方法

| 方法 | 说明 |
|------|------|
| `onLoad()` | 插件加载时调用（可选） |
| `onEnable()` | 插件启用时调用（可选） |
| `onDisable()` | 插件禁用时调用（可选） |
| `onUnload()` | 插件卸载时调用（可选） |

---

### PluginInfo

插件元数据对象。

```typescript
interface PluginInfo {
    id: string;              // 插件唯一标识（必需）
    name: string;            // 插件名称（必需）
    version: string;         // 插件版本（必需）
    description?: string;    // 插件描述
    author?: string;         // 作者
    website?: string;        // 网站
}
```

**示例**:

```javascript
this.info = {
    id: "com.example.myplugin",
    name: "My Awesome Plugin",
    version: "1.2.3",
    description: "一个很棒的插件",
    author: "Your Name",
    website: "https://github.com/yourname/myplugin"
};
```

#### 属性

| 属性 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `id` | `string` | ✅ | 插件唯一标识（反向域名格式） |
| `name` | `string` | ✅ | 插件显示名称 |
| `version` | `string` | ✅ | 版本号（推荐语义化版本） |
| `description` | `string` | ❌ | 插件功能描述 |
| `author` | `string` | ❌ | 作者名称 |
| `website` | `string` | ❌ | 项目主页或文档链接 |

---

## 日志系统

### Logger

日志记录器接口，用于输出日志信息。

```typescript
interface Logger {
    trace(message: string): void;
    debug(message: string): void;
    info(message: string): void;
    warning(message: string): void;
    warn(message: string): void;      // warning 的别名
    error(message: string): void;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger) {
        this.logger = logger;
    }
    
    async onEnable() {
        this.logger.trace("详细的追踪信息");
        this.logger.debug("调试信息");
        this.logger.info("一般信息");
        this.logger.warn("警告信息");
        this.logger.error("错误信息");
    }
    
    performOperation() {
        try {
            // 某些操作
            this.logger.info("操作成功完成");
        } catch (error) {
            this.logger.error(`操作失败: ${error.message}`);
        }
    }
}
```

#### 方法

| 方法 | 说明 | 级别 |
|------|------|------|
| `trace(message)` | 详细的追踪信息 | TRACE |
| `debug(message)` | 调试信息 | DEBUG |
| `info(message)` | 一般信息 | INFO |
| `warning(message)` | 警告信息 | WARNING |
| `warn(message)` | 警告信息（别名） | WARNING |
| `error(message)` | 错误信息 | ERROR |

#### Console 对象

除了注入的 Logger 外，插件还可以使用全局 `console` 对象：

```javascript
console.log("日志信息");    // 映射到 logger.info()
console.debug("调试信息");  // 映射到 logger.debug()
console.warn("警告");       // 映射到 logger.warning()
console.error("错误");      // 映射到 logger.error()
```

---

## 事件系统

### EventBus

事件总线接口，用于订阅和发布事件。

```typescript
interface EventBus {
    subscribe(eventName: string, handler: (event: any) => void | Promise<void>): void;
    unsubscribe(eventName: string, handler: Function): void;
    publish(eventName: string, eventData?: any): void;
    clearAll(): void;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, eventBus) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.handlers = {};
    }
    
    async onEnable() {
        // 绑定 this 上下文
        this.handlers.playerJoin = this.onPlayerJoin.bind(this);
        this.handlers.serverStart = this.onServerStart.bind(this);
        
        // 订阅事件
        this.eventBus.subscribe("PlayerJoinedEvent", this.handlers.playerJoin);
        this.eventBus.subscribe("ServerStartedEvent", this.handlers.serverStart);
    }
    
    async onDisable() {
        // 取消订阅（使用保存的引用）
        this.eventBus.unsubscribe("PlayerJoinedEvent", this.handlers.playerJoin);
        this.eventBus.unsubscribe("ServerStartedEvent", this.handlers.serverStart);
        
        // 或使用 clearAll()
        // this.eventBus.clearAll();
    }
    
    onPlayerJoin(event) {
        this.logger.info(`玩家 ${event.Player.Name} 加入了游戏`);
    }
    
    onServerStart(event) {
        this.logger.info("服务器已启动");
    }
}
```

#### 方法

| 方法 | 参数 | 说明 |
|------|------|------|
| `subscribe(eventName, handler)` | `eventName`: 事件名<br>`handler`: 处理函数 | 订阅事件 |
| `unsubscribe(eventName, handler)` | `eventName`: 事件名<br>`handler`: 处理函数引用 | 取消订阅 |
| `publish(eventName, eventData)` | `eventName`: 事件名<br>`eventData`: 事件数据 | 发布自定义事件 |
| `clearAll()` | 无 | 清除所有订阅（插件禁用时自动调用） |

### 常用事件类型

#### ServerStartedEvent

```typescript
interface ServerStartedEvent {
    timestamp: string;
}
```

#### PlayerJoinedEvent

```typescript
interface PlayerJoinedEvent {
    Player: {
        Name: string;
        Uuid: string;
    };
    Timestamp: Date;
}
```

#### PlayerLeftEvent

```typescript
interface PlayerLeftEvent {
    Player: {
        Name: string;
        Uuid: string;
    };
    Timestamp: Date;
}
```

**⚠️ 注意**: 
- 订阅和取消订阅必须使用**相同的函数引用**
- 建议在构造函数中保存 `bind()` 后的函数引用
- 在 `onDisable()` 中务必取消所有订阅

---

## 命令系统

### CommandRegistry

命令注册器接口，用于注册自定义命令。

```typescript
interface CommandRegistry {
    register(options: CommandOptions): void;
    unregister(name: string): void;
}

interface CommandOptions {
    name: string;
    callback: (context: CommandContext) => void | Promise<void>;
    description?: string;
    usage?: string;
    permission?: string;
    aliases?: string[];
}

interface CommandContext {
    sender: string;
    args: string[];
    reply(message: string): Promise<void>;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, commandRegistry) {
        this.logger = logger;
        this.commands = commandRegistry;
    }
    
    async onEnable() {
        // 注册命令
        this.commands.register({
            name: "hello",
            callback: this.cmdHello.bind(this),
            description: "打招呼命令",
            usage: "/hello [name]",
            permission: "myplugin.command.hello",
            aliases: ["hi", "greet"]
        });
        
        this.commands.register({
            name: "info",
            callback: this.cmdInfo.bind(this),
            description: "显示插件信息"
        });
    }
    
    async onDisable() {
        // 注销命令
        this.commands.unregister("hello");
        this.commands.unregister("info");
    }
    
    async cmdHello(ctx) {
        const name = ctx.args.length > 0 ? ctx.args[0] : ctx.sender;
        await ctx.reply(`Hello, ${name}!`);
        this.logger.info(`${ctx.sender} 执行了 hello 命令`);
    }
    
    async cmdInfo(ctx) {
        await ctx.reply(`插件: ${this.info.name} v${this.info.version}`);
        await ctx.reply(`作者: ${this.info.author}`);
    }
}
```

#### CommandRegistry 方法

| 方法 | 参数 | 说明 |
|------|------|------|
| `register(options)` | `options`: 命令配置 | 注册命令 |
| `unregister(name)` | `name`: 命令名 | 注销命令 |

#### CommandOptions 属性

| 属性 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `name` | `string` | ✅ | 命令名称 |
| `callback` | `Function` | ✅ | 命令处理函数 |
| `description` | `string` | ❌ | 命令描述 |
| `usage` | `string` | ❌ | 使用方法 |
| `permission` | `string` | ❌ | 所需权限 |
| `aliases` | `string[]` | ❌ | 命令别名 |

#### CommandContext 属性

| 属性/方法 | 类型 | 说明 |
|-----------|------|------|
| `sender` | `string` | 命令发送者 |
| `args` | `string[]` | 命令参数数组 |
| `reply(message)` | `Function` | 回复消息 |

---

## RCON 客户端

### RconClient

RCON 客户端接口，用于执行 Minecraft 服务器命令。

```typescript
interface RconClient {
    execute(command: string): Promise<RconResult>;
    executeBatch(commands: string[]): Promise<RconResult[]>;
    isConnected(): boolean;
}

interface RconResult {
    success: boolean;
    response: string;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, rcon) {
        this.logger = logger;
        this.rcon = rcon;
    }
    
    async teleportPlayer(playerName, x, y, z) {
        // 执行单个命令
        const result = await this.rcon.execute(`tp ${playerName} ${x} ${y} ${z}`);
        
        if (result.success) {
            this.logger.info(`已传送 ${playerName} 到 (${x}, ${y}, ${z})`);
            this.logger.debug(`服务器响应: ${result.response}`);
        } else {
            this.logger.error(`传送失败: ${result.response}`);
        }
    }
    
    async giveItems(playerName) {
        // 批量执行命令
        const commands = [
            `give ${playerName} diamond 64`,
            `give ${playerName} iron_ingot 128`,
            `give ${playerName} golden_apple 16`
        ];
        
        const results = await this.rcon.executeBatch(commands);
        
        const successCount = results.filter(r => r.success).length;
        this.logger.info(`成功执行 ${successCount}/${commands.length} 个命令`);
    }
    
    async checkConnection() {
        if (this.rcon.isConnected()) {
            this.logger.info("RCON 已连接");
        } else {
            this.logger.warn("RCON 未连接");
        }
    }
}
```

#### 方法

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `execute(command)` | `command`: 命令字符串 | `Promise<RconResult>` | 执行单个命令 |
| `executeBatch(commands)` | `commands`: 命令数组 | `Promise<RconResult[]>` | 批量执行命令 |
| `isConnected()` | 无 | `boolean` | 检查连接状态 |

#### RconResult 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `success` | `boolean` | 命令是否执行成功 |
| `response` | `string` | 服务器响应内容 |

**常用命令示例**:

```javascript
// 玩家管理
await rcon.execute("tp player1 100 64 200");
await rcon.execute("give player1 diamond 64");
await rcon.execute("gamemode creative player1");

// 世界管理
await rcon.execute("time set day");
await rcon.execute("weather clear");
await rcon.execute("difficulty hard");

// 信息查询
const result = await rcon.execute("list");
console.log(result.response);  // 在线玩家列表
```

---

## 调度器

### Scheduler

任务调度器接口，用于延迟或定时执行任务。

```typescript
interface Scheduler {
    runDelayed(callback: () => void | Promise<void>, delayMs: number): string;
    runRepeating(callback: () => void | Promise<void>, intervalMs: number, initialDelayMs?: number): string;
    cancel(taskId: string): void;
    cancelAll(): void;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, scheduler) {
        this.logger = logger;
        this.scheduler = scheduler;
        this.taskIds = [];
    }
    
    async onEnable() {
        // 延迟执行（5秒后）
        const delayedId = this.scheduler.runDelayed(() => {
            this.logger.info("延迟任务执行");
        }, 5000);
        this.taskIds.push(delayedId);
        
        // 定时重复执行（每10秒）
        const repeatingId = this.scheduler.runRepeating(
            this.checkStatus.bind(this),
            10000,  // 间隔10秒
            0       // 立即开始
        );
        this.taskIds.push(repeatingId);
        
        // 带初始延迟的重复任务（30秒后开始，每分钟执行）
        const delayedRepeatingId = this.scheduler.runRepeating(
            () => this.logger.info("每分钟任务"),
            60000,  // 间隔1分钟
            30000   // 30秒后开始
        );
        this.taskIds.push(delayedRepeatingId);
    }
    
    async onDisable() {
        // 取消所有任务
        for (const taskId of this.taskIds) {
            this.scheduler.cancel(taskId);
        }
        
        // 或使用 cancelAll()
        // this.scheduler.cancelAll();
    }
    
    checkStatus() {
        this.logger.info("执行状态检查...");
    }
}
```

#### 方法

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `runDelayed(callback, delayMs)` | `callback`: 回调函数<br>`delayMs`: 延迟毫秒数 | `string` | 延迟执行任务 |
| `runRepeating(callback, intervalMs, initialDelayMs)` | `callback`: 回调函数<br>`intervalMs`: 间隔毫秒数<br>`initialDelayMs`: 初始延迟（可选） | `string` | 定时重复任务 |
| `cancel(taskId)` | `taskId`: 任务ID | `void` | 取消指定任务 |
| `cancelAll()` | 无 | `void` | 取消所有任务 |

**时间单位转换**:

```javascript
const SECOND = 1000;
const MINUTE = 60 * SECOND;
const HOUR = 60 * MINUTE;

// 5秒后执行
scheduler.runDelayed(callback, 5 * SECOND);

// 每分钟执行
scheduler.runRepeating(callback, MINUTE);

// 1小时后开始，每30分钟执行
scheduler.runRepeating(callback, 30 * MINUTE, HOUR);
```

---

## 配置管理

### ConfigManager

配置管理器接口，用于加载和保存配置文件。

```typescript
interface ConfigManager {
    load(filename: string, defaults?: any): Promise<any>;
    get(path: string, defaultValue?: any): any;
    set(path: string, value: any): void;
    save(filename: string): Promise<void>;
    reload(filename: string): Promise<void>;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, configManager) {
        this.logger = logger;
        this.config = configManager;
    }
    
    async onLoad() {
        // 加载配置（如果不存在则使用默认值）
        const config = await this.config.load("config.yaml", {
            maxPlayers: 20,
            welcomeMessage: "Welcome to the server!",
            features: {
                teleport: true,
                kits: false,
                homes: true
            },
            locations: {
                spawn: { x: 0, y: 64, z: 0 }
            }
        });
        
        this.logger.info("配置已加载");
    }
    
    async onEnable() {
        // 读取配置值
        const maxPlayers = this.config.get("maxPlayers", 10);
        const message = this.config.get("welcomeMessage");
        
        // 使用点号路径访问嵌套属性
        const teleportEnabled = this.config.get("features.teleport");
        const spawnX = this.config.get("locations.spawn.x");
        
        this.logger.info(`最大玩家数: ${maxPlayers}`);
        this.logger.info(`传送功能: ${teleportEnabled ? "启用" : "禁用"}`);
        
        // 修改配置
        this.config.set("maxPlayers", 30);
        this.config.set("features.kits", true);
        
        // 保存配置
        await this.config.save("config.yaml");
    }
    
    async reloadConfig() {
        await this.config.reload("config.yaml");
        this.logger.info("配置已重新加载");
    }
}
```

#### 方法

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `load(filename, defaults)` | `filename`: 文件名<br>`defaults`: 默认配置（可选） | `Promise<any>` | 加载配置文件 |
| `get(path, defaultValue)` | `path`: 配置路径<br>`defaultValue`: 默认值（可选） | `any` | 获取配置值 |
| `set(path, value)` | `path`: 配置路径<br>`value`: 配置值 | `void` | 设置配置值 |
| `save(filename)` | `filename`: 文件名 | `Promise<void>` | 保存配置 |
| `reload(filename)` | `filename`: 文件名 | `Promise<any>` | 重新加载配置 |

**配置路径语法**:

```javascript
// 点号分隔的路径
config.get("features.teleport");        // 等同于 config.features.teleport
config.set("locations.spawn.x", 100);   // 设置嵌套属性

// 数组索引
config.get("players[0].name");
config.set("items[2].count", 64);
```

---

## 数据读取

### PlayerDataReader

玩家数据读取器接口，用于读取玩家存档数据。

```typescript
interface PlayerDataReader {
    read(playerName: string): Promise<any>;
    exists(playerName: string): Promise<boolean>;
    getOnlinePlayers(): Promise<string[]>;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, playerDataReader) {
        this.logger = logger;
        this.playerData = playerDataReader;
    }
    
    async getPlayerInfo(playerName) {
        // 检查玩家数据是否存在
        const exists = await this.playerData.exists(playerName);
        if (!exists) {
            this.logger.warn(`玩家 ${playerName} 的数据不存在`);
            return null;
        }
        
        // 读取玩家 NBT 数据
        const data = await this.playerData.read(playerName);
        
        if (data) {
            // 提取常用信息
            const pos = data.Pos;           // 位置 [x, y, z]
            const health = data.Health;     // 生命值
            const food = data.foodLevel;    // 饥饿值
            const xp = data.XpLevel;        // 经验等级
            const inventory = data.Inventory;  // 物品栏
            
            this.logger.info(`玩家: ${playerName}`);
            this.logger.info(`  位置: (${pos[0]}, ${pos[1]}, ${pos[2]})`);
            this.logger.info(`  生命值: ${health}/20`);
            this.logger.info(`  等级: ${xp}`);
            this.logger.info(`  物品数量: ${inventory.length}`);
            
            return {
                position: pos,
                health: health,
                food: food,
                level: xp,
                inventorySize: inventory.length
            };
        }
        
        return null;
    }
    
    async listOnlinePlayers() {
        const players = await this.playerData.getOnlinePlayers();
        this.logger.info(`在线玩家 (${players.length}):`);
        players.forEach(name => this.logger.info(`  - ${name}`));
    }
}
```

#### 方法

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `read(playerName)` | `playerName`: 玩家名 | `Promise<any>` | 读取玩家 NBT 数据 |
| `exists(playerName)` | `playerName`: 玩家名 | `Promise<boolean>` | 检查数据是否存在 |
| `getOnlinePlayers()` | 无 | `Promise<string[]>` | 获取在线玩家列表 |

**常用 NBT 字段**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `Pos` | `number[]` | 位置坐标 [x, y, z] |
| `Health` | `number` | 生命值 |
| `foodLevel` | `number` | 饥饿值 |
| `XpLevel` | `number` | 经验等级 |
| `Inventory` | `array` | 物品栏数据 |
| `EnderItems` | `array` | 末影箱数据 |
| `Dimension` | `string` | 所在维度 |

---

## 计分板

### ScoreboardApi

计分板 API 接口，用于管理记分板。

```typescript
interface ScoreboardApi {
    createObjective(name: string, criterion: string, displayName: string): Promise<void>;
    removeObjective(name: string): Promise<void>;
    setDisplay(slot: string, objective: string): Promise<void>;
    addScore(objective: string, player: string, points: number): Promise<void>;
    setScore(objective: string, player: string, points: number): Promise<void>;
    getScore(objective: string, player: string): Promise<number>;
    getScores(objective: string): Promise<Record<string, number>>;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, scoreboardApi) {
        this.logger = logger;
        this.scoreboard = scoreboardApi;
    }
    
    async onEnable() {
        // 创建排行榜
        await this.createLeaderboard();
    }
    
    async createLeaderboard() {
        // 创建计分板目标
        await this.scoreboard.createObjective(
            "kills",                  // 名称
            "playerKillCount",        // 评判标准
            "击杀排行"                // 显示名称
        );
        
        // 设置显示位置（sidebar = 侧边栏）
        await this.scoreboard.setDisplay("sidebar", "kills");
        
        this.logger.info("击杀排行榜已创建");
    }
    
    async addKill(playerName) {
        // 增加分数
        await this.scoreboard.addScore("kills", playerName, 1);
        
        // 获取当前分数
        const score = await this.scoreboard.getScore("kills", playerName);
        this.logger.info(`${playerName} 的击杀数: ${score}`);
    }
    
    async resetScore(playerName) {
        // 设置分数为0
        await this.scoreboard.setScore("kills", playerName, 0);
    }
    
    async getTopPlayers(limit = 10) {
        // 获取所有分数
        const scores = await this.scoreboard.getScores("kills");
        
        // 排序
        const sorted = Object.entries(scores)
            .sort(([, a], [, b]) => b - a)  // 降序
            .slice(0, limit);
        
        this.logger.info("排行榜前10名:");
        sorted.forEach(([name, score], index) => {
            this.logger.info(`  ${index + 1}. ${name}: ${score} 击杀`);
        });
        
        return sorted;
    }
}
```

#### 方法

| 方法 | 参数 | 说明 |
|------|------|------|
| `createObjective(name, criterion, displayName)` | 创建计分板目标 |
| `removeObjective(name)` | 删除计分板目标 |
| `setDisplay(slot, objective)` | 设置显示位置 |
| `addScore(objective, player, points)` | 增加分数 |
| `setScore(objective, player, points)` | 设置分数 |
| `getScore(objective, player)` | 获取分数 |
| `getScores(objective)` | 获取所有分数 |

**常用评判标准**:

- `dummy` - 只能通过命令修改
- `playerKillCount` - 玩家击杀数
- `deathCount` - 死亡次数
- `totalKillCount` - 总击杀数（包括生物）

**显示位置**:

- `sidebar` - 右侧边栏
- `belowName` - 玩家名称下方
- `list` - Tab 列表

---

## 权限系统

### PermissionManager

权限管理器接口，用于管理玩家权限。

```typescript
interface PermissionManager {
    hasPermission(player: string, permission: string): Promise<boolean>;
    grantPermission(player: string, permission: string): Promise<void>;
    revokePermission(player: string, permission: string): Promise<void>;
    getPlayerPermissions(player: string): Promise<string[]>;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, permissionManager) {
        this.logger = logger;
        this.permissions = permissionManager;
    }
    
    async checkPlayerPermission(playerName, action) {
        const perm = `myplugin.${action}`;
        
        if (await this.permissions.hasPermission(playerName, perm)) {
            this.logger.info(`${playerName} 有权限执行 ${action}`);
            return true;
        } else {
            this.logger.warn(`${playerName} 没有权限: ${perm}`);
            return false;
        }
    }
    
    async grantVipPermissions(playerName) {
        const vipPerms = [
            "myplugin.teleport",
            "myplugin.fly",
            "myplugin.kit.vip"
        ];
        
        for (const perm of vipPerms) {
            await this.permissions.grantPermission(playerName, perm);
        }
        
        this.logger.info(`已授予 ${playerName} VIP 权限`);
    }
    
    async listPermissions(playerName) {
        const perms = await this.permissions.getPlayerPermissions(playerName);
        this.logger.info(`${playerName} 的权限:`);
        perms.forEach(p => this.logger.info(`  - ${p}`));
    }
}
```

---

## WebSocket

### WebSocketServer

WebSocket 服务器接口，用于向客户端推送数据。

```typescript
interface WebSocketServer {
    broadcast(message: WebSocketMessage | any): Promise<void>;
    send(clientId: string, message: WebSocketMessage | any): Promise<void>;
    getConnectedClients(): string[];
    isClientConnected(clientId: string): boolean;
}

interface WebSocketMessage {
    type: string;
    data: any;
    timestamp: string;
}
```

**示例**:

```javascript
class MyPlugin {
    constructor(logger, webSocketServer) {
        this.logger = logger;
        this.ws = webSocketServer;
    }
    
    async broadcastPlayerJoin(playerName) {
        await this.ws.broadcast({
            type: "player_join",
            plugin: "myplugin",
            data: {
                player: playerName,
                timestamp: new Date().toISOString()
            }
        });
    }
    
    async sendToClient(clientId, data) {
        if (this.ws.isClientConnected(clientId)) {
            await this.ws.send(clientId, {
                type: "custom_event",
                data: data
            });
        }
    }
    
    listClients() {
        const clients = this.ws.getConnectedClients();
        this.logger.info(`已连接的客户端: ${clients.length}`);
    }
}
```

---

## 工具类

### 全局对象

#### console

全局 console 对象，映射到 Logger：

```javascript
console.log("信息");      // -> logger.info()
console.debug("调试");    // -> logger.debug()
console.warn("警告");     // -> logger.warning()
console.error("错误");    // -> logger.error()
```

#### module / exports

CommonJS 模块系统：

```javascript
// 导出类
class MyPlugin { }
module.exports = MyPlugin;

// 或使用 exports
exports.MyPlugin = MyPlugin;
```

---

## 完整示例

以下是一个综合示例，展示了大部分 API 的使用：

```javascript
/**
 * 综合示例插件
 */
class ComprehensivePlugin {
    constructor(logger, eventBus, commandRegistry, rcon, scheduler, configManager) {
        this.logger = logger;
        this.eventBus = eventBus;
        this.commands = commandRegistry;
        this.rcon = rcon;
        this.scheduler = scheduler;
        this.config = configManager;
        
        this.info = {
            id: "com.example.comprehensive",
            name: "Comprehensive Plugin",
            version: "1.0.0",
            description: "展示所有 API 功能的示例插件"
        };
        
        this.handlers = {};
        this.taskIds = [];
    }
    
    async onLoad() {
        // 加载配置
        await this.config.load("config.yaml", {
            welcomeMessage: "Welcome!",
            checkInterval: 30000
        });
        
        this.logger.info("插件配置已加载");
    }
    
    async onEnable() {
        this.logger.info("综合示例插件正在启用...");
        
        // 注册命令
        this.registerCommands();
        
        // 订阅事件
        this.subscribeEvents();
        
        // 启动定时任务
        this.startScheduledTasks();
        
        this.logger.info("插件已启用");
    }
    
    async onDisable() {
        // 清理资源
        this.eventBus.clearAll();
        
        for (const taskId of this.taskIds) {
            this.scheduler.cancel(taskId);
        }
        
        this.commands.unregister("hello");
        this.commands.unregister("teleport");
        
        this.logger.info("插件已禁用");
    }
    
    registerCommands() {
        this.commands.register({
            name: "hello",
            callback: this.cmdHello.bind(this),
            description: "打招呼",
            usage: "/hello [name]"
        });
        
        this.commands.register({
            name: "teleport",
            callback: this.cmdTeleport.bind(this),
            description: "传送",
            usage: "/teleport <x> <y> <z>",
            permission: "plugin.teleport"
        });
    }
    
    subscribeEvents() {
        this.handlers.playerJoin = this.onPlayerJoin.bind(this);
        this.eventBus.subscribe("PlayerJoinedEvent", this.handlers.playerJoin);
    }
    
    startScheduledTasks() {
        // 每30秒检查一次
        const interval = this.config.get("checkInterval", 30000);
        const taskId = this.scheduler.runRepeating(
            this.periodicCheck.bind(this),
            interval
        );
        this.taskIds.push(taskId);
    }
    
    async cmdHello(ctx) {
        const name = ctx.args[0] || ctx.sender;
        const message = this.config.get("welcomeMessage");
        await ctx.reply(`${message}, ${name}!`);
    }
    
    async cmdTeleport(ctx) {
        if (ctx.args.length < 3) {
            await ctx.reply("用法: /teleport <x> <y> <z>");
            return;
        }
        
        const [x, y, z] = ctx.args.map(Number);
        const result = await this.rcon.execute(`tp ${ctx.sender} ${x} ${y} ${z}`);
        
        if (result.success) {
            await ctx.reply(`已传送到 (${x}, ${y}, ${z})`);
        } else {
            await ctx.reply("传送失败");
        }
    }
    
    async onPlayerJoin(event) {
        const playerName = event.Player.Name;
        this.logger.info(`玩家 ${playerName} 加入了游戏`);
        
        // 发送欢迎消息
        const message = this.config.get("welcomeMessage");
        await this.rcon.execute(
            `tellraw ${playerName} {"text":"${message}","color":"green"}`
        );
    }
    
    periodicCheck() {
        this.logger.debug("执行定期检查...");
    }
}

module.exports = ComprehensivePlugin;
```

---

## 游戏显示 API

### GameDisplayApi

游戏内显示功能接口（需要 RCON 支持）。

```typescript
interface GameDisplayApi {
    // Boss 血条
    showBossBar(id: string, title: string, progress: number, color?: string, style?: string): Promise<void>;
    updateBossBar(id: string, progress?: number, title?: string): Promise<void>;
    hideBossBar(id: string): Promise<void>;
    
    // 标题
    showTitle(selector: string, title: string, subtitle?: string, fadeIn?: number, stay?: number, fadeOut?: number): Promise<void>;
    showSubtitle(selector: string, subtitle: string): Promise<void>;
    clearTitle(selector: string): Promise<void>;
    
    // 动作栏
    showActionBar(selector: string, text: string): Promise<void>;
    
    // 聊天消息
    sendChatMessage(selector: string, message: string): Promise<void>;
    broadcastMessage(message: string): Promise<void>;
    
    // 告示牌编辑
    openSignEditor(playerName: string, x: number, y: number, z: number): Promise<void>;
}
```

**示例**:

```javascript
class DisplayPlugin {
    constructor(gameDisplay, logger) {
        this.display = gameDisplay;
        this.logger = logger;
    }
    
    async showWelcome(playerName) {
        // 显示 Boss 血条
        await this.display.showBossBar(
            `welcome_${playerName}`,
            `§a欢迎 ${playerName}！`,
            1.0,
            "green",
            "progress"
        );
        
        // 显示标题
        await this.display.showTitle(
            playerName,
            "§6欢迎来到服务器",
            "§e请遵守规则",
            10, 70, 20
        );
        
        // 显示动作栏
        await this.display.showActionBar(
            playerName,
            "§7输入 /help 查看帮助"
        );
        
        // 5秒后隐藏 Boss 血条
        setTimeout(async () => {
            await this.display.hideBossBar(`welcome_${playerName}`);
        }, 5000);
    }
}
```

---

## 游戏工具 API

### GameUtilities

高级游戏操作工具（需要 RCON 支持）。

```typescript
interface GameUtilities {
    // 烟花系统
    launchFirework(position: Position, options: FireworkOptions): Promise<void>;
    launchFireworkShow(positions: Position[], options: FireworkOptions, intervalMs?: number): Promise<void>;
    
    // 命令序列
    createSequence(): CommandSequence;
    
    // 区域操作
    fillArea(region: Region, blockType: string): Promise<void>;
    cloneArea(source: Region, destination: Position): Promise<void>;
    setBlock(position: Position, blockType: string): Promise<void>;
    
    // 时间和天气
    setTime(time: number | string): Promise<void>;
    setWeather(weather: string, duration?: number): Promise<void>;
    
    // 传送
    teleport(selector: string, position: Position): Promise<void>;
    teleportRelative(selector: string, offset: Position): Promise<void>;
    
    // 效果
    giveEffect(selector: string, effect: string, duration: number, amplifier?: number, hideParticles?: boolean): Promise<void>;
    clearEffects(selector: string): Promise<void>;
    
    // 粒子
    spawnParticle(particle: string, position: Position, count?: number, spread?: Position, speed?: number): Promise<void>;
    
    // 声音
    playSound(selector: string, sound: string, volume?: number, pitch?: number): Promise<void>;
    playSoundAt(position: Position, sound: string, volume?: number, pitch?: number): Promise<void>;
    stopSound(selector: string, sound?: string): Promise<void>;
}

interface Position {
    x: number;
    y: number;
    z: number;
}

interface Region {
    from: Position;
    to: Position;
}

interface FireworkOptions {
    type: string;        // 'small_ball', 'large_ball', 'star', 'creeper', 'burst'
    colors: string[];
    fadeColors?: string[];
    flicker?: boolean;
    trail?: boolean;
    power?: number;
}

interface CommandSequence {
    execute(action: () => void | Promise<void>): CommandSequence;
    waitTicks(ticks: number): CommandSequence;
    waitSeconds(seconds: number): CommandSequence;
    repeat(times: number): CommandSequence;
    run(): Promise<void>;
}
```

**示例**:

```javascript
class UtilsPlugin {
    constructor(gameUtils, logger) {
        this.utils = gameUtils;
        this.logger = logger;
    }
    
    async launchCelebration(x, y, z) {
        // 发射烟花
        const position = { x, y, z };
        const options = {
            type: 'large_ball',
            colors: ['red', 'gold', 'blue'],
            flicker: true,
            trail: true,
            power: 2
        };
        
        await this.utils.launchFirework(position, options);
        
        // 播放声音
        await this.utils.playSound(
            '@a',
            'minecraft:entity.firework_rocket.launch',
            1.0,
            1.0
        );
    }
    
    async countdownSequence(playerName) {
        await this.utils.createSequence()
            .execute(() => this.logger.info("3..."))
            .waitSeconds(1)
            .execute(() => this.logger.info("2..."))
            .waitSeconds(1)
            .execute(() => this.logger.info("1..."))
            .waitSeconds(1)
            .execute(() => this.logger.info("GO!"))
            .run();
    }
}
```

---

## 方块数据 API

### BlockDataReader

方块实体数据读取器。

```typescript
interface BlockDataReader {
    getChestItems(position: Position): Promise<ItemStack[]>;
    getContainerData(position: Position): Promise<ContainerData | null>;
    getSignText(position: Position): Promise<string[]>;
    getBlockEntity(position: Position): Promise<any>;
    isContainer(position: Position): Promise<boolean>;
    getHopperItems(position: Position): Promise<ItemStack[]>;
    getBarrelItems(position: Position): Promise<ItemStack[]>;
    getShulkerBoxItems(position: Position): Promise<ItemStack[]>;
}

interface ItemStack {
    id: string;
    count: number;
    slot: number;
    enchantments?: Enchantment[];
    customName?: string;
}

interface ContainerData {
    position: Position;
    type: string;
    items: ItemStack[];
    customName?: string;
    lock?: string;
}
```

### BlockDataWriter

方块实体数据写入器（需要 RCON 支持）。

```typescript
interface BlockDataWriter {
    setChestItems(position: Position, items: ItemStack[]): Promise<void>;
    setContainerItems(position: Position, items: ItemStack[]): Promise<void>;
    setSignText(position: Position, lines: string[]): Promise<void>;
    updateBlockEntity(position: Position, nbtData: any): Promise<void>;
    clearContainer(position: Position): Promise<void>;
    sortContainer(position: Position, by?: string): Promise<void>;
}
```

**示例**:

```javascript
class ChestPlugin {
    constructor(blockReader, blockWriter, logger) {
        this.reader = blockReader;
        this.writer = blockWriter;
        this.logger = logger;
    }
    
    async getChestContents(x, y, z) {
        const position = { x, y, z };
        const items = await this.reader.getChestItems(position);
        
        this.logger.info(`箱子 (${x}, ${y}, ${z}) 的内容:`);
        items.forEach(item => {
            this.logger.info(`[${item.slot}] ${item.id} x${item.count}`);
        });
        
        return items;
    }
    
    async setRewardChest(x, y, z) {
        const position = { x, y, z };
        const items = [
            { id: 'minecraft:diamond', count: 64, slot: 0 },
            { id: 'minecraft:gold_ingot', count: 32, slot: 1 }
        ];
        
        await this.writer.setChestItems(position, items);
        this.logger.info("奖励箱已设置");
    }
}
```

---

## 音乐播放器 API

### MusicPlayer

使用音符盒音效播放音乐（需要 RCON 支持）。

```typescript
interface MusicPlayer {
    createMelody(): Melody;
    stopAll(selector: string): Promise<void>;
    playCMajorScale(selector: string): Promise<void>;
    playTwinkleStar(selector: string): Promise<void>;
}

interface Melody {
    addNote(note: string, durationMs: number, octave?: number, sharp?: boolean): Melody;
    addRest(durationMs: number): Melody;
    setInstrument(instrument: string): Melody;
    setVolume(volume: number): Melody;
    play(selector: string): Promise<void>;
    loop(selector: string, times: number): Promise<void>;
}
```

**示例**:

```javascript
class MusicPlugin {
    constructor(musicPlayer, logger) {
        this.player = musicPlayer;
        this.logger = logger;
    }
    
    async playWelcomeMelody(playerName) {
        const melody = this.player.createMelody();
        
        melody.setInstrument('harp')
              .setVolume(1.0)
              .addNote('C', 200)
              .addNote('E', 200)
              .addNote('G', 400);
        
        await melody.play(playerName);
        this.logger.info(`已为 ${playerName} 播放欢迎音乐`);
    }
}
```

---

## 高级功能 API

### 玩家档案 API

```typescript
interface PlayerProfileApi {
    getProfile(playerName: string): Promise<PlayerProfile | null>;
    getProfileByUuid(uuid: string): Promise<PlayerProfile | null>;
    getSkinUrl(playerName: string): Promise<string | null>;
    getCapeUrl(playerName: string): Promise<string | null>;
}

interface PlayerProfile {
    uuid: string;
    name: string;
    properties: ProfileProperty[];
}
```

### 标签系统 API

```typescript
interface TagApi {
    getBlockTags(block: string): Promise<string[]>;
    getItemTags(item: string): Promise<string[]>;
    getEntityTags(entity: string): Promise<string[]>;
    hasTag(type: string, name: string, tag: string): Promise<boolean>;
    getAllTags(type: string): Promise<string[]>;
}
```

### 成就追踪 API

```typescript
interface AdvancementTracker {
    getPlayerAdvancements(playerUuid: string): Promise<AdvancementProgress[]>;
    isAdvancementCompleted(playerUuid: string, advancement: string): Promise<boolean>;
    getCompletionPercentage(playerUuid: string): Promise<number>;
    onAdvancementCompleted(handler: (playerUuid: string, advancementName: string) => void): void;
}
```

### 排行榜系统 API

```typescript
interface LeaderboardSystem {
    create(name: string, sortOrder?: string): Promise<Leaderboard>;
    get(name: string): Promise<Leaderboard | null>;
    delete(name: string): Promise<void>;
    list(): Promise<string[]>;
}

interface Leaderboard {
    getName(): string;
    addScore(player: string, score: number, metadata?: any): Promise<void>;
    getScore(player: string): Promise<number>;
    getRank(player: string): Promise<number>;
    getTop(limit?: number): Promise<LeaderboardEntry[]>;
    remove(player: string): Promise<void>;
    clear(): Promise<void>;
    getAll(): Promise<LeaderboardEntry[]>;
}
```

---

## 相关文档

- [JavaScript 核心 API 使用指南](../03-插件开发/JavaScript核心API使用指南.md)
- [JavaScript 插件开发指南](../03-插件开发/JavaScript插件开发指南.md)
- [JavaScript 插件架构说明](../05-配置和部署/JavaScript插件架构说明.md)
- [C# API 参考](./API参考.md)
- [示例插件集](../07-示例和最佳实践/JavaScript示例插件集.md)

---

**注意**: JavaScript API 功能持续更新中，部分 API 可能尚未完全实现。如遇问题请参考文档或提交 Issue。

