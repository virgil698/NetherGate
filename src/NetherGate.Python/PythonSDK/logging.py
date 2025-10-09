"""
日志系统
"""

from enum import IntEnum
from typing import Optional


class LogLevel(IntEnum):
    """日志级别"""
    TRACE = 0
    DEBUG = 1
    INFO = 2
    WARNING = 3
    ERROR = 4


class Logger:
    """
    日志记录器
    
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    def trace(self, message: str):
        """跟踪级别日志"""
        pass
    
    def debug(self, message: str):
        """调试级别日志"""
        pass
    
    def info(self, message: str):
        """信息级别日志"""
        pass
    
    def warning(self, message: str):
        """警告级别日志"""
        pass
    
    def error(self, message: str, exception: Optional[Exception] = None):
        """错误级别日志"""
        pass
    
    def set_level(self, level: LogLevel):
        """设置日志级别"""
        pass

