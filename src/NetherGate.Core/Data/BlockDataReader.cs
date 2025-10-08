using fNbt;
using NetherGate.API.Data;
using NetherGate.API.Data.Models;
using NetherGate.API.Logging;
using System.IO;

namespace NetherGate.Core.Data;

/// <summary>
/// 方块数据读取器实现
/// </summary>
public class BlockDataReader : IBlockDataReader
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;
    private readonly WorldDataReader _worldDataReader;

    public BlockDataReader(string serverDirectory, ILogger logger, WorldDataReader worldDataReader)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
        _worldDataReader = worldDataReader;
    }

    // ========== 容器物品读取 ==========

    public async Task<List<ItemStack>> GetChestItemsAsync(Position pos, string dimension = "overworld")
    {
        return await GetContainerItemsInternalAsync(pos, dimension, "minecraft:chest");
    }

    public async Task<List<ItemStack>> GetBarrelItemsAsync(Position pos, string dimension = "overworld")
    {
        return await GetContainerItemsInternalAsync(pos, dimension, "minecraft:barrel");
    }

    public async Task<List<ItemStack>> GetShulkerBoxItemsAsync(Position pos, string dimension = "overworld")
    {
        return await GetContainerItemsInternalAsync(pos, dimension, "minecraft:shulker_box");
    }

    public async Task<List<ItemStack>> GetHopperItemsAsync(Position pos, string dimension = "overworld")
    {
        return await GetContainerItemsInternalAsync(pos, dimension, "minecraft:hopper");
    }

    public async Task<List<ItemStack>> GetDispenserItemsAsync(Position pos, string dimension = "overworld")
    {
        return await GetContainerItemsInternalAsync(pos, dimension, "minecraft:dispenser");
    }

    public async Task<List<ItemStack>> GetDropperItemsAsync(Position pos, string dimension = "overworld")
    {
        return await GetContainerItemsInternalAsync(pos, dimension, "minecraft:dropper");
    }

    public async Task<FurnaceData?> GetFurnaceDataAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return null;

        var items = ParseItemsFromNbt(nbt);
        
        return new FurnaceData
        {
            Input = items.FirstOrDefault(i => i.Slot == 0),
            Fuel = items.FirstOrDefault(i => i.Slot == 1),
            Output = items.FirstOrDefault(i => i.Slot == 2),
            BurnTime = nbt.Get<NbtShort>("BurnTime")?.Value ?? 0,
            CookTime = nbt.Get<NbtShort>("CookTime")?.Value ?? 0,
            CookTimeTotal = nbt.Get<NbtShort>("CookTimeTotal")?.Value ?? 200
        };
    }

    public async Task<BrewingStandData?> GetBrewingStandDataAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return null;

        var items = ParseItemsFromNbt(nbt);
        
        return new BrewingStandData
        {
            Bottles = new List<ItemStack?>
            {
                items.FirstOrDefault(i => i.Slot == 0),
                items.FirstOrDefault(i => i.Slot == 1),
                items.FirstOrDefault(i => i.Slot == 2)
            },
            Ingredient = items.FirstOrDefault(i => i.Slot == 3),
            Fuel = items.FirstOrDefault(i => i.Slot == 4),
            BrewTime = nbt.Get<NbtShort>("BrewTime")?.Value ?? 0
        };
    }

    public async Task<List<ItemStack>> GetContainerItemsAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return new List<ItemStack>();

        return ParseItemsFromNbt(nbt);
    }

    // ========== 展示框和盔甲架 ==========

    public async Task<ItemStack?> GetItemFrameItemAsync(Position pos, string dimension = "overworld")
    {
        // 展示框是实体，不是方块实体
        // 需要从实体数据中读取
        _logger.Warning("展示框物品读取功能需要实体数据访问，当前版本暂不支持");
        return await Task.FromResult<ItemStack?>(null);
    }

    public async Task<ArmorStandData?> GetArmorStandDataAsync(Position pos, string dimension = "overworld")
    {
        // 盔甲架也是实体
        _logger.Warning("盔甲架数据读取功能需要实体数据访问，当前版本暂不支持");
        return await Task.FromResult<ArmorStandData?>(null);
    }

    // ========== 方块实体通用读取 ==========

    public async Task<string?> GetBlockEntityNbtAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return null;

        // 将 NBT 转换为 SNBT 字符串格式
        return await Task.FromResult(nbt.ToString());
    }

    public async Task<bool> HasBlockEntityAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        return nbt != null;
    }

    public async Task<string?> GetBlockEntityTypeAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return null;

        return nbt.Get<NbtString>("id")?.Value;
    }

    // ========== 告示牌和书 ==========

    public async Task<SignData?> GetSignTextAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return null;

        var frontText = new List<string>();
        var backText = new List<string>();

        // 1.20+ 格式
        if (nbt.Get<NbtCompound>("front_text") is NbtCompound frontTextCompound)
        {
            if (frontTextCompound.Get<NbtList>("messages") is NbtList messages)
            {
                foreach (var msg in messages)
                {
                    frontText.Add(msg.StringValue);
                }
            }
        }

        if (nbt.Get<NbtCompound>("back_text") is NbtCompound backTextCompound)
        {
            if (backTextCompound.Get<NbtList>("messages") is NbtList messages)
            {
                foreach (var msg in messages)
                {
                    backText.Add(msg.StringValue);
                }
            }
        }

        // 旧格式（1.19-）
        if (frontText.Count == 0)
        {
            for (int i = 1; i <= 4; i++)
            {
                var text = nbt.Get<NbtString>($"Text{i}")?.Value ?? "";
                frontText.Add(text);
            }
        }

        return new SignData
        {
            FrontText = frontText,
            BackText = backText,
            IsGlowing = nbt.Get<NbtByte>("GlowingText")?.Value == 1,
            Color = nbt.Get<NbtString>("Color")?.Value
        };
    }

    public async Task<BookData?> GetLecternBookAsync(Position pos, string dimension = "overworld")
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return null;

        var bookTag = nbt.Get<NbtCompound>("Book");
        if (bookTag == null) return null;

        var tag = bookTag.Get<NbtCompound>("tag");
        if (tag == null) return null;

        var pages = new List<string>();
        if (tag.Get<NbtList>("pages") is NbtList pagesList)
        {
            foreach (var page in pagesList)
            {
                pages.Add(page.StringValue);
            }
        }

        return new BookData
        {
            Title = tag.Get<NbtString>("title")?.Value,
            Author = tag.Get<NbtString>("author")?.Value,
            Pages = pages,
            IsSigned = bookTag.Get<NbtString>("id")?.Value == "minecraft:written_book"
        };
    }

    // ========== 内部辅助方法 ==========

    private async Task<NbtCompound?> GetBlockEntityNbtInternalAsync(Position pos, string dimension)
    {
        try
        {
            var (chunkX, chunkZ) = ((int)Math.Floor(pos.X / 16), (int)Math.Floor(pos.Z / 16));
            var regionX = (int)Math.Floor(chunkX / 32.0);
            var regionZ = (int)Math.Floor(chunkZ / 32.0);

            var dimensionPath = dimension switch
            {
                "overworld" => "world",
                "the_nether" => Path.Combine("world", "DIM-1"),
                "the_end" => Path.Combine("world", "DIM1"),
                _ => $"world/{dimension}"
            };

            var regionPath = Path.Combine(_serverDirectory, dimensionPath, "region", $"r.{regionX}.{regionZ}.mca");

            if (!File.Exists(regionPath))
            {
                _logger.Debug($"区块文件不存在: {regionPath}");
                return null;
            }

            // 读取区块数据
            // 注意：这里需要实现 Anvil 格式的区块读取
            // 由于复杂性，这里提供简化版本
            _logger.Warning("方块实体读取功能需要完整的区块文件解析，当前为简化实现");
            
            return await Task.FromResult<NbtCompound?>(null);
        }
        catch (Exception ex)
        {
            _logger.Error($"读取方块实体失败: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<List<ItemStack>> GetContainerItemsInternalAsync(Position pos, string dimension, string expectedType)
    {
        var nbt = await GetBlockEntityNbtInternalAsync(pos, dimension);
        if (nbt == null) return new List<ItemStack>();

        var blockEntityType = nbt.Get<NbtString>("id")?.Value;
        if (blockEntityType != null && !blockEntityType.Equals(expectedType, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning($"位置 {pos} 的方块实体类型不匹配，期望 {expectedType}，实际 {blockEntityType}");
            return new List<ItemStack>();
        }

        return ParseItemsFromNbt(nbt);
    }

    private List<ItemStack> ParseItemsFromNbt(NbtCompound nbt)
    {
        var items = new List<ItemStack>();

        if (nbt.Get<NbtList>("Items") is not NbtList itemsList)
            return items;

        foreach (var itemTag in itemsList.OfType<NbtCompound>())
        {
            try
            {
                var id = itemTag.Get<NbtString>("id")?.Value;
                if (string.IsNullOrEmpty(id)) continue;

                var count = itemTag.Get<NbtByte>("Count")?.Value ?? itemTag.Get<NbtByte>("count")?.Value ?? 1;
                var slot = itemTag.Get<NbtByte>("Slot")?.Value ?? -1;

                var enchantments = new List<Enchantment>();
                string? customName = null;

                // 解析 NBT 标签
                if (itemTag.Get<NbtCompound>("tag") is NbtCompound tag)
                {
                    // 附魔
                    if (tag.Get<NbtList>("Enchantments") is NbtList enchList)
                    {
                        foreach (var enchTag in enchList.OfType<NbtCompound>())
                        {
                            enchantments.Add(new Enchantment
                            {
                                Id = enchTag.Get<NbtString>("id")?.Value ?? "",
                                Level = enchTag.Get<NbtShort>("lvl")?.Value ?? 1
                            });
                        }
                    }

                    // 自定义名称
                    if (tag.Get<NbtCompound>("display") is NbtCompound display)
                    {
                        customName = display.Get<NbtString>("Name")?.Value ?? "";
                    }
                }

                items.Add(new ItemStack
                {
                    Id = id,
                    Count = count,
                    Slot = slot,
                    CustomName = customName,
                    Enchantments = enchantments.Count > 0 ? enchantments : null!
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"解析物品失败: {ex.Message}", ex);
            }
        }

        return items;
    }
}

