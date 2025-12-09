# Configure VM for GPU-PV and copy drivers

$VMName = "DEV"

Write-Host "=== Configuring VM for GPU-PV ===" -ForegroundColor Cyan

# Stop VM
Write-Host "Stopping VM..."
Stop-VM -Name $VMName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5

# Configure MMIO
Write-Host "Setting MMIO and cache settings..."
Set-VM -VMName $VMName -GuestControlledCacheTypes $true
Set-VM -VMName $VMName -LowMemoryMappedIoSpace 1GB
Set-VM -VMName $VMName -HighMemoryMappedIoSpace 32GB

# Verify settings
Get-VM -Name $VMName | Select-Object Name, GuestControlledCacheTypes, LowMemoryMappedIoSpace, HighMemoryMappedIoSpace

# Start VM
Write-Host "Starting VM..."
Start-VM -Name $VMName
Start-Sleep -Seconds 30

Write-Host "VM configured and running!" -ForegroundColor Green
