using MessagePack;

namespace TzarBot.NeuralNetwork.Models;

/// <summary>
/// Configuration for a dense (fully connected) layer in the neural network.
/// Dense layers are evolved by the genetic algorithm.
/// </summary>
[MessagePackObject]
public sealed class DenseLayerConfig
{
    /// <summary>
    /// Number of neurons in this layer.
    /// Valid range: 64-1024 (enforced by GA mutation operators).
    /// </summary>
    [Key(0)]
    public int NeuronCount { get; set; }

    /// <summary>
    /// Activation function for this layer.
    /// Typical: ReLU for hidden layers, Tanh/Softmax for output.
    /// </summary>
    [Key(1)]
    public ActivationType Activation { get; set; }

    /// <summary>
    /// Dropout rate for regularization during training.
    /// Valid range: 0.0-0.5 (0 = no dropout).
    /// Set to 0 during inference.
    /// </summary>
    [Key(2)]
    public float DropoutRate { get; set; }

    /// <summary>
    /// Minimum neuron count for evolved layers.
    /// </summary>
    public const int MinNeurons = 64;

    /// <summary>
    /// Maximum neuron count for evolved layers.
    /// </summary>
    public const int MaxNeurons = 1024;

    /// <summary>
    /// Maximum dropout rate.
    /// </summary>
    public const float MaxDropout = 0.5f;

    /// <summary>
    /// Creates a default hidden layer configuration.
    /// </summary>
    public static DenseLayerConfig CreateHidden(int neurons, float dropout = 0.0f)
    {
        return new DenseLayerConfig
        {
            NeuronCount = Math.Clamp(neurons, MinNeurons, MaxNeurons),
            Activation = ActivationType.ReLU,
            DropoutRate = Math.Clamp(dropout, 0f, MaxDropout)
        };
    }

    /// <summary>
    /// Creates an output layer configuration for mouse position (tanh activation).
    /// </summary>
    public static DenseLayerConfig CreateMouseOutput()
    {
        return new DenseLayerConfig
        {
            NeuronCount = 2, // dx, dy
            Activation = ActivationType.Tanh,
            DropoutRate = 0f
        };
    }

    /// <summary>
    /// Creates an output layer configuration for action selection (softmax activation).
    /// </summary>
    public static DenseLayerConfig CreateActionOutput(int actionCount)
    {
        return new DenseLayerConfig
        {
            NeuronCount = actionCount,
            Activation = ActivationType.Softmax,
            DropoutRate = 0f
        };
    }

    /// <summary>
    /// Validates the configuration is within acceptable bounds.
    /// </summary>
    public bool IsValid()
    {
        return NeuronCount >= 1 &&
               DropoutRate >= 0f && DropoutRate <= MaxDropout &&
               Enum.IsDefined(typeof(ActivationType), Activation);
    }

    public override string ToString()
    {
        var dropout = DropoutRate > 0 ? $", dropout={DropoutRate:F2}" : "";
        return $"Dense({NeuronCount}, {Activation}{dropout})";
    }
}
