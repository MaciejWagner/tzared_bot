using TzarBot.Common.Models;

namespace TzarBot.StateDetection.Stats;

/// <summary>
/// Interface for extracting game statistics from screen captures.
/// </summary>
public interface IStatsExtractor : IDisposable
{
    /// <summary>
    /// Extracts game statistics from an end-game screen capture.
    /// </summary>
    /// <param name="frame">The captured screen frame (should be victory/defeat screen).</param>
    /// <returns>Extracted game statistics.</returns>
    GameStats ExtractStats(ScreenFrame frame);

    /// <summary>
    /// Returns true if the extractor is properly initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the extractor (load OCR engine, etc.).
    /// </summary>
    /// <returns>True if initialization was successful.</returns>
    bool Initialize();
}
