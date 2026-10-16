param([string]$GamePath = $env:CS1_GAME_PATH)
$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$src = Join-Path $repo "src"
$out = Join-Path $repo "bin"
# Game folder: -GamePath, else CS1_GAME_PATH, else the usual Steam library locations.
$game = $GamePath
if (-not $game) {
    foreach ($candidate in @(
        "D:\SteamLibrary\steamapps\common\Cities_Skylines",
        "C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines",
        "C:\Program Files\Steam\steamapps\common\Cities_Skylines")) {
        if (Test-Path (Join-Path $candidate "Cities_Data\Managed\ICities.dll")) { $game = $candidate; break }
    }
}
if (-not $game) {
    throw "Cities: Skylines was not found. Pass -GamePath or set CS1_GAME_PATH."
}
$managed = Join-Path $game "Cities_Data\Managed"
$csc = "$env:WINDIR\Microsoft.NET\Framework\v3.5\csc.exe"

if (!(Test-Path $csc)) {
    $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (!(Test-Path $csc)) {
    throw "csc.exe was not found. Install .NET Framework developer tools."
}

if (!(Test-Path (Join-Path $managed "ICities.dll"))) {
    throw "Cities: Skylines managed DLLs were not found at $managed"
}

New-Item -ItemType Directory -Force -Path $out | Out-Null

$sources = Get-ChildItem -LiteralPath $src -Filter *.cs | Sort-Object Name | ForEach-Object { $_.FullName }
$target = Join-Path $out "SkylinesAgentBridge.dll"

& $csc `
    /nologo `
    /target:library `
    /out:$target `
    /debug:pdbonly `
    /optimize+ `
    /define:TRACE `
    /noconfig `
    /nostdlib+ `
    /reference:"$managed\mscorlib.dll" `
    /reference:"$managed\System.dll" `
    /reference:"$managed\System.Core.dll" `
    /reference:"$managed\ICities.dll" `
    /reference:"$managed\Assembly-CSharp.dll" `
    /reference:"$managed\Assembly-CSharp-firstpass.dll" `
    /reference:"$managed\ColossalManaged.dll" `
    /reference:"$managed\UnityEngine.dll" `
    $sources

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$modDir = Join-Path $env:LOCALAPPDATA "Colossal Order\Cities_Skylines\Addons\Mods\SkylinesAgentBridge"
New-Item -ItemType Directory -Force -Path $modDir | Out-Null
Copy-Item -LiteralPath $target -Destination (Join-Path $modDir "SkylinesAgentBridge.dll") -Force

Write-Host "Built $target"
Write-Host "Copied to $modDir"
