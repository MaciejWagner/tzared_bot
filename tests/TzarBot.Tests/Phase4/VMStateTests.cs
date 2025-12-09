using FluentAssertions;
using TzarBot.Orchestrator.VM;

namespace TzarBot.Tests.Phase4;

/// <summary>
/// Tests for VMState and VMInfo classes
/// </summary>
public class VMStateTests
{
    [Theory]
    [InlineData(VMState.Running, VMHealthStatus.Healthy, true)]
    [InlineData(VMState.Running, VMHealthStatus.NoHeartbeat, false)]
    [InlineData(VMState.Running, VMHealthStatus.Unknown, false)]
    [InlineData(VMState.Off, VMHealthStatus.Healthy, false)]
    [InlineData(VMState.Paused, VMHealthStatus.Healthy, false)]
    [InlineData(VMState.Starting, VMHealthStatus.Unknown, false)]
    public void VMInfo_IsReady_ReturnsCorrectValue(VMState state, VMHealthStatus health, bool expectedReady)
    {
        // Arrange
        var vmInfo = new VMInfo
        {
            Name = "TestVM",
            State = state,
            HealthStatus = health
        };

        // Act & Assert
        vmInfo.IsReady.Should().Be(expectedReady);
    }

    [Theory]
    [InlineData(1073741824L, 1.0)]  // 1 GB
    [InlineData(2147483648L, 2.0)]  // 2 GB
    [InlineData(4294967296L, 4.0)]  // 4 GB
    [InlineData(0L, 0.0)]           // 0 bytes
    public void VMInfo_MemoryAssignedGB_CalculatesCorrectly(long bytes, double expectedGB)
    {
        // Arrange
        var vmInfo = new VMInfo
        {
            Name = "TestVM",
            MemoryAssignedBytes = bytes
        };

        // Act & Assert
        vmInfo.MemoryAssignedGB.Should().Be(expectedGB);
    }

    [Fact]
    public void VMInfo_LastUpdated_DefaultsToCurrentTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var vmInfo = new VMInfo
        {
            Name = "TestVM"
        };

        // Assert
        vmInfo.LastUpdated.Should().BeOnOrAfter(before);
        vmInfo.LastUpdated.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void VMState_HasCorrectEnumValues()
    {
        // These values match Hyper-V state codes
        ((int)VMState.Off).Should().Be(2);
        ((int)VMState.Running).Should().Be(3);
        ((int)VMState.Saved).Should().Be(6);
        ((int)VMState.Paused).Should().Be(9);
    }
}
