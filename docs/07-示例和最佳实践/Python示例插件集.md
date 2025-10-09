# Python 示例插件集

本文档提供了一系列完整的 Python 插件示例，涵盖了 NetherGate 的主要功能和常见使用场景。

---

## 📋 目录

- [1. Hello World - 最简单的插件](#1-hello-world---最简单的插件)
- [2. 玩家欢迎插件](#2-玩家欢迎插件)
- [3. 自动备份插件](#3-自动备份插件)
- [4. 传送系统插件](#4-传送系统插件)
- [5. 经济系统插件](#5-经济系统插件)
- [6. 排行榜插件](#6-排行榜插件)
- [7. 自定义工具箱插件](#7-自定义工具箱插件)
- [8. WebSocket 数据推送插件](#8-websocket-数据推送插件)
- [9. 自动公告插件](#9-自动公告插件)
- [10. 玩家统计插件](#10-玩家统计插件)

---

## 1. Hello World - 最简单的插件

最基础的插件示例，演示插件的基本结构。

### 文件结构

```
HelloWorldPlugin/
├── src/
│   └── main.py
└── resource/
    └── plugin.json
```

### plugin.json

```json
{
  "id": "com.example.helloworld",
  "name": "Hello World Plugin",
  "version": "1.0.0",
  "description": "最简单的 Python 插件示例",
  "author": "Your Name",
  "type": "python",
  "main": "main.HelloWorldPlugin"
}
```

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger

class HelloWorldPlugin(Plugin):
    """Hello World 插件"""
    
    def __init__(self, logger: Logger):
        self.logger = logger
        self.info = PluginInfo(
            id="com.example.helloworld",
            name="Hello World Plugin",
            version="1.0.0"
        )
    
    async def on_load(self):
        self.logger.info("Hello World 插件正在加载...")
    
    async def on_enable(self):
        self.logger.info("Hello World! 插件已启用")
    
    async def on_disable(self):
        self.logger.info("Goodbye! 插件已禁用")
    
    async def on_unload(self):
        self.logger.info("插件已卸载")
```

---

## 2. 玩家欢迎插件

监听玩家加入/离开事件，发送自定义欢迎和告别消息。

### plugin.json

```json
{
  "id": "com.example.welcome",
  "name": "Welcome Plugin",
  "version": "1.0.0",
  "description": "玩家欢迎插件",
  "author": "Your Name",
  "type": "python",
  "main": "main.WelcomePlugin"
}
```

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.events import EventBus, PlayerJoinEvent, PlayerLeaveEvent
from nethergate.rcon import RconClient
from nethergate.config import ConfigManager

class WelcomePlugin(Plugin):
    """玩家欢迎插件"""
    
    def __init__(
        self,
        logger: Logger,
        event_bus: EventBus,
        rcon: RconClient,
        config: ConfigManager
    ):
        self.logger = logger
        self.event_bus = event_bus
        self.rcon = rcon
        self.config = config
        
        self.info = PluginInfo(
            id="com.example.welcome",
            name="Welcome Plugin",
            version="1.0.0"
        )
    
    async def on_load(self):
        """加载配置"""
        self.config.load("config.yaml", default={
            "messages": {
                "join": "欢迎 {player} 加入服务器!",
                "leave": "{player} 离开了服务器",
                "first_join": "首次加入的玩家 {player}! 欢迎!"
            },
            "broadcast": True,
            "title": {
                "enabled": True,
                "title": "欢迎!",
                "subtitle": "祝你游戏愉快"
            }
        })
        
        # 加载玩家记录
        self.player_record = self.config.load("players.yaml", default={})
    
    async def on_enable(self):
        """订阅事件"""
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
        self.event_bus.subscribe(PlayerLeaveEvent, self.on_player_leave)
        self.logger.info("玩家欢迎插件已启用")
    
    async def on_disable(self):
        """取消订阅"""
        self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
        self.event_bus.unsubscribe(PlayerLeaveEvent, self.on_player_leave)
        
        # 保存玩家记录
        self.config.set_all(self.player_record)
        self.config.save("players.yaml")
    
    async def on_player_join(self, event: PlayerJoinEvent):
        """玩家加入处理"""
        player = event.player_name
        
        # 检查是否首次加入
        is_first_join = player not in self.player_record
        
        # 记录玩家
        if is_first_join:
            self.player_record[player] = {
                "first_join": event.timestamp.isoformat(),
                "join_count": 1
            }
        else:
            self.player_record[player]["join_count"] += 1
            self.player_record[player]["last_join"] = event.timestamp.isoformat()
        
        # 获取消息模板
        if is_first_join:
            message = self.config.get("messages.first_join")
        else:
            message = self.config.get("messages.join")
        
        # 替换占位符
        message = message.replace("{player}", player)
        
        # 广播消息
        if self.config.get("broadcast"):
            await self.rcon.execute(f'say {message}')
        
        # 显示标题
        if self.config.get("title.enabled"):
            title = self.config.get("title.title")
            subtitle = self.config.get("title.subtitle")
            await self.rcon.execute(
                f'title {player} title {{"text":"{title}","color":"gold","bold":true}}'
            )
            await self.rcon.execute(
                f'title {player} subtitle {{"text":"{subtitle}","color":"yellow"}}'
            )
        
        self.logger.info(f"{player} 加入游戏 (第 {self.player_record[player]['join_count']} 次)")
    
    async def on_player_leave(self, event: PlayerLeaveEvent):
        """玩家离开处理"""
        player = event.player_name
        
        # 获取消息
        message = self.config.get("messages.leave").replace("{player}", player)
        
        # 广播消息
        if self.config.get("broadcast"):
            await self.rcon.execute(f'say {message}')
        
        self.logger.info(f"{player} 离开游戏")
```

### config.yaml

```yaml
messages:
  join: "欢迎 {player} 加入服务器!"
  leave: "{player} 离开了服务器"
  first_join: "首次加入的玩家 {player}! 欢迎!"

broadcast: true

title:
  enabled: true
  title: "欢迎!"
  subtitle: "祝你游戏愉快"
```

---

## 3. 自动备份插件

定时自动备份服务器世界和玩家数据。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.scheduling import Scheduler
from nethergate.rcon import RconClient
from nethergate.config import ConfigManager
from datetime import datetime
import shutil
import os

class AutoBackupPlugin(Plugin):
    """自动备份插件"""
    
    def __init__(
        self,
        logger: Logger,
        scheduler: Scheduler,
        rcon: RconClient,
        config: ConfigManager
    ):
        self.logger = logger
        self.scheduler = scheduler
        self.rcon = rcon
        self.config = config
        
        self.info = PluginInfo(
            id="com.example.autobackup",
            name="Auto Backup Plugin",
            version="1.0.0"
        )
        
        self.backup_task_id = None
    
    async def on_load(self):
        """加载配置"""
        self.config.load("config.yaml", default={
            "enabled": True,
            "interval_minutes": 60,
            "max_backups": 10,
            "backup_path": "backups",
            "world_path": "world",
            "compress": True,
            "notify_players": True
        })
    
    async def on_enable(self):
        """启动备份任务"""
        if not self.config.get("enabled"):
            self.logger.info("自动备份已禁用")
            return
        
        interval = self.config.get("interval_minutes") * 60  # 转换为秒
        
        self.backup_task_id = self.scheduler.run_repeating(
            callback=self.perform_backup,
            interval_seconds=interval,
            initial_delay=interval  # 首次延迟执行
        )
        
        self.logger.info(f"自动备份已启用 (间隔: {self.config.get('interval_minutes')} 分钟)")
    
    async def on_disable(self):
        """停止备份任务"""
        if self.backup_task_id:
            self.scheduler.cancel(self.backup_task_id)
        self.logger.info("自动备份已停止")
    
    async def perform_backup(self):
        """执行备份"""
        self.logger.info("开始执行自动备份...")
        
        # 通知玩家
        if self.config.get("notify_players"):
            await self.rcon.execute('say §6正在执行自动备份...')
        
        try:
            # 1. 保存世界
            await self.rcon.execute('save-all flush')
            
            # 2. 创建备份目录
            backup_path = self.config.get("backup_path")
            os.makedirs(backup_path, exist_ok=True)
            
            # 3. 生成备份文件名
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            backup_name = f"backup_{timestamp}"
            backup_dir = os.path.join(backup_path, backup_name)
            
            # 4. 复制世界文件
            world_path = self.config.get("world_path")
            shutil.copytree(world_path, backup_dir)
            
            # 5. 压缩备份（可选）
            if self.config.get("compress"):
                archive_path = f"{backup_dir}.zip"
                shutil.make_archive(backup_dir, 'zip', backup_dir)
                shutil.rmtree(backup_dir)  # 删除未压缩的目录
                self.logger.info(f"备份已压缩: {archive_path}")
            else:
                self.logger.info(f"备份已创建: {backup_dir}")
            
            # 6. 清理旧备份
            await self.cleanup_old_backups()
            
            # 7. 通知完成
            if self.config.get("notify_players"):
                await self.rcon.execute('say §a备份完成!')
            
            self.logger.info("自动备份完成")
            
        except Exception as e:
            self.logger.error(f"备份失败: {e}", exception=e)
            if self.config.get("notify_players"):
                await self.rcon.execute('say §c备份失败! 请查看日志')
    
    async def cleanup_old_backups(self):
        """清理旧备份"""
        backup_path = self.config.get("backup_path")
        max_backups = self.config.get("max_backups")
        
        # 获取所有备份
        backups = []
        for item in os.listdir(backup_path):
            full_path = os.path.join(backup_path, item)
            if os.path.isfile(full_path) or os.path.isdir(full_path):
                backups.append((item, os.path.getctime(full_path)))
        
        # 按时间排序
        backups.sort(key=lambda x: x[1], reverse=True)
        
        # 删除超出数量的备份
        if len(backups) > max_backups:
            for backup_name, _ in backups[max_backups:]:
                backup_full_path = os.path.join(backup_path, backup_name)
                if os.path.isfile(backup_full_path):
                    os.remove(backup_full_path)
                else:
                    shutil.rmtree(backup_full_path)
                self.logger.info(f"删除旧备份: {backup_name}")
```

---

## 4. 传送系统插件

完整的传送点管理系统（参考开发指南中的示例）。

---

## 5. 经济系统插件

简单的经济系统，支持货币、交易和商店。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.commands import CommandRegistry, CommandContext
from nethergate.events import EventBus, PlayerJoinEvent
from nethergate.config import ConfigManager
from typing import Dict

class EconomyPlugin(Plugin):
    """经济系统插件"""
    
    def __init__(
        self,
        logger: Logger,
        commands: CommandRegistry,
        event_bus: EventBus,
        config: ConfigManager
    ):
        self.logger = logger
        self.commands = commands
        self.event_bus = event_bus
        self.config = config
        
        self.info = PluginInfo(
            id="com.example.economy",
            name="Economy Plugin",
            version="1.0.0"
        )
        
        # 玩家余额
        self.balances: Dict[str, float] = {}
    
    async def on_load(self):
        """加载数据"""
        self.config.load("config.yaml", default={
            "currency_name": "金币",
            "currency_symbol": "$",
            "starting_balance": 100.0,
            "max_balance": 1000000.0
        })
        
        # 加载玩家余额
        self.balances = self.config.load("balances.yaml", default={})
    
    async def on_enable(self):
        """注册命令和事件"""
        # 注册命令
        self.commands.register("balance", self.cmd_balance, "查看余额", "/balance [player]")
        self.commands.register("pay", self.cmd_pay, "转账", "/pay <player> <amount>")
        self.commands.register("addmoney", self.cmd_addmoney, "添加货币", "/addmoney <player> <amount>", permission="economy.admin")
        self.commands.register("setmoney", self.cmd_setmoney, "设置货币", "/setmoney <player> <amount>", permission="economy.admin")
        
        # 订阅事件
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
        
        self.logger.info("经济系统已启用")
    
    async def on_disable(self):
        """保存数据"""
        # 保存余额
        self.config.set_all(self.balances)
        self.config.save("balances.yaml")
        
        # 注销命令
        self.commands.unregister("balance")
        self.commands.unregister("pay")
        self.commands.unregister("addmoney")
        self.commands.unregister("setmoney")
        
        # 取消订阅
        self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
    
    async def on_player_join(self, event: PlayerJoinEvent):
        """玩家加入时初始化余额"""
        player = event.player_name
        if player not in self.balances:
            self.balances[player] = self.config.get("starting_balance")
            self.logger.info(f"为新玩家 {player} 初始化余额: {self.balances[player]}")
    
    # ========== 命令处理 ==========
    
    async def cmd_balance(self, ctx: CommandContext):
        """查看余额"""
        target = ctx.args[0] if len(ctx.args) > 0 else ctx.sender
        
        if target not in self.balances:
            await ctx.reply(f"玩家 {target} 不存在")
            return
        
        balance = self.balances[target]
        symbol = self.config.get("currency_symbol")
        
        await ctx.reply(f"💰 {target} 的余额: {symbol}{balance:.2f}")
    
    async def cmd_pay(self, ctx: CommandContext):
        """转账"""
        if len(ctx.args) < 2:
            await ctx.reply("用法: /pay <player> <amount>")
            return
        
        target = ctx.args[0]
        try:
            amount = float(ctx.args[1])
        except ValueError:
            await ctx.reply("金额必须是数字")
            return
        
        sender = ctx.sender
        
        # 验证
        if amount <= 0:
            await ctx.reply("金额必须大于 0")
            return
        
        if target not in self.balances:
            await ctx.reply(f"玩家 {target} 不存在")
            return
        
        if self.balances[sender] < amount:
            await ctx.reply("余额不足")
            return
        
        # 执行转账
        self.balances[sender] -= amount
        self.balances[target] += amount
        
        symbol = self.config.get("currency_symbol")
        await ctx.reply(f"✅ 已向 {target} 转账 {symbol}{amount:.2f}")
        
        self.logger.info(f"{sender} 向 {target} 转账 {amount}")
    
    async def cmd_addmoney(self, ctx: CommandContext):
        """添加货币（管理员）"""
        if len(ctx.args) < 2:
            await ctx.reply("用法: /addmoney <player> <amount>")
            return
        
        player = ctx.args[0]
        try:
            amount = float(ctx.args[1])
        except ValueError:
            await ctx.reply("金额必须是数字")
            return
        
        if player not in self.balances:
            self.balances[player] = 0.0
        
        self.balances[player] += amount
        
        # 限制最大值
        max_balance = self.config.get("max_balance")
        if self.balances[player] > max_balance:
            self.balances[player] = max_balance
        
        symbol = self.config.get("currency_symbol")
        await ctx.reply(f"✅ 已为 {player} 添加 {symbol}{amount:.2f}")
        await ctx.reply(f"当前余额: {symbol}{self.balances[player]:.2f}")
    
    async def cmd_setmoney(self, ctx: CommandContext):
        """设置货币（管理员）"""
        if len(ctx.args) < 2:
            await ctx.reply("用法: /setmoney <player> <amount>")
            return
        
        player = ctx.args[0]
        try:
            amount = float(ctx.args[1])
        except ValueError:
            await ctx.reply("金额必须是数字")
            return
        
        self.balances[player] = amount
        
        symbol = self.config.get("currency_symbol")
        await ctx.reply(f"✅ 已将 {player} 的余额设置为 {symbol}{amount:.2f}")
    
    # ========== API 方法 ==========
    
    def get_balance(self, player: str) -> float:
        """获取玩家余额"""
        return self.balances.get(player, 0.0)
    
    def has_money(self, player: str, amount: float) -> bool:
        """检查玩家是否有足够的钱"""
        return self.get_balance(player) >= amount
    
    def deposit(self, player: str, amount: float) -> bool:
        """存款"""
        if player not in self.balances:
            self.balances[player] = 0.0
        
        self.balances[player] += amount
        return True
    
    def withdraw(self, player: str, amount: float) -> bool:
        """取款"""
        if not self.has_money(player, amount):
            return False
        
        self.balances[player] -= amount
        return True
```

---

## 6. 排行榜插件

使用计分板实现的多维度排行榜系统。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.commands import CommandRegistry, CommandContext
from nethergate.scoreboard import ScoreboardManager
from nethergate.events import EventBus, PlayerJoinEvent
from nethergate.rcon import RconClient
from typing import List, Tuple

class LeaderboardPlugin(Plugin):
    """排行榜插件"""
    
    def __init__(
        self,
        logger: Logger,
        commands: CommandRegistry,
        scoreboard: ScoreboardManager,
        event_bus: EventBus,
        rcon: RconClient
    ):
        self.logger = logger
        self.commands = commands
        self.scoreboard = scoreboard
        self.event_bus = event_bus
        self.rcon = rcon
        
        self.info = PluginInfo(
            id="com.example.leaderboard",
            name="Leaderboard Plugin",
            version="1.0.0"
        )
        
        # 排行榜配置
        self.leaderboards = [
            {"name": "kills", "criterion": "playerKillCount", "display": "击杀排行"},
            {"name": "deaths", "criterion": "deathCount", "display": "死亡统计"},
            {"name": "playtime", "criterion": "dummy", "display": "在线时长"},
        ]
    
    async def on_enable(self):
        """创建排行榜"""
        # 创建所有排行榜
        for lb in self.leaderboards:
            await self.scoreboard.create_objective(
                name=lb["name"],
                criterion=lb["criterion"],
                display_name=lb["display"]
            )
        
        # 默认显示击杀排行
        await self.scoreboard.set_display("sidebar", "kills")
        
        # 注册命令
        self.commands.register("top", self.cmd_top, "查看排行榜", "/top [category]")
        self.commands.register("stats", self.cmd_stats, "查看个人统计", "/stats [player]")
        
        self.logger.info("排行榜系统已启用")
    
    async def on_disable(self):
        """清理"""
        self.commands.unregister("top")
        self.commands.unregister("stats")
    
    async def cmd_top(self, ctx: CommandContext):
        """查看排行榜"""
        category = ctx.args[0] if len(ctx.args) > 0 else "kills"
        
        # 验证类别
        valid_categories = [lb["name"] for lb in self.leaderboards]
        if category not in valid_categories:
            await ctx.reply(f"无效的类别。可用: {', '.join(valid_categories)}")
            return
        
        # 获取排行榜数据
        scores = await self.scoreboard.get_scores(category)
        top_players = sorted(scores.items(), key=lambda x: x[1], reverse=True)[:10]
        
        # 获取显示名称
        display_name = next((lb["display"] for lb in self.leaderboards if lb["name"] == category), category)
        
        # 显示排行榜
        await ctx.reply(f"=== {display_name} TOP 10 ===")
        for i, (player, score) in enumerate(top_players, 1):
            await ctx.reply(f"{i}. {player}: {score}")
    
    async def cmd_stats(self, ctx: CommandContext):
        """查看个人统计"""
        player = ctx.args[0] if len(ctx.args) > 0 else ctx.sender
        
        await ctx.reply(f"=== {player} 的统计 ===")
        for lb in self.leaderboards:
            score = await self.scoreboard.get_score(lb["name"], player)
            await ctx.reply(f"{lb['display']}: {score}")
```

---

## 7. 自定义工具箱插件

提供预定义的物品工具箱。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.commands import CommandRegistry, CommandContext
from nethergate.rcon import RconClient
from nethergate.config import ConfigManager

class KitPlugin(Plugin):
    """工具箱插件"""
    
    def __init__(
        self,
        logger: Logger,
        commands: CommandRegistry,
        rcon: RconClient,
        config: ConfigManager
    ):
        self.logger = logger
        self.commands = commands
        self.rcon = rcon
        self.config = config
        
        self.info = PluginInfo(
            id="com.example.kit",
            name="Kit Plugin",
            version="1.0.0"
        )
        
        self.kits = {}
        self.cooldowns = {}  # 冷却时间记录
    
    async def on_load(self):
        """加载工具箱配置"""
        self.config.load("kits.yaml", default={
            "starter": {
                "items": [
                    "wooden_sword 1",
                    "wooden_pickaxe 1",
                    "wooden_axe 1",
                    "bread 16"
                ],
                "cooldown": 0,
                "permission": None
            },
            "pvp": {
                "items": [
                    "diamond_sword{Enchantments:[{id:sharpness,lvl:5}]} 1",
                    "diamond_helmet{Enchantments:[{id:protection,lvl:4}]} 1",
                    "diamond_chestplate{Enchantments:[{id:protection,lvl:4}]} 1",
                    "diamond_leggings{Enchantments:[{id:protection,lvl:4}]} 1",
                    "diamond_boots{Enchantments:[{id:protection,lvl:4}]} 1",
                    "golden_apple 64"
                ],
                "cooldown": 3600,  # 1小时冷却
                "permission": "kit.pvp"
            }
        })
        
        self.kits = self.config.get_all()
    
    async def on_enable(self):
        """注册命令"""
        self.commands.register("kit", self.cmd_kit, "领取工具箱", "/kit <name>")
        self.commands.register("kits", self.cmd_kits, "列出所有工具箱", "/kits")
        
        self.logger.info(f"工具箱插件已启用 ({len(self.kits)} 个工具箱)")
    
    async def on_disable(self):
        """注销命令"""
        self.commands.unregister("kit")
        self.commands.unregister("kits")
    
    async def cmd_kit(self, ctx: CommandContext):
        """领取工具箱"""
        if len(ctx.args) < 1:
            await ctx.reply("用法: /kit <name>")
            await ctx.reply("使用 /kits 查看所有工具箱")
            return
        
        kit_name = ctx.args[0]
        player = ctx.sender
        
        # 检查工具箱是否存在
        if kit_name not in self.kits:
            await ctx.reply(f"工具箱 '{kit_name}' 不存在")
            return
        
        kit = self.kits[kit_name]
        
        # 检查权限
        if kit.get("permission") and not ctx.has_permission(kit["permission"]):
            await ctx.reply("权限不足")
            return
        
        # 检查冷却时间
        import time
        current_time = time.time()
        cooldown_key = f"{player}:{kit_name}"
        
        if cooldown_key in self.cooldowns:
            last_use = self.cooldowns[cooldown_key]
            cooldown = kit.get("cooldown", 0)
            remaining = cooldown - (current_time - last_use)
            
            if remaining > 0:
                minutes = int(remaining // 60)
                seconds = int(remaining % 60)
                await ctx.reply(f"冷却中! 剩余时间: {minutes}分{seconds}秒")
                return
        
        # 给予物品
        for item in kit["items"]:
            await self.rcon.execute(f"give {player} {item}")
        
        # 记录使用时间
        self.cooldowns[cooldown_key] = current_time
        
        await ctx.reply(f"✅ 已领取工具箱: {kit_name}")
        self.logger.info(f"{player} 领取了工具箱: {kit_name}")
    
    async def cmd_kits(self, ctx: CommandContext):
        """列出所有工具箱"""
        await ctx.reply("=== 可用工具箱 ===")
        for kit_name, kit_data in self.kits.items():
            permission = kit_data.get("permission", "无")
            cooldown = kit_data.get("cooldown", 0)
            cooldown_str = f"{cooldown}秒" if cooldown > 0 else "无冷却"
            
            await ctx.reply(f"- {kit_name} (权限: {permission}, 冷却: {cooldown_str})")
```

---

## 8. WebSocket 数据推送插件

实时推送服务器数据到 Web 前端。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.websocket import WebSocketServer
from nethergate.events import EventBus, PlayerJoinEvent, PlayerLeaveEvent, PlayerChatEvent
from nethergate.scheduling import Scheduler
from nethergate.rcon import RconClient
from datetime import datetime

class WebSocketPushPlugin(Plugin):
    """WebSocket 数据推送插件"""
    
    def __init__(
        self,
        logger: Logger,
        ws: WebSocketServer,
        event_bus: EventBus,
        scheduler: Scheduler,
        rcon: RconClient
    ):
        self.logger = logger
        self.ws = ws
        self.event_bus = event_bus
        self.scheduler = scheduler
        self.rcon = rcon
        
        self.info = PluginInfo(
            id="com.example.wspush",
            name="WebSocket Push Plugin",
            version="1.0.0"
        )
        
        self.task_id = None
    
    async def on_enable(self):
        """启用插件"""
        # 订阅事件
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
        self.event_bus.subscribe(PlayerLeaveEvent, self.on_player_leave)
        self.event_bus.subscribe(PlayerChatEvent, self.on_player_chat)
        
        # 启动定时推送任务（每30秒推送服务器状态）
        self.task_id = self.scheduler.run_repeating(
            callback=self.push_server_status,
            interval_seconds=30.0
        )
        
        self.logger.info("WebSocket 推送插件已启用")
    
    async def on_disable(self):
        """禁用插件"""
        # 取消订阅
        self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
        self.event_bus.unsubscribe(PlayerLeaveEvent, self.on_player_leave)
        self.event_bus.unsubscribe(PlayerChatEvent, self.on_player_chat)
        
        # 取消定时任务
        if self.task_id:
            self.scheduler.cancel(self.task_id)
    
    async def on_player_join(self, event: PlayerJoinEvent):
        """玩家加入事件"""
        await self.ws.broadcast({
            "type": "player_join",
            "data": {
                "player": event.player_name,
                "uuid": event.player_uuid,
                "timestamp": event.timestamp.isoformat()
            }
        })
    
    async def on_player_leave(self, event: PlayerLeaveEvent):
        """玩家离开事件"""
        await self.ws.broadcast({
            "type": "player_leave",
            "data": {
                "player": event.player_name,
                "timestamp": event.timestamp.isoformat()
            }
        })
    
    async def on_player_chat(self, event: PlayerChatEvent):
        """玩家聊天事件"""
        await self.ws.broadcast({
            "type": "player_chat",
            "data": {
                "player": event.player_name,
                "message": event.message,
                "timestamp": event.timestamp.isoformat()
            }
        })
    
    async def push_server_status(self):
        """推送服务器状态"""
        # 获取在线玩家列表
        result = await self.rcon.execute("list")
        
        # 获取 TPS（如果可用）
        tps_result = await self.rcon.execute("tps")
        
        await self.ws.broadcast({
            "type": "server_status",
            "data": {
                "online_players": result.response,
                "tps": tps_result.response if tps_result.success else "N/A",
                "timestamp": datetime.now().isoformat()
            }
        })
```

---

## 9. 自动公告插件

定时发送服务器公告。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.scheduling import Scheduler
from nethergate.rcon import RconClient
from nethergate.config import ConfigManager
from typing import List

class AnnouncementPlugin(Plugin):
    """自动公告插件"""
    
    def __init__(
        self,
        logger: Logger,
        scheduler: Scheduler,
        rcon: RconClient,
        config: ConfigManager
    ):
        self.logger = logger
        self.scheduler = scheduler
        self.rcon = rcon
        self.config = config
        
        self.info = PluginInfo(
            id="com.example.announcement",
            name="Announcement Plugin",
            version="1.0.0"
        )
        
        self.task_id = None
        self.messages: List[str] = []
        self.current_index = 0
    
    async def on_load(self):
        """加载配置"""
        self.config.load("config.yaml", default={
            "enabled": True,
            "interval_seconds": 300,  # 5分钟
            "prefix": "§6[公告]§r",
            "messages": [
                "欢迎来到我们的服务器!",
                "请遵守服务器规则",
                "访问我们的网站: example.com",
                "加入我们的Discord: discord.gg/example"
            ]
        })
        
        self.messages = self.config.get("messages", [])
    
    async def on_enable(self):
        """启动公告任务"""
        if not self.config.get("enabled") or not self.messages:
            self.logger.info("自动公告已禁用或无消息")
            return
        
        interval = self.config.get("interval_seconds")
        
        self.task_id = self.scheduler.run_repeating(
            callback=self.send_announcement,
            interval_seconds=interval,
            initial_delay=interval
        )
        
        self.logger.info(f"自动公告已启用 (间隔: {interval}秒, {len(self.messages)} 条消息)")
    
    async def on_disable(self):
        """停止公告任务"""
        if self.task_id:
            self.scheduler.cancel(self.task_id)
    
    async def send_announcement(self):
        """发送公告"""
        if not self.messages:
            return
        
        # 获取当前消息
        message = self.messages[self.current_index]
        prefix = self.config.get("prefix", "")
        
        # 发送消息
        full_message = f"{prefix} {message}"
        await self.rcon.execute(f'say {full_message}')
        
        # 更新索引
        self.current_index = (self.current_index + 1) % len(self.messages)
        
        self.logger.debug(f"已发送公告: {message}")
```

---

## 10. 玩家统计插件

收集和展示玩家游戏统计数据。

### main.py

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger
from nethergate.commands import CommandRegistry, CommandContext
from nethergate.data import PlayerDataReader
from nethergate.events import EventBus, PlayerJoinEvent
from nethergate.config import ConfigManager
from typing import Dict, Any

class PlayerStatsPlugin(Plugin):
    """玩家统计插件"""
    
    def __init__(
        self,
        logger: Logger,
        commands: CommandRegistry,
        player_data: PlayerDataReader,
        event_bus: EventBus,
        config: ConfigManager
    ):
        self.logger = logger
        self.commands = commands
        self.player_data = player_data
        self.event_bus = event_bus
        self.config = config
        
        self.info = PluginInfo(
            id="com.example.playerstats",
            name="Player Stats Plugin",
            version="1.0.0"
        )
        
        self.stats_cache: Dict[str, Dict[str, Any]] = {}
    
    async def on_enable(self):
        """启用插件"""
        # 注册命令
        self.commands.register("stats", self.cmd_stats, "查看玩家统计", "/stats [player]")
        self.commands.register("playtime", self.cmd_playtime, "查看在线时长", "/playtime [player]")
        
        # 订阅事件
        self.event_bus.subscribe(PlayerJoinEvent, self.on_player_join)
        
        self.logger.info("玩家统计插件已启用")
    
    async def on_disable(self):
        """禁用插件"""
        self.commands.unregister("stats")
        self.commands.unregister("playtime")
        self.event_bus.unsubscribe(PlayerJoinEvent, self.on_player_join)
    
    async def on_player_join(self, event: PlayerJoinEvent):
        """玩家加入时更新统计"""
        player = event.player_name
        await self.update_stats(player)
    
    async def update_stats(self, player: str):
        """更新玩家统计"""
        data = await self.player_data.read(player)
        
        if data:
            self.stats_cache[player] = {
                "health": data.get("Health", 0),
                "food": data.get("foodLevel", 0),
                "xp_level": data.get("XpLevel", 0),
                "xp_total": data.get("XpTotal", 0),
                "score": data.get("Score", 0),
                "inventory_count": len(data.get("Inventory", [])),
                "position": data.get("Pos", [0, 0, 0])
            }
    
    async def cmd_stats(self, ctx: CommandContext):
        """查看玩家统计"""
        player = ctx.args[0] if len(ctx.args) > 0 else ctx.sender
        
        # 更新统计
        await self.update_stats(player)
        
        if player not in self.stats_cache:
            await ctx.reply(f"玩家 {player} 的数据不可用")
            return
        
        stats = self.stats_cache[player]
        
        await ctx.reply(f"=== {player} 的统计 ===")
        await ctx.reply(f"生命值: {stats['health']:.1f}")
        await ctx.reply(f"饥饿值: {stats['food']}")
        await ctx.reply(f"经验等级: {stats['xp_level']}")
        await ctx.reply(f"总经验: {stats['xp_total']}")
        await ctx.reply(f"分数: {stats['score']}")
        await ctx.reply(f"背包物品数: {stats['inventory_count']}")
        
        pos = stats['position']
        await ctx.reply(f"位置: ({pos[0]:.1f}, {pos[1]:.1f}, {pos[2]:.1f})")
    
    async def cmd_playtime(self, ctx: CommandContext):
        """查看在线时长"""
        player = ctx.args[0] if len(ctx.args) > 0 else ctx.sender
        
        data = await self.player_data.read(player)
        
        if data:
            # 读取游戏统计数据
            play_ticks = data.get("stats", {}).get("minecraft:custom", {}).get("minecraft:play_time", 0)
            
            # 转换为小时和分钟
            play_minutes = play_ticks // 20 // 60
            hours = play_minutes // 60
            minutes = play_minutes % 60
            
            await ctx.reply(f"{player} 的在线时长: {hours} 小时 {minutes} 分钟")
        else:
            await ctx.reply(f"无法获取 {player} 的数据")
```

---

## 总结

这些示例展示了 Python 插件的各种使用场景：

1. **基础功能**: Hello World, 玩家欢迎
2. **服务器管理**: 自动备份, 自动公告
3. **游戏功能**: 传送系统, 工具箱, 经济系统
4. **数据展示**: 排行榜, 玩家统计
5. **高级功能**: WebSocket 推送

所有示例都遵循最佳实践：
- ✅ 使用依赖注入
- ✅ 正确的生命周期管理
- ✅ 完善的错误处理
- ✅ 配置文件支持
- ✅ 清晰的代码结构

## 下一步

- 参考 [Python 插件开发指南](../03-插件开发/Python插件开发指南.md)
- 查看 [Python API 参考](../08-参考/Python_API参考.md)
- 了解 [发布流程](../03-插件开发/发布流程.md)

