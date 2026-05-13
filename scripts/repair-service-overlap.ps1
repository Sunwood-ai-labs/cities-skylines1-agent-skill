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

function Place-Building($prefab, $x, $z, $angle = 180) {
    Invoke-AgentCommand "/commands/place-building" @{
        dryRun = [bool]$DryRun
        buildingPrefab = $prefab
        position = @{ x = $x; z = $z }
        angleDegrees = $angle
    } | ConvertTo-Json -Depth 8
}

function Bulldoze-Building($id) {
    Invoke-AgentCommand "/commands/bulldoze" @{
        dryRun = [bool]$DryRun
        entityType = "building"
        id = $id
    } | ConvertTo-Json -Depth 8
}

$relocatedPrefabs = @(
    "Inland Water Treatment Plant 01",
    "Boiler Station",
    "Landfill Site",
    "Medical Clinic",
    "Fire House",
    "Elementary School",
    "Cemetery",
    "711884134.Koban Police Box_Data",
    "Water Tower"
)

Write-Host "Finding old service buildings that overlap or sit too close to roads"
$facilities = Invoke-RestMethod "$BaseUrl/state/facilities?limit=500"
foreach ($facility in $facilities.facilities) {
    if ($relocatedPrefabs -contains $facility.prefab) {
        $x = [double]$facility.position.x
        $z = [double]$facility.position.z
        if ($x -ge 180 -and $x -le 580 -and $z -ge 120 -and $z -le 190) {
            Bulldoze-Building $facility.id
            Start-Sleep -Milliseconds 150
        }
    }
}

Write-Host "Adding non-overlap utility pipes along the north side of the service road"
Build-Network "Basic Road" 580 190 580 220 "Agent Sewage Plant Connector North"
Build-Network "Basic Road" 480 220 600 220 "Agent Sewage Plant Frontage Safe"
Build-Network "Water Pipe" 180 184 580 184 "Agent Service Water North Offset"
Build-Network "Water Pipe" 540 184 540 260 "Agent Sewage Plant Water Safe"
Build-Network "Water Pipe" 360 120 360 144 "Agent Boiler Water South Safe"
Build-Network "Heating Pipe" 180 184 580 184 "Agent Service Heating North Offset"
Build-Network "Heating Pipe" 360 120 360 144 "Agent Boiler Heating South Safe"
Build-Network "Power Line" 400 160 580 184 "Agent Service Power North Offset"

Write-Host "Rebuilding services off the road centerlines"
Place-Building "711884134.Koban Police Box_Data" 200 184 180
Place-Building "Fire House" 260 184 180
Place-Building "Medical Clinic" 340 184 180
Place-Building "Elementary School" 440 184 180
Place-Building "Cemetery" 500 184 180
Place-Building "Landfill Site" 540 184 180
Place-Building "Landfill Site" 500 244 180
Place-Building "Inland Water Treatment Plant 01" 540 260 180
Place-Building "Boiler Station" 360 128 0

Write-Host "Letting the simulation settle"
Invoke-AgentCommand "/commands/set-simulation-speed" @{
    paused = $false
    speed = 3
} | ConvertTo-Json -Depth 8
Start-Sleep -Seconds 20

Write-Host "Problems"
Invoke-RestMethod "$BaseUrl/state/problems?limit=100" | ConvertTo-Json -Depth 10

Write-Host "Facilities"
Invoke-RestMethod "$BaseUrl/state/facilities?limit=300" | ConvertTo-Json -Depth 10

Write-Host "Saving city"
$saveName = "AgentAutoSave-overlap-fixed-{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss")
Invoke-AgentCommand "/commands/save" @{
    name = $saveName
} | ConvertTo-Json -Depth 8
