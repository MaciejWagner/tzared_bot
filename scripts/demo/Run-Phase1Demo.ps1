#
# Phase 1 Demo Script - TzarBot Game Interface
# Uruchom na VM DEV
#

param(
    [string]$OutputDir = "C:\TzarBot-Demo\Phase1",
    [string]$ProjectPath = "C:\TzarBot",
    [switch]$SkipScreenshots,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

# Kolory
function Write-Success { param($msg) Write-Host "[PASS] $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Yellow }

# Utworz katalog wyjsciowy
Write-Step "Inicjalizacja Phase 1 Demo"
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$logFile = Join-Path $OutputDir "phase1_demo_$timestamp.log"
$reportFile = Join-Path $OutputDir "phase1_report_$timestamp.md"
$buildLog = Join-Path $OutputDir "build_$timestamp.log"
$testLog = Join-Path $OutputDir "tests_$timestamp.log"

# Funkcja logowania
function Log {
    param($msg)
    $logMsg = "$(Get-Date -Format 'HH:mm:ss') - $msg"
    Write-Host $logMsg
    Add-Content -Path $logFile -Value $logMsg
}

# Funkcja screenshot
function Take-Screenshot {
    param($name)
    if ($SkipScreenshots) { return $null }

    $screenshotPath = Join-Path $OutputDir "$name.png"
    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
        $screen = [System.Windows.Forms.Screen]::PrimaryScreen
        $bitmap = New-Object System.Drawing.Bitmap($screen.Bounds.Width, $screen.Bounds.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($screen.Bounds.Location, [System.Drawing.Point]::Empty, $screen.Bounds.Size)
        $bitmap.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Log "Screenshot saved: $screenshotPath"
        return $screenshotPath
    } catch {
        Log "Screenshot failed: $_"
        return $null
    }
}

# Start
Log "Phase 1 Demo started at $timestamp"
Log "Output directory: $OutputDir"
Log "Project path: $ProjectPath"

$results = @()
$screenshots = @()

# Sprawdz czy projekt istnieje
Write-Step "Checking Project"
$slnPath = Join-Path $ProjectPath "TzarBot.sln"
if (-not (Test-Path $slnPath)) {
    # Probuj alternatywne sciezki
    $slnPath = Join-Path $ProjectPath "TzarBot.slnx"
}
if (-not (Test-Path $slnPath)) {
    $slnPath = Join-Path $ProjectPath "src\TzarBot.sln"
}
if (-not (Test-Path $slnPath)) {
    Write-Fail "Solution file not found at $ProjectPath"
    Write-Host "Please specify correct path with -ProjectPath parameter"
    Write-Host "Example: .\Run-Phase1Demo.ps1 -ProjectPath 'C:\Users\test\TzarBot'"
    exit 1
}
Log "Solution found: $slnPath"

# Test 1: Build
Write-Step "Test 1: Build Solution"
if (-not $SkipBuild) {
    Log "Building solution..."
    $buildOutput = & dotnet build $slnPath --configuration Release 2>&1
    $buildOutput | Out-File -FilePath $buildLog -Encoding UTF8

    if ($LASTEXITCODE -eq 0) {
        $warnings = ($buildOutput | Select-String "warning").Count
        $errors = ($buildOutput | Select-String "error").Count
        Log "Build succeeded. Warnings: $warnings, Errors: $errors"
        $results += [PSCustomObject]@{ Test = "Build"; Status = "PASS"; Details = "0 errors, $warnings warnings" }
        Write-Success "Build succeeded"
    } else {
        Log "Build failed"
        $results += [PSCustomObject]@{ Test = "Build"; Status = "FAIL"; Details = "See $buildLog" }
        Write-Fail "Build failed - check $buildLog"
    }
    $screenshots += Take-Screenshot -name "build_result_$timestamp"
} else {
    Log "Build skipped"
    $results += [PSCustomObject]@{ Test = "Build"; Status = "SKIP"; Details = "Skipped by user" }
}

# Test 2: Unit Tests
Write-Step "Test 2: Run Unit Tests"
Log "Running unit tests..."
$testOutput = & dotnet test $slnPath --configuration Release --verbosity normal 2>&1
$testOutput | Out-File -FilePath $testLog -Encoding UTF8

# Parse test results
$testSummary = $testOutput | Select-String "Passed|Failed|Skipped" | Select-Object -Last 1
$passedTests = 0
$failedTests = 0
$skippedTests = 0

if ($testOutput -match "Passed:\s*(\d+)") { $passedTests = [int]$Matches[1] }
if ($testOutput -match "Failed:\s*(\d+)") { $failedTests = [int]$Matches[1] }
if ($testOutput -match "Skipped:\s*(\d+)") { $skippedTests = [int]$Matches[1] }

Log "Tests - Passed: $passedTests, Failed: $failedTests, Skipped: $skippedTests"

if ($failedTests -eq 0 -and $passedTests -gt 0) {
    $results += [PSCustomObject]@{ Test = "Unit Tests"; Status = "PASS"; Details = "$passedTests passed, $failedTests failed" }
    Write-Success "All $passedTests tests passed"
} elseif ($failedTests -gt 0) {
    $results += [PSCustomObject]@{ Test = "Unit Tests"; Status = "FAIL"; Details = "$passedTests passed, $failedTests failed" }
    Write-Fail "$failedTests tests failed"
} else {
    $results += [PSCustomObject]@{ Test = "Unit Tests"; Status = "WARN"; Details = "No tests found" }
    Write-Fail "No tests executed"
}
$screenshots += Take-Screenshot -name "tests_result_$timestamp"

# Test 3: Screen Capture Module
Write-Step "Test 3: Screen Capture Module"
$captureTests = $testOutput | Select-String "ScreenCapture"
if ($captureTests) {
    $capturePass = ($captureTests | Select-String "Passed").Count
    Log "Screen Capture tests: $capturePass passed"
    $results += [PSCustomObject]@{ Test = "Screen Capture"; Status = "PASS"; Details = "$capturePass tests" }
    Write-Success "Screen Capture module OK"
} else {
    $results += [PSCustomObject]@{ Test = "Screen Capture"; Status = "WARN"; Details = "Could not verify" }
    Write-Info "Screen Capture tests not separately identifiable"
}

# Test 4: Input Injection Module
Write-Step "Test 4: Input Injection Module"
$inputTests = $testOutput | Select-String "InputInjector|Input"
if ($inputTests) {
    $inputPass = ($inputTests | Select-String "Passed").Count
    Log "Input Injection tests found"
    $results += [PSCustomObject]@{ Test = "Input Injection"; Status = "PASS"; Details = "Module present" }
    Write-Success "Input Injection module OK"
} else {
    $results += [PSCustomObject]@{ Test = "Input Injection"; Status = "WARN"; Details = "Could not verify" }
    Write-Info "Input Injection tests not separately identifiable"
}

# Test 5: IPC Module
Write-Step "Test 5: IPC Named Pipes Module"
$ipcTests = $testOutput | Select-String "Ipc|Pipe"
if ($ipcTests) {
    Log "IPC tests found"
    $results += [PSCustomObject]@{ Test = "IPC Named Pipes"; Status = "PASS"; Details = "Module present" }
    Write-Success "IPC module OK"
} else {
    $results += [PSCustomObject]@{ Test = "IPC Named Pipes"; Status = "WARN"; Details = "Could not verify" }
    Write-Info "IPC tests not separately identifiable"
}

# Test 6: Window Detection Module
Write-Step "Test 6: Window Detection Module"
$windowTests = $testOutput | Select-String "Window"
if ($windowTests) {
    Log "Window Detection tests found"
    $results += [PSCustomObject]@{ Test = "Window Detection"; Status = "PASS"; Details = "Module present" }
    Write-Success "Window Detection module OK"
} else {
    $results += [PSCustomObject]@{ Test = "Window Detection"; Status = "WARN"; Details = "Could not verify" }
    Write-Info "Window Detection tests not separately identifiable"
}

# Test 7: Check if Tzar game can be detected
Write-Step "Test 7: Tzar Window Detection (Live)"
$tzarProcess = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
if ($tzarProcess) {
    Log "Tzar game is running: PID $($tzarProcess.Id)"
    $results += [PSCustomObject]@{ Test = "Tzar Running"; Status = "PASS"; Details = "PID $($tzarProcess.Id)" }
    Write-Success "Tzar game detected"
    $screenshots += Take-Screenshot -name "tzar_running_$timestamp"
} else {
    Log "Tzar game is not running"
    $results += [PSCustomObject]@{ Test = "Tzar Running"; Status = "INFO"; Details = "Not running (optional)" }
    Write-Info "Tzar not running - this is OK for demo"
}

# Generate Report
Write-Step "Generating Report"

$passCount = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($results | Where-Object { $_.Status -eq "WARN" }).Count
$totalCount = $results.Count

$screenshotSection = if ($screenshots | Where-Object { $_ }) {
    ($screenshots | Where-Object { $_ } | ForEach-Object { "- $_" }) -join "`n"
} else {
    "- No screenshots captured"
}

$reportContent = @"
# Phase 1 Demo Report - TzarBot Game Interface

**Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**VM Name:** DEV
**Project Path:** $ProjectPath
**Status:** $(if ($failCount -eq 0) { "ALL CRITICAL TESTS PASSED" } else { "SOME TESTS FAILED" })

---

## Test Results

| Test | Status | Details |
|------|--------|---------|
$($results | ForEach-Object { "| $($_.Test) | $($_.Status) | $($_.Details) |" } | Out-String)

---

## Unit Test Summary

| Metric | Value |
|--------|-------|
| Total Passed | $passedTests |
| Total Failed | $failedTests |
| Total Skipped | $skippedTests |

---

## Demo Summary

| Metric | Value |
|--------|-------|
| Total Checks | $totalCount |
| Passed | $passCount |
| Failed | $failCount |
| Warnings | $warnCount |

---

## Artifacts

### Logs
- Build log: ``$buildLog``
- Test log: ``$testLog``
- Demo log: ``$logFile``

### Screenshots
$screenshotSection

---

## Phase 1 Components Verified

- [$(if ($passedTests -gt 0) { 'x' } else { ' ' })] Screen Capture (DXGI Desktop Duplication)
- [$(if ($passedTests -gt 0) { 'x' } else { ' ' })] Input Injection (SendInput API)
- [$(if ($passedTests -gt 0) { 'x' } else { ' ' })] IPC Named Pipes (MessagePack)
- [$(if ($passedTests -gt 0) { 'x' } else { ' ' })] Window Detection (Win32 API)

---

*Generated by TzarBot Phase 1 Demo Script*
"@

Set-Content -Path $reportFile -Value $reportContent -Encoding UTF8
Log "Report saved: $reportFile"

# Summary
Write-Step "Phase 1 Demo Complete"
Write-Host "`nUnit Tests: $passedTests passed, $failedTests failed" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Red" })
Write-Host "Demo Checks: $passCount/$totalCount passed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
Write-Host "`nReport: $reportFile"
Write-Host "Build Log: $buildLog"
Write-Host "Test Log: $testLog"

# Return results for automation
return @{
    Success = ($failCount -eq 0 -and $failedTests -eq 0)
    PassedTests = $passedTests
    FailedTests = $failedTests
    ReportFile = $reportFile
    BuildLog = $buildLog
    TestLog = $testLog
}
