# Check test status on VM DEV
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

try {
    Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
        Write-Host "=== Test Result Files ===" -ForegroundColor Cyan
        if (Test-Path "C:\TzarBot-Tests") {
            Get-ChildItem "C:\TzarBot-Tests" -File | ForEach-Object {
                Write-Host "$($_.Name) - $($_.LastWriteTime) - $($_.Length) bytes"
            }
        } else {
            Write-Host "C:\TzarBot-Tests does not exist"
        }

        Write-Host "`n=== Running dotnet processes ===" -ForegroundColor Cyan
        $procs = Get-Process dotnet -ErrorAction SilentlyContinue
        if ($procs) {
            $procs | ForEach-Object { Write-Host "PID: $($_.Id), Started: $($_.StartTime)" }
        } else {
            Write-Host "No dotnet processes running"
        }

        Write-Host "`n=== Last Test Log (if exists) ===" -ForegroundColor Cyan
        $latestLog = Get-ChildItem "C:\TzarBot-Tests\tests_*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestLog) {
            Write-Host "File: $($latestLog.Name)"
            Write-Host "--- Last 50 lines ---"
            Get-Content $latestLog.FullName -Tail 50
        } else {
            Write-Host "No test logs found"
        }
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
