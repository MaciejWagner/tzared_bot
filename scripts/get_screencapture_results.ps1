# Get ScreenCapture test results from VM
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Write-Host "=== ScreenCapture Test Results ===" -ForegroundColor Cyan

Invoke-Command -VMName DEV -Credential $cred -ScriptBlock {
    # Check if user is logged in
    Write-Host "`n=== Session Info ===" -ForegroundColor Yellow
    $sessions = quser 2>&1
    Write-Host $sessions

    Write-Host "`n=== Test Log ===" -ForegroundColor Yellow
    if (Test-Path "C:\Temp\test_run.log") {
        Get-Content "C:\Temp\test_run.log"
    } else {
        Write-Host "Test log not found yet. Tests may still be running."
    }

    Write-Host "`n=== Current Session ProcessId ===" -ForegroundColor Yellow
    $proc = Get-Process -Id $PID
    Write-Host "SessionId: $($proc.SessionId)"
}
