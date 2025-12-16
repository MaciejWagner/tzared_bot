param(
    [Parameter(Mandatory=$true)]
    [string]$GenerationPath,

    [Parameter(Mandatory=$true)]
    [string]$MapPath,

    [int]$Duration = 40,
    [int]$TrialsPerNetwork = 4,
    [int]$ParallelSessions = 2
)

$ErrorActionPreference = "Stop"
$projectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"
$runner = "$projectRoot\publish\TrainingRunner\TrainingRunner.exe"

# Get all ONNX files
$networks = Get-ChildItem -Path "$projectRoot\$GenerationPath" -Filter "*.onnx" | Sort-Object Name
$totalNetworks = $networks.Count
$totalTrials = $totalNetworks * $TrialsPerNetwork

Write-Host "=== Parallel Training ===" -ForegroundColor Cyan
Write-Host "Networks: $totalNetworks"
Write-Host "Trials per network: $TrialsPerNetwork"
Write-Host "Total trials: $totalTrials"
Write-Host "Parallel sessions: $ParallelSessions"
Write-Host "Duration per trial: ${Duration}s"
Write-Host ""

# Create results directory
$resultsDir = "$projectRoot\$GenerationPath\..\results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# Build trial list
$trials = @()
foreach ($network in $networks) {
    for ($t = 1; $t -le $TrialsPerNetwork; $t++) {
        $trials += [PSCustomObject]@{
            NetworkPath = $network.FullName
            NetworkName = $network.BaseName
            TrialNumber = $t
            OutputFile = "$resultsDir\$($network.BaseName)_trial$t.json"
        }
    }
}

$startTime = Get-Date
$completed = 0
$results = @{}

Write-Host "Starting training at $(Get-Date -Format 'HH:mm:ss')..." -ForegroundColor Green
Write-Host ""

# Process in batches
for ($i = 0; $i -lt $trials.Count; $i += $ParallelSessions) {
    $batch = $trials[$i..([Math]::Min($i + $ParallelSessions - 1, $trials.Count - 1))]

    # Start processes for this batch
    $processes = @()
    foreach ($trial in $batch) {
        Write-Host "  Starting: $($trial.NetworkName) #$($trial.TrialNumber)" -ForegroundColor DarkGray

        $proc = Start-Process -FilePath $runner -ArgumentList @(
            "`"$($trial.NetworkPath)`"",
            "`"$projectRoot\$MapPath`"",
            $Duration,
            "`"$($trial.OutputFile)`""
        ) -NoNewWindow -PassThru

        $processes += [PSCustomObject]@{
            Process = $proc
            Trial = $trial
        }
    }

    # Wait for all processes in batch to complete
    foreach ($p in $processes) {
        $p.Process.WaitForExit()
        $completed++

        $pct = [Math]::Floor($completed * 100 / $totalTrials)
        $elapsed = (Get-Date) - $startTime
        $eta = if ($completed -gt 0) {
            [Math]::Round($elapsed.TotalSeconds / $completed * ($totalTrials - $completed))
        } else { 0 }

        # Read result
        $outcome = "ERROR"
        $duration = 0
        $actions = 0

        if (Test-Path $p.Trial.OutputFile) {
            try {
                $json = Get-Content $p.Trial.OutputFile -Raw | ConvertFrom-Json
                $outcome = $json.Outcome
                $duration = $json.ActualDurationSeconds
                $actions = $json.TotalActions
            } catch { }
        }

        $color = switch ($outcome) {
            "VICTORY" { "Green" }
            "DEFEAT" { "Red" }
            "TIMEOUT" { "Yellow" }
            default { "Gray" }
        }

        Write-Host "[$pct%] $($p.Trial.NetworkName) #$($p.Trial.TrialNumber): $outcome ($([Math]::Round($duration,1))s, $actions actions) [ETA: ${eta}s]" -ForegroundColor $color

        # Store result
        $key = $p.Trial.NetworkName
        if (-not $results.ContainsKey($key)) {
            $results[$key] = @()
        }
        $results[$key] += [PSCustomObject]@{
            Trial = $p.Trial.TrialNumber
            Outcome = $outcome
            Duration = $duration
            Actions = $actions
        }
    }
}

# Summary
Write-Host ""
Write-Host "=== TRAINING COMPLETE ===" -ForegroundColor Cyan
$totalTime = (Get-Date) - $startTime
Write-Host "Total time: $([Math]::Floor($totalTime.TotalMinutes))m $([Math]::Floor($totalTime.Seconds))s"
Write-Host ""

# Summary per network
$summary = @()
foreach ($networkName in ($results.Keys | Sort-Object)) {
    $networkResults = $results[$networkName]
    $victories = ($networkResults | Where-Object { $_.Outcome -eq "VICTORY" }).Count
    $defeats = ($networkResults | Where-Object { $_.Outcome -eq "DEFEAT" }).Count
    $timeouts = ($networkResults | Where-Object { $_.Outcome -eq "TIMEOUT" }).Count
    $avgDuration = ($networkResults | Measure-Object -Property Duration -Average).Average
    $avgActions = ($networkResults | Measure-Object -Property Actions -Average).Average

    $fitness = ($victories * 1.0 + $timeouts * 0.3) / $TrialsPerNetwork

    $summary += [PSCustomObject]@{
        Network = $networkName
        V = $victories
        D = $defeats
        T = $timeouts
        Fitness = [Math]::Round($fitness, 2)
        AvgDur = [Math]::Round($avgDuration, 1)
        AvgAct = [Math]::Round($avgActions, 0)
    }
}

$summary | Sort-Object -Property Fitness -Descending | Format-Table -AutoSize

# Save summary
$summaryFile = "$resultsDir\summary.json"
$summary | ConvertTo-Json | Set-Content $summaryFile
Write-Host "Summary saved to: $summaryFile"

# Top 5
Write-Host ""
Write-Host "=== TOP 5 ===" -ForegroundColor Green
$summary | Sort-Object -Property Fitness -Descending | Select-Object -First 5 | Format-Table -AutoSize
