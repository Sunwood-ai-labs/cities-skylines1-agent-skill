# City Dashboard

The dashboard is read-only. Run `node dashboard/server.js --relay` on the remote
host and `node dashboard/server.js --push` on the Windows game machine. Both use
the same `CITY_PUSH_TOKEN`; the local pusher uses `CITY_PUSH_URL` and polls the
game API every `CITY_PUSH_INTERVAL` seconds (default: 60). The relay stores
only the latest detailed snapshot plus compact chart samples from the last
four hours (at most 240 samples) in `CITY_DASHBOARD_DATA` (default:
`dashboard/history.json`). The file is capped at 256 KiB and old samples are
pruned every minute even if the local game stops pushing.

The current bridge exposes population, demand, zones, facilities, tax rates,
problems and simulation state. It does not expose cash, income, spending,
housing capacity or unemployment; the page intentionally does not invent them.
