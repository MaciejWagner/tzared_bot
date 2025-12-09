using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Core;

namespace TzarBot.Training.Checkpoint;

/// <summary>
/// Interface for managing training checkpoints.
/// Handles save, load, and cleanup of checkpoint files.
/// </summary>
public interface ICheckpointManager
{
    /// <summary>
    /// Directory where checkpoints are stored.
    /// </summary>
    string CheckpointDirectory { get; }

    /// <summary>
    /// Maximum number of checkpoints to keep.
    /// </summary>
    int MaxCheckpoints { get; }

    /// <summary>
    /// Saves a training checkpoint.
    /// </summary>
    /// <param name="state">Current training state.</param>
    /// <param name="config">Training configuration.</param>
    /// <param name="seed">Random seed used.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to saved checkpoint.</returns>
    Task<string> SaveCheckpointAsync(
        TrainingState state,
        TrainingConfig config,
        int seed,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the latest checkpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Checkpoint data or null if none exists.</returns>
    Task<TrainingCheckpoint?> LoadLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a checkpoint by file path.
    /// </summary>
    /// <param name="filePath">Path to checkpoint file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Checkpoint data.</returns>
    Task<TrainingCheckpoint> LoadAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a checkpoint by generation number.
    /// </summary>
    /// <param name="generation">Generation number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Checkpoint data or null if not found.</returns>
    Task<TrainingCheckpoint?> LoadByGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the best genome separately.
    /// </summary>
    /// <param name="genome">Best genome.</param>
    /// <param name="generation">Generation when found.</param>
    /// <param name="stageName">Current stage name.</param>
    /// <param name="eloRating">Optional ELO rating.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to saved file.</returns>
    Task<string> SaveBestGenomeAsync(
        NetworkGenome genome,
        int generation,
        string? stageName = null,
        int? eloRating = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the best genome.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Best genome checkpoint or null.</returns>
    Task<BestGenomeCheckpoint?> LoadBestGenomeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available checkpoints.
    /// </summary>
    /// <returns>List of checkpoint info, sorted by generation descending.</returns>
    IReadOnlyList<CheckpointInfo> ListCheckpoints();

    /// <summary>
    /// Deletes old checkpoints, keeping only the most recent N.
    /// </summary>
    /// <param name="keepCount">Number of checkpoints to keep (null = use MaxCheckpoints).</param>
    /// <returns>Number of checkpoints deleted.</returns>
    int PruneOldCheckpoints(int? keepCount = null);

    /// <summary>
    /// Deletes a specific checkpoint.
    /// </summary>
    /// <param name="filePath">Path to checkpoint file.</param>
    /// <returns>True if deleted successfully.</returns>
    bool DeleteCheckpoint(string filePath);

    /// <summary>
    /// Verifies checkpoint integrity.
    /// </summary>
    /// <param name="filePath">Path to checkpoint file.</param>
    /// <returns>True if checkpoint is valid.</returns>
    Task<bool> VerifyCheckpointAsync(string filePath, CancellationToken cancellationToken = default);
}
