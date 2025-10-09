"""
SMP (Server Management Protocol) API

提供服务器管理协议功能，包括白名单、封禁、玩家管理等
"""

from typing import List, Dict, Optional, Any
from dataclasses import dataclass
from enum import Enum


# ========== 数据类 ==========

@dataclass
class PlayerDto:
    """玩家数据传输对象"""
    uuid: str
    name: str


@dataclass
class UserBanDto:
    """玩家封禁数据"""
    uuid: str
    name: str
    created: str
    source: str
    expires: Optional[str] = None
    reason: Optional[str] = None


@dataclass
class IpBanDto:
    """IP 封禁数据"""
    ip: str
    created: str
    source: str
    expires: Optional[str] = None
    reason: Optional[str] = None


@dataclass
class OperatorDto:
    """管理员数据"""
    uuid: str
    name: str
    level: int = 4
    bypass_player_limit: bool = False


class ServerState(Enum):
    """服务器状态"""
    STARTING = "starting"
    RUNNING = "running"
    STOPPING = "stopping"
    STOPPED = "stopped"


@dataclass
class TypedRule:
    """游戏规则（带类型）"""
    name: str
    value: Any
    type: str  # "boolean", "integer", etc.


# ========== SMP API ==========

class SmpApi:
    """
    SMP (Server Management Protocol) API
    
    提供服务器管理功能的高级接口
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    # ========== 白名单管理 ==========
    
    async def get_allowlist(self) -> List[PlayerDto]:
        """
        获取白名单
        
        Returns:
            白名单玩家列表
        """
        pass
    
    async def set_allowlist(self, players: List[PlayerDto]):
        """
        设置白名单（替换整个列表）
        
        Args:
            players: 新的白名单列表
        """
        pass
    
    async def add_to_allowlist(self, player: PlayerDto):
        """
        添加玩家到白名单
        
        Args:
            player: 要添加的玩家
        """
        pass
    
    async def remove_from_allowlist(self, player: PlayerDto):
        """
        从白名单移除玩家
        
        Args:
            player: 要移除的玩家
        """
        pass
    
    async def clear_allowlist(self):
        """清空白名单"""
        pass
    
    # ========== 封禁管理 ==========
    
    async def get_bans(self) -> List[UserBanDto]:
        """
        获取玩家封禁列表
        
        Returns:
            封禁列表
        """
        pass
    
    async def set_bans(self, bans: List[UserBanDto]):
        """
        设置玩家封禁列表（替换整个列表）
        
        Args:
            bans: 新的封禁列表
        """
        pass
    
    async def add_ban(self, ban: UserBanDto):
        """
        封禁玩家
        
        Args:
            ban: 封禁信息
        """
        pass
    
    async def remove_ban(self, player: PlayerDto):
        """
        解除玩家封禁
        
        Args:
            player: 要解封的玩家
        """
        pass
    
    async def clear_bans(self):
        """清空玩家封禁列表"""
        pass
    
    # ========== IP 封禁管理 ==========
    
    async def get_ip_bans(self) -> List[IpBanDto]:
        """
        获取 IP 封禁列表
        
        Returns:
            IP 封禁列表
        """
        pass
    
    async def set_ip_bans(self, bans: List[IpBanDto]):
        """
        设置 IP 封禁列表（替换整个列表）
        
        Args:
            bans: 新的 IP 封禁列表
        """
        pass
    
    async def add_ip_ban(self, ban: IpBanDto):
        """
        封禁 IP
        
        Args:
            ban: IP 封禁信息
        """
        pass
    
    async def remove_ip_ban(self, ip: str):
        """
        解除 IP 封禁
        
        Args:
            ip: 要解封的 IP 地址
        """
        pass
    
    async def clear_ip_bans(self):
        """清空 IP 封禁列表"""
        pass
    
    # ========== 玩家管理 ==========
    
    async def get_players(self) -> List[PlayerDto]:
        """
        获取在线玩家列表
        
        Returns:
            在线玩家列表
        """
        pass
    
    async def kick_player(self, player_name: str, reason: Optional[str] = None):
        """
        踢出玩家
        
        Args:
            player_name: 玩家名称
            reason: 踢出原因（可选）
        """
        pass
    
    # ========== 管理员管理 ==========
    
    async def get_operators(self) -> List[OperatorDto]:
        """
        获取管理员列表
        
        Returns:
            管理员列表
        """
        pass
    
    async def set_operators(self, operators: List[OperatorDto]):
        """
        设置管理员列表（替换整个列表）
        
        Args:
            operators: 新的管理员列表
        """
        pass
    
    async def add_operator(self, op: OperatorDto):
        """
        添加管理员
        
        Args:
            op: 管理员信息
        """
        pass
    
    async def remove_operator(self, player: PlayerDto):
        """
        移除管理员
        
        Args:
            player: 要移除的玩家
        """
        pass
    
    async def clear_operators(self):
        """清空管理员列表"""
        pass
    
    # ========== 服务器管理 ==========
    
    async def get_server_status(self) -> ServerState:
        """
        获取服务器状态
        
        Returns:
            服务器状态
        """
        pass
    
    async def save_world(self):
        """保存世界"""
        pass
    
    async def stop_server(self):
        """停止服务器"""
        pass
    
    async def send_system_message(self, message: str):
        """
        发送系统消息
        
        Args:
            message: 消息内容
        """
        pass
    
    # ========== 游戏规则管理 ==========
    
    async def get_game_rules(self) -> Dict[str, TypedRule]:
        """
        获取所有游戏规则
        
        Returns:
            游戏规则字典
        """
        pass
    
    async def update_game_rule(self, rule: str, value: Any):
        """
        更新游戏规则
        
        Args:
            rule: 规则名称
            value: 新值
        """
        pass
    
    # ========== 服务器设置 ==========
    
    async def get_server_settings(self) -> Dict[str, Any]:
        """
        获取服务器设置
        
        Returns:
            服务器设置字典
        """
        pass
    
    async def get_server_setting(self, key: str) -> Any:
        """
        获取指定服务器设置
        
        Args:
            key: 设置键名
            
        Returns:
            设置值
        """
        pass
    
    async def set_server_setting(self, key: str, value: Any):
        """
        设置服务器设置
        
        Args:
            key: 设置键名
            value: 新值
        """
        pass

