using TzarBot.Common.Models;

namespace TzarBot.StateDetection.Detection;

/// <summary>
/// Interface for game state detection implementations.
/// </summary>
public interface IGameStateDetector
{
    /// <summary>
    /// Name of the detector for diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Detects the current game state from a screen frame.
    /// </summary>
    /// <param name="frame">The captured screen frame.</param>
    /// <returns>Detection result with state and confidence.</returns>
    DetectionResult Detect(ScreenFrame frame);

    /// <summary>
    /// Checks if this detector supports the given game state detection.
    /// </summary>
    /// <param name="state">The game state to check.</param>
    /// <returns>True if this detector can detect the specified state.</returns>
    bool SupportsState(GameState state);
}

/// <summary>
/// Interface for detectors that require initialization (e.g., loading templates).
/// </summary>
public interface IInitializableDetector : IGameStateDetector
{
    /// <summary>
    /// Initializes the detector (load templates, configure thresholds, etc.).
    /// </summary>
    /// <returns>True if initialization was successful.</returns>
    bool Initialize();

    /// <summary>
    /// Returns true if the detector has been initialized.
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Interface for detectors that manage resources and should be disposed.
/// </summary>
public interface IDisposableDetector : IGameStateDetector, IDisposable
{
}
