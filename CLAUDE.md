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
