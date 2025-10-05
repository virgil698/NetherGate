# NetherGate CLI 命令指南

NetherGate 提供了强大的命令行界面（CLI），支持交互模式和非交互式命令。

## 目录

- [启动模式](#启动模式)
- [CLI 命令](#cli-命令)
- [插件管理](#插件管理)
- [配置管理](#配置管理)
- [诊断工具](#诊断工具)
- [配置向导](#配置向导)

---

## 启动模式

### 交互模式（默认）

```bash
# 直接启动，进入交互式控制台
NetherGate.exe

# 或明确指定
NetherGate.exe --interactive
```

在交互模式下，NetherGate 会：
1. 加载配置文件
2. 初始化所有系统组件
3. 加载插件
4. 提供交互式命令行界面

### 非交互模式

非交互模式允许你执行单个命令并立即退出，适合脚本和自动化。

```bash
# 执行命令并退出
NetherGate.exe <command> [options]
```

---

## CLI 命令

### 通用命令

#### `version` - 显示版本信息

```bash
NetherGate.exe version
# 或
NetherGate.exe --version
NetherGate.exe -v
```

**输出示例：**
```
NetherGate v0.1.0-alpha
.NET 9.0
https://github.com/BlockBridge/NetherGate
```

#### `help` - 显示帮助信息

```bash
NetherGate.exe help
# 或
NetherGate.exe --help
NetherGate.exe -h
```

---

## 插件管理

### `plugin list` - 列出所有插件

列出已安装的所有插件及其基本信息。

```bash
NetherGate.exe plugin list
# 或简写
NetherGate.exe plugin ls
```

**输出示例：**
```
已安装的插件:
====================
  ✓ ExamplePlugin (v1.0.0) - 示例插件
      ID: example-plugin
      作者: YourName

  ✓ BackupManager (v2.1.0) - 自动备份插件
      ID: backup-manager
      作者: BlockBridge

共 2 个插件
```

### `plugin info` - 查看插件详情

显示指定插件的详细信息。

```bash
NetherGate.exe plugin info <plugin-id>
```

**示例：**
```bash
NetherGate.exe plugin info example-plugin
```

**输出示例：**
```
插件信息: ExamplePlugin
====================
ID: example-plugin
版本: 1.0.0
描述: 这是一个示例插件
作者: YourName
主页: https://github.com/yourname/example-plugin
仓库: https://github.com/yourname/example-plugin.git
依赖:
  - Newtonsoft.Json (>= 13.0.0)
库依赖:
  - SomeLibrary (1.2.3)
```

### `plugin enable/disable/reload` - 运行时管理

这些命令需要在运行时通过交互式控制台执行：

```bash
# 在 NetherGate 交互模式下
> plugins enable <plugin-id>
> plugins disable <plugin-id>
> plugins reload <plugin-id>
```

**注意：** 这些命令不能在非交互模式下使用，因为它们需要 NetherGate 运行时环境。

---

## 配置管理

### `config validate` - 验证配置文件

检查配置文件是否有效，并显示配置摘要。

```bash
NetherGate.exe config validate
```

**输出示例（成功）：**
```
验证配置文件...
====================
✓ 配置文件有效

配置概要:
  服务器进程: 启用
  SMP 连接: localhost:40745
  RCON: 启用
  日志级别: Info
  插件目录: plugins
```

**输出示例（失败）：**
```
验证配置文件...
====================
✗ 配置文件无效: Unexpected character encountered while parsing value
```

### `config export` - 导出配置

备份当前配置文件。

```bash
# 导出到默认文件
NetherGate.exe config export

# 导出到指定文件
NetherGate.exe config export my-backup.yaml
```

**输出示例：**
```
✓ 配置已导出到: nethergate-config.backup.yaml
```

### `config import` - 导入配置

从备份文件恢复配置。

```bash
NetherGate.exe config import <file>
```

**示例：**
```bash
NetherGate.exe config import my-backup.yaml
```

**输出示例：**
```
✓ 配置已导入
请重启 NetherGate 以应用新配置
```

---

## 诊断工具

### `diagnose` - 运行诊断

全面检查 NetherGate 的安装和配置状态。

```bash
NetherGate.exe diagnose
```

**输出示例：**
```
NetherGate 诊断报告
====================

系统信息:
  OS: Microsoft Windows NT 10.0.26100.0
  .NET: 9.0.0
  工作目录: E:\BlockBridge\NetherGate

配置文件:
  ✓ 存在: nethergate-config.yaml
  ✓ 有效

目录结构:
  ✓ 插件目录: plugins
  ✓ 配置目录: config
  ✓ 库目录: lib
  ✓ 日志目录: logs

插件:
  发现 2 个插件目录
  ✓ example-plugin
  ✓ backup-manager
```

### `check-deps` - 检查依赖

检查所有依赖项是否存在。

```bash
NetherGate.exe check-deps
```

**输出示例：**
```
检查依赖...
====================
lib/ 目录中的依赖 (3):
  ✓ Newtonsoft.Json.dll
  ✓ YamlDotNet.dll
  ✓ fNbt.dll

插件依赖:
  ExamplePlugin:
    ✓ SomeLibrary (1.2.3)
    ✗ AnotherLibrary (2.0.0) - 缺失
```

---

## 配置向导

### `setup` - 运行交互式配置向导

首次运行或需要重新配置时，可以使用配置向导。

```bash
NetherGate.exe setup
# 或
NetherGate.exe --setup
NetherGate.exe -s
```

配置向导会引导你完成以下步骤：

#### 步骤 1：服务器进程管理

选择服务器启动方式：
- **Java 直接启动（推荐）**：NetherGate 使用 Java 命令启动服务器
- **脚本启动**：使用现有的启动脚本（如 `start.sh`）
- **外部服务器**：连接到已运行的服务器

**配置项：**
- 工作目录
- Java 路径（Java 模式）
- 服务器 JAR 文件（Java 模式）
- 内存配置（Java 模式）
- JVM 参数（Java 模式，可选）
- 脚本路径（脚本模式）
- 自动重启设置

#### 步骤 2：SMP 连接

配置 Server Management Protocol 连接：
- SMP 服务器地址
- SMP 端口
- TLS 加密
- TLS 密钥（如果启用）
- 自动连接

#### 步骤 3：RCON 配置

配置 Remote Console：
- 是否启用 RCON
- RCON 地址
- RCON 端口
- RCON 密码
- 自动连接

#### 步骤 4：日志系统

配置日志系统：
- 日志级别（Debug/Info/Warning/Error）
- 控制台日志
- 彩色输出
- 文件日志
- 日志文件路径
- 日志文件大小限制
- 保留文件数量

#### 步骤 5：插件系统

配置插件系统：
- 插件目录
- 自动加载
- 热重载
- 依赖管理
  - 自动依赖管理
  - 自动下载
  - 版本冲突解决策略

**向导交互示例：**

```
╔════════════════════════════════════════════════════════╗
║                                                        ║
║          NetherGate 配置向导                           ║
║                                                        ║
║  此向导将帮助你快速配置 NetherGate                     ║
║  你可以随时手动编辑配置文件                            ║
║                                                        ║
╚════════════════════════════════════════════════════════╝

========================================
  步骤 1/5: 服务器进程管理
========================================

NetherGate 可以管理 Minecraft 服务器进程，或连接到外部运行的服务器。

选择服务器启动方式:
  * [1] 使用 Java 直接启动 (推荐)
    [2] 使用脚本启动
    [3] 连接到外部服务器
选择 [1-3] (默认: 1): 1

服务器工作目录: [server] server
Java 可执行文件路径: [java] 
服务器 JAR 文件名: [server.jar] 

内存配置 (MB):
  最小内存: [1024] 2048
  最大内存: [2048] 4096

是否需要自定义 JVM 参数？ [y/N] n

服务器崩溃时自动重启？ [Y/n] y
重启延迟 (秒): [5] 

...（继续其他步骤）

========================================
  配置完成
========================================

配置摘要:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
服务器进程:
  启动方式: java
  工作目录: server
  Java: java
  JAR: server.jar
  内存: 2048MB - 4096MB

SMP 连接:
  地址: localhost:40745
  TLS: 禁用
  自动连接: 是

RCON:
  状态: 启用
  地址: localhost:25575
  自动连接: 是

日志:
  级别: Info
  控制台: 启用
  文件: 启用

插件:
  目录: plugins
  自动加载: 是
  依赖管理: 启用

确认保存配置？ [Y/n] y

✓ 配置已保存！
位置: nethergate-config.yaml

提示: 你可以随时手动编辑配置文件
运行 'NetherGate.exe config validate' 来验证配置
```

---

## 首次运行

如果配置文件不存在，NetherGate 会在启动时提示运行配置向导：

```
[NetherGate] 正在启动...
[NetherGate] 版本: 0.1.0-alpha
[NetherGate] .NET 版本: 9.0.0

[NetherGate] [1/7] 加载配置...

⚠ 未找到配置文件！

是否运行配置向导？ [Y/n] y

（启动配置向导...）
```

如果选择 "n"，NetherGate 会使用默认配置启动。

---

## 脚本和自动化

### 示例：自动化部署脚本

```bash
#!/bin/bash
# deploy.sh - 自动化部署脚本

# 1. 验证配置
echo "验证配置..."
./NetherGate.exe config validate || exit 1

# 2. 检查依赖
echo "检查依赖..."
./NetherGate.exe check-deps

# 3. 运行诊断
echo "运行诊断..."
./NetherGate.exe diagnose

# 4. 启动 NetherGate
echo "启动 NetherGate..."
./NetherGate.exe
```

### 示例：配置备份脚本

```bash
#!/bin/bash
# backup-config.sh - 定期备份配置

BACKUP_DIR="./config-backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"
./NetherGate.exe config export "$BACKUP_DIR/config_$TIMESTAMP.yaml"

echo "配置已备份到: $BACKUP_DIR/config_$TIMESTAMP.yaml"
```

---

## 常见问题

### Q: 如何查看所有可用命令？

```bash
NetherGate.exe help
```

### Q: 配置文件损坏怎么办？

```bash
# 1. 先备份损坏的文件
mv nethergate-config.yaml nethergate-config.yaml.broken

# 2. 运行配置向导重新生成
NetherGate.exe setup
```

### Q: 如何批量检查多个 NetherGate 实例？

创建一个批处理脚本：

```bash
#!/bin/bash
for dir in /servers/*/; do
    echo "检查: $dir"
    cd "$dir"
    ./NetherGate.exe diagnose
    echo "---"
done
```

### Q: 如何在无交互环境运行？

使用非交互命令或在启动前确保配置文件存在：

```bash
# 方式 1：使用配置模板
cp nethergate-config.template.yaml nethergate-config.yaml

# 方式 2：通过环境变量（如果支持）
export NETHERGATE_AUTO_START=true
./NetherGate.exe
```

---

## 相关文档

- [配置文件指南](CONFIGURATION.md)
- [插件开发指南](PLUGIN_PROJECT_STRUCTURE.md)
- [故障排除](FAQ.md)
