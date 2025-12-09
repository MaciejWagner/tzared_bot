namespace TzarBot.Training.Curriculum;

/// <summary>
/// Defines a curriculum learning stage.
/// Each stage has specific opponent settings and fitness evaluation mode.
/// </summary>
public sealed class CurriculumStage
{
    /// <summary>
    /// Unique name of the stage.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the stage.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Order of the stage in the curriculum (lower = earlier).
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Opponent type for this stage.
    /// </summary>
    public OpponentType Opponent { get; init; } = OpponentType.PassiveAI;

    /// <summary>
    /// AI difficulty level (for AI opponents).
    /// </summary>
    public AIDifficulty Difficulty { get; init; } = AIDifficulty.Easy;

    /// <summary>
    /// Fitness evaluation mode for this stage.
    /// Determines which metrics are prioritized.
    /// </summary>
    public FitnessMode FitnessMode { get; init; } = FitnessMode.Survival;

    /// <summary>
    /// Criteria for promotion to the next stage.
    /// </summary>
    public PromotionCriteria PromotionCriteria { get; init; } = new();

    /// <summary>
    /// Criteria for demotion to the previous stage.
    /// </summary>
    public DemotionCriteria? DemotionCriteria { get; init; }

    /// <summary>
    /// Custom fitness weights for this stage.
    /// Null = use defaults based on FitnessMode.
    /// </summary>
    public FitnessWeightsOverride? FitnessWeights { get; init; }

    /// <summary>
    /// Maximum game duration for this stage.
    /// Shorter limits encourage faster play in later stages.
    /// </summary>
    public TimeSpan MaxGameDuration { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Minimum generations to spend in this stage before promotion.
    /// </summary>
    public int MinGenerationsInStage { get; init; } = 5;

    /// <summary>
    /// Whether this is the final (tournament) stage.
    /// </summary>
    public bool IsFinalStage { get; init; }

    /// <summary>
    /// Checks if promotion criteria are met.
    /// </summary>
    public bool ShouldPromote(StageMetrics metrics)
    {
        if (IsFinalStage)
            return false; // Cannot promote from final stage

        if (metrics.GenerationsInStage < MinGenerationsInStage)
            return false;

        return PromotionCriteria.IsMet(metrics);
    }

    /// <summary>
    /// Checks if demotion criteria are met.
    /// </summary>
    public bool ShouldDemote(StageMetrics metrics)
    {
        if (DemotionCriteria == null)
            return false;

        return DemotionCriteria.IsMet(metrics);
    }

    public override string ToString()
    {
        return $"Stage[{Name}] Order={Order}, Opponent={Opponent}:{Difficulty}, Mode={FitnessMode}";
    }
}

/// <summary>
/// Type of opponent for training.
/// </summary>
public enum OpponentType
{
    /// <summary>
    /// No opponent - passive environment.
    /// </summary>
    PassiveAI,

    /// <summary>
    /// Built-in game AI.
    /// </summary>
    GameAI,

    /// <summary>
    /// Self-play against other evolved genomes.
    /// </summary>
    SelfPlay,

    /// <summary>
    /// Tournament mode - Swiss-system matches.
    /// </summary>
    Tournament
}

/// <summary>
/// AI difficulty levels (matching Tzar game settings).
/// </summary>
public enum AIDifficulty
{
    Passive = 0,
    Easy = 1,
    Normal = 2,
    Hard = 3,
    Insane = 4
}

/// <summary>
/// Fitness evaluation mode - determines which metrics are prioritized.
/// </summary>
public enum FitnessMode
{
    /// <summary>
    /// Focus on staying alive as long as possible.
    /// Primary: survival time, activity
    /// Secondary: resource gathering
    /// </summary>
    Survival,

    /// <summary>
    /// Focus on building economy and units.
    /// Primary: units built, resources gathered
    /// Secondary: survival time
    /// </summary>
    Economy,

    /// <summary>
    /// Focus on combat effectiveness.
    /// Primary: enemy units killed, damage dealt
    /// Secondary: own units preserved
    /// </summary>
    Combat,

    /// <summary>
    /// Focus on winning games.
    /// Primary: wins
    /// Secondary: time efficiency
    /// </summary>
    Victory,

    /// <summary>
    /// Focus on winning quickly.
    /// Primary: wins with time bonus
    /// Secondary: efficiency
    /// </summary>
    Efficiency
}

/// <summary>
/// Criteria for stage promotion.
/// </summary>
public sealed class PromotionCriteria
{
    /// <summary>
    /// Minimum win rate required (0-1).
    /// </summary>
    public float MinWinRate { get; init; } = 0.5f;

    /// <summary>
    /// Minimum average fitness required.
    /// </summary>
    public float MinAverageFitness { get; init; } = 50f;

    /// <summary>
    /// Minimum best fitness required.
    /// </summary>
    public float MinBestFitness { get; init; } = 100f;

    /// <summary>
    /// Number of consecutive generations meeting criteria.
    /// </summary>
    public int ConsecutiveGenerations { get; init; } = 3;

    /// <summary>
    /// Checks if the criteria are met.
    /// </summary>
    public bool IsMet(StageMetrics metrics)
    {
        if (metrics.WinRate < MinWinRate)
            return false;
        if (metrics.AverageFitness < MinAverageFitness)
            return false;
        if (metrics.BestFitness < MinBestFitness)
            return false;
        if (metrics.ConsecutiveGoodGenerations < ConsecutiveGenerations)
            return false;

        return true;
    }
}

/// <summary>
/// Criteria for stage demotion.
/// </summary>
public sealed class DemotionCriteria
{
    /// <summary>
    /// Win rate below which demotion is triggered.
    /// </summary>
    public float MaxWinRate { get; init; } = 0.1f;

    /// <summary>
    /// Average fitness below which demotion is triggered.
    /// </summary>
    public float MaxAverageFitness { get; init; } = 10f;

    /// <summary>
    /// Number of consecutive poor generations before demotion.
    /// </summary>
    public int ConsecutiveGenerations { get; init; } = 5;

    /// <summary>
    /// Checks if the criteria are met.
    /// </summary>
    public bool IsMet(StageMetrics metrics)
    {
        if (metrics.WinRate > MaxWinRate)
            return false;
        if (metrics.AverageFitness > MaxAverageFitness)
            return false;
        if (metrics.ConsecutivePoorGenerations < ConsecutiveGenerations)
            return false;

        return true;
    }
}

/// <summary>
/// Metrics used for stage transition decisions.
/// </summary>
public sealed class StageMetrics
{
    public int GenerationsInStage { get; set; }
    public float WinRate { get; set; }
    public float AverageFitness { get; set; }
    public float BestFitness { get; set; }
    public int ConsecutiveGoodGenerations { get; set; }
    public int ConsecutivePoorGenerations { get; set; }
    public int TotalWins { get; set; }
    public int TotalGames { get; set; }
}

/// <summary>
/// Custom fitness weight overrides for a stage.
/// </summary>
public sealed class FitnessWeightsOverride
{
    public float? WinBonus { get; init; }
    public float? TimeBonus { get; init; }
    public float? UnitWeight { get; init; }
    public float? BuildingWeight { get; init; }
    public float? ResourceWeight { get; init; }
    public float? CombatWeight { get; init; }
    public float? ActivityWeight { get; init; }
    public float? ExplorationWeight { get; init; }
    public float? InactivityPenalty { get; init; }
    public float? InvalidActionPenalty { get; init; }
    public float? LossPenalty { get; init; }
}
