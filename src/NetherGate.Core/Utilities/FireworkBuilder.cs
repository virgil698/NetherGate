using NetherGate.API.Utilities;
using System.Text;

namespace NetherGate.Core.Utilities;

/// <summary>
/// 烟花构建器实现
/// </summary>
public class FireworkBuilder : IFireworkBuilder
{
    private readonly List<FireworkExplosion> _explosions = new();
    private int _flightDuration = 2;

    public IFireworkBuilder WithType(FireworkType type)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion { Type = type });
        }
        else
        {
            _explosions[^1].Type = type;
        }
        return this;
    }

    public IFireworkBuilder WithColor(FireworkColor color)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }
        _explosions[^1].Colors.Add(color);
        return this;
    }

    public IFireworkBuilder WithColors(params FireworkColor[] colors)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }
        foreach (var color in colors)
        {
            _explosions[^1].Colors.Add(color);
        }
        return this;
    }

    public IFireworkBuilder WithFadeColor(FireworkColor color)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }
        _explosions[^1].FadeColors.Add(color);
        return this;
    }

    public IFireworkBuilder WithFadeColors(params FireworkColor[] colors)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }
        foreach (var color in colors)
        {
            _explosions[^1].FadeColors.Add(color);
        }
        return this;
    }

    public IFireworkBuilder WithFlightDuration(int duration)
    {
        _flightDuration = Math.Clamp(duration, 1, 3);
        return this;
    }

    public IFireworkBuilder WithTrail(bool hasTrail = true)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }
        _explosions[^1].HasTrail = hasTrail;
        return this;
    }

    public IFireworkBuilder WithTwinkle(bool hasTwinkle = true)
    {
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }
        _explosions[^1].HasTwinkle = hasTwinkle;
        return this;
    }

    public IFireworkBuilder AddExplosion(Action<IFireworkExplosionBuilder> configure)
    {
        var builder = new FireworkExplosionBuilder();
        configure(builder);
        _explosions.Add(builder.Build());
        return this;
    }

    public IFireworkBuilder WithRandomColors(int count = 3)
    {
        var random = new Random();
        var colors = Enum.GetValues<FireworkColor>();
        
        if (_explosions.Count == 0)
        {
            _explosions.Add(new FireworkExplosion());
        }

        for (int i = 0; i < count; i++)
        {
            var color = colors[random.Next(colors.Length)];
            _explosions[^1].Colors.Add(color);
        }
        
        return this;
    }

    public IFireworkBuilder WithRainbowColors()
    {
        return WithColors(
            FireworkColor.Red,
            FireworkColor.Orange,
            FireworkColor.Yellow,
            FireworkColor.Lime,
            FireworkColor.Blue,
            FireworkColor.Purple
        );
    }

    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append("{Fireworks:{");
        sb.Append($"Flight:{_flightDuration}b,");
        sb.Append("Explosions:[");

        for (int i = 0; i < _explosions.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(BuildExplosion(_explosions[i]));
        }

        sb.Append("]}}");
        return sb.ToString();
    }

    private string BuildExplosion(FireworkExplosion explosion)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append($"Type:{(int)explosion.Type}b");

        if (explosion.Colors.Count > 0)
        {
            sb.Append(",Colors:[I;");
            sb.Append(string.Join(",", explosion.Colors.Select(c => GetColorValue(c))));
            sb.Append(']');
        }

        if (explosion.FadeColors.Count > 0)
        {
            sb.Append(",FadeColors:[I;");
            sb.Append(string.Join(",", explosion.FadeColors.Select(c => GetColorValue(c))));
            sb.Append(']');
        }

        if (explosion.HasTrail)
        {
            sb.Append(",Trail:1b");
        }

        if (explosion.HasTwinkle)
        {
            sb.Append(",Flicker:1b");
        }

        sb.Append('}');
        return sb.ToString();
    }

    private int GetColorValue(FireworkColor color)
    {
        return color switch
        {
            FireworkColor.Black => 0x1E1B1B,
            FireworkColor.Red => 0xB3312C,
            FireworkColor.Green => 0x3B511A,
            FireworkColor.Brown => 0x51301A,
            FireworkColor.Blue => 0x253192,
            FireworkColor.Purple => 0x7B2FBE,
            FireworkColor.Cyan => 0x287697,
            FireworkColor.LightGray => 0xABABAB,
            FireworkColor.Gray => 0x434343,
            FireworkColor.Pink => 0xD88198,
            FireworkColor.Lime => 0x41CD34,
            FireworkColor.Yellow => 0xDECF2A,
            FireworkColor.LightBlue => 0x6689D3,
            FireworkColor.Magenta => 0xC354CD,
            FireworkColor.Orange => 0xEB8844,
            FireworkColor.White => 0xF0F0F0,
            _ => 0xFFFFFF
        };
    }

    internal class FireworkExplosion
    {
        public FireworkType Type { get; set; } = FireworkType.LargeBall;
        public List<FireworkColor> Colors { get; } = new();
        public List<FireworkColor> FadeColors { get; } = new();
        public bool HasTrail { get; set; }
        public bool HasTwinkle { get; set; }
    }
}

/// <summary>
/// 烟花爆炸效果构建器
/// </summary>
internal class FireworkExplosionBuilder : IFireworkExplosionBuilder
{
    private readonly FireworkBuilder.FireworkExplosion _explosion = new();

    public IFireworkExplosionBuilder WithType(FireworkType type)
    {
        _explosion.Type = type;
        return this;
    }

    public IFireworkExplosionBuilder WithColor(FireworkColor color)
    {
        _explosion.Colors.Add(color);
        return this;
    }

    public IFireworkExplosionBuilder WithColors(params FireworkColor[] colors)
    {
        foreach (var color in colors)
        {
            _explosion.Colors.Add(color);
        }
        return this;
    }

    public IFireworkExplosionBuilder WithFadeColor(FireworkColor color)
    {
        _explosion.FadeColors.Add(color);
        return this;
    }

    public IFireworkExplosionBuilder WithFadeColors(params FireworkColor[] colors)
    {
        foreach (var color in colors)
        {
            _explosion.FadeColors.Add(color);
        }
        return this;
    }

    public IFireworkExplosionBuilder WithTrail(bool hasTrail = true)
    {
        _explosion.HasTrail = hasTrail;
        return this;
    }

    public IFireworkExplosionBuilder WithTwinkle(bool hasTwinkle = true)
    {
        _explosion.HasTwinkle = hasTwinkle;
        return this;
    }

    internal FireworkBuilder.FireworkExplosion Build() => _explosion;
}

