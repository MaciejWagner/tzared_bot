using TzarBot.GeneticAlgorithm.Fitness;
using TzarBot.Training.Core;

namespace TzarBot.Training.Curriculum;

/// <summary>
/// Interface for curriculum learning management.
/// Handles stage transitions based on training progress.
/// </summary>
public interface ICurriculumManager
{
    /// <summary>
    /// Current curriculum stage.
    /// </summary>
    CurriculumStage CurrentStage { get; }

    /// <summary>
    /// All available stages.
    /// </summary>
    IReadOnlyList<CurriculumStage> AllStages { get; }

    /// <summary>
    /// Current stage metrics.
    /// </summary>
    StageMetrics CurrentMetrics { get; }

    /// <summary>
    /// History of stage transitions.
    /// </summary>
    IReadOnlyList<StageTransitionRecord> TransitionHistory { get; }

    /// <summary>
    /// Sets the current stage by name.
    /// </summary>
    /// <param name="stageName">Name of the stage.</param>
    void SetStage(string stageName);

    /// <summary>
    /// Updates metrics with generation results.
    /// </summary>
    /// <param name="stats">Generation statistics.</param>
    void UpdateMetrics(GenerationStats stats);

    /// <summary>
    /// Evaluates whether stage transition should occur.
    /// </summary>
    /// <returns>Transition decision and reason.</returns>
    (StageTransitionType Type, string Reason) EvaluateTransition();

    /// <summary>
    /// Advances to the next stage.
    /// </summary>
    /// <param name="reason">Reason for promotion.</param>
    /// <returns>True if promotion occurred.</returns>
    bool TryPromote(string reason);

    /// <summary>
    /// Returns to the previous stage.
    /// </summary>
    /// <param name="reason">Reason for demotion.</param>
    /// <returns>True if demotion occurred.</returns>
    bool TryDemote(string reason);

    /// <summary>
    /// Gets fitness weights for the current stage.
    /// </summary>
    FitnessWeights GetCurrentFitnessWeights();

    /// <summary>
    /// Resets metrics for current stage (called on stage entry).
    /// </summary>
    void ResetStageMetrics();

    /// <summary>
    /// Event raised when stage changes.
    /// </summary>
    event EventHandler<StageTransitionRecord>? StageChanged;
}

/// <summary>
/// Type of stage transition.
/// </summary>
public enum StageTransitionType
{
    /// <summary>
    /// No transition.
    /// </summary>
    None,

    /// <summary>
    /// Promotion to next stage.
    /// </summary>
    Promotion,

    /// <summary>
    /// Demotion to previous stage.
    /// </summary>
    Demotion
}

/// <summary>
/// Record of a stage transition.
/// </summary>
public sealed class StageTransitionRecord
{
    public required string FromStage { get; init; }
    public required string ToStage { get; init; }
    public required StageTransitionType Type { get; init; }
    public required int Generation { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Reason { get; init; }
    public StageMetrics? MetricsAtTransition { get; init; }
}
