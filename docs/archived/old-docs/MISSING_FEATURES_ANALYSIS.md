# NetherGate 遗漏功能分析报告

**生成时间：** 2025-10-05  
**分析范围：** 基于用户提出的 7 大能力分类

---

## 📌 **快速总结**

根据用户要求的 7 大能力分类，NetherGate 的覆盖情况如下：

| # | 能力分类 | 完成度 | 状态 | 备注 |
|---|---------|--------|------|------|
| 1 | **SMP 协议能力** | 95% | ✅ 基本完成 | 缺服务器心跳事件（接口已补充） |
| 2 | **RCON 命令能力** | 100% | ✅ 完全实现 | 通过 `IGameDisplayApi` 全面覆盖 |
| 3 | **控制台日志监听** | 100% | ✅ 完全实现 | 所有常见事件均已支持 |
| 4 | **Java版服务端网络层** | 40% | ⚠️ 部分实现 | 接口已定义，但底层监听未实现 |
| 5 | **本地文件操作** | 100% | ✅ 完全实现 | 支持所有常见文件操作 |
| 6 | **NBT 数据操作** | 50% | ⚠️ 仅读取 | 接口已补充，但写入功能未实现 |
| 7 | **服务端性能查看** | 100% | ✅ 完全实现 | 支持 CPU/内存/TPS/MSPT 等指标 |

**整体覆盖率：** **约 85%**

---

## 🔍 **详细分析**

### **1. SMP 协议能力（95%）**

#### ✅ 已完成
- 白名单管理（增删改查）
- 封禁玩家/IP 管理
- 在线玩家管理、踢出玩家
- 管理员（OP）管理
- 服务器状态查询、保存世界、停止服务器
- 游戏规则读取和修改
- 服务器设置管理
- 实时事件推送（玩家加入/离开、白名单变更等）

#### ⚠️ 新增但未实现
- **服务器心跳事件**（`ServerHeartbeatEvent`）
  - **接口位置：** `src/NetherGate.API/Events/SmpEvents.cs`（已补充）
  - **实现要求：** 在 `src/NetherGate.Core/Protocol/SmpClient.cs` 中订阅 `server/status` 通知
  - **配置依赖：** `server.properties` 的 `status-heartbeat-interval`（默认 0，需手动启用）

---

### **2. RCON 命令能力（100%）✅**

#### ✅ 完全实现
通过 `IGameDisplayApi` 接口，插件可以执行所有 Minecraft Java 1.21.9 原版命令：

- **玩家控制：** `/give`, `/tp`, `/gamemode`, `/xp`, `/clear`
- **实体控制：** `/summon`, `/effect`
- **世界控制：** `/setblock`, `/fill`, `/weather`, `/time`, `/difficulty`, `/setworldspawn`
- **进度/配方：** `/advancement`, `/recipe`
- **队伍管理：** `/team`
- **游戏显示：** `bossbar`, `scoreboard`, `title`, `actionbar`
- **对话框（1.21.6+）：** `/dialog`
- **聊天消息：** `tellraw`（支持颜色和格式化）
- **粒子和声音：** `/particle`, `/playsound`

**接口文档：** `src/NetherGate.API/GameDisplay/IGameDisplayApi.cs`

---

### **3. 控制台日志监听（100%）✅**

#### ✅ 已实现事件
- `ServerLogEvent` - 所有服务器日志输出
- `PlayerChatEvent` - 玩家聊天（日志解析）
- `PlayerCommandEvent` - 玩家执行命令（日志解析）
- `PlayerJoinedServerEvent` - 玩家加入（日志解析）
- `PlayerLeftServerEvent` - 玩家离开（日志解析）
- `PlayerDeathEvent` - 玩家死亡消息
- `PlayerAchievementEvent` - 玩家成就/进度
- `ServerProcessStartedEvent` / `ServerProcessStoppedEvent` / `ServerProcessCrashedEvent`

**解析器位置：** `src/NetherGate.Core/Process/LogParser.cs`

---

### **4. Java版服务端网络层（40%）⚠️**

#### 🆕 新增接口（待实现）
为了支持低级网络事件，已新增 `NetworkEvents.cs`：

**文件位置：** `src/NetherGate.API/Events/NetworkEvents.cs`

**新定义的事件：**
- `PlayerConnectionAttemptEvent` - 玩家连接握手
- `PlayerLoginStartEvent` / `PlayerLoginSuccessEvent` / `PlayerLoginFailedEvent`
- `PlayerDisconnectedEvent` - 玩家断开连接
- `PacketReceivedEvent` / `PacketSentEvent` - 数据包监控（低级）
- `ServerStatusQueryEvent` / `ServerPingEvent` - 服务器列表查询
- `NetworkExceptionEvent` - 网络异常
- `MaliciousPacketDetectedEvent` - 恶意数据包检测
- `NetworkTrafficEvent` - 网络流量统计

#### ⚠️ 实现难点
1. **原版服务器不暴露网络层事件**
   - 需要使用 **Paper/Spigot API** 或 **Fabric/Forge Mod**

2. **技术方案：**
   - **方案 A（推荐）：** 配合 Paper 服务器 + Java 插件，将事件转发到 NetherGate（WebSocket/RCON）
   - **方案 B：** 使用 Fabric Mod 注入网络层钩子
   - **方案 C：** 通过 Proxy（Velocity/BungeeCord）监听部分事件

3. **现有替代方案：**
   - 玩家加入/离开 → 通过 **SMP 事件** 或 **日志解析**（已实现）
   - 玩家聊天/命令 → 通过 **日志解析**（已实现）

#### 📝 建议
- **短期：** 使用现有的 SMP 事件和日志解析满足基本需求
- **长期：** 开发配套的 Paper 插件或 Fabric Mod 实现完整网络层监控

---

### **5. 本地文件操作（100%）✅**

#### ✅ 完全实现
通过 `IServerFileAccess` 接口：

- ✅ 读写文本文件
- ✅ 读写 JSON 文件
- ✅ 读写 `server.properties`
- ✅ 文件/目录检查、创建、删除
- ✅ 文件列表（支持通配符）
- ✅ 自动备份机制

**相关接口：**
- `IFileWatcher` - 监听文件变更
- `IBackupManager` - 创建和恢复备份

**接口文档：** `src/NetherGate.API/FileSystem/IServerFileAccess.cs`

---

### **6. NBT 数据操作（50%）⚠️**

#### ✅ 已实现（读取）
通过 `IPlayerDataReader` 和 `IWorldDataReader`：

- ✅ 玩家位置、生命值、饱食度、XP、游戏模式
- ✅ 玩家背包（含附魔、自定义名称）
- ✅ 玩家盔甲、状态效果
- ✅ 玩家统计数据、进度（JSON）
- ✅ 世界信息（种子、出生点、世界边界、游戏规则）

#### 🆕 新增接口（写入，待实现）
**文件位置：** `src/NetherGate.API/Data/INbtDataWriter.cs`

**新定义的方法：**
- 更新玩家位置、生命值、饱食度、经验、游戏模式
- 添加/移除/更新背包物品
- 给予/移除状态效果
- 更新玩家盔甲
- 更新世界出生点、边界、时间、天气
- 创建实体 NBT、创建物品 NBT
- 通用 NBT 文件读写

#### ⚠️ 实现注意事项
1. **文件锁冲突：** 服务器运行时修改 NBT 文件可能导致数据损坏
   - **建议：** 仅在服务器停止时修改，或提供"安全模式"（停服→修改→启服）
   
2. **RCON 替代方案：**
   - 部分功能可通过 RCON 命令实现（如 `/data modify`），更安全
   - 但某些操作（如批量背包修改）只能通过 NBT 实现

3. **实现优先级：**
   - **P0：** 背包操作、位置修改、生命值/经验修改
   - **P1：** 状态效果、盔甲、世界数据
   - **P2：** 通用 NBT 操作（需要安全机制）

---

### **7. 服务端性能查看（100%）✅**

#### ✅ 完全实现
通过 `IPerformanceMonitor` 接口：

- ✅ CPU 使用率
- ✅ 内存使用（堆内存、非堆内存、GC 统计）
- ✅ TPS（需要 Paper/Purpur）
- ✅ MSPT（每 tick 毫秒数）
- ✅ 线程数、活动线程
- ✅ 系统总内存/可用内存

**接口文档：** `src/NetherGate.API/Monitoring/IPerformanceMonitor.cs`

---

## 🎯 **实现优先级建议**

### **P0 - 立即实现（简单）**
1. ✅ **SMP 心跳事件** - 接口已补充，只需在 `SmpClient` 中订阅通知
   - **工作量：** ~30 分钟
   - **文件：** `src/NetherGate.Core/Protocol/SmpClient.cs`

### **P1 - 短期实现（1-2 周）**
2. ✅ **NBT 写入核心实现** - 接口已定义，需要实现具体逻辑
   - **重点功能：** 玩家背包操作、位置修改、生命值/经验修改
   - **工作量：** ~3-5 天
   - **文件：** 创建 `src/NetherGate.Core/Data/NbtDataWriter.cs`

### **P2 - 中期实现（1-2 月）**
3. **基础网络事件** - 实现常用的网络事件
   - 玩家连接、登录、断开
   - 网络异常检测
   - **技术方案：** 开发配套的 Paper 插件转发事件
   - **工作量：** ~1-2 周

### **P3 - 长期实现（3+ 月）**
4. **数据包级别监控** - 深度网络调试功能
   - `PacketReceivedEvent` / `PacketSentEvent`
   - 恶意数据包检测
   - **技术方案：** Fabric Mod 或 Paper 插件
   - **工作量：** ~2-3 周

---

## 📚 **新增文件清单**

| 文件路径 | 说明 | 状态 |
|---------|------|------|
| `src/NetherGate.API/Events/NetworkEvents.cs` | 网络层事件接口定义 | 🆕 已创建 |
| `src/NetherGate.API/Data/INbtDataWriter.cs` | NBT 数据写入接口定义 | 🆕 已创建 |
| `docs/EVENT_SYSTEM_COVERAGE.md` | 事件系统完整覆盖文档 | 🆕 已创建 |
| `docs/MISSING_FEATURES_ANALYSIS.md` | 遗漏功能分析报告（本文档） | 🆕 已创建 |

---

## 🔄 **更新的文件**

| 文件路径 | 变更内容 |
|---------|---------|
| `src/NetherGate.API/Events/SmpEvents.cs` | 新增 `ServerHeartbeatEvent` |
| `src/NetherGate.API/NetherGate.API.csproj` | 添加 `fNbt` 包引用 |

---

## 💡 **给插件开发者的建议**

### **现在可以直接使用：**
✅ **SMP 管理：** 白名单、封禁、玩家、OP、游戏规则  
✅ **RCON 命令：** 所有原版命令（通过 `IGameDisplayApi`）  
✅ **日志监听：** 玩家聊天、命令、加入/离开、死亡、成就  
✅ **文件操作：** 读写服务器文件、配置、备份  
✅ **NBT 读取：** 玩家和世界数据  
✅ **性能监控：** CPU、内存、TPS、MSPT  

### **暂时需要替代方案：**
⚠️ **低级网络事件** → 使用 SMP 事件或日志解析  
⚠️ **NBT 数据写入** → 使用 RCON 命令（如 `/data modify`）或等待实现  

---

## 📞 **后续跟进**

如果用户需要优先实现某个功能，建议按照以下顺序：

1. **SMP 心跳事件** - 最简单，30 分钟完成
2. **NBT 写入核心功能** - 需求较高，建议优先实现背包操作
3. **网络事件（基础）** - 需要额外开发配套工具（Paper 插件）

---

**文档维护者：** NetherGate 开发团队  
**最后更新：** 2025-10-05
