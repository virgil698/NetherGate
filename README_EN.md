# NetherGate

<div align="center">

**🌐 Modern .NET Minecraft Server Plugin Loader 🌐**

[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dot.net)
[![Minecraft](https://img.shields.io/badge/Minecraft-Java%201.21.9+-brightgreen?logo=minecraft)](https://minecraft.net)
[![License](https://img.shields.io/badge/License-LGPL--3.0-blue.svg)](LICENSE)

[简体中文](README.md) | English

</div>

---

## ✨ **Why NetherGate?**

### **🎯 Easy to Use, Yet Powerful**

NetherGate makes plugin development **simple and powerful**:

- 🎨 **From Beginner to Expert**: Whether it's a simple check-in plugin, gift system, or complex QQ bot, backup plugin, or economy plugin - all are easy to implement
- ⚡ **Ready to Use**: Complete API design, no need to learn complex concepts to get started
- 🔥 **True Hot Reload**: Plugin updates without server restart, preserves runtime state, supports cross-version migration
- 📦 **Smart Dependency Management**: Automatic NuGet package downloads, intelligent version conflict resolution, say goodbye to dependency hell
- 🎭 **Complete Isolation**: Each plugin runs in an isolated environment, mutually independent, crashes don't affect other plugins

### **🚀 100% Feature Coverage**

| Core Capability | Status | Description |
|---------|------|------|
| **SMP Protocol** | ✅ 100% | 35 methods + 22 notification handlers |
| **RCON Commands** | ✅ 100% | Complete game command support (60+ commands) |
| **Log Listening** | ✅ 100% | Server lifecycle + player events |
| **Network Events** | ✅ 100% | 4 listening modes (LogBased ready to use) |
| **NBT Data** | ✅ 100% | Read + Write (player/world data) |
| **Data Components** | ✅ 100% | 1.20.5+ item component system support |
| **Block Data** | ✅ 100% | Chest/hopper/sign container read/write |
| **File Operations** | ✅ 100% | Read/write/watch/backup |
| **Performance Monitoring** | ✅ 100% | CPU/Memory/TPS |
| **Game Utilities** | ✅ 100% | Fireworks/music/time/area operations |
| **Extension Methods** | ✅ 100% | ItemStack/Position extension methods |

### **💡 Core Advantages**

- ⚡ **High Performance**: Native .NET compilation, far more efficient than interpreted languages
- 🛡️ **Secure Isolation**: AssemblyLoadContext mechanism, complete plugin isolation
- 🎨 **Modern Design**: Async API, event-driven, dependency injection
- 🔥 **Advanced Hot Reload**:
  - ✅ Preserve plugin state (no data loss)
  - ✅ Code changes take effect immediately
  - ✅ Automatically unload old version, load new version
  - ✅ Auto-reload plugin dependencies
- 🌐 **Cross-Platform**: Supports 8 platform architecture combinations
  - **Windows**: x64, x86, ARM64
  - **Linux**: x64, ARM, ARM64
  - **macOS**: Intel x64, Apple Silicon ARM64
- 📚 **Complete Documentation**: Chinese documentation covering all features with rich examples

---

## 🎮 **Compatibility**

### **Supported Minecraft Versions**
- ✅ **Java Edition 1.21.9+** (and above)

### **Supported Server Types**

NetherGate supports all Minecraft servers with **RCON + SMP protocol**, including but not limited to:

- **Vanilla** (Official)
- **Paper**
- **Purpur**
- **Leaves**
- **Leaf**
- **Forge**
- **Fabric**
- **NeoForge**

**Prerequisites:**
- ✅ Server must enable **RCON** (for command execution)
- ✅ Server must support **SMP protocol** (for server management)

---

## 📥 **Quick Start**

### **1. Download**

Download the version for your system from [Releases](../../releases):

| OS | Architecture | Filename |
|---------|------|--------|
| **Windows** | x64 (Recommended) | `nethergate-nightly-win-x64.zip` |
| Windows | x86 | `nethergate-nightly-win-x86.zip` |
| Windows | ARM64 | `nethergate-nightly-win-arm64.zip` |
| **Linux** | x64 (Recommended) | `nethergate-nightly-linux-x64.tar.gz` |
| Linux | ARM | `nethergate-nightly-linux-arm.tar.gz` |
| Linux | ARM64 | `nethergate-nightly-linux-arm64.tar.gz` |
| **macOS** | Intel x64 | `nethergate-nightly-osx-x64.tar.gz` |
| **macOS** | Apple Silicon | `nethergate-nightly-osx-arm64.tar.gz` |

Or build from source:

```bash
git clone https://github.com/your-org/NetherGate.git
cd NetherGate
dotnet build -c Release
```

### **2. Configuration**

First run will automatically generate configuration files:

```bash
cd bin/Release
./NetherGate.Host
```

Or use the interactive configuration wizard:

```bash
./NetherGate.Host --wizard
```

**Configuration Example:** `nethergate-config.yaml`

```yaml
server_process:
  launch_method: java  # java | script | external
  working_directory: ./
  
  java:
    jar_path: server.jar
    min_memory: 2G
    max_memory: 4G
```

**Detailed Configuration:** [Configuration Guide](docs/01-快速开始/配置文件详解.md)

### **3. Start**

```bash
./NetherGate.Host
```

**Three Launch Modes:**
- **Java Mode**: NetherGate directly starts the Minecraft server
- **Script Mode**: Use existing startup script
- **External Mode**: Connect to a running server (management only)

---

## 🔌 **Create Your First Plugin in 5 Minutes**

### **1. Install NuGet Package**

NetherGate.API is published on GitHub Packages. You need to configure the NuGet source before installation:

**Method 1: Using Project Configuration (Recommended)**

Create `nuget.config` in your project root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/virgil698/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Then authenticate and install:

```bash
# Configure GitHub Packages authentication (using GitHub Personal Access Token)
dotnet nuget add source --username YOUR_GITHUB_USERNAME --password YOUR_GITHUB_TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/virgil698/index.json"

# Create plugin project
dotnet new classlib -n MyPlugin
cd MyPlugin

# Install NetherGate.API
dotnet add package NetherGate.API
```

**Method 2: Direct Installation**

```bash
dotnet new classlib -n MyPlugin
cd MyPlugin
dotnet add package NetherGate.API --source https://nuget.pkg.github.com/virgil698/index.json
```

### **2. Write Code**

```csharp
using NetherGate.API.Plugins;
using NetherGate.API.Events;

namespace MyPlugin;

public class MyPlugin : IPlugin
{
    private IPluginContext _context;

    public void OnEnable(IPluginContext context)
    {
        _context = context;
        _context.Logger.Info("MyPlugin enabled!");
        
        // Subscribe to player join event
        _context.EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
    }

    private async void OnPlayerJoined(PlayerJoinedEvent e)
    {
        // Send welcome message
        await _context.GameDisplay.SendChatMessageAsync(
            e.PlayerName, 
            $"§aWelcome {e.PlayerName} to the server!"
        );
    }

    public void OnDisable()
    {
        _context.Logger.Info("MyPlugin disabled");
    }
}
```

### **3. Build and Load**

```bash
dotnet build
# Copy the generated DLL to plugins/ directory
# Use `plugin load MyPlugin` command to load, or restart NetherGate
```

**Complete Tutorial:** [First Plugin](docs/01-快速开始/第一个插件.md) | [Plugin Development Guide](docs/03-插件开发/插件开发指南.md)

---

## 📚 **Rich Feature API**

NetherGate provides a complete API to implement **any idea**:

### **🎮 Game Interaction**
```csharp
// BossBar, Title, ActionBar, Chat Messages
await context.GameDisplay.ShowBossBarAsync("my_boss", "Welcome!", 1.0f);
await context.GameDisplay.ShowTitleAsync("@a", "§6Title", "§eSubtitle");

// Firework System - Launch spectacular fireworks
await context.GameUtilities.LaunchFireworkAsync(
    position, FireworkType.LargeBall, 
    colors: new[] { FireworkColor.Red, FireworkColor.Gold }
);

// Music Player - Play notes in-game
await context.MusicPlayer.CreateMelody()
    .AddNote(Note.C, 200)
    .AddNote(Note.E, 200)
    .AddNote(Note.G, 400)
    .PlayAsync("@a");
```

### **📦 Data Operations**
```csharp
// NBT Data (1.20.4-), Data Components (1.20.5+)
var playerData = await context.PlayerDataReader.ReadPlayerDataAsync(uuid);
await context.NbtDataWriter.UpdatePlayerHealthAsync(uuid, 20.0f);

// Item Component System
var item = await context.ItemComponentReader.ReadInventorySlotAsync(player, slot);
await context.ItemComponentWriter.UpdateComponentAsync(player, slot, "custom_name", "Divine Sword");

// Chest Operations - Read, sort, write
var items = await context.BlockDataReader.GetChestItemsAsync(chestPos);
var sortedItems = items.SortById().FilterEnchanted(); // Using extension methods
await context.BlockDataWriter.SetContainerItemsAsync(chestPos, sortedItems);
```

### **🎯 SMP Protocol**
```csharp
// Complete server management
var players = await context.SmpApi.GetPlayersAsync();
await context.SmpApi.AddToAllowlistAsync("Player123");
await context.SmpApi.UpdateGameRuleAsync("doDaylightCycle", "false");
```

### **🔧 RCON Commands**
```csharp
// Execute any Minecraft command
var result = await context.RconClient.ExecuteCommandAsync("give @a diamond 64");

// Fluent command sequence - Delayed and repeated execution
await context.GameUtilities.CreateSequence()
    .Execute(() => DoSomething())
    .WaitTicks(20)  // Wait 1 second (20 ticks)
    .Execute(() => DoAnotherThing())
    .Repeat(3)      // Repeat 3 times
    .RunAsync();
```

### **💬 Inter-Plugin Communication**
```csharp
// Cross-plugin messaging (for modular development)
var response = await context.Messenger.SendMessageAsync(
    "EconomyPlugin", "transfer", 
    new { from = "Player1", to = "Player2", amount = 100 }
);
```

### **⚙️ More Features**
- 📁 **File System**: Read/write server files, watch changes
- 🔒 **Permission System**: Groups, inheritance, wildcards
- ⏱️ **Performance Monitoring**: CPU, memory, TPS
- 🎭 **Event System**: 30+ event types, priority support
- 🎨 **Extension Methods**: ItemStack sorting/filtering, position calculations, statistics
- 🎯 **Game Utilities**: Fireworks, music, time control, area operations

**Complete API Documentation:** [API Reference](docs/08-参考/API参考.md)

---

## 📖 **Documentation Navigation**

### **🎓 Getting Started**
- [Installation and Configuration](docs/01-快速开始/安装和配置.md)
- [First Plugin](docs/01-快速开始/第一个插件.md) - Complete check-in plugin example
- [FAQ](docs/08-参考/FAQ.md)

### **📘 Core Features**
- [SMP Protocol](docs/02-核心功能/SMP协议.md) - Server Management Protocol
- [RCON Integration](docs/02-核心功能/RCON集成.md) - Remote command execution
- [NBT Data Operations](docs/02-核心功能/NBT数据操作.md) - Player/world data read/write (1.20.4-)
- [Item Component System](docs/02-核心功能/物品组件系统.md) - Data component operations (1.20.5+)
- [Event System](docs/02-核心功能/事件系统.md) - Event subscription and publishing
- [Permission System](docs/02-核心功能/权限系统.md) - Permission management
- [Command System](docs/02-核心功能/命令系统.md) - Command registration

### **🔧 Plugin Development**
- [Plugin Development Guide](docs/03-插件开发/插件开发指南.md)
- [Configuration Files](docs/03-插件开发/配置文件.md)
- [Debugging Tips](docs/03-插件开发/调试技巧.md)
- [Publishing Process](docs/03-插件开发/发布流程.md)

### **⚙️ Advanced Features**
- [Game Display API](docs/04-高级功能/游戏显示API.md)
- [Plugin Hot Reload](docs/04-高级功能/插件热重载.md)
- [Inter-Plugin Communication](docs/04-高级功能/插件间通信.md)
- [File System](docs/04-高级功能/文件系统.md)
- [Performance Monitoring](docs/04-高级功能/性能监控.md)

**Complete Documentation Index:** [docs/README.md](docs/README.md)

---

## 🤝 **Contributing**

Contributions of code, documentation, or suggestions are welcome!

- **Report Bugs**: [Submit Issue](../../issues)
- **Feature Requests**: [Join Discussion](../../discussions)
- **Code Contributions**: [Submit Pull Request](../../pulls)

**Contributing Guide:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📄 **License**

This project is licensed under the [LGPL-3.0 License](LICENSE).

---

## 💬 **Community and Support**

- 📖 **Documentation Center**: [docs/README.md](docs/README.md)
- 🐛 **Issue Reporting**: [GitHub Issues](../../issues)
- 💡 **Feature Discussion**: [GitHub Discussions](../../discussions)
- 📧 **Contact**: See [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🌟 **Acknowledgments**

### **Special Thanks**

Thanks to the following projects for providing design inspiration and technical reference for NetherGate:

- [**MCDReforged**](https://github.com/Fallen-Breath/MCDReforged) - Provided excellent plugin system design ideas and development concepts for NetherGate. MCDReforged set the standard for Minecraft server management in the Python ecosystem. NetherGate inherits these excellent concepts and, combined with .NET's high performance and modern features, brings a more powerful and flexible development experience to plugin developers.

- [**MinecraftConnection**](https://github.com/takunology/MinecraftConnection) - Provided important reference for NetherGate's RCON command encapsulation and game operation API design. This project demonstrated how to elegantly encapsulate Minecraft commands. NetherGate further extends this foundation to implement advanced features such as firework systems, music players, and chest operations, providing developers with more convenient game interaction APIs.

### **Open Source Projects**

Thanks to the following excellent open source projects:

- [MCDReforged](https://github.com/Fallen-Breath/MCDReforged) - Design philosophy and inspiration source
- [MinecraftConnection](https://github.com/takunology/MinecraftConnection) - RCON command encapsulation and game operation API design reference
- [fNbt](https://github.com/mstefarov/fNbt) - NBT data processing
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YAML configuration support
- [Minecraft Wiki](https://zh.minecraft.wiki) - Technical documentation reference

---

<div align="center">

**⭐ If you find it useful, please give it a Star! ⭐**

Made with ❤️ by NetherGate Team

[Documentation Center](docs/README.md) • [API Reference](docs/08-参考/API参考.md) • [Quick Start](docs/01-快速开始/安装和配置.md)

</div>

