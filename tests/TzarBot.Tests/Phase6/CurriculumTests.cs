using FluentAssertions;
using TzarBot.GeneticAlgorithm.Fitness;
using TzarBot.Training.Core;
using TzarBot.Training.Curriculum;

namespace TzarBot.Tests.Phase6;

/// <summary>
/// Tests for the CurriculumManager class.
/// </summary>
public class CurriculumTests
{
    [Fact]
    public void Constructor_WithDefaultStages_ShouldStartAtBootstrap()
    {
        // Arrange & Act
        var manager = new CurriculumManager();

        // Assert
        manager.CurrentStage.Name.Should().Be("Bootstrap");
        manager.AllStages.Should().HaveCount(6);
    }

    [Fact]
    public void Constructor_WithCustomStages_ShouldUseFirstStage()
    {
        // Arrange
        var stages = StageDefinitions.GetSimplifiedStages();

        // Act
        var manager = new CurriculumManager(stages);

        // Assert
        manager.CurrentStage.Name.Should().Be("Bootstrap");
        manager.AllStages.Should().HaveCount(3);
    }

    [Fact]
    public void SetStage_ShouldChangeCurrentStage()
    {
        // Arrange
        var manager = new CurriculumManager();

        // Act
        manager.SetStage("Basic");

        // Assert
        manager.CurrentStage.Name.Should().Be("Basic");
    }

    [Fact]
    public void SetStage_WithUnknownStage_ShouldThrow()
    {
        // Arrange
        var manager = new CurriculumManager();

        // Act & Assert
        var act = () => manager.SetStage("UnknownStage");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateMetrics_ShouldTrackGenerationsInStage()
    {
        // Arrange
        var manager = new CurriculumManager();
        var stats = CreateStats(generation: 1, avgFitness: 30f, winRate: 0.5f);

        // Act
        manager.UpdateMetrics(stats);

        // Assert
        manager.CurrentMetrics.GenerationsInStage.Should().Be(1);
    }

    [Fact]
    public void UpdateMetrics_ShouldAccumulateWinsAndGames()
    {
        // Arrange
        var manager = new CurriculumManager();

        // Act
        manager.UpdateMetrics(CreateStats(1, 30f, 0.5f, gamesPlayed: 10, wins: 5));
        manager.UpdateMetrics(CreateStats(2, 35f, 0.6f, gamesPlayed: 10, wins: 6));

        // Assert
        manager.CurrentMetrics.TotalGames.Should().Be(20);
        manager.CurrentMetrics.TotalWins.Should().Be(11);
    }

    [Fact]
    public void EvaluateTransition_ShouldReturnNone_WhenCriteriaNotMet()
    {
        // Arrange
        var manager = new CurriculumManager();
        manager.UpdateMetrics(CreateStats(1, 10f, 0f));

        // Act
        var (type, reason) = manager.EvaluateTransition();

        // Assert
        type.Should().Be(StageTransitionType.None);
    }

    [Fact]
    public void EvaluateTransition_ShouldReturnPromotion_WhenCriteriaMet()
    {
        // Arrange
        var stages = new List<CurriculumStage>
        {
            new CurriculumStage
            {
                Name = "Test",
                Order = 0,
                MinGenerationsInStage = 1,
                PromotionCriteria = new PromotionCriteria
                {
                    MinWinRate = 0f,
                    MinAverageFitness = 10f,
                    MinBestFitness = 20f,
                    ConsecutiveGenerations = 1
                }
            },
            new CurriculumStage
            {
                Name = "Next",
                Order = 1,
                IsFinalStage = true
            }
        };
        var manager = new CurriculumManager(stages);

        // Update with stats that meet criteria
        manager.UpdateMetrics(CreateStats(1, 50f, 0.5f, bestFitness: 100f));
        manager.UpdateMetrics(CreateStats(2, 60f, 0.6f, bestFitness: 120f));

        // Act
        var (type, reason) = manager.EvaluateTransition();

        // Assert
        type.Should().Be(StageTransitionType.Promotion);
    }

    [Fact]
    public void TryPromote_ShouldAdvanceStage()
    {
        // Arrange
        var manager = new CurriculumManager();

        // Act
        var result = manager.TryPromote("Test promotion");

        // Assert
        result.Should().BeTrue();
        manager.CurrentStage.Name.Should().Be("Basic");
        manager.TransitionHistory.Should().HaveCount(1);
    }

    [Fact]
    public void TryPromote_ShouldFail_WhenAtFinalStage()
    {
        // Arrange
        var manager = new CurriculumManager();
        manager.SetStage("Tournament");

        // Act
        var result = manager.TryPromote("Test promotion");

        // Assert
        result.Should().BeFalse();
        manager.CurrentStage.Name.Should().Be("Tournament");
    }

    [Fact]
    public void TryDemote_ShouldReverseStage()
    {
        // Arrange
        var manager = new CurriculumManager(allowDemotion: true);
        manager.SetStage("Basic");

        // Act
        var result = manager.TryDemote("Test demotion");

        // Assert
        result.Should().BeTrue();
        manager.CurrentStage.Name.Should().Be("Bootstrap");
    }

    [Fact]
    public void TryDemote_ShouldFail_WhenAtFirstStage()
    {
        // Arrange
        var manager = new CurriculumManager(allowDemotion: true);

        // Act
        var result = manager.TryDemote("Test demotion");

        // Assert
        result.Should().BeFalse();
        manager.CurrentStage.Name.Should().Be("Bootstrap");
    }

    [Fact]
    public void TryDemote_ShouldFail_WhenDemotionDisabled()
    {
        // Arrange
        var manager = new CurriculumManager(allowDemotion: false);
        manager.SetStage("Basic");

        // Act
        var result = manager.TryDemote("Test demotion");

        // Assert
        result.Should().BeFalse();
        manager.CurrentStage.Name.Should().Be("Basic");
    }

    [Fact]
    public void GetCurrentFitnessWeights_ShouldReturnStageWeights()
    {
        // Arrange
        var manager = new CurriculumManager();

        // Act
        var weights = manager.GetCurrentFitnessWeights();

        // Assert
        weights.Should().NotBeNull();
        // Bootstrap stage has high activity weight
        weights.ActivityWeight.Should().Be(3f);
    }

    [Fact]
    public void ResetStageMetrics_ShouldClearMetrics()
    {
        // Arrange
        var manager = new CurriculumManager();
        manager.UpdateMetrics(CreateStats(1, 50f, 0.5f, 10, 5));

        // Act
        manager.ResetStageMetrics();

        // Assert
        manager.CurrentMetrics.GenerationsInStage.Should().Be(0);
        manager.CurrentMetrics.TotalGames.Should().Be(0);
        manager.CurrentMetrics.TotalWins.Should().Be(0);
    }

    [Fact]
    public void StageChanged_ShouldRaiseEvent()
    {
        // Arrange
        var manager = new CurriculumManager();
        StageTransitionRecord? eventRecord = null;
        manager.StageChanged += (s, e) => eventRecord = e;

        // Act
        manager.TryPromote("Test");

        // Assert
        eventRecord.Should().NotBeNull();
        eventRecord!.FromStage.Should().Be("Bootstrap");
        eventRecord.ToStage.Should().Be("Basic");
        eventRecord.Type.Should().Be(StageTransitionType.Promotion);
    }

    [Fact]
    public void GetCurrentFitnessWeights_BootstrapStage_ShouldHaveHighActivityWeight()
    {
        // Bootstrap stage should prioritize activity (high ActivityWeight)
        var manager = new CurriculumManager();
        manager.SetStage("Bootstrap");

        var weights = manager.GetCurrentFitnessWeights();

        weights.Should().NotBeNull();
        weights.ActivityWeight.Should().BeGreaterThan(1f);
        weights.WinBonus.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetCurrentFitnessWeights_CombatStage_ShouldHaveHighCombatWeight()
    {
        // Combat-focused stages should prioritize combat
        var manager = new CurriculumManager();
        manager.SetStage("CombatEasy");

        var weights = manager.GetCurrentFitnessWeights();

        weights.Should().NotBeNull();
        weights.CombatWeight.Should().BeGreaterThan(1f);
    }

    private static GenerationStats CreateStats(
        int generation,
        float avgFitness,
        float winRate,
        int gamesPlayed = 10,
        int wins = 5,
        float bestFitness = 0)
    {
        return new GenerationStats
        {
            Generation = generation,
            AverageFitness = avgFitness,
            BestFitness = bestFitness > 0 ? bestFitness : avgFitness * 1.5f,
            WorstFitness = avgFitness * 0.5f,
            FitnessStdDev = avgFitness * 0.2f,
            GamesPlayed = gamesPlayed,
            Wins = wins,
            BestGenomeId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };
    }
}
