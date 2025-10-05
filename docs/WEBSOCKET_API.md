# NetherGate WebSocket API 文档

NetherGate 提供了一个强大的 WebSocket API，用于实现 Web 管理面板和实时监控。

## 快速开始

### 配置文件

WebSocket 配置存储在 `websocket-config.yaml`，首次运行会自动生成：

```yaml
# NetherGate WebSocket 配置
enabled: true
host: localhost
port: 8766
max_connections: 100

authentication:
  enabled: true
  token: "your-secure-token-here"  # 自动生成
  token_expiry_hours: 24
  allowed_ips: []
  require_tls: false

cors:
  enabled: true
  allowed_origins:
    - "*"

heartbeat:
  enabled: true
  interval_seconds: 30
  timeout_seconds: 60

buffer:
  receive_buffer_size: 4096
  send_buffer_size: 4096
```

### 连接到服务器

```javascript
const ws = new WebSocket('ws://localhost:8766');

ws.onopen = () => {
  console.log('已连接到 NetherGate');
  
  // 如果启用了认证，需要先认证
  ws.send(JSON.stringify({
    type: 'auth',
    requestId: '1',
    data: {
      token: 'your-secure-token'
    }
  }));
};

ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  console.log('收到消息:', message);
};
```

## API 端点

### 1. 认证

**请求**:
```json
{
  "type": "auth",
  "requestId": "unique-id",
  "data": {
    "token": "your-token"
  }
}
```

**响应**:
```json
{
  "type": "response",
  "requestId": "unique-id",
  "success": true,
  "data": {
    "authenticated": true
  }
}
```

### 2. 心跳

**请求**:
```json
{
  "type": "ping",
  "requestId": "unique-id"
}
```

**响应**:
```json
{
  "type": "response",
  "requestId": "unique-id",
  "success": true,
  "data": {
    "pong": true
  }
}
```

### 3. 服务器信息

**获取服务器信息** (`server.info`):
```json
{
  "type": "server.info",
  "requestId": "unique-id"
}
```

**响应**:
```json
{
  "type": "response",
  "requestId": "unique-id",
  "success": true,
  "data": {
    "version": "0.1.0-alpha",
    "netVersion": "9.0.0",
    "os": "Microsoft Windows NT 10.0.26100.0",
    "uptime": {
      "days": 0,
      "hours": 2,
      "minutes": 30,
      "seconds": 15
    },
    "connections": 3
  }
}
```

**获取服务器状态** (`server.status`):
```json
{
  "type": "server.status",
  "requestId": "unique-id"
}
```

### 4. 玩家数据

**列出所有玩家** (`players.list`):
```json
{
  "type": "players.list",
  "requestId": "unique-id"
}
```

**响应**:
```json
{
  "type": "response",
  "requestId": "unique-id",
  "success": true,
  "data": {
    "players": [
      {
        "uuid": "550e8400-e29b-41d4-a716-446655440000",
        "name": "Player1",
        "level": 30,
        "health": 20.0,
        "gameMode": "Survival"
      }
    ],
    "count": 1
  }
}
```

**获取玩家详情** (`players.get`):
```json
{
  "type": "players.get",
  "requestId": "unique-id",
  "data": {
    "uuid": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

**获取玩家统计** (`players.stats`):
```json
{
  "type": "players.stats",
  "requestId": "unique-id",
  "data": {
    "uuid": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### 5. 世界数据

**获取世界信息** (`world.info`):
```json
{
  "type": "world.info",
  "requestId": "unique-id"
}
```

**列出所有世界** (`world.list`):
```json
{
  "type": "world.list",
  "requestId": "unique-id"
}
```

### 6. 插件管理

**列出所有插件** (`plugins.list`):
```json
{
  "type": "plugins.list",
  "requestId": "unique-id"
}
```

**获取插件详情** (`plugins.info`):
```json
{
  "type": "plugins.info",
  "requestId": "unique-id",
  "data": {
    "id": "example-plugin"
  }
}
```

## 完整示例

```javascript
class NetherGateClient {
  constructor(url, token) {
    this.url = url;
    this.token = token;
    this.ws = null;
    this.requestCallbacks = new Map();
    this.requestIdCounter = 0;
  }

  connect() {
    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(this.url);

      this.ws.onopen = async () => {
        console.log('已连接到 NetherGate');
        
        // 认证
        try {
          await this.request('auth', { token: this.token });
          console.log('认证成功');
          resolve();
        } catch (error) {
          reject(error);
        }
      };

      this.ws.onmessage = (event) => {
        const message = JSON.parse(event.data);
        
        if (message.type === 'response' && message.requestId) {
          const callback = this.requestCallbacks.get(message.requestId);
          if (callback) {
            this.requestCallbacks.delete(message.requestId);
            if (message.success) {
              callback.resolve(message.data);
            } else {
              callback.reject(new Error(message.error));
            }
          }
        } else if (message.type === 'event') {
          this.handleEvent(message);
        }
      };

      this.ws.onerror = (error) => {
        console.error('WebSocket 错误:', error);
        reject(error);
      };

      this.ws.onclose = () => {
        console.log('连接已关闭');
      };
    });
  }

  request(type, data = null) {
    return new Promise((resolve, reject) => {
      const requestId = `req-${++this.requestIdCounter}`;
      
      this.requestCallbacks.set(requestId, { resolve, reject });
      
      this.ws.send(JSON.stringify({
        type,
        requestId,
        data
      }));

      // 30秒超时
      setTimeout(() => {
        if (this.requestCallbacks.has(requestId)) {
          this.requestCallbacks.delete(requestId);
          reject(new Error('请求超时'));
        }
      }, 30000);
    });
  }

  handleEvent(event) {
    console.log('收到事件:', event);
    // 处理服务器推送的事件
  }

  async getServerInfo() {
    return await this.request('server.info');
  }

  async getPlayersList() {
    return await this.request('players.list');
  }

  async getPlayerData(uuid) {
    return await this.request('players.get', { uuid });
  }

  async getWorldInfo() {
    return await this.request('world.info');
  }

  async getPluginsList() {
    return await this.request('plugins.list');
  }

  disconnect() {
    if (this.ws) {
      this.ws.close();
    }
  }
}

// 使用示例
(async () => {
  const client = new NetherGateClient('ws://localhost:8766', 'your-token');
  
  try {
    await client.connect();
    
    // 获取服务器信息
    const serverInfo = await client.getServerInfo();
    console.log('服务器信息:', serverInfo);
    
    // 获取玩家列表
    const players = await client.getPlayersList();
    console.log('玩家列表:', players);
    
    // 获取世界信息
    const worldInfo = await client.getWorldInfo();
    console.log('世界信息:', worldInfo);
    
  } catch (error) {
    console.error('错误:', error);
  }
})();
```

## 安全建议

1. **生产环境必须启用认证**
2. **使用强令牌**（自动生成的令牌已足够安全）
3. **限制 IP 白名单**
4. **使用 TLS**（wss://）
5. **限制 CORS 源**
6. **定期更换令牌**

## 相关文档

- [CLI 命令指南](CLI_GUIDE.md)
- [配置文档](CONFIGURATION.md)
- [插件开发](PLUGIN_PROJECT_STRUCTURE.md)
