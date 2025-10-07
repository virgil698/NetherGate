using fNbt;
using NetherGate.API.Data;
using NetherGate.API.Data.Models;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetherGate.Core.Data;

/// <summary>
/// 物品组件读取器实现（Minecraft 1.20.5+）
/// 支持从文件系统读取离线数据和通过 RCON 读取在线数据
/// </summary>
public class ItemComponentReader : IItemComponentReader
{
    private readonly string _serverDirectory;
    private readonly IRconClient? _rconClient;
    private readonly ILogger _logger;
    private readonly Dictionary<string, Guid> _playerNameToUuid = new();
    private Version? _cachedVersion;

    public ItemComponentReader(
        string serverDirectory,
        IRconClient? rconClient,
        ILogger logger)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _rconClient = rconClient;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PlayerInventoryComponents?> ReadPlayerInventoryComponentsAsync(string playerName)
    {
        try
        {
            _logger.Debug($"读取玩家 {playerName} 的背包组件数据");

            var version = await GetServerVersionAsync();
            
            // 检查玩家是否在线
            var isOnline = await IsPlayerOnlineAsync(playerName);
            
            PlayerInventoryComponents? result;
            
            if (isOnline && _rconClient != null)
            {
                // 在线玩家：使用 RCON 命令获取实时数据
                _logger.Debug($"玩家 {playerName} 在线，使用 RCON 读取数据");
                result = await ReadOnlinePlayerInventoryAsync(playerName);
            }
            else
            {
                // 离线玩家：从文件系统读取
                _logger.Debug($"玩家 {playerName} 离线，从文件系统读取数据");
                result = await ReadOfflinePlayerInventoryAsync(playerName);
            }

            if (result != null)
            {
                result = result with { ServerVersion = version.ToString() };
                _logger.Info($"成功读取玩家 {playerName} 的背包组件数据（共 {result.Inventory.Count} 个物品）");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取玩家背包组件数据失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ItemComponents?> ReadInventorySlotComponentsAsync(string playerName, int slot)
    {
        try
        {
            if (slot < 0 || slot > 40)
            {
                _logger.Warning($"无效的槽位编号: {slot}（有效范围 0-40）");
                return null;
            }

            _logger.Debug($"读取玩家 {playerName} 槽位 {slot} 的物品组件");

            var inventory = await ReadPlayerInventoryComponentsAsync(playerName);
            return inventory?.GetItemBySlot(slot);
        }
        catch (Exception ex)
        {
            _logger.Error($"读取槽位物品组件失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ItemComponents>> ReadEnderChestComponentsAsync(string playerName)
    {
        try
        {
            _logger.Debug($"读取玩家 {playerName} 的末影箱组件数据");

            var playerUuid = await GetPlayerUuidAsync(playerName);
            if (playerUuid == Guid.Empty)
            {
                _logger.Warning($"未找到玩家 {playerName} 的 UUID");
                return new List<ItemComponents>();
            }

            var playerDataPath = GetPlayerDataFilePath(playerUuid);
            if (!File.Exists(playerDataPath))
            {
                _logger.Debug($"玩家数据文件不存在: {playerDataPath}");
                return new List<ItemComponents>();
            }

            return await Task.Run(() =>
            {
                var nbtFile = new NbtFile();
                nbtFile.LoadFromFile(playerDataPath);

                var enderItems = nbtFile.RootTag.Get<NbtList>("EnderItems");
                if (enderItems == null)
                    return new List<ItemComponents>();

                var items = new List<ItemComponents>();
                foreach (var itemTag in enderItems.Cast<NbtCompound>())
                {
                    var item = ParseItemFromNbt(itemTag);
                    if (item != null)
                        items.Add(item);
                }

                return items;
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"读取末影箱组件数据失败: {ex.Message}", ex);
            return new List<ItemComponents>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasComponentAsync(string playerName, int slot, string componentId)
    {
        try
        {
            var item = await ReadInventorySlotComponentsAsync(playerName, slot);
            return item?.HasComponent(componentId) ?? false;
        }
        catch (Exception ex)
        {
            _logger.Error($"检查组件存在性失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetComponentValueAsync<T>(string playerName, int slot, string componentId)
    {
        try
        {
            var item = await ReadInventorySlotComponentsAsync(playerName, slot);
            return item != null ? item.GetComponent<T>(componentId) : default;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取组件值失败: {ex.Message}", ex);
            return default;
        }
    }

    /// <inheritdoc/>
    public Task<BlockEntityComponents?> ReadBlockEntityComponentsAsync(
        int x, int y, int z, string dimension = "minecraft:overworld")
    {
        try
        {
            _logger.Debug($"读取方块实体组件: [{x}, {y}, {z}] in {dimension}");

            // TODO: 实现区块文件解析，读取方块实体数据
            // 需要解析 region 文件（Anvil 格式）
            
            _logger.Warning("方块实体组件读取功能尚未实现");
            return Task.FromResult<BlockEntityComponents?>(null);
        }
        catch (Exception ex)
        {
            _logger.Error($"读取方块实体组件失败: {ex.Message}", ex);
            return Task.FromResult<BlockEntityComponents?>(null);
        }
    }

    /// <inheritdoc/>
    public async Task<Version> GetServerVersionAsync()
    {
        if (_cachedVersion != null)
            return _cachedVersion;

        try
        {
            // 尝试读取 version.json
            var versionFilePath = Path.Combine(_serverDirectory, "version.json");
            if (File.Exists(versionFilePath))
            {
                var json = await File.ReadAllTextAsync(versionFilePath);
                var versionData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (versionData != null && versionData.TryGetValue("name", out var nameElement))
                {
                    var versionString = nameElement.GetString();
                    if (versionString != null)
                    {
                        // 解析版本号（如 "1.21.9" 或 "1.21"）
                        var match = Regex.Match(versionString, @"(\d+)\.(\d+)(?:\.(\d+))?");
                        if (match.Success)
                        {
                            var major = int.Parse(match.Groups[1].Value);
                            var minor = int.Parse(match.Groups[2].Value);
                            var build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                            
                            _cachedVersion = new Version(major, minor, build);
                            _logger.Info($"检测到服务器版本: {_cachedVersion}");
                            return _cachedVersion;
                        }
                    }
                }
            }

            // 默认假设是较新版本
            _cachedVersion = new Version(1, 21, 9);
            _logger.Debug($"无法检测服务器版本，使用默认值: {_cachedVersion}");
            return _cachedVersion;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取服务器版本失败: {ex.Message}", ex);
            _cachedVersion = new Version(1, 21, 9);
            return _cachedVersion;
        }
    }

    // 私有辅助方法

    /// <summary>
    /// 从文件系统读取离线玩家背包数据
    /// </summary>
    private async Task<PlayerInventoryComponents?> ReadOfflinePlayerInventoryAsync(string playerName)
    {
        var playerUuid = await GetPlayerUuidAsync(playerName);
        if (playerUuid == Guid.Empty)
        {
            _logger.Warning($"未找到玩家 {playerName} 的 UUID");
            return null;
        }

        var playerDataPath = GetPlayerDataFilePath(playerUuid);
        if (!File.Exists(playerDataPath))
        {
            _logger.Debug($"玩家数据文件不存在: {playerDataPath}");
            return null;
        }

        return await Task.Run(() =>
        {
            var nbtFile = new NbtFile();
            nbtFile.LoadFromFile(playerDataPath);

            return ParsePlayerInventoryFromNbt(nbtFile.RootTag, playerName, playerUuid);
        });
    }

    /// <summary>
    /// 使用 RCON 命令读取在线玩家背包数据
    /// </summary>
    private async Task<PlayerInventoryComponents?> ReadOnlinePlayerInventoryAsync(string playerName)
    {
        if (_rconClient == null)
        {
            _logger.Warning("RCON 客户端未配置，无法读取在线玩家数据");
            return null;
        }

        try
        {
            // 使用 /data get 命令获取玩家数据
            var response = await _rconClient.ExecuteCommandAsync($"data get entity {playerName} Inventory");
            
            if (string.IsNullOrEmpty(response) || response.Contains("No entity was found"))
            {
                _logger.Warning($"无法获取玩家 {playerName} 的在线数据");
                return null;
            }

            // 解析 SNBT 格式的响应
            // TODO: 实现 SNBT 解析器
            _logger.Warning("SNBT 解析功能尚未实现，回退到文件系统读取");
            return await ReadOfflinePlayerInventoryAsync(playerName);
        }
        catch (Exception ex)
        {
            _logger.Error($"使用 RCON 读取在线玩家数据失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 解析 NBT 数据为玩家背包组件
    /// </summary>
    private PlayerInventoryComponents ParsePlayerInventoryFromNbt(NbtCompound rootTag, string playerName, Guid playerUuid)
    {
        var allItems = new List<ItemComponents>();
        
        // 读取背包数据
        var inventoryList = rootTag.Get<NbtList>("Inventory");
        if (inventoryList != null)
        {
            foreach (var itemTag in inventoryList.Cast<NbtCompound>())
            {
                var item = ParseItemFromNbt(itemTag);
                if (item != null)
                    allItems.Add(item);
            }
        }

        // 分离背包、护甲、副手
        var inventory = allItems.Where(i => i.Slot >= 0 && i.Slot <= 35).ToList();
        var armor = allItems.Where(i => i.Slot >= 100 && i.Slot <= 103)
            .Select(i => i with { Slot = i.Slot - 64 }) // 转换槽位编号：100-103 -> 36-39
            .ToList();
        var offhand = allItems.FirstOrDefault(i => i.Slot == -106);
        if (offhand != null)
        {
            offhand = offhand with { Slot = 40 }; // 副手槽位为 40
        }

        return new PlayerInventoryComponents
        {
            PlayerName = playerName,
            PlayerUuid = playerUuid,
            Inventory = inventory,
            Armor = armor,
            Offhand = offhand,
            ServerVersion = "unknown"
        };
    }

    /// <summary>
    /// 解析单个物品的 NBT 数据为组件格式
    /// </summary>
    private ItemComponents? ParseItemFromNbt(NbtCompound itemTag)
    {
        // 获取物品 ID
        var id = itemTag.Get<NbtString>("id")?.Value;
        if (string.IsNullOrEmpty(id))
            return null;

        // 获取数量和槽位
        var count = itemTag.Get<NbtByte>("count")?.Value ?? itemTag.Get<NbtByte>("Count")?.Value ?? 1;
        var slot = itemTag.Get<NbtByte>("Slot")?.Value ?? -1;

        var components = new Dictionary<string, object>();

        // 检查是否为 1.20.5+ 格式（使用 components）
        var componentsTag = itemTag.Get<NbtCompound>("components");
        if (componentsTag != null)
        {
            // 直接解析组件
            foreach (var component in componentsTag)
            {
                if (string.IsNullOrEmpty(component.Name))
                    continue;
                    
                var value = ParseNbtValue(component);
                if (value != null)
                    components[component.Name] = value;
            }
        }
        else
        {
            // 1.20.4- 格式（使用 tag），需要转换
            var tag = itemTag.Get<NbtCompound>("tag");
            if (tag != null)
            {
                components = ConvertNbtTagToComponents(tag);
            }
        }

        return new ItemComponents
        {
            Id = id,
            Count = count,
            Slot = slot,
            Components = components
        };
    }

    /// <summary>
    /// 将旧版 NBT tag 转换为组件格式
    /// </summary>
    private Dictionary<string, object> ConvertNbtTagToComponents(NbtCompound tag)
    {
        var components = new Dictionary<string, object>();

        // 损坏值
        var damage = tag.Get<NbtInt>("Damage");
        if (damage != null)
            components["minecraft:damage"] = damage.Value;

        // 自定义名称和描述
        var display = tag.Get<NbtCompound>("display");
        if (display != null)
        {
            var name = display.Get<NbtString>("Name");
            if (name != null)
                components["minecraft:custom_name"] = name.Value;

            var lore = display.Get<NbtList>("Lore");
            if (lore != null)
            {
                var loreList = lore.Cast<NbtString>().Select(s => s.Value).ToList();
                components["minecraft:lore"] = loreList;
            }

            var color = display.Get<NbtInt>("color");
            if (color != null)
                components["minecraft:dyed_color"] = new Dictionary<string, object> { ["rgb"] = color.Value };
        }

        // 附魔
        var enchantments = tag.Get<NbtList>("Enchantments");
        if (enchantments != null && enchantments.Count > 0)
        {
            var levels = new Dictionary<string, int>();
            foreach (var ench in enchantments.Cast<NbtCompound>())
            {
                var enchId = ench.Get<NbtString>("id")?.Value;
                var level = ench.Get<NbtShort>("lvl")?.Value ?? 0;
                if (enchId != null)
                    levels[enchId] = level;
            }
            components["minecraft:enchantments"] = new Dictionary<string, object>
            {
                ["levels"] = levels,
                ["show_in_tooltip"] = true
            };
        }

        // 不可破坏
        var unbreakable = tag.Get<NbtByte>("Unbreakable");
        if (unbreakable != null && unbreakable.Value != 0)
            components["minecraft:unbreakable"] = new Dictionary<string, bool> { ["show_in_tooltip"] = true };

        // 自定义模型数据
        var customModelData = tag.Get<NbtInt>("CustomModelData");
        if (customModelData != null)
            components["minecraft:custom_model_data"] = customModelData.Value;

        // 属性修饰符
        var attributeModifiers = tag.Get<NbtList>("AttributeModifiers");
        if (attributeModifiers != null && attributeModifiers.Count > 0)
        {
            var modifiers = new List<Dictionary<string, object>>();
            foreach (var attr in attributeModifiers.Cast<NbtCompound>())
            {
                modifiers.Add(ParseNbtCompoundToDictionary(attr));
            }
            components["minecraft:attribute_modifiers"] = new Dictionary<string, object>
            {
                ["modifiers"] = modifiers
            };
        }

        // 盔甲纹饰
        var trim = tag.Get<NbtCompound>("Trim");
        if (trim != null)
            components["minecraft:trim"] = ParseNbtCompoundToDictionary(trim);

        // 自定义数据（保留原始 NBT）
        var customData = new Dictionary<string, object>();
        foreach (var nbtTag in tag)
        {
            if (string.IsNullOrEmpty(nbtTag.Name))
                continue;
                
            if (!IsStandardNbtTag(nbtTag.Name))
            {
                var value = ParseNbtValue(nbtTag);
                if (value != null)
                    customData[nbtTag.Name] = value;
            }
        }
        if (customData.Count > 0)
            components["minecraft:custom_data"] = customData;

        return components;
    }

    /// <summary>
    /// 解析 NBT 值为 C# 对象
    /// </summary>
    private object? ParseNbtValue(NbtTag tag)
    {
        return tag switch
        {
            NbtString str => str.Value,
            NbtInt i => i.Value,
            NbtLong l => l.Value,
            NbtFloat f => f.Value,
            NbtDouble d => d.Value,
            NbtByte b => (int)b.Value,
            NbtShort s => (int)s.Value,
            NbtByteArray ba => ba.Value.ToArray(),
            NbtIntArray ia => ia.Value.ToArray(),
            NbtLongArray la => la.Value.ToArray(),
            NbtList list => list.Select(ParseNbtValue).Where(v => v != null).ToList(),
            NbtCompound compound => ParseNbtCompoundToDictionary(compound),
            _ => null
        };
    }

    /// <summary>
    /// 将 NbtCompound 转换为 Dictionary
    /// </summary>
    private Dictionary<string, object> ParseNbtCompoundToDictionary(NbtCompound compound)
    {
        var dict = new Dictionary<string, object>();
        foreach (var tag in compound)
        {
            if (string.IsNullOrEmpty(tag.Name))
                continue;
                
            var value = ParseNbtValue(tag);
            if (value != null)
                dict[tag.Name] = value;
        }
        return dict;
    }

    /// <summary>
    /// 检查是否为标准 NBT 标签（已被转换为组件）
    /// </summary>
    private bool IsStandardNbtTag(string name)
    {
        return name switch
        {
            "Damage" or "display" or "Enchantments" or "Unbreakable" or
            "CustomModelData" or "AttributeModifiers" or "Trim" or "HideFlags" => true,
            _ => false
        };
    }

    /// <summary>
    /// 检查玩家是否在线
    /// </summary>
    private async Task<bool> IsPlayerOnlineAsync(string playerName)
    {
        if (_rconClient == null)
            return false;

        try
        {
            var response = await _rconClient.ExecuteCommandAsync($"list");
            return response.Contains(playerName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取玩家 UUID
    /// </summary>
    private async Task<Guid> GetPlayerUuidAsync(string playerName)
    {
        // 检查缓存
        if (_playerNameToUuid.TryGetValue(playerName, out var cachedUuid))
            return cachedUuid;

        try
        {
            // 扫描 usercache.json
            var usercachePath = Path.Combine(_serverDirectory, "usercache.json");
            if (File.Exists(usercachePath))
            {
                var json = await File.ReadAllTextAsync(usercachePath);
                var entries = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                
                if (entries != null)
                {
                    var entry = entries.FirstOrDefault(e =>
                        e.TryGetValue("name", out var nameElement) &&
                        nameElement.GetString()?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

                    if (entry != null && entry.TryGetValue("uuid", out var uuidElement))
                    {
                        var uuidString = uuidElement.GetString()?.Replace("-", "");
                        if (Guid.TryParse(uuidString, out var uuid))
                        {
                            _playerNameToUuid[playerName] = uuid;
                            return uuid;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"获取玩家 UUID 失败: {ex.Message}", ex);
        }

        return Guid.Empty;
    }

    /// <summary>
    /// 获取玩家数据文件路径
    /// </summary>
    private string GetPlayerDataFilePath(Guid playerUuid)
    {
        return Path.Combine(_serverDirectory, "world", "playerdata", $"{playerUuid}.dat");
    }
}

