# Local city autonomy loop

This is an opt-in Windows supervisor for a running CS1 city. It polls the
localhost Agent Bridge continuously, asks the signed-in Codex CLI for one
structured recommendation at a bounded interval, validates the plan locally,
and executes only a small allow-list. The model runs in Codex's read-only
sandbox and cannot invoke game commands itself. Game mutations go through the
bridge at `127.0.0.1:32123`; nothing opens a public control endpoint.

## Modes and controls

- `observe` (default): monitor and write `autonomy/runtime/status.json` and
  `autonomy/runtime/autonomy.log`; no game changes.
- `assist`: also asks Codex for a structured plan, but never executes it; plans
  are written to the local review file. This is the recommended first test.
- `auto`: permits at most one small action per model cycle. Allowed actions are
  first, one short Basic Road extension from an existing local-road node only
  when it is near a residential gap, has at least 18 m clearance from growable
  building centers, 24 m from service-building centers and 8 m from other road
  segments (after the shared-node junction), and city finances are healthy. The road
  candidate must come from the local geometry validator; the model cannot invent
  endpoints. When no safe road candidate remains, it may paint one small zone
  near existing roads/development if demand is high, lower a tax rate to 8–9
  only when a high-tax alert exists, or restore 3x speed. Occupied zoning is
  always preserved. Meaningful changes are saved to one rolling checkpoint
  named `AgentAutoSave-autonomy-latest`, so the loop does not create an unlimited
  series of multi-megabyte saves. Facilities, utilities, bulldozing, moving
  buildings, and large service/financial decisions are never automated.
  Road collision checks use the live network/building snapshot; the bridge's
  road dry-run itself only validates prefab and length, so endpoint allow-listing
  and local geometry checks are mandatory.
- A critical service outage, repeated garbage failures, low treasury, or a
  severe population collapse writes `needs-human.json` and pauses the game.
  An individually full landfill does not trigger this if garbage alerts are
  clear and the combustion plant is operating.
- Model usage is capped at 24 calls per local day by default; poll interval is
  30 seconds and model interval is at least 60 minutes. These can be tightened
  using the environment variables documented below. At the model cap, polling
  continues but mutations stop.
- Explicit stop: run `scripts/stop-autonomy.ps1` or press Ctrl+C in foreground
  mode. Restart is intentionally explicit: remove `autonomy/runtime/STOP`, then
  start the process again.

## Run

Ensure the game is loaded, the Agent Bridge is healthy, and `codex` is installed
and signed in. Start with observation first:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-autonomy.ps1 -Mode observe
```

Then validate the model connection and plan output without allowing mutations:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-autonomy.ps1 -Mode assist
```

After reviewing the status and logs, explicitly enable the bounded auto mode:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-autonomy.ps1 -Mode auto
```

For unattended start at Windows logon, install the per-user scheduled task
(default mode is `auto`):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\install-autonomy-task.ps1 -Mode auto
```

Stop and uninstall separately:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\stop-autonomy.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\remove-autonomy-task.ps1
```

The task only runs while the user's Windows session is available and the game
and bridge are up. If the game is not loaded, it waits and retries. It uses the
Codex CLI account/model configuration already present on this machine; AI
requests count against that account's usage limits.

## Optional limits

Set process environment variables before launching, or store corresponding
properties in a private `autonomy/config.json` (never commit it):

- `CITY_AUTONOMY_POLL_SECONDS` (default 30; minimum 10)
- `CITY_AUTONOMY_MODEL_INTERVAL_MINUTES` (default 60; minimum 5)
- `CITY_AUTONOMY_MAX_MODEL_CALLS_PER_DAY` (default 24)
- `CITY_AUTONOMY_MAX_MUTATIONS_PER_DAY` (default 6)
- `CITY_AUTONOMY_MIN_BALANCE` (default 30000)
- `CITY_AUTONOMY_CRITICAL_PROBLEMS` (default 5)
- `CITY_AUTONOMY_MODEL` / `CODEX_BIN`

Daily model-call and mutation counters persist across process restarts for the
local calendar day; a restart cannot reset the usage guard. There is no
time-based cooldown between successful city changes; decisions still run on
the model interval and remain bounded by the daily mutation limit.

Tests: `node --test autonomy/runner.test.js`.
