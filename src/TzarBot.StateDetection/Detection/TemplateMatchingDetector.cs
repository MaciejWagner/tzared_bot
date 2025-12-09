using System.Diagnostics;
using OpenCvSharp;
using TzarBot.Common.Models;

namespace TzarBot.StateDetection.Detection;

/// <summary>
/// Detects game states using OpenCV template matching.
///
/// Template matching works by sliding a template image over the source image
/// and computing a similarity score at each position. The maximum score location
/// indicates the best match.
///
/// For game state detection, we use normalized cross-correlation (TM_CCOEFF_NORMED)
/// which is robust to lighting variations and produces scores in [-1, 1] range.
/// </summary>
public sealed class TemplateMatchingDetector : IInitializableDetector, IDisposableDetector
{
    private readonly DetectionConfig _config;
    private readonly Dictionary<GameState, Mat> _templates = new();
    private readonly Dictionary<GameState, RegionConfig> _regions = new();
    private bool _disposed;

    public string Name => "TemplateMatching";
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Creates a new template matching detector with the specified configuration.
    /// </summary>
    public TemplateMatchingDetector(DetectionConfig? config = null)
    {
        _config = config ?? DetectionConfig.Default();
        InitializeRegions();
    }

    private void InitializeRegions()
    {
        _regions[GameState.Victory] = _config.VictoryRegion;
        _regions[GameState.Defeat] = _config.DefeatRegion;
        _regions[GameState.InGame] = _config.MinimapRegion;
        _regions[GameState.MainMenu] = _config.MenuRegion;
        _regions[GameState.Loading] = _config.LoadingBarRegion;
    }

    /// <inheritdoc />
    public bool Initialize()
    {
        if (IsInitialized) return true;

        try
        {
            LoadTemplates();
            IsInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] Initialization failed: {ex.Message}");
            return false;
        }
    }

    private void LoadTemplates()
    {
        var templateDir = _config.TemplateDirectory;

        // Define template file mappings
        var templateFiles = new Dictionary<GameState, string>
        {
            { GameState.Victory, "victory_template.png" },
            { GameState.Defeat, "defeat_template.png" },
            { GameState.InGame, "ingame_minimap_template.png" },
            { GameState.MainMenu, "mainmenu_template.png" },
            { GameState.Loading, "loading_template.png" }
        };

        foreach (var (state, filename) in templateFiles)
        {
            var templatePath = Path.Combine(templateDir, filename);

            if (File.Exists(templatePath))
            {
                var template = Cv2.ImRead(templatePath, ImreadModes.Color);
                if (!template.Empty())
                {
                    _templates[state] = template;
                    Console.WriteLine($"[{Name}] Loaded template: {filename} ({template.Width}x{template.Height})");
                }
            }
            else
            {
                Console.WriteLine($"[{Name}] Template not found: {templatePath} (will use fallback detection)");
            }
        }
    }

    /// <inheritdoc />
    public bool SupportsState(GameState state)
        => _templates.ContainsKey(state) || state == GameState.Unknown;

    /// <inheritdoc />
    public DetectionResult Detect(ScreenFrame frame)
    {
        if (!IsInitialized)
        {
            return DetectionResult.Failed(Name, "Detector not initialized");
        }

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

            // Try to detect each state in priority order
            // Victory/Defeat are most important (game over conditions)
            var detectionOrder = new[]
            {
                GameState.Victory,
                GameState.Defeat,
                GameState.Loading,
                GameState.MainMenu,
                GameState.InGame
            };

            DetectionResult? bestResult = null;

            foreach (var state in detectionOrder)
            {
                if (!_templates.TryGetValue(state, out var template))
                    continue;

                var result = MatchTemplate(sourceMat, template, state);

                if (result.Confidence >= _config.TemplateMatchThreshold)
                {
                    sw.Stop();
                    return DetectionResult.Success(
                        state,
                        result.Confidence,
                        Name,
                        sw.Elapsed.TotalMilliseconds,
                        $"Template match score: {result.Confidence:F3}");
                }

                // Track best match even if below threshold
                if (bestResult == null || result.Confidence > bestResult.Confidence)
                {
                    bestResult = result;
                }
            }

            sw.Stop();

            // Return best match if above minimum threshold, otherwise unknown
            if (bestResult != null && bestResult.Confidence >= _config.TemplateMatchThreshold * 0.7f)
            {
                return DetectionResult.Success(
                    bestResult.State,
                    bestResult.Confidence,
                    Name,
                    sw.Elapsed.TotalMilliseconds,
                    $"Best match (below threshold): {bestResult.Confidence:F3}");
            }

            return DetectionResult.Success(
                GameState.Unknown,
                0f,
                Name,
                sw.Elapsed.TotalMilliseconds,
                "No template matched above threshold");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return DetectionResult.Failed(Name, $"Detection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs template matching on a region of interest.
    /// </summary>
    private DetectionResult MatchTemplate(Mat source, Mat template, GameState targetState)
    {
        // Get region of interest if defined
        Mat roi = source;
        bool roiCreated = false;

        if (_regions.TryGetValue(targetState, out var region))
        {
            var (x, y, w, h) = region.ToAbsolute(source.Width, source.Height);

            // Ensure ROI is within bounds
            x = Math.Max(0, Math.Min(x, source.Width - 1));
            y = Math.Max(0, Math.Min(y, source.Height - 1));
            w = Math.Min(w, source.Width - x);
            h = Math.Min(h, source.Height - y);

            if (w > template.Width && h > template.Height)
            {
                roi = new Mat(source, new Rect(x, y, w, h));
                roiCreated = true;
            }
        }

        try
        {
            float bestScore = 0f;

            // Multi-scale matching if enabled
            var scales = _config.EnableMultiScaleMatching
                ? _config.ScaleFactors
                : new[] { 1.0f };

            foreach (var scale in scales)
            {
                var score = MatchAtScale(roi, template, scale);
                bestScore = Math.Max(bestScore, score);

                // Early exit if we found a good match
                if (bestScore >= _config.TemplateMatchThreshold)
                    break;
            }

            return DetectionResult.Success(
                targetState,
                bestScore,
                Name,
                diagnosticInfo: $"Template: {targetState}, Score: {bestScore:F3}");
        }
        finally
        {
            if (roiCreated)
            {
                roi.Dispose();
            }
        }
    }

    /// <summary>
    /// Performs template matching at a specific scale.
    /// </summary>
    private float MatchAtScale(Mat source, Mat template, float scale)
    {
        Mat scaledTemplate = template;
        bool templateScaled = false;

        try
        {
            // Scale template if needed
            if (Math.Abs(scale - 1.0f) > 0.01f)
            {
                int newWidth = (int)(template.Width * scale);
                int newHeight = (int)(template.Height * scale);

                if (newWidth > 0 && newHeight > 0 &&
                    newWidth < source.Width && newHeight < source.Height)
                {
                    scaledTemplate = new Mat();
                    Cv2.Resize(template, scaledTemplate, new Size(newWidth, newHeight));
                    templateScaled = true;
                }
                else
                {
                    return 0f; // Invalid scale for this source/template combination
                }
            }

            // Template must be smaller than source
            if (scaledTemplate.Width >= source.Width || scaledTemplate.Height >= source.Height)
            {
                return 0f;
            }

            // Perform template matching using normalized cross-correlation
            // TM_CCOEFF_NORMED produces values in [-1, 1] range
            // 1.0 = perfect match, 0.0 = no correlation, -1.0 = inverse correlation
            using var result = new Mat();
            Cv2.MatchTemplate(source, scaledTemplate, result, TemplateMatchModes.CCoeffNormed);

            // Find the best match location and score
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

            // Convert from [-1, 1] to [0, 1] range for consistency
            return (float)((maxVal + 1.0) / 2.0);
        }
        finally
        {
            if (templateScaled)
            {
                scaledTemplate.Dispose();
            }
        }
    }

    /// <summary>
    /// Converts a ScreenFrame to OpenCV Mat.
    /// </summary>
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
                // Copy data to Mat
                Buffer.MemoryCopy(
                    dataPtr,
                    (void*)mat.DataPointer,
                    frame.Data.Length,
                    frame.Data.Length);
            }
        }

        // Convert to BGR if needed (OpenCV default format)
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

        foreach (var template in _templates.Values)
        {
            template.Dispose();
        }
        _templates.Clear();

        _disposed = true;
    }
}
