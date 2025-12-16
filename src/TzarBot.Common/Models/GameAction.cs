using MessagePack;

namespace TzarBot.Common.Models;

/// <summary>
/// Represents an action to be performed in the game.
/// </summary>
[MessagePackObject]
public sealed record GameAction
{
    /// <summary>
    /// The type of action to perform.
    /// </summary>
    [Key(0)]
    public ActionType Type { get; init; }

    /// <summary>
    /// Relative mouse movement in X direction (-1.0 to 1.0).
    /// Only used for mouse movement actions.
    /// </summary>
    [Key(1)]
    public float MouseDeltaX { get; init; }

    /// <summary>
    /// Relative mouse movement in Y direction (-1.0 to 1.0).
    /// Only used for mouse movement actions.
    /// </summary>
    [Key(2)]
    public float MouseDeltaY { get; init; }

    /// <summary>
    /// Optional hotkey number (0-9) for hotkey actions.
    /// </summary>
    [Key(3)]
    public int? HotkeyNumber { get; init; }

    /// <summary>
    /// Timestamp when the action was created.
    /// </summary>
    [Key(4)]
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Frame ID that this action was generated for.
    /// </summary>
    [Key(5)]
    public long SourceFrameId { get; init; }

    /// <summary>
    /// Confidence score from the neural network (0.0 to 1.0).
    /// </summary>
    [Key(6)]
    public float Confidence { get; init; }

    /// <summary>
    /// Creates a no-operation action.
    /// </summary>
    public static GameAction NoOp() => new() { Type = ActionType.None };

    /// <summary>
    /// Creates a mouse move action.
    /// </summary>
    public static GameAction MouseMove(float deltaX, float deltaY) => new()
    {
        Type = ActionType.MouseMove,
        MouseDeltaX = Math.Clamp(deltaX, -1f, 1f),
        MouseDeltaY = Math.Clamp(deltaY, -1f, 1f)
    };

    /// <summary>
    /// Creates a left click action.
    /// </summary>
    public static GameAction LeftClick() => new() { Type = ActionType.LeftClick };

    /// <summary>
    /// Creates a right click action.
    /// </summary>
    public static GameAction RightClick() => new() { Type = ActionType.RightClick };
}

/// <summary>
/// Types of actions that can be performed in the game.
/// </summary>
public enum ActionType
{
    /// <summary>No action.</summary>
    None = 0,

    // Mouse movement (8 directions + neutral)
    /// <summary>Move mouse in relative direction.</summary>
    MouseMove = 1,

    // Mouse clicks
    /// <summary>Left mouse button click.</summary>
    LeftClick = 10,
    /// <summary>Right mouse button click.</summary>
    RightClick = 11,
    /// <summary>Double left click.</summary>
    DoubleClick = 12,

    // Drag operations (for unit selection)
    /// <summary>Start drag operation.</summary>
    DragStart = 20,
    /// <summary>End drag operation.</summary>
    DragEnd = 21,
    /// <summary>Drag-select: creates selection box around current mouse position.</summary>
    DragSelect = 22,

    // Hotkeys (unit groups)
    /// <summary>Press number key 1-0.</summary>
    Hotkey = 30,
    /// <summary>Ctrl + number key (assign group).</summary>
    HotkeyCtrl = 31,

    // Scroll
    /// <summary>Scroll wheel up.</summary>
    ScrollUp = 40,
    /// <summary>Scroll wheel down.</summary>
    ScrollDown = 41,

    // Special keys
    /// <summary>Escape key.</summary>
    Escape = 50,
    /// <summary>Enter key.</summary>
    Enter = 51
}
