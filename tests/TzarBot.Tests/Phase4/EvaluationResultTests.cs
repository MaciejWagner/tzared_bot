using FluentAssertions;
using TzarBot.Orchestrator.Communication;

namespace TzarBot.Tests.Phase4;

/// <summary>
/// Tests for EvaluationResult and related communication types
/// </summary>
public class EvaluationResultTests
{
    [Fact]
    public void EvaluationResult_SuccessfulEvaluation_HasCorrectProperties()
    {
        // Arrange & Act
        var result = new EvaluationResult
        {
            EvaluationId = Guid.NewGuid(),
            VMName = "TzarBot-Worker-0",
            GenomeId = "genome_001",
            Success = true,
            FitnessScore = 0.85,
            Outcome = GameOutcome.Win,
            EvaluationDuration = TimeSpan.FromMinutes(5)
        };

        // Assert
        result.Success.Should().BeTrue();
        result.FitnessScore.Should().Be(0.85);
        result.Outcome.Should().Be(GameOutcome.Win);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void EvaluationResult_FailedEvaluation_HasErrorMessage()
    {
        // Arrange & Act
        var result = new EvaluationResult
        {
            EvaluationId = Guid.NewGuid(),
            VMName = "TzarBot-Worker-0",
            GenomeId = "genome_002",
            Success = false,
            ErrorMessage = "Game crashed during evaluation",
            Outcome = GameOutcome.Error
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Outcome.Should().Be(GameOutcome.Error);
        result.FitnessScore.Should().Be(0);
    }

    [Fact]
    public void EvaluationResult_CompletedAt_DefaultsToCurrentTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var result = new EvaluationResult
        {
            VMName = "TestVM",
            GenomeId = "test"
        };

        // Assert
        result.CompletedAt.Should().BeOnOrAfter(before);
        result.CompletedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void GameMetrics_TracksAllExpectedFields()
    {
        // Arrange & Act
        var metrics = new GameMetrics
        {
            GameDurationSeconds = 300,
            UnitsCreated = 50,
            UnitsLost = 10,
            EnemyUnitsDestroyed = 30,
            ResourcesGathered = 5000,
            BuildingsConstructed = 8,
            ActionsPerMinute = 45.5
        };

        // Assert
        metrics.GameDurationSeconds.Should().Be(300);
        metrics.UnitsCreated.Should().Be(50);
        metrics.UnitsLost.Should().Be(10);
        metrics.EnemyUnitsDestroyed.Should().Be(30);
        metrics.ResourcesGathered.Should().Be(5000);
        metrics.BuildingsConstructed.Should().Be(8);
        metrics.ActionsPerMinute.Should().Be(45.5);
    }

    [Theory]
    [InlineData(GameOutcome.Win)]
    [InlineData(GameOutcome.Loss)]
    [InlineData(GameOutcome.Draw)]
    [InlineData(GameOutcome.Timeout)]
    [InlineData(GameOutcome.Error)]
    [InlineData(GameOutcome.Unknown)]
    public void GameOutcome_AllValuesAreDefined(GameOutcome outcome)
    {
        // Assert that each outcome is a valid enum value
        Enum.IsDefined(typeof(GameOutcome), outcome).Should().BeTrue();
    }

    [Theory]
    [InlineData(BotServiceStatus.Running)]
    [InlineData(BotServiceStatus.Stopped)]
    [InlineData(BotServiceStatus.Starting)]
    [InlineData(BotServiceStatus.EvaluatingGenome)]
    [InlineData(BotServiceStatus.Error)]
    [InlineData(BotServiceStatus.Unknown)]
    public void BotServiceStatus_AllValuesAreDefined(BotServiceStatus status)
    {
        // Assert that each status is a valid enum value
        Enum.IsDefined(typeof(BotServiceStatus), status).Should().BeTrue();
    }
}
