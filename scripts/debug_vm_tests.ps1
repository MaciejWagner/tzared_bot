# Debug test status on VM DEV
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== dotnet processes ===" -ForegroundColor Cyan
    Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "PID: $($_.Id), CPU: $([math]::Round($_.CPU, 1))s, Mem: $([math]::Round($_.WorkingSet64/1MB, 1))MB, Start: $($_.StartTime)"
    }

    Write-Host "`n=== Test Results Directory ===" -ForegroundColor Cyan
    Get-ChildItem "C:\TzarBot\tests\TzarBot.Tests\TestResults" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "$($_.Name) - $($_.Length) bytes"
    }

    Write-Host "`n=== TRX files anywhere ===" -ForegroundColor Cyan
    Get-ChildItem "C:\TzarBot" -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "$($_.FullName) - $($_.Length) bytes"
    }

    Write-Host "`n=== Kill hanging dotnet and re-run ===" -ForegroundColor Yellow
    Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host "Killed all dotnet processes"
}
