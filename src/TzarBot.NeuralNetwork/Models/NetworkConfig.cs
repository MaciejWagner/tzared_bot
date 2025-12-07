using MessagePack;

namespace TzarBot.NeuralNetwork.Models;

/// <summary>
/// Default network configuration for TzarBot.
/// Defines the fixed architecture (input shape, conv layers, output heads).
/// </summary>
[MessagePackObject]
public sealed class NetworkConfig
{
    #region Input Configuration

    /// <summary>
    /// Number of stacked frames in input (temporal information).
    /// </summary>
    [Key(0)]
    public int StackedFrames { get; set; } = 4;

    /// <summary>
    /// Input image width (downscaled from game resolution).
    /// </summary>
    [Key(1)]
    public int InputWidth { get; set; } = 240;

    /// <summary>
    /// Input image height (downscaled from game resolution).
    /// </summary>
    [Key(2)]
    public int InputHeight { get; set; } = 135;

    /// <summary>
    /// Number of input channels (stacked grayscale frames).
    /// </summary>
    [IgnoreMember]
    public int InputChannels => StackedFrames;

    #endregion

    #region Convolutional Layers (Frozen)

    /// <summary>
    /// Convolutional layers configuration (frozen, not evolved).
    /// Default: 32@8x8s4 -> 64@4x4s2 -> 64@3x3s1
    /// </summary>
    [Key(3)]
    public ConvLayerConfig[] ConvLayers { get; set; } = DefaultConvLayers();

    private static ConvLayerConfig[] DefaultConvLayers()
    {
        return new[]
        {
            // Layer 1: 4x240x135 -> 32x59x32
            // Output: floor((240 - 8) / 4) + 1 = 59, floor((135 - 8) / 4) + 1 = 32
            ConvLayerConfig.Create(filterCount: 32, kernelSize: 8, stride: 4),

            // Layer 2: 32x59x32 -> 64x28x15
            // Output: floor((59 - 4) / 2) + 1 = 28, floor((32 - 4) / 2) + 1 = 15
            ConvLayerConfig.Create(filterCount: 64, kernelSize: 4, stride: 2),

            // Layer 3: 64x28x15 -> 64x26x13
            // Output: floor((28 - 3) / 1) + 1 = 26, floor((15 - 3) / 1) + 1 = 13
            ConvLayerConfig.Create(filterCount: 64, kernelSize: 3, stride: 1)
        };
    }

    #endregion

    #region Output Configuration

    /// <summary>
    /// Number of actions in the action output head.
    /// Matches ActionType enum values in TzarBot.Common.
    /// </summary>
    [Key(4)]
    public int ActionCount { get; set; } = 30;

    /// <summary>
    /// Mouse output head has 2 neurons (dx, dy).
    /// </summary>
    [IgnoreMember]
    public int MouseOutputCount => 2;

    #endregion

    #region Calculated Properties

    /// <summary>
    /// Calculates the flattened output size after all conv layers.
    /// This is the input size to the first dense layer.
    /// </summary>
    [IgnoreMember]
    public int FlattenedConvOutputSize
    {
        get
        {
            int width = InputWidth;
            int height = InputHeight;
            int channels = InputChannels;

            foreach (var conv in ConvLayers)
            {
                width = conv.CalculateOutputSize(width);
                height = conv.CalculateOutputSize(height);
                channels = conv.FilterCount;
            }

            return channels * width * height;
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates the default network configuration for TzarBot.
    /// </summary>
    public static NetworkConfig Default()
    {
        return new NetworkConfig();
    }

    /// <summary>
    /// Creates the output layer configurations (mouse + action heads).
    /// </summary>
    public (DenseLayerConfig mouseHead, DenseLayerConfig actionHead) CreateOutputHeads()
    {
        return (
            DenseLayerConfig.CreateMouseOutput(),
            DenseLayerConfig.CreateActionOutput(ActionCount)
        );
    }

    #endregion

    public override string ToString()
    {
        return $"NetworkConfig(input={StackedFrames}x{InputWidth}x{InputHeight}, " +
               $"conv=[{string.Join(" -> ", ConvLayers.Select(c => c.ToString()))}], " +
               $"flatten={FlattenedConvOutputSize}, actions={ActionCount})";
    }
}
