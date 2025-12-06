<#
.SYNOPSIS
    Removes TzarBot worker VMs and their VHD files.

.DESCRIPTION
    Stops and removes specified worker VMs along with their differencing disk files.
    Use -All to remove all worker VMs at once.

.PARAMETER Names
    Specific VM names to remove.

.PARAMETER All
    Remove all TzarBot worker VMs.

.EXAMPLE
    .\Remove-TzarWorkerVM.ps1 -Names "TzarBot-Worker-0", "TzarBot-Worker-1"
    Removes two specific worker VMs

.EXAMPLE
    .\Remove-TzarWorkerVM.ps1 -All
    Removes all worker VMs (with confirmation)

.EXAMPLE
    .\Remove-TzarWorkerVM.ps1 -All -Confirm:$false
    Removes all worker VMs without confirmation

.NOTES
    Part of Phase 4: Hyper-V Infrastructure
    WARNING: This permanently deletes VMs and their disk files!
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(ParameterSetName = 'ByName')]
    [string[]]$Names,

    [Parameter(ParameterSetName = 'All')]
    [switch]$All
)

# Import configuration
. "$PSScriptRoot\VMConfig.ps1"
$config = Get-VMConfig

# Get VMs to remove
if ($All) {
    $vms = Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }
    if ($vms.Count -eq 0) {
        Write-Host "No worker VMs found." -ForegroundColor Yellow
        exit 0
    }
    Write-Host "Found $($vms.Count) worker VM(s) to remove:" -ForegroundColor Yellow
    $vms | Format-Table Name, State -AutoSize
}
elseif ($Names) {
    $vms = Get-VM | Where-Object { $Names -contains $_.Name }
    if ($vms.Count -eq 0) {
        Write-Warning "No matching VMs found for: $($Names -join ', ')"
        exit 0
    }
}
else {
    Write-Error "Specify -Names or -All parameter"
    exit 1
}

# Remove VMs
$removed = @()
$failed = @()

foreach ($vm in $vms) {
    if ($PSCmdlet.ShouldProcess($vm.Name, "Remove VM and VHD")) {
        try {
            # Step 1: Stop if running
            if ($vm.State -ne 'Off') {
                Write-Host "  Stopping $($vm.Name)..." -NoNewline
                Stop-VM -Name $vm.Name -Force -TurnOff -ErrorAction Stop
                Write-Host " OK" -ForegroundColor Green
            }

            # Step 2: Get VHD path before removing VM
            $vhdPath = $vm.HardDrives | Select-Object -First 1 -ExpandProperty Path

            # Step 3: Remove VM
            Write-Host "  Removing VM $($vm.Name)..." -NoNewline
            Remove-VM -Name $vm.Name -Force -ErrorAction Stop
            Write-Host " OK" -ForegroundColor Green

            # Step 4: Remove VHD file
            if ($vhdPath -and (Test-Path $vhdPath)) {
                Write-Host "  Removing VHD $vhdPath..." -NoNewline
                Remove-Item $vhdPath -Force -ErrorAction Stop
                Write-Host " OK" -ForegroundColor Green
            }

            $removed += $vm.Name
            Write-Host "  Removed: $($vm.Name)" -ForegroundColor Yellow
        }
        catch {
            Write-Host " FAILED" -ForegroundColor Red
            Write-Error "Failed to remove '$($vm.Name)': $_"
            $failed += $vm.Name
        }
    }
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Removed: $($removed.Count) VM(s)" -ForegroundColor Yellow
if ($failed.Count -gt 0) {
    Write-Host "Failed: $($failed.Count) VM(s)" -ForegroundColor Red
    Write-Host "Failed VMs: $($failed -join ', ')"
}

# Show remaining VMs
$remainingVMs = Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }
if ($remainingVMs.Count -gt 0) {
    Write-Host "`nRemaining worker VMs:" -ForegroundColor Cyan
    $remainingVMs | Format-Table Name, State -AutoSize
}
else {
    Write-Host "`nNo worker VMs remaining." -ForegroundColor Green
}
