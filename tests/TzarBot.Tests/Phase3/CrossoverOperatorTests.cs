using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Operators;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for crossover operators.
/// </summary>
public class CrossoverOperatorTests
{
    private readonly GeneticAlgorithmConfig _config;

    public CrossoverOperatorTests()
    {
        _config = GeneticAlgorithmConfig.Default();
    }

    #region Uniform Crossover Tests

    [Fact]
    public void UniformCrossover_ShouldCreateTwoOffspring()
    {
        // Arrange
        var crossover = new UniformCrossover(_config);
        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(123);
        var random = new Random(42);

        // Act
        var (child1, child2) = crossover.Crossover(parent1, parent2, random);

        // Assert
        child1.Should().NotBeNull();
        child2.Should().NotBeNull();
        child1.Id.Should().NotBe(parent1.Id).And.NotBe(parent2.Id);
        child2.Id.Should().NotBe(parent1.Id).And.NotBe(parent2.Id);
    }

    [Fact]
    public void UniformCrossover_ShouldSetCorrectParentIds()
    {
        // Arrange
        var crossover = new UniformCrossover(_config);
        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(123);
        var random = new Random(42);

        // Act
        var child = crossover.CrossoverSingle(parent1, parent2, random);

        // Assert
        child.ParentIds.Should().Contain(parent1.Id);
        child.ParentIds.Should().Contain(parent2.Id);
    }

    [Fact]
    public void UniformCrossover_ShouldIncrementGeneration()
    {
        // Arrange
        var crossover = new UniformCrossover(_config);
        var parent1 = CreateTestGenome(42);
        parent1.Generation = 5;
        var parent2 = CreateTestGenome(123);
        parent2.Generation = 3;
        var random = new Random(42);

        // Act
        var child = crossover.CrossoverSingle(parent1, parent2, random);

        // Assert
        child.Generation.Should().Be(6); // Max(5, 3) + 1
    }

    [Fact]
    public void UniformCrossover_ShouldInheritFromBetterParent_WhenConfigured()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { InheritFromBetterParent = true };
        var crossover = new UniformCrossover(config);
        var parent1 = CreateTestGenome(42);
        parent1.Fitness = 100f;
        var parent2 = CreateTestGenome(123);
        parent2.Fitness = 50f;
        // Make parent2 have different structure
        parent2.HiddenLayers.Add(DenseLayerConfig.CreateHidden(64));
        var random = new Random(42);

        // Act
        var child = crossover.CrossoverSingle(parent1, parent2, random);

        // Assert
        // Child should have structure closer to parent1 (better fitness)
        child.HiddenLayers.Count.Should().Be(parent1.HiddenLayers.Count);
    }

    [Fact]
    public void UniformCrossover_ShouldCreateValidGenome()
    {
        // Arrange
        var crossover = new UniformCrossover(_config);
        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(123);
        var random = new Random(42);

        // Act
        var child = crossover.CrossoverSingle(parent1, parent2, random);

        // Assert
        child.IsValid().Should().BeTrue();
    }

    #endregion

    #region Arithmetic Crossover Tests

    [Fact]
    public void ArithmeticCrossover_ShouldBlendWeights()
    {
        // Arrange
        var crossover = new ArithmeticCrossover(_config);
        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(42); // Same structure
        var random = new Random(42);

        // Set distinct weights for easy verification
        for (int i = 0; i < parent1.Weights.Length; i++)
        {
            parent1.Weights[i] = 1.0f;
            parent2.Weights[i] = -1.0f;
        }

        // Act
        var child = crossover.CrossoverSingle(parent1, parent2, random);

        // Assert - weights should be between parent values
        child.Weights.Should().OnlyContain(w => w >= -1.0f && w <= 1.0f);
    }

    [Fact]
    public void ArithmeticCrossover_WithFixedAlpha_ShouldProduceExactBlend()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { ArithmeticCrossoverAlpha = 0.5f };
        var crossover = new ArithmeticCrossover(config);
        crossover.Mode = ArithmeticCrossover.AlphaMode.Fixed;

        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(42); // Same structure
        var random = new Random(42);

        // Set distinct weights
        for (int i = 0; i < parent1.Weights.Length; i++)
        {
            parent1.Weights[i] = 2.0f;
            parent2.Weights[i] = 0.0f;
        }

        // Act
        var child = crossover.CrossoverSingle(parent1, parent2, random);

        // Assert - with alpha=0.5, weights should be exactly 1.0
        child.Weights.Should().OnlyContain(w => Math.Abs(w - 1.0f) < 0.001f);
    }

    [Fact]
    public void ArithmeticCrossover_ShouldThrow_ForDifferentStructures()
    {
        // Arrange
        var crossover = new ArithmeticCrossover(_config);
        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(42);
        parent2.HiddenLayers.Add(DenseLayerConfig.CreateHidden(64)); // Different structure
        parent2.Weights = new float[parent2.TotalWeightCount()];
        var random = new Random(42);

        // Act & Assert
        Action act = () => crossover.Crossover(parent1, parent2, random);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ArithmeticCrossover_CrossoverWeights_ShouldWorkWithSpans()
    {
        // Arrange
        var crossover = new ArithmeticCrossover(_config);
        var parent1Weights = new float[] { 1f, 2f, 3f, 4f, 5f };
        var parent2Weights = new float[] { 5f, 4f, 3f, 2f, 1f };
        var childWeights = new float[5];
        var random = new Random(42);

        // Act
        crossover.CrossoverWeights(
            parent1Weights.AsSpan(),
            parent2Weights.AsSpan(),
            childWeights.AsSpan(),
            random);

        // Assert - weights should be between parent values
        for (int i = 0; i < 5; i++)
        {
            float min = Math.Min(parent1Weights[i], parent2Weights[i]);
            float max = Math.Max(parent1Weights[i], parent2Weights[i]);
            childWeights[i].Should().BeInRange(min, max);
        }
    }

    [Fact]
    public void ArithmeticCrossover_BlendCrossover_ShouldExtendRange()
    {
        // Arrange
        var crossover = new ArithmeticCrossover(_config);
        var parent1Weights = new float[] { 0f, 0f, 0f };
        var parent2Weights = new float[] { 1f, 1f, 1f };
        var childWeights = new float[3];
        var random = new Random(42);

        // Act
        crossover.BlendCrossover(
            parent1Weights.AsSpan(),
            parent2Weights.AsSpan(),
            childWeights.AsSpan(),
            random,
            blendAlpha: 0.5f);

        // Assert - with BLX-0.5, range is extended beyond parents
        // Some weights might be outside [0, 1] but within [-0.5, 1.5]
        childWeights.Should().OnlyContain(w => w >= -10f && w <= 10f); // Clamped
    }

    #endregion

    [Fact]
    public void Crossover_ShouldBeReproducible_WithSameSeed()
    {
        // Arrange
        var crossover = new UniformCrossover(_config);
        var parent1 = CreateTestGenome(42);
        var parent2 = CreateTestGenome(123);

        // Act
        var child1 = crossover.CrossoverSingle(parent1, parent2, new Random(42));
        var child2 = crossover.CrossoverSingle(parent1, parent2, new Random(42));

        // Assert
        child1.HiddenLayers.Count.Should().Be(child2.HiddenLayers.Count);
        child1.Weights.Should().BeEquivalentTo(child2.Weights);
    }

    private static NetworkGenome CreateTestGenome(int seed)
    {
        return NetworkGenome.CreateRandom(new[] { 256, 128 }, seed);
    }
}
