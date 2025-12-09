<#
.SYNOPSIS
    Prepares a template VM for TzarBot workers.

.DESCRIPTION
    This script helps prepare a Windows 10 VM as a template for TzarBot workers.
    It configures auto-login, startup scripts, and prepares the system for cloning.

    NOTE: This is a semi-automated script that requires manual intervention for some steps.

.PARAMETER VMName
    Name of the VM to prepare as template. Default: "DEV"

.PARAMETER Username
    Username for auto-login. Default: "test"

.PARAMETER Password
    Password for auto-login. Default: "password123"

.PARAMETER TzarPath
    Path to Tzar game executable. Default: "C:\Program Files\Tzared\Tzared.exe"

.PARAMETER BotPath
    Path to TzarBot interface. Default: "C:\TzarBot\TzarBot.GameInterface.exe"

.PARAMETER CreateCheckpoint
    Create a "Clean" checkpoint after preparation. Default: true

.EXAMPLE
    .\Prepare-TzarTemplate.ps1 -VMName "DEV"
    Prepares the DEV VM as a template

.EXAMPLE
    .\Prepare-TzarTemplate.ps1 -VMName "TzarBot-Template" -CreateCheckpoint
    Prepares VM and creates a Clean checkpoint

.NOTES
    Part of Phase 4: Hyper-V Infrastructure
    Requires: Hyper-V PowerShell module, Administrator privileges

    MANUAL STEPS REQUIRED:
    1. Install Windows 10 on the VM
    2. Install Tzar game
    3. Deploy TzarBot interface
    4. Run this script
    5. Verify everything works
    6. Create template VHDX
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$VMName = "DEV",

    [Parameter()]
    [string]$Username = "test",

    [Parameter()]
    [string]$Password = "password123",

    [Parameter()]
    [string]$TzarPath = "C:\Program Files\Tzared\Tzared.exe",

    [Parameter()]
    [string]$BotPath = "C:\TzarBot\TzarBot.GameInterface.exe",

    [Parameter()]
    [switch]$CreateCheckpoint
)

$ErrorActionPreference = "Stop"

Write-Host @"
===============================================
TzarBot Template VM Preparation Script
===============================================

This script will configure the VM for use as a worker template.

Target VM: $VMName
Auto-login: $Username

PREREQUISITES CHECKLIST:
[ ] Windows 10 installed on VM
[ ] Tzar game installed at: $TzarPath
[ ] TzarBot interface deployed to: $BotPath
[ ] PowerShell Direct enabled (test with: Enter-PSSession -VMName $VMName)

"@ -ForegroundColor Cyan

# Verify VM exists
$vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
if (-not $vm) {
    Write-Error "VM '$VMName' not found. Please create the VM first."
    exit 1
}

Write-Host "VM '$VMName' found. State: $($vm.State)" -ForegroundColor Green

# Verify VM is running
if ($vm.State -ne 'Running') {
    Write-Host "Starting VM..." -ForegroundColor Yellow
    Start-VM -Name $VMName
    Start-Sleep -Seconds 10
}

# Create credential for PowerShell Direct
$securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)

# Test PowerShell Direct
Write-Host "`nTesting PowerShell Direct connection..." -ForegroundColor Cyan
try {
    $testResult = Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
        Write-Output "PowerShell Direct OK - Hostname: $env:COMPUTERNAME"
    } -ErrorAction Stop
    Write-Host $testResult -ForegroundColor Green
}
catch {
    Write-Error @"
Failed to connect via PowerShell Direct.
Please ensure:
1. VM is running and booted
2. Correct username/password
3. PowerShell remoting is enabled in the VM
4. Try manually: Enter-PSSession -VMName $VMName -Credential (Get-Credential)
Error: $_
"@
    exit 1
}

Write-Host "`n=== Configuring VM ===" -ForegroundColor Cyan

# 1. Configure Auto-Login
Write-Host "`n[1/7] Configuring auto-login..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    param($user, $pass)

    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"

    Set-ItemProperty -Path $regPath -Name "AutoAdminLogon" -Value "1" -Type String
    Set-ItemProperty -Path $regPath -Name "DefaultUserName" -Value $user -Type String
    Set-ItemProperty -Path $regPath -Name "DefaultPassword" -Value $pass -Type String
    Set-ItemProperty -Path $regPath -Name "DefaultDomainName" -Value $env:COMPUTERNAME -Type String

    Write-Output "Auto-login configured for user: $user"
} -ArgumentList $Username, $Password
Write-Host "  Auto-login configured" -ForegroundColor Green

# 2. Create TzarBot directories
Write-Host "`n[2/7] Creating TzarBot directories..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    $dirs = @(
        "C:\TzarBot",
        "C:\TzarBot\Genomes",
        "C:\TzarBot\Results",
        "C:\TzarBot\Logs"
    )
    foreach ($dir in $dirs) {
        if (-not (Test-Path $dir)) {
            New-Item -Path $dir -ItemType Directory -Force | Out-Null
            Write-Output "  Created: $dir"
        } else {
            Write-Output "  Exists: $dir"
        }
    }
}
Write-Host "  Directories created" -ForegroundColor Green

# 3. Create startup script
Write-Host "`n[3/7] Creating startup script..." -ForegroundColor Yellow
$startupScript = @"
# TzarBot Startup Script
# Runs at user login to start Tzar and Bot interface

`$logPath = "C:\TzarBot\Logs\startup_`$(Get-Date -Format 'yyyyMMdd').log"

function Log {
    param([string]`$Message)
    "`$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - `$Message" | Out-File -FilePath `$logPath -Append
    Write-Host `$Message
}

Log "TzarBot startup script started"

# Wait for desktop to be ready
Start-Sleep -Seconds 5

# Start Tzar game (minimized/windowed)
`$tzarPath = "$TzarPath"
if (Test-Path `$tzarPath) {
    Log "Starting Tzar..."
    Start-Process -FilePath `$tzarPath -WindowStyle Normal
    Start-Sleep -Seconds 10
    Log "Tzar started"
} else {
    Log "ERROR: Tzar not found at `$tzarPath"
}

# Start Bot Interface
`$botPath = "$BotPath"
if (Test-Path `$botPath) {
    Log "Starting Bot Interface..."
    Start-Process -FilePath `$botPath -WindowStyle Hidden
    Log "Bot Interface started"
} else {
    Log "WARNING: Bot interface not found at `$botPath"
}

Log "Startup script completed"
"@

Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    param($script)
    $scriptPath = "C:\TzarBot\startup.ps1"
    Set-Content -Path $scriptPath -Value $script -Force
    Write-Output "Startup script saved to: $scriptPath"
} -ArgumentList $startupScript
Write-Host "  Startup script created" -ForegroundColor Green

# 4. Configure startup task
Write-Host "`n[4/7] Creating scheduled task for startup..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    param($user)

    $taskName = "TzarBotStartup"
    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue

    if ($existingTask) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }

    $action = New-ScheduledTaskAction -Execute "powershell.exe" `
        -Argument "-ExecutionPolicy Bypass -WindowStyle Hidden -File C:\TzarBot\startup.ps1"

    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $user

    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable

    Register-ScheduledTask -TaskName $taskName `
        -Action $action `
        -Trigger $trigger `
        -Settings $settings `
        -User $user `
        -RunLevel Highest `
        -Force | Out-Null

    Write-Output "Scheduled task '$taskName' created"
} -ArgumentList $Username
Write-Host "  Scheduled task created" -ForegroundColor Green

# 5. Disable Windows Update
Write-Host "`n[5/7] Disabling Windows Update..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    # Disable Windows Update service
    Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue
    Set-Service -Name wuauserv -StartupType Disabled -ErrorAction SilentlyContinue

    # Disable via registry
    $regPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"
    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
    }
    Set-ItemProperty -Path $regPath -Name "NoAutoUpdate" -Value 1 -Type DWord

    Write-Output "Windows Update disabled"
}
Write-Host "  Windows Update disabled" -ForegroundColor Green

# 6. Disable Windows Defender real-time protection
Write-Host "`n[6/7] Configuring Windows Defender..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    try {
        # Add exclusion for TzarBot folder
        Add-MpPreference -ExclusionPath "C:\TzarBot" -ErrorAction SilentlyContinue

        # Try to disable real-time protection (may require Tamper Protection disabled)
        Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction SilentlyContinue

        Write-Output "Windows Defender configured (exclusion added)"
    }
    catch {
        Write-Output "WARNING: Could not fully configure Defender - may need manual adjustment"
    }
}
Write-Host "  Windows Defender configured" -ForegroundColor Green

# 7. Verify Tzar installation
Write-Host "`n[7/7] Verifying installations..." -ForegroundColor Yellow
$verification = Invoke-Command -VMName $VMName -Credential $credential -ScriptBlock {
    param($tzarPath, $botPath)

    $results = @{
        TzarInstalled = Test-Path $tzarPath
        BotDeployed = Test-Path $botPath
        DirectoriesExist = (Test-Path "C:\TzarBot\Genomes") -and (Test-Path "C:\TzarBot\Results")
        StartupScriptExists = Test-Path "C:\TzarBot\startup.ps1"
    }

    $results
} -ArgumentList $TzarPath, $BotPath

Write-Host "`nVerification Results:" -ForegroundColor Cyan
Write-Host "  Tzar Installed:      $(if ($verification.TzarInstalled) { 'YES' } else { 'NO - Please install Tzar' })" -ForegroundColor $(if ($verification.TzarInstalled) { 'Green' } else { 'Red' })
Write-Host "  Bot Deployed:        $(if ($verification.BotDeployed) { 'YES' } else { 'NO - Please deploy TzarBot interface' })" -ForegroundColor $(if ($verification.BotDeployed) { 'Yellow' } else { 'Yellow' })
Write-Host "  Directories Exist:   $(if ($verification.DirectoriesExist) { 'YES' } else { 'NO' })" -ForegroundColor $(if ($verification.DirectoriesExist) { 'Green' } else { 'Red' })
Write-Host "  Startup Script:      $(if ($verification.StartupScriptExists) { 'YES' } else { 'NO' })" -ForegroundColor $(if ($verification.StartupScriptExists) { 'Green' } else { 'Red' })

# Create checkpoint if requested
if ($CreateCheckpoint) {
    Write-Host "`n=== Creating Clean Checkpoint ===" -ForegroundColor Cyan

    # Stop VM first for consistent checkpoint
    Write-Host "Stopping VM for checkpoint..." -ForegroundColor Yellow
    Stop-VM -Name $VMName -Force
    Start-Sleep -Seconds 5

    # Remove old checkpoint if exists
    $existingCheckpoint = Get-VMSnapshot -VMName $VMName -Name "Clean" -ErrorAction SilentlyContinue
    if ($existingCheckpoint) {
        Write-Host "Removing existing 'Clean' checkpoint..."
        Remove-VMSnapshot -VMName $VMName -Name "Clean" -Confirm:$false
        Start-Sleep -Seconds 3
    }

    # Create new checkpoint
    Write-Host "Creating 'Clean' checkpoint..."
    Checkpoint-VM -Name $VMName -SnapshotName "Clean"

    # Start VM again
    Write-Host "Starting VM..."
    Start-VM -Name $VMName

    Write-Host "Clean checkpoint created" -ForegroundColor Green
}

Write-Host @"

===============================================
Template Preparation Complete!
===============================================

Next Steps:
1. Start the VM and verify auto-login works
2. Verify Tzar game launches automatically
3. Test bot interface connectivity
4. $(if ($CreateCheckpoint) { "Checkpoint 'Clean' has been created" } else { "Create a checkpoint: Checkpoint-VM -Name $VMName -SnapshotName 'Clean'" })
5. Export template VHDX:
   Stop-VM -Name $VMName -Force
   Copy-Item "C:\ProgramData\Microsoft\Windows\Virtual Hard Disks\$VMName.vhdx" "C:\VMs\TzarBot-Template.vhdx"

To create worker VMs from this template:
   .\New-TzarWorkerVM.ps1 -Count 3

"@ -ForegroundColor Cyan
