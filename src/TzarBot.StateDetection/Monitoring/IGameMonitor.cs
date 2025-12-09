using TzarBot.Common.Models;

namespace TzarBot.StateDetection.Monitoring;

/// <summary>
/// Interface for game monitoring implementations.
/// </summary>
public interface IGameMonitor : IDisposable
{
    /// <summary>
    /// Current detected game state.
    /// </summary>
    GameState CurrentState { get; }

    /// <summary>
    /// Returns true if monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Event raised when the game state changes.
    /// </summary>
    event EventHandler<StateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when the game ends (victory, defeat, crash, timeout).
    /// </summary>
    event EventHandler<GameEndedEventArgs>? GameEnded;

    /// <summary>
    /// Starts monitoring the game.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when monitoring ends.</returns>
    Task<MonitoringResult> StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Processes a single frame for state detection.
    /// </summary>
    /// <param name="frame">The frame to process.</param>
    /// <returns>Current detected state.</returns>
    GameState ProcessFrame(ScreenFrame frame);
}

/// <summary>
/// Event arguments for state change events.
/// </summary>
public sealed class StateChangedEventArgs : EventArgs
{
    public required GameState PreviousState { get; init; }
    public required GameState NewState { get; init; }
    public required float Confidence { get; init; }
    public required TimeSpan ElapsedTime { get; init; }
}

/// <summary>
/// Event arguments for game ended events.
/// </summary>
public sealed class GameEndedEventArgs : EventArgs
{
    public required GameOutcome Outcome { get; init; }
    public required GameState FinalState { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Reason { get; init; }
}
