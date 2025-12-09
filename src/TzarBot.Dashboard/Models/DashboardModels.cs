namespace TzarBot.Dashboard.Models;

/// <summary>
/// Generation statistics for dashboard display.
/// </summary>
public sealed class DashboardGenerationStats
{
    /// <summary>
    /// Generation number.
    /// </summary>
    public int Generation { get; init; }

    /// <summary>
    /// Curriculum stage name.
    /// </summary>
    public string Stage { get; init; } = "Stage 1";

    /// <summary>
    /// Best fitness in this generation.
    /// </summary>
    public float BestFitness { get; init; }

    /// <summary>
    /// Average fitness of the population.
    /// </summary>
    public float AverageFitness { get; init; }

    /// <summary>
    /// Worst fitness in this generation.
    /// </summary>
    public float WorstFitness { get; init; }

    /// <summary>
    /// Standard deviation of fitness values.
    /// </summary>
    public float FitnessStdDev { get; init; }

    /// <summary>
    /// Number of elite genomes preserved.
    /// </summary>
    public int EliteCount { get; init; }

    /// <summary>
    /// Win rate for this generation (0.0 to 1.0).
    /// </summary>
    public float WinRate { get; init; }

    /// <summary>
    /// Number of games played in this generation.
    /// </summary>
    public int GamesPlayed { get; init; }

    /// <summary>
    /// Duration of this generation evaluation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Timestamp when this generation completed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the best genome.
    /// </summary>
    public Guid BestGenomeId { get; init; }

    /// <summary>
    /// Improvement from previous generation.
    /// </summary>
    public float Improvement { get; init; }
}

/// <summary>
/// Summary of a genome for dashboard display.
/// </summary>
public sealed record GenomeSummary
{
    /// <summary>
    /// Genome unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Generation when this genome was created.
    /// </summary>
    public int Generation { get; init; }

    /// <summary>
    /// Fitness score.
    /// </summary>
    public float Fitness { get; init; }

    /// <summary>
    /// ELO rating (if applicable).
    /// </summary>
    public int EloRating { get; init; }

    /// <summary>
    /// Number of games played.
    /// </summary>
    public int GamesPlayed { get; init; }

    /// <summary>
    /// Number of wins.
    /// </summary>
    public int Wins { get; init; }

    /// <summary>
    /// Win rate (0.0 to 1.0).
    /// </summary>
    public float WinRate => GamesPlayed > 0 ? (float)Wins / GamesPlayed : 0f;

    /// <summary>
    /// Number of hidden layers.
    /// </summary>
    public int HiddenLayerCount { get; init; }

    /// <summary>
    /// Total number of parameters/weights.
    /// </summary>
    public int ParameterCount { get; init; }

    /// <summary>
    /// Short display ID (first 8 characters of GUID).
    /// </summary>
    public string ShortId => Id.ToString("N")[..8];
}

/// <summary>
/// Activity log entry for dashboard feed.
/// </summary>
public sealed class ActivityLogEntry
{
    /// <summary>
    /// Entry timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Log level (Info, Warning, Error, Success).
    /// </summary>
    public ActivityLevel Level { get; init; }

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Associated generation (if any).
    /// </summary>
    public int? Generation { get; init; }

    /// <summary>
    /// Associated genome ID (if any).
    /// </summary>
    public Guid? GenomeId { get; init; }
}

/// <summary>
/// Activity log level.
/// </summary>
public enum ActivityLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// VM worker status for dashboard display.
/// </summary>
public sealed class VMWorkerStatus
{
    /// <summary>
    /// VM name/identifier.
    /// </summary>
    public string VMName { get; init; } = string.Empty;

    /// <summary>
    /// Current state.
    /// </summary>
    public VMWorkerState State { get; init; }

    /// <summary>
    /// Currently evaluating genome ID (if any).
    /// </summary>
    public Guid? CurrentGenomeId { get; init; }

    /// <summary>
    /// Number of completed evaluations.
    /// </summary>
    public int CompletedEvaluations { get; init; }

    /// <summary>
    /// Number of failed evaluations.
    /// </summary>
    public int FailedEvaluations { get; init; }

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    public float CpuUsage { get; init; }

    /// <summary>
    /// Memory usage percentage.
    /// </summary>
    public float MemoryUsage { get; init; }
}

/// <summary>
/// VM worker state.
/// </summary>
public enum VMWorkerState
{
    Idle,
    Starting,
    Evaluating,
    Stopping,
    Error,
    Offline
}

/// <summary>
/// Chart data point for fitness over time.
/// </summary>
public sealed class FitnessDataPoint
{
    public int Generation { get; init; }
    public float Best { get; init; }
    public float Average { get; init; }
    public float Worst { get; init; }
    public string? StageMarker { get; init; }
}
