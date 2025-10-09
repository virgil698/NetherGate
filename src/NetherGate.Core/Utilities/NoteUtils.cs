using NetherGate.API.Audio;
using NetherGate.API.Utilities;

namespace NetherGate.Core.Utilities;

/// <summary>
/// 音符工具类
/// 提供音符和乐器的转换功能
/// </summary>
public static class NoteUtils
{
    /// <summary>
    /// 获取乐器对应的 Minecraft 声音 ID
    /// </summary>
    /// <param name="instrument">乐器类型</param>
    /// <returns>Minecraft 声音 ID</returns>
    public static string GetInstrumentSound(Instrument instrument)
    {
        return instrument switch
        {
            Instrument.Piano => "minecraft:block.note_block.harp",
            Instrument.Bass => "minecraft:block.note_block.bass",
            Instrument.BaseDrum => "minecraft:block.note_block.basedrum",
            Instrument.Snare => "minecraft:block.note_block.snare",
            Instrument.Hat => "minecraft:block.note_block.hat",
            Instrument.Guitar => "minecraft:block.note_block.guitar",
            Instrument.Flute => "minecraft:block.note_block.flute",
            Instrument.Bell => "minecraft:block.note_block.bell",
            Instrument.Chime => "minecraft:block.note_block.chime",
            Instrument.Xylophone => "minecraft:block.note_block.xylophone",
            Instrument.IronXylophone => "minecraft:block.note_block.iron_xylophone",
            Instrument.CowBell => "minecraft:block.note_block.cow_bell",
            Instrument.Didgeridoo => "minecraft:block.note_block.didgeridoo",
            Instrument.Bit => "minecraft:block.note_block.bit",
            Instrument.Banjo => "minecraft:block.note_block.banjo",
            Instrument.Pling => "minecraft:block.note_block.pling",
            _ => "minecraft:block.note_block.harp"
        };
    }

    /// <summary>
    /// 获取音符对应的音调值
    /// Minecraft 音符盒音调计算公式：pitch = 2^((note-12)/12)
    /// </summary>
    /// <param name="note">音符</param>
    /// <returns>音调值</returns>
    public static double GetNotePitch(Note note)
    {
        var noteValue = (int)note;
        return Math.Pow(2, (noteValue - 12) / 12.0);
    }

    /// <summary>
    /// 获取预定义旋律
    /// </summary>
    /// <param name="melody">旋律类型</param>
    /// <returns>音符和延迟的列表</returns>
    public static List<(Note note, int delayMs)> GetPredefinedMelody(PredefinedMelody melody)
    {
        return melody switch
        {
            PredefinedMelody.Victory => new List<(Note, int)>
            {
                (Note.C, 200), (Note.E, 200), (Note.G, 200), (Note.CHigh, 400)
            },
            PredefinedMelody.Defeat => new List<(Note, int)>
            {
                (Note.CHigh, 200), (Note.G, 200), (Note.E, 200), (Note.C, 400)
            },
            PredefinedMelody.Welcome => new List<(Note, int)>
            {
                (Note.C, 150), (Note.D, 150), (Note.E, 150), (Note.F, 150), (Note.G, 300)
            },
            PredefinedMelody.Notification => new List<(Note, int)>
            {
                (Note.G, 100), (Note.CHigh, 200)
            },
            PredefinedMelody.Warning => new List<(Note, int)>
            {
                (Note.E, 150), (Note.E, 150), (Note.E, 300)
            },
            PredefinedMelody.Error => new List<(Note, int)>
            {
                (Note.G, 100), (Note.F, 100), (Note.E, 200)
            },
            PredefinedMelody.Success => new List<(Note, int)>
            {
                (Note.E, 100), (Note.G, 100), (Note.CHigh, 200)
            },
            _ => new List<(Note, int)>()
        };
    }
}

