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

<!-- Add commands as they are established -->
<!-- Example: npm install, npm run build, npm test, etc. -->

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

## Project Structure

```
tzar_bot/
├── CLAUDE.md           # This file - project guidance
├── chat_history.md     # Conversation log
├── plans/              # Project plans and documentation
│   └── 1general_plan.md
└── prompts/            # Reusable prompts for Claude
    └── 1_planning_prompt.md
```
