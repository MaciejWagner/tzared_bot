namespace TzarBot.StateDetection.Monitoring;

/// <summary>
/// Configuration for the game monitor.
/// </summary>
public sealed class GameMonitorConfig
{
    /// <summary>
    /// Maximum game duration before timeout (default: 30 minutes).
    /// </summary>
    public TimeSpan MaxGameDuration { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Interval between state checks (default: 500ms).
    /// </summary>
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Time without state changes before considering the game stuck (default: 2 minutes).
    /// </summary>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Time a window can be not responding before considering it crashed (default: 10 seconds).
    /// </summary>
    public TimeSpan NotRespondingTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Time in loading screen before considering it stuck (default: 3 minutes).
    /// </summary>
    public TimeSpan LoadingTimeout { get; init; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Minimum confidence threshold for state transitions.
    /// </summary>
    public float StateTransitionThreshold { get; init; } = 0.8f;

    /// <summary>
    /// Number of consecutive detections required to confirm a state change.
    /// This prevents false positives from single-frame glitches.
    /// </summary>
    public int ConsecutiveDetectionsRequired { get; init; } = 3;

    /// <summary>
    /// Enable activity tracking (frame changes) for stuck detection.
    /// </summary>
    public bool EnableActivityTracking { get; init; } = true;

    /// <summary>
    /// Minimum frame difference threshold to consider activity (0.0-1.0).
    /// Lower values are more sensitive.
    /// </summary>
    public float ActivityThreshold { get; init; } = 0.01f;

    /// <summary>
    /// Process name of the game to monitor (for crash detection).
    /// </summary>
    public string GameProcessName { get; init; } = "Tzar";

    /// <summary>
    /// Window title pattern for the game (for window detection).
    /// </summary>
    public string GameWindowTitlePattern { get; init; } = "Tzar";

    /// <summary>
    /// Enable logging of state transitions.
    /// </summary>
    public bool EnableLogging { get; init; } = true;

    /// <summary>
    /// Creates default configuration.
    /// </summary>
    public static GameMonitorConfig Default() => new();

    /// <summary>
    /// Creates configuration optimized for fast games (shorter timeouts).
    /// </summary>
    public static GameMonitorConfig FastGame() => new()
    {
        MaxGameDuration = TimeSpan.FromMinutes(15),
        IdleTimeout = TimeSpan.FromMinutes(1),
        LoadingTimeout = TimeSpan.FromMinutes(1),
        CheckInterval = TimeSpan.FromMilliseconds(250)
    };

    /// <summary>
    /// Creates configuration for long games (extended timeouts).
    /// </summary>
    public static GameMonitorConfig LongGame() => new()
    {
        MaxGameDuration = TimeSpan.FromHours(1),
        IdleTimeout = TimeSpan.FromMinutes(5),
        LoadingTimeout = TimeSpan.FromMinutes(5)
    };
}
