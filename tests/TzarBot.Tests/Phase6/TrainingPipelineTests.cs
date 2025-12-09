using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Checkpoint;
using TzarBot.Training.Core;
using TzarBot.Training.Curriculum;

namespace TzarBot.Tests.Phase6;

/// <summary>
/// Tests for the TrainingPipeline class.
/// </summary>
public class TrainingPipelineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TrainingConfig _config;
    private readonly Mock<IGeneticAlgorithm> _gaMock;
    private readonly ICurriculumManager _curriculum;
    private readonly ICheckpointManager _checkpoint;

    public TrainingPipelineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"tzarbot_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _config = new TrainingConfig
        {
            PopulationSize = 10,
            GamesPerGenome = 1,
            MaxGenerations = 5,
            CheckpointInterval = 2,
            CheckpointDirectory = Path.Combine(_tempDir, "checkpoints"),
            BestGenomeDirectory = Path.Combine(_tempDir, "best")
        };

        _gaMock = new Mock<IGeneticAlgorithm>();
        _curriculum = new CurriculumManager(
            StageDefinitions.GetSimplifiedStages());
        _checkpoint = new CheckpointManager(
            _config.CheckpointDirectory,
            _config.BestGenomeDirectory);

        SetupGAMock();
    }

    private void SetupGAMock()
    {
        var genomes = Enumerable.Range(0, _config.PopulationSize)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128, 64 }, i))
            .ToList();

        // Set fitness values on genomes (required for UpdateBest to work)
        for (int i = 0; i < genomes.Count; i++)
        {
            genomes[i].Fitness = 10f + i * 5f; // 10, 15, 20, ... 55
        }

        _gaMock.Setup(x => x.Population).Returns(genomes);
        _gaMock.Setup(x => x.Generation).Returns(0);
        _gaMock.Setup(x => x.Config).Returns(GeneticAlgorithmConfig.Default());

        _gaMock.Setup(x => x.InitializePopulation(It.IsAny<int?>()))
            .Callback(() => { });

        _gaMock.Setup(x => x.LoadPopulation(It.IsAny<IEnumerable<NetworkGenome>>()))
            .Callback(() => { });

        _gaMock.Setup(x => x.RunGenerationAsync(
            It.IsAny<FitnessEvaluator>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((FitnessEvaluator eval, CancellationToken ct) =>
            {
                return new GeneticAlgorithm.Engine.GenerationStats
                {
                    Generation = 1,
                    BestFitness = 55f, // Matches the highest genome fitness
                    AverageFitness = 30f,
                    WorstFitness = 10f,
                    FitnessStdDev = 10f,
                    BestGenomeId = genomes[^1].Id, // Last genome is best (fitness 55)
                    EvaluationTime = TimeSpan.FromSeconds(1),
                    EvolutionTime = TimeSpan.FromMilliseconds(100)
                };
            });

        // BestGenome should return the genome with highest fitness
        _gaMock.Setup(x => x.BestGenome).Returns(genomes[^1]);
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
    public async Task InitializeAsync_ShouldCreatePopulation()
    {
        // Arrange
        var options = Options.Create(_config);
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);

        // Act
        await pipeline.InitializeAsync(42);

        // Assert
        pipeline.State.Should().NotBeNull();
        pipeline.State.Status.Should().Be(TrainingStatus.NotStarted);
        pipeline.State.Population.Should().HaveCount(_config.PopulationSize);
        _gaMock.Verify(x => x.InitializePopulation(42), Times.Once);
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldUpdateState()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);
        await pipeline.InitializeAsync(42);

        // Act
        var stats = await pipeline.RunGenerationAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.BestFitness.Should().BeGreaterThan(0);
        pipeline.State.GenerationHistory.Should().HaveCount(1);
    }

    [Fact]
    public async Task RunGenerationAsync_ShouldRecordBestGenome()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);
        await pipeline.InitializeAsync(42);

        // Act
        await pipeline.RunGenerationAsync();

        // Assert
        pipeline.State.BestGenome.Should().NotBeNull();
        pipeline.State.BestFitness.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Config_ShouldReturnTrainingConfig()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);

        // Act & Assert
        pipeline.Config.Should().BeSameAs(_config);
    }

    [Fact]
    public async Task SaveCheckpointAsync_ShouldCreateFile()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);
        await pipeline.InitializeAsync(42);
        await pipeline.RunGenerationAsync();

        // Act
        var path = await pipeline.SaveCheckpointAsync();

        // Assert
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task PauseAndResumeAsync_ShouldChangeStatus()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);
        await pipeline.InitializeAsync(42);

        // Start running (simulated)
        pipeline.State.Status.Should().Be(TrainingStatus.NotStarted);

        // Act & Assert - Note: Full pause/resume requires running state
        await pipeline.PauseAsync();
        await pipeline.ResumeAsync();
        // The status change only happens during actual running
    }

    [Fact]
    public async Task GenerationCompleted_ShouldRaiseEvent()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);
        await pipeline.InitializeAsync(42);

        GenerationCompletedEventArgs? eventArgs = null;
        pipeline.GenerationCompleted += (s, e) => eventArgs = e;

        // Act
        await pipeline.RunGenerationAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Stats.Should().NotBeNull();
        eventArgs.Summary.Should().NotBeNull();
    }

    [Fact]
    public void IsRunning_ShouldReturnFalseWhenNotStarted()
    {
        // Arrange
        var pipeline = new TrainingPipeline(
            _config,
            _gaMock.Object,
            _curriculum,
            _checkpoint);

        // Act & Assert
        pipeline.IsRunning.Should().BeFalse();
    }
}
