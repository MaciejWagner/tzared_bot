# Train a generation of networks
# Usage: .\train_generation.ps1 -Generation 1 -Duration 60

param(
    [Parameter(Mandatory=$true)]
    [int]$Generation,

    [int]$Duration = 60,
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

Write-Host "=== TzarBot Generation $Generation Training ===" -ForegroundColor Cyan
Write-Host "Duration: ${Duration}s per network"
Write-Host "Map: $mapPath"
Write-Host "Networks: $StartNetwork to $EndNetwork"
Write-Host ""

$results = @()
$startTime = Get-Date

for ($i = $StartNetwork; $i -le $EndNetwork; $i++) {
    $networkFile = Join-Path $onnxDir ("network_{0:D2}.onnx" -f $i)
    $outputFile = Join-Path $resultsDir ("network_{0:D2}_trial.json" -f $i)

    if (-not (Test-Path $networkFile)) {
        Write-Host "[$i] SKIP - Network not found: $networkFile" -ForegroundColor Yellow
        continue
    }

    Write-Host "[$i] Training network_{0:D2}..." -f $i -NoNewline

    $trialStart = Get-Date
    try {
        & $trainingRunner $networkFile $mapPath $Duration $outputFile 2>&1 | Out-Null
        $trialEnd = Get-Date
        $trialDuration = ($trialEnd - $trialStart).TotalSeconds

        if (Test-Path $outputFile) {
            $result = Get-Content $outputFile | ConvertFrom-Json
            Write-Host " OK - Actions: $($result.TotalActions), APS: $([math]::Round($result.ActionsPerSecond, 2)), Time: $([math]::Round($trialDuration, 1))s" -ForegroundColor Green
            $results += [PSCustomObject]@{
                NetworkId = $i
                Outcome = $result.Outcome
                Duration = $result.ActualDurationSeconds
                Actions = $result.TotalActions
                APS = [math]::Round($result.ActionsPerSecond, 2)
                InferenceMs = [math]::Round($result.AverageInferenceMs, 1)
            }
        } else {
            Write-Host " FAIL - No output file" -ForegroundColor Red
        }
    } catch {
        Write-Host " ERROR - $($_.Exception.Message)" -ForegroundColor Red
    }
}

$endTime = Get-Date
$totalDuration = ($endTime - $startTime).TotalMinutes

# Save summary
$summaryFile = Join-Path $resultsDir "batch_summary.json"
$results | ConvertTo-Json -Depth 10 | Set-Content $summaryFile

Write-Host ""
Write-Host "=== Training Complete ===" -ForegroundColor Cyan
Write-Host "Networks trained: $($results.Count)"
Write-Host "Total time: $([math]::Round($totalDuration, 1)) minutes"
Write-Host "Summary saved: $summaryFile"

# Show ranking
Write-Host ""
Write-Host "=== Top 10 Performers ===" -ForegroundColor Yellow
$results | Sort-Object -Property Actions -Descending | Select-Object -First 10 | Format-Table NetworkId, Actions, APS, InferenceMs
