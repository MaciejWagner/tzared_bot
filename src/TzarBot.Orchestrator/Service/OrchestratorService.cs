using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TzarBot.Orchestrator.Communication;
using TzarBot.Orchestrator.VM;
using TzarBot.Orchestrator.Worker;

namespace TzarBot.Orchestrator.Service;

/// <summary>
/// Background service that orchestrates genome evaluation across multiple VMs
/// </summary>
public class OrchestratorService : BackgroundService
{
    private readonly ILogger<OrchestratorService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly VMPool _vmPool;
    private readonly IVMCommunicator _communicator;
    private readonly GenomeTransfer _genomeTransfer;
    private readonly OrchestratorConfig _config;
    private readonly CommunicationOptions _commOptions;

    private readonly ConcurrentDictionary<string, WorkerAgent> _workers = new();
    private readonly Channel<EvaluationRequest> _requestChannel;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<EvaluationResult>> _pendingRequests = new();

    public OrchestratorService(
        ILogger<OrchestratorService> logger,
        ILoggerFactory loggerFactory,
        VMPool vmPool,
        IVMCommunicator communicator,
        GenomeTransfer genomeTransfer,
        IOptions<OrchestratorConfig> config,
        IOptions<CommunicationOptions> commOptions)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _vmPool = vmPool;
        _communicator = communicator;
        _genomeTransfer = genomeTransfer;
        _config = config.Value;
        _commOptions = commOptions.Value;

        _requestChannel = Channel.CreateBounded<EvaluationRequest>(
            new BoundedChannelOptions(_config.MaxPendingEvaluations)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    /// <summary>
    /// Submits a genome for evaluation and returns the result asynchronously
    /// </summary>
    public async Task<EvaluationResult> EvaluateGenomeAsync(
        string genomeId,
        byte[] genomeData,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<EvaluationResult>();

        _pendingRequests[requestId] = tcs;

        var request = new EvaluationRequest
        {
            RequestId = requestId,
            GenomeId = genomeId,
            GenomeData = genomeData,
            Timeout = timeout ?? _config.DefaultEvaluationTimeout
        };

        await _requestChannel.Writer.WriteAsync(request, cancellationToken);

        _logger.LogInformation("Genome {GenomeId} queued for evaluation (RequestId: {RequestId})",
            genomeId, requestId);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(request.Timeout + TimeSpan.FromMinutes(1)); // Extra time for queue wait

            return await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _pendingRequests.TryRemove(requestId, out _);
            return new EvaluationResult
            {
                EvaluationId = requestId,
                VMName = "N/A",
                GenomeId = genomeId,
                Success = false,
                ErrorMessage = "Evaluation timed out or cancelled",
                Outcome = GameOutcome.Timeout
            };
        }
    }

    /// <summary>
    /// Submits multiple genomes for parallel evaluation
    /// </summary>
    public async Task<IReadOnlyList<EvaluationResult>> EvaluateGenomesAsync(
        IEnumerable<(string GenomeId, byte[] GenomeData)> genomes,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = genomes.Select(g => EvaluateGenomeAsync(g.GenomeId, g.GenomeData, timeout, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Gets current status of the orchestrator
    /// </summary>
    public OrchestratorStatus GetStatus()
    {
        return new OrchestratorStatus
        {
            TotalWorkers = _workers.Count,
            ActiveWorkers = _workers.Values.Count(w => w.State == WorkerState.Evaluating),
            IdleWorkers = _workers.Values.Count(w => w.State == WorkerState.Idle),
            ErrorWorkers = _workers.Values.Count(w => w.State == WorkerState.Error),
            PendingRequests = _pendingRequests.Count,
            TotalCompletedEvaluations = _workers.Values.Sum(w => w.CompletedEvaluations),
            TotalFailedEvaluations = _workers.Values.Sum(w => w.FailedEvaluations),
            Workers = _workers.Values.Select(w => new WorkerStatus
            {
                VMName = w.VMName,
                State = w.State,
                CompletedEvaluations = w.CompletedEvaluations,
                FailedEvaluations = w.FailedEvaluations
            }).ToList()
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Orchestrator service starting...");

        try
        {
            // Initialize VM pool
            await _vmPool.InitializeAsync(stoppingToken);
            _logger.LogInformation("VM pool initialized with {Count} VMs", _vmPool.TotalCount);

            // Create workers
            await InitializeWorkersAsync(stoppingToken);

            // Start request processing and status reporting
            var requestProcessing = ProcessRequestsAsync(stoppingToken);
            var statusReporting = ReportStatusAsync(stoppingToken);

            // Wait for cancellation
            await Task.WhenAll(requestProcessing, statusReporting);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Orchestrator service stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator service error");
            throw;
        }
        finally
        {
            await ShutdownWorkersAsync();
            _logger.LogInformation("Orchestrator service stopped");
        }
    }

    private async Task InitializeWorkersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing {Count} workers...", _config.WorkerCount);

        for (int i = 0; i < _config.WorkerCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lease = await _vmPool.AcquireAsync(_config.VMPoolTimeout, cancellationToken);
            if (lease == null)
            {
                _logger.LogWarning("Could not acquire VM for worker {Index}", i);
                continue;
            }

            var worker = new WorkerAgent(
                _loggerFactory.CreateLogger<WorkerAgent>(),
                lease,
                _communicator,
                _genomeTransfer,
                _commOptions);

            _workers[lease.VMName] = worker;
            worker.Start();

            _logger.LogInformation("Worker {Index} initialized on VM {VMName}", i, lease.VMName);
        }

        _logger.LogInformation("Initialized {Count} workers", _workers.Count);
    }

    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting request processor");

        await foreach (var request in _requestChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                // Find an idle worker
                var worker = _workers.Values.FirstOrDefault(w => w.State == WorkerState.Idle);
                if (worker == null)
                {
                    // All workers busy, queue will naturally back-pressure
                    _logger.LogDebug("All workers busy, waiting for available worker");

                    // Simple round-robin to the least loaded worker
                    worker = _workers.Values.OrderBy(w => w.CompletedEvaluations).First();
                }

                var task = new EvaluationTask
                {
                    GenomeId = request.GenomeId,
                    GenomeData = request.GenomeData,
                    EvaluationTimeout = request.Timeout,
                    ResultCallback = result =>
                    {
                        if (_pendingRequests.TryRemove(request.RequestId, out var tcs))
                        {
                            tcs.SetResult(result);
                        }
                    }
                };

                await worker.QueueEvaluationAsync(task, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId}", request.RequestId);

                if (_pendingRequests.TryRemove(request.RequestId, out var tcs))
                {
                    tcs.SetResult(new EvaluationResult
                    {
                        EvaluationId = request.RequestId,
                        VMName = "N/A",
                        GenomeId = request.GenomeId,
                        Success = false,
                        ErrorMessage = ex.Message,
                        Outcome = GameOutcome.Error
                    });
                }
            }
        }
    }

    private async Task ReportStatusAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_config.StatusReportInterval, cancellationToken);

            var status = GetStatus();
            _logger.LogInformation(
                "Orchestrator Status - Workers: {Active}/{Total} active, Pending: {Pending}, Completed: {Completed}, Failed: {Failed}",
                status.ActiveWorkers,
                status.TotalWorkers,
                status.PendingRequests,
                status.TotalCompletedEvaluations,
                status.TotalFailedEvaluations);
        }
    }

    private async Task ShutdownWorkersAsync()
    {
        _logger.LogInformation("Shutting down {Count} workers...", _workers.Count);

        var shutdownTasks = _workers.Values.Select(async worker =>
        {
            try
            {
                await worker.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error shutting down worker {VMName}", worker.VMName);
            }
        });

        await Task.WhenAll(shutdownTasks);
        _workers.Clear();

        _logger.LogInformation("All workers shut down");
    }
}

/// <summary>
/// Internal request for genome evaluation
/// </summary>
internal class EvaluationRequest
{
    public Guid RequestId { get; init; }
    public required string GenomeId { get; init; }
    public required byte[] GenomeData { get; init; }
    public TimeSpan Timeout { get; init; }
}

/// <summary>
/// Status of the orchestrator
/// </summary>
public class OrchestratorStatus
{
    public int TotalWorkers { get; init; }
    public int ActiveWorkers { get; init; }
    public int IdleWorkers { get; init; }
    public int ErrorWorkers { get; init; }
    public int PendingRequests { get; init; }
    public int TotalCompletedEvaluations { get; init; }
    public int TotalFailedEvaluations { get; init; }
    public IReadOnlyList<WorkerStatus> Workers { get; init; } = Array.Empty<WorkerStatus>();
}

/// <summary>
/// Status of a single worker
/// </summary>
public class WorkerStatus
{
    public required string VMName { get; init; }
    public WorkerState State { get; init; }
    public int CompletedEvaluations { get; init; }
    public int FailedEvaluations { get; init; }
}
