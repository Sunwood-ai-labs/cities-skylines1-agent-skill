# Release QA Inventory

## Release Context

- repository: `cities-skylines1-agent-skill`
- release tag: `v0.1.0`
- compare range: `<none>; initial release mode`, target `HEAD`
- requested outputs: GitHub release body draft, docs-backed release notes
- validation commands run: `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\collect-release-context.ps1 -Target HEAD`, `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\verify-svg-assets.ps1 -RepoPath . -Path docs/public/agent-bridge-icon.svg,docs/public/release-header-v0.1.0.svg`, `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-doc-links.ps1`, `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\verify-release-qa-inventory.ps1 -RepoPath . -Tag v0.1.0`; attempted `npm install --package-lock=false` but `npm` was unavailable on PATH
- release URLs: pending publication; release body saved at `tmp/release-notes-v0.1.0.md`

## Claim Matrix

| claim | code refs | validation refs | docs surfaces touched | scope |
| --- | --- | --- | --- | --- |
| Initial release ships a CS1 localhost API bridge with read and command endpoints | `src/ApiServer.cs`, `src/GameState.cs`, `src/RoadCommands.cs`, `src/BuildingCommands.cs`, `src/BulldozeCommands.cs`, `src/SaveCommands.cs`, `src/SimulationCommands.cs`, `src/ZoneCommands.cs`, `src/BatchCommands.cs` | Endpoint route search with `rg`, collector changed-file list | `README.md`, `README.ja.md`, `docs/api.md`, `docs/ja/api.md`, `docs/releases/v0.1.0/index.md`, `docs/ja/releases/v0.1.0/index.md` | initial_release |
| State-changing operations are queued onto the CS1 game thread and surfaced through in-game API notifications | `src/CommandQueue.cs`, `src/AgentBridgeThreading.cs`, `src/AgentBridgeNotifier.cs`, `src/ApiServer.cs`, `src/AgentBridge.cs` | Game-thread and notifier route search with `rg` | `README.md`, `README.ja.md`, `docs/api.md`, `docs/ja/api.md`, `docs/releases/v0.1.0/index.md`, `docs/ja/releases/v0.1.0/index.md` | initial_release |
| Repository includes operator scripts for build, launch, smoke test, bounded repair, and saving workflows | `scripts/build.ps1`, `scripts/start-resume.ps1`, `scripts/start-new-map.ps1`, `scripts/smoke-test.ps1`, `scripts/inspect-road-anomalies.ps1`, `scripts/repair-road-anomalies.ps1`, `scripts/repair-service-overlap.ps1`, `scripts/save-city.ps1` | collector changed-file list, script inspection | `README.md`, `README.ja.md`, `docs/guide/getting-started.md`, `docs/guide/usage.md`, `docs/ja/guide/getting-started.md`, `docs/ja/guide/usage.md`, `docs/releases/v0.1.0/index.md`, `docs/ja/releases/v0.1.0/index.md` | initial_release |
| Repository is packaged as a Codex skill with public docs and CI/Pages workflows | `SKILL.md`, `agents/openai.yaml`, `docs/.vitepress/config.mts`, `.github/workflows/docs.yml`, `.github/workflows/pages.yml` | collector changed-file list, docs config inspection | `README.md`, `README.ja.md`, `docs/index.md`, `docs/ja/index.md`, `docs/releases/v0.1.0/index.md`, `docs/ja/releases/v0.1.0/index.md` | initial_release |

## Steady-State Docs Review

| surface | status | evidence |
| --- | --- | --- |
| README.md | pass | Reviewed existing quick start, API surface, skill usage, docs links, and status text; no truth-sync update required for the release-note-only change. |
| README.ja.md | pass | Reviewed Japanese README for the same operator-facing claims; no truth-sync update required. |
| docs/api.md | pass | Reviewed endpoint list and examples against `src/ApiServer.cs`; release page links to this existing reference. |
| docs/ja/api.md | pass | Reviewed localized API reference presence; release page links to this existing reference. |
| docs/guide/getting-started.md | pass | Reviewed build/install/resume and smoke-test guidance; release page links to this existing guide. |
| docs/guide/usage.md | pass | Reviewed inspect-first repair loop; release page links to this existing guide. |
| docs/ja/guide/getting-started.md | pass | Reviewed localized setup guidance; release page links to this existing guide. |
| docs/ja/guide/usage.md | pass | Reviewed localized agent workflow guidance; release page links to this existing guide. |
| docs/index.md | pass | Reviewed English docs home for current repository scope and primary workflow claims; no truth-sync update required. |
| docs/ja/index.md | pass | Reviewed Japanese docs home for current repository scope and primary workflow claims; no truth-sync update required. |
| docs/.vitepress/config.mts | pass | Updated navigation/sidebar with release-note links for English and Japanese docs. |
| docs/releases/v0.1.0/index.md | pass | Added docs-backed English release notes. |
| docs/ja/releases/v0.1.0/index.md | pass | Added docs-backed Japanese release notes. |

## QA Inventory

| criterion_id | status | evidence |
| --- | --- | --- |
| compare_range | pass | No existing tags after `git fetch --tags --prune`; collector ran with `-Target HEAD` and reported initial release mode. |
| release_claims_backed | pass | Claim matrix references inspected source, script, docs, and workflow files from the collector output. |
| docs_release_notes | pass | `docs/releases/v0.1.0/index.md`, `docs/ja/releases/v0.1.0/index.md` |
| companion_walkthrough | user_waived | User requested release notes for v0.1.0 only; existing walkthrough/article content remains at `docs/articles/building-cities-skylines-with-ai-agents-ja.md`. |
| operator_claims_extracted | pass | Claim matrix captures API bridge, threading/notifications, scripts, skill packaging, and docs/CI claims. |
| impl_sensitive_claims_verified | pass | Endpoint, game-thread, notification, save, script, and docs workflow claims were checked against the referenced code paths and workflow files. |
| steady_state_docs_reviewed | pass | README, localized README, API references, getting-started guides, usage guides, and VitePress config are listed in Steady-State Docs Review. |
| claim_scope_precise | pass | Wording scopes this as an initial release for the repository state included in `v0.1.0`. |
| latest_release_links_updated | pass | VitePress nav/sidebar now exposes `v0.1.0` release notes; no prior latest-release pointer existed. |
| svg_assets_validated | pass | `verify-svg-assets.ps1 -RepoPath . -Path docs/public/agent-bridge-icon.svg,docs/public/release-header-v0.1.0.svg` passed. |
| docs_assets_committed_before_tag | not_applicable | No tag creation or GitHub publication was requested or performed in this task. |
| docs_deployed_live | not_applicable | No docs deployment was requested or performed; GitHub release body is saved as a draft. |
| tag_local_remote | not_applicable | `v0.1.0` tag has not been created locally or remotely. |
| github_release_verified | not_applicable | GitHub Release was not created or edited. |
| validation_commands_recorded | pass | Release Context lists collector, SVG validation, docs link validation, QA inventory validation, and the failed docs build prerequisite attempt caused by missing `npm`. |
| publish_date_verified | not_applicable | Draft does not hardcode a publication date because the release does not exist yet. |

## Notes

- blockers: none for draft release notes
- waivers: companion walkthrough was treated as out of scope because the user asked specifically for release notes
- follow-up docs tasks: after publishing, verify live docs URLs before editing the final GitHub release body
