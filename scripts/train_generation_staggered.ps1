param(
    [Parameter(Mandatory=$true)]
    [string]$GenerationPath,

    [string[]]$MapPaths = @("training_maps/training-0.tzared", "training_maps/training-1.tzared", "training_maps/training-2.tzared"),

    [int]$Duration = 40,
    [int]$TrialsPerNetwork = 5,
    [int]$ParallelSessions = 3,
    [int]$StaggerDelaySeconds = 4  # Delay between starting each session (GPU init is heavy)
)

$ErrorActionPreference = "Stop"
$projectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"
$runner = "$projectRoot\publish\TrainingRunner\TrainingRunner.exe"

# Get all ONNX files (in onnx/ subfolder)
$onnxPath = "$projectRoot\$GenerationPath\onnx"
if (-not (Test-Path $onnxPath)) {
    # Fallback to main folder if no onnx subfolder
    $onnxPath = "$projectRoot\$GenerationPath"
}
$networks = Get-ChildItem -Path $onnxPath -Filter "*.onnx" | Sort-Object Name
$totalNetworks = $networks.Count
$totalTrials = $totalNetworks * $TrialsPerNetwork

Write-Host "=== Staggered Parallel Training ===" -ForegroundColor Cyan
Write-Host "Networks: $totalNetworks"
Write-Host "Trials per network: $TrialsPerNetwork"
Write-Host "Total trials: $totalTrials"
Write-Host "Max parallel sessions: $ParallelSessions"
Write-Host "Stagger delay: ${StaggerDelaySeconds}s"
Write-Host "Duration per trial: ${Duration}s"
Write-Host "Maps: $($MapPaths -join ', ')"
Write-Host ""

# Create results directory (inside generation folder)
$resultsDir = "$projectRoot\$GenerationPath\results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# Build trial queue (alternating maps)
$trialQueue = [System.Collections.Queue]::new()
$mapIndex = 0
foreach ($network in $networks) {
    for ($t = 1; $t -le $TrialsPerNetwork; $t++) {
        $mapPath = $MapPaths[$mapIndex % $MapPaths.Count]
        $trialQueue.Enqueue([PSCustomObject]@{
            NetworkPath = $network.FullName
            NetworkName = $network.BaseName
            TrialNumber = $t
            MapPath = $mapPath
            OutputFile = "$resultsDir\$($network.BaseName)_trial$t.json"
        })
        $mapIndex++
    }
}

$startTime = Get-Date
$completed = 0
$results = @{}
$activeProcesses = @{}  # Dictionary: processId -> trial info
$lastStartTime = [DateTime]::MinValue

Write-Host "Starting training at $(Get-Date -Format 'HH:mm:ss')..." -ForegroundColor Green
Write-Host ""

function Start-Trial($trial) {
    $mapName = [System.IO.Path]::GetFileNameWithoutExtension($trial.MapPath)
    Write-Host "  [$(Get-Date -Format 'HH:mm:ss')] Starting: $($trial.NetworkName) #$($trial.TrialNumber) [$mapName]" -ForegroundColor DarkGray

    $proc = Start-Process -FilePath $runner -ArgumentList @(
        "`"$($trial.NetworkPath)`"",
        "`"$projectRoot\$($trial.MapPath)`"",
        $Duration,
        "`"$($trial.OutputFile)`""
    ) -NoNewWindow -PassThru

    return $proc
}

function Process-CompletedTrial($proc, $trial) {
    $script:completed++

    $pct = [Math]::Floor($completed * 100 / $totalTrials)
    $elapsed = (Get-Date) - $startTime
    $eta = if ($completed -gt 0) {
        [Math]::Round($elapsed.TotalSeconds / $completed * ($totalTrials - $completed))
    } else { 0 }

    # Read result
    $outcome = "ERROR"
    $duration = 0
    $actions = 0

    if (Test-Path $trial.OutputFile) {
        try {
            $json = Get-Content $trial.OutputFile -Raw | ConvertFrom-Json
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

    Write-Host "[$pct%] $($trial.NetworkName) #$($trial.TrialNumber): $outcome ($([Math]::Round($duration,1))s, $actions actions) [ETA: ${eta}s]" -ForegroundColor $color

    # Store result
    $key = $trial.NetworkName
    if (-not $results.ContainsKey($key)) {
        $results[$key] = @()
    }
    $results[$key] += [PSCustomObject]@{
        Trial = $trial.TrialNumber
        Outcome = $outcome
        Duration = $duration
        Actions = $actions
    }
}

# Main loop - worker pool with staggered starts
while ($trialQueue.Count -gt 0 -or $activeProcesses.Count -gt 0) {

    # Check for completed processes
    $completedIds = @()
    foreach ($entry in $activeProcesses.GetEnumerator()) {
        if ($entry.Value.Process.HasExited) {
            Process-CompletedTrial $entry.Value.Process $entry.Value.Trial
            $completedIds += $entry.Key
        }
    }

    # Remove completed
    foreach ($id in $completedIds) {
        $activeProcesses.Remove($id)
    }

    # Start new processes if:
    # 1. We have capacity
    # 2. There are trials in queue
    # 3. Enough time passed since last start (stagger delay)
    $timeSinceLastStart = ((Get-Date) - $lastStartTime).TotalSeconds

    if ($activeProcesses.Count -lt $ParallelSessions -and
        $trialQueue.Count -gt 0 -and
        $timeSinceLastStart -ge $StaggerDelaySeconds) {

        $trial = $trialQueue.Dequeue()
        $proc = Start-Trial $trial
        $activeProcesses[$proc.Id] = @{
            Process = $proc
            Trial = $trial
        }
        $lastStartTime = Get-Date
    }

    # Small sleep to avoid busy-waiting
    Start-Sleep -Milliseconds 200
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
