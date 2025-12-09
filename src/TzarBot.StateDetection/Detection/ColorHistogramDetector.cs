using System.Diagnostics;
using OpenCvSharp;
using TzarBot.Common.Models;

namespace TzarBot.StateDetection.Detection;

/// <summary>
/// Detects game states using color histogram analysis.
///
/// This detector analyzes the color distribution in specific screen regions
/// to identify game states. It serves as a backup/complementary method to
/// template matching.
///
/// Color signatures:
/// - Victory: Typically golden/yellow dominant colors
/// - Defeat: Red/dark dominant colors
/// - InGame: Mixed colors with characteristic UI elements
/// - MainMenu: Specific menu background colors
/// - Loading: Dark background with progress bar colors
/// </summary>
public sealed class ColorHistogramDetector : IGameStateDetector, IDisposable
{
    private readonly DetectionConfig _config;
    private readonly Dictionary<GameState, ColorSignature> _signatures = new();
    private bool _disposed;

    public string Name => "ColorHistogram";

    /// <summary>
    /// Creates a new color histogram detector with the specified configuration.
    /// </summary>
    public ColorHistogramDetector(DetectionConfig? config = null)
    {
        _config = config ?? DetectionConfig.Default();
        InitializeSignatures();
    }

    private void InitializeSignatures()
    {
        // Victory screen signature: gold/yellow colors
        // Typical Tzar victory has golden text and bright background
        _signatures[GameState.Victory] = new ColorSignature
        {
            DominantHue = 30, // Yellow-gold in HSV (0-180 range)
            HueTolerance = 20,
            MinSaturation = 100,
            MinValue = 150,
            RequiredRatio = 0.15f // At least 15% of pixels should match
        };

        // Defeat screen signature: red/dark colors
        // Defeat typically shows red text/elements
        _signatures[GameState.Defeat] = new ColorSignature
        {
            DominantHue = 0, // Red in HSV
            HueTolerance = 15,
            MinSaturation = 100,
            MinValue = 100,
            RequiredRatio = 0.10f
        };

        // Main menu signature: menu background colors
        // (these values should be calibrated from actual game screenshots)
        _signatures[GameState.MainMenu] = new ColorSignature
        {
            DominantHue = 20, // Brown/sepia typical for Tzar menus
            HueTolerance = 25,
            MinSaturation = 50,
            MinValue = 80,
            RequiredRatio = 0.30f
        };

        // Loading screen: dark with some bright elements
        _signatures[GameState.Loading] = new ColorSignature
        {
            IsDark = true,
            MaxAverageBrightness = 60,
            RequiredRatio = 0.70f
        };
    }

    /// <inheritdoc />
    public bool SupportsState(GameState state)
        => _signatures.ContainsKey(state);

    /// <inheritdoc />
    public DetectionResult Detect(ScreenFrame frame)
    {
        if (frame == null || frame.Data == null || frame.Data.Length == 0)
        {
            return DetectionResult.Failed(Name, "Invalid frame");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            using var sourceMat = ConvertFrameToMat(frame);
            if (sourceMat.Empty())
            {
                return DetectionResult.Failed(Name, "Failed to convert frame to Mat");
            }

            // Convert to HSV for color analysis
            using var hsvMat = new Mat();
            Cv2.CvtColor(sourceMat, hsvMat, ColorConversionCodes.BGR2HSV);

            DetectionResult? bestResult = null;
            float bestScore = 0f;

            // Check each signature
            foreach (var (state, signature) in _signatures)
            {
                var region = GetRegionForState(state);
                var score = AnalyzeRegion(hsvMat, region, signature, frame.Width, frame.Height);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestResult = DetectionResult.Success(
                        state,
                        score,
                        Name,
                        diagnosticInfo: $"Color analysis score: {score:F3}");
                }
            }

            sw.Stop();

            if (bestResult != null && bestScore >= _config.HistogramMatchThreshold)
            {
                return DetectionResult.Success(
                    bestResult.State,
                    bestScore,
                    Name,
                    sw.Elapsed.TotalMilliseconds,
                    bestResult.DiagnosticInfo);
            }

            // If no good match for specific states, try to detect InGame
            var inGameScore = DetectInGame(sourceMat, hsvMat, frame.Width, frame.Height);
            if (inGameScore >= _config.HistogramMatchThreshold)
            {
                return DetectionResult.Success(
                    GameState.InGame,
                    inGameScore,
                    Name,
                    sw.Elapsed.TotalMilliseconds,
                    $"InGame detection score: {inGameScore:F3}");
            }

            return DetectionResult.Success(
                GameState.Unknown,
                bestScore,
                Name,
                sw.Elapsed.TotalMilliseconds,
                $"Best match: {bestResult?.State}, score: {bestScore:F3}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return DetectionResult.Failed(Name, $"Detection error: {ex.Message}");
        }
    }

    private RegionConfig GetRegionForState(GameState state)
    {
        return state switch
        {
            GameState.Victory => _config.VictoryRegion,
            GameState.Defeat => _config.DefeatRegion,
            GameState.MainMenu => _config.MenuRegion,
            GameState.Loading => _config.LoadingBarRegion,
            GameState.InGame => _config.MinimapRegion,
            _ => new RegionConfig { X = 0, Y = 0, Width = 1, Height = 1 }
        };
    }

    private float AnalyzeRegion(Mat hsvMat, RegionConfig region, ColorSignature signature, int width, int height)
    {
        var (x, y, w, h) = region.ToAbsolute(width, height);

        // Ensure bounds
        x = Math.Max(0, Math.Min(x, width - 1));
        y = Math.Max(0, Math.Min(y, height - 1));
        w = Math.Min(w, width - x);
        h = Math.Min(h, height - y);

        if (w <= 0 || h <= 0) return 0f;

        using var roi = new Mat(hsvMat, new Rect(x, y, w, h));

        if (signature.IsDark)
        {
            return AnalyzeDarkness(roi, signature);
        }
        else
        {
            return AnalyzeColorSignature(roi, signature);
        }
    }

    private float AnalyzeColorSignature(Mat hsvRoi, ColorSignature signature)
    {
        // Count pixels matching the color signature
        int totalPixels = hsvRoi.Rows * hsvRoi.Cols;
        int matchingPixels = 0;

        // Create mask for the target color range
        // Handle hue wrap-around for red (hue near 0 or 180)
        using var mask = new Mat();

        if (signature.DominantHue < signature.HueTolerance)
        {
            // Red color wrap-around case
            using var lowerMask = new Mat();
            using var upperMask = new Mat();

            Cv2.InRange(
                hsvRoi,
                new Scalar(0, signature.MinSaturation, signature.MinValue),
                new Scalar(signature.DominantHue + signature.HueTolerance, 255, 255),
                lowerMask);

            Cv2.InRange(
                hsvRoi,
                new Scalar(180 - (signature.HueTolerance - signature.DominantHue), signature.MinSaturation, signature.MinValue),
                new Scalar(180, 255, 255),
                upperMask);

            Cv2.BitwiseOr(lowerMask, upperMask, mask);
        }
        else if (signature.DominantHue + signature.HueTolerance > 180)
        {
            // Wrap-around at upper end
            using var lowerMask = new Mat();
            using var upperMask = new Mat();

            Cv2.InRange(
                hsvRoi,
                new Scalar(signature.DominantHue - signature.HueTolerance, signature.MinSaturation, signature.MinValue),
                new Scalar(180, 255, 255),
                lowerMask);

            Cv2.InRange(
                hsvRoi,
                new Scalar(0, signature.MinSaturation, signature.MinValue),
                new Scalar((signature.DominantHue + signature.HueTolerance) - 180, 255, 255),
                upperMask);

            Cv2.BitwiseOr(lowerMask, upperMask, mask);
        }
        else
        {
            // Normal case
            Cv2.InRange(
                hsvRoi,
                new Scalar(signature.DominantHue - signature.HueTolerance, signature.MinSaturation, signature.MinValue),
                new Scalar(signature.DominantHue + signature.HueTolerance, 255, 255),
                mask);
        }

        matchingPixels = Cv2.CountNonZero(mask);
        float ratio = (float)matchingPixels / totalPixels;

        // Calculate confidence based on how much the ratio exceeds the required threshold
        if (ratio >= signature.RequiredRatio)
        {
            // Scale confidence: at threshold = 0.5, at 2x threshold = 1.0
            float confidence = Math.Min(1f, 0.5f + (ratio - signature.RequiredRatio) / signature.RequiredRatio * 0.5f);
            return confidence;
        }
        else
        {
            // Below threshold but still some match
            return ratio / signature.RequiredRatio * 0.5f;
        }
    }

    private float AnalyzeDarkness(Mat hsvRoi, ColorSignature signature)
    {
        // Calculate average brightness (V channel)
        Mat[] channels;
        Cv2.Split(hsvRoi, out channels);

        try
        {
            var avgBrightness = Cv2.Mean(channels[2]).Val0; // V channel

            if (avgBrightness <= signature.MaxAverageBrightness)
            {
                // The darker, the higher the score (up to a point)
                float darkness = 1f - (float)(avgBrightness / signature.MaxAverageBrightness);
                return Math.Min(1f, 0.5f + darkness * 0.5f);
            }
            else
            {
                // Too bright
                return Math.Max(0f, 1f - (float)((avgBrightness - signature.MaxAverageBrightness) / 100));
            }
        }
        finally
        {
            foreach (var ch in channels)
                ch?.Dispose();
        }
    }

    private float DetectInGame(Mat sourceMat, Mat hsvMat, int width, int height)
    {
        // In-game detection based on presence of UI elements:
        // 1. Resource bar at top (has specific colors/patterns)
        // 2. Minimap at bottom-right (varied colors from terrain)
        // 3. Not too dark (not loading) and not too uniform (not menu)

        float score = 0f;

        // Check resource bar region for UI presence
        var resourceBar = _config.ResourceBarRegion;
        var (rx, ry, rw, rh) = resourceBar.ToAbsolute(width, height);
        if (rw > 0 && rh > 0)
        {
            using var resourceRoi = new Mat(sourceMat, new Rect(rx, ry, rw, rh));
            var stdDev = new Scalar();
            Cv2.MeanStdDev(resourceRoi, out _, out stdDev);

            // UI typically has moderate variation (not uniform)
            float variation = (float)(stdDev.Val0 + stdDev.Val1 + stdDev.Val2) / 3f;
            if (variation > 20 && variation < 80)
            {
                score += 0.3f;
            }
        }

        // Check minimap region for terrain-like variation
        var minimap = _config.MinimapRegion;
        var (mx, my, mw, mh) = minimap.ToAbsolute(width, height);
        if (mw > 0 && mh > 0 && mx + mw <= width && my + mh <= height)
        {
            using var minimapRoi = new Mat(hsvMat, new Rect(mx, my, mw, mh));

            // Minimap should have varied colors (terrain, units)
            var stdDev = new Scalar();
            Cv2.MeanStdDev(minimapRoi, out _, out stdDev);

            float hueVariation = (float)stdDev.Val0;
            if (hueVariation > 15)
            {
                score += 0.3f;
            }
        }

        // Check overall brightness (in-game is typically medium brightness)
        var avgBrightness = Cv2.Mean(hsvMat).Val2;
        if (avgBrightness > 40 && avgBrightness < 200)
        {
            score += 0.2f;
        }

        // Check that it's not a solid color (menu screens often are more uniform)
        var overallStdDev = new Scalar();
        Cv2.MeanStdDev(sourceMat, out _, out overallStdDev);
        float avgStdDev = (float)(overallStdDev.Val0 + overallStdDev.Val1 + overallStdDev.Val2) / 3f;
        if (avgStdDev > 30)
        {
            score += 0.2f;
        }

        return Math.Min(1f, score);
    }

    private static Mat ConvertFrameToMat(ScreenFrame frame)
    {
        var matType = frame.Format switch
        {
            PixelFormat.BGRA32 => MatType.CV_8UC4,
            PixelFormat.RGB24 => MatType.CV_8UC3,
            PixelFormat.Grayscale8 => MatType.CV_8UC1,
            _ => throw new ArgumentException($"Unsupported pixel format: {frame.Format}")
        };

        var mat = new Mat(frame.Height, frame.Width, matType);

        unsafe
        {
            fixed (byte* dataPtr = frame.Data)
            {
                Buffer.MemoryCopy(
                    dataPtr,
                    (void*)mat.DataPointer,
                    frame.Data.Length,
                    frame.Data.Length);
            }
        }

        // Convert to BGR if needed
        if (frame.Format == PixelFormat.BGRA32)
        {
            using var bgra = mat;
            mat = new Mat();
            Cv2.CvtColor(bgra, mat, ColorConversionCodes.BGRA2BGR);
        }
        else if (frame.Format == PixelFormat.Grayscale8)
        {
            using var gray = mat;
            mat = new Mat();
            Cv2.CvtColor(gray, mat, ColorConversionCodes.GRAY2BGR);
        }

        return mat;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

/// <summary>
/// Defines a color signature for a specific game state.
/// </summary>
internal sealed class ColorSignature
{
    /// <summary>
    /// Dominant hue value (0-180 in OpenCV HSV).
    /// </summary>
    public int DominantHue { get; init; }

    /// <summary>
    /// Tolerance for hue matching.
    /// </summary>
    public int HueTolerance { get; init; } = 15;

    /// <summary>
    /// Minimum saturation value (0-255).
    /// </summary>
    public int MinSaturation { get; init; }

    /// <summary>
    /// Minimum value/brightness (0-255).
    /// </summary>
    public int MinValue { get; init; }

    /// <summary>
    /// Required ratio of matching pixels (0.0-1.0).
    /// </summary>
    public float RequiredRatio { get; init; } = 0.1f;

    /// <summary>
    /// If true, this is a dark screen signature (check brightness instead of hue).
    /// </summary>
    public bool IsDark { get; init; }

    /// <summary>
    /// Maximum average brightness for dark signatures.
    /// </summary>
    public int MaxAverageBrightness { get; init; } = 50;
}
