# Copy GPU drivers to VM via SMB share
# This script creates a temp SMB share and copies drivers from within the VM

param(
    [string]$VMName = "DEV"
)

$ErrorActionPreference = "Continue"
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Write-Host "=== GPU-PV Driver Copy via SMB ===" -ForegroundColor Cyan

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

$driverFolderName = $nvDriverFolder.Name
Write-Host "Driver folder: $driverFolderName" -ForegroundColor Yellow

# Create temporary share on host
$shareName = "NVDrivers"
$sharePath = $nvDriverFolder.FullName

Write-Host "`nCreating temporary SMB share..." -ForegroundColor Yellow
Remove-SmbShare -Name $shareName -Force -ErrorAction SilentlyContinue
New-SmbShare -Name $shareName -Path $sharePath -FullAccess "Everyone" | Out-Null

# Get host IP for VM network
$hostIP = "192.168.100.1"  # NAT gateway from env_settings

Write-Host "Share created at: \\$hostIP\$shareName" -ForegroundColor Green

# Copy drivers from within VM
Write-Host "`nCopying drivers inside VM (this may take several minutes)..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($hostIP, $shareName, $driverFolderName)

    $ErrorActionPreference = "Continue"

    # Create destination folder
    $destPath = "C:\Windows\System32\DriverStore\FileRepository\$driverFolderName"
    Write-Host "Creating destination: $destPath"

    if (-not (Test-Path $destPath)) {
        New-Item -Path $destPath -ItemType Directory -Force | Out-Null
    }

    # Map network drive
    $networkPath = "\\$hostIP\$shareName"
    Write-Host "Connecting to: $networkPath"

    # Use net use without credentials (guest access)
    net use Z: $networkPath /user:maciek password123 2>&1

    if (Test-Path "Z:\") {
        Write-Host "Network drive mapped successfully!"

        # Copy all files
        Write-Host "Copying files (this will take a while)..."
        $files = Get-ChildItem "Z:\" -Recurse -File
        $total = $files.Count
        $count = 0

        foreach ($file in $files) {
            $count++
            $relativePath = $file.FullName.Substring(3)  # Remove "Z:\"
            $destFile = Join-Path $destPath $relativePath
            $destDir = Split-Path $destFile -Parent

            if (-not (Test-Path $destDir)) {
                New-Item -Path $destDir -ItemType Directory -Force | Out-Null
            }

            Copy-Item $file.FullName -Destination $destFile -Force

            if ($count % 50 -eq 0) {
                Write-Host "  Progress: $count / $total files"
            }
        }

        Write-Host "Driver files copied: $count"

        # Disconnect
        net use Z: /delete 2>&1
    } else {
        Write-Host "ERROR: Could not access network share" -ForegroundColor Red
        Write-Host "Trying direct UNC path..."

        if (Test-Path $networkPath) {
            Copy-Item "$networkPath\*" -Destination $destPath -Recurse -Force
            Write-Host "Copied via UNC path"
        } else {
            Write-Host "UNC path also failed"
        }
    }

} -ArgumentList $hostIP, $shareName, $driverFolderName

# Copy System32 DLLs
Write-Host "`n=== Copying System32 DLLs ===" -ForegroundColor Yellow

$dllsToShare = @(
    "C:\Windows\System32\nvapi64.dll"
)

foreach ($dll in $dllsToShare) {
    if (Test-Path $dll) {
        $dllName = Split-Path $dll -Leaf
        Write-Host "Sharing $dllName..."

        # Create temp folder and share
        $tempDllPath = "C:\Temp\NvDll"
        New-Item -Path $tempDllPath -ItemType Directory -Force -ErrorAction SilentlyContinue | Out-Null
        Copy-Item $dll -Destination "$tempDllPath\$dllName" -Force

        Remove-SmbShare -Name "NvDll" -Force -ErrorAction SilentlyContinue
        New-SmbShare -Name "NvDll" -Path $tempDllPath -FullAccess "Everyone" | Out-Null

        Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
            param($hostIP, $dllName)
            $networkPath = "\\$hostIP\NvDll\$dllName"
            $destPath = "C:\Windows\System32\$dllName"

            net use Y: "\\$hostIP\NvDll" /user:maciek password123 2>&1 | Out-Null

            if (Test-Path "Y:\$dllName") {
                Copy-Item "Y:\$dllName" -Destination $destPath -Force
                Write-Host "Copied $dllName to System32"
            } else {
                Write-Host "Could not access $dllName"
            }

            net use Y: /delete 2>&1 | Out-Null
        } -ArgumentList $hostIP, $dllName

        Remove-SmbShare -Name "NvDll" -Force -ErrorAction SilentlyContinue
    }
}

# Cleanup
Write-Host "`nCleaning up shares..." -ForegroundColor Yellow
Remove-SmbShare -Name $shareName -Force -ErrorAction SilentlyContinue
Remove-Item "C:\Temp\NvDll" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n=== Done! ===" -ForegroundColor Green
Write-Host "Restart the VM to apply changes." -ForegroundColor Yellow
