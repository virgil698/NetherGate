using System.Text.Json;
using NetherGate.API.Data;
using NetherGate.API.Logging;
using NetherGate.API.Protocol;

namespace NetherGate.Core.Data;

/// <summary>
/// 标签系统 API 实现
/// 基于 Minecraft 1.21.9+ 的标签系统
/// </summary>
public class TagApi : ITagApi
{
    private readonly string _serverDirectory;
    private readonly ILogger _logger;
    private readonly IRconClient? _rconClient;
    private readonly Dictionary<string, List<string>> _blockTagsCache = new();
    private readonly Dictionary<string, List<string>> _itemTagsCache = new();
    private readonly Dictionary<string, List<string>> _entityTagsCache = new();
    private readonly Dictionary<string, List<string>> _fluidTagsCache = new();
    private readonly Dictionary<string, List<string>> _gameEventTagsCache = new();
    private readonly Dictionary<string, List<string>> _biomeTagsCache = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public TagApi(string serverDirectory, ILogger logger, IRconClient? rconClient = null)
    {
        _serverDirectory = Path.GetFullPath(serverDirectory);
        _logger = logger;
        _rconClient = rconClient;
    }

    public async Task<bool> IsBlockInTagAsync(string blockId, string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_blockTagsCache.TryGetValue(normalizedTag, out var blocks))
        {
            return blocks.Contains(NormalizeId(blockId));
        }

        // 如果缓存中没有，尝试从文件读取
        return await CheckTagFromFileAsync("blocks", blockId, tag);
    }

    public async Task<bool> IsItemInTagAsync(string itemId, string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_itemTagsCache.TryGetValue(normalizedTag, out var items))
        {
            return items.Contains(NormalizeId(itemId));
        }

        return await CheckTagFromFileAsync("items", itemId, tag);
    }

    // ==================== 流体标签 ====================

    public async Task<bool> IsFluidInTagAsync(string fluidId, string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_fluidTagsCache.TryGetValue(normalizedTag, out var fluids))
        {
            return fluids.Contains(NormalizeId(fluidId));
        }

        return await CheckTagFromFileAsync("fluids", fluidId, tag);
    }

    public async Task<List<string>> GetFluidsInTagAsync(string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_fluidTagsCache.TryGetValue(normalizedTag, out var fluids))
        {
            return new List<string>(fluids);
        }

        return await ReadTagFromFileAsync("fluids", tag);
    }

    public async Task<List<string>> GetAllFluidTagsAsync()
    {
        await EnsureCacheUpdatedAsync();
        return _fluidTagsCache.Keys.ToList();
    }

    // ==================== 游戏事件标签 ====================

    public async Task<bool> IsGameEventInTagAsync(string eventId, string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_gameEventTagsCache.TryGetValue(normalizedTag, out var events))
        {
            return events.Contains(NormalizeId(eventId));
        }

        return await CheckTagFromFileAsync("game_events", eventId, tag);
    }

    public async Task<List<string>> GetGameEventsInTagAsync(string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_gameEventTagsCache.TryGetValue(normalizedTag, out var events))
        {
            return new List<string>(events);
        }

        return await ReadTagFromFileAsync("game_events", tag);
    }

    public async Task<List<string>> GetAllGameEventTagsAsync()
    {
        await EnsureCacheUpdatedAsync();
        return _gameEventTagsCache.Keys.ToList();
    }

    // ==================== 生物群系标签 ====================

    public async Task<bool> IsBiomeInTagAsync(string biomeId, string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_biomeTagsCache.TryGetValue(normalizedTag, out var biomes))
        {
            return biomes.Contains(NormalizeId(biomeId));
        }

        return await CheckTagFromFileAsync("biomes", biomeId, tag);
    }

    public async Task<List<string>> GetBiomesInTagAsync(string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_biomeTagsCache.TryGetValue(normalizedTag, out var biomes))
        {
            return new List<string>(biomes);
        }

        return await ReadTagFromFileAsync("biomes", tag);
    }

    public async Task<List<string>> GetAllBiomeTagsAsync()
    {
        await EnsureCacheUpdatedAsync();
        return _biomeTagsCache.Keys.ToList();
    }

    // ==================== 实体标签 ====================

    public async Task<bool> IsEntityInTagAsync(string entityType, string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_entityTagsCache.TryGetValue(normalizedTag, out var entities))
        {
            return entities.Contains(NormalizeId(entityType));
        }

        return await CheckTagFromFileAsync("entity_types", entityType, tag);
    }

    public async Task<List<string>> GetBlocksInTagAsync(string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_blockTagsCache.TryGetValue(normalizedTag, out var blocks))
        {
            return new List<string>(blocks);
        }

        return await ReadTagFromFileAsync("blocks", tag);
    }

    public async Task<List<string>> GetItemsInTagAsync(string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_itemTagsCache.TryGetValue(normalizedTag, out var items))
        {
            return new List<string>(items);
        }

        return await ReadTagFromFileAsync("items", tag);
    }

    public async Task<List<string>> GetEntitiesInTagAsync(string tag)
    {
        await EnsureCacheUpdatedAsync();
        
        var normalizedTag = NormalizeTag(tag);
        if (_entityTagsCache.TryGetValue(normalizedTag, out var entities))
        {
            return new List<string>(entities);
        }

        return await ReadTagFromFileAsync("entity_types", tag);
    }

    public async Task<List<string>> GetAllBlockTagsAsync()
    {
        await EnsureCacheUpdatedAsync();
        return _blockTagsCache.Keys.ToList();
    }

    public async Task<List<string>> GetAllItemTagsAsync()
    {
        await EnsureCacheUpdatedAsync();
        return _itemTagsCache.Keys.ToList();
    }

    public async Task<List<string>> GetAllEntityTagsAsync()
    {
        await EnsureCacheUpdatedAsync();
        return _entityTagsCache.Keys.ToList();
    }

    /// <summary>
    /// 确保缓存已更新
    /// </summary>
    private async Task EnsureCacheUpdatedAsync()
    {
        if (DateTime.UtcNow - _lastCacheUpdate < _cacheExpiration)
            return;

        await LoadTagsAsync();
        _lastCacheUpdate = DateTime.UtcNow;
    }

    /// <summary>
    /// 从文件加载所有标签
    /// </summary>
    private async Task LoadTagsAsync()
    {
        try
        {
            // 加载方块标签
            var blockTagsPath = Path.Combine(_serverDirectory, "data", "minecraft", "tags", "block");
            if (Directory.Exists(blockTagsPath))
            {
                await LoadTagDirectoryAsync(blockTagsPath, _blockTagsCache, "minecraft");
            }

            // 加载物品标签
            var itemTagsPath = Path.Combine(_serverDirectory, "data", "minecraft", "tags", "item");
            if (Directory.Exists(itemTagsPath))
            {
                await LoadTagDirectoryAsync(itemTagsPath, _itemTagsCache, "minecraft");
            }

            // 加载实体标签
            var entityTagsPath = Path.Combine(_serverDirectory, "data", "minecraft", "tags", "entity_type");
            if (Directory.Exists(entityTagsPath))
            {
                await LoadTagDirectoryAsync(entityTagsPath, _entityTagsCache, "minecraft");
            }

            // 加载流体标签
            var fluidTagsPath = Path.Combine(_serverDirectory, "data", "minecraft", "tags", "fluid");
            if (Directory.Exists(fluidTagsPath))
            {
                await LoadTagDirectoryAsync(fluidTagsPath, _fluidTagsCache, "minecraft");
            }

            // 加载游戏事件标签
            var gameEventTagsPath = Path.Combine(_serverDirectory, "data", "minecraft", "tags", "game_event");
            if (Directory.Exists(gameEventTagsPath))
            {
                await LoadTagDirectoryAsync(gameEventTagsPath, _gameEventTagsCache, "minecraft");
            }

            // 加载生物群系标签
            var biomeTagsPath = Path.Combine(_serverDirectory, "data", "minecraft", "tags", "worldgen", "biome");
            if (Directory.Exists(biomeTagsPath))
            {
                await LoadTagDirectoryAsync(biomeTagsPath, _biomeTagsCache, "minecraft");
            }

            _logger.Debug($"已加载标签: {_blockTagsCache.Count} 个方块标签, " +
                         $"{_itemTagsCache.Count} 个物品标签, " +
                         $"{_entityTagsCache.Count} 个实体标签, " +
                         $"{_fluidTagsCache.Count} 个流体标签, " +
                         $"{_gameEventTagsCache.Count} 个游戏事件标签, " +
                         $"{_biomeTagsCache.Count} 个生物群系标签");
        }
        catch (Exception ex)
        {
            _logger.Error("加载标签失败", ex);
        }
    }

    /// <summary>
    /// 加载标签目录
    /// </summary>
    private async Task LoadTagDirectoryAsync(string directory, Dictionary<string, List<string>> cache, string namespace_)
    {
        if (!Directory.Exists(directory))
            return;

        var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var tag = JsonSerializer.Deserialize<TagFile>(json);
                
                if (tag?.Values == null)
                    continue;

                // 从文件路径提取标签名称
                var relativePath = Path.GetRelativePath(directory, file);
                var tagName = Path.ChangeExtension(relativePath, null).Replace('\\', '/');
                var fullTagName = $"{namespace_}:{tagName}";

                var values = new List<string>();
                foreach (var value in tag.Values)
                {
                    if (value.StartsWith("#"))
                    {
                        // 这是对另一个标签的引用，递归解析
                        var referencedTagName = value.Substring(1); // 移除 # 前缀
                        var resolvedValues = await ResolveTagReferenceAsync(directory, cache, namespace_, referencedTagName);
                        values.AddRange(resolvedValues);
                    }
                    else
                    {
                        values.Add(NormalizeId(value));
                    }
                }

                cache[fullTagName] = values;
            }
            catch (Exception ex)
            {
                _logger.Error($"加载标签文件失败: {file}", ex);
            }
        }
    }

    /// <summary>
    /// 递归解析标签引用
    /// </summary>
    /// <param name="directory">标签目录</param>
    /// <param name="cache">缓存</param>
    /// <param name="defaultNamespace">默认命名空间</param>
    /// <param name="tagReference">标签引用（如 "minecraft:planks" 或 "planks"）</param>
    /// <returns>解析后的物品/方块/实体列表</returns>
    private async Task<List<string>> ResolveTagReferenceAsync(
        string directory, 
        Dictionary<string, List<string>> cache, 
        string defaultNamespace, 
        string tagReference)
    {
        try
        {
            // 规范化标签名称
            var fullTagName = tagReference.Contains(":") 
                ? tagReference 
                : $"{defaultNamespace}:{tagReference}";

            // 检查缓存（避免循环引用）
            if (cache.ContainsKey(fullTagName))
            {
                return cache[fullTagName];
            }

            // 解析标签文件路径
            var (namespace_, tagPath) = ParseTag(fullTagName);
            var tagFilePath = Path.Combine(
                Path.GetDirectoryName(directory)!,
                "..", "..",
                namespace_,
                "tags",
                Path.GetFileName(directory), // 类别（blocks/items/entity_type）
                $"{tagPath}.json"
            );

            tagFilePath = Path.GetFullPath(tagFilePath);

            if (!File.Exists(tagFilePath))
            {
                _logger.Debug($"引用的标签文件不存在: {tagFilePath}");
                return new List<string>();
            }

            // 读取并解析标签文件
            var json = await File.ReadAllTextAsync(tagFilePath);
            var tag = JsonSerializer.Deserialize<TagFile>(json);

            if (tag?.Values == null)
            {
                return new List<string>();
            }

            // 临时标记以防止循环引用
            cache[fullTagName] = new List<string>();

            // 递归解析所有值
            var resolvedValues = new List<string>();
            foreach (var value in tag.Values)
            {
                if (value.StartsWith("#"))
                {
                    // 递归解析嵌套标签
                    var nestedTagName = value.Substring(1);
                    var nestedValues = await ResolveTagReferenceAsync(directory, cache, namespace_, nestedTagName);
                    resolvedValues.AddRange(nestedValues);
                }
                else
                {
                    resolvedValues.Add(NormalizeId(value));
                }
            }

            // 更新缓存
            cache[fullTagName] = resolvedValues;
            return resolvedValues;
        }
        catch (Exception ex)
        {
            _logger.Error($"解析标签引用失败: {tagReference}", ex);
            return new List<string>();
        }
    }

    /// <summary>
    /// 从文件检查标签
    /// </summary>
    private async Task<bool> CheckTagFromFileAsync(string category, string id, string tag)
    {
        var items = await ReadTagFromFileAsync(category, tag);
        return items.Contains(NormalizeId(id));
    }

    /// <summary>
    /// 从文件读取标签
    /// </summary>
    private async Task<List<string>> ReadTagFromFileAsync(string category, string tag)
    {
        try
        {
            var (namespace_, tagPath) = ParseTag(tag);
            var filePath = Path.Combine(_serverDirectory, "data", namespace_, "tags", category, $"{tagPath}.json");

            if (!File.Exists(filePath))
            {
                _logger.Debug($"标签文件不存在: {filePath}");
                return new List<string>();
            }

            var json = await File.ReadAllTextAsync(filePath);
            var tagFile = JsonSerializer.Deserialize<TagFile>(json);

            if (tagFile?.Values == null)
                return new List<string>();

            var values = new List<string>();
            foreach (var value in tagFile.Values)
            {
                if (!value.StartsWith("#"))
                {
                    values.Add(NormalizeId(value));
                }
            }

            return values;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取标签文件失败: {tag}", ex);
            return new List<string>();
        }
    }

    /// <summary>
    /// 规范化标签名称
    /// </summary>
    private string NormalizeTag(string tag)
    {
        if (!tag.Contains(':'))
            return $"minecraft:{tag}";
        return tag;
    }

    /// <summary>
    /// 规范化 ID
    /// </summary>
    private string NormalizeId(string id)
    {
        if (!id.Contains(':'))
            return $"minecraft:{id}";
        return id;
    }

    /// <summary>
    /// 解析标签名称
    /// </summary>
    private (string namespace_, string path) ParseTag(string tag)
    {
        var parts = tag.Split(':', 2);
        if (parts.Length == 2)
            return (parts[0], parts[1]);
        return ("minecraft", parts[0]);
    }

    /// <summary>
    /// 标签文件结构
    /// </summary>
    private class TagFile
    {
        public bool? Replace { get; set; }
        public List<string>? Values { get; set; }
    }
}

