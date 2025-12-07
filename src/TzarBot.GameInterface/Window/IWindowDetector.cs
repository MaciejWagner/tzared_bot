namespace TzarBot.GameInterface.Window;

/// <summary>
/// Interface for window detection and management.
/// </summary>
public interface IWindowDetector
{
    /// <summary>
    /// Finds a window by title pattern.
    /// </summary>
    /// <param name="titlePattern">Pattern to match in window title.</param>
    /// <returns>Window info if found, null otherwise.</returns>
    WindowInfo? FindWindow(string titlePattern);

    /// <summary>
    /// Finds a window by its class name.
    /// </summary>
    /// <param name="className">Window class name to find.</param>
    /// <returns>Window info if found, null otherwise.</returns>
    WindowInfo? FindWindowByClass(string className);

    /// <summary>
    /// Finds a window by process name.
    /// </summary>
    /// <param name="processName">Process name (without .exe).</param>
    /// <returns>Window info if found, null otherwise.</returns>
    WindowInfo? FindWindowByProcess(string processName);

    /// <summary>
    /// Gets window info for a specific handle.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <returns>Window info if valid, null otherwise.</returns>
    WindowInfo? GetWindowInfo(nint handle);

    /// <summary>
    /// Brings the window to foreground.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <returns>True if successful.</returns>
    bool SetForeground(nint handle);

    /// <summary>
    /// Enumerates all visible windows.
    /// </summary>
    /// <returns>Collection of window info.</returns>
    IEnumerable<WindowInfo> EnumerateWindows();
}
