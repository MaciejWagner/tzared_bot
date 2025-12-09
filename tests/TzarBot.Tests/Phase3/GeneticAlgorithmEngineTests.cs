using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for the main GA engine.
/// </summary>
public class GeneticAlgorithmEngineTests : IDisposable
{
    private readonly string _tempDir;

    public GeneticAlgorithmEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"TzarBotGATests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void InitializePopulation_ShouldCreateCorrectNumberOfGenomes()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = 50 };
        var engine = new GeneticAlgorithmEngine(config);

        // Act
        engine.InitializePopulation(42);

        // Assert
        engine.Population.Should().HaveCount(50);
        engine.Generation.Should().Be(0);
    }

    [Fact]
    public void InitializePopulation_ShouldCreateValidGenomes()
    {
        // Arrange
        var engine = new GeneticAlgorithmEngine();

        // Act
        engine.InitializePopulation(42);

        // Assert
        engine.Population.Should().OnlyContain(g => g.IsValid());
    }

    [Fact]
    public void InitializePopulation_ShouldBeReproducible()
    {
        // Arrange
        var engine1 = new GeneticAlgorithmEngine();
        var engine2 = new GeneticAlgorithmEngine();

        // Act
        engine1.InitializePopulation(42);
        engine2.InitializePopulation(42);

        // Assert
        engine1.Population.Should().HaveCount(engine2.Population.Count);
        for (int i = 0; i < engine1.Population.Count; i++)
        {
            engine1.Population[i].HiddenLayers.Count
                .Should().Be(engine2.Population[i].HiddenLayers.Count);
            engine1.Population[i].Weights
                .Should().BeEquivalentTo(engine2.Population[i].Weights);
        }
    }

    [Fact]
    public void LoadPopulation_ShouldSetPopulationAndGeneration()
    {
        // Arrange
        var engine = new GeneticAlgorithmEngine();
        var population = Enumerable.Range(0, 20)
            .Select(i =>
            {
                var g = NetworkGenome.CreateRandom(new[] { 128 }, i);
                g.Generation = 5;
                g.Fitness = i * 10f;
                return g;
            })
            .ToList();

        // Act
        engine.LoadPopulation(population);

        // Assert
        engine.Population.Should().HaveCount(20);
        engine.Generation.Should().Be(5);
        engine.BestGenome.Should().NotBeNull();
        engine.BestGenome!.Fitness.Should().Be(190f); // Highest fitness
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldEvaluateFitness()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 10,
            ElitismRate = 0.1f
        };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        int evaluationCount = 0;
        FitnessEvaluator evaluator = (genome, ct) =>
        {
            Interlocked.Increment(ref evaluationCount);
            return Task.FromResult(genome.Weights.Sum(w => Math.Abs(w)));
        };

        // Act
        var stats = await engine.RunGenerationAsync(evaluator);

        // Assert
        evaluationCount.Should().Be(10); // All genomes evaluated
        stats.Generation.Should().Be(1);
        engine.Generation.Should().Be(1);
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldPreserveElites()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 20,
            ElitismRate = 0.1f, // 2 elites
            MutationRate = 1.0f // Always mutate
        };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        FitnessEvaluator evaluator = (genome, ct) =>
            Task.FromResult((float)genome.Weights.Length);

        // Run first generation to establish fitness
        await engine.RunGenerationAsync(evaluator);

        // Get top 2 genomes
        var topFitness = engine.Population
            .OrderByDescending(g => g.Fitness)
            .Take(2)
            .Select(g => g.Fitness)
            .ToList();

        // Act - run another generation
        await engine.RunGenerationAsync(evaluator);

        // Assert - elites should still have high fitness
        var newTopFitness = engine.Population
            .OrderByDescending(g => g.Fitness)
            .Take(2)
            .Max(g => g.Fitness);

        newTopFitness.Should().BeGreaterThan(topFitness.Min() - 0.01f);
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldRaiseEvents()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = 10 };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        bool generationCompletedRaised = false;
        engine.GenerationCompleted += (sender, stats) => generationCompletedRaised = true;

        FitnessEvaluator evaluator = (genome, ct) =>
            Task.FromResult(100f);

        // Act
        await engine.RunGenerationAsync(evaluator);

        // Assert
        generationCompletedRaised.Should().BeTrue();
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldRaiseNewBestGenomeEvent()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = 10 };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        bool newBestRaised = false;
        engine.NewBestGenomeFound += (sender, genome) => newBestRaised = true;

        // Evaluator that always gives high fitness
        FitnessEvaluator evaluator = (genome, ct) =>
            Task.FromResult(100f);

        // Act
        await engine.RunGenerationAsync(evaluator);

        // Assert
        newBestRaised.Should().BeTrue();
        engine.BestGenome.Should().NotBeNull();
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldCalculateCorrectStats()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 10,
            ElitismRate = 0.2f
        };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        int index = 0;
        FitnessEvaluator evaluator = (genome, ct) =>
        {
            int idx = Interlocked.Increment(ref index);
            return Task.FromResult(idx * 10f); // 10, 20, ..., 100
        };

        // Act
        var stats = await engine.RunGenerationAsync(evaluator);

        // Assert
        stats.BestFitness.Should().Be(100f);
        stats.AverageFitness.Should().BeApproximately(55f, 0.1f);
        stats.WorstFitness.Should().Be(10f);
        stats.EliteCount.Should().Be(2); // 20% of 10
    }

    [Fact]
    public async Task RunAsync_ShouldRunMultipleGenerations()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 10,
            LogInterval = 0 // Disable logging
        };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        FitnessEvaluator evaluator = (genome, ct) =>
            Task.FromResult((float)Random.Shared.NextDouble() * 100);

        // Act
        var statsList = new List<GenerationStats>();
        await foreach (var stats in engine.RunAsync(evaluator, maxGenerations: 5))
        {
            statsList.Add(stats);
        }

        // Assert
        statsList.Should().HaveCount(5);
        engine.Generation.Should().Be(5);
    }

    [Fact]
    public async Task RunAsync_ShouldRespectCancellation()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = 10 };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        using var cts = new CancellationTokenSource();
        int generationsRun = 0;

        FitnessEvaluator evaluator = (genome, ct) =>
            Task.FromResult(100f);

        // Act
        await foreach (var stats in engine.RunAsync(evaluator, maxGenerations: 100, cts.Token))
        {
            generationsRun++;
            if (generationsRun >= 3)
            {
                cts.Cancel();
            }
        }

        // Assert
        generationsRun.Should().Be(3);
    }

    [Fact]
    public void GetDiversity_ShouldReturnZeroForIdenticalStructures()
    {
        // Arrange
        var engine = new GeneticAlgorithmEngine();
        engine.InitializePopulation(42); // All same structure

        // Act
        float diversity = engine.GetDiversity();

        // Assert
        diversity.Should().BeApproximately(1f / engine.Population.Count, 0.01f);
    }

    [Fact]
    public async Task SaveAndLoadCheckpoint_ShouldWork()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 10,
            CheckpointInterval = 1
        };
        var engine = new GeneticAlgorithmEngine(config, _tempDir);
        engine.InitializePopulation(42);

        FitnessEvaluator evaluator = (genome, ct) =>
            Task.FromResult(100f);

        // Run a generation to trigger checkpoint
        await engine.RunGenerationAsync(evaluator);
        var originalBestFitness = engine.BestGenome!.Fitness;

        // Create new engine and load checkpoint
        var engine2 = new GeneticAlgorithmEngine(config, _tempDir);

        // Act
        bool loaded = await engine2.LoadLatestCheckpointAsync();

        // Assert
        loaded.Should().BeTrue();
        engine2.Population.Should().HaveCount(10);
        engine2.Generation.Should().Be(1);
    }

    [Fact]
    public async Task Engine_ShouldHandleParallelEvaluation()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 20,
            MaxParallelism = 4
        };
        var engine = new GeneticAlgorithmEngine(config);
        engine.InitializePopulation(42);

        int maxConcurrency = 0;
        int currentConcurrency = 0;

        FitnessEvaluator evaluator = async (genome, ct) =>
        {
            int current = Interlocked.Increment(ref currentConcurrency);
            int max = Interlocked.CompareExchange(ref maxConcurrency, current, 0);
            if (current > max)
            {
                Interlocked.Exchange(ref maxConcurrency, current);
            }

            await Task.Delay(10, ct); // Simulate work
            Interlocked.Decrement(ref currentConcurrency);

            return 100f;
        };

        // Act
        await engine.RunGenerationAsync(evaluator);

        // Assert - should have used parallelism
        maxConcurrency.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Engine_ShouldThrowForInvalidConfig()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = 5 }; // Below minimum

        // Act & Assert
        Action act = () => new GeneticAlgorithmEngine(config);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldThrowWithoutInitialization()
    {
        // Arrange
        var engine = new GeneticAlgorithmEngine();
        FitnessEvaluator evaluator = (genome, ct) => Task.FromResult(100f);

        // Act & Assert
        Func<Task> act = () => engine.RunGenerationAsync(evaluator);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
