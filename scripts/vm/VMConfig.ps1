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
    MemoryStartupMB   = 4096
    MemoryMinMB       = 2048
    MemoryMaxMB       = 8192
    ProcessorCount    = 2

    # Defaults
    DefaultWorkerCount = 8
    MaxWorkerCount     = 16

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
