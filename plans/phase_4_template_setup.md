# Phase 4: Template VM Setup Guide

## Overview

This document provides detailed step-by-step instructions for creating the Template VM that will be cloned for worker VMs during training. This expands on task F4.T1 from `phase_4_detailed.md`.

## Prerequisites

Before starting:
- [ ] Host machine configured with Hyper-V (see `phase_0_prerequisites.md`)
- [ ] Phase 1 completed (TzarBot.GameInterface built)
- [ ] Windows 10/11 ISO available
- [ ] Network switch "TzarBotSwitch" created

## Step 1: Create Template VM

```powershell
# Run on Host as Administrator

# Verify switch exists (create if not)
if (-not (Get-VMSwitch -Name "TzarBotSwitch" -ErrorAction SilentlyContinue)) {
    New-VMSwitch -Name "TzarBotSwitch" -SwitchType Internal
}

# Create Template VM
New-VM -Name "TzarBot-Template" `
       -Generation 2 `
       -MemoryStartupBytes 4GB `
       -NewVHDPath "C:\VMs\TzarBot-Template.vhdx" `
       -NewVHDSizeBytes 50GB `
       -SwitchName "TzarBotSwitch"

# Configure VM resources
Set-VM -Name "TzarBot-Template" `
       -ProcessorCount 2 `
       -DynamicMemory `
       -MemoryMinimumBytes 2GB `
       -MemoryMaximumBytes 8GB `
       -CheckpointType Production

# Enable Enhanced Session (clipboard, display scaling)
Set-VM -Name "TzarBot-Template" -EnhancedSessionTransportType HvSocket

# Disable Secure Boot (may be needed for some Windows versions)
Set-VMFirmware -VMName "TzarBot-Template" -EnableSecureBoot Off

# Or use Microsoft certificate if Secure Boot is needed:
# Set-VMFirmware -VMName "TzarBot-Template" `
#                -SecureBootTemplate "MicrosoftUEFICertificateAuthority"

# Mount Windows ISO
Add-VMDvdDrive -VMName "TzarBot-Template" -Path "C:\ISOs\Windows.iso"

# Set boot order
$dvd = Get-VMDvdDrive -VMName "TzarBot-Template"
Set-VMFirmware -VMName "TzarBot-Template" -FirstBootDevice $dvd
```

## Step 2: Install Windows

1. Start VM:
   ```powershell
   Start-VM -Name "TzarBot-Template"
   ```

2. Connect via Hyper-V Manager (double-click VM)

3. Install Windows 10/11:
   - Select "Custom: Install Windows only"
   - Choose the virtual disk
   - Wait for installation (~10-15 minutes)

4. Initial setup:
   - **Region**: Your preference
   - **Keyboard**: Your preference
   - **Network**: Skip for now (configure later)
   - **Account**: Create LOCAL account named "TzarBot"
   - **Password**: Use a known password (e.g., `TzarBot123!`)
   - **Security questions**: Skip if possible
   - **Privacy**: Disable all telemetry options
   - **Cortana**: Decline

## Step 3: Configure Windows

After Windows boots to desktop, run these commands in PowerShell (as Administrator):

### 3.1 Set Static IP Address

```powershell
# Get the network adapter name (usually "Ethernet")
Get-NetAdapter | Format-Table Name, Status, MacAddress

# Set static IP (adjust adapter name if needed)
New-NetIPAddress -InterfaceAlias "Ethernet" `
                 -IPAddress 192.168.100.50 `
                 -PrefixLength 24 `
                 -DefaultGateway 192.168.100.1

# Set DNS servers
Set-DnsClientServerAddress -InterfaceAlias "Ethernet" `
                           -ServerAddresses 8.8.8.8,8.8.4.4
```

### 3.2 Configure Auto-Login

```powershell
# Configure automatic login
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
Set-ItemProperty -Path $regPath -Name AutoAdminLogon -Value "1"
Set-ItemProperty -Path $regPath -Name DefaultUserName -Value "TzarBot"
Set-ItemProperty -Path $regPath -Name DefaultPassword -Value "TzarBot123!"
# Replace password with your actual password
```

### 3.3 Disable Sleep and Screen Saver

```powershell
# Disable sleep
powercfg /change standby-timeout-ac 0
powercfg /change standby-timeout-dc 0
powercfg /change monitor-timeout-ac 0
powercfg /change monitor-timeout-dc 0
powercfg /change hibernate-timeout-ac 0

# Disable screen saver
Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name ScreenSaveActive -Value "0"
```

### 3.4 Disable UAC Prompts

```powershell
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" `
                 -Name "EnableLUA" -Value 0
```

### 3.5 Enable PowerShell Remoting

```powershell
Enable-PSRemoting -Force -SkipNetworkProfileCheck
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force

# Configure firewall
Enable-NetFirewallRule -DisplayGroup "Windows Remote Management"
Enable-NetFirewallRule -DisplayGroup "File and Printer Sharing"
```

### 3.6 Set Display Resolution

```powershell
# Set resolution to 1920x1080 (adjust as needed)
# This usually needs to be done via GUI or third-party tools
# In VM settings, set display resolution to desired size
```

## Step 4: Install Tzar Game

### 4.1 Transfer Game Installer

The game installer is located at: `files/tzared.windows.zip` in the project repository.

**On Host machine:**
```powershell
# Copy to shared folder
Copy-Item "C:\Users\maciek\ai_experiments\tzar_bot\files\tzared.windows.zip" `
          -Destination "C:\TzarBotShare\"

# Or copy directly to VM (requires Integration Services)
Copy-VMFile -VMName "TzarBot-Template" `
            -SourcePath "C:\Users\maciek\ai_experiments\tzar_bot\files\tzared.windows.zip" `
            -DestinationPath "C:\Users\TzarBot\Downloads\tzared.windows.zip" `
            -FileSource Host `
            -CreateFullPath
```

**In VM (if using shared folder):**
```powershell
# Map network drive
net use Z: \\192.168.100.1\TzarBotShare /persistent:yes

# Copy to local
Copy-Item "Z:\tzared.windows.zip" -Destination "C:\Users\TzarBot\Downloads\"
```

### 4.2 Extract and Install Game

In VM:
```powershell
# Create game directory
New-Item -Path "C:\Games" -ItemType Directory -Force

# Extract archive
Expand-Archive -Path "C:\Users\TzarBot\Downloads\tzared.windows.zip" `
               -DestinationPath "C:\Games\Tzar" -Force

# Find game executable
$exeFiles = Get-ChildItem "C:\Games\Tzar" -Recurse -Filter "*.exe"
$exeFiles | Format-Table FullName, Name
```

### 4.3 Configure Game Settings

1. **Launch the game** to create configuration files
2. **Configure graphics**:
   - Resolution: **1920x1080** (or your VM resolution)
   - Mode: **Windowed** (CRITICAL!)
   - VSync: On
   - Graphics quality: Medium (to reduce GPU load)
3. **Configure game**:
   - Skip intro videos (if option exists)
   - Set default AI difficulty
   - Create a quick skirmish preset

### 4.4 Compatibility Mode (if needed)

If game doesn't start:
```powershell
# Find the game executable path
$gamePath = "C:\Games\Tzar\Tzar.exe"  # Adjust as needed

# Set Windows XP SP3 compatibility
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" `
                 -Name $gamePath -Value "WINXPSP3 RUNASADMIN"

# Or use Windows 7 compatibility
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" `
                 -Name $gamePath -Value "WIN7RTM RUNASADMIN"
```

### 4.5 Test Game

1. Start a single player skirmish game
2. Play for 1-2 minutes
3. Verify:
   - [ ] Game renders correctly
   - [ ] Mouse input works
   - [ ] Keyboard input works
   - [ ] Can select and move units
   - [ ] Game exits cleanly

## Step 5: Install Bot Interface

### 5.1 Copy Bot Interface Files

**On Host:**
```powershell
# Build the bot interface (if not already built)
cd "C:\Users\maciek\ai_experiments\tzar_bot"
dotnet publish src/TzarBot.GameInterface -c Release -o publish/GameInterface

# Copy to VM
Copy-VMFile -VMName "TzarBot-Template" `
            -SourcePath "C:\Users\maciek\ai_experiments\tzar_bot\publish\GameInterface" `
            -DestinationPath "C:\TzarBot\" `
            -FileSource Host `
            -CreateFullPath
```

Or via shared folder:
```powershell
# Copy to share
Copy-Item "C:\Users\maciek\ai_experiments\tzar_bot\publish\GameInterface\*" `
          -Destination "C:\TzarBotShare\GameInterface\" -Recurse
```

### 5.2 Create Startup Script

In VM, create `C:\TzarBot\start_bot.bat`:
```batch
@echo off
echo Starting TzarBot...

cd C:\TzarBot
start "" TzarBot.GameInterface.exe

echo Waiting for bot interface to initialize...
timeout /t 5 /nobreak

echo Starting Tzar game...
start "" "C:\Games\Tzar\Tzar.exe"

echo Bot started successfully.
```

### 5.3 Configure Auto-Start

```powershell
# Get startup folder path
$startupPath = [Environment]::GetFolderPath("Startup")

# Create shortcut to startup script
$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut("$startupPath\TzarBot.lnk")
$shortcut.TargetPath = "C:\TzarBot\start_bot.bat"
$shortcut.WorkingDirectory = "C:\TzarBot"
$shortcut.Save()
```

## Step 6: Configure Named Pipes for Host Communication

### 6.1 Firewall Rules

```powershell
# Allow named pipe communication
New-NetFirewallRule -DisplayName "TzarBot Named Pipes" `
                    -Direction Inbound `
                    -Protocol TCP `
                    -LocalPort 445 `
                    -Action Allow

# Or allow all SMB traffic
Enable-NetFirewallRule -DisplayGroup "File and Printer Sharing"
```

### 6.2 Test Communication

From Host:
```powershell
# Test if VM is reachable
Test-Connection -ComputerName 192.168.100.50 -Count 4

# Test WinRM
Test-WSMan -ComputerName 192.168.100.50

# Test remote PowerShell
Enter-PSSession -ComputerName 192.168.100.50 -Credential TzarBot
```

## Step 7: Finalize Template

### 7.1 Clean Up

In VM:
```powershell
# Clean temp files
Remove-Item -Path "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue

# Clear download folder
Remove-Item -Path "C:\Users\TzarBot\Downloads\*" -Force -ErrorAction SilentlyContinue

# Run disk cleanup
cleanmgr /sagerun:1
```

### 7.2 Shutdown and Compact

```powershell
# Shutdown VM
Stop-VM -Name "TzarBot-Template" -Force

# Compact VHD (optional - reduces disk size)
Optimize-VHD -Path "C:\VMs\TzarBot-Template.vhdx" -Mode Full
```

### 7.3 Create Checkpoint

```powershell
# Create checkpoint as recovery point
Checkpoint-VM -Name "TzarBot-Template" -SnapshotName "Ready for Cloning"
```

## Verification Checklist

Before using template for cloning:

| Item | Status |
|------|--------|
| VM boots to desktop | [ ] |
| Auto-login works | [ ] |
| Tzar game launches | [ ] |
| Game runs in windowed mode | [ ] |
| Bot interface starts | [ ] |
| PowerShell remoting works from host | [ ] |
| Static IP configured | [ ] |
| Named pipes accessible | [ ] |

## Troubleshooting

### Game Won't Start
1. Check compatibility mode settings
2. Try running as Administrator
3. Install DirectX/Visual C++ redistributables
4. Check event viewer for errors

### Can't Connect from Host
1. Verify VM IP address: `ipconfig`
2. Check firewall rules
3. Test basic connectivity: `ping`
4. Verify WinRM: `winrm quickconfig`

### Bot Interface Errors
1. Check .NET runtime is installed
2. Verify all DLLs are present
3. Check Windows Event Viewer
4. Run manually to see error messages

---

*Template VM Setup Guide - Version 1.0*
*Part of Phase 4: Hyper-V Infrastructure*
