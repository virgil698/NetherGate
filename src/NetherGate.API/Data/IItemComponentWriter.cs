using NetherGate.API.Data.Models;

namespace NetherGate.API.Data;

/// <summary>
/// 物品组件写入器（Minecraft 1.20.5+）
/// 用于修改使用数据组件格式存储的物品数据
/// </summary>
public interface IItemComponentWriter
{
    /// <summary>
    /// 给予玩家物品（使用组件）
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="item">物品组件数据</param>
    /// <returns>是否成功给予物品</returns>
    Task<bool> GiveItemAsync(string playerName, ItemComponents item);

    /// <summary>
    /// 设置特定槽位的物品组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号（0-40）</param>
    /// <param name="item">物品组件数据</param>
    /// <returns>是否成功设置物品</returns>
    Task<bool> SetInventorySlotComponentsAsync(string playerName, int slot, ItemComponents item);

    /// <summary>
    /// 修改物品的特定组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号</param>
    /// <param name="componentId">组件 ID（如 "minecraft:custom_name"）</param>
    /// <param name="value">组件值</param>
    /// <returns>是否成功修改组件</returns>
    Task<bool> UpdateComponentAsync(string playerName, int slot, string componentId, object value);

    /// <summary>
    /// 移除物品的特定组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号</param>
    /// <param name="componentId">组件 ID</param>
    /// <returns>是否成功移除组件</returns>
    Task<bool> RemoveComponentAsync(string playerName, int slot, string componentId);

    /// <summary>
    /// 批量修改组件
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号</param>
    /// <param name="components">要修改的组件字典</param>
    /// <returns>是否成功修改所有组件</returns>
    Task<bool> UpdateComponentsAsync(string playerName, int slot, Dictionary<string, object> components);

    /// <summary>
    /// 清空特定槽位的物品
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="slot">槽位编号</param>
    /// <returns>是否成功清空槽位</returns>
    Task<bool> ClearSlotAsync(string playerName, int slot);
}

