param(
    [ValidateSet('observe', 'assist', 'auto')]
    [string]$Mode = 'auto'
)

$ErrorActionPreference = 'Stop'
$taskName = 'CitiesSkylines1AgentAutonomy'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    throw "Scheduled task '$taskName' already exists; inspect/remove it manually before replacing."
}
$repo = Split-Path -Parent $PSScriptRoot
$runtime = Join-Path $repo 'autonomy\runtime'
if (Test-Path -LiteralPath (Join-Path $runtime 'STOP')) {
    throw 'Autonomy STOP file is present. An explicit restart is required before installation.'
}
$node = (Get-Command node.exe -ErrorAction Stop).Source
$codex = (Get-Command codex.exe -ErrorAction Stop).Source
$entry = Join-Path $repo 'autonomy\runner.js'
$action = New-ScheduledTaskAction -Execute $node -Argument ('"' + $entry + '" --mode=' + $Mode + ' --codex-bin="' + $codex + '"') -WorkingDirectory $repo
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 5) -ExecutionTimeLimit ([TimeSpan]::Zero)
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Description "Local Cities: Skylines 1 agent monitor ($Mode mode); all game APIs remain bound to localhost." -RunLevel Limited | Out-Null
Start-ScheduledTask -TaskName $taskName
Write-Output "Installed and started '$taskName' for user $env:USERNAME. To stop/uninstall, use scripts\stop-autonomy.ps1 and scripts\remove-autonomy-task.ps1."
