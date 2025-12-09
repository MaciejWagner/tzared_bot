using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TzarBot.Orchestrator.VM;

/// <summary>
/// Manages a pool of worker VMs for parallel genome evaluation
/// </summary>
public class VMPool : IDisposable
{
    private readonly ILogger<VMPool> _logger;
    private readonly IVMManager _vmManager;
    private readonly VMPoolOptions _options;

    private readonly ConcurrentDictionary<string, VMPoolEntry> _pool = new();
    private readonly ConcurrentQueue<string> _availableVMs = new();
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly CancellationTokenSource _backgroundCts = new();

    private bool _initialized;
    private bool _disposed;

    public VMPool(ILogger<VMPool> logger, IVMManager vmManager, IOptions<VMPoolOptions> options)
    {
        _logger = logger;
        _vmManager = vmManager;
        _options = options.Value;
        _poolSemaphore = new SemaphoreSlim(0, _options.MaxPoolSize);
    }

    /// <summary>
    /// Initializes the VM pool by discovering and starting worker VMs
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            _logger.LogWarning("VM Pool already initialized");
            return;
        }

        _logger.LogInformation("Initializing VM pool (MaxSize: {MaxSize})", _options.MaxPoolSize);

        var workers = await _vmManager.GetAllWorkersAsync(cancellationToken);
        _logger.LogInformation("Found {Count} existing worker VMs", workers.Count);

        foreach (var worker in workers.Take(_options.MaxPoolSize))
        {
            var entry = new VMPoolEntry
            {
                VMName = worker.Name,
                State = worker.State == VMState.Running ? VMPoolState.Available : VMPoolState.Offline
            };

            _pool[worker.Name] = entry;

            if (_options.AutoStartVMs && worker.State != VMState.Running)
            {
                _logger.LogInformation("Starting VM: {VMName}", worker.Name);
                await _vmManager.StartVMAsync(worker.Name, cancellationToken);
            }

            if (worker.IsReady)
            {
                _availableVMs.Enqueue(worker.Name);
                _poolSemaphore.Release();
            }
        }

        // Start background health monitor
        _ = MonitorPoolHealthAsync(_backgroundCts.Token);

        _initialized = true;
        _logger.LogInformation("VM pool initialized with {Count} VMs ({Available} available)",
            _pool.Count, _availableVMs.Count);
    }

    /// <summary>
    /// Acquires an available VM from the pool
    /// </summary>
    public async Task<VMPoolLease?> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            throw new InvalidOperationException("VM Pool not initialized. Call InitializeAsync first.");

        var effectiveTimeout = timeout ?? _options.DefaultAcquireTimeout;
        _logger.LogDebug("Attempting to acquire VM (timeout: {Timeout}s)", effectiveTimeout.TotalSeconds);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        try
        {
            // Wait for an available VM
            await _poolSemaphore.WaitAsync(cts.Token);

            // Get VM from queue
            if (_availableVMs.TryDequeue(out var vmName))
            {
                if (_pool.TryGetValue(vmName, out var entry))
                {
                    entry.State = VMPoolState.Acquired;
                    entry.AcquiredAt = DateTime.UtcNow;

                    _logger.LogInformation("VM acquired: {VMName}", vmName);

                    return new VMPoolLease(vmName, this, _vmManager);
                }
            }

            // If we got semaphore but no VM available, something went wrong
            _logger.LogWarning("Semaphore released but no VM available");
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("VM acquisition timed out after {Timeout}s", effectiveTimeout.TotalSeconds);
            return null;
        }
    }

    /// <summary>
    /// Releases a VM back to the pool
    /// </summary>
    internal async Task ReleaseAsync(string vmName, bool needsReset = false)
    {
        _logger.LogInformation("Releasing VM: {VMName} (needsReset: {NeedsReset})", vmName, needsReset);

        if (!_pool.TryGetValue(vmName, out var entry))
        {
            _logger.LogWarning("VM {VMName} not found in pool", vmName);
            return;
        }

        if (needsReset && _options.ResetOnRelease)
        {
            entry.State = VMPoolState.Resetting;
            _logger.LogInformation("Resetting VM: {VMName}", vmName);

            var resetSuccess = await _vmManager.ResetToCheckpointAsync(vmName, _options.CleanCheckpointName);
            if (resetSuccess)
            {
                await _vmManager.StartVMAsync(vmName);
                var healthyAfterReset = await _vmManager.WaitForHealthyAsync(vmName, _options.BootTimeout);

                if (!healthyAfterReset)
                {
                    _logger.LogWarning("VM {VMName} not healthy after reset", vmName);
                    entry.State = VMPoolState.Error;
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Failed to reset VM {VMName}", vmName);
                entry.State = VMPoolState.Error;
                return;
            }
        }

        entry.State = VMPoolState.Available;
        entry.AcquiredAt = null;
        entry.LastReleasedAt = DateTime.UtcNow;

        _availableVMs.Enqueue(vmName);
        _poolSemaphore.Release();

        _logger.LogInformation("VM released to pool: {VMName}", vmName);
    }

    /// <summary>
    /// Gets the current status of all VMs in the pool
    /// </summary>
    public IReadOnlyDictionary<string, VMPoolEntry> GetPoolStatus()
    {
        return _pool;
    }

    /// <summary>
    /// Gets count of available VMs
    /// </summary>
    public int AvailableCount => _availableVMs.Count;

    /// <summary>
    /// Gets total VM count in pool
    /// </summary>
    public int TotalCount => _pool.Count;

    private async Task MonitorPoolHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting pool health monitor");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.HealthCheckInterval, cancellationToken);

                foreach (var (vmName, entry) in _pool)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var vmInfo = await _vmManager.GetVMAsync(vmName, cancellationToken);
                    if (vmInfo == null)
                    {
                        _logger.LogWarning("VM {VMName} not found during health check", vmName);
                        entry.State = VMPoolState.Error;
                        continue;
                    }

                    // Check for stuck acquired VMs
                    if (entry.State == VMPoolState.Acquired && entry.AcquiredAt.HasValue)
                    {
                        var acquiredDuration = DateTime.UtcNow - entry.AcquiredAt.Value;
                        if (acquiredDuration > _options.MaxAcquiredDuration)
                        {
                            _logger.LogWarning("VM {VMName} has been acquired for {Duration}m, releasing",
                                vmName, acquiredDuration.TotalMinutes);

                            // Force release
                            await ReleaseAsync(vmName, needsReset: true);
                        }
                    }

                    // Check for unhealthy VMs
                    if (entry.State == VMPoolState.Available && !vmInfo.IsReady)
                    {
                        _logger.LogWarning("Available VM {VMName} is not ready (State: {State}, Health: {Health})",
                            vmName, vmInfo.State, vmInfo.HealthStatus);

                        // Remove from available queue and try to recover
                        entry.State = VMPoolState.Recovering;

                        // Attempt restart
                        await _vmManager.RestartVMAsync(vmName, cancellationToken);
                        var recovered = await _vmManager.WaitForHealthyAsync(vmName, _options.BootTimeout, cancellationToken);

                        if (recovered)
                        {
                            entry.State = VMPoolState.Available;
                            _logger.LogInformation("VM {VMName} recovered", vmName);
                        }
                        else
                        {
                            entry.State = VMPoolState.Error;
                            _logger.LogError("VM {VMName} could not be recovered", vmName);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during pool health check");
            }
        }

        _logger.LogInformation("Pool health monitor stopped");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _backgroundCts.Cancel();
        _backgroundCts.Dispose();
        _poolSemaphore.Dispose();
    }
}

/// <summary>
/// State of a VM in the pool
/// </summary>
public enum VMPoolState
{
    Available,
    Acquired,
    Offline,
    Resetting,
    Recovering,
    Error
}

/// <summary>
/// Entry for a VM in the pool
/// </summary>
public class VMPoolEntry
{
    public required string VMName { get; init; }
    public VMPoolState State { get; set; }
    public DateTime? AcquiredAt { get; set; }
    public DateTime? LastReleasedAt { get; set; }
    public int TotalAcquisitions { get; set; }
    public int TotalErrors { get; set; }
}

/// <summary>
/// Represents a lease on a VM from the pool
/// </summary>
public class VMPoolLease : IAsyncDisposable
{
    private readonly VMPool _pool;
    private readonly IVMManager _vmManager;
    private bool _disposed;
    private bool _needsReset;

    public string VMName { get; }

    internal VMPoolLease(string vmName, VMPool pool, IVMManager vmManager)
    {
        VMName = vmName;
        _pool = pool;
        _vmManager = vmManager;
    }

    /// <summary>
    /// Sends a file to the VM
    /// </summary>
    public Task<bool> SendFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _vmManager.SendFileToVMAsync(VMName, localPath, remotePath, cancellationToken);
    }

    /// <summary>
    /// Executes a command on the VM
    /// </summary>
    public Task<(bool Success, string Output, string Error)> ExecuteAsync(
        string command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _vmManager.ExecuteOnVMAsync(VMName, command, timeout, cancellationToken);
    }

    /// <summary>
    /// Marks the VM as needing reset when released
    /// </summary>
    public void MarkForReset()
    {
        _needsReset = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VMPoolLease));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _pool.ReleaseAsync(VMName, _needsReset);
    }
}

/// <summary>
/// Configuration options for VMPool
/// </summary>
public class VMPoolOptions
{
    public const string SectionName = "VMPool";

    /// <summary>
    /// Maximum number of VMs in the pool (considering 10GB RAM limit)
    /// </summary>
    public int MaxPoolSize { get; set; } = 3;

    /// <summary>
    /// Whether to automatically start VMs on pool initialization
    /// </summary>
    public bool AutoStartVMs { get; set; } = true;

    /// <summary>
    /// Whether to reset VMs to checkpoint when released
    /// </summary>
    public bool ResetOnRelease { get; set; } = true;

    /// <summary>
    /// Name of the clean checkpoint to reset to
    /// </summary>
    public string CleanCheckpointName { get; set; } = "Clean";

    /// <summary>
    /// Default timeout for acquiring a VM
    /// </summary>
    public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum time a VM can be acquired before forced release
    /// </summary>
    public TimeSpan MaxAcquiredDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Timeout for waiting for VM to boot
    /// </summary>
    public TimeSpan BootTimeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Interval for health checks
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}
