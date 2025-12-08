<#
.SYNOPSIS
    Deploy project and run all tests on VM DEV.

.DESCRIPTION
    This script:
    1. Creates ZIP archives of src, tests, scripts
    2. Copies them to VM DEV via PowerShell Direct
    3. Extracts and runs dotnet test
    4. Returns results

.PARAMETER Category
    Optional test category filter (Phase1, Phase2, All)

.PARAMETER OutputDir
    Output directory on VM for test results
#>

param(
    [string]$Category = "All",
    [string]$OutputDir = "C:\TzarBot-Tests"
)

$ErrorActionPreference = "Stop"
$basePath = "C:\Users\maciek\ai_experiments\tzar_bot"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TzarBot Remote Test Runner" -ForegroundColor Cyan
Write-Host "  VM: DEV" -ForegroundColor Cyan
Write-Host "  Category: $Category" -ForegroundColor Cyan
Write-Host "  Time: $timestamp" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Create ZIP archives
Write-Host "`n[1/5] Creating ZIP archives..." -ForegroundColor Yellow
Remove-Item "$basePath\src.zip" -Force -ErrorAction SilentlyContinue
Remove-Item "$basePath\tests.zip" -Force -ErrorAction SilentlyContinue

Compress-Archive -Path "$basePath\src\*" -DestinationPath "$basePath\src.zip" -Force
Compress-Archive -Path "$basePath\tests\*" -DestinationPath "$basePath\tests.zip" -Force
Write-Host "  ZIP archives created" -ForegroundColor Green

# Step 2: Clean VM directory and copy files
Write-Host "`n[2/5] Preparing VM directory..." -ForegroundColor Yellow
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($OutputDir)
    Remove-Item "C:\TzarBot" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path "C:\TzarBot" -Force | Out-Null
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "  VM directories prepared"
} -ArgumentList $OutputDir

# Step 3: Copy files to VM
Write-Host "`n[3/5] Copying files to VM..." -ForegroundColor Yellow
Copy-VMFile -Name "DEV" -SourcePath "$basePath\src.zip" -DestinationPath "C:\TzarBot\src.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name "DEV" -SourcePath "$basePath\tests.zip" -DestinationPath "C:\TzarBot\tests.zip" -CreateFullPath -FileSource Host -Force
Copy-VMFile -Name "DEV" -SourcePath "$basePath\TzarBot.sln" -DestinationPath "C:\TzarBot\TzarBot.sln" -CreateFullPath -FileSource Host -Force
Write-Host "  Files copied to VM" -ForegroundColor Green

# Step 4: Extract and build on VM
Write-Host "`n[4/5] Building on VM..." -ForegroundColor Yellow
$buildResult = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($OutputDir, $timestamp)

    Set-Location "C:\TzarBot"

    # Extract archives
    Expand-Archive -Path "C:\TzarBot\src.zip" -DestinationPath "C:\TzarBot\src" -Force
    Expand-Archive -Path "C:\TzarBot\tests.zip" -DestinationPath "C:\TzarBot\tests" -Force
    Remove-Item "C:\TzarBot\*.zip" -Force

    # Build
    $buildLog = Join-Path $OutputDir "build_$timestamp.log"
    $buildOutput = dotnet build "C:\TzarBot\TzarBot.sln" --configuration Debug 2>&1
    $buildOutput | Out-File -FilePath $buildLog -Encoding UTF8

    $buildSuccess = $LASTEXITCODE -eq 0

    return @{
        Success = $buildSuccess
        LogFile = $buildLog
        Output = $buildOutput | Select-Object -Last 20 | Out-String
    }
} -ArgumentList $OutputDir, $timestamp

if ($buildResult.Success) {
    Write-Host "  Build successful" -ForegroundColor Green
} else {
    Write-Host "  Build failed! See log: $($buildResult.LogFile)" -ForegroundColor Red
    Write-Host $buildResult.Output
    exit 1
}

# Step 5: Run tests on VM
Write-Host "`n[5/5] Running tests on VM..." -ForegroundColor Yellow
$testResult = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($Category, $OutputDir, $timestamp)

    Set-Location "C:\TzarBot"

    $testLog = Join-Path $OutputDir "tests_$timestamp.log"
    $testArgs = @(
        "test"
        "C:\TzarBot\TzarBot.sln"
        "--no-build"
        "--verbosity", "normal"
        "--logger", "trx;LogFileName=TestResults_$timestamp.trx"
    )

    if ($Category -ne "All") {
        $filter = switch ($Category) {
            "Phase1" { "FullyQualifiedName~Phase1" }
            "Phase2" { "FullyQualifiedName~Phase2|FullyQualifiedName~NeuralNetwork" }
            default { $null }
        }
        if ($filter) {
            $testArgs += "--filter", $filter
        }
    }

    Write-Host "Running: dotnet $($testArgs -join ' ')"
    $testOutput = & dotnet @testArgs 2>&1
    $testExitCode = $LASTEXITCODE

    $testOutput | Out-File -FilePath $testLog -Encoding UTF8

    # Parse results
    $passedMatch = $testOutput | Select-String "Passed:\s*(\d+)"
    $failedMatch = $testOutput | Select-String "Failed:\s*(\d+)"
    $skippedMatch = $testOutput | Select-String "Skipped:\s*(\d+)"
    $totalMatch = $testOutput | Select-String "Total:\s*(\d+)"

    $passed = if ($passedMatch) { [int]$passedMatch.Matches[0].Groups[1].Value } else { 0 }
    $failed = if ($failedMatch) { [int]$failedMatch.Matches[0].Groups[1].Value } else { 0 }
    $skipped = if ($skippedMatch) { [int]$skippedMatch.Matches[0].Groups[1].Value } else { 0 }
    $total = if ($totalMatch) { [int]$totalMatch.Matches[0].Groups[1].Value } else { $passed + $failed + $skipped }

    return @{
        Success = ($testExitCode -eq 0)
        ExitCode = $testExitCode
        LogFile = $testLog
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        Total = $total
        Output = $testOutput | Out-String
        TrxFile = "TestResults\TestResults_$timestamp.trx"
    }
} -ArgumentList $Category, $OutputDir, $timestamp

# Display results
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  TEST RESULTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n| Metric | Value |" -ForegroundColor White
Write-Host "|--------|-------|" -ForegroundColor White
Write-Host "| Total  | $($testResult.Total) |"
Write-Host "| Passed | $($testResult.Passed) |" -ForegroundColor Green
Write-Host "| Failed | $($testResult.Failed) |" -ForegroundColor $(if ($testResult.Failed -gt 0) { "Red" } else { "Green" })
Write-Host "| Skipped| $($testResult.Skipped) |" -ForegroundColor Yellow

if ($testResult.Success) {
    Write-Host "`nALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "`nSOME TESTS FAILED" -ForegroundColor Red
    Write-Host "`nFailed test output:" -ForegroundColor Yellow
    $testResult.Output -split "`n" | Where-Object { $_ -match "Failed|Error|Exception" } | ForEach-Object { Write-Host $_ -ForegroundColor Red }
}

Write-Host "`nLog files on VM:" -ForegroundColor Cyan
Write-Host "  Build: $($buildResult.LogFile)"
Write-Host "  Tests: $($testResult.LogFile)"

# Return results
return @{
    BuildSuccess = $buildResult.Success
    TestSuccess = $testResult.Success
    Passed = $testResult.Passed
    Failed = $testResult.Failed
    Skipped = $testResult.Skipped
    Total = $testResult.Total
    BuildLog = $buildResult.LogFile
    TestLog = $testResult.LogFile
    Timestamp = $timestamp
}
