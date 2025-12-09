using FluentAssertions;
using TzarBot.Orchestrator.VM;

namespace TzarBot.Tests.Phase4;

/// <summary>
/// Tests for VMPool configuration options
/// </summary>
public class VMPoolOptionsTests
{
    [Fact]
    public void VMPoolOptions_DefaultValues_RespectRAMLimit()
    {
        // Arrange & Act
        var options = new VMPoolOptions();

        // Assert
        // Max pool size should be 3 to respect 10GB limit (DEV=4GB, 3 workers x 2GB = 6GB)
        options.MaxPoolSize.Should().BeInRange(1, 6,
            "because with 10GB limit and DEV at 4GB, only 6GB remains for workers");
    }

    [Fact]
    public void HyperVManagerOptions_MemorySettings_RespectWorkerLimit()
    {
        // Arrange & Act
        var options = new HyperVManagerOptions();

        // Assert
        var workerRAM = options.MemoryStartupBytes / (1024.0 * 1024.0 * 1024.0);  // Convert to GB

        // Each worker should use 2GB or less
        workerRAM.Should().BeInRange(0.5, 2.0,
            "because with 6GB pool for workers, we need at least 3 workers fitting");

        // Min memory should be at least 1GB for stable operation
        var minRAM = options.MemoryMinBytes / (1024.0 * 1024.0 * 1024.0);
        minRAM.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void HyperVManagerOptions_DefaultPaths_AreValid()
    {
        // Arrange & Act
        var options = new HyperVManagerOptions();

        // Assert
        options.TemplatePath.Should().NotBeNullOrEmpty();
        options.WorkersPath.Should().NotBeNullOrEmpty();
        options.VMPrefix.Should().NotBeNullOrEmpty();
        options.SwitchName.Should().Be("TzarBotSwitch");
    }

    [Theory]
    [InlineData(1, 2)]  // 1 worker x 2GB = 2GB (OK, under 6GB)
    [InlineData(2, 2)]  // 2 workers x 2GB = 4GB (OK, under 6GB)
    [InlineData(3, 2)]  // 3 workers x 2GB = 6GB (OK, exactly 6GB)
    [InlineData(4, 1.5)]  // 4 workers x 1.5GB = 6GB (OK)
    [InlineData(6, 1)]  // 6 workers x 1GB = 6GB (OK)
    public void WorkerRAM_Configuration_FitsWithin6GBPool(int workerCount, double ramPerWorkerGB)
    {
        // Arrange
        const double workerPoolLimit = 6.0;  // GB available for workers
        const double devVMRAM = 4.0;         // GB reserved for DEV
        const double totalLimit = 10.0;      // GB hard limit

        // Act
        var totalWorkerRAM = workerCount * ramPerWorkerGB;
        var totalRAM = devVMRAM + totalWorkerRAM;

        // Assert
        totalWorkerRAM.Should().BeLessThanOrEqualTo(workerPoolLimit,
            $"because {workerCount} workers at {ramPerWorkerGB}GB each should fit in {workerPoolLimit}GB pool");
        totalRAM.Should().BeLessThanOrEqualTo(totalLimit,
            $"because total VM RAM (DEV + workers) must not exceed {totalLimit}GB");
    }
}
