"""
事件系统

提供事件订阅和发布功能
"""

from typing import Callable, Type, Any
from datetime import datetime


class Event:
    """基础事件类"""
    
    def __init__(self):
        self.timestamp = datetime.now()


# ========== 服务器事件 ==========

class ServerStartingEvent(Event):
    """服务器启动中事件"""
    pass


class ServerStartedEvent(Event):
    """服务器已启动事件"""
    pass


class ServerStoppingEvent(Event):
    """服务器停止中事件"""
    pass


class ServerStoppedEvent(Event):
    """服务器已停止事件"""
    pass


# ========== 玩家事件 ==========

class PlayerJoinEvent(Event):
    """玩家加入事件"""
    
    def __init__(self, player_name: str, player_uuid: str):
        super().__init__()
        self.player_name = player_name
        self.player_uuid = player_uuid


class PlayerLeaveEvent(Event):
    """玩家离开事件"""
    
    def __init__(self, player_name: str):
        super().__init__()
        self.player_name = player_name


class PlayerChatEvent(Event):
    """玩家聊天事件"""
    
    def __init__(self, player_name: str, message: str):
        super().__init__()
        self.player_name = player_name
        self.message = message
        self._cancelled = False
    
    def cancel(self):
        """取消事件"""
        self._cancelled = True
    
    def is_cancelled(self) -> bool:
        """检查事件是否被取消"""
        return self._cancelled


class PlayerDeathEvent(Event):
    """玩家死亡事件"""
    
    def __init__(self, player_name: str, death_message: str):
        super().__init__()
        self.player_name = player_name
        self.death_message = death_message


class PlayerAdvancementEvent(Event):
    """玩家达成成就事件"""
    
    def __init__(self, player_name: str, advancement: str):
        super().__init__()
        self.player_name = player_name
        self.advancement = advancement


# ========== 网络事件 ==========

class RconConnectedEvent(Event):
    """RCON 已连接事件"""
    pass


class RconDisconnectedEvent(Event):
    """RCON 已断开事件"""
    pass


class WebSocketClientConnected(Event):
    """WebSocket 客户端连接事件"""
    
    def __init__(self, client_id: str):
        super().__init__()
        self.client_id = client_id


class WebSocketClientDisconnected(Event):
    """WebSocket 客户端断开事件"""
    
    def __init__(self, client_id: str):
        super().__init__()
        self.client_id = client_id


# ========== 事件总线 ==========

class EventBus:
    """
    事件总线
    
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    def subscribe(self, event_type: Type[Event], handler: Callable[[Event], Any]):
        """
        订阅事件
        
        Args:
            event_type: 事件类型
            handler: 事件处理函数
        """
        pass
    
    def unsubscribe(self, event_type: Type[Event], handler: Callable[[Event], Any]):
        """
        取消订阅事件
        
        Args:
            event_type: 事件类型
            handler: 事件处理函数
        """
        pass
    
    async def publish(self, event: Event):
        """
        发布事件
        
        Args:
            event: 事件实例
        """
        pass

