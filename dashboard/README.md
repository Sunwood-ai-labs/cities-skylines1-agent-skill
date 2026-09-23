# City Dashboard

The dashboard is read-only. Run `node dashboard/server.js --relay` on the remote
host and `node dashboard/server.js --push` on the Windows game machine. Both use
the same `CITY_PUSH_TOKEN`; the local pusher uses `CITY_PUSH_URL` and polls the
game API every `CITY_PUSH_INTERVAL` seconds (default: 60). The relay stores up
to 1440 snapshots in `CITY_DASHBOARD_DATA` (default: `dashboard/history.json`).

The current bridge exposes population, demand, zones, facilities, tax rates,
problems and simulation state. It does not expose cash, income, spending,
housing capacity or unemployment; the page intentionally does not invent them.
