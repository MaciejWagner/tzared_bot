using System.Security.Cryptography;
using MessagePack;
using Microsoft.Extensions.Logging;
using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Core;

namespace TzarBot.Training.Checkpoint;

/// <summary>
/// Manages training checkpoints with auto-cleanup and integrity verification.
///
/// Features:
/// - Saves full training state with MessagePack + LZ4 compression
/// - Maintains "latest" symlink for quick access
/// - Auto-prunes old checkpoints (keeps last N)
/// - Saves best genome separately
/// - Checksum verification for data integrity
/// </summary>
public sealed class CheckpointManager : ICheckpointManager
{
    private readonly ILogger<CheckpointManager>? _logger;
    private readonly string _checkpointDirectory;
    private readonly string _bestGenomeDirectory;
    private readonly int _maxCheckpoints;
    private readonly MessagePackSerializerOptions _serializerOptions;

    /// <inheritdoc />
    public string CheckpointDirectory => _checkpointDirectory;

    /// <inheritdoc />
    public int MaxCheckpoints => _maxCheckpoints;

    /// <summary>
    /// Creates a checkpoint manager.
    /// </summary>
    public CheckpointManager(
        string checkpointDirectory,
        string? bestGenomeDirectory = null,
        int maxCheckpoints = 10,
        ILogger<CheckpointManager>? logger = null)
    {
        _checkpointDirectory = checkpointDirectory;
        _bestGenomeDirectory = bestGenomeDirectory ?? Path.Combine(checkpointDirectory, "best");
        _maxCheckpoints = maxCheckpoints;
        _logger = logger;

        // Use LZ4 compression for efficient storage
        _serializerOptions = MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        // Ensure directories exist
        Directory.CreateDirectory(_checkpointDirectory);
        Directory.CreateDirectory(_bestGenomeDirectory);

        _logger?.LogInformation("CheckpointManager initialized: {Dir}, MaxCheckpoints={Max}",
            _checkpointDirectory, _maxCheckpoints);
    }

    /// <inheritdoc />
    public async Task<string> SaveCheckpointAsync(
        TrainingState state,
        TrainingConfig config,
        int seed,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = TrainingCheckpoint.Create(state, config, seed, metadata);

        // Calculate checksum before serialization
        var data = MessagePackSerializer.Serialize(checkpoint, _serializerOptions);
        checkpoint.Checksum = ComputeChecksum(data);

        // Re-serialize with checksum
        data = MessagePackSerializer.Serialize(checkpoint, _serializerOptions);

        // Generate filename
        var fileName = CheckpointInfo.GenerateFilename(state.CurrentGeneration);
        var filePath = Path.Combine(_checkpointDirectory, fileName);

        // Save checkpoint
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);

        // Update "latest" link
        var latestPath = Path.Combine(_checkpointDirectory, "latest.bin");
        await File.WriteAllBytesAsync(latestPath, data, cancellationToken);

        _logger?.LogInformation("Checkpoint saved: {Path} ({Size}KB)",
            filePath, data.Length / 1024);

        // Prune old checkpoints
        PruneOldCheckpoints();

        return filePath;
    }

    /// <inheritdoc />
    public async Task<TrainingCheckpoint?> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestPath = Path.Combine(_checkpointDirectory, "latest.bin");

        if (!File.Exists(latestPath))
        {
            // Try to find the most recent checkpoint
            var checkpoints = ListCheckpoints();
            if (checkpoints.Count == 0)
            {
                _logger?.LogWarning("No checkpoints found in {Dir}", _checkpointDirectory);
                return null;
            }

            latestPath = checkpoints[0].FilePath;
        }

        return await LoadAsync(latestPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingCheckpoint> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Checkpoint not found: {filePath}");

        var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var checkpoint = MessagePackSerializer.Deserialize<TrainingCheckpoint>(data, _serializerOptions);

        if (!checkpoint.IsValid())
            throw new InvalidDataException($"Invalid checkpoint data: {filePath}");

        _logger?.LogInformation("Checkpoint loaded: {Path} (Gen={Gen}, Pop={Pop})",
            filePath, checkpoint.State.CurrentGeneration, checkpoint.State.Population.Count);

        return checkpoint;
    }

    /// <inheritdoc />
    public async Task<TrainingCheckpoint?> LoadByGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"checkpoint_gen{generation:D6}_*.bin";
        var files = Directory.GetFiles(_checkpointDirectory, pattern);

        if (files.Length == 0)
        {
            _logger?.LogWarning("No checkpoint found for generation {Gen}", generation);
            return null;
        }

        // Load the most recent one for this generation
        var filePath = files.OrderByDescending(f => f).First();
        return await LoadAsync(filePath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> SaveBestGenomeAsync(
        NetworkGenome genome,
        int generation,
        string? stageName = null,
        int? eloRating = null,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = new BestGenomeCheckpoint
        {
            Genome = genome,
            FoundAtGeneration = generation,
            Fitness = genome.Fitness,
            StageName = stageName,
            EloRating = eloRating,
            GamesPlayed = genome.GamesPlayed,
            Wins = genome.Wins
        };

        var data = MessagePackSerializer.Serialize(checkpoint, _serializerOptions);

        // Save with fitness in filename for easy identification
        var fileName = CheckpointInfo.GenerateBestGenomeFilename(genome.Fitness, generation);
        var filePath = Path.Combine(_bestGenomeDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, data, cancellationToken);

        // Also save as "best.bin" for quick access
        var bestPath = Path.Combine(_bestGenomeDirectory, "best.bin");
        await File.WriteAllBytesAsync(bestPath, data, cancellationToken);

        _logger?.LogInformation("Best genome saved: {Path} (Fit={Fit:F2})",
            filePath, genome.Fitness);

        return filePath;
    }

    /// <inheritdoc />
    public async Task<BestGenomeCheckpoint?> LoadBestGenomeAsync(CancellationToken cancellationToken = default)
    {
        var bestPath = Path.Combine(_bestGenomeDirectory, "best.bin");

        if (!File.Exists(bestPath))
        {
            // Try to find the best genome file
            var bestFiles = Directory.GetFiles(_bestGenomeDirectory, "best_*.bin");
            if (bestFiles.Length == 0)
                return null;

            bestPath = bestFiles.OrderByDescending(f => f).First();
        }

        var data = await File.ReadAllBytesAsync(bestPath, cancellationToken);
        return MessagePackSerializer.Deserialize<BestGenomeCheckpoint>(data, _serializerOptions);
    }

    /// <inheritdoc />
    public IReadOnlyList<CheckpointInfo> ListCheckpoints()
    {
        var files = Directory.GetFiles(_checkpointDirectory, "checkpoint_gen*.bin");
        var checkpoints = new List<CheckpointInfo>();

        foreach (var file in files)
        {
            var info = CheckpointInfo.TryParseFromFile(file);
            if (info != null)
            {
                checkpoints.Add(info);
            }
        }

        return checkpoints.OrderByDescending(c => c.Generation).ToList();
    }

    /// <inheritdoc />
    public int PruneOldCheckpoints(int? keepCount = null)
    {
        keepCount ??= _maxCheckpoints;

        var checkpoints = ListCheckpoints();
        var toDelete = checkpoints.Skip(keepCount.Value).ToList();

        int deleted = 0;
        foreach (var checkpoint in toDelete)
        {
            if (DeleteCheckpoint(checkpoint.FilePath))
            {
                deleted++;
            }
        }

        if (deleted > 0)
        {
            _logger?.LogInformation("Pruned {Count} old checkpoints", deleted);
        }

        return deleted;
    }

    /// <inheritdoc />
    public bool DeleteCheckpoint(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger?.LogDebug("Deleted checkpoint: {Path}", filePath);
                return true;
            }
        }
        catch (IOException ex)
        {
            _logger?.LogWarning(ex, "Failed to delete checkpoint: {Path}", filePath);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyCheckpointAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var checkpoint = MessagePackSerializer.Deserialize<TrainingCheckpoint>(data, _serializerOptions);

            if (!checkpoint.IsValid())
                return false;

            // Verify checksum if present
            if (!string.IsNullOrEmpty(checkpoint.Checksum))
            {
                // Clear checksum, recompute, and compare
                var expectedChecksum = checkpoint.Checksum;
                checkpoint.Checksum = null;
                var checkData = MessagePackSerializer.Serialize(checkpoint, _serializerOptions);
                var actualChecksum = ComputeChecksum(checkData);

                if (expectedChecksum != actualChecksum)
                {
                    _logger?.LogWarning("Checksum mismatch for {Path}", filePath);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Checkpoint verification failed: {Path}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Computes SHA256 checksum of data.
    /// </summary>
    private static string ComputeChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }
}
