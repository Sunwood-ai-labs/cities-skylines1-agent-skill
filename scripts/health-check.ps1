param([string]$BaseUrl = "http://127.0.0.1:32123")
# One-screen city health check: every vital sign with OK / WATCH / ACT.
$ErrorActionPreference = "Stop"
$u = $BaseUrl
$rows = New-Object System.Collections.ArrayList
function Add($area, $item, $value, $status, $note) { [void]$rows.Add([pscustomobject]@{ Area = $area; Item = $item; Value = $value; Status = $status; Note = $note }) }
function Level($v, $watch, $act) { if ($v -ge $act) { "ACT" } elseif ($v -ge $watch) { "WATCH" } else { "OK" } }

$s = Invoke-RestMethod "$u/state/summary"
$ut = Invoke-RestMethod "$u/state/utilities"
$bg = Invoke-RestMethod "$u/state/budget?top=1"
$pc = (Invoke-RestMethod "$u/state/problems?limit=6000").countsByProblem
$fac = (Invoke-RestMethod "$u/state/facilities?limit=30000").facilities
$lines = (Invoke-RestMethod "$u/state/transport-lines").lines | ? complete
$ext = Invoke-RestMethod "$u/state/external-connections?limit=3"

# --- game state
$speed = if ($s.simulation.paused) { "paused" } else { "speed $($s.simulation.selectedSpeed)" }
Add "Game" "Simulation" $speed $(if (-not $s.simulation.paused -and $s.simulation.selectedSpeed -gt 1) { "WATCH" } else { "OK" }) "normal speed preferred"
Add "Game" "Population" ("{0:N0}" -f $ut.population) "OK" ""
foreach ($d in @(@("Residential", $s.demand.residential), @("Commercial", $s.demand.commercial), @("Workplace", $s.demand.workplace))) {
    Add "Demand" $d[0] $d[1] (Level ([int]$d[1]) 40 70) $(if ([int]$d[1] -ge 40) { "zone more / densify" } else { "" })
}
# --- utilities
foreach ($n in "electricity", "water", "sewage") {
    $x = $ut.$n; Add "Utilities" $n ("{0:N0}% of {1:N0} {2}" -f [double]$x.usedPercent, [double]$x.capacity, $x.unit) (Level ([double]$x.usedPercent) 75 85) $(if ([double]$x.usedPercent -ge 75) { "add capacity before expanding" } else { "" })
}
$gp = [double]$ut.garbage.processingUsedPercent
Add "Garbage" "Incineration load" ("{0:N0}%" -f $gp) (Level $gp 85 95) $(if ($gp -ge 85) { "add an incinerator" } else { "" })
$lf = [double]$ut.garbage.landfillUsedPercent
Add "Garbage" "Landfill fill" ("{0:N0}%" -f $lf) (Level $lf 65 80) ""
foreach ($f in $fac | ? { $_.prefab -in "Landfill Site", "Cemetery" }) {
    $st = if ($f.problems -match "LandfillFull") { "ACT" } elseif ($f.problems -match "EmptyingFinished") { "ACT" } elseif ($f.problems -match "Emptying") { "WATCH" } else { "OK" }
    $note = if ($f.problems -match "LandfillFull") { "full: empty it / add another" } elseif ($f.problems -match "EmptyingFinished") { "switch back to normal" } else { "" }
    Add $(if ($f.prefab -eq "Cemetery") { "Deathcare" } else { "Garbage" }) ("{0} #{1}" -f $f.prefab, $f.id) $(if ($f.problems) { $f.problems } else { "normal" }) $st $note
}
$plants = $fac | ? { $_.prefab -match "Power Plant|Dam Power House" }
$nofuel = @($plants | ? { $_.problems -match "NoFuel" }).Count
Add "Utilities" "Power plants" ("{0} ({1} without fuel)" -f @($plants).Count, $nofuel) $(if ($nofuel -gt 0) { "WATCH" } else { "OK" }) ""
# --- money
$net = [double]$bg.totalIncome - [double]$bg.totalExpenses
Add "Budget" "Cash" ("{0:N0}" -f [double]$bg.cash) "OK" ""
Add "Budget" "Week so far (income - services)" ("{0:N0}" -f $net) $(if ($net -lt 0) { "ACT" } else { "OK" }) ""
Add "Budget" "Last cash change" ("{0:N0}" -f [double]$bg.lastCashDelta) $(if ([double]$bg.lastCashDelta -lt 0) { "WATCH" } else { "OK" }) "negative = losing money"
# --- transport and connections
foreach ($l in $lines) { Add "Transport" ("Bus line #{0}" -f $l.number) ("{0}/{1} buses, {2} riders" -f $l.vehicles, $l.targetVehicles, $l.passengers) $(if ($l.vehicles -lt $l.targetVehicles) { "WATCH" } else { "OK" }) "" }
Add "Roads" "Connected to outside" $ext.cityConnectedToOutside $(if ($ext.cityConnectedToOutside -and $ext.disconnectedLocalRoadComponents -eq 0) { "OK" } else { "ACT" }) $(if ($ext.disconnectedLocalRoadComponents -gt 0) { "$($ext.disconnectedLocalRoadComponents) disconnected pieces" } else { "" })
# --- problem markers (watch / act thresholds)
$checks = [ordered]@{ Electricity = @(5, 20); ElectricityNotConnected = @(3, 10); Water = @(5, 20); Sewage = @(5, 20); Garbage = @(5, 20); Death = @(5, 20); Fire = @(1, 4); Crime = @(10, 30); TaxesTooHigh = @(5, 20); Abandoned = @(5, 20); Flooded = @(1, 10); NoWorkers = @(15, 40); NoEducatedWorkers = @(15, 40); LineNotConnected = @(1, 5); Collapsed = @(1, 10) }
foreach ($k in $checks.Keys) { $v = [int]$pc.$k; if ($v -gt 0) { Add "Problems" $k $v (Level $v $checks[$k][0] $checks[$k][1]) "" } }

# --- trends and forecasts: compare with earlier runs (game time), project time to limits
$histPath = Join-Path (Split-Path -Parent $PSScriptRoot) "health-history.json"
$now = [pscustomobject]@{ game = [string]$s.gameTime; population = [double]$ut.population; electricity = [double]$ut.electricity.usedPercent; water = [double]$ut.water.usedPercent; sewage = [double]$ut.sewage.usedPercent; garbage = $gp; landfill = $lf; cash = [double]$bg.cash }
$hist = @(); if (Test-Path $histPath) { try { $hist = @(Get-Content $histPath -Raw | ConvertFrom-Json | % { $_ }) } catch { $hist = @() } }
$base = $hist | ? { $_.game -ne $now.game } | Select -Last 1
if ($hist.Count -gt 0) {
    # use the oldest reading within the history window for a steadier trend
    $base = $hist | Select -First 1
    $days = ([datetime]$now.game - [datetime]$base.game).TotalDays
    if ($days -gt 1) {
        $popRate = ($now.population - $base.population) / $days * 7
        Add "Forecast" "Population trend" ("{0:+#,0;-#,0;0} per game week" -f $popRate) "OK" ("over the last {0:N0} game days" -f $days)
        foreach ($m in @(@("electricity", 85), @("water", 85), @("sewage", 85), @("garbage", 95), @("landfill", 80))) {
            $rate = ($now.($m[0]) - [double]$base.($m[0])) / $days
            if ($rate -le 0.01) { continue }
            $left = ($m[1] - $now.($m[0])) / $rate
            if ($left -le 0) { continue }
            $weeks = $left / 7
            Add "Forecast" ("{0} reaches {1}%" -f $m[0], $m[1]) ("in ~{0:N1} game weeks" -f $weeks) $(if ($weeks -lt 4) { "ACT" } elseif ($weeks -lt 12) { "WATCH" } else { "OK" }) ("rising {0:N2} pts/day" -f $rate)
        }
        $cashRate = ($now.cash - $base.cash) / $days * 7
        Add "Forecast" "Cash trend" ("{0:+#,0;-#,0;0} per game week" -f $cashRate) $(if ($cashRate -lt 0) { "WATCH" } else { "OK" }) ""
    }
}
$hist = @($hist) + $now; if ($hist.Count -gt 30) { $hist = $hist[($hist.Count - 30)..($hist.Count - 1)] }
$hist | ConvertTo-Json | Set-Content $histPath -Encoding utf8

$order = @{ ACT = 0; WATCH = 1; OK = 2 }
$rows | Sort-Object { $order[$_.Status] }, Area | Format-Table Status, Area, Item, Value, Note -AutoSize | Out-String -Width 200
$a = @($rows | ? Status -eq "ACT").Count; $w = @($rows | ? Status -eq "WATCH").Count
"SUMMARY: {0} need action, {1} to watch, {2} OK" -f $a, $w, @($rows | ? Status -eq "OK").Count
