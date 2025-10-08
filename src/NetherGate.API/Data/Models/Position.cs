namespace NetherGate.API.Data.Models;

/// <summary>
/// 三维位置坐标
/// 用于表示方块或实体的精确位置
/// </summary>
public record Position
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Z 坐标
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public Position(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public Position() : this(0, 0, 0)
    {
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";

    /// <summary>
    /// 解构
    /// </summary>
    public void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }
}

