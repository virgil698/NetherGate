#!/bin/bash
# NetherGate Publish Script for Linux/macOS
# 创建多平台独立可执行文件包
# ===========================================

set -e

# 切换到项目根目录
cd "$(dirname "$0")/.."

echo "========================================"
echo "NetherGate 跨平台发布脚本"
echo "========================================"
echo ""

# 检查参数
PLATFORM=${1:-all}
echo "发布平台: $PLATFORM"
echo ""

# 清理旧的发布文件
echo "[1/3] 清理旧的发布文件..."
rm -rf publish/
mkdir -p publish
echo ""

# 发布函数
do_publish() {
    local RID=$1
    local NAME=$2
    
    echo ""
    echo "== 发布 $NAME ($RID) =="
    dotnet publish src/NetherGate.Host/NetherGate.Host.csproj \
        --configuration Release \
        --runtime $RID \
        --self-contained true \
        --output publish/$RID \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:EnableCompressionInSingleFile=true
    
    # Linux/macOS 需要执行权限
    if [[ "$RID" != win-* ]]; then
        chmod +x publish/$RID/NetherGate
    fi
    
    echo "$NAME 发布完成: publish/$RID/"
}

# 根据参数发布
echo "[2/3] 发布平台..."

case $PLATFORM in
    all)
        do_publish "win-x64" "Windows x64"
        do_publish "linux-x64" "Linux x64"
        do_publish "osx-x64" "macOS Intel"
        do_publish "osx-arm64" "macOS Apple Silicon"
        ;;
    win-x64)
        do_publish "win-x64" "Windows x64"
        ;;
    linux-x64)
        do_publish "linux-x64" "Linux x64"
        ;;
    osx-x64)
        do_publish "osx-x64" "macOS Intel"
        ;;
    osx-arm64)
        do_publish "osx-arm64" "macOS Apple Silicon"
        ;;
    *)
        echo "ERROR: 未知平台 $PLATFORM"
        echo ""
        echo "支持的平台:"
        echo "  all         - 所有平台"
        echo "  win-x64     - Windows 64位"
        echo "  linux-x64   - Linux 64位"
        echo "  osx-x64     - macOS Intel"
        echo "  osx-arm64   - macOS Apple Silicon"
        exit 1
        ;;
esac

# 创建压缩包
echo ""
echo "[3/3] 创建压缩包..."

cd publish

if [ -d "win-x64" ]; then
    echo "打包 Windows 版本..."
    zip -r NetherGate-win-x64.zip win-x64/ -q
fi

if [ -d "linux-x64" ]; then
    echo "打包 Linux 版本..."
    tar czf NetherGate-linux-x64.tar.gz linux-x64/
fi

if [ -d "osx-x64" ]; then
    echo "打包 macOS Intel 版本..."
    tar czf NetherGate-osx-x64.tar.gz osx-x64/
fi

if [ -d "osx-arm64" ]; then
    echo "打包 macOS ARM 版本..."
    tar czf NetherGate-osx-arm64.tar.gz osx-arm64/
fi

cd ..

echo ""
echo "========================================"
echo "发布成功！"
echo "========================================"
echo ""
echo "输出目录: publish/"
echo ""

if ls publish/*.{zip,tar.gz} 1> /dev/null 2>&1; then
    echo "压缩包:"
    ls -1 publish/*.{zip,tar.gz} 2>/dev/null | xargs -n 1 basename
    echo ""
fi

echo "使用方法:"
echo "  1. 解压对应平台的压缩包"
echo "  2. 首次运行会自动创建 nethergate-config.json"
echo "  3. 编辑配置文件后再次运行"
echo ""

