# Create training task on VM (run this script, then manually start task on VM)
# This script uses Copy-VMFile which doesn't require credentials

param(
    [int]$NetworkId = 0,
    [int]$DurationSeconds = 120
)

$VMName = "DEV"

Write-Host "=== Setting up Training Task on VM ===" -ForegroundColor Cyan
Write-Host "Network ID: $NetworkId"
Write-Host "Duration: ${DurationSeconds}s"

# Create batch file that will run on VM
$modelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $NetworkId
$outputPath = "C:\TzarBot\Results\network_{0:D2}_trial.json" -f $NetworkId
$logPath = "C:\TzarBot\Logs\training_network_{0:D2}.log" -f $NetworkId

$batchContent = @"
@echo off
echo === TzarBot Training Trial ===
echo Started: %date% %time%
echo Model: $modelPath
echo Duration: $DurationSeconds seconds
echo.

cd /d C:\TzarBot\TrainingRunner
dotnet TrainingRunner.dll "$modelPath" "C:\TzarBot\Maps\training-0.tzared" $DurationSeconds "$outputPath" > "$logPath" 2>&1

echo.
echo Finished: %date% %time%
echo Exit code: %ERRORLEVEL%
pause
"@

$localBatchPath = "C:\Users\maciek\ai_experiments\tzar_bot\scripts\vm_run_training.bat"
$batchContent | Out-File $localBatchPath -Encoding ASCII

Write-Host ""
Write-Host "Batch file created: $localBatchPath" -ForegroundColor Green

# Copy to VM
Write-Host "Copying to VM..."
Copy-VMFile -Name $VMName -SourcePath $localBatchPath -DestinationPath "C:\TzarBot\run_training.bat" -FileSource Host -Force -CreateFullPath

Write-Host ""
Write-Host "=== NEXT STEPS ===" -ForegroundColor Yellow
Write-Host "1. Connect to VM DEV via RDP or Hyper-V Manager"
Write-Host "2. Log in as 'test' (password: password123)"
Write-Host "3. Run: C:\TzarBot\run_training.bat"
Write-Host ""
Write-Host "Or run from PowerShell on VM:"
Write-Host "  cd C:\TzarBot\TrainingRunner"
Write-Host "  dotnet TrainingRunner.dll C:\TzarBot\Models\generation_0\network_00.onnx C:\TzarBot\Maps\training-0.tzared 120 C:\TzarBot\Results\network_00_trial.json"
