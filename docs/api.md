# API Reference

[日本語版](ja/api.md)

Base URL:

```text
http://127.0.0.1:32123
```

While a city is loaded, every API request that touches game state also appears
in the CS1 UI as a short overlay notification. The overlay keeps the latest few
messages for several seconds, for example `API OK: Read city problems` or
`API OK: Build network Basic Road`. `/health` is intentionally excluded because
it can be called before a level exists.

![API notification overlay](assets/api-notification.jpg)

## Agent Workflow

The intended workflow is intentionally generic:

1. Read state from the API.
2. Decide which small change is needed.
3. Call one command API.
4. Let the simulation settle.
5. Re-read state.
6. Save and verify the save file.

![Agent-built starter city](assets/city-overview.jpg)

## GET /health

Returns bridge status without requiring a loaded city.

## GET /state/summary

Returns a small city snapshot: game time, build index, network counts, citizen count, and demand values.

## GET /prefabs/roads

Returns loaded `NetInfo` prefabs that look like roads.

## GET /prefabs/networks

Returns loaded network prefabs. Optional service filter:

```http
GET /prefabs/networks?service=Water
```

## GET /prefabs/buildings

Returns loaded building prefabs. Optional service filter:

```http
GET /prefabs/buildings?service=Electricity
```

## GET /state/problems

Returns in-game notification/problem icons from CS1 data, without using screenshots or computer vision.

```http
GET /state/problems?limit=200
```

The response includes both the legacy combined `problems` string and
structured `problemNames`, `problem1Raw`, `problem2Raw`, and
`countsByProblem` fields so agents can match individual alerts such as
`TaxesTooHigh` even when CS1 marks the same building as major or fatal.
Building rows also surface alert-like flags such as `Abandoned`, `BurnedDown`,
`Collapsed`, `Flooded`, and `RoadAccessFailed`.

Scanned entity types:

- `building`
- `netNode`
- `netSegment`

## GET /state/economy

Returns the currently configured tax rates for zoned residential, commercial,
industrial, and office sub-services across levels.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/economy
```

## GET /state/facilities

Returns current buildings grouped by CS1 service, with optional service
filtering. This is the API-friendly replacement for reading service icons from
the screen. Facility items include prefab footprint and rotation so an agent can
avoid placing large buildings through roads.

By default this excludes internal pipe helper buildings such as `Water Pipe
Junction` and `Heating Pipe Junction`; pass `includeMapObjects=true` when an
agent specifically needs raw map objects.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?limit=500
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?service=HealthCare
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?service=PoliceDepartment
Invoke-RestMethod http://127.0.0.1:32123/state/facilities?includeMapObjects=true
```

The response includes:

- `countsByService`
- `countsBySubService`
- `facilities[]` with `id`, `prefab`, `displayName`, `service`, `subService`, `level`, `width`, `length`, `angleDegrees`, `problems`, and `position`

Response shape:

```json
{
  "ok": true,
  "total": 17,
  "returned": 17,
  "countsByService": {
    "Water": 14,
    "HealthCare": 3
  },
  "facilities": [
    {
      "id": 123,
      "prefab": "Inland Water Treatment Plant 01",
      "service": "Water",
      "width": 5,
      "length": 7,
      "angleDegrees": 180,
      "problems": "",
      "position": { "x": 10, "y": 0, "z": 20 }
    }
  ]
}
```

## GET /state/networks

Returns current network segments from CS1 data, without screenshots. Optional
service filtering is useful for checking roads, water pipes, heating pipes, and
power lines separately.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/networks?service=Road
Invoke-RestMethod http://127.0.0.1:32123/state/networks?service=Water
Invoke-RestMethod http://127.0.0.1:32123/state/networks?limit=1000
```

Each segment includes `id`, `prefab`, `service`, `subService`, `problems`,
`startNodeId`, `endNodeId`, `start`, `end`, and `middle`.

## GET /state/road-anomalies

Detects road geometry that can look connected on screen but is not actually a
proper CS1 road graph connection.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=true"
```

Detected anomaly types:

- `deadEndNearRoad`: a one-segment road endpoint is very close to another road segment, which often means the endpoint visually touches a road but did not create an intersection.
- `deadEndRoad`: a normal road dead end. This is legal in CS1, but useful for agent-side design QA because unwanted frontage/service-road stubs often look like this.
- `shortRoadStub`: a short road segment with a dead end, often left behind by failed frontage-road or service-road placement.

Each anomaly includes the affected node or segment IDs plus world coordinates,
so an agent can call `/commands/bulldoze` or add a connector road without using
image recognition.

Helper scripts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\inspect-road-anomalies.ps1

# Remove suspicious short/dead-end service roads only in a bounded area.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\repair-road-anomalies.ps1 `
  -MinX 450 -MaxX 620 -MinZ 90 -MaxZ 250
```

## GET /state/building-anomalies

Detects service buildings whose footprint intersects a road segment. This is
for API-side QA when a building appears to be placed through a road, without
using screenshots.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/building-anomalies?limit=200
```

## POST /commands/build-network

Generic network creation. Use this for roads, water pipes, heating pipes, and
power lines. `roadPrefab` is kept as the request field name for compatibility
with the early bridge prototype; pass any loaded `NetInfo` prefab name.

Request:

```json
{
  "dryRun": true,
  "roadPrefab": "Basic Road",
  "start": { "x": 0, "z": 0 },
  "end": { "x": 80, "z": 0 },
  "name": "Agent Test Road"
}
```

Response:

```json
{
  "ok": true,
  "dryRun": true,
  "message": "Build-road validation passed."
}
```

When `dryRun` is `false`, the mod creates two nodes and one segment with `NetManager`.

## POST /commands/build-road

Compatibility alias for `/commands/build-network`.

## POST /commands/set-zone

Request:

```json
{
  "dryRun": true,
  "zone": "ResidentialLow",
  "center": { "x": 40, "z": 0 },
  "radius": 32
}
```

Supported zones:

- `Unzoned`
- `ResidentialLow`
- `ResidentialHigh`
- `CommercialLow`
- `CommercialHigh`
- `Industrial`
- `Office`

The command paints existing zone blocks near `center`. It works best after roads have created zoning blocks.

## POST /commands/place-building

Request:

```json
{
  "dryRun": true,
  "buildingPrefab": "Wind Turbine",
  "position": { "x": 300, "z": 200 },
  "angleDegrees": 0
}
```

## POST /commands/move-building

Recreates an existing building at a new position with the same prefab and
deletes the old building. This is intentionally separate from detection and
from save operations so agents can make small, explicit repair steps.

```json
{
  "dryRun": false,
  "id": 123,
  "position": { "x": 340, "z": 200 },
  "angleDegrees": 180
}
```

## POST /commands/bulldoze

Deletes a problem entity by API. Useful for agent-side repair loops after
reading `/state/problems`.

```powershell
$body = @{
  entityType = "netSegment"
  id = 21778
  keepNodes = $false
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/bulldoze -Body $body -ContentType "application/json"
```

Supported `entityType` values:

- `building`
- `netSegment`
- `netNode`

## POST /commands/save

Requests an in-game save through CS1's `SavePanel.SaveGame`, the same code path
used by the normal UI save button. The game writes the `.crp` package
asynchronously, so poll `/state/saves` until the returned file appears.

```powershell
$body = @{ name = "AgentAutoSave-20260512-1900" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/save -Body $body -ContentType "application/json"

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\save-city.ps1 -Name AgentAutoSave-test
```

## GET /state/saves

Lists local `.crp` saves with paths, timestamps, and file sizes.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/saves
```

## POST /commands/set-simulation-speed

Request:

```json
{
  "paused": false,
  "speed": 3
}
```

`speed` is clamped to `0..3`.

## POST /commands/set-tax-rate

Sets tax rates for zoned services. Omit `service`, `subService`, or `level` to
apply the rate broadly; pass `dryRun: true` to preview the affected tax rows.

```json
{
  "dryRun": false,
  "service": "Commercial",
  "rate": 9
}
```

Useful services are `Residential`, `Commercial`, `Industrial`, and `Office`.
`rate` must be between `0` and `29`.

## POST /commands/batch

Request:

```json
{
  "dryRun": true,
  "stopOnError": true,
  "commands": [
    {
      "type": "build-road",
      "roadPrefab": "Basic Road",
      "start": { "x": 120, "z": 0 },
      "end": { "x": 200, "z": 0 },
      "name": "Agent Batch Road"
    },
    {
      "type": "set-zone",
      "zone": "ResidentialLow",
      "center": { "x": 160, "z": 0 },
      "radius": 48
    }
  ]
}
```

Supported command types:

- `build-road`
- `set-zone`

If an item does not include `dryRun`, it inherits the batch-level `dryRun` value. Batches are limited to 32 commands.
