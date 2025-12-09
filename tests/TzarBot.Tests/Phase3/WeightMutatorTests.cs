using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Operators;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for weight mutation operator.
/// </summary>
public class WeightMutatorTests
{
    private readonly GeneticAlgorithmConfig _config;
    private readonly WeightMutator _mutator;

    public WeightMutatorTests()
    {
        _config = new GeneticAlgorithmConfig
        {
            WeightMutationRate = 1.0f, // Always mutate for testing
            WeightPerturbationRate = 0.5f,
            WeightMutationStrength = 0.5f,
            WeightResetRate = 0.1f,
            MinWeight = -10f,
            MaxWeight = 10f
        };
        _mutator = new WeightMutator(_config);
    }

    [Fact]
    public void Mutate_ShouldModifyWeights_WhenMutationRateIsOne()
    {
        // Arrange
        var genome = CreateTestGenome();
        var originalWeights = genome.Weights.ToArray();
        var random = new Random(42);

        // Act
        bool mutated = _mutator.Mutate(genome, random);

        // Assert
        mutated.Should().BeTrue();
        genome.Weights.Should().NotBeEquivalentTo(originalWeights);
    }

    [Fact]
    public void Mutate_ShouldNotModifyWeights_WhenMutationRateIsZero()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { WeightMutationRate = 0f };
        var mutator = new WeightMutator(config);
        var genome = CreateTestGenome();
        var originalWeights = genome.Weights.ToArray();
        var random = new Random(42);

        // Act
        bool mutated = mutator.Mutate(genome, random);

        // Assert
        mutated.Should().BeFalse();
        genome.Weights.Should().BeEquivalentTo(originalWeights);
    }

    [Fact]
    public void Mutate_ShouldClampWeights_ToValidRange()
    {
        // Arrange
        var genome = CreateTestGenome();
        // Set some extreme weights
        genome.Weights[0] = 100f;
        genome.Weights[1] = -100f;
        var random = new Random(42);

        // Act
        _mutator.Mutate(genome, random);

        // Assert
        genome.Weights.Should().OnlyContain(w =>
            w >= _config.MinWeight && w <= _config.MaxWeight);
    }

    [Fact]
    public void MutateRange_ShouldOnlyAffectSpecifiedRange()
    {
        // Arrange
        var weights = new float[100];
        for (int i = 0; i < 100; i++) weights[i] = 1.0f;
        var random = new Random(42);

        // Act
        int mutated = _mutator.MutateRange(weights, 20, 30, random);

        // Assert - weights outside range should be unchanged
        for (int i = 0; i < 20; i++)
        {
            weights[i].Should().Be(1.0f);
        }
        for (int i = 50; i < 100; i++)
        {
            weights[i].Should().Be(1.0f);
        }
    }

    [Fact]
    public void ValidateWeights_ShouldReturnTrue_ForValidWeights()
    {
        // Arrange
        var weights = new float[] { 0f, 1f, -1f, 5f, -5f, 9.9f, -9.9f };

        // Act & Assert
        _mutator.ValidateWeights(weights).Should().BeTrue();
    }

    [Fact]
    public void ValidateWeights_ShouldReturnFalse_ForNaN()
    {
        // Arrange
        var weights = new float[] { 0f, float.NaN, 1f };

        // Act & Assert
        _mutator.ValidateWeights(weights).Should().BeFalse();
    }

    [Fact]
    public void ValidateWeights_ShouldReturnFalse_ForInfinity()
    {
        // Arrange
        var weights = new float[] { 0f, float.PositiveInfinity, 1f };

        // Act & Assert
        _mutator.ValidateWeights(weights).Should().BeFalse();
    }

    [Fact]
    public void ValidateWeights_ShouldReturnFalse_ForOutOfRange()
    {
        // Arrange
        var weights = new float[] { 0f, 15f, 1f }; // 15 > MaxWeight

        // Act & Assert
        _mutator.ValidateWeights(weights).Should().BeFalse();
    }

    [Fact]
    public void ClampWeights_ShouldFixNaNAndInfinity()
    {
        // Arrange
        var weights = new float[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 5f };

        // Act
        _mutator.ClampWeights(weights);

        // Assert
        weights[0].Should().Be(0f);
        weights[1].Should().Be(0f);
        weights[2].Should().Be(0f);
        weights[3].Should().Be(5f);
    }

    [Fact]
    public void ClampWeights_ShouldClampToRange()
    {
        // Arrange
        var weights = new float[] { -20f, 20f, 5f };

        // Act
        _mutator.ClampWeights(weights);

        // Assert
        weights[0].Should().Be(_config.MinWeight);
        weights[1].Should().Be(_config.MaxWeight);
        weights[2].Should().Be(5f);
    }

    [Fact]
    public void Mutate_ShouldBeReproducible_WithSameSeed()
    {
        // Arrange
        var genome1 = CreateTestGenome();
        var genome2 = CreateTestGenome();

        // Ensure same initial weights
        Array.Copy(genome1.Weights, genome2.Weights, genome1.Weights.Length);

        // Act
        _mutator.Mutate(genome1, new Random(42));
        _mutator.Mutate(genome2, new Random(42));

        // Assert
        genome1.Weights.Should().BeEquivalentTo(genome2.Weights);
    }

    private static NetworkGenome CreateTestGenome()
    {
        return NetworkGenome.CreateRandom(new[] { 128, 64 }, 42);
    }
}
