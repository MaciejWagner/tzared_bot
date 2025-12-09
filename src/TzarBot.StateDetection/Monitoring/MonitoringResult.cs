using TzarBot.GeneticAlgorithm.Fitness;
using TzarBot.StateDetection.Stats;

namespace TzarBot.StateDetection.Monitoring;

/// <summary>
/// Represents the result of a game monitoring session.
/// </summary>
public sealed class MonitoringResult
{
    /// <summary>
    /// Final outcome of the game.
    /// </summary>
    public required GameOutcome Outcome { get; init; }

    /// <summary>
    /// Final detected game state.
    /// </summary>
    public required GameState FinalState { get; init; }

    /// <summary>
    /// Total duration of the game (from start to end detection).
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Timestamp when monitoring started.
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// Timestamp when monitoring ended.
    /// </summary>
    public required DateTime EndTime { get; init; }

    /// <summary>
    /// Reason for the game ending (if not natural win/loss).
    /// </summary>
    public string? EndReason { get; init; }

    /// <summary>
    /// Number of state changes detected during monitoring.
    /// </summary>
    public int StateChangeCount { get; init; }

    /// <summary>
    /// Number of frames where the game was detected as idle/stuck.
    /// </summary>
    public int IdleFrameCount { get; init; }

    /// <summary>
    /// Number of frames where the game was detected as active.
    /// </summary>
    public int ActiveFrameCount { get; init; }

    /// <summary>
    /// History of state transitions during monitoring.
    /// </summary>
    public List<StateTransition> StateHistory { get; init; } = new();

    /// <summary>
    /// Extracted game statistics (if available from OCR).
    /// </summary>
    public GameStats? ExtractedStats { get; init; }

    /// <summary>
    /// Converts monitoring result to GameResult for fitness calculation.
    /// </summary>
    public GameResult ToGameResult()
    {
        return new GameResult
        {
            Won = Outcome == GameOutcome.Victory,
            DurationSeconds = (float)Duration.TotalSeconds,
            MaxDurationSeconds = 3600f, // 1 hour max
            ValidActions = ActiveFrameCount,
            InvalidActions = 0, // Not tracked by monitor
            IdleFrames = IdleFrameCount,
            TotalFrames = ActiveFrameCount + IdleFrameCount,
            UnitsBuilt = ExtractedStats?.UnitsBuilt ?? 0,
            UnitsLost = ExtractedStats?.UnitsLost ?? 0,
            UnitsKilled = ExtractedStats?.EnemyUnitsKilled ?? 0,
            BuildingsBuilt = ExtractedStats?.BuildingsBuilt ?? 0,
            BuildingsLost = ExtractedStats?.BuildingsDestroyed ?? 0,
            BuildingsDestroyed = ExtractedStats?.EnemyBuildingsDestroyed ?? 0,
            ResourcesGathered = ExtractedStats?.ResourcesGathered ?? 0,
            ResourcesSpent = ExtractedStats?.ResourcesSpent ?? 0
        };
    }

    /// <summary>
    /// Returns true if the game ended successfully (victory or defeat).
    /// </summary>
    public bool IsSuccessfulCompletion
        => Outcome == GameOutcome.Victory || Outcome == GameOutcome.Defeat;
}

/// <summary>
/// Represents the outcome of a monitored game.
/// </summary>
public enum GameOutcome
{
    /// <summary>
    /// Player won the game.
    /// </summary>
    Victory,

    /// <summary>
    /// Player lost the game.
    /// </summary>
    Defeat,

    /// <summary>
    /// Game ended due to timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// Game crashed or became unresponsive.
    /// </summary>
    Crashed,

    /// <summary>
    /// Game was stuck/idle for too long.
    /// </summary>
    Stuck,

    /// <summary>
    /// Monitoring was cancelled externally.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Outcome could not be determined.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents a state transition during monitoring.
/// </summary>
public sealed class StateTransition
{
    /// <summary>
    /// Previous state.
    /// </summary>
    public required GameState FromState { get; init; }

    /// <summary>
    /// New state.
    /// </summary>
    public required GameState ToState { get; init; }

    /// <summary>
    /// Timestamp of the transition.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Confidence of the new state detection.
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Elapsed time since monitoring started.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }
}
