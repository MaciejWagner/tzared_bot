using System.Text.RegularExpressions;
using OpenCvSharp;
using Tesseract;
using TzarBot.Common.Models;
using TzarBot.StateDetection.Detection;

namespace TzarBot.StateDetection.Stats;

/// <summary>
/// Extracts game statistics from end-game screens using Tesseract OCR.
///
/// OCR extraction process:
/// 1. Extract the stats region from the frame
/// 2. Preprocess the image (grayscale, threshold, denoise)
/// 3. Run Tesseract OCR on the preprocessed image
/// 4. Parse the text to extract numeric values
///
/// Note: This is a SHOULD priority feature. The accuracy depends heavily
/// on the game's font and screen resolution. Placeholder patterns are used
/// and should be calibrated from actual game screenshots.
/// </summary>
public sealed class OcrStatsExtractor : IStatsExtractor
{
    private readonly DetectionConfig _config;
    private readonly string _tessDataPath;
    private TesseractEngine? _engine;
    private bool _disposed;

    public bool IsInitialized => _engine != null;

    /// <summary>
    /// Creates a new OCR stats extractor.
    /// </summary>
    /// <param name="config">Detection configuration with region definitions.</param>
    /// <param name="tessDataPath">Path to Tesseract data files (default: "./tessdata").</param>
    public OcrStatsExtractor(DetectionConfig? config = null, string? tessDataPath = null)
    {
        _config = config ?? DetectionConfig.Default();
        _tessDataPath = tessDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    }

    /// <inheritdoc />
    public bool Initialize()
    {
        if (IsInitialized) return true;

        try
        {
            // Check if tessdata exists
            if (!Directory.Exists(_tessDataPath))
            {
                Console.WriteLine($"[OcrStatsExtractor] Warning: tessdata directory not found at {_tessDataPath}");
                Console.WriteLine("[OcrStatsExtractor] OCR will not be available. Download tessdata from: https://github.com/tesseract-ocr/tessdata");
                return false;
            }

            // Initialize Tesseract with English language
            _engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);

            // Configure for numeric/alphanumeric text
            _engine.SetVariable("tessedit_char_whitelist", "0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz .-/");

            Console.WriteLine("[OcrStatsExtractor] Tesseract initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OcrStatsExtractor] Failed to initialize Tesseract: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public GameStats ExtractStats(ScreenFrame frame)
    {
        if (!IsInitialized)
        {
            if (!Initialize())
            {
                return GameStats.Empty();
            }
        }

        if (frame == null || frame.Data == null || frame.Data.Length == 0)
        {
            return GameStats.Empty();
        }

        try
        {
            // Convert frame to Mat and extract stats region
            using var sourceMat = ConvertFrameToMat(frame);
            if (sourceMat.Empty())
            {
                return GameStats.Empty();
            }

            // Extract and preprocess the stats region
            using var statsRegion = ExtractStatsRegion(sourceMat, frame.Width, frame.Height);
            using var preprocessed = PreprocessForOcr(statsRegion);

            // Run OCR
            var rawText = RunOcr(preprocessed);

            // Parse the extracted text
            return ParseStats(rawText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OcrStatsExtractor] Extraction error: {ex.Message}");
            return GameStats.Empty();
        }
    }

    private Mat ExtractStatsRegion(Mat source, int width, int height)
    {
        var (x, y, w, h) = _config.StatsRegion.ToAbsolute(width, height);

        // Ensure bounds
        x = Math.Max(0, Math.Min(x, width - 1));
        y = Math.Max(0, Math.Min(y, height - 1));
        w = Math.Min(w, width - x);
        h = Math.Min(h, height - y);

        if (w <= 0 || h <= 0)
        {
            return source.Clone();
        }

        return new Mat(source, new OpenCvSharp.Rect(x, y, w, h));
    }

    private Mat PreprocessForOcr(Mat source)
    {
        // Convert to grayscale
        using var gray = new Mat();
        if (source.Channels() > 1)
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            source.CopyTo(gray);
        }

        // Scale up for better OCR (2x)
        using var scaled = new Mat();
        Cv2.Resize(gray, scaled, new Size(gray.Width * 2, gray.Height * 2), interpolation: InterpolationFlags.Cubic);

        // Apply adaptive threshold for better text contrast
        using var thresholded = new Mat();
        Cv2.AdaptiveThreshold(
            scaled,
            thresholded,
            255,
            AdaptiveThresholdTypes.GaussianC,
            ThresholdTypes.Binary,
            11,
            2);

        // Denoise
        var denoised = new Mat();
        Cv2.FastNlMeansDenoising(thresholded, denoised, 10, 7, 21);

        return denoised;
    }

    private string RunOcr(Mat preprocessed)
    {
        if (_engine == null)
        {
            return string.Empty;
        }

        // Convert Mat to Pix for Tesseract
        var imageData = preprocessed.ToBytes(".png");

        using var pix = Pix.LoadFromMemory(imageData);
        using var page = _engine.Process(pix);

        return page.GetText();
    }

    private GameStats ParseStats(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new GameStats
            {
                ExtractionConfidence = 0f,
                RawText = rawText,
                FailedFields = new List<string> { "All fields - empty OCR result" }
            };
        }

        var failedFields = new List<string>();
        int successCount = 0;
        int totalFields = 10;

        // Parse duration (format: "MM:SS" or "HH:MM:SS")
        var durationSeconds = 0;
        string? durationFormatted = null;
        var durationMatch = Regex.Match(rawText, @"(?:Duration|Time|Czas)[:\s]*(\d{1,2}:\d{2}(?::\d{2})?)", RegexOptions.IgnoreCase);
        if (durationMatch.Success)
        {
            durationFormatted = durationMatch.Groups[1].Value;
            durationSeconds = ParseDuration(durationFormatted);
            successCount++;
        }
        else
        {
            failedFields.Add("Duration");
        }

        // Parse units built
        var unitsBuilt = ExtractNumber(rawText, @"(?:Units?\s*Built|Jednostki\s*Zbudowane)[:\s]*(\d+)", failedFields, "UnitsBuilt", ref successCount);

        // Parse units lost
        var unitsLost = ExtractNumber(rawText, @"(?:Units?\s*Lost|Jednostki\s*Utracone)[:\s]*(\d+)", failedFields, "UnitsLost", ref successCount);

        // Parse enemy units killed
        var unitsKilled = ExtractNumber(rawText, @"(?:Enemy\s*Units?\s*Killed|Wrogowie\s*Zabici)[:\s]*(\d+)", failedFields, "EnemyUnitsKilled", ref successCount);

        // Parse buildings built
        var buildingsBuilt = ExtractNumber(rawText, @"(?:Buildings?\s*Built|Budynki\s*Zbudowane)[:\s]*(\d+)", failedFields, "BuildingsBuilt", ref successCount);

        // Parse buildings destroyed (own)
        var buildingsDestroyed = ExtractNumber(rawText, @"(?:Buildings?\s*(?:Lost|Destroyed)|Budynki\s*Utracone)[:\s]*(\d+)", failedFields, "BuildingsDestroyed", ref successCount);

        // Parse enemy buildings destroyed
        var enemyBuildingsDestroyed = ExtractNumber(rawText, @"(?:Enemy\s*Buildings?\s*Destroyed|Budynki\s*Wroga\s*Zniszczone)[:\s]*(\d+)", failedFields, "EnemyBuildingsDestroyed", ref successCount);

        // Parse resources (gold, wood, stone, food)
        var goldGathered = ExtractNumber(rawText, @"(?:Gold|Zloto)[:\s]*(\d+)", failedFields, "Gold", ref successCount);
        var woodGathered = ExtractNumber(rawText, @"(?:Wood|Drewno)[:\s]*(\d+)", failedFields, "Wood", ref successCount);
        var stoneGathered = ExtractNumber(rawText, @"(?:Stone|Kamien)[:\s]*(\d+)", failedFields, "Stone", ref successCount);
        var foodProduced = ExtractNumber(rawText, @"(?:Food|Zywnosc)[:\s]*(\d+)", failedFields, "Food", ref successCount);

        // Calculate totals
        var resourcesGathered = goldGathered + woodGathered + stoneGathered + foodProduced;

        // Parse score if available
        int? score = null;
        var scoreMatch = Regex.Match(rawText, @"(?:Score|Wynik|Points)[:\s]*(\d+)", RegexOptions.IgnoreCase);
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var parsedScore))
        {
            score = parsedScore;
        }

        // Calculate confidence
        float confidence = (float)successCount / totalFields;

        return new GameStats
        {
            DurationSeconds = durationSeconds,
            DurationFormatted = durationFormatted,
            UnitsBuilt = unitsBuilt,
            UnitsLost = unitsLost,
            EnemyUnitsKilled = unitsKilled,
            BuildingsBuilt = buildingsBuilt,
            BuildingsDestroyed = buildingsDestroyed,
            EnemyBuildingsDestroyed = enemyBuildingsDestroyed,
            ResourcesGathered = resourcesGathered,
            GoldGathered = goldGathered,
            WoodGathered = woodGathered,
            StoneGathered = stoneGathered,
            FoodProduced = foodProduced,
            Score = score,
            ExtractionConfidence = confidence,
            RawText = rawText,
            FailedFields = failedFields
        };
    }

    private static int ExtractNumber(
        string text,
        string pattern,
        List<string> failedFields,
        string fieldName,
        ref int successCount)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
        {
            successCount++;
            return value;
        }

        failedFields.Add(fieldName);
        return 0;
    }

    private static int ParseDuration(string durationStr)
    {
        var parts = durationStr.Split(':');
        int seconds = 0;

        try
        {
            if (parts.Length == 2)
            {
                // MM:SS
                seconds = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
            }
            else if (parts.Length == 3)
            {
                // HH:MM:SS
                seconds = int.Parse(parts[0]) * 3600 + int.Parse(parts[1]) * 60 + int.Parse(parts[2]);
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return seconds;
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

        return mat;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _engine?.Dispose();
        _engine = null;

        _disposed = true;
    }
}
