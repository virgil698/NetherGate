using NetherGate.API.Data.Models;

namespace NetherGate.API.Data;

/// <summary>
/// 物品组件读取器（Minecraft 1.20.5+）
/// 用于读取使用数据组件格式存储的物品数据
/// </summary>
public interface IItemComponentReader
{
    /// <summary>
    /// 读取玩家背包所有物品的组件数据
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <returns>玩家背包组件数据，如果玩家不存在则返回 null</returns>
    Task<PlayerInventoryComponents?> ReadPlayerInventoryComponentsAsync(string playerName);

    /// <summary>
    /// 读取特定槽位的物品组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号（0-40）</param>
    /// <returns>物品组件数据，如果槽位为空则返回 null</returns>
    Task<ItemComponents?> ReadInventorySlotComponentsAsync(string playerName, int slot);

    /// <summary>
    /// 读取末影箱物品组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <returns>末影箱内所有物品的组件数据</returns>
    Task<List<ItemComponents>> ReadEnderChestComponentsAsync(string playerName);

    /// <summary>
    /// 检查物品是否有特定组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号</param>
    /// <param name="componentId">组件 ID（如 "minecraft:custom_name"）</param>
    /// <returns>如果物品存在且包含该组件则返回 true</returns>
    Task<bool> HasComponentAsync(string playerName, int slot, string componentId);

    /// <summary>
    /// 读取特定组件的值
    /// </summary>
    /// <typeparam name="T">组件值的类型</typeparam>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号</param>
    /// <param name="componentId">组件 ID</param>
    /// <returns>组件的值，如果不存在则返回 default(T)</returns>
    Task<T?> GetComponentValueAsync<T>(string playerName, int slot, string componentId);

    /// <summary>
    /// 读取方块实体组件
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="dimension">维度（默认为主世界）</param>
    /// <returns>方块实体组件数据，如果不存在则返回 null</returns>
    Task<BlockEntityComponents?> ReadBlockEntityComponentsAsync(int x, int y, int z, string dimension = "minecraft:overworld");

    /// <summary>
    /// 获取服务器 Minecraft 版本
    /// </summary>
    /// <returns>服务器版本</returns>
    Task<Version> GetServerVersionAsync();
}

