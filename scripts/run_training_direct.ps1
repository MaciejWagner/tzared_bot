# Run training trial directly via Invoke-Command (not Scheduled Task)
# This provides better output capture but requires user test to be logged in

param(
    [int]$NetworkId = 0,
    [int]$DurationSeconds = 60
)

$VMName = "DEV"
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

$modelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $NetworkId
$outputPath = "C:\TzarBot\Results\network_{0:D2}_trial.json" -f $NetworkId

Write-Host "=== Training Trial (Direct) ===" -ForegroundColor Cyan
Write-Host "Network ID: $NetworkId"
Write-Host "Model: $modelPath"
Write-Host "Duration: ${DurationSeconds}s"
Write-Host ""

$result = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($modelPath, $mapPath, $duration, $outputPath)

    Set-Location "C:\TzarBot\TrainingRunner"

    Write-Host "Starting TrainingRunner..."
    $startTime = Get-Date

    # Run with output capture
    $output = & .\TrainingRunner.exe $modelPath $mapPath $duration $outputPath 2>&1
    $exitCode = $LASTEXITCODE

    $endTime = Get-Date

    # Output everything
    $output | ForEach-Object { Write-Host $_ }

    Write-Host ""
    Write-Host "Exit code: $exitCode"
    Write-Host "Actual duration: $([math]::Round(($endTime - $startTime).TotalSeconds, 1))s"

} -ArgumentList $modelPath, "C:\TzarBot\Maps\training-0.tzared", $DurationSeconds, $outputPath

# Copy results to host
Write-Host ""
Write-Host "Copying results to host..." -ForegroundColor Yellow

$localResultsDir = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_0\results"
New-Item -ItemType Directory -Force -Path $localResultsDir -ErrorAction SilentlyContinue | Out-Null

$session = New-PSSession -VMName $VMName -Credential $cred
$resultFile = "network_{0:D2}_trial.json" -f $NetworkId
$vmResultPath = "C:\TzarBot\Results\$resultFile"

$exists = Invoke-Command -Session $session -ScriptBlock { param($p); Test-Path $p } -ArgumentList $vmResultPath
if ($exists) {
    Copy-Item -FromSession $session -Path $vmResultPath -Destination "$localResultsDir\" -Force
    Write-Host "Results saved to: $localResultsDir\$resultFile" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Result Content ===" -ForegroundColor Cyan
    Get-Content "$localResultsDir\$resultFile"
}
else {
    Write-Host "Result file not found on VM: $vmResultPath" -ForegroundColor Red
}

Remove-PSSession $session

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
