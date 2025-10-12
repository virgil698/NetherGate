"""
音乐播放器 API

使用 Minecraft 音符盒音效播放音乐
"""

from typing import Optional, Union
from enum import Enum


class Note(Enum):
    """音符"""
    C = "C"
    D = "D"
    E = "E"
    F = "F"
    G = "G"
    A = "A"
    B = "B"


class Instrument(Enum):
    """乐器"""
    HARP = "harp"
    BASS = "bass"
    BASEDRUM = "basedrum"
    SNARE = "snare"
    HAT = "hat"
    GUITAR = "guitar"
    FLUTE = "flute"
    BELL = "bell"
    CHIME = "chime"
    XYLOPHONE = "xylophone"
    IRON_XYLOPHONE = "iron_xylophone"
    COW_BELL = "cow_bell"
    DIDGERIDOO = "didgeridoo"
    BIT = "bit"
    BANJO = "banjo"
    PLING = "pling"


class Melody:
    """
    旋律构建器
    
    用于创建音符序列
    """
    
    def add_note(
        self,
        note: Union[str, Note],
        duration_ms: int,
        octave: int = 1,
        sharp: bool = False
    ) -> 'Melody':
        """
        添加音符
        
        Args:
            note: 音符（C/D/E/F/G/A/B 或 Note 枚举）
            duration_ms: 持续时间（毫秒）
            octave: 八度（0-2）
            sharp: 是否升半音（#）
            
        Returns:
            自身，用于链式调用
            
        Example:
            >>> melody.add_note("C", 200).add_note("E", 200).add_note("G", 400)
            >>> melody.add_note(Note.C, 200, octave=1, sharp=False)
        """
        pass
    
    def add_rest(self, duration_ms: int) -> 'Melody':
        """
        添加休止符
        
        Args:
            duration_ms: 持续时间（毫秒）
            
        Returns:
            自身，用于链式调用
        """
        pass
    
    def set_instrument(self, instrument: Union[str, Instrument]) -> 'Melody':
        """
        设置乐器
        
        Args:
            instrument: 乐器类型
            
        Returns:
            自身，用于链式调用
        """
        pass
    
    def set_volume(self, volume: float) -> 'Melody':
        """
        设置音量
        
        Args:
            volume: 音量（0.0 - 1.0）
            
        Returns:
            自身，用于链式调用
        """
        pass
    
    async def play(self, selector: str):
        """
        播放旋律
        
        Args:
            selector: 目标选择器
            
        Example:
            >>> await melody.play("@a")
        """
        pass
    
    async def loop(self, selector: str, times: int):
        """
        循环播放旋律
        
        Args:
            selector: 目标选择器
            times: 循环次数
            
        Example:
            >>> await melody.loop("@a", 3)
        """
        pass


class MusicPlayer:
    """
    音乐播放器
    
    使用 Minecraft 音符盒音效播放音乐
    注意：这是一个接口类，实际实现由 C# 桥接提供
    ⚠️ 需要 RCON 支持
    """
    
    def create_melody(self) -> Melody:
        """
        创建新的旋律
        
        Returns:
            旋律构建器
            
        Example:
            >>> melody = player.create_melody()
            >>> melody.add_note("C", 200).add_note("E", 200).add_note("G", 400)
            >>> await melody.play("@a")
        """
        pass
    
    async def stop_all(self, selector: str):
        """
        停止所有音乐
        
        Args:
            selector: 目标选择器
            
        Example:
            >>> await player.stop_all("@a")
        """
        pass
    
    # ========== 预设旋律 ==========
    
    async def play_c_major_scale(self, selector: str):
        """
        播放 C 大调音阶
        
        Args:
            selector: 目标选择器
        """
        melody = self.create_melody()
        notes = [Note.C, Note.D, Note.E, Note.F, Note.G, Note.A, Note.B]
        for note in notes:
            melody.add_note(note, 200)
        await melody.play(selector)
    
    async def play_twinkle_star(self, selector: str):
        """
        播放《小星星》
        
        Args:
            selector: 目标选择器
        """
        melody = self.create_melody()
        # C C G G A A G
        melody.add_note(Note.C, 400).add_note(Note.C, 400)
        melody.add_note(Note.G, 400).add_note(Note.G, 400)
        melody.add_note(Note.A, 400).add_note(Note.A, 400)
        melody.add_note(Note.G, 800)
        # F F E E D D C
        melody.add_note(Note.F, 400).add_note(Note.F, 400)
        melody.add_note(Note.E, 400).add_note(Note.E, 400)
        melody.add_note(Note.D, 400).add_note(Note.D, 400)
        melody.add_note(Note.C, 800)
        await melody.play(selector)

