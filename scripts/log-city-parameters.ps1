param(
    [string]$BaseUrl = "http://127.0.0.1:32123",
    [int]$DurationSeconds = 300,
    [int]$IntervalSeconds = 5,
    [string]$OutputDir = ".\tmp\parameter-logs",
    [switch]$IncludeProblems
)

$ErrorActionPreference = "Stop"

if ($IntervalSeconds -lt 1) {
    throw "IntervalSeconds must be at least 1."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$jsonlPath = Join-Path $OutputDir "city-parameters-$stamp.jsonl"
$csvPath = Join-Path $OutputDir "city-parameters-$stamp.csv"
$eventsPath = Join-Path $OutputDir "city-parameter-events-$stamp.log"

function Get-State($path) {
    Invoke-RestMethod -Uri "$BaseUrl$path" -TimeoutSec 10
}

function Get-TaxMap($economy) {
    $map = [ordered]@{}
    foreach ($rate in $economy.aggregateTaxRates) {
        $key = "$($rate.service).$($rate.subService)"
        $map[$key] = [int]$rate.rate
    }
    return $map
}

function Add-EventLine($message) {
    $line = "[{0}] {1}" -f (Get-Date -Format "s"), $message
    Add-Content -LiteralPath $eventsPath -Value $line -Encoding UTF8
    Write-Host $line
}

$health = Get-State "/health"
if (-not $health.ok) {
    throw "Bridge health check failed."
}

$startedAt = Get-Date
$deadline = $startedAt.AddSeconds($DurationSeconds)
$previousTaxMap = $null
$sample = 0

"sample,wallTime,gameTime,buildIndex,paused,selectedSpeed,finalSpeed,citizens,residentialDemand,commercialDemand,workplaceDemand,problemTotal,problemCounts,taxResidentialLow,taxResidentialHigh,taxCommercialLow,taxCommercialHigh,taxIndustrialGeneric,taxOfficeGeneric" |
    Set-Content -LiteralPath $csvPath -Encoding UTF8

Add-EventLine "Logging started. jsonl=$jsonlPath csv=$csvPath"

while ((Get-Date) -lt $deadline) {
    $wallTime = Get-Date
    $summary = Get-State "/state/summary"
    $demand = Get-State "/state/demand"
    $economy = Get-State "/state/economy"
    $problems = if ($IncludeProblems) { Get-State "/state/problems?limit=300" } else { Get-State "/state/problems?limit=80" }

    $taxMap = Get-TaxMap $economy
    if ($previousTaxMap -ne $null) {
        foreach ($key in $taxMap.Keys) {
            if ($previousTaxMap.Contains($key) -and $previousTaxMap[$key] -ne $taxMap[$key]) {
                Add-EventLine ("TAX_CHANGE {0}: {1} -> {2} at gameTime={3} buildIndex={4}" -f $key, $previousTaxMap[$key], $taxMap[$key], $summary.gameTime, $summary.buildIndex)
            }
        }
    }
    $previousTaxMap = $taxMap

    $record = [ordered]@{
        sample = $sample
        wallTime = $wallTime.ToString("o")
        summary = $summary
        demand = $demand
        aggregateTaxRates = $economy.aggregateTaxRates
        taxRates = $economy.taxRates
        problems = $problems
    }

    ($record | ConvertTo-Json -Depth 12 -Compress) | Add-Content -LiteralPath $jsonlPath -Encoding UTF8

    $problemCounts = if ($problems.countsByProblem) {
        (($problems.countsByProblem.PSObject.Properties | ForEach-Object { "$($_.Name):$($_.Value)" }) -join "|")
    } else {
        ""
    }

    $row = [pscustomobject]@{
        sample = $sample
        wallTime = $wallTime.ToString("s")
        gameTime = $summary.gameTime
        buildIndex = $summary.buildIndex
        paused = $summary.simulation.paused
        selectedSpeed = $summary.simulation.selectedSpeed
        finalSpeed = $summary.simulation.finalSpeed
        citizens = $summary.citizens.count
        residentialDemand = $demand.residential
        commercialDemand = $demand.commercial
        workplaceDemand = $demand.workplace
        problemTotal = $problems.total
        problemCounts = $problemCounts
        taxResidentialLow = $taxMap["Residential.ResidentialLow"]
        taxResidentialHigh = $taxMap["Residential.ResidentialHigh"]
        taxCommercialLow = $taxMap["Commercial.CommercialLow"]
        taxCommercialHigh = $taxMap["Commercial.CommercialHigh"]
        taxIndustrialGeneric = $taxMap["Industrial.IndustrialGeneric"]
        taxOfficeGeneric = $taxMap["Office.OfficeGeneric"]
    }

    $row | ConvertTo-Csv -NoTypeInformation | Select-Object -Skip 1 | Add-Content -LiteralPath $csvPath -Encoding UTF8

    $sample++
    Start-Sleep -Seconds $IntervalSeconds
}

Add-EventLine "Logging finished. samples=$sample"

[pscustomobject]@{
    ok = $true
    samples = $sample
    jsonlPath = (Resolve-Path -LiteralPath $jsonlPath).Path
    csvPath = (Resolve-Path -LiteralPath $csvPath).Path
    eventsPath = (Resolve-Path -LiteralPath $eventsPath).Path
}
