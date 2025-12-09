using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TzarBot.Orchestrator.Communication;
using TzarBot.Orchestrator.VM;

namespace TzarBot.Orchestrator.Worker;

/// <summary>
/// Represents a worker agent that evaluates genomes on a single VM
/// </summary>
public class WorkerAgent : IAsyncDisposable
{
    private readonly ILogger<WorkerAgent> _logger;
    private readonly VMPoolLease _vmLease;
    private readonly IVMCommunicator _communicator;
    private readonly GenomeTransfer _genomeTransfer;
    private readonly CommunicationOptions _options;

    private readonly Channel<EvaluationTask> _taskQueue;
    private readonly CancellationTokenSource _workerCts = new();
    private Task? _workerLoop;
    private bool _disposed;

    public string VMName => _vmLease.VMName;
    public WorkerState State { get; private set; } = WorkerState.Idle;
    public int CompletedEvaluations { get; private set; }
    public int FailedEvaluations { get; private set; }

    public WorkerAgent(
        ILogger<WorkerAgent> logger,
        VMPoolLease vmLease,
        IVMCommunicator communicator,
        GenomeTransfer genomeTransfer,
        CommunicationOptions options)
    {
        _logger = logger;
        _vmLease = vmLease;
        _communicator = communicator;
        _genomeTransfer = genomeTransfer;
        _options = options;

        _taskQueue = Channel.CreateBounded<EvaluationTask>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>
    /// Starts the worker loop
    /// </summary>
    public void Start()
    {
        if (_workerLoop != null)
            throw new InvalidOperationException("Worker already started");

        _logger.LogInformation("Starting worker agent for VM {VMName}", VMName);
        State = WorkerState.Idle;
        _workerLoop = RunWorkerLoopAsync(_workerCts.Token);
    }

    /// <summary>
    /// Queues a genome for evaluation
    /// </summary>
    public async Task<bool> QueueEvaluationAsync(EvaluationTask task, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WorkerAgent));

        try
        {
            await _taskQueue.Writer.WriteAsync(task, cancellationToken);
            _logger.LogDebug("Queued evaluation task {GenomeId} for VM {VMName}", task.GenomeId, VMName);
            return true;
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Cannot queue task - worker is stopped");
            return false;
        }
    }

    /// <summary>
    /// Stops the worker agent
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping worker agent for VM {VMName}", VMName);

        _taskQueue.Writer.Complete();
        _workerCts.Cancel();

        if (_workerLoop != null)
        {
            try
            {
                await _workerLoop;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        State = WorkerState.Stopped;
        _logger.LogInformation("Worker agent for VM {VMName} stopped. Completed: {Completed}, Failed: {Failed}",
            VMName, CompletedEvaluations, FailedEvaluations);
    }

    private async Task RunWorkerLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker loop started for VM {VMName}", VMName);

        try
        {
            // Initialize bot service
            State = WorkerState.Initializing;
            var botStarted = await _communicator.StartBotServiceAsync(VMName, cancellationToken);
            if (!botStarted)
            {
                _logger.LogError("Failed to start bot service on VM {VMName}", VMName);
                State = WorkerState.Error;
                return;
            }

            State = WorkerState.Idle;

            await foreach (var task in _taskQueue.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessTaskAsync(task, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker loop cancelled for VM {VMName}", VMName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in worker loop for VM {VMName}", VMName);
            State = WorkerState.Error;
        }
        finally
        {
            // Cleanup
            try
            {
                await _communicator.StopBotServiceAsync(VMName, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping bot service on VM {VMName}", VMName);
            }
        }
    }

    private async Task ProcessTaskAsync(EvaluationTask task, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Processing evaluation task {GenomeId} on VM {VMName}", task.GenomeId, VMName);

        State = WorkerState.Evaluating;

        try
        {
            // Transfer genome
            var remotePath = Path.Combine(_options.RemoteGenomePath, $"{task.GenomeId}.bin");
            var transferResult = await _genomeTransfer.TransferGenomeAsync(
                VMName, task.GenomeId, task.GenomeData, remotePath, cancellationToken);

            if (!transferResult.Success)
            {
                FailedEvaluations++;
                task.ResultCallback?.Invoke(CreateErrorResult(task, transferResult.ErrorMessage ?? "Transfer failed"));
                return;
            }

            // Signal bot to start evaluation
            var sendSuccess = await _communicator.SendGenomeAsync(VMName, task.GenomeData, cancellationToken);
            if (!sendSuccess)
            {
                FailedEvaluations++;
                task.ResultCallback?.Invoke(CreateErrorResult(task, "Failed to signal genome load"));
                return;
            }

            // Wait for result
            var result = await _communicator.ReceiveResultAsync(VMName, task.EvaluationTimeout, cancellationToken);

            if (result == null)
            {
                FailedEvaluations++;
                task.ResultCallback?.Invoke(CreateErrorResult(task, "Evaluation timed out"));
                return;
            }

            stopwatch.Stop();
            CompletedEvaluations++;

            _logger.LogInformation("Evaluation completed for {GenomeId} on VM {VMName}: Score={Score}, Duration={Duration}ms",
                task.GenomeId, VMName, result.FitnessScore, stopwatch.ElapsedMilliseconds);

            task.ResultCallback?.Invoke(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Evaluation cancelled for {GenomeId} on VM {VMName}", task.GenomeId, VMName);
            task.ResultCallback?.Invoke(CreateErrorResult(task, "Evaluation cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating {GenomeId} on VM {VMName}", task.GenomeId, VMName);
            FailedEvaluations++;
            task.ResultCallback?.Invoke(CreateErrorResult(task, ex.Message));
        }
        finally
        {
            State = WorkerState.Idle;

            // Cleanup
            await _genomeTransfer.CleanupGenomesAsync(VMName, _options.RemoteGenomePath, CancellationToken.None);
        }
    }

    private EvaluationResult CreateErrorResult(EvaluationTask task, string errorMessage)
    {
        return new EvaluationResult
        {
            EvaluationId = Guid.NewGuid(),
            VMName = VMName,
            GenomeId = task.GenomeId,
            Success = false,
            ErrorMessage = errorMessage,
            Outcome = GameOutcome.Error
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopAsync();
        _workerCts.Dispose();
        await _vmLease.DisposeAsync();
    }
}

/// <summary>
/// State of a worker agent
/// </summary>
public enum WorkerState
{
    Idle,
    Initializing,
    Evaluating,
    Stopped,
    Error
}

/// <summary>
/// Represents a task to evaluate a genome
/// </summary>
public class EvaluationTask
{
    /// <summary>
    /// Unique identifier for the genome
    /// </summary>
    public required string GenomeId { get; init; }

    /// <summary>
    /// Serialized genome data
    /// </summary>
    public required byte[] GenomeData { get; init; }

    /// <summary>
    /// Timeout for the evaluation
    /// </summary>
    public TimeSpan EvaluationTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Priority (higher = more important)
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Callback when result is ready
    /// </summary>
    public Action<EvaluationResult>? ResultCallback { get; init; }

    /// <summary>
    /// Timestamp when task was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
