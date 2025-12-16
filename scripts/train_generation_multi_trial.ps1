# Train a generation with multiple trials per network
# Usage: .\train_generation_multi_trial.ps1 -Generation 1 -TrialsPerNetwork 10 -Duration 60

param(
    [Parameter(Mandatory=$true)]
    [int]$Generation,

    [int]$TrialsPerNetwork = 10,
    [int]$Duration = 60,
    [int]$Timeout = 90,
    [string]$Map = "training_maps\training-0.tzared",
    [int]$StartNetwork = 0,
    [int]$EndNetwork = 19
)

$ErrorActionPreference = "Stop"

$baseDir = "C:\Users\maciek\ai_experiments\tzar_bot"
$genDir = Join-Path $baseDir "training\generation_$Generation"
$onnxDir = Join-Path $genDir "onnx"
$resultsDir = Join-Path $genDir "results"
$trainingRunner = Join-Path $baseDir "publish\TrainingRunner\TrainingRunner.exe"
$mapPath = Join-Path $baseDir $Map

# Validate paths
if (-not (Test-Path $trainingRunner)) {
    Write-Error "TrainingRunner not found: $trainingRunner"
    exit 1
}

if (-not (Test-Path $onnxDir)) {
    Write-Error "ONNX directory not found: $onnxDir"
    exit 1
}

if (-not (Test-Path $mapPath)) {
    Write-Error "Map not found: $mapPath"
    exit 1
}

# Create results directory
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
}

$networkCount = $EndNetwork - $StartNetwork + 1
$totalTrials = $networkCount * $TrialsPerNetwork

Write-Host "=== TzarBot Generation $Generation Multi-Trial Training ===" -ForegroundColor Cyan
Write-Host "Networks: $StartNetwork to $EndNetwork ($networkCount networks)"
Write-Host "Trials per network: $TrialsPerNetwork"
Write-Host "Total trials: $totalTrials"
Write-Host "Duration: ${Duration}s per trial (timeout: ${Timeout}s)"
Write-Host "Estimated time: ~$([math]::Round($totalTrials * 35 / 60, 0)) minutes"
Write-Host "Map: $mapPath"
Write-Host ""

$allTrials = @()
$networkSummaries = @()
$startTime = Get-Date
$completedTrials = 0

for ($netId = $StartNetwork; $netId -le $EndNetwork; $netId++) {
    $networkFile = Join-Path $onnxDir ("network_{0:D2}.onnx" -f $netId)

    if (-not (Test-Path $networkFile)) {
        Write-Host "[$netId] SKIP - Network not found" -ForegroundColor Yellow
        continue
    }

    Write-Host ""
    Write-Host "=== Network $netId ===" -ForegroundColor Yellow

    $networkTrials = @()

    for ($trial = 1; $trial -le $TrialsPerNetwork; $trial++) {
        $outputFile = Join-Path $resultsDir ("network_{0:D2}_trial_{1:D2}.json" -f $netId, $trial)

        $progress = [math]::Round(($completedTrials / $totalTrials) * 100, 1)
        $elapsed = (Get-Date) - $startTime
        $remaining = if ($completedTrials -gt 0) {
            [TimeSpan]::FromSeconds(($elapsed.TotalSeconds / $completedTrials) * ($totalTrials - $completedTrials))
        } else {
            [TimeSpan]::Zero
        }

        Write-Host "  Trial $trial/$TrialsPerNetwork (${progress}% total, ETA: $($remaining.ToString('hh\:mm\:ss')))..." -NoNewline

        $trialStart = Get-Date
        try {
            $process = Start-Process -FilePath $trainingRunner `
                -ArgumentList "`"$networkFile`" `"$mapPath`" $Duration `"$outputFile`"" `
                -NoNewWindow -PassThru -Wait

            $trialEnd = Get-Date
            $trialDuration = ($trialEnd - $trialStart).TotalSeconds

            if ((Test-Path $outputFile) -and $process.ExitCode -eq 0) {
                $result = Get-Content $outputFile | ConvertFrom-Json
                Write-Host " $($result.Outcome) - Actions: $($result.TotalActions), APS: $([math]::Round($result.ActionsPerSecond, 2)), Time: $([math]::Round($trialDuration, 1))s" -ForegroundColor Green

                $networkTrials += [PSCustomObject]@{
                    Trial = $trial
                    Outcome = $result.Outcome
                    Duration = $result.ActualDurationSeconds
                    Actions = $result.TotalActions
                    APS = [math]::Round($result.ActionsPerSecond, 2)
                }

                $allTrials += [PSCustomObject]@{
                    NetworkId = $netId
                    Trial = $trial
                    Outcome = $result.Outcome
                    Duration = $result.ActualDurationSeconds
                    Actions = $result.TotalActions
                    APS = [math]::Round($result.ActionsPerSecond, 2)
                }
            } else {
                Write-Host " FAIL" -ForegroundColor Red
            }
        } catch {
            Write-Host " ERROR - $($_.Exception.Message)" -ForegroundColor Red
        }

        $completedTrials++
    }

    # Calculate network summary
    if ($networkTrials.Count -gt 0) {
        $avgActions = [math]::Round(($networkTrials | Measure-Object -Property Actions -Average).Average, 1)
        $maxActions = ($networkTrials | Measure-Object -Property Actions -Maximum).Maximum
        $minActions = ($networkTrials | Measure-Object -Property Actions -Minimum).Minimum
        $avgAPS = [math]::Round(($networkTrials | Measure-Object -Property APS -Average).Average, 2)
        $victories = ($networkTrials | Where-Object { $_.Outcome -eq "Victory" }).Count
        $defeats = ($networkTrials | Where-Object { $_.Outcome -eq "Defeat" }).Count
        $timeouts = ($networkTrials | Where-Object { $_.Outcome -eq "Timeout" }).Count

        $networkSummaries += [PSCustomObject]@{
            NetworkId = $netId
            Trials = $networkTrials.Count
            AvgActions = $avgActions
            MaxActions = $maxActions
            MinActions = $minActions
            AvgAPS = $avgAPS
            Victories = $victories
            Defeats = $defeats
            Timeouts = $timeouts
        }

        Write-Host "  Summary: Avg=$avgActions, Max=$maxActions, V/D/T=$victories/$defeats/$timeouts" -ForegroundColor Cyan
    }
}

$endTime = Get-Date
$totalDuration = ($endTime - $startTime).TotalMinutes

# Save all trials
$allTrialsFile = Join-Path $resultsDir "all_trials.json"
$allTrials | ConvertTo-Json -Depth 10 | Set-Content $allTrialsFile

# Save summary (compatible with EvolveGeneration)
$summaryFile = Join-Path $resultsDir "batch_summary.json"
$networkSummaries | ForEach-Object {
    [PSCustomObject]@{
        NetworkId = $_.NetworkId
        Actions = $_.AvgActions  # Use average for fitness
        APS = $_.AvgAPS
        Outcome = if ($_.Victories -gt $_.Defeats) { "Victory" } elseif ($_.Defeats -gt 0) { "Defeat" } else { "Timeout" }
        Trials = $_.Trials
        MaxActions = $_.MaxActions
        MinActions = $_.MinActions
        Victories = $_.Victories
        Defeats = $_.Defeats
        Timeouts = $_.Timeouts
    }
} | ConvertTo-Json -Depth 10 | Set-Content $summaryFile

Write-Host ""
Write-Host "=== Training Complete ===" -ForegroundColor Cyan
Write-Host "Total trials: $completedTrials"
Write-Host "Total time: $([math]::Round($totalDuration, 1)) minutes"
Write-Host "All trials: $allTrialsFile"
Write-Host "Summary: $summaryFile"

# Show ranking by average actions
Write-Host ""
Write-Host "=== Ranking (by Average Actions) ===" -ForegroundColor Yellow
$networkSummaries | Sort-Object -Property AvgActions -Descending |
    Format-Table NetworkId, AvgActions, MaxActions, MinActions, AvgAPS, @{L='V/D/T';E={"$($_.Victories)/$($_.Defeats)/$($_.Timeouts)"}}
