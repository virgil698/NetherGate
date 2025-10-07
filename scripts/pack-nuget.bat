@echo off
REM NetherGate.API NuGet Package Build Script

echo ======================================
echo Building NetherGate.API NuGet Package
echo ======================================

cd /d "%~dp0.."

echo.
echo [1/3] Cleaning previous builds...
dotnet clean src\NetherGate.API\NetherGate.API.csproj -c Release

echo.
echo [2/3] Building NetherGate.API...
dotnet build src\NetherGate.API\NetherGate.API.csproj -c Release

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

echo.
echo [3/3] Packing NuGet package...
dotnet pack src\NetherGate.API\NetherGate.API.csproj -c Release -o nupkg --include-symbols --include-source

if errorlevel 1 (
    echo Pack failed!
    exit /b 1
)

echo.
echo ======================================
echo Package created successfully!
echo Location: nupkg\
echo ======================================
echo.
echo To publish to NuGet.org:
echo   dotnet nuget push nupkg\NetherGate.API.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
echo.
echo To publish to GitHub Packages:
echo   dotnet nuget push nupkg\NetherGate.API.*.nupkg --api-key YOUR_GITHUB_TOKEN --source https://nuget.pkg.github.com/virgil698/index.json
echo.

pause

