<#
.SYNOPSIS
    Shows status of TzarBot worker VMs.

.DESCRIPTION
    Displays detailed status information for all TzarBot worker VMs including
    state, resource usage, uptime, and network connectivity.

.PARAMETER Detailed
    Show extended information including IP addresses.

.EXAMPLE
    .\Get-TzarWorkerStatus.ps1
    Shows basic status of all worker VMs

.EXAMPLE
    .\Get-TzarWorkerStatus.ps1 -Detailed
    Shows detailed status including network information

.NOTES
    Part of Phase 4: Hyper-V Infrastructure
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$Detailed
)

# Import configuration
. "$PSScriptRoot\VMConfig.ps1"
$config = Get-VMConfig

# Get all worker VMs
$vms = Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" } | Sort-Object Name

if ($vms.Count -eq 0) {
    Write-Host "No worker VMs found." -ForegroundColor Yellow
    Write-Host "Create workers with: .\New-TzarWorkerVM.ps1 -Count 8"
    exit 0
}

# Header
Write-Host "`n=== TzarBot Worker VMs ===" -ForegroundColor Cyan
Write-Host "Total: $($vms.Count) VMs`n"

# Basic status
$statusTable = $vms | ForEach-Object {
    [PSCustomObject]@{
        Name      = $_.Name
        State     = $_.State
        CPU       = "$($_.CPUUsage)%"
        Memory    = "$([math]::Round($_.MemoryAssigned / 1GB, 1)) GB"
        Uptime    = if ($_.Uptime.TotalMinutes -gt 0) {
            "{0:hh\:mm\:ss}" -f $_.Uptime
        } else { "-" }
        Heartbeat = switch ($_.Heartbeat) {
            'OkApplicationsHealthy' { 'OK' }
            'OkApplicationsUnknown' { 'OK (apps unknown)' }
            'NoContact'             { 'No contact' }
            default                 { $_ }
        }
    }
}

$statusTable | Format-Table -AutoSize

# Detailed info
if ($Detailed) {
    Write-Host "`n=== Detailed Information ===" -ForegroundColor Cyan

    foreach ($vm in $vms) {
        Write-Host "`n$($vm.Name):" -ForegroundColor Yellow

        # Network adapters
        $adapters = Get-VMNetworkAdapter -VMName $vm.Name
        foreach ($adapter in $adapters) {
            Write-Host "  Network: $($adapter.SwitchName)"
            if ($adapter.IPAddresses) {
                Write-Host "  IP: $($adapter.IPAddresses -join ', ')"
            }
            Write-Host "  MAC: $($adapter.MacAddress)"
        }

        # VHD info
        $vhd = $vm.HardDrives | Select-Object -First 1
        if ($vhd) {
            $vhdInfo = Get-VHD -Path $vhd.Path -ErrorAction SilentlyContinue
            if ($vhdInfo) {
                Write-Host "  VHD: $([math]::Round($vhdInfo.FileSize / 1GB, 2)) GB (used)"
                Write-Host "  VHD Path: $($vhd.Path)"
            }
        }

        # Integration services
        $integration = Get-VMIntegrationService -VMName $vm.Name |
            Where-Object { $_.Enabled -and $_.PrimaryStatusDescription -ne 'OK' }
        if ($integration) {
            Write-Host "  Integration Issues:" -ForegroundColor Yellow
            $integration | ForEach-Object {
                Write-Host "    - $($_.Name): $($_.PrimaryStatusDescription)"
            }
        }
    }
}

# Summary statistics
Write-Host "`n=== Summary ===" -ForegroundColor Cyan

$running = ($vms | Where-Object { $_.State -eq 'Running' }).Count
$off = ($vms | Where-Object { $_.State -eq 'Off' }).Count
$healthy = ($vms | Where-Object { $_.Heartbeat -eq 'OkApplicationsHealthy' }).Count
$totalMemory = ($vms | Measure-Object -Property MemoryAssigned -Sum).Sum / 1GB

Write-Host "Running: $running / $($vms.Count)"
Write-Host "Healthy: $healthy / $running (of running)"
Write-Host "Total Memory: $([math]::Round($totalMemory, 1)) GB"

# Quick commands
Write-Host "`n=== Quick Commands ===" -ForegroundColor Gray
Write-Host "Start all:  .\Start-TzarWorkers.ps1 -All -Wait"
Write-Host "Stop all:   .\Stop-TzarWorkers.ps1 -All"
Write-Host "Remove all: .\Remove-TzarWorkerVM.ps1 -All"
