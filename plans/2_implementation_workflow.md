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
- [ ] F0.T1: Host Machine Setup (Hyper-V, .NET 8 SDK, directories)
- [ ] F0.T2: Development VM Setup (create VM, install Windows, configure network)
- [ ] F0.T3: Tzar Game Installation (install from files/tzared.windows.zip)
- [ ] F0.T4: Environment Verification (test connectivity, document configuration)

See `plans/phase_0_prerequisites.md` for detailed instructions.

### Phase 1: Game Interface
- [ ] F1.T1: Project Setup
- [ ] F1.T2: Screen Capture Implementation
- [ ] F1.T3: Input Injection Implementation
- [ ] F1.T4: IPC Named Pipes
- [ ] F1.T5: Window Detection
- [ ] F1.T6: Integration & Smoke Tests

### Phase 2: Neural Network
- [ ] F2.T1: NetworkGenome & Serialization
- [ ] F2.T2: Image Preprocessor
- [ ] F2.T3: ONNX Network Builder
- [ ] F2.T4: Inference Engine
- [ ] F2.T5: Integration Tests

### Phase 3: Genetic Algorithm
- [ ] F3.T1: GA Engine Core
- [ ] F3.T2: Mutation Operators
- [ ] F3.T3: Crossover Operators
- [ ] F3.T4: Selection & Elitism
- [ ] F3.T5: Fitness Calculator & Persistence

### Phase 4: Hyper-V Infrastructure
- [ ] F4.T1: Template VM Preparation (Manual)
- [ ] F4.T2: VM Cloning Scripts
- [ ] F4.T3: VM Manager Implementation
- [ ] F4.T4: Orchestrator Service
- [ ] F4.T5: Communication Protocol
- [ ] F4.T6: Multi-VM Integration Test

### Phase 5: Game State Detection
- [ ] F5.T1: Template Capture Tool
- [ ] F5.T2: GameStateDetector
- [ ] F5.T3: GameMonitor
- [ ] F5.T4: Stats Extraction (OCR)

### Phase 6: Training Pipeline
- [ ] F6.T1: Training Loop Core
- [ ] F6.T2: Curriculum Manager
- [ ] F6.T3: Checkpoint Manager
- [ ] F6.T4: Tournament System
- [ ] F6.T5: Blazor Dashboard
- [ ] F6.T6: Full Integration Test

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
