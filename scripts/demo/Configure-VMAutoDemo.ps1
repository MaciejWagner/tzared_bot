#
# Configure VM for automatic demo execution with screenshots
# This script sets up AutoLogon and a scheduled task to run demos on VM startup
#

param(
    [string]$VMName = "DEV",
    [string]$VMUsername = "test",
    [string]$VMPassword = "password123"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Configuring VM $VMName for automatic demo execution ===" -ForegroundColor Cyan

# Create credential
$secPassword = ConvertTo-SecureString $VMPassword -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($VMUsername, $secPassword)

# Configure AutoLogon and scheduled task on VM
Write-Host "Configuring AutoLogon and scheduled task..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($username, $password)

    # Set AutoLogon in registry
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    Set-ItemProperty -Path $regPath -Name "AutoAdminLogon" -Value "1"
    Set-ItemProperty -Path $regPath -Name "DefaultUserName" -Value $username
    Set-ItemProperty -Path $regPath -Name "DefaultPassword" -Value $password
    Set-ItemProperty -Path $regPath -Name "DefaultDomainName" -Value $env:COMPUTERNAME

    Write-Host "AutoLogon configured for user: $username" -ForegroundColor Green

    # Create demo runner script
    $demoScript = @'
# TzarBot Demo Runner - executed at logon
$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$OutputDir = "C:\TzarBot-Demo\AutoRun_$timestamp"

# Wait for desktop to fully load
Start-Sleep -Seconds 10

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Log start
$logFile = Join-Path $OutputDir "autorun.log"
"Demo started at $(Get-Date)" | Out-File $logFile

# Run Phase 0 demo
try {
    & "C:\TzarBot\scripts\demo\Run-Phase0Demo.ps1" -OutputDir (Join-Path $OutputDir "Phase0")
    "Phase 0 completed" | Out-File $logFile -Append
} catch {
    "Phase 0 error: $_" | Out-File $logFile -Append
}

# Run Phase 1 demo
try {
    & "C:\TzarBot\scripts\demo\Run-Phase1Demo.ps1" -OutputDir (Join-Path $OutputDir "Phase1") -ProjectPath "C:\TzarBot"
    "Phase 1 completed" | Out-File $logFile -Append
} catch {
    "Phase 1 error: $_" | Out-File $logFile -Append
}

# Mark completion
"Demo completed at $(Get-Date)" | Out-File $logFile -Append

# Create marker file for host to detect completion
"COMPLETED" | Out-File "C:\TzarBot-Demo\DEMO_COMPLETE.flag"
'@

    $scriptPath = "C:\TzarBot\scripts\demo\AutoRunDemo.ps1"
    New-Item -ItemType Directory -Path "C:\TzarBot\scripts\demo" -Force | Out-Null
    Set-Content -Path $scriptPath -Value $demoScript -Encoding UTF8
    Write-Host "Demo script created at $scriptPath" -ForegroundColor Green

    # Create scheduled task to run at logon
    $taskName = "TzarBot-AutoDemo"

    # Remove existing task if present
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -WindowStyle Normal -File `"$scriptPath`""
    $trigger = New-ScheduledTaskTrigger -AtLogon -User $username
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    $principal = New-ScheduledTaskPrincipal -UserId $username -LogonType Interactive -RunLevel Highest

    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force

    Write-Host "Scheduled task '$taskName' created" -ForegroundColor Green

} -ArgumentList $VMUsername, $VMPassword

Write-Host "`n=== Configuration complete ===" -ForegroundColor Green
Write-Host @"

Next steps:
1. Restart the VM to trigger AutoLogon and demo execution:
   Restart-VM -Name $VMName -Force

2. Wait for demo to complete (check for C:\TzarBot-Demo\DEMO_COMPLETE.flag)

3. Copy results from VM:
   powershell -File scripts\demo\Copy-DemoResults.ps1

"@
