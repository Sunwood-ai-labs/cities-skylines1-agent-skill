# City Dashboard

The dashboard is read-only. Run `node dashboard/server.js --relay` on the remote
host and `node dashboard/server.js --push` on the Windows game machine. Both use
the same `CITY_PUSH_TOKEN`. The local pusher reads a consolidated game snapshot
every 3 seconds by default; facilities, zoning and problem details are refreshed
every 30 seconds. The page also polls the relay every 3 seconds by default.

The relay stores one latest full snapshot and compact samples for the last hour
(at most 1200 samples) in `CITY_DASHBOARD_DATA` (default:
`dashboard/history.json`). The file is capped at 512 KiB and expired samples are
pruned every minute even if the local game stops pushing.

The bridge exposes population, demand, zones, facilities, tax rates, problems,
simulation state, treasury balance, cash change, and budget-window income and
expenses. The dashboard also shows actual growable building counts for
residential, commercial, and industrial services (separate from zoned land
cells). Historical charts show population, treasury balance, income, expenses,
net budget and demand.
