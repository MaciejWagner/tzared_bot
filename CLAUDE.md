# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## Project Overview

**TzarBot** - AI bot for the strategy game Tzar (https://tza.red/) using genetic algorithms and neural networks.

**Current Status:** Training locally on host machine with GPU (Edge + Playwright for browser automation).

## Build & Development Commands

```bash
# Build solution
dotnet build TzarBot.sln

# Run tests
dotnet test TzarBot.sln

# Build TrainingRunner (self-contained)
dotnet publish tools/TrainingRunner/TrainingRunner.csproj -c Release -r win-x64 --self-contained -o publish/TrainingRunner

# Run training locally
./publish/TrainingRunner/TrainingRunner.exe <model.onnx> <map.tzared> <duration_seconds> [output.json]
```

## Key Technologies

| Component | Technology |
|-----------|------------|
| Language | C# / .NET 8 |
| ML Inference | ONNX Runtime |
| Browser Automation | Playwright (Edge) |
| Image Processing | SkiaSharp, OpenCvSharp4 |
| Dashboard | Blazor Server |

## Project Structure

```
tzar_bot/
├── CLAUDE.md                 # This file
├── continue.md               # Current status and next steps
├── env_settings.md           # Environment configuration
├── src/                      # Source code
│   ├── TzarBot.BrowserInterface/    # Playwright browser control
│   ├── TzarBot.NeuralNetwork/       # ONNX inference & preprocessing
│   ├── TzarBot.GeneticAlgorithm/    # GA engine
│   ├── TzarBot.Common/              # Shared models
│   └── TzarBot.Dashboard/           # Blazor monitoring
├── tools/
│   └── TrainingRunner/       # Main training executable
├── training/                 # Training data and results
│   └── generation_0/         # First generation networks
├── plans/                    # Architecture documentation
├── docs/                     # Technical documentation
└── archive/                  # Historical files (not actively used)
```

## Browser Configuration (IMPORTANT)

tza.red requires WebGPU which doesn't work in VMs. Current working config uses **Edge + SwiftShader**:

```csharp
// PlaywrightGameInterface.cs
Channel = "msedge",
Args = new[] {
    "--use-gl=swiftshader",
    "--enable-unsafe-swiftshader"
}
```

See `docs/browser_testing_results.md` for details.

## Git Commit Rules

- NEVER add "Co-Authored-By" or AI attribution
- Keep commit messages clean and professional

## Available Agents

| Agent | Purpose |
|-------|---------|
| `tzarbot-agent-dotnet-senior` | C#/.NET development |
| `tzarbot-agent-ai-senior` | Neural networks, ONNX, GA |
| `tzarbot-agent-fullstack-blazor` | Dashboard UI |
| `agent-project-manager` | Project coordination |

## Workflow

1. Update `continue.md` when stopping work
2. Use `/continue-workflow` to resume
3. Track progress in session notes

## Training Rules

- After evolving a new generation, **DELETE the previous generation** to save disk space
- Keep only the current generation in `training/generation_N/`
- Results and reports can be kept if needed for analysis

## Training Configuration

Default training parameters (script: `scripts/train_generation_staggered.ps1`):

| Parameter | Default | Description |
|-----------|---------|-------------|
| `ParallelSessions` | 3 | Number of concurrent training windows |
| `StaggerDelaySeconds` | 4 | Delay between starting runners (GPU init is heavy) |
| `MapPath` | Previous map | Training map (use same as last run by default) |
| `Duration` | 40 | Trial duration in seconds |
| `TrialsPerNetwork` | 5 | Number of trials per network |

Example:
```powershell
./scripts/train_generation_staggered.ps1 -GenerationPath "training/generation_6" -StaggerDelaySeconds 4 -ParallelSessions 3
```

**Why staggered starts?** CUDA/cuDNN kernel compilation on first inference takes 2-6 seconds and uses significant GPU. Staggering prevents all instances from initializing simultaneously.

## Training Resume Protocol

When resuming training after context loss, crash, or shutdown:

### 1. Check current state
```powershell
# Find latest generation
ls training/ | Sort-Object Name

# Check which networks have results
ls training/generation_N/results/*.json | Measure-Object
ls training/generation_N/onnx/*.onnx | Measure-Object

# See completed trials per network
ls training/generation_N/results/ | Group-Object { $_.Name -replace '_trial\d+\.json$' }
```

### 2. Determine resume point

| State | Action |
|-------|--------|
| No results folder | Training not started - run full training |
| Partial results (some networks done) | Resume with remaining networks |
| All results present | Proceed to evolution |
| ONNX files in next gen | Evolution done - start next training |

### 3. Resume training
```powershell
# If partial - check which networks are missing results
$done = ls training/generation_N/results/*.json | ForEach-Object { $_.Name -replace '_.*' } | Sort-Object -Unique
$all = ls training/generation_N/onnx/*.onnx | ForEach-Object { $_.BaseName }
$missing = $all | Where-Object { $_ -notin $done }
echo "Missing: $missing"

# Re-run training (script skips existing results or run manually for specific networks)
./scripts/train_generation_staggered.ps1 -GenerationPath "training/generation_N"
```

### 4. After training complete
```powershell
# Summarize results (if not auto-generated)
./scripts/summarize_results.ps1 -GenerationPath "training/generation_N"

# Evolve next generation (example for 40 networks with leader network_00)
./publish/EvolveGeneration/EvolveGeneration.exe `
    training/generation_N `
    training/generation_N/results/summary.json `
    training/generation_N+1 `
    --population 40 `
    --elite 0 `
    --mutated-copies 4 `
    --forced-parent 0 `
    --forced-crossover-count 10 `
    --random-ratio 0.15 `
    --top 10

# IMPORTANT: Delete old generation immediately after evolution (save disk space!)
rm -r training/generation_N-1
```

**Evolution parameters explained:**
- `--population 40` - 40 networks in new generation
- `--elite 0` - no unchanged copies (mutations handle this)
- `--mutated-copies 4` - 4 mutated variants of best network
- `--forced-parent 0` - network_00 as forced parent
- `--forced-crossover-count 10` - 10 crossovers with forced parent
- `--random-ratio 0.15` - 6 random networks (15% of 40)
- Normal crossovers: remaining (40 - 0 - 4 - 10 - 6 = 20)

### 5. Update continue.md
Always update `continue.md` with:
- Current generation number
- Training status (in progress / complete)
- Next action to take

## Evolution Log

After each training cycle, update `training/evolution_log.md` with:
- Training results (victory rate, top 5 networks)
- Map used
- Evolution parameters
- Any notes about crossover compatibility

**Format for new generation entry:**
```markdown
### Generation N
| Parametr | Wartość |
|----------|---------|
| Data | YYYY-MM-DD |
| Mapa | training-X.tzared |
| Czas treningu | XXm XXs |
| Victory rate | XX% (X/40) |

**Top 5:**
| Network | V | D | T | Fitness | AvgDur | AvgAct |
|---------|---|---|---|---------|--------|--------|
| network_XX | X | X | X | X.X | XXs | XX |

**Ewolucja -> GenN+1:**
- Lider: network_XX (Fitness X.X)
- Uwagi: ...
```

## Key Files

| File | Purpose |
|------|---------|
| `continue.md` | Current status, next steps |
| `training/evolution_log.md` | History of all generations and evolution |
| `env_settings.md` | Paths, configuration |
| `docs/browser_testing_results.md` | Browser compatibility |
| `training/generation_N/` | Current training data |
