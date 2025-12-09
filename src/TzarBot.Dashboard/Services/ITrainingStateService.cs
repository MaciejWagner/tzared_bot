using TzarBot.Dashboard.Models;

namespace TzarBot.Dashboard.Services;

/// <summary>
/// Interface for accessing training state and controlling the training pipeline.
/// </summary>
public interface ITrainingStateService
{
    /// <summary>
    /// Current training status (Running, Paused, Stopped).
    /// </summary>
    TrainingStatus Status { get; }

    /// <summary>
    /// Current generation number.
    /// </summary>
    int CurrentGeneration { get; }

    /// <summary>
    /// Current curriculum stage name.
    /// </summary>
    string CurrentStage { get; }

    /// <summary>
    /// Best fitness achieved so far.
    /// </summary>
    float BestFitness { get; }

    /// <summary>
    /// Average fitness of current population.
    /// </summary>
    float AverageFitness { get; }

    /// <summary>
    /// Population size.
    /// </summary>
    int PopulationSize { get; }

    /// <summary>
    /// Total number of games played.
    /// </summary>
    int TotalGamesPlayed { get; }

    /// <summary>
    /// Overall win rate (0.0 to 1.0).
    /// </summary>
    float WinRate { get; }

    /// <summary>
    /// Time when training started.
    /// </summary>
    DateTime? TrainingStartedAt { get; }

    /// <summary>
    /// History of generation statistics.
    /// </summary>
    IReadOnlyList<DashboardGenerationStats> GenerationHistory { get; }

    /// <summary>
    /// Current population of genomes (summary data only).
    /// </summary>
    IReadOnlyList<GenomeSummary> CurrentPopulation { get; }

    /// <summary>
    /// Recent activity log entries.
    /// </summary>
    IReadOnlyList<ActivityLogEntry> RecentActivity { get; }

    /// <summary>
    /// Pause the training pipeline.
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Resume the training pipeline.
    /// </summary>
    Task ResumeAsync();

    /// <summary>
    /// Save a checkpoint of current state.
    /// </summary>
    Task SaveCheckpointAsync();

    /// <summary>
    /// Event fired when a generation completes.
    /// </summary>
    event Action<DashboardGenerationStats>? OnGenerationComplete;

    /// <summary>
    /// Event fired when training status changes.
    /// </summary>
    event Action<TrainingStatus>? OnStatusChanged;

    /// <summary>
    /// Event fired when advancing to a new curriculum stage.
    /// </summary>
    event Action<string>? OnStageAdvanced;

    /// <summary>
    /// Event fired when a new best genome is found.
    /// </summary>
    event Action<GenomeSummary>? OnNewBestGenome;
}

/// <summary>
/// Training pipeline status.
/// </summary>
public enum TrainingStatus
{
    Stopped,
    Running,
    Paused,
    Error
}
