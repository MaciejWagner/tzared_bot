using FluentAssertions;
using TzarBot.Orchestrator.Communication;

namespace TzarBot.Tests.Phase4;

/// <summary>
/// Tests for GenomeTransferResult
/// </summary>
public class GenomeTransferResultTests
{
    [Fact]
    public void GenomeTransferResult_SuccessfulTransfer_HasAllProperties()
    {
        // Arrange & Act
        var result = new GenomeTransferResult
        {
            Success = true,
            GenomeId = "genome_001",
            VMName = "TzarBot-Worker-0",
            TransferDuration = TimeSpan.FromSeconds(2.5),
            Checksum = "ABCDEF123456",
            BytesTransferred = 1024
        };

        // Assert
        result.Success.Should().BeTrue();
        result.GenomeId.Should().Be("genome_001");
        result.VMName.Should().Be("TzarBot-Worker-0");
        result.TransferDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Checksum.Should().NotBeNullOrEmpty();
        result.BytesTransferred.Should().BeGreaterThan(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void GenomeTransferResult_FailedTransfer_HasErrorMessage()
    {
        // Arrange & Act
        var result = new GenomeTransferResult
        {
            Success = false,
            GenomeId = "genome_002",
            VMName = "TzarBot-Worker-1",
            ErrorMessage = "Network timeout"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Network timeout");
        result.BytesTransferred.Should().Be(0);
        result.Checksum.Should().BeNull();
    }

    [Fact]
    public void GenomeTransferResult_RequiredProperties_MustBeProvided()
    {
        // This test verifies that GenomeId and VMName are required via 'required' keyword
        // The compiler enforces this, but we can verify the properties exist

        var result = new GenomeTransferResult
        {
            GenomeId = "required_genome",
            VMName = "required_vm"
        };

        result.GenomeId.Should().NotBeNull();
        result.VMName.Should().NotBeNull();
    }
}
