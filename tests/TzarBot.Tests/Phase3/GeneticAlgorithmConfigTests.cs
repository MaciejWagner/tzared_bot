using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for GeneticAlgorithmConfig validation and defaults.
/// </summary>
public class GeneticAlgorithmConfigTests
{
    [Fact]
    public void Default_ShouldBeValid()
    {
        // Arrange & Act
        var config = GeneticAlgorithmConfig.Default();

        // Assert
        config.IsValid().Should().BeTrue();
    }

    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var config = GeneticAlgorithmConfig.Default();

        // Assert
        config.PopulationSize.Should().Be(100);
        config.TournamentSize.Should().Be(3);
        config.ElitismRate.Should().Be(0.05f);
        config.CrossoverRate.Should().Be(0.7f);
        config.MutationRate.Should().Be(0.3f);
        config.WeightMutationRate.Should().Be(0.8f);
        config.MinWeight.Should().Be(-10f);
        config.MaxWeight.Should().Be(10f);
    }

    [Theory]
    [InlineData(5)]   // Below minimum
    [InlineData(1001)] // Above maximum
    public void IsValid_ShouldReturnFalse_ForInvalidPopulationSize(int size)
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = size };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void IsValid_ShouldReturnTrue_ForValidPopulationSize(int size)
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { PopulationSize = size };

        // Act & Assert
        config.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]   // Too small
    [InlineData(150)] // Larger than population
    public void IsValid_ShouldReturnFalse_ForInvalidTournamentSize(int tournamentSize)
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = 100,
            TournamentSize = tournamentSize
        };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void IsValid_ShouldReturnFalse_ForInvalidElitismRate(float rate)
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { ElitismRate = rate };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void IsValid_ShouldReturnFalse_ForInvalidCrossoverRate(float rate)
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { CrossoverRate = rate };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void IsValid_ShouldReturnFalse_ForInvalidMutationRate(float rate)
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { MutationRate = rate };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMinWeightGreaterThanMaxWeight()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            MinWeight = 10f,
            MaxWeight = -10f
        };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenWeightMutationStrengthIsZero()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig { WeightMutationStrength = 0f };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMinHiddenLayersGreaterThanMax()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig
        {
            MinHiddenLayers = 5,
            MaxHiddenLayers = 2
        };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var config = GeneticAlgorithmConfig.Default();

        // Act
        var result = config.ToString();

        // Assert
        result.Should().Contain("pop=100");
        result.Should().Contain("tour=3");
        result.Should().Contain("elite=");
    }
}
