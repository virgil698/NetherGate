using NetherGate.API.Audio;
using NetherGate.API.Data.Models;
using NetherGate.API.GameDisplay;
using NetherGate.API.Logging;
using NetherGate.API.Utilities;

namespace NetherGate.Core.Audio;

/// <summary>
/// 音乐播放器实现
/// </summary>
public class MusicPlayer : IMusicPlayer
{
    private readonly IGameDisplayApi _gameDisplayApi;
    private readonly ILogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsPlaying { get; private set; }

    public MusicPlayer(IGameDisplayApi gameDisplayApi, ILogger logger)
    {
        _gameDisplayApi = gameDisplayApi;
        _logger = logger;
    }

    public async Task PlayNoteSequenceAsync(string targets, IEnumerable<MusicNote> notes, Position? pos = null)
    {
        if (IsPlaying)
        {
            _logger.Warning("音乐正在播放中");
            return;
        }

        IsPlaying = true;
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            foreach (var note in notes)
            {
                if (token.IsCancellationRequested)
                {
                    _logger.Debug("音乐播放已取消");
                    break;
                }

                if (!note.IsRest)
                {
                    var soundId = GetInstrumentSound(note.Instrument);
                    var pitch = GetNotePitch(note.Note);
                    
                    await _gameDisplayApi.PlaySoundAsync(
                        soundId,
                        "record",
                        targets,
                        pos?.X,
                        pos?.Y,
                        pos?.Z,
                        note.Volume,
                        pitch
                    );
                }

                if (note.DurationMs > 0)
                {
                    await Task.Delay(note.DurationMs, token);
                }
            }

            _logger.Info("音乐播放完成");
        }
        catch (OperationCanceledException)
        {
            _logger.Info("音乐播放已取消");
        }
        catch (Exception ex)
        {
            _logger.Error($"音乐播放失败: {ex.Message}", ex);
        }
        finally
        {
            IsPlaying = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public async Task PlayChordAsync(string targets, IEnumerable<Note> notes, Instrument instrument = Instrument.Piano, Position? pos = null)
    {
        var soundId = GetInstrumentSound(instrument);
        
        foreach (var note in notes)
        {
            var pitch = GetNotePitch(note);
            _ = _gameDisplayApi.PlaySoundAsync(soundId, "record", targets, pos?.X, pos?.Y, pos?.Z, 1.0, pitch);
        }

        await Task.CompletedTask;
    }

    public async Task PlayPredefinedMelodyAsync(string targets, PredefinedMelody melody, Position? pos = null)
    {
        var notes = GetPredefinedMelodyNotes(melody);
        await PlayNoteSequenceAsync(targets, notes, pos);
    }

    public IMelodyBuilder CreateMelody()
    {
        return new MelodyBuilder(_logger, this);
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        IsPlaying = false;
    }

    private string GetInstrumentSound(Instrument instrument)
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

    private double GetNotePitch(Note note)
    {
        var noteValue = (int)note;
        return Math.Pow(2, (noteValue - 12) / 12.0);
    }

    private List<MusicNote> GetPredefinedMelodyNotes(PredefinedMelody melody)
    {
        return melody switch
        {
            PredefinedMelody.Victory => new List<MusicNote>
            {
                MusicNote.Create(Note.C, Instrument.Piano, 200),
                MusicNote.Create(Note.E, Instrument.Piano, 200),
                MusicNote.Create(Note.G, Instrument.Piano, 200),
                MusicNote.Create(Note.CHigh, Instrument.Piano, 400)
            },
            PredefinedMelody.Defeat => new List<MusicNote>
            {
                MusicNote.Create(Note.CHigh, Instrument.Piano, 200),
                MusicNote.Create(Note.G, Instrument.Piano, 200),
                MusicNote.Create(Note.E, Instrument.Piano, 200),
                MusicNote.Create(Note.C, Instrument.Piano, 400)
            },
            PredefinedMelody.Welcome => new List<MusicNote>
            {
                MusicNote.Create(Note.C, Instrument.Piano, 150),
                MusicNote.Create(Note.D, Instrument.Piano, 150),
                MusicNote.Create(Note.E, Instrument.Piano, 150),
                MusicNote.Create(Note.F, Instrument.Piano, 150),
                MusicNote.Create(Note.G, Instrument.Piano, 300)
            },
            PredefinedMelody.Notification => new List<MusicNote>
            {
                MusicNote.Create(Note.G, Instrument.Bell, 100),
                MusicNote.Create(Note.CHigh, Instrument.Bell, 200)
            },
            PredefinedMelody.Warning => new List<MusicNote>
            {
                MusicNote.Create(Note.E, Instrument.BaseDrum, 150),
                MusicNote.Create(Note.E, Instrument.BaseDrum, 150),
                MusicNote.Create(Note.E, Instrument.BaseDrum, 300)
            },
            PredefinedMelody.Error => new List<MusicNote>
            {
                MusicNote.Create(Note.G, Instrument.Piano, 100),
                MusicNote.Create(Note.F, Instrument.Piano, 100),
                MusicNote.Create(Note.E, Instrument.Piano, 200)
            },
            PredefinedMelody.Success => new List<MusicNote>
            {
                MusicNote.Create(Note.E, Instrument.Chime, 100),
                MusicNote.Create(Note.G, Instrument.Chime, 100),
                MusicNote.Create(Note.CHigh, Instrument.Chime, 200)
            },
            _ => new List<MusicNote>()
        };
    }
}

/// <summary>
/// 旋律构建器实现
/// </summary>
internal class MelodyBuilder : IMelodyBuilder
{
    private readonly List<MusicNote> _notes = new();
    private readonly ILogger _logger;
    private readonly MusicPlayer _musicPlayer;
    private Instrument _defaultInstrument = Instrument.Piano;
    private int _defaultDuration = 500;
    private int _bpm = 120;

    public MelodyBuilder(ILogger logger, MusicPlayer musicPlayer)
    {
        _logger = logger;
        _musicPlayer = musicPlayer;
    }

    public IMelodyBuilder AddNote(Note note, int durationMs = 500, Instrument instrument = Instrument.Piano)
    {
        _notes.Add(MusicNote.Create(note, instrument, durationMs));
        return this;
    }

    public IMelodyBuilder AddRest(int durationMs)
    {
        _notes.Add(MusicNote.Rest(durationMs));
        return this;
    }

    public IMelodyBuilder AddChord(IEnumerable<Note> notes, int durationMs = 500, Instrument instrument = Instrument.Piano)
    {
        foreach (var note in notes)
        {
            _notes.Add(MusicNote.Create(note, instrument, 0)); // 同时播放，延迟为 0
        }
        // 最后一个音符添加延迟
        if (_notes.Count > 0)
        {
            var lastNote = _notes[^1];
            _notes[^1] = lastNote with { DurationMs = durationMs };
        }
        return this;
    }

    public IMelodyBuilder SetDefaultInstrument(Instrument instrument)
    {
        _defaultInstrument = instrument;
        return this;
    }

    public IMelodyBuilder SetDefaultDuration(int durationMs)
    {
        _defaultDuration = durationMs;
        return this;
    }

    public IMelodyBuilder SetTempo(int bpm)
    {
        _bpm = bpm;
        // 计算默认音符时长：60000ms / BPM = 四分音符时长
        _defaultDuration = 60000 / bpm;
        return this;
    }

    public IMelodyBuilder FromString(string notation)
    {
        // 简单的音符解析：C4 D4 E4 F4 G4
        var parts = notation.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            if (part.Equals("R", StringComparison.OrdinalIgnoreCase))
            {
                AddRest(_defaultDuration);
            }
            else if (Enum.TryParse<Note>(part, true, out var note))
            {
                AddNote(note, _defaultDuration, _defaultInstrument);
            }
            else
            {
                _logger.Warning($"无法解析音符: {part}");
            }
        }

        return this;
    }

    public IMelodyBuilder Repeat(int times)
    {
        if (_notes.Count == 0) return this;

        var lastNote = _notes[^1];
        for (int i = 1; i < times; i++)
        {
            _notes.Add(lastNote);
        }
        
        return this;
    }

    public List<MusicNote> Build()
    {
        return _notes.ToList();
    }

    public async Task PlayAsync(string targets, Position? pos = null)
    {
        var notes = Build();
        await _musicPlayer.PlayNoteSequenceAsync(targets, notes, pos);
    }
}

