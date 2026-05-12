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

function Build-Network($prefab, $x1, $z1, $x2, $z2, $name) {
    Invoke-AgentCommand "/commands/build-road" @{
        dryRun = [bool]$DryRun
        roadPrefab = $prefab
        start = @{ x = $x1; z = $z1 }
        end = @{ x = $x2; z = $z2 }
        name = $name
    } | ConvertTo-Json -Depth 8
}

function Place-Building($prefab, $x, $z, $angle = 0) {
    Invoke-AgentCommand "/commands/place-building" @{
        dryRun = [bool]$DryRun
        buildingPrefab = $prefab
        position = @{ x = $x; z = $z }
        angleDegrees = $angle
    } | ConvertTo-Json -Depth 8
}

Write-Host "Creating connected road and zone district"
if ($DryRun) {
    & (Join-Path $PSScriptRoot "develop-starter-city.ps1") -BaseUrl $BaseUrl -DryRun
}
else {
    & (Join-Path $PSScriptRoot "develop-starter-city.ps1") -BaseUrl $BaseUrl
}

Write-Host "Placing utilities"
Place-Building "Wind Turbine" 120 220 0
Place-Building "Water Tower" 120 (-220) 0
Build-Network "Basic Road" 480 160 600 160 "Agent Sewage Loop South"
Build-Network "Basic Road" 480 160 480 220 "Agent Sewage Loop West"
Build-Network "Basic Road" 480 220 600 220 "Agent Sewage Loop North"
Build-Network "Basic Road" 600 160 600 220 "Agent Sewage Loop East"
Place-Building "Inland Water Treatment Plant 01" 540 260 180

Write-Host "Laying water pipes"
Build-Network "Water Pipe" 120 (-220) 120 200 "Agent Water Trunk"
Build-Network "Water Pipe" 120 (-120) 520 (-120) "Agent Water West-East -120"
Build-Network "Water Pipe" 120 0 520 0 "Agent Water West-East 0"
Build-Network "Water Pipe" 120 120 520 120 "Agent Water West-East 120"
Build-Network "Water Pipe" 320 (-180) 320 180 "Agent Water North-South"
Build-Network "Water Pipe" 480 120 580 120 "Agent Restore Service Pipe A"
Build-Network "Water Pipe" 180 200 580 200 "Agent Service Water North Offset"
Build-Network "Water Pipe" 540 184 540 260 "Agent Sewage Plant Water Safe"
Build-Network "Water Pipe" 360 120 360 220 "Agent Boiler Water South Safe"

Write-Host "Laying power line"
Build-Network "Power Line" 120 220 240 160 "Agent Power Tie 1"
Build-Network "Power Line" 240 160 400 160 "Agent Power Tie 2"
Build-Network "Power Line" 400 160 400 80 "Agent Power Loop 1"
Build-Network "Power Line" 400 80 240 80 "Agent Power Loop 2"

Write-Host "Placing basic city services"
Place-Building "711884134.Koban Police Box_Data" 200 200 180
Place-Building "1691800474.Block Services - Fire_Data" 260 200 180
Place-Building "1691802727.Block Services - Health_Data" 340 200 180
Place-Building "1692831837.Block Services - Elementary_Data" 440 200 180
Place-Building "1691797536.Block Services - Death_Data" 500 200 180
Place-Building "1691801543.Block Services - Garbage_Data" 540 200 180
Place-Building "1691801543.Block Services - Garbage_Data" 500 260 180
Place-Building "Boiler Station" 360 220 180
Build-Network "Heating Pipe" 120 (-220) 120 200 "Agent Heating Trunk"
Build-Network "Heating Pipe" 120 (-120) 520 (-120) "Agent Heating West-East -120"
Build-Network "Heating Pipe" 120 0 520 0 "Agent Heating West-East 0"
Build-Network "Heating Pipe" 120 120 520 120 "Agent Heating West-East 120"
Build-Network "Heating Pipe" 180 200 580 200 "Agent Service Heating North Offset"
Build-Network "Heating Pipe" 360 120 360 220 "Agent Boiler Heating South Safe"
Build-Network "Heating Pipe" 320 (-180) 320 180 "Agent Heating North-South"
Build-Network "Power Line" 400 160 580 200 "Agent Service Power North Offset"

Write-Host "Adding extra residential capacity"
Invoke-AgentCommand "/commands/set-zone" @{
    dryRun = [bool]$DryRun
    zone = "ResidentialLow"
    center = @{ x = 180; z = -40 }
    radius = 60
} | ConvertTo-Json -Depth 8
Invoke-AgentCommand "/commands/set-zone" @{
    dryRun = [bool]$DryRun
    zone = "ResidentialLow"
    center = @{ x = 260; z = -40 }
    radius = 60
} | ConvertTo-Json -Depth 8
Invoke-AgentCommand "/commands/set-zone" @{
    dryRun = [bool]$DryRun
    zone = "ResidentialLow"
    center = @{ x = 260; z = 120 }
    radius = 60
} | ConvertTo-Json -Depth 8

Write-Host "Letting the city run"
Invoke-AgentCommand "/commands/set-simulation-speed" @{
    paused = $false
    speed = 3
} | ConvertTo-Json -Depth 8
Start-Sleep -Seconds 5

Write-Host "Summary"
Invoke-RestMethod "$BaseUrl/state/summary" | ConvertTo-Json -Depth 8

Write-Host "Problems"
Invoke-RestMethod "$BaseUrl/state/problems?limit=100" | ConvertTo-Json -Depth 10

Write-Host "Facilities"
Invoke-RestMethod "$BaseUrl/state/facilities?limit=300" | ConvertTo-Json -Depth 10

Write-Host "Saving city"
$saveName = "AgentAutoSave-{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss")
Invoke-AgentCommand "/commands/save" @{
    name = $saveName
} | ConvertTo-Json -Depth 8
