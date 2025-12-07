using MessagePack;

namespace TzarBot.Common.Models;

/// <summary>
/// Pixel format of the screen frame.
/// </summary>
public enum PixelFormat
{
    /// <summary>32-bit BGRA format (Blue, Green, Red, Alpha).</summary>
    BGRA32,
    /// <summary>24-bit RGB format.</summary>
    RGB24,
    /// <summary>8-bit grayscale format.</summary>
    Grayscale8
}

/// <summary>
/// Represents a captured screen frame from the game.
/// </summary>
[MessagePackObject]
public sealed class ScreenFrame
{
    /// <summary>
    /// Raw pixel data.
    /// </summary>
    [Key(0)]
    public required byte[] Data { get; init; }

    /// <summary>
    /// Width of the frame in pixels.
    /// </summary>
    [Key(1)]
    public required int Width { get; init; }

    /// <summary>
    /// Height of the frame in pixels.
    /// </summary>
    [Key(2)]
    public required int Height { get; init; }

    /// <summary>
    /// Timestamp in ticks when the frame was captured.
    /// </summary>
    [Key(3)]
    public required long TimestampTicks { get; init; }

    /// <summary>
    /// Pixel format of the frame data.
    /// </summary>
    [Key(4)]
    public required PixelFormat Format { get; init; }

    /// <summary>
    /// Stride (bytes per row) of the image data.
    /// </summary>
    [IgnoreMember]
    public int Stride => Width * GetBytesPerPixel();

    /// <summary>
    /// Gets bytes per pixel based on format.
    /// </summary>
    public int GetBytesPerPixel() => Format switch
    {
        PixelFormat.BGRA32 => 4,
        PixelFormat.RGB24 => 3,
        PixelFormat.Grayscale8 => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(Format))
    };

    /// <summary>
    /// Indicates whether the frame contains valid data.
    /// </summary>
    [IgnoreMember]
    public bool IsValid => Data.Length == Width * Height * GetBytesPerPixel();
}
