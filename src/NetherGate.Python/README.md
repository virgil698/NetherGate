# NetherGate.Python

NetherGate Python 插件支持扩展，提供 Python 插件桥接能力。

> **⚠️ 性能与功能说明**
>
> Python 插件基于 Python.NET 桥接层实现，适合快速开发和原型验证。请注意：
>
> - **功能范围**：提供核心功能封装，部分高级 API 可能未实现
> - **性能开销**：跨语言调用会产生额外性能开销
> - **适用场景**：推荐用于原型开发、简单脚本、学习实验
>
> **生产环境建议使用 C# 插件以获得最佳性能和完整功能支持。**

## 功能特性

- ✅ **完整的生命周期支持**: 支持 OnLoad, OnEnable, OnDisable, OnUnload
- ✅ **依赖注入**: 自动注入 Logger, EventBus, CommandRegistry 等服务
- ✅ **异步支持**: 完整的 async/await 支持
- ✅ **自动依赖管理**: 自动安装 Python 包依赖
- ✅ **热重载**: 支持插件热重载（开发中）

## 系统要求

- .NET 8.0+
- Python 3.8+
- NetherGate.API
- NetherGate.Core

## 安装

### 1. 添加到项目

```bash
dotnet add package NetherGate.Python
```

### 2. 配置服务

在 `Program.cs` 或 `Startup.cs` 中：

```csharp
using NetherGate.Python;

// 添加 Python 插件支持
services.AddPythonPluginSupport();
```

## 使用

### 创建 Python 插件

1. **创建插件目录结构**

```
MyPythonPlugin/
├── src/
│   └── main.py
└── resource/
    └── plugin.json
```

2. **编写 plugin.json**

```json
{
  "id": "com.example.myplugin",
  "name": "My Python Plugin",
  "version": "1.0.0",
  "description": "A Python plugin example",
  "author": "Your Name",
  "type": "python",
  "main": "main.MyPlugin",
  "python_version": "3.8+",
  "python_dependencies": [
    "requests>=2.28.0"
  ]
}
```

3. **编写插件代码**

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger

class MyPlugin(Plugin):
    def __init__(self, logger: Logger):
        self.logger = logger
        self.info = PluginInfo(
            id="com.example.myplugin",
            name="My Python Plugin",
            version="1.0.0"
        )
    
    async def on_load(self):
        self.logger.info("Python plugin loading...")
    
    async def on_enable(self):
        self.logger.info("Python plugin enabled!")
    
    async def on_disable(self):
        self.logger.info("Python plugin disabled")
    
    async def on_unload(self):
        self.logger.info("Python plugin unloaded")
```

## 架构说明

NetherGate.Python 使用以下架构：

```
NetherGate.Host
    ↓
NetherGate.Core (PluginManager)
    ↓
NetherGate.Python
    ├── PythonRuntime (Python 引擎管理)
    ├── PythonPluginLoader (插件扫描和加载)
    └── PythonPluginAdapter (IPlugin 桥接)
        ↓
    Python Runtime (Python.NET)
        ↓
    Python Plugin (.py)
```

### 关键组件

1. **PythonRuntime**: 管理 Python 解释器生命周期
2. **PythonPluginLoader**: 扫描和加载 Python 插件
3. **PythonPluginAdapter**: 将 Python 插件适配为 IPlugin 接口

## 文档

- [Python 插件开发指南](../../docs/03-插件开发/Python插件开发指南.md)
- [Python API 参考](../../docs/08-参考/Python_API参考.md)
- [Python 示例插件集](../../docs/07-示例和最佳实践/Python示例插件集.md)
- [架构说明](../../docs/05-配置和部署/Python插件架构说明.md)

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License

