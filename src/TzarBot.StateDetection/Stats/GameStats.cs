namespace TzarBot.StateDetection.Stats;

/// <summary>
/// Represents game statistics extracted from the end-game screen using OCR.
/// </summary>
public sealed class GameStats
{
    /// <summary>
    /// Game duration in seconds.
    /// </summary>
    public int DurationSeconds { get; init; }

    /// <summary>
    /// Formatted game duration (e.g., "15:30").
    /// </summary>
    public string? DurationFormatted { get; init; }

    /// <summary>
    /// Number of units built during the game.
    /// </summary>
    public int UnitsBuilt { get; init; }

    /// <summary>
    /// Number of units lost during the game.
    /// </summary>
    public int UnitsLost { get; init; }

    /// <summary>
    /// Number of enemy units killed.
    /// </summary>
    public int EnemyUnitsKilled { get; init; }

    /// <summary>
    /// Number of buildings constructed.
    /// </summary>
    public int BuildingsBuilt { get; init; }

    /// <summary>
    /// Number of buildings destroyed (own buildings).
    /// </summary>
    public int BuildingsDestroyed { get; init; }

    /// <summary>
    /// Number of enemy buildings destroyed.
    /// </summary>
    public int EnemyBuildingsDestroyed { get; init; }

    /// <summary>
    /// Total resources gathered (gold + wood + stone + food).
    /// </summary>
    public int ResourcesGathered { get; init; }

    /// <summary>
    /// Resources spent during the game.
    /// </summary>
    public int ResourcesSpent { get; init; }

    /// <summary>
    /// Gold gathered.
    /// </summary>
    public int GoldGathered { get; init; }

    /// <summary>
    /// Wood gathered.
    /// </summary>
    public int WoodGathered { get; init; }

    /// <summary>
    /// Stone gathered.
    /// </summary>
    public int StoneGathered { get; init; }

    /// <summary>
    /// Food gathered/produced.
    /// </summary>
    public int FoodProduced { get; init; }

    /// <summary>
    /// Final score from the game (if displayed).
    /// </summary>
    public int? Score { get; init; }

    /// <summary>
    /// Confidence of the OCR extraction (0.0-1.0).
    /// </summary>
    public float ExtractionConfidence { get; init; }

    /// <summary>
    /// Raw text extracted by OCR (for debugging).
    /// </summary>
    public string? RawText { get; init; }

    /// <summary>
    /// Fields that failed to extract (for diagnostics).
    /// </summary>
    public List<string> FailedFields { get; init; } = new();

    /// <summary>
    /// Returns true if the extraction was at least partially successful.
    /// </summary>
    public bool IsValid => ExtractionConfidence > 0.3f;

    /// <summary>
    /// Creates an empty stats object (extraction failed).
    /// </summary>
    public static GameStats Empty() => new()
    {
        ExtractionConfidence = 0f,
        FailedFields = new List<string> { "All fields" }
    };
}
