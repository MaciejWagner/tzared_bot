namespace TzarBot.StateDetection.Detection;

/// <summary>
/// Configuration for game state detection.
/// </summary>
public sealed class DetectionConfig
{
    /// <summary>
    /// Directory containing template images.
    /// </summary>
    public string TemplateDirectory { get; init; } = "Templates";

    /// <summary>
    /// Minimum confidence threshold for template matching to be considered a match.
    /// Range: 0.0 to 1.0. Default: 0.85 (85% match required).
    /// </summary>
    public float TemplateMatchThreshold { get; init; } = 0.85f;

    /// <summary>
    /// Minimum confidence threshold for color histogram matching.
    /// Range: 0.0 to 1.0. Default: 0.75 (75% match required).
    /// </summary>
    public float HistogramMatchThreshold { get; init; } = 0.75f;

    /// <summary>
    /// Minimum confidence threshold for a detection to be considered reliable.
    /// Range: 0.0 to 1.0. Default: 0.8 (80% confidence required).
    /// </summary>
    public float ReliabilityThreshold { get; init; } = 0.8f;

    /// <summary>
    /// Screen width expected for templates (for scaling).
    /// </summary>
    public int ReferenceWidth { get; init; } = 1920;

    /// <summary>
    /// Screen height expected for templates (for scaling).
    /// </summary>
    public int ReferenceHeight { get; init; } = 1080;

    /// <summary>
    /// Enable multi-scale template matching for different resolutions.
    /// </summary>
    public bool EnableMultiScaleMatching { get; init; } = true;

    /// <summary>
    /// Scale factors to try for multi-scale matching.
    /// </summary>
    public float[] ScaleFactors { get; init; } = [0.5f, 0.75f, 1.0f, 1.25f, 1.5f];

    /// <summary>
    /// Victory screen detection region (relative coordinates 0.0-1.0).
    /// </summary>
    public RegionConfig VictoryRegion { get; init; } = new()
    {
        X = 0.3f,
        Y = 0.2f,
        Width = 0.4f,
        Height = 0.2f
    };

    /// <summary>
    /// Defeat screen detection region (relative coordinates 0.0-1.0).
    /// </summary>
    public RegionConfig DefeatRegion { get; init; } = new()
    {
        X = 0.3f,
        Y = 0.2f,
        Width = 0.4f,
        Height = 0.2f
    };

    /// <summary>
    /// Minimap detection region for in-game detection (relative coordinates 0.0-1.0).
    /// Tzar minimap is typically in bottom-right corner.
    /// </summary>
    public RegionConfig MinimapRegion { get; init; } = new()
    {
        X = 0.75f,
        Y = 0.75f,
        Width = 0.25f,
        Height = 0.25f
    };

    /// <summary>
    /// Resource bar detection region (top of screen).
    /// </summary>
    public RegionConfig ResourceBarRegion { get; init; } = new()
    {
        X = 0.0f,
        Y = 0.0f,
        Width = 1.0f,
        Height = 0.05f
    };

    /// <summary>
    /// Menu button region for main menu detection.
    /// </summary>
    public RegionConfig MenuRegion { get; init; } = new()
    {
        X = 0.3f,
        Y = 0.3f,
        Width = 0.4f,
        Height = 0.4f
    };

    /// <summary>
    /// Loading bar region for loading screen detection.
    /// </summary>
    public RegionConfig LoadingBarRegion { get; init; } = new()
    {
        X = 0.2f,
        Y = 0.8f,
        Width = 0.6f,
        Height = 0.1f
    };

    /// <summary>
    /// Stats region for OCR extraction (end-game statistics).
    /// </summary>
    public RegionConfig StatsRegion { get; init; } = new()
    {
        X = 0.2f,
        Y = 0.3f,
        Width = 0.6f,
        Height = 0.5f
    };

    /// <summary>
    /// Creates default configuration.
    /// </summary>
    public static DetectionConfig Default() => new();
}

/// <summary>
/// Configuration for a screen region (relative coordinates).
/// </summary>
public sealed class RegionConfig
{
    /// <summary>
    /// X coordinate (0.0-1.0, relative to screen width).
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Y coordinate (0.0-1.0, relative to screen height).
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Width (0.0-1.0, relative to screen width).
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// Height (0.0-1.0, relative to screen height).
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// Converts relative coordinates to absolute pixel coordinates.
    /// </summary>
    public (int X, int Y, int Width, int Height) ToAbsolute(int screenWidth, int screenHeight)
    {
        return (
            (int)(X * screenWidth),
            (int)(Y * screenHeight),
            (int)(Width * screenWidth),
            (int)(Height * screenHeight)
        );
    }
}
