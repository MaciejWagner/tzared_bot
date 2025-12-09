using System.Diagnostics;
using TzarBot.Common.Models;

namespace TzarBot.StateDetection.Detection;

/// <summary>
/// Combines multiple detection methods for robust game state detection.
///
/// Detection strategy:
/// 1. Run all detectors in parallel (or sequentially based on config)
/// 2. Combine results using weighted voting
/// 3. Return the state with highest combined confidence
///
/// The composite detector provides higher accuracy by leveraging multiple
/// detection methods, each with different strengths:
/// - Template matching: Precise for known screen layouts
/// - Color histogram: Robust to UI variations, works when templates miss
/// </summary>
public sealed class CompositeGameStateDetector : IGameStateDetector, IInitializableDetector, IDisposable
{
    private readonly List<IGameStateDetector> _detectors = new();
    private readonly DetectionConfig _config;
    private readonly Dictionary<string, float> _detectorWeights = new();
    private bool _disposed;

    public string Name => "CompositeDetector";
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Creates a composite detector with default configuration and detectors.
    /// </summary>
    public CompositeGameStateDetector() : this(DetectionConfig.Default())
    {
    }

    /// <summary>
    /// Creates a composite detector with the specified configuration.
    /// </summary>
    public CompositeGameStateDetector(DetectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Adds a detector to the composite.
    /// </summary>
    /// <param name="detector">The detector to add.</param>
    /// <param name="weight">Weight for this detector (default 1.0).</param>
    public void AddDetector(IGameStateDetector detector, float weight = 1.0f)
    {
        if (detector == null) throw new ArgumentNullException(nameof(detector));

        _detectors.Add(detector);
        _detectorWeights[detector.Name] = weight;
    }

    /// <inheritdoc />
    public bool Initialize()
    {
        if (IsInitialized) return true;

        // Add default detectors if none were added
        if (_detectors.Count == 0)
        {
            AddDetector(new TemplateMatchingDetector(_config), 1.5f); // Higher weight for template matching
            AddDetector(new ColorHistogramDetector(_config), 1.0f);
        }

        // Initialize all initializable detectors
        bool allInitialized = true;
        foreach (var detector in _detectors)
        {
            if (detector is IInitializableDetector initializable)
            {
                if (!initializable.Initialize())
                {
                    Console.WriteLine($"[{Name}] Warning: Detector '{detector.Name}' failed to initialize");
                    allInitialized = false;
                }
            }
        }

        IsInitialized = true;
        return allInitialized;
    }

    /// <inheritdoc />
    public bool SupportsState(GameState state)
        => _detectors.Any(d => d.SupportsState(state));

    /// <inheritdoc />
    public DetectionResult Detect(ScreenFrame frame)
    {
        if (!IsInitialized)
        {
            Initialize();
        }

        if (frame == null || frame.Data == null || frame.Data.Length == 0)
        {
            return DetectionResult.Failed(Name, "Invalid frame");
        }

        var sw = Stopwatch.StartNew();

        try
        {
            // Run all detectors and collect results
            var results = new List<(DetectionResult Result, float Weight)>();

            foreach (var detector in _detectors)
            {
                try
                {
                    var result = detector.Detect(frame);
                    var weight = _detectorWeights.GetValueOrDefault(detector.Name, 1.0f);
                    results.Add((result, weight));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{Name}] Detector '{detector.Name}' threw exception: {ex.Message}");
                }
            }

            if (results.Count == 0)
            {
                return DetectionResult.Failed(Name, "No detectors produced results");
            }

            // Combine results using weighted voting
            var combinedResult = CombineResults(results);

            sw.Stop();
            return DetectionResult.Success(
                combinedResult.State,
                combinedResult.Confidence,
                Name,
                sw.Elapsed.TotalMilliseconds,
                BuildDiagnosticInfo(results));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return DetectionResult.Failed(Name, $"Detection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Combines multiple detection results using weighted voting.
    /// </summary>
    private (GameState State, float Confidence) CombineResults(
        List<(DetectionResult Result, float Weight)> results)
    {
        // Group by detected state and sum weighted confidences
        var stateScores = new Dictionary<GameState, float>();
        var stateWeights = new Dictionary<GameState, float>();

        foreach (var (result, weight) in results)
        {
            if (result.State == GameState.Unknown && result.Confidence < 0.3f)
            {
                // Skip low-confidence unknown results
                continue;
            }

            var state = result.State;
            var weightedConfidence = result.Confidence * weight;

            if (!stateScores.ContainsKey(state))
            {
                stateScores[state] = 0f;
                stateWeights[state] = 0f;
            }

            stateScores[state] += weightedConfidence;
            stateWeights[state] += weight;
        }

        if (stateScores.Count == 0)
        {
            return (GameState.Unknown, 0f);
        }

        // Find the state with highest weighted score
        var bestState = GameState.Unknown;
        float bestScore = 0f;
        float bestNormalizedConfidence = 0f;

        foreach (var (state, score) in stateScores)
        {
            if (score > bestScore)
            {
                bestScore = score;
                bestState = state;
                // Normalize confidence by total weight for this state
                bestNormalizedConfidence = stateWeights[state] > 0
                    ? score / stateWeights[state]
                    : 0f;
            }
        }

        // Adjust confidence based on agreement between detectors
        // If multiple detectors agree, boost confidence
        int agreementCount = results.Count(r => r.Result.State == bestState && r.Result.Confidence > 0.5f);
        float agreementBonus = agreementCount > 1 ? 0.1f * (agreementCount - 1) : 0f;

        float finalConfidence = Math.Min(1f, bestNormalizedConfidence + agreementBonus);

        return (bestState, finalConfidence);
    }

    private static string BuildDiagnosticInfo(List<(DetectionResult Result, float Weight)> results)
    {
        var parts = results.Select(r =>
            $"{r.Result.DetectorName}:{r.Result.State}({r.Result.Confidence:F2})");
        return string.Join(", ", parts);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var detector in _detectors)
        {
            if (detector is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _detectors.Clear();

        _disposed = true;
    }
}
