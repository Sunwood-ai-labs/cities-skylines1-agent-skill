---
name: cities-skylines1-agent-skill
description: "Operate Cities: Skylines 1 through the Skylines Agent Bridge mod and localhost API. Use when Codex needs to build, inspect, repair, resume, save, or continue a CS1 city using focused API calls rather than screenshot recognition, including road connectivity, service infrastructure, zoning, facilities, problem icons, and save verification."
---

# Cities: Skylines 1 Agent Skill

Use this skill to control a running Cities: Skylines 1 city through the local Skylines Agent Bridge API.

## Core Rules

- Prefer API state over image recognition.
- Resume and repair existing saves by default. Start fresh only when explicitly requested.
- Use small, separated commands: inspect, bulldoze, build, place, move, zone, simulate, save.
- Save after meaningful city changes and verify the `.crp` file exists.
- Commit repository changes after each coherent code/docs task when working inside this repository.

## Local Setup

The bridge listens on:

```text
http://127.0.0.1:32123
```

Build and install the mod:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Resume the latest save:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

Start a new map only when the user asks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

## Inspection Loop

Use these before acting:

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
Invoke-RestMethod "http://127.0.0.1:32123/state/problems?limit=200"
Invoke-RestMethod "http://127.0.0.1:32123/state/economy"
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?limit=500&nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200"
Invoke-RestMethod "http://127.0.0.1:32123/state/facilities?limit=500"
Invoke-RestMethod "http://127.0.0.1:32123/state/networks?limit=1000&service=Road"
```

Use `includeMapObjects=true` on `/state/facilities` only when raw helper objects such as pipe junctions are needed.

## Command Pattern

Use separate commands so the repair remains auditable.

Delete a bad segment:

```powershell
$body = @{ entityType = "netSegment"; id = 19023; keepNodes = $false } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/bulldoze -Body $body -ContentType "application/json"
```

Build a network segment:

```powershell
$body = @{
  roadPrefab = "Basic Road"
  start = @{ x = 400; z = 300 }
  end = @{ x = 423.614; z = 554.945 }
  name = "Agent Highway Link"
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/build-network -Body $body -ContentType "application/json"
```

Place or move a building:

```powershell
$body = @{ buildingPrefab = "Water Tower"; position = @{ x = 120; z = -220 }; angleDegrees = 0 } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/place-building -Body $body -ContentType "application/json"
```

Paint zones:

```powershell
$body = @{ zone = "ResidentialLow"; center = @{ x = 240; z = -40 }; radius = 70 } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-zone -Body $body -ContentType "application/json"
```

Run simulation:

```powershell
$body = @{ paused = $false; speed = 3 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-simulation-speed -Body $body -ContentType "application/json"
```

Lower taxes when `/state/problems` reports `TaxesTooHigh`:

```powershell
$body = @{ service = "Commercial"; rate = 9 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-tax-rate -Body $body -ContentType "application/json"
```

Save and verify:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\save-city.ps1 -Name AgentAutoSave-clean
Invoke-RestMethod http://127.0.0.1:32123/state/saves
```

## Known Gotchas

- CS1 network crossings are not intersections unless a real node is created. If a pipe or road visually crosses another segment but does not connect, bulldoze and rebuild split segments with a shared endpoint.
- Heating service buildings may create an actual connection helper offset from the building center. Query `/state/facilities?service=Water&includeMapObjects=true` when diagnosing heating pipe issues.
- Roads that look connected to highways can still have separate nodes. Use `/state/road-anomalies` and rebuild with endpoints close enough to reuse the existing road nodes.
- Do not treat all dead ends as errors. Use bounded checks or `includeDeadEnds=false` unless the user asks to remove cul-de-sacs/stubs.
