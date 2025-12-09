namespace TzarBot.GeneticAlgorithm.Fitness;

/// <summary>
/// Calculates fitness score for game results.
///
/// Fitness components:
/// 1. Win bonus (major component)
/// 2. Time bonus (faster wins are better)
/// 3. Units/buildings score (production and military)
/// 4. Resource efficiency
/// 5. Penalties (inactivity, invalid actions)
///
/// The fitness function is designed to:
/// - Strongly reward wins
/// - Encourage active gameplay
/// - Penalize degenerate behaviors (idle, spamming invalid actions)
/// - Support incremental learning (partial rewards)
/// </summary>
public sealed class GameFitnessCalculator : IFitnessCalculator
{
    private readonly FitnessWeights _weights;

    /// <summary>
    /// Creates fitness calculator with default weights.
    /// </summary>
    public GameFitnessCalculator() : this(FitnessWeights.Default())
    {
    }

    /// <summary>
    /// Creates fitness calculator with custom weights.
    /// </summary>
    public GameFitnessCalculator(FitnessWeights weights)
    {
        _weights = weights ?? throw new ArgumentNullException(nameof(weights));
    }

    /// <inheritdoc />
    public float Calculate(GameResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        float fitness = 0f;

        // 1. Win bonus (most important)
        if (result.Won)
        {
            fitness += _weights.WinBonus;

            // Time bonus for fast wins
            float timeFactor = result.TimeEfficiency;
            fitness += _weights.TimeBonus * timeFactor;
        }

        // 2. Unit score
        float unitScore = CalculateUnitScore(result);
        fitness += _weights.UnitWeight * unitScore;

        // 3. Building score
        float buildingScore = CalculateBuildingScore(result);
        fitness += _weights.BuildingWeight * buildingScore;

        // 4. Resource efficiency
        float resourceScore = CalculateResourceScore(result);
        fitness += _weights.ResourceWeight * resourceScore;

        // 5. Combat score
        float combatScore = CalculateCombatScore(result);
        fitness += _weights.CombatWeight * combatScore;

        // 6. Activity score
        float activityScore = CalculateActivityScore(result);
        fitness += _weights.ActivityWeight * activityScore;

        // 7. Exploration bonus
        fitness += _weights.ExplorationWeight * result.ExplorationScore;

        // 8. Penalties
        float penalties = CalculatePenalties(result);
        fitness -= penalties;

        // 9. Bonus from game score (if available)
        if (result.GameScore.HasValue)
        {
            fitness += _weights.GameScoreWeight * (result.GameScore.Value / 10000f);
        }

        // Ensure non-negative fitness (but allow negative for very poor performance)
        return Math.Max(fitness, _weights.MinimumFitness);
    }

    /// <inheritdoc />
    public float CalculateAverage(IEnumerable<GameResult> results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        var resultList = results.ToList();
        if (resultList.Count == 0)
            return 0f;

        return resultList.Average(r => Calculate(r));
    }

    /// <summary>
    /// Calculates unit-related score.
    /// </summary>
    private float CalculateUnitScore(GameResult result)
    {
        // Units built contribute positively
        float score = result.UnitsBuilt * 0.1f;

        // Kill/death ratio bonus
        if (result.UnitKDRatio > 1f)
        {
            score += (result.UnitKDRatio - 1f) * 5f;
        }

        // Enemy units killed
        score += result.UnitsKilled * 0.2f;

        return Math.Min(score, 100f); // Cap to prevent outliers
    }

    /// <summary>
    /// Calculates building-related score.
    /// </summary>
    private float CalculateBuildingScore(GameResult result)
    {
        // Buildings are valuable
        float score = result.BuildingsBuilt * 0.5f;

        // Destroying enemy buildings is very good
        score += result.BuildingsDestroyed * 1.0f;

        // Losing buildings is bad (but already counted in loss penalty)
        score -= result.BuildingsLost * 0.2f;

        return Math.Max(0f, Math.Min(score, 50f));
    }

    /// <summary>
    /// Calculates resource efficiency score.
    /// </summary>
    private float CalculateResourceScore(GameResult result)
    {
        if (result.ResourcesGathered == 0)
            return 0f;

        // Spending resources indicates activity
        float spendRatio = result.ResourcesSpent / (float)Math.Max(1, result.ResourcesGathered);

        // Optimal spend ratio is around 0.7-0.9
        float efficiency = 1f - Math.Abs(spendRatio - 0.8f);
        efficiency = Math.Max(0f, efficiency);

        // Base score from resources gathered
        float baseScore = result.ResourcesGathered / 1000f;

        return Math.Min(baseScore * efficiency, 30f);
    }

    /// <summary>
    /// Calculates combat effectiveness score.
    /// </summary>
    private float CalculateCombatScore(GameResult result)
    {
        if (result.DamageDealt == 0 && result.DamageReceived == 0)
            return 0f;

        // Damage ratio (dealt vs received)
        float totalDamage = result.DamageDealt + result.DamageReceived;
        if (totalDamage == 0) return 0f;

        float damageRatio = result.DamageDealt / totalDamage;

        // Score is proportional to damage dealt and ratio
        float score = (result.DamageDealt / 1000f) * damageRatio;

        return Math.Min(score, 50f);
    }

    /// <summary>
    /// Calculates activity score (penalizes idle behavior).
    /// </summary>
    private float CalculateActivityScore(GameResult result)
    {
        // Valid actions indicate engagement
        float actionScore = result.ValidActions * 0.01f;

        // Activity ratio (1 - inactivity)
        float activityRatio = 1f - result.InactivityRatio;

        return Math.Min(actionScore * activityRatio, 20f);
    }

    /// <summary>
    /// Calculates penalties for undesired behaviors.
    /// </summary>
    private float CalculatePenalties(GameResult result)
    {
        float penalties = 0f;

        // Inactivity penalty
        if (result.InactivityRatio > 0.5f)
        {
            penalties += _weights.InactivityPenalty * (result.InactivityRatio - 0.5f) * 2f;
        }

        // Invalid action penalty
        if (result.InvalidActionRatio > 0.1f)
        {
            penalties += _weights.InvalidActionPenalty * result.InvalidActionRatio;
        }

        // Loss penalty (if applicable)
        if (!result.Won)
        {
            penalties += _weights.LossPenalty;

            // Additional penalty for quick losses
            if (result.DurationSeconds < 60f)
            {
                penalties += _weights.QuickLossPenalty;
            }
        }

        return penalties;
    }

    /// <summary>
    /// Calculates weighted fitness from multiple game results.
    /// More recent games can be weighted higher.
    /// </summary>
    public float CalculateWeightedAverage(
        IReadOnlyList<GameResult> results,
        bool weightRecent = true)
    {
        if (results == null || results.Count == 0)
            return 0f;

        if (!weightRecent || results.Count == 1)
            return CalculateAverage(results);

        float totalWeight = 0f;
        float weightedSum = 0f;

        for (int i = 0; i < results.Count; i++)
        {
            // More recent games (higher index) get higher weight
            float weight = 1f + (i / (float)results.Count);
            weightedSum += Calculate(results[i]) * weight;
            totalWeight += weight;
        }

        return weightedSum / totalWeight;
    }
}

/// <summary>
/// Weight configuration for fitness calculation.
/// </summary>
public sealed class FitnessWeights
{
    /// <summary>Bonus for winning a game.</summary>
    public float WinBonus { get; init; } = 100f;

    /// <summary>Bonus for fast wins (scaled by time efficiency).</summary>
    public float TimeBonus { get; init; } = 50f;

    /// <summary>Weight for unit-related score.</summary>
    public float UnitWeight { get; init; } = 1f;

    /// <summary>Weight for building-related score.</summary>
    public float BuildingWeight { get; init; } = 1f;

    /// <summary>Weight for resource efficiency.</summary>
    public float ResourceWeight { get; init; } = 0.5f;

    /// <summary>Weight for combat effectiveness.</summary>
    public float CombatWeight { get; init; } = 1.5f;

    /// <summary>Weight for activity score.</summary>
    public float ActivityWeight { get; init; } = 1f;

    /// <summary>Weight for exploration.</summary>
    public float ExplorationWeight { get; init; } = 0.3f;

    /// <summary>Weight for game score (if available).</summary>
    public float GameScoreWeight { get; init; } = 0.1f;

    /// <summary>Penalty for excessive inactivity.</summary>
    public float InactivityPenalty { get; init; } = 50f;

    /// <summary>Penalty for invalid actions.</summary>
    public float InvalidActionPenalty { get; init; } = 30f;

    /// <summary>Penalty for losing a game.</summary>
    public float LossPenalty { get; init; } = 10f;

    /// <summary>Additional penalty for quick losses (< 60 seconds).</summary>
    public float QuickLossPenalty { get; init; } = 20f;

    /// <summary>Minimum allowed fitness (floor).</summary>
    public float MinimumFitness { get; init; } = -100f;

    /// <summary>
    /// Creates default weights.
    /// </summary>
    public static FitnessWeights Default() => new();

    /// <summary>
    /// Creates weights optimized for early training (more forgiving).
    /// </summary>
    public static FitnessWeights EarlyTraining() => new()
    {
        WinBonus = 50f,
        TimeBonus = 20f,
        UnitWeight = 2f,
        BuildingWeight = 2f,
        ResourceWeight = 1f,
        ActivityWeight = 2f,
        InactivityPenalty = 20f,
        InvalidActionPenalty = 10f,
        LossPenalty = 5f,
        QuickLossPenalty = 10f,
        MinimumFitness = -50f
    };

    /// <summary>
    /// Creates weights for competitive training (stricter).
    /// </summary>
    public static FitnessWeights Competitive() => new()
    {
        WinBonus = 200f,
        TimeBonus = 100f,
        UnitWeight = 0.5f,
        BuildingWeight = 0.5f,
        CombatWeight = 2f,
        ActivityWeight = 0.5f,
        InactivityPenalty = 100f,
        InvalidActionPenalty = 50f,
        LossPenalty = 50f,
        QuickLossPenalty = 50f,
        MinimumFitness = -200f
    };
}
