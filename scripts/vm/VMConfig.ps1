# VMConfig.ps1 - Configuration for TzarBot VM Management
# Part of Phase 4: Hyper-V Infrastructure

$Script:VMConfig = @{
    # Paths
    TemplatePath      = "C:\VMs\TzarBot-Template.vhdx"
    WorkersPath       = "C:\VMs\Workers"

    # VM Naming
    VMPrefix          = "TzarBot-Worker-"
    TemplateVMName    = "TzarBot-Template"

    # Network
    SwitchName        = "TzarBotSwitch"
    BaseIPAddress     = "192.168.100"  # Workers will be .100, .101, etc.

    # VM Resources
    # IMPORTANT: 10GB RAM HARD LIMIT for all VMs (DEV=4GB, Workers=6GB)
    # With 3 workers at 2GB each = 6GB total for workers
    MemoryStartupMB   = 2048    # 2GB per worker (changed from 4GB to respect limit)
    MemoryMinMB       = 1024    # 1GB minimum
    MemoryMaxMB       = 3072    # 3GB max to allow dynamic growth
    ProcessorCount    = 2

    # Defaults - respecting 10GB RAM limit
    # DEV (4GB) + 3 workers (3x2GB=6GB) = 10GB
    DefaultWorkerCount = 3
    MaxWorkerCount     = 6      # Maximum if each worker uses 1GB

    # Resource Limits
    TotalRAMLimitGB    = 10     # HARD LIMIT
    DevVMRAMGB         = 4      # Reserved for DEV VM
    WorkerPoolRAMGB    = 6      # Available for workers

    # Timeouts
    BootTimeoutSeconds = 120
    HeartbeatCheckIntervalSeconds = 5
}

function Get-VMConfig {
    <#
    .SYNOPSIS
        Returns the VM configuration hashtable.
    .DESCRIPTION
        Provides access to all VM management settings.
    .EXAMPLE
        $config = Get-VMConfig
        Write-Host "Template: $($config.TemplatePath)"
    #>
    return $Script:VMConfig
}

function Test-VMConfig {
    <#
    .SYNOPSIS
        Validates the VM configuration.
    .DESCRIPTION
        Checks if all required paths and resources exist.
    .EXAMPLE
        if (Test-VMConfig) { Write-Host "Config OK" }
    #>
    $config = Get-VMConfig
    $valid = $true

    # Check template VHD
    if (-not (Test-Path $config.TemplatePath)) {
        Write-Warning "Template VHD not found: $($config.TemplatePath)"
        $valid = $false
    }

    # Check switch
    $switch = Get-VMSwitch -Name $config.SwitchName -ErrorAction SilentlyContinue
    if (-not $switch) {
        Write-Warning "VM Switch not found: $($config.SwitchName)"
        $valid = $false
    }

    # Check workers directory (create if missing)
    if (-not (Test-Path $config.WorkersPath)) {
        Write-Host "Creating workers directory: $($config.WorkersPath)"
        New-Item -Path $config.WorkersPath -ItemType Directory -Force | Out-Null
    }

    return $valid
}

# Export functions
Export-ModuleMember -Function Get-VMConfig, Test-VMConfig -Variable VMConfig
