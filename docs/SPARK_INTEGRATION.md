# spark 性能监控集成

NetherGate 集成了 [spark](https://spark.lucko.me/) Standalone Agent，为所有 Minecraft 服务器类型（包括 Vanilla）提供强大的性能分析和监控能力。

---

## 📊 spark 简介

[spark](https://spark.lucko.me/) 是一个强大的 Minecraft 服务器性能分析工具，支持：

- ✅ **CPU 性能分析** - 识别导致卡顿的代码
- ✅ **内存分析** - 检测内存泄漏
- ✅ **TPS 监控** - 实时查看服务器性能
- ✅ **健康检查** - 全面的服务器诊断
- ✅ **支持所有服务器** - Vanilla, Paper, Spigot, Forge, Fabric 等

### spark 集成类型

NetherGate 支持两种 spark 集成方式：

#### 1. Standalone Agent（独立代理版）

spark Standalone Agent 通过 Java `-javaagent` 参数注入到服务器进程。

**官方文档**: https://spark.lucko.me/docs/Standalone-Agent

**优势**:
- 无需安装插件/模组
- 支持所有 Java 版服务器（Vanilla、Paper、Spigot、Forge、Fabric 等）
- 更底层的 JVM 性能数据
- 通过 SSH 协议提供交互式 shell

**适用场景**:
- 需要深度性能分析
- 服务器不支持插件/模组（如 Vanilla）
- 使用 `launch_method = "java"` 启动模式

#### 2. Plugin/Mod（插件/模组版）

通过 RCON 与服务器上已安装的 spark 插件/模组交互。

**优势**:
- 无需修改 JVM 参数
- 服务器已有 spark 插件时直接使用
- 完全通过 RCON 控制
- 适合脚本启动模式

**适用场景**:
- 服务器已安装 spark 插件/模组
- 使用 `launch_method = "script"` 启动模式
- 不希望修改 JVM 参数

**选择建议**:
- 如果使用 Java 启动模式且未安装 spark 插件，推荐 **Standalone Agent**
- 如果使用脚本启动或已有 spark 插件，推荐 **Plugin/Mod**
- 如果两者都可用，**Standalone Agent** 提供更全面的数据

---

## 🎯 功能对比

| 对比项 | RCON (Paper/Purpur) | spark Standalone | spark Plugin |
|-------|---------------------|------------------|--------------|
| **服务器支持** | 仅 Paper/Purpur | 所有服务器（包括 Vanilla） | 支持插件/模组的服务器 |
| **TPS/MSPT** | ✅ | ✅ | ✅ |
| **CPU 性能分析** | ❌ | ✅ | ✅ |
| **内存分析** | ❌ | ✅ | ✅ |
| **卡顿分析** | ❌ | ✅ | ✅ |
| **健康诊断** | ❌ | ✅ | ✅ |
| **Web 可视化** | ❌ | ✅ | ✅ |
| **JVM 深度监控** | ❌ | ✅ | 部分 |
| **需要安装插件/模组** | ❌ | ❌ | ✅ |
| **需要修改 JVM** | ❌ | ✅ | ❌ |
| **交互方式** | TCP (RCON) | SSH | RCON |

**结论**: 
- **RCON**: 基础性能数据，仅限 Paper/Purpur
- **spark Standalone**: 最全面的性能监控，支持所有服务器
- **spark Plugin**: 平衡的选择，适合已有插件的服务器

---

## ⚙️ 配置说明

### 1. Standalone Agent 配置

在 `nethergate-config.json` 中配置：

```json
{
  "server_process": {
    "launch_method": "java",  // 推荐 java 模式（自动注入）
    "server": {
      "working_directory": "./minecraft_server"  // spark 会下载到这里
    }
  },
  "spark": {
    "enabled": true,                      // 启用 spark
    "type": "standalone",                 // 使用独立代理版
    "force_enable_for_script_mode": false,// 脚本模式强制启用
    "auto_download": true,                // 自动下载
    "agent_jar": null,                    // 留空自动下载
    "ssh_port": 2222,                     // SSH 端口
    "ssh_password": null,                 // 留空自动生成
    "auto_start_profiling": false,        // 启动时自动分析
    "version": null,                      // 留空使用最新版
    "download_url": "https://spark.lucko.me/download/stable"
  }
}
```

### 2. Plugin/Mod 配置

如果服务器已安装 spark 插件/模组，使用此配置：

```json
{
  "rcon": {
    "enabled": true,
    "host": "localhost",
    "port": 25575,
    "password": "your_rcon_password"
  },
  "spark": {
    "enabled": true,
    "type": "plugin"  // 使用插件/模组版
  }
}
```

**要求**:
- 必须启用并正确配置 RCON
- 服务器必须已安装 spark 插件/模组
- NetherGate 会自动检测 spark 是否可用

### 3. 配置项详解

| 配置项 | 类型 | 默认值 | 说明 |
|-------|------|--------|------|
| `enabled` | boolean | `false` | 是否启用 spark |
| `type` | string | `"standalone"` | spark 类型：`"standalone"` 或 `"plugin"` |
| `force_enable_for_script_mode` | boolean | `false` | 脚本模式下是否强制启用（仅 standalone）⚠️ |
| `auto_download` | boolean | `true` | 是否自动下载 spark agent（仅 standalone） |
| `agent_jar` | string? | `null` | spark agent jar 路径（仅 standalone），留空自动下载 |
| `ssh_port` | int | `2222` | spark SSH 接口监听端口（仅 standalone） |
| `ssh_password` | string? | `null` | SSH 密码（仅 standalone），留空自动生成 |
| `auto_start_profiling` | boolean | `false` | 启动时自动开始性能分析（仅 standalone） |
| `version` | string? | `null` | spark 版本（仅 standalone），留空使用最新版 |
| `download_url` | string | `...` | spark 下载地址（仅 standalone），可配置镜像源 |

### 4. ⚠️ 启动模式说明（仅 Standalone）

**Java 模式（推荐）**
- `launch_method = "java"`: spark 自动注入 `-javaagent` 参数
- 无需任何额外配置
- ✅ **推荐使用**

**脚本模式**
- `launch_method = "script"`: 默认跳过 spark
- 如需启用，设置 `force_enable_for_script_mode = true`
- ⚠️ **需要手动在脚本中添加 `-javaagent` 参数**

### 5. 存放位置（仅 Standalone）

spark agent jar 会下载到**服务器工作目录**（与 server.jar 同目录）：

```
minecraft_server/
├── server.jar
├── spark-standalone-agent.jar  ✨ spark agent
├── world/
├── plugins/
└── ...
```

**优势：**
- ✅ 与服务端文件集中管理
- ✅ 便于备份和迁移
- ✅ 符合服务器目录结构习惯

---

## 🚀 使用说明

### 启动 NetherGate

当 spark 启用时，NetherGate 会：

1. **自动下载** spark agent（如果未指定路径）
2. **注入参数** 在 Java 启动命令中添加 `-javaagent`
3. **显示连接信息** 在服务器启动后打印 SSH 连接指令

### 控制台输出示例

```
[INFO] 服务器进程已启动 (PID: 12345)
============================================================
spark Standalone Agent 已启动
============================================================
SSH 端口: 2222
SSH 密码: Abc123XyZ456

连接命令:
  ssh -p 2222 spark@localhost

可用命令:
  /spark profiler start        - 开始性能分析
  /spark profiler stop         - 停止并生成报告
  /spark tps                   - 查看 TPS
  /spark health                - 查看服务器健康状况
  exit                         - 断开连接
============================================================
```

### 连接到 spark SSH 接口

1. **打开新终端**

2. **执行 SSH 命令**:
   ```bash
   ssh -p 2222 spark@localhost
   ```

3. **输入密码** (见上方控制台输出)

4. **开始使用 spark 命令**

---

## 💻 常用命令

### TPS 监控

```bash
# 查看实时 TPS
/spark tps

# 输出示例:
# TPS from last 5s, 10s, 1m, 5m, 15m:
# ▃▅▇ 20.0, 20.0, 19.98, 19.95, 19.92
```

### CPU 性能分析

```bash
# 开始性能分析（默认 30 秒）
/spark profiler start

# 开始性能分析（指定时长）
/spark profiler start --timeout 60

# 停止分析并生成报告
/spark profiler stop

# 输出示例:
# Profiler finished, took 30 seconds
# Results: https://spark.lucko.me/XXXXXX
```

### 内存分析

```bash
# 内存使用情况
/spark health

# 查看堆内存详情
/spark heapsummary

# 垃圾回收统计
/spark gc
```

### 服务器诊断

```bash
# 全面健康检查
/spark health

# 输出示例:
# --- Server Health ---
# ● TPS: 20.0
# ● Memory: 2048MB / 4096MB (50%)
# ● CPU: 45%
# ● Player Count: 15 / 100
# ● Chunk Count: 1234
```

---

## 🔧 自动下载机制

### 下载流程

1. NetherGate 检查服务器工作目录（默认 `minecraft_server/`）
2. 如果 spark agent 不存在且 `auto_download = true`，自动从官方下载最新版本
3. 下载完成后保存到服务器目录：`minecraft_server/spark-standalone-agent.jar`
4. 下次启动直接使用已下载的版本

### 禁用自动下载

如果您需要手动管理 spark agent：

```json
{
  "spark": {
    "enabled": true,
    "auto_download": false,  // 禁用自动下载
    "agent_jar": "spark-1.10.53-standalone-agent.jar"  // 必须指定路径
  }
}
```

**路径规则：**
- **相对路径**: 相对于服务器工作目录
  - `"agent_jar": "spark-agent.jar"` → `minecraft_server/spark-agent.jar`
- **绝对路径**: 使用完整路径
  - `"agent_jar": "/opt/minecraft/spark-agent.jar"`

### 手动指定 spark agent

如果您已有 spark agent jar 文件：

```json
{
  "spark": {
    "enabled": true,
    "auto_download": false,  // 禁用自动下载
    "agent_jar": "my-custom-spark.jar"  // 指定文件名
  }
}
```

### 配置镜像源

如果无法访问官方下载地址，可配置镜像：

```json
{
  "spark": {
    "enabled": true,
    "auto_download": true,
    "download_url": "https://your-mirror.com/spark/spark-agent.jar"
  }
}
```

---

## 📈 性能分析最佳实践

### 1. **识别卡顿源**

```bash
# 当服务器卡顿时，立即开始分析
/spark profiler start --timeout 120

# 等待卡顿再次发生

# 停止并查看报告
/spark profiler stop
```

### 2. **定期健康检查**

建议每天检查一次服务器健康状况：

```bash
/spark health
/spark tps
```

### 3. **长期性能监控**

可以结合 NetherGate 的 `IPerformanceMonitor` 自动记录性能数据：

```csharp
// 插件代码
var perfMonitor = Context.PerformanceMonitor;
perfMonitor.Start(intervalSeconds: 60);

perfMonitor.PerformanceWarning += (sender, warning) =>
{
    if (warning.Type == PerformanceWarningType.HighCpuUsage)
    {
        // 自动触发 spark 分析
        TriggerSparkProfiling();
    }
};
```

### 4. **分析 Web 报告**

spark 会生成在线可视化报告，例如：
- https://spark.lucko.me/XXXXXX

报告包含：
- 🔥 **火焰图** - CPU 调用栈可视化
- 📊 **方法耗时** - 按方法排序的时间消耗
- 🧵 **线程分析** - 每个线程的活动情况

---

## 🎯 与 RCON TPS 的对比

### RCON 方案（Paper/Purpur）

```csharp
// 通过 RCON 获取 TPS
var rconPerf = Server.RconClient as IRconPerformance;
var tpsData = await rconPerf.GetTpsAsync();

// 优点: 简单直接
// 缺点: 仅支持 Paper/Purpur，无深度分析
```

### spark 方案（所有服务器）

```bash
# 通过 spark SSH 获取 TPS
ssh -p 2222 spark@localhost
/spark tps

# 优点: 支持所有服务器，提供深度分析
# 缺点: 需要 SSH 连接（可自动化）
```

### 推荐方案

- **Paper/Purpur 服务器**: 使用 **RCON + spark** 组合
  - RCON 用于快速获取 TPS/MSPT
  - spark 用于深度性能分析

- **Vanilla/其他服务器**: 使用 **spark**
  - 是唯一的性能监控方案

---

## 📚 spark 命令速查

| 命令 | 功能 |
|------|------|
| `/spark tps` | 查看 TPS |
| `/spark health` | 服务器健康检查 |
| `/spark profiler start` | 开始性能分析 |
| `/spark profiler stop` | 停止并生成报告 |
| `/spark profiler cancel` | 取消当前分析 |
| `/spark heapsummary` | 堆内存摘要 |
| `/spark gc` | 垃圾回收统计 |
| `/spark tickmonitor` | 监控刻循环 |
| `/spark activity` | 查看最近活动 |
| `exit` | 退出 SSH 连接 |

完整命令列表: https://spark.lucko.me/docs/Command-Usage

---

## 🔐 安全注意事项

### SSH 密码管理

1. **自动生成密码** (推荐)
   - 每次启动生成新密码
   - 密码显示在控制台，仅当次有效

2. **固定密码**
   ```json
   {
     "spark": {
       "ssh_password": "YourSecurePassword123"
     }
   }
   ```
   - 便于自动化脚本连接
   - 注意密码安全

### 端口安全

- spark SSH 默认监听 `localhost:2222`
- **不要**将此端口暴露到公网
- 如需远程访问，使用 SSH 隧道：
  ```bash
  ssh -L 2222:localhost:2222 user@your-server
  ```

---

## 🐛 故障排查

### 1. spark agent 下载失败

**问题**: 无法连接到 spark 官方下载地址

**解决**:
- 配置镜像源（见上方）
- 手动下载并指定 `agent_jar` 路径

### 2. SSH 连接被拒绝

**问题**: `Connection refused`

**检查**:
1. 确认 spark 已启用: `"enabled": true`
2. 确认端口正确: 默认 `2222`
3. 确认服务器已启动
4. 查看 NetherGate 日志是否有错误

### 3. 启动参数未注入

**问题**: spark agent 未生效

**检查**:
1. 确认 `launch_method` 为 `"java"`（或脚本模式已设置 `force_enable_for_script_mode = true`）
2. 查看启动日志，确认有 `-javaagent` 参数
3. 确认 Java 版本兼容（Java 8+）

**脚本模式解决方案：**

如果使用脚本启动，需要手动添加 `-javaagent` 参数：

```bash
#!/bin/bash
# start.sh

java -javaagent:spark-standalone-agent.jar=port=2222 \
     -Xms2G -Xmx4G \
     -jar server.jar nogui
```

配置文件：
```json
{
  "server_process": { "launch_method": "script" },
  "spark": {
    "enabled": true,
    "force_enable_for_script_mode": true,  // 启用此项
    "auto_download": true
  }
}
```

启动时会显示需要添加的参数：
```
⚠️ 注意: 当前为脚本启动模式
⚠️ 需要在启动脚本中手动添加以下 JVM 参数:
⚠️   -javaagent:minecraft_server/spark-standalone-agent.jar=port=2222
```

### 4. 密码不正确

**问题**: SSH 密码验证失败

**解决**:
- 查看 NetherGate 控制台输出的密码
- 注意区分大小写
- 如果忘记密码，重启服务器会生成新密码

---

## 🚀 高级用法

### 自动化脚本

```bash
#!/bin/bash
# 自动获取 TPS 并记录

PASSWORD="YourSparkPassword"

sshpass -p "$PASSWORD" ssh -p 2222 -o StrictHostKeyChecking=no spark@localhost << EOF
/spark tps
exit
EOF
```

### 定时性能分析

```bash
#!/bin/bash
# 每小时进行一次性能分析

while true; do
  sshpass -p "$PASSWORD" ssh -p 2222 spark@localhost << EOF
  /spark profiler start --timeout 300
  exit
EOF
  
  sleep 3600  # 1 小时
done
```

---

## 📚 相关文档

- [spark 官方文档](https://spark.lucko.me/docs)
- [TPS/MSPT 监控指南](TPS_MSPT_MONITORING.md)
- [配置文件说明](CONFIGURATION.md)
- [服务器进程管理](SERVER_PROCESS.md)

---

## 🔮 未来计划

- [ ] SSH 客户端自动化（无需手动连接）
- [ ] spark 数据自动采集到数据库
- [ ] Web 界面集成 spark 查看器
- [ ] 自动触发性能分析（当 TPS 低时）
- [ ] 性能报告自动归档

---

**总结**: spark Standalone Agent 为 NetherGate 提供了业界领先的性能监控能力，支持所有 Minecraft 服务器类型，是服务器运维的必备工具。

