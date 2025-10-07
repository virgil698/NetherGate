namespace NetherGate.API.Data.Models;

/// <summary>
/// 玩家背包组件数据（Minecraft 1.20.5+）
/// </summary>
public record PlayerInventoryComponents
{
    /// <summary>
    /// 玩家名称
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid? PlayerUuid { get; init; }

    /// <summary>
    /// 主背包物品（槽位 0-35）
    /// 包含快捷栏（0-8）和背包（9-35）
    /// </summary>
    public required List<ItemComponents> Inventory { get; init; } = new();

    /// <summary>
    /// 护甲栏（槽位 36-39）
    /// 36=靴子, 37=护腿, 38=胸甲, 39=头盔
    /// </summary>
    public required List<ItemComponents> Armor { get; init; } = new();

    /// <summary>
    /// 副手（槽位 40）
    /// </summary>
    public ItemComponents? Offhand { get; init; }

    /// <summary>
    /// 末影箱内容
    /// </summary>
    public List<ItemComponents>? EnderChest { get; init; }

    /// <summary>
    /// 服务器 Minecraft 版本
    /// </summary>
    public required string ServerVersion { get; init; }

    /// <summary>
    /// 数据读取时间
    /// </summary>
    public DateTime ReadTime { get; init; } = DateTime.UtcNow;

    // 便捷方法

    /// <summary>
    /// 获取所有物品（包含背包、护甲、副手）
    /// </summary>
    public IEnumerable<ItemComponents> GetAllItems()
    {
        foreach (var item in Inventory)
            yield return item;

        foreach (var item in Armor)
            yield return item;

        if (Offhand != null)
            yield return Offhand;
    }

    /// <summary>
    /// 根据槽位获取物品
    /// </summary>
    /// <param name="slot">槽位编号（0-40）</param>
    /// <returns>物品组件，如果槽位为空则返回 null</returns>
    public ItemComponents? GetItemBySlot(int slot)
    {
        if (slot >= 0 && slot <= 35)
        {
            return Inventory.FirstOrDefault(i => i.Slot == slot);
        }
        else if (slot >= 36 && slot <= 39)
        {
            return Armor.FirstOrDefault(i => i.Slot == slot);
        }
        else if (slot == 40)
        {
            return Offhand;
        }
        return null;
    }

    /// <summary>
    /// 查找特定物品
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <returns>所有匹配的物品</returns>
    public List<ItemComponents> FindItems(string itemId)
    {
        return GetAllItems().Where(i => i.Id == itemId).ToList();
    }

    /// <summary>
    /// 计算特定物品的总数量
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <returns>物品总数量</returns>
    public int CountItems(string itemId)
    {
        return GetAllItems().Where(i => i.Id == itemId).Sum(i => i.Count);
    }

    /// <summary>
    /// 查找第一个空槽位
    /// </summary>
    /// <returns>空槽位编号，如果背包已满则返回 -1</returns>
    public int FindEmptySlot()
    {
        for (int i = 0; i <= 35; i++)
        {
            if (GetItemBySlot(i) == null)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 检查背包是否已满
    /// </summary>
    public bool IsInventoryFull => FindEmptySlot() == -1;

    /// <summary>
    /// 获取快捷栏物品（槽位 0-8）
    /// </summary>
    public List<ItemComponents> GetHotbar()
    {
        return Inventory.Where(i => i.Slot >= 0 && i.Slot <= 8).ToList();
    }

    /// <summary>
    /// 获取主背包物品（槽位 9-35）
    /// </summary>
    public List<ItemComponents> GetMainInventory()
    {
        return Inventory.Where(i => i.Slot >= 9 && i.Slot <= 35).ToList();
    }

    /// <summary>
    /// 获取头盔
    /// </summary>
    public ItemComponents? GetHelmet() => Armor.FirstOrDefault(i => i.Slot == 39);

    /// <summary>
    /// 获取胸甲
    /// </summary>
    public ItemComponents? GetChestplate() => Armor.FirstOrDefault(i => i.Slot == 38);

    /// <summary>
    /// 获取护腿
    /// </summary>
    public ItemComponents? GetLeggings() => Armor.FirstOrDefault(i => i.Slot == 37);

    /// <summary>
    /// 获取靴子
    /// </summary>
    public ItemComponents? GetBoots() => Armor.FirstOrDefault(i => i.Slot == 36);
}

