# Setup autologon and run tests in user session
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Write-Host "=== Setting up Auto-Logon on VM ===" -ForegroundColor Cyan

Invoke-Command -VMName DEV -Credential $cred -ScriptBlock {
    # Configure autologon
    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"

    Set-ItemProperty -Path $regPath -Name "AutoAdminLogon" -Value "1"
    Set-ItemProperty -Path $regPath -Name "DefaultUserName" -Value "test"
    Set-ItemProperty -Path $regPath -Name "DefaultPassword" -Value "password123"
    Set-ItemProperty -Path $regPath -Name "DefaultDomainName" -Value "."

    Write-Host "AutoLogon configured for user 'test'"

    # Create a batch file that will run tests
    $batchContent = @"
@echo off
cd C:\TzarBot
echo Starting tests at %date% %time% > C:\Temp\test_run.log
dotnet test tests\TzarBot.Tests\TzarBot.Tests.csproj --filter "FullyQualifiedName~ScreenCapture" --no-build >> C:\Temp\test_run.log 2>&1
echo Tests completed at %date% %time% >> C:\Temp\test_run.log
"@
    $batchContent | Out-File -FilePath "C:\TzarBot\run_screencapture_tests.bat" -Encoding ASCII

    Write-Host "Created test batch file"

    # Create scheduled task to run at logon
    $action = New-ScheduledTaskAction -Execute "C:\TzarBot\run_screencapture_tests.bat"
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User "test"
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest

    Unregister-ScheduledTask -TaskName "RunScreenCaptureTests" -Confirm:$false -ErrorAction SilentlyContinue
    Register-ScheduledTask -TaskName "RunScreenCaptureTests" -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force

    Write-Host "Created scheduled task 'RunScreenCaptureTests'"
}

Write-Host "`n=== Restarting VM ===" -ForegroundColor Yellow
Restart-VM -Name DEV -Force
Write-Host "VM is restarting. Wait ~60 seconds for autologon and tests to run."

Write-Host "`nTo check results, run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File scripts\get_screencapture_results.ps1"
