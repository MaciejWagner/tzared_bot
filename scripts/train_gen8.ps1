param(
    [int]$TrialsPerNetwork = 5,
    [int]$ParallelCount = 3,
    [int]$StaggerDelay = 3
)

$baseDir = "C:\Users\maciek\ai_experiments\tzar_bot"
$exe = "$baseDir\publish\TrainingRunner\TrainingRunner.exe"
$map = "$baseDir\training_maps\training-0b.tzared"
$genDir = "$baseDir\training\generation_8"
$duration = 30

# Get all networks
$networks = Get-ChildItem "$genDir\onnx\*.onnx" | Sort-Object Name
$totalNetworks = $networks.Count

Write-Host "=== Generation 8 Training ===" -ForegroundColor Cyan
Write-Host "Networks: $totalNetworks"
Write-Host "Trials per network: $TrialsPerNetwork"
Write-Host "Parallel sessions: $ParallelCount"
Write-Host "Map: $map"
Write-Host ""

# Create results directory
$resultsDir = "$genDir\results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$allResults = @()

# Process networks in batches
for ($netIdx = 0; $netIdx -lt $totalNetworks; $netIdx += $ParallelCount) {
    $batch = @()

    for ($p = 0; $p -lt $ParallelCount -and ($netIdx + $p) -lt $totalNetworks; $p++) {
        $network = $networks[$netIdx + $p]
        $networkName = $network.BaseName

        Write-Host "[$networkName] Starting $TrialsPerNetwork trials..." -ForegroundColor Yellow

        for ($trial = 1; $trial -le $TrialsPerNetwork; $trial++) {
            $outputPath = "$resultsDir\${networkName}_trial${trial}.json"

            $proc = Start-Process -FilePath $exe -ArgumentList "`"$($network.FullName)`"", "`"$map`"", $duration, "`"$outputPath`"" -PassThru -NoNewWindow
            $batch += @{Process=$proc; Network=$networkName; Trial=$trial; Output=$outputPath}

            if ($batch.Count -lt ($ParallelCount * $TrialsPerNetwork)) {
                Start-Sleep -Seconds $StaggerDelay
            }
        }
    }

    # Wait for batch to complete
    Write-Host "Waiting for batch ($($batch.Count) trials)..." -ForegroundColor Gray
    foreach ($item in $batch) {
        $item.Process.WaitForExit()
    }

    # Collect results
    foreach ($item in $batch) {
        if (Test-Path $item.Output) {
            $json = Get-Content $item.Output | ConvertFrom-Json
            $allResults += [PSCustomObject]@{
                Network = $item.Network
                Trial = $item.Trial
                Outcome = $json.Outcome
                Duration = [math]::Round($json.ActualDurationSeconds, 1)
                Actions = $json.TotalActions
            }

            $color = if ($json.Outcome -eq "VICTORY") { "Green" } else { "Red" }
            Write-Host "  $($item.Network) trial $($item.Trial): $($json.Outcome) ($([math]::Round($json.ActualDurationSeconds, 1))s)" -ForegroundColor $color
        }
    }
}

# Summary
Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Green

$grouped = $allResults | Group-Object Network | ForEach-Object {
    $victories = ($_.Group | Where-Object { $_.Outcome -eq "VICTORY" }).Count
    $avgDur = ($_.Group | Measure-Object -Property Duration -Average).Average
    $avgAct = ($_.Group | Measure-Object -Property Actions -Average).Average
    [PSCustomObject]@{
        Network = $_.Name
        V = $victories
        D = $TrialsPerNetwork - $victories
        AvgDur = [math]::Round($avgDur, 1)
        AvgAct = [math]::Round($avgAct, 0)
        Fitness = ($victories * 100) + $avgAct
    }
} | Sort-Object Fitness -Descending

$grouped | Format-Table -AutoSize

# Save summary
$grouped | ConvertTo-Json | Out-File "$resultsDir\summary.json"

$totalV = ($grouped | Measure-Object -Property V -Sum).Sum
$totalTrials = $totalNetworks * $TrialsPerNetwork
Write-Host "Total: $totalV VICTORY / $totalTrials trials ($([math]::Round($totalV * 100 / $totalTrials, 1))%)" -ForegroundColor $(if ($totalV -gt 0) { "Green" } else { "Yellow" })
