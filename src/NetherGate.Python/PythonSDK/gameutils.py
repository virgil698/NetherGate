"""
游戏工具 API

提供游戏实用工具功能（烟花、命令序列、区域操作等）
"""

from typing import List, Optional, Callable, Union, Awaitable
from dataclasses import dataclass
from enum import Enum


# ========== 数据类 ==========

@dataclass
class Position:
    """位置坐标"""
    x: float
    y: float
    z: float


@dataclass
class Region:
    """区域（两个位置定义的长方体）"""
    from_pos: Position
    to_pos: Position


class FireworkType(Enum):
    """烟花类型"""
    SMALL_BALL = "small_ball"
    LARGE_BALL = "large_ball"
    STAR = "star"
    CREEPER = "creeper"
    BURST = "burst"


@dataclass
class FireworkOptions:
    """烟花选项"""
    type: FireworkType = FireworkType.LARGE_BALL
    colors: List[str] = None
    fade_colors: List[str] = None
    flicker: bool = False
    trail: bool = False
    power: int = 1
    
    def __post_init__(self):
        if self.colors is None:
            self.colors = ["red", "yellow"]
        if self.fade_colors is None:
            self.fade_colors = []


# ========== 命令序列 ==========

class CommandSequence:
    """
    命令序列构建器
    
    用于创建延时和重复的命令序列
    """
    
    def execute(self, action: Union[Callable[[], None], Callable[[], Awaitable[None]]]) -> 'CommandSequence':
        """
        添加要执行的动作
        
        Args:
            action: 要执行的函数（同步或异步）
            
        Returns:
            自身，用于链式调用
            
        Example:
            >>> seq = sequence.execute(lambda: print("Hello"))
        """
        pass
    
    def wait_ticks(self, ticks: int) -> 'CommandSequence':
        """
        等待指定 tick 数
        
        Args:
            ticks: tick 数量（1秒 = 20 ticks）
            
        Returns:
            自身，用于链式调用
        """
        pass
    
    def wait_seconds(self, seconds: float) -> 'CommandSequence':
        """
        等待指定秒数
        
        Args:
            seconds: 秒数
            
        Returns:
            自身，用于链式调用
        """
        pass
    
    def repeat(self, times: int) -> 'CommandSequence':
        """
        重复执行序列
        
        Args:
            times: 重复次数
            
        Returns:
            自身，用于链式调用
        """
        pass
    
    async def run(self):
        """
        运行序列
        
        Example:
            >>> await sequence.execute(action1).wait_seconds(1).execute(action2).run()
        """
        pass


# ========== 游戏工具 API ==========

class GameUtilities:
    """
    游戏工具 API
    
    提供高级游戏操作功能
    注意：这是一个接口类，实际实现由 C# 桥接提供
    ⚠️ 需要 RCON 支持
    """
    
    # ========== 烟花系统 ==========
    
    async def launch_firework(self, position: Position, options: FireworkOptions):
        """
        发射单个烟花
        
        Args:
            position: 烟花位置
            options: 烟花选项
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> opts = FireworkOptions(
            ...     type=FireworkType.LARGE_BALL,
            ...     colors=["red", "gold"],
            ...     flicker=True,
            ...     trail=True,
            ...     power=2
            ... )
            >>> await utils.launch_firework(pos, opts)
        """
        pass
    
    async def launch_firework_show(
        self,
        positions: List[Position],
        options: FireworkOptions,
        interval_ms: int = 200
    ):
        """
        发射烟花表演（多个烟花按间隔发射）
        
        Args:
            positions: 烟花位置列表
            options: 烟花选项
            interval_ms: 发射间隔（毫秒）
            
        Example:
            >>> positions = [Position(100, 64, 200), Position(110, 64, 210)]
            >>> opts = FireworkOptions(colors=["red", "blue"])
            >>> await utils.launch_firework_show(positions, opts, 500)
        """
        pass
    
    # ========== 命令序列 ==========
    
    def create_sequence(self) -> CommandSequence:
        """
        创建命令序列
        
        Returns:
            命令序列构建器
            
        Example:
            >>> seq = utils.create_sequence()
            >>> await seq.execute(action1).wait_seconds(1).execute(action2).run()
        """
        pass
    
    # ========== 区域操作 ==========
    
    async def fill_area(self, region: Region, block_type: str):
        """
        填充区域
        
        Args:
            region: 要填充的区域
            block_type: 方块类型
            
        Example:
            >>> region = Region(Position(0, 60, 0), Position(10, 70, 10))
            >>> await utils.fill_area(region, "minecraft:stone")
        """
        pass
    
    async def clone_area(self, source: Region, destination: Position):
        """
        复制区域
        
        Args:
            source: 源区域
            destination: 目标位置
            
        Example:
            >>> source = Region(Position(0, 60, 0), Position(10, 70, 10))
            >>> dest = Position(100, 60, 100)
            >>> await utils.clone_area(source, dest)
        """
        pass
    
    async def set_block(self, position: Position, block_type: str):
        """
        设置单个方块
        
        Args:
            position: 方块位置
            block_type: 方块类型
            
        Example:
            >>> await utils.set_block(Position(100, 64, 200), "minecraft:diamond_block")
        """
        pass
    
    # ========== 时间和天气 ==========
    
    async def set_time(self, time: Union[int, str]):
        """
        设置世界时间
        
        Args:
            time: 时间值（tick 数或 "day"/"night"/"noon"/"midnight"）
            
        Example:
            >>> await utils.set_time("day")
            >>> await utils.set_time(6000)  # noon
        """
        pass
    
    async def set_weather(self, weather: str, duration: Optional[int] = None):
        """
        设置天气
        
        Args:
            weather: 天气类型 ("clear"/"rain"/"thunder")
            duration: 持续时间（秒，可选）
            
        Example:
            >>> await utils.set_weather("clear")
            >>> await utils.set_weather("rain", 300)  # 5分钟
        """
        pass
    
    # ========== 传送 ==========
    
    async def teleport(self, selector: str, position: Position):
        """
        传送实体到指定位置
        
        Args:
            selector: 目标选择器
            position: 目标位置
            
        Example:
            >>> await utils.teleport("@a", Position(0, 64, 0))
        """
        pass
    
    async def teleport_relative(self, selector: str, offset: Position):
        """
        相对传送
        
        Args:
            selector: 目标选择器
            offset: 相对偏移量
            
        Example:
            >>> await utils.teleport_relative("@p", Position(0, 10, 0))  # 上升10格
        """
        pass
    
    # ========== 效果 ==========
    
    async def give_effect(
        self,
        selector: str,
        effect: str,
        duration: int,
        amplifier: int = 0,
        hide_particles: bool = False
    ):
        """
        给予状态效果
        
        Args:
            selector: 目标选择器
            effect: 效果 ID（如 "minecraft:speed"）
            duration: 持续时间（秒）
            amplifier: 效果等级（0=I, 1=II, ...）
            hide_particles: 是否隐藏粒子效果
            
        Example:
            >>> await utils.give_effect("@a", "minecraft:speed", 60, 1)
        """
        pass
    
    async def clear_effects(self, selector: str):
        """
        清除所有状态效果
        
        Args:
            selector: 目标选择器
            
        Example:
            >>> await utils.clear_effects("@a")
        """
        pass
    
    # ========== 粒子 ==========
    
    async def spawn_particle(
        self,
        particle: str,
        position: Position,
        count: int = 1,
        spread: Optional[Position] = None,
        speed: float = 0.0
    ):
        """
        生成粒子效果
        
        Args:
            particle: 粒子类型（如 "minecraft:heart"）
            position: 位置
            count: 粒子数量
            spread: 扩散范围
            speed: 速度
            
        Example:
            >>> await utils.spawn_particle(
            ...     "minecraft:heart",
            ...     Position(100, 64, 200),
            ...     count=10,
            ...     spread=Position(1, 1, 1)
            ... )
        """
        pass
    
    # ========== 声音 ==========
    
    async def play_sound(
        self,
        selector: str,
        sound: str,
        volume: float = 1.0,
        pitch: float = 1.0
    ):
        """
        播放声音
        
        Args:
            selector: 目标选择器
            sound: 声音 ID（如 "minecraft:entity.player.levelup"）
            volume: 音量（0.0 - 1.0）
            pitch: 音调（0.5 - 2.0）
            
        Example:
            >>> await utils.play_sound("@a", "minecraft:entity.player.levelup", 1.0, 1.0)
        """
        pass
    
    async def play_sound_at(
        self,
        position: Position,
        sound: str,
        volume: float = 1.0,
        pitch: float = 1.0
    ):
        """
        在指定位置播放声音
        
        Args:
            position: 位置
            sound: 声音 ID
            volume: 音量
            pitch: 音调
            
        Example:
            >>> await utils.play_sound_at(Position(100, 64, 200), "minecraft:block.note_block.pling")
        """
        pass
    
    async def stop_sound(self, selector: str, sound: Optional[str] = None):
        """
        停止声音
        
        Args:
            selector: 目标选择器
            sound: 声音 ID（可选，如果不指定则停止所有声音）
            
        Example:
            >>> await utils.stop_sound("@a")
            >>> await utils.stop_sound("@a", "minecraft:music.game")
        """
        pass

