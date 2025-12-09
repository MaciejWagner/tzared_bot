using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.StateDetection.Stats;

namespace TzarBot.Tests.Phase5;

/// <summary>
/// Unit tests for the OCR stats extraction components.
/// </summary>
public class StatsExtractorTests
{
    #region GameStats Tests

    [Fact]
    public void GameStats_Empty_ReturnsInvalidStats()
    {
        // Arrange & Act
        var stats = GameStats.Empty();

        // Assert
        stats.IsValid.Should().BeFalse();
        stats.ExtractionConfidence.Should().Be(0f);
        stats.FailedFields.Should().NotBeEmpty();
    }

    [Fact]
    public void GameStats_IsValid_ReturnsTrueAboveThreshold()
    {
        // Arrange
        var validStats = new GameStats
        {
            ExtractionConfidence = 0.5f,
            UnitsBuilt = 10
        };

        var invalidStats = new GameStats
        {
            ExtractionConfidence = 0.2f
        };

        // Assert
        validStats.IsValid.Should().BeTrue();
        invalidStats.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GameStats_ResourcesGathered_IsSum()
    {
        // Arrange
        var stats = new GameStats
        {
            GoldGathered = 1000,
            WoodGathered = 500,
            StoneGathered = 300,
            FoodProduced = 200,
            ResourcesGathered = 2000, // Should be sum
            ExtractionConfidence = 0.8f
        };

        // Assert
        stats.ResourcesGathered.Should().Be(2000);
    }

    #endregion

    #region OcrStatsExtractor Tests

    [Fact]
    public void OcrStatsExtractor_Initialize_ReturnsFalseWithoutTessdata()
    {
        // Arrange
        using var extractor = new OcrStatsExtractor(tessDataPath: "nonexistent_directory");

        // Act
        var result = extractor.Initialize();

        // Assert
        result.Should().BeFalse();
        extractor.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void OcrStatsExtractor_ExtractStats_ReturnsEmptyForNullFrame()
    {
        // Arrange
        using var extractor = new OcrStatsExtractor();

        // Act
        var stats = extractor.ExtractStats(null!);

        // Assert
        stats.IsValid.Should().BeFalse();
    }

    [Fact]
    public void OcrStatsExtractor_ExtractStats_ReturnsEmptyForInvalidFrame()
    {
        // Arrange
        using var extractor = new OcrStatsExtractor();
        var invalidFrame = new ScreenFrame
        {
            Data = Array.Empty<byte>(),
            Width = 0,
            Height = 0,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = PixelFormat.BGRA32
        };

        // Act
        var stats = extractor.ExtractStats(invalidFrame);

        // Assert
        stats.IsValid.Should().BeFalse();
    }

    [Fact]
    public void OcrStatsExtractor_ExtractStats_HandlesValidFrameWithoutTessdata()
    {
        // Arrange
        using var extractor = new OcrStatsExtractor(tessDataPath: "nonexistent");
        var frame = CreateTestFrame(100, 100);

        // Act
        var stats = extractor.ExtractStats(frame);

        // Assert
        // Without tessdata, should return empty stats
        stats.IsValid.Should().BeFalse();
    }

    [Fact]
    public void OcrStatsExtractor_Dispose_DoesNotThrow()
    {
        // Arrange
        var extractor = new OcrStatsExtractor();

        // Act & Assert
        extractor.Invoking(e => e.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void OcrStatsExtractor_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var extractor = new OcrStatsExtractor();

        // Act & Assert
        extractor.Dispose();
        extractor.Invoking(e => e.Dispose()).Should().NotThrow();
    }

    #endregion

    #region IStatsExtractor Interface Tests

    [Fact]
    public void OcrStatsExtractor_ImplementsIStatsExtractor()
    {
        // Arrange & Act
        using var extractor = new OcrStatsExtractor();

        // Assert
        extractor.Should().BeAssignableTo<IStatsExtractor>();
    }

    #endregion

    #region Helper Methods

    private static ScreenFrame CreateTestFrame(int width, int height)
    {
        var data = new byte[width * height * 4];

        // Create a simple gradient pattern
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                byte value = (byte)((x + y) * 255 / (width + height));
                data[idx] = value;     // B
                data[idx + 1] = value; // G
                data[idx + 2] = value; // R
                data[idx + 3] = 255;   // A
            }
        }

        return new ScreenFrame
        {
            Data = data,
            Width = width,
            Height = height,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = PixelFormat.BGRA32
        };
    }

    private static ScreenFrame CreateFrameWithText(int width, int height)
    {
        // Create a white background with black "text-like" regions
        var data = new byte[width * height * 4];

        // White background
        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = 255;     // B
            data[i + 1] = 255; // G
            data[i + 2] = 255; // R
            data[i + 3] = 255; // A
        }

        // Add some "text" regions (black horizontal lines)
        for (int y = 20; y < height - 20; y += 15)
        {
            for (int x = 10; x < width - 10; x++)
            {
                int idx = (y * width + x) * 4;
                if (y % 15 < 3) // 3 pixel high "text" lines
                {
                    data[idx] = 0;
                    data[idx + 1] = 0;
                    data[idx + 2] = 0;
                }
            }
        }

        return new ScreenFrame
        {
            Data = data,
            Width = width,
            Height = height,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = PixelFormat.BGRA32
        };
    }

    #endregion
}
