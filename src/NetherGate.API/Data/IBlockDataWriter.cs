using NetherGate.API.Data.Models;
using NetherGate.API.Utilities;

namespace NetherGate.API.Data;

/// <summary>
/// 方块数据写入器接口
/// 用于修改容器（箱子、漏斗等）和方块实体的数据
/// ⚠️ 警告：修改方块数据需要谨慎，建议在服务器停止时操作或使用 RCON 命令
/// </summary>
public interface IBlockDataWriter
{
    // ========== 容器物品写入 ==========
    
    /// <summary>
    /// 设置箱子内的物品
    /// </summary>
    /// <param name="pos">箱子位置</param>
    /// <param name="items">物品列表</param>
    /// <param name="dimension">维度（默认主世界）</param>
    Task SetChestItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    /// <summary>
    /// 设置木桶内的物品
    /// </summary>
    Task SetBarrelItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    /// <summary>
    /// 设置潜影盒内的物品
    /// </summary>
    Task SetShulkerBoxItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    /// <summary>
    /// 设置漏斗内的物品
    /// </summary>
    Task SetHopperItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    /// <summary>
    /// 设置发射器内的物品
    /// </summary>
    Task SetDispenserItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    /// <summary>
    /// 设置投掷器内的物品
    /// </summary>
    Task SetDropperItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    /// <summary>
    /// 设置通用容器内的物品（自动识别容器类型）
    /// </summary>
    Task SetContainerItemsAsync(Position pos, List<ItemStack> items, string dimension = "overworld");
    
    // ========== 容器排序 ==========
    
    /// <summary>
    /// 排序容器内的物品
    /// </summary>
    /// <param name="pos">容器位置</param>
    /// <param name="mode">排序模式</param>
    /// <param name="descending">是否降序</param>
    /// <param name="dimension">维度</param>
    Task SortContainerAsync(Position pos, ItemSortMode mode, bool descending = false, string dimension = "overworld");
    
    /// <summary>
    /// 按 ID 排序容器
    /// </summary>
    Task SortContainerByIdAsync(Position pos, bool descending = false, string dimension = "overworld");
    
    /// <summary>
    /// 按数量排序容器
    /// </summary>
    Task SortContainerByCountAsync(Position pos, bool descending = false, string dimension = "overworld");
    
    /// <summary>
    /// 整理容器（合并相同物品，移除空槽位）
    /// </summary>
    Task ConsolidateContainerAsync(Position pos, string dimension = "overworld");
    
    // ========== 清空容器 ==========
    
    /// <summary>
    /// 清空容器内的所有物品
    /// </summary>
    Task ClearContainerAsync(Position pos, string dimension = "overworld");
    
    /// <summary>
    /// 移除容器中特定类型的物品
    /// </summary>
    Task RemoveItemFromContainerAsync(Position pos, string itemId, int? maxCount = null, string dimension = "overworld");
    
    // ========== 容器物品操作 ==========
    
    /// <summary>
    /// 向容器添加物品
    /// </summary>
    /// <param name="pos">容器位置</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="dimension">维度</param>
    /// <returns>是否成功添加（false 表示容器已满）</returns>
    Task<bool> AddItemToContainerAsync(Position pos, ItemStack item, string dimension = "overworld");
    
    /// <summary>
    /// 从容器取出物品
    /// </summary>
    /// <param name="pos">容器位置</param>
    /// <param name="slot">槽位</param>
    /// <param name="dimension">维度</param>
    /// <returns>取出的物品，如果槽位为空则返回 null</returns>
    Task<ItemStack?> TakeItemFromContainerAsync(Position pos, int slot, string dimension = "overworld");
    
    /// <summary>
    /// 交换容器中两个槽位的物品
    /// </summary>
    Task SwapContainerSlotsAsync(Position pos, int slot1, int slot2, string dimension = "overworld");
    
    // ========== 告示牌写入 ==========
    
    /// <summary>
    /// 设置告示牌文本
    /// </summary>
    /// <param name="pos">告示牌位置</param>
    /// <param name="lines">文本行（最多4行）</param>
    /// <param name="isGlowing">是否发光</param>
    /// <param name="color">染料颜色</param>
    /// <param name="dimension">维度</param>
    Task SetSignTextAsync(Position pos, List<string> lines, bool isGlowing = false, string? color = null, string dimension = "overworld");
    
    // ========== 方块实体通用写入 ==========
    
    /// <summary>
    /// 写入方块实体的 NBT 数据
    /// </summary>
    Task SetBlockEntityNbtAsync(Position pos, string nbtData, string dimension = "overworld");
    
    /// <summary>
    /// 更新方块实体的特定 NBT 标签
    /// </summary>
    Task UpdateBlockEntityTagAsync(Position pos, string tagPath, object value, string dimension = "overworld");
}

