# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tzar Bot - AI bot for the strategy game Tzar (https://tza.red/) using genetic algorithms and neural networks.

## Chat History Protocol

**IMPORTANT:** After each user message and assistant response, append a summary to `chat_history.md`:

```markdown
---

### User [YYYY-MM-DD HH:MM]:
[Paste the user's message/request]

### Assistant:
[Brief summary of what was done, key decisions made, files created/modified]
```

This ensures continuity across sessions and documents project progress.

## Workflow Progress Tracking

**CRITICAL:** During workflow execution, track progress in `workflow_progress.md`:

### Starting a Phase/Task
When starting any phase or task, record it:
```markdown
## Current Session: [YYYY-MM-DD HH:MM]

### Started:
- Phase X.TaskY: [Task Name] - STARTED [timestamp]
- Agent: [Agent ID - e.g., DOTNET_SENIOR, AI_SENIOR]

### Status: IN_PROGRESS
```

### Completing a Phase/Task
When completing, update the record:
```markdown
### Completed:
- Phase X.TaskY: [Task Name] - COMPLETED [timestamp]
- Result: [SUCCESS/PARTIAL/FAILED]
- Notes: [Any important observations]
- Files created/modified: [list]
```

### Resuming Workflow
When resuming an interrupted session:
1. Read `workflow_progress.md` to find last completed step
2. Verify the last step was fully completed (check git status, run tests)
3. Continue from the next pending task
4. Update progress file before starting

### Progress File Structure
```markdown
# TzarBot Workflow Progress

## Last Completed Step
- Phase: X
- Task: Y
- Timestamp: [YYYY-MM-DD HH:MM]
- Status: COMPLETED

## Current Phase Progress
| Task | Status | Agent | Started | Completed |
|------|--------|-------|---------|-----------|
| F1.T1 | COMPLETED | DOTNET_SENIOR | 2024-01-15 10:00 | 2024-01-15 12:30 |
| F1.T2 | IN_PROGRESS | DOTNET_SENIOR | 2024-01-15 13:00 | - |
| F1.T3 | PENDING | DOTNET_SENIOR | - | - |

## Session History
- Session 1: F0.T1 -> F0.T4 (completed)
- Session 2: F1.T1 -> F1.T3 (interrupted at F1.T3)
- Session 3: Resumed from F1.T3
```

This ensures workflow can be safely interrupted and resumed at any point.

## Build & Development Commands

```bash
# Build solution
dotnet build src/TzarBot.sln

# Run tests
dotnet test src/TzarBot.sln

# Run specific project
dotnet run --project src/TzarBot.GameInterface/TzarBot.GameInterface.csproj
```

## Architecture

See `plans/1general_plan.md` for the complete project architecture covering:
- Phase 1: Game Interface (screen capture + input injection)
- Phase 2: Neural Network Architecture
- Phase 3: Genetic Algorithm
- Phase 4: Hyper-V Infrastructure
- Phase 5: Game Result Detection
- Phase 6: Training Pipeline

## Key Technologies

- **Language:** C# / .NET 8
- **ML Inference:** ONNX Runtime
- **Screen Capture:** SharpDX / Vortice.Windows (DXGI)
- **Image Processing:** OpenCvSharp4
- **Virtualization:** Hyper-V + PowerShell
- **Dashboard:** Blazor Server

## Available Agents (Slash Commands)

### Konwencja nazewnictwa

**WAŻNE:** Wszystkie agenty MUSZĄ mieć prefix `agent-` w nazwie pliku i w polu `name:`. Dzięki temu można je łatwo znaleźć za pomocą `/agent-*` lub wyszukując "agent-".

Przykład: `agent-project-manager.md` z `name: agent-project-manager`

### Agenci Projektowi (TzarBot)

| Komenda | Opis | Skills |
|---------|------|--------|
| `tzarbot-agent-dotnet-senior` | Senior .NET/C# Developer | csharp, dotnet, windows-api |
| `tzarbot-agent-ai-senior` | Senior AI/ML Engineer | machine-learning, neural-networks, genetic-algorithms, onnx |
| `tzarbot-agent-fullstack-blazor` | Fullstack Blazor Developer | blazor, signalr, chartjs, css |
| `tzarbot-agent-hyperv-admin` | Hyper-V Infrastructure Admin | powershell, hyper-v, windows-server, networking |

### Agenci Systemowi

| Komenda | Opis | Użycie |
|---------|------|--------|
| `/agent-audit` | Audyt workflow i tasków | `/agent-audit [dodatkowe instrukcje]` |
| `/agent-optimizer` | Optymalizacja zasobów AI | `/agent-optimizer [zakres analizy]` |
| `/agent-project-manager` | Zarządzanie projektem (backlog, Gantt, metryki, demo) | `/agent-project-manager [zakres]` |

### Przykłady użycia:
```bash
# Audyt całego workflow
/agent-audit

# Audyt ze szczególnym naciskiem na Phase 1
/agent-audit Skup się na Phase 1 i zależnościach między taskami

# Optymalizacja wykorzystania AI
/agent-optimizer

# Analiza kosztów modeli
/agent-optimizer Sprawdź czy używamy odpowiednich modeli do prostych zadań
```

## Other Slash Commands

| Komenda | Opis |
|---------|------|
| `/continue-workflow` | Kontynuuj workflow od miejsca zatrzymania (czyta `continue.md`) |
| `/prompt [opis]` | Generuje nowy prompt i zapisuje w katalogu prompts |
| `/audit [instrukcje]` | Szybki audyt (bez agenta) |

## Workflow Interruption Protocol

**WAŻNE:** Przy zatrzymaniu workflow (np. oczekiwanie na instalację, przerwanie przez użytkownika):

1. **Zaktualizuj `continue.md`** z aktualnym statusem:
   - Ostatnia ukończona faza/task
   - Aktualny task i jego status
   - Powód wstrzymania
   - Następne kroki po wznowieniu
   - Komendy do wykonania

2. **Format `continue.md`:**
```markdown
# TzarBot Workflow Continuation Report

**Ostatnia aktualizacja:** [data]
**Status:** WSTRZYMANY

## Status aktualny
| Pole | Wartość |
|------|---------|
| Ostatnia ukończona faza | ... |
| Aktualny task | ... |
| Status | WSTRZYMANY - [powód] |

## Następne kroki po wznowieniu
1. ...
2. ...
```

3. **Wznowienie:** Użyj `/continue-workflow` aby kontynuować od miejsca zatrzymania

## Project Structure

```
tzar_bot/
├── CLAUDE.md                 # This file - project guidance
├── chat_history.md           # Conversation log
├── workflow_progress.md      # Current workflow status
├── plans/                    # Project plans and documentation
│   └── 1general_plan.md
├── prompts/                  # Reusable prompts for Claude
│   └── 1_planning_prompt.md
├── project_management/       # Project tracking artifacts
│   ├── project_overview.md   # High-level project summary
│   ├── progress_dashboard.md # Current status dashboard
│   ├── gantt.md              # Mermaid Gantt chart
│   ├── timetracking.md       # Time metrics and velocity
│   ├── backlog/              # Phase backlogs
│   │   ├── phase_0_backlog.md  # Prerequisites
│   │   ├── phase_1_backlog.md  # Game Interface ✅ COMPLETED
│   │   ├── phase_2_backlog.md  # Neural Network
│   │   ├── phase_3_backlog.md  # Genetic Algorithm
│   │   ├── phase_4_backlog.md  # Hyper-V Infrastructure
│   │   ├── phase_5_backlog.md  # Game State Detection
│   │   └── phase_6_backlog.md  # Training Pipeline
│   └── demo/                 # Demo documentation per phase
│       └── phase_1_demo.md   # Phase 1 demo instructions
├── src/                      # Source code
│   ├── TzarBot.sln           # Main solution
│   ├── TzarBot.GameInterface/      # Screen capture & input
│   ├── TzarBot.GameInterface.Tests/  # Unit tests
│   └── ...
└── .claude/
    ├── commands/             # Slash commands (/agent-*, /prompt, /audit)
    └── agents/               # Agent definitions
```

## Project Status

| Metric | Value |
|--------|-------|
| Overall Progress | 17% (6/36 tasks) |
| Current Phase | Phase 1 COMPLETED |
| Next Phase | Phase 0 (Prerequisites) or Phase 2 (Neural Network) |
| Unit Tests | 46 PASS |

See `project_management/progress_dashboard.md` for detailed status.
