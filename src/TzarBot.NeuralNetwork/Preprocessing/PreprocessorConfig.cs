using System.Drawing;
using MessagePack;

namespace TzarBot.NeuralNetwork.Preprocessing;

/// <summary>
/// Normalization modes for image preprocessing.
/// </summary>
public enum NormalizationMode
{
    /// <summary>Normalize to [0.0, 1.0] range.</summary>
    ZeroToOne,
    /// <summary>Normalize to [-1.0, 1.0] range.</summary>
    MinusOneToOne
}

/// <summary>
/// Configuration for image preprocessing pipeline.
/// Defines input/output dimensions, crop region, and processing options.
/// </summary>
[MessagePackObject]
public sealed class PreprocessorConfig
{
    #region Input Configuration

    /// <summary>
    /// Source image width (e.g., 1920 for Full HD).
    /// </summary>
    [Key(0)]
    public int InputWidth { get; set; } = 1920;

    /// <summary>
    /// Source image height (e.g., 1080 for Full HD).
    /// </summary>
    [Key(1)]
    public int InputHeight { get; set; } = 1080;

    #endregion

    #region Output Configuration

    /// <summary>
    /// Target width after downscaling (default: 240).
    /// </summary>
    [Key(2)]
    public int OutputWidth { get; set; } = 240;

    /// <summary>
    /// Target height after downscaling (default: 135).
    /// </summary>
    [Key(3)]
    public int OutputHeight { get; set; } = 135;

    #endregion

    #region Crop Region

    /// <summary>
    /// Rectangle defining the crop region in source coordinates.
    /// If empty (0,0,0,0), no cropping is performed.
    /// </summary>
    [Key(4)]
    public Rectangle CropRegion { get; set; } = Rectangle.Empty;

    /// <summary>
    /// Whether a crop region is defined.
    /// </summary>
    [IgnoreMember]
    public bool HasCropRegion => CropRegion.Width > 0 && CropRegion.Height > 0;

    #endregion

    #region Processing Options

    /// <summary>
    /// Convert image to grayscale (default: true for performance).
    /// </summary>
    [Key(5)]
    public bool UseGrayscale { get; set; } = true;

    /// <summary>
    /// Number of frames to stack for temporal context (default: 4).
    /// </summary>
    [Key(6)]
    public int FrameStackSize { get; set; } = 4;

    /// <summary>
    /// Normalization mode for output values.
    /// </summary>
    [Key(7)]
    public NormalizationMode NormalizationMode { get; set; } = NormalizationMode.ZeroToOne;

    #endregion

    #region Calculated Properties

    /// <summary>
    /// Total elements in the output tensor (FrameStackSize * OutputHeight * OutputWidth).
    /// </summary>
    [IgnoreMember]
    public int OutputTensorSize => FrameStackSize * OutputHeight * OutputWidth;

    /// <summary>
    /// Size of a single processed frame (OutputHeight * OutputWidth).
    /// </summary>
    [IgnoreMember]
    public int SingleFrameSize => OutputHeight * OutputWidth;

    /// <summary>
    /// Effective input width after cropping (or InputWidth if no crop).
    /// </summary>
    [IgnoreMember]
    public int EffectiveInputWidth => HasCropRegion ? CropRegion.Width : InputWidth;

    /// <summary>
    /// Effective input height after cropping (or InputHeight if no crop).
    /// </summary>
    [IgnoreMember]
    public int EffectiveInputHeight => HasCropRegion ? CropRegion.Height : InputHeight;

    /// <summary>
    /// Horizontal scale factor for downscaling.
    /// </summary>
    [IgnoreMember]
    public float ScaleX => (float)EffectiveInputWidth / OutputWidth;

    /// <summary>
    /// Vertical scale factor for downscaling.
    /// </summary>
    [IgnoreMember]
    public float ScaleY => (float)EffectiveInputHeight / OutputHeight;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates default configuration for TzarBot (1920x1080 -> 240x135, grayscale, 4 frames).
    /// </summary>
    public static PreprocessorConfig Default()
    {
        return new PreprocessorConfig();
    }

    /// <summary>
    /// Creates configuration with custom dimensions.
    /// </summary>
    /// <param name="inputWidth">Source width</param>
    /// <param name="inputHeight">Source height</param>
    /// <param name="outputWidth">Target width</param>
    /// <param name="outputHeight">Target height</param>
    public static PreprocessorConfig Create(
        int inputWidth, int inputHeight,
        int outputWidth, int outputHeight)
    {
        return new PreprocessorConfig
        {
            InputWidth = inputWidth,
            InputHeight = inputHeight,
            OutputWidth = outputWidth,
            OutputHeight = outputHeight
        };
    }

    /// <summary>
    /// Creates configuration with crop region.
    /// </summary>
    public static PreprocessorConfig CreateWithCrop(
        int inputWidth, int inputHeight,
        Rectangle cropRegion,
        int outputWidth = 240, int outputHeight = 135)
    {
        return new PreprocessorConfig
        {
            InputWidth = inputWidth,
            InputHeight = inputHeight,
            CropRegion = cropRegion,
            OutputWidth = outputWidth,
            OutputHeight = outputHeight
        };
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public bool IsValid()
    {
        if (InputWidth <= 0 || InputHeight <= 0)
            return false;

        if (OutputWidth <= 0 || OutputHeight <= 0)
            return false;

        if (FrameStackSize < 1 || FrameStackSize > 16)
            return false;

        if (HasCropRegion)
        {
            // Crop region must be within input bounds
            if (CropRegion.X < 0 || CropRegion.Y < 0)
                return false;

            if (CropRegion.Right > InputWidth || CropRegion.Bottom > InputHeight)
                return false;
        }

        return true;
    }

    #endregion

    public override string ToString()
    {
        var crop = HasCropRegion ? $" crop={CropRegion}" : "";
        return $"PreprocessorConfig({InputWidth}x{InputHeight} -> {OutputWidth}x{OutputHeight}, " +
               $"stack={FrameStackSize}, gray={UseGrayscale}, norm={NormalizationMode}{crop})";
    }
}
