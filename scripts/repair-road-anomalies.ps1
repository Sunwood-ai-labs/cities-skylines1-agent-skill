param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [double]$NearMissDistance = 18,
    [double]$ShortSegmentLength = 32,
    [double]$MinX = -100000,
    [double]$MaxX = 100000,
    [double]$MinZ = -100000,
    [double]$MaxZ = 100000,
    [switch]$IncludeDeadEnds,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Invoke-AgentCommand($path, $body) {
    $json = $body | ConvertTo-Json -Depth 8
    Write-Host "POST $path $json"
    Invoke-RestMethod -Method Post -Uri "$BaseUrl$path" -Body $json -ContentType "application/json"
}

function In-Box($point) {
    if (-not $point) { return $false }
    return ([double]$point.x -ge $MinX -and [double]$point.x -le $MaxX -and [double]$point.z -ge $MinZ -and [double]$point.z -le $MaxZ)
}

$uri = "{0}/state/road-anomalies?limit=500&nearMissDistance={1}&shortSegmentLength={2}&includeDeadEnds={3}" -f $BaseUrl, $NearMissDistance, $ShortSegmentLength, ([string][bool]$IncludeDeadEnds).ToLowerInvariant()
$result = Invoke-RestMethod $uri
$result | ConvertTo-Json -Depth 10

$segmentsToRemove = New-Object 'System.Collections.Generic.HashSet[int]'
foreach ($item in $result.anomalies) {
    if ($item.type -eq "shortRoadStub" -and (In-Box $item.start -or In-Box $item.end)) {
        [void]$segmentsToRemove.Add([int]$item.segmentId)
    }
    elseif ($item.type -eq "deadEndNearRoad" -and (In-Box $item.position)) {
        [void]$segmentsToRemove.Add([int]$item.ownSegmentId)
    }
    elseif ($IncludeDeadEnds -and $item.type -eq "deadEndRoad" -and (In-Box $item.position)) {
        [void]$segmentsToRemove.Add([int]$item.ownSegmentId)
    }
}

foreach ($segmentId in $segmentsToRemove) {
    Invoke-AgentCommand "/commands/bulldoze" @{
        dryRun = [bool]$DryRun
        entityType = "netSegment"
        id = $segmentId
        keepNodes = $false
    } | ConvertTo-Json -Depth 8
    Start-Sleep -Milliseconds 150
}

if ($segmentsToRemove.Count -gt 0 -and -not $DryRun) {
    Invoke-AgentCommand "/commands/save" @{
        name = "AgentAutoSave-road-anomalies-repaired-{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss")
    } | ConvertTo-Json -Depth 8
}
