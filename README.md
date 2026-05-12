# cities-skylines1-agent-skill

[日本語 README](README.ja.md)

An experimental Codex skill and Cities: Skylines 1 mod for letting an AI agent inspect, build, repair, and save a CS1 city through a local HTTP API.

The goal is simple: stop relying on screenshots for city state, expose useful game data as API responses, and let agents make small explicit changes such as "delete this segment", "build this road", "place this service", "paint this zone", and "save the city".

![API notification overlay in Cities: Skylines 1](docs/assets/api-notification.jpg)

## What It Does

- Runs a CS1 mod that listens on `http://127.0.0.1:32123`.
- Exposes city state APIs for problems, facilities, networks, road anomalies, building placement anomalies, saves, and prefabs.
- Exposes focused command APIs for network creation, zoning, building placement, building movement, bulldozing, simulation speed, and saving.
- Shows in-game API activity notifications so the CS1 screen reflects what the agent is doing.
- Includes Windows scripts for building the mod, launching Resume, starting a fresh map, developing a starter city, inspecting issues, and saving.

## Screenshot Tour

### In-Game API Notifications

Every game-state API request appears in the CS1 UI for a few seconds.

![API notification overlay](docs/assets/api-notification.jpg)

### Agent-Built Starter City

The bridge can resume a save, inspect city data, repair infrastructure, and keep developing the city without starting over.

![Agent-built starter city](docs/assets/city-overview.jpg)

### Road Repair Workflow

Road issues are detected from CS1 network data, not image recognition. The agent can then call separate APIs to bulldoze bad segments and rebuild clean connections.

## API Surface

Read APIs:

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

Command APIs:

- `POST /commands/build-network`
- `POST /commands/build-road` compatibility alias
- `POST /commands/set-zone`
- `POST /commands/place-building`
- `POST /commands/move-building`
- `POST /commands/bulldoze`
- `POST /commands/save`
- `POST /commands/set-simulation-speed`
- `POST /commands/batch` optional convenience wrapper

See [docs/api.md](docs/api.md) for request examples and response shapes.

## Skill Usage

This repository is also a Codex skill. The root [SKILL.md](SKILL.md) tells an agent how to operate CS1 through this bridge.

Example prompt:

```text
Use $cities-skylines1-agent-skill to resume my CS1 city, inspect current problems, repair road/infrastructure issues with separate API calls, save the city, and report what changed.
```

## Article

- [Japanese: Letting Codex build a Cities: Skylines city as an AI mayor](docs/articles/building-cities-skylines-with-ai-agents-ja.md)

## Build The Mod

Edit `scripts/build.ps1` if your CS1 install path differs, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The script compiles `SkylinesAgentBridge.dll` and copies it into:

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge
```

Enable the mod in the CS1 content manager, load a city, then test:

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
```

## Resume A City

This is the normal repair loop. It launches through Steam, clicks the Paradox Launcher Resume button, waits for the API, and prints the newest local save before launching:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

## Start A Fresh Map

For clean experiments:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

Useful flags:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1 -SkipNewMap
```

## Agent Repair Pattern

Keep the agent workflow generic. Prefer separate commands over a magical repair endpoint:

1. Inspect with `/state/problems`, `/state/road-anomalies`, `/state/building-anomalies`, `/state/facilities`, and `/state/networks`.
2. Remove bad objects with `/commands/bulldoze`.
3. Rebuild with `/commands/build-network`, `/commands/place-building`, `/commands/move-building`, and `/commands/set-zone`.
4. Let the simulation settle with `/commands/set-simulation-speed`.
5. Re-check state APIs.
6. Save with `/commands/save` and verify with `/state/saves`.

## Repository Layout

```text
.
├── SKILL.md                 # Codex skill instructions
├── agents/openai.yaml       # Skill UI metadata
├── src/                     # CS1 mod source
├── scripts/                 # Build, launch, inspect, repair, save scripts
├── docs/api.md              # API reference
└── docs/assets/             # README screenshots
```

## Status

This is experimental and built for CS1 on Windows. Test on throwaway saves first. The bridge mutates live CS1 simulation objects through game-thread queued commands, so keep changes small and verify after each step.

## License

MIT. See [LICENSE](LICENSE).
