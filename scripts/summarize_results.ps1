# summarize_results.ps1 - Podsumowanie wyników treningu generacji
# Użycie: ./scripts/summarize_results.ps1 -GenerationPath "training/generation_12"

param(
    [Parameter(Mandatory=$true)]
    [string]$GenerationPath
)

$ProjectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"
$FullPath = Join-Path $ProjectRoot $GenerationPath
$ResultsPath = Join-Path $FullPath "results"

if (-not (Test-Path $ResultsPath)) {
    Write-Host "ERROR: Results path not found: $ResultsPath" -ForegroundColor Red
    exit 1
}

# Collect all trial results
$trialFiles = Get-ChildItem "$ResultsPath\*_trial*.json" -ErrorAction SilentlyContinue
if (-not $trialFiles -or $trialFiles.Count -eq 0) {
    Write-Host "ERROR: No trial files found in $ResultsPath" -ForegroundColor Red
    exit 1
}

Write-Host "Processing $($trialFiles.Count) trial files..."

# Group by network
$networkResults = @{}

foreach ($file in $trialFiles) {
    $json = Get-Content $file.FullName | ConvertFrom-Json
    $networkName = $file.BaseName -replace '_trial\d+$', ''

    if (-not $networkResults.ContainsKey($networkName)) {
        $networkResults[$networkName] = @{
            Victories = 0
            Defeats = 0
            Timeouts = 0
            TotalDuration = 0
            TotalActions = 0
            TrialCount = 0
        }
    }

    $nr = $networkResults[$networkName]
    $nr.TrialCount++
    $nr.TotalDuration += $json.ActualDurationSeconds
    $nr.TotalActions += $json.TotalActions

    switch ($json.Outcome) {
        "VICTORY" { $nr.Victories++ }
        "DEFEAT" { $nr.Defeats++ }
        "TIMEOUT" { $nr.Timeouts++ }
    }
}

# Calculate summary for each network
$summary = @()

foreach ($network in $networkResults.Keys | Sort-Object) {
    $nr = $networkResults[$network]
    $avgDur = $nr.TotalDuration / $nr.TrialCount
    $avgAct = $nr.TotalActions / $nr.TrialCount

    # Fitness formula: victories worth most, timeouts worth some, actions as tiebreaker
    $fitness = ($nr.Victories * 100) + ($nr.Timeouts * 30) + ($avgAct / 10)

    $summary += [PSCustomObject]@{
        Network = $network
        V = $nr.Victories
        D = $nr.Defeats
        T = $nr.Timeouts
        Fitness = [math]::Round($fitness, 1)
        AvgDur = [math]::Round($avgDur, 1)
        AvgAct = [math]::Round($avgAct, 1)
    }
}

# Sort by fitness
$summary = $summary | Sort-Object -Property Fitness -Descending

# Display results
Write-Host ""
Write-Host "=== NETWORK RESULTS ===" -ForegroundColor Cyan
$summary | Format-Table -AutoSize

# Overall stats
$totalV = ($summary | Measure-Object -Property V -Sum).Sum
$totalD = ($summary | Measure-Object -Property D -Sum).Sum
$totalT = ($summary | Measure-Object -Property T -Sum).Sum
$totalTrials = $totalV + $totalD + $totalT

Write-Host "=== OVERALL SUMMARY ===" -ForegroundColor Cyan
Write-Host "Networks: $($summary.Count)"
Write-Host "Total Trials: $totalTrials"
Write-Host "VICTORY: $totalV ($([math]::Round($totalV * 100 / $totalTrials, 1))%)" -ForegroundColor Green
Write-Host "DEFEAT:  $totalD ($([math]::Round($totalD * 100 / $totalTrials, 1))%)" -ForegroundColor Red
Write-Host "TIMEOUT: $totalT ($([math]::Round($totalT * 100 / $totalTrials, 1))%)" -ForegroundColor Yellow

# Save summary.json
$summaryFile = Join-Path $ResultsPath "summary.json"
$summary | ConvertTo-Json -Depth 10 | Set-Content $summaryFile

Write-Host ""
Write-Host "Summary saved to: $summaryFile" -ForegroundColor Green
