# Run only Phase2/NeuralNetwork tests on VM DEV
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Running Phase2/NeuralNetwork Tests" -ForegroundColor Cyan
Write-Host "  VM: DEV" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# First kill any existing dotnet processes
Write-Host "`nKilling any existing dotnet processes..." -ForegroundColor Yellow
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

Write-Host "`nRunning NeuralNetwork tests..." -ForegroundColor Green

$result = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Set-Location "C:\TzarBot"

    # Run Phase2/NeuralNetwork tests with timeout
    Write-Host "Executing: dotnet test --filter FullyQualifiedName~NeuralNetwork" -ForegroundColor Cyan

    $job = Start-Job -ScriptBlock {
        Set-Location "C:\TzarBot"
        & dotnet test TzarBot.sln --filter "FullyQualifiedName~NeuralNetwork" --verbosity normal 2>&1
    }

    # Wait max 5 minutes
    $completed = Wait-Job $job -Timeout 300

    if ($completed) {
        $output = Receive-Job $job
        $exitCode = 0
        # Check if any failures in output
        if ($output -match "Failed:") {
            $exitCode = 1
        }
    } else {
        Stop-Job $job -Force
        $output = "Test timeout after 5 minutes"
        $exitCode = -1
    }

    Remove-Job $job -Force -ErrorAction SilentlyContinue

    return @{
        ExitCode = $exitCode
        Output = $output | Out-String
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$color = switch ($result.ExitCode) {
    0 { "Green" }
    -1 { "Yellow" }
    default { "Red" }
}

Write-Host "Exit code: $($result.ExitCode)" -ForegroundColor $color
Write-Host $result.Output
