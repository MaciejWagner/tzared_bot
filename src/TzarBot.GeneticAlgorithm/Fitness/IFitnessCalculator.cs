namespace TzarBot.GeneticAlgorithm.Fitness;

/// <summary>
/// Interface for calculating fitness from game results.
/// </summary>
public interface IFitnessCalculator
{
    /// <summary>
    /// Calculates fitness score from game result.
    /// </summary>
    /// <param name="result">Game result data.</param>
    /// <returns>Fitness score (higher is better).</returns>
    float Calculate(GameResult result);

    /// <summary>
    /// Calculates average fitness from multiple game results.
    /// </summary>
    /// <param name="results">Multiple game results.</param>
    /// <returns>Average fitness score.</returns>
    float CalculateAverage(IEnumerable<GameResult> results);
}

/// <summary>
/// Represents the result of a single game.
/// Used for fitness calculation.
/// </summary>
public sealed class GameResult
{
    /// <summary>
    /// Whether the bot won the game.
    /// </summary>
    public bool Won { get; init; }

    /// <summary>
    /// Game duration in seconds.
    /// </summary>
    public float DurationSeconds { get; init; }

    /// <summary>
    /// Maximum game duration in seconds (for normalization).
    /// </summary>
    public float MaxDurationSeconds { get; init; } = 3600f; // 1 hour

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
    public int UnitsKilled { get; init; }

    /// <summary>
    /// Number of buildings constructed.
    /// </summary>
    public int BuildingsBuilt { get; init; }

    /// <summary>
    /// Number of buildings lost.
    /// </summary>
    public int BuildingsLost { get; init; }

    /// <summary>
    /// Number of enemy buildings destroyed.
    /// </summary>
    public int BuildingsDestroyed { get; init; }

    /// <summary>
    /// Total resources gathered during the game.
    /// </summary>
    public int ResourcesGathered { get; init; }

    /// <summary>
    /// Total resources spent during the game.
    /// </summary>
    public int ResourcesSpent { get; init; }

    /// <summary>
    /// Number of valid actions performed.
    /// </summary>
    public int ValidActions { get; init; }

    /// <summary>
    /// Number of invalid/rejected actions.
    /// </summary>
    public int InvalidActions { get; init; }

    /// <summary>
    /// Number of frames with no action (potential inactivity).
    /// </summary>
    public int IdleFrames { get; init; }

    /// <summary>
    /// Total frames in the game.
    /// </summary>
    public int TotalFrames { get; init; }

    /// <summary>
    /// Exploration score (map coverage, scouting).
    /// </summary>
    public float ExplorationScore { get; init; }

    /// <summary>
    /// Damage dealt to enemy.
    /// </summary>
    public float DamageDealt { get; init; }

    /// <summary>
    /// Damage received from enemy.
    /// </summary>
    public float DamageReceived { get; init; }

    /// <summary>
    /// Final score from the game (if available).
    /// </summary>
    public int? GameScore { get; init; }

    /// <summary>
    /// Custom metadata for extensibility.
    /// </summary>
    public Dictionary<string, float>? CustomMetrics { get; init; }

    /// <summary>
    /// Calculates inactivity ratio (idle frames / total frames).
    /// </summary>
    public float InactivityRatio => TotalFrames > 0 ? (float)IdleFrames / TotalFrames : 1f;

    /// <summary>
    /// Calculates invalid action ratio.
    /// </summary>
    public float InvalidActionRatio => (ValidActions + InvalidActions) > 0
        ? (float)InvalidActions / (ValidActions + InvalidActions)
        : 0f;

    /// <summary>
    /// Calculates kill/death ratio for units.
    /// </summary>
    public float UnitKDRatio => UnitsLost > 0 ? (float)UnitsKilled / UnitsLost : UnitsKilled;

    /// <summary>
    /// Time efficiency (shorter wins are better).
    /// </summary>
    public float TimeEfficiency => MaxDurationSeconds > 0
        ? 1f - (DurationSeconds / MaxDurationSeconds)
        : 0f;
}
