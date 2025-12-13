# TzarBot Training Strategy

## Overview

This document defines the complete training strategy for TzarBot, including the genetic algorithm approach, neural network architecture, training phases, and fitness functions.

## Table of Contents

1. [Training Philosophy](#training-philosophy)
2. [Neural Network Architecture](#neural-network-architecture)
3. [Genetic Algorithm Design](#genetic-algorithm-design)
4. [Training Phases](#training-phases)
5. [Fitness Functions](#fitness-functions)
6. [Infrastructure & Throughput](#infrastructure--throughput)
7. [Evolution Roadmap](#evolution-roadmap)

---

## Training Philosophy

### Progressive Difficulty Approach

The bot learns through progressively harder challenges:

```
Basics → Easy AI → Normal AI → Hard AI → Self-play
   ↓         ↓          ↓          ↓         ↓
 Micro    Simple     Tactical   Strategic  Meta
 skills   combat     decisions  planning   gaming
```

### Key Principles

1. **Controlled Environment First** - Custom maps with specific objectives before open gameplay
2. **Measurable Progress** - Clear fitness metrics at each stage
3. **Gradual Complexity** - Don't expose bot to hard challenges until basics are mastered
4. **Parallel Training** - Utilize multiple VMs for throughput

---

## Neural Network Architecture

### Input Processing

Raw screenshots are too large for direct processing:

| Resolution | Pixels | With RGB | Problem |
|------------|--------|----------|---------|
| 1920x1080 | 2.07M | 6.22M | Too large |
| 960x540 | 518K | 1.55M | Still large |
| **160x90** | **14.4K** | **43.2K** | ✅ Manageable |

**Solution:** Downscale to 160x90 + CNN feature extraction

### Network Architecture (Stage 1 - Fixed)

```
┌─────────────────────────────────────────────────────────────────┐
│                         INPUT LAYER                              │
│                   160 x 90 x 3 = 43,200                         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    CONVOLUTIONAL BLOCK 1                         │
│         Conv2D(32 filters, 5x5, stride=2) + ReLU                │
│                   → 78 x 43 x 32                                 │
│                     MaxPool(2x2)                                 │
│                   → 39 x 21 x 32                                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    CONVOLUTIONAL BLOCK 2                         │
│           Conv2D(64 filters, 3x3) + ReLU                        │
│                   → 37 x 19 x 64                                 │
│                     MaxPool(2x2)                                 │
│                   → 18 x 9 x 64                                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    CONVOLUTIONAL BLOCK 3                         │
│          Conv2D(128 filters, 3x3) + ReLU                        │
│                   → 16 x 7 x 128                                 │
│                     MaxPool(2x2)                                 │
│                   → 8 x 3 x 128 = 3,072                         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       FLATTEN                                    │
│                        3,072                                     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    DENSE LAYER 1                                 │
│               Dense(512) + LeakyReLU                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    DENSE LAYER 2                                 │
│               Dense(256) + LeakyReLU                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      OUTPUT HEADS                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   GridX     │  │   GridY     │  │      ActionType         │  │
│  │ 20 neurons  │  │ 15 neurons  │  │      10 neurons         │  │
│  │  (softmax)  │  │  (softmax)  │  │       (softmax)         │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│                                                                  │
│  Total Output: 45 neurons                                        │
└─────────────────────────────────────────────────────────────────┘
```

### Action Space (Hybrid Approach)

The output is split into three heads:

| Head | Neurons | Encoding | Description |
|------|---------|----------|-------------|
| GridX | 20 | Softmax | Horizontal screen position (0-19) |
| GridY | 15 | Softmax | Vertical screen position (0-14) |
| ActionType | 10 | Softmax | Type of action to perform |

**Action Types:**

| Index | Action | Description |
|-------|--------|-------------|
| 0 | LeftClick | Select unit/building, confirm action |
| 1 | RightClick | Move, attack, gather |
| 2 | DoubleClick | Select all units of type |
| 3 | DragSelect | Start box selection |
| 4 | Hotkey_1 | Hotkey group 1 |
| 5 | Hotkey_2 | Hotkey group 2 |
| 6 | Hotkey_3 | Hotkey group 3 |
| 7 | Hotkey_B | Build menu |
| 8 | Hotkey_T | Technology menu |
| 9 | NoOp | Do nothing this frame |

### Parameter Count

| Layer | Parameters |
|-------|------------|
| Conv1 (5x5x3x32 + 32 bias) | 2,432 |
| Conv2 (3x3x32x64 + 64 bias) | 18,496 |
| Conv3 (3x3x64x128 + 128 bias) | 73,856 |
| Dense1 (3072x512 + 512 bias) | 1,573,376 |
| Dense2 (512x256 + 256 bias) | 131,328 |
| Output (256x45 + 45 bias) | 11,565 |
| **TOTAL** | **~1.81M parameters** |

---

## Genetic Algorithm Design

### Stage 1: Simple Neuroevolution (Fixed Weights)

In Stage 1, we evolve only the weights of a fixed architecture network.

#### Genome Structure

```csharp
public class SimpleGenome
{
    // Core genetic material
    public Guid Id { get; set; }
    public float[] Weights { get; set; }  // ~1.81M floats

    // Fitness tracking
    public float Fitness { get; set; }
    public float AdjustedFitness { get; set; }

    // Lineage tracking
    public int Generation { get; set; }
    public Guid? Parent1Id { get; set; }
    public Guid? Parent2Id { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public string MutationType { get; set; }  // "gaussian", "uniform", "crossover"
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
}
```

#### Genetic Operators

**Selection: Tournament Selection (k=3)**
```
1. Randomly select 3 individuals from population
2. Choose the one with highest fitness
3. Repeat for each parent needed
```

**Crossover: Uniform Crossover**
```
For each weight index i:
    if random() < 0.5:
        child.weights[i] = parent1.weights[i]
    else:
        child.weights[i] = parent2.weights[i]
```

**Mutation: Gaussian Noise**
```
mutation_rate = 0.05  (5% of weights)
mutation_strength = 0.1  (σ for Gaussian)

For each weight:
    if random() < mutation_rate:
        weight += gaussian(0, mutation_strength)
```

#### Population Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Population Size | 100 | Balance between diversity and throughput |
| Elite Count | 10 | Top 10% preserved unchanged |
| Tournament Size | 3 | Selection pressure |
| Crossover Rate | 0.7 | 70% offspring from crossover |
| Mutation Rate | 0.05 | 5% of weights mutated |
| Mutation Strength | 0.1 | Gaussian σ |

### Stage 2: NEAT (Future Enhancement)

After Stage 1 is validated, upgrade to NEAT (NeuroEvolution of Augmenting Topologies).

#### NEAT Genome Structure

```csharp
public class NEATGenome
{
    public int Id { get; set; }
    public List<NodeGene> Nodes { get; set; }
    public List<ConnectionGene> Connections { get; set; }

    public float Fitness { get; set; }
    public float AdjustedFitness { get; set; }
    public int SpeciesId { get; set; }
}

public class NodeGene
{
    public int Id { get; set; }
    public NodeType Type { get; set; }  // Input, Hidden, Output
    public ActivationFunction Activation { get; set; }
    public float Bias { get; set; }
}

public class ConnectionGene
{
    public int InnovationNumber { get; set; }  // Global innovation counter
    public int FromNode { get; set; }
    public int ToNode { get; set; }
    public float Weight { get; set; }
    public bool Enabled { get; set; }
}
```

#### NEAT-Specific Features

1. **Innovation Numbers** - Track structural mutations globally
2. **Speciation** - Group similar topologies to protect innovation
3. **Complexification** - Start minimal, add complexity as needed

---

## Training Phases

### Phase 7.1: Basics (Educational Maps)

**Goal:** Learn fundamental game mechanics through custom scenarios.

| Scenario | Objective | Victory Condition | Time Limit |
|----------|-----------|-------------------|------------|
| Move01 | Unit movement | Peasant reaches flag marker | 2 min |
| Move02 | Multi-unit movement | 3 peasants reach different flags | 3 min |
| Gather01 | Wood gathering | Collect 100 wood | 3 min |
| Gather02 | Gold mining | Collect 100 gold | 3 min |
| Gather03 | Multi-resource | Collect 50 wood + 50 gold | 4 min |
| Build01 | Single building | Build a hut | 4 min |
| Build02 | Building chain | Build hut → barracks | 5 min |
| Attack01 | Basic combat | Destroy enemy building | 3 min |
| Attack02 | Unit combat | Kill 3 enemy units | 4 min |
| Tech01 | Research | Complete 1 technology | 5 min |
| Combined01 | Integration | Build army of 5 soldiers | 5 min |
| Combined02 | Full basics | Gather, build, train, attack | 5 min |

**Training Parameters:**
- Population: 100
- Generations: 50
- Games per genome: 3 (average fitness)
- Progression: Complete scenario when >80% of population wins

### Phase 7.2: Easy AI Combat

**Goal:** Learn to defeat weakest AI opponents.

| Stage | Map Size | Opponent | Time Limit | Win Rate Target |
|-------|----------|----------|------------|-----------------|
| 7.2.1 | Small (2p) | Easiest AI | 20 min | >50% |
| 7.2.2 | Small (2p) | Easy AI | 20 min | >40% |

**Training Parameters:**
- Population: 100
- Generations: 100
- Games per genome: 5

### Phase 7.3: Normal AI Combat

**Goal:** Compete against standard AI on various map sizes.

| Stage | Map Size | Opponent | Time Limit | Win Rate Target |
|-------|----------|----------|------------|-----------------|
| 7.3.1 | Small | Normal AI | 20 min | >50% |
| 7.3.2 | Medium | Normal AI | 30 min | >40% |
| 7.3.3 | Large | Normal AI | 45 min | >30% |

**Training Parameters:**
- Population: 100-200
- Generations: 200
- Games per genome: 5

### Phase 7.4: Maturity Test

**Goal:** Validate bot can defeat Hard AI consistently.

| Requirement | Value |
|-------------|-------|
| Map Size | Large |
| Opponent | Hard AI |
| Time Limit | None |
| Pass Criteria | Win 3 out of 5 games |

**Process:**
1. Select top 10 genomes from Phase 7.3
2. Each plays 5 games against Hard AI
3. If any genome passes → proceed to Phase 7.5
4. If none pass → return to Phase 7.3 for more training

### Phase 7.5: Self-Play Evolution

**Goal:** Continuous improvement through bot vs bot competition.

**Tournament Structure:**
- Round-robin tournament (each bot plays every other bot)
- ELO rating system for ranking
- Fitness = ELO score

**ELO System:**
```
K-factor = 32
Expected score: E = 1 / (1 + 10^((Rb - Ra) / 400))
New rating: Ra' = Ra + K * (S - E)
Where S = 1 (win), 0.5 (draw), 0 (loss)
```

**Population Management:**
- Keep top 50% by ELO
- Generate new individuals from top performers
- Periodically introduce "wild card" random genomes

---

## Fitness Functions

### Phase 7.1 Fitness (Basics)

```csharp
public float CalculateBasicsFitness(GameResult result)
{
    float fitness = 0;

    // Progress toward objective (0-200)
    float progressRatio = result.ObjectiveProgress / result.ObjectiveTarget;
    fitness += progressRatio * 200;

    // Time efficiency bonus (0-100)
    if (result.Won)
    {
        float timeRatio = 1 - (result.GameTimeSeconds / result.TimeLimitSeconds);
        fitness += timeRatio * 100;
    }

    // Micro-achievements
    fitness += result.UnitsCreated * 10;
    fitness += result.BuildingsBuilt * 20;
    fitness += result.ResourcesGathered * 0.1f;
    fitness += result.TechnologiesResearched * 30;

    // Victory bonus
    if (result.Won)
        fitness += 500;

    // Penalty for doing nothing
    if (result.ActionsPerformed < 10)
        fitness -= 100;

    return Math.Max(0, fitness);
}
```

### Phase 7.2-7.3 Fitness (AI Combat)

```csharp
public float CalculateCombatFitness(GameResult result)
{
    float fitness = 0;

    // === Survival Component (0-300) ===
    float survivalMinutes = result.SurvivalTimeSeconds / 60f;
    fitness += Math.Min(survivalMinutes * 15, 300);

    // === Economic Component (0-200) ===
    fitness += Math.Min(result.TotalResourcesGathered / 50, 200);

    // === Military Component (0-300) ===
    // Kill ratio
    float kills = result.EnemyUnitsKilled;
    float deaths = Math.Max(1, result.OwnUnitsLost);
    float killRatio = kills / deaths;
    fitness += Math.Min(killRatio * 50, 150);

    // Damage dealt
    fitness += Math.Min(result.DamageDealt / 100, 150);

    // === Building Component (0-100) ===
    fitness += result.BuildingsBuilt * 10;
    fitness += result.EnemyBuildingsDestroyed * 20;

    // === Outcome Bonuses ===
    if (result.Won)
    {
        fitness += 1000;

        // Speed bonus (faster win = better)
        float maxTime = result.TimeLimitSeconds;
        float timeRatio = 1 - (result.GameTimeSeconds / maxTime);
        fitness += timeRatio * 500;
    }
    else if (result.Draw)
    {
        fitness += 300;
    }

    // Penalty for very short games (likely crashed or stuck)
    if (result.GameTimeSeconds < 60)
        fitness -= 200;

    return Math.Max(0, fitness);
}
```

### Phase 7.5 Fitness (Self-Play)

```csharp
public float CalculateSelfPlayFitness(BotStats stats)
{
    // Primary: ELO rating
    float fitness = stats.EloRating;

    // Secondary: Game diversity bonus
    // Reward bots that can win in different ways
    float strategyDiversity = CalculateStrategyDiversity(stats.GameHistories);
    fitness += strategyDiversity * 50;

    // Tertiary: Consistency bonus
    // Reward bots with low variance in performance
    float consistency = 1 - stats.WinRateStandardDeviation;
    fitness += consistency * 100;

    return fitness;
}
```

---

## Infrastructure & Throughput

### VM Configuration

| VM | RAM | Purpose | Games/Hour |
|----|-----|---------|------------|
| Worker1 | 1.5 GB | Training games | ~12 |
| Worker2 | 1.5 GB | Training games | ~12 |
| Worker3 | 1.5 GB | Training games | ~12 |
| Worker4 | 1.5 GB | Training games | ~12 |
| Worker5 | 1.5 GB | Training games | ~12 |
| Worker6 | 1.5 GB | Training games | ~12 |
| **Total** | **9 GB** | | **~72/hour** |

*Note: 1 GB reserved for DEV VM when needed*

### Throughput Estimates

| Phase | Avg Game Time | Games/Hour (6 VM) | Pop Size | Gens | Time/Gen | Total Time |
|-------|---------------|-------------------|----------|------|----------|------------|
| 7.1 Basics | 5 min | 72 | 100 | 50 | 1.4h | ~70h |
| 7.2 Easy AI | 20 min | 18 | 100 | 100 | 5.5h | ~550h |
| 7.3 Normal AI | 30 min | 12 | 100 | 200 | 8.3h | ~1660h |
| 7.4 Maturity | 45 min | 8 | 10 | 10 | 1.25h | ~12h |

**Total Estimated Training Time: ~2300 hours (~96 days)**

### Optimization Strategies

1. **Early Termination** - End games when outcome is clear (e.g., all units lost)
2. **Checkpoint Saving** - Save population state every N generations
3. **Adaptive Game Length** - Shorter limits for early phases
4. **Parallel Evaluation** - Run multiple games per genome simultaneously

---

## Evolution Roadmap

### Stage 1: Simple Neuroevolution (Current Plan)

```
Timeline: Phases 7.1 - 7.5
Genome: Fixed architecture, evolve weights only
Complexity: ~1.81M parameters
Goal: Validate entire training pipeline
```

### Stage 2: NEAT Integration (Future)

```
Trigger: After Stage 1 achieves Hard AI victory
Genome: NEAT with topology evolution
Complexity: Starts minimal, grows as needed
Goal: Discover optimal architecture automatically
```

### Stage 3: Advanced Techniques (Research)

```
Potential enhancements:
- Novelty search (reward behavioral diversity)
- Curriculum learning (automatic difficulty adjustment)
- Transfer learning (use weights from previous stages)
- Multi-objective optimization (Pareto fronts)
```

---

## Appendix A: Map Editor Specifications

### Trigger System Requirements

Each educational map needs:

1. **Start Conditions**
   - Player units/buildings placement
   - Resource locations
   - Enemy placement (if any)

2. **Victory Triggers**
   - Resource threshold checks
   - Building completion checks
   - Unit count checks
   - Location reached checks

3. **Failure Triggers**
   - Time limit exceeded
   - All units lost
   - Critical building destroyed

### Example Map Script (Pseudocode)

```
MAP: Gather01_CollectWood

SETUP:
    player.add_unit(PEASANT, x=10, y=10)
    place_resource(TREES, x=15, y=10, amount=500)
    set_time_limit(180)  // 3 minutes

VICTORY_CONDITIONS:
    player.wood >= 100

FAILURE_CONDITIONS:
    time_elapsed > time_limit
    player.units.count == 0

ON_VICTORY:
    show_message("Wood gathering complete!")
    end_game(VICTORY)

ON_FAILURE:
    show_message("Time's up!")
    end_game(DEFEAT)
```

---

## Appendix B: Genome Serialization

### Binary Format (Stage 1)

```
Header (16 bytes):
    - Magic: "TZAR" (4 bytes)
    - Version: uint32 (4 bytes)
    - GenomeId: uint64 (8 bytes)

Metadata (variable):
    - Generation: int32
    - Fitness: float32
    - Parent1Id: uint64 (0 if none)
    - Parent2Id: uint64 (0 if none)
    - CreatedAt: int64 (Unix timestamp)

Weights:
    - Count: int32
    - Data: float32[] (Count * 4 bytes)

Total size: ~7.2 MB per genome (1.81M * 4 bytes)
```

### Compression

Use LZ4 compression for storage:
- Expected compression ratio: ~2-3x
- Compressed size: ~2.4-3.6 MB per genome

---

## Appendix C: Monitoring Metrics

### Per-Generation Metrics

| Metric | Description |
|--------|-------------|
| best_fitness | Highest fitness in generation |
| avg_fitness | Mean fitness across population |
| fitness_std | Standard deviation of fitness |
| win_rate | Percentage of games won |
| avg_game_length | Mean game duration |
| unique_strategies | Behavioral diversity measure |

### Per-Genome Metrics

| Metric | Description |
|--------|-------------|
| games_played | Total games this genome played |
| wins / losses / draws | Game outcomes |
| avg_resources | Mean resources gathered |
| avg_units_created | Mean units produced |
| avg_buildings | Mean buildings constructed |
| action_distribution | Frequency of each action type |

### Dashboard Visualizations

1. **Fitness Over Time** - Line chart of best/avg fitness per generation
2. **Win Rate Progression** - Area chart showing win rate improvement
3. **Population Diversity** - Heatmap of genome similarities
4. **Action Distribution** - Pie chart of action frequencies
5. **Game Length Histogram** - Distribution of game durations

---

*Document Version: 1.0*
*Created: 2024-12-09*
*Last Updated: 2024-12-09*
