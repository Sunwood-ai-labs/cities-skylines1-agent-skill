$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$src = Join-Path $repo "src"
$out = Join-Path $repo "bin"
$game = "D:\SteamLibrary\steamapps\common\Cities_Skylines"
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
