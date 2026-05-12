param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Invoke-AgentCommand($path, $body) {
    $json = $body | ConvertTo-Json -Depth 8
    Write-Host "POST $path $json"
    Invoke-RestMethod -Method Post -Uri "$BaseUrl$path" -Body $json -ContentType "application/json"
}

function Build-Road($x1, $z1, $x2, $z2, $name) {
    Invoke-AgentCommand "/commands/build-road" @{
        dryRun = [bool]$DryRun
        roadPrefab = "Basic Road"
        start = @{ x = $x1; z = $z1 }
        end = @{ x = $x2; z = $z2 }
        name = $name
    } | ConvertTo-Json -Depth 8
}

function Set-Zone($zone, $x, $z, $radius) {
    Invoke-AgentCommand "/commands/set-zone" @{
        dryRun = [bool]$DryRun
        zone = $zone
        center = @{ x = $x; z = $z }
        radius = $radius
    } | ConvertTo-Json -Depth 8
}

Write-Host "Health"
Invoke-RestMethod "$BaseUrl/health" | ConvertTo-Json -Depth 8

# A compact connected starter district. Each road is one block segment, so
# the bridge can reuse matching endpoints and create actual intersections.
# Keep the district inland; the eastern shoreline on the default map floods
# low roads around x=640+.
$xs = @(160, 240, 320, 400, 480)
$zs = @((-160), (-80), 0, 80, 160)

foreach ($z in $zs) {
    for ($i = 0; $i -lt ($xs.Count - 1); $i++) {
        Build-Road $xs[$i] $z $xs[$i + 1] $z "Agent Block E-W $($xs[$i]) $z"
        Start-Sleep -Milliseconds 120
    }
}

foreach ($x in $xs) {
    for ($i = 0; $i -lt ($zs.Count - 1); $i++) {
        Build-Road $x $zs[$i] $x $zs[$i + 1] "Agent Block N-S $x $($zs[$i])"
        Start-Sleep -Milliseconds 120
    }
}

Set-Zone "ResidentialLow" 200 (-120) 48
Set-Zone "ResidentialLow" 200 120 48
Set-Zone "ResidentialLow" 320 40 48
Set-Zone "CommercialLow" 400 (-80) 42
Set-Zone "Office" 440 80 42
Set-Zone "Industrial" 480 0 36

Build-Road 400 160 400 300 "Agent Outside Arterial 1"
Build-Road 400 300 422.737 522.957 "Agent Outside Highway Link South"
Build-Road 400 300 423.614 554.945 "Agent Outside Highway Link North"

Write-Host "Summary"
Invoke-RestMethod "$BaseUrl/state/summary" | ConvertTo-Json -Depth 8
