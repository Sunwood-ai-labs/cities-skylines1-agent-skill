param(
    [ValidateSet("overview", "transport", "underground", "metro", "route-map")]
    [string]$Preset = "overview",
    [string]$Name = "",
    [int]$SuperSize = 1,
    [switch]$KeepCamera,
    [string]$BaseUrl = "http://127.0.0.1:32123"
)

$ErrorActionPreference = "Stop"

$body = @{
    preset = $Preset
    superSize = $SuperSize
    setCamera = -not $KeepCamera
}

if ($Name) {
    $body.name = $Name
}

$response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/commands/capture-view" -Body ($body | ConvertTo-Json -Depth 5) -ContentType "application/json"
$response

if ($response.path) {
    Write-Host "Waiting for capture file..."
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 500
        if (Test-Path -LiteralPath $response.path) {
            $file = Get-Item -LiteralPath $response.path
            Write-Host ("Capture ready: {0} ({1} bytes)" -f $file.FullName, $file.Length)
            exit 0
        }
    }

    Write-Warning "Capture was requested, but the file did not appear within 10 seconds. The game may need another rendered frame."
}
