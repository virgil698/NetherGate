"""
方块数据操作 API

提供方块实体数据的读取和写入功能（箱子、漏斗、告示牌等）
"""

from typing import List, Optional, Dict, Any
from dataclasses import dataclass, field
from .data import ItemStack, Position


@dataclass
class ContainerData:
    """容器数据"""
    position: Position
    type: str  # "chest", "barrel", "hopper", "dispenser", etc.
    items: List[ItemStack] = field(default_factory=list)
    custom_name: Optional[str] = None
    lock: Optional[str] = None


class BlockDataReader:
    """
    方块数据读取器
    
    读取方块实体数据（箱子、漏斗、告示牌等）
    注意：这是一个接口类，实际实现由 C# 桥接提供
    """
    
    async def get_chest_items(self, position: Position) -> List[ItemStack]:
        """
        获取箱子内的物品
        
        Args:
            position: 箱子位置
            
        Returns:
            物品列表
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> items = await reader.get_chest_items(pos)
            >>> for item in items:
            ...     print(f"[{item.slot}] {item.id} x{item.count}")
        """
        pass
    
    async def get_container_data(self, position: Position) -> Optional[ContainerData]:
        """
        获取容器数据（箱子、桶、漏斗等）
        
        Args:
            position: 容器位置
            
        Returns:
            容器数据，如果不是容器则返回 None
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> container = await reader.get_container_data(pos)
            >>> if container:
            ...     print(f"容器类型: {container.type}")
            ...     print(f"物品数量: {len(container.items)}")
        """
        pass
    
    async def get_sign_text(self, position: Position) -> List[str]:
        """
        获取告示牌文本
        
        Args:
            position: 告示牌位置
            
        Returns:
            4行文本列表
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> lines = await reader.get_sign_text(pos)
            >>> for i, line in enumerate(lines, 1):
            ...     print(f"第{i}行: {line}")
        """
        pass
    
    async def get_block_entity(self, position: Position) -> Optional[Dict[str, Any]]:
        """
        获取方块实体的原始 NBT 数据
        
        Args:
            position: 方块位置
            
        Returns:
            NBT 数据字典，如果不是方块实体则返回 None
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> nbt = await reader.get_block_entity(pos)
            >>> if nbt:
            ...     print(f"方块 ID: {nbt.get('id')}")
        """
        pass
    
    async def is_container(self, position: Position) -> bool:
        """
        检查是否是容器方块
        
        Args:
            position: 方块位置
            
        Returns:
            如果是容器返回 True
        """
        pass
    
    async def get_hopper_items(self, position: Position) -> List[ItemStack]:
        """
        获取漏斗内的物品
        
        Args:
            position: 漏斗位置
            
        Returns:
            物品列表
        """
        pass
    
    async def get_barrel_items(self, position: Position) -> List[ItemStack]:
        """
        获取木桶内的物品
        
        Args:
            position: 木桶位置
            
        Returns:
            物品列表
        """
        pass
    
    async def get_shulker_box_items(self, position: Position) -> List[ItemStack]:
        """
        获取潜影盒内的物品
        
        Args:
            position: 潜影盒位置
            
        Returns:
            物品列表
        """
        pass


class BlockDataWriter:
    """
    方块数据写入器
    
    修改方块实体数据
    注意：这是一个接口类，实际实现由 C# 桥接提供
    ⚠️ 需要 RCON 支持
    """
    
    async def set_chest_items(self, position: Position, items: List[ItemStack]):
        """
        设置箱子内的物品
        
        Args:
            position: 箱子位置
            items: 物品列表
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> items = [
            ...     ItemStack(id="minecraft:diamond", count=64, slot=0),
            ...     ItemStack(id="minecraft:gold_ingot", count=32, slot=1)
            ... ]
            >>> await writer.set_chest_items(pos, items)
        """
        pass
    
    async def set_container_items(self, position: Position, items: List[ItemStack]):
        """
        设置容器内的物品（支持各种容器类型）
        
        Args:
            position: 容器位置
            items: 物品列表
            
        Example:
            >>> await writer.set_container_items(pos, items)
        """
        pass
    
    async def set_sign_text(self, position: Position, lines: List[str]):
        """
        设置告示牌文本
        
        Args:
            position: 告示牌位置
            lines: 文本列表（最多4行）
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> lines = ["§6欢迎！", "§e服务器", "", "§7v1.0"]
            >>> await writer.set_sign_text(pos, lines)
        """
        pass
    
    async def update_block_entity(self, position: Position, nbt_data: Dict[str, Any]):
        """
        更新方块实体的 NBT 数据
        
        Args:
            position: 方块位置
            nbt_data: NBT 数据字典
            
        Example:
            >>> pos = Position(100, 64, 200)
            >>> nbt = {"CustomName": '{"text":"宝箱"}'}
            >>> await writer.update_block_entity(pos, nbt)
        """
        pass
    
    async def clear_container(self, position: Position):
        """
        清空容器内的所有物品
        
        Args:
            position: 容器位置
            
        Example:
            >>> await writer.clear_container(Position(100, 64, 200))
        """
        pass
    
    async def sort_container(self, position: Position, by: str = "id"):
        """
        排序容器内的物品
        
        Args:
            position: 容器位置
            by: 排序方式 ("id"/"count"/"slot")
            
        Example:
            >>> await writer.sort_container(pos, by="id")
        """
        pass

