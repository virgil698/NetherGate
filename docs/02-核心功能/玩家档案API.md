# 玩家档案 API

玩家档案 API 允许您获取玩家的档案信息，包括 UUID、皮肤纹理、披风等。该功能基于 Minecraft 1.21.9+ 的 `/fetchprofile` 命令。

---

## 📋 **目录**

- [功能概述](#功能概述)
- [快速开始](#快速开始)
- [API 参考](#api-参考)
- [使用示例](#使用示例)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 🌟 **功能概述**

玩家档案 API 提供以下功能：

- ✅ 通过玩家名称获取档案信息
- ✅ 通过 UUID 获取档案信息
- ✅ 批量获取多个玩家档案
- ✅ 提取皮肤和披风纹理 URL
- ✅ 生成玩家头颅物品
- ✅ 自动缓存（30分钟有效期）

### **系统要求**

- ✅ Minecraft Java 版 **1.21.9+**
- ✅ **RCON 必须启用**（需要执行 `/fetchprofile` 命令）

---

## 🚀 **快速开始**

### **1. 获取玩家档案**

```csharp
public class PlayerProfileExample : IPlugin
{
    private IPluginContext _context = null!;

    public async Task OnEnableAsync(IPluginContext context)
    {
        _context = context;
        
        // 通过玩家名称获取档案
        var profile = await _context.PlayerProfileApi.FetchProfileByNameAsync("Notch");
        
        if (profile != null)
        {
            _context.Logger.Info($"玩家: {profile.Name}");
            _context.Logger.Info($"UUID: {profile.Uuid}");
            
            // 获取皮肤 URL
            var skinUrl = profile.GetSkinUrl();
            if (skinUrl != null)
            {
                _context.Logger.Info($"皮肤: {skinUrl}");
            }
        }
    }
}
```

### **2. 生成玩家头颅**

```csharp
// 获取玩家头颅的 NBT 数据
var nbt = await _context.PlayerProfileApi.GetPlayerHeadNbtAsync("Notch");

if (nbt != null)
{
    // 使用 RCON 给予玩家头颅
    await _context.RconClient.ExecuteCommandAsync(
        $"give @p minecraft:player_head{nbt}"
    );
}
```

---

## 📖 **API 参考**

### **IPlayerProfileApi 接口**

```csharp
namespace NetherGate.API.Data
{
    public interface IPlayerProfileApi
    {
        /// <summary>
        /// 通过玩家名称获取玩家档案
        /// </summary>
        Task<PlayerProfile?> FetchProfileByNameAsync(string playerName);
        
        /// <summary>
        /// 通过玩家 UUID 获取玩家档案
        /// </summary>
        Task<PlayerProfile?> FetchProfileByUuidAsync(Guid uuid);
        
        /// <summary>
        /// 批量获取玩家档案
        /// </summary>
        Task<List<PlayerProfile>> FetchProfilesAsync(params string[] playerNames);
        
        /// <summary>
        /// 获取玩家头颅物品的 NBT 数据
        /// </summary>
        Task<string?> GetPlayerHeadNbtAsync(string playerName);
    }
}
```

### **PlayerProfile 数据模型**

```csharp
public class PlayerProfile
{
    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public Guid Uuid { get; init; }
    
    /// <summary>
    /// 玩家名称
    /// </summary>
    public string Name { get; init; }
    
    /// <summary>
    /// 档案属性（包含皮肤纹理等）
    /// </summary>
    public List<ProfileProperty> Properties { get; init; }
    
    /// <summary>
    /// 皮肤纹理（Minecraft 1.21.9+）
    /// </summary>
    public ProfileTexture? Texture { get; set; }
    
    /// <summary>
    /// 披风纹理（Minecraft 1.21.9+）
    /// </summary>
    public ProfileTexture? Cape { get; set; }
    
    /// <summary>
    /// 鞘翅纹理（Minecraft 1.21.9+）
    /// </summary>
    public ProfileTexture? Elytra { get; set; }
    
    /// <summary>
    /// 玩家模型类型（Minecraft 1.21.9+）
    /// </summary>
    public PlayerModel Model { get; set; } = PlayerModel.Wide;
    
    /// <summary>
    /// 获取皮肤纹理 URL
    /// </summary>
    public string? GetSkinUrl();
    
    /// <summary>
    /// 获取披风纹理 URL
    /// </summary>
    public string? GetCapeUrl();
}

/// <summary>
/// 纹理数据（Minecraft 1.21.9+）
/// </summary>
public class ProfileTexture
{
    /// <summary>
    /// 纹理 URL
    /// </summary>
    public string Url { get; init; }
    
    /// <summary>
    /// 纹理元数据（可选）
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// 玩家模型类型（Minecraft 1.21.9+）
/// </summary>
public enum PlayerModel
{
    /// <summary>标准模型（4px 臂宽）</summary>
    Wide,
    
    /// <summary>纤细模型（3px 臂宽）</summary>
    Slim
}
```

---

## 💡 **使用示例**

### **示例 1：玩家档案查询命令**

创建一个命令，让玩家查询其他玩家的档案信息：

```csharp
public class ProfileCommand : ICommand
{
    private readonly IPluginContext _context;

    public string Name => "profile";
    public string Description => "查询玩家档案信息";
    public string Usage => "#profile <玩家名称>";
    public List<string> Aliases => new() { "pf" };
    public string PluginId => "ProfilePlugin";
    public string? Permission => null;

    public ProfileCommand(IPluginContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (args.Length < 1)
            return CommandResult.Fail($"用法: {Usage}");

        var playerName = args[0];
        
        try
        {
            var profile = await _context.PlayerProfileApi.FetchProfileByNameAsync(playerName);
            
            if (profile == null)
            {
                return CommandResult.Fail($"§c找不到玩家: {playerName}");
            }

            var message = $"""
                §6═══════════════════════════════
                §f  玩家档案: §e{profile.Name}
                
                §7  UUID: §f{profile.Uuid}
                §7  模型: §f{(profile.Model == PlayerModel.Slim ? "纤细 (3px)" : "标准 (4px)")}
                
                """;

            // 1.21.9+ 扩展字段
            if (profile.Texture != null)
            {
                message += $"§7  皮肤: §f{profile.Texture.Url}\n";
            }
            else
            {
                // 兼容旧版本
                var skinUrl = profile.GetSkinUrl();
                if (skinUrl != null)
                {
                    message += $"§7  皮肤: §f{skinUrl}\n";
                }
            }

            if (profile.Cape != null)
            {
                message += $"§7  披风: §f{profile.Cape.Url}\n";
            }
            else
            {
                // 兼容旧版本
                var capeUrl = profile.GetCapeUrl();
                if (capeUrl != null)
                {
                    message += $"§7  披风: §f{capeUrl}\n";
                }
            }

            if (profile.Elytra != null)
            {
                message += $"§7  鞘翅: §f{profile.Elytra.Url}\n";
            }

            message += "\n§6═══════════════════════════════";

            sender.SendMessage(message);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            _context.Logger.Error($"获取玩家档案失败: {ex.Message}", ex);
            return CommandResult.Fail("§c获取档案信息时发生错误");
        }
    }
}
```

### **示例 2：玩家头颅商店**

创建一个商店系统，让玩家购买其他玩家的头颅：

```csharp
public class PlayerHeadShop : IPlugin
{
    private IPluginContext _context = null!;

    public async Task OnEnableAsync(IPluginContext context)
    {
        _context = context;
        
        _context.CommandManager.RegisterCommand(new BuyHeadCommand(_context));
    }
}

public class BuyHeadCommand : ICommand
{
    private readonly IPluginContext _context;

    public string Name => "buyhead";
    public string Description => "购买玩家头颅";
    public string Usage => "#buyhead <玩家名称>";
    public List<string> Aliases => new() { "bh" };
    public string PluginId => "PlayerHeadShop";
    public string? Permission => null;

    public BuyHeadCommand(IPluginContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> ExecuteAsync(ICommandSender sender, string[] args)
    {
        if (sender.IsConsole)
            return CommandResult.Fail("§c此命令只能在游戏内使用");

        if (args.Length < 1)
            return CommandResult.Fail($"用法: {Usage}");

        var targetPlayer = args[0];
        var price = 100; // 价格

        try
        {
            // 检查经济（假设有经济插件）
            var balance = await _context.Messenger.SendRequestAsync<int>(
                "economy.balance", 
                new { PlayerName = sender.Name }
            );

            if (balance < price)
            {
                return CommandResult.Fail($"§c余额不足！需要 {price} 金币");
            }

            // 获取玩家头颅 NBT
            var nbt = await _context.PlayerProfileApi.GetPlayerHeadNbtAsync(targetPlayer);
            
            if (nbt == null)
            {
                return CommandResult.Fail($"§c找不到玩家: {targetPlayer}");
            }

            // 扣除金币
            await _context.Messenger.SendRequestAsync<bool>(
                "economy.remove",
                new { PlayerName = sender.Name, Amount = price }
            );

            // 给予玩家头颅
            await _context.RconClient!.ExecuteCommandAsync(
                $"give {sender.Name} minecraft:player_head{nbt}"
            );

            sender.SendMessage($"§a成功购买 {targetPlayer} 的头颅！花费 {price} 金币");
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            _context.Logger.Error($"购买头颅失败: {ex.Message}", ex);
            return CommandResult.Fail("§c购买失败");
        }
    }
}
```

### **示例 3：批量获取档案**

获取多个玩家的档案信息：

```csharp
public async Task ShowTopPlayers()
{
    var topPlayerNames = new[] { "Notch", "jeb_", "Dinnerbone" };
    
    // 批量获取
    var profiles = await _context.PlayerProfileApi.FetchProfilesAsync(topPlayerNames);
    
    var message = "§6═══ 知名玩家列表 ═══\n";
    
    foreach (var profile in profiles)
    {
        message += $"§e• {profile.Name} §7({profile.Uuid})\n";
        
        var skinUrl = profile.GetSkinUrl();
        if (skinUrl != null)
        {
            message += $"  §7皮肤: §f{skinUrl}\n";
        }
    }
    
    await _context.GameDisplay.BroadcastMessageAsync(message);
}
```

### **示例 4：皮肤变更监控**

监控玩家皮肤变更：

```csharp
public class SkinMonitor : IPlugin
{
    private IPluginContext _context = null!;
    private Dictionary<string, string?> _playerSkins = new();

    public async Task OnEnableAsync(IPluginContext context)
    {
        _context = context;
        
        // 玩家加入时记录皮肤
        _context.EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
    }

    private async void OnPlayerJoined(PlayerJoinedEvent e)
    {
        try
        {
            var profile = await _context.PlayerProfileApi.FetchProfileByNameAsync(e.Player.Name);
            
            if (profile == null)
                return;

            var currentSkin = profile.GetSkinUrl();
            
            if (_playerSkins.TryGetValue(e.Player.Name, out var previousSkin))
            {
                if (currentSkin != previousSkin)
                {
                    _context.Logger.Info($"{e.Player.Name} 更换了皮肤");
                    
                    await _context.GameDisplay.BroadcastMessageAsync(
                        $"§e{e.Player.Name} 更换了新皮肤！"
                    );
                }
            }
            
            _playerSkins[e.Player.Name] = currentSkin;
        }
        catch (Exception ex)
        {
            _context.Logger.Error($"检查皮肤失败: {ex.Message}", ex);
        }
    }
}
```

---

## ✅ **最佳实践**

### **1. 利用缓存机制**

API 内置了 30 分钟的缓存，避免频繁请求：

```csharp
// 第一次请求会从 Mojang 服务器获取
var profile1 = await _context.PlayerProfileApi.FetchProfileByNameAsync("Notch");

// 30 分钟内的后续请求会使用缓存
var profile2 = await _context.PlayerProfileApi.FetchProfileByNameAsync("Notch");
```

### **2. 错误处理**

始终检查返回值是否为 null：

```csharp
var profile = await _context.PlayerProfileApi.FetchProfileByNameAsync(playerName);

if (profile == null)
{
    _context.Logger.Warning($"无法获取玩家档案: {playerName}");
    return;
}

// 使用 profile
```

### **3. 批量获取优化**

当需要获取多个玩家档案时，使用批量 API：

```csharp
// ✅ 推荐：批量获取
var profiles = await _context.PlayerProfileApi.FetchProfilesAsync(
    "Player1", "Player2", "Player3"
);

// ❌ 不推荐：循环单独获取
foreach (var name in playerNames)
{
    var profile = await _context.PlayerProfileApi.FetchProfileByNameAsync(name);
}
```

### **4. 检查 RCON 可用性**

在使用前确保 RCON 已启用：

```csharp
if (_context.RconClient == null)
{
    _context.Logger.Warning("RCON 未启用，无法使用玩家档案 API");
    return;
}

var profile = await _context.PlayerProfileApi.FetchProfileByNameAsync(playerName);
```

---

## ❓ **常见问题**

### **Q1: 为什么获取档案失败？**

**A:** 可能的原因：

1. RCON 未启用或未正确配置
2. 玩家名称拼写错误
3. Minecraft 服务器版本低于 1.21.9
4. Mojang 服务器连接失败

### **Q2: 可以获取离线玩家的档案吗？**

**A:** 可以，只要玩家曾经登录过正版服务器，就可以通过名称或 UUID 获取档案。

### **Q3: 档案信息多久更新一次？**

**A:** API 内置 30 分钟缓存。如需强制刷新，可以等待缓存过期或重启服务器。

### **Q4: 皮肤 URL 是什么格式？**

**A:** Mojang 官方纹理服务器的 URL，类似：
```
http://textures.minecraft.net/texture/abc123...
```

### **Q5: 如何判断玩家是否有披风？**

**A:** 使用 `GetCapeUrl()` 方法，如果返回 null 表示没有披风：

```csharp
var capeUrl = profile.GetCapeUrl();
if (capeUrl != null)
{
    _context.Logger.Info($"{profile.Name} 拥有披风");
}
```

---

## 🔗 **相关文档**

- [NBT 数据操作](./NBT数据操作.md)
- [RCON 集成](./RCON集成.md)
- [常见场景实现](../07-示例和最佳实践/常见场景.md)

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-09

