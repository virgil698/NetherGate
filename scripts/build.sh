#!/bin/bash
# NetherGate Build Script for Linux/macOS
# =========================================

set -e  # 遇到错误立即退出

# 切换到项目根目录
cd "$(dirname "$0")/.."

echo "========================================"
echo "NetherGate Build Script"
echo "========================================"
echo ""

# 检查 .NET SDK
echo "[1/5] 检查 .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK 未安装或未在 PATH 中"
    echo "请访问: https://dotnet.microsoft.com/download"
    exit 1
fi

dotnet --version
echo ""

# 清理旧的构建文件
echo "[2/5] 清理旧的构建文件..."
dotnet clean NetherGate.sln --configuration Release --verbosity quiet
rm -rf bin/
echo "清理完成"
echo ""

# 还原依赖
echo "[3/5] 还原 NuGet 依赖..."
dotnet restore NetherGate.sln
echo ""

# 构建项目
echo "[4/5] 构建项目..."
dotnet build NetherGate.sln --configuration Release --no-restore
echo ""

# 发布项目
echo "[5/5] 发布项目..."
dotnet publish src/NetherGate.Host/NetherGate.Host.csproj \
    --configuration Release \
    --output bin/Release \
    --no-build
echo ""

echo ""

# 设置执行权限
chmod +x bin/Release/NetherGate

echo "========================================"
echo "构建成功！"
echo "========================================"
echo ""
echo "输出目录: bin/Release/"
echo "可执行文件: NetherGate"
echo ""
echo "运行方式:"
echo "  cd bin/Release"
echo "  ./NetherGate"
echo ""

