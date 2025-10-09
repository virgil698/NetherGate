"""
任务调度器

提供定时任务和延迟执行功能
"""

from typing import Callable, Awaitable
from datetime import datetime


class Scheduler:
    """
    任务调度器
    
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    def run_delayed(
        self,
        callback: Callable[[], Awaitable[None]],
        delay_seconds: float
    ) -> str:
        """
        延迟执行任务
        
        Args:
            callback: 要执行的函数
            delay_seconds: 延迟时间（秒）
            
        Returns:
            任务 ID
        """
        return ""
    
    def run_repeating(
        self,
        callback: Callable[[], Awaitable[None]],
        interval_seconds: float,
        initial_delay: float = 0.0
    ) -> str:
        """
        定时重复执行任务
        
        Args:
            callback: 要执行的函数
            interval_seconds: 执行间隔（秒）
            initial_delay: 初始延迟（秒）
            
        Returns:
            任务 ID
        """
        return ""
    
    def run_at(
        self,
        callback: Callable[[], Awaitable[None]],
        run_time: datetime
    ) -> str:
        """
        在指定时间执行任务
        
        Args:
            callback: 要执行的函数
            run_time: 执行时间
            
        Returns:
            任务 ID
        """
        return ""
    
    def cancel(self, task_id: str):
        """
        取消任务
        
        Args:
            task_id: 任务 ID
        """
        pass
    
    def cancel_all(self):
        """取消所有任务"""
        pass

