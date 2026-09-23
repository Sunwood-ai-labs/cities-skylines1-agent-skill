param(
    [string]$EnvironmentFile = 'C:\Users\hhhh\aimayor\city-dashboard.env'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $EnvironmentFile)) {
    throw "Dashboard environment file missing: $EnvironmentFile"
}
foreach ($line in Get-Content -LiteralPath $EnvironmentFile) {
    if ($line -match '^([A-Z_]+)=(.*)$') {
        [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
    }
}
if (-not $env:CITY_PUSH_TOKEN) { throw 'CITY_PUSH_TOKEN missing' }
$env:CITY_PUSH_URL = 'https://city.whodothis.uk/api/push'
$env:CITY_PUSH_INTERVAL = '60'
$node = (Get-Command node.exe -ErrorAction Stop).Source
& $node (Join-Path $PSScriptRoot 'server.js') --push
