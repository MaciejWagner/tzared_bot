using FluentAssertions;
using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Checkpoint;
using TzarBot.Training.Core;

namespace TzarBot.Tests.Phase6;

/// <summary>
/// Tests for the CheckpointManager class.
/// </summary>
public class CheckpointTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CheckpointManager _manager;

    public CheckpointTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"tzarbot_checkpoint_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _manager = new CheckpointManager(_tempDir, maxCheckpoints: 5);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); }
            catch { }
        }
    }

    [Fact]
    public async Task SaveCheckpointAsync_ShouldCreateFile()
    {
        // Arrange
        var state = CreateTestState(generation: 10);
        var config = TrainingConfig.Default();

        // Act
        var path = await _manager.SaveCheckpointAsync(state, config, 42);

        // Assert
        File.Exists(path).Should().BeTrue();
        Path.GetDirectoryName(path).Should().Be(_tempDir);
    }

    [Fact]
    public async Task SaveCheckpointAsync_ShouldCreateLatestLink()
    {
        // Arrange
        var state = CreateTestState(generation: 10);
        var config = TrainingConfig.Default();

        // Act
        await _manager.SaveCheckpointAsync(state, config, 42);

        // Assert
        var latestPath = Path.Combine(_tempDir, "latest.bin");
        File.Exists(latestPath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadLatestAsync_ShouldLoadSavedCheckpoint()
    {
        // Arrange
        var state = CreateTestState(generation: 10);
        var config = TrainingConfig.Default();
        await _manager.SaveCheckpointAsync(state, config, 42);

        // Act
        var loaded = await _manager.LoadLatestAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.State.CurrentGeneration.Should().Be(10);
        loaded.Seed.Should().Be(42);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadFromPath()
    {
        // Arrange
        var state = CreateTestState(generation: 5);
        var config = TrainingConfig.Default();
        var path = await _manager.SaveCheckpointAsync(state, config, 123);

        // Act
        var loaded = await _manager.LoadAsync(path);

        // Assert
        loaded.State.CurrentGeneration.Should().Be(5);
        loaded.Seed.Should().Be(123);
    }

    [Fact]
    public async Task LoadByGenerationAsync_ShouldFindCorrectCheckpoint()
    {
        // Arrange
        var config = TrainingConfig.Default();
        await _manager.SaveCheckpointAsync(CreateTestState(5), config, 1);
        await _manager.SaveCheckpointAsync(CreateTestState(10), config, 2);
        await _manager.SaveCheckpointAsync(CreateTestState(15), config, 3);

        // Act
        var loaded = await _manager.LoadByGenerationAsync(10);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.State.CurrentGeneration.Should().Be(10);
    }

    [Fact]
    public async Task SaveBestGenomeAsync_ShouldSaveGenome()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 128, 64 }, 42);
        genome.Fitness = 150f;

        // Act
        var path = await _manager.SaveBestGenomeAsync(genome, generation: 100, "CombatEasy");

        // Assert
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task LoadBestGenomeAsync_ShouldLoadSavedGenome()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 128, 64 }, 42);
        genome.Fitness = 150f;
        genome.GamesPlayed = 50;
        genome.Wins = 30;
        await _manager.SaveBestGenomeAsync(genome, generation: 100, "CombatEasy");

        // Act
        var loaded = await _manager.LoadBestGenomeAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Fitness.Should().Be(150f);
        loaded.FoundAtGeneration.Should().Be(100);
        loaded.StageName.Should().Be("CombatEasy");
        loaded.GamesPlayed.Should().Be(50);
        loaded.Wins.Should().Be(30);
    }

    [Fact]
    public async Task ListCheckpoints_ShouldReturnAllCheckpoints()
    {
        // Arrange
        var config = TrainingConfig.Default();
        await _manager.SaveCheckpointAsync(CreateTestState(5), config, 1);
        await Task.Delay(10); // Ensure different timestamps
        await _manager.SaveCheckpointAsync(CreateTestState(10), config, 2);
        await Task.Delay(10);
        await _manager.SaveCheckpointAsync(CreateTestState(15), config, 3);

        // Act
        var list = _manager.ListCheckpoints();

        // Assert
        list.Should().HaveCount(3);
        list[0].Generation.Should().Be(15); // Most recent first
        list[1].Generation.Should().Be(10);
        list[2].Generation.Should().Be(5);
    }

    [Fact]
    public async Task PruneOldCheckpoints_ShouldRemoveOldest()
    {
        // Arrange - use a manager with high maxCheckpoints to prevent auto-pruning during save
        var tempDir = Path.Combine(Path.GetTempPath(), $"tzarbot_prune_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = new CheckpointManager(tempDir, maxCheckpoints: 100); // High limit to avoid auto-prune
            var config = TrainingConfig.Default();
            for (int i = 1; i <= 10; i++)
            {
                await manager.SaveCheckpointAsync(CreateTestState(i), config, i);
                await Task.Delay(10); // Small delay for different timestamps
            }

            // Verify we have 10 checkpoints before pruning
            manager.ListCheckpoints().Should().HaveCount(10);

            // Act
            var deleted = manager.PruneOldCheckpoints(5);

            // Assert
            deleted.Should().Be(5);
            manager.ListCheckpoints().Should().HaveCount(5);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DeleteCheckpoint_ShouldRemoveFile()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "test_checkpoint.bin");
        File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

        // Act
        var result = _manager.DeleteCheckpoint(path);

        // Assert
        result.Should().BeTrue();
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCheckpointAsync_ShouldReturnTrueForValidCheckpoint()
    {
        // Arrange
        var state = CreateTestState(10);
        var config = TrainingConfig.Default();
        var path = await _manager.SaveCheckpointAsync(state, config, 42);

        // Act
        var isValid = await _manager.VerifyCheckpointAsync(path);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyCheckpointAsync_ShouldReturnFalseForCorruptedFile()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "corrupted.bin");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var isValid = await _manager.VerifyCheckpointAsync(path);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task Roundtrip_ShouldPreserveAllData()
    {
        // Arrange
        var state = CreateTestState(generation: 50);
        state.BestFitness = 123.45f;
        state.TotalGamesPlayed = 1000;
        state.TotalWins = 456;
        state.CurrentStageName = "CombatNormal";

        var config = new TrainingConfig
        {
            PopulationSize = 100,
            GamesPerGenome = 3,
            MaxGenerations = 500
        };

        // Act
        await _manager.SaveCheckpointAsync(state, config, 12345);
        var loaded = await _manager.LoadLatestAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.State.CurrentGeneration.Should().Be(50);
        loaded.State.BestFitness.Should().Be(123.45f);
        loaded.State.TotalGamesPlayed.Should().Be(1000);
        loaded.State.TotalWins.Should().Be(456);
        loaded.State.CurrentStageName.Should().Be("CombatNormal");
        loaded.Seed.Should().Be(12345);
        loaded.Config.PopulationSize.Should().Be(100);
        loaded.Config.GamesPerGenome.Should().Be(3);
    }

    [Fact]
    public void CheckpointInfo_TryParseFromFile_ShouldParseValidFilename()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "checkpoint_gen000050_20241215_103045.bin");
        // File must exist for FileInfo to work
        File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

        // Act
        var info = CheckpointInfo.TryParseFromFile(path);

        // Assert
        info.Should().NotBeNull();
        info!.Generation.Should().Be(50);
    }

    [Fact]
    public void CheckpointInfo_TryParseFromFile_ShouldReturnNullForInvalidFilename()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "invalid_file.bin");

        // Act
        var info = CheckpointInfo.TryParseFromFile(path);

        // Assert
        info.Should().BeNull();
    }

    private static TrainingState CreateTestState(int generation)
    {
        var genomes = Enumerable.Range(0, 10)
            .Select(i => NetworkGenome.CreateRandom(new[] { 64 }, i))
            .ToList();

        return new TrainingState
        {
            CurrentGeneration = generation,
            Population = genomes,
            BestGenome = genomes[0].Clone(),
            BestFitness = 50f,
            StartTime = DateTime.UtcNow.AddHours(-1),
            ElapsedTime = TimeSpan.FromHours(1)
        };
    }
}
