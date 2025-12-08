# Wait for tests to complete on VM DEV and get results
param(
    [int]$TimeoutMinutes = 30
)

$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

$startTime = Get-Date
$endTime = $startTime.AddMinutes($TimeoutMinutes)

Write-Host "Waiting for tests to complete on VM DEV (timeout: $TimeoutMinutes min)..." -ForegroundColor Cyan

while ((Get-Date) -lt $endTime) {
    $status = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
        $procs = Get-Process dotnet -ErrorAction SilentlyContinue
        $testLog = Get-ChildItem "C:\TzarBot-Tests\tests_*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1

        return @{
            DotnetProcesses = $procs.Count
            TestLogExists = ($null -ne $testLog)
            TestLogPath = if ($testLog) { $testLog.FullName } else { $null }
        }
    }

    if ($status.TestLogExists) {
        Write-Host "`nTests completed! Reading results..." -ForegroundColor Green

        $results = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
            param($logPath)
            Get-Content $logPath
        } -ArgumentList $status.TestLogPath

        Write-Host $results
        exit 0
    }

    $elapsed = [math]::Round(((Get-Date) - $startTime).TotalMinutes, 1)
    Write-Host "  [$elapsed min] Dotnet processes: $($status.DotnetProcesses)" -ForegroundColor Gray
    Start-Sleep -Seconds 30
}

Write-Host "Timeout reached! Checking final status..." -ForegroundColor Yellow

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== Processes ===" -ForegroundColor Cyan
    Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "PID: $($_.Id), WorkingSet: $([math]::Round($_.WorkingSet64/1MB, 1)) MB"
    }

    Write-Host "`n=== Files in C:\TzarBot-Tests ===" -ForegroundColor Cyan
    Get-ChildItem "C:\TzarBot-Tests" -ErrorAction SilentlyContinue
}
