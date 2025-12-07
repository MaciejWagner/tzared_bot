#
# Phase 0 Demo Script - TzarBot Prerequisites Verification
# Uruchom na VM DEV jako Administrator
#

param(
    [string]$OutputDir = "C:\TzarBot-Demo\Phase0",
    [switch]$SkipScreenshots
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

# Kolory
function Write-Success { param($msg) Write-Host "[PASS] $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Yellow }

# Utworz katalog wyjsciowy
Write-Step "Inicjalizacja Phase 0 Demo"
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$logFile = Join-Path $OutputDir "phase0_demo_$timestamp.log"
$reportFile = Join-Path $OutputDir "phase0_report_$timestamp.md"

# Funkcja logowania
function Log {
    param($msg)
    $logMsg = "$(Get-Date -Format 'HH:mm:ss') - $msg"
    Write-Host $logMsg
    Add-Content -Path $logFile -Value $logMsg
}

# Funkcja screenshot (wymaga dodatkowego modulu lub narzedzia)
function Take-Screenshot {
    param($name)
    if ($SkipScreenshots) { return $null }

    $screenshotPath = Join-Path $OutputDir "$name.png"
    try {
        Add-Type -AssemblyName System.Windows.Forms
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
Log "Phase 0 Demo started at $timestamp"
Log "Output directory: $OutputDir"

$results = @()

# Test 1: System Info
Write-Step "Test 1: System Information"
$osInfo = Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, BuildNumber
$cpuInfo = Get-CimInstance Win32_Processor | Select-Object Name, NumberOfCores
$ramInfo = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)

Log "OS: $($osInfo.Caption) (Build $($osInfo.BuildNumber))"
Log "CPU: $($cpuInfo.Name) ($($cpuInfo.NumberOfCores) cores)"
Log "RAM: $ramInfo GB"

$results += [PSCustomObject]@{
    Test = "System Info"
    Status = "PASS"
    Details = "OS: $($osInfo.Caption), RAM: $ramInfo GB"
}
Write-Success "System information collected"

# Test 2: Network Configuration
Write-Step "Test 2: Network Configuration"
$ipConfig = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.100.*" }
if ($ipConfig) {
    Log "IP Address: $($ipConfig.IPAddress)"
    Log "Prefix Length: $($ipConfig.PrefixLength)"
    $results += [PSCustomObject]@{ Test = "Network IP"; Status = "PASS"; Details = $ipConfig.IPAddress }
    Write-Success "Network IP configured correctly"
} else {
    $results += [PSCustomObject]@{ Test = "Network IP"; Status = "FAIL"; Details = "No 192.168.100.x IP found" }
    Write-Fail "Network IP not configured"
}

# Test 3: Gateway Connectivity
Write-Step "Test 3: Gateway Connectivity"
$pingGateway = Test-Connection -ComputerName 192.168.100.1 -Count 2 -Quiet
if ($pingGateway) {
    Log "Gateway 192.168.100.1 is reachable"
    $results += [PSCustomObject]@{ Test = "Gateway Ping"; Status = "PASS"; Details = "192.168.100.1 reachable" }
    Write-Success "Gateway connectivity OK"
} else {
    $results += [PSCustomObject]@{ Test = "Gateway Ping"; Status = "FAIL"; Details = "Gateway unreachable" }
    Write-Fail "Gateway unreachable"
}

# Test 4: Internet Connectivity
Write-Step "Test 4: Internet Connectivity"
$pingInternet = Test-Connection -ComputerName 8.8.8.8 -Count 2 -Quiet
if ($pingInternet) {
    Log "Internet (8.8.8.8) is reachable"
    $results += [PSCustomObject]@{ Test = "Internet Ping"; Status = "PASS"; Details = "8.8.8.8 reachable" }
    Write-Success "Internet connectivity OK"
} else {
    $results += [PSCustomObject]@{ Test = "Internet Ping"; Status = "FAIL"; Details = "Internet unreachable" }
    Write-Fail "Internet unreachable"
}

# Test 5: .NET SDK
Write-Step "Test 5: .NET SDK Installation"
try {
    $dotnetVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Log ".NET SDK Version: $dotnetVersion"
        $results += [PSCustomObject]@{ Test = ".NET SDK"; Status = "PASS"; Details = "Version $dotnetVersion" }
        Write-Success ".NET SDK installed: $dotnetVersion"
    } else {
        throw "dotnet command failed"
    }
} catch {
    $results += [PSCustomObject]@{ Test = ".NET SDK"; Status = "FAIL"; Details = "Not installed" }
    Write-Fail ".NET SDK not found"
}

# Test 6: Tzar Game
Write-Step "Test 6: Tzar Game Installation"
$tzarPath = "C:\Program Files\Tzared\Tzared.exe"
if (Test-Path $tzarPath) {
    $tzarInfo = Get-Item $tzarPath
    Log "Tzar found: $tzarPath"
    Log "Size: $([math]::Round($tzarInfo.Length / 1MB, 2)) MB"
    $results += [PSCustomObject]@{ Test = "Tzar Game"; Status = "PASS"; Details = $tzarPath }
    Write-Success "Tzar game installed"
} else {
    # Sprawdz alternatywne lokalizacje
    $altPaths = @(
        "C:\Games\Tzar\Tzared.exe",
        "C:\Tzared\Tzared.exe",
        "$env:USERPROFILE\Desktop\Tzared\Tzared.exe"
    )
    $found = $false
    foreach ($path in $altPaths) {
        if (Test-Path $path) {
            Log "Tzar found at alternative location: $path"
            $results += [PSCustomObject]@{ Test = "Tzar Game"; Status = "PASS"; Details = $path }
            Write-Success "Tzar game installed at $path"
            $found = $true
            break
        }
    }
    if (-not $found) {
        $results += [PSCustomObject]@{ Test = "Tzar Game"; Status = "FAIL"; Details = "Not found" }
        Write-Fail "Tzar game not found"
    }
}

# Test 7: Disk Space
Write-Step "Test 7: Disk Space"
$disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$freeGB = [math]::Round($disk.FreeSpace / 1GB, 2)
$totalGB = [math]::Round($disk.Size / 1GB, 2)
Log "Disk C: $freeGB GB free of $totalGB GB"
if ($freeGB -gt 10) {
    $results += [PSCustomObject]@{ Test = "Disk Space"; Status = "PASS"; Details = "$freeGB GB free" }
    Write-Success "Sufficient disk space"
} else {
    $results += [PSCustomObject]@{ Test = "Disk Space"; Status = "WARN"; Details = "Only $freeGB GB free" }
    Write-Fail "Low disk space"
}

# Screenshot
Write-Step "Capturing Screenshot"
$screenshot = Take-Screenshot -name "phase0_system_$timestamp"

# Generate Report
Write-Step "Generating Report"

$passCount = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$totalCount = $results.Count

$reportContent = @"
# Phase 0 Demo Report - TzarBot Prerequisites

**Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**VM Name:** DEV
**Status:** $(if ($failCount -eq 0) { "ALL TESTS PASSED" } else { "SOME TESTS FAILED" })

---

## Environment Information

| Parameter | Value |
|-----------|-------|
| OS | $($osInfo.Caption) |
| Build | $($osInfo.BuildNumber) |
| CPU | $($cpuInfo.Name) |
| Cores | $($cpuInfo.NumberOfCores) |
| RAM | $ramInfo GB |
| Free Disk | $freeGB GB |

---

## Test Results

| Test | Status | Details |
|------|--------|---------|
$($results | ForEach-Object { "| $($_.Test) | $($_.Status) | $($_.Details) |" } | Out-String)

---

## Summary

| Metric | Value |
|--------|-------|
| Total Tests | $totalCount |
| Passed | $passCount |
| Failed | $failCount |
| Success Rate | $([math]::Round($passCount / $totalCount * 100, 1))% |

---

## Screenshots

$(if ($screenshot) { "- ![$screenshot]($screenshot)" } else { "- Screenshots skipped or failed" })

---

## Log File

Full log available at: ``$logFile``

---

*Generated by TzarBot Phase 0 Demo Script*
"@

Set-Content -Path $reportFile -Value $reportContent -Encoding UTF8
Log "Report saved: $reportFile"

# Summary
Write-Step "Phase 0 Demo Complete"
Write-Host "`nResults: $passCount/$totalCount tests passed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
Write-Host "Report: $reportFile"
Write-Host "Log: $logFile"

# Return results for automation
return @{
    Success = ($failCount -eq 0)
    PassCount = $passCount
    FailCount = $failCount
    ReportFile = $reportFile
    LogFile = $logFile
}
