# Run multi-trial training for all networks
# Each network runs multiple trials to get statistically significant results
# Usage: run_multi_trial_training.ps1 -TrialsPerNetwork 30 -DurationSeconds 60

param(
    [int]$StartId = 0,
    [int]$EndId = 19,
    [int]$TrialsPerNetwork = 30,
    [int]$DurationSeconds = 60
)

$VMName = "DEV"
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm"
$localResultsDir = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_0\multi_trial_$timestamp"
New-Item -ItemType Directory -Force -Path $localResultsDir | Out-Null

$totalNetworks = $EndId - $StartId + 1
$totalTrials = $totalNetworks * $TrialsPerNetwork
$estimatedMinutes = [math]::Round(($totalTrials * ($DurationSeconds + 20)) / 60, 0)

Write-Host "=== Multi-Trial Training ===" -ForegroundColor Cyan
Write-Host "Networks: $StartId to $EndId ($totalNetworks networks)"
Write-Host "Trials per network: $TrialsPerNetwork"
Write-Host "Duration per trial: ${DurationSeconds}s"
Write-Host "Total trials: $totalTrials"
Write-Host "Estimated time: ~${estimatedMinutes} minutes"
Write-Host "Results directory: $localResultsDir"
Write-Host ""

$allResults = @()
$networkSummaries = @()
$startTime = Get-Date
$trialCount = 0

for ($networkId = $StartId; $networkId -le $EndId; $networkId++) {
    $networkResults = @()
    $modelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $networkId

    Write-Host ""
    Write-Host "=== Network $("{0:D2}" -f $networkId) ($TrialsPerNetwork trials) ===" -ForegroundColor Yellow

    for ($trial = 1; $trial -le $TrialsPerNetwork; $trial++) {
        $trialCount++
        $outputPath = "C:\TzarBot\Results\network_{0:D2}_trial_{1:D2}.json" -f $networkId, $trial

        $elapsed = (Get-Date) - $startTime
        $avgPerTrial = if ($trialCount -gt 1) { $elapsed.TotalSeconds / ($trialCount - 1) } else { 80 }
        $remaining = [math]::Round((($totalTrials - $trialCount + 1) * $avgPerTrial) / 60, 1)

        Write-Host "  [$trialCount/$totalTrials] Network $("{0:D2}" -f $networkId) Trial $trial/$TrialsPerNetwork (ETA: ${remaining}min)..." -ForegroundColor Gray -NoNewline

        $trialStart = Get-Date

        try {
            $output = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
                param($modelPath, $mapPath, $duration, $outputPath)
                Set-Location "C:\TzarBot\TrainingRunner"
                & .\TrainingRunner.exe $modelPath $mapPath $duration $outputPath 2>&1
                $LASTEXITCODE
            } -ArgumentList $modelPath, "C:\TzarBot\Maps\training-0.tzared", $DurationSeconds, $outputPath -ErrorAction Stop

            # Copy result
            $session = New-PSSession -VMName $VMName -Credential $cred
            $localFile = "network_{0:D2}_trial_{1:D2}.json" -f $networkId, $trial

            $exists = Invoke-Command -Session $session -ScriptBlock { param($p); Test-Path $p } -ArgumentList $outputPath
            if ($exists) {
                Copy-Item -FromSession $session -Path $outputPath -Destination "$localResultsDir\$localFile" -Force

                $json = Get-Content "$localResultsDir\$localFile" | ConvertFrom-Json
                $result = [PSCustomObject]@{
                    NetworkId = $networkId
                    Trial = $trial
                    Outcome = $json.Outcome
                    Duration = [math]::Round($json.ActualDurationSeconds, 2)
                    Actions = $json.TotalActions
                    APS = [math]::Round($json.ActionsPerSecond, 3)
                    InferenceMs = [math]::Round($json.AverageInferenceMs, 2)
                }
                $networkResults += $result
                $allResults += $result

                Write-Host " Actions: $($json.TotalActions), APS: $([math]::Round($json.ActionsPerSecond, 2))" -ForegroundColor Green
            }
            else {
                Write-Host " FAILED (no result file)" -ForegroundColor Red
            }
            Remove-PSSession $session
        }
        catch {
            Write-Host " ERROR: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Network summary
    if ($networkResults.Count -gt 0) {
        $avgActions = [math]::Round(($networkResults | Measure-Object -Property Actions -Average).Average, 2)
        $avgAPS = [math]::Round(($networkResults | Measure-Object -Property APS -Average).Average, 3)
        $avgInference = [math]::Round(($networkResults | Measure-Object -Property InferenceMs -Average).Average, 2)
        $minActions = ($networkResults | Measure-Object -Property Actions -Minimum).Minimum
        $maxActions = ($networkResults | Measure-Object -Property Actions -Maximum).Maximum
        $stdDev = [math]::Round([math]::Sqrt(($networkResults | ForEach-Object { [math]::Pow($_.Actions - $avgActions, 2) } | Measure-Object -Average).Average), 2)

        $networkSummaries += [PSCustomObject]@{
            NetworkId = $networkId
            Trials = $networkResults.Count
            AvgActions = $avgActions
            MinActions = $minActions
            MaxActions = $maxActions
            StdDev = $stdDev
            AvgAPS = $avgAPS
            AvgInferenceMs = $avgInference
        }

        Write-Host "  Summary: Avg=$avgActions, Min=$minActions, Max=$maxActions, StdDev=$stdDev" -ForegroundColor Cyan
    }
}

$endTime = Get-Date
$totalDuration = $endTime - $startTime

Write-Host ""
Write-Host "=== FINAL SUMMARY ===" -ForegroundColor Cyan
Write-Host "Total time: $([math]::Round($totalDuration.TotalMinutes, 1)) minutes"
Write-Host "Total trials completed: $($allResults.Count)"
Write-Host ""

# Sort by average actions
$networkSummaries = $networkSummaries | Sort-Object -Property AvgActions -Descending

Write-Host "Network Rankings (by Avg Actions):" -ForegroundColor Yellow
$networkSummaries | Format-Table -AutoSize

# Save summaries
$allResults | ConvertTo-Json -Depth 3 | Out-File "$localResultsDir\all_trials.json" -Encoding UTF8
$networkSummaries | ConvertTo-Json -Depth 3 | Out-File "$localResultsDir\network_summaries.json" -Encoding UTF8

# Create markdown report
$report = @"
# Generation 0 Multi-Trial Training Results

**Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm")
**Networks:** $StartId to $EndId ($totalNetworks networks)
**Trials per network:** $TrialsPerNetwork
**Duration per trial:** ${DurationSeconds}s
**Total trials:** $($allResults.Count)
**Total time:** $([math]::Round($totalDuration.TotalMinutes, 1)) minutes

## Network Rankings (by Average Actions)

| Rank | Network | Trials | Avg Actions | Min | Max | StdDev | Avg APS | Avg Inference |
|------|---------|--------|-------------|-----|-----|--------|---------|---------------|
"@

$rank = 1
foreach ($net in $networkSummaries) {
    $report += "| $rank | $("{0:D2}" -f $net.NetworkId) | $($net.Trials) | $($net.AvgActions) | $($net.MinActions) | $($net.MaxActions) | $($net.StdDev) | $($net.AvgAPS) | $($net.AvgInferenceMs) ms |`n"
    $rank++
}

$report += @"

## Top 10 Selection

Based on average actions across $TrialsPerNetwork trials:

"@

$top10 = $networkSummaries | Select-Object -First 10
foreach ($net in $top10) {
    $report += "- **Network $("{0:D2}" -f $net.NetworkId)**: $($net.AvgActions) avg actions (stddev: $($net.StdDev))`n"
}

$report | Out-File "$localResultsDir\report.md" -Encoding UTF8

Write-Host "Results saved to: $localResultsDir"
Write-Host "- all_trials.json ($($allResults.Count) trials)"
Write-Host "- network_summaries.json ($($networkSummaries.Count) networks)"
Write-Host "- report.md"
