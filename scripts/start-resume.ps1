param(
    [int]$ApiPort = 32123,
    [string]$SteamAppId = "255710",
    [int]$LauncherTimeoutSeconds = 90,
    [int]$GameLoadTimeoutSeconds = 300,
    [switch]$SkipKill,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host "[$(Get-Date -Format HH:mm:ss)] $message"
}

function Ensure-Win32Input {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public struct SAB_RECT {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public static class SAB_ResumeWin32Input {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out SAB_RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
'@ -ErrorAction SilentlyContinue
}

function Click-Screen([int]$x, [int]$y) {
    [SAB_ResumeWin32Input]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 120
    [SAB_ResumeWin32Input]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 90
    [SAB_ResumeWin32Input]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

function Click-Relative($process, [double]$rx, [double]$ry) {
    $rect = New-Object SAB_RECT
    [SAB_ResumeWin32Input]::GetWindowRect($process.MainWindowHandle, [ref]$rect) | Out-Null
    [SAB_ResumeWin32Input]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 400
    $x = $rect.Left + [int](($rect.Right - $rect.Left) * $rx)
    $y = $rect.Top + [int](($rect.Bottom - $rect.Top) * $ry)
    Click-Screen $x $y
}

function Wait-ProcessWindow([string]$name, [string]$title, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        $process = Get-Process -ErrorAction SilentlyContinue |
            Where-Object { $_.ProcessName -eq $name -and $_.MainWindowHandle -ne 0 -and ($title -eq "" -or $_.MainWindowTitle -eq $title) } |
            Select-Object -First 1

        if ($process) {
            return $process
        }

        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for window: process=$name title=$title"
}

function Test-ApiPort([int]$port) {
    $client = New-Object Net.Sockets.TcpClient
    try {
        $iar = $client.BeginConnect("127.0.0.1", $port, $null, $null)
        if ($iar.AsyncWaitHandle.WaitOne(500, $false)) {
            $client.EndConnect($iar)
            return $true
        }
        return $false
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

function Wait-AgentBridge([int]$port, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    do {
        if (Test-ApiPort $port) {
            try {
                return Invoke-RestMethod "http://127.0.0.1:$port/health"
            }
            catch {
                Start-Sleep -Seconds 1
            }
        }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for Skylines Agent Bridge on port $port"
}

function Disable-InGameAutoSave([int]$port) {
    try {
        $settings = Invoke-RestMethod "http://127.0.0.1:$port/state/game-settings"
        if ($settings.autoSave -eq $true) {
            Write-Step "Disabling in-game autosave to avoid AutoSave.crp sharing violations"
            $body = @{ enabled = $false } | ConvertTo-Json
            Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:$port/commands/set-autosave" -Body $body -ContentType "application/json" | Out-Null
        }
        else {
            Write-Step "In-game autosave is already disabled"
        }
    }
    catch {
        Write-Step "Could not verify autosave setting through the bridge: $($_.Exception.Message)"
    }
}

Ensure-Win32Input

if (-not $SkipBuild) {
    Write-Step "Building and installing SkylinesAgentBridge.dll"
    & (Join-Path $PSScriptRoot "build.ps1")
}

$saveDir = Join-Path $env:LOCALAPPDATA "Colossal Order\Cities_Skylines\Saves"
$latestSave = Get-ChildItem -LiteralPath $saveDir -Filter *.crp -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($latestSave) {
    Write-Step "Latest local save before Resume: $($latestSave.Name) $($latestSave.LastWriteTime)"
}

if (-not $SkipKill) {
    Write-Step "Stopping existing Cities.exe"
    Get-Process Cities -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

Write-Step "Launching Cities: Skylines through Steam"
Start-Process "steam://rungameid/$SteamAppId"

Write-Step "Waiting for Paradox Launcher"
$launcher = Wait-ProcessWindow "Paradox Launcher" "Cities: Skylines" $LauncherTimeoutSeconds

Write-Step "Clicking launcher Resume button"
Click-Relative $launcher 0.53 0.39

$cities = $null
for ($attempt = 1; $attempt -le 3 -and -not $cities; $attempt++) {
    Write-Step "Waiting for Cities.exe (attempt $attempt/3)"
    try {
        $cities = Wait-ProcessWindow "Cities" "Cities: Skylines" 45
    }
    catch {
        Write-Step "Cities.exe did not appear yet; clicking launcher Resume again"
        $launcher = Wait-ProcessWindow "Paradox Launcher" "Cities: Skylines" $LauncherTimeoutSeconds
        Click-Relative $launcher 0.53 0.39
    }
}
if (-not $cities) {
    throw "Timed out waiting for Cities.exe"
}

Write-Step "Waiting for Agent Bridge API"
$health = Wait-AgentBridge $ApiPort $GameLoadTimeoutSeconds
Disable-InGameAutoSave $ApiPort

Write-Step "Ready"
$health | ConvertTo-Json -Depth 8
