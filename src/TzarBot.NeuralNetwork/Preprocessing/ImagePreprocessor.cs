using System.Buffers;
using System.Runtime.CompilerServices;
using TzarBot.Common.Models;

namespace TzarBot.NeuralNetwork.Preprocessing;

/// <summary>
/// Image preprocessing pipeline for neural network input.
///
/// Pipeline stages:
/// 1. Crop to game area (optional)
/// 2. Downscale using bilinear interpolation
/// 3. Convert to grayscale (optional)
/// 4. Normalize values to [0-1] or [-1,1]
/// 5. Stack multiple frames for temporal context
///
/// Performance considerations:
/// - Uses ArrayPool for intermediate buffers
/// - Processes directly from BGRA32 to grayscale float
/// - Bilinear interpolation implemented without external dependencies
/// </summary>
public sealed class ImagePreprocessor : IDisposable
{
    private readonly PreprocessorConfig _config;
    private readonly FrameBuffer _frameBuffer;
    private readonly float[] _singleFrameBuffer;
    private readonly float[] _outputTensor;
    private bool _disposed;

    /// <summary>
    /// Gets the preprocessor configuration.
    /// </summary>
    public PreprocessorConfig Config => _config;

    /// <summary>
    /// Gets the frame buffer for accessing individual frames.
    /// </summary>
    public FrameBuffer FrameBuffer => _frameBuffer;

    /// <summary>
    /// Gets the size of the output tensor (FrameStackSize * Height * Width).
    /// </summary>
    public int OutputTensorSize => _config.OutputTensorSize;

    /// <summary>
    /// Creates a new image preprocessor with the specified configuration.
    /// </summary>
    /// <param name="config">Preprocessing configuration (null for defaults)</param>
    public ImagePreprocessor(PreprocessorConfig? config = null)
    {
        _config = config ?? PreprocessorConfig.Default();

        if (!_config.IsValid())
            throw new ArgumentException("Invalid preprocessor configuration", nameof(config));

        _frameBuffer = new FrameBuffer(_config.FrameStackSize, _config.SingleFrameSize);
        _singleFrameBuffer = new float[_config.SingleFrameSize];
        _outputTensor = new float[_config.OutputTensorSize];
    }

    /// <summary>
    /// Processes a screen frame and adds it to the frame buffer.
    /// Returns true if the buffer has enough frames for inference.
    /// </summary>
    /// <param name="frame">Input screen frame (BGRA32 format expected)</param>
    /// <returns>True if frame buffer is full (ready for inference)</returns>
    public bool ProcessFrame(ScreenFrame frame)
    {
        ThrowIfDisposed();

        if (frame.Format != PixelFormat.BGRA32)
        {
            throw new ArgumentException(
                $"Expected BGRA32 format, got {frame.Format}",
                nameof(frame));
        }

        // Process frame into single frame buffer
        ProcessFrameInternal(frame.Data, frame.Width, frame.Height, _singleFrameBuffer);

        // Add to frame buffer
        _frameBuffer.AddFrame(_singleFrameBuffer);

        return _frameBuffer.IsFull;
    }

    /// <summary>
    /// Gets the current stacked frames as a tensor ready for neural network input.
    /// Shape: [1, FrameStackSize, Height, Width] flattened to 1D array.
    ///
    /// Returns a copy of the data (thread-safe).
    /// </summary>
    public float[] GetTensor()
    {
        ThrowIfDisposed();
        return _frameBuffer.GetStackedFrames();
    }

    /// <summary>
    /// Gets the current stacked frames into a pre-allocated buffer.
    /// More efficient than GetTensor() for hot paths.
    /// </summary>
    /// <param name="destination">Destination buffer (must be OutputTensorSize)</param>
    public void GetTensor(Span<float> destination)
    {
        ThrowIfDisposed();
        _frameBuffer.GetStackedFrames(destination);
    }

    /// <summary>
    /// Processes a single frame without adding to the frame buffer.
    /// Useful for testing or one-off preprocessing.
    /// </summary>
    /// <param name="frame">Input screen frame</param>
    /// <returns>Preprocessed frame as float array [Height * Width]</returns>
    public float[] ProcessSingleFrame(ScreenFrame frame)
    {
        ThrowIfDisposed();

        if (frame.Format != PixelFormat.BGRA32)
        {
            throw new ArgumentException(
                $"Expected BGRA32 format, got {frame.Format}",
                nameof(frame));
        }

        float[] result = new float[_config.SingleFrameSize];
        ProcessFrameInternal(frame.Data, frame.Width, frame.Height, result);
        return result;
    }

    /// <summary>
    /// Internal processing: crop, resize, grayscale, normalize.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ProcessFrameInternal(byte[] sourceData, int sourceWidth, int sourceHeight, float[] destination)
    {
        int cropX = _config.HasCropRegion ? _config.CropRegion.X : 0;
        int cropY = _config.HasCropRegion ? _config.CropRegion.Y : 0;
        int cropWidth = _config.HasCropRegion ? _config.CropRegion.Width : sourceWidth;
        int cropHeight = _config.HasCropRegion ? _config.CropRegion.Height : sourceHeight;

        int outWidth = _config.OutputWidth;
        int outHeight = _config.OutputHeight;

        float scaleX = (float)cropWidth / outWidth;
        float scaleY = (float)cropHeight / outHeight;

        int sourceStride = sourceWidth * 4; // BGRA32

        for (int outY = 0; outY < outHeight; outY++)
        {
            for (int outX = 0; outX < outWidth; outX++)
            {
                // Map output coordinates to source coordinates
                float srcXf = outX * scaleX;
                float srcYf = outY * scaleY;

                // Bilinear interpolation coordinates
                int srcX0 = (int)srcXf;
                int srcY0 = (int)srcYf;
                int srcX1 = Math.Min(srcX0 + 1, cropWidth - 1);
                int srcY1 = Math.Min(srcY0 + 1, cropHeight - 1);

                float xFrac = srcXf - srcX0;
                float yFrac = srcYf - srcY0;

                // Add crop offset
                srcX0 += cropX;
                srcY0 += cropY;
                srcX1 += cropX;
                srcY1 += cropY;

                // Clamp to source bounds
                srcX0 = Math.Clamp(srcX0, 0, sourceWidth - 1);
                srcY0 = Math.Clamp(srcY0, 0, sourceHeight - 1);
                srcX1 = Math.Clamp(srcX1, 0, sourceWidth - 1);
                srcY1 = Math.Clamp(srcY1, 0, sourceHeight - 1);

                float value;

                if (_config.UseGrayscale)
                {
                    // Sample 4 corners and interpolate
                    float v00 = GetGrayscaleValue(sourceData, srcX0, srcY0, sourceStride);
                    float v10 = GetGrayscaleValue(sourceData, srcX1, srcY0, sourceStride);
                    float v01 = GetGrayscaleValue(sourceData, srcX0, srcY1, sourceStride);
                    float v11 = GetGrayscaleValue(sourceData, srcX1, srcY1, sourceStride);

                    // Bilinear interpolation
                    float top = v00 * (1 - xFrac) + v10 * xFrac;
                    float bottom = v01 * (1 - xFrac) + v11 * xFrac;
                    value = top * (1 - yFrac) + bottom * yFrac;
                }
                else
                {
                    // For non-grayscale, use luminance of the center pixel
                    value = GetGrayscaleValue(sourceData, srcX0, srcY0, sourceStride);
                }

                // Normalize
                value = NormalizeValue(value);

                destination[outY * outWidth + outX] = value;
            }
        }
    }

    /// <summary>
    /// Gets grayscale value from BGRA32 pixel using luminance formula.
    /// Formula: 0.299*R + 0.587*G + 0.114*B (ITU-R BT.601)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetGrayscaleValue(byte[] data, int x, int y, int stride)
    {
        int offset = y * stride + x * 4;

        // BGRA format: offset+0=B, offset+1=G, offset+2=R, offset+3=A
        byte b = data[offset];
        byte g = data[offset + 1];
        byte r = data[offset + 2];

        // ITU-R BT.601 luminance formula (range 0-255)
        return 0.299f * r + 0.587f * g + 0.114f * b;
    }

    /// <summary>
    /// Normalizes a value from [0-255] to the configured range.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float NormalizeValue(float value)
    {
        return _config.NormalizationMode switch
        {
            NormalizationMode.ZeroToOne => value / 255f,
            NormalizationMode.MinusOneToOne => (value / 127.5f) - 1f,
            _ => value / 255f
        };
    }

    /// <summary>
    /// Resets the frame buffer, clearing all stored frames.
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        _frameBuffer.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImagePreprocessor));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _frameBuffer.Dispose();
    }
}
