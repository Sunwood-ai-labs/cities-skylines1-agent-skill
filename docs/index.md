---
layout: home

hero:
  name: Cities: Skylines 1 Agent Skill
  text: API-driven city operations for Codex agents
  tagline: Inspect, repair, build, zone, simulate, and save a Cities: Skylines 1 city through a local bridge instead of screen scraping.
  image:
    src: /agent-bridge-icon.svg
    alt: Cities: Skylines Agent Bridge icon
  actions:
    - theme: brand
      text: Start the Guide
      link: /guide/getting-started
    - theme: alt
      text: API Reference
      link: /api

features:
  - title: State First
    details: Read problems, facilities, road anomalies, networks, saves, and prefabs directly from CS1 data.
  - title: Small Commands
    details: Use explicit build, bulldoze, place, move, zone, speed, batch, and save operations that are easy to audit.
  - title: Agent Ready
    details: The root SKILL.md teaches Codex how to resume a city, inspect it, make scoped repairs, and verify the save.
---

## What This Repository Contains

- A Cities: Skylines 1 mod source tree under `src/`.
- A local HTTP API bridge on `http://127.0.0.1:32123`.
- PowerShell scripts for build, launch, smoke tests, road inspection, repair loops, and saves.
- A Codex skill definition in `SKILL.md` and Skill UI metadata in `agents/openai.yaml`.

## Primary Workflow

1. Build and install the mod.
2. Resume a city through Steam and the Paradox Launcher.
3. Inspect API state before changing anything.
4. Apply one small command at a time.
5. Let the simulation settle, re-check state, then save.

See [Getting Started](/guide/getting-started) for setup and [Agent Workflow](/guide/usage) for the repair loop.
