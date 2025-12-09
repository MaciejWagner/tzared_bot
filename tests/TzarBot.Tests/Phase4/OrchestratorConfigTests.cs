using FluentAssertions;
using TzarBot.Orchestrator.Service;

namespace TzarBot.Tests.Phase4;

/// <summary>
/// Tests for OrchestratorConfig
/// </summary>
public class OrchestratorConfigTests
{
    [Fact]
    public void OrchestratorConfig_DefaultWorkerCount_RespectsRAMLimit()
    {
        // Arrange & Act
        var config = new OrchestratorConfig();

        // Assert
        // Default should be 3 workers (3 x 2GB = 6GB, fitting in worker pool)
        config.WorkerCount.Should().Be(3,
            "because with 6GB worker pool and 2GB per worker, 3 workers is the default");
    }

    [Fact]
    public void OrchestratorConfig_MaxParallelEvaluations_MatchesWorkerCount()
    {
        // Arrange & Act
        var config = new OrchestratorConfig();

        // Assert
        config.MaxParallelEvaluations.Should().Be(config.WorkerCount,
            "because we can only do as many parallel evaluations as we have workers");
    }

    [Fact]
    public void OrchestratorConfig_DefaultTimeout_IsReasonable()
    {
        // Arrange & Act
        var config = new OrchestratorConfig();

        // Assert
        config.DefaultEvaluationTimeout.Should().BeGreaterThan(TimeSpan.Zero);
        config.DefaultEvaluationTimeout.TotalMinutes.Should().BeLessThanOrEqualTo(30,
            "because a game should complete within 30 minutes");
    }

    [Fact]
    public void OrchestratorConfig_AutoRecovery_IsEnabledByDefault()
    {
        // Arrange & Act
        var config = new OrchestratorConfig();

        // Assert
        config.AutoRecoverWorkers.Should().BeTrue();
        config.MaxRecoveryAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public void OrchestratorConfig_SectionName_IsCorrect()
    {
        // Assert
        OrchestratorConfig.SectionName.Should().Be("Orchestrator");
    }

    [Fact]
    public void OrchestratorConfig_DefaultPaths_AreSet()
    {
        // Arrange & Act
        var config = new OrchestratorConfig();

        // Assert
        config.LogPath.Should().NotBeNullOrEmpty();
        config.ResultsPath.Should().NotBeNullOrEmpty();
    }
}
