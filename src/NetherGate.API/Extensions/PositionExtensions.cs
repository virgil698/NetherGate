using NetherGate.API.Data.Models;

namespace NetherGate.API.Extensions;

/// <summary>
/// 位置扩展方法
/// </summary>
public static class PositionExtensions
{
    // ========== 偏移操作 ==========
    
    /// <summary>
    /// 按偏移量创建新位置
    /// </summary>
    public static Position Offset(this Position pos, double x, double y, double z)
    {
        return new Position(pos.X + x, pos.Y + y, pos.Z + z);
    }
    
    /// <summary>
    /// 向上偏移
    /// </summary>
    public static Position Up(this Position pos, double offset = 1)
    {
        return pos.Offset(0, offset, 0);
    }
    
    /// <summary>
    /// 向下偏移
    /// </summary>
    public static Position Down(this Position pos, double offset = 1)
    {
        return pos.Offset(0, -offset, 0);
    }
    
    /// <summary>
    /// 向北偏移（-Z）
    /// </summary>
    public static Position North(this Position pos, double offset = 1)
    {
        return pos.Offset(0, 0, -offset);
    }
    
    /// <summary>
    /// 向南偏移（+Z）
    /// </summary>
    public static Position South(this Position pos, double offset = 1)
    {
        return pos.Offset(0, 0, offset);
    }
    
    /// <summary>
    /// 向东偏移（+X）
    /// </summary>
    public static Position East(this Position pos, double offset = 1)
    {
        return pos.Offset(offset, 0, 0);
    }
    
    /// <summary>
    /// 向西偏移（-X）
    /// </summary>
    public static Position West(this Position pos, double offset = 1)
    {
        return pos.Offset(-offset, 0, 0);
    }
    
    // ========== 距离计算 ==========
    
    /// <summary>
    /// 计算到另一个位置的欧几里得距离
    /// </summary>
    public static double DistanceTo(this Position from, Position to)
    {
        return Math.Sqrt(
            Math.Pow(to.X - from.X, 2) + 
            Math.Pow(to.Y - from.Y, 2) + 
            Math.Pow(to.Z - from.Z, 2));
    }
    
    /// <summary>
    /// 计算到另一个位置的曼哈顿距离（不考虑对角线）
    /// </summary>
    public static double ManhattanDistanceTo(this Position from, Position to)
    {
        return Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y) + Math.Abs(to.Z - from.Z);
    }
    
    /// <summary>
    /// 计算水平距离（忽略 Y 轴）
    /// </summary>
    public static double HorizontalDistanceTo(this Position from, Position to)
    {
        return Math.Sqrt(
            Math.Pow(to.X - from.X, 2) + 
            Math.Pow(to.Z - from.Z, 2));
    }
    
    // ========== 方向计算 ==========
    
    /// <summary>
    /// 计算指向另一个位置的方向向量（归一化）
    /// </summary>
    public static (double X, double Y, double Z) DirectionTo(this Position from, Position to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var dz = to.Z - from.Z;
        
        var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (length == 0) return (0, 0, 0);
        
        return (dx / length, dy / length, dz / length);
    }
    
    /// <summary>
    /// 获取朝向（基于 X-Z 平面）
    /// </summary>
    public static CardinalDirection GetCardinalDirection(this Position from, Position to)
    {
        var dx = to.X - from.X;
        var dz = to.Z - from.Z;
        
        var angle = Math.Atan2(dz, dx) * (180 / Math.PI);
        
        // 将角度转换为 0-360 度
        if (angle < 0) angle += 360;
        
        // 转换为方向
        if (angle >= 315 || angle < 45) return CardinalDirection.East;
        if (angle >= 45 && angle < 135) return CardinalDirection.South;
        if (angle >= 135 && angle < 225) return CardinalDirection.West;
        return CardinalDirection.North;
    }
    
    // ========== 区域操作 ==========
    
    /// <summary>
    /// 检查位置是否在区域内
    /// </summary>
    public static bool IsInside(this Position pos, Position corner1, Position corner2)
    {
        var minX = Math.Min(corner1.X, corner2.X);
        var maxX = Math.Max(corner1.X, corner2.X);
        var minY = Math.Min(corner1.Y, corner2.Y);
        var maxY = Math.Max(corner1.Y, corner2.Y);
        var minZ = Math.Min(corner1.Z, corner2.Z);
        var maxZ = Math.Max(corner1.Z, corner2.Z);
        
        return pos.X >= minX && pos.X <= maxX &&
               pos.Y >= minY && pos.Y <= maxY &&
               pos.Z >= minZ && pos.Z <= maxZ;
    }
    
    /// <summary>
    /// 获取两个位置之间的所有整数坐标位置
    /// </summary>
    public static List<Position> GetPositionsBetween(this Position from, Position to)
    {
        var positions = new List<Position>();
        
        var minX = (int)Math.Min(from.X, to.X);
        var maxX = (int)Math.Max(from.X, to.X);
        var minY = (int)Math.Min(from.Y, to.Y);
        var maxY = (int)Math.Max(from.Y, to.Y);
        var minZ = (int)Math.Min(from.Z, to.Z);
        var maxZ = (int)Math.Max(from.Z, to.Z);
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    positions.Add(new Position(x, y, z));
                }
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// 计算区域体积（方块数）
    /// </summary>
    public static int GetVolume(this Position corner1, Position corner2)
    {
        var dx = (int)Math.Abs(corner2.X - corner1.X) + 1;
        var dy = (int)Math.Abs(corner2.Y - corner1.Y) + 1;
        var dz = (int)Math.Abs(corner2.Z - corner1.Z) + 1;
        
        return dx * dy * dz;
    }
    
    // ========== 坐标转换 ==========
    
    /// <summary>
    /// 转换为方块坐标（向下取整）
    /// </summary>
    public static Position ToBlockPosition(this Position pos)
    {
        return new Position(Math.Floor(pos.X), Math.Floor(pos.Y), Math.Floor(pos.Z));
    }
    
    /// <summary>
    /// 转换为区块坐标
    /// </summary>
    public static (int ChunkX, int ChunkZ) ToChunkPosition(this Position pos)
    {
        return ((int)Math.Floor(pos.X / 16), (int)Math.Floor(pos.Z / 16));
    }
    
    /// <summary>
    /// 转换为中心坐标（方块中心）
    /// </summary>
    public static Position ToCenterPosition(this Position pos)
    {
        return new Position(Math.Floor(pos.X) + 0.5, Math.Floor(pos.Y), Math.Floor(pos.Z) + 0.5);
    }
    
    // ========== 插值操作 ==========
    
    /// <summary>
    /// 线性插值到另一个位置
    /// </summary>
    /// <param name="from">起始位置</param>
    /// <param name="to">目标位置</param>
    /// <param name="t">插值参数（0-1）</param>
    public static Position Lerp(this Position from, Position to, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Position(
            from.X + (to.X - from.X) * t,
            from.Y + (to.Y - from.Y) * t,
            from.Z + (to.Z - from.Z) * t
        );
    }
    
    /// <summary>
    /// 获取从起点到终点的路径上的所有位置
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="to">终点</param>
    /// <param name="steps">步数</param>
    public static List<Position> GetPath(this Position from, Position to, int steps = 10)
    {
        var path = new List<Position>();
        for (int i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            path.Add(from.Lerp(to, t));
        }
        return path;
    }
    
    // ========== 实用方法 ==========
    
    /// <summary>
    /// 获取相邻的六个方块位置（上下东西南北）
    /// </summary>
    public static List<Position> GetAdjacentPositions(this Position pos)
    {
        return new List<Position>
        {
            pos.Up(),
            pos.Down(),
            pos.East(),
            pos.West(),
            pos.North(),
            pos.South()
        };
    }
    
    /// <summary>
    /// 获取周围的 26 个方块位置（3x3x3 立方体，不包括中心）
    /// </summary>
    public static List<Position> GetSurroundingPositions(this Position pos)
    {
        var positions = new List<Position>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0) continue;
                    positions.Add(pos.Offset(x, y, z));
                }
            }
        }
        return positions;
    }
    
    /// <summary>
    /// 转换为命令格式字符串（整数坐标）
    /// </summary>
    public static string ToCommandString(this Position pos)
    {
        return $"{(int)pos.X} {(int)pos.Y} {(int)pos.Z}";
    }
    
    /// <summary>
    /// 转换为精确命令格式字符串（带小数）
    /// </summary>
    public static string ToPreciseCommandString(this Position pos, int decimals = 2)
    {
        return $"{pos.X.ToString($"F{decimals}")} {pos.Y.ToString($"F{decimals}")} {pos.Z.ToString($"F{decimals}")}";
    }
    
    /// <summary>
    /// 转换为可读字符串
    /// </summary>
    public static string ToReadableString(this Position pos)
    {
        return $"({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})";
    }
}

/// <summary>
/// 基本方向
/// </summary>
public enum CardinalDirection
{
    /// <summary>
    /// 北（-Z）
    /// </summary>
    North,
    
    /// <summary>
    /// 南（+Z）
    /// </summary>
    South,
    
    /// <summary>
    /// 东（+X）
    /// </summary>
    East,
    
    /// <summary>
    /// 西（-X）
    /// </summary>
    West
}

