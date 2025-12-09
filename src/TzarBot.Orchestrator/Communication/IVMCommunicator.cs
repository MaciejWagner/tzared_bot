namespace TzarBot.Orchestrator.Communication;

/// <summary>
/// Interface for communication with worker VMs
/// </summary>
public interface IVMCommunicator
{
    /// <summary>
    /// Sends a genome to the VM for evaluation
    /// </summary>
    Task<bool> SendGenomeAsync(string vmName, byte[] genomeData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives evaluation result from the VM
    /// </summary>
    Task<EvaluationResult?> ReceiveResultAsync(string vmName, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends heartbeat to check if VM is responsive
    /// </summary>
    Task<bool> HeartbeatAsync(string vmName, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the bot service on the VM
    /// </summary>
    Task<bool> StartBotServiceAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the bot service on the VM
    /// </summary>
    Task<bool> StopBotServiceAsync(string vmName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the bot service
    /// </summary>
    Task<BotServiceStatus> GetBotStatusAsync(string vmName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Status of the bot service on a VM
/// </summary>
public enum BotServiceStatus
{
    Unknown,
    Stopped,
    Starting,
    Running,
    EvaluatingGenome,
    Error
}

/// <summary>
/// Result of genome evaluation on a VM
/// </summary>
public record EvaluationResult
{
    /// <summary>
    /// Unique identifier for this evaluation
    /// </summary>
    public Guid EvaluationId { get; init; }

    /// <summary>
    /// Name of the VM that performed the evaluation
    /// </summary>
    public required string VMName { get; init; }

    /// <summary>
    /// Identifier of the genome that was evaluated
    /// </summary>
    public required string GenomeId { get; init; }

    /// <summary>
    /// Fitness score of the genome (higher is better)
    /// </summary>
    public double FitnessScore { get; init; }

    /// <summary>
    /// Whether the evaluation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if evaluation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Time taken for evaluation
    /// </summary>
    public TimeSpan EvaluationDuration { get; init; }

    /// <summary>
    /// Game outcome (win/loss/draw)
    /// </summary>
    public GameOutcome Outcome { get; init; }

    /// <summary>
    /// Additional metrics from the game
    /// </summary>
    public GameMetrics? Metrics { get; init; }

    /// <summary>
    /// Timestamp when evaluation completed
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Outcome of a game
/// </summary>
public enum GameOutcome
{
    Unknown,
    Win,
    Loss,
    Draw,
    Timeout,
    Error
}

/// <summary>
/// Additional metrics collected during game
/// </summary>
public record GameMetrics
{
    /// <summary>
    /// Duration of the game in seconds
    /// </summary>
    public int GameDurationSeconds { get; init; }

    /// <summary>
    /// Number of units created
    /// </summary>
    public int UnitsCreated { get; init; }

    /// <summary>
    /// Number of units lost
    /// </summary>
    public int UnitsLost { get; init; }

    /// <summary>
    /// Number of enemy units destroyed
    /// </summary>
    public int EnemyUnitsDestroyed { get; init; }

    /// <summary>
    /// Total resources gathered
    /// </summary>
    public int ResourcesGathered { get; init; }

    /// <summary>
    /// Number of buildings constructed
    /// </summary>
    public int BuildingsConstructed { get; init; }

    /// <summary>
    /// Average actions per minute
    /// </summary>
    public double ActionsPerMinute { get; init; }
}
