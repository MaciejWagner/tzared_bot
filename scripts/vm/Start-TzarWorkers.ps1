<#
.SYNOPSIS
    Starts TzarBot worker VMs.

.DESCRIPTION
    Starts specified worker VMs and optionally waits for them to be fully booted.

.PARAMETER Names
    Specific VM names to start.

.PARAMETER All
    Start all TzarBot worker VMs.

.PARAMETER Wait
    Wait for VMs to boot and report healthy heartbeat.

.PARAMETER TimeoutSeconds
    Maximum time to wait for VMs to boot (default: 120 seconds).

.EXAMPLE
    .\Start-TzarWorkers.ps1 -All
    Starts all worker VMs

.EXAMPLE
    .\Start-TzarWorkers.ps1 -All -Wait
    Starts all worker VMs and waits for them to boot

.EXAMPLE
    .\Start-TzarWorkers.ps1 -Names "TzarBot-Worker-0", "TzarBot-Worker-1" -Wait
    Starts specific VMs and waits for them

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
    [switch]$Wait,

    [Parameter()]
    [int]$TimeoutSeconds = 120
)

# Import configuration
. "$PSScriptRoot\VMConfig.ps1"
$config = Get-VMConfig

# Get VMs to start
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

Write-Host "Starting $($vms.Count) worker VM(s)..." -ForegroundColor Cyan

# Start VMs
$started = @()
foreach ($vm in $vms) {
    if ($vm.State -eq 'Running') {
        Write-Host "  $($vm.Name): Already running" -ForegroundColor Gray
        $started += $vm.Name
        continue
    }

    Write-Host "  Starting $($vm.Name)..." -NoNewline
    try {
        Start-VM -Name $vm.Name -ErrorAction Stop
        Write-Host " OK" -ForegroundColor Green
        $started += $vm.Name
    }
    catch {
        Write-Host " FAILED" -ForegroundColor Red
        Write-Error "Failed to start '$($vm.Name)': $_"
    }
}

# Wait for heartbeat if requested
if ($Wait -and $started.Count -gt 0) {
    Write-Host "`nWaiting for VMs to boot (timeout: ${TimeoutSeconds}s)..." -ForegroundColor Cyan

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $ready = @()
    $pending = $started.Clone()

    while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds -and $pending.Count -gt 0) {
        foreach ($vmName in $pending.Clone()) {
            $vm = Get-VM -Name $vmName -ErrorAction SilentlyContinue
            if ($vm.Heartbeat -eq 'OkApplicationsHealthy') {
                Write-Host "  $vmName: Ready" -ForegroundColor Green
                $ready += $vmName
                $pending = $pending | Where-Object { $_ -ne $vmName }
            }
        }

        if ($pending.Count -gt 0) {
            $elapsed = [math]::Round($stopwatch.Elapsed.TotalSeconds)
            Write-Host "`r  Waiting... ($elapsed/${TimeoutSeconds}s) - $($pending.Count) pending  " -NoNewline
            Start-Sleep -Seconds $config.HeartbeatCheckIntervalSeconds
        }
    }

    Write-Host ""  # New line after progress
    $stopwatch.Stop()

    if ($pending.Count -gt 0) {
        Write-Warning "Timeout! These VMs did not become ready: $($pending -join ', ')"
    }

    Write-Host "`n=== Boot Summary ===" -ForegroundColor Cyan
    Write-Host "Ready: $($ready.Count)/$($started.Count)" -ForegroundColor $(if ($ready.Count -eq $started.Count) { 'Green' } else { 'Yellow' })
    Write-Host "Time: $([math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) seconds"
}

# Show status
Write-Host "`nCurrent status:" -ForegroundColor Cyan
Get-VM | Where-Object { $started -contains $_.Name } |
    Select-Object Name, State,
        @{L='CPU (%)'; E={$_.CPUUsage}},
        @{L='Memory (GB)'; E={[math]::Round($_.MemoryAssigned / 1GB, 1)}},
        Heartbeat |
    Format-Table -AutoSize
