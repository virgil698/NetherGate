using NetherGate.API.Data.Models;

namespace NetherGate.API.Data;

/// <summary>
/// 方块数据读取器接口
/// 用于读取容器（箱子、漏斗等）和方块实体的数据
/// </summary>
public interface IBlockDataReader
{
    // ========== 容器物品读取 ==========
    
    /// <summary>
    /// 获取箱子内的物品
    /// </summary>
    /// <param name="pos">箱子位置</param>
    /// <param name="dimension">维度（默认主世界）</param>
    /// <returns>箱子内的物品列表</returns>
    Task<List<ItemStack>> GetChestItemsAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取木桶内的物品
    /// </summary>
    Task<List<ItemStack>> GetBarrelItemsAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取潜影盒内的物品
    /// </summary>
    Task<List<ItemStack>> GetShulkerBoxItemsAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取漏斗内的物品
    /// </summary>
    Task<List<ItemStack>> GetHopperItemsAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取发射器内的物品
    /// </summary>
    Task<List<ItemStack>> GetDispenserItemsAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取投掷器内的物品
    /// </summary>
    Task<List<ItemStack>> GetDropperItemsAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取熔炉内的物品
    /// </summary>
    Task<FurnaceData?> GetFurnaceDataAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取酿造台内的物品
    /// </summary>
    Task<BrewingStandData?> GetBrewingStandDataAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取通用容器内的物品（自动识别容器类型）
    /// </summary>
    Task<List<ItemStack>> GetContainerItemsAsync(Position pos, string dimension = "overworld");
    
    // ========== 展示框和盔甲架 ==========
    
    /// <summary>
    /// 获取展示框中的物品
    /// </summary>
    Task<ItemStack?> GetItemFrameItemAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取盔甲架的装备
    /// </summary>
    Task<ArmorStandData?> GetArmorStandDataAsync(Position pos, string dimension = "overworld");
    
    // ========== 方块实体通用读取 ==========
    
    /// <summary>
    /// 读取方块实体的 NBT 数据
    /// </summary>
    Task<string?> GetBlockEntityNbtAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 检查位置是否存在方块实体
    /// </summary>
    Task<bool> HasBlockEntityAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 获取方块实体类型
    /// </summary>
    Task<string?> GetBlockEntityTypeAsync(Position pos, string dimension = "overworld");
    
    // ========== 告示牌和书 ==========
    
    /// <summary>
    /// 读取告示牌文本
    /// </summary>
    Task<SignData?> GetSignTextAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 读取讲台上的书
    /// </summary>
    Task<BookData?> GetLecternBookAsync(Position pos, string dimension = "overworld");
}

/// <summary>
/// 熔炉数据
/// </summary>
public record FurnaceData
{
    /// <summary>
    /// 输入槽物品
    /// </summary>
    public ItemStack? Input { get; init; }
    
    /// <summary>
    /// 燃料槽物品
    /// </summary>
    public ItemStack? Fuel { get; init; }
    
    /// <summary>
    /// 输出槽物品
    /// </summary>
    public ItemStack? Output { get; init; }
    
    /// <summary>
    /// 燃烧时间（tick）
    /// </summary>
    public short BurnTime { get; init; }
    
    /// <summary>
    /// 烹饪时间（tick）
    /// </summary>
    public short CookTime { get; init; }
    
    /// <summary>
    /// 总烹饪时间（tick）
    /// </summary>
    public short CookTimeTotal { get; init; }
}

/// <summary>
/// 酿造台数据
/// </summary>
public record BrewingStandData
{
    /// <summary>
    /// 三个药水瓶槽位
    /// </summary>
    public List<ItemStack?> Bottles { get; init; } = new();
    
    /// <summary>
    /// 材料槽物品
    /// </summary>
    public ItemStack? Ingredient { get; init; }
    
    /// <summary>
    /// 燃料槽物品（烈焰粉）
    /// </summary>
    public ItemStack? Fuel { get; init; }
    
    /// <summary>
    /// 酿造时间（tick）
    /// </summary>
    public short BrewTime { get; init; }
}

/// <summary>
/// 盔甲架数据
/// </summary>
public record ArmorStandData
{
    /// <summary>
    /// 头部装备
    /// </summary>
    public ItemStack? Helmet { get; init; }
    
    /// <summary>
    /// 胸部装备
    /// </summary>
    public ItemStack? Chestplate { get; init; }
    
    /// <summary>
    /// 腿部装备
    /// </summary>
    public ItemStack? Leggings { get; init; }
    
    /// <summary>
    /// 脚部装备
    /// </summary>
    public ItemStack? Boots { get; init; }
    
    /// <summary>
    /// 主手物品
    /// </summary>
    public ItemStack? MainHand { get; init; }
    
    /// <summary>
    /// 副手物品
    /// </summary>
    public ItemStack? OffHand { get; init; }
    
    /// <summary>
    /// 位置
    /// </summary>
    public Position Position { get; init; } = new(0, 0, 0);
    
    /// <summary>
    /// 是否有基座
    /// </summary>
    public bool HasBasePlate { get; init; }
    
    /// <summary>
    /// 是否显示手臂
    /// </summary>
    public bool ShowArms { get; init; }
}

/// <summary>
/// 告示牌数据
/// </summary>
public record SignData
{
    /// <summary>
    /// 前面的文本（4行）
    /// </summary>
    public List<string> FrontText { get; init; } = new();
    
    /// <summary>
    /// 背面的文本（4行，1.20+）
    /// </summary>
    public List<string> BackText { get; init; } = new();
    
    /// <summary>
    /// 是否发光
    /// </summary>
    public bool IsGlowing { get; init; }
    
    /// <summary>
    /// 染料颜色
    /// </summary>
    public string? Color { get; init; }
}

/// <summary>
/// 书数据
/// </summary>
public record BookData
{
    /// <summary>
    /// 书的标题
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; init; }
    
    /// <summary>
    /// 页面内容
    /// </summary>
    public List<string> Pages { get; init; } = new();
    
    /// <summary>
    /// 是否已签名
    /// </summary>
    public bool IsSigned { get; init; }
}

