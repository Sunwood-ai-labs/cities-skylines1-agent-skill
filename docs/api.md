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

## GET /state/demand

Returns the three demand bars shown in the CS1 UI: residential, commercial,
and workplace demand. Values are `0..100`.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/demand
```

## GET /state/chirps

Returns recent Chirper/citizen messages from CS1's message manager, including
sender name, sender id, text, message type, and message metadata when available.
This is useful for reading citizen feedback such as housing demand, tax,
traffic, service, and city satisfaction comments without OCR.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/chirps?limit=50"
```

## GET /state/zones

Returns zoning cell counts and approximate area by zone type. CS1 zoning cells
are reported as 8m x 8m cells, so `areaSquareMeters` is approximate but useful
for comparing residential, commercial, industrial, office, and unzoned area.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/zones
```

## GET /state/growables

Returns existing growable residential, commercial, industrial, and office
buildings with service, sub-service, footprint size, position, active/abandoned
state, and problem flags. Use this before zoning to avoid painting over already
developed blocks.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/growables?limit=500"
```

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

Known broken/blocked building assets are omitted from this list. The current
blocked family is `Block Services - ...`.

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
industrial, and office sub-services across levels. `aggregateTaxRates` mirrors
the six tax sliders shown in the CS1 budget UI.

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
`name`, `startNodeId`, `endNodeId`, `start`, `end`, and `middle`.

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
- `duplicateRoadSegments`: two road segments share the same pair of endpoint nodes, which usually means one should be removed.
- `overlappingRoadSegments`: two road segments run nearly on top of each other at the same height for a meaningful distance, which usually means a duplicate or accidental overlay.
- `roadCrossingWithoutNode`: two road segments cross at nearly the same height without sharing a node, which usually means they visually overlap but are not a real intersection.
- `roadTerrainCliff`: a ground road has a large height mismatch against nearby sampled terrain, which can indicate buried roads or terrain spikes/cliffs caused by bad road placement.
- `roadBelowLocalGrade`: an agent-built ground road sits far below the surrounding local road grade, which often means a sunken or buried road.

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

## GET /state/external-connections

Checks whether the city's local road component is connected to CS1 outside road
nodes. This is useful when a city visually has highways nearby but no outside
cars enter because the local road graph is still separate from the highway
network.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/external-connections?limit=50"
```

The response includes `cityConnectedToOutside`,
`disconnectedLocalRoadComponents`, outside node counts, and sampled road
components.

## GET /state/building-anomalies

Detects user-placed and growable buildings whose footprint intersects a road
segment. This is for API-side QA when a building appears to be placed through a
road, without using screenshots. Pass `roadClearance` to also report buildings
whose footprint is very close to a road centerline, which helps catch road-edge
overlaps that are visually obvious but do not cross the exact centerline.
Original map objects such as rocks and bridge pillars are skipped by default;
pass `includeOriginal=true` when auditing map objects too.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/building-anomalies?limit=200
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200&roadClearance=0"
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200&roadClearance=1&includeOriginal=true"
```

Detected anomaly types:

- `buildingRoadOverlap`: a road centerline intersects the building footprint.
- `buildingRoadTooClose`: the road centerline is within `roadClearance` meters of the building footprint.

## GET /state/zone-anomalies

Detects mottled zoning from CS1 zone blocks without using screenshots. This is
useful when circular or overlapping zone paint leaves residential, commercial,
industrial, office, and unzoned cells mixed inside the same block.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/zone-anomalies?limit=200&includeUnzonedHoles=true"
```

Detected anomaly types:

- `mixedZoneBlock`: one zoning block contains multiple non-empty zone types,
  such as residential cells mixed with commercial or industrial cells.
- `patchyUnzonedHoles`: one zoning block is mostly one zone type but contains
  many unzoned cells, which often means an agent left visible holes after
  repainting.

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
  "preserveOccupied": true,
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
`preserveOccupied` defaults to `true` and skips zone blocks that already contain
residential, commercial, industrial, office, service, park, or monument
buildings, so broad zoning commands do not overwrite developed city blocks.

## POST /commands/repair-zones-to-growables

Repairs zone blocks that contain existing growable buildings by aligning
non-empty zoning cells with the nearest residential, commercial, industrial, or
office building. Blocks with ambiguous mixed-use occupancy are skipped.

```json
{
  "dryRun": true
}
```

## POST /commands/repair-zone-clusters

Repairs larger 80m zoning clusters when a whole city block is visually mottled.
The command can fill unzoned holes and, by default, prefers the nearest existing
growable building's zone for occupied blocks so cluster repair does not blindly
convert developed buildings to the cluster's dominant zone.

```json
{
  "dryRun": true,
  "includePatchy": true,
  "fillUnzoned": true,
  "preferGrowableZone": true,
  "gridSize": 80
}
```

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

## POST /commands/set-building-active

Turns an existing building on or off by id.

```json
{
  "id": 123,
  "active": false
}
```

## POST /commands/disable-blocked-assets

Disables known broken assets in CS1's package asset state so the game should not
use them. The current blocked family is `Block Services - ...`.

```powershell
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/disable-blocked-assets
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

## POST /commands/capture-view

Requests a PNG screenshot for quick human review of the loaded city. The
command can move the camera to an API-friendly overview and switch CS1 info
views before asking Unity to write the image. Screenshot writing happens after
the next rendered frame, so the response includes `pending: true` and a `path`
to poll.

Supported `preset` values:

- `overview`: city-wide camera with the normal view.
- `transport`, `route-map`: public transport route info view.
- `underground`, `metro`, `subway`: underground tunnel info view for metro and other below-grade networks.

```powershell
$body = @{ preset = "overview"; superSize = 1 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/capture-view -Body $body -ContentType "application/json"

$body = @{ preset = "transport"; name = "routes.png"; superSize = 2 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/capture-view -Body $body -ContentType "application/json"

$body = @{ preset = "underground"; name = "metro-underground.png" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/capture-view -Body $body -ContentType "application/json"
```

Optional camera overrides include `center`, `zoom`, `height`, `angleX`,
`angleY`, and `setCamera: false` to keep the current camera while still
switching the info view and capturing.

Helper script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\capture-city-view.ps1 -Preset overview
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\capture-city-view.ps1 -Preset transport -SuperSize 2
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\capture-city-view.ps1 -Preset underground -Name metro-underground.png
```

## GET /state/captures

Lists PNG files created by `/commands/capture-view`.

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/captures
```

## POST /commands/restore-ui

Restores the normal CS1 UI after capture, info-view, or camera testing leaves
the HUD hidden. It clears info views, returns to the default tool, re-enables
the main UI view and toolbar, and refreshes the UI.

```powershell
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/restore-ui
```

## GET /state/transport-line-anomalies

Detects broken public transport lines at the line level. This includes
`lineNotConnected` stop nodes, incomplete lines, too few stops, and lines that
target vehicles but have none spawned. Use this before treating a line as
operational; vehicle counts alone can be misleading.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-line-anomalies?limit=200"
```

## GET /state/transport-station-anomalies

Detects passenger station buildings that have no nearby same-type public
transport line stop. This catches stations that are built but not actually
served by a route. `maxStopDistance` defaults to `96` meters.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-station-anomalies?limit=200&maxStopDistance=96"
```

## GET /state/transport-vehicles

Returns live vehicles for all transport lines, or for one line with `lineId`.
Each vehicle includes its line id, path id, position, velocity-derived speed,
target/source buildings, and wait/block counters.

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-vehicles?lineId=29&limit=50"
```

## POST /commands/set-simulation-speed

Request:

```json
{
  "paused": false,
  "speed": 3
}
```

`speed` is clamped to `1..3`. Use `paused: true` to pause the
simulation while keeping the selected speed in a valid UI state.

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
