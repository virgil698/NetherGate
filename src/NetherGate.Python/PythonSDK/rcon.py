"""
RCON 客户端

提供 Minecraft 服务器命令执行功能
"""

from typing import List, Optional, Dict, Any
from dataclasses import dataclass


@dataclass
class RconResponse:
    """RCON 响应"""
    success: bool = False
    response: str = ""
    error: str = ""
    request_id: int = 0
    
    def __bool__(self) -> bool:
        """允许直接在布尔上下文中使用"""
        return self.success


class RconClient:
    """
    RCON 客户端
    
    提供与 Minecraft 服务器的 RCON 连接功能
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    async def execute(self, command: str) -> RconResponse:
        """
        执行单条命令
        
        Args:
            command: Minecraft 命令（不需要前导斜杠）
            
        Returns:
            命令执行结果
            
        Example:
            >>> response = await rcon.execute("say Hello, world!")
            >>> if response.success:
            ...     print(f"命令执行成功: {response.response}")
        """
        pass
    
    async def execute_batch(self, commands: List[str]) -> List[RconResponse]:
        """
        批量执行命令
        
        Args:
            commands: 命令列表
            
        Returns:
            执行结果列表
            
        Example:
            >>> commands = ["say Line 1", "say Line 2", "say Line 3"]
            >>> responses = await rcon.execute_batch(commands)
            >>> for resp in responses:
            ...     print(f"成功: {resp.success}, 响应: {resp.response}")
        """
        pass
    
    def is_connected(self) -> bool:
        """
        检查连接状态
        
        Returns:
            是否已连接到服务器
        """
        pass
    
    async def connect(self, host: str = "localhost", port: int = 25575, password: str = ""):
        """
        连接到 RCON 服务器
        
        Args:
            host: 服务器地址
            port: RCON 端口
            password: RCON 密码
        """
        pass
    
    async def disconnect(self):
        """断开 RCON 连接"""
        pass
    
    # ========== 便捷方法 ==========
    
    async def say(self, message: str) -> RconResponse:
        """
        发送聊天消息
        
        Args:
            message: 消息内容
            
        Returns:
            执行结果
        """
        return await self.execute(f"say {message}")
    
    async def tell(self, player: str, message: str) -> RconResponse:
        """
        向玩家发送私聊消息
        
        Args:
            player: 玩家名称
            message: 消息内容
            
        Returns:
            执行结果
        """
        return await self.execute(f"tell {player} {message}")
    
    async def kick(self, player: str, reason: str = "") -> RconResponse:
        """
        踢出玩家
        
        Args:
            player: 玩家名称
            reason: 踢出原因
            
        Returns:
            执行结果
        """
        cmd = f"kick {player}"
        if reason:
            cmd += f" {reason}"
        return await self.execute(cmd)
    
    async def ban(self, player: str, reason: str = "") -> RconResponse:
        """
        封禁玩家
        
        Args:
            player: 玩家名称
            reason: 封禁原因
            
        Returns:
            执行结果
        """
        cmd = f"ban {player}"
        if reason:
            cmd += f" {reason}"
        return await self.execute(cmd)
    
    async def pardon(self, player: str) -> RconResponse:
        """
        解封玩家
        
        Args:
            player: 玩家名称
            
        Returns:
            执行结果
        """
        return await self.execute(f"pardon {player}")
    
    async def op(self, player: str) -> RconResponse:
        """
        给予玩家管理员权限
        
        Args:
            player: 玩家名称
            
        Returns:
            执行结果
        """
        return await self.execute(f"op {player}")
    
    async def deop(self, player: str) -> RconResponse:
        """
        移除玩家管理员权限
        
        Args:
            player: 玩家名称
            
        Returns:
            执行结果
        """
        return await self.execute(f"deop {player}")
    
    async def whitelist_add(self, player: str) -> RconResponse:
        """
        添加玩家到白名单
        
        Args:
            player: 玩家名称
            
        Returns:
            执行结果
        """
        return await self.execute(f"whitelist add {player}")
    
    async def whitelist_remove(self, player: str) -> RconResponse:
        """
        从白名单移除玩家
        
        Args:
            player: 玩家名称
            
        Returns:
            执行结果
        """
        return await self.execute(f"whitelist remove {player}")
    
    async def give(self, player: str, item: str, count: int = 1) -> RconResponse:
        """
        给予玩家物品
        
        Args:
            player: 玩家名称
            item: 物品 ID（如 "minecraft:diamond"）
            count: 数量
            
        Returns:
            执行结果
        """
        return await self.execute(f"give {player} {item} {count}")
    
    async def teleport(self, player: str, x: float, y: float, z: float) -> RconResponse:
        """
        传送玩家
        
        Args:
            player: 玩家名称
            x: X 坐标
            y: Y 坐标
            z: Z 坐标
            
        Returns:
            执行结果
        """
        return await self.execute(f"tp {player} {x} {y} {z}")
    
    async def gamemode(self, player: str, mode: str) -> RconResponse:
        """
        设置玩家游戏模式
        
        Args:
            player: 玩家名称
            mode: 游戏模式 (survival/creative/adventure/spectator)
            
        Returns:
            执行结果
        """
        return await self.execute(f"gamemode {mode} {player}")
    
    async def time_set(self, time: str) -> RconResponse:
        """
        设置世界时间
        
        Args:
            time: 时间值 (day/night/noon/midnight 或 tick 数)
            
        Returns:
            执行结果
        """
        return await self.execute(f"time set {time}")
    
    async def weather(self, weather: str) -> RconResponse:
        """
        设置天气
        
        Args:
            weather: 天气 (clear/rain/thunder)
            
        Returns:
            执行结果
        """
        return await self.execute(f"weather {weather}")
    
    async def difficulty(self, level: str) -> RconResponse:
        """
        设置难度
        
        Args:
            level: 难度级别 (peaceful/easy/normal/hard)
            
        Returns:
            执行结果
        """
        return await self.execute(f"difficulty {level}")
    
    async def gamerule(self, rule: str, value: Any) -> RconResponse:
        """
        设置游戏规则
        
        Args:
            rule: 规则名称
            value: 规则值
            
        Returns:
            执行结果
        """
        return await self.execute(f"gamerule {rule} {value}")
    
    async def save_all(self) -> RconResponse:
        """
        保存世界数据
        
        Returns:
            执行结果
        """
        return await self.execute("save-all")
    
    async def stop(self) -> RconResponse:
        """
        停止服务器
        
        Returns:
            执行结果
        """
        return await self.execute("stop")

