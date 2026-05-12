![cities-skylines1-agent-skill v0.1.0](https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/release-header-v0.1.0.svg)

# cities-skylines1-agent-skill v0.1.0

`v0.1.0` is the initial release of `cities-skylines1-agent-skill`. There is no previous tag, so these notes cover the full repository state shipped with the tag.

Docs:

- [Release notes](https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/releases/v0.1.0/)
- [Japanese release notes](https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/ja/releases/v0.1.0/)
- [Getting Started](https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/guide/getting-started)
- [API Reference](https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/api)

## Highlights

- Ships a Cities: Skylines 1 mod that exposes a local HTTP bridge on `http://127.0.0.1:32123`.
- Adds read APIs for city summary, problem icons, facilities, networks, road anomalies, building placement anomalies, saves, and loaded prefabs.
- Adds command APIs for building networks, zoning, placing and moving buildings, bulldozing, simulation speed, batch helpers, and saving.
- Routes state-changing work through the CS1 game thread and shows in-game API activity notifications for operator visibility.
- Packages the repository as a Codex skill with `SKILL.md` and `agents/openai.yaml`.
- Publishes a VitePress documentation site with English and Japanese guides, API references, screenshots, and GitHub Pages workflows.

## Tooling And Automation

- `scripts/build.ps1` compiles and installs `SkylinesAgentBridge.dll`.
- `scripts/start-resume.ps1` and `scripts/start-new-map.ps1` launch CS1 flows and wait for the API.
- `scripts/smoke-test.ps1` checks health, state reads, prefabs, and dry-run commands.
- `scripts/inspect-road-anomalies.ps1`, `scripts/repair-road-anomalies.ps1`, and `scripts/repair-service-overlap.ps1` support bounded repair workflows.
- `scripts/save-city.ps1` requests a save and polls `/state/saves`.
- `scripts/check-doc-links.ps1` validates docs links used by CI.

## Validation

Release-note preparation used the bundled `gh-release-notes` collector in initial release mode, inspected the key API, script, docs, and workflow files, and validated SVG release assets.

Docs link validation and VitePress build were run successfully after installing Node.js/npm in the local environment.

The release notes were prepared from the repository state included in the `v0.1.0` tag.
