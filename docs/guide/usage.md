# Agent Workflow

The bridge is designed for cautious, inspect-first city operations. Agents should avoid large hidden repair routines and instead use small API calls that can be explained after the run.

## Recommended Loop

1. Check bridge health and summary state.
2. Read problems, road anomalies, building anomalies, facilities, and networks.
3. Choose one scoped change.
4. Run a command API, preferably as a dry run when the endpoint supports it.
5. Let the simulation settle.
6. Re-read state and compare results.
7. Save the city and verify the save through `/state/saves`.

## Inspection Commands

```powershell
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary
Invoke-RestMethod "http://127.0.0.1:32123/state/problems?limit=200"
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?limit=500&nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200"
Invoke-RestMethod "http://127.0.0.1:32123/state/facilities?limit=500"
Invoke-RestMethod "http://127.0.0.1:32123/state/networks?limit=1000&service=Road"
```

Use `includeMapObjects=true` on `/state/facilities` only when raw helper buildings, such as pipe or heating junctions, are needed.

## Repair Pattern

- Use `/commands/bulldoze` for a known bad building, node, or segment.
- Use `/commands/build-network` for roads, pipes, heating pipes, and power lines.
- Use `/commands/place-building` or `/commands/move-building` for services.
- Use `/commands/set-zone` after roads have created zone blocks.
- Use `/commands/set-simulation-speed` to let the city settle before judging the result.
- Use `/commands/save` or `scripts/save-city.ps1` after meaningful changes.

## Prompt Template

```text
Use $cities-skylines1-agent-skill to resume my CS1 city, inspect current problems, repair road/infrastructure issues with separate API calls, save the city, and report what changed.
```
