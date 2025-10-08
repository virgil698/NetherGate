using NetherGate.API.Data;
using NetherGate.API.Data.Models;
using System.Text.RegularExpressions;

namespace NetherGate.API.Extensions;

/// <summary>
/// 物品堆扩展方法
/// </summary>
public static class ItemStackExtensions
{
    // ========== 排序扩展 ==========
    
    /// <summary>
    /// 按物品 ID 排序
    /// </summary>
    public static List<ItemStack> SortById(this List<ItemStack> items, bool descending = false)
    {
        return descending 
            ? items.OrderByDescending(i => i.Id).ToList()
            : items.OrderBy(i => i.Id).ToList();
    }
    
    /// <summary>
    /// 按物品 ID 排序（降序）
    /// </summary>
    public static List<ItemStack> SortByIdDescending(this List<ItemStack> items)
    {
        return items.SortById(descending: true);
    }
    
    /// <summary>
    /// 按数量排序
    /// </summary>
    public static List<ItemStack> SortByCount(this List<ItemStack> items, bool descending = false)
    {
        return descending 
            ? items.OrderByDescending(i => i.Count).ToList()
            : items.OrderBy(i => i.Count).ToList();
    }
    
    /// <summary>
    /// 按数量排序（降序）
    /// </summary>
    public static List<ItemStack> SortByCountDescending(this List<ItemStack> items)
    {
        return items.SortByCount(descending: true);
    }
    
    /// <summary>
    /// 按自定义名称排序
    /// </summary>
    public static List<ItemStack> SortByCustomName(this List<ItemStack> items, bool descending = false)
    {
        return descending 
            ? items.OrderByDescending(i => i.CustomName ?? i.Id).ToList()
            : items.OrderBy(i => i.CustomName ?? i.Id).ToList();
    }
    
    /// <summary>
    /// 按槽位排序
    /// </summary>
    public static List<ItemStack> SortBySlot(this List<ItemStack> items, bool descending = false)
    {
        return descending 
            ? items.OrderByDescending(i => i.Slot).ToList()
            : items.OrderBy(i => i.Slot).ToList();
    }
    
    // ========== 筛选扩展 ==========
    
    /// <summary>
    /// 按物品类型筛选
    /// </summary>
    public static List<ItemStack> FilterByType(this List<ItemStack> items, string itemId)
    {
        return items.Where(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    
    /// <summary>
    /// 按物品类型筛选（支持通配符）
    /// </summary>
    public static List<ItemStack> FilterByPattern(this List<ItemStack> items, string pattern)
    {
        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase);
        
        return items.Where(i => regex.IsMatch(i.Id)).ToList();
    }
    
    /// <summary>
    /// 筛选附魔物品
    /// </summary>
    public static List<ItemStack> FilterEnchanted(this List<ItemStack> items)
    {
        return items.Where(i => i.Enchantments?.Any() == true).ToList();
    }
    
    /// <summary>
    /// 筛选有自定义名称的物品
    /// </summary>
    public static List<ItemStack> FilterNamed(this List<ItemStack> items)
    {
        return items.Where(i => !string.IsNullOrEmpty(i.CustomName)).ToList();
    }
    
    /// <summary>
    /// 筛选可堆叠物品（数量 > 1 或可堆叠类型）
    /// </summary>
    public static List<ItemStack> FilterStackable(this List<ItemStack> items)
    {
        return items.Where(i => i.Count > 1).ToList();
    }
    
    /// <summary>
    /// 按最小数量筛选
    /// </summary>
    public static List<ItemStack> FilterByMinCount(this List<ItemStack> items, int minCount)
    {
        return items.Where(i => i.Count >= minCount).ToList();
    }
    
    /// <summary>
    /// 按最大数量筛选
    /// </summary>
    public static List<ItemStack> FilterByMaxCount(this List<ItemStack> items, int maxCount)
    {
        return items.Where(i => i.Count <= maxCount).ToList();
    }
    
    // ========== 统计扩展 ==========
    
    /// <summary>
    /// 计算物品总数
    /// </summary>
    public static int TotalCount(this List<ItemStack> items)
    {
        return items.Sum(i => i.Count);
    }
    
    /// <summary>
    /// 计算特定物品的总数
    /// </summary>
    public static int TotalCountOf(this List<ItemStack> items, string itemId)
    {
        return items.Where(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                    .Sum(i => i.Count);
    }
    
    /// <summary>
    /// 获取所有唯一的物品类型
    /// </summary>
    public static List<string> GetUniqueTypes(this List<ItemStack> items)
    {
        return items.Select(i => i.Id).Distinct().OrderBy(id => id).ToList();
    }
    
    /// <summary>
    /// 按类型分组
    /// </summary>
    public static Dictionary<string, List<ItemStack>> GroupByType(this List<ItemStack> items)
    {
        return items.GroupBy(i => i.Id)
                    .ToDictionary(g => g.Key, g => g.ToList());
    }
    
    /// <summary>
    /// 获取物品统计信息
    /// </summary>
    public static ItemStatistics GetStatistics(this List<ItemStack> items)
    {
        return new ItemStatistics
        {
            TotalSlots = items.Count,
            TotalItems = items.Sum(i => i.Count),
            UniqueTypes = items.Select(i => i.Id).Distinct().Count(),
            EnchantedItems = items.Count(i => i.Enchantments?.Any() == true),
            NamedItems = items.Count(i => !string.IsNullOrEmpty(i.CustomName)),
            AverageStackSize = items.Count > 0 ? (double)items.Sum(i => i.Count) / items.Count : 0
        };
    }
    
    // ========== 转换扩展 ==========
    
    /// <summary>
    /// 合并相同物品堆叠
    /// </summary>
    public static List<ItemStack> Consolidate(this List<ItemStack> items, int maxStackSize = 64)
    {
        var result = new List<ItemStack>();
        var groups = items.GroupBy(i => new { i.Id, i.CustomName, Enchants = string.Join(",", i.Enchantments?.Select(e => e.Id) ?? Array.Empty<string>()) });
        
        foreach (var group in groups)
        {
            var totalCount = group.Sum(i => i.Count);
            var template = group.First();
            
            while (totalCount > 0)
            {
                var stackCount = Math.Min(totalCount, maxStackSize);
                result.Add(new ItemStack
                {
                    Id = template.Id,
                    Count = stackCount,
                    CustomName = template.CustomName,
                    Enchantments = template.Enchantments,
                    Slot = -1
                });
                totalCount -= stackCount;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 移除空物品堆
    /// </summary>
    public static List<ItemStack> RemoveEmpty(this List<ItemStack> items)
    {
        return items.Where(i => i.Count > 0 && !string.IsNullOrEmpty(i.Id) && i.Id != "minecraft:air").ToList();
    }
    
    /// <summary>
    /// 克隆物品列表
    /// </summary>
    public static List<ItemStack> Clone(this List<ItemStack> items)
    {
        return items.Select(i => new ItemStack
        {
            Id = i.Id,
            Count = i.Count,
            Slot = i.Slot,
            CustomName = i.CustomName,
            Enchantments = i.Enchantments?.Select(e => new Enchantment { Id = e.Id, Level = e.Level }).ToList() ?? null!
        }).ToList();
    }
}

/// <summary>
/// 物品统计信息
/// </summary>
public record ItemStatistics
{
    /// <summary>
    /// 总槽位数（包含空槽位）
    /// </summary>
    public int TotalSlots { get; init; }
    
    /// <summary>
    /// 总物品数
    /// </summary>
    public int TotalItems { get; init; }
    
    /// <summary>
    /// 唯一物品类型数
    /// </summary>
    public int UniqueTypes { get; init; }
    
    /// <summary>
    /// 附魔物品数
    /// </summary>
    public int EnchantedItems { get; init; }
    
    /// <summary>
    /// 自定义名称物品数
    /// </summary>
    public int NamedItems { get; init; }
    
    /// <summary>
    /// 平均堆叠大小
    /// </summary>
    public double AverageStackSize { get; init; }
    
    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    public override string ToString() => 
        $"Slots: {TotalSlots}, Items: {TotalItems}, Types: {UniqueTypes}, Avg Stack: {AverageStackSize:F1}";
}

