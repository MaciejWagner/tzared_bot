$gen = 6
$trialsPerNetwork = 2
$networks = Get-ChildItem "training/generation_$gen/onnx/*.onnx" | Sort-Object Name
$map = "training_maps/training-0.tzared"
$duration = 30

Write-Host "=== Generation $gen Training (2 trials per network) ===" -ForegroundColor Cyan
Write-Host "Networks: $($networks.Count)"
Write-Host "Map: $map"
Write-Host "Duration: ${duration}s"
Write-Host ""

$results = @()

foreach ($network in $networks) {
    $networkName = $network.BaseName
    Write-Host "[$networkName] Starting 2 trials..." -ForegroundColor Yellow

    $victories = 0
    $defeats = 0
    $durations = @()
    $actions = @()

    for ($trial = 1; $trial -le $trialsPerNetwork; $trial++) {
        $outputFile = "training/generation_$gen/results/${networkName}_trial${trial}.json"

        $output = & ./publish/TrainingRunner/TrainingRunner.exe $network.FullName $map $duration $outputFile 2>&1

        # Parse outcome
        $outcome = "UNKNOWN"
        if ($output -match "Outcome: (\w+)") {
            $outcome = $matches[1]
            if ($outcome -eq "VICTORY") { $victories++ }
            else { $defeats++ }
        }

        # Parse duration
        if ($output -match "Duration: ([\d.]+)s") {
            $durations += [double]$matches[1]
        }

        # Parse actions
        if ($output -match "Total Actions: (\d+)") {
            $actions += [int]$matches[1]
        }

        $color = if ($outcome -eq "VICTORY") { "Green" } else { "Red" }
        Write-Host "  Trial $trial : $outcome" -ForegroundColor $color
    }

    $avgDur = if ($durations.Count -gt 0) { ($durations | Measure-Object -Average).Average } else { 0 }
    $avgAct = if ($actions.Count -gt 0) { ($actions | Measure-Object -Average).Average } else { 0 }

    $results += [PSCustomObject]@{
        Network = $networkName
        V = $victories
        D = $defeats
        AvgDur = [math]::Round($avgDur, 1)
        AvgAct = [math]::Round($avgAct, 0)
    }

    Write-Host "  Summary: V=$victories D=$defeats AvgDur=$([math]::Round($avgDur, 1))s" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== FINAL RESULTS ===" -ForegroundColor Green
$results | Format-Table -AutoSize

# Save summary
$results | ConvertTo-Json | Out-File "training/generation_$gen/results/summary_2trials.json"

# Count total victories
$totalV = ($results | Measure-Object -Property V -Sum).Sum
$totalD = ($results | Measure-Object -Property D -Sum).Sum
$totalColor = if ($totalV -gt $totalD) { "Green" } else { "Yellow" }
Write-Host "Total: $totalV VICTORY / $totalD DEFEAT" -ForegroundColor $totalColor
