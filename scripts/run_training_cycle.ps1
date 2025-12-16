# Training Cycle Script
# Runs training, checks for early stop (80% victory), evolves next generation

param(
    [Parameter(Mandatory=$true)]
    [int]$StartGeneration,

    [int]$NumCycles = 10,
    [int]$VictoryThreshold = 80,  # Stop early if this % of networks have victories
    [string]$MapPath = "training_maps/training-0b.tzared"
)

$ErrorActionPreference = "Stop"
$projectRoot = "C:\Users\maciek\ai_experiments\tzar_bot"

function Check-TrainingComplete {
    param([string]$GenPath)

    $resultsDir = "$projectRoot\$GenPath\results"
    if (-not (Test-Path $resultsDir)) { return $false }

    $resultFiles = Get-ChildItem -Path $resultsDir -Filter "*.json" | Where-Object { $_.Name -ne "summary.json" }
    $onnxFiles = Get-ChildItem -Path "$projectRoot\$GenPath\onnx" -Filter "*.onnx"

    $expectedTrials = $onnxFiles.Count * 5
    return $resultFiles.Count -ge $expectedTrials
}

function Check-EarlyStop {
    param([string]$GenPath)

    $resultsDir = "$projectRoot\$GenPath\results"
    if (-not (Test-Path $resultsDir)) { return $false }

    $resultFiles = Get-ChildItem -Path $resultsDir -Filter "*.json" | Where-Object { $_.Name -ne "summary.json" }

    # Group by network
    $networkResults = @{}
    foreach ($file in $resultFiles) {
        $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
        $networkName = $file.Name -replace '_trial\d+\.json$', ''
        if (-not $networkResults.ContainsKey($networkName)) {
            $networkResults[$networkName] = @{ V = 0; Total = 0 }
        }
        $networkResults[$networkName].Total++
        if ($json.Outcome -eq "VICTORY") {
            $networkResults[$networkName].V++
        }
    }

    # Count networks with at least one victory (if they have all 5 trials)
    $completeNetworks = $networkResults.Values | Where-Object { $_.Total -ge 5 }
    $victoryNetworks = $completeNetworks | Where-Object { $_.V -gt 0 }

    if ($completeNetworks.Count -eq 0) { return $false }

    $victoryPct = ($victoryNetworks.Count / $completeNetworks.Count) * 100
    Write-Host "  Complete networks: $($completeNetworks.Count), with victories: $($victoryNetworks.Count) ($([Math]::Round($victoryPct))%)"

    return $victoryPct -ge $VictoryThreshold
}

function Run-Evolution {
    param([int]$SourceGen, [int]$TargetGen)

    $sourcePath = "training/generation_$SourceGen"
    $targetPath = "training/generation_$TargetGen"
    $summaryFile = "$projectRoot\$sourcePath\results\summary.json"

    Write-Host "Evolving generation $SourceGen -> $TargetGen..."

    & "$projectRoot\publish\EvolveGeneration\EvolveGeneration.exe" `
        $sourcePath `
        $summaryFile `
        $targetPath `
        --population 40 `
        --elite 0 `
        --mutated-copies 4 `
        --forced-parent 0 `
        --forced-crossover-count 10 `
        --random-ratio 0.15 `
        --top 10

    if ($LASTEXITCODE -ne 0) {
        throw "Evolution failed!"
    }

    # Delete old generation
    $oldGenPath = "$projectRoot\training\generation_$($SourceGen - 1)"
    if (Test-Path $oldGenPath) {
        Write-Host "Deleting old generation $($SourceGen - 1)..."
        Remove-Item -Recurse -Force $oldGenPath
    }
}

function Run-Training {
    param([int]$Generation)

    $genPath = "training/generation_$Generation"

    Write-Host "Starting training for generation $Generation..."

    & "$projectRoot\scripts\train_generation_staggered.ps1" `
        -GenerationPath $genPath `
        -MapPaths $MapPath `
        -Duration 40 `
        -TrialsPerNetwork 5 `
        -ParallelSessions 3 `
        -StaggerDelaySeconds 4
}

# Main loop
$currentGen = $StartGeneration

for ($cycle = 0; $cycle -lt $NumCycles; $cycle++) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "CYCLE $cycle - Generation $currentGen" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    $genPath = "training/generation_$currentGen"

    # Check if training is complete
    if (-not (Check-TrainingComplete $genPath)) {
        Write-Host "Training not complete, running training..."
        Run-Training $currentGen
    }

    # Summary
    Write-Host ""
    Write-Host "Generation $currentGen training complete!"

    # Evolve next generation
    $nextGen = $currentGen + 1
    Run-Evolution $currentGen $nextGen

    $currentGen = $nextGen

    # Update cycle config
    $configFile = "$projectRoot\training_cycle_config.md"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
    Add-Content -Path $configFile -Value "`n| $cycle | $currentGen | COMPLETED | - | - | - | $timestamp |"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "ALL CYCLES COMPLETE!" -ForegroundColor Green
Write-Host "Final generation: $currentGen" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
