namespace NetherGate.API.Data;

/// <summary>
/// 标签系统 API
/// <para>标签（Tag）允许使用 JSON 文件将游戏资源分组，存储于数据包的 data/&lt;命名空间&gt;/tags/&lt;注册名&gt; 目录下。</para>
/// <para>在命令中引用标签时，需要以 # 开头，如 #minecraft:copper。</para>
/// </summary>
/// <remarks>
/// 支持的标签类型：
/// <list type="bullet">
/// <item>方块标签 (block)</item>
/// <item>物品标签 (item)</item>
/// <item>实体类型标签 (entity_type)</item>
/// <item>流体标签 (fluid)</item>
/// <item>游戏事件标签 (game_event)</item>
/// <item>生物群系标签 (worldgen/biome)</item>
/// </list>
/// </remarks>
public interface ITagApi
{
    // ==================== 方块标签 ====================
    
    /// <summary>
    /// 检查方块是否属于某个标签
    /// </summary>
    /// <param name="blockId">方块 ID（如 "minecraft:copper_block"）</param>
    /// <param name="tag">标签（如 "minecraft:copper" 或 "copper"，无需 # 前缀）</param>
    /// <returns>是否属于该标签</returns>
    Task<bool> IsBlockInTagAsync(string blockId, string tag);

    /// <summary>
    /// 获取标签中的所有方块
    /// </summary>
    /// <param name="tag">标签名称（无需 # 前缀）</param>
    /// <returns>方块 ID 列表</returns>
    Task<List<string>> GetBlocksInTagAsync(string tag);

    /// <summary>
    /// 获取所有可用的方块标签
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<string>> GetAllBlockTagsAsync();

    // ==================== 物品标签 ====================
    
    /// <summary>
    /// 检查物品是否属于某个标签
    /// </summary>
    /// <param name="itemId">物品 ID（如 "minecraft:copper_ingot"）</param>
    /// <param name="tag">标签（如 "minecraft:copper" 或 "copper"，无需 # 前缀）</param>
    /// <returns>是否属于该标签</returns>
    Task<bool> IsItemInTagAsync(string itemId, string tag);

    /// <summary>
    /// 获取标签中的所有物品
    /// </summary>
    /// <param name="tag">标签名称（无需 # 前缀）</param>
    /// <returns>物品 ID 列表</returns>
    Task<List<string>> GetItemsInTagAsync(string tag);

    /// <summary>
    /// 获取所有可用的物品标签
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<string>> GetAllItemTagsAsync();

    // ==================== 实体类型标签 ====================
    
    /// <summary>
    /// 检查实体是否属于某个标签
    /// </summary>
    /// <param name="entityType">实体类型（如 "minecraft:villager"）</param>
    /// <param name="tag">标签（无需 # 前缀）</param>
    /// <returns>是否属于该标签</returns>
    Task<bool> IsEntityInTagAsync(string entityType, string tag);

    /// <summary>
    /// 获取标签中的所有实体类型
    /// </summary>
    /// <param name="tag">标签名称（无需 # 前缀）</param>
    /// <returns>实体类型 ID 列表</returns>
    Task<List<string>> GetEntitiesInTagAsync(string tag);

    /// <summary>
    /// 获取所有可用的实体标签
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<string>> GetAllEntityTagsAsync();

    // ==================== 流体标签 ====================
    
    /// <summary>
    /// 检查流体是否属于某个标签
    /// </summary>
    /// <param name="fluidId">流体 ID（如 "minecraft:water"）</param>
    /// <param name="tag">标签（无需 # 前缀）</param>
    /// <returns>是否属于该标签</returns>
    Task<bool> IsFluidInTagAsync(string fluidId, string tag);

    /// <summary>
    /// 获取标签中的所有流体
    /// </summary>
    /// <param name="tag">标签名称（无需 # 前缀）</param>
    /// <returns>流体 ID 列表</returns>
    Task<List<string>> GetFluidsInTagAsync(string tag);

    /// <summary>
    /// 获取所有可用的流体标签
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<string>> GetAllFluidTagsAsync();

    // ==================== 游戏事件标签 ====================
    
    /// <summary>
    /// 检查游戏事件是否属于某个标签
    /// </summary>
    /// <param name="eventId">游戏事件 ID</param>
    /// <param name="tag">标签（无需 # 前缀）</param>
    /// <returns>是否属于该标签</returns>
    Task<bool> IsGameEventInTagAsync(string eventId, string tag);

    /// <summary>
    /// 获取标签中的所有游戏事件
    /// </summary>
    /// <param name="tag">标签名称（无需 # 前缀）</param>
    /// <returns>游戏事件 ID 列表</returns>
    Task<List<string>> GetGameEventsInTagAsync(string tag);

    /// <summary>
    /// 获取所有可用的游戏事件标签
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<string>> GetAllGameEventTagsAsync();

    // ==================== 生物群系标签 ====================
    
    /// <summary>
    /// 检查生物群系是否属于某个标签
    /// </summary>
    /// <param name="biomeId">生物群系 ID（如 "minecraft:plains"）</param>
    /// <param name="tag">标签（无需 # 前缀）</param>
    /// <returns>是否属于该标签</returns>
    Task<bool> IsBiomeInTagAsync(string biomeId, string tag);

    /// <summary>
    /// 获取标签中的所有生物群系
    /// </summary>
    /// <param name="tag">标签名称（无需 # 前缀）</param>
    /// <returns>生物群系 ID 列表</returns>
    Task<List<string>> GetBiomesInTagAsync(string tag);

    /// <summary>
    /// 获取所有可用的生物群系标签
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<string>> GetAllBiomeTagsAsync();
}

/// <summary>
/// 常见标签常量（Minecraft 1.21.9+）
/// </summary>
public static class MinecraftTags
{
    /// <summary>
    /// 方块标签
    /// </summary>
    public static class Blocks
    {
        /// <summary>铜箱子标签</summary>
        public const string CopperChests = "minecraft:copper_chests";
        
        /// <summary>铜方块标签</summary>
        public const string Copper = "minecraft:copper";
        
        /// <summary>铜质工具不适用标签</summary>
        public const string IncorrectForCopperTool = "minecraft:incorrect_for_copper_tool";
        
        /// <summary>铜傀儡像标签</summary>
        public const string CopperGolemStatues = "minecraft:copper_golem_statues";
        
        /// <summary>避雷针标签</summary>
        public const string LightningRods = "minecraft:lightning_rods";
        
        /// <summary>木架标签</summary>
        public const string WoodenShelves = "minecraft:wooden_shelves";
        
        /// <summary>链条标签</summary>
        public const string Chains = "minecraft:chains";
        
        /// <summary>灯笼标签</summary>
        public const string Lanterns = "minecraft:lanterns";
        
        /// <summary>涂蜡氧化铜灯笼标签</summary>
        public const string WaxedOxidizedCopperLantern = "minecraft:waxed_oxidized_copper_lantern";
        
        /// <summary>栏杆标签</summary>
        public const string Bars = "minecraft:bars";
    }

    /// <summary>
    /// 物品标签
    /// </summary>
    public static class Items
    {
        /// <summary>铜箱子物品标签</summary>
        public const string CopperChests = "minecraft:copper_chests";
        
        /// <summary>铜物品标签</summary>
        public const string Copper = "minecraft:copper";
        
        /// <summary>铜质工具材料标签</summary>
        public const string CopperToolMaterials = "minecraft:copper_tool_materials";
        
        /// <summary>修复铜盔甲的物品标签</summary>
        public const string RepairsCopperArmor = "minecraft:repairs_copper_armor";
        
        /// <summary>铜傀儡像物品标签</summary>
        public const string CopperGolemStatues = "minecraft:copper_golem_statues";
        
        /// <summary>避雷针物品标签</summary>
        public const string LightningRods = "minecraft:lightning_rods";
        
        /// <summary>木架物品标签</summary>
        public const string WoodenShelves = "minecraft:wooden_shelves";
        
        /// <summary>链条物品标签</summary>
        public const string Chains = "minecraft:chains";
        
        /// <summary>灯笼物品标签</summary>
        public const string Lanterns = "minecraft:lanterns";
        
        /// <summary>栏杆物品标签</summary>
        public const string Bars = "minecraft:bars";
        
        /// <summary>可从铜傀儡剪取的物品标签</summary>
        public const string ShearableFromCopperGolem = "minecraft:shearable_from_copper_golem";
    }

    /// <summary>
    /// 实体类型标签
    /// </summary>
    public static class EntityTypes
    {
        /// <summary>接受铁傀儡礼物的实体标签</summary>
        public const string AcceptsIronGolemGift = "minecraft:accepts_iron_golem_gift";
        
        /// <summary>有资格获得铁傀儡礼物的实体标签</summary>
        public const string CandidateForIronGolemGift = "minecraft:candidate_for_iron_golem_gift";
        
        /// <summary>不会被推上船的实体标签</summary>
        public const string CannotBePushedOntoBoats = "minecraft:cannot_be_pushed_onto_boats";
    }

    /// <summary>
    /// 游戏事件标签
    /// </summary>
    public static class GameEvents
    {
        /// <summary>要塞偏向生成的群系标签</summary>
        public const string StrongholdBiasedTo = "minecraft:stronghold_biased_to";
        
        /// <summary>丛生最大可采集物标签</summary>
        public const string ClusterMaxHarvestables = "minecraft:cluster_max_harvestables";
        
        /// <summary>不可渗透方块标签</summary>
        public const string Impermeable = "minecraft:impermeable";
        
        /// <summary>振动</summary>
        public const string Vibrations = "minecraft:vibrations";
        
        /// <summary>可忽略的振动</summary>
        public const string IgnoreVibrationsSneaking = "minecraft:ignore_vibrations_sneaking";
    }

    /// <summary>
    /// 流体标签
    /// </summary>
    public static class Fluids
    {
        /// <summary>水标签</summary>
        public const string Water = "minecraft:water";
        
        /// <summary>熔岩标签</summary>
        public const string Lava = "minecraft:lava";
    }

    /// <summary>
    /// 生物群系标签
    /// </summary>
    public static class Biomes
    {
        /// <summary>可生成村庄的生物群系</summary>
        public const string HasStructureVillage = "minecraft:has_structure/village";
        
        /// <summary>可生成沙漠神殿的生物群系</summary>
        public const string HasStructureDesertPyramid = "minecraft:has_structure/desert_pyramid";
        
        /// <summary>可生成林地府邸的生物群系</summary>
        public const string HasStructureWoodlandMansion = "minecraft:has_structure/woodland_mansion";
        
        /// <summary>可生成海底神殿的生物群系</summary>
        public const string HasStructureOceanMonument = "minecraft:has_structure/ocean_monument";
        
        /// <summary>可生成要塞的生物群系</summary>
        public const string HasStructureStronghold = "minecraft:has_structure/stronghold";
        
        /// <summary>下界生物群系</summary>
        public const string IsNether = "minecraft:is_nether";
        
        /// <summary>末地生物群系</summary>
        public const string IsEnd = "minecraft:is_end";
        
        /// <summary>主世界生物群系</summary>
        public const string IsOverworld = "minecraft:is_overworld";
        
        /// <summary>海洋生物群系</summary>
        public const string IsOcean = "minecraft:is_ocean";
        
        /// <summary>河流生物群系</summary>
        public const string IsRiver = "minecraft:is_river";
        
        /// <summary>沙滩生物群系</summary>
        public const string IsBeach = "minecraft:is_beach";
    }
}

/// <summary>
/// 标签 JSON 数据模型
/// </summary>
public class TagDefinition
{
    /// <summary>
    /// 是否完全覆盖低优先级数据包中的同名标签（默认 false）
    /// </summary>
    public bool Replace { get; set; } = false;

    /// <summary>
    /// 标签包含的值列表
    /// </summary>
    public List<TagValue> Values { get; set; } = new();
}

/// <summary>
/// 标签值
/// </summary>
public class TagValue
{
    /// <summary>
    /// ID（可以是资源 ID 或标签引用 #namespace:tag）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 是否必须（默认 true）
    /// <para>如果为 false，ID 不存在时不会报错</para>
    /// </summary>
    public bool Required { get; set; } = true;
}

