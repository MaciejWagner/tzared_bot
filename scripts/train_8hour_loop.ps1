param(
    [int]$MaxHours = 8,
    [int]$ParallelSessions = 4,
    [int]$TrialsPerMap = 4,
    [int]$TrialDuration = 40,
    [int]$StartGeneration = 2
)

$ErrorActionPreference = "Stop"
$projectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"
$runner = "$projectRoot\publish\TrainingRunner\TrainingRunner.exe"
$evolverPath = "$projectRoot\publish\EvolveGeneration\EvolveGeneration.exe"

$maps = @(
    "$projectRoot\training_maps\training-0.tzared",
    "$projectRoot\training_maps\training-1.tzared"
)

$startTime = Get-Date
$endTime = $startTime.AddHours($MaxHours)
$currentGeneration = $StartGeneration

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  TzarBot 8-Hour Training Loop" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Start: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "End:   $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Host "Maps: $($maps.Count)"
Write-Host "Trials per map: $TrialsPerMap"
Write-Host "Parallel sessions: $ParallelSessions"
Write-Host "Trial duration: ${TrialDuration}s"
Write-Host "Starting generation: $currentGeneration"
Write-Host ""

function Get-GenerationPath($gen) {
    return "$projectRoot\training\generation_$gen"
}

function Get-TotalTrials($networkCount, $mapCount, $trialsPerMap) {
    return $networkCount * $mapCount * $trialsPerMap
}

function Train-Generation {
    param(
        [int]$Generation,
        [string[]]$Maps,
        [int]$TrialsPerMap,
        [int]$Duration,
        [int]$Parallel
    )

    $genPath = Get-GenerationPath $Generation
    $onnxDir = "$genPath\onnx"
    $resultsDir = "$genPath\results"

    if (-not (Test-Path $onnxDir)) {
        Write-Host "ERROR: Generation $Generation not found at $onnxDir" -ForegroundColor Red
        return $null
    }

    New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

    $networks = Get-ChildItem -Path $onnxDir -Filter "*.onnx" | Sort-Object Name
    $totalTrials = Get-TotalTrials $networks.Count $Maps.Count $TrialsPerMap

    Write-Host ""
    Write-Host "=== Training Generation $Generation ===" -ForegroundColor Yellow
    Write-Host "Networks: $($networks.Count)"
    Write-Host "Maps: $($Maps.Count)"
    Write-Host "Trials per map: $TrialsPerMap"
    Write-Host "Total trials: $totalTrials"
    Write-Host ""

    # Build trial list
    $trials = @()
    foreach ($network in $networks) {
        $mapIndex = 0
        foreach ($map in $Maps) {
            $mapName = [System.IO.Path]::GetFileNameWithoutExtension($map)
            for ($t = 1; $t -le $TrialsPerMap; $t++) {
                $trials += [PSCustomObject]@{
                    NetworkPath = $network.FullName
                    NetworkName = $network.BaseName
                    MapPath = $map
                    MapName = $mapName
                    TrialNumber = $t
                    OutputFile = "$resultsDir\$($network.BaseName)_${mapName}_trial$t.json"
                }
            }
            $mapIndex++
        }
    }

    $completed = 0
    $allResults = @()
    $genStartTime = Get-Date

    # Process in batches
    for ($i = 0; $i -lt $trials.Count; $i += $Parallel) {
        $batch = $trials[$i..([Math]::Min($i + $Parallel - 1, $trials.Count - 1))]

        # Start processes
        $processes = @()
        foreach ($trial in $batch) {
            Write-Host "  Starting: $($trial.NetworkName) $($trial.MapName) #$($trial.TrialNumber)" -ForegroundColor DarkGray

            $proc = Start-Process -FilePath $runner -ArgumentList @(
                "`"$($trial.NetworkPath)`"",
                "`"$($trial.MapPath)`"",
                $Duration,
                "`"$($trial.OutputFile)`""
            ) -NoNewWindow -PassThru

            $processes += [PSCustomObject]@{
                Process = $proc
                Trial = $trial
            }
        }

        # Wait for batch
        foreach ($p in $processes) {
            $p.Process.WaitForExit()
            $completed++

            $pct = [Math]::Floor($completed * 100 / $totalTrials)
            $elapsed = (Get-Date) - $genStartTime
            $eta = if ($completed -gt 0) {
                [TimeSpan]::FromSeconds($elapsed.TotalSeconds / $completed * ($totalTrials - $completed))
            } else { [TimeSpan]::Zero }

            # Read result
            $outcome = "ERROR"
            $duration = 0
            $actions = 0
            $aps = 0

            if (Test-Path $p.Trial.OutputFile) {
                try {
                    $json = Get-Content $p.Trial.OutputFile -Raw | ConvertFrom-Json
                    $outcome = $json.Outcome
                    $duration = $json.ActualDurationSeconds
                    $actions = $json.TotalActions
                    $aps = $json.ActionsPerSecond
                } catch { }
            }

            $color = switch ($outcome) {
                "VICTORY" { "Green" }
                "DEFEAT" { "Red" }
                "TIMEOUT" { "Yellow" }
                default { "Gray" }
            }

            Write-Host "[$pct%] $($p.Trial.NetworkName) $($p.Trial.MapName) #$($p.Trial.TrialNumber): $outcome ($actions act, ETA: $($eta.ToString('hh\:mm\:ss')))" -ForegroundColor $color

            $allResults += [PSCustomObject]@{
                NetworkName = $p.Trial.NetworkName
                MapName = $p.Trial.MapName
                Trial = $p.Trial.TrialNumber
                Outcome = $outcome
                Duration = $duration
                Actions = $actions
                APS = $aps
            }
        }

        # Check time limit
        if ((Get-Date) -gt $endTime) {
            Write-Host "TIME LIMIT REACHED - stopping training" -ForegroundColor Magenta
            break
        }
    }

    # Create summary
    $summary = @()
    $networkGroups = $allResults | Group-Object -Property NetworkName

    foreach ($group in $networkGroups) {
        $networkResults = $group.Group
        $networkId = [int]($group.Name -replace 'network_', '')

        $victories = ($networkResults | Where-Object { $_.Outcome -eq "VICTORY" }).Count
        $defeats = ($networkResults | Where-Object { $_.Outcome -eq "DEFEAT" }).Count
        $timeouts = ($networkResults | Where-Object { $_.Outcome -eq "TIMEOUT" }).Count
        $totalActions = ($networkResults | Measure-Object -Property Actions -Sum).Sum
        $avgAPS = ($networkResults | Measure-Object -Property APS -Average).Average

        # Fitness: victories worth most, timeouts some, defeats least
        # Also factor in total actions
        $fitness = ($victories * 100) + ($timeouts * 30) + ($totalActions / 10)

        $summary += [PSCustomObject]@{
            NetworkId = $networkId
            Outcome = if ($victories -gt 0) { "VICTORY" } elseif ($timeouts -gt 0) { "TIMEOUT" } else { "DEFEAT" }
            Duration = ($networkResults | Measure-Object -Property Duration -Average).Average
            Actions = $totalActions
            APS = [Math]::Round($avgAPS, 2)
            InferenceMs = 0
            Fitness = $fitness
            V = $victories
            D = $defeats
            T = $timeouts
        }
    }

    # Sort by fitness and save
    $summary = $summary | Sort-Object -Property Fitness -Descending

    $summaryFile = "$resultsDir\batch_summary.json"
    $summary | ConvertTo-Json | Set-Content $summaryFile

    Write-Host ""
    Write-Host "=== Generation $Generation Summary ===" -ForegroundColor Green
    $summary | Select-Object NetworkId, V, D, T, Actions, APS, Fitness | Format-Table -AutoSize

    Write-Host "Results saved: $summaryFile"

    return $summaryFile
}

function Evolve-NextGeneration {
    param(
        [int]$SourceGen,
        [string]$ResultsFile
    )

    $sourceDir = Get-GenerationPath $SourceGen
    $targetGen = $SourceGen + 1
    $targetDir = Get-GenerationPath $targetGen

    Write-Host ""
    Write-Host "=== Evolving Generation $targetGen ===" -ForegroundColor Magenta

    # Build evolver if needed
    if (-not (Test-Path $evolverPath)) {
        Write-Host "Building EvolveGeneration..."
        dotnet publish "$projectRoot\tools\EvolveGeneration\EvolveGeneration.csproj" -c Release -r win-x64 --self-contained -o "$projectRoot\publish\EvolveGeneration" | Out-Null
    }

    & $evolverPath $sourceDir $ResultsFile $targetDir --population 20 --mutation 0.15 --elite-ratio 0.3 --random-ratio 0.1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Evolution failed" -ForegroundColor Red
        return -1
    }

    Write-Host "Generation $targetGen created!" -ForegroundColor Green
    return $targetGen
}

function Cleanup-OldGeneration {
    param(
        [int]$Generation,
        [int]$KeepGenerations = 2
    )

    # Keep the last N generations, delete older ones
    $genToDelete = $Generation - $KeepGenerations
    if ($genToDelete -lt 0) {
        return
    }

    $oldGenPath = Get-GenerationPath $genToDelete
    if (Test-Path $oldGenPath) {
        Write-Host "Cleaning up generation $genToDelete to save space..." -ForegroundColor DarkYellow

        # Keep only batch_summary.json from results
        $summaryFile = "$oldGenPath\results\batch_summary.json"
        $summaryBackup = "$projectRoot\training\summaries\generation_${genToDelete}_summary.json"

        # Create summaries folder if needed
        $summariesDir = "$projectRoot\training\summaries"
        if (-not (Test-Path $summariesDir)) {
            New-Item -ItemType Directory -Force -Path $summariesDir | Out-Null
        }

        # Backup summary
        if (Test-Path $summaryFile) {
            Copy-Item $summaryFile $summaryBackup -Force
            Write-Host "  Saved summary to: $summaryBackup" -ForegroundColor DarkGray
        }

        # Delete generation folder
        Remove-Item -Path $oldGenPath -Recurse -Force
        Write-Host "  Deleted: $oldGenPath" -ForegroundColor DarkGray
    }
}

# Main loop
$generationCount = 0

while ((Get-Date) -lt $endTime) {
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "  Starting Generation $currentGeneration" -ForegroundColor Cyan
    Write-Host "  Time remaining: $(($endTime - (Get-Date)).ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan

    # Train current generation
    $resultsFile = Train-Generation -Generation $currentGeneration -Maps $maps -TrialsPerMap $TrialsPerMap -Duration $TrialDuration -Parallel $ParallelSessions

    if ($null -eq $resultsFile) {
        Write-Host "Training failed, exiting" -ForegroundColor Red
        break
    }

    $generationCount++

    # Check if we have time for evolution + next generation
    $remainingTime = $endTime - (Get-Date)
    if ($remainingTime.TotalMinutes -lt 10) {
        Write-Host "Not enough time for next generation, stopping" -ForegroundColor Yellow
        break
    }

    # Evolve next generation
    $nextGen = Evolve-NextGeneration -SourceGen $currentGeneration -ResultsFile $resultsFile

    if ($nextGen -lt 0) {
        Write-Host "Evolution failed, stopping" -ForegroundColor Red
        break
    }

    # Cleanup old generations (keep last 2)
    Cleanup-OldGeneration -Generation $nextGen -KeepGenerations 2

    $currentGeneration = $nextGen
}

# Final summary
$totalTime = (Get-Date) - $startTime

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Training Complete!" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Total time: $($totalTime.ToString('hh\:mm\:ss'))"
Write-Host "Generations trained: $generationCount"
Write-Host "Final generation: $currentGeneration"
Write-Host ""
