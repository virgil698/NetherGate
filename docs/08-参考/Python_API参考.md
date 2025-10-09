# Python API 参考

NetherGate Python API 提供了与 C# API 对等的功能，允许 Python 开发者使用熟悉的语法开发插件。

> **⚠️ 重要说明**
>
> 本文档描述的 Python API 是 C# API 的桥接封装。虽然我们努力提供完整的功能覆盖，但请注意：
>
> 1. **功能完整性**：部分高级功能或新增 API 可能尚未在 Python 中实现
> 2. **性能考虑**：跨语言调用会引入额外开销，建议避免在热路径中频繁调用
> 3. **类型系统**：Python 的动态类型可能导致运行时错误，建议使用类型注解和类型检查工具
> 4. **API 更新**：Python API 的更新可能略滞后于 C# API
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

插件基类，所有插件必须继承此类。

```python
from nethergate import Plugin, PluginInfo

class MyPlugin(Plugin):
    def __init__(self):
        self.info = PluginInfo(
            id="com.example.plugin",
            name="My Plugin",
            version="1.0.0"
        )
    
    async def on_load(self): pass
    async def on_enable(self): pass
    async def on_disable(self): pass
    async def on_unload(self): pass
```

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `info` | `PluginInfo` | 插件元数据 |

#### 生命周期方法

| 方法 | 说明 |
|------|------|
| `async on_load()` | 插件加载时调用 |
| `async on_enable()` | 插件启用时调用 |
| `async on_disable()` | 插件禁用时调用 |
| `async on_unload()` | 插件卸载时调用 |

---

### PluginInfo

插件元数据类。

```python
from nethergate import PluginInfo

info = PluginInfo(
    id="com.example.plugin",
    name="My Plugin",
    version="1.0.0",
    description="插件描述",
    author="作者名",
    website="https://example.com",
    dependencies=["com.example.dependency"],
    soft_dependencies=["com.example.optional"],
    load_order=100
)
```

#### 属性

| 属性 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `id` | `str` | ✅ | 插件唯一标识 |
| `name` | `str` | ✅ | 插件名称 |
| `version` | `str` | ✅ | 版本号 |
| `description` | `str` | ❌ | 描述 |
| `author` | `str` | ❌ | 作者 |
| `website` | `str` | ❌ | 网站 |
| `dependencies` | `List[str]` | ❌ | 硬依赖 |
| `soft_dependencies` | `List[str]` | ❌ | 软依赖 |
| `load_order` | `int` | ❌ | 加载顺序 |

---

## 日志系统

### Logger

日志记录器，支持多级别日志输出。

```python
from nethergate.logging import Logger, LogLevel

class MyPlugin(Plugin):
    def __init__(self, logger: Logger):
        self.logger = logger
    
    async def on_enable(self):
        self.logger.trace("跟踪信息")
        self.logger.debug("调试信息")
        self.logger.info("一般信息")
        self.logger.warning("警告信息")
        self.logger.error("错误信息")
        
        # 带异常的日志
        try:
            raise ValueError("测试")
        except Exception as e:
            self.logger.error("发生错误", exception=e)
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `trace` | `trace(message: str)` | 跟踪级别日志 |
| `debug` | `debug(message: str)` | 调试级别日志 |
| `info` | `info(message: str)` | 信息级别日志 |
| `warning` | `warning(message: str)` | 警告级别日志 |
| `error` | `error(message: str, exception: Exception = None)` | 错误级别日志 |
| `set_level` | `set_level(level: LogLevel)` | 设置日志级别 |

#### LogLevel 枚举

```python
from nethergate.logging import LogLevel

LogLevel.TRACE     # 0
LogLevel.DEBUG     # 1
LogLevel.INFO      # 2
LogLevel.WARNING   # 3
LogLevel.ERROR     # 4
```

---

## 事件系统

### EventBus

事件总线，用于订阅和发布事件。

```python
from nethergate.events import EventBus, ServerStartedEvent, PlayerJoinEvent

class MyPlugin(Plugin):
    def __init__(self, event_bus: EventBus):
        self.event_bus = event_bus
    
    async def on_enable(self):
        # 订阅事件
        self.event_bus.subscribe(ServerStartedEvent, self.on_server_started)
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
    
    async def on_disable(self):
        # 取消订阅
        self.event_bus.unsubscribe(ServerStartedEvent, self.on_server_started)
        self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
    
    async def on_server_started(self, event: ServerStartedEvent):
        self.logger.info("服务器已启动")
    
    async def on_player_join(self, event: PlayerJoinEvent):
        self.logger.info(f"{event.player_name} 加入游戏")
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `subscribe` | `subscribe(event_type: Type[Event], handler: Callable)` | 订阅事件 |
| `unsubscribe` | `unsubscribe(event_type: Type[Event], handler: Callable)` | 取消订阅 |
| `publish` | `async publish(event: Event)` | 发布事件 |

### 常用事件

#### 服务器事件

```python
from nethergate.events import (
    ServerStartingEvent,   # 服务器启动中
    ServerStartedEvent,    # 服务器已启动
    ServerStoppingEvent,   # 服务器停止中
    ServerStoppedEvent,    # 服务器已停止
)

async def on_server_started(self, event: ServerStartedEvent):
    # 事件属性
    event.timestamp  # 时间戳
```

#### 玩家事件

```python
from nethergate.events import (
    PlayerJoinEvent,       # 玩家加入
    PlayerLeaveEvent,      # 玩家离开
    PlayerChatEvent,       # 玩家聊天
    PlayerDeathEvent,      # 玩家死亡
    PlayerAdvancementEvent # 玩家达成成就
)

async def on_player_join(self, event: PlayerJoinEvent):
    event.player_name    # 玩家名
    event.player_uuid    # 玩家 UUID
    event.timestamp      # 时间戳

async def on_player_chat(self, event: PlayerChatEvent):
    event.player_name    # 玩家名
    event.message        # 消息内容
    event.cancel()       # 取消事件
```

#### 网络事件

```python
from nethergate.events import (
    RconConnectedEvent,     # RCON 已连接
    RconDisconnectedEvent,  # RCON 已断开
    WebSocketClientConnected,    # WebSocket 客户端连接
    WebSocketClientDisconnected  # WebSocket 客户端断开
)
```

---

## 命令系统

### CommandRegistry

命令注册器，用于注册和管理自定义命令。

```python
from nethergate.commands import CommandRegistry, CommandContext

class MyPlugin(Plugin):
    def __init__(self, commands: CommandRegistry):
        self.commands = commands
    
    async def on_enable(self):
        # 注册命令
        self.commands.register(
            name="hello",
            callback=self.cmd_hello,
            description="打招呼",
            usage="/hello [name]",
            permission="myplugin.hello",
            aliases=["hi", "greet"]
        )
    
    async def on_disable(self):
        # 注销命令
        self.commands.unregister("hello")
    
    async def cmd_hello(self, ctx: CommandContext):
        """命令处理函数"""
        if len(ctx.args) > 0:
            name = ctx.args[0]
            await ctx.reply(f"Hello, {name}!")
        else:
            await ctx.reply("Hello, World!")
```

#### CommandRegistry 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `register` | `register(name: str, callback: Callable, description: str = "", usage: str = "", permission: str = None, aliases: List[str] = None)` | 注册命令 |
| `unregister` | `unregister(name: str)` | 注销命令 |
| `get_command` | `get_command(name: str) -> Command` | 获取命令信息 |
| `list_commands` | `list_commands() -> List[str]` | 列出所有命令 |

### CommandContext

命令执行上下文。

```python
async def cmd_example(self, ctx: CommandContext):
    # 获取命令参数
    args = ctx.args           # List[str]: 参数列表
    sender = ctx.sender       # str: 命令发送者
    is_console = ctx.is_console  # bool: 是否来自控制台
    
    # 回复消息
    await ctx.reply("消息内容")
    
    # 检查权限
    if not ctx.has_permission("myplugin.admin"):
        await ctx.reply("权限不足")
        return
    
    # 验证参数
    if len(ctx.args) < 1:
        await ctx.reply(f"用法: {ctx.usage}")
        return
```

#### CommandContext 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `command_name` | `str` | 命令名称 |
| `args` | `List[str]` | 参数列表 |
| `sender` | `str` | 发送者 |
| `is_console` | `bool` | 是否控制台命令 |
| `usage` | `str` | 用法说明 |

#### CommandContext 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `reply` | `async reply(message: str)` | 回复消息 |
| `has_permission` | `has_permission(permission: str) -> bool` | 检查权限 |

---

## RCON 客户端

### RconClient

RCON 客户端，用于执行 Minecraft 服务器命令。

```python
from nethergate.rcon import RconClient, RconResponse

class MyPlugin(Plugin):
    def __init__(self, rcon: RconClient):
        self.rcon = rcon
    
    async def execute_command(self):
        # 执行单条命令
        result = await self.rcon.execute("list")
        if result.success:
            self.logger.info(f"在线玩家: {result.response}")
        else:
            self.logger.error(f"命令执行失败: {result.error}")
        
        # 批量执行命令
        commands = [
            "say Hello!",
            "time set day",
            "weather clear"
        ]
        results = await self.rcon.execute_batch(commands)
        for cmd, result in zip(commands, results):
            if result.success:
                self.logger.info(f"{cmd} 成功")
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `execute` | `async execute(command: str) -> RconResponse` | 执行命令 |
| `execute_batch` | `async execute_batch(commands: List[str]) -> List[RconResponse]` | 批量执行命令 |
| `is_connected` | `is_connected() -> bool` | 检查连接状态 |

### RconResponse

RCON 响应对象。

```python
response = await self.rcon.execute("list")

# 属性
response.success      # bool: 是否成功
response.response     # str: 响应内容
response.error        # str: 错误信息（如果失败）
response.request_id   # int: 请求 ID
```

---

## 调度器

### Scheduler

任务调度器，用于延迟执行和定时执行任务。

```python
from nethergate.scheduling import Scheduler

class MyPlugin(Plugin):
    def __init__(self, scheduler: Scheduler):
        self.scheduler = scheduler
        self.task_ids = []
    
    async def on_enable(self):
        # 延迟执行（5秒后）
        self.scheduler.run_delayed(
            callback=self.delayed_task,
            delay_seconds=5.0
        )
        
        # 定时重复执行（每10秒）
        task_id = self.scheduler.run_repeating(
            callback=self.repeating_task,
            interval_seconds=10.0,
            initial_delay=0.0
        )
        self.task_ids.append(task_id)
        
        # 在指定时间执行
        from datetime import datetime, timedelta
        run_at = datetime.now() + timedelta(hours=1)
        self.scheduler.run_at(
            callback=self.scheduled_task,
            run_time=run_at
        )
    
    async def on_disable(self):
        # 取消所有任务
        for task_id in self.task_ids:
            self.scheduler.cancel(task_id)
    
    async def delayed_task(self):
        self.logger.info("延迟任务执行")
    
    async def repeating_task(self):
        self.logger.info("定时任务执行")
    
    async def scheduled_task(self):
        self.logger.info("计划任务执行")
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `run_delayed` | `run_delayed(callback: Callable, delay_seconds: float) -> str` | 延迟执行 |
| `run_repeating` | `run_repeating(callback: Callable, interval_seconds: float, initial_delay: float = 0.0) -> str` | 定时重复执行 |
| `run_at` | `run_at(callback: Callable, run_time: datetime) -> str` | 在指定时间执行 |
| `cancel` | `cancel(task_id: str)` | 取消任务 |
| `cancel_all` | `cancel_all()` | 取消所有任务 |

---

## 配置管理

### ConfigManager

配置管理器，支持 YAML 和 JSON 格式。

```python
from nethergate.config import ConfigManager

class MyPlugin(Plugin):
    def __init__(self, config: ConfigManager):
        self.config = config
    
    async def on_load(self):
        # 加载配置（如果不存在则创建默认配置）
        default_config = {
            "enabled": True,
            "max_players": 20,
            "features": {
                "teleport": True,
                "kits": False
            },
            "messages": {
                "welcome": "Welcome!",
                "goodbye": "See you!"
            }
        }
        self.config.load("config.yaml", default=default_config)
        
        # 读取配置
        enabled = self.config.get("enabled", default=False)
        max_players = self.config.get("max_players")
        teleport_enabled = self.config.get("features.teleport")  # 支持点号路径
        welcome_msg = self.config.get("messages.welcome")
        
        # 修改配置
        self.config.set("max_players", 30)
        self.config.set("features.kits", True)
        
        # 保存配置
        self.config.save("config.yaml")
        
        # 重载配置
        self.config.reload("config.yaml")
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `load` | `load(filename: str, default: dict = None) -> dict` | 加载配置文件 |
| `save` | `save(filename: str)` | 保存配置文件 |
| `reload` | `reload(filename: str) -> dict` | 重载配置文件 |
| `get` | `get(path: str, default: Any = None) -> Any` | 获取配置值 |
| `set` | `set(path: str, value: Any)` | 设置配置值 |
| `set_all` | `set_all(data: dict)` | 设置所有配置 |
| `has` | `has(path: str) -> bool` | 检查配置是否存在 |
| `delete` | `delete(path: str)` | 删除配置项 |

---

## 数据读取

### PlayerDataReader

玩家数据读取器，用于读取玩家的 NBT 数据。

```python
from nethergate.data import PlayerDataReader

class MyPlugin(Plugin):
    def __init__(self, player_data: PlayerDataReader):
        self.player_data = player_data
    
    async def get_player_info(self, player_name: str):
        """获取玩家信息"""
        data = await self.player_data.read(player_name)
        
        if data:
            # 读取玩家数据
            pos = data.get("Pos")           # 位置 [x, y, z]
            health = data.get("Health")     # 生命值
            food = data.get("foodLevel")    # 饥饿值
            xp = data.get("XpLevel")        # 经验等级
            inventory = data.get("Inventory")  # 背包
            ender_items = data.get("EnderItems")  # 末影箱
            
            self.logger.info(f"{player_name}:")
            self.logger.info(f"  位置: {pos}")
            self.logger.info(f"  生命: {health}")
            self.logger.info(f"  饥饿: {food}")
            self.logger.info(f"  等级: {xp}")
            self.logger.info(f"  物品数: {len(inventory)}")
            
            return data
        else:
            self.logger.warning(f"玩家 {player_name} 数据不存在")
            return None
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `read` | `async read(player_name: str) -> dict` | 读取玩家数据 |
| `read_uuid` | `async read_uuid(player_uuid: str) -> dict` | 通过 UUID 读取 |
| `exists` | `exists(player_name: str) -> bool` | 检查玩家数据是否存在 |
| `list_players` | `list_players() -> List[str]` | 列出所有玩家 |

### BlockDataReader

方块数据读取器，用于读取世界方块数据。

```python
from nethergate.data import BlockDataReader

class MyPlugin(Plugin):
    def __init__(self, block_data: BlockDataReader):
        self.block_data = block_data
    
    async def read_block(self, x: int, y: int, z: int):
        """读取指定位置的方块"""
        block = await self.block_data.read_block(x, y, z)
        
        self.logger.info(f"方块 ({x}, {y}, {z}):")
        self.logger.info(f"  类型: {block.block_type}")
        self.logger.info(f"  状态: {block.block_state}")
        
        return block
```

---

## 计分板

### ScoreboardManager

计分板管理器，用于操作游戏计分板。

```python
from nethergate.scoreboard import ScoreboardManager

class MyPlugin(Plugin):
    def __init__(self, scoreboard: ScoreboardManager):
        self.scoreboard = scoreboard
    
    async def setup_leaderboard(self):
        """创建排行榜"""
        # 创建计分板目标
        await self.scoreboard.create_objective(
            name="kills",
            criterion="playerKillCount",
            display_name="击杀排行"
        )
        
        # 设置显示位置
        await self.scoreboard.set_display(
            slot="sidebar",      # 侧边栏
            objective="kills"
        )
    
    async def add_player_score(self, player: str, points: int):
        """增加玩家分数"""
        await self.scoreboard.add_score("kills", player, points)
    
    async def get_top_players(self, limit: int = 10):
        """获取排名前N的玩家"""
        scores = await self.scoreboard.get_scores("kills")
        sorted_scores = sorted(
            scores.items(),
            key=lambda x: x[1],
            reverse=True
        )
        return sorted_scores[:limit]
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `create_objective` | `async create_objective(name: str, criterion: str, display_name: str = "")` | 创建计分板目标 |
| `remove_objective` | `async remove_objective(name: str)` | 删除计分板目标 |
| `set_display` | `async set_display(slot: str, objective: str)` | 设置显示位置 |
| `set_score` | `async set_score(objective: str, target: str, score: int)` | 设置分数 |
| `add_score` | `async add_score(objective: str, target: str, score: int)` | 增加分数 |
| `remove_score` | `async remove_score(objective: str, target: str, score: int)` | 减少分数 |
| `get_score` | `async get_score(objective: str, target: str) -> int` | 获取分数 |
| `get_scores` | `async get_scores(objective: str) -> Dict[str, int]` | 获取所有分数 |

---

## 权限系统

### PermissionManager

权限管理器，用于检查和管理玩家权限。

```python
from nethergate.permissions import PermissionManager

class MyPlugin(Plugin):
    def __init__(self, permissions: PermissionManager):
        self.permissions = permissions
    
    async def check_admin(self, player: str) -> bool:
        """检查玩家是否是管理员"""
        return self.permissions.has_permission(
            player,
            "myplugin.admin"
        )
    
    async def grant_vip(self, player: str):
        """授予 VIP 权限"""
        await self.permissions.grant_permission(
            player,
            "myplugin.vip"
        )
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `has_permission` | `has_permission(player: str, permission: str) -> bool` | 检查权限 |
| `grant_permission` | `async grant_permission(player: str, permission: str)` | 授予权限 |
| `revoke_permission` | `async revoke_permission(player: str, permission: str)` | 撤销权限 |
| `list_permissions` | `list_permissions(player: str) -> List[str]` | 列出玩家权限 |

---

## WebSocket

### WebSocketServer

WebSocket 服务器，用于向客户端推送数据。

```python
from nethergate.websocket import WebSocketServer

class MyPlugin(Plugin):
    def __init__(self, ws: WebSocketServer):
        self.ws = ws
    
    async def broadcast_data(self, data: dict):
        """广播数据到所有客户端"""
        await self.ws.broadcast({
            "type": "custom_event",
            "plugin": "myplugin",
            "data": data,
            "timestamp": datetime.now().isoformat()
        })
    
    async def send_to_client(self, client_id: str, data: dict):
        """发送数据到指定客户端"""
        await self.ws.send_to_client(client_id, data)
```

#### 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `broadcast` | `async broadcast(data: dict)` | 广播到所有客户端 |
| `send_to_client` | `async send_to_client(client_id: str, data: dict)` | 发送到指定客户端 |
| `get_clients` | `get_clients() -> List[str]` | 获取所有客户端 ID |

---

## 工具类

### TextComponent

文本组件构建器，用于创建 Minecraft 格式化文本。

```python
from nethergate.utils import TextComponent

# 创建简单文本
text = TextComponent.text("Hello", color="green")

# 创建可点击文本
clickable = TextComponent.text("点击我", color="blue") \
    .click_run_command("/help") \
    .hover_show_text("显示帮助")

# 创建复杂文本
complex = TextComponent.text("欢迎 ", color="yellow") \
    .append(TextComponent.text("玩家", color="green", bold=True)) \
    .append(TextComponent.text("!", color="yellow"))

# 发送给玩家
await self.rcon.execute(f'tellraw @a {complex.to_json()}')
```

### UUID 工具

```python
from nethergate.utils import UuidUtils

# 玩家名转 UUID
uuid = UuidUtils.name_to_uuid("Notch")

# UUID 转玩家名
name = UuidUtils.uuid_to_name("069a79f4-44e9-4726-a5be-fca90e38aaf5")

# 格式化 UUID
formatted = UuidUtils.format_uuid("069a79f444e94726a5befca90e38aaf5")
# 结果: 069a79f4-44e9-4726-a5be-fca90e38aaf5
```

### 文件系统

```python
from nethergate.filesystem import FileSystem

class MyPlugin(Plugin):
    def __init__(self, fs: FileSystem):
        self.fs = fs
    
    async def save_data(self):
        """保存数据到文件"""
        # 插件数据目录
        data_dir = self.fs.data_directory
        
        # 写入文件
        await self.fs.write_text(
            f"{data_dir}/data.txt",
            "Hello, World!"
        )
        
        # 读取文件
        content = await self.fs.read_text(f"{data_dir}/data.txt")
        
        # 写入 JSON
        await self.fs.write_json(f"{data_dir}/config.json", {
            "key": "value"
        })
        
        # 读取 JSON
        data = await self.fs.read_json(f"{data_dir}/config.json")
```

---

## 完整示例

### 综合示例插件

```python
"""
综合示例插件 - 展示各种 API 的使用
"""
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.events import EventBus, PlayerJoinEvent, ServerStartedEvent
from nethergate.commands import CommandRegistry, CommandContext
from nethergate.rcon import RconClient
from nethergate.scheduling import Scheduler
from nethergate.config import ConfigManager
from nethergate.scoreboard import ScoreboardManager

class ComprehensivePlugin(Plugin):
    """综合示例插件"""
    
    def __init__(
        self,
        logger: Logger,
        event_bus: EventBus,
        commands: CommandRegistry,
        rcon: RconClient,
        scheduler: Scheduler,
        config: ConfigManager,
        scoreboard: ScoreboardManager
    ):
        # 依赖注入
        self.logger = logger
        self.event_bus = event_bus
        self.commands = commands
        self.rcon = rcon
        self.scheduler = scheduler
        self.config = config
        self.scoreboard = scoreboard
        
        # 插件信息
        self.info = PluginInfo(
            id="com.example.comprehensive",
            name="Comprehensive Plugin",
            version="1.0.0",
            description="综合示例插件",
            author="Your Name"
        )
        
        # 内部状态
        self.task_ids = []
        self.player_scores = {}
    
    # ========== 生命周期 ==========
    
    async def on_load(self):
        """加载配置"""
        self.logger.info("正在加载插件...")
        
        # 加载配置
        self.config.load("config.yaml", default={
            "welcome_message": "Welcome to the server!",
            "auto_save_interval": 300,
            "scoreboard_enabled": True
        })
    
    async def on_enable(self):
        """启用插件"""
        self.logger.info("正在启用插件...")
        
        # 注册事件
        self.event_bus.subscribe(ServerStartedEvent, self.on_server_started)
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
        
        # 注册命令
        self.commands.register(
            name="status",
            callback=self.cmd_status,
            description="查看服务器状态",
            usage="/status"
        )
        
        # 启动定时任务
        if self.config.get("scoreboard_enabled"):
            await self.setup_scoreboard()
        
        task_id = self.scheduler.run_repeating(
            callback=self.auto_save,
            interval_seconds=self.config.get("auto_save_interval", 300)
        )
        self.task_ids.append(task_id)
        
        self.logger.info("插件已启用")
    
    async def on_disable(self):
        """禁用插件"""
        self.logger.info("正在禁用插件...")
        
        # 注销事件
        self.event_bus.unsubscribe(ServerStartedEvent, self.on_server_started)
        self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
        
        # 注销命令
        self.commands.unregister("status")
        
        # 取消定时任务
        for task_id in self.task_ids:
            self.scheduler.cancel(task_id)
        
        # 保存数据
        await self.save_data()
        
        self.logger.info("插件已禁用")
    
    async def on_unload(self):
        """卸载插件"""
        self.logger.info("插件已卸载")
    
    # ========== 事件处理 ==========
    
    async def on_server_started(self, event: ServerStartedEvent):
        """服务器启动事件"""
        self.logger.info("服务器已启动!")
        await self.rcon.execute('say Server is ready!')
    
    async def on_player_join(self, event: PlayerJoinEvent):
        """玩家加入事件"""
        player = event.player_name
        self.logger.info(f"{player} 加入了游戏")
        
        # 发送欢迎消息
        welcome = self.config.get("welcome_message")
        await self.rcon.execute(
            f'tellraw {player} {{"text":"{welcome}","color":"green"}}'
        )
        
        # 更新分数
        if player not in self.player_scores:
            self.player_scores[player] = 0
            await self.scoreboard.set_score("logins", player, 0)
        
        await self.scoreboard.add_score("logins", player, 1)
    
    # ========== 命令处理 ==========
    
    async def cmd_status(self, ctx: CommandContext):
        """状态命令"""
        # 获取在线玩家
        result = await self.rcon.execute("list")
        
        await ctx.reply("=== 服务器状态 ===")
        await ctx.reply(f"在线玩家: {result.response}")
        await ctx.reply(f"插件版本: {self.info.version}")
    
    # ========== 定时任务 ==========
    
    async def auto_save(self):
        """自动保存任务"""
        self.logger.info("执行自动保存...")
        await self.save_data()
        await self.rcon.execute("save-all")
    
    # ========== 计分板 ==========
    
    async def setup_scoreboard(self):
        """设置计分板"""
        await self.scoreboard.create_objective(
            name="logins",
            criterion="dummy",
            display_name="登录次数"
        )
        
        await self.scoreboard.set_display(
            slot="sidebar",
            objective="logins"
        )
    
    # ========== 数据管理 ==========
    
    async def save_data(self):
        """保存数据"""
        self.config.set_all({"player_scores": self.player_scores})
        self.config.save("data.yaml")
        self.logger.debug("数据已保存")
```

---

## 类型提示

NetherGate Python API 提供完整的类型提示支持，推荐使用：

```python
from typing import List, Dict, Optional
from nethergate import Plugin
from nethergate.logging import Logger

class MyPlugin(Plugin):
    def __init__(self, logger: Logger) -> None:
        self.logger: Logger = logger
        self.data: Dict[str, int] = {}
    
    async def get_player_data(self, player: str) -> Optional[dict]:
        """类型提示的方法"""
        pass
```

---

## 异步编程

所有 API 调用都是异步的，需要使用 `async`/`await`：

```python
# ✅ 正确
async def my_function(self):
    result = await self.rcon.execute("list")
    data = await self.player_data.read("Notch")

# ❌ 错误
def my_function(self):
    result = self.rcon.execute("list")  # 会得到 coroutine 对象而非结果
```

---

## 相关文档

- [Python 插件开发指南](../03-插件开发/Python插件开发指南.md)
- [Python 示例插件集](../07-示例和最佳实践/Python示例插件集.md)
- [C# API 参考](API参考.md)

