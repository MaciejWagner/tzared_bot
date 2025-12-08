using TzarBot.Common.Models;

namespace TzarBot.NeuralNetwork.Inference;

/// <summary>
/// Interface for neural network inference engines.
/// Processes preprocessed frame data and produces game actions.
/// </summary>
public interface IInferenceEngine : IDisposable
{
    /// <summary>
    /// Runs inference on preprocessed input and returns a game action.
    /// </summary>
    /// <param name="preprocessedInput">
    /// Preprocessed frame stack as float array.
    /// Shape: [1, Channels, Height, Width] flattened.
    /// </param>
    /// <returns>The predicted game action.</returns>
    GameAction Infer(float[] preprocessedInput);

    /// <summary>
    /// Runs inference and returns raw network output without action decoding.
    /// Useful for debugging and analysis.
    /// </summary>
    /// <param name="preprocessedInput">Preprocessed frame stack.</param>
    /// <returns>
    /// Tuple of (mouseOutput, actionOutput):
    /// - mouseOutput: 2 floats for dx, dy in [-1, 1]
    /// - actionOutput: N floats for action probabilities (sum = 1)
    /// </returns>
    (float[] mouseOutput, float[] actionOutput) InferRaw(float[] preprocessedInput);

    /// <summary>
    /// Gets whether GPU acceleration is enabled.
    /// </summary>
    bool IsGpuEnabled { get; }

    /// <summary>
    /// Gets the time taken for the last inference operation.
    /// </summary>
    TimeSpan LastInferenceTime { get; }

    /// <summary>
    /// Gets the average inference time over recent operations.
    /// </summary>
    TimeSpan AverageInferenceTime { get; }

    /// <summary>
    /// Gets the network configuration used by this engine.
    /// </summary>
    Models.NetworkConfig Config { get; }
}
