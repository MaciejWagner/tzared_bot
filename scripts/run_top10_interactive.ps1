# Run top 10 networks in interactive session on VM DEV
# Execute this script directly on VM DEV desktop (not via PowerShell Direct)
# Usage: .\run_top10_interactive.ps1 [-DurationSeconds 60]

param(
    [int]$DurationSeconds = 60
)

# Set Playwright browsers path
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\Users\test\AppData\Local\ms-playwright"
Write-Host "PLAYWRIGHT_BROWSERS_PATH = $env:PLAYWRIGHT_BROWSERS_PATH"

$top10 = @(18, 16, 13, 14, 15, 17, 19, 12, 6, 5)

Write-Host "=== Top 10 Networks Training (Interactive Session) ===" -ForegroundColor Cyan
Write-Host "Duration per network: ${DurationSeconds}s"
Write-Host "Networks: $($top10 -join ', ')"
Write-Host "Estimated total time: $(($top10.Count) * ($DurationSeconds + 30))s (~15 min)"
Write-Host ""
Write-Host "Press any key to start..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

$results = @()

foreach ($id in $top10) {
    $modelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $id
    $outputPath = "C:\TzarBot\Results\network_{0:D2}_interactive.json" -f $id

    Write-Host ""
    Write-Host "[$($top10.IndexOf($id) + 1)/10] Training network_$("{0:D2}" -f $id)..." -ForegroundColor Yellow

    $startTime = Get-Date

    Set-Location "C:\TzarBot\TrainingRunner"
    & .\TrainingRunner.exe $modelPath "C:\TzarBot\Maps\training-0.tzared" $DurationSeconds $outputPath

    $endTime = Get-Date
    $actualDuration = [math]::Round(($endTime - $startTime).TotalSeconds, 1)

    if (Test-Path $outputPath) {
        $json = Get-Content $outputPath | ConvertFrom-Json
        $results += [PSCustomObject]@{
            NetworkId = $id
            Outcome = $json.Outcome
            Duration = [math]::Round($json.ActualDurationSeconds, 1)
            Actions = $json.TotalActions
            APS = [math]::Round($json.ActionsPerSecond, 2)
            InferenceMs = [math]::Round($json.AverageInferenceMs, 1)
        }
        Write-Host "  Result: $($json.Outcome), Actions: $($json.TotalActions), APS: $([math]::Round($json.ActionsPerSecond, 2))" -ForegroundColor Green
    }
    else {
        Write-Host "  Result file not found!" -ForegroundColor Red
    }

    Write-Host "  Completed in ${actualDuration}s"

    # Short pause between networks
    Start-Sleep -Seconds 3
}

Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize

# Save summary
$summaryPath = "C:\TzarBot\Results\top10_interactive_summary.json"
$results | ConvertTo-Json | Out-File $summaryPath -Encoding UTF8
Write-Host "Summary saved to: $summaryPath"

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
