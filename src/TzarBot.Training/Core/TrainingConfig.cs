namespace TzarBot.Training.Core;

/// <summary>
/// Configuration for the training pipeline.
/// Contains all tunable parameters for training evolution.
/// </summary>
public sealed class TrainingConfig
{
    /// <summary>
    /// Configuration section name for IConfiguration binding.
    /// </summary>
    public const string SectionName = "Training";

    #region Population Settings

    /// <summary>
    /// Size of the population (number of genomes).
    /// Must be at least 10 for meaningful evolution.
    /// </summary>
    public int PopulationSize { get; set; } = 100;

    /// <summary>
    /// Number of games to play per genome per generation for fitness evaluation.
    /// More games = more accurate fitness but slower training.
    /// </summary>
    public int GamesPerGenome { get; set; } = 3;

    #endregion

    #region Parallelization Settings

    /// <summary>
    /// Maximum number of VMs to use for parallel evaluation.
    /// Limited by 10GB RAM constraint for VM pool.
    /// </summary>
    public int MaxParallelVMs { get; set; } = 3;

    /// <summary>
    /// Maximum parallel evaluations per VM.
    /// Typically 1 for game-based evaluation.
    /// </summary>
    public int MaxParallelEvaluationsPerVM { get; set; } = 1;

    #endregion

    #region Training Duration Settings

    /// <summary>
    /// Maximum number of generations to train.
    /// Set to -1 for unlimited (run until stopped).
    /// </summary>
    public int MaxGenerations { get; set; } = 1000;

    /// <summary>
    /// Maximum total training time.
    /// Training stops when this limit is reached.
    /// </summary>
    public TimeSpan MaxTrainingTime { get; set; } = TimeSpan.FromHours(48);

    /// <summary>
    /// Timeout for a single game evaluation.
    /// Games exceeding this are terminated and marked as timeout.
    /// </summary>
    public TimeSpan GameTimeout { get; set; } = TimeSpan.FromMinutes(30);

    #endregion

    #region Checkpoint Settings

    /// <summary>
    /// Interval (in generations) for saving checkpoints.
    /// 0 = disable periodic checkpoints (still saves on best genome).
    /// </summary>
    public int CheckpointInterval { get; set; } = 10;

    /// <summary>
    /// Number of checkpoints to keep.
    /// Older checkpoints are automatically deleted.
    /// </summary>
    public int MaxCheckpoints { get; set; } = 10;

    /// <summary>
    /// Directory for checkpoint files.
    /// </summary>
    public string CheckpointDirectory { get; set; } = @"C:\TzarBot\Checkpoints";

    /// <summary>
    /// Whether to automatically save best genome separately.
    /// </summary>
    public bool SaveBestGenome { get; set; } = true;

    /// <summary>
    /// Directory for best genome exports.
    /// </summary>
    public string BestGenomeDirectory { get; set; } = @"C:\TzarBot\BestGenomes";

    #endregion

    #region Curriculum Settings

    /// <summary>
    /// Whether to use curriculum learning (staged difficulty).
    /// </summary>
    public bool UseCurriculum { get; set; } = true;

    /// <summary>
    /// Initial curriculum stage name.
    /// </summary>
    public string InitialStage { get; set; } = "Bootstrap";

    /// <summary>
    /// Whether to allow demotion to previous stage on poor performance.
    /// </summary>
    public bool AllowDemotion { get; set; } = true;

    #endregion

    #region Tournament Settings

    /// <summary>
    /// Whether to use tournament mode for final stages.
    /// </summary>
    public bool UseTournament { get; set; } = true;

    /// <summary>
    /// Number of rounds in tournament (Swiss-system).
    /// </summary>
    public int TournamentRounds { get; set; } = 5;

    /// <summary>
    /// Initial ELO rating for new genomes.
    /// </summary>
    public int InitialEloRating { get; set; } = 1000;

    /// <summary>
    /// K-factor for ELO calculation (rating volatility).
    /// Higher = faster rating changes.
    /// </summary>
    public int EloKFactor { get; set; } = 32;

    #endregion

    #region Logging Settings

    /// <summary>
    /// Interval (in generations) for detailed logging.
    /// </summary>
    public int LogInterval { get; set; } = 1;

    /// <summary>
    /// Whether to log individual genome evaluations.
    /// </summary>
    public bool LogIndividualEvaluations { get; set; } = false;

    /// <summary>
    /// Directory for training logs.
    /// </summary>
    public string LogDirectory { get; set; } = @"C:\TzarBot\Logs\Training";

    #endregion

    #region Random Seed

    /// <summary>
    /// Random seed for reproducibility.
    /// Set to -1 for random initialization.
    /// </summary>
    public int Seed { get; set; } = -1;

    #endregion

    #region Validation

    /// <summary>
    /// Validates the configuration values are within acceptable ranges.
    /// </summary>
    public bool IsValid()
    {
        if (PopulationSize < 10)
            return false;
        if (GamesPerGenome < 1)
            return false;
        if (MaxParallelVMs < 1 || MaxParallelVMs > 10)
            return false;
        if (MaxGenerations < -1 || MaxGenerations == 0)
            return false;
        if (GameTimeout <= TimeSpan.Zero)
            return false;
        if (CheckpointInterval < 0)
            return false;
        if (MaxCheckpoints < 1)
            return false;
        if (TournamentRounds < 1)
            return false;
        if (EloKFactor < 1)
            return false;

        return true;
    }

    /// <summary>
    /// Gets validation errors as a list of strings.
    /// </summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (PopulationSize < 10)
            errors.Add("PopulationSize must be at least 10");
        if (GamesPerGenome < 1)
            errors.Add("GamesPerGenome must be at least 1");
        if (MaxParallelVMs < 1 || MaxParallelVMs > 10)
            errors.Add("MaxParallelVMs must be between 1 and 10");
        if (MaxGenerations < -1 || MaxGenerations == 0)
            errors.Add("MaxGenerations must be -1 (unlimited) or positive");
        if (GameTimeout <= TimeSpan.Zero)
            errors.Add("GameTimeout must be positive");
        if (CheckpointInterval < 0)
            errors.Add("CheckpointInterval cannot be negative");
        if (MaxCheckpoints < 1)
            errors.Add("MaxCheckpoints must be at least 1");
        if (TournamentRounds < 1)
            errors.Add("TournamentRounds must be at least 1");
        if (EloKFactor < 1)
            errors.Add("EloKFactor must be at least 1");

        return errors;
    }

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static TrainingConfig Default() => new();

    /// <summary>
    /// Creates a configuration optimized for quick testing.
    /// </summary>
    public static TrainingConfig QuickTest() => new()
    {
        PopulationSize = 20,
        GamesPerGenome = 1,
        MaxGenerations = 10,
        CheckpointInterval = 5,
        GameTimeout = TimeSpan.FromMinutes(10),
        MaxTrainingTime = TimeSpan.FromHours(1)
    };

    #endregion

    public override string ToString()
    {
        return $"TrainingConfig(pop={PopulationSize}, games={GamesPerGenome}, " +
               $"vms={MaxParallelVMs}, maxGen={MaxGenerations})";
    }
}
