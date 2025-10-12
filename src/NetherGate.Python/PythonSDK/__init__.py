"""
NetherGate Python SDK

为 Python 插件提供 NetherGate API 的 Python 绑定
"""

__version__ = "2.0.0"
__author__ = "NetherGate Team"

from .plugin import Plugin, PluginInfo
from .logging import Logger, LogLevel
from .events import (
    Event, EventBus,
    ServerStartingEvent, ServerStartedEvent, ServerStoppingEvent, ServerStoppedEvent,
    PlayerJoinEvent, PlayerLeaveEvent, PlayerChatEvent, PlayerDeathEvent, PlayerAdvancementEvent,
    RconConnectedEvent, RconDisconnectedEvent,
    WebSocketClientConnected, WebSocketClientDisconnected
)
from .commands import CommandRegistry, CommandContext
from .rcon import RconClient, RconResponse
from .scheduling import Scheduler
from .config import ConfigManager
from .smp import (
    SmpApi, PlayerDto, UserBanDto, IpBanDto, OperatorDto, 
    ServerState, TypedRule
)
from .logmatcher import (
    ILogMatcher, RegexLogMatcher, ServerEvent,
    PlayerJoinMatcher, PlayerLeaveMatcher, PlayerChatMatcher, ServerDoneMatcher
)
from .data import (
    PlayerDataReader, WorldDataReader,
    PlayerData, PlayerStats, PlayerAdvancements,
    GameMode, PlayerPosition, ItemStack, PlayerArmor, StatusEffect,
    WorldData, CompletedAdvancement, AdvancementProgress
)

# ========== 新增高级功能 ==========

from .gamedisplay import GameDisplayApi
from .gameutils import (
    GameUtilities, CommandSequence,
    Position, Region, FireworkType, FireworkOptions
)
from .musicplayer import MusicPlayer, Melody, Note, Instrument
from .blockdata import (
    BlockDataReader, BlockDataWriter, ContainerData
)
from .advanced import (
    # NBT 写入
    NbtDataWriter,
    # 物品组件
    ItemComponentReader, ItemComponentWriter,
    # 玩家档案
    PlayerProfileApi, PlayerProfile, ProfileProperty,
    # 标签系统
    TagApi,
    # 计分板
    ScoreboardApi,
    # 成就追踪
    AdvancementTracker, AdvancementProgress,
    # 统计追踪
    StatisticsTracker, StatisticsData,
    # 排行榜
    LeaderboardSystem, Leaderboard, LeaderboardEntry
)
from .system import (
    # 文件系统
    FileWatcher, ServerFileAccess, BackupManager,
    FileChangeEvent, FileChangeType,
    # 性能监控
    PerformanceMonitor, PerformanceMetrics,
    # WebSocket
    DataBroadcaster, WebSocketMessage,
    # 插件间通信
    PluginMessenger,
    # 日志监听
    LogListener, LogPattern
)

__all__ = [
    # Core
    'Plugin',
    'PluginInfo',
    
    # Logging
    'Logger',
    'LogLevel',
    
    # Events
    'Event',
    'EventBus',
    'ServerStartingEvent',
    'ServerStartedEvent',
    'ServerStoppingEvent',
    'ServerStoppedEvent',
    'PlayerJoinEvent',
    'PlayerLeaveEvent',
    'PlayerChatEvent',
    'PlayerDeathEvent',
    'PlayerAdvancementEvent',
    'RconConnectedEvent',
    'RconDisconnectedEvent',
    'WebSocketClientConnected',
    'WebSocketClientDisconnected',
    
    # Commands
    'CommandRegistry',
    'CommandContext',
    
    # RCON
    'RconClient',
    'RconResponse',
    
    # Scheduling
    'Scheduler',
    
    # Config
    'ConfigManager',
    
    # SMP API
    'SmpApi',
    'PlayerDto',
    'UserBanDto',
    'IpBanDto',
    'OperatorDto',
    'ServerState',
    'TypedRule',
    
    # Log Matchers
    'ILogMatcher',
    'RegexLogMatcher',
    'ServerEvent',
    'PlayerJoinMatcher',
    'PlayerLeaveMatcher',
    'PlayerChatMatcher',
    'ServerDoneMatcher',
    
    # Data API
    'PlayerDataReader',
    'WorldDataReader',
    'PlayerData',
    'PlayerStats',
    'PlayerAdvancements',
    'GameMode',
    'PlayerPosition',
    'ItemStack',
    'PlayerArmor',
    'StatusEffect',
    'WorldData',
    'CompletedAdvancement',
    'AdvancementProgress',
    
    # ========== 高级功能 ==========
    
    # 游戏显示
    'GameDisplayApi',
    
    # 游戏工具
    'GameUtilities',
    'CommandSequence',
    'Position',
    'Region',
    'FireworkType',
    'FireworkOptions',
    
    # 音乐播放器
    'MusicPlayer',
    'Melody',
    'Note',
    'Instrument',
    
    # 方块数据
    'BlockDataReader',
    'BlockDataWriter',
    'ContainerData',
    
    # NBT 写入
    'NbtDataWriter',
    
    # 物品组件
    'ItemComponentReader',
    'ItemComponentWriter',
    
    # 玩家档案
    'PlayerProfileApi',
    'PlayerProfile',
    'ProfileProperty',
    
    # 标签系统
    'TagApi',
    
    # 计分板
    'ScoreboardApi',
    
    # 成就追踪
    'AdvancementTracker',
    'AdvancementProgress',
    
    # 统计追踪
    'StatisticsTracker',
    'StatisticsData',
    
    # 排行榜
    'LeaderboardSystem',
    'Leaderboard',
    'LeaderboardEntry',
    
    # 文件系统
    'FileWatcher',
    'ServerFileAccess',
    'BackupManager',
    'FileChangeEvent',
    'FileChangeType',
    
    # 性能监控
    'PerformanceMonitor',
    'PerformanceMetrics',
    
    # WebSocket
    'DataBroadcaster',
    'WebSocketMessage',
    
    # 插件间通信
    'PluginMessenger',
    
    # 日志监听
    'LogListener',
    'LogPattern',
]
