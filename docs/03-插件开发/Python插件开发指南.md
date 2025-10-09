# Python 插件开发指南

> **注意**: Python 插件支持需要安装 `NetherGate.Python` 扩展模块

本指南介绍如何使用 Python 开发 NetherGate 插件。Python 插件通过 `NetherGate.Python` 桥接层与主系统交互，提供与 C# 插件相同的功能。

> **⚠️ 功能限制与性能说明**
>
> Python 插件基于桥接层实现，适合快速开发和原型验证，但请注意以下限制：
>
> - **API 覆盖范围**：Python SDK 主要封装核心功能，部分高级 API 可能未完全实现
> - **性能影响**：跨语言调用会产生额外开销，不适合高频率调用或性能敏感场景
> - **类型检查**：Python 的动态类型在运行时才能发现错误，而 C# 可在编译时检查
> - **调试体验**：跨语言调试相对复杂，错误堆栈可能跨越 Python 和 .NET
>
> **何时选择 Python 插件**：
> - ✅ 快速原型和概念验证
> - ✅ 简单的游戏逻辑和脚本
> - ✅ 学习 NetherGate API
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
- [打包发布](#打包发布)
- [调试技巧](#调试技巧)
- [最佳实践](#最佳实践)

---

## 环境要求

### 必需组件

- **Python**: 3.8 或更高版本
- **NetherGate**: 最新版本
- **NetherGate.Python**: Python 桥接扩展

### 可选工具

- **IDE**: PyCharm, VS Code, 或其他 Python IDE
- **虚拟环境**: venv 或 conda（推荐用于开发）

### 安装 NetherGate.Python

```bash
# 方式1: 使用 pip 安装（推荐）
pip install nethergate-python

# 方式2: 从源码安装
git clone https://github.com/your-org/NetherGate.Python.git
cd NetherGate.Python
pip install -e .
```

---

## 快速开始

### 1. 创建插件项目

```bash
# 创建插件目录
mkdir MyPythonPlugin
cd MyPythonPlugin

# 创建标准目录结构
mkdir src
mkdir resource

# 创建主文件
touch src/main.py
touch resource/plugin.json
```

### 2. 编写插件元数据

创建 `resource/plugin.json`:

```json
{
  "id": "com.example.myplugin",
  "name": "My Python Plugin",
  "version": "1.0.0",
  "description": "我的第一个 Python 插件",
  "author": "Your Name",
  "website": "https://github.com/yourname/myplugin",
  
  "type": "python",
  "main": "main.MyPlugin",
  "python_version": "3.8+",
  
  "dependencies": [],
  "soft_dependencies": [],
  "load_order": 100,
  
  "python_dependencies": [
    "requests>=2.28.0"
  ]
}
```

### 3. 编写插件代码

创建 `src/main.py`:

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.events import EventBus, ServerStartedEvent

class MyPlugin(Plugin):
    """我的第一个 NetherGate Python 插件"""
    
    def __init__(self, logger: Logger, event_bus: EventBus):
        """
        构造函数 - 支持依赖注入
        
        Args:
            logger: 日志记录器
            event_bus: 事件总线
        """
        self.logger = logger
        self.event_bus = event_bus
        self.info = PluginInfo(
            id="com.example.myplugin",
            name="My Python Plugin",
            version="1.0.0",
            description="我的第一个 Python 插件",
            author="Your Name"
        )
    
    async def on_load(self):
        """插件加载时调用"""
        self.logger.info(f"{self.info.name} 正在加载...")
    
    async def on_enable(self):
        """插件启用时调用"""
        self.logger.info(f"{self.info.name} 已启用!")
        
        # 注册事件监听器
        self.event_bus.subscribe(ServerStartedEvent, self.on_server_started)
    
    async def on_disable(self):
        """插件禁用时调用"""
        self.logger.info(f"{self.info.name} 已禁用")
        
        # 注销事件监听器
        self.event_bus.unsubscribe(ServerStartedEvent, self.on_server_started)
    
    async def on_unload(self):
        """插件卸载时调用"""
        self.logger.info(f"{self.info.name} 已卸载")
    
    async def on_server_started(self, event: ServerStartedEvent):
        """服务器启动事件处理"""
        self.logger.info("检测到服务器启动!")
```

### 4. 部署插件

将插件复制到 NetherGate 的 `plugins` 目录：

```
NetherGate/
  plugins/
    MyPythonPlugin/
      src/
        main.py
      resource/
        plugin.json
        config.yaml  (可选)
```

或打包为 `.ngpy` 文件（推荐）：

```bash
# 在插件根目录执行
python -m nethergate.pack
# 生成: MyPythonPlugin.ngpy
```

---

## 项目结构

### 标准目录结构

```
MyPythonPlugin/
├── src/                    # Python 源代码目录
│   ├── __init__.py
│   ├── main.py            # 插件主类（必需）
│   ├── commands.py        # 命令处理
│   ├── events.py          # 事件处理
│   └── utils.py           # 工具函数
│
├── resource/              # 资源文件目录
│   ├── plugin.json        # 插件元数据（必需）
│   ├── config.yaml        # 默认配置文件
│   └── lang/              # 国际化文件
│       ├── en_US.yaml
│       └── zh_CN.yaml
│
├── tests/                 # 测试文件（可选）
│   ├── test_main.py
│   └── test_commands.py
│
├── requirements.txt       # Python 依赖
├── README.md             # 插件说明
└── LICENSE               # 开源协议
```

### 关键文件说明

| 文件 | 必需 | 说明 |
|------|------|------|
| `src/main.py` | ✅ | 插件主类，必须包含实现 `Plugin` 基类的类 |
| `resource/plugin.json` | ✅ | 插件元数据，描述插件信息 |
| `requirements.txt` | ❌ | Python 依赖声明（建议提供） |
| `resource/config.yaml` | ❌ | 默认配置文件 |
| `README.md` | ❌ | 插件文档（建议提供） |

---

## 插件元数据

### plugin.json 完整示例

```json
{
  "id": "com.example.advanced_plugin",
  "name": "Advanced Python Plugin",
  "version": "2.1.0",
  "description": "一个功能丰富的 Python 插件示例",
  "author": "Your Name",
  "website": "https://github.com/yourname/advanced-plugin",
  
  "type": "python",
  "main": "main.AdvancedPlugin",
  "python_version": "3.8+",
  
  "dependencies": [
    "com.example.base_plugin"
  ],
  "soft_dependencies": [
    "com.example.optional_plugin"
  ],
  "load_order": 100,
  
  "python_dependencies": [
    "requests>=2.28.0",
    "pyyaml>=6.0",
    "aiohttp>=3.8.0"
  ],
  
  "permissions": [
    "nethergate.command.register",
    "nethergate.rcon.execute"
  ]
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
| `type` | string | ✅ | 插件类型，Python 插件必须为 `"python"` |
| `main` | string | ✅ | 插件主类，格式: `模块名.类名` |
| `python_version` | string | ❌ | Python 版本要求（如 `"3.8+"`, `">=3.9,<4.0"`） |
| `dependencies` | array | ❌ | 硬依赖插件列表 |
| `soft_dependencies` | array | ❌ | 软依赖插件列表 |
| `load_order` | number | ❌ | 加载顺序（默认 100） |
| `python_dependencies` | array | ❌ | Python 包依赖列表 |
| `permissions` | array | ❌ | 插件需要的权限列表 |

---

## 基础插件类

### Plugin 基类

所有 Python 插件必须继承 `nethergate.Plugin` 基类：

```python
from nethergate import Plugin, PluginInfo

class MyPlugin(Plugin):
    """插件主类"""
    
    def __init__(self):
        """构造函数"""
        self.info = PluginInfo(
            id="com.example.myplugin",
            name="My Plugin",
            version="1.0.0"
        )
    
    async def on_load(self):
        """插件加载"""
        pass
    
    async def on_enable(self):
        """插件启用"""
        pass
    
    async def on_disable(self):
        """插件禁用"""
        pass
    
    async def on_unload(self):
        """插件卸载"""
        pass
```

### 构造函数注入（推荐）

插件支持通过构造函数进行依赖注入：

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.events import EventBus
from nethergate.commands import CommandRegistry
from nethergate.rcon import RconClient
from nethergate.scheduling import Scheduler

class MyPlugin(Plugin):
    def __init__(
        self,
        logger: Logger,              # 日志记录器
        event_bus: EventBus,         # 事件总线
        commands: CommandRegistry,   # 命令注册器
        rcon: RconClient,           # RCON 客户端
        scheduler: Scheduler        # 调度器
    ):
        self.logger = logger
        self.event_bus = event_bus
        self.commands = commands
        self.rcon = rcon
        self.scheduler = scheduler
        
        self.info = PluginInfo(
            id="com.example.myplugin",
            name="My Plugin",
            version="1.0.0"
        )
```

### 可注入的服务

| 服务类型 | 说明 |
|---------|------|
| `Logger` | 日志记录器 |
| `EventBus` | 事件总线 |
| `CommandRegistry` | 命令注册器 |
| `RconClient` | RCON 客户端 |
| `Scheduler` | 任务调度器 |
| `ConfigManager` | 配置管理器 |
| `PermissionManager` | 权限管理器 |
| `ScoreboardManager` | 计分板管理器 |
| `PlayerDataReader` | 玩家数据读取器 |
| `WebSocketServer` | WebSocket 服务器 |

---

## 生命周期

### 生命周期方法

Python 插件的生命周期与 C# 插件保持一致：

```python
async def on_load(self):
    """
    插件加载阶段
    
    时机: 插件被加载到内存，但尚未启用
    用途: 
      - 初始化基本配置
      - 加载资源文件
      - 验证依赖项
    注意: 此时不应注册事件或命令
    """
    pass

async def on_enable(self):
    """
    插件启用阶段
    
    时机: 插件开始正式工作
    用途:
      - 注册事件监听器
      - 注册命令
      - 启动定时任务
      - 连接外部服务
    """
    pass

async def on_disable(self):
    """
    插件禁用阶段
    
    时机: 插件停止工作
    用途:
      - 注销事件监听器
      - 注销命令
      - 停止定时任务
      - 断开外部连接
    注意: 应该清理所有注册的资源
    """
    pass

async def on_unload(self):
    """
    插件卸载阶段
    
    时机: 插件从内存中移除
    用途:
      - 释放所有资源
      - 保存持久化数据
      - 关闭文件句柄
    """
    pass
```

### 生命周期流程图

```
[未加载] 
   ↓ (on_load)
[已加载]
   ↓ (on_enable)
[已启用] ←→ (热重载)
   ↓ (on_disable)
[已禁用]
   ↓ (on_unload)
[已卸载]
```

---

## API 使用

### 日志记录

```python
from nethergate.logging import Logger, LogLevel

class MyPlugin(Plugin):
    def __init__(self, logger: Logger):
        self.logger = logger
    
    async def on_enable(self):
        # 不同级别的日志
        self.logger.trace("详细的调试信息")
        self.logger.debug("调试信息")
        self.logger.info("一般信息")
        self.logger.warning("警告信息")
        self.logger.error("错误信息")
        
        # 带异常的日志
        try:
            risky_operation()
        except Exception as e:
            self.logger.error("操作失败", exception=e)
```

### 事件系统

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
        self.logger.info(f"玩家 {event.player_name} 加入了游戏")
        
        # 发送欢迎消息
        await self.rcon.execute(
            f'tellraw {event.player_name} {{"text":"欢迎回来!","color":"green"}}'
        )
```

### 命令注册

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
            description="打招呼命令",
            usage="/hello [name]",
            permission="myplugin.command.hello"
        )
    
    async def on_disable(self):
        # 注销命令
        self.commands.unregister("hello")
    
    async def cmd_hello(self, ctx: CommandContext):
        """命令处理函数"""
        name = ctx.args[0] if ctx.args else "World"
        await ctx.reply(f"Hello, {name}!")
```

### RCON 执行

```python
from nethergate.rcon import RconClient

class MyPlugin(Plugin):
    def __init__(self, rcon: RconClient):
        self.rcon = rcon
    
    async def teleport_player(self, player: str, x: int, y: int, z: int):
        """传送玩家"""
        result = await self.rcon.execute(f"tp {player} {x} {y} {z}")
        if result.success:
            self.logger.info(f"已传送 {player} 到 ({x}, {y}, {z})")
        else:
            self.logger.error(f"传送失败: {result.response}")
    
    async def give_item(self, player: str, item: str, count: int = 1):
        """给予物品"""
        await self.rcon.execute(f"give {player} {item} {count}")
```

### 定时任务

```python
from nethergate.scheduling import Scheduler

class MyPlugin(Plugin):
    def __init__(self, scheduler: Scheduler):
        self.scheduler = scheduler
        self.task_id = None
    
    async def on_enable(self):
        # 延迟执行（5秒后）
        self.scheduler.run_delayed(
            callback=self.delayed_task,
            delay_seconds=5.0
        )
        
        # 定时重复执行（每10秒）
        self.task_id = self.scheduler.run_repeating(
            callback=self.repeating_task,
            interval_seconds=10.0,
            initial_delay=0.0
        )
    
    async def on_disable(self):
        # 取消定时任务
        if self.task_id:
            self.scheduler.cancel(self.task_id)
    
    async def delayed_task(self):
        self.logger.info("延迟任务执行")
    
    async def repeating_task(self):
        self.logger.info("定时任务执行")
```

### 配置管理

```python
from nethergate.config import ConfigManager

class MyPlugin(Plugin):
    def __init__(self, config: ConfigManager):
        self.config = config
    
    async def on_load(self):
        # 加载配置（自动创建默认配置）
        self.config.load("config.yaml", default={
            "max_players": 20,
            "welcome_message": "Welcome!",
            "features": {
                "teleport": True,
                "kits": False
            }
        })
    
    async def on_enable(self):
        # 读取配置
        max_players = self.config.get("max_players", default=10)
        message = self.config.get("welcome_message")
        teleport_enabled = self.config.get("features.teleport")  # 支持点号路径
        
        # 修改配置
        self.config.set("max_players", 25)
        
        # 保存配置
        self.config.save("config.yaml")
```

### 玩家数据读取

```python
from nethergate.data import PlayerDataReader

class MyPlugin(Plugin):
    def __init__(self, player_data: PlayerDataReader):
        self.player_data = player_data
    
    async def get_player_stats(self, player_name: str):
        """获取玩家统计信息"""
        # 读取玩家 NBT 数据
        data = await self.player_data.read(player_name)
        
        if data:
            pos = data.get("Pos")
            health = data.get("Health")
            inventory = data.get("Inventory")
            
            self.logger.info(f"{player_name}: 位置={pos}, 生命值={health}")
            return {
                "position": pos,
                "health": health,
                "inventory_count": len(inventory)
            }
        return None
```

### 计分板操作

```python
from nethergate.scoreboard import ScoreboardManager

class MyPlugin(Plugin):
    def __init__(self, scoreboard: ScoreboardManager):
        self.scoreboard = scoreboard
    
    async def create_leaderboard(self):
        """创建排行榜"""
        # 创建计分板
        await self.scoreboard.create_objective(
            name="kills",
            criterion="playerKillCount",
            display_name="击杀排行"
        )
        
        # 设置显示位置
        await self.scoreboard.set_display(
            slot="sidebar",
            objective="kills"
        )
    
    async def add_score(self, player: str, points: int):
        """增加分数"""
        await self.scoreboard.add_score("kills", player, points)
    
    async def get_top_players(self, limit: int = 10):
        """获取排名前N的玩家"""
        scores = await self.scoreboard.get_scores("kills")
        sorted_scores = sorted(scores.items(), key=lambda x: x[1], reverse=True)
        return sorted_scores[:limit]
```

### WebSocket 推送

```python
from nethergate.websocket import WebSocketServer

class MyPlugin(Plugin):
    def __init__(self, ws: WebSocketServer):
        self.ws = ws
    
    async def broadcast_event(self, event_type: str, data: dict):
        """广播事件到所有 WebSocket 客户端"""
        await self.ws.broadcast({
            "type": event_type,
            "plugin": "myplugin",
            "data": data,
            "timestamp": datetime.now().isoformat()
        })
    
    async def on_player_join(self, event):
        """玩家加入时推送"""
        await self.broadcast_event("player_join", {
            "player": event.player_name,
            "uuid": event.player_uuid
        })
```

---

## 打包发布

### 准备发布

1. **检查依赖**

```bash
# 生成 requirements.txt
pip freeze > requirements.txt

# 或手动编辑（推荐，只包含必需依赖）
echo "requests>=2.28.0" > requirements.txt
```

2. **测试插件**

```bash
# 运行单元测试
python -m pytest tests/

# 代码风格检查
flake8 src/
black src/
```

3. **更新文档**

- 更新 `README.md`
- 更新 `CHANGELOG.md`
- 确保 `plugin.json` 版本正确

### 打包为 .ngpy

使用 NetherGate Python 打包工具：

```bash
# 方式1: 使用命令行工具
python -m nethergate.pack

# 方式2: 使用打包脚本
python pack.py
```

**pack.py 示例**:

```python
#!/usr/bin/env python3
import zipfile
import os
from pathlib import Path

def pack_plugin():
    """打包插件为 .ngpy 文件"""
    plugin_name = "MyPythonPlugin"
    output_file = f"{plugin_name}.ngpy"
    
    with zipfile.ZipFile(output_file, 'w', zipfile.ZIP_DEFLATED) as zf:
        # 添加源代码
        for py_file in Path("src").rglob("*.py"):
            zf.write(py_file, f"src/{py_file.relative_to('src')}")
        
        # 添加资源文件
        for res_file in Path("resource").rglob("*"):
            if res_file.is_file():
                zf.write(res_file, f"resource/{res_file.relative_to('resource')}")
        
        # 添加依赖声明
        if os.path.exists("requirements.txt"):
            zf.write("requirements.txt")
        
        # 添加文档
        for doc in ["README.md", "LICENSE", "CHANGELOG.md"]:
            if os.path.exists(doc):
                zf.write(doc)
    
    print(f"✅ 插件已打包: {output_file}")

if __name__ == "__main__":
    pack_plugin()
```

### 文件结构验证

打包后的 `.ngpy` 文件内容应该是：

```
MyPythonPlugin.ngpy (ZIP 格式)
├── src/
│   ├── __init__.py
│   ├── main.py
│   └── ...
├── resource/
│   ├── plugin.json
│   ├── config.yaml
│   └── ...
├── requirements.txt
├── README.md
└── LICENSE
```

### 发布到 GitHub Releases

```bash
# 1. 创建 Git 标签
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# 2. 上传 .ngpy 文件到 GitHub Releases
# 在 GitHub 网页操作，或使用 gh CLI:
gh release create v1.0.0 MyPythonPlugin.ngpy --title "v1.0.0" --notes "首次发布"
```

---

## 调试技巧

### 本地开发调试

```python
import sys
from pathlib import Path

# 添加开发路径（用于直接运行，无需安装）
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from nethergate import Plugin

class MyPlugin(Plugin):
    async def on_enable(self):
        # 开发调试代码
        import pdb; pdb.set_trace()  # 断点调试
        
        # 或使用 IPython
        # from IPython import embed; embed()
```

### 日志调试

```python
class MyPlugin(Plugin):
    def __init__(self, logger: Logger):
        self.logger = logger
        # 设置调试级别
        self.logger.set_level(LogLevel.TRACE)
    
    async def debug_function(self):
        self.logger.trace("详细执行流程")
        self.logger.debug(f"变量值: x={x}, y={y}")
```

### 性能分析

```python
import time
import functools

def timing_decorator(func):
    """性能计时装饰器"""
    @functools.wraps(func)
    async def wrapper(self, *args, **kwargs):
        start = time.perf_counter()
        result = await func(self, *args, **kwargs)
        elapsed = time.perf_counter() - start
        self.logger.debug(f"{func.__name__} 耗时: {elapsed:.4f}秒")
        return result
    return wrapper

class MyPlugin(Plugin):
    @timing_decorator
    async def expensive_operation(self):
        # 耗时操作
        pass
```

### 异常处理

```python
class MyPlugin(Plugin):
    async def safe_operation(self):
        """安全的操作包装"""
        try:
            await self.risky_operation()
        except ValueError as e:
            self.logger.warning(f"参数错误: {e}")
        except ConnectionError as e:
            self.logger.error(f"连接失败: {e}")
        except Exception as e:
            self.logger.error(f"未知错误: {e}", exception=e)
            # 可以选择重新抛出
            # raise
```

---

## 最佳实践

### 1. 代码组织

```python
# ✅ 推荐：模块化设计
src/
  ├── main.py          # 插件主类
  ├── commands.py      # 命令处理
  ├── events.py        # 事件处理
  ├── config.py        # 配置管理
  └── utils.py         # 工具函数

# ❌ 不推荐：所有代码写在一个文件
```

### 2. 异步编程

```python
# ✅ 推荐：使用 async/await
async def on_enable(self):
    result = await self.rcon.execute("list")
    await self.process_result(result)

# ❌ 不推荐：阻塞调用
def on_enable(self):
    result = self.rcon.execute_sync("list")  # 会阻塞主线程
```

### 3. 资源清理

```python
class MyPlugin(Plugin):
    async def on_enable(self):
        # 注册资源
        self.event_bus.subscribe(PlayerJoinEvent, self.handler)
        self.task_id = self.scheduler.run_repeating(self.task, 10.0)
    
    async def on_disable(self):
        # ✅ 务必清理所有资源
        self.event_bus.unsubscribe(PlayerJoinEvent, self.handler)
        self.scheduler.cancel(self.task_id)
```

### 4. 配置验证

```python
async def on_load(self):
    config = self.config.load("config.yaml", default=DEFAULT_CONFIG)
    
    # ✅ 验证配置
    if not 1 <= config.get("max_players") <= 100:
        raise ValueError("max_players 必须在 1-100 之间")
    
    if not config.get("server_address"):
        raise ValueError("缺少必需的 server_address 配置")
```

### 5. 错误处理

```python
# ✅ 推荐：具体的异常处理
async def process_player(self, player_name: str):
    try:
        data = await self.player_data.read(player_name)
    except FileNotFoundError:
        self.logger.warning(f"玩家 {player_name} 数据不存在")
        return None
    except PermissionError:
        self.logger.error(f"无权限读取 {player_name} 数据")
        return None

# ❌ 不推荐：捕获所有异常
async def process_player(self, player_name: str):
    try:
        data = await self.player_data.read(player_name)
    except:  # 不推荐
        pass
```

### 6. 性能优化

```python
# ✅ 推荐：批量操作
async def teleport_players(self, players: list, x: int, y: int, z: int):
    commands = [f"tp {p} {x} {y} {z}" for p in players]
    await self.rcon.execute_batch(commands)

# ❌ 不推荐：逐个操作
async def teleport_players(self, players: list, x: int, y: int, z: int):
    for p in players:
        await self.rcon.execute(f"tp {p} {x} {y} {z}")  # 多次网络请求
```

### 7. 版本兼容

```python
from nethergate import __version__

class MyPlugin(Plugin):
    async def on_load(self):
        # 检查 NetherGate 版本
        required_version = "1.5.0"
        if __version__ < required_version:
            raise RuntimeError(
                f"需要 NetherGate {required_version}+，当前版本: {__version__}"
            )
```

### 8. 国际化支持

```python
from nethergate.i18n import Translator

class MyPlugin(Plugin):
    def __init__(self, translator: Translator):
        self.t = translator
        self.t.load_translations("resource/lang")
    
    async def send_message(self, player: str, key: str):
        message = self.t.translate(key, locale="zh_CN")
        await self.rcon.execute(f'tellraw {player} {{"text":"{message}"}}')
```

---

## 完整示例

### 传送插件示例

```python
"""
传送插件 - 提供玩家传送功能
"""
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.commands import CommandRegistry, CommandContext
from nethergate.config import ConfigManager
from nethergate.rcon import RconClient
from typing import Dict, Tuple

class TeleportPlugin(Plugin):
    """传送插件主类"""
    
    def __init__(
        self,
        logger: Logger,
        commands: CommandRegistry,
        config: ConfigManager,
        rcon: RconClient
    ):
        self.logger = logger
        self.commands = commands
        self.config = config
        self.rcon = rcon
        
        self.info = PluginInfo(
            id="com.example.teleport",
            name="Teleport Plugin",
            version="1.0.0",
            description="玩家传送插件",
            author="Your Name"
        )
        
        # 传送点存储
        self.warps: Dict[str, Tuple[int, int, int]] = {}
    
    async def on_load(self):
        """加载配置"""
        self.logger.info("正在加载传送插件...")
        
        # 加载配置
        config = self.config.load("config.yaml", default={
            "max_warps": 10,
            "teleport_delay": 3,
            "require_permission": True
        })
        
        # 加载传送点
        warps_data = self.config.load("warps.yaml", default={})
        self.warps = {
            name: tuple(pos) 
            for name, pos in warps_data.items()
        }
        
        self.logger.info(f"已加载 {len(self.warps)} 个传送点")
    
    async def on_enable(self):
        """注册命令"""
        self.logger.info("正在启用传送插件...")
        
        # 注册命令
        self.commands.register(
            name="setwarp",
            callback=self.cmd_setwarp,
            description="设置传送点",
            usage="/setwarp <name> <x> <y> <z>",
            permission="teleport.setwarp"
        )
        
        self.commands.register(
            name="warp",
            callback=self.cmd_warp,
            description="传送到传送点",
            usage="/warp <name>",
            permission="teleport.warp"
        )
        
        self.commands.register(
            name="delwarp",
            callback=self.cmd_delwarp,
            description="删除传送点",
            usage="/delwarp <name>",
            permission="teleport.delwarp"
        )
        
        self.commands.register(
            name="listwarps",
            callback=self.cmd_listwarps,
            description="列出所有传送点",
            usage="/listwarps"
        )
        
        self.logger.info("传送插件已启用")
    
    async def on_disable(self):
        """注销命令并保存数据"""
        self.logger.info("正在禁用传送插件...")
        
        # 保存传送点
        warps_data = {name: list(pos) for name, pos in self.warps.items()}
        self.config.set_all(warps_data)
        self.config.save("warps.yaml")
        
        # 注销命令
        self.commands.unregister("setwarp")
        self.commands.unregister("warp")
        self.commands.unregister("delwarp")
        self.commands.unregister("listwarps")
        
        self.logger.info("传送插件已禁用")
    
    async def on_unload(self):
        """清理资源"""
        self.logger.info("传送插件已卸载")
    
    # 命令处理函数
    
    async def cmd_setwarp(self, ctx: CommandContext):
        """设置传送点"""
        if len(ctx.args) < 4:
            await ctx.reply("用法: /setwarp <name> <x> <y> <z>")
            return
        
        name = ctx.args[0]
        try:
            x, y, z = int(ctx.args[1]), int(ctx.args[2]), int(ctx.args[3])
        except ValueError:
            await ctx.reply("坐标必须是整数")
            return
        
        # 检查数量限制
        max_warps = self.config.get("max_warps", 10)
        if len(self.warps) >= max_warps and name not in self.warps:
            await ctx.reply(f"传送点数量已达上限 ({max_warps})")
            return
        
        self.warps[name] = (x, y, z)
        await ctx.reply(f"✅ 传送点 '{name}' 已设置为 ({x}, {y}, {z})")
        self.logger.info(f"创建传送点: {name} -> ({x}, {y}, {z})")
    
    async def cmd_warp(self, ctx: CommandContext):
        """传送到传送点"""
        if len(ctx.args) < 1:
            await ctx.reply("用法: /warp <name>")
            return
        
        name = ctx.args[0]
        if name not in self.warps:
            await ctx.reply(f"❌ 传送点 '{name}' 不存在")
            await ctx.reply(f"使用 /listwarps 查看所有传送点")
            return
        
        x, y, z = self.warps[name]
        player = ctx.sender  # 假设有 sender 属性
        
        # 执行传送
        result = await self.rcon.execute(f"tp {player} {x} {y} {z}")
        if result.success:
            await ctx.reply(f"✅ 已传送到 '{name}'")
            self.logger.info(f"{player} 传送到 {name}")
        else:
            await ctx.reply(f"❌ 传送失败")
            self.logger.error(f"传送失败: {result.response}")
    
    async def cmd_delwarp(self, ctx: CommandContext):
        """删除传送点"""
        if len(ctx.args) < 1:
            await ctx.reply("用法: /delwarp <name>")
            return
        
        name = ctx.args[0]
        if name not in self.warps:
            await ctx.reply(f"❌ 传送点 '{name}' 不存在")
            return
        
        del self.warps[name]
        await ctx.reply(f"✅ 传送点 '{name}' 已删除")
        self.logger.info(f"删除传送点: {name}")
    
    async def cmd_listwarps(self, ctx: CommandContext):
        """列出所有传送点"""
        if not self.warps:
            await ctx.reply("当前没有传送点")
            return
        
        await ctx.reply(f"📍 传送点列表 ({len(self.warps)}):")
        for name, (x, y, z) in self.warps.items():
            await ctx.reply(f"  - {name}: ({x}, {y}, {z})")
```

---

## 下一步

- 阅读 [Python API 参考](../08-参考/Python_API参考.md)
- 查看 [Python 示例插件集](../07-示例和最佳实践/Python示例插件集.md)
- 了解 [插件发布流程](发布流程.md)
- 加入社区交流

---

## 相关文档

- [C# 插件开发指南](插件开发指南.md)
- [配置文件](配置文件.md)
- [调试技巧](调试技巧.md)
- [发布流程](发布流程.md)

---

**注意**: Python 插件功能目前处于测试阶段，API 可能会有变化。建议关注项目更新。

