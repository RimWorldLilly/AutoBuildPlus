@echo off
setlocal
cd /d "%~dp0"

set "GAME_DIR=%RIMWORLD_DIR%"
if "%GAME_DIR%"=="" set "GAME_DIR=Y:\SteamLibrary\steamapps\common\RimWorld"

if not exist "%GAME_DIR%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" (
    echo ERROR: RimWorld assemblies were not found.
    echo Set RIMWORLD_DIR to your RimWorld installation folder.
    echo Current path: "%GAME_DIR%"
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is required. Install a supported .NET SDK and retry.
    exit /b 1
)

dotnet build "%~dp0Source\AutoBuildInstantConstruction.csproj" --configuration Release --nologo -p:GameDir="%GAME_DIR%"
if errorlevel 1 (
    echo.
    echo Build failed.
    exit /b 1
)

echo.
echo Build succeeded:
echo %~dp0Assemblies\AutoBuildInstantConstruction.dll
exit /b 0
