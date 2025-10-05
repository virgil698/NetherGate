@echo off
chcp 65001 >nul 2>&1
REM NetherGate Publish Script for Windows
REM 创建多平台独立可执行文件包
REM =======================================

REM 切换到项目根目录
cd /d "%~dp0\.."

echo ========================================
echo NetherGate 跨平台发布脚本
echo ========================================
echo.

REM 检查参数
set PLATFORM=%1
if "%PLATFORM%"=="" set PLATFORM=all

echo 发布平台: %PLATFORM%
echo.

REM 清理旧的发布文件
echo [1/3] 清理旧的发布文件...
if exist "publish\" rmdir /s /q publish
mkdir publish
echo.

REM 发布函数
if "%PLATFORM%"=="all" goto :publish_all
if "%PLATFORM%"=="win-x64" goto :publish_win_x64
if "%PLATFORM%"=="linux-x64" goto :publish_linux_x64
if "%PLATFORM%"=="osx-x64" goto :publish_osx_x64
if "%PLATFORM%"=="osx-arm64" goto :publish_osx_arm64

echo ERROR: 未知平台 %PLATFORM%
echo.
echo 支持的平台:
echo   all         - 所有平台
echo   win-x64     - Windows 64位
echo   linux-x64   - Linux 64位
echo   osx-x64     - macOS Intel
echo   osx-arm64   - macOS Apple Silicon
pause
exit /b 1

:publish_all
echo [2/3] 发布所有平台...
call :do_publish win-x64 "Windows x64"
call :do_publish linux-x64 "Linux x64"
call :do_publish osx-x64 "macOS Intel"
call :do_publish osx-arm64 "macOS Apple Silicon"
goto :package

:publish_win_x64
call :do_publish win-x64 "Windows x64"
goto :package

:publish_linux_x64
call :do_publish linux-x64 "Linux x64"
goto :package

:publish_osx_x64
call :do_publish osx-x64 "macOS Intel"
goto :package

:publish_osx_arm64
call :do_publish osx-arm64 "macOS Apple Silicon"
goto :package

:do_publish
set RID=%~1
set NAME=%~2
echo.
echo == 发布 %NAME% (%RID%) ==
dotnet publish src\NetherGate.Host\NetherGate.Host.csproj ^
    --configuration Release ^
    --runtime %RID% ^
    --self-contained true ^
    --output publish\%RID% ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true

echo %NAME% 发布完成: publish\%RID%\
goto :eof

:package
echo.
echo [3/3] 创建压缩包...

REM 需要 PowerShell 来创建 ZIP
if exist "publish\win-x64\" (
    echo 打包 Windows 版本...
    powershell -Command "Compress-Archive -Path publish\win-x64\* -DestinationPath publish\NetherGate-win-x64.zip -Force"
)

if exist "publish\linux-x64\" (
    echo 打包 Linux 版本...
    powershell -Command "Compress-Archive -Path publish\linux-x64\* -DestinationPath publish\NetherGate-linux-x64.zip -Force"
)

if exist "publish\osx-x64\" (
    echo 打包 macOS Intel 版本...
    powershell -Command "Compress-Archive -Path publish\osx-x64\* -DestinationPath publish\NetherGate-osx-x64.zip -Force"
)

if exist "publish\osx-arm64\" (
    echo 打包 macOS ARM 版本...
    powershell -Command "Compress-Archive -Path publish\osx-arm64\* -DestinationPath publish\NetherGate-osx-arm64.zip -Force"
)

echo.
echo ========================================
echo 发布成功！
echo ========================================
echo.
echo 输出目录: publish\
echo.

if exist "publish\*.zip" (
    echo 压缩包:
    dir /b publish\*.zip
    echo.
)

echo 使用方法:
echo   1. 解压对应平台的压缩包
echo   2. 首次运行会自动创建 nethergate-config.json
echo   3. 编辑配置文件后再次运行
echo.
pause

