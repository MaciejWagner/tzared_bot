# Phase 4: Hyper-V Infrastructure - Detailed Plan

## Overview

The Hyper-V Infrastructure enables parallel training by running multiple instances of the game on virtual machines. The orchestrator manages VM lifecycle and distributes genomes for evaluation.

## Task Dependency Diagram

```
F4.T1 (Template VM) [MANUAL]
   │
   ▼
F4.T2 (VM Cloning Scripts)
   │
   ▼
F4.T3 (VM Manager)
   │
   ├──────────────┐
   │              │
   ▼              ▼
F4.T4          F4.T5
(Orchestrator) (Communication)
   │              │
   └──────┬───────┘
          │
          ▼
       F4.T6
   (Integration)
```

## Definition of Done - Phase 4

- [ ] All 6 tasks completed with passing tests
- [ ] Template VM prepared with game installed
- [ ] Scripts can create/delete worker VMs
- [ ] Orchestrator manages VM pool
- [ ] Genomes can be sent to VMs for evaluation
- [ ] Results collected from VMs
- [ ] Demo: 4 VMs running parallel evaluations
- [ ] Git tag: `phase-4-complete`

---

## Task Definitions

### F4.T1: Template VM Preparation (Manual)

```yaml
task_id: "F4.T1"
name: "Template VM Preparation"
description: |
  Manually create and configure a template virtual machine with
  Windows, Tzar game, and bot interface installed. This is a
  one-time manual setup that will be cloned for workers.

inputs:
  - "Windows 10 ISO"
  - "Tzar game installer (files/tzared.windows.zip)"
  - "TzarBot.GameInterface binaries (from Phase 1)"

detailed_guide: "plans/phase_4_template_setup.md"

outputs:
  - "C:/VMs/TzarBot-Template.vhdx"
  - "Template VM configured in Hyper-V"
  - "plans/phase_4_template_setup.md (documentation)"

test_command: "Manual verification checklist"

test_criteria: |
  - Windows 10 installed and activated
  - Tzar game installed and runs
  - Auto-login configured
  - Bot Interface runs on startup
  - Hyper-V Integration Services enabled
  - VM boots within 60 seconds

dependencies: ["F1.T6"]
estimated_complexity: "M"

manual_steps: |
  ## Step-by-Step Template VM Setup

  ### 1. Create New VM
  ```powershell
  # Create virtual switch (if not exists)
  New-VMSwitch -Name "TzarBotSwitch" -SwitchType Internal

  # Create VM
  New-VM -Name "TzarBot-Template" `
         -Generation 2 `
         -MemoryStartupBytes 4GB `
         -NewVHDPath "C:\VMs\TzarBot-Template.vhdx" `
         -NewVHDSizeBytes 50GB `
         -SwitchName "TzarBotSwitch"

  # Configure VM
  Set-VM -Name "TzarBot-Template" `
         -ProcessorCount 2 `
         -DynamicMemory `
         -MemoryMinimumBytes 2GB `
         -MemoryMaximumBytes 8GB

  # Enable secure boot with Microsoft template
  Set-VMFirmware -VMName "TzarBot-Template" `
                 -SecureBootTemplate "MicrosoftUEFICertificateAuthority"

  # Mount Windows ISO
  Add-VMDvdDrive -VMName "TzarBot-Template" `
                 -Path "C:\ISOs\Windows10.iso"
  ```

  ### 2. Install Windows
  - Start VM and install Windows 10 (prefer LTSC for minimal footprint)
  - Create local account "TzarBot" with known password
  - Disable Windows Update (for consistency)
  - Configure auto-login:
    ```
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v AutoAdminLogon /t REG_SZ /d 1 /f
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v DefaultUserName /t REG_SZ /d TzarBot /f
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v DefaultPassword /t REG_SZ /d [PASSWORD] /f
    ```

  ### 3. Install Tzar
  - Install Tzar from https://tza.red/
  - Configure graphics settings (windowed mode, fixed resolution)
  - Set to skip intro videos
  - Create skirmish profile for quick start
  - Test that game runs correctly

  ### 4. Install Bot Interface
  - Copy compiled TzarBot.GameInterface to C:\TzarBot\
  - Create startup script:
    ```batch
    @echo off
    cd C:\TzarBot
    start "" TzarBot.GameInterface.exe
    timeout /t 5
    start "" "C:\Games\Tzar\Tzar.exe"
    ```
  - Add startup script to shell:startup folder

  ### 5. Configure Windows
  - Disable screen saver and sleep
  - Disable UAC prompts
  - Configure Windows Firewall for named pipes
  - Enable PowerShell remoting:
    ```powershell
    Enable-PSRemoting -Force
    Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
    ```

  ### 6. Install Hyper-V Integration
  - Ensure Integration Services are enabled
  - Test that guest services work (file copy, heartbeat)

  ### 7. Finalize
  - Clean up temp files
  - Defragment VHD
  - Create checkpoint before making differencing disks
  - Document configuration in phase_4_template_setup.md

validation_checklist:
  - [ ] VM boots successfully
  - [ ] Auto-login works
  - [ ] Tzar starts automatically
  - [ ] Bot Interface starts automatically
  - [ ] Named pipe is accessible from host
  - [ ] PowerShell remoting works from host
  - [ ] VM shuts down cleanly

on_failure: |
  If template creation fails:
  1. Check Hyper-V is enabled on host
  2. Verify sufficient disk space
  3. Check Windows activation
  4. Test Tzar compatibility with Windows version
  5. Verify network connectivity within VM
```

---

### F4.T2: VM Cloning Scripts

```yaml
task_id: "F4.T2"
name: "VM Cloning Scripts"
description: |
  Create PowerShell scripts to manage worker VMs:
  cloning from template, starting, stopping, and cleanup.

inputs:
  - "C:/VMs/TzarBot-Template.vhdx (from F4.T1)"
  - "Template VM configuration"

outputs:
  - "scripts/vm/New-TzarWorkerVM.ps1"
  - "scripts/vm/Remove-TzarWorkerVM.ps1"
  - "scripts/vm/Start-TzarWorkers.ps1"
  - "scripts/vm/Stop-TzarWorkers.ps1"
  - "scripts/vm/Get-TzarWorkerStatus.ps1"
  - "scripts/vm/VMConfig.ps1"

test_command: ".\\scripts\\vm\\New-TzarWorkerVM.ps1 -Count 1 -WhatIf"

test_criteria: |
  - Scripts run without syntax errors
  - Can create differencing VHD from template
  - VMs are created with correct configuration
  - VMs start and respond to heartbeat
  - VMs can be stopped and deleted cleanly
  - Status reporting works

dependencies: ["F4.T1"]
estimated_complexity: "M"

claude_prompt: |
  Create PowerShell scripts for managing Hyper-V worker VMs.

  ## Context
  Template VM exists at C:\VMs\TzarBot-Template.vhdx. Create management scripts.

  ## Requirements

  1. Create `scripts/vm/VMConfig.ps1`:
     ```powershell
     # Configuration for VM management
     $Script:VMConfig = @{
         TemplatePath = "C:\VMs\TzarBot-Template.vhdx"
         WorkersPath = "C:\VMs\Workers"
         VMPrefix = "TzarBot-Worker-"
         SwitchName = "TzarBotSwitch"
         MemoryStartupMB = 4096
         MemoryMinMB = 2048
         MemoryMaxMB = 8192
         ProcessorCount = 2
         DefaultWorkerCount = 8
     }

     function Get-VMConfig { return $Script:VMConfig }
     ```

  2. Create `scripts/vm/New-TzarWorkerVM.ps1`:
     ```powershell
     [CmdletBinding(SupportsShouldProcess)]
     param(
         [int]$Count = 8,
         [int]$StartIndex = 0
     )

     . "$PSScriptRoot\VMConfig.ps1"
     $config = Get-VMConfig

     # Create workers directory
     if (-not (Test-Path $config.WorkersPath)) {
         New-Item -Path $config.WorkersPath -ItemType Directory -Force
     }

     for ($i = $StartIndex; $i -lt ($StartIndex + $Count); $i++) {
         $vmName = "$($config.VMPrefix)$i"
         $vhdPath = Join-Path $config.WorkersPath "$vmName.vhdx"

         if ($PSCmdlet.ShouldProcess($vmName, "Create VM")) {
             # Create differencing disk
             New-VHD -Path $vhdPath `
                     -ParentPath $config.TemplatePath `
                     -Differencing

             # Create VM
             New-VM -Name $vmName `
                    -Generation 2 `
                    -MemoryStartupBytes ($config.MemoryStartupMB * 1MB) `
                    -VHDPath $vhdPath `
                    -SwitchName $config.SwitchName

             # Configure VM
             Set-VM -Name $vmName `
                    -ProcessorCount $config.ProcessorCount `
                    -DynamicMemory `
                    -MemoryMinimumBytes ($config.MemoryMinMB * 1MB) `
                    -MemoryMaximumBytes ($config.MemoryMaxMB * 1MB) `
                    -AutomaticStartAction Start `
                    -AutomaticStopAction ShutDown

             Write-Host "Created VM: $vmName" -ForegroundColor Green
         }
     }
     ```

  3. Create `scripts/vm/Remove-TzarWorkerVM.ps1`:
     ```powershell
     [CmdletBinding(SupportsShouldProcess, ConfirmImpact='High')]
     param(
         [string[]]$Names,
         [switch]$All
     )

     . "$PSScriptRoot\VMConfig.ps1"
     $config = Get-VMConfig

     if ($All) {
         $vms = Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }
     } else {
         $vms = Get-VM | Where-Object { $Names -contains $_.Name }
     }

     foreach ($vm in $vms) {
         if ($PSCmdlet.ShouldProcess($vm.Name, "Remove VM and VHD")) {
             # Stop if running
             if ($vm.State -ne 'Off') {
                 Stop-VM -Name $vm.Name -Force -TurnOff
             }

             # Remove VM
             Remove-VM -Name $vm.Name -Force

             # Remove VHD
             $vhdPath = $vm.HardDrives[0].Path
             if (Test-Path $vhdPath) {
                 Remove-Item $vhdPath -Force
             }

             Write-Host "Removed VM: $($vm.Name)" -ForegroundColor Yellow
         }
     }
     ```

  4. Create `scripts/vm/Start-TzarWorkers.ps1`:
     ```powershell
     [CmdletBinding()]
     param(
         [string[]]$Names,
         [switch]$All,
         [switch]$Wait
     )

     . "$PSScriptRoot\VMConfig.ps1"
     $config = Get-VMConfig

     $vms = if ($All) {
         Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }
     } else {
         Get-VM | Where-Object { $Names -contains $_.Name }
     }

     foreach ($vm in $vms) {
         if ($vm.State -eq 'Off') {
             Start-VM -Name $vm.Name
             Write-Host "Starting: $($vm.Name)" -ForegroundColor Cyan
         }
     }

     if ($Wait) {
         Write-Host "Waiting for VMs to boot..."
         $timeout = 120
         $elapsed = 0

         while ($elapsed -lt $timeout) {
             $allReady = $true
             foreach ($vm in $vms) {
                 $heartbeat = (Get-VM -Name $vm.Name).Heartbeat
                 if ($heartbeat -ne 'OkApplicationsHealthy') {
                     $allReady = $false
                     break
                 }
             }

             if ($allReady) {
                 Write-Host "All VMs ready!" -ForegroundColor Green
                 return
             }

             Start-Sleep -Seconds 5
             $elapsed += 5
         }

         Write-Warning "Timeout waiting for VMs"
     }
     ```

  5. Create `scripts/vm/Stop-TzarWorkers.ps1`:
     ```powershell
     [CmdletBinding()]
     param(
         [string[]]$Names,
         [switch]$All,
         [switch]$Force
     )

     . "$PSScriptRoot\VMConfig.ps1"
     $config = Get-VMConfig

     $vms = if ($All) {
         Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }
     } else {
         Get-VM | Where-Object { $Names -contains $_.Name }
     }

     foreach ($vm in $vms) {
         if ($vm.State -ne 'Off') {
             if ($Force) {
                 Stop-VM -Name $vm.Name -Force -TurnOff
             } else {
                 Stop-VM -Name $vm.Name -Force
             }
             Write-Host "Stopped: $($vm.Name)" -ForegroundColor Yellow
         }
     }
     ```

  6. Create `scripts/vm/Get-TzarWorkerStatus.ps1`:
     ```powershell
     [CmdletBinding()]
     param()

     . "$PSScriptRoot\VMConfig.ps1"
     $config = Get-VMConfig

     $vms = Get-VM | Where-Object { $_.Name -like "$($config.VMPrefix)*" }

     $vms | ForEach-Object {
         [PSCustomObject]@{
             Name = $_.Name
             State = $_.State
             CPU = "$($_.CPUUsage)%"
             Memory = "$([math]::Round($_.MemoryAssigned / 1GB, 1)) GB"
             Uptime = $_.Uptime
             Heartbeat = $_.Heartbeat
         }
     } | Format-Table -AutoSize
     ```

  7. Create tests (manual):
     - Create 1 VM with -WhatIf
     - Create 1 actual VM
     - Start VM and wait for heartbeat
     - Get status
     - Stop and remove VM

validation_steps:
  - "Scripts have no syntax errors"
  - "New-TzarWorkerVM creates VM successfully"
  - "VM starts and has heartbeat"
  - "Stop and Remove work correctly"
  - "Status shows correct information"

on_failure: |
  If scripts fail:
  1. Check Hyper-V module is available
  2. Verify running as Administrator
  3. Check template VHD exists
  4. Verify network switch exists
  5. Check disk space for differencing VHDs
```

---

### F4.T3: VM Manager Implementation

```yaml
task_id: "F4.T3"
name: "VM Manager Implementation"
description: |
  Create a C# wrapper for VM management that interfaces with
  PowerShell scripts and provides programmatic VM control.

inputs:
  - "scripts/vm/*.ps1"
  - "src/TzarBot.Common/TzarBot.Common.csproj"

outputs:
  - "src/TzarBot.Orchestrator/TzarBot.Orchestrator.csproj"
  - "src/TzarBot.Orchestrator/VM/IVMManager.cs"
  - "src/TzarBot.Orchestrator/VM/HyperVManager.cs"
  - "src/TzarBot.Orchestrator/VM/VMInfo.cs"
  - "src/TzarBot.Orchestrator/VM/VMState.cs"
  - "tests/TzarBot.Tests/Phase4/VMManagerTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase4.VMManager\""

test_criteria: |
  - VMManager initializes correctly
  - Can list available VMs
  - Can start/stop VMs programmatically
  - Heartbeat detection works
  - Status updates are accurate

dependencies: ["F4.T2"]
estimated_complexity: "M"

claude_prompt: |
  Create C# VM Manager that wraps PowerShell scripts.

  ## Context
  Create new project `src/TzarBot.Orchestrator/`. Manage VMs via PowerShell.

  ## Requirements

  1. Create project with dependencies:
     - System.Management.Automation
     - Reference TzarBot.Common

  2. Create `VMState` enum:
     ```csharp
     public enum VMState
     {
         Unknown,
         Off,
         Starting,
         Running,
         Stopping,
         Saved,
         Paused,
         Error
     }
     ```

  3. Create `VMInfo` class:
     ```csharp
     public class VMInfo
     {
         public string Name { get; set; }
         public Guid Id { get; set; }
         public VMState State { get; set; }
         public double CpuUsage { get; set; }
         public long MemoryAssignedMB { get; set; }
         public TimeSpan Uptime { get; set; }
         public bool HeartbeatOk { get; set; }
         public string? IpAddress { get; set; }
         public bool IsAvailable => State == VMState.Running && HeartbeatOk;
     }
     ```

  4. Create interface:
     ```csharp
     public interface IVMManager
     {
         Task<IEnumerable<VMInfo>> GetWorkersAsync();
         Task<VMInfo?> GetWorkerAsync(string name);
         Task<IEnumerable<VMInfo>> GetAvailableWorkersAsync();

         Task CreateWorkersAsync(int count);
         Task StartWorkerAsync(string name);
         Task StartAllWorkersAsync();
         Task StopWorkerAsync(string name);
         Task StopAllWorkersAsync();
         Task RemoveWorkerAsync(string name);
         Task RemoveAllWorkersAsync();

         Task<bool> WaitForHeartbeatAsync(string name, TimeSpan timeout);
         Task<bool> WaitForAllHeartbeatsAsync(TimeSpan timeout);

         event Action<string, VMState>? OnVMStateChanged;
     }
     ```

  5. Implement `HyperVManager`:
     ```csharp
     public class HyperVManager : IVMManager
     {
         private readonly string _scriptsPath;
         private readonly string _vmPrefix;

         public async Task<IEnumerable<VMInfo>> GetWorkersAsync()
         {
             using var ps = PowerShell.Create();
             ps.AddScript(@"
                 Get-VM | Where-Object { $_.Name -like 'TzarBot-Worker-*' } |
                 Select-Object Name, Id, State, CPUUsage, MemoryAssigned,
                               Uptime, Heartbeat
             ");

             var results = await ps.InvokeAsync();

             return results.Select(r => new VMInfo
             {
                 Name = r.Properties["Name"].Value as string,
                 Id = (Guid)r.Properties["Id"].Value,
                 State = ParseState(r.Properties["State"].Value),
                 CpuUsage = Convert.ToDouble(r.Properties["CPUUsage"].Value),
                 MemoryAssignedMB = Convert.ToInt64(r.Properties["MemoryAssigned"].Value) / 1024 / 1024,
                 Uptime = (TimeSpan)r.Properties["Uptime"].Value,
                 HeartbeatOk = r.Properties["Heartbeat"].Value?.ToString() == "OkApplicationsHealthy"
             });
         }

         public async Task CreateWorkersAsync(int count)
         {
             using var ps = PowerShell.Create();
             ps.AddScript($@". '{_scriptsPath}\New-TzarWorkerVM.ps1' -Count {count}");
             await ps.InvokeAsync();

             if (ps.HadErrors)
             {
                 throw new VMException("Failed to create workers: " +
                     string.Join(", ", ps.Streams.Error.Select(e => e.ToString())));
             }
         }

         public async Task<bool> WaitForHeartbeatAsync(string name, TimeSpan timeout)
         {
             var deadline = DateTime.UtcNow + timeout;

             while (DateTime.UtcNow < deadline)
             {
                 var vm = await GetWorkerAsync(name);
                 if (vm?.HeartbeatOk == true)
                 {
                     return true;
                 }
                 await Task.Delay(TimeSpan.FromSeconds(2));
             }

             return false;
         }
     }
     ```

  6. Create tests:
     - Test_GetWorkers_ReturnsVMList (mock or real)
     - Test_VMStateMapping_Correct
     - Test_HeartbeatDetection_Works

  ## Note
  For testing without actual VMs, create a MockVMManager that simulates behavior.

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase4.VMManager"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test with actual VM"

on_failure: |
  If PowerShell integration fails:
  1. Check execution policy
  2. Verify PowerShell version (5.1+)
  3. Check module import works
  4. Try running PowerShell directly first
```

---

### F4.T4: Orchestrator Service

```yaml
task_id: "F4.T4"
name: "Orchestrator Service"
description: |
  Create the main orchestrator service that coordinates training
  across multiple VMs, distributing genomes and collecting results.

inputs:
  - "src/TzarBot.Orchestrator/VM/HyperVManager.cs"
  - "src/TzarBot.GeneticAlgorithm/Core/GeneticAlgorithmEngine.cs"
  - "plans/1general_plan.md (section 4.4)"

outputs:
  - "src/TzarBot.Orchestrator/Core/IOrchestrator.cs"
  - "src/TzarBot.Orchestrator/Core/TrainingOrchestrator.cs"
  - "src/TzarBot.Orchestrator/Core/OrchestratorConfig.cs"
  - "src/TzarBot.Orchestrator/Core/WorkItem.cs"
  - "tests/TzarBot.Tests/Phase4/OrchestratorTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase4.Orchestrator\""

test_criteria: |
  - Orchestrator initializes with VM pool
  - Work items are distributed to available VMs
  - Results are collected and aggregated
  - Failed VMs are handled gracefully
  - Generation completes when all items done

dependencies: ["F4.T3"]
estimated_complexity: "L"

claude_prompt: |
  Create the Training Orchestrator service.

  ## Context
  Project: `src/TzarBot.Orchestrator/`. Coordinate training across VMs.

  ## Requirements

  1. Create configuration:
     ```csharp
     public class OrchestratorConfig
     {
         public int MaxConcurrentVMs { get; set; } = 8;
         public int GamesPerGenome { get; set; } = 3;
         public TimeSpan GameTimeout { get; set; } = TimeSpan.FromMinutes(30);
         public TimeSpan VMStartupTimeout { get; set; } = TimeSpan.FromMinutes(2);
         public int MaxRetries { get; set; } = 2;
         public bool AutoRestartFailedVMs { get; set; } = true;
     }
     ```

  2. Create `WorkItem` class:
     ```csharp
     public class WorkItem
     {
         public Guid Id { get; set; }
         public NetworkGenome Genome { get; set; }
         public int GameIndex { get; set; }  // Which game out of GamesPerGenome
         public WorkItemState State { get; set; }
         public string? AssignedVM { get; set; }
         public DateTime? StartedAt { get; set; }
         public DateTime? CompletedAt { get; set; }
         public GameResult? Result { get; set; }
         public int RetryCount { get; set; }
         public string? Error { get; set; }
     }

     public enum WorkItemState
     {
         Pending,
         Assigned,
         Running,
         Completed,
         Failed,
         TimedOut
     }
     ```

  3. Create interface:
     ```csharp
     public interface IOrchestrator
     {
         Task InitializeAsync();
         Task<GenerationResult> RunGenerationAsync(
             IEnumerable<NetworkGenome> population,
             CancellationToken ct);
         Task ShutdownAsync();

         OrchestratorStatus Status { get; }
         event Action<WorkItem>? OnWorkItemCompleted;
         event Action<string, Exception>? OnVMError;
     }

     public class OrchestratorStatus
     {
         public int TotalVMs { get; set; }
         public int ActiveVMs { get; set; }
         public int PendingItems { get; set; }
         public int RunningItems { get; set; }
         public int CompletedItems { get; set; }
         public int FailedItems { get; set; }
     }
     ```

  4. Implement `TrainingOrchestrator`:
     ```csharp
     public class TrainingOrchestrator : IOrchestrator
     {
         private readonly IVMManager _vmManager;
         private readonly OrchestratorConfig _config;
         private readonly ConcurrentQueue<WorkItem> _pendingWork;
         private readonly ConcurrentDictionary<string, WorkItem> _runningWork;

         public async Task<GenerationResult> RunGenerationAsync(
             IEnumerable<NetworkGenome> population,
             CancellationToken ct)
         {
             // Create work items
             var workItems = population
                 .SelectMany(genome => Enumerable
                     .Range(0, _config.GamesPerGenome)
                     .Select(i => new WorkItem
                     {
                         Id = Guid.NewGuid(),
                         Genome = genome,
                         GameIndex = i,
                         State = WorkItemState.Pending
                     }))
                 .ToList();

             foreach (var item in workItems)
             {
                 _pendingWork.Enqueue(item);
             }

             // Start worker loop
             var workers = await StartWorkersAsync(ct);

             // Wait for all work to complete
             while (!ct.IsCancellationRequested &&
                    (_pendingWork.Count > 0 || _runningWork.Count > 0))
             {
                 await DistributeWorkAsync(ct);
                 await Task.Delay(1000, ct);
             }

             // Aggregate results
             return new GenerationResult
             {
                 WorkItems = workItems,
                 TotalGamesPlayed = workItems.Count(w => w.State == WorkItemState.Completed),
                 FailedGames = workItems.Count(w => w.State == WorkItemState.Failed),
                 Duration = DateTime.UtcNow - startTime
             };
         }

         private async Task DistributeWorkAsync(CancellationToken ct)
         {
             var availableVMs = await _vmManager.GetAvailableWorkersAsync();

             foreach (var vm in availableVMs)
             {
                 if (_runningWork.ContainsKey(vm.Name))
                     continue;

                 if (_pendingWork.TryDequeue(out var workItem))
                 {
                     await AssignWorkToVMAsync(vm, workItem, ct);
                 }
             }
         }

         private async Task AssignWorkToVMAsync(VMInfo vm, WorkItem item, CancellationToken ct)
         {
             item.State = WorkItemState.Assigned;
             item.AssignedVM = vm.Name;
             item.StartedAt = DateTime.UtcNow;

             _runningWork[vm.Name] = item;

             try
             {
                 // Send genome to VM
                 await SendGenomeToVMAsync(vm.Name, item.Genome, ct);

                 // Start game
                 await StartGameOnVMAsync(vm.Name, ct);

                 item.State = WorkItemState.Running;

                 // Wait for result (with timeout)
                 item.Result = await WaitForGameResultAsync(
                     vm.Name,
                     _config.GameTimeout,
                     ct);

                 item.State = WorkItemState.Completed;
                 item.CompletedAt = DateTime.UtcNow;
             }
             catch (TimeoutException)
             {
                 item.State = WorkItemState.TimedOut;
                 await HandleVMFailureAsync(vm.Name, item);
             }
             catch (Exception ex)
             {
                 item.State = WorkItemState.Failed;
                 item.Error = ex.Message;
                 await HandleVMFailureAsync(vm.Name, item);
             }
             finally
             {
                 _runningWork.TryRemove(vm.Name, out _);
                 OnWorkItemCompleted?.Invoke(item);
             }
         }
     }
     ```

  5. Create tests:
     - Test_WorkDistribution_UsesAvailableVMs
     - Test_FailedWork_IsRetried
     - Test_Timeout_HandledGracefully
     - Test_GenerationCompletes_WhenAllWorkDone

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase4.Orchestrator"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test with mock VMs"

on_failure: |
  If orchestration fails:
  1. Check VM availability detection
  2. Verify work queue operations are thread-safe
  3. Add more detailed logging
  4. Test timeout handling independently
```

---

### F4.T5: Communication Protocol

```yaml
task_id: "F4.T5"
name: "VM Communication Protocol"
description: |
  Implement the communication protocol between orchestrator and
  VMs for sending genomes and receiving results.

inputs:
  - "src/TzarBot.Orchestrator/Core/TrainingOrchestrator.cs"
  - "src/TzarBot.GameInterface/IPC/Protocol.cs (from Phase 1)"

outputs:
  - "src/TzarBot.Orchestrator/Communication/IVMCommunicator.cs"
  - "src/TzarBot.Orchestrator/Communication/WinRMCommunicator.cs"
  - "src/TzarBot.Orchestrator/Communication/NamedPipeCommunicator.cs"
  - "src/TzarBot.Orchestrator/Communication/Messages.cs"
  - "tests/TzarBot.Tests/Phase4/CommunicationTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase4.Communication\""

test_criteria: |
  - Can connect to VM via chosen protocol
  - Genome transfers correctly
  - Game start command works
  - Results are received correctly
  - Connection failures are handled

dependencies: ["F4.T3"]
estimated_complexity: "M"

claude_prompt: |
  Implement communication protocol for VM interaction.

  ## Context
  Project: `src/TzarBot.Orchestrator/`. Communicate with VMs to send work and get results.

  ## Design Decision
  Two approaches are available:
  1. **WinRM**: PowerShell remoting to control VM
  2. **Named Pipes**: Direct pipe connection to bot interface

  We'll implement both and choose based on reliability.

  ## Requirements

  1. Create message types:
     ```csharp
     [MessagePackObject]
     public abstract class VMMessage
     {
         [Key(0)] public Guid MessageId { get; set; } = Guid.NewGuid();
         [Key(1)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
     }

     [MessagePackObject]
     public class LoadGenomeMessage : VMMessage
     {
         [Key(2)] public byte[] GenomeData { get; set; }
     }

     [MessagePackObject]
     public class StartGameMessage : VMMessage
     {
         [Key(2)] public GameConfig GameConfig { get; set; }
     }

     [MessagePackObject]
     public class GameResultMessage : VMMessage
     {
         [Key(2)] public GameResult Result { get; set; }
     }

     [MessagePackObject]
     public class StatusMessage : VMMessage
     {
         [Key(2)] public VMBotStatus Status { get; set; }
     }
     ```

  2. Create interface:
     ```csharp
     public interface IVMCommunicator
     {
         Task ConnectAsync(string vmName, TimeSpan timeout, CancellationToken ct);
         Task DisconnectAsync();
         bool IsConnected { get; }

         Task SendGenomeAsync(NetworkGenome genome, CancellationToken ct);
         Task StartGameAsync(GameConfig config, CancellationToken ct);
         Task<GameResult> WaitForResultAsync(TimeSpan timeout, CancellationToken ct);
         Task<VMBotStatus> GetStatusAsync(CancellationToken ct);
         Task ResetAsync(CancellationToken ct);
     }
     ```

  3. Implement `NamedPipeCommunicator`:
     ```csharp
     public class NamedPipeCommunicator : IVMCommunicator
     {
         private NamedPipeClientStream? _pipe;
         private readonly string _pipeNameFormat;

         public async Task ConnectAsync(string vmName, TimeSpan timeout, CancellationToken ct)
         {
             // For named pipes across Hyper-V, use the VM's hostname/IP
             var vmInfo = await GetVMNetworkInfo(vmName);
             var pipeName = $"\\\\{vmInfo.IpAddress}\\pipe\\TzarBot";

             _pipe = new NamedPipeClientStream(
                 vmInfo.IpAddress,
                 "TzarBot",
                 PipeDirection.InOut,
                 PipeOptions.Asynchronous);

             await _pipe.ConnectAsync((int)timeout.TotalMilliseconds, ct);
         }

         public async Task SendGenomeAsync(NetworkGenome genome, CancellationToken ct)
         {
             var message = new LoadGenomeMessage
             {
                 GenomeData = GenomeSerializer.Serialize(genome)
             };

             await SendMessageAsync(message, ct);
             await WaitForAckAsync(ct);
         }

         public async Task<GameResult> WaitForResultAsync(TimeSpan timeout, CancellationToken ct)
         {
             using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
             cts.CancelAfter(timeout);

             while (!cts.Token.IsCancellationRequested)
             {
                 var message = await ReceiveMessageAsync(cts.Token);
                 if (message is GameResultMessage result)
                 {
                     return result.Result;
                 }
             }

             throw new TimeoutException("Game result not received in time");
         }
     }
     ```

  4. Implement `WinRMCommunicator` (alternative):
     ```csharp
     public class WinRMCommunicator : IVMCommunicator
     {
         // Uses PowerShell remoting to execute commands on VM
         // Simpler but higher latency

         public async Task SendGenomeAsync(NetworkGenome genome, CancellationToken ct)
         {
             var genomeData = Convert.ToBase64String(
                 GenomeSerializer.Serialize(genome));

             await InvokeRemoteAsync($@"
                 $data = [Convert]::FromBase64String('{genomeData}')
                 $data | Set-Content -Path 'C:\TzarBot\current_genome.bin' -AsByteStream
             ", ct);
         }

         public async Task StartGameAsync(GameConfig config, CancellationToken ct)
         {
             await InvokeRemoteAsync($@"
                 # Signal bot to start game
                 Set-Content -Path 'C:\TzarBot\start_signal' -Value 'START'
             ", ct);
         }

         public async Task<GameResult> WaitForResultAsync(TimeSpan timeout, CancellationToken ct)
         {
             var deadline = DateTime.UtcNow + timeout;

             while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
             {
                 var result = await InvokeRemoteAsync<string>(@"
                     if (Test-Path 'C:\TzarBot\result.json') {
                         Get-Content 'C:\TzarBot\result.json' -Raw
                     }
                 ", ct);

                 if (!string.IsNullOrEmpty(result))
                 {
                     return JsonSerializer.Deserialize<GameResult>(result);
                 }

                 await Task.Delay(1000, ct);
             }

             throw new TimeoutException();
         }
     }
     ```

  5. Create tests:
     - Test_Connect_Succeeds
     - Test_SendGenome_TransfersData
     - Test_WaitForResult_ReturnsResult
     - Test_Timeout_ThrowsException

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase4.Communication"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Test communication with actual VM"

on_failure: |
  If communication fails:
  1. Check firewall rules on VM
  2. Verify named pipe server is running
  3. Test WinRM connectivity manually
  4. Check network configuration
```

---

### F4.T6: Multi-VM Integration Test

```yaml
task_id: "F4.T6"
name: "Multi-VM Integration Test"
description: |
  Create integration tests that verify the full infrastructure
  works with multiple VMs running in parallel.

inputs:
  - "All Phase 4 components"
  - "At least 2 worker VMs created"

outputs:
  - "tests/TzarBot.Tests/Phase4/IntegrationTests.cs"
  - "src/TzarBot.Orchestrator.Demo/Program.cs"
  - "src/TzarBot.Orchestrator.Demo/TzarBot.Orchestrator.Demo.csproj"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase4.Integration\""

test_criteria: |
  - Orchestrator manages VM pool correctly
  - Multiple genomes are evaluated in parallel
  - Results are collected from all VMs
  - VM failures are handled
  - Demo runs successfully with real VMs

dependencies: ["F4.T4", "F4.T5"]
estimated_complexity: "L"

claude_prompt: |
  Create integration tests and demo for Phase 4.

  ## Context
  All Phase 4 components are implemented. Verify multi-VM operation.

  ## Requirements

  1. Create demo console app:
     ```csharp
     // Demo workflow:
     // 1. Initialize VMManager
     // 2. Create/start worker VMs (or use existing)
     // 3. Create orchestrator
     // 4. Generate test population (10 random genomes)
     // 5. Run one generation
     // 6. Print results
     // 7. Show statistics (games played, time, failures)

     var vmManager = new HyperVManager();
     var orchestrator = new TrainingOrchestrator(vmManager, config);

     await orchestrator.InitializeAsync();

     // Create test population
     var population = Enumerable.Range(0, 10)
         .Select(_ => GenomeFactory.CreateRandom(rng, 240, 135))
         .ToList();

     Console.WriteLine($"Running generation with {population.Count} genomes...");

     var result = await orchestrator.RunGenerationAsync(population, cts.Token);

     Console.WriteLine($"Completed: {result.TotalGamesPlayed} games");
     Console.WriteLine($"Failed: {result.FailedGames} games");
     Console.WriteLine($"Duration: {result.Duration}");

     foreach (var genome in population)
     {
         var gameResults = result.WorkItems
             .Where(w => w.Genome.Id == genome.Id && w.Result != null)
             .Select(w => w.Result);

         var avgFitness = gameResults.Average(r => fitnessCalc.Calculate(r));
         Console.WriteLine($"Genome {genome.Id}: Fitness = {avgFitness:F2}");
     }
     ```

  2. Create integration tests:
     ```csharp
     [Collection("Integration")]
     public class Phase4IntegrationTests
     {
         [Fact(Skip = "Requires VMs")]
         public async Task Orchestrator_RunsGeneration_OnMultipleVMs()
         {
             var vmManager = new HyperVManager();
             var config = new OrchestratorConfig
             {
                 MaxConcurrentVMs = 2,
                 GamesPerGenome = 1,
                 GameTimeout = TimeSpan.FromMinutes(5)
             };

             var orchestrator = new TrainingOrchestrator(vmManager, config);
             await orchestrator.InitializeAsync();

             var population = Enumerable.Range(0, 4)
                 .Select(_ => GenomeFactory.CreateRandom(new Random(), 240, 135))
                 .ToList();

             var result = await orchestrator.RunGenerationAsync(
                 population,
                 CancellationToken.None);

             Assert.Equal(4, result.TotalGamesPlayed);
             Assert.Equal(0, result.FailedGames);
         }

         [Fact]
         public async Task MockOrchestrator_SimulatesParallelExecution()
         {
             // Use mock VM manager for unit testing
             var mockVM = new MockVMManager(vmCount: 4);
             var orchestrator = new TrainingOrchestrator(mockVM, config);

             // Test with mock...
         }
     }
     ```

  3. Create monitoring output:
     ```
     ╔════════════════════════════════════════════════════════════╗
     ║              TZAR BOT ORCHESTRATOR - DEMO                   ║
     ╠════════════════════════════════════════════════════════════╣
     ║  VMs: 4/4 active                                            ║
     ║  Generation: 1                                               ║
     ║  Progress: [████████░░░░░░░░░░░░] 40%                       ║
     ╠════════════════════════════════════════════════════════════╣
     ║  VM-0: Running genome abc123... (5:23 elapsed)              ║
     ║  VM-1: Running genome def456... (3:45 elapsed)              ║
     ║  VM-2: Completed 2 games, waiting...                        ║
     ║  VM-3: Running genome ghi789... (1:12 elapsed)              ║
     ╠════════════════════════════════════════════════════════════╣
     ║  Completed: 8/20 games | Failed: 0 | Avg time: 4:32         ║
     ╚════════════════════════════════════════════════════════════╝
     ```

  After completion:
  1. Run: `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase4.Integration"`
  2. Run: `dotnet run --project src/TzarBot.Orchestrator.Demo`

validation_steps:
  - "Integration tests pass (or skip appropriately)"
  - "Demo runs with mock VMs"
  - "Demo runs with real VMs (manual)"
  - "Parallel execution verified"

on_failure: |
  If integration fails:
  1. Test each component independently
  2. Check VM network connectivity
  3. Verify communication protocol
  4. Add detailed logging
  5. Test with fewer VMs first
```

---

## Rollback Plan

If Phase 4 implementation fails:

1. **Single VM Mode**: Run everything on one VM sequentially
   - Slower but simpler
   - No orchestration complexity

2. **Local Execution**: Run game directly on host (if possible)
   - Fastest iteration
   - May have conflicts with other software

3. **Docker Alternative**: If game runs on Wine/Linux
   - More portable
   - Easier to scale

---

## API Documentation

### VM Management API

```powershell
# Create worker VMs
.\scripts\vm\New-TzarWorkerVM.ps1 -Count 8

# Start all workers
.\scripts\vm\Start-TzarWorkers.ps1 -All -Wait

# Check status
.\scripts\vm\Get-TzarWorkerStatus.ps1

# Stop all workers
.\scripts\vm\Stop-TzarWorkers.ps1 -All

# Remove all workers
.\scripts\vm\Remove-TzarWorkerVM.ps1 -All
```

### Orchestrator API

```csharp
// Initialize
var orchestrator = new TrainingOrchestrator(vmManager, config);
await orchestrator.InitializeAsync();

// Subscribe to events
orchestrator.OnWorkItemCompleted += item =>
{
    Console.WriteLine($"Completed: {item.Genome.Id} on {item.AssignedVM}");
};

// Run generation
var result = await orchestrator.RunGenerationAsync(population, ct);

// Shutdown
await orchestrator.ShutdownAsync();
```

---

*Phase 4 Detailed Plan - Version 1.0*
*See prompts/phase_4/ for individual task prompts*
