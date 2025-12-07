# TzarBot Environment Settings

**Ostatnia aktualizacja:** 2025-12-07

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
| Hyper-V | PENDING | Awaiting installation verification |

---

## Network Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Virtual Switch | TzarBotSwitch | Internal switch (to be created) |
| NAT Name | TzarBotNAT | (to be created) |
| NAT Subnet | 192.168.100.0/24 | |
| Host Gateway IP | 192.168.100.1 | Interface on TzarBotSwitch |

---

## Virtual Machines

| VM Name | IP | RAM | Purpose | Status |
|---------|-----|-----|---------|--------|
| DEV | 192.168.100.10 | 4 GB | Development/Testing | PENDING |
| TzarBot-Worker-01 | 192.168.100.101 | 1-2 GB | Training worker | PENDING |
| TzarBot-Worker-02 | 192.168.100.102 | 1-2 GB | Training worker | PENDING |
| TzarBot-Worker-03 | 192.168.100.103 | 1-2 GB | Training worker | PENDING |

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
| Tzar Game | TBD | To be set after installation |
| Bot Working Dir | TBD | |

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
