using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Operators;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for structure mutation operator.
/// </summary>
public class StructureMutatorTests
{
    private readonly GeneticAlgorithmConfig _config;
    private readonly StructureMutator _mutator;

    public StructureMutatorTests()
    {
        _config = new GeneticAlgorithmConfig
        {
            StructureMutationRate = 1.0f, // Always mutate for testing
            AddLayerProbability = 0.5f,
            NeuronCountMutationRate = 1.0f,
            MaxNeuronCountDelta = 32,
            MinHiddenLayers = 1,
            MaxHiddenLayers = 5
        };
        _mutator = new StructureMutator(_config);
    }

    [Fact]
    public void Mutate_ShouldModifyStructure_WhenMutationRateIsOne()
    {
        // Arrange
        var genome = CreateTestGenome();
        var originalLayerCount = genome.HiddenLayers.Count;
        var originalNeuronCounts = genome.HiddenLayers.Select(l => l.NeuronCount).ToList();
        var random = new Random(42);

        // Act
        bool mutated = _mutator.Mutate(genome, random);

        // Assert
        mutated.Should().BeTrue();
        // Either layer count changed or neuron counts changed
        var structureChanged = genome.HiddenLayers.Count != originalLayerCount ||
                              !genome.HiddenLayers.Select(l => l.NeuronCount).SequenceEqual(originalNeuronCounts);
        structureChanged.Should().BeTrue();
    }

    [Fact]
    public void Mutate_ShouldNotExceedMaxLayers()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            StructureMutationRate = 1.0f,
            AddLayerProbability = 1.0f, // Always add
            MaxHiddenLayers = 3,
            MinHiddenLayers = 1,
            NeuronCountMutationRate = 0f
        };
        var mutator = new StructureMutator(config);
        var genome = CreateTestGenome();
        var random = new Random(42);

        // Act - try to add layers multiple times
        for (int i = 0; i < 10; i++)
        {
            mutator.Mutate(genome, random);
        }

        // Assert
        genome.HiddenLayers.Count.Should().BeLessThanOrEqualTo(config.MaxHiddenLayers);
    }

    [Fact]
    public void Mutate_ShouldNotGoBelowMinLayers()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            StructureMutationRate = 1.0f,
            AddLayerProbability = 0.0f, // Always remove
            MaxHiddenLayers = 5,
            MinHiddenLayers = 1,
            NeuronCountMutationRate = 0f
        };
        var mutator = new StructureMutator(config);
        var genome = CreateTestGenome();
        var random = new Random(42);

        // Act - try to remove layers multiple times
        for (int i = 0; i < 10; i++)
        {
            mutator.Mutate(genome, random);
        }

        // Assert
        genome.HiddenLayers.Count.Should().BeGreaterThanOrEqualTo(config.MinHiddenLayers);
    }

    [Fact]
    public void Mutate_ShouldMaintainValidWeightCount()
    {
        // Arrange
        var genome = CreateTestGenome();
        var random = new Random(42);

        // Act
        _mutator.Mutate(genome, random);

        // Assert
        var expectedWeights = genome.TotalWeightCount();
        genome.Weights.Length.Should().Be(expectedWeights);
    }

    [Fact]
    public void Mutate_ShouldKeepNeuronCountsInValidRange()
    {
        // Arrange
        var genome = CreateTestGenome();
        var random = new Random(42);

        // Act - mutate multiple times
        for (int i = 0; i < 20; i++)
        {
            _mutator.Mutate(genome, random);
        }

        // Assert
        genome.HiddenLayers.Should().OnlyContain(l =>
            l.NeuronCount >= DenseLayerConfig.MinNeurons &&
            l.NeuronCount <= DenseLayerConfig.MaxNeurons);
    }

    [Fact]
    public void ValidateStructure_ShouldReturnTrue_ForValidGenome()
    {
        // Arrange
        var genome = CreateTestGenome();

        // Act & Assert
        _mutator.ValidateStructure(genome).Should().BeTrue();
    }

    [Fact]
    public void ValidateStructure_ShouldReturnFalse_WhenTooFewLayers()
    {
        // Arrange
        var genome = CreateTestGenome();
        genome.HiddenLayers.Clear(); // Remove all layers

        // Act & Assert
        _mutator.ValidateStructure(genome).Should().BeFalse();
    }

    [Fact]
    public void ValidateStructure_ShouldReturnFalse_WhenTooManyLayers()
    {
        // Arrange
        var genome = CreateTestGenome();
        // Add layers until exceeding max
        while (genome.HiddenLayers.Count <= _config.MaxHiddenLayers)
        {
            genome.HiddenLayers.Add(DenseLayerConfig.CreateHidden(128));
        }

        // Act & Assert
        _mutator.ValidateStructure(genome).Should().BeFalse();
    }

    [Fact]
    public void GetTotalNeuronCount_ShouldReturnCorrectSum()
    {
        // Arrange
        var genome = CreateTestGenome();
        int expectedCount = genome.HiddenLayers.Sum(l => l.NeuronCount);

        // Act
        int count = StructureMutator.GetTotalNeuronCount(genome);

        // Assert
        count.Should().Be(expectedCount);
    }

    [Fact]
    public void Mutate_ShouldBeReproducible_WithSameSeed()
    {
        // Arrange
        var genome1 = CreateTestGenome();
        var genome2 = CreateTestGenome();

        // Act
        _mutator.Mutate(genome1, new Random(42));
        _mutator.Mutate(genome2, new Random(42));

        // Assert - structure should be the same
        genome1.HiddenLayers.Count.Should().Be(genome2.HiddenLayers.Count);
        for (int i = 0; i < genome1.HiddenLayers.Count; i++)
        {
            genome1.HiddenLayers[i].NeuronCount.Should().Be(genome2.HiddenLayers[i].NeuronCount);
        }
    }

    private static NetworkGenome CreateTestGenome()
    {
        return NetworkGenome.CreateRandom(new[] { 256, 128 }, 42);
    }
}
