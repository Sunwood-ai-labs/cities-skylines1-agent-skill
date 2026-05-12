$ErrorActionPreference = "Stop"

$base = "http://127.0.0.1:32123"

Write-Host "Checking $base/health"
Invoke-RestMethod "$base/health" | ConvertTo-Json -Depth 8

Write-Host "Checking $base/state/summary"
Invoke-RestMethod "$base/state/summary" | ConvertTo-Json -Depth 8

Write-Host "Checking $base/state/problems"
Invoke-RestMethod "$base/state/problems?limit=20" | ConvertTo-Json -Depth 10

Write-Host "Checking $base/prefabs/roads"
Invoke-RestMethod "$base/prefabs/roads" | ConvertTo-Json -Depth 8

Write-Host "Checking dry-run road command"
$road = @{
    dryRun = $true
    roadPrefab = "Basic Road"
    start = @{ x = 0; z = 0 }
    end = @{ x = 80; z = 0 }
    name = "Agent Smoke Test Road"
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post -Uri "$base/commands/build-road" -Body $road -ContentType "application/json" | ConvertTo-Json -Depth 8

Write-Host "Checking dry-run batch command"
$batch = @{
    dryRun = $true
    stopOnError = $true
    commands = @(
        @{
            type = "build-road"
            roadPrefab = "Basic Road"
            start = @{ x = 120; z = 0 }
            end = @{ x = 200; z = 0 }
            name = "Agent Batch Smoke Road"
        },
        @{
            type = "set-zone"
            zone = "ResidentialLow"
            center = @{ x = 160; z = 0 }
            radius = 48
        }
    )
} | ConvertTo-Json -Depth 6

Invoke-RestMethod -Method Post -Uri "$base/commands/batch" -Body $batch -ContentType "application/json" | ConvertTo-Json -Depth 10
