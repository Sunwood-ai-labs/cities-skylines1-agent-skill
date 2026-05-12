# v0.1.0 Release Notes

![v0.1.0 release header](/release-header-v0.1.0.svg)

`v0.1.0` is the initial release of `cities-skylines1-agent-skill`. There is no previous tag, so these notes cover the full repository state shipped with the tag.

## Highlights

- Ships a Cities: Skylines 1 mod that exposes a local HTTP bridge on `http://127.0.0.1:32123`.
- Adds read APIs for city summary, problem icons, facilities, networks, road anomalies, building placement anomalies, saves, and loaded prefabs.
- Adds command APIs for building networks, zoning, placing and moving buildings, bulldozing, simulation speed, batch helpers, and saving.
- Routes state-changing work through the CS1 game thread and shows in-game API activity notifications for operator visibility.
- Packages the repository as a Codex skill with `SKILL.md` and `agents/openai.yaml`.
- Publishes a VitePress documentation site with English and Japanese guides, API references, screenshots, and GitHub Pages workflows.

## Agent Bridge Mod

The release includes the core mod source under `src/`. `ApiServer.cs` registers the bridge endpoints, `GameState.cs` reads CS1 simulation data, and command modules keep each action explicit: roads and other networks, zoning, building placement, bulldozing, saves, simulation speed, and limited batch operations.

The API is intentionally inspect-first. Agents can read the real city graph and facility state before mutating the simulation, then perform one small command at a time and verify the result.

## Tooling And Automation

PowerShell scripts under `scripts/` cover the local operator loop:

- `build.ps1` compiles and installs `SkylinesAgentBridge.dll`.
- `start-resume.ps1` and `start-new-map.ps1` launch CS1 flows and wait for the API.
- `smoke-test.ps1` checks health, state reads, prefabs, and dry-run commands.
- `inspect-road-anomalies.ps1`, `repair-road-anomalies.ps1`, and `repair-service-overlap.ps1` support bounded repair workflows.
- `save-city.ps1` requests a save and polls `/state/saves`.
- `check-doc-links.ps1` validates local docs links used by CI.

## Docs And Assets

The docs surface includes:

- `README.md` and `README.ja.md` for public repository entry points.
- VitePress docs under `docs/` with getting started, agent workflow, architecture, troubleshooting, and API reference pages.
- Japanese localized docs under `docs/ja/`.
- Screenshots for the in-game API notification overlay and an agent-built city overview.
- Docs and Pages workflows under `.github/workflows/`.

## Validation

Release-note preparation used the bundled `gh-release-notes` collector in initial release mode, inspected the key API, script, docs, and workflow files, and validated SVG release assets.

Additional checks run for this note:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\verify-svg-assets.ps1 -RepoPath . -Path docs/public/agent-bridge-icon.svg
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1
npm install
npm run build
```

The release notes were prepared from the repository state included in the `v0.1.0` tag.

## Links

- [Getting Started](/guide/getting-started)
- [Agent Workflow](/guide/usage)
- [Architecture](/guide/architecture)
- [API Reference](/api)
- [Japanese release notes](/ja/releases/v0.1.0/)
