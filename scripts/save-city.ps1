param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [string]$Name = ("AgentAutoSave-{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss")),
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

$body = @{ name = $Name } | ConvertTo-Json
$response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/commands/save" -Body $body -ContentType "application/json"
$response | ConvertTo-Json -Depth 8

if (-not $response.path) {
    throw "Save response did not include a path."
}

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    if (Test-Path -LiteralPath $response.path) {
        Get-Item -LiteralPath $response.path | Select-Object FullName,LastWriteTime,Length | ConvertTo-Json -Depth 4
        exit 0
    }

    Start-Sleep -Seconds 3
} while ((Get-Date) -lt $deadline)

throw "Timed out waiting for save file: $($response.path)"
