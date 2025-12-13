# Run batch training trials for multiple networks
# Usage: run_batch_training.ps1 -StartId 0 -EndId 4 -DurationSeconds 60

param(
    [int]$StartId = 0,
    [int]$EndId = 4,
    [int]$DurationSeconds = 60
)

$VMName = "DEV"
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

$localResultsDir = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_0\results"
New-Item -ItemType Directory -Force -Path $localResultsDir -ErrorAction SilentlyContinue | Out-Null

Write-Host "=== Batch Training ($StartId to $EndId) ===" -ForegroundColor Cyan
Write-Host "Duration per network: ${DurationSeconds}s"
Write-Host "Estimated total time: $(($EndId - $StartId + 1) * ($DurationSeconds + 20))s"
Write-Host ""

$results = @()

for ($i = $StartId; $i -le $EndId; $i++) {
    $modelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $i
    $outputPath = "C:\TzarBot\Results\network_{0:D2}_trial.json" -f $i

    Write-Host "[$i/$EndId] Training network_$("{0:D2}" -f $i)..." -ForegroundColor Yellow
    $startTime = Get-Date

    $output = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        param($modelPath, $mapPath, $duration, $outputPath)

        Set-Location "C:\TzarBot\TrainingRunner"
        & .\TrainingRunner.exe $modelPath $mapPath $duration $outputPath 2>&1
        $LASTEXITCODE

    } -ArgumentList $modelPath, "C:\TzarBot\Maps\training-0.tzared", $DurationSeconds, $outputPath

    $endTime = Get-Date
    $actualDuration = [math]::Round(($endTime - $startTime).TotalSeconds, 1)

    # Copy results
    $session = New-PSSession -VMName $VMName -Credential $cred
    $resultFile = "network_{0:D2}_trial.json" -f $i
    $vmResultPath = "C:\TzarBot\Results\$resultFile"

    $exists = Invoke-Command -Session $session -ScriptBlock { param($p); Test-Path $p } -ArgumentList $vmResultPath
    if ($exists) {
        Copy-Item -FromSession $session -Path $vmResultPath -Destination "$localResultsDir\" -Force

        # Parse JSON
        $json = Get-Content "$localResultsDir\$resultFile" | ConvertFrom-Json
        $results += [PSCustomObject]@{
            NetworkId = $i
            Outcome = $json.Outcome
            Duration = $json.ActualDurationSeconds
            Actions = $json.TotalActions
            APS = [math]::Round($json.ActionsPerSecond, 2)
            InferenceMs = [math]::Round($json.AverageInferenceMs, 1)
        }

        Write-Host "  Result: $($json.Outcome), Actions: $($json.TotalActions), APS: $([math]::Round($json.ActionsPerSecond, 2))" -ForegroundColor Gray
    }
    else {
        Write-Host "  Result file not found!" -ForegroundColor Red
    }
    Remove-PSSession $session

    Write-Host "  Completed in ${actualDuration}s"
    Write-Host ""
}

# Summary
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize

# Save summary
$summaryPath = "$localResultsDir\batch_summary.json"
$results | ConvertTo-Json | Out-File $summaryPath
Write-Host "Summary saved to: $summaryPath"
