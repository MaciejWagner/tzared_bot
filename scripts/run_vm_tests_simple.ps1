# Simple test runner on VM DEV - runs directly with output
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "Running tests on VM DEV..." -ForegroundColor Cyan
Write-Host "This may take 5-10 minutes." -ForegroundColor Yellow

$result = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Set-Location "C:\TzarBot"

    # Run tests with timeout
    Write-Host "Starting dotnet test..." -ForegroundColor Green
    $output = & dotnet test TzarBot.sln --verbosity normal 2>&1
    $exitCode = $LASTEXITCODE

    # Save output
    $output | Out-File "C:\TzarBot-Tests\test_output.log" -Encoding UTF8

    # Return result
    return @{
        ExitCode = $exitCode
        Output = $output | Out-String
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Exit code: $($result.ExitCode)"
Write-Host $result.Output
