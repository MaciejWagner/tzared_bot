# Copy GPU drivers using Copy-VMFile (requires Guest Service Interface)
$ErrorActionPreference = "Stop"
$VMName = "DEV"

Write-Host "=== GPU-PV Driver Copy using Copy-VMFile ===" -ForegroundColor Cyan

# Ensure Guest Services are enabled
Write-Host "Enabling Guest Service Interface..." -ForegroundColor Yellow
Enable-VMIntegrationService -VMName $VMName -Name "Guest Service Interface"
Start-Sleep -Seconds 5

# Get VM credentials
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

# First, create the destination folders on VM
Write-Host "Creating destination folders on VM..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    New-Item -ItemType Directory -Path "C:\Windows\System32\HostDriverStore\FileRepository" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\Temp\NvDrivers" -Force | Out-Null
    Write-Host "Folders created"
}

# Find NVIDIA driver folder
$driverStore = "C:\Windows\System32\DriverStore\FileRepository"
$nvFolder = Get-ChildItem $driverStore -Directory | Where-Object { $_.Name -like "nv_disp*" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$nvFolderName = $nvFolder.Name

Write-Host "Source folder: $($nvFolder.FullName)" -ForegroundColor Yellow

# Get essential files (DLLs and SYS)
$essentialFiles = Get-ChildItem $nvFolder.FullName -Include "*.dll","*.sys" -Recurse | Select-Object -First 50

Write-Host "Copying $($essentialFiles.Count) essential files..." -ForegroundColor Yellow

$tempDest = "C:\Temp\NvDrivers"
$count = 0
foreach ($file in $essentialFiles) {
    try {
        Copy-VMFile -VMName $VMName -SourcePath $file.FullName -DestinationPath "$tempDest\$($file.Name)" -CreateFullPath -FileSource Host -Force
        $count++
        if ($count % 10 -eq 0) {
            Write-Host "  Copied $count files..."
        }
    }
    catch {
        Write-Host "  Warning: Could not copy $($file.Name): $_" -ForegroundColor Yellow
    }
}

Write-Host "Copied $count files" -ForegroundColor Green

# Copy nvapi64.dll specifically
Write-Host "Copying nvapi64.dll..." -ForegroundColor Yellow
try {
    Copy-VMFile -VMName $VMName -SourcePath "C:\Windows\System32\nvapi64.dll" -DestinationPath "C:\Windows\System32\nvapi64.dll" -CreateFullPath -FileSource Host -Force
    Write-Host "nvapi64.dll copied!" -ForegroundColor Green
}
catch {
    Write-Host "Warning: Could not copy nvapi64.dll: $_" -ForegroundColor Yellow
}

# Move files to correct location inside VM
Write-Host "Moving files to HostDriverStore..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($folderName)

    $src = "C:\Temp\NvDrivers"
    $dest = "C:\Windows\System32\HostDriverStore\FileRepository\$folderName"

    if (-not (Test-Path $dest)) {
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
    }

    $files = Get-ChildItem $src -File
    foreach ($f in $files) {
        Move-Item $f.FullName -Destination $dest -Force
    }

    Write-Host "Moved $($files.Count) files to $dest"

    # Verify
    $count = (Get-ChildItem $dest -File).Count
    Write-Host "Total files in HostDriverStore: $count"

} -ArgumentList $nvFolderName

# Verify
Write-Host "`n=== Verification ===" -ForegroundColor Cyan
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Write-Host "nvapi64.dll exists: $(Test-Path 'C:\Windows\System32\nvapi64.dll')"

    $hostStore = "C:\Windows\System32\HostDriverStore\FileRepository"
    if (Test-Path $hostStore) {
        $folders = Get-ChildItem $hostStore -Directory
        Write-Host "HostDriverStore folders: $($folders.Count)"
        foreach ($f in $folders) {
            $fileCount = (Get-ChildItem $f.FullName -File).Count
            Write-Host "  $($f.Name): $fileCount files"
        }
    }

    Get-PnpDevice -Class Display | Format-Table FriendlyName, Status -AutoSize
}

Write-Host "`n=== Done! Please restart VM to apply changes ===" -ForegroundColor Green
