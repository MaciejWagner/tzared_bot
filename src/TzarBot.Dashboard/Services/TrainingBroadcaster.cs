using Microsoft.AspNetCore.SignalR;
using TzarBot.Dashboard.Hubs;
using TzarBot.Dashboard.Models;

namespace TzarBot.Dashboard.Services;

/// <summary>
/// Background service that broadcasts training updates via SignalR.
/// </summary>
public class TrainingBroadcaster : BackgroundService
{
    private readonly IHubContext<TrainingHub> _hubContext;
    private readonly ITrainingStateService _trainingService;
    private readonly ILogger<TrainingBroadcaster> _logger;

    public TrainingBroadcaster(
        IHubContext<TrainingHub> hubContext,
        ITrainingStateService trainingService,
        ILogger<TrainingBroadcaster> logger)
    {
        _hubContext = hubContext;
        _trainingService = trainingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrainingBroadcaster started");

        // Subscribe to training service events
        _trainingService.OnGenerationComplete += BroadcastGenerationComplete;
        _trainingService.OnStatusChanged += BroadcastStatusChanged;
        _trainingService.OnStageAdvanced += BroadcastStageAdvanced;
        _trainingService.OnNewBestGenome += BroadcastNewBestGenome;

        try
        {
            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        finally
        {
            // Unsubscribe from events
            _trainingService.OnGenerationComplete -= BroadcastGenerationComplete;
            _trainingService.OnStatusChanged -= BroadcastStatusChanged;
            _trainingService.OnStageAdvanced -= BroadcastStageAdvanced;
            _trainingService.OnNewBestGenome -= BroadcastNewBestGenome;

            _logger.LogInformation("TrainingBroadcaster stopped");
        }
    }

    private void BroadcastGenerationComplete(DashboardGenerationStats stats)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients.Group("monitoring")
                    .SendAsync("GenerationComplete", stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting generation complete");
            }
        });
    }

    private void BroadcastStatusChanged(TrainingStatus status)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients.Group("monitoring")
                    .SendAsync("StatusChanged", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting status change");
            }
        });
    }

    private void BroadcastStageAdvanced(string newStage)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients.Group("monitoring")
                    .SendAsync("StageAdvanced", newStage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting stage advancement");
            }
        });
    }

    private void BroadcastNewBestGenome(GenomeSummary genome)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients.Group("monitoring")
                    .SendAsync("NewBestGenome", genome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting new best genome");
            }
        });
    }
}
