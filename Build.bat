@echo off
setlocal
cd /d "%~dp0"

set "GAME_DIR=%RIMWORLD_DIR%"
if "%GAME_DIR%"=="" set "GAME_DIR=G:\SteamLibrary\steamapps\common\RimWorld"

set "HARMONY_DLL=%HARMONY_DLL%"
if "%HARMONY_DLL%"=="" set "HARMONY_DLL=G:\SteamLibrary\steamapps\workshop\content\294100\2009463077\1.7\Assemblies\0Harmony.dll"

for %%F in (Assembly-CSharp.dll RimWorld.dll UnityStubs.dll Waylith.dll) do (
    if not exist "%GAME_DIR%\%%F" (
        echo ERROR: Required RimWorld 1.7 assembly not found:
        echo "%GAME_DIR%\%%F"
        exit /b 1
    )
)

if not exist "%HARMONY_DLL%" (
    echo ERROR: 0Harmony.dll was not found:
    echo "%HARMONY_DLL%"
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK 10 is required. Install .NET 10 SDK and retry.
    exit /b 1
)

dotnet build "%~dp0Source\AutoBuildInstantConstruction.csproj" --configuration Release --nologo -p:GameDir="%GAME_DIR%" -p:HarmonyDll="%HARMONY_DLL%"
if errorlevel 1 (
    echo.
    echo Build failed.
    exit /b 1
)

echo.
echo Build succeeded:
echo %~dp0Assemblies\AutoBuildInstantConstruction.dll
exit /b 0
