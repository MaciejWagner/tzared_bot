using FluentAssertions;
using Moq;
using TzarBot.Common.Models;
using TzarBot.StateDetection;
using TzarBot.StateDetection.Detection;
using TzarBot.StateDetection.Monitoring;

namespace TzarBot.Tests.Phase5;

/// <summary>
/// Unit tests for the GameMonitor class.
/// </summary>
public class GameMonitorTests
{
    #region GameMonitorConfig Tests

    [Fact]
    public void GameMonitorConfig_Default_HasReasonableTimeouts()
    {
        // Arrange & Act
        var config = GameMonitorConfig.Default();

        // Assert
        config.MaxGameDuration.Should().BeGreaterThan(TimeSpan.Zero);
        config.CheckInterval.Should().BeGreaterThan(TimeSpan.Zero);
        config.IdleTimeout.Should().BeGreaterThan(TimeSpan.Zero);
        config.NotRespondingTimeout.Should().BeGreaterThan(TimeSpan.Zero);
        config.LoadingTimeout.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GameMonitorConfig_FastGame_HasShorterTimeouts()
    {
        // Arrange & Act
        var defaultConfig = GameMonitorConfig.Default();
        var fastConfig = GameMonitorConfig.FastGame();

        // Assert
        fastConfig.MaxGameDuration.Should().BeLessThan(defaultConfig.MaxGameDuration);
        fastConfig.IdleTimeout.Should().BeLessThan(defaultConfig.IdleTimeout);
        fastConfig.LoadingTimeout.Should().BeLessThan(defaultConfig.LoadingTimeout);
    }

    [Fact]
    public void GameMonitorConfig_LongGame_HasLongerTimeouts()
    {
        // Arrange & Act
        var defaultConfig = GameMonitorConfig.Default();
        var longConfig = GameMonitorConfig.LongGame();

        // Assert
        longConfig.MaxGameDuration.Should().BeGreaterThan(defaultConfig.MaxGameDuration);
        longConfig.IdleTimeout.Should().BeGreaterThan(defaultConfig.IdleTimeout);
    }

    #endregion

    #region MonitoringResult Tests

    [Fact]
    public void MonitoringResult_IsSuccessfulCompletion_TrueForVictoryAndDefeat()
    {
        // Arrange
        var victoryResult = new MonitoringResult
        {
            Outcome = GameOutcome.Victory,
            FinalState = GameState.Victory,
            Duration = TimeSpan.FromMinutes(10),
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow
        };

        var defeatResult = new MonitoringResult
        {
            Outcome = GameOutcome.Defeat,
            FinalState = GameState.Defeat,
            Duration = TimeSpan.FromMinutes(10),
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow
        };

        var timeoutResult = new MonitoringResult
        {
            Outcome = GameOutcome.Timeout,
            FinalState = GameState.InGame,
            Duration = TimeSpan.FromMinutes(30),
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow
        };

        // Assert
        victoryResult.IsSuccessfulCompletion.Should().BeTrue();
        defeatResult.IsSuccessfulCompletion.Should().BeTrue();
        timeoutResult.IsSuccessfulCompletion.Should().BeFalse();
    }

    [Fact]
    public void MonitoringResult_ToGameResult_ConvertsCorrectly()
    {
        // Arrange
        var monitoringResult = new MonitoringResult
        {
            Outcome = GameOutcome.Victory,
            FinalState = GameState.Victory,
            Duration = TimeSpan.FromMinutes(15),
            StartTime = DateTime.UtcNow.AddMinutes(-15),
            EndTime = DateTime.UtcNow,
            ActiveFrameCount = 1000,
            IdleFrameCount = 100
        };

        // Act
        var gameResult = monitoringResult.ToGameResult();

        // Assert
        gameResult.Won.Should().BeTrue();
        gameResult.DurationSeconds.Should().BeApproximately(900f, 1f);
        gameResult.ValidActions.Should().Be(1000);
        gameResult.IdleFrames.Should().Be(100);
        gameResult.TotalFrames.Should().Be(1100);
    }

    [Fact]
    public void MonitoringResult_ToGameResult_WithExtractedStats_IncludesStats()
    {
        // Arrange
        var monitoringResult = new MonitoringResult
        {
            Outcome = GameOutcome.Defeat,
            FinalState = GameState.Defeat,
            Duration = TimeSpan.FromMinutes(10),
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            ActiveFrameCount = 500,
            IdleFrameCount = 50,
            ExtractedStats = new StateDetection.Stats.GameStats
            {
                UnitsBuilt = 25,
                UnitsLost = 10,
                EnemyUnitsKilled = 15,
                BuildingsBuilt = 5,
                ExtractionConfidence = 0.8f
            }
        };

        // Act
        var gameResult = monitoringResult.ToGameResult();

        // Assert
        gameResult.Won.Should().BeFalse();
        gameResult.UnitsBuilt.Should().Be(25);
        gameResult.UnitsLost.Should().Be(10);
        gameResult.UnitsKilled.Should().Be(15);
        gameResult.BuildingsBuilt.Should().Be(5);
    }

    #endregion

    #region StateTransition Tests

    [Fact]
    public void StateTransition_RecordsTransitionDetails()
    {
        // Arrange & Act
        var transition = new StateTransition
        {
            FromState = GameState.Loading,
            ToState = GameState.InGame,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.95f,
            ElapsedTime = TimeSpan.FromSeconds(30)
        };

        // Assert
        transition.FromState.Should().Be(GameState.Loading);
        transition.ToState.Should().Be(GameState.InGame);
        transition.Confidence.Should().Be(0.95f);
        transition.ElapsedTime.TotalSeconds.Should().Be(30);
    }

    #endregion

    #region GameMonitor Tests

    [Fact]
    public void GameMonitor_Constructor_AcceptsCustomDetector()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.Setup(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.InGame, 0.9f, "MockDetector"));

        // Act
        using var monitor = new GameMonitor(mockDetector.Object);

        // Assert
        monitor.Should().NotBeNull();
        monitor.CurrentState.Should().Be(GameState.Unknown);
        monitor.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void GameMonitor_ProcessFrame_UpdatesCurrentState()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.Setup(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.InGame, 0.95f, "MockDetector"));

        var config = new GameMonitorConfig
        {
            ConsecutiveDetectionsRequired = 1, // Immediate transition for testing
            StateTransitionThreshold = 0.8f,
            EnableLogging = false
        };

        using var monitor = new GameMonitor(mockDetector.Object, config);
        var frame = CreateTestFrame(100, 100);

        // Act
        var state = monitor.ProcessFrame(frame);

        // Assert
        state.Should().Be(GameState.InGame);
        monitor.IsMonitoring.Should().BeTrue();
    }

    [Fact]
    public void GameMonitor_ProcessFrame_RequiresConsecutiveDetections()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.SetupSequence(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.InGame, 0.9f, "Mock"))
            .Returns(DetectionResult.Success(GameState.Victory, 0.9f, "Mock"))
            .Returns(DetectionResult.Success(GameState.Victory, 0.9f, "Mock"))
            .Returns(DetectionResult.Success(GameState.Victory, 0.9f, "Mock"));

        var config = new GameMonitorConfig
        {
            ConsecutiveDetectionsRequired = 3,
            StateTransitionThreshold = 0.8f,
            EnableLogging = false
        };

        using var monitor = new GameMonitor(mockDetector.Object, config);
        var frame = CreateTestFrame(100, 100);

        // Act - First frame (InGame)
        monitor.ProcessFrame(frame);

        // Second frame (Victory) - not enough consecutive
        var state2 = monitor.ProcessFrame(frame);

        // Third frame (Victory) - still not enough
        var state3 = monitor.ProcessFrame(frame);

        // Fourth frame (Victory) - now should transition
        var state4 = monitor.ProcessFrame(frame);

        // Assert
        // After first InGame detection with ConsecutiveRequired=3, we need 3 InGame detections to confirm
        // But sequence starts with InGame then switches to Victory
        // So we should stay Unknown until Victory reaches 3 consecutive
        state4.Should().Be(GameState.Victory);
    }

    [Fact]
    public void GameMonitor_StateChanged_EventRaised_OnTransition()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.Setup(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.InGame, 0.95f, "Mock"));

        var config = new GameMonitorConfig
        {
            ConsecutiveDetectionsRequired = 1,
            StateTransitionThreshold = 0.8f,
            EnableLogging = false
        };

        using var monitor = new GameMonitor(mockDetector.Object, config);

        StateChangedEventArgs? receivedArgs = null;
        monitor.StateChanged += (s, e) => receivedArgs = e;

        var frame = CreateTestFrame(100, 100);

        // Act
        monitor.ProcessFrame(frame);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.NewState.Should().Be(GameState.InGame);
        receivedArgs.Confidence.Should().Be(0.95f);
    }

    [Fact]
    public void GameMonitor_GameEnded_EventRaised_OnVictory()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.Setup(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.Victory, 0.95f, "Mock"));

        var config = new GameMonitorConfig
        {
            ConsecutiveDetectionsRequired = 1,
            StateTransitionThreshold = 0.8f,
            EnableLogging = false
        };

        using var monitor = new GameMonitor(mockDetector.Object, config);

        GameEndedEventArgs? receivedArgs = null;
        monitor.GameEnded += (s, e) => receivedArgs = e;

        var frame = CreateTestFrame(100, 100);

        // Act
        monitor.ProcessFrame(frame);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Outcome.Should().Be(GameOutcome.Victory);
        receivedArgs.FinalState.Should().Be(GameState.Victory);
    }

    [Fact]
    public void GameMonitor_StopMonitoring_StopsMonitoring()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.Setup(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.InGame, 0.9f, "Mock"));

        using var monitor = new GameMonitor(mockDetector.Object, new GameMonitorConfig { EnableLogging = false });

        var frame = CreateTestFrame(100, 100);
        monitor.ProcessFrame(frame); // Start monitoring

        // Act
        monitor.StopMonitoring();

        // Assert
        monitor.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public async Task GameMonitor_StartMonitoringAsync_ThrowsIfAlreadyMonitoring()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        mockDetector.Setup(d => d.Detect(It.IsAny<ScreenFrame>()))
            .Returns(DetectionResult.Success(GameState.InGame, 0.9f, "Mock"));

        using var monitor = new GameMonitor(mockDetector.Object, new GameMonitorConfig { EnableLogging = false });

        var frame = CreateTestFrame(100, 100);
        monitor.ProcessFrame(frame); // Start monitoring

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            monitor.StartMonitoringAsync(CancellationToken.None));
    }

    [Fact]
    public void GameMonitor_Dispose_DisposesResources()
    {
        // Arrange
        var mockDetector = new Mock<IGameStateDetector>();
        var disposableMock = mockDetector.As<IDisposable>();

        var monitor = new GameMonitor(mockDetector.Object, new GameMonitorConfig { EnableLogging = false });

        // Act
        monitor.Dispose();

        // Assert
        disposableMock.Verify(d => d.Dispose(), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static ScreenFrame CreateTestFrame(int width, int height, byte fillColor = 0x80)
    {
        var data = new byte[width * height * 4];

        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = fillColor;
            data[i + 1] = fillColor;
            data[i + 2] = fillColor;
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
