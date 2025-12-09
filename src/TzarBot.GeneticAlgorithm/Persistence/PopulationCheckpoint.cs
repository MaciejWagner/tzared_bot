using MessagePack;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Persistence;

/// <summary>
/// Manages population checkpoints for save/restore functionality.
/// Supports both file-based and repository-based persistence.
/// </summary>
public sealed class PopulationCheckpoint
{
    private readonly string _checkpointDirectory;

    /// <summary>
    /// Creates checkpoint manager with specified directory.
    /// </summary>
    /// <param name="checkpointDirectory">Directory for checkpoint files.</param>
    public PopulationCheckpoint(string checkpointDirectory)
    {
        _checkpointDirectory = checkpointDirectory ?? throw new ArgumentNullException(nameof(checkpointDirectory));
        Directory.CreateDirectory(_checkpointDirectory);
    }

    /// <summary>
    /// Saves a population checkpoint.
    /// </summary>
    /// <param name="population">Population to save.</param>
    /// <param name="generation">Current generation number.</param>
    /// <param name="stats">Optional generation statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to saved checkpoint.</returns>
    public async Task<string> SaveAsync(
        IEnumerable<NetworkGenome> population,
        int generation,
        GenerationStats? stats = null,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = new CheckpointData
        {
            Generation = generation,
            Timestamp = DateTime.UtcNow,
            Stats = stats != null ? new CheckpointStats(stats) : null,
            Genomes = population.ToList()
        };

        var fileName = $"checkpoint_gen{generation:D6}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";
        var filePath = Path.Combine(_checkpointDirectory, fileName);

        var data = SerializeCheckpoint(checkpoint);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);

        // Also save a "latest" link
        var latestPath = Path.Combine(_checkpointDirectory, "latest.bin");
        await File.WriteAllBytesAsync(latestPath, data, cancellationToken);

        return filePath;
    }

    /// <summary>
    /// Loads the latest checkpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Checkpoint data or null if none exists.</returns>
    public async Task<CheckpointData?> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        var latestPath = Path.Combine(_checkpointDirectory, "latest.bin");

        if (!File.Exists(latestPath))
        {
            // Try to find the most recent checkpoint
            var checkpoints = Directory.GetFiles(_checkpointDirectory, "checkpoint_gen*.bin");
            if (checkpoints.Length == 0)
                return null;

            latestPath = checkpoints.OrderByDescending(f => f).First();
        }

        var data = await File.ReadAllBytesAsync(latestPath, cancellationToken);
        return DeserializeCheckpoint(data);
    }

    /// <summary>
    /// Loads a specific checkpoint by generation.
    /// </summary>
    /// <param name="generation">Generation number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Checkpoint data or null if not found.</returns>
    public async Task<CheckpointData?> LoadByGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"checkpoint_gen{generation:D6}_*.bin";
        var files = Directory.GetFiles(_checkpointDirectory, pattern);

        if (files.Length == 0)
            return null;

        // Load the most recent checkpoint for this generation
        var filePath = files.OrderByDescending(f => f).First();
        var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return DeserializeCheckpoint(data);
    }

    /// <summary>
    /// Lists all available checkpoints.
    /// </summary>
    public IReadOnlyList<CheckpointInfo> ListCheckpoints()
    {
        var files = Directory.GetFiles(_checkpointDirectory, "checkpoint_gen*.bin");
        var checkpoints = new List<CheckpointInfo>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (TryParseCheckpointName(fileName, out int generation, out DateTime timestamp))
            {
                checkpoints.Add(new CheckpointInfo
                {
                    FilePath = file,
                    Generation = generation,
                    Timestamp = timestamp,
                    FileSize = new FileInfo(file).Length
                });
            }
        }

        return checkpoints.OrderByDescending(c => c.Generation).ToList();
    }

    /// <summary>
    /// Deletes old checkpoints, keeping only the most recent N.
    /// </summary>
    /// <param name="keepCount">Number of checkpoints to keep.</param>
    public void PruneOldCheckpoints(int keepCount)
    {
        var checkpoints = ListCheckpoints();

        foreach (var checkpoint in checkpoints.Skip(keepCount))
        {
            try
            {
                File.Delete(checkpoint.FilePath);
            }
            catch (IOException)
            {
                // Ignore deletion errors
            }
        }
    }

    /// <summary>
    /// Exports a checkpoint to a standalone file.
    /// </summary>
    public async Task ExportAsync(
        CheckpointData checkpoint,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var data = SerializeCheckpoint(checkpoint);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllBytesAsync(filePath, data, cancellationToken);
    }

    /// <summary>
    /// Imports a checkpoint from a file.
    /// </summary>
    public async Task<CheckpointData> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return DeserializeCheckpoint(data);
    }

    private static byte[] SerializeCheckpoint(CheckpointData checkpoint)
    {
        return MessagePackSerializer.Serialize(checkpoint,
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
    }

    private static CheckpointData DeserializeCheckpoint(byte[] data)
    {
        return MessagePackSerializer.Deserialize<CheckpointData>(data,
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
    }

    private static bool TryParseCheckpointName(string fileName, out int generation, out DateTime timestamp)
    {
        generation = 0;
        timestamp = default;

        // Expected format: checkpoint_gen000123_20240115_143022.bin
        if (!fileName.StartsWith("checkpoint_gen"))
            return false;

        try
        {
            var parts = fileName.Split('_');
            if (parts.Length < 4)
                return false;

            var genStr = parts[1].Substring(3); // Remove "gen"
            generation = int.Parse(genStr);

            var dateStr = parts[2];
            var timeStr = parts[3].Replace(".bin", "");

            timestamp = DateTime.ParseExact(
                $"{dateStr}_{timeStr}",
                "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture);

            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Data stored in a checkpoint.
/// </summary>
[MessagePackObject]
public sealed class CheckpointData
{
    [Key(0)]
    public int Generation { get; set; }

    [Key(1)]
    public DateTime Timestamp { get; set; }

    [Key(2)]
    public CheckpointStats? Stats { get; set; }

    [Key(3)]
    public List<NetworkGenome> Genomes { get; set; } = new();

    [Key(4)]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Statistics stored with checkpoint.
/// </summary>
[MessagePackObject]
public sealed class CheckpointStats
{
    [Key(0)]
    public float BestFitness { get; set; }

    [Key(1)]
    public float AverageFitness { get; set; }

    [Key(2)]
    public float WorstFitness { get; set; }

    [Key(3)]
    public int EliteCount { get; set; }

    [Key(4)]
    public Guid BestGenomeId { get; set; }

    public CheckpointStats() { }

    public CheckpointStats(GenerationStats stats)
    {
        BestFitness = stats.BestFitness;
        AverageFitness = stats.AverageFitness;
        WorstFitness = stats.WorstFitness;
        EliteCount = stats.EliteCount;
        BestGenomeId = stats.BestGenomeId;
    }
}

/// <summary>
/// Information about a checkpoint file.
/// </summary>
public sealed class CheckpointInfo
{
    public string FilePath { get; init; } = string.Empty;
    public int Generation { get; init; }
    public DateTime Timestamp { get; init; }
    public long FileSize { get; init; }
}
