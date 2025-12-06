# Phase 6: Training Pipeline - Detailed Plan

## Overview

The Training Pipeline orchestrates the entire training process, managing generations, curriculum learning, checkpointing, and tournament play. It integrates all previous phases into a cohesive training system.

## Task Dependency Diagram

```
F6.T1 (Training Loop)
   │
   ├──────────────┬──────────────┐
   │              │              │
   ▼              ▼              ▼
F6.T2          F6.T3          F6.T4
(Curriculum)   (Checkpoint)   (Tournament)
   │              │              │
   └──────────────┼──────────────┘
                  │
                  ▼
               F6.T5
            (Dashboard)
                  │
                  ▼
               F6.T6
           (Integration)
```

## Definition of Done - Phase 6

- [ ] All 6 tasks completed with passing tests
- [ ] Training loop runs continuously
- [ ] Curriculum advances based on performance
- [ ] Checkpoints save/restore correctly
- [ ] Tournament ranking works
- [ ] Dashboard shows real-time progress
- [ ] Demo: 24h continuous training without intervention
- [ ] Git tag: `phase-6-complete`

---

## Task Definitions

### F6.T1: Training Loop Core

```yaml
task_id: "F6.T1"
name: "Training Loop Core"
description: |
  Implement the main training loop that coordinates evolution,
  evaluation, and progression through generations.

inputs:
  - "src/TzarBot.GeneticAlgorithm/Core/GeneticAlgorithmEngine.cs"
  - "src/TzarBot.Orchestrator/Core/TrainingOrchestrator.cs"
  - "plans/1general_plan.md (section 6)"

outputs:
  - "src/TzarBot.Training/TzarBot.Training.csproj"
  - "src/TzarBot.Training/Core/ITrainingPipeline.cs"
  - "src/TzarBot.Training/Core/TrainingPipeline.cs"
  - "src/TzarBot.Training/Core/TrainingConfig.cs"
  - "src/TzarBot.Training/Core/TrainingState.cs"
  - "tests/TzarBot.Tests/Phase6/TrainingPipelineTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase6.TrainingPipeline\""

test_criteria: |
  - Training loop starts and runs
  - Generations progress correctly
  - Fitness is calculated and assigned
  - GA evolves population
  - State is tracked accurately
  - Graceful shutdown works

dependencies: ["F3.T5", "F4.T6", "F5.T3"]
estimated_complexity: "L"

claude_prompt: |
  Implement the core training loop that coordinates all components.

  ## Context
  Create new project `src/TzarBot.Training/`. This is the top-level orchestration.

  ## Requirements

  1. Create configuration:
     ```csharp
     public class TrainingConfig
     {
         // Population
         public int PopulationSize { get; set; } = 100;
         public int InitialGeneration { get; set; } = 0;

         // Evaluation
         public int GamesPerGenome { get; set; } = 3;
         public TimeSpan GameTimeout { get; set; } = TimeSpan.FromMinutes(30);

         // Infrastructure
         public int MaxParallelVMs { get; set; } = 8;
         public bool AutoStartVMs { get; set; } = true;

         // Checkpointing
         public int CheckpointInterval { get; set; } = 10; // generations
         public string CheckpointPath { get; set; } = "./checkpoints";

         // Stopping criteria
         public int? MaxGenerations { get; set; } = null;
         public float? TargetFitness { get; set; } = null;
         public float? TargetWinRate { get; set; } = 0.9f; // 90% vs Hard AI
     }
     ```

  2. Create `TrainingState`:
     ```csharp
     public class TrainingState
     {
         public int CurrentGeneration { get; set; }
         public string CurrentStage { get; set; }
         public Population Population { get; set; }
         public NetworkGenome BestGenome { get; set; }
         public List<GenerationStats> History { get; set; }
         public DateTime StartedAt { get; set; }
         public DateTime? LastCheckpoint { get; set; }
         public TrainingStatus Status { get; set; }
     }

     public enum TrainingStatus
     {
         NotStarted,
         Initializing,
         Running,
         Paused,
         Completed,
         Failed
     }
     ```

  3. Create interface:
     ```csharp
     public interface ITrainingPipeline
     {
         TrainingState State { get; }
         TrainingConfig Config { get; }

         Task InitializeAsync(CancellationToken ct);
         Task RunAsync(CancellationToken ct);
         Task PauseAsync();
         Task ResumeAsync();
         Task<bool> SaveCheckpointAsync();
         Task<bool> LoadCheckpointAsync(string path);

         event Action<GenerationStats>? OnGenerationComplete;
         event Action<string>? OnStageAdvanced;
         event Action<NetworkGenome>? OnNewBestGenome;
         event Action<string>? OnError;
     }
     ```

  4. Implement `TrainingPipeline`:
     ```csharp
     public class TrainingPipeline : ITrainingPipeline
     {
         private readonly IGeneticAlgorithm _ga;
         private readonly IOrchestrator _orchestrator;
         private readonly IFitnessCalculator _fitnessCalc;
         private readonly ICurriculumManager _curriculum;
         private readonly ICheckpointManager _checkpoint;
         private readonly TrainingConfig _config;

         private TrainingState _state;
         private bool _isPaused;

         public async Task InitializeAsync(CancellationToken ct)
         {
             _state = new TrainingState
             {
                 StartedAt = DateTime.UtcNow,
                 Status = TrainingStatus.Initializing,
                 History = new List<GenerationStats>()
             };

             // Try to load from checkpoint
             if (await _checkpoint.HasCheckpointAsync())
             {
                 var loaded = await _checkpoint.LoadLatestAsync();
                 if (loaded != null)
                 {
                     _state = loaded;
                     _ga.LoadPopulation(loaded.Population);
                     Console.WriteLine($"Resumed from generation {_state.CurrentGeneration}");
                 }
             }
             else
             {
                 // Initialize fresh population
                 _ga.Initialize(
                     _config.PopulationSize,
                     inputWidth: 240,
                     inputHeight: 135);
                 _state.Population = _ga.CurrentPopulation;
             }

             // Start orchestrator
             await _orchestrator.InitializeAsync();

             _state.Status = TrainingStatus.Running;
         }

         public async Task RunAsync(CancellationToken ct)
         {
             while (!ct.IsCancellationRequested && !ShouldStop())
             {
                 if (_isPaused)
                 {
                     await Task.Delay(1000, ct);
                     continue;
                 }

                 await RunGenerationAsync(ct);

                 // Checkpoint if needed
                 if (_state.CurrentGeneration % _config.CheckpointInterval == 0)
                 {
                     await SaveCheckpointAsync();
                 }
             }

             _state.Status = TrainingStatus.Completed;
         }

         private async Task RunGenerationAsync(CancellationToken ct)
         {
             var generationStart = DateTime.UtcNow;

             // Get current curriculum stage
             var stage = _curriculum.GetCurrentStage(_state);
             _state.CurrentStage = stage.Name;

             // Run evaluation
             var population = _ga.CurrentPopulation.Individuals;
             var result = await _orchestrator.RunGenerationAsync(population, ct);

             // Calculate and assign fitness
             foreach (var workItem in result.WorkItems.Where(w => w.Result != null))
             {
                 var fitness = _fitnessCalc.Calculate(workItem.Result);
                 _ga.SetFitness(workItem.Genome.Id, fitness);
             }

             // Track statistics
             var stats = new GenerationStats
             {
                 Generation = _state.CurrentGeneration,
                 Stage = stage.Name,
                 BestFitness = _ga.BestGenome.Fitness,
                 AverageFitness = _ga.CurrentPopulation.AverageFitness,
                 WinRate = CalculateWinRate(result),
                 Duration = DateTime.UtcNow - generationStart,
                 GamesPlayed = result.TotalGamesPlayed
             };

             _state.History.Add(stats);
             OnGenerationComplete?.Invoke(stats);

             // Check for new best
             if (_state.BestGenome == null ||
                 _ga.BestGenome.Fitness > _state.BestGenome.Fitness)
             {
                 _state.BestGenome = _ga.BestGenome.Clone();
                 OnNewBestGenome?.Invoke(_state.BestGenome);
             }

             // Check curriculum advancement
             if (_curriculum.ShouldAdvance(_state, stage))
             {
                 _curriculum.AdvanceStage();
                 OnStageAdvanced?.Invoke(_curriculum.CurrentStage.Name);
             }

             // Evolve to next generation
             _ga.EvolveNextGeneration();
             _state.CurrentGeneration++;
             _state.Population = _ga.CurrentPopulation;
         }

         private bool ShouldStop()
         {
             if (_config.MaxGenerations.HasValue &&
                 _state.CurrentGeneration >= _config.MaxGenerations)
                 return true;

             if (_config.TargetFitness.HasValue &&
                 _state.BestGenome?.Fitness >= _config.TargetFitness)
                 return true;

             return false;
         }
     }
     ```

  5. Create tests:
     - Test_Initialize_CreatesPopulation
     - Test_RunGeneration_AdvancesGeneration
     - Test_FitnessAssignment_Works
     - Test_BestGenome_IsTracked
     - Test_GracefulShutdown_SavesState

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase6.TrainingPipeline"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify generation progression"

on_failure: |
  If training fails:
  1. Check component integrations
  2. Verify orchestrator is working
  3. Check fitness calculation
  4. Add detailed logging
  5. Test each component in isolation
```

---

### F6.T2: Curriculum Manager

```yaml
task_id: "F6.T2"
name: "Curriculum Manager"
description: |
  Implement curriculum learning that progressively increases
  difficulty as the bot improves.

inputs:
  - "src/TzarBot.Training/Core/TrainingState.cs"
  - "plans/1general_plan.md (section 6.2)"

outputs:
  - "src/TzarBot.Training/Curriculum/ICurriculumManager.cs"
  - "src/TzarBot.Training/Curriculum/CurriculumManager.cs"
  - "src/TzarBot.Training/Curriculum/CurriculumStage.cs"
  - "src/TzarBot.Training/Curriculum/StageDefinitions.cs"
  - "tests/TzarBot.Tests/Phase6/CurriculumTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase6.Curriculum\""

test_criteria: |
  - Stages are defined correctly
  - Stage advancement works based on criteria
  - Fitness function changes per stage
  - Opponent difficulty increases
  - Stages can be reverted if performance drops

dependencies: ["F6.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement curriculum learning for progressive difficulty.

  ## Context
  Project: `src/TzarBot.Training/`. Define stages of training.

  ## Requirements

  1. Create `CurriculumStage`:
     ```csharp
     public class CurriculumStage
     {
         public string Name { get; set; }
         public int StageNumber { get; set; }
         public OpponentType Opponent { get; set; }
         public OpponentDifficulty Difficulty { get; set; }
         public int GamesPerGenome { get; set; }
         public FitnessMode FitnessMode { get; set; }
         public Func<TrainingState, bool> PromotionCriteria { get; set; }
         public Func<TrainingState, bool>? DemotionCriteria { get; set; }
         public string Description { get; set; }
     }

     public enum OpponentType
     {
         None,
         PassiveAI,
         GameAI,
         SelfPlay
     }

     public enum OpponentDifficulty
     {
         None,
         Easy,
         Normal,
         Hard,
         Insane
     }

     public enum FitnessMode
     {
         Survival,   // Focus on staying alive
         Economy,    // Focus on building
         Combat,     // Focus on winning
         Efficiency  // Focus on winning quickly
     }
     ```

  2. Create stage definitions:
     ```csharp
     public static class StageDefinitions
     {
         public static CurriculumStage Bootstrap = new()
         {
             Name = "Bootstrap",
             StageNumber = 0,
             Opponent = OpponentType.PassiveAI,
             Difficulty = OpponentDifficulty.None,
             GamesPerGenome = 3,
             FitnessMode = FitnessMode.Survival,
             PromotionCriteria = state =>
                 state.History.TakeLast(5).Average(h => h.AverageSurvivalTime) >
                 TimeSpan.FromMinutes(2).TotalSeconds,
             Description = "Learn basic game interaction"
         };

         public static CurriculumStage Basic = new()
         {
             Name = "Basic",
             StageNumber = 1,
             Opponent = OpponentType.GameAI,
             Difficulty = OpponentDifficulty.Easy,
             GamesPerGenome = 5,
             FitnessMode = FitnessMode.Economy,
             PromotionCriteria = state =>
                 state.History.TakeLast(10).Average(h => h.AverageBuildingCount) >= 5,
             DemotionCriteria = state =>
                 state.History.TakeLast(20).Average(h => h.AverageSurvivalTime) <
                 TimeSpan.FromMinutes(1).TotalSeconds,
             Description = "Learn economy and building"
         };

         public static CurriculumStage CombatEasy = new()
         {
             Name = "Combat-Easy",
             StageNumber = 2,
             Opponent = OpponentType.GameAI,
             Difficulty = OpponentDifficulty.Easy,
             GamesPerGenome = 10,
             FitnessMode = FitnessMode.Combat,
             PromotionCriteria = state =>
                 state.History.TakeLast(20).Average(h => h.WinRate) >= 0.5f,
             Description = "Learn to win against Easy AI"
         };

         public static CurriculumStage CombatNormal = new()
         {
             Name = "Combat-Normal",
             StageNumber = 3,
             Opponent = OpponentType.GameAI,
             Difficulty = OpponentDifficulty.Normal,
             GamesPerGenome = 10,
             FitnessMode = FitnessMode.Combat,
             PromotionCriteria = state =>
                 state.History.TakeLast(20).Average(h => h.WinRate) >= 0.5f,
             Description = "Learn to win against Normal AI"
         };

         public static CurriculumStage CombatHard = new()
         {
             Name = "Combat-Hard",
             StageNumber = 4,
             Opponent = OpponentType.GameAI,
             Difficulty = OpponentDifficulty.Hard,
             GamesPerGenome = 10,
             FitnessMode = FitnessMode.Efficiency,
             PromotionCriteria = state =>
                 state.History.TakeLast(20).Average(h => h.WinRate) >= 0.5f,
             Description = "Master Hard AI"
         };

         public static CurriculumStage Tournament = new()
         {
             Name = "Tournament",
             StageNumber = 5,
             Opponent = OpponentType.SelfPlay,
             Difficulty = OpponentDifficulty.None,
             GamesPerGenome = 20,
             FitnessMode = FitnessMode.Combat,
             PromotionCriteria = _ => false, // Never advance, infinite self-play
             Description = "Continuous improvement through self-play"
         };

         public static List<CurriculumStage> AllStages => new()
         {
             Bootstrap, Basic, CombatEasy, CombatNormal, CombatHard, Tournament
         };
     }
     ```

  3. Create interface:
     ```csharp
     public interface ICurriculumManager
     {
         CurriculumStage CurrentStage { get; }
         int CurrentStageNumber { get; }

         CurriculumStage GetCurrentStage(TrainingState state);
         bool ShouldAdvance(TrainingState state, CurriculumStage stage);
         bool ShouldDemote(TrainingState state, CurriculumStage stage);
         void AdvanceStage();
         void DemoteStage();
         void SetStage(int stageNumber);

         FitnessConfig GetFitnessConfig(CurriculumStage stage);
     }
     ```

  4. Implement `CurriculumManager`:
     ```csharp
     public class CurriculumManager : ICurriculumManager
     {
         private readonly List<CurriculumStage> _stages;
         private int _currentStageIndex = 0;

         public CurriculumManager()
         {
             _stages = StageDefinitions.AllStages;
         }

         public CurriculumStage CurrentStage => _stages[_currentStageIndex];

         public bool ShouldAdvance(TrainingState state, CurriculumStage stage)
         {
             if (_currentStageIndex >= _stages.Count - 1)
                 return false;

             return stage.PromotionCriteria(state);
         }

         public bool ShouldDemote(TrainingState state, CurriculumStage stage)
         {
             if (_currentStageIndex == 0)
                 return false;

             return stage.DemotionCriteria?.Invoke(state) ?? false;
         }

         public FitnessConfig GetFitnessConfig(CurriculumStage stage)
         {
             return stage.FitnessMode switch
             {
                 FitnessMode.Survival => new FitnessConfig
                 {
                     WinBonus = 100,
                     SurvivalPerMinute = 200,
                     UnitsBuiltWeight = 1,
                     IdlePenaltyPerSecond = 5
                 },
                 FitnessMode.Economy => new FitnessConfig
                 {
                     WinBonus = 500,
                     SurvivalPerMinute = 50,
                     BuildingsBuiltWeight = 50,
                     UnitsBuiltWeight = 10
                 },
                 FitnessMode.Combat => new FitnessConfig
                 {
                     WinBonus = 1000,
                     FastWinMultiplier = 500,
                     EnemyKilledWeight = 20,
                     BuildingsDestroyedWeight = 50
                 },
                 FitnessMode.Efficiency => new FitnessConfig
                 {
                     WinBonus = 1000,
                     FastWinMultiplier = 1000,
                     EnemyKilledWeight = 10
                 },
                 _ => new FitnessConfig()
             };
         }
     }
     ```

  5. Create tests:
     - Test_StageProgression_BootstrapToBasic
     - Test_ShouldAdvance_WhenCriteriaMet
     - Test_ShouldDemote_WhenPerformanceDrops
     - Test_FitnessConfig_ChangesPerStage

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase6.Curriculum"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify stage transitions work"

on_failure: |
  If curriculum fails:
  1. Simplify promotion criteria
  2. Add more logging to track metrics
  3. Test criteria independently
  4. Check history is being recorded
```

---

### F6.T3: Checkpoint Manager

```yaml
task_id: "F6.T3"
name: "Checkpoint Manager"
description: |
  Implement checkpoint saving and loading for training recovery
  and state persistence.

inputs:
  - "src/TzarBot.Training/Core/TrainingState.cs"
  - "src/TzarBot.GeneticAlgorithm/Persistence/SqliteGenomeRepository.cs"

outputs:
  - "src/TzarBot.Training/Checkpoint/ICheckpointManager.cs"
  - "src/TzarBot.Training/Checkpoint/CheckpointManager.cs"
  - "src/TzarBot.Training/Checkpoint/Checkpoint.cs"
  - "tests/TzarBot.Tests/Phase6/CheckpointTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase6.Checkpoint\""

test_criteria: |
  - Checkpoint saves all training state
  - Checkpoint loads correctly
  - Training resumes from checkpoint
  - Old checkpoints are cleaned up
  - Best genome is always saved

dependencies: ["F6.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement checkpoint saving and loading for training persistence.

  ## Context
  Project: `src/TzarBot.Training/`. Enable recovery from failures.

  ## Requirements

  1. Create `Checkpoint`:
     ```csharp
     [MessagePackObject]
     public class Checkpoint
     {
         [Key(0)] public int Generation { get; set; }
         [Key(1)] public string StageName { get; set; }
         [Key(2)] public int StageNumber { get; set; }
         [Key(3)] public List<byte[]> PopulationData { get; set; }
         [Key(4)] public byte[] BestGenomeData { get; set; }
         [Key(5)] public List<GenerationStats> History { get; set; }
         [Key(6)] public DateTime CreatedAt { get; set; }
         [Key(7)] public DateTime TrainingStartedAt { get; set; }
         [Key(8)] public string Version { get; set; }
         [Key(9)] public Dictionary<string, object> Metadata { get; set; }
     }
     ```

  2. Create interface:
     ```csharp
     public interface ICheckpointManager
     {
         Task<bool> HasCheckpointAsync();
         Task<TrainingState?> LoadLatestAsync();
         Task<TrainingState?> LoadAsync(string path);
         Task SaveAsync(TrainingState state);
         Task<IEnumerable<CheckpointInfo>> ListCheckpointsAsync();
         Task CleanupOldCheckpointsAsync(int keepCount);
     }

     public class CheckpointInfo
     {
         public string Path { get; set; }
         public int Generation { get; set; }
         public DateTime CreatedAt { get; set; }
         public long SizeBytes { get; set; }
     }
     ```

  3. Implement `CheckpointManager`:
     ```csharp
     public class CheckpointManager : ICheckpointManager
     {
         private readonly string _checkpointDir;
         private readonly int _keepCount;

         public CheckpointManager(string checkpointDir, int keepCount = 10)
         {
             _checkpointDir = checkpointDir;
             _keepCount = keepCount;
             Directory.CreateDirectory(checkpointDir);
         }

         public async Task SaveAsync(TrainingState state)
         {
             var checkpoint = new Checkpoint
             {
                 Generation = state.CurrentGeneration,
                 StageName = state.CurrentStage,
                 PopulationData = state.Population.Individuals
                     .Select(g => GenomeSerializer.Serialize(g))
                     .ToList(),
                 BestGenomeData = GenomeSerializer.Serialize(state.BestGenome),
                 History = state.History,
                 CreatedAt = DateTime.UtcNow,
                 TrainingStartedAt = state.StartedAt,
                 Version = GetVersion()
             };

             var filename = $"checkpoint_gen{state.CurrentGeneration:D6}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";
             var path = Path.Combine(_checkpointDir, filename);

             var data = MessagePackSerializer.Serialize(checkpoint);
             await File.WriteAllBytesAsync(path, data);

             // Also save best genome separately for easy access
             var bestPath = Path.Combine(_checkpointDir, "best_genome.bin");
             await File.WriteAllBytesAsync(bestPath, checkpoint.BestGenomeData);

             // Cleanup old checkpoints
             await CleanupOldCheckpointsAsync(_keepCount);

             state.LastCheckpoint = DateTime.UtcNow;
         }

         public async Task<TrainingState?> LoadLatestAsync()
         {
             var files = Directory.GetFiles(_checkpointDir, "checkpoint_*.bin")
                 .OrderByDescending(f => f)
                 .ToList();

             if (files.Count == 0)
                 return null;

             return await LoadAsync(files[0]);
         }

         public async Task<TrainingState?> LoadAsync(string path)
         {
             if (!File.Exists(path))
                 return null;

             var data = await File.ReadAllBytesAsync(path);
             var checkpoint = MessagePackSerializer.Deserialize<Checkpoint>(data);

             var population = new Population();
             foreach (var genomeData in checkpoint.PopulationData)
             {
                 var genome = GenomeSerializer.Deserialize(genomeData);
                 population.Individuals.Add(genome);
             }

             return new TrainingState
             {
                 CurrentGeneration = checkpoint.Generation,
                 CurrentStage = checkpoint.StageName,
                 Population = population,
                 BestGenome = GenomeSerializer.Deserialize(checkpoint.BestGenomeData),
                 History = checkpoint.History,
                 StartedAt = checkpoint.TrainingStartedAt,
                 LastCheckpoint = checkpoint.CreatedAt,
                 Status = TrainingStatus.Running
             };
         }

         public async Task CleanupOldCheckpointsAsync(int keepCount)
         {
             var files = Directory.GetFiles(_checkpointDir, "checkpoint_*.bin")
                 .OrderByDescending(f => f)
                 .Skip(keepCount)
                 .ToList();

             foreach (var file in files)
             {
                 File.Delete(file);
             }

             await Task.CompletedTask;
         }

         public async Task<IEnumerable<CheckpointInfo>> ListCheckpointsAsync()
         {
             var files = Directory.GetFiles(_checkpointDir, "checkpoint_*.bin");

             return files.Select(f => new CheckpointInfo
             {
                 Path = f,
                 Generation = ExtractGeneration(f),
                 CreatedAt = File.GetCreationTimeUtc(f),
                 SizeBytes = new FileInfo(f).Length
             }).OrderByDescending(c => c.Generation);
         }
     }
     ```

  4. Create auto-save trigger:
     ```csharp
     public class AutoCheckpointer : IDisposable
     {
         private readonly Timer _timer;
         private readonly ICheckpointManager _manager;
         private readonly Func<TrainingState> _stateProvider;

         public AutoCheckpointer(
             ICheckpointManager manager,
             Func<TrainingState> stateProvider,
             TimeSpan interval)
         {
             _manager = manager;
             _stateProvider = stateProvider;
             _timer = new Timer(async _ =>
             {
                 var state = _stateProvider();
                 if (state != null)
                 {
                     await _manager.SaveAsync(state);
                 }
             }, null, interval, interval);
         }

         public void Dispose()
         {
             _timer.Dispose();
         }
     }
     ```

  5. Create tests:
     - Test_Save_CreatesFile
     - Test_Load_RestoresState
     - Test_RoundTrip_PreservesData
     - Test_Cleanup_KeepsOnlyRecent
     - Test_BestGenome_AlwaysSaved

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase6.Checkpoint"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify checkpoint files are created"
  - "Test resume from checkpoint"

on_failure: |
  If checkpointing fails:
  1. Check serialization works for all types
  2. Verify disk space
  3. Check file permissions
  4. Test with smaller population
```

---

### F6.T4: Tournament System

```yaml
task_id: "F6.T4"
name: "Tournament System"
description: |
  Implement tournament-based evaluation for self-play,
  including ELO rating system for ranking genomes.

inputs:
  - "src/TzarBot.Training/Core/TrainingPipeline.cs"
  - "plans/1general_plan.md (section 6.3)"

outputs:
  - "src/TzarBot.Training/Tournament/ITournamentManager.cs"
  - "src/TzarBot.Training/Tournament/TournamentManager.cs"
  - "src/TzarBot.Training/Tournament/EloCalculator.cs"
  - "src/TzarBot.Training/Tournament/MatchResult.cs"
  - "tests/TzarBot.Tests/Phase6/TournamentTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase6.Tournament\""

test_criteria: |
  - Tournament runs between genomes
  - ELO ratings are calculated correctly
  - Rankings are updated after matches
  - Swiss-system pairing works
  - Results are recorded properly

dependencies: ["F6.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement tournament system for self-play evaluation.

  ## Context
  Project: `src/TzarBot.Training/`. Enable self-play competition.

  ## Requirements

  1. Create `MatchResult`:
     ```csharp
     public class MatchResult
     {
         public Guid Genome1Id { get; set; }
         public Guid Genome2Id { get; set; }
         public Guid? WinnerId { get; set; }  // null = draw
         public TimeSpan Duration { get; set; }
         public MatchOutcome Outcome { get; set; }
         public DateTime PlayedAt { get; set; }
     }

     public enum MatchOutcome
     {
         Genome1Win,
         Genome2Win,
         Draw,
         Error
     }
     ```

  2. Create `EloCalculator`:
     ```csharp
     public class EloCalculator
     {
         private readonly float _kFactor;

         public EloCalculator(float kFactor = 32f)
         {
             _kFactor = kFactor;
         }

         public (float newRating1, float newRating2) Calculate(
             float rating1, float rating2, MatchOutcome outcome)
         {
             float expected1 = ExpectedScore(rating1, rating2);
             float expected2 = 1 - expected1;

             float score1, score2;
             switch (outcome)
             {
                 case MatchOutcome.Genome1Win:
                     score1 = 1; score2 = 0;
                     break;
                 case MatchOutcome.Genome2Win:
                     score1 = 0; score2 = 1;
                     break;
                 default:
                     score1 = 0.5f; score2 = 0.5f;
                     break;
             }

             float new1 = rating1 + _kFactor * (score1 - expected1);
             float new2 = rating2 + _kFactor * (score2 - expected2);

             return (new1, new2);
         }

         private float ExpectedScore(float rating1, float rating2)
         {
             return 1f / (1f + MathF.Pow(10f, (rating2 - rating1) / 400f));
         }
     }
     ```

  3. Create interface:
     ```csharp
     public interface ITournamentManager
     {
         Task<TournamentResult> RunTournamentAsync(
             IEnumerable<NetworkGenome> participants,
             CancellationToken ct);

         IEnumerable<(Guid GenomeId, float Rating)> GetRankings();
         float GetElo(Guid genomeId);
     }

     public class TournamentResult
     {
         public List<MatchResult> Matches { get; set; }
         public Dictionary<Guid, float> FinalRatings { get; set; }
         public Guid WinnerId { get; set; }
         public TimeSpan Duration { get; set; }
     }
     ```

  4. Implement `TournamentManager`:
     ```csharp
     public class TournamentManager : ITournamentManager
     {
         private readonly IOrchestrator _orchestrator;
         private readonly EloCalculator _elo;
         private readonly Dictionary<Guid, float> _ratings;

         public async Task<TournamentResult> RunTournamentAsync(
             IEnumerable<NetworkGenome> participants,
             CancellationToken ct)
         {
             var genomes = participants.ToList();
             var matches = new List<MatchResult>();

             // Initialize ratings
             foreach (var g in genomes)
             {
                 if (!_ratings.ContainsKey(g.Id))
                 {
                     _ratings[g.Id] = g.EloRating ?? 1200f;
                 }
             }

             // Swiss-system tournament
             int rounds = CalculateRounds(genomes.Count);

             for (int round = 0; round < rounds; round++)
             {
                 var pairings = GenerateSwissPairings(genomes, _ratings, round);

                 // Run all matches in parallel
                 var roundMatches = await RunMatchesAsync(pairings, ct);
                 matches.AddRange(roundMatches);

                 // Update ELO ratings
                 foreach (var match in roundMatches)
                 {
                     var (new1, new2) = _elo.Calculate(
                         _ratings[match.Genome1Id],
                         _ratings[match.Genome2Id],
                         match.Outcome);

                     _ratings[match.Genome1Id] = new1;
                     _ratings[match.Genome2Id] = new2;
                 }
             }

             return new TournamentResult
             {
                 Matches = matches,
                 FinalRatings = new Dictionary<Guid, float>(_ratings),
                 WinnerId = _ratings.OrderByDescending(r => r.Value).First().Key
             };
         }

         private List<(NetworkGenome, NetworkGenome)> GenerateSwissPairings(
             List<NetworkGenome> genomes,
             Dictionary<Guid, float> ratings,
             int round)
         {
             // Sort by current rating
             var sorted = genomes
                 .OrderByDescending(g => ratings.GetValueOrDefault(g.Id, 1200f))
                 .ToList();

             var pairings = new List<(NetworkGenome, NetworkGenome)>();

             // Pair adjacent players (Swiss style)
             for (int i = 0; i < sorted.Count - 1; i += 2)
             {
                 pairings.Add((sorted[i], sorted[i + 1]));
             }

             // Handle odd number - bye for lowest rated
             return pairings;
         }

         private async Task<List<MatchResult>> RunMatchesAsync(
             List<(NetworkGenome g1, NetworkGenome g2)> pairings,
             CancellationToken ct)
         {
             var tasks = pairings.Select(p => RunMatchAsync(p.g1, p.g2, ct));
             var results = await Task.WhenAll(tasks);
             return results.ToList();
         }

         private async Task<MatchResult> RunMatchAsync(
             NetworkGenome genome1,
             NetworkGenome genome2,
             CancellationToken ct)
         {
             // This requires special handling - two bots playing each other
             // For now, simulate by playing each against AI and comparing

             // ... implementation depends on game support for PvP
         }
     }
     ```

  5. Create tests:
     - Test_EloCalculation_WinnerGainsPoints
     - Test_EloCalculation_LoserLosesPoints
     - Test_SwissPairing_GroupsBySimilarRating
     - Test_Tournament_UpdatesAllRatings
     - Test_Rankings_AreCorrect

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase6.Tournament"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify ELO calculations are correct"

on_failure: |
  If tournament fails:
  1. Test ELO calculator independently
  2. Simplify pairing algorithm
  3. Mock match execution for testing
  4. Verify rankings update correctly
```

---

### F6.T5: Blazor Dashboard

```yaml
task_id: "F6.T5"
name: "Blazor Dashboard"
description: |
  Create a web dashboard for monitoring training progress,
  visualizing statistics, and controlling the training process.

inputs:
  - "src/TzarBot.Training/Core/TrainingPipeline.cs"
  - "All training state and statistics classes"

outputs:
  - "src/TzarBot.Dashboard/TzarBot.Dashboard.csproj"
  - "src/TzarBot.Dashboard/Program.cs"
  - "src/TzarBot.Dashboard/Pages/Index.razor"
  - "src/TzarBot.Dashboard/Pages/Generations.razor"
  - "src/TzarBot.Dashboard/Pages/Population.razor"
  - "src/TzarBot.Dashboard/Services/TrainingStateService.cs"
  - "src/TzarBot.Dashboard/Hubs/TrainingHub.cs"

test_command: "dotnet build src/TzarBot.Dashboard"

test_criteria: |
  - Dashboard builds successfully
  - Real-time updates via SignalR work
  - Charts display correctly
  - Training can be paused/resumed
  - Current state is displayed

dependencies: ["F6.T1"]
estimated_complexity: "L"

claude_prompt: |
  Create a Blazor Server dashboard for training monitoring.

  ## Context
  Create new project `src/TzarBot.Dashboard/`. Use Blazor Server with SignalR.

  ## Requirements

  1. Create project:
     ```bash
     dotnet new blazorserver -n TzarBot.Dashboard
     ```
     Add references to TzarBot.Training and common libraries.

  2. Create SignalR Hub:
     ```csharp
     public class TrainingHub : Hub
     {
         public async Task JoinMonitoring()
         {
             await Groups.AddToGroupAsync(Context.ConnectionId, "Monitors");
         }

         public async Task LeaveMonitoring()
         {
             await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Monitors");
         }
     }
     ```

  3. Create state service:
     ```csharp
     public class TrainingStateService
     {
         private readonly ITrainingPipeline _pipeline;
         private readonly IHubContext<TrainingHub> _hub;

         public TrainingState CurrentState => _pipeline.State;

         public TrainingStateService(
             ITrainingPipeline pipeline,
             IHubContext<TrainingHub> hub)
         {
             _pipeline = pipeline;
             _hub = hub;

             _pipeline.OnGenerationComplete += async stats =>
             {
                 await _hub.Clients.Group("Monitors")
                     .SendAsync("GenerationComplete", stats);
             };

             _pipeline.OnNewBestGenome += async genome =>
             {
                 await _hub.Clients.Group("Monitors")
                     .SendAsync("NewBestGenome", genome.Id, genome.Fitness);
             };
         }

         public async Task PauseAsync() => await _pipeline.PauseAsync();
         public async Task ResumeAsync() => await _pipeline.ResumeAsync();
     }
     ```

  4. Create Index page:
     ```razor
     @page "/"
     @inject TrainingStateService StateService
     @implements IAsyncDisposable

     <PageTitle>TzarBot Training Dashboard</PageTitle>

     <div class="dashboard">
         <div class="status-panel">
             <h2>Training Status</h2>
             <div class="status @GetStatusClass()">
                 @StateService.CurrentState.Status
             </div>

             <div class="stats-grid">
                 <div class="stat">
                     <label>Generation</label>
                     <span>@_currentGeneration</span>
                 </div>
                 <div class="stat">
                     <label>Stage</label>
                     <span>@StateService.CurrentState.CurrentStage</span>
                 </div>
                 <div class="stat">
                     <label>Best Fitness</label>
                     <span>@_bestFitness.ToString("F2")</span>
                 </div>
                 <div class="stat">
                     <label>Population</label>
                     <span>@StateService.CurrentState.Population?.Individuals.Count</span>
                 </div>
             </div>

             <div class="controls">
                 <button @onclick="Pause" disabled="@(!IsRunning)">Pause</button>
                 <button @onclick="Resume" disabled="@(IsRunning)">Resume</button>
                 <button @onclick="SaveCheckpoint">Save Checkpoint</button>
             </div>
         </div>

         <div class="chart-panel">
             <h2>Fitness Over Generations</h2>
             <canvas id="fitnessChart"></canvas>
         </div>

         <div class="recent-panel">
             <h2>Recent Games</h2>
             <table>
                 <thead>
                     <tr>
                         <th>Genome</th>
                         <th>Result</th>
                         <th>Duration</th>
                         <th>Fitness</th>
                     </tr>
                 </thead>
                 <tbody>
                     @foreach (var game in _recentGames)
                     {
                         <tr>
                             <td>@game.GenomeId.ToString()[..8]</td>
                             <td class="@GetResultClass(game.Outcome)">@game.Outcome</td>
                             <td>@game.Duration.ToString(@"mm\:ss")</td>
                             <td>@game.Fitness.ToString("F0")</td>
                         </tr>
                     }
                 </tbody>
             </table>
         </div>
     </div>

     @code {
         private HubConnection? _hubConnection;
         private int _currentGeneration;
         private float _bestFitness;
         private List<GameSummary> _recentGames = new();

         protected override async Task OnInitializedAsync()
         {
             _hubConnection = new HubConnectionBuilder()
                 .WithUrl(NavigationManager.ToAbsoluteUri("/traininghub"))
                 .Build();

             _hubConnection.On<GenerationStats>("GenerationComplete", stats =>
             {
                 _currentGeneration = stats.Generation;
                 _bestFitness = stats.BestFitness;
                 InvokeAsync(StateHasChanged);
             });

             await _hubConnection.StartAsync();
             await _hubConnection.SendAsync("JoinMonitoring");
         }

         private async Task Pause() => await StateService.PauseAsync();
         private async Task Resume() => await StateService.ResumeAsync();

         public async ValueTask DisposeAsync()
         {
             if (_hubConnection != null)
             {
                 await _hubConnection.DisposeAsync();
             }
         }
     }
     ```

  5. Add Chart.js integration:
     ```html
     <!-- wwwroot/index.html -->
     <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
     ```

     ```javascript
     // wwwroot/js/charts.js
     window.initFitnessChart = (data) => {
         const ctx = document.getElementById('fitnessChart');
         new Chart(ctx, {
             type: 'line',
             data: {
                 labels: data.generations,
                 datasets: [{
                     label: 'Best Fitness',
                     data: data.bestFitness,
                     borderColor: 'rgb(75, 192, 192)',
                 }, {
                     label: 'Average Fitness',
                     data: data.avgFitness,
                     borderColor: 'rgb(255, 99, 132)',
                 }]
             }
         });
     };
     ```

  6. Create additional pages:
     - Generations.razor: Detailed generation history
     - Population.razor: Current population viewer
     - Settings.razor: Training configuration

  After completion:
  1. Run: `dotnet build src/TzarBot.Dashboard`
  2. Run: `dotnet run --project src/TzarBot.Dashboard`

validation_steps:
  - "Dashboard builds successfully"
  - "Pages render without errors"
  - "SignalR connection works"
  - "Charts display data"

on_failure: |
  If dashboard fails:
  1. Check Blazor Server configuration
  2. Verify SignalR hub is registered
  3. Check JavaScript console for errors
  4. Test service injection
```

---

### F6.T6: Full Integration Test

```yaml
task_id: "F6.T6"
name: "Full Integration Test"
description: |
  Create comprehensive integration tests that verify the entire
  training pipeline works end-to-end for extended periods.

inputs:
  - "All Phase 6 components"
  - "All previous phase implementations"

outputs:
  - "tests/TzarBot.Tests/Phase6/IntegrationTests.cs"
  - "tests/TzarBot.Tests/Phase6/EnduranceTests.cs"
  - "src/TzarBot.Training.Demo/Program.cs"
  - "src/TzarBot.Training.Demo/TzarBot.Training.Demo.csproj"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase6.Integration\""

test_criteria: |
  - Full pipeline runs for 10 generations
  - Fitness improves over generations
  - Checkpoints are saved and loadable
  - Dashboard connects and updates
  - No memory leaks after 1 hour
  - Graceful shutdown works

dependencies: ["F6.T2", "F6.T3", "F6.T4", "F6.T5"]
estimated_complexity: "L"

claude_prompt: |
  Create comprehensive integration tests for the full training system.

  ## Context
  All components are complete. Verify system works end-to-end.

  ## Requirements

  1. Create demo application:
     ```csharp
     class Program
     {
         static async Task Main(string[] args)
         {
             var config = new TrainingConfig
             {
                 PopulationSize = 50,
                 MaxParallelVMs = 4,
                 GamesPerGenome = 2,
                 CheckpointInterval = 5
             };

             // Create components
             var vmManager = new HyperVManager();
             var orchestrator = new TrainingOrchestrator(vmManager, ...);
             var ga = new GeneticAlgorithmEngine(...);
             var curriculum = new CurriculumManager();
             var checkpoint = new CheckpointManager("./checkpoints");

             var pipeline = new TrainingPipeline(
                 orchestrator, ga, curriculum, checkpoint, config);

             // Subscribe to events
             pipeline.OnGenerationComplete += stats =>
             {
                 Console.WriteLine($"Gen {stats.Generation}: " +
                     $"Best={stats.BestFitness:F0}, " +
                     $"Avg={stats.AverageFitness:F0}");
             };

             pipeline.OnStageAdvanced += stage =>
             {
                 Console.WriteLine($"Advanced to stage: {stage}");
             };

             // Handle shutdown
             Console.CancelKeyPress += async (s, e) =>
             {
                 e.Cancel = true;
                 Console.WriteLine("Saving checkpoint...");
                 await pipeline.SaveCheckpointAsync();
                 Environment.Exit(0);
             };

             // Run training
             Console.WriteLine("Starting training...");
             await pipeline.InitializeAsync(CancellationToken.None);
             await pipeline.RunAsync(CancellationToken.None);
         }
     }
     ```

  2. Create integration tests:
     ```csharp
     public class Phase6IntegrationTests
     {
         [Fact]
         public async Task FullPipeline_Runs10Generations()
         {
             // Use mock orchestrator for testing
             var mockOrchestrator = new MockOrchestrator();
             var config = new TrainingConfig
             {
                 PopulationSize = 10,
                 MaxGenerations = 10
             };

             var pipeline = CreatePipeline(mockOrchestrator, config);

             await pipeline.InitializeAsync(CancellationToken.None);
             await pipeline.RunAsync(CancellationToken.None);

             Assert.Equal(10, pipeline.State.CurrentGeneration);
         }

         [Fact]
         public async Task Checkpoint_SaveAndRestore()
         {
             var pipeline1 = CreatePipeline();
             await pipeline1.InitializeAsync(CancellationToken.None);

             // Run 5 generations
             for (int i = 0; i < 5; i++)
             {
                 await pipeline1.RunGenerationAsync(CancellationToken.None);
             }

             await pipeline1.SaveCheckpointAsync();

             // Create new pipeline and restore
             var pipeline2 = CreatePipeline();
             await pipeline2.LoadCheckpointAsync("./checkpoints");

             Assert.Equal(5, pipeline2.State.CurrentGeneration);
             Assert.Equal(
                 pipeline1.State.BestGenome.Id,
                 pipeline2.State.BestGenome.Id);
         }

         [Fact]
         public async Task Curriculum_AdvancesOnSuccess()
         {
             var pipeline = CreatePipeline();
             await pipeline.InitializeAsync(CancellationToken.None);

             // Simulate successful training
             for (int i = 0; i < 50; i++)
             {
                 // Set high fitness to trigger advancement
                 foreach (var genome in pipeline.State.Population.Individuals)
                 {
                     genome.Fitness = 5000;
                 }
                 await pipeline.RunGenerationAsync(CancellationToken.None);
             }

             // Should have advanced past Bootstrap stage
             Assert.NotEqual("Bootstrap", pipeline.State.CurrentStage);
         }
     }
     ```

  3. Create endurance tests:
     ```csharp
     public class EnduranceTests
     {
         [Fact(Skip = "Long running test")]
         public async Task Pipeline_Runs1Hour_NoMemoryLeak()
         {
             var pipeline = CreatePipeline();
             await pipeline.InitializeAsync(CancellationToken.None);

             var initialMemory = GC.GetTotalMemory(true);
             var cts = new CancellationTokenSource(TimeSpan.FromHours(1));

             try
             {
                 await pipeline.RunAsync(cts.Token);
             }
             catch (OperationCanceledException) { }

             GC.Collect();
             var finalMemory = GC.GetTotalMemory(true);

             // Allow 100MB growth
             Assert.True(
                 finalMemory - initialMemory < 100 * 1024 * 1024,
                 $"Memory grew from {initialMemory / 1024 / 1024}MB " +
                 $"to {finalMemory / 1024 / 1024}MB");
         }

         [Fact(Skip = "Requires VMs")]
         public async Task Pipeline_24Hours_Stability()
         {
             // Full 24-hour stability test
             // Run with actual VMs
         }
     }
     ```

  4. Create performance benchmarks:
     ```csharp
     public class PerformanceBenchmarks
     {
         [Fact]
         public void Generation_CompletesIn_ReasonableTime()
         {
             // Benchmark generation time
         }

         [Fact]
         public void Checkpoint_SavesIn_Under30Seconds()
         {
             // Benchmark checkpoint save time
         }
     }
     ```

  After completion:
  1. Run: `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase6"`
  2. Run: `dotnet run --project src/TzarBot.Training.Demo`

validation_steps:
  - "All integration tests pass"
  - "Demo runs for 10+ generations"
  - "Checkpoint save/restore works"
  - "No memory leaks detected"
  - "Graceful shutdown saves state"

on_failure: |
  If integration fails:
  1. Test components individually
  2. Add extensive logging
  3. Check resource cleanup
  4. Profile for memory leaks
  5. Verify thread safety
```

---

## Rollback Plan

If Phase 6 implementation fails:

1. **Simple Training Loop**: No curriculum, just basic evolution
   - Single opponent type
   - No stage advancement

2. **No Dashboard**: Console-only monitoring
   - Less convenient but functional

3. **Manual Checkpoints**: Save on demand only
   - No automatic saving

---

## API Documentation

### TrainingPipeline API

```csharp
var pipeline = new TrainingPipeline(config);

pipeline.OnGenerationComplete += stats =>
{
    Console.WriteLine($"Generation {stats.Generation} complete");
};

await pipeline.InitializeAsync(ct);
await pipeline.RunAsync(ct);
```

### Checkpoint API

```csharp
var checkpoint = new CheckpointManager("./checkpoints");

// Save
await checkpoint.SaveAsync(trainingState);

// Load latest
var state = await checkpoint.LoadLatestAsync();

// List available
var checkpoints = await checkpoint.ListCheckpointsAsync();
```

### Dashboard API

```
http://localhost:5000/          - Main dashboard
http://localhost:5000/generations - Generation history
http://localhost:5000/population - Current population
```

---

*Phase 6 Detailed Plan - Version 1.0*
*See prompts/phase_6/ for individual task prompts*
