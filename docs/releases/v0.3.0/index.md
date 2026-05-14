# v0.3.0 Release Notes

![v0.3.0 release header](/release-header-v0.3.0.svg)

`v0.3.0` turns the bridge from a basic command surface into a stronger city-operations loop. Compared with `v0.1.0`, agents can read more of the city's live state, diagnose road, zoning, economy, and asset issues with API data, make smaller repair commands, and keep the operator informed through the in-game API console.

## Highlights

- Adds state APIs for demand, Chirper messages, zoning totals, growables, economy, external road connections, and zone anomalies.
- Adds command APIs for tax rates, blocked asset disabling, building activation, zone repair, cluster zone repair, and safer bulldozing.
- Expands road anomaly detection with duplicate, overlap, crossing-without-node, terrain cliff, sunken road, short stub, dead-end, and outside-connection checks.
- Improves broad zoning commands so occupied blocks are protected by default and repair commands can align zoning with nearby growable buildings.
- Replaces the transient notification overlay with a persistent in-game API console that keeps recent calls, supports clear/minimize, and can be dragged.
- Adds local operator scripts for city parameter logging, infrastructure development, and service-overlap repair workflows.
- Documents the lightweight Git Flow review process and hardens docs link validation for CI.

## City State And Economy

The API now exposes more signals that an agent can use before changing the city:

- `/state/demand` returns residential, commercial, and workplace demand bars.
- `/state/chirps` reads recent CS1 messages without OCR.
- `/state/zones` summarizes zoning cells and approximate area by zone type.
- `/state/growables` lists existing residential, commercial, industrial, and office growables with position and state.
- `/state/economy` returns aggregate UI tax sliders and detailed tax-rate rows.
- `/state/external-connections` reports whether the local road component is connected to outside road nodes.
- `/state/zone-anomalies` identifies mixed zone blocks and patchy unzoned holes.

`/commands/set-tax-rate` can set residential, commercial, industrial, and office tax rates, with `dryRun` support and optional `service`, `subService`, and `level` filters.

## Repair And Safety Improvements

v0.3.0 focuses on inspect-first repair rather than hidden one-shot fixes. Road anomaly collection now covers visual overlaps that are not real graph connections, duplicate road segments, dead-end stubs, terrain cliffs, and agent-built ground roads that sit below the local grade. The review fix for this release also replaces the road-overlap nested scan with spatial grid candidate filtering so `/state/road-anomalies` avoids unnecessary full pair scans on larger cities.

Zoning is safer as well. `/commands/set-zone` now defaults to `preserveOccupied: true`, `RepairZonesToGrowables` aligns non-empty zoning cells with nearby growable buildings, and `RepairZoneClusters` can repair larger mottled 80m clusters while preferring existing growable zone context.

Bulldozing and building movement share `GameThreadHelpers` for same-thread release fallbacks. The segment fallback preserves `keepNodes` when the underlying private CS1 method supports it, avoiding a review-identified bug where node preservation could be lost.

## Operator Tooling

New and updated scripts support repeatable local operations:

- `scripts/log-city-parameters.ps1` records summary, demand, economy, and problem samples to JSONL and CSV, and logs tax changes as events.
- `scripts/develop-city-with-infrastructure.ps1` builds a connected starter city with roads, utilities, services, extra zoning, simulation settling, and a save.
- `scripts/repair-service-overlap.ps1` relocates service buildings and supporting infrastructure away from road centerlines.
- `scripts/check-doc-links.ps1` now validates bracketed Markdown links and ignores generated dependency/build folders.

## Docs And Release Process

The release updates English and Japanese README/API docs, adds a Japanese contributing guide, introduces a development-flow guide, and keeps the VitePress docs build reproducible through `docs/package-lock.json`.

The repository now documents the branch model used for this release: feature work targets `develop`, release stabilization targets `main`, and release fixes are back-merged into `develop`.

## Validation

The release branch was built and docs links were checked before merge:

```powershell
.\scripts\build.ps1
.\scripts\check-doc-links.ps1
```

Release-note preparation also validates the new SVG header and rebuilds the docs site before publishing.

## Links

- [Walkthrough article](/guide/articles/v0-3-0-agent-city-operations)
- [API Reference](/api)
- [Agent Workflow](/guide/usage)
- [Development Flow](/guide/development-flow)
- [Japanese release notes](/ja/releases/v0.3.0/)
