# Architecture

The repository has two connected parts: a CS1 mod that exposes a local API and a Codex skill that teaches an agent how to use that API safely.

## Runtime Shape

```text
Codex agent
  -> PowerShell scripts
  -> http://127.0.0.1:32123
  -> SkylinesAgentBridge mod
  -> CS1 game thread queue
  -> game managers and save panel
```

The API server accepts local HTTP requests. State reads and mutating commands are dispatched through a game-thread queue so CS1 simulation objects are touched from the correct thread.

## Source Layout

```text
src/
├── ApiServer.cs
├── GameState.cs
├── RoadCommands.cs
├── BuildingCommands.cs
├── ZoneCommands.cs
├── BulldozeCommands.cs
├── SimulationCommands.cs
├── SaveCommands.cs
├── BatchCommands.cs
└── AgentBridge*.cs
```

`ApiServer.cs` maps HTTP paths to state builders or command handlers. `GameState.cs` builds JSON for city state. The command files keep mutating operations separated so an agent can choose one clear action at a time.

## In-Game Notifications

Game-state API calls are written to a small in-game API console instead of a
temporary toast. The console keeps recent request history, shows timestamps,
and has a minimize button so it can stay available without covering the city.

`/health` is intentionally quiet because it can be called before a level is loaded.

## Skill Metadata

`SKILL.md` contains the agent operating procedure. `agents/openai.yaml` provides the display name, default prompt, and implicit invocation setting for Codex skill surfaces.
