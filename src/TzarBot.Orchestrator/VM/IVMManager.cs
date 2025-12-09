namespace TzarBot.Orchestrator.VM;

/// <summary>
/// Interface for managing Hyper-V virtual machines
/// </summary>
public interface IVMManager : IDisposable
{
    /// <summary>
    /// Gets information about all worker VMs
    /// </summary>
    Task<IReadOnlyList<VMInfo>> GetAllWorkersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific VM
    /// </summary>
    Task<VMInfo?> GetVMAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a VM
    /// </summary>
    Task<bool> StartVMAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a VM gracefully
    /// </summary>
    Task<bool> StopVMAsync(string vmName, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts a VM
    /// </summary>
    Task<bool> RestartVMAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for a VM to become healthy
    /// </summary>
    Task<bool> WaitForHealthyAsync(string vmName, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new worker VM from template
    /// </summary>
    Task<bool> CreateWorkerVMAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a worker VM
    /// </summary>
    Task<bool> RemoveWorkerVMAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a VM to clean checkpoint
    /// </summary>
    Task<bool> ResetToCheckpointAsync(string vmName, string checkpointName = "Clean", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the IP address of a running VM
    /// </summary>
    Task<string?> GetVMIPAddressAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a file to a VM using PowerShell Direct
    /// </summary>
    Task<bool> SendFileToVMAsync(string vmName, string localPath, string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command on a VM using PowerShell Direct
    /// </summary>
    Task<(bool Success, string Output, string Error)> ExecuteOnVMAsync(
        string vmName,
        string command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
