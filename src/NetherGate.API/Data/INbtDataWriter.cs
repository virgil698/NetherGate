using fNbt;

namespace NetherGate.API.Data;

/// <summary>
/// NBT 数据写入器接口
/// 用于编辑和写入 NBT 数据
/// </summary>
public interface INbtDataWriter
{
    // ========== 玩家数据写入 ==========
    
    /// <summary>
    /// 更新玩家位置
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="dimension">维度（可选）</param>
    Task UpdatePlayerPositionAsync(Guid playerUuid, double x, double y, double z, string? dimension = null);
    
    /// <summary>
    /// 更新玩家生命值
    /// </summary>
    Task UpdatePlayerHealthAsync(Guid playerUuid, float health);
    
    /// <summary>
    /// 更新玩家饱食度
    /// </summary>
    Task UpdatePlayerFoodLevelAsync(Guid playerUuid, int foodLevel, float saturation);
    
    /// <summary>
    /// 更新玩家经验
    /// </summary>
    Task UpdatePlayerExperienceAsync(Guid playerUuid, int level, float progress, int total);
    
    /// <summary>
    /// 更新玩家游戏模式
    /// </summary>
    Task UpdatePlayerGameModeAsync(Guid playerUuid, GameMode gameMode);
    
    /// <summary>
    /// 添加物品到玩家背包
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="item">物品信息</param>
    /// <param name="slot">背包槽位（可选，-1 表示自动寻找空位）</param>
    Task AddItemToInventoryAsync(Guid playerUuid, ItemStack item, int slot = -1);
    
    /// <summary>
    /// 从玩家背包移除物品
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="slot">背包槽位</param>
    Task RemoveItemFromInventoryAsync(Guid playerUuid, int slot);
    
    /// <summary>
    /// 更新玩家背包物品
    /// </summary>
    Task UpdateInventoryItemAsync(Guid playerUuid, int slot, ItemStack item);
    
    /// <summary>
    /// 清空玩家背包
    /// </summary>
    Task ClearPlayerInventoryAsync(Guid playerUuid);
    
    /// <summary>
    /// 给予玩家状态效果
    /// </summary>
    Task AddStatusEffectAsync(Guid playerUuid, StatusEffect effect);
    
    /// <summary>
    /// 移除玩家状态效果
    /// </summary>
    Task RemoveStatusEffectAsync(Guid playerUuid, int effectId);
    
    /// <summary>
    /// 更新玩家盔甲
    /// </summary>
    Task UpdatePlayerArmorAsync(Guid playerUuid, PlayerArmor armor);
    
    /// <summary>
    /// 直接写入自定义 NBT 数据到玩家文件
    /// </summary>
    /// <param name="playerUuid">玩家 UUID</param>
    /// <param name="nbtModifier">NBT 修改函数</param>
    Task ModifyPlayerNbtAsync(Guid playerUuid, Action<NbtCompound> nbtModifier);
    
    // ========== 世界数据写入 ==========
    
    /// <summary>
    /// 更新世界出生点
    /// </summary>
    Task UpdateWorldSpawnAsync(string worldName, int x, int y, int z);
    
    /// <summary>
    /// 更新世界边界
    /// </summary>
    Task UpdateWorldBorderAsync(string worldName, double centerX, double centerZ, double size, double damagePerBlock, double warningDistance);
    
    /// <summary>
    /// 更新游戏规则
    /// </summary>
    Task UpdateGameRuleAsync(string worldName, string ruleName, string value);
    
    /// <summary>
    /// 更新世界时间
    /// </summary>
    Task UpdateWorldTimeAsync(string worldName, long dayTime, long gameTime);
    
    /// <summary>
    /// 更新世界天气
    /// </summary>
    Task UpdateWorldWeatherAsync(string worldName, bool raining, int rainTime, bool thundering, int thunderTime);
    
    /// <summary>
    /// 直接写入自定义 NBT 数据到世界文件
    /// </summary>
    /// <param name="worldName">世界名称</param>
    /// <param name="nbtModifier">NBT 修改函数</param>
    Task ModifyWorldNbtAsync(string worldName, Action<NbtCompound> nbtModifier);
    
    // ========== 实体数据写入 ==========
    
    /// <summary>
    /// 创建实体 NBT
    /// </summary>
    /// <param name="entityType">实体类型（如 "minecraft:zombie"）</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="customNbt">自定义 NBT 数据</param>
    /// <returns>实体 NBT 复合标签</returns>
    NbtCompound CreateEntityNbt(string entityType, double x, double y, double z, NbtCompound? customNbt = null);
    
    /// <summary>
    /// 创建物品 NBT
    /// </summary>
    /// <param name="itemId">物品 ID（如 "minecraft:diamond_sword"）</param>
    /// <param name="count">数量</param>
    /// <param name="enchantments">附魔列表（可选）</param>
    /// <param name="customName">自定义名称（可选）</param>
    /// <param name="lore">描述文本（可选）</param>
    /// <param name="customNbt">自定义 NBT 数据（可选）</param>
    /// <returns>物品 NBT 复合标签</returns>
    NbtCompound CreateItemNbt(
        string itemId, 
        int count = 1, 
        List<Enchantment>? enchantments = null,
        string? customName = null,
        List<string>? lore = null,
        NbtCompound? customNbt = null);
    
    // ========== 通用 NBT 操作 ==========
    
    /// <summary>
    /// 从文件读取 NBT 数据
    /// </summary>
    Task<NbtCompound?> ReadNbtFileAsync(string filePath);
    
    /// <summary>
    /// 写入 NBT 数据到文件
    /// </summary>
    Task WriteNbtFileAsync(string filePath, NbtCompound nbt, bool backup = true);
    
    /// <summary>
    /// 验证 NBT 数据结构
    /// </summary>
    bool ValidateNbt(NbtCompound nbt, string expectedRootTag);
}
