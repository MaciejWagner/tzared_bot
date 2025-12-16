param([int]$Generation = 1)

$resultsDir = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_$Generation\results"
$files = Get-ChildItem $resultsDir -Filter "network_*_trial_*.json" | Sort-Object Name

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   GENERATION $Generation TRAINING SUMMARY" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total trials: $($files.Count)" -ForegroundColor Yellow
Write-Host ""

# Aggregate by network
$networks = @{}
foreach ($f in $files) {
    $json = Get-Content $f.FullName | ConvertFrom-Json
    $netId = [int]($f.Name -replace "network_(\d+)_trial.*",'$1')

    if (-not $networks.ContainsKey($netId)) {
        $networks[$netId] = @{
            Trials = 0
            Actions = @()
            Durations = @()
            Victories = 0
            Defeats = 0
            Timeouts = 0
        }
    }
    $networks[$netId].Trials++
    $networks[$netId].Actions += $json.TotalActions
    $networks[$netId].Durations += $json.ActualDurationSeconds
    if ($json.Outcome -eq "VICTORY") { $networks[$netId].Victories++ }
    elseif ($json.Outcome -eq "DEFEAT") { $networks[$netId].Defeats++ }
    else { $networks[$netId].Timeouts++ }
}

# Calculate summaries
$summary = $networks.GetEnumerator() | ForEach-Object {
    $avgActions = ($_.Value.Actions | Measure-Object -Average).Average
    $avgDuration = ($_.Value.Durations | Measure-Object -Average).Average
    $minActions = ($_.Value.Actions | Measure-Object -Minimum).Minimum
    $maxActions = ($_.Value.Actions | Measure-Object -Maximum).Maximum
    [PSCustomObject]@{
        Net = $_.Key
        Trials = $_.Value.Trials
        AvgActions = [math]::Round($avgActions, 1)
        MinActions = $minActions
        MaxActions = $maxActions
        AvgTime = [math]::Round($avgDuration, 1)
        V = $_.Value.Victories
        D = $_.Value.Defeats
        T = $_.Value.Timeouts
        WinRate = [math]::Round($_.Value.Victories / $_.Value.Trials * 100, 0)
    }
} | Sort-Object AvgActions -Descending

Write-Host "=== ALL NETWORKS (sorted by Avg Actions) ===" -ForegroundColor Green
$summary | Format-Table Net, Trials, AvgActions, MinActions, MaxActions, AvgTime, @{L="V/D/T";E={"$($_.V)/$($_.D)/$($_.T)"}}, @{L="Win%";E={"$($_.WinRate)%"}} -AutoSize

Write-Host ""
Write-Host "=== STATISTICS ===" -ForegroundColor Yellow
$totalVictories = ($summary | Measure-Object -Property V -Sum).Sum
$totalDefeats = ($summary | Measure-Object -Property D -Sum).Sum
$totalTimeouts = ($summary | Measure-Object -Property T -Sum).Sum
$overallAvgActions = [math]::Round(($summary | Measure-Object -Property AvgActions -Average).Average, 1)

Write-Host "Overall Win Rate: $([math]::Round($totalVictories / $files.Count * 100, 1))%"
Write-Host "Total: V=$totalVictories, D=$totalDefeats, T=$totalTimeouts"
Write-Host "Average Actions (all networks): $overallAvgActions"
Write-Host ""

Write-Host "=== TOP 10 NETWORKS ===" -ForegroundColor Green
$summary | Select-Object -First 10 | Format-Table Net, AvgActions, MinActions, MaxActions, @{L="V/D/T";E={"$($_.V)/$($_.D)/$($_.T)"}}, @{L="Win%";E={"$($_.WinRate)%"}} -AutoSize

Write-Host "=== BOTTOM 5 NETWORKS ===" -ForegroundColor Red
$summary | Select-Object -Last 5 | Format-Table Net, AvgActions, MinActions, MaxActions, @{L="V/D/T";E={"$($_.V)/$($_.D)/$($_.T)"}}, @{L="Win%";E={"$($_.WinRate)%"}} -AutoSize
