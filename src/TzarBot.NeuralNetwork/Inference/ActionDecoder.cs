using TzarBot.Common.Models;

namespace TzarBot.NeuralNetwork.Inference;

/// <summary>
/// Decodes raw neural network output into game actions.
///
/// Network output format:
/// - Mouse head: 2 neurons with Tanh activation (dx, dy in [-1, 1])
/// - Action head: N neurons with Softmax activation (probabilities)
///
/// The decoder performs argmax on action probabilities and scales
/// mouse deltas to screen coordinates.
/// </summary>
public sealed class ActionDecoder
{
    /// <summary>
    /// Maximum mouse delta in pixels. Mouse output is scaled from [-1, 1] to [-MaxDelta, +MaxDelta].
    /// Set to 600 (half screen width) so networks can move across entire screen.
    /// With delta=0.01 -> 6px, delta=0.1 -> 60px, delta=1.0 -> 600px
    /// </summary>
    public const int MaxMouseDelta = 600;

    /// <summary>
    /// Minimum confidence threshold for non-None actions.
    /// If the highest action probability is below this threshold, return None.
    /// For training: use low threshold (0.03) to allow exploration.
    /// For production: use higher threshold (0.1+) for stable behavior.
    /// </summary>
    public const float MinConfidenceThreshold = 0.03f; // ~1/30 for 30 actions

    // Map from output index to ActionType
    private static readonly ActionType[] ActionTypeMap = BuildActionTypeMap();

    /// <summary>
    /// Decodes neural network output into a game action.
    /// </summary>
    /// <param name="mouseOutput">Mouse head output (2 floats: dx, dy in [-1, 1]).</param>
    /// <param name="actionOutput">Action head output (N floats: softmax probabilities).</param>
    /// <param name="frameId">Optional frame ID for action tracking.</param>
    /// <returns>Decoded game action with confidence score.</returns>
    public GameAction Decode(float[] mouseOutput, float[] actionOutput, long frameId = 0)
    {
        if (mouseOutput.Length != 2)
            throw new ArgumentException($"Mouse output must have 2 elements, got {mouseOutput.Length}", nameof(mouseOutput));

        // Extract mouse deltas (scaled from [-1, 1] to [-MaxDelta, MaxDelta])
        float dx = mouseOutput[0];
        float dy = mouseOutput[1];

        // Find best action (argmax)
        int bestActionIndex = ArgMax(actionOutput);
        float confidence = actionOutput[bestActionIndex];

        // Map to ActionType
        ActionType actionType = bestActionIndex < ActionTypeMap.Length
            ? ActionTypeMap[bestActionIndex]
            : ActionType.None;

        // If confidence is too low, default to None
        if (confidence < MinConfidenceThreshold && actionType != ActionType.None)
        {
            actionType = ActionType.None;
            confidence = 1f - confidence; // Confidence in "doing nothing"
        }

        // Build the action
        return new GameAction
        {
            Type = actionType,
            MouseDeltaX = dx,
            MouseDeltaY = dy,
            Confidence = confidence,
            SourceFrameId = frameId,
            Timestamp = DateTime.UtcNow,
            HotkeyNumber = ExtractHotkeyNumber(actionType, bestActionIndex)
        };
    }

    /// <summary>
    /// Gets the action probabilities as a dictionary for debugging.
    /// </summary>
    public Dictionary<ActionType, float> GetActionProbabilities(float[] actionOutput)
    {
        var result = new Dictionary<ActionType, float>();

        for (int i = 0; i < Math.Min(actionOutput.Length, ActionTypeMap.Length); i++)
        {
            var actionType = ActionTypeMap[i];
            if (!result.ContainsKey(actionType))
            {
                result[actionType] = actionOutput[i];
            }
            else
            {
                result[actionType] += actionOutput[i]; // Sum for hotkeys
            }
        }

        return result;
    }

    /// <summary>
    /// Scales mouse output from network range [-1, 1] to screen pixels.
    /// </summary>
    public static (int pixelDx, int pixelDy) ScaleMouseToPixels(float dx, float dy)
    {
        return (
            (int)(dx * MaxMouseDelta),
            (int)(dy * MaxMouseDelta)
        );
    }

    private static int ArgMax(float[] values)
    {
        if (values.Length == 0)
            return 0;

        int maxIndex = 0;
        float maxValue = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > maxValue)
            {
                maxValue = values[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    private static int? ExtractHotkeyNumber(ActionType type, int outputIndex)
    {
        // Hotkeys are indices 8-17 (Hotkey0-9) and 18-27 (HotkeyCtrl0-9)
        if (type == ActionType.Hotkey && outputIndex >= 8 && outputIndex <= 17)
        {
            return (outputIndex - 8) % 10; // 0-9
        }
        if (type == ActionType.HotkeyCtrl && outputIndex >= 18 && outputIndex <= 27)
        {
            return (outputIndex - 18) % 10; // 0-9
        }

        return null;
    }

    /// <summary>
    /// Size of drag selection box in pixels (centered on mouse position).
    /// Drag action creates a selection box of DragSize x DragSize pixels.
    /// </summary>
    public const int DragSize = 150;

    /// <summary>
    /// Builds the mapping from output neuron index to ActionType.
    ///
    /// CURRICULUM PHASE 2: LeftClick + RightClick + MouseMove + DragSelect
    /// Network can: LeftClick (select single) or DragSelect (select area) -> RightClick (move)
    /// All 30 output neurons mapped cyclically to 4 actions (25% each).
    ///
    /// Output mapping:
    /// 0, 4, 8, 12, 16, 20, 24, 28: LeftClick (select single unit)
    /// 1, 5, 9, 13, 17, 21, 25, 29: RightClick (move/attack)
    /// 2, 6, 10, 14, 18, 22, 26: MouseMove (explore)
    /// 3, 7, 11, 15, 19, 23, 27: DragSelect (select area 150x150 around mouse)
    /// </summary>
    private static ActionType[] BuildActionTypeMap()
    {
        // CURRICULUM PHASE 2: Select + Move + Explore + DragSelect
        // No auto-select - network must click/drag on units to select them
        // DragSelect creates 150x150 selection box - good for selecting multiple units
        var mouseActions = new ActionType[]
        {
            ActionType.LeftClick,   // 0 - select single unit
            ActionType.RightClick,  // 1 - move/attack command
            ActionType.MouseMove,   // 2 - move cursor to find units
            ActionType.DragSelect,  // 3 - drag-select 150x150 area around mouse
        };

        // Map all 30 neurons to these 4 actions cyclically
        var map = new ActionType[30];
        for (int i = 0; i < 30; i++)
        {
            map[i] = mouseActions[i % 4];
        }
        return map;
    }

    /* FULL ACTION SPACE (uncomment after first VICTORY):
    private static ActionType[] BuildActionTypeMapFull()
    {
        return new ActionType[]
        {
            ActionType.None,        // 0
            ActionType.MouseMove,   // 1
            ActionType.LeftClick,   // 2
            ActionType.RightClick,  // 3
            ActionType.DoubleClick, // 4
            ActionType.DragStart,   // 5
            ActionType.DragEnd,     // 6
            ActionType.ScrollUp,    // 7
            ActionType.Hotkey,      // 8 - Hotkey 1
            ActionType.Hotkey,      // 9 - Hotkey 2
            ActionType.Hotkey,      // 10 - Hotkey 3
            ActionType.Hotkey,      // 11 - Hotkey 4
            ActionType.Hotkey,      // 12 - Hotkey 5
            ActionType.Hotkey,      // 13 - Hotkey 6
            ActionType.Hotkey,      // 14 - Hotkey 7
            ActionType.Hotkey,      // 15 - Hotkey 8
            ActionType.Hotkey,      // 16 - Hotkey 9
            ActionType.Hotkey,      // 17 - Hotkey 0
            ActionType.HotkeyCtrl,  // 18 - Ctrl+1
            ActionType.HotkeyCtrl,  // 19 - Ctrl+2
            ActionType.HotkeyCtrl,  // 20 - Ctrl+3
            ActionType.HotkeyCtrl,  // 21 - Ctrl+4
            ActionType.HotkeyCtrl,  // 22 - Ctrl+5
            ActionType.HotkeyCtrl,  // 23 - Ctrl+6
            ActionType.HotkeyCtrl,  // 24 - Ctrl+7
            ActionType.HotkeyCtrl,  // 25 - Ctrl+8
            ActionType.HotkeyCtrl,  // 26 - Ctrl+9
            ActionType.HotkeyCtrl,  // 27 - Ctrl+0
            ActionType.Escape,      // 28
            ActionType.Enter        // 29
        };
    }
    */
}
