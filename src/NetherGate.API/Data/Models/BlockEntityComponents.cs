namespace NetherGate.API.Data.Models;

/// <summary>
/// 方块实体组件数据（Minecraft 1.20.5+）
/// </summary>
public record BlockEntityComponents
{
    /// <summary>
    /// 方块 ID（如 "minecraft:chest"）
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 方块位置
    /// </summary>
    public required BlockPosition Position { get; init; }

    /// <summary>
    /// 组件数据字典
    /// 键为组件 ID（如 "minecraft:custom_name"）
    /// 值为组件数据
    /// </summary>
    public required Dictionary<string, object> Components { get; init; } = new();

    // 快捷访问属性

    /// <summary>
    /// 快捷访问：自定义名称（minecraft:custom_name）
    /// </summary>
    public string? CustomName => GetComponent<string>("minecraft:custom_name");

    /// <summary>
    /// 快捷访问：锁定状态（minecraft:lock）
    /// </summary>
    public string? Lock => GetComponent<string>("minecraft:lock");

    /// <summary>
    /// 快捷访问：容器内容（minecraft:container）
    /// </summary>
    public List<ItemComponents>? Container => GetComponent<List<ItemComponents>>("minecraft:container");

    /// <summary>
    /// 快捷访问：战利品表（minecraft:container_loot）
    /// </summary>
    public Dictionary<string, object>? ContainerLoot => GetComponent<Dictionary<string, object>>("minecraft:container_loot");

    // 辅助方法

    /// <summary>
    /// 获取特定组件的值
    /// </summary>
    /// <typeparam name="T">组件值的类型</typeparam>
    /// <param name="componentId">组件 ID</param>
    /// <returns>组件值，如果不存在则返回 default(T)</returns>
    public T? GetComponent<T>(string componentId)
    {
        if (Components.TryGetValue(componentId, out var value))
        {
            try
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// 检查是否包含特定组件
    /// </summary>
    /// <param name="componentId">组件 ID</param>
    /// <returns>如果包含该组件则返回 true</returns>
    public bool HasComponent(string componentId) => Components.ContainsKey(componentId);

    /// <summary>
    /// 检查方块实体是否被锁定
    /// </summary>
    public bool IsLocked => !string.IsNullOrEmpty(Lock);

    /// <summary>
    /// 检查是否为容器方块实体
    /// </summary>
    public bool IsContainer => HasComponent("minecraft:container");
}

/// <summary>
/// 方块位置
/// </summary>
public record BlockPosition
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// Z 坐标
    /// </summary>
    public required int Z { get; init; }

    /// <summary>
    /// 维度（如 "minecraft:overworld"）
    /// </summary>
    public required string Dimension { get; init; }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    public override string ToString() => $"[{X}, {Y}, {Z}] in {Dimension}";

    /// <summary>
    /// 计算与另一个位置的距离
    /// </summary>
    public double DistanceTo(BlockPosition other)
    {
        if (Dimension != other.Dimension)
            return double.PositiveInfinity;

        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

