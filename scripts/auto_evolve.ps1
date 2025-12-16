# auto_evolve.ps1 - Automatyczna ewolucja do osiągnięcia 80% victory rate
# Użycie: ./scripts/auto_evolve.ps1 -StartGeneration 12 -TargetVictoryRate 80

param(
    [int]$StartGeneration = 14,
    [int]$TargetVictoryRate = 80,
    [string[]]$MapPaths = @(
        "training_maps/training-0-1.tzared",
        "training_maps/training-0-2.tzared",
        "training_maps/training-0-3.tzared",
        "training_maps/training-0-4.tzared",
        "training_maps/training-0-5.tzared",
        "training_maps/training-0-6.tzared"
    ),
    [int]$Duration = 40,
    [int]$TrialsPerNetwork = 8,
    [int]$ParallelSessions = 3,
    [int]$StaggerDelaySeconds = 4,
    [int]$Population = 50,
    [int]$Elite = 10,
    [int]$MutatedPerElite = 2,
    [double]$RandomRatio = 0.08
)

$ErrorActionPreference = "Stop"
$ProjectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"

Write-Host "=== TzarBot Auto-Evolution ===" -ForegroundColor Cyan
Write-Host "Target: ${TargetVictoryRate}% victory rate"
Write-Host "Starting from generation: $StartGeneration"
Write-Host ""

$currentGen = $StartGeneration
$victoryRate = 0

while ($victoryRate -lt $TargetVictoryRate) {
    $genPath = "$ProjectRoot\training\generation_$currentGen"
    $resultsPath = "$genPath\results"
    $summaryFile = "$resultsPath\summary.json"

    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "GENERATION $currentGen" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow

    # 1. Check if training needed
    $onnxFiles = Get-ChildItem "$genPath\onnx\*.onnx" -ErrorAction SilentlyContinue
    if (-not $onnxFiles) {
        Write-Host "ERROR: No ONNX files in $genPath\onnx\" -ForegroundColor Red
        exit 1
    }

    $networkCount = $onnxFiles.Count
    Write-Host "Networks to train: $networkCount"

    # 2. Run training if results don't exist
    if (-not (Test-Path $summaryFile)) {
        Write-Host ""
        Write-Host "--- Training Generation $currentGen ---" -ForegroundColor Green

        & "$ProjectRoot\scripts\train_generation_staggered.ps1" `
            -GenerationPath "training\generation_$currentGen" `
            -MapPaths $MapPaths `
            -Duration $Duration `
            -TrialsPerNetwork $TrialsPerNetwork `
            -ParallelSessions $ParallelSessions `
            -StaggerDelaySeconds $StaggerDelaySeconds

        # 3. Summarize results
        Write-Host ""
        Write-Host "--- Summarizing Results ---" -ForegroundColor Green
        & "$ProjectRoot\scripts\summarize_results.ps1" -GenerationPath "training\generation_$currentGen"
    } else {
        Write-Host "Results already exist, skipping training..."
    }

    # 4. Calculate victory rate
    if (-not (Test-Path $summaryFile)) {
        Write-Host "ERROR: Summary file not found: $summaryFile" -ForegroundColor Red
        exit 1
    }

    $summary = Get-Content $summaryFile | ConvertFrom-Json
    $totalVictories = ($summary | Measure-Object -Property V -Sum).Sum
    $totalTrials = $networkCount * $TrialsPerNetwork
    $victoryRate = [math]::Round(($totalVictories / $totalTrials) * 100, 1)

    # Find best network
    $best = $summary | Sort-Object -Property Fitness -Descending | Select-Object -First 1

    Write-Host ""
    Write-Host "--- Results ---" -ForegroundColor Cyan
    Write-Host "Victory Rate: $victoryRate% ($totalVictories / $totalTrials)"
    Write-Host "Best: $($best.Network) - Fitness: $($best.Fitness), V:$($best.V) D:$($best.D) T:$($best.T)"

    # 5. Log to evolution_log.md
    $logFile = "$ProjectRoot\training\evolution_log.md"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
    $top5 = $summary | Sort-Object -Property Fitness -Descending | Select-Object -First 5

    $logEntry = @"

### Generation $currentGen
| Parametr | Wartość |
|----------|---------|
| Data | $timestamp |
| Mapa | $(Split-Path $MapPath -Leaf) |
| Victory rate | $victoryRate% ($totalVictories/$totalTrials) |
| Population | $networkCount |

**Top 5:**
| Network | V | D | T | Fitness | AvgDur | AvgAct |
|---------|---|---|---|---------|--------|--------|
"@

    foreach ($net in $top5) {
        $logEntry += "| $($net.Network) | $($net.V) | $($net.D) | $($net.T) | $($net.Fitness) | $([math]::Round($net.AvgDur, 1))s | $([math]::Round($net.AvgAct, 0)) |`n"
    }

    # 6. Check if target reached
    if ($victoryRate -ge $TargetVictoryRate) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "TARGET REACHED! $victoryRate% >= $TargetVictoryRate%" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green

        $logEntry += "`n**Status:** TARGET REACHED ($victoryRate% >= $TargetVictoryRate%)`n"
        Add-Content -Path $logFile -Value $logEntry
        break
    }

    # 7. Evolve to next generation
    $nextGen = $currentGen + 1
    $nextGenPath = "$ProjectRoot\training\generation_$nextGen"

    Write-Host ""
    Write-Host "--- Evolving to Generation $nextGen ---" -ForegroundColor Magenta

    & "$ProjectRoot\publish\EvolveGeneration\EvolveGeneration.exe" `
        "$genPath" `
        "$summaryFile" `
        "$nextGenPath" `
        --population $Population `
        --elite $Elite `
        --mutated-per-elite $MutatedPerElite `
        --random-ratio $RandomRatio `
        --top $Elite

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Evolution failed!" -ForegroundColor Red
        exit 1
    }

    # 8. Update log with evolution info
    $logEntry += "`n**Ewolucja -> Gen${nextGen}:**`n"
    $logEntry += "- Lider: $($best.Network) (Fitness $($best.Fitness))`n"
    $logEntry += "- Population: $Population, Elite: $Elite, MutatedPerElite: $MutatedPerElite, Random: $([math]::Round($RandomRatio * $Population))`n"

    Add-Content -Path $logFile -Value $logEntry

    # 9. Delete old generation (keep current and previous only)
    $oldGen = $currentGen - 1
    $oldGenPath = "$ProjectRoot\training\generation_$oldGen"
    if ((Test-Path $oldGenPath) -and $oldGen -ge 0) {
        Write-Host "Deleting old generation_$oldGen to save space..."
        Remove-Item -Recurse -Force $oldGenPath
    }

    # 10. Move to next generation
    $currentGen = $nextGen

    Write-Host ""
    Write-Host "Proceeding to generation $currentGen..." -ForegroundColor Cyan
    Write-Host ""
}

Write-Host ""
Write-Host "=== Auto-Evolution Complete ===" -ForegroundColor Cyan
Write-Host "Final generation: $currentGen"
Write-Host "Final victory rate: $victoryRate%"
