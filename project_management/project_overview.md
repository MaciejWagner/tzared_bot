# TzarBot - Przeglad Projektu

**Ostatnia aktualizacja:** 2025-12-07
**Wersja dokumentu:** 1.0

---

## Opis projektu

TzarBot to projekt budowy bota AI do gry strategicznej Tzar (https://tza.red/), wykorzystujacego algorytm genetyczny do ewolucji sieci neuronowych. Bot uczy sie gry poprzez:
- Przechwytywanie obrazu z gry
- Podejmowanie decyzji przez siec neuronowa
- Ewolucje sieci poprzez selekcje najlepszych graczy

---

## Cel projektu

**Glowny cel:** Bot AI wygrywajacy z Hard AI w grze Tzar

**Kamienie milowe:**
| # | Milestone | Opis | Status |
|---|-----------|------|--------|
| M1 | Bot klika w grze | Faza 1 - Game Interface | COMPLETED |
| M2 | Siec neuronowa podejmuje decyzje | Faza 2 - Neural Network | PENDING |
| M3 | Populacja sieci ewoluuje | Faza 3 - Genetic Algorithm | PENDING |
| M4 | Trening na 4+ VM | Fazy 4+5 | PENDING |
| M5 | Bot wygrywa z Easy AI | Faza 6 - Training | PENDING |
| M6 | Bot wygrywa z Hard AI | Sukces projektu | PENDING |

---

## Architektura systemu

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ARCHITEKTURA SYSTEMU                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                        HOST MACHINE                             │    │
│  │  ┌─────────────────────────────────────────────────────────┐   │    │
│  │  │                 ORCHESTRATOR SERVICE                     │   │    │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │    │
│  │  │  │   Training  │  │  Checkpoint │  │  Curriculum │     │   │    │
│  │  │  │   Pipeline  │  │   Manager   │  │   Manager   │     │   │    │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │    │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │    │
│  │  │  │  Genetic    │  │  Tournament │  │    VM       │     │   │    │
│  │  │  │  Algorithm  │  │   Manager   │  │   Manager   │     │   │    │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │    │
│  │  └─────────────────────────────────────────────────────────┘   │    │
│  │                                                                  │    │
│  │  ┌─────────────────────────────────────────────────────────┐   │    │
│  │  │                   HYPER-V LAYER                          │   │    │
│  │  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐    │   │    │
│  │  │  │    VM #1     │ │    VM #2     │ │    VM #N     │    │   │    │
│  │  │  │  ┌────────┐  │ │  ┌────────┐  │ │  ┌────────┐  │    │   │    │
│  │  │  │  │ TZAR   │  │ │  │ TZAR   │  │ │  │ TZAR   │  │    │   │    │
│  │  │  │  └───┬────┘  │ │  └───┬────┘  │ │  └───┬────┘  │    │   │    │
│  │  │  │  ┌───▼────┐  │ │  ┌───▼────┐  │ │  ┌───▼────┐  │    │   │    │
│  │  │  │  │  BOT   │  │ │  │  BOT   │  │ │  │  BOT   │  │    │   │    │
│  │  │  │  │INTERFACE│  │ │  │INTERFACE│  │ │  │INTERFACE│  │    │   │    │
│  │  │  │  └───┬────┘  │ │  └───┬────┘  │ │  └───┬────┘  │    │   │    │
│  │  │  │  ┌───▼────┐  │ │  ┌───▼────┐  │ │  ┌───▼────┐  │    │   │    │
│  │  │  │  │NEURAL  │  │ │  │NEURAL  │  │ │  │NEURAL  │  │    │   │    │
│  │  │  │  │NETWORK │  │ │  │NETWORK │  │ │  │NETWORK │  │    │   │    │
│  │  │  │  └────────┘  │ │  └────────┘  │ │  └────────┘  │    │   │    │
│  │  │  └──────────────┘ └──────────────┘ └──────────────┘    │   │    │
│  │  └─────────────────────────────────────────────────────────┘   │    │
│  │                                                                  │    │
│  │  ┌─────────────────────────────────────────────────────────┐   │    │
│  │  │                    MONITORING                            │   │    │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │    │
│  │  │  │   Blazor    │  │   SignalR   │  │   Chart.js  │     │   │    │
│  │  │  │  Dashboard  │  │    Hub      │  │   Graphs    │     │   │    │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │    │
│  │  └─────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Fazy projektu

| Faza | Nazwa | Taski | Status | Postep |
|------|-------|-------|--------|--------|
| 0 | Prerequisites | 4 | PENDING | 0% |
| 1 | Game Interface | 6 | **COMPLETED** | 100% |
| 2 | Neural Network | 5 | PENDING | 0% |
| 3 | Genetic Algorithm | 5 | PENDING | 0% |
| 4 | Hyper-V Infrastructure | 6 | PENDING | 0% |
| 5 | Game State Detection | 4 | PENDING | 0% |
| 6 | Training Pipeline | 6 | PENDING | 0% |

**Calkowity postep:** 17% (6/36 taskow)

---

## Stack technologiczny

| Obszar | Technologia | Wersja |
|--------|-------------|--------|
| Jezyk glowny | C# / .NET | 10.0 |
| Screen Capture | Vortice.Windows (DXGI) | 3.8.1 |
| Image Processing | OpenCvSharp4 | 4.10.0 |
| ML Inference | ONNX Runtime | 1.16.x |
| Serialization | MessagePack | 3.1.3 |
| Database | SQLite | 3.x |
| IPC | System.IO.Pipes | Built-in |
| Virtualization | Hyper-V | Windows |
| Dashboard | Blazor Server | 10.0 |
| Charts | Chart.js | 4.x |
| Testing | xUnit + FluentAssertions | 9.0.0 / 8.0.1 |

---

## Zespol i role

| Agent | Rola | Fazy |
|-------|------|------|
| tzarbot-agent-dotnet-senior | Senior .NET Developer | 1, 2, 3, 4, 5 |
| tzarbot-agent-ai-senior | Senior AI/ML Engineer | 2, 3, 5, 6 |
| tzarbot-agent-fullstack-blazor | Fullstack Blazor Developer | 6 |
| tzarbot-agent-hyperv-admin | Hyper-V Infrastructure Admin | 0, 4 |
| QA_INTEGRATION | QA/Integration Specialist | 1, 2, 4, 5, 6 |

---

## Kluczowe dokumenty

### Plany
| Dokument | Sciezka |
|----------|---------|
| Plan glowny | `plans/1general_plan.md` |
| Workflow implementacji | `plans/2_implementation_workflow.md` |
| Plan Fazy 0 | `plans/phase_0_prerequisites.md` |
| Plan Fazy 1 | `plans/phase_1_detailed.md` |
| Plan Fazy 2 | `plans/phase_2_detailed.md` |
| Plan Fazy 3 | `plans/phase_3_detailed.md` |
| Plan Fazy 4 | `plans/phase_4_detailed.md` |
| Plan Fazy 5 | `plans/phase_5_detailed.md` |
| Plan Fazy 6 | `plans/phase_6_detailed.md` |

### Zarzadzanie projektem
| Dokument | Sciezka |
|----------|---------|
| Backlogi faz | `project_management/backlog/` |
| Wykres Gantta | `project_management/gantt.md` |
| Time Tracking | `project_management/timetracking.md` |
| Progress Dashboard | `project_management/progress_dashboard.md` |
| Demo Phase 1 | `project_management/demo/phase_1_demo.md` |

### Raporty
| Dokument | Sciezka |
|----------|---------|
| Audyt AI | `reports/1_ai_resource_audit.md` |
| Raport Phase 1 | `reports/2_phase1_completion_report.md` |
| Progress JSON | `reports/progress.json` |

### Sledzenie
| Dokument | Sciezka |
|----------|---------|
| Historia czatu | `chat_history.md` |
| Progress workflow | `workflow_progress.md` |

---

## Kontakt i wsparcie

- **Repozytorium:** `C:\Users\maciek\ai_experiments\tzar_bot`
- **Gra Tzar:** https://tza.red/
- **Instalator gry:** `files/tzared.windows.zip`

---

## Historia aktualizacji

| Data | Wersja | Zmiany |
|------|--------|--------|
| 2025-12-07 | 1.0 | Utworzenie dokumentu, Faza 1 ukonczona |
