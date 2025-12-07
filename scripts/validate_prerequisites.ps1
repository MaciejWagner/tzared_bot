# TzarBot Prerequisites Validation Script
# Run this to verify your environment is ready for development

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"
$script:Errors = @()
$script:Warnings = @()

function Write-Status {
    param([string]$Message, [string]$Status, [string]$Color = "White")
    $statusText = switch($Status) {
        "OK" { "[OK]"; $Color = "Green" }
        "FAIL" { "[FAIL]"; $Color = "Red" }
        "WARN" { "[WARN]"; $Color = "Yellow" }
        "INFO" { "[INFO]"; $Color = "Cyan" }
        default { "[$Status]" }
    }
    Write-Host "$statusText " -ForegroundColor $Color -NoNewline
    Write-Host $Message
}

function Test-Admin {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Write-Host ""
Write-Host "=== TzarBot Prerequisites Validation ===" -ForegroundColor Cyan
Write-Host "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# 1. Check Windows Edition
Write-Host "[1] Checking Windows Edition..." -ForegroundColor Yellow
$os = Get-WmiObject -Class Win32_OperatingSystem
$edition = $os.Caption
if ($edition -match "Pro|Enterprise|Education") {
    Write-Status "Windows Edition: $edition" "OK"
} else {
    Write-Status "Windows Edition: $edition (Hyper-V requires Pro/Enterprise)" "FAIL"
    $script:Errors += "Windows edition does not support Hyper-V"
}

# 2. Check Hyper-V
Write-Host "`n[2] Checking Hyper-V..." -ForegroundColor Yellow
$hyperv = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -ErrorAction SilentlyContinue
if ($hyperv -and $hyperv.State -eq "Enabled") {
    Write-Status "Hyper-V: Enabled" "OK"

    # Check if we can run Get-VM
    try {
        $vms = Get-VM -ErrorAction Stop
        Write-Status "Hyper-V accessible ($(($vms | Measure-Object).Count) VMs found)" "OK"
    } catch {
        Write-Status "Hyper-V enabled but not accessible. Run as Administrator?" "WARN"
        $script:Warnings += "Run as Administrator to access Hyper-V"
    }
} else {
    Write-Status "Hyper-V: Not Enabled" "FAIL"
    Write-Status "Enable with: Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All" "INFO"
    $script:Errors += "Hyper-V is not enabled"
}

# 3. Check Virtual Switch
Write-Host "`n[3] Checking Virtual Switch..." -ForegroundColor Yellow
try {
    $switch = Get-VMSwitch -Name "TzarBotSwitch" -ErrorAction Stop
    Write-Status "TzarBotSwitch: Found ($($switch.SwitchType))" "OK"
} catch {
    Write-Status "TzarBotSwitch: Not Found" "WARN"
    Write-Status "Create with: New-VMSwitch -Name 'TzarBotSwitch' -SwitchType Internal" "INFO"
    $script:Warnings += "TzarBotSwitch not created yet"
}

# 4. Check .NET SDK
Write-Host "`n[4] Checking .NET SDK..." -ForegroundColor Yellow
$dotnet = & dotnet --version 2>$null
if ($dotnet -and $dotnet -match "^8\.") {
    Write-Status ".NET SDK: $dotnet" "OK"
} elseif ($dotnet) {
    Write-Status ".NET SDK: $dotnet (8.x recommended)" "WARN"
    $script:Warnings += ".NET 8.x is recommended"
} else {
    Write-Status ".NET SDK: Not Found" "FAIL"
    Write-Status "Download from: https://dotnet.microsoft.com/download/dotnet/8.0" "INFO"
    $script:Errors += ".NET SDK not installed"
}

# 5. Check Git
Write-Host "`n[5] Checking Git..." -ForegroundColor Yellow
$git = & git --version 2>$null
if ($git) {
    Write-Status "Git: $($git -replace 'git version ','')" "OK"
} else {
    Write-Status "Git: Not Found" "FAIL"
    $script:Errors += "Git not installed"
}

# 6. Check PowerShell Version
Write-Host "`n[6] Checking PowerShell..." -ForegroundColor Yellow
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 7) {
    Write-Status "PowerShell: $psVersion (PowerShell 7+)" "OK"
} elseif ($psVersion.Major -ge 5) {
    Write-Status "PowerShell: $psVersion (5.1 OK, 7+ recommended)" "OK"
} else {
    Write-Status "PowerShell: $psVersion" "WARN"
    $script:Warnings += "PowerShell 5.1+ recommended"
}

# 7. Check Hardware
Write-Host "`n[7] Checking Hardware..." -ForegroundColor Yellow
$ram = [math]::Round((Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 1)
$cpu = (Get-WmiObject -Class Win32_Processor).NumberOfLogicalProcessors

if ($ram -ge 32) {
    Write-Status "RAM: ${ram}GB (32GB+ recommended)" "OK"
} elseif ($ram -ge 16) {
    Write-Status "RAM: ${ram}GB (32GB recommended for 8 VMs)" "WARN"
    $script:Warnings += "32GB RAM recommended for full parallel training"
} else {
    Write-Status "RAM: ${ram}GB (16GB minimum)" "FAIL"
    $script:Errors += "Insufficient RAM for VM training"
}

if ($cpu -ge 8) {
    Write-Status "CPU: $cpu logical processors" "OK"
} elseif ($cpu -ge 4) {
    Write-Status "CPU: $cpu logical processors (8+ recommended)" "WARN"
} else {
    Write-Status "CPU: $cpu logical processors (4+ required)" "FAIL"
    $script:Errors += "Insufficient CPU cores"
}

# 8. Check Project Directories
Write-Host "`n[8] Checking Project Structure..." -ForegroundColor Yellow
$projectRoot = Split-Path -Parent $PSScriptRoot
$requiredDirs = @(
    "plans",
    "prompts",
    "config"
)
$optionalDirs = @(
    "files",
    "assets/templates",
    "checkpoints",
    "logs"
)

foreach ($dir in $requiredDirs) {
    $path = Join-Path $projectRoot $dir
    if (Test-Path $path) {
        Write-Status "Directory exists: $dir" "OK"
    } else {
        Write-Status "Directory missing: $dir" "FAIL"
        $script:Errors += "Required directory missing: $dir"
    }
}

foreach ($dir in $optionalDirs) {
    $path = Join-Path $projectRoot $dir
    if (Test-Path $path) {
        Write-Status "Directory exists: $dir" "OK"
    } else {
        Write-Status "Directory missing (optional): $dir" "INFO"
    }
}

# 9. Check Game Installer
Write-Host "`n[9] Checking Game Files..." -ForegroundColor Yellow
$gameZip = Join-Path $projectRoot "files\tzared.windows.zip"
if (Test-Path $gameZip) {
    $size = [math]::Round((Get-Item $gameZip).Length / 1MB, 1)
    Write-Status "Game installer found: ${size}MB" "OK"
} else {
    Write-Status "Game installer not found: files/tzared.windows.zip" "WARN"
    Write-Status "Download from: https://tza.red/" "INFO"
    $script:Warnings += "Game installer not yet downloaded"
}

# 10. Check VMs
Write-Host "`n[10] Checking Hyper-V VMs..." -ForegroundColor Yellow
try {
    $devVm = Get-VM -Name "TzarBot-Dev" -ErrorAction Stop
    Write-Status "TzarBot-Dev VM: $($devVm.State)" "OK"
} catch {
    Write-Status "TzarBot-Dev VM: Not found" "INFO"
}

try {
    $templateVm = Get-VM -Name "TzarBot-Template" -ErrorAction Stop
    Write-Status "TzarBot-Template VM: $($templateVm.State)" "OK"
} catch {
    Write-Status "TzarBot-Template VM: Not found (needed for Phase 4)" "INFO"
}

# Summary
Write-Host "`n=== Validation Summary ===" -ForegroundColor Cyan

if ($script:Errors.Count -eq 0 -and $script:Warnings.Count -eq 0) {
    Write-Host "All checks passed! Environment is ready." -ForegroundColor Green
} else {
    if ($script:Errors.Count -gt 0) {
        Write-Host "`nErrors ($($script:Errors.Count)):" -ForegroundColor Red
        foreach ($err in $script:Errors) {
            Write-Host "  - $err" -ForegroundColor Red
        }
    }

    if ($script:Warnings.Count -gt 0) {
        Write-Host "`nWarnings ($($script:Warnings.Count)):" -ForegroundColor Yellow
        foreach ($warn in $script:Warnings) {
            Write-Host "  - $warn" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Validation Complete ===" -ForegroundColor Cyan

# Exit code
if ($script:Errors.Count -gt 0) {
    exit 1
} else {
    exit 0
}
