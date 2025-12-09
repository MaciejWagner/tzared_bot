using FluentAssertions;
using TzarBot.GeneticAlgorithm.Fitness;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for fitness calculation.
/// </summary>
public class FitnessCalculatorTests
{
    private readonly GameFitnessCalculator _calculator;

    public FitnessCalculatorTests()
    {
        _calculator = new GameFitnessCalculator();
    }

    [Fact]
    public void Calculate_WinningGame_ShouldHaveHighFitness()
    {
        // Arrange
        var result = new GameResult
        {
            Won = true,
            DurationSeconds = 300f, // 5 minutes
            MaxDurationSeconds = 3600f,
            UnitsBuilt = 50,
            UnitsKilled = 30,
            BuildingsBuilt = 10,
            ValidActions = 1000,
            TotalFrames = 5000
        };

        // Act
        float fitness = _calculator.Calculate(result);

        // Assert
        fitness.Should().BeGreaterThan(50f); // Win bonus should dominate
    }

    [Fact]
    public void Calculate_LosingGame_ShouldHaveLowerFitness()
    {
        // Arrange
        var winResult = new GameResult { Won = true, DurationSeconds = 300f };
        var loseResult = new GameResult { Won = false, DurationSeconds = 300f };

        // Act
        float winFitness = _calculator.Calculate(winResult);
        float loseFitness = _calculator.Calculate(loseResult);

        // Assert
        winFitness.Should().BeGreaterThan(loseFitness);
    }

    [Fact]
    public void Calculate_FasterWin_ShouldHaveHigherFitness()
    {
        // Arrange
        var fastWin = new GameResult
        {
            Won = true,
            DurationSeconds = 60f, // 1 minute
            MaxDurationSeconds = 3600f
        };
        var slowWin = new GameResult
        {
            Won = true,
            DurationSeconds = 1800f, // 30 minutes
            MaxDurationSeconds = 3600f
        };

        // Act
        float fastFitness = _calculator.Calculate(fastWin);
        float slowFitness = _calculator.Calculate(slowWin);

        // Assert
        fastFitness.Should().BeGreaterThan(slowFitness);
    }

    [Fact]
    public void Calculate_HighInactivity_ShouldBePenalized()
    {
        // Arrange
        var activeResult = new GameResult
        {
            Won = false,
            IdleFrames = 100,
            TotalFrames = 1000, // 10% idle
            ValidActions = 500
        };
        var inactiveResult = new GameResult
        {
            Won = false,
            IdleFrames = 900,
            TotalFrames = 1000, // 90% idle
            ValidActions = 50
        };

        // Act
        float activeFitness = _calculator.Calculate(activeResult);
        float inactiveFitness = _calculator.Calculate(inactiveResult);

        // Assert
        activeFitness.Should().BeGreaterThan(inactiveFitness);
    }

    [Fact]
    public void Calculate_HighInvalidActions_ShouldBePenalized()
    {
        // Arrange
        var validResult = new GameResult
        {
            Won = false,
            ValidActions = 900,
            InvalidActions = 100, // 10% invalid
            TotalFrames = 1000
        };
        var invalidResult = new GameResult
        {
            Won = false,
            ValidActions = 100,
            InvalidActions = 900, // 90% invalid
            TotalFrames = 1000
        };

        // Act
        float validFitness = _calculator.Calculate(validResult);
        float invalidFitness = _calculator.Calculate(invalidResult);

        // Assert
        validFitness.Should().BeGreaterThan(invalidFitness);
    }

    [Fact]
    public void Calculate_MoreUnitsKilled_ShouldHaveHigherFitness()
    {
        // Arrange
        var lowKills = new GameResult
        {
            Won = false,
            UnitsKilled = 5,
            UnitsLost = 10
        };
        var highKills = new GameResult
        {
            Won = false,
            UnitsKilled = 50,
            UnitsLost = 10
        };

        // Act
        float lowFitness = _calculator.Calculate(lowKills);
        float highFitness = _calculator.Calculate(highKills);

        // Assert
        highFitness.Should().BeGreaterThan(lowFitness);
    }

    [Fact]
    public void Calculate_MoreBuildings_ShouldHaveHigherFitness()
    {
        // Arrange
        var fewBuildings = new GameResult
        {
            Won = false,
            BuildingsBuilt = 2,
            BuildingsDestroyed = 0
        };
        var manyBuildings = new GameResult
        {
            Won = false,
            BuildingsBuilt = 20,
            BuildingsDestroyed = 5
        };

        // Act
        float fewFitness = _calculator.Calculate(fewBuildings);
        float manyFitness = _calculator.Calculate(manyBuildings);

        // Assert
        manyFitness.Should().BeGreaterThan(fewFitness);
    }

    [Fact]
    public void Calculate_QuickLoss_ShouldBeHeavilyPenalized()
    {
        // Arrange
        var quickLoss = new GameResult
        {
            Won = false,
            DurationSeconds = 30f // Less than 60 seconds
        };
        var normalLoss = new GameResult
        {
            Won = false,
            DurationSeconds = 300f
        };

        // Act
        float quickFitness = _calculator.Calculate(quickLoss);
        float normalFitness = _calculator.Calculate(normalLoss);

        // Assert
        quickFitness.Should().BeLessThan(normalFitness);
    }

    [Fact]
    public void Calculate_ShouldRespectMinimumFitness()
    {
        // Arrange
        var terribleResult = new GameResult
        {
            Won = false,
            DurationSeconds = 10f, // Quick loss
            IdleFrames = 999,
            TotalFrames = 1000, // 99.9% idle
            InvalidActions = 1000,
            ValidActions = 0
        };

        // Act
        float fitness = _calculator.Calculate(terribleResult);

        // Assert
        fitness.Should().BeGreaterThanOrEqualTo(-100f); // Default minimum
    }

    [Fact]
    public void CalculateAverage_ShouldReturnCorrectAverage()
    {
        // Arrange
        var results = new[]
        {
            new GameResult { Won = true, DurationSeconds = 300f },
            new GameResult { Won = false, DurationSeconds = 300f },
            new GameResult { Won = true, DurationSeconds = 600f }
        };

        // Act
        float avgFitness = _calculator.CalculateAverage(results);

        // Assert
        var individual = results.Select(r => _calculator.Calculate(r)).ToArray();
        float expectedAvg = individual.Average();
        avgFitness.Should().BeApproximately(expectedAvg, 0.001f);
    }

    [Fact]
    public void CalculateWeightedAverage_ShouldWeightRecentHigher()
    {
        // Arrange
        // First games are losses, last game is a win
        var results = new List<GameResult>
        {
            new() { Won = false, DurationSeconds = 300f },
            new() { Won = false, DurationSeconds = 300f },
            new() { Won = false, DurationSeconds = 300f },
            new() { Won = true, DurationSeconds = 300f } // Last = most recent
        };

        // Act
        float avgFitness = _calculator.CalculateAverage(results);
        float weightedAvgFitness = _calculator.CalculateWeightedAverage(results, weightRecent: true);

        // Assert - weighted should be higher because win is more recent
        weightedAvgFitness.Should().BeGreaterThan(avgFitness);
    }

    [Fact]
    public void GameResult_InactivityRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new GameResult
        {
            IdleFrames = 250,
            TotalFrames = 1000
        };

        // Act & Assert
        result.InactivityRatio.Should().Be(0.25f);
    }

    [Fact]
    public void GameResult_InvalidActionRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new GameResult
        {
            ValidActions = 700,
            InvalidActions = 300
        };

        // Act & Assert
        result.InvalidActionRatio.Should().Be(0.3f);
    }

    [Fact]
    public void GameResult_UnitKDRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new GameResult
        {
            UnitsKilled = 50,
            UnitsLost = 10
        };

        // Act & Assert
        result.UnitKDRatio.Should().Be(5f);
    }

    [Fact]
    public void GameResult_TimeEfficiency_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new GameResult
        {
            DurationSeconds = 900f, // 15 minutes
            MaxDurationSeconds = 3600f // 1 hour
        };

        // Act & Assert
        result.TimeEfficiency.Should().Be(0.75f); // 1 - (900/3600) = 0.75
    }

    [Fact]
    public void FitnessWeights_EarlyTraining_ShouldBeMoreForgiving()
    {
        // Arrange
        var defaultWeights = FitnessWeights.Default();
        var earlyWeights = FitnessWeights.EarlyTraining();

        // Assert
        earlyWeights.InactivityPenalty.Should().BeLessThan(defaultWeights.InactivityPenalty);
        earlyWeights.InvalidActionPenalty.Should().BeLessThan(defaultWeights.InvalidActionPenalty);
        earlyWeights.LossPenalty.Should().BeLessThan(defaultWeights.LossPenalty);
    }

    [Fact]
    public void FitnessWeights_Competitive_ShouldBeStricter()
    {
        // Arrange
        var defaultWeights = FitnessWeights.Default();
        var competitiveWeights = FitnessWeights.Competitive();

        // Assert
        competitiveWeights.WinBonus.Should().BeGreaterThan(defaultWeights.WinBonus);
        competitiveWeights.InactivityPenalty.Should().BeGreaterThan(defaultWeights.InactivityPenalty);
    }
}
