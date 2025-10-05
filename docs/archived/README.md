# 归档文档说明

本目录用于归档已迁移到新文档结构的旧文档。

---

## 📁 目录结构

### `old-docs/` - 已迁移的老文档

这些文档的内容已完全迁移到新的文档结构中，保留在此仅供历史参考。

**迁移完成日期**: 2025-10-05

---

## 📋 文档迁移对照表

### **API 和架构设计**

| 老文档 | 新文档位置 |
|--------|-----------|
| `API_DESIGN.md` | `08-参考/API参考.md` |
| `PROJECT_STRUCTURE.md` | 内容分散到各模块文档 |
| `DOCUMENTATION_GUIDE.md` | 已整合到新文档规范 |
| `FUTURE_EXTENSIBILITY.md` | 整合到相关功能文档 |

---

### **插件系统**

| 老文档 | 新文档位置 |
|--------|-----------|
| `PLUGIN_PROJECT_STRUCTURE.md` | `03-插件开发/插件开发指南.md` |
| `PLUGIN_DEPENDENCIES.md` | `03-插件开发/插件开发指南.md` |
| `PLUGIN_HOTRELOAD_AND_MESSENGER.md` | `04-高级功能/插件热重载.md`<br>`04-高级功能/插件间通信.md` |
| `PLUGIN_MANAGEMENT_COMMANDS.md` | `08-参考/内置命令.md` |
| `PLUGIN_NBT_USAGE.md` | `02-核心功能/NBT数据操作.md` |
| `AUTO_DEPENDENCY_MANAGEMENT.md` | `03-插件开发/插件开发指南.md` |

---

### **命令和权限系统**

| 老文档 | 新文档位置 |
|--------|-----------|
| `COMMAND_SYSTEM.md` | `02-核心功能/命令系统.md` |
| `COMMAND_AND_PERMISSION_GUIDE.md` | `02-核心功能/命令系统.md`<br>`02-核心功能/权限系统.md` |
| `COMMAND_PREFIX.md` | `02-核心功能/命令系统.md` |

---

### **事件系统**

| 老文档 | 新文档位置 |
|--------|-----------|
| `EVENT_PRIORITY_STRATEGY.md` | `02-核心功能/事件系统.md` |
| `EVENT_SYSTEM_COVERAGE.md` | `02-核心功能/事件系统.md`<br>`08-参考/事件列表.md` |
| `LIFECYCLE_AND_NETWORK_EVENTS.md` | `08-参考/事件列表.md` |
| `FINAL_EVENT_SYSTEM_REPORT.md` | 整合到事件系统文档 |

---

### **协议和网络**

| 老文档 | 新文档位置 |
|--------|-----------|
| `SMP_INTERFACE.md` | `02-核心功能/SMP协议.md` |
| `SMP_PROTOCOL_COMPLETE.md` | `02-核心功能/SMP协议.md` |
| `RCON_INTEGRATION.md` | `02-核心功能/RCON集成.md` |
| `WEBSOCKET_API.md` | `01-快速开始/配置文件详解.md` (WebSocket配置部分) |

---

### **服务器管理**

| 老文档 | 新文档位置 |
|--------|-----------|
| `SERVER_PROCESS.md` | `02-核心功能/服务器进程管理.md` |
| `LAUNCH_METHODS.md` | `01-快速开始/配置文件详解.md` (启动方式部分) |
| `LOCAL_FEATURES.md` | 整合到相关功能文档 |

---

### **配置和部署**

| 老文档 | 新文档位置 |
|--------|-----------|
| `CONFIGURATION.md` | `01-快速开始/配置文件详解.md` |
| `nethergate-config.example.yaml` | `docs/config.example.yaml` |

---

### **其他**

| 老文档 | 新文档位置 |
|--------|-----------|
| `SPARK_INTEGRATION.md` | `04-高级功能/性能监控.md` |
| `SAMPLES_PROJECT.md` | `07-示例和最佳实践/示例插件集.md` |
| `TRIMMING_CONSIDERATIONS.md` | 整合到构建文档 |
| `ROADMAP_MCDR_INSPIRED.md` | 项目规划文档（已完成） |
| `MISSING_FEATURES_ANALYSIS.md` | 项目规划文档（已完成） |
| `功能覆盖率报告.md` | 项目过程文档（已完成） |

---

## ⚠️ **重要说明**

### **不要直接使用这些老文档！**

这些文档的内容可能：
- ❌ 已过时
- ❌ 与当前代码不一致
- ❌ 缺少最新功能
- ❌ 结构不完整

### **请使用新文档！**

新文档位于 `docs/` 目录下，按照以下结构组织：

```
docs/
├── 01-快速开始/        # 入门指南
├── 02-核心功能/        # 核心功能说明
├── 03-插件开发/        # 插件开发指南
├── 04-高级功能/        # 高级功能
├── 05-配置和部署/      # 配置和部署
├── 07-示例和最佳实践/ # 示例和最佳实践
└── 08-参考/           # API和命令参考
```

---

## 📚 **查找文档**

如果你在老文档中看到了某个主题，想找到对应的新文档：

1. **查看上面的对照表** - 找到老文档对应的新文档位置
2. **查看 [docs/README.md](../README.md)** - 了解新文档结构
3. **搜索关键词** - 在新文档中搜索相关功能

---

## 🗑️ **清理计划**

这些归档文档将在以下情况下被删除：
- ✅ 确认所有内容已迁移到新文档
- ✅ 新文档经过充分验证
- ✅ 无人再需要参考老文档

**预计删除时间**: 2026-01-01 或更晚

在此之前，这些文档将继续保留在归档目录中。

---

## 💡 **贡献指南**

如果你发现：
- 老文档中有内容在新文档中缺失
- 新文档中的信息有误

请：
1. 创建 GitHub Issue 报告问题
2. 提交 Pull Request 修正新文档
3. **不要直接修改归档的老文档**

---

**归档日期**: 2025-10-05  
**迁移负责人**: NetherGate 开发团队  
**文档版本**: v2.0

