param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [double]$NearMissDistance = 18,
    [double]$ShortSegmentLength = 32,
    [bool]$IncludeDeadEnds = $true
)

$ErrorActionPreference = "Stop"

$uri = "{0}/state/road-anomalies?limit=500&nearMissDistance={1}&shortSegmentLength={2}&includeDeadEnds={3}" -f $BaseUrl, $NearMissDistance, $ShortSegmentLength, ([string]$IncludeDeadEnds).ToLowerInvariant()
$result = Invoke-RestMethod $uri
$result | ConvertTo-Json -Depth 10

if ($result.total -gt 0) {
    Write-Host ""
    Write-Host "Road anomaly repair hints:"
    foreach ($item in $result.anomalies) {
        if ($item.type -eq "deadEndNearRoad") {
            Write-Host ("- Connect dead-end node {0} to nearby segment {1}; distance {2}m at x={3}, z={4}" -f $item.nodeId, $item.nearestSegmentId, $item.distance, $item.position.x, $item.position.z)
        }
        elseif ($item.type -eq "shortRoadStub") {
            Write-Host ("- Consider bulldozing or extending short road segment {0}; length {1}m" -f $item.segmentId, $item.length)
        }
        elseif ($item.type -eq "deadEndRoad") {
            Write-Host ("- Review dead-end node {0}; segment {1} at x={2}, z={3}" -f $item.nodeId, $item.ownSegmentId, $item.position.x, $item.position.z)
        }
    }
}
