using NetherGate.API.Data;
using NetherGate.API.Data.Models;
using NetherGate.API.Extensions;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.API.Utilities;
using NetherGate.Core.Utilities;

namespace NetherGate.Core.Data;

/// <summary>
/// 方块数据写入器实现
/// 使用 RCON 命令实现容器操作
/// </summary>
public class BlockDataWriter : IBlockDataWriter
{
    private readonly IRconClient _rconClient;
    private readonly IBlockDataReader _blockDataReader;
    private readonly ILogger _logger;

    public BlockDataWriter(IRconClient rconClient, IBlockDataReader blockDataReader, ILogger logger)
    {
        _rconClient = rconClient;
        _blockDataReader = blockDataReader;
        _logger = logger;
    }

    // ========== 容器物品写入 ==========

    public async Task SetChestItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    public async Task SetBarrelItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    public async Task SetShulkerBoxItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    public async Task SetHopperItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    public async Task SetDispenserItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    public async Task SetDropperItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    public async Task SetContainerItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld")
    {
        await SetContainerItemsInternalAsync(pos, items, dimension);
    }

    // ========== 容器排序 ==========

    public async Task SortContainerAsync(Position pos, ItemSortMode mode, bool descending = false, string dimension = "overworld")
    {
        var items = await _blockDataReader.GetContainerItemsAsync(pos, dimension);
        if (items.Count == 0) return;

        var sorted = mode switch
        {
            ItemSortMode.ById => items.SortById(descending),
            ItemSortMode.ByCount => items.SortByCount(descending),
            ItemSortMode.ByCustomName => items.SortByCustomName(descending),
            ItemSortMode.BySlot => items.SortBySlot(descending),
            _ => items
        };

        // 重新分配槽位
        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            sorted[i] = new ItemStack 
            { 
                Id = item.Id,
                Count = item.Count,
                Slot = i,
                CustomName = item.CustomName,
                Enchantments = item.Enchantments
            };
        }

        await SetContainerItemsAsync(pos, sorted, dimension);
    }

    public async Task SortContainerByIdAsync(Position pos, bool descending = false, string dimension = "overworld")
    {
        await SortContainerAsync(pos, ItemSortMode.ById, descending, dimension);
    }

    public async Task SortContainerByCountAsync(Position pos, bool descending = false, string dimension = "overworld")
    {
        await SortContainerAsync(pos, ItemSortMode.ByCount, descending, dimension);
    }

    public async Task ConsolidateContainerAsync(Position pos, string dimension = "overworld")
    {
        var items = await _blockDataReader.GetContainerItemsAsync(pos, dimension);
        if (items.Count == 0) return;

        var consolidated = items.RemoveEmpty().Consolidate();
        
        // 重新分配槽位
        for (int i = 0; i < consolidated.Count; i++)
        {
            var item = consolidated[i];
            consolidated[i] = new ItemStack 
            { 
                Id = item.Id,
                Count = item.Count,
                Slot = i,
                CustomName = item.CustomName,
                Enchantments = item.Enchantments
            };
        }

        await SetContainerItemsAsync(pos, consolidated, dimension);
    }

    // ========== 清空容器 ==========

    public async Task ClearContainerAsync(Position pos, string dimension = "overworld")
    {
        var posStr = pos.ToCommandString();
        await ExecuteInDimensionAsync(dimension, $"data remove block {posStr} Items");
        _logger.Info($"已清空容器: {pos}");
    }

    public async Task RemoveItemFromContainerAsync(Position pos, string itemId, int? maxCount = null, string dimension = "overworld")
    {
        var items = await _blockDataReader.GetContainerItemsAsync(pos, dimension);
        var removed = 0;
        var remaining = new List<ItemStack>();

        foreach (var item in items)
        {
            if (item.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
            {
                if (maxCount.HasValue && removed >= maxCount.Value)
                {
                    remaining.Add(item);
                }
                else
                {
                    removed += item.Count;
                    if (maxCount.HasValue && removed > maxCount.Value)
                    {
                        var excess = removed - maxCount.Value;
                        remaining.Add(new ItemStack 
                        { 
                            Id = item.Id,
                            Count = excess,
                            Slot = item.Slot,
                            CustomName = item.CustomName,
                            Enchantments = item.Enchantments
                        });
                        removed = maxCount.Value;
                    }
                }
            }
            else
            {
                remaining.Add(item);
            }
        }

        await SetContainerItemsAsync(pos, remaining, dimension);
        _logger.Info($"从容器移除了 {removed} 个 {itemId}");
    }

    // ========== 容器物品操作 ==========

    public async Task<bool> AddItemToContainerAsync(Position pos, ItemStack item, string dimension = "overworld")
    {
        var items = await _blockDataReader.GetContainerItemsAsync(pos, dimension);
        
        // 查找空槽位
        var maxSlots = 27; // 默认箱子大小
        var occupiedSlots = items.Select(i => i.Slot).ToHashSet();
        
        for (int slot = 0; slot < maxSlots; slot++)
        {
            if (!occupiedSlots.Contains(slot))
            {
                items.Add(new ItemStack 
                { 
                    Id = item.Id,
                    Count = item.Count,
                    Slot = slot,
                    CustomName = item.CustomName,
                    Enchantments = item.Enchantments
                });
                await SetContainerItemsAsync(pos, items, dimension);
                return true;
            }
        }

        _logger.Warning($"容器已满，无法添加物品: {item.Id}");
        return false;
    }

    public async Task<ItemStack?> TakeItemFromContainerAsync(Position pos, int slot, string dimension = "overworld")
    {
        var items = await _blockDataReader.GetContainerItemsAsync(pos, dimension);
        var item = items.FirstOrDefault(i => i.Slot == slot);
        
        if (item != null)
        {
            items.Remove(item);
            await SetContainerItemsAsync(pos, items, dimension);
        }

        return item;
    }

    public async Task SwapContainerSlotsAsync(Position pos, int slot1, int slot2, string dimension = "overworld")
    {
        var items = await _blockDataReader.GetContainerItemsAsync(pos, dimension);
        
        var item1 = items.FirstOrDefault(i => i.Slot == slot1);
        var item2 = items.FirstOrDefault(i => i.Slot == slot2);

        if (item1 != null)
        {
            items.Remove(item1);
            items.Add(new ItemStack 
            { 
                Id = item1.Id,
                Count = item1.Count,
                Slot = slot2,
                CustomName = item1.CustomName,
                Enchantments = item1.Enchantments
            });
        }

        if (item2 != null)
        {
            items.Remove(item2);
            items.Add(new ItemStack 
            { 
                Id = item2.Id,
                Count = item2.Count,
                Slot = slot1,
                CustomName = item2.CustomName,
                Enchantments = item2.Enchantments
            });
        }

        await SetContainerItemsAsync(pos, items, dimension);
    }

    // ========== 告示牌写入 ==========

    public async Task SetSignTextAsync(Position pos, List<string> lines, bool isGlowing = false, string? color = null, string dimension = "overworld")
    {
        var posStr = pos.ToCommandString();
        
        // 限制为4行
        while (lines.Count < 4) lines.Add("");
        if (lines.Count > 4) lines = lines.Take(4).ToList();

        // 构建 NBT 数据
        var frontTextNbt = $"{{messages:[{StringEscapeUtils.QuoteSnbt(lines[0], true)},{StringEscapeUtils.QuoteSnbt(lines[1], true)},{StringEscapeUtils.QuoteSnbt(lines[2], true)},{StringEscapeUtils.QuoteSnbt(lines[3], true)}]}}";
        
        await ExecuteInDimensionAsync(dimension, $"data merge block {posStr} {{front_text:{frontTextNbt}}}");

        if (isGlowing)
        {
            await ExecuteInDimensionAsync(dimension, $"data merge block {posStr} {{GlowingText:1b}}");
        }

        if (!string.IsNullOrEmpty(color))
        {
            await ExecuteInDimensionAsync(dimension, $"data merge block {posStr} {{Color:\"{color}\"}}");
        }

        _logger.Info($"已设置告示牌文本: {pos}");
    }

    // ========== 方块实体通用写入 ==========

    public async Task SetBlockEntityNbtAsync(Position pos, string nbtData, string dimension = "overworld")
    {
        var posStr = pos.ToCommandString();
        await ExecuteInDimensionAsync(dimension, $"data merge block {posStr} {nbtData}");
        _logger.Info($"已设置方块实体 NBT: {pos}");
    }

    public async Task UpdateBlockEntityTagAsync(Position pos, string tagPath, object value, string dimension = "overworld")
    {
        var posStr = pos.ToCommandString();
        var valueStr = FormatNbtValue(value);
        await ExecuteInDimensionAsync(dimension, $"data modify block {posStr} {tagPath} set value {valueStr}");
        _logger.Info($"已更新方块实体标签 {tagPath}: {pos}");
    }

    // ========== 内部辅助方法 ==========

    private async Task SetContainerItemsInternalAsync(Position pos, List<ItemStack> items, string dimension)
    {
        try
        {
            // 先清空容器
            await ClearContainerAsync(pos, dimension);

            // 构建 Items NBT 列表
            var itemsNbt = new List<string>();
            
            foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.Id) && i.Id != "minecraft:air"))
            {
                var itemNbt = $"{{Slot:{item.Slot}b,id:\"{item.Id}\",Count:{item.Count}b";
                
                // 添加附魔和自定义名称等
                if (!string.IsNullOrEmpty(item.CustomName) || item.Enchantments?.Any() == true)
                {
                    itemNbt += ",tag:{";
                    var tagParts = new List<string>();

                    if (!string.IsNullOrEmpty(item.CustomName))
                    {
                        tagParts.Add($"display:{{Name:{StringEscapeUtils.QuoteSnbt(item.CustomName, true)}}}");
                    }

                    if (item.Enchantments?.Any() == true)
                    {
                        var enchants = string.Join(",", item.Enchantments.Select(e => $"{{id:\"{e.Id}\",lvl:{e.Level}s}}"));
                        tagParts.Add($"Enchantments:[{enchants}]");
                    }

                    itemNbt += string.Join(",", tagParts) + "}";
                }

                itemNbt += "}";
                itemsNbt.Add(itemNbt);
            }

            if (itemsNbt.Count > 0)
            {
                var posStr = pos.ToCommandString();
                var nbtData = $"{{Items:[{string.Join(",", itemsNbt)}]}}";
                await ExecuteInDimensionAsync(dimension, $"data merge block {posStr} {nbtData}");
            }

            _logger.Info($"已设置容器物品: {pos}, 共 {items.Count} 个物品");
        }
        catch (Exception ex)
        {
            _logger.Error($"设置容器物品失败: {ex.Message}", ex);
            throw;
        }
    }

    private async Task ExecuteInDimensionAsync(string dimension, string command)
    {
        var dimPrefix = dimension switch
        {
            "overworld" => "",
            "the_nether" => "in minecraft:the_nether run ",
            "the_end" => "in minecraft:the_end run ",
            _ => $"in {dimension} run "
        };

        await _rconClient.ExecuteCommandAsync($"execute {dimPrefix}{command}");
    }

    private string FormatNbtValue(object value)
    {
        return value switch
        {
            string s => StringEscapeUtils.QuoteSnbt(s, true),
            int i => $"{i}",
            long l => $"{l}L",
            float f => $"{f}f",
            double d => $"{d}d",
            bool b => b ? "1b" : "0b",
            byte by => $"{by}b",
            short sh => $"{sh}s",
            _ => value.ToString() ?? ""
        };
    }

    // JSON 转义功能已移至 StringEscapeUtils 工具类
}

