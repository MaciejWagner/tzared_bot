using FluentAssertions;
using TzarBot.Orchestrator.Worker;

namespace TzarBot.Tests.Phase4;

/// <summary>
/// Tests for WorkerAgent and EvaluationTask
/// </summary>
public class WorkerAgentTests
{
    [Fact]
    public void EvaluationTask_CreatedWithRequiredProperties()
    {
        // Arrange
        var genomeData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var task = new EvaluationTask
        {
            GenomeId = "genome_001",
            GenomeData = genomeData,
            Priority = 1
        };

        // Assert
        task.GenomeId.Should().Be("genome_001");
        task.GenomeData.Should().BeEquivalentTo(genomeData);
        task.Priority.Should().Be(1);
    }

    [Fact]
    public void EvaluationTask_DefaultTimeout_Is10Minutes()
    {
        // Arrange & Act
        var task = new EvaluationTask
        {
            GenomeId = "test",
            GenomeData = new byte[] { 1 }
        };

        // Assert
        task.EvaluationTimeout.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void EvaluationTask_CreatedAt_DefaultsToCurrentTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var task = new EvaluationTask
        {
            GenomeId = "test",
            GenomeData = new byte[] { 1 }
        };

        // Assert
        task.CreatedAt.Should().BeOnOrAfter(before);
        task.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void EvaluationTask_ResultCallback_CanBeSet()
    {
        // Arrange
        bool callbackInvoked = false;

        // Act
        var task = new EvaluationTask
        {
            GenomeId = "test",
            GenomeData = new byte[] { 1 },
            ResultCallback = _ => callbackInvoked = true
        };

        task.ResultCallback?.Invoke(null!);

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkerState.Idle)]
    [InlineData(WorkerState.Initializing)]
    [InlineData(WorkerState.Evaluating)]
    [InlineData(WorkerState.Stopped)]
    [InlineData(WorkerState.Error)]
    public void WorkerState_AllValuesAreDefined(WorkerState state)
    {
        // Assert that each state is a valid enum value
        Enum.IsDefined(typeof(WorkerState), state).Should().BeTrue();
    }
}
