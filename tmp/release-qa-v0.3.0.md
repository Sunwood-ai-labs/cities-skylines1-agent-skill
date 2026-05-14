# Release QA Inventory

## Release Context

- repository: `Sunwood-ai-labs/cities-skylines1-agent-skill`
- release tag: `v0.3.0`
- compare range: `v0.1.0..v0.3.0`
- requested outputs: GitHub release body, docs-backed release notes, companion walkthrough article
- validation commands run: `.\scripts\build.ps1`, `.\scripts\check-doc-links.ps1`, `npm.cmd run build`, `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\verify-svg-assets.ps1 -RepoPath . -Path docs/public/release-header-v0.3.0.svg`, `powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\makim\.codex\skills\gh-release-notes\scripts\verify-release-qa-inventory.ps1 -RepoPath . -Tag v0.3.0`, `gh release view v0.3.0 --json tagName,name,url,isDraft,isPrerelease,publishedAt,body`, `Invoke-WebRequest` checks for release docs, articles, and header SVG
- release URLs: GitHub Release https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill/releases/tag/v0.3.0, English docs https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/releases/v0.3.0/, Japanese docs https://sunwood-ai-labs.github.io/cities-skylines1-agent-skill/ja/releases/v0.3.0/

## Claim Matrix

| claim | code refs | validation refs | docs surfaces touched | scope |
| --- | --- | --- | --- | --- |
| v0.3.0 adds demand, chirps, zones, economy, growables, external connections, and zone anomaly state APIs | `src/ApiServer.cs`, `src/GameState.cs`, `src/EconomyCommands.cs` | `git diff v0.1.0..v0.3.0`, `.\scripts\build.ps1` | `docs/api.md`, `docs/ja/api.md`, `docs/releases/v0.3.0/index.md`, `docs/ja/releases/v0.3.0/index.md`, `docs/guide/articles/v0-3-0-agent-city-operations.md`, `docs/ja/guide/articles/v0-3-0-agent-city-operations.md` | API state surfaces |
| v0.3.0 adds tax-rate, blocked-asset, building-active, zone-repair, cluster-repair, and safer bulldoze commands | `src/ApiServer.cs`, `src/EconomyCommands.cs`, `src/AssetCommands.cs`, `src/BuildingCommands.cs`, `src/ZoneCommands.cs`, `src/BulldozeCommands.cs`, `src/GameThreadHelpers.cs` | `git show v0.3.0^{}:src/ApiServer.cs`, `.\scripts\build.ps1` | `docs/api.md`, `docs/ja/api.md`, `docs/releases/v0.3.0/index.md`, `docs/ja/releases/v0.3.0/index.md`, `docs/guide/articles/v0-3-0-agent-city-operations.md`, `docs/ja/guide/articles/v0-3-0-agent-city-operations.md` | command APIs |
| road anomaly collection includes expanded anomaly types and avoids full pair scans for overlap checks | `src/GameState.cs` | `git show 55eda54`, `.\scripts\build.ps1` | `docs/api.md`, `docs/ja/api.md`, `docs/releases/v0.3.0/index.md`, `docs/ja/releases/v0.3.0/index.md`, `docs/guide/articles/v0-3-0-agent-city-operations.md`, `docs/ja/guide/articles/v0-3-0-agent-city-operations.md` | road QA |
| operator visibility is improved with a persistent draggable API console | `src/AgentBridgeNotifier.cs`, `src/ApiServer.cs` | `git diff v0.1.0..v0.3.0 -- src/AgentBridgeNotifier.cs src/ApiServer.cs`, `.\scripts\build.ps1` | `README.md`, `README.ja.md`, `docs/api.md`, `docs/ja/api.md`, `docs/releases/v0.3.0/index.md`, `docs/ja/releases/v0.3.0/index.md`, `docs/guide/articles/v0-3-0-agent-city-operations.md`, `docs/ja/guide/articles/v0-3-0-agent-city-operations.md` | in-game UI |
| local scripts now cover city parameter logging and infrastructure/service-overlap workflows | `scripts/log-city-parameters.ps1`, `scripts/develop-city-with-infrastructure.ps1`, `scripts/repair-service-overlap.ps1` | `git diff v0.1.0..v0.3.0 -- scripts`, `.\scripts\check-doc-links.ps1` | `docs/releases/v0.3.0/index.md`, `docs/ja/releases/v0.3.0/index.md`, `docs/guide/development-flow.md`, `docs/ja/guide/development-flow.md` | operator scripts |

## Steady-State Docs Review

| surface | status | evidence |
| --- | --- | --- |
| README.md | pass | Already describes the expanded API surface, persistent console, repair pattern, and docs links included in v0.3.0 |
| README.ja.md | pass | Reviewed for parity with README and existing v0.3.0-facing API and workflow claims |
| SKILL.md | pass | Reviewed current skill instructions for API-driven CS1 operation; no release-note-only changes needed |
| docs/api.md | pass | Existing API reference covers the v0.3.0 state and command endpoints referenced by release notes |
| docs/ja/api.md | pass | Japanese API reference mirrors the v0.3.0 endpoint coverage |
| docs/guide/usage.md | pass | Existing inspect-first workflow remains accurate for v0.3.0 |
| docs/ja/guide/usage.md | pass | Japanese workflow remains accurate for v0.3.0 |
| docs/guide/development-flow.md | pass | Existing release and branch workflow matches the v0.3.0 release process |
| docs/ja/guide/development-flow.md | pass | Japanese development-flow page mirrors the release process |
| docs/releases/v0.3.0/index.md | pass | Added English docs-backed release notes |
| docs/ja/releases/v0.3.0/index.md | pass | Added Japanese docs-backed release notes |
| docs/guide/articles/v0-3-0-agent-city-operations.md | pass | Added English companion walkthrough article |
| docs/ja/guide/articles/v0-3-0-agent-city-operations.md | pass | Added Japanese companion walkthrough article |

## QA Inventory

| criterion_id | status | evidence |
| --- | --- | --- |
| compare_range | pass | Compared `v0.1.0..v0.3.0` with `git log`, `git diff --stat`, and changed-file inspection |
| release_claims_backed | pass | Claim matrix cites `src/ApiServer.cs`, `src/GameState.cs`, command modules, scripts, and docs diffs |
| docs_release_notes | pass | docs/releases/v0.3.0/index.md, docs/ja/releases/v0.3.0/index.md |
| companion_walkthrough | pass | docs/guide/articles/v0-3-0-agent-city-operations.md, docs/ja/guide/articles/v0-3-0-agent-city-operations.md |
| operator_claims_extracted | pass | Claim matrix lists API, command, road QA, console, and script claims |
| impl_sensitive_claims_verified | pass | Verified endpoint routing in `src/ApiServer.cs`, release fallback behavior in `src/GameThreadHelpers.cs`, economy behavior in `src/EconomyCommands.cs`, and console behavior in `src/AgentBridgeNotifier.cs` |
| steady_state_docs_reviewed | pass | README, SKILL, API docs, workflow docs, release pages, and article pages reviewed in the table above |
| claim_scope_precise | pass | Claims are scoped to specific endpoints, command APIs, scripts, and the in-game console |
| latest_release_links_updated | pass | `docs/.vitepress/config.mts` updated release nav/sidebar links from v0.1.0 to v0.3.0 |
| svg_assets_validated | pass | Validated `docs/public/release-header-v0.1.0.svg`, `docs/public/agent-bridge-icon.svg`, and `docs/public/release-header-v0.3.0.svg` with `verify-svg-assets.ps1` |
| docs_assets_committed_before_tag | not_applicable | `v0.3.0` tag already existed before this release-note task; docs collateral is committed before GitHub Release publication |
| docs_deployed_live | pass | Pages deploy run `25810189260` succeeded and `Invoke-WebRequest` returned 200 for English release notes, Japanese release notes, English article, Japanese article, and `release-header-v0.3.0.svg` |
| tag_local_remote | pass | `git tag --format` shows local `v0.3.0`; `git push origin v0.3.0` completed before this task |
| github_release_verified | pass | `gh release view v0.3.0` returned https://github.com/Sunwood-ai-labs/cities-skylines1-agent-skill/releases/tag/v0.3.0 as a non-draft non-prerelease release |
| validation_commands_recorded | pass | Release Context records build, docs link, docs build, SVG, and QA validation commands |
| publish_date_verified | pass | `gh release view v0.3.0 --json publishedAt` returned `2026-05-13T15:51:03Z` |

## Notes

- blockers: none
- waivers: `docs_assets_committed_before_tag` is not applicable because the release tag was already created and pushed before the release-note request
- follow-up docs tasks: none
