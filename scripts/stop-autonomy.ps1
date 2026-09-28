$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$runtime = Join-Path $repo 'autonomy\runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
Set-Content -LiteralPath (Join-Path $runtime 'STOP') -Value 'Explicit operator stop' -Encoding utf8
Write-Output "Stop signal written. The local autonomy loop will exit on its next poll. Restart requires an explicit operator action and removal of autonomy\runtime\STOP."
