"""
NetherGate Python SDK

为 Python 插件提供 NetherGate API 的 Python 绑定
"""

__version__ = "1.0.0"
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
]
