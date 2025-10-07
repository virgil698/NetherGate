using NetherGate.API.Data;
using NetherGate.API.Data.Models;
using NetherGate.API.Logging;
using System.Text.Json;

namespace NetherGate.Core.Data;

/// <summary>
/// NBT 到数据组件转换器实现
/// </summary>
public class ItemComponentConverter : IItemComponentConverter
{
    private readonly ILogger _logger;
    private readonly ItemComponentReader _reader;
    private readonly PlayerDataReader _playerDataReader;

    // NBT 到组件的映射表
    private static readonly Dictionary<string, string> NbtToComponentMapping = new()
    {
        ["tag.Damage"] = "minecraft:damage",
        ["tag.display.Name"] = "minecraft:custom_name",
        ["tag.display.Lore"] = "minecraft:lore",
        ["tag.Enchantments"] = "minecraft:enchantments",
        ["tag.Unbreakable"] = "minecraft:unbreakable",
        ["tag.CustomModelData"] = "minecraft:custom_model_data",
        ["tag.AttributeModifiers"] = "minecraft:attribute_modifiers",
        ["tag.Trim"] = "minecraft:trim",
        ["tag.display.color"] = "minecraft:dyed_color",
        ["tag.BlockEntityTag"] = "minecraft:block_entity_data",
        ["tag.Items"] = "minecraft:container",
        ["tag.SkullOwner"] = "minecraft:profile",
        ["tag.HideFlags"] = "minecraft:hide_tooltip"
    };

    public ItemComponentConverter(
        ILogger logger,
        ItemComponentReader reader,
        PlayerDataReader playerDataReader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _playerDataReader = playerDataReader ?? throw new ArgumentNullException(nameof(playerDataReader));
    }

    /// <inheritdoc/>
    public ItemComponents ConvertFromNbt(string nbtData)
    {
        try
        {
            _logger.Debug("将 NBT 数据转换为组件格式");

            var nbtJson = JsonSerializer.Deserialize<Dictionary<string, object>>(nbtData);
            if (nbtJson == null)
            {
                throw new ArgumentException("无效的 NBT 数据");
            }

            var item = new ItemComponents
            {
                Id = GetNbtValue<string>(nbtJson, "id") ?? "minecraft:air",
                Count = GetNbtValue<int>(nbtJson, "Count", 1),
                Slot = GetNbtValue<int>(nbtJson, "Slot", -1),
                Components = ConvertNbtTagToComponents(nbtJson)
            };

            _logger.Debug($"成功转换物品: {item.Id}");
            return item;
        }
        catch (Exception ex)
        {
            _logger.Error($"NBT 转换失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public string ConvertToNbt(ItemComponents item)
    {
        try
        {
            _logger.Debug($"将组件格式转换为 NBT: {item.Id}");

            var nbtData = new Dictionary<string, object>
            {
                ["id"] = item.Id,
                ["Count"] = item.Count
            };

            if (item.Slot >= 0)
            {
                nbtData["Slot"] = item.Slot;
            }

            // 转换组件回 NBT tag
            var tag = ConvertComponentsToNbtTag(item.Components);
            if (tag.Count > 0)
            {
                nbtData["tag"] = tag;
            }

            var json = JsonSerializer.Serialize(nbtData);
            _logger.Debug("成功转换为 NBT 格式");
            return json;
        }
        catch (Exception ex)
        {
            _logger.Error($"组件转 NBT 失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PlayerInventoryComponents?> ConvertPlayerInventoryAsync(string playerName)
    {
        try
        {
            _logger.Info($"转换玩家 {playerName} 的背包数据");

            // 检查是否支持组件
            if (!await IsComponentSupportedAsync())
            {
                _logger.Warning("服务器不支持数据组件，无法转换");
                return null;
            }

            // 读取 NBT 格式的玩家数据
            var playerData = await _playerDataReader.ReadPlayerDataAsync(Guid.Empty); // TODO: 获取实际 UUID
            if (playerData == null)
            {
                _logger.Warning($"未找到玩家 {playerName} 的数据");
                return null;
            }

            // 转换为组件格式
            var inventory = playerData.Inventory
                .Select(ConvertItemStackToComponents)
                .ToList();

            var armor = new List<ItemComponents>();
            if (playerData.Armor.Helmet != null) armor.Add(ConvertItemStackToComponents(playerData.Armor.Helmet));
            if (playerData.Armor.Chestplate != null) armor.Add(ConvertItemStackToComponents(playerData.Armor.Chestplate));
            if (playerData.Armor.Leggings != null) armor.Add(ConvertItemStackToComponents(playerData.Armor.Leggings));
            if (playerData.Armor.Boots != null) armor.Add(ConvertItemStackToComponents(playerData.Armor.Boots));

            var enderChest = playerData.EnderChest
                .Select(ConvertItemStackToComponents)
                .ToList();

            var version = await _reader.GetServerVersionAsync();

            var result = new PlayerInventoryComponents
            {
                PlayerName = playerName,
                PlayerUuid = playerData.Uuid,
                Inventory = inventory,
                Armor = armor,
                EnderChest = enderChest,
                ServerVersion = version.ToString()
            };

            _logger.Info($"成功转换玩家背包数据，共 {inventory.Count} 个物品");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"转换玩家背包失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsComponentSupportedAsync()
    {
        try
        {
            var version = await _reader.GetServerVersionAsync();
            return version >= new Version(1, 20, 5);
        }
        catch (Exception ex)
        {
            _logger.Error($"检查组件支持失败: {ex.Message}", ex);
            return false;
        }
    }

    // 辅助方法

    /// <summary>
    /// 将 NBT tag 转换为组件
    /// </summary>
    private Dictionary<string, object> ConvertNbtTagToComponents(Dictionary<string, object> nbtJson)
    {
        var components = new Dictionary<string, object>();

        if (!nbtJson.TryGetValue("tag", out var tagObj) || tagObj is not Dictionary<string, object> tag)
        {
            return components;
        }

        // 损坏值
        if (tag.TryGetValue("Damage", out var damage))
        {
            components["minecraft:damage"] = damage;
        }

        // 自定义名称
        if (tag.TryGetValue("display", out var displayObj) && displayObj is Dictionary<string, object> display)
        {
            if (display.TryGetValue("Name", out var name))
            {
                components["minecraft:custom_name"] = name;
            }

            if (display.TryGetValue("Lore", out var lore))
            {
                components["minecraft:lore"] = lore;
            }

            if (display.TryGetValue("color", out var color))
            {
                components["minecraft:dyed_color"] = new Dictionary<string, object> { ["rgb"] = color };
            }
        }

        // 附魔
        if (tag.TryGetValue("Enchantments", out var enchantments))
        {
            components["minecraft:enchantments"] = ConvertNbtEnchantmentsToComponent(enchantments);
        }

        // 不可破坏
        if (tag.TryGetValue("Unbreakable", out var unbreakable) && (bool)unbreakable)
        {
            components["minecraft:unbreakable"] = new Dictionary<string, bool> { ["show_in_tooltip"] = true };
        }

        // 自定义模型数据
        if (tag.TryGetValue("CustomModelData", out var customModelData))
        {
            components["minecraft:custom_model_data"] = customModelData;
        }

        // 属性修饰符
        if (tag.TryGetValue("AttributeModifiers", out var attributeModifiers))
        {
            components["minecraft:attribute_modifiers"] = new Dictionary<string, object>
            {
                ["modifiers"] = attributeModifiers
            };
        }

        // 盔甲纹饰
        if (tag.TryGetValue("Trim", out var trim))
        {
            components["minecraft:trim"] = trim;
        }

        return components;
    }

    /// <summary>
    /// 将组件转换为 NBT tag
    /// </summary>
    private Dictionary<string, object> ConvertComponentsToNbtTag(Dictionary<string, object> components)
    {
        var tag = new Dictionary<string, object>();

        // 损坏值
        if (components.TryGetValue("minecraft:damage", out var damage))
        {
            tag["Damage"] = damage;
        }

        // 自定义名称和描述
        var display = new Dictionary<string, object>();
        if (components.TryGetValue("minecraft:custom_name", out var customName))
        {
            display["Name"] = customName;
        }
        if (components.TryGetValue("minecraft:lore", out var lore))
        {
            display["Lore"] = lore;
        }
        if (components.TryGetValue("minecraft:dyed_color", out var dyedColorObj) 
            && dyedColorObj is Dictionary<string, object> dyedColor
            && dyedColor.TryGetValue("rgb", out var rgb))
        {
            display["color"] = rgb;
        }
        if (display.Count > 0)
        {
            tag["display"] = display;
        }

        // 附魔
        if (components.TryGetValue("minecraft:enchantments", out var enchantments))
        {
            tag["Enchantments"] = ConvertComponentEnchantmentsToNbt(enchantments);
        }

        // 不可破坏
        if (components.ContainsKey("minecraft:unbreakable"))
        {
            tag["Unbreakable"] = true;
        }

        // 自定义模型数据
        if (components.TryGetValue("minecraft:custom_model_data", out var customModelData))
        {
            tag["CustomModelData"] = customModelData;
        }

        return tag;
    }

    /// <summary>
    /// 转换 NBT 附魔格式到组件格式
    /// </summary>
    private Dictionary<string, object> ConvertNbtEnchantmentsToComponent(object enchantments)
    {
        var levels = new Dictionary<string, int>();

        if (enchantments is List<object> enchList)
        {
            foreach (var ench in enchList)
            {
                if (ench is Dictionary<string, object> enchDict)
                {
                    var id = GetNbtValue<string>(enchDict, "id") ?? "";
                    var level = GetNbtValue<int>(enchDict, "lvl", 1);
                    levels[id] = level;
                }
            }
        }

        return new Dictionary<string, object>
        {
            ["levels"] = levels,
            ["show_in_tooltip"] = true
        };
    }

    /// <summary>
    /// 转换组件附魔格式到 NBT 格式
    /// </summary>
    private List<Dictionary<string, object>> ConvertComponentEnchantmentsToNbt(object enchantments)
    {
        var result = new List<Dictionary<string, object>>();

        if (enchantments is Dictionary<string, object> enchDict 
            && enchDict.TryGetValue("levels", out var levelsObj)
            && levelsObj is Dictionary<string, int> levels)
        {
            foreach (var (id, level) in levels)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["id"] = id,
                    ["lvl"] = level
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 将旧的 ItemStack 转换为 ItemComponents
    /// </summary>
    private ItemComponents ConvertItemStackToComponents(ItemStack itemStack)
    {
        var components = new Dictionary<string, object>();

        // 自定义名称
        if (!string.IsNullOrEmpty(itemStack.CustomName))
        {
            components["minecraft:custom_name"] = itemStack.CustomName;
        }

        // 附魔
        if (itemStack.Enchantments.Count > 0)
        {
            var levels = itemStack.Enchantments.ToDictionary(e => e.Id, e => e.Level);
            components["minecraft:enchantments"] = new Dictionary<string, object>
            {
                ["levels"] = levels,
                ["show_in_tooltip"] = true
            };
        }

        return new ItemComponents
        {
            Id = itemStack.Id,
            Count = itemStack.Count,
            Slot = itemStack.Slot,
            Components = components
        };
    }

    /// <summary>
    /// 从 NBT JSON 中获取值
    /// </summary>
    private T? GetNbtValue<T>(Dictionary<string, object> nbt, string key, T? defaultValue = default)
    {
        if (nbt.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}

