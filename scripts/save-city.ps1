param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [string]$Name = ("AgentAutoSave-{0}" -f (Get-Date -Format "yyyyMMdd-HHmmss")),
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

if ($Name.Trim() -ieq "AutoSave") {
    throw "Refusing to save as AutoSave.crp. Use a unique Agent* save name to avoid CS1 autosave file sharing violations."
}

try {
    $settings = Invoke-RestMethod "$BaseUrl/state/game-settings"
    if ($settings.autoSave -eq $true) {
        $settingsBody = @{ enabled = $false } | ConvertTo-Json
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/commands/set-autosave" -Body $settingsBody -ContentType "application/json" | Out-Null
    }
}
catch {
    Write-Warning "Could not verify or disable in-game autosave before saving: $($_.Exception.Message)"
}

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
