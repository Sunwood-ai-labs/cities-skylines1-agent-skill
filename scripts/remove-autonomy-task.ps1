$ErrorActionPreference = 'Stop'
$taskName = 'CitiesSkylines1AgentAutonomy'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Output "Removed scheduled task '$taskName'. Local autonomy files and logs were preserved."
} else {
    Write-Output "Scheduled task '$taskName' is not installed."
}
