# Python 核心 API 使用指南

本文档介绍 NetherGate Python SDK 中最重要的三个核心功能：SMP、RCON 和日志匹配器的详细使用方法。

> **⚠️ 重要提示**
> 
> Python 插件 API 通过桥接层实现，主要提供基础功能和常用操作的封装。虽然 Python 插件开发更加简单快捷，但存在以下限制：
> 
> - **功能覆盖**：Python API 仅封装了最常用的核心功能，某些高级 API 可能未完全实现
> - **性能开销**：Python 与 .NET 之间的互操作会带来额外的性能开销
> - **类型安全**：Python 的动态类型特性可能导致运行时错误，而 C# 可以在编译期发现问题
> - **生态集成**：某些深度依赖 .NET 生态的功能可能无法在 Python 中使用
> 
> **推荐场景**：
> - ✅ Python 插件：快速原型、简单的游戏逻辑、脚本化任务、学习和实验
> - ✅ .NET 插件：生产环境、高性能需求、复杂业务逻辑、深度集成、企业级应用
> 
> 如果您需要更强的控制能力、更好的性能和完整的 API 访问，强烈推荐使用 C# 开发 .NET 插件。详见 [插件开发指南](./插件开发指南.md)。

## 目录

- [RCON 客户端](#rcon-客户端)
- [SMP (服务器管理协议)](#smp-服务器管理协议)
- [日志匹配器](#日志匹配器)
- [玩家数据读取](#玩家数据读取)

---

## RCON 客户端

RCON 客户端提供了与 Minecraft 服务器通信的能力，可以执行游戏命令。

### 基础用法

```python
from nethergate import Plugin, RconClient, Logger

class MyPlugin(Plugin):
    def __init__(self, rcon: RconClient, logger: Logger):
        super().__init__()
        self.rcon = rcon
        self.logger = logger
    
    async def on_enable(self):
        # 执行单条命令
        response = await self.rcon.execute("say Hello from Python!")
        if response.success:
            self.logger.info(f"命令执行成功: {response.response}")
        else:
            self.logger.error(f"命令执行失败: {response.error}")
```

### 批量执行命令

```python
async def broadcast_messages(self):
    messages = [
        "say Welcome to our server!",
        "say Type /help for commands",
        "say Have fun!"
    ]
    
    responses = await self.rcon.execute_batch(messages)
    for i, resp in enumerate(responses):
        if resp.success:
            self.logger.info(f"消息 {i+1} 发送成功")
        else:
            self.logger.error(f"消息 {i+1} 发送失败: {resp.error}")
```

### 便捷方法

RCON 客户端提供了许多便捷方法来简化常用操作：

```python
async def player_management_example(self):
    # 发送聊天消息
    await self.rcon.say("服务器将在 5 分钟后重启")
    
    # 向特定玩家发送消息
    await self.rcon.tell("Player123", "欢迎回来!")
    
    # 踢出玩家
    await self.rcon.kick("BadPlayer", "违反服务器规则")
    
    # 封禁玩家
    await self.rcon.ban("Cheater", "使用作弊客户端")
    
    # 解封玩家
    await self.rcon.pardon("ReformedPlayer")
    
    # 给予管理员权限
    await self.rcon.op("TrustedPlayer")
    
    # 移除管理员权限
    await self.rcon.deop("FormerAdmin")
```

### 白名单管理

```python
async def whitelist_example(self):
    # 添加到白名单
    await self.rcon.whitelist_add("NewPlayer")
    
    # 从白名单移除
    await self.rcon.whitelist_remove("LeftPlayer")
```

### 游戏控制

```python
async def game_control_example(self):
    # 给予物品
    await self.rcon.give("Player123", "minecraft:diamond", 64)
    
    # 传送玩家
    await self.rcon.teleport("Player123", 0, 100, 0)
    
    # 设置游戏模式
    await self.rcon.gamemode("Player123", "creative")
    
    # 设置时间
    await self.rcon.time_set("day")
    
    # 设置天气
    await self.rcon.weather("clear")
    
    # 设置难度
    await self.rcon.difficulty("normal")
    
    # 设置游戏规则
    await self.rcon.gamerule("keepInventory", "true")
    
    # 保存世界
    await self.rcon.save_all()
```

### 检查连接状态

```python
def check_rcon_status(self):
    if self.rcon.is_connected():
        self.logger.info("RCON 已连接")
    else:
        self.logger.warning("RCON 未连接")
```

---

## SMP (服务器管理协议)

SMP 提供了更高级的服务器管理功能，包括白名单、封禁、管理员、游戏规则等的完整管理。

### 基础用法

```python
from nethergate import Plugin, SmpApi, PlayerDto, Logger

class ServerManagerPlugin(Plugin):
    def __init__(self, smp: SmpApi, logger: Logger):
        super().__init__()
        self.smp = smp
        self.logger = logger
    
    async def on_enable(self):
        # 获取在线玩家
        players = await self.smp.get_players()
        self.logger.info(f"当前在线 {len(players)} 名玩家")
```

### 白名单管理

```python
async def manage_allowlist(self):
    # 获取白名单
    allowlist = await self.smp.get_allowlist()
    self.logger.info(f"白名单中有 {len(allowlist)} 名玩家")
    
    # 添加玩家到白名单
    player = PlayerDto(
        uuid="123e4567-e89b-12d3-a456-426614174000",
        name="NewPlayer"
    )
    await self.smp.add_to_allowlist(player)
    
    # 从白名单移除玩家
    await self.smp.remove_from_allowlist(player)
    
    # 清空白名单
    await self.smp.clear_allowlist()
```

### 玩家封禁管理

```python
from nethergate import UserBanDto
from datetime import datetime

async def manage_bans(self):
    # 获取封禁列表
    bans = await self.smp.get_bans()
    self.logger.info(f"当前有 {len(bans)} 个封禁")
    
    # 封禁玩家
    ban = UserBanDto(
        uuid="123e4567-e89b-12d3-a456-426614174001",
        name="Cheater",
        created="2025-10-09T12:00:00Z",
        source="Admin",
        reason="使用作弊客户端",
        expires="2025-11-09T12:00:00Z"  # 30天后过期
    )
    await self.smp.add_ban(ban)
    
    # 解除封禁
    player = PlayerDto(uuid=ban.uuid, name=ban.name)
    await self.smp.remove_ban(player)
    
    # 清空封禁列表
    await self.smp.clear_bans()
```

### IP 封禁管理

```python
from nethergate import IpBanDto

async def manage_ip_bans(self):
    # 获取 IP 封禁列表
    ip_bans = await self.smp.get_ip_bans()
    
    # 封禁 IP
    ip_ban = IpBanDto(
        ip="192.168.1.100",
        created="2025-10-09T12:00:00Z",
        source="Admin",
        reason="恶意攻击"
    )
    await self.smp.add_ip_ban(ip_ban)
    
    # 解除 IP 封禁
    await self.smp.remove_ip_ban("192.168.1.100")
```

### 管理员管理

```python
from nethergate import OperatorDto

async def manage_operators(self):
    # 获取管理员列表
    ops = await self.smp.get_operators()
    
    # 添加管理员
    op = OperatorDto(
        uuid="123e4567-e89b-12d3-a456-426614174002",
        name="TrustedPlayer",
        level=4,  # 管理员等级 (1-4)
        bypass_player_limit=True
    )
    await self.smp.add_operator(op)
    
    # 移除管理员
    player = PlayerDto(uuid=op.uuid, name=op.name)
    await self.smp.remove_operator(player)
```

### 玩家管理

```python
async def manage_players(self):
    # 获取在线玩家列表
    players = await self.smp.get_players()
    for player in players:
        self.logger.info(f"玩家: {player.name} (UUID: {player.uuid})")
    
    # 踢出玩家
    await self.smp.kick_player("Player123", "你需要休息一下")
```

### 服务器管理

```python
from nethergate import ServerState

async def manage_server(self):
    # 获取服务器状态
    status = await self.smp.get_server_status()
    self.logger.info(f"服务器状态: {status.value}")
    
    # 保存世界
    await self.smp.save_world()
    self.logger.info("世界已保存")
    
    # 发送系统消息
    await self.smp.send_system_message("服务器将在 5 分钟后重启")
    
    # 停止服务器
    # await self.smp.stop_server()  # 谨慎使用!
```

### 游戏规则管理

```python
async def manage_game_rules(self):
    # 获取所有游戏规则
    rules = await self.smp.get_game_rules()
    for rule_name, typed_rule in rules.items():
        self.logger.info(f"{rule_name} = {typed_rule.value} ({typed_rule.type})")
    
    # 更新游戏规则
    await self.smp.update_game_rule("keepInventory", True)
    await self.smp.update_game_rule("doDaylightCycle", False)
    await self.smp.update_game_rule("maxEntityCramming", 8)
```

### 服务器设置

```python
async def manage_server_settings(self):
    # 获取所有设置
    settings = await self.smp.get_server_settings()
    
    # 获取特定设置
    difficulty = await self.smp.get_server_setting("difficulty")
    self.logger.info(f"当前难度: {difficulty}")
    
    # 设置服务器设置
    await self.smp.set_server_setting("pvp", True)
    await self.smp.set_server_setting("max-players", 50)
```

---

## 日志匹配器

日志匹配器允许你将服务器日志转换为强类型事件，这对于监控和响应服务器活动非常有用。

### 创建自定义日志匹配器

```python
import re
from nethergate import RegexLogMatcher, Event, Logger

# 定义自定义事件
class PlayerCraftedItemEvent(Event):
    def __init__(self, player_name: str, item_id: str, count: int):
        super().__init__()
        self.player_name = player_name
        self.item_id = item_id
        self.count = count

# 创建匹配器
class PlayerCraftedMatcher(RegexLogMatcher):
    """匹配玩家合成物品的日志"""
    
    def __init__(self):
        # 匹配格式: "Player123 crafted 64 minecraft:oak_planks"
        super().__init__(
            pattern=r"^(\w+) crafted (\d+) (.+)$",
            priority=100
        )
    
    def on_match(self, match: re.Match, level: str, thread: str | None) -> Event | None:
        player_name = match.group(1)
        count = int(match.group(2))
        item_id = match.group(3)
        
        return PlayerCraftedItemEvent(player_name, item_id, count)

# 在插件中使用
class CraftTrackingPlugin(Plugin):
    def __init__(self, logger: Logger):
        super().__init__()
        self.logger = logger
        self.matcher = PlayerCraftedMatcher()
    
    def on_load(self):
        # 注册匹配器到事件系统
        # （具体注册方式取决于实现）
        pass
    
    def handle_server_log(self, message: str, level: str, thread: str | None):
        """处理服务器日志"""
        event = self.matcher.try_match(message, level, thread)
        if event and isinstance(event, PlayerCraftedItemEvent):
            self.logger.info(
                f"{event.player_name} 合成了 {event.count} 个 {event.item_id}"
            )
```

### 内置匹配器示例

SDK 提供了一些内置的日志匹配器：

```python
from nethergate import (
    PlayerJoinMatcher,
    PlayerLeaveMatcher,
    PlayerChatMatcher,
    ServerDoneMatcher
)

class LogMonitorPlugin(Plugin):
    def __init__(self, logger: Logger):
        super().__init__()
        self.logger = logger
        
        # 创建匹配器
        self.matchers = [
            PlayerJoinMatcher(),
            PlayerLeaveMatcher(),
            PlayerChatMatcher(),
            ServerDoneMatcher()
        ]
    
    def handle_log_line(self, message: str, level: str, thread: str | None):
        """处理日志行"""
        for matcher in self.matchers:
            event = matcher.try_match(message, level, thread)
            if event:
                self.on_log_event(event)
                break
    
    def on_log_event(self, event: Event):
        """处理日志事件"""
        event_type = type(event).__name__
        self.logger.debug(f"检测到事件: {event_type}")
```

### 高级匹配器示例

```python
from nethergate import RegexLogMatcher, Event

class PlayerDeathMatcher(RegexLogMatcher):
    """匹配玩家死亡消息"""
    
    def __init__(self):
        # 匹配各种死亡消息格式
        super().__init__(
            pattern=r"^(\w+) (.+)$",  # 简化的示例
            priority=90
        )
    
    def on_match(self, match: re.Match, level: str, thread: str | None) -> Event | None:
        player_name = match.group(1)
        death_message = match.group(2)
        
        # 检查是否是死亡消息
        death_keywords = [
            "was slain by",
            "was shot by",
            "drowned",
            "burned to death",
            "fell from a high place",
            "blew up",
            "died"
        ]
        
        if any(keyword in death_message for keyword in death_keywords):
            from nethergate import PlayerDeathEvent
            return PlayerDeathEvent(player_name, f"{player_name} {death_message}")
        
        return None

class AdvancementMatcher(RegexLogMatcher):
    """匹配玩家成就解锁"""
    
    def __init__(self):
        # 匹配格式: "Player123 has made the advancement [Stone Age]"
        super().__init__(
            pattern=r"^(\w+) has made the advancement \[(.+)\]$",
            priority=100
        )
    
    def on_match(self, match: re.Match, level: str, thread: str | None) -> Event | None:
        player_name = match.group(1)
        advancement_name = match.group(2)
        
        from nethergate import PlayerAdvancementEvent
        return PlayerAdvancementEvent(
            player_name=player_name,
            advancement_id=advancement_name,
            advancement_title=advancement_name
        )
```

### 匹配器优先级

优先级越高的匹配器越先执行。这对于处理重叠的模式很有用：

```python
# 高优先级匹配器 - 处理特定格式
class SpecificMatcher(RegexLogMatcher):
    def __init__(self):
        super().__init__(
            pattern=r"^SPECIAL: (.+)$",
            priority=200  # 高优先级
        )
    
    def on_match(self, match: re.Match, level: str, thread: str | None):
        # 处理特殊格式
        pass

# 低优先级匹配器 - 处理通用格式
class GenericMatcher(RegexLogMatcher):
    def __init__(self):
        super().__init__(
            pattern=r"^(.+)$",
            priority=10  # 低优先级
        )
    
    def on_match(self, match: re.Match, level: str, thread: str | None):
        # 处理通用格式
        pass
```

---

## 玩家数据读取

玩家数据读取器提供了访问玩家 NBT 数据、统计数据和成就进度的能力。

### 基础用法

```python
from nethergate import Plugin, PlayerDataReader, Logger

class PlayerStatsPlugin(Plugin):
    def __init__(self, player_data: PlayerDataReader, logger: Logger):
        super().__init__()
        self.player_data = player_data
        self.logger = logger
    
    async def get_player_info(self, player_uuid: str):
        # 读取玩家数据
        data = await self.player_data.read_player_data(player_uuid)
        if data:
            self.logger.info(f"玩家: {data.name}")
            self.logger.info(f"生命值: {data.health}")
            self.logger.info(f"等级: {data.xp_level}")
            self.logger.info(f"位置: ({data.position.x}, {data.position.y}, {data.position.z})")
```

### 统计数据

```python
async def show_player_stats(self, player_uuid: str):
    stats = await self.player_data.read_player_stats(player_uuid)
    if stats:
        self.logger.info(f"游玩时间: {stats.play_time_minutes} 分钟")
        self.logger.info(f"死亡次数: {stats.deaths}")
        self.logger.info(f"跳跃次数: {stats.jumps}")
        self.logger.info(f"行走距离: {stats.distance_walked:.2f} 米")
        
        # 击杀统计
        self.logger.info("生物击杀:")
        for mob, count in stats.mob_kills.items():
            self.logger.info(f"  {mob}: {count}")
```

### 成就进度

```python
async def show_advancements(self, player_uuid: str):
    advancements = await self.player_data.read_player_advancements(player_uuid)
    if advancements:
        self.logger.info(f"完成度: {advancements.completion_percent:.2f}%")
        self.logger.info(f"已完成: {len(advancements.completed)} 个成就")
        
        # 显示最近完成的成就
        for adv in advancements.completed[-5:]:
            self.logger.info(f"  {adv.id} - {adv.completed_at}")
```

### 列出所有玩家

```python
def list_all_players(self):
    player_uuids = self.player_data.list_players()
    self.logger.info(f"服务器共有 {len(player_uuids)} 名玩家")
    
    for uuid in player_uuids:
        if self.player_data.player_data_exists(uuid):
            self.logger.info(f"  - {uuid}")
```

### 在线玩家

```python
async def show_online_players(self):
    online_players = await self.player_data.get_online_players()
    self.logger.info(f"当前在线: {len(online_players)} 名玩家")
    
    for player in online_players:
        self.logger.info(
            f"{player.name}: "
            f"生命 {player.health}/20, "
            f"等级 {player.xp_level}, "
            f"模式 {player.game_mode.name}"
        )
```

---

## 完整示例

下面是一个综合使用这些 API 的完整示例：

```python
from nethergate import (
    Plugin, PluginInfo,
    Logger, RconClient, SmpApi, PlayerDataReader,
    EventBus, PlayerJoinEvent,
    RegexLogMatcher, Event
)
import re

class ComprehensivePlugin(Plugin):
    """综合示例插件"""
    
    def __init__(
        self,
        logger: Logger,
        rcon: RconClient,
        smp: SmpApi,
        player_data: PlayerDataReader,
        event_bus: EventBus
    ):
        super().__init__()
        self.logger = logger
        self.rcon = rcon
        self.smp = smp
        self.player_data = player_data
        self.event_bus = event_bus
    
    async def on_load(self):
        self.logger.info("插件加载中...")
    
    async def on_enable(self):
        self.logger.info("插件已启用")
        
        # 订阅玩家加入事件
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
        
        # 显示服务器状态
        await self.show_server_status()
    
    async def on_disable(self):
        self.logger.info("插件已禁用")
    
    async def on_player_join(self, event: PlayerJoinEvent):
        """处理玩家加入"""
        player_name = event.player_name
        
        # 欢迎消息
        await self.rcon.tell(player_name, f"欢迎回来, {player_name}!")
        
        # 获取玩家数据
        # 注意：需要将玩家名称转换为 UUID
        # 这里简化处理
        
        self.logger.info(f"{player_name} 加入了游戏")
    
    async def show_server_status(self):
        """显示服务器状态"""
        # 获取在线玩家
        players = await self.smp.get_players()
        self.logger.info(f"在线玩家: {len(players)}")
        
        # 获取服务器状态
        status = await self.smp.get_server_status()
        self.logger.info(f"服务器状态: {status.value}")
        
        # 获取游戏规则
        rules = await self.smp.get_game_rules()
        self.logger.info(f"游戏规则数量: {len(rules)}")

# 插件元数据
def get_plugin_info() -> PluginInfo:
    return PluginInfo(
        id="comprehensive-example",
        name="综合示例插件",
        version="1.0.0",
        description="展示核心 API 的综合使用",
        author="Your Name"
    )
```

---

## 最佳实践

### 1. 错误处理

始终检查 RCON 响应的成功状态：

```python
response = await self.rcon.execute("gamemode creative Player123")
if not response.success:
    self.logger.error(f"命令执行失败: {response.error}")
    return
```

### 2. 异步操作

所有涉及网络或 I/O 的操作都应该使用 `async/await`：

```python
async def my_async_function(self):
    # 正确 ✓
    result = await self.rcon.execute("list")
    
    # 错误 ✗ - 不要忘记 await
    # result = self.rcon.execute("list")
```

### 3. 资源清理

确保在插件卸载时清理资源：

```python
async def on_disable(self):
    # 取消订阅事件
    self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
    
    # 清理其他资源
    self.logger.info("资源已清理")
```

### 4. 日志级别

使用适当的日志级别：

```python
self.logger.debug("调试信息 - 详细的执行流程")
self.logger.info("普通信息 - 重要的状态更新")
self.logger.warning("警告 - 可能的问题")
self.logger.error("错误 - 需要注意的问题")
```

### 5. 批量操作

对于多个命令，使用批量执行以提高性能：

```python
# 好的做法 ✓
commands = [f"give {player} diamond 1" for player in player_list]
await self.rcon.execute_batch(commands)

# 不推荐 ✗
for player in player_list:
    await self.rcon.execute(f"give {player} diamond 1")
```

---

## 下一步

- 查看 [Python API 参考](../../08-参考/Python_API参考.md) 了解完整的 API 文档
- 查看 [Python 插件开发指南](./Python插件开发指南.md) 了解插件结构
- 查看 [Python 示例插件集](../../07-示例和最佳实践/Python示例插件集.md) 查看更多实际示例

