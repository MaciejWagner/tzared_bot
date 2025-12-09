namespace TzarBot.Orchestrator.VM;

/// <summary>
/// Represents the state of a Hyper-V virtual machine
/// </summary>
public enum VMState
{
    Unknown = 0,
    Off = 2,
    Running = 3,
    Saved = 6,
    Paused = 9,
    Starting = 10,
    Saving = 15,
    Stopping = 16,
    Resuming = 17,
    Critical = 18,
    Pausing = 19,

    // Custom states for tracking
    Acquired = 100,  // VM is acquired by pool and being used
    Error = 999
}

/// <summary>
/// Represents the health status of a VM
/// </summary>
public enum VMHealthStatus
{
    Unknown,
    Healthy,
    NoHeartbeat,
    ApplicationsUnhealthy,
    Error
}

/// <summary>
/// Represents information about a single VM
/// </summary>
public record VMInfo
{
    public required string Name { get; init; }
    public VMState State { get; init; }
    public VMHealthStatus HealthStatus { get; init; }
    public long MemoryAssignedBytes { get; init; }
    public int ProcessorCount { get; init; }
    public TimeSpan Uptime { get; init; }
    public string? IpAddress { get; init; }
    public string? VhdPath { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Memory assigned in GB
    /// </summary>
    public double MemoryAssignedGB => Math.Round(MemoryAssignedBytes / (1024.0 * 1024.0 * 1024.0), 2);

    /// <summary>
    /// Indicates if VM is ready for work
    /// </summary>
    public bool IsReady => State == VMState.Running && HealthStatus == VMHealthStatus.Healthy;
}
