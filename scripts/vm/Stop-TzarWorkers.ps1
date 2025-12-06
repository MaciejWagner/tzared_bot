<#
.SYNOPSIS
    Stops TzarBot worker VMs.

.DESCRIPTION
    Gracefully stops specified worker VMs. Use -Force to immediately turn off VMs
    without waiting for graceful shutdown.

.PARAMETER Names
    Specific VM names to stop.

.PARAMETER All
    Stop all TzarBot worker VMs.

.PARAMETER Force
    Force immediate shutdown (turn off without graceful shutdown).

.EXAMPLE
    .\Stop-TzarWorkers.ps1 -All
    Gracefully stops all worker VMs

.EXAMPLE
    .\Stop-TzarWorkers.ps1 -All -Force
    Immediately turns off all worker VMs

.EXAMPLE
    .\Stop-TzarWorkers.ps1 -Names "TzarBot-Worker-0"
    Stops a specific VM

.NOTES
    Part of Phase 4: Hyper-V Infrastructure
#>

[CmdletBinding()]
param(
    [Parameter(ParameterSetName = 'ByName')]
    [string[]]$Names,

    [Parameter(ParameterSetName = 'All')]
    [switch]$All,

    [Parameter()]
    [switch]$Force
)

# Import configuration
. "$PSScriptRoot\VMConfig.ps1"
$config = Get-VMConfig

# Get VMs to stop
if ($All) {
    $vms = Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }
}
elseif ($Names) {
    $vms = Get-VM | Where-Object { $Names -contains $_.Name }
}
else {
    Write-Error "Specify -Names or -All parameter"
    exit 1
}

if ($vms.Count -eq 0) {
    Write-Warning "No worker VMs found."
    exit 0
}

$mode = if ($Force) { "Force shutdown" } else { "Graceful shutdown" }
Write-Host "Stopping $($vms.Count) worker VM(s) ($mode)..." -ForegroundColor Cyan

# Stop VMs
$stopped = @()
foreach ($vm in $vms) {
    if ($vm.State -eq 'Off') {
        Write-Host "  $($vm.Name): Already off" -ForegroundColor Gray
        continue
    }

    Write-Host "  Stopping $($vm.Name)..." -NoNewline
    try {
        if ($Force) {
            Stop-VM -Name $vm.Name -Force -TurnOff -ErrorAction Stop
        }
        else {
            Stop-VM -Name $vm.Name -Force -ErrorAction Stop
        }
        Write-Host " OK" -ForegroundColor Yellow
        $stopped += $vm.Name
    }
    catch {
        Write-Host " FAILED" -ForegroundColor Red
        Write-Error "Failed to stop '$($vm.Name)': $_"
    }
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Stopped: $($stopped.Count) VM(s)" -ForegroundColor Yellow

# Show status
Write-Host "`nCurrent status:" -ForegroundColor Cyan
Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" } |
    Select-Object Name, State, Uptime |
    Format-Table -AutoSize
