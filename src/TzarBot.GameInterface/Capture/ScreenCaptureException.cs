namespace TzarBot.GameInterface.Capture;

/// <summary>
/// Exception thrown when screen capture fails.
/// </summary>
public class ScreenCaptureException : Exception
{
    /// <summary>
    /// Creates a new ScreenCaptureException with the specified message.
    /// </summary>
    public ScreenCaptureException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new ScreenCaptureException with the specified message and inner exception.
    /// </summary>
    public ScreenCaptureException(string message, Exception inner) : base(message, inner)
    {
    }
}
