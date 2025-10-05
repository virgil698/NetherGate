# 游戏显示 API

NetherGate 提供了完整的 Minecraft 游戏显示和控制 API，允许插件通过 RCON 与游戏进行交互，实现 BossBar、记分板、标题、对话框等功能。

---

## 📋 **目录**

- [快速开始](#快速开始)
- [BossBar API](#bossbar-api)
- [记分板 API](#记分板-api)
- [标题显示 API](#标题显示-api)
- [对话框 API](#对话框-api)
- [聊天消息 API](#聊天消息-api)
- [玩家控制 API](#玩家控制-api)
- [实体控制 API](#实体控制-api)
- [世界控制 API](#世界控制-api)
- [粒子和音效 API](#粒子和音效-api)
- [最佳实践](#最佳实践)

---

## 🚀 **快速开始**

### **获取 API 实例**

```csharp
public class MyPlugin : IPlugin
{
    private IPluginContext _context;
    private IGameDisplayApi _gameApi;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        _gameApi = context.GameDisplayApi;
        
        // 使用 API
        ShowWelcomeBossBar("Steve");
    }

    private async void ShowWelcomeBossBar(string playerName)
    {
        await _gameApi.ShowBossBar(
            playerName,
            "welcome_bar",
            "欢迎来到服务器！",
            BossBarColor.Green,
            BossBarStyle.Progress,
            progress: 1.0f
        );
    }
}
```

---

## 📊 **BossBar API**

BossBar 是屏幕顶部的状态条，用于显示重要信息。

### **显示 BossBar**

```csharp
// 显示基本 BossBar
await _gameApi.ShowBossBar(
    playerName: "Steve",
    id: "my_bar",
    title: "活动进行中",
    color: BossBarColor.Green,
    style: BossBarStyle.Progress,
    progress: 0.75f  // 75% 进度
);

// 显示带标志的 BossBar
await _gameApi.ShowBossBar(
    playerName: "Steve",
    id: "warning_bar",
    title: "警告：Boss 即将出现",
    color: BossBarColor.Red,
    style: BossBarStyle.NotchedSix,
    progress: 1.0f,
    darkenScreen: true,     // 使屏幕变暗
    createFog: true,        // 创建雾效
    playBossMusic: true     // 播放 Boss 音乐
);
```

### **更新 BossBar**

```csharp
// 更新进度
await _gameApi.UpdateBossBarValue("Steve", "my_bar", 0.5f);

// 更新标题
await _gameApi.UpdateBossBarName("Steve", "my_bar", "新的标题");

// 更新颜色
await _gameApi.UpdateBossBarColor("Steve", "my_bar", BossBarColor.Yellow);

// 更新样式
await _gameApi.UpdateBossBarStyle("Steve", "my_bar", BossBarStyle.NotchedTen);
```

### **移除 BossBar**

```csharp
await _gameApi.RemoveBossBar("Steve", "my_bar");
```

### **BossBar 枚举**

```csharp
public enum BossBarColor
{
    Pink,    // 粉色
    Blue,    // 蓝色
    Red,     // 红色
    Green,   // 绿色
    Yellow,  // 黄色
    Purple,  // 紫色
    White    // 白色
}

public enum BossBarStyle
{
    Progress,     // 单条进度条
    NotchedSix,   // 6 段
    NotchedTen,   // 10 段
    NotchedTwelve,// 12 段
    NotchedTwenty // 20 段
}
```

### **应用示例**

```csharp
// 倒计时 BossBar
public async Task ShowCountdownAsync(string player, int seconds)
{
    await _gameApi.ShowBossBar(player, "countdown", $"倒计时: {seconds}秒", 
        BossBarColor.Red, BossBarStyle.Progress, 1.0f);
    
    for (int i = seconds; i > 0; i--)
    {
        float progress = (float)i / seconds;
        await _gameApi.UpdateBossBarValue(player, "countdown", progress);
        await _gameApi.UpdateBossBarName(player, "countdown", $"倒计时: {i}秒");
        await Task.Delay(1000);
    }
    
    await _gameApi.RemoveBossBar(player, "countdown");
}

// Boss 战进度
public async Task UpdateBossHealthAsync(string player, float health, float maxHealth)
{
    float progress = health / maxHealth;
    await _gameApi.UpdateBossBarValue(player, "boss_health", progress);
    await _gameApi.UpdateBossBarName(player, "boss_health", 
        $"末影龙 - {health:F0}/{maxHealth:F0}");
}
```

---

## 🏅 **记分板 API**

记分板用于显示分数、排行榜等信息。

### **创建记分板**

```csharp
// 创建记分板目标
await _gameApi.CreateScoreboardObjective(
    name: "kills",
    criterion: "playerKillCount",  // 击杀数
    displayName: "击杀排行榜"
);

// 自定义标准（由插件控制）
await _gameApi.CreateScoreboardObjective(
    name: "coins",
    criterion: "dummy",  // 自定义
    displayName: "金币"
);
```

### **设置显示位置**

```csharp
public enum DisplaySlot
{
    Sidebar,       // 侧边栏（右侧）
    List,          // 玩家列表（Tab）
    BelowName      // 玩家名称下方
}

// 在侧边栏显示
await _gameApi.SetScoreboardDisplay(DisplaySlot.Sidebar, "kills");

// 在玩家名称下方显示
await _gameApi.SetScoreboardDisplay(DisplaySlot.BelowName, "health");
```

### **设置分数**

```csharp
// 设置玩家分数
await _gameApi.SetScoreboardValue("coins", "Steve", 100);

// 设置虚拟条目（用于显示文本）
await _gameApi.SetScoreboardValue("sidebar", "§e--- 服务器状态 ---", 10);
await _gameApi.SetScoreboardValue("sidebar", "§a在线玩家: 15", 9);
await _gameApi.SetScoreboardValue("sidebar", "§b当前世界: 主世界", 8);
```

### **修改分数**

```csharp
// 增加分数
await _gameApi.AddScoreboardValue("coins", "Steve", 50);  // +50

// 减少分数
await _gameApi.RemoveScoreboardValue("coins", "Steve", 10);  // -10
```

### **移除记分板**

```csharp
// 移除玩家分数
await _gameApi.ResetScoreboardScore("coins", "Steve");

// 移除整个记分板
await _gameApi.RemoveScoreboardObjective("coins");
```

### **应用示例**

```csharp
// 实时更新记分板
public async Task UpdateServerStatsAsync()
{
    await _gameApi.CreateScoreboardObjective("stats", "dummy", "§6§l服务器状态");
    await _gameApi.SetScoreboardDisplay(DisplaySlot.Sidebar, "stats");
    
    while (true)
    {
        var playerCount = GetOnlinePlayerCount();
        var tps = GetServerTPS();
        var uptime = GetServerUptime();
        
        await _gameApi.SetScoreboardValue("stats", "§e----------", 10);
        await _gameApi.SetScoreboardValue("stats", $"§a在线: {playerCount}", 9);
        await _gameApi.SetScoreboardValue("stats", $"§bTPS: {tps:F1}", 8);
        await _gameApi.SetScoreboardValue("stats", $"§c运行: {uptime}", 7);
        await _gameApi.SetScoreboardValue("stats", "§e----------", 6);
        
        await Task.Delay(1000);
    }
}

// 击杀排行榜
public async Task ShowKillLeaderboardAsync()
{
    await _gameApi.CreateScoreboardObjective("kills", "playerKillCount", "§c§l击杀排行");
    await _gameApi.SetScoreboardDisplay(DisplaySlot.Sidebar, "kills");
}
```

---

## 🎬 **标题显示 API**

标题是屏幕中央的大字，副标题是下方的小字，ActionBar 是物品栏上方的文本。

### **显示标题**

```csharp
// 完整标题
await _gameApi.ShowTitle(
    playerName: "Steve",
    title: "§6欢迎来到服务器",
    subtitle: "§e祝你玩得愉快",
    fadeIn: 10,    // 淡入时间（tick）
    stay: 70,      // 停留时间
    fadeOut: 20    // 淡出时间
);

// 仅标题
await _gameApi.ShowTitleOnly("Steve", "§c§l警告", fadeIn: 5, stay: 40, fadeOut: 10);

// 仅副标题
await _gameApi.ShowSubtitleOnly("Steve", "§7请遵守服务器规则", fadeIn: 10, stay: 50, fadeOut: 10);
```

### **ActionBar 消息**

```csharp
// 显示 ActionBar（物品栏上方）
await _gameApi.ShowActionBar("Steve", "§a金币: 100  §c生命: 20/20");

// ActionBar 常用于实时信息
public async Task UpdateActionBarAsync(string player)
{
    while (true)
    {
        var health = GetPlayerHealth(player);
        var hunger = GetPlayerHunger(player);
        var coins = GetPlayerCoins(player);
        
        await _gameApi.ShowActionBar(player, 
            $"§c❤ {health}/20  §6🍖 {hunger}/20  §e⭐ {coins}");
        
        await Task.Delay(1000);
    }
}
```

### **清除标题**

```csharp
// 清除当前显示的标题
await _gameApi.ClearTitle("Steve");

// 重置标题时间设置
await _gameApi.ResetTitleTimes("Steve");
```

### **应用示例**

```csharp
// 活动开始倒计时
public async Task EventCountdownAsync(string player, int seconds)
{
    for (int i = seconds; i > 0; i--)
    {
        string color = i <= 3 ? "§c" : (i <= 5 ? "§e" : "§a");
        await _gameApi.ShowTitleOnly(player, $"{color}§l{i}", 
            fadeIn: 0, stay: 20, fadeOut: 5);
        await Task.Delay(1000);
    }
    
    await _gameApi.ShowTitle(player, "§a§l开始！", "§e祝你好运", 
        fadeIn: 5, stay: 30, fadeOut: 10);
}

// 成就通知
public async Task ShowAchievementAsync(string player, string achievement)
{
    await _gameApi.ShowTitle(player, 
        "§6§l成就达成", 
        $"§e{achievement}", 
        fadeIn: 10, stay: 60, fadeOut: 20);
}
```

---

## 💬 **对话框 API**

对话框（Written Book UI）用于显示富文本、可点击的交互式内容。

### **显示对话框**

```csharp
// 简单文本对话框
await _gameApi.ShowDialog(
    playerName: "Steve",
    title: "服务器规则",
    author: "管理员",
    pages: new[]
    {
        "欢迎来到服务器！\n\n请遵守以下规则：",
        "1. 不要破坏他人建筑\n2. 不要使用外挂\n3. 友善待人",
        "违反规则将被封禁。\n\n祝你玩得愉快！"
    }
);

// 带点击事件的对话框
var pages = new[]
{
    @"{""text"":""点击下方链接查看详情"",""color"":""gold""}",
    @"{""text"":""访问官网"",""color"":""blue"",""underlined"":true,""clickEvent"":{""action"":""open_url"",""value"":""https://example.com""}}"
};

await _gameApi.ShowDialog("Steve", "服务器信息", "管理团队", pages);
```

### **JSON 文本格式**

```csharp
// 富文本示例
var page = new
{
    text = "欢迎！\n",
    color = "gold",
    bold = true,
    extra = new[]
    {
        new { text = "点击这里", color = "blue", underlined = true,
              clickEvent = new { action = "run_command", value = "/spawn" } },
        new { text = " 返回出生点", color = "gray" }
    }
};

await _gameApi.ShowDialog("Steve", "传送", "系统", new[] { 
    JsonSerializer.Serialize(page) 
});
```

### **应用示例**

```csharp
// 任务系统
public async Task ShowQuestDialogAsync(string player, Quest quest)
{
    var pages = new[]
    {
        $"§6§l{quest.Title}\n\n§r{quest.Description}",
        $"§e奖励：\n§f- {quest.RewardExp} 经验\n§f- {quest.RewardCoins} 金币",
        "§a点击接受任务",
    };
    
    await _gameApi.ShowDialog(player, "任务", quest.Giver, pages);
}

// 商店系统
public async Task ShowShopDialogAsync(string player)
{
    var pages = new[]
    {
        "§6§l服务器商店\n\n§r选择你要购买的物品：",
        "§e[1] 钻石剑 - 100金币\n§e[2] 附魔书 - 50金币\n§e[3] 食物包 - 10金币"
    };
    
    await _gameApi.ShowDialog(player, "商店", "商人", pages);
}
```

---

## 💬 **聊天消息 API**

### **发送聊天消息**

```csharp
// 发送普通消息
await _gameApi.SendChatMessage("Steve", "欢迎来到服务器！");

// 发送彩色消息
await _gameApi.SendChatMessage("Steve", "§a[系统] §f你获得了 §e100金币§f！");

// 发送 JSON 格式消息（富文本）
await _gameApi.SendJsonMessage("Steve", @"
{
    ""text"": ""[商店] "",
    ""color"": ""gold"",
    ""extra"": [
        {
            ""text"": ""点击这里"",
            ""color"": ""aqua"",
            ""underlined"": true,
            ""clickEvent"": {
                ""action"": ""run_command"",
                ""value"": ""/shop""
            },
            ""hoverEvent"": {
                ""action"": ""show_text"",
                ""value"": ""打开商店""
            }
        },
        {
            ""text"": "" 打开商店"",
            ""color"": ""white""
        }
    ]
}");
```

### **广播消息**

```csharp
// 发送给所有玩家
await _gameApi.BroadcastMessage("§c[公告] §f服务器将在5分钟后重启");

// 发送给多个玩家
foreach (var player in players)
{
    await _gameApi.SendChatMessage(player, "你被邀请参加活动！");
}
```

### **颜色代码**

```csharp
public enum TextColor
{
    Black,        // §0
    DarkBlue,     // §1
    DarkGreen,    // §2
    DarkAqua,     // §3
    DarkRed,      // §4
    DarkPurple,   // §5
    Gold,         // §6
    Gray,         // §7
    DarkGray,     // §8
    Blue,         // §9
    Green,        // §a
    Aqua,         // §b
    Red,          // §c
    LightPurple,  // §d
    Yellow,       // §e
    White         // §f
}

// 使用
var message = $"{TextColor.Gold.ToCode()}[VIP] {TextColor.White.ToCode()}Steve: Hello!";
```

---

## 🎮 **玩家控制 API**

### **传送玩家**

```csharp
// 传送到坐标
await _gameApi.TeleportPlayer("Steve", x: 0, y: 64, z: 0);

// 传送到另一个玩家
await _gameApi.TeleportPlayerToPlayer("Steve", "Alex");

// 传送到指定方向
await _gameApi.TeleportPlayer("Steve", 100, 64, 200, yaw: 0, pitch: 0);
```

### **修改玩家属性**

```csharp
// 设置生命值
await _gameApi.SetPlayerHealth("Steve", 20.0f);

// 设置饥饿值
await _gameApi.SetPlayerHunger("Steve", 20);

// 设置经验等级
await _gameApi.SetPlayerLevel("Steve", 30);

// 设置游戏模式
await _gameApi.SetGameMode("Steve", GameMode.Creative);

// 给予效果
await _gameApi.GiveEffect("Steve", "speed", duration: 60, amplifier: 1);

// 清除效果
await _gameApi.ClearEffect("Steve", "poison");
await _gameApi.ClearAllEffects("Steve");
```

### **物品操作**

```csharp
// 给予物品
await _gameApi.GiveItem("Steve", "diamond", count: 64);
await _gameApi.GiveItem("Steve", "diamond_sword", count: 1, 
    nbt: "{Enchantments:[{id:\"sharpness\",lvl:5}]}");

// 清除物品
await _gameApi.ClearInventory("Steve");
await _gameApi.ClearItem("Steve", "dirt");
```

---

## 🐉 **实体控制 API**

```csharp
// 生成实体
await _gameApi.SummonEntity("zombie", x: 100, y: 64, z: 200);
await _gameApi.SummonEntity("armor_stand", 0, 65, 0, 
    nbt: "{CustomName:'{\"text\":\"传送点\"}',NoGravity:1b}");

// 击杀实体
await _gameApi.KillEntity("@e[type=zombie,distance=..10]");

// 修改实体数据
await _gameApi.ModifyEntityData("@e[type=armor_stand,limit=1]", 
    "{CustomName:'{\"text\":\"新名称\"}'}");
```

---

## 🌍 **世界控制 API**

```csharp
// 设置时间
await _gameApi.SetWorldTime("day");      // 白天
await _gameApi.SetWorldTime("night");    // 夜晚
await _gameApi.SetWorldTime(6000);       // 自定义时间

// 设置天气
await _gameApi.SetWeather("clear");      // 晴天
await _gameApi.SetWeather("rain");       // 下雨
await _gameApi.SetWeather("thunder");    // 雷暴

// 游戏规则
await _gameApi.SetGameRule("doDaylightCycle", "false");  // 停止时间流动
await _gameApi.SetGameRule("doMobSpawning", "false");    // 禁止生物生成
await _gameApi.SetGameRule("keepInventory", "true");     // 死亡不掉落

// 设置方块
await _gameApi.SetBlock(0, 64, 0, "diamond_block");
await _gameApi.FillBlocks(0, 64, 0, 10, 74, 10, "air");  // 清空区域
```

---

## ✨ **粒子和音效 API**

```csharp
// 播放粒子效果
await _gameApi.PlayParticle("heart", x: 0, y: 65, z: 0, 
    count: 10, speed: 0.1f);
await _gameApi.PlayParticle("explosion", 100, 64, 200);

// 播放音效
await _gameApi.PlaySound("Steve", "entity.player.levelup", volume: 1.0f, pitch: 1.0f);
await _gameApi.PlaySound("@a", "block.note_block.pling", 
    x: 0, y: 64, z: 0, volume: 1.0f, pitch: 2.0f);

// 停止音效
await _gameApi.StopSound("Steve", "music.game");
```

---

## 💡 **最佳实践**

### **1. 错误处理**

```csharp
try
{
    await _gameApi.ShowBossBar("Steve", "bar", "Title", 
        BossBarColor.Green, BossBarStyle.Progress, 1.0f);
}
catch (Exception ex)
{
    _context.Logger.Error($"显示 BossBar 失败: {ex.Message}");
}
```

### **2. 批量操作**

```csharp
// 批量发送消息
var players = GetOnlinePlayers();
foreach (var player in players)
{
    await _gameApi.SendChatMessage(player, "活动即将开始！");
    await Task.Delay(50);  // 避免过快发送
}
```

### **3. 资源清理**

```csharp
public async void OnDisable()
{
    // 移除所有 BossBar
    var players = GetOnlinePlayers();
    foreach (var player in players)
    {
        await _gameApi.RemoveBossBar(player, "my_bar");
    }
    
    // 移除记分板
    await _gameApi.RemoveScoreboardObjective("my_scoreboard");
}
```

---

## 📚 **相关文档**

- [插件开发指南](../03-插件开发/插件开发指南.md)
- [事件系统](./事件系统.md)
- [RCON 集成](../02-核心功能/RCON集成.md)

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05
