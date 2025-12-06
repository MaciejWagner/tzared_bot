# Phase 0: Prerequisites - Setup Guide

## Overview

This document describes all prerequisites required before starting development on the Tzar Bot project. Completing these steps ensures a smooth development experience.

## Task Dependency Diagram

```
F0.T1 (Host Setup)
   │
   ├──────────────────┐
   │                  │
   ▼                  ▼
F0.T2              F0.T3
(Dev VM)           (Game Install)
   │                  │
   └────────┬─────────┘
            │
            ▼
         F0.T4
    (Verification)
```

## Definition of Done - Phase 0

- [ ] Host machine configured with Hyper-V
- [ ] Development VM created and accessible
- [ ] Tzar game installed and running in Dev VM
- [ ] All required software installed
- [ ] Network connectivity verified
- [ ] Git tag: `phase-0-complete`

---

## Task Definitions

### F0.T1: Host Machine Setup

```yaml
task_id: "F0.T1"
name: "Host Machine Setup"
description: |
  Configure the host machine with all required software and Hyper-V.
  This is the physical machine that will run the VMs.

inputs:
  - "Windows 10/11 Pro or Enterprise (required for Hyper-V)"

outputs:
  - "Hyper-V enabled and functional"
  - "All development tools installed"
  - "Internal network switch created"

test_criteria: |
  - Hyper-V feature enabled
  - Virtual switches visible in Hyper-V Manager
  - PowerShell can run Get-VM without errors
  - .NET 8 SDK installed and working

dependencies: []
estimated_complexity: "S"

manual_steps: |
  ## Hardware Requirements

  Minimum recommended configuration for development:
  - **CPU**: 4+ cores (8+ recommended for parallel VM testing)
  - **RAM**: 16GB minimum (32GB recommended)
  - **Disk**: 100GB SSD free space (200GB for full training)
  - **GPU**: Not required for training (CPU-based inference)

  ## Software Installation

  ### 1. Enable Hyper-V

  Run PowerShell as Administrator:
  ```powershell
  # Check if Hyper-V is available
  Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V

  # Enable Hyper-V
  Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All

  # Restart required after enabling
  Restart-Computer
  ```

  After restart, verify:
  ```powershell
  Get-VM  # Should work without errors (empty list is OK)
  ```

  ### 2. Install Development Tools

  #### .NET 8 SDK
  Download from: https://dotnet.microsoft.com/download/dotnet/8.0

  Verify installation:
  ```powershell
  dotnet --version
  # Should show 8.x.x
  ```

  #### Git
  Download from: https://git-scm.com/download/win

  #### Visual Studio Code (recommended)
  Download from: https://code.visualstudio.com/

  Extensions to install:
  - C# Dev Kit
  - PowerShell

  #### PowerShell 7 (optional but recommended)
  ```powershell
  winget install Microsoft.PowerShell
  ```

  ### 3. Create Hyper-V Network Switch

  ```powershell
  # Create internal switch for VM communication
  New-VMSwitch -Name "TzarBotSwitch" -SwitchType Internal

  # Optionally configure NAT for internet access in VMs
  New-NetIPAddress -IPAddress 192.168.100.1 -PrefixLength 24 -InterfaceAlias "vEthernet (TzarBotSwitch)"
  New-NetNat -Name "TzarBotNAT" -InternalIPInterfaceAddressPrefix 192.168.100.0/24
  ```

  ### 4. Create Project Directories

  ```powershell
  # Create VM storage directory
  New-Item -Path "C:\VMs" -ItemType Directory -Force
  New-Item -Path "C:\VMs\Workers" -ItemType Directory -Force

  # Create shared folder for file exchange
  New-Item -Path "C:\TzarBotShare" -ItemType Directory -Force
  ```

validation_checklist:
  - [ ] Hyper-V Manager opens without errors
  - [ ] TzarBotSwitch visible in Virtual Switch Manager
  - [ ] `dotnet --version` shows 8.x.x
  - [ ] `git --version` works
  - [ ] C:\VMs directory exists
```

---

### F0.T2: Development VM Setup

```yaml
task_id: "F0.T2"
name: "Development VM Setup"
description: |
  Create a dedicated Hyper-V virtual machine for development and testing.
  This VM isolates the bot development from the host system and allows
  safe testing of screen capture and input injection.

inputs:
  - "Windows 10/11 ISO (from MSDN, VLSC, or Media Creation Tool)"
  - "TzarBotSwitch (from F0.T1)"

outputs:
  - "TzarBot-Dev VM created and running"
  - "Development tools installed in VM"
  - "Network connectivity configured"

test_criteria: |
  - VM boots successfully
  - Can RDP or Enhanced Session to VM
  - Internet access works (for package downloads)
  - .NET 8 SDK installed in VM

dependencies: ["F0.T1"]
estimated_complexity: "M"

manual_steps: |
  ## Step-by-Step Development VM Setup

  ### 1. Obtain Windows ISO

  Options:
  - **Windows 10 Media Creation Tool**: https://www.microsoft.com/software-download/windows10
  - **Windows 11 ISO**: https://www.microsoft.com/software-download/windows11
  - **MSDN/Visual Studio Subscription**: For development licenses

  Save ISO to `C:\ISOs\Windows.iso`

  ### 2. Create Development VM

  ```powershell
  # Create VM
  New-VM -Name "TzarBot-Dev" `
         -Generation 2 `
         -MemoryStartupBytes 8GB `
         -NewVHDPath "C:\VMs\TzarBot-Dev.vhdx" `
         -NewVHDSizeBytes 100GB `
         -SwitchName "TzarBotSwitch"

  # Configure VM for development (more resources than workers)
  Set-VM -Name "TzarBot-Dev" `
         -ProcessorCount 4 `
         -DynamicMemory `
         -MemoryMinimumBytes 4GB `
         -MemoryMaximumBytes 16GB `
         -CheckpointType Production

  # Enable Enhanced Session Mode (for clipboard, better display)
  Set-VM -Name "TzarBot-Dev" -EnhancedSessionTransportType HvSocket

  # Mount Windows ISO
  Add-VMDvdDrive -VMName "TzarBot-Dev" -Path "C:\ISOs\Windows.iso"

  # Set boot order to DVD first
  $dvd = Get-VMDvdDrive -VMName "TzarBot-Dev"
  Set-VMFirmware -VMName "TzarBot-Dev" -FirstBootDevice $dvd
  ```

  ### 3. Install Windows in VM

  1. Start VM: `Start-VM -Name "TzarBot-Dev"`
  2. Connect via Hyper-V Manager (double-click VM)
  3. Install Windows:
     - Choose "Custom: Install Windows only"
     - Select the virtual disk
     - Wait for installation to complete
  4. Create local account "Developer" (use a password you'll remember)
  5. Complete OOBE (Out-of-Box Experience)

  ### 4. Post-Installation Configuration

  Inside the VM:

  #### Set Static IP (for easy access)
  ```powershell
  # In the VM - set static IP
  New-NetIPAddress -InterfaceAlias "Ethernet" -IPAddress 192.168.100.10 -PrefixLength 24 -DefaultGateway 192.168.100.1
  Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ServerAddresses 8.8.8.8,8.8.4.4
  ```

  #### Enable Remote Desktop
  ```powershell
  Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name "fDenyTSConnections" -Value 0
  Enable-NetFirewallRule -DisplayGroup "Remote Desktop"
  ```

  #### Install Development Tools
  ```powershell
  # Install .NET 8 SDK
  winget install Microsoft.DotNet.SDK.8

  # Install Git
  winget install Git.Git

  # Install VS Code (optional)
  winget install Microsoft.VisualStudioCode
  ```

  ### 5. Configure Shared Folders

  On Host:
  ```powershell
  # Share folder for file exchange
  New-SmbShare -Name "TzarBotShare" -Path "C:\TzarBotShare" -FullAccess "Everyone"
  ```

  In VM:
  ```powershell
  # Map network drive to host share
  # Use host's IP on the TzarBotSwitch (192.168.100.1)
  net use Z: \\192.168.100.1\TzarBotShare /persistent:yes
  ```

  ### 6. Create VM Checkpoint

  ```powershell
  # On host - create checkpoint before installing game
  Checkpoint-VM -Name "TzarBot-Dev" -SnapshotName "Clean Install"
  ```

validation_checklist:
  - [ ] VM boots to Windows desktop
  - [ ] Can connect via RDP from host: `mstsc /v:192.168.100.10`
  - [ ] Internet works in VM (test: `ping google.com`)
  - [ ] `dotnet --version` shows 8.x.x in VM
  - [ ] Shared folder accessible in VM
```

---

### F0.T3: Tzar Game Installation

```yaml
task_id: "F0.T3"
name: "Tzar Game Installation"
description: |
  Install and configure the Tzar game in the Development VM.
  The game installer is provided in the project files.

inputs:
  - "files/tzared.windows.zip (game installer)"
  - "TzarBot-Dev VM (from F0.T2)"

outputs:
  - "Tzar game installed and running"
  - "Game configured for bot usage"
  - "Known window class/title documented"

test_criteria: |
  - Game starts without errors
  - Can start a skirmish game
  - Game runs in windowed mode
  - Window can be detected by title

dependencies: ["F0.T2"]
estimated_complexity: "S"

manual_steps: |
  ## Tzar Game Installation Guide

  ### 1. Copy Installer to VM

  The game installer is located at: `files/tzared.windows.zip`

  Options to transfer:

  **Option A: Via Shared Folder**
  ```powershell
  # On host - copy to shared folder
  Copy-Item "C:\Users\maciek\ai_experiments\tzar_bot\files\tzared.windows.zip" -Destination "C:\TzarBotShare\"
  ```

  Then in VM:
  ```powershell
  # Copy from mapped drive
  Copy-Item "Z:\tzared.windows.zip" -Destination "C:\Users\Developer\Downloads\"
  ```

  **Option B: Via Enhanced Session clipboard**
  - Connect to VM with Enhanced Session
  - Drag and drop or copy-paste the file

  ### 2. Extract and Install

  In the VM:
  ```powershell
  # Create game directory
  New-Item -Path "C:\Games" -ItemType Directory -Force

  # Extract archive
  Expand-Archive -Path "C:\Users\Developer\Downloads\tzared.windows.zip" -DestinationPath "C:\Games\Tzar" -Force
  ```

  ### 3. Run Game and Initial Setup

  1. Navigate to `C:\Games\Tzar`
  2. Find and run the game executable (likely `Tzar.exe` or similar)
  3. If the game doesn't start:
     - Try running as Administrator
     - Try compatibility mode (Windows XP/7)
     - Install DirectX/Visual C++ redistributables if prompted

  ### 4. Configure Game Settings

  Once the game runs, configure these settings:

  #### Graphics Settings
  - **Resolution**: 1920x1080 (or fixed resolution matching VM display)
  - **Mode**: Windowed (important for screen capture)
  - **Skip Intro Videos**: Enable if available

  #### Game Settings
  - Create a skirmish profile for quick testing
  - Note the AI difficulty options available

  ### 5. Document Window Information

  Use this PowerShell script in the VM to find window details:
  ```powershell
  Add-Type @"
  using System;
  using System.Runtime.InteropServices;
  using System.Text;
  public class Win32 {
      [DllImport("user32.dll")]
      public static extern IntPtr GetForegroundWindow();

      [DllImport("user32.dll")]
      public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

      [DllImport("user32.dll")]
      public static extern int GetClassName(IntPtr hWnd, StringBuilder className, int count);
  }
  "@

  # With game window focused, run:
  $hwnd = [Win32]::GetForegroundWindow()
  $title = New-Object System.Text.StringBuilder 256
  $class = New-Object System.Text.StringBuilder 256
  [Win32]::GetWindowText($hwnd, $title, 256)
  [Win32]::GetClassName($hwnd, $class, 256)

  Write-Host "Window Title: $($title.ToString())"
  Write-Host "Window Class: $($class.ToString())"
  ```

  Record these values - they will be used in Phase 1 for window detection.

  ### 6. Test Game Functionality

  Verify the following work:
  - [ ] Game launches without errors
  - [ ] Main menu is visible and responsive
  - [ ] Can start a new skirmish game
  - [ ] Game renders correctly
  - [ ] Can control units with mouse/keyboard
  - [ ] Can exit game cleanly

  ### 7. Create Post-Game Checkpoint

  On host:
  ```powershell
  Checkpoint-VM -Name "TzarBot-Dev" -SnapshotName "With Tzar Installed"
  ```

game_information:
  source: "files/tzared.windows.zip"
  website: "https://tza.red/"
  notes: |
    - This is TZA.RED version of Tzar (community-maintained)
    - May require Windows compatibility mode
    - Runs on modern Windows with DirectX support

validation_checklist:
  - [ ] Game executable found and runs
  - [ ] Game displays correctly in windowed mode
  - [ ] Can start and play a skirmish game
  - [ ] Window title and class documented
  - [ ] Checkpoint created with game installed
```

---

### F0.T4: Environment Verification

```yaml
task_id: "F0.T4"
name: "Environment Verification"
description: |
  Verify that the complete development environment is working correctly.
  Run diagnostic checks and document the configuration.

inputs:
  - "Completed F0.T1, F0.T2, F0.T3"

outputs:
  - "plans/phase_0_verification.md (environment documentation)"
  - "All verification tests pass"

test_criteria: |
  - Host can communicate with Dev VM
  - Game runs in VM
  - Screen capture possible (test with built-in tools)
  - All paths and configurations documented

dependencies: ["F0.T1", "F0.T2", "F0.T3"]
estimated_complexity: "S"

manual_steps: |
  ## Verification Checklist

  Run these checks to verify the environment is ready:

  ### 1. Host-VM Connectivity

  From host:
  ```powershell
  # Ping VM
  Test-Connection -ComputerName 192.168.100.10 -Count 4

  # Test RDP
  Test-NetConnection -ComputerName 192.168.100.10 -Port 3389

  # Test VM status
  Get-VM -Name "TzarBot-Dev" | Select-Object Name, State, CPUUsage, MemoryAssigned
  ```

  ### 2. Development Tools in VM

  Via RDP or Enhanced Session:
  ```powershell
  # Check .NET
  dotnet --list-sdks

  # Check Git
  git --version

  # Check PowerShell
  $PSVersionTable.PSVersion
  ```

  ### 3. Game Test

  In VM:
  1. Launch Tzar game
  2. Start a skirmish game
  3. Play for 30 seconds
  4. Exit cleanly
  5. Verify no errors/crashes

  ### 4. Screen Capture Test (Preview)

  Use Windows built-in tools to verify screen capture works:
  ```powershell
  # In VM - take screenshot with game running
  Add-Type -AssemblyName System.Windows.Forms
  [System.Windows.Forms.Screen]::PrimaryScreen | Format-List *

  # Use Snipping Tool or Win+Shift+S to capture game window
  ```

  ### 5. Document Configuration

  Create `plans/phase_0_verification.md` with:
  - Host hardware specs
  - VM configuration
  - Network settings
  - Game window title/class
  - Any compatibility settings used

verification_script: |
  # Run this on host to verify setup
  Write-Host "=== TzarBot Environment Verification ===" -ForegroundColor Cyan

  # Check Hyper-V
  Write-Host "`n[1] Checking Hyper-V..." -ForegroundColor Yellow
  $hyperv = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V
  if ($hyperv.State -eq "Enabled") {
      Write-Host "    Hyper-V: ENABLED" -ForegroundColor Green
  } else {
      Write-Host "    Hyper-V: NOT ENABLED" -ForegroundColor Red
  }

  # Check VM
  Write-Host "`n[2] Checking Dev VM..." -ForegroundColor Yellow
  $vm = Get-VM -Name "TzarBot-Dev" -ErrorAction SilentlyContinue
  if ($vm) {
      Write-Host "    VM Found: $($vm.Name)" -ForegroundColor Green
      Write-Host "    State: $($vm.State)"
      Write-Host "    Memory: $([math]::Round($vm.MemoryAssigned / 1GB, 1)) GB"
  } else {
      Write-Host "    Dev VM not found!" -ForegroundColor Red
  }

  # Check network
  Write-Host "`n[3] Checking Network..." -ForegroundColor Yellow
  $switch = Get-VMSwitch -Name "TzarBotSwitch" -ErrorAction SilentlyContinue
  if ($switch) {
      Write-Host "    Switch: TzarBotSwitch FOUND" -ForegroundColor Green
  } else {
      Write-Host "    Switch not found!" -ForegroundColor Red
  }

  # Check connectivity
  Write-Host "`n[4] Checking VM Connectivity..." -ForegroundColor Yellow
  if (Test-Connection -ComputerName 192.168.100.10 -Count 1 -Quiet) {
      Write-Host "    VM Ping: SUCCESS" -ForegroundColor Green
  } else {
      Write-Host "    VM Ping: FAILED (VM may be off or IP different)" -ForegroundColor Yellow
  }

  # Check .NET
  Write-Host "`n[5] Checking .NET SDK..." -ForegroundColor Yellow
  $dotnet = dotnet --version 2>$null
  if ($dotnet -like "8.*") {
      Write-Host "    .NET SDK: $dotnet" -ForegroundColor Green
  } else {
      Write-Host "    .NET 8 not found!" -ForegroundColor Red
  }

  # Check game installer
  Write-Host "`n[6] Checking Game Installer..." -ForegroundColor Yellow
  $gameZip = "C:\Users\maciek\ai_experiments\tzar_bot\files\tzared.windows.zip"
  if (Test-Path $gameZip) {
      $size = [math]::Round((Get-Item $gameZip).Length / 1MB, 1)
      Write-Host "    Game ZIP: FOUND ($size MB)" -ForegroundColor Green
  } else {
      Write-Host "    Game ZIP not found!" -ForegroundColor Red
  }

  Write-Host "`n=== Verification Complete ===" -ForegroundColor Cyan

validation_checklist:
  - [ ] All connectivity tests pass
  - [ ] Game runs in VM without issues
  - [ ] Configuration documented
  - [ ] Ready to proceed to Phase 1
```

---

## Summary

After completing Phase 0:

| Component | Status | Location |
|-----------|--------|----------|
| Host Hyper-V | Enabled | Host machine |
| Development VM | Running | TzarBot-Dev |
| Network Switch | Created | TzarBotSwitch |
| Tzar Game | Installed | C:\Games\Tzar (in VM) |
| .NET 8 SDK | Installed | Both host and VM |

## Next Steps

After Phase 0 is complete:
1. Proceed to Phase 1 (Game Interface)
2. Development and testing will happen primarily in TzarBot-Dev VM
3. Template VM (for workers) will be created in Phase 4

---

*Phase 0 Prerequisites - Version 1.0*
