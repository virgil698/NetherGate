"""
插件基类和元数据
"""

from typing import List, Optional
from abc import ABC, abstractmethod


class PluginInfo:
    """插件元数据"""
    
    def __init__(
        self,
        id: str,
        name: str,
        version: str,
        description: str = "",
        author: str = "",
        website: Optional[str] = None,
        dependencies: Optional[List[str]] = None,
        soft_dependencies: Optional[List[str]] = None,
        load_order: int = 100
    ):
        self.id = id
        self.name = name
        self.version = version
        self.description = description
        self.author = author
        self.website = website
        self.dependencies = dependencies or []
        self.soft_dependencies = soft_dependencies or []
        self.load_order = load_order
    
    def __str__(self) -> str:
        return f"{self.name} v{self.version} by {self.author}"


class Plugin(ABC):
    """
    插件基类
    
    所有 NetherGate Python 插件必须继承此类并实现生命周期方法
    """
    
    def __init__(self):
        self.info: PluginInfo = PluginInfo(
            id="unknown",
            name="Unknown Plugin",
            version="1.0.0"
        )
    
    @abstractmethod
    async def on_load(self):
        """
        插件加载时调用
        
        此时插件被加载到内存，但尚未启用
        用于初始化插件的基本配置和资源
        """
        pass
    
    @abstractmethod
    async def on_enable(self):
        """
        插件启用时调用
        
        此时插件开始正式工作，可以注册事件、命令等
        """
        pass
    
    @abstractmethod
    async def on_disable(self):
        """
        插件禁用时调用
        
        此时插件停止工作，应该注销所有事件、命令等
        """
        pass
    
    @abstractmethod
    async def on_unload(self):
        """
        插件卸载时调用
        
        此时插件将从内存中移除，应该释放所有资源
        """
        pass

