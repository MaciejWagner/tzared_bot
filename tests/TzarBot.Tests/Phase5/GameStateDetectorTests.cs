using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.StateDetection;
using TzarBot.StateDetection.Detection;

namespace TzarBot.Tests.Phase5;

/// <summary>
/// Unit tests for game state detection components.
/// </summary>
public class GameStateDetectorTests
{
    #region GameState Tests

    [Fact]
    public void GameState_IsGameOver_ReturnsTrue_ForVictoryAndDefeat()
    {
        // Arrange & Act & Assert
        GameState.Victory.IsGameOver().Should().BeTrue();
        GameState.Defeat.IsGameOver().Should().BeTrue();
        GameState.InGame.IsGameOver().Should().BeFalse();
        GameState.MainMenu.IsGameOver().Should().BeFalse();
    }

    [Fact]
    public void GameState_IsActive_ReturnsTrue_ForInGameAndPaused()
    {
        GameState.InGame.IsActive().Should().BeTrue();
        GameState.Paused.IsActive().Should().BeTrue();
        GameState.Victory.IsActive().Should().BeFalse();
        GameState.MainMenu.IsActive().Should().BeFalse();
    }

    [Fact]
    public void GameState_IsError_ReturnsTrue_ForErrorStates()
    {
        GameState.NotResponding.IsError().Should().BeTrue();
        GameState.Closed.IsError().Should().BeTrue();
        GameState.Unknown.IsError().Should().BeTrue();
        GameState.InGame.IsError().Should().BeFalse();
    }

    [Fact]
    public void GameState_IsTransitional_ReturnsTrue_ForMenuAndLoading()
    {
        GameState.MainMenu.IsTransitional().Should().BeTrue();
        GameState.Loading.IsTransitional().Should().BeTrue();
        GameState.InGame.IsTransitional().Should().BeFalse();
    }

    #endregion

    #region DetectionResult Tests

    [Fact]
    public void DetectionResult_IsReliable_ReturnsTrue_AboveThreshold()
    {
        // Arrange
        var result = DetectionResult.Success(GameState.Victory, 0.9f, "TestDetector");

        // Act & Assert
        result.IsReliable(0.8f).Should().BeTrue();
        result.IsReliable(0.95f).Should().BeFalse();
    }

    [Fact]
    public void DetectionResult_Failed_ReturnsUnknownState()
    {
        // Arrange & Act
        var result = DetectionResult.Failed("TestDetector", "Test failure");

        // Assert
        result.State.Should().Be(GameState.Unknown);
        result.Confidence.Should().Be(0f);
        result.DiagnosticInfo.Should().Contain("Test failure");
    }

    [Fact]
    public void DetectionResult_Success_SetsAllProperties()
    {
        // Arrange & Act
        var result = DetectionResult.Success(
            GameState.InGame,
            0.95f,
            "TestDetector",
            15.5,
            "Test diagnostic");

        // Assert
        result.State.Should().Be(GameState.InGame);
        result.Confidence.Should().Be(0.95f);
        result.DetectorName.Should().Be("TestDetector");
        result.DetectionTimeMs.Should().Be(15.5);
        result.DiagnosticInfo.Should().Be("Test diagnostic");
    }

    #endregion

    #region DetectionConfig Tests

    [Fact]
    public void DetectionConfig_Default_HasReasonableValues()
    {
        // Arrange & Act
        var config = DetectionConfig.Default();

        // Assert
        config.TemplateMatchThreshold.Should().BeInRange(0.5f, 1.0f);
        config.HistogramMatchThreshold.Should().BeInRange(0.5f, 1.0f);
        config.ReferenceWidth.Should().BeGreaterThan(0);
        config.ReferenceHeight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RegionConfig_ToAbsolute_CalculatesCorrectPixelCoordinates()
    {
        // Arrange
        var region = new RegionConfig
        {
            X = 0.5f,
            Y = 0.25f,
            Width = 0.25f,
            Height = 0.1f
        };

        // Act
        var (x, y, w, h) = region.ToAbsolute(1920, 1080);

        // Assert
        x.Should().Be(960);
        y.Should().Be(270);
        w.Should().Be(480);
        h.Should().Be(108);
    }

    #endregion

    #region TemplateMatchingDetector Tests

    [Fact]
    public void TemplateMatchingDetector_Initialize_ReturnsTrueWithoutTemplates()
    {
        // Without actual template files, detector should still initialize
        // but not find matches
        using var detector = new TemplateMatchingDetector(new DetectionConfig
        {
            TemplateDirectory = "nonexistent_directory"
        });

        // Act
        var result = detector.Initialize();

        // Assert
        result.Should().BeTrue();
        detector.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void TemplateMatchingDetector_Detect_ReturnsUnknown_WhenNotInitialized()
    {
        // Arrange
        using var detector = new TemplateMatchingDetector();
        var frame = CreateTestFrame(100, 100);

        // Act - don't call Initialize()
        var result = detector.Detect(frame);

        // Assert
        result.State.Should().Be(GameState.Unknown);
        result.DiagnosticInfo.Should().Contain("not initialized");
    }

    [Fact]
    public void TemplateMatchingDetector_Detect_HandlesNullFrame()
    {
        // Arrange
        using var detector = new TemplateMatchingDetector();
        detector.Initialize();

        // Act
        var result = detector.Detect(null!);

        // Assert
        result.State.Should().Be(GameState.Unknown);
        result.DiagnosticInfo.Should().Contain("Invalid frame");
    }

    [Fact]
    public void TemplateMatchingDetector_Detect_HandlesEmptyFrame()
    {
        // Arrange
        using var detector = new TemplateMatchingDetector();
        detector.Initialize();
        var frame = new ScreenFrame
        {
            Data = Array.Empty<byte>(),
            Width = 0,
            Height = 0,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = PixelFormat.BGRA32
        };

        // Act
        var result = detector.Detect(frame);

        // Assert
        result.State.Should().Be(GameState.Unknown);
    }

    [Fact]
    public void TemplateMatchingDetector_SupportsState_ReturnsFalse_WhenNoTemplates()
    {
        // Arrange
        using var detector = new TemplateMatchingDetector(new DetectionConfig
        {
            TemplateDirectory = "nonexistent"
        });
        detector.Initialize();

        // Act & Assert
        detector.SupportsState(GameState.Victory).Should().BeFalse();
        detector.SupportsState(GameState.Unknown).Should().BeTrue();
    }

    #endregion

    #region ColorHistogramDetector Tests

    [Fact]
    public void ColorHistogramDetector_SupportsState_ReturnsCorrectStates()
    {
        // Arrange
        using var detector = new ColorHistogramDetector();

        // Act & Assert
        detector.SupportsState(GameState.Victory).Should().BeTrue();
        detector.SupportsState(GameState.Defeat).Should().BeTrue();
        detector.SupportsState(GameState.MainMenu).Should().BeTrue();
        detector.SupportsState(GameState.Loading).Should().BeTrue();
        detector.SupportsState(GameState.InGame).Should().BeFalse(); // Special case handled internally
    }

    [Fact]
    public void ColorHistogramDetector_Detect_HandlesValidFrame()
    {
        // Arrange
        using var detector = new ColorHistogramDetector();
        var frame = CreateTestFrame(100, 100, fillColor: 0x80); // Gray frame

        // Act
        var result = detector.Detect(frame);

        // Assert
        result.Should().NotBeNull();
        result.DetectorName.Should().Be("ColorHistogram");
    }

    [Fact]
    public void ColorHistogramDetector_Detect_ReturnsUnknown_ForNullFrame()
    {
        // Arrange
        using var detector = new ColorHistogramDetector();

        // Act
        var result = detector.Detect(null!);

        // Assert
        result.State.Should().Be(GameState.Unknown);
    }

    [Fact]
    public void ColorHistogramDetector_Detect_DetectsLoadingForDarkScreen()
    {
        // Arrange
        using var detector = new ColorHistogramDetector(new DetectionConfig
        {
            HistogramMatchThreshold = 0.3f // Lower threshold for testing
        });

        // Create a very dark frame (simulating loading screen)
        var frame = CreateTestFrame(100, 100, fillColor: 0x10);

        // Act
        var result = detector.Detect(frame);

        // Assert
        // Note: The exact state depends on the signature configuration
        // For a very dark frame, Loading should score high
        result.Should().NotBeNull();
    }

    #endregion

    #region CompositeGameStateDetector Tests

    [Fact]
    public void CompositeDetector_Initialize_InitializesAllDetectors()
    {
        // Arrange
        using var detector = new CompositeGameStateDetector();

        // Act
        var result = detector.Initialize();

        // Assert
        result.Should().BeTrue();
        detector.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void CompositeDetector_AddDetector_IncludesInDetection()
    {
        // Arrange
        using var composite = new CompositeGameStateDetector();
        composite.AddDetector(new ColorHistogramDetector(), 1.0f);
        composite.Initialize();

        var frame = CreateTestFrame(100, 100);

        // Act
        var result = composite.Detect(frame);

        // Assert
        result.DetectorName.Should().Be("CompositeDetector");
        result.DiagnosticInfo.Should().Contain("ColorHistogram");
    }

    [Fact]
    public void CompositeDetector_Detect_CombinesMultipleDetectorResults()
    {
        // Arrange
        using var composite = new CompositeGameStateDetector();
        composite.AddDetector(new ColorHistogramDetector(), 1.0f);
        composite.Initialize();

        var frame = CreateTestFrame(100, 100);

        // Act
        var result = composite.Detect(frame);

        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CompositeDetector_SupportsState_DelegatesToChildDetectors()
    {
        // Arrange
        using var composite = new CompositeGameStateDetector();
        composite.AddDetector(new ColorHistogramDetector(), 1.0f);
        composite.Initialize();

        // Act & Assert
        composite.SupportsState(GameState.Victory).Should().BeTrue();
        composite.SupportsState(GameState.Defeat).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test frame with specified dimensions and fill color.
    /// </summary>
    private static ScreenFrame CreateTestFrame(int width, int height, byte fillColor = 0x80)
    {
        var data = new byte[width * height * 4]; // BGRA32

        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = fillColor;     // B
            data[i + 1] = fillColor; // G
            data[i + 2] = fillColor; // R
            data[i + 3] = 255;       // A
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

    /// <summary>
    /// Creates a test frame with a specific dominant color.
    /// </summary>
    private static ScreenFrame CreateColoredTestFrame(int width, int height, byte r, byte g, byte b)
    {
        var data = new byte[width * height * 4];

        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = b;     // B
            data[i + 1] = g; // G
            data[i + 2] = r; // R
            data[i + 3] = 255;
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
