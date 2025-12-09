using Microsoft.Extensions.Logging;
using TzarBot.GeneticAlgorithm.Fitness;
using TzarBot.Training.Core;

namespace TzarBot.Training.Curriculum;

/// <summary>
/// Manages curriculum learning progression.
///
/// Curriculum learning gradually increases difficulty as the population improves.
/// This prevents premature convergence to local optima and ensures stable learning.
///
/// Key behaviors:
/// 1. Tracks metrics per stage
/// 2. Promotes when criteria are consistently met
/// 3. Demotes when performance drops severely (optional)
/// 4. Provides stage-specific fitness weights
/// </summary>
public sealed class CurriculumManager : ICurriculumManager
{
    private readonly ILogger<CurriculumManager>? _logger;
    private readonly IReadOnlyList<CurriculumStage> _stages;
    private readonly bool _allowDemotion;
    private readonly List<StageTransitionRecord> _transitionHistory = new();

    private CurriculumStage _currentStage;
    private StageMetrics _currentMetrics = new();
    private int _currentGeneration;

    // Thresholds for consecutive generation tracking
    private float _goodFitnessThreshold;
    private float _poorFitnessThreshold;

    /// <inheritdoc />
    public CurriculumStage CurrentStage => _currentStage;

    /// <inheritdoc />
    public IReadOnlyList<CurriculumStage> AllStages => _stages;

    /// <inheritdoc />
    public StageMetrics CurrentMetrics => _currentMetrics;

    /// <inheritdoc />
    public IReadOnlyList<StageTransitionRecord> TransitionHistory => _transitionHistory.AsReadOnly();

    /// <inheritdoc />
    public event EventHandler<StageTransitionRecord>? StageChanged;

    /// <summary>
    /// Creates a curriculum manager with default stages.
    /// </summary>
    public CurriculumManager(
        ILogger<CurriculumManager>? logger = null,
        bool allowDemotion = true)
        : this(StageDefinitions.GetAllStages(), logger, allowDemotion)
    {
    }

    /// <summary>
    /// Creates a curriculum manager with custom stages.
    /// </summary>
    public CurriculumManager(
        IReadOnlyList<CurriculumStage> stages,
        ILogger<CurriculumManager>? logger = null,
        bool allowDemotion = true)
    {
        _logger = logger;
        _allowDemotion = allowDemotion;

        if (stages == null || stages.Count == 0)
            throw new ArgumentException("At least one stage is required", nameof(stages));

        // Sort stages by order
        _stages = stages.OrderBy(s => s.Order).ToList();

        // Start at the first stage
        _currentStage = _stages[0];
        UpdateThresholds();

        _logger?.LogInformation("Curriculum initialized with {Count} stages, starting at {Stage}",
            _stages.Count, _currentStage.Name);
    }

    /// <inheritdoc />
    public void SetStage(string stageName)
    {
        var stage = _stages.FirstOrDefault(s =>
            s.Name.Equals(stageName, StringComparison.OrdinalIgnoreCase));

        if (stage == null)
            throw new ArgumentException($"Unknown stage: {stageName}", nameof(stageName));

        var oldStage = _currentStage;
        _currentStage = stage;
        ResetStageMetrics();
        UpdateThresholds();

        _logger?.LogInformation("Stage set to {NewStage} (from {OldStage})",
            _currentStage.Name, oldStage.Name);
    }

    /// <inheritdoc />
    public void UpdateMetrics(GenerationStats stats)
    {
        _currentGeneration = stats.Generation;
        _currentMetrics.GenerationsInStage++;
        _currentMetrics.TotalGames += stats.GamesPlayed;
        _currentMetrics.TotalWins += stats.Wins;
        _currentMetrics.WinRate = _currentMetrics.TotalGames > 0
            ? (float)_currentMetrics.TotalWins / _currentMetrics.TotalGames
            : 0f;
        _currentMetrics.AverageFitness = stats.AverageFitness;
        _currentMetrics.BestFitness = Math.Max(_currentMetrics.BestFitness, stats.BestFitness);

        // Track consecutive good/poor generations
        if (IsGoodGeneration(stats))
        {
            _currentMetrics.ConsecutiveGoodGenerations++;
            _currentMetrics.ConsecutivePoorGenerations = 0;
        }
        else if (IsPoorGeneration(stats))
        {
            _currentMetrics.ConsecutivePoorGenerations++;
            _currentMetrics.ConsecutiveGoodGenerations = 0;
        }
        else
        {
            // Neutral - reset both counters
            _currentMetrics.ConsecutiveGoodGenerations = 0;
            _currentMetrics.ConsecutivePoorGenerations = 0;
        }

        _logger?.LogDebug(
            "Stage {Stage} metrics updated: Gen={Gen}, WinRate={WinRate:P0}, " +
            "AvgFit={AvgFit:F1}, GoodStreak={Good}, PoorStreak={Poor}",
            _currentStage.Name,
            _currentMetrics.GenerationsInStage,
            _currentMetrics.WinRate,
            _currentMetrics.AverageFitness,
            _currentMetrics.ConsecutiveGoodGenerations,
            _currentMetrics.ConsecutivePoorGenerations);
    }

    /// <inheritdoc />
    public (StageTransitionType Type, string Reason) EvaluateTransition()
    {
        // Check promotion first
        if (_currentStage.ShouldPromote(_currentMetrics))
        {
            return (StageTransitionType.Promotion,
                $"Promotion criteria met: WinRate={_currentMetrics.WinRate:P0}, " +
                $"AvgFitness={_currentMetrics.AverageFitness:F1}, " +
                $"GoodStreak={_currentMetrics.ConsecutiveGoodGenerations}");
        }

        // Check demotion
        if (_allowDemotion && _currentStage.ShouldDemote(_currentMetrics))
        {
            return (StageTransitionType.Demotion,
                $"Demotion criteria met: WinRate={_currentMetrics.WinRate:P0}, " +
                $"AvgFitness={_currentMetrics.AverageFitness:F1}, " +
                $"PoorStreak={_currentMetrics.ConsecutivePoorGenerations}");
        }

        return (StageTransitionType.None, string.Empty);
    }

    /// <inheritdoc />
    public bool TryPromote(string reason)
    {
        if (_currentStage.IsFinalStage)
        {
            _logger?.LogWarning("Cannot promote from final stage {Stage}", _currentStage.Name);
            return false;
        }

        var nextIndex = _stages.ToList().IndexOf(_currentStage) + 1;
        if (nextIndex >= _stages.Count)
        {
            _logger?.LogWarning("No next stage available after {Stage}", _currentStage.Name);
            return false;
        }

        var nextStage = _stages[nextIndex];
        var record = RecordTransition(_currentStage, nextStage, StageTransitionType.Promotion, reason);

        _currentStage = nextStage;
        ResetStageMetrics();
        UpdateThresholds();

        _logger?.LogInformation("PROMOTION: {From} -> {To}. Reason: {Reason}",
            record.FromStage, record.ToStage, reason);

        StageChanged?.Invoke(this, record);
        return true;
    }

    /// <inheritdoc />
    public bool TryDemote(string reason)
    {
        if (!_allowDemotion)
        {
            _logger?.LogDebug("Demotion disabled");
            return false;
        }

        var currentIndex = _stages.ToList().IndexOf(_currentStage);
        if (currentIndex <= 0)
        {
            _logger?.LogWarning("Cannot demote from first stage {Stage}", _currentStage.Name);
            return false;
        }

        var prevStage = _stages[currentIndex - 1];
        var record = RecordTransition(_currentStage, prevStage, StageTransitionType.Demotion, reason);

        _currentStage = prevStage;
        ResetStageMetrics();
        UpdateThresholds();

        _logger?.LogWarning("DEMOTION: {From} -> {To}. Reason: {Reason}",
            record.FromStage, record.ToStage, reason);

        StageChanged?.Invoke(this, record);
        return true;
    }

    /// <inheritdoc />
    public FitnessWeights GetCurrentFitnessWeights()
    {
        var baseWeights = GetBaseWeightsForMode(_currentStage.FitnessMode);

        // Apply stage-specific overrides
        var overrides = _currentStage.FitnessWeights;
        if (overrides == null)
            return baseWeights;

        return new FitnessWeights
        {
            WinBonus = overrides.WinBonus ?? baseWeights.WinBonus,
            TimeBonus = overrides.TimeBonus ?? baseWeights.TimeBonus,
            UnitWeight = overrides.UnitWeight ?? baseWeights.UnitWeight,
            BuildingWeight = overrides.BuildingWeight ?? baseWeights.BuildingWeight,
            ResourceWeight = overrides.ResourceWeight ?? baseWeights.ResourceWeight,
            CombatWeight = overrides.CombatWeight ?? baseWeights.CombatWeight,
            ActivityWeight = overrides.ActivityWeight ?? baseWeights.ActivityWeight,
            ExplorationWeight = overrides.ExplorationWeight ?? baseWeights.ExplorationWeight,
            InactivityPenalty = overrides.InactivityPenalty ?? baseWeights.InactivityPenalty,
            InvalidActionPenalty = overrides.InvalidActionPenalty ?? baseWeights.InvalidActionPenalty,
            LossPenalty = overrides.LossPenalty ?? baseWeights.LossPenalty
        };
    }

    /// <inheritdoc />
    public void ResetStageMetrics()
    {
        _currentMetrics = new StageMetrics();
    }

    /// <summary>
    /// Gets base fitness weights for a given fitness mode.
    /// </summary>
    private static FitnessWeights GetBaseWeightsForMode(FitnessMode mode)
    {
        return mode switch
        {
            FitnessMode.Survival => new FitnessWeights
            {
                WinBonus = 20f,
                TimeBonus = 10f,
                UnitWeight = 1.5f,
                BuildingWeight = 1.5f,
                ResourceWeight = 2f,
                CombatWeight = 0.5f,
                ActivityWeight = 3f,
                ExplorationWeight = 1f,
                InactivityPenalty = 100f,
                InvalidActionPenalty = 50f,
                LossPenalty = 5f
            },
            FitnessMode.Economy => new FitnessWeights
            {
                WinBonus = 50f,
                TimeBonus = 20f,
                UnitWeight = 2f,
                BuildingWeight = 3f,
                ResourceWeight = 3f,
                CombatWeight = 0.5f,
                ActivityWeight = 1.5f,
                ExplorationWeight = 0.5f,
                InactivityPenalty = 50f,
                InvalidActionPenalty = 30f,
                LossPenalty = 10f
            },
            FitnessMode.Combat => new FitnessWeights
            {
                WinBonus = 100f,
                TimeBonus = 30f,
                UnitWeight = 1f,
                BuildingWeight = 0.5f,
                ResourceWeight = 0.5f,
                CombatWeight = 3f,
                ActivityWeight = 1f,
                ExplorationWeight = 0.5f,
                InactivityPenalty = 30f,
                InvalidActionPenalty = 20f,
                LossPenalty = 20f
            },
            FitnessMode.Victory => new FitnessWeights
            {
                WinBonus = 150f,
                TimeBonus = 50f,
                UnitWeight = 0.5f,
                BuildingWeight = 0.5f,
                ResourceWeight = 0.5f,
                CombatWeight = 1.5f,
                ActivityWeight = 0.5f,
                ExplorationWeight = 0.3f,
                InactivityPenalty = 20f,
                InvalidActionPenalty = 10f,
                LossPenalty = 30f
            },
            FitnessMode.Efficiency => new FitnessWeights
            {
                WinBonus = 200f,
                TimeBonus = 100f,
                UnitWeight = 0.3f,
                BuildingWeight = 0.3f,
                ResourceWeight = 0.3f,
                CombatWeight = 1f,
                ActivityWeight = 0.3f,
                ExplorationWeight = 0.2f,
                InactivityPenalty = 10f,
                InvalidActionPenalty = 5f,
                LossPenalty = 50f
            },
            _ => FitnessWeights.Default()
        };
    }

    private bool IsGoodGeneration(GenerationStats stats)
    {
        // A generation is "good" if it exceeds the threshold based on promotion criteria
        return stats.AverageFitness >= _goodFitnessThreshold &&
               (stats.WinRate >= _currentStage.PromotionCriteria.MinWinRate ||
                _currentStage.PromotionCriteria.MinWinRate == 0f);
    }

    private bool IsPoorGeneration(GenerationStats stats)
    {
        // A generation is "poor" if it's below the demotion threshold
        if (_currentStage.DemotionCriteria == null)
            return false;

        return stats.AverageFitness <= _poorFitnessThreshold &&
               stats.WinRate <= _currentStage.DemotionCriteria.MaxWinRate;
    }

    private void UpdateThresholds()
    {
        // Set thresholds based on promotion/demotion criteria
        _goodFitnessThreshold = _currentStage.PromotionCriteria.MinAverageFitness * 0.8f;
        _poorFitnessThreshold = _currentStage.DemotionCriteria?.MaxAverageFitness ?? 0f;
    }

    private StageTransitionRecord RecordTransition(
        CurriculumStage from,
        CurriculumStage to,
        StageTransitionType type,
        string reason)
    {
        var record = new StageTransitionRecord
        {
            FromStage = from.Name,
            ToStage = to.Name,
            Type = type,
            Generation = _currentGeneration,
            Timestamp = DateTime.UtcNow,
            Reason = reason,
            MetricsAtTransition = new StageMetrics
            {
                GenerationsInStage = _currentMetrics.GenerationsInStage,
                WinRate = _currentMetrics.WinRate,
                AverageFitness = _currentMetrics.AverageFitness,
                BestFitness = _currentMetrics.BestFitness,
                TotalGames = _currentMetrics.TotalGames,
                TotalWins = _currentMetrics.TotalWins,
                ConsecutiveGoodGenerations = _currentMetrics.ConsecutiveGoodGenerations,
                ConsecutivePoorGenerations = _currentMetrics.ConsecutivePoorGenerations
            }
        };

        _transitionHistory.Add(record);
        return record;
    }
}
