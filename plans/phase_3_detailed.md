# Phase 3: Genetic Algorithm - Detailed Plan

## Overview

The Genetic Algorithm evolves a population of neural networks through selection, crossover, and mutation. It manages the evolutionary process that improves bot performance over generations.

## Task Dependency Diagram

```
F3.T1 (GA Engine Core)
   │
   ├──────────────┬──────────────┐
   │              │              │
   ▼              ▼              ▼
F3.T2          F3.T3          F3.T4
(Mutation)     (Crossover)    (Selection)
   │              │              │
   └──────────────┼──────────────┘
                  │
                  ▼
               F3.T5
          (Fitness & Persistence)
```

## Definition of Done - Phase 3

- [ ] All 5 tasks completed with passing tests
- [ ] Population can be initialized and evolved
- [ ] Mutation operators work correctly
- [ ] Crossover produces valid offspring
- [ ] Selection favors higher fitness
- [ ] Population persists to database
- [ ] Demo: 50 generations of evolution (mock fitness)
- [ ] Git tag: `phase-3-complete`

---

## Task Definitions

### F3.T1: GA Engine Core

```yaml
task_id: "F3.T1"
name: "GA Engine Core"
description: |
  Implement the core genetic algorithm engine that manages
  population evolution through generations.

inputs:
  - "src/TzarBot.NeuralNetwork/Genome/NetworkGenome.cs"
  - "plans/1general_plan.md (section 3.1)"

outputs:
  - "src/TzarBot.GeneticAlgorithm/TzarBot.GeneticAlgorithm.csproj"
  - "src/TzarBot.GeneticAlgorithm/Core/IGeneticAlgorithm.cs"
  - "src/TzarBot.GeneticAlgorithm/Core/GeneticAlgorithmEngine.cs"
  - "src/TzarBot.GeneticAlgorithm/Core/Population.cs"
  - "src/TzarBot.GeneticAlgorithm/Core/GAConfig.cs"
  - "tests/TzarBot.Tests/Phase3/GAEngineTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase3.GAEngine\""

test_criteria: |
  - Population initializes with correct size
  - Generations increment correctly
  - Best genome is tracked
  - Evolution loop runs without error
  - Statistics are calculated correctly

dependencies: ["F2.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement the core Genetic Algorithm engine.

  ## Context
  Create new project `src/TzarBot.GeneticAlgorithm/`.

  ## Requirements

  1. Create project with dependencies:
     - Reference TzarBot.NeuralNetwork
     - Reference TzarBot.Common

  2. Create configuration:
     ```csharp
     public class GAConfig
     {
         public int PopulationSize { get; set; } = 100;
         public int EliteCount { get; set; } = 5;       // Top N preserved
         public float CrossoverRate { get; set; } = 0.7f;
         public float MutationRate { get; set; } = 0.2f;
         public float WeightMutationStrength { get; set; } = 0.1f;
         public float StructureMutationRate { get; set; } = 0.05f;
         public int TournamentSize { get; set; } = 3;
         public int MaxGenerations { get; set; } = 1000;
         public bool PreserveParents { get; set; } = false;
     }
     ```

  3. Create Population class:
     ```csharp
     public class Population
     {
         public int Generation { get; private set; }
         public List<NetworkGenome> Individuals { get; }
         public NetworkGenome BestGenome => Individuals.MaxBy(g => g.Fitness);
         public float AverageFitness => Individuals.Average(g => g.Fitness);
         public float MaxFitness => Individuals.Max(g => g.Fitness);
         public float MinFitness => Individuals.Min(g => g.Fitness);
         public float FitnessStdDev { get; }

         public void AdvanceGeneration();
         public void UpdateFitness(Guid genomeId, float fitness);
         public IEnumerable<NetworkGenome> GetElites(int count);
     }
     ```

  4. Create interface:
     ```csharp
     public interface IGeneticAlgorithm
     {
         Population CurrentPopulation { get; }
         int CurrentGeneration { get; }
         NetworkGenome BestGenome { get; }

         void Initialize(int inputWidth, int inputHeight);
         void EvolveNextGeneration();
         void SetFitness(Guid genomeId, float fitness);

         event Action<GenerationStats>? OnGenerationComplete;
     }
     ```

  5. Implement `GeneticAlgorithmEngine`:
     ```csharp
     public class GeneticAlgorithmEngine : IGeneticAlgorithm
     {
         private readonly GAConfig _config;
         private readonly ISelectionOperator _selection;
         private readonly ICrossoverOperator _crossover;
         private readonly IMutationOperator _mutation;

         public void Initialize(int inputWidth, int inputHeight)
         {
             // Create initial random population
             for (int i = 0; i < _config.PopulationSize; i++)
             {
                 var genome = GenomeFactory.CreateRandom(_rng, inputWidth, inputHeight);
                 _population.Add(genome);
             }
         }

         public void EvolveNextGeneration()
         {
             var newPopulation = new List<NetworkGenome>();

             // Elitism
             var elites = _population.GetElites(_config.EliteCount);
             newPopulation.AddRange(elites.Select(e => e.Clone()));

             // Fill rest of population
             while (newPopulation.Count < _config.PopulationSize)
             {
                 // Selection
                 var parent1 = _selection.Select(_population);
                 var parent2 = _selection.Select(_population);

                 // Crossover
                 NetworkGenome child;
                 if (_rng.NextFloat() < _config.CrossoverRate)
                 {
                     child = _crossover.Crossover(parent1, parent2);
                 }
                 else
                 {
                     child = parent1.Clone();
                 }

                 // Mutation
                 if (_rng.NextFloat() < _config.MutationRate)
                 {
                     _mutation.Mutate(child);
                 }

                 child.Id = Guid.NewGuid();
                 child.Generation = _population.Generation + 1;
                 newPopulation.Add(child);
             }

             _population.Individuals.Clear();
             _population.Individuals.AddRange(newPopulation);
             _population.AdvanceGeneration();

             OnGenerationComplete?.Invoke(CalculateStats());
         }
     }
     ```

  6. Create statistics class:
     ```csharp
     public class GenerationStats
     {
         public int Generation { get; set; }
         public float BestFitness { get; set; }
         public float AverageFitness { get; set; }
         public float StdDevFitness { get; set; }
         public float Diversity { get; set; }
         public int UniqueStructures { get; set; }
         public TimeSpan GenerationTime { get; set; }
     }
     ```

  7. Create tests:
     - Test_Initialize_CreatesPopulation
     - Test_EvolveGeneration_IncreasesCounter
     - Test_Elitism_PreservesBest
     - Test_SetFitness_UpdatesGenome
     - Test_Statistics_CalculatedCorrectly

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase3.GAEngine"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify population size is maintained"

on_failure: |
  If evolution fails:
  1. Check elite preservation logic
  2. Verify crossover produces valid genomes
  3. Ensure mutation doesn't corrupt genome
  4. Add bounds checking for weights
```

---

### F3.T2: Mutation Operators

```yaml
task_id: "F3.T2"
name: "Mutation Operators"
description: |
  Implement mutation operators for both weights and network structure.
  These introduce genetic variation into the population.

inputs:
  - "src/TzarBot.GeneticAlgorithm/TzarBot.GeneticAlgorithm.csproj"
  - "src/TzarBot.NeuralNetwork/Genome/NetworkGenome.cs"
  - "plans/1general_plan.md (section 3.2)"

outputs:
  - "src/TzarBot.GeneticAlgorithm/Operators/IMutationOperator.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/WeightMutator.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/StructureMutator.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/CompositeMutator.cs"
  - "tests/TzarBot.Tests/Phase3/MutationTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase3.Mutation\""

test_criteria: |
  - Weight mutation changes values
  - Gaussian noise has correct distribution
  - Structure mutation adds/removes layers
  - Mutated genome is still valid
  - Weights are clamped to valid range
  - Mutation respects probability

dependencies: ["F3.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement mutation operators for genetic algorithm.

  ## Context
  Project: `src/TzarBot.GeneticAlgorithm/`. Create mutation operators.

  ## Requirements

  1. Create interface:
     ```csharp
     public interface IMutationOperator
     {
         void Mutate(NetworkGenome genome);
         string Name { get; }
     }
     ```

  2. Implement `WeightMutator`:
     ```csharp
     public class WeightMutator : IMutationOperator
     {
         private readonly float _mutationRate;      // Per-weight probability
         private readonly float _mutationStrength;  // Gaussian std dev
         private readonly float _minWeight;
         private readonly float _maxWeight;

         public void Mutate(NetworkGenome genome)
         {
             for (int i = 0; i < genome.Weights.Length; i++)
             {
                 if (_rng.NextFloat() < _mutationRate)
                 {
                     // Gaussian mutation
                     float delta = (float)NextGaussian() * _mutationStrength;
                     genome.Weights[i] += delta;

                     // Clamp
                     genome.Weights[i] = Math.Clamp(
                         genome.Weights[i],
                         _minWeight,
                         _maxWeight
                     );
                 }
             }
         }

         private double NextGaussian()
         {
             // Box-Muller transform
             double u1 = 1.0 - _rng.NextDouble();
             double u2 = 1.0 - _rng.NextDouble();
             return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
         }
     }
     ```

  3. Implement `StructureMutator`:
     ```csharp
     public class StructureMutator : IMutationOperator
     {
         private readonly int _minLayers = 1;
         private readonly int _maxLayers = 5;
         private readonly int _minNeurons = 32;
         private readonly int _maxNeurons = 1024;

         public void Mutate(NetworkGenome genome)
         {
             float roll = _rng.NextFloat();

             if (roll < 0.25f && genome.HiddenLayers.Count < _maxLayers)
             {
                 AddLayer(genome);
             }
             else if (roll < 0.5f && genome.HiddenLayers.Count > _minLayers)
             {
                 RemoveLayer(genome);
             }
             else if (roll < 0.75f)
             {
                 ChangeNeuronCount(genome);
             }
             else
             {
                 ChangeActivation(genome);
             }

             // Recalculate weights after structure change
             ReinitializeWeightsForNewStructure(genome);
         }

         private void AddLayer(NetworkGenome genome)
         {
             int position = _rng.Next(genome.HiddenLayers.Count + 1);
             var newLayer = new DenseLayerConfig
             {
                 NeuronCount = _rng.Next(_minNeurons, _maxNeurons + 1),
                 Activation = ActivationType.ReLU
             };
             genome.HiddenLayers.Insert(position, newLayer);
         }

         private void RemoveLayer(NetworkGenome genome)
         {
             int position = _rng.Next(genome.HiddenLayers.Count);
             genome.HiddenLayers.RemoveAt(position);
         }

         private void ChangeNeuronCount(NetworkGenome genome)
         {
             int layerIdx = _rng.Next(genome.HiddenLayers.Count);
             int delta = _rng.Next(-64, 65);
             genome.HiddenLayers[layerIdx].NeuronCount = Math.Clamp(
                 genome.HiddenLayers[layerIdx].NeuronCount + delta,
                 _minNeurons,
                 _maxNeurons
             );
         }
     }
     ```

  4. Implement `CompositeMutator`:
     ```csharp
     public class CompositeMutator : IMutationOperator
     {
         private readonly IMutationOperator _weightMutator;
         private readonly IMutationOperator _structureMutator;
         private readonly float _structureMutationRate;

         public void Mutate(NetworkGenome genome)
         {
             // Always do weight mutation
             _weightMutator.Mutate(genome);

             // Occasionally do structure mutation
             if (_rng.NextFloat() < _structureMutationRate)
             {
                 _structureMutator.Mutate(genome);
             }
         }
     }
     ```

  5. Create tests:
     - Test_WeightMutation_ChangesWeights
     - Test_WeightMutation_GaussianDistribution
     - Test_WeightMutation_RespectsClamp
     - Test_StructureMutation_AddsLayer
     - Test_StructureMutation_RemovesLayer
     - Test_MutatedGenome_StillValid

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase3.Mutation"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify mutated genomes are valid"

on_failure: |
  If mutation produces invalid genomes:
  1. Add validation after mutation
  2. Check bounds for layer counts
  3. Verify weight reinitialization logic
  4. Add safeguards for edge cases
```

---

### F3.T3: Crossover Operators

```yaml
task_id: "F3.T3"
name: "Crossover Operators"
description: |
  Implement crossover operators that combine genetic material
  from two parent genomes to create offspring.

inputs:
  - "src/TzarBot.GeneticAlgorithm/TzarBot.GeneticAlgorithm.csproj"
  - "src/TzarBot.NeuralNetwork/Genome/NetworkGenome.cs"

outputs:
  - "src/TzarBot.GeneticAlgorithm/Operators/ICrossoverOperator.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/UniformCrossover.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/ArithmeticCrossover.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/StructuralCrossover.cs"
  - "tests/TzarBot.Tests/Phase3/CrossoverTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase3.Crossover\""

test_criteria: |
  - Crossover produces valid offspring
  - Child has genetic material from both parents
  - Weight crossover works correctly
  - Structure crossover handles different architectures
  - Output genome is valid

dependencies: ["F3.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement crossover operators for genetic algorithm.

  ## Context
  Project: `src/TzarBot.GeneticAlgorithm/`. Create crossover operators.

  ## Requirements

  1. Create interface:
     ```csharp
     public interface ICrossoverOperator
     {
         NetworkGenome Crossover(NetworkGenome parent1, NetworkGenome parent2);
         string Name { get; }
     }
     ```

  2. Implement `UniformCrossover` (for weights):
     ```csharp
     public class UniformCrossover : ICrossoverOperator
     {
         private readonly float _swapProbability = 0.5f;

         public NetworkGenome Crossover(NetworkGenome parent1, NetworkGenome parent2)
         {
             // Use structure from parent with higher fitness
             var child = (parent1.Fitness >= parent2.Fitness)
                 ? parent1.Clone()
                 : parent2.Clone();

             // Crossover weights
             int minLength = Math.Min(parent1.Weights.Length, parent2.Weights.Length);
             for (int i = 0; i < minLength; i++)
             {
                 if (_rng.NextFloat() < _swapProbability)
                 {
                     child.Weights[i] = parent2.Weights[i];
                 }
                 else
                 {
                     child.Weights[i] = parent1.Weights[i];
                 }
             }

             // Record parents
             child.ParentId1 = parent1.Id;
             child.ParentId2 = parent2.Id;

             return child;
         }
     }
     ```

  3. Implement `ArithmeticCrossover`:
     ```csharp
     public class ArithmeticCrossover : ICrossoverOperator
     {
         public NetworkGenome Crossover(NetworkGenome parent1, NetworkGenome parent2)
         {
             // Use structure from better parent
             var child = (parent1.Fitness >= parent2.Fitness)
                 ? parent1.Clone()
                 : parent2.Clone();

             // Blend weights
             int minLength = Math.Min(parent1.Weights.Length, parent2.Weights.Length);
             for (int i = 0; i < child.Weights.Length; i++)
             {
                 if (i < minLength)
                 {
                     float alpha = _rng.NextFloat();
                     child.Weights[i] = alpha * parent1.Weights[i]
                                      + (1 - alpha) * parent2.Weights[i];
                 }
                 // Weights beyond minLength keep from cloned parent
             }

             child.ParentId1 = parent1.Id;
             child.ParentId2 = parent2.Id;
             return child;
         }
     }
     ```

  4. Implement `StructuralCrossover`:
     ```csharp
     public class StructuralCrossover : ICrossoverOperator
     {
         public NetworkGenome Crossover(NetworkGenome parent1, NetworkGenome parent2)
         {
             var child = new NetworkGenome
             {
                 Id = Guid.NewGuid(),
                 ParentId1 = parent1.Id,
                 ParentId2 = parent2.Id,
                 ConvLayers = parent1.ConvLayers, // Conv layers are fixed
                 InputWidth = parent1.InputWidth,
                 InputHeight = parent1.InputHeight,
                 InputChannels = parent1.InputChannels,
                 OutputActionCount = parent1.OutputActionCount
             };

             // Crossover hidden layers
             child.HiddenLayers = new List<DenseLayerConfig>();
             int maxLayers = Math.Max(
                 parent1.HiddenLayers.Count,
                 parent2.HiddenLayers.Count
             );

             for (int i = 0; i < maxLayers; i++)
             {
                 NetworkGenome source;
                 if (i >= parent1.HiddenLayers.Count)
                 {
                     source = parent2;
                 }
                 else if (i >= parent2.HiddenLayers.Count)
                 {
                     source = parent1;
                 }
                 else
                 {
                     source = _rng.NextFloat() < 0.5f ? parent1 : parent2;
                 }

                 if (i < source.HiddenLayers.Count)
                 {
                     child.HiddenLayers.Add(source.HiddenLayers[i].Clone());
                 }
             }

             // Initialize weights for new structure
             child.Weights = new float[child.CalculateTotalWeights()];
             WeightInitializer.XavierUniform(child.Weights, ...);

             // Try to copy compatible weights from parents
             CopyCompatibleWeights(child, parent1, parent2);

             return child;
         }
     }
     ```

  5. Create tests:
     - Test_UniformCrossover_ProducesValidChild
     - Test_UniformCrossover_HasGeneticMaterial_FromBothParents
     - Test_ArithmeticCrossover_BlendedWeights
     - Test_StructuralCrossover_DifferentArchitectures
     - Test_Crossover_SetsParentIds

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase3.Crossover"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify offspring have valid architecture"

on_failure: |
  If crossover produces invalid genomes:
  1. Add validation after crossover
  2. Check weight array sizes
  3. Handle mismatched architectures gracefully
  4. Ensure parent references are set correctly
```

---

### F3.T4: Selection & Elitism

```yaml
task_id: "F3.T4"
name: "Selection & Elitism"
description: |
  Implement selection operators that choose which individuals
  reproduce and implement elitism to preserve best genomes.

inputs:
  - "src/TzarBot.GeneticAlgorithm/TzarBot.GeneticAlgorithm.csproj"
  - "src/TzarBot.GeneticAlgorithm/Core/Population.cs"

outputs:
  - "src/TzarBot.GeneticAlgorithm/Operators/ISelectionOperator.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/TournamentSelection.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/RouletteWheelSelection.cs"
  - "src/TzarBot.GeneticAlgorithm/Operators/ElitismHandler.cs"
  - "tests/TzarBot.Tests/Phase3/SelectionTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase3.Selection\""

test_criteria: |
  - Tournament selection picks from tournament
  - Higher fitness individuals selected more often
  - Roulette wheel proportional to fitness
  - Elitism preserves exact copies of best
  - Selection handles equal fitness

dependencies: ["F3.T1"]
estimated_complexity: "S"

claude_prompt: |
  Implement selection operators for genetic algorithm.

  ## Context
  Project: `src/TzarBot.GeneticAlgorithm/`. Create selection operators.

  ## Requirements

  1. Create interface:
     ```csharp
     public interface ISelectionOperator
     {
         NetworkGenome Select(Population population);
         string Name { get; }
     }
     ```

  2. Implement `TournamentSelection`:
     ```csharp
     public class TournamentSelection : ISelectionOperator
     {
         private readonly int _tournamentSize;

         public TournamentSelection(int tournamentSize = 3)
         {
             _tournamentSize = tournamentSize;
         }

         public NetworkGenome Select(Population population)
         {
             NetworkGenome best = null;
             float bestFitness = float.MinValue;

             for (int i = 0; i < _tournamentSize; i++)
             {
                 var candidate = population.Individuals[
                     _rng.Next(population.Individuals.Count)
                 ];

                 if (candidate.Fitness > bestFitness)
                 {
                     best = candidate;
                     bestFitness = candidate.Fitness;
                 }
             }

             return best;
         }
     }
     ```

  3. Implement `RouletteWheelSelection`:
     ```csharp
     public class RouletteWheelSelection : ISelectionOperator
     {
         public NetworkGenome Select(Population population)
         {
             // Normalize fitness (handle negative values)
             float minFitness = population.MinFitness;
             float totalFitness = population.Individuals
                 .Sum(g => g.Fitness - minFitness + 1);

             float spin = _rng.NextFloat() * totalFitness;
             float cumulative = 0;

             foreach (var genome in population.Individuals)
             {
                 cumulative += genome.Fitness - minFitness + 1;
                 if (cumulative >= spin)
                 {
                     return genome;
                 }
             }

             return population.Individuals.Last();
         }
     }
     ```

  4. Implement `ElitismHandler`:
     ```csharp
     public class ElitismHandler
     {
         private readonly int _eliteCount;

         public IEnumerable<NetworkGenome> GetElites(Population population)
         {
             return population.Individuals
                 .OrderByDescending(g => g.Fitness)
                 .Take(_eliteCount)
                 .Select(g => g.Clone());
         }

         public bool IsElite(NetworkGenome genome, Population population)
         {
             var topFitness = population.Individuals
                 .OrderByDescending(g => g.Fitness)
                 .Take(_eliteCount)
                 .Min(g => g.Fitness);

             return genome.Fitness >= topFitness;
         }
     }
     ```

  5. Create tests:
     - Test_TournamentSelection_ReturnsBestOfTournament
     - Test_TournamentSelection_HigherFitness_SelectedMoreOften
     - Test_RouletteWheel_ProportionalToFitness
     - Test_Elitism_PreservesTopN
     - Test_Selection_HandlesEqualFitness

  ## Statistical Test
  ```csharp
  [Fact]
  public void Test_Selection_FavorsHighFitness()
  {
      var population = CreatePopulationWithKnownFitness();
      var selection = new TournamentSelection(3);

      var counts = new Dictionary<Guid, int>();
      for (int i = 0; i < 10000; i++)
      {
          var selected = selection.Select(population);
          counts[selected.Id] = counts.GetValueOrDefault(selected.Id) + 1;
      }

      // Higher fitness should be selected more often
      var sortedByFitness = population.Individuals.OrderByDescending(g => g.Fitness);
      var sortedBySelection = counts.OrderByDescending(c => c.Value);

      // Top 3 by fitness should be in top 5 by selection count
      // (with high probability)
  }
  ```

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase3.Selection"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify statistical properties"

on_failure: |
  If selection seems random:
  1. Check tournament size (too small = near random)
  2. Verify fitness values are correctly set
  3. Check for integer overflow in calculations
  4. Increase sample size for statistical tests
```

---

### F3.T5: Fitness Calculator & Persistence

```yaml
task_id: "F3.T5"
name: "Fitness Calculator & Persistence"
description: |
  Implement fitness calculation from game results and
  population persistence to SQLite database.

inputs:
  - "src/TzarBot.GeneticAlgorithm/TzarBot.GeneticAlgorithm.csproj"
  - "src/TzarBot.Common/Models/GameResult.cs"
  - "plans/1general_plan.md (section 3.3)"

outputs:
  - "src/TzarBot.GeneticAlgorithm/Fitness/IFitnessCalculator.cs"
  - "src/TzarBot.GeneticAlgorithm/Fitness/FitnessCalculator.cs"
  - "src/TzarBot.GeneticAlgorithm/Fitness/FitnessConfig.cs"
  - "src/TzarBot.GeneticAlgorithm/Persistence/IGenomeRepository.cs"
  - "src/TzarBot.GeneticAlgorithm/Persistence/SqliteGenomeRepository.cs"
  - "tests/TzarBot.Tests/Phase3/FitnessTests.cs"
  - "tests/TzarBot.Tests/Phase3/PersistenceTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase3.Fitness|Phase3.Persistence\""

test_criteria: |
  - Fitness calculation produces expected values
  - Winning game has higher fitness than losing
  - Database saves and loads correctly
  - Population persists across sessions
  - Query performance is acceptable

dependencies: ["F3.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement fitness calculation and database persistence.

  ## Context
  Project: `src/TzarBot.GeneticAlgorithm/`. Use SQLite for persistence.

  ## Requirements

  1. Add NuGet packages:
     - Microsoft.Data.Sqlite
     - Dapper (optional, for simpler queries)

  2. Create GameResult in Common:
     ```csharp
     public class GameResult
     {
         public Guid GenomeId { get; set; }
         public GameOutcome Outcome { get; set; }
         public TimeSpan Duration { get; set; }
         public int UnitsBuilt { get; set; }
         public int BuildingsBuilt { get; set; }
         public int EnemyUnitsKilled { get; set; }
         public int EnemyBuildingsDestroyed { get; set; }
         public int ResourcesGathered { get; set; }
         public int IdleTimeSeconds { get; set; }
         public int InvalidActionsCount { get; set; }
         public DateTime PlayedAt { get; set; }
     }

     public enum GameOutcome
     {
         Victory,
         Defeat,
         Timeout,
         Crashed,
         Stuck,
         Cancelled
     }
     ```

  3. Create fitness config:
     ```csharp
     public class FitnessConfig
     {
         // Win/loss weights
         public float WinBonus { get; set; } = 1000f;
         public float SurvivalPerMinute { get; set; } = 100f;

         // Time bonuses
         public float FastWinMultiplier { get; set; } = 500f;

         // Activity weights
         public float UnitsBuiltWeight { get; set; } = 10f;
         public float BuildingsBuiltWeight { get; set; } = 5f;
         public float EnemyKilledWeight { get; set; } = 20f;
         public float BuildingsDestroyedWeight { get; set; } = 50f;

         // Penalties
         public float IdlePenaltyPerSecond { get; set; } = 1f;
         public float InvalidActionPenalty { get; set; } = 0.5f;
     }
     ```

  4. Implement `FitnessCalculator`:
     ```csharp
     public class FitnessCalculator : IFitnessCalculator
     {
         private readonly FitnessConfig _config;

         public float Calculate(GameResult result)
         {
             float fitness = 0;

             // Base outcome
             switch (result.Outcome)
             {
                 case GameOutcome.Victory:
                     fitness += _config.WinBonus;
                     fitness += _config.FastWinMultiplier /
                                (float)result.Duration.TotalMinutes;
                     break;

                 case GameOutcome.Defeat:
                     fitness += _config.SurvivalPerMinute *
                                (float)result.Duration.TotalMinutes;
                     break;

                 case GameOutcome.Timeout:
                 case GameOutcome.Stuck:
                     fitness += _config.SurvivalPerMinute * 0.5f *
                                (float)result.Duration.TotalMinutes;
                     break;
             }

             // Activity bonuses
             fitness += result.UnitsBuilt * _config.UnitsBuiltWeight;
             fitness += result.BuildingsBuilt * _config.BuildingsBuiltWeight;
             fitness += result.EnemyUnitsKilled * _config.EnemyKilledWeight;
             fitness += result.EnemyBuildingsDestroyed *
                        _config.BuildingsDestroyedWeight;

             // Penalties
             fitness -= result.IdleTimeSeconds * _config.IdlePenaltyPerSecond;
             fitness -= result.InvalidActionsCount * _config.InvalidActionPenalty;

             return Math.Max(0, fitness);
         }

         public float CalculateAggregate(IEnumerable<GameResult> results)
         {
             // Average of multiple games
             return results.Average(r => Calculate(r));
         }
     }
     ```

  5. Implement `SqliteGenomeRepository`:
     ```csharp
     public class SqliteGenomeRepository : IGenomeRepository, IDisposable
     {
         private readonly string _connectionString;

         public async Task InitializeAsync()
         {
             // Create tables if not exist
             await ExecuteAsync(@"
                 CREATE TABLE IF NOT EXISTS Genomes (
                     Id TEXT PRIMARY KEY,
                     Generation INTEGER,
                     Fitness REAL,
                     EloRating REAL,
                     ParentId1 TEXT,
                     ParentId2 TEXT,
                     CreatedAt TEXT,
                     Data BLOB
                 );

                 CREATE TABLE IF NOT EXISTS GameResults (
                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                     GenomeId TEXT,
                     Outcome INTEGER,
                     Duration INTEGER,
                     UnitsBuilt INTEGER,
                     EnemyUnitsKilled INTEGER,
                     PlayedAt TEXT,
                     FOREIGN KEY (GenomeId) REFERENCES Genomes(Id)
                 );

                 CREATE INDEX IF NOT EXISTS idx_genomes_generation
                 ON Genomes(Generation);
             ");
         }

         public async Task SaveGenomeAsync(NetworkGenome genome);
         public async Task<NetworkGenome?> GetGenomeAsync(Guid id);
         public async Task<IEnumerable<NetworkGenome>> GetGenerationAsync(int gen);
         public async Task SaveGameResultAsync(GameResult result);
         public async Task<IEnumerable<GameResult>> GetResultsForGenomeAsync(Guid id);
     }
     ```

  6. Create tests:
     - Test_Fitness_Win_HigherThan_Loss
     - Test_Fitness_FastWin_HigherThan_SlowWin
     - Test_Fitness_NonNegative
     - Test_Repository_SaveAndLoad_RoundTrip
     - Test_Repository_GetGeneration

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase3.Fitness|Phase3.Persistence"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Verify database file is created"
  - "Check data persists between runs"

on_failure: |
  If persistence fails:
  1. Check SQLite connection string
  2. Verify file permissions
  3. Check MessagePack serialization
  4. Ensure tables are created before queries
```

---

## Rollback Plan

If Phase 3 implementation fails:

1. **Simpler GA**: Use only weight mutation (no structure evolution)
   - Fewer edge cases
   - Easier to debug
   - Can add complexity later

2. **Alternative Storage**: Use JSON files instead of SQLite
   - Simpler implementation
   - Easier to debug
   - Human-readable

3. **Fixed Population**: Use fixed population with simple random selection
   - Eliminates selection bugs
   - Focus on mutation only

---

## API Documentation

### GeneticAlgorithm API

```csharp
// Create and configure GA
var config = new GAConfig
{
    PopulationSize = 100,
    EliteCount = 5,
    MutationRate = 0.2f
};

var ga = new GeneticAlgorithmEngine(config);
ga.OnGenerationComplete += stats =>
{
    Console.WriteLine($"Gen {stats.Generation}: Best={stats.BestFitness}");
};

// Initialize population
ga.Initialize(inputWidth: 240, inputHeight: 135);

// After evaluation, set fitness
foreach (var result in gameResults)
{
    float fitness = fitnessCalc.Calculate(result);
    ga.SetFitness(result.GenomeId, fitness);
}

// Evolve to next generation
ga.EvolveNextGeneration();
```

### Persistence API

```csharp
var repo = new SqliteGenomeRepository("tzarbot.db");
await repo.InitializeAsync();

// Save population
foreach (var genome in population.Individuals)
{
    await repo.SaveGenomeAsync(genome);
}

// Load previous generation
var previousGen = await repo.GetGenerationAsync(generation - 1);

// Get best genome ever
var best = await repo.GetBestGenomeAsync();
```

---

*Phase 3 Detailed Plan - Version 1.0*
*See prompts/phase_3/ for individual task prompts*
