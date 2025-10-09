# NetherGate Python SDK

Python SDK for developing NetherGate plugins.

## Installation

```bash
pip install nethergate-python
```

## Quick Start

```python
from nethergate import Plugin, PluginInfo
from nethergate.logging import Logger

class MyPlugin(Plugin):
    def __init__(self, logger: Logger):
        self.logger = logger
        self.info = PluginInfo(
            id="com.example.myplugin",
            name="My Plugin",
            version="1.0.0",
            author="Your Name"
        )
    
    async def on_load(self):
        self.logger.info("Plugin loading...")
    
    async def on_enable(self):
        self.logger.info("Plugin enabled!")
    
    async def on_disable(self):
        self.logger.info("Plugin disabled")
    
    async def on_unload(self):
        self.logger.info("Plugin unloaded")
```

## Documentation

For full documentation, visit: [NetherGate Docs](https://github.com/your-org/NetherGate/tree/main/docs)

## License

MIT License

