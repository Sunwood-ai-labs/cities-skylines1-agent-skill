# Troubleshooting

## The Build Script Cannot Find CS1

Edit the `$game` path in `scripts/build.ps1` so it points to your Cities: Skylines install directory. The script checks for `Cities_Data\Managed\ICities.dll` before compiling.

## The API Does Not Respond

Check these in order:

1. The mod is enabled in the CS1 content manager.
2. A city is loaded.
3. Port `32123` is not already in use.
4. `scripts/start-resume.ps1` or `scripts/start-new-map.ps1` finished with a `/health` response.

## Roads Look Connected but Traffic Fails

CS1 network crossings are not intersections unless a real node exists. Use:

```powershell
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
```

Then remove the bad segment with `/commands/bulldoze` and rebuild with endpoints close enough to reuse the intended road nodes.

## Service Buildings Intersect Roads

Use:

```powershell
Invoke-RestMethod http://127.0.0.1:32123/state/building-anomalies?limit=200
```

Move or replace the building with `/commands/move-building` or `/commands/place-building`, then re-check the anomaly endpoint.

## Saves Are Not Visible Yet

`/commands/save` uses CS1's normal save panel path and writes asynchronously. Poll `/state/saves` after requesting a save, or use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\save-city.ps1 -Name AgentAutoSave-clean
```
