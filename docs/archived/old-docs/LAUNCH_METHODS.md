# 服务器启动方式配置

NetherGate 提供两种服务器启动方式，您可以根据需求选择：

---

## 📋 启动方式对比

| 特性 | Java 启动 | Script 启动 |
|------|-----------|-------------|
| **配置方式** | 在配置文件中设置 JVM 参数 | 使用现有的启动脚本 |
| **灵活性** | 标准化，NetherGate 自动构建命令 | 高度灵活，完全自定义 |
| **适用场景** | 简单部署，标准 Minecraft 服务器 | 复杂脚本，自定义启动逻辑 |
| **脚本支持** | 不需要脚本 | 支持 .bat, .sh, .cmd, .exe |
| **推荐用户** | 新手，标准部署 | 高级用户，已有启动脚本 |

---

## 🎯 方式 1：Java 启动（推荐）

NetherGate 自动构建 Java 启动命令，适合大多数场景。

### 配置示例

```json
{
    "server_process": {
        "enabled": true,
        "launch_method": "java",
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
                "-XX:MaxGCPauseMillis=200",
                "-Dfile.encoding=UTF-8"
            ],
            "server": [
                "--nogui"
            ]
        }
    }
}
```

### 生成的命令示例

```bash
java -Xms2048M -Xmx4096M -XX:+UseG1GC -XX:MaxGCPauseMillis=200 -Dfile.encoding=UTF-8 -jar server.jar --nogui
```

### 优点

- ✅ 配置简单，直观易懂
- ✅ NetherGate 自动处理命令构建
- ✅ 跨平台一致性
- ✅ 易于调整 JVM 参数

### 适用场景

- 标准 Minecraft 服务器部署
- 不需要复杂启动逻辑
- 希望配置文件管理所有参数
- 新手或简单部署

---

## 🔧 方式 2：Script 启动（高级）

使用您现有的启动脚本，NetherGate 只负责执行和监控。

### 配置示例

```json
{
    "server_process": {
        "enabled": true,
        "launch_method": "script",
        "script": {
            "path": "./start.sh",
            "arguments": [],
            "working_directory": "./minecraft_server",
            "use_shell": false
        }
    }
}
```

### 脚本示例

**Windows (start.bat)**:
```batch
@echo off
java -Xms4G -Xmx8G -XX:+UseZGC -jar server.jar nogui
```

**Linux/macOS (start.sh)**:
```bash
#!/bin/bash
java -Xms4G -Xmx8G -XX:+UseZGC -jar server.jar nogui
```

### 配置说明

#### `path` - 脚本路径
- 支持的格式：`.bat`, `.sh`, `.cmd`, `.exe`
- 可以是相对路径或绝对路径
- 相对路径基于 `working_directory`

示例：
```json
"path": "./start.sh"           // Linux/macOS
"path": "./start.bat"          // Windows
"path": "/path/to/start.sh"    // 绝对路径
"path": "./launcher.exe"       // 可执行文件
```

#### `arguments` - 脚本参数
传递给脚本的参数列表。

示例：
```json
"arguments": ["--config", "custom.yml", "--verbose"]
```

相当于执行：
```bash
./start.sh --config custom.yml --verbose
```

#### `working_directory` - 工作目录
脚本执行的目录。

#### `use_shell` - 使用 Shell 执行（Linux/macOS）
- `false`（默认）: 直接执行脚本文件
- `true`: 使用 `sh -c` 执行

**何时启用**：
- 脚本包含 shell 特性（管道、重定向等）
- 需要执行复杂的 shell 命令

### 优点

- ✅ 完全控制启动流程
- ✅ 可以使用现有的启动脚本
- ✅ 支持复杂的启动逻辑
- ✅ 兼容各种启动器和工具

### 适用场景

- 已有成熟的启动脚本
- 需要复杂的启动前/后处理
- 使用第三方启动器（如 Forge, Fabric）
- 需要环境变量设置或条件判断

---

## 🔄 切换启动方式

只需修改 `launch_method` 字段：

### 从 Script 切换到 Java

```json
{
    "server_process": {
        "launch_method": "java",  // 改为 "java"
        // 确保 java, server, memory, arguments 配置正确
    }
}
```

### 从 Java 切换到 Script

```json
{
    "server_process": {
        "launch_method": "script",  // 改为 "script"
        // 确保 script 配置正确
    }
}
```

---

## 💡 最佳实践

### Java 启动方式

1. **内存配置**
   - 根据服务器规模调整 `min` 和 `max`
   - 推荐 `min` = `max` 以避免内存抖动

2. **JVM 参数**
   - `jvm_prefix`: 很少使用，留空即可
   - `jvm_middle`: 主要的 JVM 优化参数
   - `server`: Minecraft 服务器参数（如 `--nogui`）

3. **推荐参数组合**
   - **小型服务器** (< 10 人): 使用默认 G1GC 参数
   - **中型服务器** (10-50 人): Aikar's flags（示例配置中已包含）
   - **大型服务器** (50+ 人): 考虑使用 ZGC（Java 15+）

### Script 启动方式

1. **脚本权限**
   - Linux/macOS 确保脚本有执行权限：`chmod +x start.sh`
   - Windows 确保脚本路径正确

2. **路径处理**
   - 使用相对路径时，基于 `working_directory`
   - 推荐使用绝对路径避免混淆

3. **日志输出**
   - 脚本应该输出到 stdout/stderr
   - NetherGate 会自动捕获和监控

4. **退出处理**
   - 脚本应该正确传递服务器的退出代码
   - 避免脚本中使用无限循环

---

## ⚙️ 进程监控

无论使用哪种启动方式，NetherGate 都会监控服务器进程：

### 启动检测
```json
"startup_detection": {
    "enabled": true,
    "keywords": ["Done (", "For help, type \"help\""]
}
```

### 崩溃检测
```json
"crash_detection": {
    "enabled": true,
    "keywords": [
        "Exception in server tick loop",
        "A fatal error has been detected",
        "OutOfMemoryError"
    ]
}
```

### 自动重启
```json
"auto_restart": {
    "enabled": true,
    "max_retries": 3,
    "retry_delay": 5000,
    "reset_timer": 600000
}
```

这些监控配置对两种启动方式都有效。

---

## 🎯 选择建议

### 选择 Java 启动，如果您：
- ✅ 是新手，刚开始使用 NetherGate
- ✅ 运行标准的 Minecraft 服务器
- ✅ 希望通过配置文件管理所有参数
- ✅ 需要跨平台一致性

### 选择 Script 启动，如果您：
- ✅ 已经有成熟的启动脚本
- ✅ 需要复杂的启动前/后处理
- ✅ 使用第三方启动器（Forge, Fabric 等）
- ✅ 需要高度自定义的启动流程

---

## 📚 相关文档

- [配置文件详解](CONFIGURATION.md)
- [服务器进程管理](SERVER_PROCESS.md)
- [配置示例](config.example.json)

---

**最后更新**: 2025-10-04

