<#
.SYNOPSIS
    Creates TzarBot worker VMs by cloning the template.

.DESCRIPTION
    Creates one or more worker VMs using differencing disks from the template VHD.
    Each worker VM is configured with dynamic memory and connects to the TzarBot network.

.PARAMETER Count
    Number of worker VMs to create. Default is 8.

.PARAMETER StartIndex
    Starting index for VM naming. Default is 0.
    VMs will be named TzarBot-Worker-0, TzarBot-Worker-1, etc.

.EXAMPLE
    .\New-TzarWorkerVM.ps1 -Count 4
    Creates 4 worker VMs (TzarBot-Worker-0 through TzarBot-Worker-3)

.EXAMPLE
    .\New-TzarWorkerVM.ps1 -Count 4 -StartIndex 4
    Creates 4 worker VMs starting from index 4 (TzarBot-Worker-4 through TzarBot-Worker-7)

.EXAMPLE
    .\New-TzarWorkerVM.ps1 -Count 1 -WhatIf
    Shows what would happen without creating VMs

.NOTES
    Part of Phase 4: Hyper-V Infrastructure
    Requires: Hyper-V PowerShell module, Administrator privileges
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [ValidateRange(1, 16)]
    [int]$Count = 8,

    [Parameter()]
    [ValidateRange(0, 99)]
    [int]$StartIndex = 0
)

# Import configuration
. "$PSScriptRoot\VMConfig.ps1"
$config = Get-VMConfig

# Validate configuration
Write-Host "Validating configuration..." -ForegroundColor Cyan
if (-not (Test-VMConfig)) {
    Write-Error "Configuration validation failed. Please check prerequisites."
    exit 1
}

# Create workers directory if not exists
if (-not (Test-Path $config.WorkersPath)) {
    New-Item -Path $config.WorkersPath -ItemType Directory -Force | Out-Null
    Write-Host "Created workers directory: $($config.WorkersPath)" -ForegroundColor Green
}

# Create VMs
Write-Host "`nCreating $Count worker VM(s)..." -ForegroundColor Cyan
$created = @()
$failed = @()

for ($i = $StartIndex; $i -lt ($StartIndex + $Count); $i++) {
    $vmName = "$($config.VMPrefix)$i"
    $vhdPath = Join-Path $config.WorkersPath "$vmName.vhdx"

    # Check if VM already exists
    $existingVM = Get-VM -Name $vmName -ErrorAction SilentlyContinue
    if ($existingVM) {
        Write-Warning "VM '$vmName' already exists. Skipping."
        continue
    }

    if ($PSCmdlet.ShouldProcess($vmName, "Create VM with differencing disk")) {
        try {
            # Step 1: Create differencing disk
            Write-Host "  Creating differencing disk for $vmName..." -NoNewline
            New-VHD -Path $vhdPath `
                    -ParentPath $config.TemplatePath `
                    -Differencing | Out-Null
            Write-Host " OK" -ForegroundColor Green

            # Step 2: Create VM
            Write-Host "  Creating VM $vmName..." -NoNewline
            New-VM -Name $vmName `
                   -Generation 2 `
                   -MemoryStartupBytes ($config.MemoryStartupMB * 1MB) `
                   -VHDPath $vhdPath `
                   -SwitchName $config.SwitchName | Out-Null
            Write-Host " OK" -ForegroundColor Green

            # Step 3: Configure VM
            Write-Host "  Configuring VM resources..." -NoNewline
            Set-VM -Name $vmName `
                   -ProcessorCount $config.ProcessorCount `
                   -DynamicMemory `
                   -MemoryMinimumBytes ($config.MemoryMinMB * 1MB) `
                   -MemoryMaximumBytes ($config.MemoryMaxMB * 1MB) `
                   -AutomaticStartAction Start `
                   -AutomaticStopAction ShutDown `
                   -AutomaticStartDelay ($i * 10)  # Stagger startup by 10 seconds
            Write-Host " OK" -ForegroundColor Green

            # Step 4: Disable Secure Boot (if template doesn't have it)
            Write-Host "  Configuring firmware..." -NoNewline
            Set-VMFirmware -VMName $vmName -EnableSecureBoot Off -ErrorAction SilentlyContinue
            Write-Host " OK" -ForegroundColor Green

            $created += $vmName
            Write-Host "  Created: $vmName" -ForegroundColor Green
        }
        catch {
            Write-Host " FAILED" -ForegroundColor Red
            Write-Error "Failed to create VM '$vmName': $_"
            $failed += $vmName

            # Cleanup partial creation
            if (Test-Path $vhdPath) {
                Remove-Item $vhdPath -Force -ErrorAction SilentlyContinue
            }
            $partialVM = Get-VM -Name $vmName -ErrorAction SilentlyContinue
            if ($partialVM) {
                Remove-VM -Name $vmName -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Created: $($created.Count) VM(s)" -ForegroundColor Green
if ($failed.Count -gt 0) {
    Write-Host "Failed: $($failed.Count) VM(s)" -ForegroundColor Red
    Write-Host "Failed VMs: $($failed -join ', ')" -ForegroundColor Red
}

# Show created VMs
if ($created.Count -gt 0) {
    Write-Host "`nCreated VMs:" -ForegroundColor Cyan
    Get-VM | Where-Object { $created -contains $_.Name } |
        Format-Table Name, State, ProcessorCount,
            @{L='Memory (MB)'; E={[math]::Round($_.MemoryStartup / 1MB)}} -AutoSize
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Start VMs: .\Start-TzarWorkers.ps1 -All"
Write-Host "  2. Check status: .\Get-TzarWorkerStatus.ps1"
