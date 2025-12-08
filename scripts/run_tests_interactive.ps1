# Run tests in user's interactive session on VM DEV
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "=== Running tests in interactive session ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    # Create a scheduled task to run in the user's session
    $taskName = "TzarBotTest_Temp"

    # Remove existing task if any
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument @"
-ExecutionPolicy Bypass -Command "Set-Location 'C:\TzarBot'; dotnet test TzarBot.sln --filter 'FullyQualifiedName~ScreenCaptureTests' --verbosity normal 2>&1 | Out-File 'C:\TzarBot-Tests\interactive_test.log' -Encoding UTF8; Write-Output 'DONE' | Out-File 'C:\TzarBot-Tests\test_complete.flag'"
"@

    # Run as the logged-in user (test)
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest

    # Create and run the task
    $task = New-ScheduledTask -Action $action -Principal $principal
    Register-ScheduledTask -TaskName $taskName -InputObject $task | Out-Null

    Write-Host "Starting task in user session..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Wait for completion (max 2 minutes)
    $timeout = 120
    $elapsed = 0

    while ($elapsed -lt $timeout) {
        Start-Sleep -Seconds 5
        $elapsed += 5

        if (Test-Path "C:\TzarBot-Tests\test_complete.flag") {
            Write-Host "Test completed!" -ForegroundColor Green
            break
        }

        $taskInfo = Get-ScheduledTaskInfo -TaskName $taskName
        if ($taskInfo.LastTaskResult -ne 267009) { # 267009 = task is running
            if ($taskInfo.LastTaskResult -ne 0) {
                Write-Host "Task finished with code: $($taskInfo.LastTaskResult)"
                break
            }
        }

        Write-Host "  Waiting... ($elapsed s)"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item "C:\TzarBot-Tests\test_complete.flag" -Force -ErrorAction SilentlyContinue

    # Return results
    if (Test-Path "C:\TzarBot-Tests\interactive_test.log") {
        Write-Host "`n=== TEST RESULTS ===" -ForegroundColor Cyan
        Get-Content "C:\TzarBot-Tests\interactive_test.log" -Tail 50
    } else {
        Write-Host "No test output found!" -ForegroundColor Red
    }
}
