"""
游戏显示 API

提供在游戏中显示 BossBar、Title、ActionBar 等的功能
"""

from typing import Optional


class GameDisplayApi:
    """
    游戏显示 API
    
    提供游戏内显示功能（BossBar、Title、ActionBar、聊天消息等）
    注意：这是一个接口类，实际实现由 C# 桥接提供
    ⚠️ 需要 RCON 支持
    """
    
    # ========== Boss 血条 ==========
    
    async def show_bossbar(
        self,
        id: str,
        title: str,
        progress: float,
        color: str = "white",
        style: str = "progress"
    ):
        """
        显示 Boss 血条
        
        Args:
            id: Boss 血条的唯一标识符
            title: 显示的标题文本
            progress: 进度（0.0 - 1.0）
            color: 颜色 (white/blue/green/yellow/pink/red)
            style: 样式 (progress/notched_6/notched_10/notched_12/notched_20)
            
        Example:
            >>> await display.show_bossbar(
            ...     "welcome",
            ...     "§a欢迎来到服务器！",
            ...     1.0,
            ...     "green",
            ...     "progress"
            ... )
        """
        pass
    
    async def update_bossbar(
        self,
        id: str,
        progress: Optional[float] = None,
        title: Optional[str] = None
    ):
        """
        更新 Boss 血条
        
        Args:
            id: Boss 血条的唯一标识符
            progress: 新的进度（可选）
            title: 新的标题（可选）
            
        Example:
            >>> await display.update_bossbar("download", progress=0.75)
        """
        pass
    
    async def hide_bossbar(self, id: str):
        """
        隐藏 Boss 血条
        
        Args:
            id: Boss 血条的唯一标识符
        """
        pass
    
    # ========== 标题 ==========
    
    async def show_title(
        self,
        selector: str,
        title: str,
        subtitle: str = "",
        fade_in: int = 10,
        stay: int = 70,
        fade_out: int = 20
    ):
        """
        显示标题
        
        Args:
            selector: 目标选择器（如 "@a", "@p", 玩家名）
            title: 主标题文本
            subtitle: 副标题文本
            fade_in: 淡入时间（tick）
            stay: 停留时间（tick）
            fade_out: 淡出时间（tick）
            
        Example:
            >>> await display.show_title(
            ...     "@a",
            ...     "§6欢迎！",
            ...     "§e请遵守规则",
            ...     10, 70, 20
            ... )
        """
        pass
    
    async def show_subtitle(self, selector: str, subtitle: str):
        """
        显示副标题
        
        Args:
            selector: 目标选择器
            subtitle: 副标题文本
        """
        pass
    
    async def clear_title(self, selector: str):
        """
        清除标题
        
        Args:
            selector: 目标选择器
        """
        pass
    
    # ========== 动作栏 ==========
    
    async def show_actionbar(self, selector: str, text: str):
        """
        显示动作栏消息
        
        Args:
            selector: 目标选择器
            text: 消息文本
            
        Example:
            >>> await display.show_actionbar("@a", "§7坐标: X: 100 Y: 64 Z: 200")
        """
        pass
    
    # ========== 聊天消息 ==========
    
    async def send_chat_message(self, selector: str, message: str):
        """
        发送聊天消息给指定玩家
        
        Args:
            selector: 目标选择器
            message: 消息内容
            
        Example:
            >>> await display.send_chat_message("Steve", "§a你好！")
        """
        pass
    
    async def broadcast_message(self, message: str):
        """
        广播消息给所有玩家
        
        Args:
            message: 消息内容
            
        Example:
            >>> await display.broadcast_message("§c服务器将在 5 分钟后重启")
        """
        pass
    
    # ========== 告示牌编辑 ==========
    
    async def open_sign_editor(self, player_name: str, x: int, y: int, z: int):
        """
        为玩家打开告示牌编辑界面
        
        Args:
            player_name: 玩家名称
            x: 告示牌 X 坐标
            y: 告示牌 Y 坐标
            z: 告示牌 Z 坐标
            
        Example:
            >>> await display.open_sign_editor("Steve", 100, 64, 200)
        """
        pass

