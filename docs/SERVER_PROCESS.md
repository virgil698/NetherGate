# NetherGate 服务器进程管理

本文档详细说明 NetherGate 如何管理 Minecraft 服务器进程。

---

## 📋 概述

NetherGate 可以作为 Minecraft 服务器的**启动器和管理器**，负责：

- 🚀 启动和停止 Minecraft 服务器进程
- 📺 监听并显示服务器输出（标准输出/错误）
- 🔄 自动重启崩溃的服务器
- ⚙️ 灵活配置启动参数（内存、JVM 参数、服务器参数）
- 🎯 检测服务器启动完成状态
- 📊 监控服务器进程状态

### 首次启动初始化

NetherGate 首次启动时会自动创建必要的目录结构：

```
NetherGate/
├── NetherGate.exe           # 主程序
├── config/                  # 配置目录（自动创建）
│   ├── nethergate.json     # 主程序配置（从 config.example.json 复制）
│   ├── plugin-a/           # 插件A配置
│   │   └── config.json
│   └── plugin-b/           # 插件B配置
│       └── config.json
├── plugins/                 # 插件代码目录（自动创建）
│   ├── plugin-a/
│   │   ├── plugin.json
│   │   ├── PluginA.dll
│   │   └── 依赖DLL...
│   └── README.md
└── logs/                   # 日志目录（自动创建）
    ├── latest.log                  # 当前日志
    ├── 2025-10-04-1.log.gz        # 归档日志（自动压缩）
    └── 2025-10-04-2.log.gz
```

**目录说明**：
- **config/**: 统一管理所有配置文件
  - 主程序配置：`config/nethergate.json`
  - 插件配置：`config/<plugin-id>/config.json`
- **plugins/**: 存放插件代码（DLL）和依赖
  - 每个插件一个子目录
  - 包含 `plugin.json` 元数据文件
- **logs/**: 日志文件存放位置

**优势**：
- ✅ 配置和代码分离，便于管理
- ✅ 更新插件代码不影响配置
- ✅ 方便备份和迁移配置
- ✅ 配置文件集中，易于查找

**注意**：
- 首次启动时，如果 `config/nethergate.json` 不存在，会自动从 `config.example.json` 复制并提示用户修改
- `config/`、`plugins/` 和 `logs/` 目录会自动创建
- 示例插件代码位于独立项目 [NetherGate-Samples](https://github.com/YourName/NetherGate-Samples)，可单独下载学习

---

## 🏗️ 架构设计

### 启动流程

```
NetherGate 启动
    ↓
读取配置文件
    ↓
构建 Java 启动命令
    ↓
启动 MC 服务器进程
    ↓
监听标准输出/错误流 ────→ 转发到 NetherGate 控制台
    ↓
检测 "Done" 关键字（服务器启动完成）
    ↓
连接服务端管理协议（WebSocket）
    ↓
加载和初始化插件
    ↓
NetherGate 就绪
```

### 模块结构

```
ServerProcessManager
├── ProcessLauncher          # 进程启动器
├── OutputMonitor            # 输出监控器
├── StateDetector            # 状态检测器
├── CrashHandler             # 崩溃处理器
└── CommandBuilder           # 命令构建器
```

---

## ⚙️ 配置设计

### 配置文件示例

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
        },
        "memory": {
            "min": 2048,
            "max": 4096
        },
        "arguments": {
            "jvm_prefix": [],
            "jvm_middle": [
                "-XX:+UseG1GC",
                "-XX:+ParallelRefProcEnabled",
                "-XX:MaxGCPauseMillis=200",
                "-Dfile.encoding=UTF-8",
                "-Duser.language=zh",
                "-Duser.country=CN"
            ],
            "server": [
                "--nogui",
                "--world-dir",
                "world"
            ]
        },
        "monitoring": {
            "startup_timeout": 300,
            "startup_detection": {
                "enabled": true,
                "keywords": [
                    "Done (",
                    "For help, type \"help\""
                ]
            },
            "crash_detection": {
                "enabled": true,
                "keywords": [
                    "Exception",
                    "Error",
                    "Crash"
                ]
            }
        },
        "auto_restart": {
            "enabled": true,
            "max_retries": 3,
            "retry_delay": 5000,
            "reset_timer": 600000
        }
    },
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

### 配置说明

#### `server_process.enabled`
- **类型**: `boolean`
- **默认**: `true`
- **说明**: 是否启用服务器进程管理。设为 `false` 时，NetherGate 仅连接到已运行的服务器

#### `java.path`
- **类型**: `string`
- **默认**: `"java"`
- **说明**: Java 可执行文件路径
- **路径解析规则**:
  - **相对路径或命令名**（推荐）: 使用环境变量 `PATH` 中的 Java
    - `"java"` - 使用系统环境变量的 Java（最常用）
    - `"java.exe"` - Windows 上指定 .exe 扩展名（可选）
  - **绝对路径**: 直接使用指定的 Java 可执行文件
    - Windows: `"C:\\Program Files\\Java\\jdk-21\\bin\\java.exe"`
    - Linux: `"/usr/lib/jvm/java-21-openjdk/bin/java"`
    - macOS: `"/Library/Java/JavaVirtualMachines/jdk-21.jdk/Contents/Home/bin/java"`
  - **相对于 NetherGate 的相对路径**:
    - `"./java/bin/java"` - 使用 NetherGate 目录下的 java 文件夹
    - `"../jdk-21/bin/java"` - 使用上级目录的 Java
- **推荐配置**: 直接使用 `"java"`，确保系统已正确配置 `JAVA_HOME` 和 `PATH` 环境变量

#### `java.version_check`
- **类型**: `boolean`
- **默认**: `true`
- **说明**: 启动前检查 Java 版本是否满足要求

#### `server.jar`
- **类型**: `string`
- **默认**: `"server.jar"`
- **说明**: Minecraft 服务器 JAR 文件名（相对于 working_directory）

#### `server.working_directory`
- **类型**: `string`
- **默认**: `"./minecraft_server"`
- **说明**: 服务器工作目录

#### `memory.min` / `memory.max`
- **类型**: `number`
- **单位**: MB
- **说明**: 最小/最大内存分配
- **注意**: 会自动转换为 `-Xms` 和 `-Xmx` 参数

#### `arguments.jvm_prefix`
- **类型**: `string[]`
- **说明**: JVM 前置参数（在 `-Xms`/`-Xmx` 之前）
- **用途**: 特殊 JVM 选项，如 `-server`、`-Dfile.encoding` 等
- **示例**:
  ```json
  "jvm_prefix": [
      "-server",
      "-Dfile.encoding=UTF-8"
  ]
  ```

#### `arguments.jvm_middle`
- **类型**: `string[]`
- **说明**: JVM 中间参数（在 `-Xms`/`-Xmx` 之后，`-jar` 之前）
- **用途**: GC 参数、性能优化参数等
- **示例**:
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
      "-XX:G1HeapRegionSize=8M"
  ]
  ```

#### `arguments.server`
- **类型**: `string[]`
- **说明**: 服务器参数（在 JAR 文件名之后）
- **示例**:
  ```json
  "server": [
      "--nogui",
      "--world-dir", "world",
      "--port", "25565"
  ]
  ```

#### `monitoring.startup_timeout`
- **类型**: `number`
- **单位**: 秒
- **默认**: `300`
- **说明**: 服务器启动超时时间，超时后视为启动失败

#### `monitoring.startup_detection.keywords`
- **类型**: `string[]`
- **说明**: 用于检测服务器启动完成的关键字
- **默认**: `["Done (", "For help, type \"help\""]`
- **注意**: 匹配任意一个关键字即认为启动完成

#### `auto_restart.max_retries`
- **类型**: `number`
- **默认**: `3`
- **说明**: 服务器崩溃后的最大重启次数

#### `auto_restart.retry_delay`
- **类型**: `number`
- **单位**: 毫秒
- **默认**: `5000`
- **说明**: 重启前的延迟时间

#### `auto_restart.reset_timer`
- **类型**: `number`
- **单位**: 毫秒
- **默认**: `600000` (10分钟)
- **说明**: 服务器正常运行超过此时间后，重启计数器归零

---

## 🔨 命令构建

### 完整命令格式

```
java [jvm_prefix] -Xms[min]M -Xmx[max]M [jvm_middle] -jar [server.jar] [server]
```

### 示例

**配置**:
```json
{
    "java": { "path": "java" },
    "memory": { "min": 2048, "max": 4096 },
    "arguments": {
        "jvm_prefix": ["-server"],
        "jvm_middle": [
            "-XX:+UseG1GC",
            "-XX:MaxGCPauseMillis=200"
        ],
        "server": ["--nogui"]
    },
    "server": { "jar": "server.jar" }
}
```

**生成的命令**:
```bash
java -server -Xms2048M -Xmx4096M -XX:+UseG1GC -XX:MaxGCPauseMillis=200 -jar server.jar --nogui
```

---

## 📊 输出监控

### 输出处理

NetherGate 会监听服务器的标准输出和标准错误，并进行以下处理：

1. **转发到控制台**: 所有输出实时显示在 NetherGate 控制台
2. **颜色处理**: 保留 ANSI 颜色代码
3. **日志记录**: 可选择性地记录到文件
4. **关键字检测**: 检测启动完成、错误、崩溃等关键字

### 输出格式

```
[Server] [12:00:00] [Server thread/INFO]: Starting minecraft server version 1.21.9
[Server] [12:00:05] [Server thread/INFO]: Done (5.123s)! For help, type "help"
[NetherGate] [12:00:05] [INFO] Server startup detected!
[NetherGate] [12:00:06] [INFO] Connecting to management protocol...
```

### 关键字检测

#### 启动完成检测
- 检测关键字（默认）:
  - `"Done ("`
  - `"For help, type \"help\""`
- 检测到后触发:
  - `ServerStartedEvent` 事件
  - 开始连接服务端管理协议
  - 加载插件

#### 崩溃检测
- 检测关键字:
  - `"Exception"`
  - `"Error"`
  - `"Crash"`
- 检测到后:
  - 记录错误日志
  - 触发 `ServerCrashedEvent` 事件
  - 根据配置决定是否自动重启

---

## 🔄 自动重启机制

### 重启触发条件

1. **服务器崩溃**: 进程意外退出（退出码 ≠ 0）
2. **启动超时**: 超过 `startup_timeout` 仍未检测到启动完成
3. **手动重启**: 通过命令或 API 触发

### 重启逻辑

```
服务器崩溃
    ↓
检查重启次数 < max_retries?
    ↓ Yes
等待 retry_delay 毫秒
    ↓
清理资源
    ↓
重新启动服务器
    ↓
重启次数 +1
    ↓
服务器正常运行超过 reset_timer?
    ↓ Yes
重启次数归零
```

### 示例

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

**场景 1**: 服务器崩溃
- 第1次: 5秒后重启 ✅
- 第2次: 5秒后重启 ✅
- 第3次: 5秒后重启 ✅
- 第4次: **不再重启**，需要手动处理 ❌

**场景 2**: 服务器稳定运行
- 崩溃 → 重启 (计数: 1)
- 运行 10 分钟 → 计数归零
- 崩溃 → 重启 (计数: 1) ✅

---

## 🎯 状态管理

### 服务器进程状态

```csharp
public enum ServerProcessState
{
    Stopped,        // 未启动
    Starting,       // 启动中
    Running,        // 运行中
    Stopping,       // 停止中
    Crashed,        // 崩溃
    Restarting      // 重启中
}
```

### 状态转换

```
[Stopped] ──启动──→ [Starting] ──启动成功──→ [Running]
                        │                      │
                    启动失败                  停止
                        │                      ↓
                    [Crashed] ←────崩溃──── [Stopping]
                        │                      │
                    自动重启                完成停止
                        │                      ↓
                    [Restarting] ──────────→ [Stopped]
```

---

## 💻 核心 API 设计

### ServerProcessManager

```csharp
namespace NetherGate.Core.Process
{
    public class ServerProcessManager
    {
        /// <summary>
        /// 当前进程状态
        /// </summary>
        public ServerProcessState State { get; }
        
        /// <summary>
        /// 进程是否正在运行
        /// </summary>
        public bool IsRunning { get; }
        
        /// <summary>
        /// 启动服务器
        /// </summary>
        public async Task<bool> StartAsync()
        
        /// <summary>
        /// 停止服务器
        /// </summary>
        /// <param name="force">是否强制终止</param>
        /// <param name="timeout">等待超时时间（毫秒）</param>
        public async Task StopAsync(bool force = false, int timeout = 30000)
        
        /// <summary>
        /// 重启服务器
        /// </summary>
        public async Task RestartAsync()
        
        /// <summary>
        /// 向服务器发送命令（通过标准输入）
        /// </summary>
        public async Task SendCommandAsync(string command)
        
        /// <summary>
        /// 强制终止服务器进程
        /// </summary>
        public void Kill()
        
        // 事件
        public event EventHandler<ServerProcessStateChangedEventArgs> StateChanged;
        public event EventHandler<ServerOutputEventArgs> OutputReceived;
        public event EventHandler<ServerOutputEventArgs> ErrorReceived;
        public event EventHandler<ServerStartedEventArgs> ServerStarted;
        public event EventHandler<ServerStoppedEventArgs> ServerStopped;
        public event EventHandler<ServerCrashedEventArgs> ServerCrashed;
    }
}
```

### 事件参数

```csharp
public class ServerOutputEventArgs : EventArgs
{
    public string Line { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsError { get; init; }
}

public class ServerStartedEventArgs : EventArgs
{
    public TimeSpan StartupTime { get; init; }
    public int ProcessId { get; init; }
}

public class ServerStoppedEventArgs : EventArgs
{
    public int ExitCode { get; init; }
    public bool WasClean { get; init; }
    public string? Reason { get; init; }
}

public class ServerCrashedEventArgs : EventArgs
{
    public int ExitCode { get; init; }
    public string? LastOutput { get; init; }
    public Exception? Exception { get; init; }
    public bool WillRestart { get; init; }
    public int RetryCount { get; init; }
}
```

---

## 📝 使用示例

### 基本启动

```csharp
var processManager = new ServerProcessManager(config);

// 订阅事件
processManager.OutputReceived += (sender, e) => 
{
    Console.WriteLine($"[Server] {e.Line}");
};

processManager.ServerStarted += (sender, e) => 
{
    Console.WriteLine($"Server started in {e.StartupTime.TotalSeconds:F2}s");
};

// 启动服务器
await processManager.StartAsync();
```

### 监控和控制

```csharp
// 检查状态
if (processManager.IsRunning)
{
    // 发送命令
    await processManager.SendCommandAsync("say Hello, World!");
    
    // 停止服务器
    await processManager.StopAsync();
}

// 重启服务器
await processManager.RestartAsync();
```

### 崩溃处理

```csharp
processManager.ServerCrashed += async (sender, e) => 
{
    Logger.Error($"Server crashed! Exit code: {e.ExitCode}");
    Logger.Error($"Last output: {e.LastOutput}");
    
    if (e.WillRestart)
    {
        Logger.Info($"Restarting... (Attempt {e.RetryCount + 1})");
    }
    else
    {
        Logger.Fatal("Max retries reached. Manual intervention required.");
        await SendAdminNotificationAsync("Server crashed and won't restart!");
    }
};
```

---

## 🔐 安全考虑

### 1. 命令注入防护

所有参数都经过转义和验证，防止命令注入攻击。

```csharp
// ❌ 不安全
var command = $"java -jar {userInput}"; // 可能被注入

// ✅ 安全
var args = new List<string> { "-jar", SanitizeArgument(userInput) };
Process.Start("java", args);
```

### 2. 工作目录隔离

服务器进程运行在指定的工作目录中，限制文件访问范围。

### 3. 资源限制

- 监控进程资源使用（CPU、内存）
- 检测异常资源消耗
- 可选的资源限制配置

---

## 🔧 Java 路径配置示例

### 示例 1: 使用环境变量（推荐）

```json
{
    "java": {
        "path": "java"
    }
}
```

**前提条件**：
- Windows: 确保 `JAVA_HOME` 环境变量已设置，或 Java 已添加到 `PATH`
- Linux/macOS: 确保 `java` 命令可用（`which java` 能找到）

**验证方法**：
```bash
# Windows
java -version

# Linux/macOS
which java
java -version
```

### 示例 2: 指定绝对路径

**Windows**:
```json
{
    "java": {
        "path": "C:\\Program Files\\Java\\jdk-21\\bin\\java.exe"
    }
}
```

**Linux**:
```json
{
    "java": {
        "path": "/usr/lib/jvm/java-21-openjdk/bin/java"
    }
}
```

**macOS**:
```json
{
    "java": {
        "path": "/Library/Java/JavaVirtualMachines/jdk-21.jdk/Contents/Home/bin/java"
    }
}
```

### 示例 3: 使用相对路径（便携版）

适用于将 Java 打包在 NetherGate 目录中的场景：

```json
{
    "java": {
        "path": "./jre/bin/java"
    }
}
```

目录结构：
```
NetherGate/
├── NetherGate.exe
├── config.json
└── jre/                    # 便携式 JRE
    └── bin/
        └── java.exe
```

---

## 🎨 预设配置模板

虽然不预设参数，但提供一些常用配置模板供参考：

### 小型服务器（2-4GB 内存）

```json
{
    "memory": { "min": 2048, "max": 4096 },
    "arguments": {
        "jvm_middle": [
            "-XX:+UseG1GC",
            "-XX:MaxGCPauseMillis=200"
        ]
    }
}
```

### 中型服务器（8GB 内存，Aikar's Flags）

```json
{
    "memory": { "min": 8192, "max": 8192 },
    "arguments": {
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
    }
}
```

### 大型服务器（16GB+ 内存，ZGC）

```json
{
    "memory": { "min": 16384, "max": 16384 },
    "arguments": {
        "jvm_middle": [
            "-XX:+UseZGC",
            "-XX:+ZGenerational",
            "-XX:ZCollectionInterval=5",
            "-XX:ZAllocationSpikeTolerance=2.0"
        ]
    }
}
```

---

## 📖 参考资料

- [Aikar's Minecraft Server Flags](https://aikar.co/2018/07/02/tuning-the-jvm-g1gc-garbage-collector-flags-for-minecraft/)
- [Java HotSpot VM Options](https://www.oracle.com/java/technologies/javase/vmoptions-jsp.html)
- [ZGC Documentation](https://wiki.openjdk.org/display/zgc)

---

**下一步**: 查看 [DEVELOPMENT.md](../DEVELOPMENT.md) 了解完整的开发计划。

