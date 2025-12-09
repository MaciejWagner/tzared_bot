using TzarBot.Dashboard.Models;

namespace TzarBot.Dashboard.Services;

/// <summary>
/// Real implementation of ITrainingStateService that connects to actual training pipeline.
/// For now, this is a placeholder that can be extended to integrate with TzarBot.Orchestrator.
/// </summary>
public sealed class TrainingStateService : ITrainingStateService
{
    private readonly ILogger<TrainingStateService> _logger;
    private readonly List<DashboardGenerationStats> _generationHistory = new();
    private readonly List<GenomeSummary> _population = new();
    private readonly List<ActivityLogEntry> _activityLog = new();
    private readonly object _lock = new();

    public TrainingStateService(ILogger<TrainingStateService> logger)
    {
        _logger = logger;
    }

    public TrainingStatus Status { get; private set; } = TrainingStatus.Stopped;
    public int CurrentGeneration { get; private set; }
    public string CurrentStage { get; private set; } = "Not Started";
    public float BestFitness { get; private set; }
    public float AverageFitness { get; private set; }
    public int PopulationSize => _population.Count;
    public int TotalGamesPlayed { get; private set; }
    public float WinRate { get; private set; }
    public DateTime? TrainingStartedAt { get; private set; }

    public IReadOnlyList<DashboardGenerationStats> GenerationHistory
    {
        get
        {
            lock (_lock)
            {
                return _generationHistory.ToList();
            }
        }
    }

    public IReadOnlyList<GenomeSummary> CurrentPopulation
    {
        get
        {
            lock (_lock)
            {
                return _population.OrderByDescending(g => g.Fitness).ToList();
            }
        }
    }

    public IReadOnlyList<ActivityLogEntry> RecentActivity
    {
        get
        {
            lock (_lock)
            {
                return _activityLog.TakeLast(50).Reverse().ToList();
            }
        }
    }

    public event Action<DashboardGenerationStats>? OnGenerationComplete;
    public event Action<TrainingStatus>? OnStatusChanged;
    public event Action<string>? OnStageAdvanced;
    public event Action<GenomeSummary>? OnNewBestGenome;

    public async Task PauseAsync()
    {
        // TODO: Integrate with actual training pipeline
        _logger.LogInformation("Pause requested - not yet integrated with training pipeline");
        Status = TrainingStatus.Paused;
        OnStatusChanged?.Invoke(Status);
        await Task.CompletedTask;
    }

    public async Task ResumeAsync()
    {
        // TODO: Integrate with actual training pipeline
        _logger.LogInformation("Resume requested - not yet integrated with training pipeline");
        Status = TrainingStatus.Running;
        OnStatusChanged?.Invoke(Status);
        await Task.CompletedTask;
    }

    public async Task SaveCheckpointAsync()
    {
        // TODO: Integrate with actual training pipeline
        _logger.LogInformation("Checkpoint save requested - not yet integrated with training pipeline");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called by training pipeline to update generation stats.
    /// </summary>
    public void UpdateGenerationStats(DashboardGenerationStats stats)
    {
        lock (_lock)
        {
            _generationHistory.Add(stats);
            CurrentGeneration = stats.Generation;
            BestFitness = stats.BestFitness;
            AverageFitness = stats.AverageFitness;
            CurrentStage = stats.Stage;

            // Keep history limited
            while (_generationHistory.Count > 500)
            {
                _generationHistory.RemoveAt(0);
            }
        }

        OnGenerationComplete?.Invoke(stats);
    }

    /// <summary>
    /// Called by training pipeline to update population.
    /// </summary>
    public void UpdatePopulation(IEnumerable<GenomeSummary> genomes)
    {
        lock (_lock)
        {
            _population.Clear();
            _population.AddRange(genomes);
        }
    }

    /// <summary>
    /// Called by training pipeline to add activity log entry.
    /// </summary>
    public void AddActivity(ActivityLogEntry entry)
    {
        lock (_lock)
        {
            _activityLog.Add(entry);
            while (_activityLog.Count > 100)
            {
                _activityLog.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Called by training pipeline when a new best genome is found.
    /// </summary>
    public void NotifyNewBestGenome(GenomeSummary genome)
    {
        OnNewBestGenome?.Invoke(genome);
    }

    /// <summary>
    /// Called by training pipeline when advancing to a new stage.
    /// </summary>
    public void NotifyStageAdvanced(string newStage)
    {
        CurrentStage = newStage;
        OnStageAdvanced?.Invoke(newStage);
    }

    /// <summary>
    /// Called by training pipeline when training starts.
    /// </summary>
    public void NotifyTrainingStarted()
    {
        Status = TrainingStatus.Running;
        TrainingStartedAt = DateTime.UtcNow;
        OnStatusChanged?.Invoke(Status);
    }

    /// <summary>
    /// Called by training pipeline when training stops.
    /// </summary>
    public void NotifyTrainingStopped()
    {
        Status = TrainingStatus.Stopped;
        OnStatusChanged?.Invoke(Status);
    }
}
