# WebSocket 数据推送

NetherGate 提供了强大的 WebSocket 数据广播系统，可以实时向网页、OBS 覆盖层等客户端推送游戏数据。

## 功能特性

- ✅ **实时数据推送** - 向 WebSocket 客户端推送实时数据
- ✅ **频道系统** - 支持多个独立的数据频道
- ✅ **自动数据源** - 注册定时推送的数据源
- ✅ **事件驱动** - 客户端连接/断开事件通知
- ✅ **类型安全** - 泛型支持，自动序列化

## 基本用法

### 广播数据到频道

```csharp
// 向指定频道广播数据
await context.DataBroadcaster.BroadcastAsync("game-stats", new
{
    onlinePlayers = 10,
    tps = 20.0,
    timestamp = DateTime.UtcNow
});
```

### 发送给特定客户端

```csharp
// 向特定客户端发送数据
await context.DataBroadcaster.SendToClientAsync("player-stats", clientId, new
{
    playerName = "Player1",
    health = 20,
    hunger = 20
});
```

### 注册自动数据源

```csharp
// 注册定时推送的数据源
await context.DataBroadcaster.RegisterDataSourceAsync(
    "server-status",                    // 频道名称
    async () => new                     // 数据提供函数
    {
        onlinePlayers = (await context.SmpApi.GetPlayersAsync()).Count,
        tps = await context.PerformanceMonitor.GetCurrentTpsAsync(),
        memoryUsage = GC.GetTotalMemory(false) / 1024.0 / 1024.0
    },
    TimeSpan.FromSeconds(1)             // 推送间隔
);
```

### 取消数据源

```csharp
// 取消注册的数据源
await context.DataBroadcaster.UnregisterDataSourceAsync("server-status");
```

### 查询频道信息

```csharp
// 获取频道的订阅者数量
var count = context.DataBroadcaster.GetSubscriberCount("game-stats");
context.Logger.Info($"频道订阅者: {count}");

// 获取所有活跃频道
var channels = context.DataBroadcaster.GetActiveChannels();
foreach (var channel in channels)
{
    context.Logger.Info($"频道: {channel}");
}
```

## 客户端连接管理

### 监听客户端事件

```csharp
public void OnEnable(IPluginContext context)
{
    // 监听客户端连接
    context.DataBroadcaster.ClientConnected += OnClientConnected;
    
    // 监听客户端断开
    context.DataBroadcaster.ClientDisconnected += OnClientDisconnected;
}

private void OnClientConnected(object? sender, ClientConnectedEventArgs e)
{
    context.Logger.Info($"客户端 {e.ClientId} 连接到频道 {e.Channel}");
    context.Logger.Info($"IP: {e.IpAddress}");
}

private void OnClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
{
    context.Logger.Info($"客户端 {e.ClientId} 断开连接: {e.Reason}");
}
```

## 实战示例

### 示例 1: OBS 覆盖层 - 排行榜展示

```csharp
public class LeaderboardOverlayPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        
        // 订阅排行榜变化事件
        context.LeaderboardSystem.RankChanged += OnRankChanged;
        context.LeaderboardSystem.ScoreUpdated += OnScoreUpdated;
        
        // 注册定时推送前 10 名
        await context.DataBroadcaster.RegisterDataSourceAsync(
            "leaderboard-top10",
            async () =>
            {
                var topPlayers = await context.LeaderboardSystem.GetTopPlayersAsync("kills", 10);
                return new
                {
                    type = "leaderboard",
                    leaderboard = "kills",
                    title = "击杀排行",
                    players = topPlayers.Select(p => new
                    {
                        rank = p.Rank,
                        name = p.PlayerName,
                        score = p.Score
                    })
                };
            },
            TimeSpan.FromSeconds(2)
        );
    }

    private async void OnRankChanged(object? sender, RankChangedEventArgs e)
    {
        // 实时推送排名变化
        await _context.DataBroadcaster.BroadcastAsync("leaderboard-updates", new
        {
            type = "rank_changed",
            player = e.PlayerName,
            oldRank = e.OldRank,
            newRank = e.NewRank,
            leaderboard = e.LeaderboardName
        });
    }

    private async void OnScoreUpdated(object? sender, ScoreUpdatedEventArgs e)
    {
        // 实时推送分数更新
        await _context.DataBroadcaster.BroadcastAsync("leaderboard-updates", new
        {
            type = "score_updated",
            player = e.PlayerName,
            oldScore = e.OldScore,
            newScore = e.NewScore,
            leaderboard = e.LeaderboardName
        });
    }

    public void OnDisable()
    {
        _context.DataBroadcaster.UnregisterDataSourceAsync("leaderboard-top10").Wait();
    }
}
```

### 示例 2: 成就进度追踪

```csharp
public class AdvancementProgressOverlayPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        
        // 订阅成就完成事件
        context.AdvancementTracker.AdvancementCompleted += OnAdvancementCompleted;
        
        // 注册成就进度数据源
        await context.DataBroadcaster.RegisterDataSourceAsync(
            "advancement-progress",
            async () =>
            {
                var players = await context.SmpApi.GetPlayersAsync();
                var progressData = new List<object>();

                foreach (var player in players)
                {
                    var data = await context.AdvancementTracker.GetPlayerAdvancementsAsync(player.Uuid);
                    if (data != null)
                    {
                        progressData.Add(new
                        {
                            player = player.Name,
                            total = data.TotalAdvancements,
                            completed = data.CompletedCount,
                            percentage = data.ProgressPercentage
                        });
                    }
                }

                return new
                {
                    type = "advancement_progress",
                    players = progressData
                };
            },
            TimeSpan.FromSeconds(5)
        );
    }

    private async void OnAdvancementCompleted(object? sender, AdvancementCompletedEventArgs e)
    {
        // 推送成就完成通知
        await _context.DataBroadcaster.BroadcastAsync("advancement-alerts", new
        {
            type = "advancement_completed",
            player = e.PlayerName,
            advancement = e.AdvancementName,
            timestamp = e.CompletedAt
        });
        
        // 触发动画效果
        await _context.DataBroadcaster.BroadcastAsync("overlay-effects", new
        {
            effect = "celebration",
            player = e.PlayerName,
            duration = 3000
        });
    }
}
```

### 示例 3: 服务器状态监控面板

```csharp
public class ServerStatusDashboardPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        
        // 注册服务器状态数据源
        await context.DataBroadcaster.RegisterDataSourceAsync(
            "server-dashboard",
            async () =>
            {
                var players = await context.SmpApi.GetPlayersAsync();
                var performance = await GetPerformanceData();
                
                return new
                {
                    server = new
                    {
                        online = true,
                        version = "1.21.9",
                        motd = "NetherGate Server"
                    },
                    players = new
                    {
                        online = players.Count,
                        max = 20,
                        list = players.Select(p => new
                        {
                            name = p.Name,
                            uuid = p.Uuid
                        })
                    },
                    performance
                };
            },
            TimeSpan.FromSeconds(1)
        );
    }

    private async Task<object> GetPerformanceData()
    {
        return new
        {
            tps = await _context.PerformanceMonitor.GetCurrentTpsAsync(),
            memory = new
            {
                used = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                max = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0
            },
            cpu = await _context.PerformanceMonitor.GetCpuUsageAsync()
        };
    }
}
```

## 前端集成示例

### HTML + JavaScript 客户端

```html
<!DOCTYPE html>
<html>
<head>
    <title>NetherGate Overlay</title>
    <style>
        body {
            font-family: 'Minecraft', monospace;
            background: transparent;
            color: white;
            margin: 20px;
        }
        .leaderboard {
            background: rgba(0, 0, 0, 0.7);
            padding: 20px;
            border-radius: 10px;
        }
        .player {
            margin: 5px 0;
            font-size: 18px;
        }
        .rank-1 { color: #FFD700; }
        .rank-2 { color: #C0C0C0; }
        .rank-3 { color: #CD7F32; }
    </style>
</head>
<body>
    <div class="leaderboard">
        <h2>🏆 击杀排行榜</h2>
        <div id="players"></div>
    </div>

    <script>
        // 连接 WebSocket
        const ws = new WebSocket('ws://localhost:8080/ws?channel=leaderboard-top10');

        ws.onmessage = (event) => {
            const data = JSON.parse(event.data);
            
            if (data.Type === 'data' && data.Data.type === 'leaderboard') {
                updateLeaderboard(data.Data.players);
            }
        };

        function updateLeaderboard(players) {
            const container = document.getElementById('players');
            container.innerHTML = players.map(p => `
                <div class="player rank-${p.rank}">
                    #${p.rank} ${p.name} - ${p.score} 击杀
                </div>
            `).join('');
        }
    </script>
</body>
</html>
```

### React 组件示例

```jsx
import React, { useEffect, useState } from 'react';

function LeaderboardOverlay() {
    const [players, setPlayers] = useState([]);

    useEffect(() => {
        const ws = new WebSocket('ws://localhost:8080/ws?channel=leaderboard-top10');

        ws.onmessage = (event) => {
            const data = JSON.parse(event.data);
            if (data.Type === 'data' && data.Data.type === 'leaderboard') {
                setPlayers(data.Data.players);
            }
        };

        return () => ws.close();
    }, []);

    return (
        <div className="leaderboard">
            <h2>🏆 击杀排行榜</h2>
            {players.map(p => (
                <div key={p.rank} className={`player rank-${p.rank}`}>
                    #{p.rank} {p.name} - {p.score} 击杀
                </div>
            ))}
        </div>
    );
}
```

## 消息格式

WebSocket 消息采用统一的 JSON 格式：

```json
{
  "Type": "data",
  "Channel": "leaderboard-top10",
  "Data": {
    "type": "leaderboard",
    "players": [...]
  },
  "Timestamp": 1728384000000,
  "MessageId": "uuid-here"
}
```

## WebSocket 端点配置

在 `websocket-config.yaml` 中配置：

```yaml
websocket:
  enabled: true
  host: "0.0.0.0"
  port: 8080
  path: "/ws"
  
  # 允许的来源（CORS）
  allowed_origins:
    - "http://localhost:*"
    - "obs://overlay"
  
  # 心跳配置
  heartbeat:
    enabled: true
    interval: 30  # 秒
```

## 安全考虑

1. **访问控制** - 限制允许的来源
2. **身份验证** - 实现 Token 认证机制
3. **数据过滤** - 不推送敏感信息
4. **速率限制** - 限制推送频率

## 最佳实践

1. **合理的推送频率** - 避免过于频繁的更新
2. **数据精简** - 只推送必要的数据
3. **错误处理** - 妥善处理客户端断开
4. **频道隔离** - 使用不同频道分离不同类型的数据
5. **资源清理** - 及时取消不需要的数据源

## 相关 API

- [排行榜系统](排行榜系统.md)
- [成就和统计追踪](成就和统计追踪.md)
- [性能监控](性能监控.md)

## 参考

WebSocket 数据推送功能的设计灵感来自 [AATool](https://github.com/DarwinBaker/AATool) 的覆盖层系统。

