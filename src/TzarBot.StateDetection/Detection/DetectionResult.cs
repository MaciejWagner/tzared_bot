namespace TzarBot.StateDetection.Detection;

/// <summary>
/// Represents the result of a game state detection operation.
/// </summary>
public sealed class DetectionResult
{
    /// <summary>
    /// The detected game state.
    /// </summary>
    public required GameState State { get; init; }

    /// <summary>
    /// Confidence level of the detection (0.0 to 1.0).
    /// Higher values indicate more certain detection.
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Name of the detection method that produced this result.
    /// </summary>
    public required string DetectorName { get; init; }

    /// <summary>
    /// Timestamp when the detection was performed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional diagnostic information (optional).
    /// </summary>
    public string? DiagnosticInfo { get; init; }

    /// <summary>
    /// Time taken to perform the detection in milliseconds.
    /// </summary>
    public double DetectionTimeMs { get; init; }

    /// <summary>
    /// Returns true if the detection is considered reliable (confidence above threshold).
    /// </summary>
    /// <param name="threshold">Minimum confidence threshold (default 0.8).</param>
    public bool IsReliable(float threshold = 0.8f) => Confidence >= threshold;

    /// <summary>
    /// Creates a result indicating detection failure.
    /// </summary>
    public static DetectionResult Failed(string detectorName, string? diagnosticInfo = null)
        => new()
        {
            State = GameState.Unknown,
            Confidence = 0f,
            DetectorName = detectorName,
            DiagnosticInfo = diagnosticInfo ?? "Detection failed"
        };

    /// <summary>
    /// Creates a result for a successful detection.
    /// </summary>
    public static DetectionResult Success(
        GameState state,
        float confidence,
        string detectorName,
        double detectionTimeMs = 0,
        string? diagnosticInfo = null)
        => new()
        {
            State = state,
            Confidence = confidence,
            DetectorName = detectorName,
            DetectionTimeMs = detectionTimeMs,
            DiagnosticInfo = diagnosticInfo
        };

    public override string ToString()
        => $"[{DetectorName}] {State} (confidence: {Confidence:P1}, time: {DetectionTimeMs:F1}ms)";
}
