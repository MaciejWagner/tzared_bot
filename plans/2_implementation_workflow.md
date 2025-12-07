# Tzar Bot - Implementation Workflow

## Overview

This document defines the complete implementation workflow for the Tzar Bot project. Each phase is broken down into atomic tasks that can be completed in a single Claude Code session.

## Workflow Approach

**Selected: Option D - Hybrid with Hooks**

Combining:
- Slash commands (`.claude/commands/`) for each task
- Validation hooks for automatic testing
- Master checklist for tracking progress

## Directory Structure

```
tzar_bot/
├── .claude/
│   └── commands/
│       ├── phase1-setup.md
│       ├── phase1-capture.md
│       └── ...
├── src/
│   ├── TzarBot.GameInterface/
│   ├── TzarBot.NeuralNetwork/
│   ├── TzarBot.GeneticAlgorithm/
│   ├── TzarBot.Orchestrator/
│   ├── TzarBot.StateDetection/
│   ├── TzarBot.Training/
│   └── TzarBot.Dashboard/
├── tests/
│   └── TzarBot.Tests/
│       ├── Phase1/
│       ├── Phase2/
│       └── ...
├── scripts/
│   ├── validate_phase.ps1
│   ├── run_all_tests.ps1
│   └── generate_progress_report.ps1
├── reports/
│   ├── progress.json
│   └── test_results/
├── plans/
│   ├── 1general_plan.md
│   ├── 2_implementation_workflow.md
│   ├── phase_1_detailed.md
│   ├── phase_2_detailed.md
│   ├── phase_3_detailed.md
│   ├── phase_4_detailed.md
│   ├── phase_5_detailed.md
│   └── phase_6_detailed.md
└── prompts/
    ├── phase_1/
    ├── phase_2/
    └── ...
```

## Task Naming Convention

```
F{phase}.T{task}
Example: F1.T1 = Phase 1, Task 1
```

## Execution Model

### Before Each Task
1. Read the task prompt from `prompts/phase_X/FX.TY_name.md`
2. Verify all dependencies are completed (check `reports/progress.json`)
3. Read required input files

### After Each Task
1. Run validation: `.\scripts\validate_phase.ps1 -Phase X -Task Y`
2. Update `reports/progress.json`
3. Commit changes with tag `FX.TY-complete`

## Agent Definitions

The following specialized agents are defined for task execution:

### DOTNET_SENIOR - Senior .NET Backend Developer
**Minimum Requirements:**
- 5+ years experience in C#/.NET
- Proficiency in async/await, LINQ, DI
- Experience with unit testing (xUnit)
- Knowledge of MessagePack/serialization
- Basic Win32 API knowledge

### AI_SENIOR - Senior AI/ML Developer
**Minimum Requirements:**
- 3+ years experience in ML/AI
- Knowledge of ONNX Runtime
- Experience with neural networks (CNN, Dense)
- Familiarity with genetic algorithms
- C#/Python for ML

### DEVOPS_SENIOR - DevOps/Infrastructure Engineer
**Minimum Requirements:**
- 3+ years experience with Hyper-V
- Advanced PowerShell
- Knowledge of Windows Server
- Experience with VM automation
- Networking (NAT, virtual switches)

### FULLSTACK_BLAZOR - Full-Stack Developer (Blazor)
**Minimum Requirements:**
- 2+ years experience in Blazor Server
- SignalR
- Chart.js / visualization
- HTML/CSS/JavaScript
- REST API

### QA_INTEGRATION - QA/Integration Specialist
**Minimum Requirements:**
- 3+ years testing experience
- Integration testing
- Performance testing
- Knowledge of xUnit, FluentAssertions
- Complex systems debugging

## Phase Summary

| Phase | Name | Tasks | Complexity | Dependencies |
|-------|------|-------|------------|--------------|
| **0** | **Prerequisites** | **4** | **S** | **None** |
| 1 | Game Interface | 6 | M | F0 |
| 2 | Neural Network | 5 | L | F1 |
| 3 | Genetic Algorithm | 5 | M | F2 |
| 4 | Hyper-V Infrastructure | 6 | XL | F1 |
| 5 | Game State Detection | 4 | M | F1 |
| 6 | Training Pipeline | 6 | L | F3, F4, F5 |

## Master Checklist

### Phase 0: Prerequisites (MUST COMPLETE FIRST)
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F0.T1 | Host Machine Setup | DEVOPS_SENIOR | [ ] |
| F0.T2 | Development VM Setup | DEVOPS_SENIOR | [ ] |
| F0.T3 | Tzar Game Installation | DEVOPS_SENIOR | [ ] |
| F0.T4 | Environment Verification | DEVOPS_SENIOR | [ ] |

See `plans/phase_0_prerequisites.md` for detailed instructions.

### Phase 1: Game Interface
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F1.T1 | Project Setup | DOTNET_SENIOR | [ ] |
| F1.T2 | Screen Capture Implementation | DOTNET_SENIOR | [ ] |
| F1.T3 | Input Injection Implementation | DOTNET_SENIOR | [ ] |
| F1.T4 | IPC Named Pipes | DOTNET_SENIOR | [ ] |
| F1.T5 | Window Detection | DOTNET_SENIOR | [ ] |
| F1.T6 | Integration & Smoke Tests | QA_INTEGRATION | [ ] |

### Phase 2: Neural Network
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F2.T1 | NetworkGenome & Serialization | AI_SENIOR | [ ] |
| F2.T2 | Image Preprocessor | DOTNET_SENIOR | [ ] |
| F2.T3 | ONNX Network Builder | AI_SENIOR | [ ] |
| F2.T4 | Inference Engine | AI_SENIOR | [ ] |
| F2.T5 | Integration Tests | QA_INTEGRATION | [ ] |

### Phase 3: Genetic Algorithm
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F3.T1 | GA Engine Core | DOTNET_SENIOR | [ ] |
| F3.T2 | Mutation Operators | DOTNET_SENIOR | [ ] |
| F3.T3 | Crossover Operators | DOTNET_SENIOR | [ ] |
| F3.T4 | Selection & Elitism | DOTNET_SENIOR | [ ] |
| F3.T5 | Fitness Calculator & Persistence | AI_SENIOR | [ ] |

### Phase 4: Hyper-V Infrastructure
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F4.T1 | Template VM Preparation | DEVOPS_SENIOR | [ ] MANUAL |
| F4.T2 | VM Cloning Scripts | DEVOPS_SENIOR | [ ] |
| F4.T3 | VM Manager Implementation | DOTNET_SENIOR | [ ] |
| F4.T4 | Orchestrator Service | DEVOPS_SENIOR | [ ] |
| F4.T5 | Communication Protocol | DOTNET_SENIOR | [ ] |
| F4.T6 | Multi-VM Integration Test | QA_INTEGRATION | [ ] |

### Phase 5: Game State Detection
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F5.T1 | Template Capture Tool | QA_INTEGRATION | [ ] |
| F5.T2 | GameStateDetector | AI_SENIOR | [ ] |
| F5.T3 | GameMonitor | QA_INTEGRATION | [ ] |
| F5.T4 | Stats Extraction (OCR) | AI_SENIOR | [ ] |

### Phase 6: Training Pipeline
| Task | Name | Agent | Status |
|------|------|-------|--------|
| F6.T1 | Training Loop Core | AI_SENIOR | [ ] |
| F6.T2 | Curriculum Manager | AI_SENIOR | [ ] |
| F6.T3 | Checkpoint Manager | QA_INTEGRATION | [ ] |
| F6.T4 | Tournament System | AI_SENIOR | [ ] |
| F6.T5 | Blazor Dashboard | FULLSTACK_BLAZOR | [ ] |
| F6.T6 | Full Integration Test | QA_INTEGRATION | [ ] |

## Progress Tracking

Progress is tracked in `reports/progress.json`:

```json
{
  "lastUpdated": "2024-01-15T10:30:00Z",
  "phases": {
    "1": {
      "name": "Game Interface",
      "status": "in_progress",
      "tasks": {
        "F1.T1": {
          "name": "Project Setup",
          "status": "completed",
          "completedAt": "2024-01-15T09:00:00Z",
          "testsPassed": true
        },
        "F1.T2": {
          "name": "Screen Capture",
          "status": "in_progress",
          "startedAt": "2024-01-15T10:00:00Z"
        }
      }
    }
  }
}
```

## Error Handling Protocol

### If a test fails:
1. Check the error message in test output
2. Review the generated code against requirements
3. If issue is clear: fix and re-run validation
4. If issue is unclear:
   - Check dependencies are correctly implemented
   - Review the original plan for missed requirements
   - Ask for clarification if needed

### If a task is blocked:
1. Document the blocker in `reports/blockers.md`
2. Check if another task can be done in parallel
3. If the blocker is from an external factor (missing software, permissions), resolve before continuing

## Rollback Protocol

If a task corrupts the project:
1. Check git status for uncommitted changes
2. `git stash` if there are valuable partial changes
3. `git checkout HEAD -- .` to restore last commit
4. Review what went wrong before retrying

## Validation Commands

```powershell
# Validate specific task
.\scripts\validate_phase.ps1 -Phase 1 -Task 1

# Validate entire phase
.\scripts\validate_phase.ps1 -Phase 1

# Run all tests
.\scripts\run_all_tests.ps1

# Generate progress report
.\scripts\generate_progress_report.ps1
```

## Daily Workflow

1. Check `reports/progress.json` for current state
2. Identify next pending task
3. Execute task using slash command or prompt file
4. Run validation
5. Commit with appropriate message
6. Update progress
7. Repeat

## Parallel Work Opportunities

After Phase 1 is complete, these can run in parallel:
- Phase 2 (Neural Network)
- Phase 4 (Hyper-V) - except final integration
- Phase 5 (State Detection)

Phase 3 must wait for Phase 2.
Phase 6 must wait for Phases 3, 4, and 5.

---

*See individual phase_X_detailed.md files for complete task definitions.*
