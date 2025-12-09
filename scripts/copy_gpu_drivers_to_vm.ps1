# Copy GPU-PV drivers from Host to VM DEV
# This script copies the necessary NVIDIA drivers for GPU-PV to work

param(
    [string]$VMName = "DEV"
)

$ErrorActionPreference = "Stop"

Write-Host "=== GPU-PV Driver Copy Script ===" -ForegroundColor Cyan
Write-Host "Target VM: $VMName" -ForegroundColor Yellow

# Find the latest NVIDIA display driver folder
$driverPath = "C:\Windows\System32\DriverStore\FileRepository"
$nvDriverFolder = Get-ChildItem $driverPath -Directory |
    Where-Object { $_.Name -like "nv_disp*" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $nvDriverFolder) {
    Write-Host "ERROR: NVIDIA driver folder not found!" -ForegroundColor Red
    exit 1
}

Write-Host "`nSource driver folder: $($nvDriverFolder.FullName)" -ForegroundColor Green
Write-Host "Folder size: $([math]::Round((Get-ChildItem $nvDriverFolder.FullName -Recurse | Measure-Object Length -Sum).Sum / 1MB, 2)) MB" -ForegroundColor Yellow

# Ensure VM is running
$vm = Get-VM -Name $VMName
if ($vm.State -ne 'Running') {
    Write-Host "Starting VM $VMName..." -ForegroundColor Yellow
    Start-VM -Name $VMName
    Start-Sleep -Seconds 30
}

# System32 DLLs to copy
$system32DLLs = @(
    "nvapi64.dll",
    "nvapi.dll",
    "nvd3dumx.dll",
    "nvwgf2umx.dll",
    "nvwgf2um.dll"
)

Write-Host "`n=== Copying driver folder ===" -ForegroundColor Cyan
Write-Host "This may take several minutes for ~1.8 GB..." -ForegroundColor Yellow

# Copy driver folder
$destFolder = "C:\Windows\System32\DriverStore\FileRepository\$($nvDriverFolder.Name)"
try {
    Copy-VMFile -VMName $VMName -SourcePath $nvDriverFolder.FullName -DestinationPath $destFolder -CreateFullPath -FileSource Host -Force -Recurse
    Write-Host "Driver folder copied successfully!" -ForegroundColor Green
} catch {
    Write-Host "ERROR copying driver folder: $_" -ForegroundColor Red
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
}

Write-Host "`n=== Copying System32 DLLs ===" -ForegroundColor Cyan
foreach ($dll in $system32DLLs) {
    $srcPath = "C:\Windows\System32\$dll"
    if (Test-Path $srcPath) {
        Write-Host "Copying $dll..." -ForegroundColor Yellow
        try {
            Copy-VMFile -VMName $VMName -SourcePath $srcPath -DestinationPath "C:\Windows\System32\$dll" -CreateFullPath -FileSource Host -Force
            Write-Host "  $dll copied!" -ForegroundColor Green
        } catch {
            Write-Host "  ERROR copying $dll : $_" -ForegroundColor Red
        }
    } else {
        Write-Host "  $dll not found on host" -ForegroundColor Gray
    }
}

# Also copy SysWOW64 DLLs (32-bit)
Write-Host "`n=== Copying SysWOW64 DLLs ===" -ForegroundColor Cyan
$sysWow64DLLs = @("nvapi.dll", "nvd3dum.dll", "nvwgf2um.dll")
foreach ($dll in $sysWow64DLLs) {
    $srcPath = "C:\Windows\SysWOW64\$dll"
    if (Test-Path $srcPath) {
        Write-Host "Copying $dll (32-bit)..." -ForegroundColor Yellow
        try {
            Copy-VMFile -VMName $VMName -SourcePath $srcPath -DestinationPath "C:\Windows\SysWOW64\$dll" -CreateFullPath -FileSource Host -Force
            Write-Host "  $dll copied!" -ForegroundColor Green
        } catch {
            Write-Host "  ERROR copying $dll : $_" -ForegroundColor Red
        }
    }
}

Write-Host "`n=== Done! ===" -ForegroundColor Green
Write-Host "Please restart the VM to apply the changes." -ForegroundColor Yellow
Write-Host "After restart, run: Get-WmiObject Win32_VideoController" -ForegroundColor Yellow
