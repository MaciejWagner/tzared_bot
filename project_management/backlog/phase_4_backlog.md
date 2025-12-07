# Backlog Fazy 4: Hyper-V Infrastructure

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** PENDING
**Priorytet:** MUST (wymagane dla rownoleglego treningu)

---

## Podsumowanie

Faza 4 obejmuje budowe infrastruktury do rownoleglego treningu na wielu maszynach wirtualnych z gra Tzar. Umozliwia ewaluacje wielu genomow jednoczesnie.

---

## Taski

### F4.T1: Template VM Preparation (MANUAL)
| Pole | Wartosc |
|------|---------|
| **ID** | F4.T1 |
| **Tytul** | Przygotowanie template VM |
| **Opis** | Reczne przygotowanie wzorcowej maszyny wirtualnej z zainstalowana gra i botem |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-hyperv-admin |
| **Zaleznosci** | F0.T2 (Development VM), F1 (Game Interface) |
| **Uwagi** | TASK MANUALNY - wymaga interakcji operatora |

**Kryteria akceptacji:**
- [ ] Windows 10 LTSC zainstalowany (minimal footprint)
- [ ] Tzar zainstalowany i skonfigurowany
- [ ] Bot Interface zainstalowany jako Windows Service
- [ ] Auto-login do konta lokalnego
- [ ] Startup script uruchamiajacy gre
- [ ] VHDX zapisany jako template

**Powiazane pliki:**
- `plans/phase_4_template_setup.md`
- `plans/phase_4_detailed.md`

---

### F4.T2: VM Cloning Scripts
| Pole | Wartosc |
|------|---------|
| **ID** | F4.T2 |
| **Tytul** | Skrypty klonowania VM |
| **Opis** | Skrypty PowerShell do tworzenia VM workerow z template |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-hyperv-admin |
| **Zaleznosci** | F4.T1 |

**Kryteria akceptacji:**
- [ ] Tworzenie differencing disks z template
- [ ] Parametryzowana liczba VM (N=2-16)
- [ ] Konfiguracja sieci (Internal Switch)
- [ ] Auto-start VM
- [ ] Cleanup script do usuwania workerow

**Powiazane pliki:**
- `scripts/vm/New-TzarWorkerVM.ps1`
- `scripts/vm/Remove-TzarWorkerVM.ps1`

---

### F4.T3: VM Manager Implementation
| Pole | Wartosc |
|------|---------|
| **ID** | F4.T3 |
| **Tytul** | Implementacja VM Manager |
| **Opis** | Klasa C# do zarzadzania maszynami wirtualnymi |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F4.T2 |

**Kryteria akceptacji:**
- [ ] IVMManager interface
- [ ] Hyper-V PowerShell cmdlets integration
- [ ] Start/Stop/Restart VM
- [ ] Get VM status
- [ ] Send file to VM (genom)
- [ ] Execute command on VM

**Powiazane pliki:**
- `src/TzarBot.Orchestrator/VM/`

---

### F4.T4: Orchestrator Service
| Pole | Wartosc |
|------|---------|
| **ID** | F4.T4 |
| **Tytul** | Serwis orkiestratora |
| **Opis** | Windows Service zarzadzajacy pula VM i dystrybucja genomow |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-hyperv-admin |
| **Zaleznosci** | F4.T3 |

**Kryteria akceptacji:**
- [ ] Background service Windows
- [ ] Pool management (acquire/release VM)
- [ ] Dystrybucja genomow do ewaluacji
- [ ] Zbieranie wynikow
- [ ] Timeout handling
- [ ] Auto-recovery po crashu VM

**Powiazane pliki:**
- `src/TzarBot.Orchestrator/`

---

### F4.T5: Communication Protocol
| Pole | Wartosc |
|------|---------|
| **ID** | F4.T5 |
| **Tytul** | Protokol komunikacji Host-VM |
| **Opis** | Implementacja komunikacji miedzy hostem a VM workerami |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F4.T3, F4.T4 |

**Kryteria akceptacji:**
- [ ] WinRM lub TCP communication
- [ ] Send genome to VM
- [ ] Receive game result from VM
- [ ] Heartbeat/health check
- [ ] Timeout i retry logic

**Powiazane pliki:**
- `plans/phase_4_detailed.md`

---

### F4.T6: Multi-VM Integration Test
| Pole | Wartosc |
|------|---------|
| **ID** | F4.T6 |
| **Tytul** | Test integracyjny wielu VM |
| **Opis** | Weryfikacja dzialania calej infrastruktury z wieloma VM |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | QA_INTEGRATION |
| **Zaleznosci** | F4.T1, F4.T2, F4.T3, F4.T4, F4.T5 |

**Kryteria akceptacji:**
- [ ] Automatyczne tworzenie 2+ VM
- [ ] Rownolegla ewaluacja genomow
- [ ] Zbieranie wynikow
- [ ] Cleanup po tescie
- [ ] Stabilnosc przez 1h

**Powiazane pliki:**
- `tests/TzarBot.Tests/Phase4/`

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 6 |
| Ukonczonych | 0 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 6 |
| Postep | 0% |

---

## Wymagania sprzetowe

| Zasob | Minimum | Zalecane |
|-------|---------|----------|
| RAM Host | 32GB | 64GB |
| CPU Cores | 8 | 16 |
| Storage | 200GB SSD | 500GB NVMe |
| VM count | 4 | 8-16 |

---

## Zaleznosci

- **Wymaga:** Faza 0 (Prerequisites) - Hyper-V setup
- **Wymaga:** Faza 1 (Game Interface) - Bot Interface musi dzialac w VM
- **Blokuje:** Faza 6 (Training Pipeline)

---

## Metryki sukcesu

- [ ] Automatyczne tworzenie 8+ VM z template
- [ ] Stabilnosc: VM dzialaja 24h bez interwencji
- [ ] Czas setupu nowej generacji < 5 minut
