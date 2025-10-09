"""
命令系统

提供命令注册和处理功能
"""

from typing import List, Callable, Awaitable, Optional


class CommandContext:
    """
    命令执行上下文
    
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    def __init__(self):
        self.command_name: str = ""
        self.args: List[str] = []
        self.sender: str = ""
        self.is_console: bool = False
        self.usage: str = ""
    
    async def reply(self, message: str):
        """
        回复消息
        
        Args:
            message: 消息内容
        """
        pass
    
    def has_permission(self, permission: str) -> bool:
        """
        检查权限
        
        Args:
            permission: 权限节点
            
        Returns:
            是否有权限
        """
        return True


class CommandRegistry:
    """
    命令注册器
    
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    def register(
        self,
        name: str,
        callback: Callable[[CommandContext], Awaitable[None]],
        description: str = "",
        usage: str = "",
        permission: Optional[str] = None,
        aliases: Optional[List[str]] = None
    ):
        """
        注册命令
        
        Args:
            name: 命令名称
            callback: 命令处理函数
            description: 命令描述
            usage: 用法说明
            permission: 所需权限
            aliases: 命令别名
        """
        pass
    
    def unregister(self, name: str):
        """
        注销命令
        
        Args:
            name: 命令名称
        """
        pass
    
    def get_command(self, name: str):
        """
        获取命令信息
        
        Args:
            name: 命令名称
            
        Returns:
            命令信息
        """
        pass
    
    def list_commands(self) -> List[str]:
        """
        列出所有命令
        
        Returns:
            命令名称列表
        """
        return []

