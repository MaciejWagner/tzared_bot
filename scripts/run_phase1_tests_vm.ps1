# Run only Phase1 tests on VM DEV (avoid heavy NN tests)
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Running Phase1 Tests on VM DEV" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# First kill any existing dotnet processes
Write-Host "`nKilling any existing dotnet processes..." -ForegroundColor Yellow
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

Write-Host "`nRunning Phase1 tests..." -ForegroundColor Green

$result = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Set-Location "C:\TzarBot"

    # Run only Phase1 tests
    Write-Host "Executing: dotnet test --filter FullyQualifiedName~Phase1" -ForegroundColor Cyan
    $output = & dotnet test TzarBot.sln --filter "FullyQualifiedName~Phase1" --verbosity normal 2>&1
    $exitCode = $LASTEXITCODE

    return @{
        ExitCode = $exitCode
        Output = $output | Out-String
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Exit code: $($result.ExitCode)" -ForegroundColor $(if ($result.ExitCode -eq 0) { "Green" } else { "Red" })
Write-Host $result.Output
