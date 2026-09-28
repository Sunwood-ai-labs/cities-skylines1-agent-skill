param(
    [ValidateSet('observe', 'assist', 'auto')]
    [string]$Mode = 'observe',
    [switch]$Background
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$runtime = Join-Path $repo 'autonomy\runtime'
$stopFile = Join-Path $runtime 'STOP'
if (Test-Path -LiteralPath $stopFile) {
    throw "Autonomy is stopped. Remove $stopFile only after an explicit restart request."
}
if ($Mode -eq 'auto') {
    Write-Warning 'AUTO MODE can change zoning and lower taxes within strict local limits. Roads, buildings, demolition, and service expansion always require human review.'
}
$env:CITY_AUTONOMY_MODE = $Mode
$env:CITY_BRIDGE_URL = 'http://127.0.0.1:32123'
$node = (Get-Command node.exe -ErrorAction Stop).Source
$env:CODEX_BIN = (Get-Command codex.exe -ErrorAction Stop).Source
$entry = Join-Path $repo 'autonomy\runner.js'
if ($Background) {
    Start-Process -FilePath $node -ArgumentList @('"' + $entry + '"') -WorkingDirectory $repo -WindowStyle Hidden
    Write-Output "Autonomy started in background ($Mode mode). Status: $runtime\status.json; stop: scripts\stop-autonomy.ps1"
} else {
    & $node $entry
}
