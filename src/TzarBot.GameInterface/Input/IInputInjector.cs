namespace TzarBot.GameInterface.Input;

/// <summary>
/// Interface for mouse and keyboard input injection.
/// </summary>
public interface IInputInjector
{
    /// <summary>
    /// Moves the mouse cursor to an absolute screen position.
    /// </summary>
    /// <param name="x">X coordinate in screen pixels.</param>
    /// <param name="y">Y coordinate in screen pixels.</param>
    /// <param name="absolute">If true, coordinates are absolute; if false, relative.</param>
    void MoveMouse(int x, int y, bool absolute = true);

    /// <summary>
    /// Moves the mouse cursor by a relative amount.
    /// </summary>
    /// <param name="dx">Horizontal movement in pixels.</param>
    /// <param name="dy">Vertical movement in pixels.</param>
    void MoveMouseRelative(int dx, int dy);

    /// <summary>
    /// Performs a left mouse button click.
    /// </summary>
    void LeftClick();

    /// <summary>
    /// Performs a right mouse button click.
    /// </summary>
    void RightClick();

    /// <summary>
    /// Performs a double left click.
    /// </summary>
    void DoubleClick();

    /// <summary>
    /// Starts a drag operation (presses left button down).
    /// </summary>
    void DragStart();

    /// <summary>
    /// Ends a drag operation (releases left button).
    /// </summary>
    void DragEnd();

    /// <summary>
    /// Presses and holds a key.
    /// </summary>
    /// <param name="key">The virtual key to press.</param>
    void PressKey(VirtualKey key);

    /// <summary>
    /// Releases a previously pressed key.
    /// </summary>
    /// <param name="key">The virtual key to release.</param>
    void ReleaseKey(VirtualKey key);

    /// <summary>
    /// Types a single key (press and release).
    /// </summary>
    /// <param name="key">The virtual key to type.</param>
    void TypeKey(VirtualKey key);

    /// <summary>
    /// Types a hotkey combination (modifier + key).
    /// </summary>
    /// <param name="modifier">The modifier key (Ctrl, Alt, Shift).</param>
    /// <param name="key">The key to press with the modifier.</param>
    void TypeHotkey(VirtualKey modifier, VirtualKey key);

    /// <summary>
    /// Scrolls the mouse wheel.
    /// </summary>
    /// <param name="delta">Positive for up, negative for down.</param>
    void Scroll(int delta);

    /// <summary>
    /// Minimum delay between actions for rate limiting.
    /// </summary>
    TimeSpan MinActionDelay { get; set; }
}
