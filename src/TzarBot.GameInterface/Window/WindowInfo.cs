using System.Drawing;

namespace TzarBot.GameInterface.Window;

/// <summary>
/// Information about a window.
/// </summary>
public sealed class WindowInfo
{
    /// <summary>
    /// Window handle.
    /// </summary>
    public required nint Handle { get; init; }

    /// <summary>
    /// Window title text.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Window class name.
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Window bounds including frame.
    /// </summary>
    public required Rectangle Bounds { get; init; }

    /// <summary>
    /// Client area bounds (content area).
    /// </summary>
    public required Rectangle ClientBounds { get; init; }

    /// <summary>
    /// Whether the window is the foreground window.
    /// </summary>
    public required bool IsFocused { get; init; }

    /// <summary>
    /// Whether the window is minimized.
    /// </summary>
    public required bool IsMinimized { get; init; }

    /// <summary>
    /// Whether the window is visible.
    /// </summary>
    public required bool IsVisible { get; init; }
}

/// <summary>
/// Constants for Tzar game window.
/// </summary>
public static class TzarWindow
{
    /// <summary>
    /// Expected window title.
    /// </summary>
    public const string WindowTitle = "Tzar";

    /// <summary>
    /// Expected process name.
    /// </summary>
    public const string ProcessName = "Tzar";
}
