using NetherGate.API.Data.Models;
using NetherGate.API.Utilities;

namespace NetherGate.API.Audio;

/// <summary>
/// 音乐播放器接口
/// 使用 Minecraft 音符盒音效播放音乐
/// </summary>
public interface IMusicPlayer
{
    /// <summary>
    /// 播放音符序列
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="notes">音符序列</param>
    /// <param name="pos">播放位置（可选）</param>
    Task PlayNoteSequenceAsync(string targets, IEnumerable<MusicNote> notes, Position? pos = null);
    
    /// <summary>
    /// 播放和弦
    /// </summary>
    /// <param name="targets">目标玩家</param>
    /// <param name="notes">同时播放的音符</param>
    /// <param name="instrument">乐器</param>
    /// <param name="pos">播放位置（可选）</param>
    Task PlayChordAsync(string targets, IEnumerable<Note> notes, Instrument instrument = Instrument.Piano, Position? pos = null);
    
    /// <summary>
    /// 播放预设旋律
    /// </summary>
    Task PlayPredefinedMelodyAsync(string targets, PredefinedMelody melody, Position? pos = null);
    
    /// <summary>
    /// 创建旋律构建器
    /// </summary>
    IMelodyBuilder CreateMelody();
    
    /// <summary>
    /// 停止播放（实际上 Minecraft 无法停止已播放的声音序列，但可以取消待播放的序列）
    /// </summary>
    void Stop();
    
    /// <summary>
    /// 是否正在播放
    /// </summary>
    bool IsPlaying { get; }
}

/// <summary>
/// 旋律构建器接口
/// </summary>
public interface IMelodyBuilder
{
    /// <summary>
    /// 添加音符
    /// </summary>
    IMelodyBuilder AddNote(Note note, int durationMs = 500, Instrument instrument = Instrument.Piano);
    
    /// <summary>
    /// 添加休止符
    /// </summary>
    IMelodyBuilder AddRest(int durationMs);
    
    /// <summary>
    /// 添加和弦
    /// </summary>
    IMelodyBuilder AddChord(IEnumerable<Note> notes, int durationMs = 500, Instrument instrument = Instrument.Piano);
    
    /// <summary>
    /// 设置默认乐器
    /// </summary>
    IMelodyBuilder SetDefaultInstrument(Instrument instrument);
    
    /// <summary>
    /// 设置默认音符时长
    /// </summary>
    IMelodyBuilder SetDefaultDuration(int durationMs);
    
    /// <summary>
    /// 设置节拍（BPM）
    /// </summary>
    IMelodyBuilder SetTempo(int bpm);
    
    /// <summary>
    /// 添加从字符串解析的音符（如 "C4 D4 E4 F4 G4"）
    /// </summary>
    IMelodyBuilder FromString(string notation);
    
    /// <summary>
    /// 重复前面的段落
    /// </summary>
    IMelodyBuilder Repeat(int times);
    
    /// <summary>
    /// 构建音符序列
    /// </summary>
    List<MusicNote> Build();
    
    /// <summary>
    /// 播放构建的旋律
    /// </summary>
    Task PlayAsync(string targets, Position? pos = null);
}

/// <summary>
/// 音符数据
/// </summary>
public record MusicNote
{
    /// <summary>
    /// 音符
    /// </summary>
    public Note Note { get; init; }
    
    /// <summary>
    /// 乐器
    /// </summary>
    public Instrument Instrument { get; init; } = Instrument.Piano;
    
    /// <summary>
    /// 持续时间（毫秒）
    /// </summary>
    public int DurationMs { get; init; } = 500;
    
    /// <summary>
    /// 是否为休止符
    /// </summary>
    public bool IsRest { get; init; }
    
    /// <summary>
    /// 音量
    /// </summary>
    public double Volume { get; init; } = 1.0;
    
    /// <summary>
    /// 创建音符
    /// </summary>
    public static MusicNote Create(Note note, Instrument instrument = Instrument.Piano, int durationMs = 500)
    {
        return new MusicNote
        {
            Note = note,
            Instrument = instrument,
            DurationMs = durationMs,
            IsRest = false
        };
    }
    
    /// <summary>
    /// 创建休止符
    /// </summary>
    public static MusicNote Rest(int durationMs)
    {
        return new MusicNote
        {
            DurationMs = durationMs,
            IsRest = true
        };
    }
    
    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    public override string ToString() => 
        IsRest ? $"Rest({DurationMs}ms)" : $"{Note} ({Instrument}, {DurationMs}ms)";
}

