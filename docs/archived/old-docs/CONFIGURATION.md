# NetherGate 配置指南

本文档详细说明 NetherGate 的所有配置选项。

---

## 📋 目录

- [配置文件位置](#配置文件位置)
- [服务器进程管理](#服务器进程管理-server_process)
- [SMP 连接配置](#smp-连接配置-server_connection)
- [RCON 客户端配置](#rcon-客户端配置-rcon)
- [日志监听器配置](#日志监听器配置-log_listener)
- [插件管理配置](#插件管理配置-plugins)
- [日志系统配置](#日志系统配置-logging)
- [高级配置](#高级配置-advanced)
- [完整配置示例](#完整配置示例)

---

## 配置文件位置

NetherGate 配置文件位于程序根目录，**支持 JSON 和 YAML 两种格式**。

```
NetherGate/
├── nethergate-config.yaml  # 主配置文件（YAML 格式，推荐）
├── nethergate-config.yml   # 或使用 .yml 扩展名
└── nethergate-config.json  # 或使用 JSON 格式
```

**配置文件优先级：**
1. `nethergate-config.yaml` (优先级最高)
2. `nethergate-config.yml`
3. `nethergate-config.json`

**首次启动流程：**
1. NetherGate 检测到配置文件不存在
2. 自动生成 `nethergate-config.yaml`（默认使用 YAML 格式）
3. 提示用户修改重要配置项
4. 程序退出，等待用户编辑配置后重启

**格式选择建议：**
- **YAML** - 推荐使用，支持注释，可读性强，适合复杂配置
- **JSON** - 兼容性好，适合程序化处理

> 💡 **提示**：可以同时存在多个格式的配置文件，系统会按优先级加载第一个找到的文件

---

## 服务器进程管理 (server_process)

NetherGate 可以自动启动和管理 Minecraft 服务器进程。

### 基础配置

```json
{
    "server_process": {
        "enabled": true,
        "java": {
            "path": "java",
            "version_check": true
        },
        "server": {
            "jar": "server.jar",
            "working_directory": "./minecraft_server"
        }
    }
}
```

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `enabled` | boolean | `true` | 是否启用进程管理。`false` 时假设服务器已运行 |
| `java.path` | string | `"java"` | Java 路径，支持：<br>• `"java"` - 系统 PATH<br>• `"JAVA_HOME"` - 环境变量<br>• 绝对路径 |
| `java.version_check` | boolean | `true` | 启动前检查 Java 版本 |
| `server.jar` | string | `"server.jar"` | 服务器 JAR 文件名 |
| `server.working_directory` | string | `"./minecraft_server"` | 服务器工作目录 |

### 内存配置

```json
{
    "memory": {
        "min": 2048,
        "max": 4096
    }
}
```

| 配置项 | 类型 | 单位 | 说明 |
|--------|------|------|------|
| `min` | number | MB | 最小内存 (`-Xms`) |
| `max` | number | MB | 最大内存 (`-Xmx`) |

**建议值：**
- 小型服务器（<10人）：2GB - 4GB
- 中型服务器（10-50人）：4GB - 8GB
- 大型服务器（50+人）：8GB+

### JVM 参数配置

```json
{
    "arguments": {
        "jvm_prefix": [],
        "jvm_middle": [
            "-XX:+UseG1GC",
            "-Dfile.encoding=UTF-8"
        ],
        "server": [
            "--nogui"
        ]
    }
}
```

**完整启动命令格式：**
```
java [jvm_prefix] -Xms<min>M -Xmx<max>M [jvm_middle] -jar <jar> [server]
```

**示例命令：**
```bash
java -Xms2048M -Xmx4096M -XX:+UseG1GC -Dfile.encoding=UTF-8 -jar server.jar --nogui
```

#### jvm_prefix (JVM 前置参数)

在 `-Xms/-Xmx` **之前**的参数。

**常用场景：**
```json
"jvm_prefix": [
    "-Dlog4j2.formatMsgNoLookups=true"  // Log4j 漏洞修复
]
```

#### jvm_middle (JVM 中间参数)

在 `-Xms/-Xmx` **之后**，`-jar` **之前**的参数。

**G1GC 推荐配置（默认）：**
```json
"jvm_middle": [
    "-XX:+UseG1GC",
    "-XX:+ParallelRefProcEnabled",
    "-XX:MaxGCPauseMillis=200",
    "-XX:+UnlockExperimentalVMOptions",
    "-XX:+DisableExplicitGC",
    "-XX:+AlwaysPreTouch",
    "-XX:G1NewSizePercent=30",
    "-XX:G1MaxNewSizePercent=40",
    "-XX:G1HeapRegionSize=8M",
    "-XX:G1ReservePercent=20",
    "-XX:G1HeapWastePercent=5",
    "-XX:G1MixedGCCountTarget=4",
    "-XX:InitiatingHeapOccupancyPercent=15",
    "-XX:G1MixedGCLiveThresholdPercent=90",
    "-XX:G1RSetUpdatingPauseTimePercent=5",
    "-XX:SurvivorRatio=32",
    "-XX:+PerfDisableSharedMem",
    "-XX:MaxTenuringThreshold=1",
    "-Dfile.encoding=UTF-8"
]
```

**Aikar's Flags（经典优化）：**
```json
"jvm_middle": [
    "-XX:+UseG1GC",
    "-XX:+ParallelRefProcEnabled",
    "-XX:MaxGCPauseMillis=200",
    "-XX:+UnlockExperimentalVMOptions",
    "-XX:+DisableExplicitGC",
    "-XX:+AlwaysPreTouch",
    "-XX:G1NewSizePercent=30",
    "-XX:G1MaxNewSizePercent=40",
    "-XX:G1HeapRegionSize=8M",
    "-XX:G1ReservePercent=20",
    "-XX:G1HeapWastePercent=5",
    "-XX:G1MixedGCCountTarget=4",
    "-XX:InitiatingHeapOccupancyPercent=15",
    "-XX:G1MixedGCLiveThresholdPercent=90",
    "-XX:G1RSetUpdatingPauseTimePercent=5",
    "-XX:SurvivorRatio=32",
    "-XX:+PerfDisableSharedMem",
    "-XX:MaxTenuringThreshold=1"
]
```

**ZGC 配置（Java 17+，大内存服务器）：**
```json
"jvm_middle": [
    "-XX:+UseZGC",
    "-XX:AllocatePrefetchStyle=1",
    "-XX:-ZProactive"
]
```

#### server (服务器参数)

传递给服务器 JAR 的参数。

```json
"server": [
    "--nogui",           // 无 GUI 模式
    "--world myworld",   // 指定世界名称
    "--port 25565"       // 指定端口
]
```

### 监控配置

```json
{
    "monitoring": {
        "startup_timeout": 300,
        "startup_detection": {
            "enabled": true,
            "keywords": ["Done ("]
        },
        "crash_detection": {
            "enabled": true,
            "keywords": ["Exception in server tick loop"]
        }
    }
}
```

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `startup_timeout` | number | 启动超时时间（秒） |
| `startup_detection.enabled` | boolean | 是否启用启动检测 |
| `startup_detection.keywords` | string[] | 启动完成关键词 |
| `crash_detection.enabled` | boolean | 是否启用崩溃检测 |
| `crash_detection.keywords` | string[] | 崩溃关键词 |

### 自动重启配置

```json
{
    "auto_restart": {
        "enabled": true,
        "max_retries": 3,
        "retry_delay": 5000,
        "reset_timer": 600000
    }
}
```

| 配置项 | 类型 | 单位 | 说明 |
|--------|------|------|------|
| `enabled` | boolean | - | 是否启用自动重启 |
| `max_retries` | number | 次 | 最大重试次数 |
| `retry_delay` | number | 毫秒 | 重试延迟 |
| `reset_timer` | number | 毫秒 | 重试计数器重置时间 |

**工作原理：**
1. 服务器崩溃时，NetherGate 自动重启
2. 连续重启 `max_retries` 次后停止（防止无限重启）
3. 如果服务器运行超过 `reset_timer`，重试计数器重置为 0

---

## SMP 连接配置 (server_connection)

服务端管理协议 (SMP) 连接配置，用于结构化管理服务器。

```json
{
    "server_connection": {
        "host": "localhost",
        "port": 40745,
        "secret": "your-40-character-secret-token",
        "use_tls": false,
        "auto_connect": true,
        "connect_delay": 3000,
        "wait_for_server_ready": true
    }
}
```

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `host` | string | SMP 服务器地址 |
| `port` | number | SMP 端口（对应 `management-server-port`） |
| `secret` | string | 认证令牌（对应 `management-server-secret`） |
| `use_tls` | boolean | 是否使用 TLS 加密 |
| `tls_certificate` | string\|null | TLS 证书路径 |
| `tls_password` | string\|null | TLS 证书密码 |
| `reconnect_interval` | number | 重连间隔（毫秒） |
| `heartbeat_timeout` | number | 心跳超时（毫秒） |
| `auto_connect` | boolean | 是否自动连接 |
| `connect_delay` | number | 连接延迟（毫秒） |
| `wait_for_server_ready` | boolean | 是否等待服务器启动完成 |

### 对应的 server.properties 配置

```properties
# 启用 SMP
management-server-enabled=true
management-server-host=localhost
management-server-port=40745
management-server-secret=<你的40位认证令牌>
management-server-tls-enabled=false
```

### 生成认证令牌

**Linux/macOS:**
```bash
openssl rand -base64 30 | tr -d '+/=' | cut -c1-40
```

**Windows PowerShell:**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 40 | % {[char]$_})
```

**在线工具:**
- https://www.random.org/strings/

---

## RCON 客户端配置 (rcon)

RCON 用于执行游戏内命令（如 `/give`, `/tp`, `/tellraw`）。

```json
{
    "rcon": {
        "enabled": true,
        "host": "localhost",
        "port": 25566,
        "password": "your-rcon-password",
        "connect_timeout": 5000,
        "command_timeout": 10000,
        "auto_connect": true,
        "connect_delay": 3000
    }
}
```

| 配置项 | 类型 | 单位 | 说明 |
|--------|------|------|------|
| `enabled` | boolean | - | 是否启用 RCON 客户端 |
| `host` | string | - | RCON 服务器地址 |
| `port` | number | - | RCON 端口（对应 `rcon.port`） |
| `password` | string | - | RCON 密码（对应 `rcon.password`） |
| `connect_timeout` | number | 毫秒 | 连接超时 |
| `command_timeout` | number | 毫秒 | 命令执行超时 |
| `auto_connect` | boolean | - | 是否自动连接 |
| `connect_delay` | number | 毫秒 | 连接延迟 |

### 对应的 server.properties 配置

```properties
# 启用 RCON
enable-rcon=true
rcon.port=25566
rcon.password=<你的RCON密码>
```

---

## spark 性能监控配置 (spark)

spark 是强大的性能分析工具，通过 `-javaagent` 注入实现深度监控。

```json
{
    "spark": {
        "enabled": false,
        "type": "standalone",
        "force_enable_for_script_mode": false,
        "auto_download": true,
        "agent_jar": null,
        "ssh_port": 2222,
        "ssh_password": null,
        "auto_start_profiling": false,
        "version": null,
        "download_url": "https://spark.lucko.me/download/stable"
    }
}
```

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `enabled` | boolean | `false` | 是否启用 spark |
| `type` | string | `"standalone"` | spark 类型：`"standalone"` 或 `"plugin"` |
| `force_enable_for_script_mode` | boolean | `false` | 脚本模式下是否强制启用（仅 standalone） |
| `auto_download` | boolean | `true` | 是否自动下载 spark agent（仅 standalone） |
| `agent_jar` | string? | `null` | spark agent jar 路径（仅 standalone） |
| `ssh_port` | number | `2222` | spark SSH 监听端口（仅 standalone） |
| `ssh_password` | string? | `null` | SSH 密码（仅 standalone，留空自动生成） |
| `auto_start_profiling` | boolean | `false` | 启动时自动开始性能分析（仅 standalone） |
| `version` | string? | `null` | spark 版本（仅 standalone，留空最新版） |
| `download_url` | string | `...` | spark 下载地址（仅 standalone） |

### spark 类型说明

**1. `type = "standalone"`（独立代理版）**

- **原理**: 通过 `-javaagent` JVM 参数注入到服务器进程
- **优势**: 
  - 无需服务器安装插件/模组
  - 支持所有 Java 版服务器（Vanilla、Paper、Spigot、Forge、Fabric 等）
  - 更底层的性能数据（JVM、线程、内存等）
- **交互方式**: SSH 协议
- **适用场景**: 
  - 需要深度性能分析
  - 服务器不支持插件/模组
  - 希望独立于游戏服务器的监控

**2. `type = "plugin"`（插件/模组版）**

- **原理**: 通过 RCON 与已安装的 spark 插件/模组交互
- **优势**:
  - 无需修改 JVM 参数
  - 服务器已安装 spark 插件/模组时直接使用
  - 无需 SSH 连接，完全通过 RCON
- **交互方式**: RCON 命令
- **适用场景**:
  - 服务器已有 spark 插件
  - 使用脚本启动模式
  - 不希望修改 JVM 参数

**选择建议**:
- 如果使用 `launch_method = "java"` 且未安装 spark 插件，推荐 `type = "standalone"`
- 如果使用 `launch_method = "script"` 或已有 spark 插件，推荐 `type = "plugin"`
- 如果两者都可用，`standalone` 提供更全面的数据

### 重要说明

**对于 `type = "standalone"`**:

1. **启动模式限制**
   - `launch_method = "java"`: spark 自动注入（推荐）
   - `launch_method = "script"`: 需要设置 `force_enable_for_script_mode = true` 并在脚本中手动添加 `-javaagent` 参数

2. **存放位置**
   - spark agent 会下载到服务器工作目录（与 server.jar 同目录）
   - 例如：`minecraft_server/spark-standalone-agent.jar`

3. **路径配置**
   - `agent_jar`: 相对路径（相对于服务器目录）或绝对路径
   - 留空且 `auto_download = true` 时自动下载

**对于 `type = "plugin"`**:

1. **前置要求**
   - 必须启用 RCON 并正确配置
   - 服务器必须已安装 spark 插件/模组
   
2. **自动检测**
   - NetherGate 会在服务器启动后自动检测 spark 是否可用
   - 通过 RCON 执行 `spark` 命令验证

3. **功能限制**
   - 相比 standalone 模式，部分底层数据可能不可用
   - 具体功能取决于 spark 插件版本

### 使用场景

**场景 1：独立代理版（自动下载）**
```json
{
    "spark": {
        "enabled": true,
        "type": "standalone",
        "auto_download": true,
        "agent_jar": null,
        "ssh_port": 2222
    }
}
```

**场景 2：独立代理版（手动指定路径）**
```json
{
    "spark": {
        "enabled": true,
        "type": "standalone",
        "auto_download": false,
        "agent_jar": "spark-1.10.53-standalone-agent.jar",
        "ssh_port": 2222
    }
}
```

**场景 3：独立代理版（脚本模式）**
```json
{
    "server_process": { "launch_method": "script" },
    "spark": {
        "enabled": true,
        "type": "standalone",
        "force_enable_for_script_mode": true
    }
}
```

**场景 4：插件/模组版**
```json
{
    "rcon": {
        "enabled": true,
        "host": "localhost",
        "port": 25575,
        "password": "your_password"
    },
    "spark": {
        "enabled": true,
        "type": "plugin"
    }
}
```

启动时会显示需要手动添加的 JVM 参数。

### 功能对比

| 功能 | RCON | spark |
|------|------|-------|
| 支持服务器 | Paper/Purpur | **所有服务器** |
| TPS/MSPT | ✅ | ✅ |
| CPU 性能分析 | ❌ | ✅ |
| 内存分析 | ❌ | ✅ |
| Web 可视化 | ❌ | ✅ |

**详细文档**: [spark 性能监控集成](SPARK_INTEGRATION.md)

---

## 日志监听器配置 (log_listener)

监听服务器日志输出，捕获玩家聊天、命令等事件。

```json
{
    "log_listener": {
        "enabled": true,
        "parsing": {
            "parse_chat": true,
            "parse_commands": true,
            "parse_player_events": true,
            "parse_errors": true
        },
        "filters": {
            "ignore_patterns": [
                "Can't keep up!",
                "moved too quickly!"
            ],
            "log_levels": ["INFO", "WARN", "ERROR"]
        }
    }
}
```

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `enabled` | boolean | 是否启用日志监听 |
| `parsing.parse_chat` | boolean | 解析玩家聊天 |
| `parsing.parse_commands` | boolean | 解析玩家命令 |
| `parsing.parse_player_events` | boolean | 解析玩家加入/离开 |
| `parsing.parse_errors` | boolean | 解析错误日志 |
| `filters.ignore_patterns` | string[] | 忽略的日志模式 |
| `filters.log_levels` | string[] | 处理的日志级别 |

**注意：** 当 `server_process.enabled=true` 时，日志监听自动启用。

---

## 插件管理配置 (plugins)

```json
{
    "plugins": {
        "directory": "plugins",
        "auto_load": true,
        "hot_reload": true,
        "enabled_plugins": ["*"],
        "disabled_plugins": [],
        "load_after_server_ready": true,
        "load_timeout": 30
    }
}
```

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `directory` | string | 插件目录 |
| `auto_load` | boolean | 自动加载插件 |
| `hot_reload` | boolean | 热重载支持（实验性） |
| `enabled_plugins` | string[] | 启用的插件列表，`["*"]` 表示全部 |
| `disabled_plugins` | string[] | 禁用的插件列表 |
| `load_after_server_ready` | boolean | 服务器启动后加载插件 |
| `load_timeout` | number | 插件加载超时（秒） |

---

## 日志系统配置 (logging)

NetherGate 自身的日志系统（不是 MC 服务器日志）。

```json
{
    "logging": {
        "level": "Info",
        "console": {
            "enabled": true,
            "colored": true,
            "show_server_output": true,
            "server_output_prefix": "[MC] "
        },
        "file": {
            "enabled": true,
            "path": "logs/latest.log",
            "max_size": 10485760,
            "max_files": 10
        }
    }
}
```

### 日志级别

| 级别 | 说明 | 使用场景 |
|------|------|---------|
| `Trace` | 最详细 | 深度调试 |
| `Debug` | 调试信息 | 开发阶段 |
| `Info` | 一般信息 | **生产环境推荐** |
| `Warning` | 警告信息 | 只记录警告和错误 |
| `Error` | 错误信息 | 只记录错误 |
| `Fatal` | 致命错误 | 最小日志 |

### 日志归档

NetherGate 会在每次启动时自动归档上一次的日志文件：

**归档规则**：
- **主日志文件**: `logs/latest.log`（始终保存最新日志）
- **归档格式**: `logs/yyyy-MM-dd-N.log.gz`（GZip 压缩）
- **命名规则**: 
  - `2025-10-04-1.log.gz` - 10月4日第1次启动的归档
  - `2025-10-04-2.log.gz` - 10月4日第2次启动的归档
  - `2025-10-04-3.log.gz` - 10月4日第3次启动的归档

**归档流程**：
1. 启动时检查 `latest.log` 是否存在
2. 如果存在且不为空，将其压缩为 `yyyy-MM-dd-N.log.gz`
3. 自动删除原 `latest.log` 文件
4. 创建新的 `latest.log` 开始记录

**优势**：
- ✅ 自动压缩，节省磁盘空间（通常压缩率 80-90%）
- ✅ 按日期和启动次数组织，便于追溯问题
- ✅ 始终保持 `latest.log` 为最新日志
- ✅ 压缩后的日志文件可用任何解压工具打开

**示例**：
```
logs/
├── latest.log              # 当前运行的日志（1.2 MB）
├── 2025-10-04-1.log.gz    # 今天第1次启动归档（150 KB）
├── 2025-10-04-2.log.gz    # 今天第2次启动归档（180 KB）
├── 2025-10-03-1.log.gz    # 昨天第1次启动归档（200 KB）
└── 2025-10-03-2.log.gz    # 昨天第2次启动归档（120 KB）
```

---

## 高级配置 (advanced)

```json
{
    "advanced": {
        "performance": {
            "enabled": false,
            "report_interval": 60
        },
        "security": {
            "hide_secrets_in_logs": true,
            "plugin_sandbox": false
        },
        "experimental": {
            "enabled": false,
            "hot_reload": false,
            "web_interface": false
        }
    }
}
```

---

## 完整配置示例

### 最小配置（快速开始）

```json
{
    "server_process": {
        "enabled": true,
        "java": { "path": "java" },
        "server": {
            "jar": "server.jar",
            "working_directory": "./minecraft_server"
        },
        "memory": { "min": 2048, "max": 4096 }
    },
    "server_connection": {
        "host": "localhost",
        "port": 40745,
        "secret": "your-40-character-secret-token"
    },
    "rcon": {
        "enabled": true,
        "port": 25566,
        "password": "your-rcon-password"
    }
}
```

### 生产环境配置

完整配置请参考 [`config.example.json`](../config.example.json)。

---

## 配置验证

NetherGate 启动时会自动验证配置：

✅ **有效配置**
```
[INFO] Configuration loaded successfully
[INFO] Server process management: enabled
[INFO] SMP connection: localhost:40745
[INFO] RCON client: enabled
```

❌ **无效配置**
```
[ERROR] Configuration validation failed:
  - server_connection.secret: must be 40 characters
  - rcon.password: cannot be empty
```

---

## 常见配置场景

### 场景 1：本地开发环境

```json
{
    "server_process": { "enabled": true },
    "server_connection": {
        "port": 40745,
        "use_tls": false
    },
    "logging": { "level": "Debug" }
}
```

### 场景 2：生产服务器

```json
{
    "server_process": { "enabled": true },
    "server_connection": {
        "port": 40745,
        "use_tls": true,
        "tls_certificate": "/path/to/cert.pfx"
    },
    "logging": { "level": "Info" },
    "advanced": {
        "security": { "hide_secrets_in_logs": true }
    }
}
```

### 场景 3：仅连接模式（服务器已运行）

```json
{
    "server_process": { "enabled": false },
    "server_connection": {
        "host": "remote-server.com",
        "port": 40745,
        "secret": "...",
        "auto_connect": true
    }
}
```

---

## 故障排查

### 问题：连接失败

**检查：**
1. `server.properties` 中 `management-server-enabled=true`
2. 端口配置一致
3. 认证令牌正确
4. 防火墙允许连接

### 问题：服务器启动失败

**检查：**
1. Java 路径正确
2. `server.jar` 文件存在
3. 内存配置合理
4. 查看日志：`logs/latest.log`（历史日志会自动归档为 `logs/yyyy-MM-dd-N.log.gz`）

### 问题：插件加载失败

**检查：**
1. 插件目录正确
2. `plugin.json` 格式有效
3. 插件依赖完整
4. 查看插件加载日志

---

## 配置文件示例

完整的配置文件示例可以在以下位置找到：

- [nethergate-config.example.yaml](nethergate-config.example.yaml) - **YAML 格式**（推荐）
- [config.example.json](config.example.json) - JSON 格式

> 💡 **提示**：首次启动 NetherGate 时，会自动生成 YAML 格式的默认配置文件。

---

## 相关文档

- [服务器进程管理](SERVER_PROCESS.md) - 进程管理详解
- [SMP 接口](SMP_INTERFACE.md) - 协议接口说明
- [RCON 集成](RCON_INTEGRATION.md) - RCON 使用指南
- [常见问题 FAQ](FAQ.md) - 问题解答

---

**配置文件随项目版本更新，请关注变更日志。**

