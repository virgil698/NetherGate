"""
日志匹配器

用于将服务器日志转换为强类型事件
"""

import re
from typing import Optional, Pattern
from abc import ABC, abstractmethod


class ServerEvent:
    """服务器事件基类"""
    pass


class ILogMatcher(ABC):
    """
    日志匹配器接口
    
    用于将日志行转为强类型事件
    """
    
    @property
    @abstractmethod
    def priority(self) -> int:
        """
        匹配器优先级（数字越大越先执行）
        
        Returns:
            优先级值
        """
        pass
    
    @abstractmethod
    def try_match(self, message: str, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """
        尝试匹配日志消息并产生事件
        
        Args:
            message: 日志消息（已剥离时间戳/线程/级别）
            level: 日志级别
            thread: 线程名（可空）
            
        Returns:
            匹配成功返回事件，失败返回 None
        """
        pass


class RegexLogMatcher(ILogMatcher):
    """
    基于正则的日志匹配器基类
    
    提供正则表达式匹配功能，子类只需实现 on_match 方法
    """
    
    def __init__(self, pattern: str, priority: int = 0, flags: int = 0):
        """
        构造函数
        
        Args:
            pattern: 正则表达式模式字符串
            priority: 优先级，默认为 0
            flags: 正则表达式标志，默认为 0
        """
        self._pattern: Pattern = re.compile(pattern, flags)
        self._priority = priority
    
    @property
    def priority(self) -> int:
        """匹配器优先级"""
        return self._priority
    
    @property
    def pattern(self) -> Pattern:
        """正则表达式模式"""
        return self._pattern
    
    def try_match(self, message: str, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """
        尝试匹配日志消息并产生事件
        
        Args:
            message: 日志消息
            level: 日志级别
            thread: 线程名
            
        Returns:
            匹配成功返回事件，失败返回 None
        """
        match = self._pattern.match(message)
        if match:
            return self.on_match(match, level, thread)
        return None
    
    @abstractmethod
    def on_match(self, match: re.Match, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """
        当正则匹配成功时调用，由子类实现以生成具体事件
        
        Args:
            match: 正则匹配结果
            level: 日志级别
            thread: 线程名
            
        Returns:
            生成的服务器事件
        """
        pass


# ========== 示例匹配器 ==========

class PlayerJoinMatcher(RegexLogMatcher):
    """玩家加入匹配器示例"""
    
    def __init__(self):
        # 匹配格式: "Player123 joined the game"
        super().__init__(
            pattern=r"^(\w+) joined the game$",
            priority=100
        )
    
    def on_match(self, match: re.Match, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """生成玩家加入事件"""
        from .events import PlayerJoinEvent
        player_name = match.group(1)
        # 注意：实际使用时需要获取 UUID
        return PlayerJoinEvent(player_name=player_name, player_uuid="")


class PlayerLeaveMatcher(RegexLogMatcher):
    """玩家离开匹配器示例"""
    
    def __init__(self):
        # 匹配格式: "Player123 left the game"
        super().__init__(
            pattern=r"^(\w+) left the game$",
            priority=100
        )
    
    def on_match(self, match: re.Match, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """生成玩家离开事件"""
        from .events import PlayerLeaveEvent
        player_name = match.group(1)
        return PlayerLeaveEvent(player_name=player_name)


class PlayerChatMatcher(RegexLogMatcher):
    """玩家聊天匹配器示例"""
    
    def __init__(self):
        # 匹配格式: "<Player123> Hello world"
        super().__init__(
            pattern=r"^<(\w+)> (.+)$",
            priority=100
        )
    
    def on_match(self, match: re.Match, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """生成玩家聊天事件"""
        from .events import PlayerChatEvent
        player_name = match.group(1)
        message = match.group(2)
        return PlayerChatEvent(player_name=player_name, message=message)


class ServerDoneMatcher(RegexLogMatcher):
    """服务器启动完成匹配器示例"""
    
    def __init__(self):
        # 匹配格式: "Done (3.5s)! For help, type..."
        super().__init__(
            pattern=r"^Done \(([0-9.]+)s\)!",
            priority=100
        )
    
    def on_match(self, match: re.Match, level: str, thread: Optional[str]) -> Optional[ServerEvent]:
        """生成服务器启动完成事件"""
        from .events import ServerStartedEvent
        return ServerStartedEvent()

