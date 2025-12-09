using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Training.Core;

/// <summary>
/// Interface for the training pipeline.
/// Coordinates genetic algorithm, orchestrator, curriculum, and checkpointing.
/// </summary>
public interface ITrainingPipeline : IAsyncDisposable
{
    #region Properties

    /// <summary>
    /// Current training state.
    /// </summary>
    TrainingState State { get; }

    /// <summary>
    /// Training configuration.
    /// </summary>
    TrainingConfig Config { get; }

    /// <summary>
    /// Whether the pipeline is currently running.
    /// </summary>
    bool IsRunning { get; }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Initializes the training pipeline.
    /// Creates initial population if not loading from checkpoint.
    /// </summary>
    /// <param name="seed">Random seed for initialization (-1 for random).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(int seed = -1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes from a checkpoint.
    /// </summary>
    /// <param name="checkpointPath">Path to checkpoint file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeFromCheckpointAsync(
        string checkpointPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the training loop until completion, error, or cancellation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Final training state.</returns>
    Task<TrainingState> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a single generation of training.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics for this generation.</returns>
    Task<GenerationStats> RunGenerationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the training loop.
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Resumes paused training.
    /// </summary>
    Task ResumeAsync();

    /// <summary>
    /// Stops training and performs cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Checkpoint Methods

    /// <summary>
    /// Saves a checkpoint of the current state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to saved checkpoint.</returns>
    Task<string> SaveCheckpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the best genome to ONNX format.
    /// </summary>
    /// <param name="outputPath">Path for the ONNX file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportBestGenomeAsync(string outputPath, CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Raised when a generation completes.
    /// </summary>
    event EventHandler<GenerationCompletedEventArgs>? GenerationCompleted;

    /// <summary>
    /// Raised when a new best genome is found.
    /// </summary>
    event EventHandler<NewBestGenomeEventArgs>? NewBestGenomeFound;

    /// <summary>
    /// Raised when the curriculum stage changes.
    /// </summary>
    event EventHandler<StageChangedEventArgs>? StageChanged;

    /// <summary>
    /// Raised when training status changes.
    /// </summary>
    event EventHandler<TrainingStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Raised when an error occurs during training.
    /// </summary>
    event EventHandler<TrainingErrorEventArgs>? ErrorOccurred;

    #endregion
}

/// <summary>
/// Event args for generation completed event.
/// </summary>
public sealed class GenerationCompletedEventArgs : EventArgs
{
    public required GenerationStats Stats { get; init; }
    public required TrainingSummary Summary { get; init; }
}

/// <summary>
/// Event args for new best genome event.
/// </summary>
public sealed class NewBestGenomeEventArgs : EventArgs
{
    public required NetworkGenome Genome { get; init; }
    public required float Fitness { get; init; }
    public required int Generation { get; init; }
    public required float Improvement { get; init; }
}

/// <summary>
/// Event args for stage changed event.
/// </summary>
public sealed class StageChangedEventArgs : EventArgs
{
    public required string FromStage { get; init; }
    public required string ToStage { get; init; }
    public required int Generation { get; init; }
    public required string Reason { get; init; }
    public required bool IsPromotion { get; init; }
}

/// <summary>
/// Event args for training status changed event.
/// </summary>
public sealed class TrainingStatusChangedEventArgs : EventArgs
{
    public required TrainingStatus OldStatus { get; init; }
    public required TrainingStatus NewStatus { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event args for training error event.
/// </summary>
public sealed class TrainingErrorEventArgs : EventArgs
{
    public required Exception Exception { get; init; }
    public required string Context { get; init; }
    public required bool IsFatal { get; init; }
}
