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
    /// </summary>
    public const int MaxMouseDelta = 100;

    /// <summary>
    /// Minimum confidence threshold for non-None actions.
    /// If the highest action probability is below this threshold, return None.
    /// </summary>
    public const float MinConfidenceThreshold = 0.1f;

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
    /// Builds the mapping from output neuron index to ActionType.
    /// Output head has 30 neurons total:
    /// 0: None
    /// 1: MouseMove
    /// 2: LeftClick
    /// 3: RightClick
    /// 4: DoubleClick
    /// 5: DragStart
    /// 6: DragEnd
    /// 7: ScrollUp (skipped for now, use index for clarity)
    /// 8-17: Hotkey 0-9
    /// 18-27: HotkeyCtrl 0-9
    /// 28: Escape
    /// 29: Enter
    /// </summary>
    private static ActionType[] BuildActionTypeMap()
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
}
