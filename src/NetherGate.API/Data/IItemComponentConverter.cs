using NetherGate.API.Data.Models;

namespace NetherGate.API.Data;

/// <summary>
/// NBT 到数据组件的转换器
/// 用于在旧版 NBT 格式和新版组件格式之间转换
/// </summary>
public interface IItemComponentConverter
{
    /// <summary>
    /// 将 NBT 格式的物品数据转换为组件格式
    /// </summary>
    /// <param name="nbtData">NBT 数据（JSON 字符串）</param>
    /// <returns>转换后的物品组件</returns>
    ItemComponents ConvertFromNbt(string nbtData);

    /// <summary>
    /// 将组件格式的物品数据转换为 NBT 格式
    /// </summary>
    /// <param name="item">物品组件</param>
    /// <returns>NBT 数据（JSON 字符串）</returns>
    string ConvertToNbt(ItemComponents item);

    /// <summary>
    /// 批量转换玩家背包
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <returns>转换后的玩家背包组件数据</returns>
    Task<PlayerInventoryComponents?> ConvertPlayerInventoryAsync(string playerName);

    /// <summary>
    /// 检查服务器是否支持数据组件
    /// </summary>
    /// <returns>如果服务器版本 >= 1.20.5 则返回 true</returns>
    Task<bool> IsComponentSupportedAsync();
}

