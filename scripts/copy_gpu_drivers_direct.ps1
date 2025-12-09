# Copy GPU drivers directly to running VM using PowerShell Direct
# This creates a share on host and copies from within VM

$ErrorActionPreference = "Continue"
$VMName = "DEV"

Write-Host "=== GPU-PV Driver Copy (Direct Method) ===" -ForegroundColor Cyan

# Get credentials for VM
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

# Find NVIDIA driver files on host
Write-Host "`nFinding NVIDIA driver files on host..." -ForegroundColor Yellow

$driverStore = "C:\Windows\System32\DriverStore\FileRepository"
$nvFolder = Get-ChildItem $driverStore -Directory | Where-Object { $_.Name -like "nv_disp*" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $nvFolder) {
    Write-Host "ERROR: NVIDIA driver folder not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Found: $($nvFolder.Name)" -ForegroundColor Green

# Create a staging folder with driver files
$stagingPath = "C:\Temp\NVDriverStaging"
Write-Host "`nCreating staging folder: $stagingPath" -ForegroundColor Yellow

if (Test-Path $stagingPath) {
    Remove-Item $stagingPath -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

# Copy essential files (not the entire 1.8GB folder)
Write-Host "Copying essential driver files..." -ForegroundColor Yellow

# Key files needed for GPU-PV
$essentialPatterns = @(
    "*.dll",
    "*.sys",
    "*.inf",
    "*.cat"
)

$sourceDir = $nvFolder.FullName
$destDir = Join-Path $stagingPath $nvFolder.Name
New-Item -ItemType Directory -Path $destDir -Force | Out-Null

# Copy files matching patterns
$copiedCount = 0
foreach ($pattern in $essentialPatterns) {
    $files = Get-ChildItem $sourceDir -Filter $pattern -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        Copy-Item $file.FullName -Destination $destDir -Force
        $copiedCount++
    }
}
Write-Host "  Copied $copiedCount files from driver folder"

# Also copy System32 files
$sys32Files = @(
    "C:\Windows\System32\nvapi64.dll",
    "C:\Windows\System32\nvcuda.dll",
    "C:\Windows\System32\nvml.dll"
)

$sys32Staging = Join-Path $stagingPath "System32"
New-Item -ItemType Directory -Path $sys32Staging -Force | Out-Null

foreach ($file in $sys32Files) {
    if (Test-Path $file) {
        Copy-Item $file -Destination $sys32Staging -Force
        Write-Host "  Staged: $(Split-Path $file -Leaf)"
    }
}

# Get staging folder size
$stagingSize = [math]::Round((Get-ChildItem $stagingPath -Recurse | Measure-Object Length -Sum).Sum / 1MB, 2)
Write-Host "`nStaging folder size: $stagingSize MB" -ForegroundColor Yellow

# Create network share
$shareName = "NVDrivers"
Write-Host "`nCreating network share..." -ForegroundColor Yellow
Remove-SmbShare -Name $shareName -Force -ErrorAction SilentlyContinue
$share = New-SmbShare -Name $shareName -Path $stagingPath -FullAccess "$env:COMPUTERNAME\$env:USERNAME" -ErrorAction SilentlyContinue

if (-not $share) {
    # Try with explicit Everyone
    Write-Host "Trying alternate share method..." -ForegroundColor Yellow
    net share $shareName=$stagingPath /grant:Everyone,FULL 2>&1 | Out-Null
}

$hostIP = "192.168.100.1"
Write-Host "Share: \\$hostIP\$shareName" -ForegroundColor Green

# Get host credentials for network access
$hostUsername = "$env:COMPUTERNAME\$env:USERNAME"
Write-Host "Host user: $hostUsername"

# Copy files from within VM using PowerShell Direct
Write-Host "`nCopying files to VM..." -ForegroundColor Yellow

$nvFolderName = $nvFolder.Name

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($hostIP, $shareName, $nvFolderName, $hostUser)

    $ErrorActionPreference = "Continue"

    Write-Host "Inside VM - starting copy operation"

    # Try to connect to share
    $networkPath = "\\$hostIP\$shareName"
    Write-Host "Connecting to: $networkPath"

    # First, disconnect any existing connection
    net use /delete $networkPath 2>&1 | Out-Null

    # Connect using cmdkey for Windows credentials
    # Note: password is not ideal but necessary for automated setup
    $result = net use $networkPath 2>&1
    Write-Host "Net use result: $result"

    if (Test-Path $networkPath) {
        Write-Host "Connection successful!"

        # Create HostDriverStore folder (where GPU-PV looks for drivers)
        $hostDriverStore = "C:\Windows\System32\HostDriverStore\FileRepository"
        if (-not (Test-Path $hostDriverStore)) {
            New-Item -ItemType Directory -Path $hostDriverStore -Force | Out-Null
        }

        # Copy driver folder
        $srcDriverFolder = Join-Path $networkPath $nvFolderName
        $destDriverFolder = Join-Path $hostDriverStore $nvFolderName

        if (Test-Path $srcDriverFolder) {
            Write-Host "Copying driver folder to: $destDriverFolder"
            if (-not (Test-Path $destDriverFolder)) {
                New-Item -ItemType Directory -Path $destDriverFolder -Force | Out-Null
            }
            Copy-Item "$srcDriverFolder\*" -Destination $destDriverFolder -Force -Recurse
            $count = (Get-ChildItem $destDriverFolder -File).Count
            Write-Host "Copied $count files to HostDriverStore"
        } else {
            Write-Host "ERROR: Source driver folder not found: $srcDriverFolder"
        }

        # Copy System32 files
        $srcSys32 = Join-Path $networkPath "System32"
        if (Test-Path $srcSys32) {
            Write-Host "Copying System32 files..."
            Get-ChildItem $srcSys32 -File | ForEach-Object {
                Copy-Item $_.FullName -Destination "C:\Windows\System32\$($_.Name)" -Force
                Write-Host "  Copied: $($_.Name)"
            }
        }

        # Disconnect
        net use /delete $networkPath 2>&1 | Out-Null

    } else {
        Write-Host "ERROR: Cannot access network path"

        # List what we can see
        Write-Host "Checking network connectivity..."
        Test-Connection $hostIP -Count 1 -Quiet
    }

} -ArgumentList $hostIP, $shareName, $nvFolderName, $hostUsername

# Cleanup
Write-Host "`nCleaning up..." -ForegroundColor Yellow
Remove-SmbShare -Name $shareName -Force -ErrorAction SilentlyContinue
net share $shareName /delete 2>&1 | Out-Null

Write-Host "`n=== Verifying installation ===" -ForegroundColor Cyan

# Check if drivers were copied
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Write-Host "`n=== Checking HostDriverStore ==="
    $hostDriverStore = "C:\Windows\System32\HostDriverStore\FileRepository"
    if (Test-Path $hostDriverStore) {
        $folders = Get-ChildItem $hostDriverStore -Directory
        Write-Host "Folders found: $($folders.Count)"
        $folders | ForEach-Object { Write-Host "  - $($_.Name)" }
    } else {
        Write-Host "HostDriverStore does not exist"
    }

    Write-Host "`n=== Checking nvapi64.dll ==="
    if (Test-Path "C:\Windows\System32\nvapi64.dll") {
        Write-Host "nvapi64.dll: EXISTS"
    } else {
        Write-Host "nvapi64.dll: NOT FOUND"
    }

    Write-Host "`n=== GPU Status ==="
    Get-PnpDevice -Class Display | Format-Table FriendlyName, Status -AutoSize
}

Write-Host "`n=== Done! ===" -ForegroundColor Green
Write-Host "You may need to restart the VM for drivers to load." -ForegroundColor Yellow
