namespace TzarBot.Orchestrator.Service;

/// <summary>
/// Configuration for the Orchestrator service
/// </summary>
public class OrchestratorConfig
{
    public const string SectionName = "Orchestrator";

    /// <summary>
    /// Number of worker VMs to use (limited by 10GB RAM constraint)
    /// </summary>
    public int WorkerCount { get; set; } = 3;

    /// <summary>
    /// Maximum parallel evaluations
    /// </summary>
    public int MaxParallelEvaluations { get; set; } = 3;

    /// <summary>
    /// Timeout for acquiring a VM from the pool
    /// </summary>
    public TimeSpan VMPoolTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Default timeout for genome evaluation
    /// </summary>
    public TimeSpan DefaultEvaluationTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Interval for status reports
    /// </summary>
    public TimeSpan StatusReportInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum queue size for pending evaluations
    /// </summary>
    public int MaxPendingEvaluations { get; set; } = 100;

    /// <summary>
    /// Whether to auto-recover crashed workers
    /// </summary>
    public bool AutoRecoverWorkers { get; set; } = true;

    /// <summary>
    /// Maximum recovery attempts before giving up on a worker
    /// </summary>
    public int MaxRecoveryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between recovery attempts
    /// </summary>
    public TimeSpan RecoveryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Path for evaluation logs
    /// </summary>
    public string LogPath { get; set; } = @"C:\TzarBot\Logs\Orchestrator";

    /// <summary>
    /// Whether to persist evaluation results
    /// </summary>
    public bool PersistResults { get; set; } = true;

    /// <summary>
    /// Path for persisted results
    /// </summary>
    public string ResultsPath { get; set; } = @"C:\TzarBot\Results";
}
