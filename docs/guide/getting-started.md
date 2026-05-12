# Getting Started

This guide assumes a Windows machine with Cities: Skylines 1 installed through Steam.

## Requirements

- Cities: Skylines 1.
- PowerShell.
- .NET Framework compiler tools available through Windows.
- CS1 managed assemblies under the game install directory.

The default build script expects the game at:

```text
D:\SteamLibrary\steamapps\common\Cities_Skylines
```

If your install path is different, edit `scripts/build.ps1` before building.

## Build and Install

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The script compiles `SkylinesAgentBridge.dll` and copies it into:

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge
```

Enable the mod in the CS1 content manager before loading a city.

## Resume a City

The normal loop starts from the latest local save:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

The script builds the mod unless `-SkipBuild` is passed, launches CS1 through Steam, clicks the launcher Resume button, and waits for the local API.

## Start a Fresh Map

For clean experiments:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

Useful flags:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipNewMap
```

## Verify the Bridge

Once a city is loaded:

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
```

Use `scripts/smoke-test.ps1` for a broader read and dry-run command check.
