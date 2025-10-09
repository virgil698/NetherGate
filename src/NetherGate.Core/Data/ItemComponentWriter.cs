using NetherGate.API.Data;
using NetherGate.API.Data.Models;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using NetherGate.Core.Utilities;
using System.Text;
using System.Text.Json;

namespace NetherGate.Core.Data;

/// <summary>
/// 物品组件写入器实现（Minecraft 1.20.5+）
/// 使用 RCON 命令修改在线玩家物品数据
/// </summary>
public class ItemComponentWriter : IItemComponentWriter
{
    private readonly IRconClient _rconClient;
    private readonly ILogger _logger;
    private readonly ItemComponentReader _reader;

    public ItemComponentWriter(
        IRconClient rconClient,
        ILogger logger,
        ItemComponentReader reader)
    {
        _rconClient = rconClient ?? throw new ArgumentNullException(nameof(rconClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <inheritdoc/>
    public async Task<bool> GiveItemAsync(string playerName, ItemComponents item)
    {
        try
        {
            _logger.Debug($"给予玩家 {playerName} 物品: {item.Id} x{item.Count}");

            // 检查服务器版本
            var version = await _reader.GetServerVersionAsync();
            if (version < new Version(1, 20, 5))
            {
                _logger.Warning($"服务器版本 {version} 不支持数据组件");
                return false;
            }

            // 构建 /give 命令，包含组件数据
            var command = BuildGiveCommand(playerName, item);
            var response = await _rconClient.ExecuteCommandAsync(command);

            _logger.Info($"成功给予玩家 {playerName} 物品: {item.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"给予物品失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetInventorySlotComponentsAsync(string playerName, int slot, ItemComponents item)
    {
        try
        {
            if (slot < 0 || slot > 40)
            {
                _logger.Warning($"无效的槽位编号: {slot}");
                return false;
            }

            _logger.Debug($"设置玩家 {playerName} 槽位 {slot} 的物品: {item.Id}");

            // 使用 /item replace 命令
            var command = BuildReplaceCommand(playerName, slot, item);
            var response = await _rconClient.ExecuteCommandAsync(command);

            _logger.Info($"成功设置槽位 {slot} 的物品");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"设置槽位物品失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateComponentAsync(string playerName, int slot, string componentId, object value)
    {
        try
        {
            _logger.Debug($"修改玩家 {playerName} 槽位 {slot} 的组件 {componentId}");

            // 使用 /item modify 命令
            var command = BuildModifyCommand(playerName, slot, componentId, value);
            var response = await _rconClient.ExecuteCommandAsync(command);

            _logger.Info($"成功修改组件 {componentId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"修改组件失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveComponentAsync(string playerName, int slot, string componentId)
    {
        try
        {
            _logger.Debug($"移除玩家 {playerName} 槽位 {slot} 的组件 {componentId}");

            // 使用 /item modify 命令移除组件（前缀 ! 表示移除）
            var slotName = GetSlotName(slot);
            var command = $"item modify entity {playerName} {slotName} {{\"function\":\"set_components\",\"components\":{{\"{componentId}\":{{}}}}}}";
            
            var response = await _rconClient.ExecuteCommandAsync(command);

            _logger.Info($"成功移除组件 {componentId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"移除组件失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateComponentsAsync(string playerName, int slot, Dictionary<string, object> components)
    {
        try
        {
            _logger.Debug($"批量修改玩家 {playerName} 槽位 {slot} 的 {components.Count} 个组件");

            // 批量修改多个组件
            foreach (var (componentId, value) in components)
            {
                var success = await UpdateComponentAsync(playerName, slot, componentId, value);
                if (!success)
                {
                    _logger.Warning($"批量修改中，组件 {componentId} 修改失败");
                    return false;
                }
            }

            _logger.Info($"成功批量修改 {components.Count} 个组件");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"批量修改组件失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClearSlotAsync(string playerName, int slot)
    {
        try
        {
            _logger.Debug($"清空玩家 {playerName} 槽位 {slot}");

            var slotName = GetSlotName(slot);
            var command = $"item replace entity {playerName} {slotName} with air";
            
            var response = await _rconClient.ExecuteCommandAsync(command);

            _logger.Info($"成功清空槽位 {slot}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"清空槽位失败: {ex.Message}", ex);
            return false;
        }
    }

    // 辅助方法

    /// <summary>
    /// 构建 /give 命令
    /// </summary>
    private string BuildGiveCommand(string playerName, ItemComponents item)
    {
        var componentsSnbt = BuildComponentsSnbt(item.Components);
        if (string.IsNullOrEmpty(componentsSnbt))
        {
            return $"give {playerName} {item.Id} {item.Count}";
        }
        return $"give {playerName} {item.Id}[{componentsSnbt}] {item.Count}";
    }

    /// <summary>
    /// 构建 /item replace 命令
    /// </summary>
    private string BuildReplaceCommand(string playerName, int slot, ItemComponents item)
    {
        var slotName = GetSlotName(slot);
        var componentsSnbt = BuildComponentsSnbt(item.Components);
        
        if (string.IsNullOrEmpty(componentsSnbt))
        {
            return $"item replace entity {playerName} {slotName} with {item.Id} {item.Count}";
        }
        return $"item replace entity {playerName} {slotName} with {item.Id}[{componentsSnbt}] {item.Count}";
    }

    /// <summary>
    /// 构建 /item modify 命令
    /// </summary>
    private string BuildModifyCommand(string playerName, int slot, string componentId, object value)
    {
        var slotName = GetSlotName(slot);
        var valueSnbt = ConvertToSnbt(value);
        
        // Minecraft 1.20.5+ 使用的组件语法
        return $"item modify entity {playerName} {slotName} {{function:\"minecraft:set_components\",components:{{\"{componentId}\":{valueSnbt}}}}}";
    }

    /// <summary>
    /// 获取槽位名称
    /// </summary>
    private string GetSlotName(int slot)
    {
        return slot switch
        {
            >= 0 and <= 8 => $"hotbar.{slot}",           // 快捷栏
            >= 9 and <= 35 => $"inventory.{slot - 9}",    // 背包
            36 => "armor.feet",                           // 靴子
            37 => "armor.legs",                           // 护腿
            38 => "armor.chest",                          // 胸甲
            39 => "armor.head",                           // 头盔
            40 => "weapon.offhand",                       // 副手
            _ => throw new ArgumentException($"无效的槽位: {slot}")
        };
    }

    /// <summary>
    /// 将组件字典转换为 SNBT 格式字符串
    /// </summary>
    private string BuildComponentsSnbt(Dictionary<string, object> components)
    {
        if (components.Count == 0)
            return "";

        var sb = new StringBuilder();
        var first = true;

        foreach (var (key, value) in components)
        {
            if (!first)
                sb.Append(',');
            first = false;

            // 转义组件 ID（去除命名空间前缀）
            var componentKey = key;
            if (key.StartsWith("minecraft:"))
                componentKey = key.Substring("minecraft:".Length);

            sb.Append(componentKey);
            sb.Append('=');
            sb.Append(ConvertToSnbt(value));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 将 C# 对象转换为 SNBT (Stringified NBT) 格式
    /// </summary>
    private string ConvertToSnbt(object value)
    {
        return value switch
        {
            null => "null",
            string str => StringEscapeUtils.QuoteSnbt(str),
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            long l => $"{l}L",
            float f => $"{f}f",
            double d => $"{d}d",
            byte by => $"{by}b",
            short s => $"{s}s",
            
            Dictionary<string, object> dict => BuildSnbtCompound(dict),
            List<object> list => BuildSnbtList(list),
            
            IEnumerable<object> enumerable => BuildSnbtList(enumerable.ToList()),
            
            _ => JsonSerializer.Serialize(value)
        };
    }

    /// <summary>
    /// 构建 SNBT 复合标签（字典）
    /// </summary>
    private string BuildSnbtCompound(Dictionary<string, object> dict)
    {
        var sb = new StringBuilder("{");
        var first = true;

        foreach (var (key, value) in dict)
        {
            if (!first)
                sb.Append(',');
            first = false;

            sb.Append(key);
            sb.Append(':');
            sb.Append(ConvertToSnbt(value));
        }

        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// 构建 SNBT 列表
    /// </summary>
    private string BuildSnbtList(List<object> list)
    {
        var sb = new StringBuilder("[");
        var first = true;

        foreach (var item in list)
        {
            if (!first)
                sb.Append(',');
            first = false;

            sb.Append(ConvertToSnbt(item));
        }

        sb.Append(']');
        return sb.ToString();
    }

    // SNBT 转义功能已移至 StringEscapeUtils 工具类
}

