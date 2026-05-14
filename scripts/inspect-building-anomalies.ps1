param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [int]$Limit = 200,
    [double]$RoadClearance = 0,
    [switch]$IncludeOriginal
)

$ErrorActionPreference = "Stop"

$uri = "{0}/state/building-anomalies?limit={1}&roadClearance={2}&includeOriginal={3}" -f $BaseUrl, $Limit, $RoadClearance, ([string][bool]$IncludeOriginal).ToLowerInvariant()
$result = Invoke-RestMethod $uri

Write-Host ("Building anomalies: {0} total, {1} returned" -f $result.total, $result.returned)
if ($result.counts) {
    $result.counts.PSObject.Properties | Sort-Object Name | ForEach-Object {
        Write-Host ("  {0}: {1}" -f $_.Name, $_.Value)
    }
}

$result.anomalies |
    Select-Object type, buildingId, segmentId, displayName, service, roadCenterlineDistance, roadClearance, position |
    Format-Table -AutoSize
