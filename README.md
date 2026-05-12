# Skylines Agent Bridge

Experimental Cities: Skylines 1 mod that exposes a localhost HTTP API for AI agents.

The mod is intentionally small at first. Agents should usually call the focused endpoints one at a time; `batch` exists as a convenience for compact experiments.

- `GET /health`
- `GET /state/summary`
- `GET /state/problems`
- `GET /state/facilities`
- `GET /state/networks`
- `GET /state/road-anomalies`
- `GET /state/building-anomalies`
- `GET /state/saves`
- `GET /prefabs/roads`
- `GET /prefabs/networks`
- `GET /prefabs/buildings`
- `POST /commands/build-network`
- `POST /commands/build-road` compatibility alias
- `POST /commands/set-zone`
- `POST /commands/place-building`
- `POST /commands/move-building`
- `POST /commands/bulldoze`
- `POST /commands/save`
- `POST /commands/set-simulation-speed`
- `POST /commands/batch` optional

The HTTP server runs on `127.0.0.1:32123`. Requests that touch game state are queued and executed from the CS1 simulation update callback, so external agents do not directly mutate game objects from an arbitrary network thread.

## Build

Edit `scripts/build.ps1` if your Cities: Skylines install path differs, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The script compiles `SkylinesAgentBridge.dll` and copies it into:

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge
```

Enable the mod in the CS1 content manager, load a city, then query:

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
```

Or run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1
```

## Start A Fresh Map

On this machine, this script kills the current CS1 process, launches through Steam and the Paradox Launcher, dismisses the startup modals, starts the currently selected new-game map, and waits for the Agent Bridge API:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

Useful flags:

```powershell
# Do not rebuild the DLL first.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipBuild

# Launch and wait for API without starting a new map from the menu.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipNewMap
```

## Resume Latest Save

This is the normal repair loop. It launches through Steam, clicks the launcher
Resume button, waits for the API, and prints the newest local save before
launching:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

## Road Command

Dry run:

```powershell
$body = @{
  dryRun = $true
  roadPrefab = "Basic Road"
  start = @{ x = 0; z = 0 }
  end = @{ x = 80; z = 0 }
  name = "Agent Test Road"
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/build-network -Body $body -ContentType "application/json"
```

Actual build:

```powershell
$body = @{
  dryRun = $false
  roadPrefab = "Basic Road"
  start = @{ x = 0; z = 0 }
  end = @{ x = 80; z = 0 }
  name = "Agent Test Road"
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/build-network -Body $body -ContentType "application/json"
```

This is an early bridge, not a mature city planner. Use a throwaway save while testing.

Zone paint dry run:

```powershell
$body = @{
  dryRun = $true
  zone = "ResidentialLow"
  center = @{ x = 40; z = 0 }
  radius = 32
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-zone -Body $body -ContentType "application/json"
```

Batch dry run:

```powershell
$body = @{
  dryRun = $true
  stopOnError = $true
  commands = @(
    @{
      type = "build-road"
      roadPrefab = "Basic Road"
      start = @{ x = 120; z = 0 }
      end = @{ x = 200; z = 0 }
      name = "Agent Batch Road"
    },
    @{
      type = "set-zone"
      zone = "ResidentialLow"
      center = @{ x = 160; z = 0 }
      radius = 48
    }
  )
} | ConvertTo-Json -Depth 6

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/batch -Body $body -ContentType "application/json"
```
