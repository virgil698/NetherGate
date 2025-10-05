@echo off
chcp 65001 >nul 2>&1
REM NetherGate Build Script for Windows
REM ====================================

REM 切换到项目根目录
cd /d "%~dp0\.."

echo ========================================
echo NetherGate Build Script
echo ========================================
echo.

REM 检查 .NET SDK
echo [1/5] 检查 .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK 未安装或未在 PATH 中
    echo 请访问: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

dotnet --version
echo.

REM 清理旧的构建文件
echo [2/5] 清理旧的构建文件...
dotnet clean NetherGate.sln --configuration Release --verbosity quiet
if exist "bin\" rmdir /s /q bin
echo 清理完成
echo.

REM 还原依赖
echo [3/5] 还原 NuGet 依赖...
dotnet restore NetherGate.sln
if errorlevel 1 (
    echo ERROR: 依赖还原失败
    pause
    exit /b 1
)
echo.

REM 构建项目
echo [4/5] 构建项目...
dotnet build NetherGate.sln --configuration Release --no-restore
if errorlevel 1 (
    echo ERROR: 构建失败
    pause
    exit /b 1
)
echo.

REM 发布项目
echo [5/5] 发布项目...
dotnet publish src\NetherGate.Host\NetherGate.Host.csproj ^
    --configuration Release ^
    --output bin\Release ^
    --no-build
if errorlevel 1 (
    echo ERROR: 发布失败
    pause
    exit /b 1
)

echo.

echo ========================================
echo 构建成功！
echo ========================================
echo.
echo 输出目录: bin\Release\
echo 可执行文件: NetherGate.exe
echo.
echo 运行方式:
echo   cd bin\Release
echo   NetherGate.exe
echo.
pause

