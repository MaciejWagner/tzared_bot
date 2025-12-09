using MessagePack;
using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Curriculum;

namespace TzarBot.Training.Core;

/// <summary>
/// Represents the current state of the training pipeline.
/// Tracks all state needed for checkpoint/restore functionality.
/// </summary>
[MessagePackObject]
public sealed class TrainingState
{
    #region Current Progress

    /// <summary>
    /// Current generation number (0-indexed).
    /// </summary>
    [Key(0)]
    public int CurrentGeneration { get; set; }

    /// <summary>
    /// Current curriculum stage name.
    /// </summary>
    [Key(1)]
    public string CurrentStageName { get; set; } = "Bootstrap";

    /// <summary>
    /// Current training status.
    /// </summary>
    [Key(2)]
    public TrainingStatus Status { get; set; } = TrainingStatus.NotStarted;

    #endregion

    #region Population

    /// <summary>
    /// Current population of genomes.
    /// </summary>
    [Key(3)]
    public List<NetworkGenome> Population { get; set; } = new();

    /// <summary>
    /// Best genome found so far (highest fitness).
    /// </summary>
    [Key(4)]
    public NetworkGenome? BestGenome { get; set; }

    /// <summary>
    /// All-time best fitness score achieved.
    /// </summary>
    [Key(5)]
    public float BestFitness { get; set; }

    #endregion

    #region History

    /// <summary>
    /// History of generation statistics.
    /// </summary>
    [Key(6)]
    public List<GenerationStats> GenerationHistory { get; set; } = new();

    /// <summary>
    /// History of stage transitions.
    /// </summary>
    [Key(7)]
    public List<StageTransition> StageHistory { get; set; } = new();

    #endregion

    #region ELO Ratings (for Tournament mode)

    /// <summary>
    /// ELO ratings for genomes (GenomeId -> Rating).
    /// </summary>
    [Key(8)]
    public Dictionary<Guid, int> EloRatings { get; set; } = new();

    #endregion

    #region Timing

    /// <summary>
    /// When training started.
    /// </summary>
    [Key(9)]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Total elapsed training time (excluding pauses).
    /// </summary>
    [Key(10)]
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// When the state was last updated.
    /// </summary>
    [Key(11)]
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;

    #endregion

    #region Metrics

    /// <summary>
    /// Total number of games played.
    /// </summary>
    [Key(12)]
    public int TotalGamesPlayed { get; set; }

    /// <summary>
    /// Total number of wins.
    /// </summary>
    [Key(13)]
    public int TotalWins { get; set; }

    /// <summary>
    /// Generations since last improvement (for early stopping).
    /// </summary>
    [Key(14)]
    public int GenerationsSinceImprovement { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Win rate across all games played.
    /// </summary>
    [IgnoreMember]
    public float WinRate => TotalGamesPlayed > 0 ? (float)TotalWins / TotalGamesPlayed : 0f;

    /// <summary>
    /// Average fitness of the current population.
    /// </summary>
    [IgnoreMember]
    public float AverageFitness => Population.Count > 0
        ? Population.Average(g => g.Fitness)
        : 0f;

    /// <summary>
    /// Average games per second (throughput metric).
    /// </summary>
    [IgnoreMember]
    public float GamesPerSecond => ElapsedTime.TotalSeconds > 0
        ? TotalGamesPlayed / (float)ElapsedTime.TotalSeconds
        : 0f;

    /// <summary>
    /// Whether training has converged (no improvement for many generations).
    /// </summary>
    [IgnoreMember]
    public bool HasConverged => GenerationsSinceImprovement > 50;

    #endregion

    #region Methods

    /// <summary>
    /// Updates the best genome if the candidate is better.
    /// </summary>
    /// <param name="candidate">Candidate genome to check.</param>
    /// <returns>True if a new best was found.</returns>
    public bool UpdateBest(NetworkGenome candidate)
    {
        if (BestGenome == null || candidate.Fitness > BestFitness)
        {
            BestGenome = candidate.Clone();
            BestFitness = candidate.Fitness;
            GenerationsSinceImprovement = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Records a generation's statistics.
    /// </summary>
    public void RecordGeneration(GenerationStats stats)
    {
        GenerationHistory.Add(stats);
        CurrentGeneration = stats.Generation;
        LastUpdateTime = DateTime.UtcNow;

        // Check if best improved
        if (stats.BestFitness > BestFitness)
        {
            GenerationsSinceImprovement = 0;
        }
        else
        {
            GenerationsSinceImprovement++;
        }
    }

    /// <summary>
    /// Records a stage transition.
    /// </summary>
    public void RecordStageTransition(string fromStage, string toStage, string reason)
    {
        StageHistory.Add(new StageTransition
        {
            FromStage = fromStage,
            ToStage = toStage,
            Generation = CurrentGeneration,
            Timestamp = DateTime.UtcNow,
            Reason = reason
        });
        CurrentStageName = toStage;
    }

    /// <summary>
    /// Creates a summary of the current state.
    /// </summary>
    public TrainingSummary GetSummary()
    {
        return new TrainingSummary
        {
            Generation = CurrentGeneration,
            Stage = CurrentStageName,
            Status = Status,
            BestFitness = BestFitness,
            AverageFitness = AverageFitness,
            WinRate = WinRate,
            TotalGamesPlayed = TotalGamesPlayed,
            ElapsedTime = ElapsedTime,
            PopulationSize = Population.Count,
            GenerationsSinceImprovement = GenerationsSinceImprovement
        };
    }

    #endregion

    public override string ToString()
    {
        return $"TrainingState[Gen={CurrentGeneration}, Stage={CurrentStageName}, " +
               $"Status={Status}, BestFit={BestFitness:F2}, Pop={Population.Count}]";
    }
}

/// <summary>
/// Training pipeline status.
/// </summary>
public enum TrainingStatus
{
    /// <summary>
    /// Training has not started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Initializing population and resources.
    /// </summary>
    Initializing,

    /// <summary>
    /// Training is actively running.
    /// </summary>
    Running,

    /// <summary>
    /// Training is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Training completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Training stopped due to error.
    /// </summary>
    Error,

    /// <summary>
    /// Training was cancelled by user.
    /// </summary>
    Cancelled
}

/// <summary>
/// Records a stage transition during training.
/// </summary>
[MessagePackObject]
public sealed class StageTransition
{
    [Key(0)]
    public required string FromStage { get; init; }

    [Key(1)]
    public required string ToStage { get; init; }

    [Key(2)]
    public int Generation { get; init; }

    [Key(3)]
    public DateTime Timestamp { get; init; }

    [Key(4)]
    public string? Reason { get; init; }
}

/// <summary>
/// Summary of training state for display/reporting.
/// </summary>
public sealed class TrainingSummary
{
    public int Generation { get; init; }
    public string Stage { get; init; } = string.Empty;
    public TrainingStatus Status { get; init; }
    public float BestFitness { get; init; }
    public float AverageFitness { get; init; }
    public float WinRate { get; init; }
    public int TotalGamesPlayed { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public int PopulationSize { get; init; }
    public int GenerationsSinceImprovement { get; init; }
}

/// <summary>
/// Extended generation statistics for training pipeline.
/// Extends the base GenerationStats with additional training metrics.
/// </summary>
[MessagePackObject]
public sealed class GenerationStats
{
    [Key(0)]
    public int Generation { get; init; }

    [Key(1)]
    public float BestFitness { get; init; }

    [Key(2)]
    public float AverageFitness { get; init; }

    [Key(3)]
    public float WorstFitness { get; init; }

    [Key(4)]
    public float FitnessStdDev { get; init; }

    [Key(5)]
    public Guid BestGenomeId { get; init; }

    [Key(6)]
    public TimeSpan EvaluationDuration { get; init; }

    [Key(7)]
    public TimeSpan EvolutionDuration { get; init; }

    [Key(8)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Key(9)]
    public string StageName { get; set; } = string.Empty;

    [Key(10)]
    public int GamesPlayed { get; init; }

    [Key(11)]
    public int Wins { get; init; }

    [Key(12)]
    public int EliteCount { get; init; }

    [Key(13)]
    public int MutationCount { get; init; }

    [Key(14)]
    public int CrossoverCount { get; init; }

    /// <summary>
    /// Win rate for this generation.
    /// </summary>
    [IgnoreMember]
    public float WinRate => GamesPlayed > 0 ? (float)Wins / GamesPlayed : 0f;

    /// <summary>
    /// Total duration of this generation.
    /// </summary>
    [IgnoreMember]
    public TimeSpan TotalDuration => EvaluationDuration + EvolutionDuration;

    /// <summary>
    /// Improvement from previous generation (if known).
    /// </summary>
    [Key(15)]
    public float Improvement { get; init; }

    public override string ToString()
    {
        return $"Gen {Generation}: Best={BestFitness:F2}, Avg={AverageFitness:F2}, " +
               $"WinRate={WinRate:P0}, Games={GamesPlayed}, " +
               $"Time={TotalDuration.TotalSeconds:F1}s";
    }

    /// <summary>
    /// Creates GenerationStats from GA engine stats.
    /// </summary>
    public static GenerationStats FromGAStats(
        TzarBot.GeneticAlgorithm.Engine.GenerationStats gaStats,
        string stageName,
        int gamesPlayed,
        int wins)
    {
        return new GenerationStats
        {
            Generation = gaStats.Generation,
            BestFitness = gaStats.BestFitness,
            AverageFitness = gaStats.AverageFitness,
            WorstFitness = gaStats.WorstFitness,
            FitnessStdDev = gaStats.FitnessStdDev,
            BestGenomeId = gaStats.BestGenomeId,
            EvaluationDuration = gaStats.EvaluationTime,
            EvolutionDuration = gaStats.EvolutionTime,
            Timestamp = gaStats.Timestamp,
            StageName = stageName,
            GamesPlayed = gamesPlayed,
            Wins = wins,
            EliteCount = gaStats.EliteCount,
            MutationCount = gaStats.MutationCount,
            CrossoverCount = gaStats.CrossoverCount,
            Improvement = gaStats.Improvement
        };
    }
}
