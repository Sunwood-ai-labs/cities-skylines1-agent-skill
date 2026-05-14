# v0.3.0: Running Cities With API Evidence

![v0.3.0 release header](/release-header-v0.3.0.svg)

`cities-skylines1-agent-skill` v0.3.0 is about making the agent loop more observable. Instead of asking an agent to infer everything from screenshots, the bridge now exposes enough state for the agent to inspect demand, economy, zoning, road connectivity, building placement, and save state before it acts.

## What Changed In The Workflow

The recommended loop is still deliberately small:

1. Read the city state.
2. Choose one scoped repair.
3. Run one command, preferably with `dryRun` when available.
4. Let the simulation settle.
5. Read state again.
6. Save and verify the save.

v0.3.0 gives that loop better inputs. `/state/demand`, `/state/chirps`, `/state/zones`, `/state/growables`, `/state/economy`, `/state/external-connections`, and `/state/zone-anomalies` all reduce the need to guess from screen pixels.

## Roads And Connectivity

Road QA now covers more than obvious dead ends. `/state/road-anomalies` reports duplicate segments, overlapping segments, crossings without an intersection node, short stubs, terrain cliffs, and agent-built roads that sit below the local grade. `/state/external-connections` adds a higher-level check: whether the city road component is actually connected to outside road nodes.

That combination helps an agent distinguish "the road is visible" from "the road graph is usable." It is especially useful when a starter city looks near a highway but no outside traffic reaches it.

## Zoning Without Overwriting The City

The zoning changes aim to protect existing development. `/commands/set-zone` defaults to `preserveOccupied: true`, and repair commands can use growable buildings as evidence for the intended zone. This matters for agent-driven repair because the safest command is often not "paint this whole area again"; it is "change only the cells that conflict with nearby developed buildings."

## Economy And Tax Control

The new economy surface lets agents read aggregate tax sliders and detailed tax rows. `/commands/set-tax-rate` can preview or apply changes across service, sub-service, and level filters. `scripts/log-city-parameters.ps1` can then record demand, economy, summary, and problem snapshots over time so tax experiments leave a machine-readable trail.

## Operator Visibility

The in-game notification overlay is now a persistent API console. It keeps recent calls, timestamps them, and supports clear, minimize, and drag controls. This is small, but important: when an agent mutates a live save, the operator should be able to see what just happened without reading logs outside the game.

## Release Notes And References

- [v0.3.0 Release Notes](/releases/v0.3.0/)
- [API Reference](/api)
- [Agent Workflow](/guide/usage)
- [Development Flow](/guide/development-flow)
