# Run a single training trial on VM DEV
# Run from HOST machine

param(
    [Parameter(Mandatory=$true)]
    [int]$NetworkId,  # 0-19

    [int]$DurationSeconds = 300,  # 5 minutes default

    [string]$MapPath = "C:\TzarBot\Maps\training-0.tzared"
)

$ErrorActionPreference = "Stop"

$VMName = "DEV"
$ModelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $NetworkId
$OutputPath = "C:\TzarBot\Results\network_{0:D2}_trial.json" -f $NetworkId

Write-Host "=== Training Trial ===" -ForegroundColor Cyan
Write-Host "Network ID: $NetworkId"
Write-Host "Model: $ModelPath"
Write-Host "Map: $MapPath"
Write-Host "Duration: ${DurationSeconds}s"
Write-Host ""

# VM credentials
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

# Check if model exists
$modelExists = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($path)
    Test-Path $path
} -ArgumentList $ModelPath

if (-not $modelExists) {
    Write-Error "Model not found on VM: $ModelPath"
    Write-Host "Run deploy_training_runner.ps1 first to copy models to VM."
    exit 1
}

# Run training
Write-Host "Starting training on VM DEV..." -ForegroundColor Yellow
Write-Host ""

$result = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($modelPath, $mapPath, $duration, $outputPath)

    Set-Location "C:\TzarBot\TrainingRunner"

    $startTime = Get-Date
    Write-Host "Starting at: $startTime"

    # Run TrainingRunner
    $output = & ".\TrainingRunner.exe" $modelPath $mapPath $duration $outputPath 2>&1
    $exitCode = $LASTEXITCODE

    $endTime = Get-Date
    Write-Host "Finished at: $endTime"
    Write-Host "Exit code: $exitCode"
    Write-Host ""
    Write-Host "=== Output ==="
    Write-Host $output

    # Return results
    @{
        ExitCode = $exitCode
        Output = $output
        StartTime = $startTime
        EndTime = $endTime
        Duration = ($endTime - $startTime).TotalSeconds
    }
} -ArgumentList $ModelPath, $MapPath, $DurationSeconds, $OutputPath

Write-Host ""
Write-Host "=== Trial Complete ===" -ForegroundColor Green
Write-Host "Duration: $($result.Duration)s"
Write-Host "Exit code: $($result.ExitCode)"

# Copy results back to host
$localResultsDir = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_0\results"
if (-not (Test-Path $localResultsDir)) {
    New-Item -ItemType Directory -Force -Path $localResultsDir | Out-Null
}

$session = New-PSSession -VMName $VMName -Credential $cred
$resultFile = "network_{0:D2}_trial.json" -f $NetworkId
if (Invoke-Command -Session $session -ScriptBlock { Test-Path "C:\TzarBot\Results\$using:resultFile" }) {
    Copy-Item -FromSession $session -Path "C:\TzarBot\Results\$resultFile" -Destination "$localResultsDir\" -Force
    Write-Host "Results copied to: $localResultsDir\$resultFile" -ForegroundColor Cyan
}
Remove-PSSession $session
