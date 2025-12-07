using TzarBot.Common.Models;

namespace TzarBot.GameInterface.Capture;

/// <summary>
/// Interface for screen capture implementations.
/// </summary>
public interface IScreenCapture : IDisposable
{
    /// <summary>
    /// Captures the current screen frame.
    /// </summary>
    /// <returns>The captured frame, or null if no new frame is available.</returns>
    ScreenFrame? CaptureFrame();

    /// <summary>
    /// Width of the captured area in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Height of the captured area in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Indicates whether the capture system is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
}
