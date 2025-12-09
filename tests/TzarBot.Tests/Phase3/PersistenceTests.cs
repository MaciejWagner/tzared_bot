using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Persistence;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for persistence layer (repository and checkpoints).
/// </summary>
public class PersistenceTests : IDisposable
{
    private readonly string _tempDir;

    public PersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"TzarBotTests_{Guid.NewGuid():N}");
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

    #region SqliteGenomeRepository Tests

    [Fact]
    public async Task Repository_SaveAndLoad_ShouldPreserveGenome()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        var genome = NetworkGenome.CreateRandom(new[] { 128, 64 }, 42);
        genome.Fitness = 42.5f;
        genome.GamesPlayed = 10;
        genome.Wins = 7;

        // Act
        await repo.SaveAsync(genome);
        var loaded = await repo.GetByIdAsync(genome.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(genome.Id);
        loaded.Fitness.Should().Be(genome.Fitness);
        loaded.GamesPlayed.Should().Be(genome.GamesPlayed);
        loaded.Wins.Should().Be(genome.Wins);
        loaded.HiddenLayers.Should().HaveCount(genome.HiddenLayers.Count);
        loaded.Weights.Should().BeEquivalentTo(genome.Weights);
    }

    [Fact]
    public async Task Repository_SaveMany_ShouldSaveAllGenomes()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        var genomes = Enumerable.Range(0, 10)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, i))
            .ToList();

        // Act
        await repo.SaveManyAsync(genomes);
        int count = await repo.GetCountAsync();

        // Assert
        count.Should().Be(10);
    }

    [Fact]
    public async Task Repository_GetByGeneration_ShouldReturnCorrectGenomes()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        var gen1Genomes = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var g = NetworkGenome.CreateRandom(new[] { 128 }, i);
                g.Generation = 1;
                return g;
            })
            .ToList();
        var gen2Genomes = Enumerable.Range(0, 3)
            .Select(i =>
            {
                var g = NetworkGenome.CreateRandom(new[] { 128 }, i + 100);
                g.Generation = 2;
                return g;
            })
            .ToList();

        await repo.SaveManyAsync(gen1Genomes);
        await repo.SaveManyAsync(gen2Genomes);

        // Act
        var result = await repo.GetByGenerationAsync(1);

        // Assert
        result.Should().HaveCount(5);
        result.Should().OnlyContain(g => g.Generation == 1);
    }

    [Fact]
    public async Task Repository_GetTopByFitness_ShouldReturnHighestFitness()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        var genomes = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var g = NetworkGenome.CreateRandom(new[] { 128 }, i);
                g.Fitness = i * 10f; // 0, 10, 20, ..., 90
                return g;
            })
            .ToList();

        await repo.SaveManyAsync(genomes);

        // Act
        var top3 = await repo.GetTopByFitnessAsync(3);

        // Assert
        top3.Should().HaveCount(3);
        top3[0].Fitness.Should().Be(90f);
        top3[1].Fitness.Should().Be(80f);
        top3[2].Fitness.Should().Be(70f);
    }

    [Fact]
    public async Task Repository_Delete_ShouldRemoveGenome()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        var genome = NetworkGenome.CreateRandom(new[] { 128 }, 42);
        await repo.SaveAsync(genome);

        // Act
        bool deleted = await repo.DeleteAsync(genome.Id);
        var loaded = await repo.GetByIdAsync(genome.Id);

        // Assert
        deleted.Should().BeTrue();
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task Repository_DeleteOlderThanGeneration_ShouldRemoveOldGenomes()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        for (int gen = 0; gen < 10; gen++)
        {
            var genome = NetworkGenome.CreateRandom(new[] { 128 }, gen);
            genome.Generation = gen;
            await repo.SaveAsync(genome);
        }

        // Act
        int deleted = await repo.DeleteOlderThanGenerationAsync(5);
        int remaining = await repo.GetCountAsync();

        // Assert
        deleted.Should().Be(5); // Generations 0-4
        remaining.Should().Be(5); // Generations 5-9
    }

    [Fact]
    public async Task Repository_GetLatestGeneration_ShouldReturnHighestGeneration()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        for (int gen = 0; gen < 5; gen++)
        {
            var genome = NetworkGenome.CreateRandom(new[] { 128 }, gen);
            genome.Generation = gen;
            await repo.SaveAsync(genome);
        }

        // Act
        int latest = await repo.GetLatestGenerationAsync();

        // Assert
        latest.Should().Be(4);
    }

    [Fact]
    public async Task Repository_UpdateFitness_ShouldUpdateValues()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        var genome = NetworkGenome.CreateRandom(new[] { 128 }, 42);
        genome.Fitness = 0f;
        genome.GamesPlayed = 0;
        genome.Wins = 0;
        await repo.SaveAsync(genome);

        // Act
        await repo.UpdateFitnessAsync(genome.Id, 100f, 10, 8);
        var loaded = await repo.GetByIdAsync(genome.Id);

        // Assert
        loaded!.Fitness.Should().Be(100f);
        loaded.GamesPlayed.Should().Be(10);
        loaded.Wins.Should().Be(8);
    }

    [Fact]
    public async Task Repository_GetGenerationStatistics_ShouldCalculateCorrectly()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();
        for (int i = 0; i < 10; i++)
        {
            var genome = NetworkGenome.CreateRandom(new[] { 128 }, i);
            genome.Generation = 1;
            genome.Fitness = i * 10f; // 0, 10, 20, ..., 90
            genome.GamesPlayed = 5;
            genome.Wins = i % 5;
            await repo.SaveAsync(genome);
        }

        // Act
        var stats = await repo.GetGenerationStatisticsAsync(1);

        // Assert
        stats.Should().NotBeNull();
        stats!.Generation.Should().Be(1);
        stats.PopulationSize.Should().Be(10);
        stats.AverageFitness.Should().Be(45f); // Average of 0-90
        stats.MaxFitness.Should().Be(90f);
        stats.MinFitness.Should().Be(0f);
        stats.TotalGamesPlayed.Should().Be(50);
    }

    [Fact]
    public async Task Repository_Metadata_ShouldPersist()
    {
        // Arrange
        await using var repo = SqliteGenomeRepository.CreateInMemory();

        // Act
        await repo.SetMetadataAsync("test_key", "test_value");
        var value = await repo.GetMetadataAsync("test_key");

        // Assert
        value.Should().Be("test_value");
    }

    #endregion

    #region PopulationCheckpoint Tests

    [Fact]
    public async Task Checkpoint_SaveAndLoad_ShouldPreservePopulation()
    {
        // Arrange
        var checkpoint = new PopulationCheckpoint(_tempDir);
        var population = Enumerable.Range(0, 10)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, i))
            .ToList();

        // Act
        await checkpoint.SaveAsync(population, 5);
        var loaded = await checkpoint.LoadLatestAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Generation.Should().Be(5);
        loaded.Genomes.Should().HaveCount(10);
    }

    [Fact]
    public async Task Checkpoint_SaveWithStats_ShouldPreserveStats()
    {
        // Arrange
        var checkpoint = new PopulationCheckpoint(_tempDir);
        var population = Enumerable.Range(0, 10)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, i))
            .ToList();
        var stats = new GenerationStats
        {
            Generation = 5,
            BestFitness = 100f,
            AverageFitness = 50f,
            WorstFitness = 10f,
            EliteCount = 3
        };

        // Act
        await checkpoint.SaveAsync(population, 5, stats);
        var loaded = await checkpoint.LoadLatestAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Stats.Should().NotBeNull();
        loaded.Stats!.BestFitness.Should().Be(100f);
        loaded.Stats.AverageFitness.Should().Be(50f);
    }

    [Fact]
    public async Task Checkpoint_LoadByGeneration_ShouldFindCorrectCheckpoint()
    {
        // Arrange
        var checkpoint = new PopulationCheckpoint(_tempDir);
        var pop1 = Enumerable.Range(0, 5)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, i))
            .ToList();
        var pop2 = Enumerable.Range(0, 5)
            .Select(i => NetworkGenome.CreateRandom(new[] { 256 }, i + 100))
            .ToList();

        await checkpoint.SaveAsync(pop1, 10);
        await Task.Delay(100); // Ensure different timestamps
        await checkpoint.SaveAsync(pop2, 20);

        // Act
        var loaded = await checkpoint.LoadByGenerationAsync(10);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Generation.Should().Be(10);
    }

    [Fact]
    public async Task Checkpoint_ListCheckpoints_ShouldReturnAllCheckpoints()
    {
        // Arrange
        var checkpoint = new PopulationCheckpoint(_tempDir);
        var population = Enumerable.Range(0, 5)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, i))
            .ToList();

        // Save multiple checkpoints
        for (int gen = 0; gen < 5; gen++)
        {
            await checkpoint.SaveAsync(population, gen);
        }

        // Act
        var list = checkpoint.ListCheckpoints();

        // Assert
        list.Should().HaveCount(5);
        list.Should().BeInDescendingOrder(c => c.Generation);
    }

    [Fact]
    public async Task Checkpoint_PruneOldCheckpoints_ShouldKeepOnlyRecent()
    {
        // Arrange
        var checkpoint = new PopulationCheckpoint(_tempDir);
        var population = Enumerable.Range(0, 5)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, i))
            .ToList();

        // Save 10 checkpoints
        for (int gen = 0; gen < 10; gen++)
        {
            await checkpoint.SaveAsync(population, gen);
        }

        // Act
        checkpoint.PruneOldCheckpoints(3);
        var remaining = checkpoint.ListCheckpoints();

        // Assert
        remaining.Should().HaveCount(3);
        remaining.Should().OnlyContain(c => c.Generation >= 7);
    }

    [Fact]
    public async Task Checkpoint_ExportAndImport_ShouldWork()
    {
        // Arrange
        var checkpoint = new PopulationCheckpoint(_tempDir);
        var population = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var g = NetworkGenome.CreateRandom(new[] { 128 }, i);
                g.Fitness = i * 10f;
                return g;
            })
            .ToList();

        var original = new CheckpointData
        {
            Generation = 42,
            Timestamp = DateTime.UtcNow,
            Genomes = population
        };

        var exportPath = Path.Combine(_tempDir, "export_test.bin");

        // Act
        await checkpoint.ExportAsync(original, exportPath);
        var imported = await checkpoint.ImportAsync(exportPath);

        // Assert
        imported.Generation.Should().Be(42);
        imported.Genomes.Should().HaveCount(5);
        imported.Genomes[0].Fitness.Should().Be(0f);
        imported.Genomes[4].Fitness.Should().Be(40f);
    }

    #endregion
}
