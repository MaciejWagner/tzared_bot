using Microsoft.AspNetCore.SignalR;
using TzarBot.Dashboard.Models;
using TzarBot.Dashboard.Services;

namespace TzarBot.Dashboard.Hubs;

/// <summary>
/// SignalR hub for real-time training updates.
/// </summary>
public class TrainingHub : Hub
{
    private readonly ITrainingStateService _trainingService;
    private readonly ILogger<TrainingHub> _logger;

    public TrainingHub(ITrainingStateService trainingService, ILogger<TrainingHub> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    /// <summary>
    /// Join the monitoring group to receive updates.
    /// </summary>
    public async Task JoinMonitoring()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "monitoring");
        _logger.LogInformation("Client {ConnectionId} joined monitoring", Context.ConnectionId);

        // Send current state immediately
        await Clients.Caller.SendAsync("StateSnapshot", new TrainingStateSnapshot
        {
            Status = _trainingService.Status,
            CurrentGeneration = _trainingService.CurrentGeneration,
            CurrentStage = _trainingService.CurrentStage,
            BestFitness = _trainingService.BestFitness,
            AverageFitness = _trainingService.AverageFitness,
            PopulationSize = _trainingService.PopulationSize,
            TotalGamesPlayed = _trainingService.TotalGamesPlayed,
            WinRate = _trainingService.WinRate,
            TrainingStartedAt = _trainingService.TrainingStartedAt
        });
    }

    /// <summary>
    /// Leave the monitoring group.
    /// </summary>
    public async Task LeaveMonitoring()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "monitoring");
        _logger.LogInformation("Client {ConnectionId} left monitoring", Context.ConnectionId);
    }

    /// <summary>
    /// Request pause of training.
    /// </summary>
    public async Task RequestPause()
    {
        _logger.LogInformation("Pause requested by client {ConnectionId}", Context.ConnectionId);
        await _trainingService.PauseAsync();
    }

    /// <summary>
    /// Request resume of training.
    /// </summary>
    public async Task RequestResume()
    {
        _logger.LogInformation("Resume requested by client {ConnectionId}", Context.ConnectionId);
        await _trainingService.ResumeAsync();
    }

    /// <summary>
    /// Request checkpoint save.
    /// </summary>
    public async Task RequestSaveCheckpoint()
    {
        _logger.LogInformation("Checkpoint save requested by client {ConnectionId}", Context.ConnectionId);
        await _trainingService.SaveCheckpointAsync();
    }

    /// <summary>
    /// Get generation history.
    /// </summary>
    public Task<IReadOnlyList<DashboardGenerationStats>> GetGenerationHistory()
    {
        return Task.FromResult(_trainingService.GenerationHistory);
    }

    /// <summary>
    /// Get current population.
    /// </summary>
    public Task<IReadOnlyList<GenomeSummary>> GetCurrentPopulation()
    {
        return Task.FromResult(_trainingService.CurrentPopulation);
    }

    /// <summary>
    /// Get recent activity log.
    /// </summary>
    public Task<IReadOnlyList<ActivityLogEntry>> GetRecentActivity()
    {
        return Task.FromResult(_trainingService.RecentActivity);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "monitoring");
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Snapshot of current training state.
/// </summary>
public class TrainingStateSnapshot
{
    public TrainingStatus Status { get; init; }
    public int CurrentGeneration { get; init; }
    public string CurrentStage { get; init; } = string.Empty;
    public float BestFitness { get; init; }
    public float AverageFitness { get; init; }
    public int PopulationSize { get; init; }
    public int TotalGamesPlayed { get; init; }
    public float WinRate { get; init; }
    public DateTime? TrainingStartedAt { get; init; }
}
