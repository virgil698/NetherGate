# NetherGate 构建脚本

本目录包含 NetherGate 的构建和发布脚本。

---

## 📋 脚本列表

### 1. build.bat / build.sh
**用途**: 开发构建脚本

编译项目并创建开发版本（依赖 .NET Runtime）。

**使用方法**:

**Windows**:
```batch
scripts\build.bat
```

**Linux/macOS**:
```bash
chmod +x scripts/build.sh
./scripts/build.sh
```

**输出**:
- `bin/Release/` - 包含所有 DLL 和可执行文件
- 需要系统安装 .NET 9.0 Runtime

---

### 2. publish.bat / publish.sh
**用途**: 跨平台发布脚本

创建独立可执行文件包（包含 .NET Runtime，无需安装）。

**使用方法**:

**发布所有平台**:
```batch
# Windows
scripts\publish.bat all

# Linux/macOS
./scripts/publish.sh all
```

**发布单个平台**:
```batch
# Windows
scripts\publish.bat win-x64

# Linux/macOS
./scripts/publish.sh linux-x64
```

**支持的平台**:
- `win-x64` - Windows 64位
- `linux-x64` - Linux 64位
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon (M1/M2/M3)

**输出**:
- `publish/<平台>/` - 各平台的文件
- `publish/NetherGate-<平台>.zip` (Windows)
- `publish/NetherGate-<平台>.tar.gz` (Linux/macOS)

**特点**:
- ✅ 单文件可执行程序
- ✅ 自包含 .NET Runtime
- ✅ 无需安装 .NET
- ✅ 优化文件大小（已启用 Trimming）
- ✅ 压缩单文件

---

## 🚀 快速开始

### 开发构建

如果你只是想编译和运行 NetherGate（开发环境）：

**Windows**:
```batch
# 方式 1：双击运行
双击 scripts\build.bat 文件（会自动暂停等待按键）

# 方式 2：命令行运行
cd E:\BlockBridge\NetherGate
scripts\build.bat

# 3. 运行
cd bin\Release
NetherGate.exe
```

**Linux/macOS**:
```bash
# 1. 添加执行权限（首次）
chmod +x scripts/build.sh

# 2. 构建项目
./scripts/build.sh

# 3. 运行
cd bin/Release
./NetherGate
```

> **💡 提示**：
> - Windows 脚本会在结束时暂停，按任意键关闭窗口
> - 如果看到中文乱码，请更新脚本（已在最新版本中修复）
> - 脚本执行成功会创建 `bin/Release/` 目录

### 发布部署

如果你想创建可分发的独立可执行文件：

**Windows**:
```batch
# 1. 发布所有平台
scripts\publish.bat all

# 2. 压缩包在 publish/ 目录
dir publish\*.zip

# 3. 解压并运行
unzip publish\NetherGate-win-x64.zip -d NetherGate
cd NetherGate\win-x64
NetherGate.exe
```

**Linux/macOS**:
```bash
# 1. 发布所有平台
chmod +x scripts/publish.sh
./scripts/publish.sh all

# 2. 压缩包在 publish/ 目录
ls -la publish/*.{zip,tar.gz}

# 3. 解压并运行 (以 Linux 为例)
tar xzf publish/NetherGate-linux-x64.tar.gz
cd linux-x64
./NetherGate
```

---

## 📦 文件大小对比

### 开发构建 (build)
- **大小**: ~5-10 MB
- **需求**: 系统已安装 .NET 9.0 Runtime
- **启动速度**: 快
- **适用**: 开发和测试

### 独立发布 (publish)
- **大小**: ~60-80 MB（自包含 Runtime）
- **需求**: 无需安装任何依赖
- **启动速度**: 中等
- **适用**: 生产部署和分发

---

## 🔧 高级选项

### 自定义构建配置

编辑 `.csproj` 文件可以调整构建选项：

```xml
<PropertyGroup>
    <!-- 单文件发布 -->
    <PublishSingleFile>true</PublishSingleFile>
    
    <!-- 启用裁剪（减小体积） -->
    <PublishTrimmed>true</PublishTrimmed>
    
    <!-- 压缩单文件 -->
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    
    <!-- 包含 PDB 文件（调试用） -->
    <DebugType>embedded</DebugType>
</PropertyGroup>
```

### 优化体积

如果需要更小的文件：

```batch
# 使用 Ready-to-Run (R2R) 编译
dotnet publish -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:PublishReadyToRun=true
```

### 调试版本

如果需要调试信息：

```batch
# 发布 Debug 版本
dotnet publish -c Debug -r win-x64 --self-contained true
```

---

## 📝 CI/CD 集成

### GitHub Actions

```yaml
name: Build and Publish

on: [push, pull_request]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Build
        run: |
          chmod +x scripts/build.sh
          ./scripts/build.sh
      
      - name: Publish
        run: |
          chmod +x scripts/publish.sh
          ./scripts/publish.sh all
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: NetherGate-${{ matrix.os }}
          path: publish/*.{zip,tar.gz}
```

---

## ❓ 故障排查

### 问题: 脚本无法执行

**Linux/macOS**:
```bash
# 添加执行权限
chmod +x scripts/build.sh
chmod +x scripts/publish.sh
```

### 问题: 找不到项目或解决方案文件

**错误信息**:
```
MSBUILD : error MSB1003: 请指定项目或解决方案文件。
当前工作目录中未包含项目或解决方案文件。
```

**解决方法**:

确保从项目根目录运行脚本：

```batch
# Windows - 正确
E:\BlockBridge\NetherGate> scripts\build.bat

# Windows - 错误（不要在 scripts 目录内运行）
E:\BlockBridge\NetherGate\scripts> build.bat
```

```bash
# Linux/macOS - 正确
~/NetherGate$ ./scripts/build.sh

# Linux/macOS - 错误
~/NetherGate/scripts$ ./build.sh
```

**原因**: 脚本需要在项目根目录找到 `NetherGate.sln` 解决方案文件。

### 问题: .NET SDK 未找到

确保已安装 .NET 9.0 SDK:

```bash
# 检查版本
dotnet --version

# 应该显示 9.0.x 或 10.0.x
```

下载地址: https://dotnet.microsoft.com/download

### 问题: 发布文件太大

使用 `PublishTrimmed` 可以减小体积，但可能导致某些反射功能失效。

如果遇到运行时错误，可以禁用裁剪：

```batch
dotnet publish -p:PublishTrimmed=false
```

---

## 📚 相关文档

- [.NET 发布文档](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [单文件应用](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file)
- [裁剪自包含部署](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)

---

**最后更新**: 2025-10-04

