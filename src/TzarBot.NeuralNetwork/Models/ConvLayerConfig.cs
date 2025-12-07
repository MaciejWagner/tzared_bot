using MessagePack;

namespace TzarBot.NeuralNetwork.Models;

/// <summary>
/// Configuration for a convolutional layer in the neural network.
/// Conv layers are frozen (not evolved) - they extract features from the input image.
/// </summary>
[MessagePackObject]
public sealed class ConvLayerConfig
{
    /// <summary>
    /// Number of convolutional filters (output channels).
    /// Typical values: 32, 64, 128.
    /// </summary>
    [Key(0)]
    public int FilterCount { get; set; }

    /// <summary>
    /// Size of the convolutional kernel (square: KernelSize x KernelSize).
    /// Typical values: 3, 4, 8.
    /// </summary>
    [Key(1)]
    public int KernelSize { get; set; }

    /// <summary>
    /// Stride of the convolution (step size).
    /// Stride > 1 reduces spatial dimensions.
    /// </summary>
    [Key(2)]
    public int Stride { get; set; }

    /// <summary>
    /// Activation function applied after convolution.
    /// </summary>
    [Key(3)]
    public ActivationType Activation { get; set; }

    /// <summary>
    /// Padding mode. Default is 0 (valid padding, no padding).
    /// </summary>
    [Key(4)]
    public int Padding { get; set; }

    /// <summary>
    /// Creates a default ReLU-activated convolutional layer.
    /// </summary>
    public static ConvLayerConfig Create(int filterCount, int kernelSize, int stride, int padding = 0)
    {
        return new ConvLayerConfig
        {
            FilterCount = filterCount,
            KernelSize = kernelSize,
            Stride = stride,
            Padding = padding,
            Activation = ActivationType.ReLU
        };
    }

    /// <summary>
    /// Calculates output spatial dimension after applying this convolution.
    /// Formula: floor((input + 2*padding - kernel) / stride) + 1
    /// </summary>
    public int CalculateOutputSize(int inputSize)
    {
        return ((inputSize + 2 * Padding - KernelSize) / Stride) + 1;
    }

    public override string ToString()
    {
        return $"Conv2D({FilterCount}, {KernelSize}x{KernelSize}, stride={Stride}, {Activation})";
    }
}
