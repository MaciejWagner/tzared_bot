using MessagePack;
using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Core;

namespace TzarBot.Training.Checkpoint;

/// <summary>
/// Represents a complete training checkpoint.
/// Contains all data needed to resume training from this point.
///
/// Serialized using MessagePack with LZ4 compression for efficient storage.
/// </summary>
[MessagePackObject]
public sealed class TrainingCheckpoint
{
    /// <summary>
    /// Unique identifier for this checkpoint.
    /// </summary>
    [Key(0)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Version of the checkpoint format (for backward compatibility).
    /// </summary>
    [Key(1)]
    public int Version { get; set; } = 1;

    /// <summary>
    /// When this checkpoint was created.
    /// </summary>
    [Key(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Full training state at checkpoint time.
    /// </summary>
    [Key(3)]
    public required TrainingState State { get; set; }

    /// <summary>
    /// Training configuration used.
    /// </summary>
    [Key(4)]
    public required TrainingConfigSnapshot Config { get; set; }

    /// <summary>
    /// Random seed used for this training run.
    /// </summary>
    [Key(5)]
    public int Seed { get; set; }

    /// <summary>
    /// Custom metadata for extensibility.
    /// </summary>
    [Key(6)]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Checksum for data integrity verification.
    /// </summary>
    [Key(7)]
    public string? Checksum { get; set; }

    /// <summary>
    /// Creates a checkpoint from current training state.
    /// </summary>
    public static TrainingCheckpoint Create(
        TrainingState state,
        TrainingConfig config,
        int seed,
        Dictionary<string, string>? metadata = null)
    {
        return new TrainingCheckpoint
        {
            State = state,
            Config = TrainingConfigSnapshot.FromConfig(config),
            Seed = seed,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Validates the checkpoint integrity.
    /// </summary>
    public bool IsValid()
    {
        if (State == null)
            return false;
        if (State.Population == null || State.Population.Count == 0)
            return false;
        if (Config == null)
            return false;
        if (Version < 1)
            return false;

        return true;
    }

    public override string ToString()
    {
        return $"Checkpoint[{Id:N8}] Gen={State?.CurrentGeneration}, " +
               $"Pop={State?.Population.Count}, Stage={State?.CurrentStageName}";
    }
}

/// <summary>
/// Snapshot of training configuration stored in checkpoint.
/// Separate from TrainingConfig to allow for format evolution.
/// </summary>
[MessagePackObject]
public sealed class TrainingConfigSnapshot
{
    [Key(0)]
    public int PopulationSize { get; set; }

    [Key(1)]
    public int GamesPerGenome { get; set; }

    [Key(2)]
    public int MaxParallelVMs { get; set; }

    [Key(3)]
    public int MaxGenerations { get; set; }

    [Key(4)]
    public int GameTimeoutSeconds { get; set; }

    [Key(5)]
    public int CheckpointInterval { get; set; }

    [Key(6)]
    public bool UseCurriculum { get; set; }

    [Key(7)]
    public string? InitialStage { get; set; }

    [Key(8)]
    public bool AllowDemotion { get; set; }

    [Key(9)]
    public bool UseTournament { get; set; }

    [Key(10)]
    public int TournamentRounds { get; set; }

    [Key(11)]
    public int InitialEloRating { get; set; }

    [Key(12)]
    public int EloKFactor { get; set; }

    /// <summary>
    /// Creates a snapshot from configuration.
    /// </summary>
    public static TrainingConfigSnapshot FromConfig(TrainingConfig config)
    {
        return new TrainingConfigSnapshot
        {
            PopulationSize = config.PopulationSize,
            GamesPerGenome = config.GamesPerGenome,
            MaxParallelVMs = config.MaxParallelVMs,
            MaxGenerations = config.MaxGenerations,
            GameTimeoutSeconds = (int)config.GameTimeout.TotalSeconds,
            CheckpointInterval = config.CheckpointInterval,
            UseCurriculum = config.UseCurriculum,
            InitialStage = config.InitialStage,
            AllowDemotion = config.AllowDemotion,
            UseTournament = config.UseTournament,
            TournamentRounds = config.TournamentRounds,
            InitialEloRating = config.InitialEloRating,
            EloKFactor = config.EloKFactor
        };
    }

    /// <summary>
    /// Converts snapshot back to configuration.
    /// </summary>
    public TrainingConfig ToConfig()
    {
        return new TrainingConfig
        {
            PopulationSize = PopulationSize,
            GamesPerGenome = GamesPerGenome,
            MaxParallelVMs = MaxParallelVMs,
            MaxGenerations = MaxGenerations,
            GameTimeout = TimeSpan.FromSeconds(GameTimeoutSeconds),
            CheckpointInterval = CheckpointInterval,
            UseCurriculum = UseCurriculum,
            InitialStage = InitialStage ?? "Bootstrap",
            AllowDemotion = AllowDemotion,
            UseTournament = UseTournament,
            TournamentRounds = TournamentRounds,
            InitialEloRating = InitialEloRating,
            EloKFactor = EloKFactor
        };
    }
}

/// <summary>
/// Checkpoint for saving the best genome separately.
/// </summary>
[MessagePackObject]
public sealed class BestGenomeCheckpoint
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Key(1)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Key(2)]
    public required NetworkGenome Genome { get; set; }

    [Key(3)]
    public int FoundAtGeneration { get; set; }

    [Key(4)]
    public float Fitness { get; set; }

    [Key(5)]
    public string? StageName { get; set; }

    [Key(6)]
    public int? EloRating { get; set; }

    [Key(7)]
    public int GamesPlayed { get; set; }

    [Key(8)]
    public int Wins { get; set; }

    [Key(9)]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Win rate calculated from games played.
    /// </summary>
    [IgnoreMember]
    public float WinRate => GamesPlayed > 0 ? (float)Wins / GamesPlayed : 0f;

    public override string ToString()
    {
        return $"BestGenome[{Genome.Id:N8}] Fit={Fitness:F2}, Gen={FoundAtGeneration}, " +
               $"WinRate={WinRate:P0}";
    }
}
