namespace NetherGate.API.Data.Models;

/// <summary>
/// 物品组件数据（Minecraft 1.20.5+）
/// </summary>
public record ItemComponents
{
    /// <summary>
    /// 物品 ID（如 "minecraft:diamond_sword"）
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 物品数量
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// 槽位编号（-1 表示自动查找空槽位）
    /// </summary>
    public required int Slot { get; init; }

    /// <summary>
    /// 组件数据字典
    /// 键为组件 ID（如 "minecraft:custom_name"）
    /// 值为组件数据（类型根据组件不同而不同）
    /// </summary>
    public required Dictionary<string, object> Components { get; init; } = new();

    // 快捷访问属性

    /// <summary>
    /// 快捷访问：自定义名称（minecraft:custom_name）
    /// </summary>
    public string? CustomName => GetComponent<string>("minecraft:custom_name");

    /// <summary>
    /// 快捷访问：物品描述（minecraft:lore）
    /// </summary>
    public List<string>? Lore => GetComponent<List<string>>("minecraft:lore");

    /// <summary>
    /// 快捷访问：附魔（minecraft:enchantments）
    /// </summary>
    public Dictionary<string, int>? Enchantments
    {
        get
        {
            var enchData = GetComponent<Dictionary<string, object>>("minecraft:enchantments");
            if (enchData?.TryGetValue("levels", out var levels) == true && levels is Dictionary<string, int> levelDict)
            {
                return levelDict;
            }
            return null;
        }
    }

    /// <summary>
    /// 快捷访问：损坏值（minecraft:damage）
    /// </summary>
    public int? Damage => GetComponent<int?>("minecraft:damage");

    /// <summary>
    /// 快捷访问：最大耐久（minecraft:max_damage）
    /// </summary>
    public int? MaxDamage => GetComponent<int?>("minecraft:max_damage");

    /// <summary>
    /// 快捷访问：最大堆叠数（minecraft:max_stack_size）
    /// </summary>
    public int? MaxStackSize => GetComponent<int?>("minecraft:max_stack_size");

    /// <summary>
    /// 快捷访问：稀有度（minecraft:rarity）
    /// </summary>
    public string? Rarity => GetComponent<string>("minecraft:rarity");

    /// <summary>
    /// 快捷访问：自定义模型数据（minecraft:custom_model_data）
    /// </summary>
    public int? CustomModelData => GetComponent<int?>("minecraft:custom_model_data");

    /// <summary>
    /// 快捷访问：自定义数据（minecraft:custom_data）
    /// 用于存储插件的自定义数据
    /// </summary>
    public Dictionary<string, object>? CustomData => GetComponent<Dictionary<string, object>>("minecraft:custom_data");

    /// <summary>
    /// 快捷访问：是否不可破坏（minecraft:unbreakable）
    /// </summary>
    public bool IsUnbreakable => HasComponent("minecraft:unbreakable");

    /// <summary>
    /// 快捷访问：容器内容（minecraft:container）
    /// 用于潜影盒等容器物品
    /// </summary>
    public List<ItemComponents>? ContainerContents => GetComponent<List<ItemComponents>>("minecraft:container");

    // 辅助方法

    /// <summary>
    /// 获取特定组件的值
    /// </summary>
    /// <typeparam name="T">组件值的类型</typeparam>
    /// <param name="componentId">组件 ID（如 "minecraft:custom_name"）</param>
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
                // 尝试类型转换
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
    /// 获取剩余耐久度
    /// </summary>
    /// <returns>剩余耐久，如果物品不可损坏则返回 null</returns>
    public int? GetRemainingDurability()
    {
        if (Damage.HasValue && MaxDamage.HasValue)
        {
            return MaxDamage.Value - Damage.Value;
        }
        return null;
    }

    /// <summary>
    /// 获取耐久度百分比
    /// </summary>
    /// <returns>耐久度百分比（0-100），如果物品不可损坏则返回 null</returns>
    public double? GetDurabilityPercent()
    {
        var remaining = GetRemainingDurability();
        if (remaining.HasValue && MaxDamage.HasValue && MaxDamage.Value > 0)
        {
            return (remaining.Value * 100.0) / MaxDamage.Value;
        }
        return null;
    }

    /// <summary>
    /// 检查物品是否已损坏
    /// </summary>
    public bool IsDamaged => Damage.HasValue && Damage.Value > 0;

    /// <summary>
    /// 检查物品是否可堆叠
    /// </summary>
    public bool IsStackable => (MaxStackSize ?? 1) > 1 && !Damage.HasValue;
}

