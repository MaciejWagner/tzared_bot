# Copy remaining GPU driver files
$ErrorActionPreference = "Continue"
$VMName = "DEV"

$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Write-Host "=== Copying remaining GPU driver files ===" -ForegroundColor Cyan

# Files to copy from System32
$sys32Files = @(
    "nvlddmkm.sys",
    "nvwgf2umx.dll",
    "nvwgf2um.dll",
    "nvd3dumx.dll",
    "nvd3dum.dll",
    "nvoglv64.dll",
    "nvoglv32.dll"
)

# Function to copy file via temp
function Copy-ToVM {
    param($srcPath, $destPath)

    $fileName = Split-Path $srcPath -Leaf

    if (-not (Test-Path $srcPath)) {
        Write-Host "  Not found: $fileName" -ForegroundColor Gray
        return
    }

    Write-Host "  Copying $fileName..." -ForegroundColor Yellow
    $tempDest = "C:\Temp\$fileName"

    try {
        Copy-VMFile -VMName $VMName -SourcePath $srcPath -DestinationPath $tempDest -CreateFullPath -FileSource Host -Force

        Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
            param($src, $dest)
            Move-Item $src -Destination $dest -Force -ErrorAction SilentlyContinue
        } -ArgumentList $tempDest, $destPath

        Write-Host "    OK" -ForegroundColor Green
    }
    catch {
        Write-Host "    Failed: $_" -ForegroundColor Red
    }
}

# Copy System32 files
Write-Host "`n=== System32 files ===" -ForegroundColor Cyan
foreach ($file in $sys32Files) {
    $srcPath = "C:\Windows\System32\$file"
    $destPath = "C:\Windows\System32\$file"
    Copy-ToVM -srcPath $srcPath -destPath $destPath
}

# Copy SysWOW64 files (32-bit)
Write-Host "`n=== SysWOW64 files ===" -ForegroundColor Cyan
$sysWow64Files = @("nvapi.dll", "nvcuda.dll", "nvd3dum.dll", "nvoglv32.dll")
foreach ($file in $sysWow64Files) {
    $srcPath = "C:\Windows\SysWOW64\$file"
    $destPath = "C:\Windows\SysWOW64\$file"
    Copy-ToVM -srcPath $srcPath -destPath $destPath
}

# Copy driver folder to HostDriverStore (more files)
Write-Host "`n=== Copying more driver files to HostDriverStore ===" -ForegroundColor Cyan

$driverStore = "C:\Windows\System32\DriverStore\FileRepository"
$nvFolder = Get-ChildItem $driverStore -Directory | Where-Object { $_.Name -like "nv_disp*" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1

# Get all INF and CAT files
$essentialFiles = Get-ChildItem $nvFolder.FullName -Include "*.inf","*.cat" -File

Write-Host "Copying $($essentialFiles.Count) INF/CAT files..." -ForegroundColor Yellow

$tempDest = "C:\Temp\NvDrivers2"
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    New-Item -ItemType Directory -Path "C:\Temp\NvDrivers2" -Force | Out-Null
}

foreach ($file in $essentialFiles) {
    try {
        Copy-VMFile -VMName $VMName -SourcePath $file.FullName -DestinationPath "$tempDest\$($file.Name)" -CreateFullPath -FileSource Host -Force
    }
    catch {
        # Ignore errors
    }
}

# Move to HostDriverStore
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($folderName)

    $src = "C:\Temp\NvDrivers2"
    $dest = "C:\Windows\System32\HostDriverStore\FileRepository\$folderName"

    if (Test-Path $src) {
        Get-ChildItem $src -File | ForEach-Object {
            Move-Item $_.FullName -Destination $dest -Force -ErrorAction SilentlyContinue
        }
        Write-Host "Moved INF/CAT files"
    }
} -ArgumentList $nvFolder.Name

# Restart GPU in VM
Write-Host "`n=== Restarting GPU device ===" -ForegroundColor Cyan
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    # Disable and re-enable GPU
    $gpu = Get-PnpDevice | Where-Object { $_.FriendlyName -like "*NVIDIA*" }
    if ($gpu) {
        Write-Host "Disabling GPU..."
        Disable-PnpDevice -InstanceId $gpu.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        Write-Host "Enabling GPU..."
        Enable-PnpDevice -InstanceId $gpu.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
    }

    Write-Host "`nGPU Status after restart:"
    Get-PnpDevice -Class Display | Format-Table FriendlyName, Status -AutoSize
}

Write-Host "`n=== Done ===" -ForegroundColor Green
