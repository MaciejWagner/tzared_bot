# TzarBot Environment Settings

**Ostatnia aktualizacja:** 2025-12-08

> Ten plik zawiera wszystkie ustawienia srodowiskowe projektu TzarBot.
> WAZNE: Aktualizuj ten plik przy kazdej zmianie konfiguracji!

---

## Resource Limits

| Resource | Limit | Notes |
|----------|-------|-------|
| **Max RAM dla VM** | **10 GB** | HARD LIMIT - suma RAM wszystkich VM (DEV + Workers) |
| DEV VM RAM | 4 GB | Maszyna deweloperska |
| Workers RAM Pool | 6 GB | Pozostale dla worker VM |

**WAZNE:** Przy tworzeniu nowych VM ZAWSZE weryfikuj, ze suma RAM nie przekracza 10GB!

---

## Host Machine

| Setting | Value | Notes |
|---------|-------|-------|
| OS | Windows 11 | Host development machine |
| .NET Version | 8.0 | Required for TzarBot |
| Hyper-V | ENABLED | Verified 2025-12-07 |

---

## Network Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Virtual Switch | TzarBotSwitch | Internal switch - CREATED 2025-12-07 |
| NAT Name | TzarBotNAT | CREATED 2025-12-07 |
| NAT Subnet | 192.168.100.0/24 | Active |
| Host Gateway IP | 192.168.100.1 | InterfaceIndex: 19 |

---

## Virtual Machines

| VM Name | IP | RAM | Purpose | Status |
|---------|-----|-----|---------|--------|
| DEV | 192.168.100.10 | 4 GB | Development/Testing | RUNNING - Network OK |

### VM Credentials

| VM | Username | Password | Notes |
|----|----------|----------|-------|
| DEV | test | password123 | PowerShell Direct enabled |

### VM Template

Po skonfigurowaniu DEV można utworzyć template do klonowania worker VM:

```powershell
# 1. Wyłącz VM
Stop-VM -Name "DEV" -Force

# 2. Eksportuj jako template
Export-VM -Name "DEV" -Path "C:\Hyper-V\Templates"

# 3. Lub skopiuj VHDX jako base image
Copy-Item "C:\ProgramData\Microsoft\Windows\Virtual Hard Disks\DEV.vhdx" "C:\Hyper-V\Templates\TzarBot-Base.vhdx"
```

Konfiguracja DEV (do replikacji na workerach):
- Windows 10/11 Pro (nieaktywowany)
- .NET SDK 8.0.416
- IP: DHCP lub statyczne z puli 192.168.100.101-199
- User: test (bez hasła)

### Worker VMs (do utworzenia w Phase 4)

| VM Name | IP | RAM | Status |
|---------|-----|-----|--------|
| TzarBot-Worker-01 | 192.168.100.101 | 1-2 GB | PENDING |
| TzarBot-Worker-02 | 192.168.100.102 | 1-2 GB | PENDING |
| TzarBot-Worker-03 | 192.168.100.103 | 1-2 GB | PENDING |

**RAM Summary:** DEV (4GB) + Workers (max 6GB) = 10GB LIMIT

---

## Paths (Host)

| Component | Path | Notes |
|-----------|------|-------|
| Project Root | C:\Users\maciek\ai_experiments\tzar_bot | |
| Source Code | C:\Users\maciek\ai_experiments\tzar_bot\src | |
| Scripts | C:\Users\maciek\ai_experiments\tzar_bot\scripts | |

---

## Paths (VM - DEV)

| Component | Path | Notes |
|-----------|------|-------|
| Tzar Game | C:\Program Files\Tzared\Tzared.exe | Windowed mode enabled |
| Bot Working Dir | C:\TzarBot | Working directory for bot |
| Genomes Dir | C:\TzarBot\Genomes | Genome files for evaluation |
| Results Dir | C:\TzarBot\Results | Evaluation results |
| Logs Dir | C:\TzarBot\Logs | Bot logs |
| Startup Script | C:\TzarBot\startup.ps1 | Auto-start script |

---

## VM Template Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Template Path | C:\VMs\TzarBot-Template.vhdx | Base VHDX for workers |
| Workers Path | C:\VMs\Workers | Directory for worker VHDs |
| Clean Checkpoint | "Clean" | Checkpoint name for reset |
| VM Prefix | TzarBot-Worker- | Worker VM naming convention |

### Worker VM Naming Convention

| Pattern | Example | IP Range |
|---------|---------|----------|
| TzarBot-Worker-{N} | TzarBot-Worker-0 | 192.168.100.100+N |

---

## Orchestrator Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Default Worker Count | 3 | Respects 10GB RAM limit |
| Worker RAM | 2 GB each | 3 x 2GB = 6GB pool |
| Evaluation Timeout | 10 minutes | Per genome |
| Max Parallel | 3 | One per worker |
| Auto-Recovery | Enabled | Restarts crashed workers |

---

## Communication Protocol (Phase 4)

| Protocol | Use Case | Notes |
|----------|----------|-------|
| PowerShell Direct | File transfer, commands | Uses Hyper-V integration |
| File-based signaling | Genome load trigger | C:\TzarBot\Genomes\load_genome.trigger |
| JSON result files | Evaluation results | C:\TzarBot\Results\evaluation_result.json |

---

## Ports & Services

| Service | Port | Protocol | Host/VM |
|---------|------|----------|---------|
| Dashboard | 5000 | HTTP | Host |
| SignalR Hub | 5001 | WebSocket | Host |
| IPC Named Pipe | N/A | Named Pipe | VM |

---

## Credentials

> UWAGA: Nie zapisuj haseł w tym pliku!
> Używaj Windows Credential Manager lub secure storage.

| Account | Username | Storage Location |
|---------|----------|------------------|
| VM Admin | tzarbot | Windows Credential Manager |

---

## Version Info

| Component | Version | Notes |
|-----------|---------|-------|
| .NET SDK | 8.0.x | |
| ONNX Runtime | TBD | |
| OpenCvSharp4 | TBD | |
| Vortice.Windows | TBD | |

---

## History

| Date | Change | By |
|------|--------|-----|
| 2025-12-07 | Initial file created | Claude |
| 2025-12-08 | Phase 4 implementation: VM paths, template config, orchestrator settings | Claude |
