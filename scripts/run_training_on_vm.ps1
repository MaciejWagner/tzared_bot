# Run training trial on VM DEV via Scheduled Task (for GUI access)
# Run from HOST machine

param(
    [int]$NetworkId = 0,  # 0-19
    [int]$DurationSeconds = 120,  # 2 minutes default for testing
    [int]$TimeoutSeconds = 180  # Wait up to 3 minutes
)

$ErrorActionPreference = "Stop"
$VMName = "DEV"
$taskName = "TzarBot_TrainingTrial"

Write-Host "=== Training Trial (via Scheduled Task) ===" -ForegroundColor Cyan
Write-Host "Network ID: $NetworkId"
Write-Host "Duration: ${DurationSeconds}s"
Write-Host "Timeout: ${TimeoutSeconds}s"
Write-Host ""

# VM credentials
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

# Create and run task on VM
Write-Host "Setting up scheduled task on VM..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($networkId, $duration, $taskName)

    $modelPath = "C:\TzarBot\Models\generation_0\network_{0:D2}.onnx" -f $networkId
    $mapPath = "C:\TzarBot\Maps\training-0.tzared"
    $outputPath = "C:\TzarBot\Results\network_{0:D2}_trial.json" -f $networkId
    $logPath = "C:\TzarBot\Logs\training_network_{0:D2}.log" -f $networkId

    # Check model exists
    if (-not (Test-Path $modelPath)) {
        Write-Error "Model not found: $modelPath"
        return
    }

    # Create run script
    $runScript = @"
Set-Location "C:\TzarBot\TrainingRunner"
`$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Output "=== Training Trial ===" | Out-File "$logPath"
Write-Output "Started: `$timestamp" | Out-File "$logPath" -Append
Write-Output "Model: $modelPath" | Out-File "$logPath" -Append
Write-Output "Map: $mapPath" | Out-File "$logPath" -Append
Write-Output "Duration: $duration seconds" | Out-File "$logPath" -Append
Write-Output "" | Out-File "$logPath" -Append

try {
    `$output = & ".\TrainingRunner.exe" "$modelPath" "$mapPath" $duration "$outputPath" 2>&1
    `$output | Out-File "$logPath" -Append
    Write-Output "" | Out-File "$logPath" -Append
    Write-Output "Exit code: `$LASTEXITCODE" | Out-File "$logPath" -Append
}
catch {
    Write-Output "ERROR: `$_" | Out-File "$logPath" -Append
}

`$endTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Output "Finished: `$endTime" | Out-File "$logPath" -Append
"@

    $runScriptPath = "C:\TzarBot\run_training.ps1"
    $runScript | Out-File $runScriptPath -Encoding UTF8

    # Clean up old task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Create task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $runScriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
    Write-Host "Task created: $taskName"

    # Start task
    Start-ScheduledTask -TaskName $taskName
    Write-Host "Task started!"

} -ArgumentList $NetworkId, $DurationSeconds, $taskName

# Wait for completion
Write-Host ""
Write-Host "Waiting for training to complete..." -ForegroundColor Yellow

$waited = 0
$pollInterval = 10

do {
    Start-Sleep -Seconds $pollInterval
    $waited += $pollInterval

    $status = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        param($taskName)
        $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        if ($task) { $task.State } else { "NotFound" }
    } -ArgumentList $taskName

    Write-Host "  [$waited s] Status: $status"

} while ($status -eq "Running" -and $waited -lt $TimeoutSeconds)

# Get results
Write-Host ""
Write-Host "=== Results ===" -ForegroundColor Cyan

$logContent = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($networkId)
    $logPath = "C:\TzarBot\Logs\training_network_{0:D2}.log" -f $networkId
    if (Test-Path $logPath) {
        Get-Content $logPath
    } else {
        "Log not found: $logPath"
    }
} -ArgumentList $NetworkId

Write-Host $logContent

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
} else {
    Write-Host "Result file not found on VM" -ForegroundColor Red
}

$logFile = "training_network_{0:D2}.log" -f $NetworkId
$vmLogPath = "C:\TzarBot\Logs\$logFile"
$exists = Invoke-Command -Session $session -ScriptBlock { param($p); Test-Path $p } -ArgumentList $vmLogPath
if ($exists) {
    Copy-Item -FromSession $session -Path $vmLogPath -Destination "$localResultsDir\" -Force
    Write-Host "Log saved to: $localResultsDir\$logFile" -ForegroundColor Green
}

Remove-PSSession $session

# Cleanup task
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($taskName)
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
} -ArgumentList $taskName

Write-Host ""
Write-Host "=== Training Trial Complete ===" -ForegroundColor Cyan
