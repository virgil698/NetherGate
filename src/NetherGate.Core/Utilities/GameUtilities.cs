using NetherGate.API.Data.Models;
using NetherGate.API.Extensions;
using NetherGate.API.GameDisplay;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.API.Utilities;

namespace NetherGate.Core.Utilities;

/// <summary>
/// 游戏实用工具实现
/// </summary>
public class GameUtilities : IGameUtilities
{
    private readonly IRconClient _rconClient;
    private readonly IGameDisplayApi _gameDisplayApi;
    private readonly ILogger _logger;
    private readonly Random _random = new();

    public GameUtilities(IRconClient rconClient, IGameDisplayApi gameDisplayApi, ILogger logger)
    {
        _rconClient = rconClient;
        _gameDisplayApi = gameDisplayApi;
        _logger = logger;
    }

    // ========== 时间便捷操作 ==========

    public async Task SetDayAsync()
    {
        await _gameDisplayApi.SetTimeAsync("day");
    }

    public async Task SetNightAsync()
    {
        await _gameDisplayApi.SetTimeAsync("night");
    }

    public async Task SetNoonAsync()
    {
        await _gameDisplayApi.SetTimeAsync("noon");
    }

    public async Task SetMidnightAsync()
    {
        await _gameDisplayApi.SetTimeAsync("midnight");
    }

    public async Task SetSunriseAsync()
    {
        await _rconClient.ExecuteCommandAsync("time set 23000");
    }

    public async Task SetSunsetAsync()
    {
        await _rconClient.ExecuteCommandAsync("time set 12000");
    }

    // ========== 天气便捷操作 ==========

    public async Task SetClearWeatherAsync(int? durationSeconds = null)
    {
        await _gameDisplayApi.SetWeatherAsync("clear", durationSeconds);
    }

    public async Task SetRainAsync(int? durationSeconds = null)
    {
        await _gameDisplayApi.SetWeatherAsync("rain", durationSeconds);
    }

    public async Task SetThunderAsync(int? durationSeconds = null)
    {
        await _gameDisplayApi.SetWeatherAsync("thunder", durationSeconds);
    }

    // ========== 烟花操作 ==========

    public async Task LaunchFireworkAsync(
        Position pos,
        FireworkType type = FireworkType.LargeBall,
        List<FireworkColor>? colors = null,
        List<FireworkColor>? fadeColors = null,
        int flightDuration = 2,
        bool hasTrail = false,
        bool hasTwinkle = false)
    {
        var builder = new FireworkBuilder();
        builder.WithType(type)
               .WithFlightDuration(flightDuration)
               .WithTrail(hasTrail)
               .WithTwinkle(hasTwinkle);

        if (colors != null && colors.Count > 0)
        {
            builder.WithColors(colors.ToArray());
        }
        else
        {
            builder.WithRandomColors(3);
        }

        if (fadeColors != null && fadeColors.Count > 0)
        {
            builder.WithFadeColors(fadeColors.ToArray());
        }

        var nbt = builder.Build();
        var posStr = pos.ToPreciseCommandString();
        
        await _rconClient.ExecuteCommandAsync($"summon minecraft:firework_rocket {posStr} {nbt}");
        _logger.Debug($"发射烟花: {pos}");
    }

    public async Task LaunchRandomFireworkAsync(Position pos, int flightDuration = 2)
    {
        var types = Enum.GetValues<FireworkType>();
        var type = types[_random.Next(types.Length)];
        
        await LaunchFireworkAsync(pos, type, null, null, flightDuration, _random.Next(2) == 0, _random.Next(2) == 0);
    }

    public async Task LaunchFireworkShowAsync(Position centerPos, int count = 10, double radius = 5.0, int delayMs = 200)
    {
        for (int i = 0; i < count; i++)
        {
            var randomPos = GetRandomPosition(
                centerPos.Offset(-radius, -2, -radius),
                centerPos.Offset(radius, 2, radius)
            );

            await LaunchRandomFireworkAsync(randomPos);
            
            if (i < count - 1)
            {
                await Task.Delay(delayMs);
            }
        }
        
        _logger.Info($"烟花秀完成: {count} 个烟花");
    }

    public async Task LaunchCustomFireworkAsync(Position pos, Action<IFireworkBuilder> configure)
    {
        var builder = new FireworkBuilder();
        configure(builder);
        
        var nbt = builder.Build();
        var posStr = pos.ToPreciseCommandString();
        
        await _rconClient.ExecuteCommandAsync($"summon minecraft:firework_rocket {posStr} {nbt}");
    }

    // ========== 声音便捷操作 ==========

    public async Task PlaySoundAsync(string targets, MinecraftSound sound, Position? pos = null, double volume = 1.0, double pitch = 1.0)
    {
        var soundId = GetSoundId(sound);
        await _gameDisplayApi.PlaySoundAsync(soundId, "master", targets, pos?.X, pos?.Y, pos?.Z, volume, pitch);
    }

    public async Task PlayNoteAsync(string targets, Note note, Instrument instrument = Instrument.Piano, Position? pos = null)
    {
        var soundId = GetInstrumentSound(instrument);
        var pitch = GetNotePitch(note);
        await _gameDisplayApi.PlaySoundAsync(soundId, "record", targets, pos?.X, pos?.Y, pos?.Z, 1.0, pitch);
    }

    public async Task PlayMelodyAsync(string targets, IEnumerable<(Note note, int delayMs)> melody, Instrument instrument = Instrument.Piano, Position? pos = null)
    {
        foreach (var (note, delayMs) in melody)
        {
            await PlayNoteAsync(targets, note, instrument, pos);
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }
    }

    public async Task PlayPredefinedMelodyAsync(string targets, PredefinedMelody melody, Position? pos = null)
    {
        var notes = GetPredefinedMelody(melody);
        await PlayMelodyAsync(targets, notes, Instrument.Piano, pos);
    }

    // ========== 区域操作 ==========

    public async Task ClearAreaAsync(Position from, Position to)
    {
        await _gameDisplayApi.FillBlocksAsync(
            (int)from.X, (int)from.Y, (int)from.Z,
            (int)to.X, (int)to.Y, (int)to.Z,
            "minecraft:air", "replace"
        );
    }

    public async Task FillAreaAsync(Position from, Position to, string block)
    {
        await _gameDisplayApi.FillBlocksAsync(
            (int)from.X, (int)from.Y, (int)from.Z,
            (int)to.X, (int)to.Y, (int)to.Z,
            block, "replace"
        );
    }

    public async Task CloneAreaAsync(Position from, Position to, Position destination, CloneMode mode = CloneMode.Replace)
    {
        var modeStr = mode.ToString().ToLower();
        await _rconClient.ExecuteCommandAsync(
            $"clone {from.ToCommandString()} {to.ToCommandString()} {destination.ToCommandString()} {modeStr}"
        );
    }

    public async Task CreateHollowCubeAsync(Position from, Position to, string block)
    {
        await _gameDisplayApi.FillBlocksAsync(
            (int)from.X, (int)from.Y, (int)from.Z,
            (int)to.X, (int)to.Y, (int)to.Z,
            block, "hollow"
        );
    }

    public async Task CreateSphereAsync(Position center, double radius, string block, bool hollow = false)
    {
        var positions = new List<Position>();
        var r = (int)Math.Ceiling(radius);

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int z = -r; z <= r; z++)
                {
                    var distance = Math.Sqrt(x * x + y * y + z * z);
                    
                    if (hollow)
                    {
                        if (Math.Abs(distance - radius) < 0.5)
                        {
                            positions.Add(center.Offset(x, y, z));
                        }
                    }
                    else
                    {
                        if (distance <= radius)
                        {
                            positions.Add(center.Offset(x, y, z));
                        }
                    }
                }
            }
        }

        foreach (var pos in positions)
        {
            await _gameDisplayApi.SetBlockAsync((int)pos.X, (int)pos.Y, (int)pos.Z, block);
        }

        _logger.Info($"创建球体: 半径 {radius}, 方块数 {positions.Count}");
    }

    public async Task CreateCylinderAsync(Position base1, Position base2, double radius, string block, bool hollow = false)
    {
        var height = Math.Abs(base2.Y - base1.Y) + 1;
        var minY = Math.Min(base1.Y, base2.Y);
        var centerX = (base1.X + base2.X) / 2;
        var centerZ = (base1.Z + base2.Z) / 2;

        var positions = new List<Position>();
        var r = (int)Math.Ceiling(radius);

        for (int y = 0; y < height; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int z = -r; z <= r; z++)
                {
                    var distance = Math.Sqrt(x * x + z * z);
                    
                    if (hollow)
                    {
                        if (Math.Abs(distance - radius) < 0.5)
                        {
                            positions.Add(new Position(centerX + x, minY + y, centerZ + z));
                        }
                    }
                    else
                    {
                        if (distance <= radius)
                        {
                            positions.Add(new Position(centerX + x, minY + y, centerZ + z));
                        }
                    }
                }
            }
        }

        foreach (var pos in positions)
        {
            await _gameDisplayApi.SetBlockAsync((int)pos.X, (int)pos.Y, (int)pos.Z, block);
        }

        _logger.Info($"创建圆柱体: 半径 {radius}, 高度 {height}, 方块数 {positions.Count}");
    }

    // ========== 粒子效果 ==========

    public async Task CreateParticleLineAsync(Position from, Position to, string particle, int density = 10)
    {
        var path = from.GetPath(to, density);
        foreach (var pos in path)
        {
            await _gameDisplayApi.SpawnParticleAsync(particle, pos.X, pos.Y, pos.Z);
            await Task.Delay(50);
        }
    }

    public async Task CreateParticleCircleAsync(Position center, double radius, string particle, int points = 20)
    {
        for (int i = 0; i < points; i++)
        {
            var angle = (2 * Math.PI * i) / points;
            var x = center.X + radius * Math.Cos(angle);
            var z = center.Z + radius * Math.Sin(angle);
            
            await _gameDisplayApi.SpawnParticleAsync(particle, x, center.Y, z);
        }
    }

    public async Task CreateParticleSphereAsync(Position center, double radius, string particle, int density = 50)
    {
        for (int i = 0; i < density; i++)
        {
            var theta = _random.NextDouble() * Math.PI * 2;
            var phi = Math.Acos(2 * _random.NextDouble() - 1);
            
            var x = center.X + radius * Math.Sin(phi) * Math.Cos(theta);
            var y = center.Y + radius * Math.Sin(phi) * Math.Sin(theta);
            var z = center.Z + radius * Math.Cos(phi);
            
            await _gameDisplayApi.SpawnParticleAsync(particle, x, y, z);
        }
    }

    public async Task CreateParticleSpiralAsync(Position start, double height, double radius, string particle, int turns = 3, int pointsPerTurn = 20)
    {
        var totalPoints = turns * pointsPerTurn;
        
        for (int i = 0; i < totalPoints; i++)
        {
            var progress = i / (double)totalPoints;
            var angle = progress * turns * 2 * Math.PI;
            
            var x = start.X + radius * Math.Cos(angle);
            var y = start.Y + height * progress;
            var z = start.Z + radius * Math.Sin(angle);
            
            await _gameDisplayApi.SpawnParticleAsync(particle, x, y, z);
            await Task.Delay(50);
        }
    }

    // ========== 传送便捷操作 ==========

    public async Task TeleportToSpawnAsync(string targets)
    {
        await _rconClient.ExecuteCommandAsync($"tp {targets} @s");
        await _rconClient.ExecuteCommandAsync($"spawnpoint {targets}");
    }

    public async Task TeleportToBedAsync(string targets)
    {
        // Minecraft 没有直接传送到床的命令，使用重生点
        await _rconClient.ExecuteCommandAsync($"kill {targets}");
        _logger.Info("玩家将在床或出生点重生");
    }

    public async Task RandomTeleportAsync(string targets, Position center, double radius)
    {
        var randomPos = GetRandomPosition(
            center.Offset(-radius, -10, -radius),
            center.Offset(radius, 10, radius)
        );

        await _gameDisplayApi.TeleportAsync(targets, randomPos.X, randomPos.Y, randomPos.Z);
    }

    public async Task GatherPlayersAsync(Position location)
    {
        await _gameDisplayApi.TeleportAsync("@a", location.X, location.Y, location.Z);
    }

    // ========== 批量玩家操作 ==========

    public async Task HealAllPlayersAsync()
    {
        await _rconClient.ExecuteCommandAsync("effect give @a minecraft:instant_health 1 10 true");
    }

    public async Task FeedAllPlayersAsync()
    {
        await _rconClient.ExecuteCommandAsync("effect give @a minecraft:saturation 1 10 true");
    }

    public async Task ClearAllEffectsAsync(string targets = "@a")
    {
        await _gameDisplayApi.ClearEffectAsync(targets);
    }

    public async Task GiveFullArmorAsync(string targets, ArmorMaterial material = ArmorMaterial.Diamond)
    {
        var materialName = material.ToString().ToLower();
        await _gameDisplayApi.GiveItemAsync(targets, $"minecraft:{materialName}_helmet", 1);
        await _gameDisplayApi.GiveItemAsync(targets, $"minecraft:{materialName}_chestplate", 1);
        await _gameDisplayApi.GiveItemAsync(targets, $"minecraft:{materialName}_leggings", 1);
        await _gameDisplayApi.GiveItemAsync(targets, $"minecraft:{materialName}_boots", 1);
    }

    // ========== 实用功能 ==========

    public ICommandSequence CreateSequence()
    {
        return new CommandSequence(_logger);
    }

    public async Task DelayAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }

    public async Task DelayTicksAsync(int ticks)
    {
        await Task.Delay(ticks * 50); // 1 tick = 50ms
    }

    public Position GetRandomPosition(Position min, Position max)
    {
        return new Position(
            min.X + _random.NextDouble() * (max.X - min.X),
            min.Y + _random.NextDouble() * (max.Y - min.Y),
            min.Z + _random.NextDouble() * (max.Z - min.Z)
        );
    }

    public FireworkColor GetRandomColor()
    {
        var colors = Enum.GetValues<FireworkColor>();
        return colors[_random.Next(colors.Length)];
    }

    // ========== 内部辅助方法 ==========

    private string GetSoundId(MinecraftSound sound)
    {
        return sound switch
        {
            MinecraftSound.PlayerLevelUp => "minecraft:entity.player.levelup",
            MinecraftSound.ExperienceOrb => "minecraft:entity.experience_orb.pickup",
            MinecraftSound.AnvilPlace => "minecraft:block.anvil.place",
            MinecraftSound.AnvilUse => "minecraft:block.anvil.use",
            MinecraftSound.AnvilBreak => "minecraft:block.anvil.break",
            MinecraftSound.ChestOpen => "minecraft:block.chest.open",
            MinecraftSound.ChestClose => "minecraft:block.chest.close",
            MinecraftSound.DoorOpen => "minecraft:block.wooden_door.open",
            MinecraftSound.DoorClose => "minecraft:block.wooden_door.close",
            MinecraftSound.ButtonClick => "minecraft:block.stone_button.click_on",
            MinecraftSound.LeverClick => "minecraft:block.lever.click",
            MinecraftSound.EnderDragonDeath => "minecraft:entity.ender_dragon.death",
            MinecraftSound.WitherSpawn => "minecraft:entity.wither.spawn",
            MinecraftSound.WitherDeath => "minecraft:entity.wither.death",
            MinecraftSound.VillagerYes => "minecraft:entity.villager.yes",
            MinecraftSound.VillagerNo => "minecraft:entity.villager.no",
            MinecraftSound.ZombieAmbient => "minecraft:entity.zombie.ambient",
            MinecraftSound.SkeletonShoot => "minecraft:entity.skeleton.shoot",
            MinecraftSound.CreeperPrimed => "minecraft:entity.creeper.primed",
            MinecraftSound.EndermanTeleport => "minecraft:entity.enderman.teleport",
            MinecraftSound.FireworkLaunch => "minecraft:entity.firework_rocket.launch",
            MinecraftSound.FireworkBlast => "minecraft:entity.firework_rocket.blast",
            MinecraftSound.EnchantmentTable => "minecraft:block.enchantment_table.use",
            MinecraftSound.DrinkPotion => "minecraft:entity.generic.drink",
            MinecraftSound.Eat => "minecraft:entity.generic.eat",
            MinecraftSound.Burp => "minecraft:entity.player.burp",
            MinecraftSound.Thunder => "minecraft:entity.lightning_bolt.thunder",
            MinecraftSound.Rain => "minecraft:weather.rain",
            _ => "minecraft:block.note_block.harp"
        };
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
        // Minecraft 音符盒音调计算：pitch = 2^((note-12)/12)
        var noteValue = (int)note;
        return Math.Pow(2, (noteValue - 12) / 12.0);
    }

    private List<(Note note, int delayMs)> GetPredefinedMelody(PredefinedMelody melody)
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

