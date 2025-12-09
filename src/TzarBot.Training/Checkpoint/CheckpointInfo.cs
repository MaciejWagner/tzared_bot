namespace TzarBot.Training.Checkpoint;

/// <summary>
/// Metadata about a saved checkpoint file.
/// Used for listing and selecting checkpoints without loading full data.
/// </summary>
public sealed class CheckpointInfo
{
    /// <summary>
    /// Full path to the checkpoint file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Checkpoint unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Generation number at checkpoint.
    /// </summary>
    public int Generation { get; init; }

    /// <summary>
    /// Curriculum stage at checkpoint.
    /// </summary>
    public string? StageName { get; init; }

    /// <summary>
    /// Best fitness at checkpoint.
    /// </summary>
    public float BestFitness { get; init; }

    /// <summary>
    /// Population size.
    /// </summary>
    public int PopulationSize { get; init; }

    /// <summary>
    /// When the checkpoint was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Whether this is the "latest" checkpoint.
    /// </summary>
    public bool IsLatest { get; init; }

    /// <summary>
    /// Whether this is a "best genome" checkpoint.
    /// </summary>
    public bool IsBestGenome { get; init; }

    /// <summary>
    /// Parses checkpoint info from filename.
    /// Expected format: checkpoint_gen{generation:D6}_{timestamp:yyyyMMdd_HHmmss}.bin
    /// </summary>
    public static CheckpointInfo? TryParseFromFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName == null)
            return null;

        // Handle special files
        if (fileName.Equals("latest.bin", StringComparison.OrdinalIgnoreCase))
        {
            var fileInfo = new FileInfo(filePath);
            return new CheckpointInfo
            {
                FilePath = filePath,
                Timestamp = fileInfo.LastWriteTimeUtc,
                FileSize = fileInfo.Length,
                IsLatest = true
            };
        }

        if (fileName.StartsWith("best_", StringComparison.OrdinalIgnoreCase))
        {
            var fileInfo = new FileInfo(filePath);
            return new CheckpointInfo
            {
                FilePath = filePath,
                Timestamp = fileInfo.LastWriteTimeUtc,
                FileSize = fileInfo.Length,
                IsBestGenome = true
            };
        }

        // Parse regular checkpoint filename
        if (!fileName.StartsWith("checkpoint_gen", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var parts = fileName.Split('_');
            if (parts.Length < 4)
                return null;

            var genStr = parts[1].Substring(3); // Remove "gen"
            var generation = int.Parse(genStr);

            var dateStr = parts[2];
            var timeStr = parts[3].Replace(".bin", "");
            var timestamp = DateTime.ParseExact(
                $"{dateStr}_{timeStr}",
                "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture);

            var fileInfo = new FileInfo(filePath);

            return new CheckpointInfo
            {
                FilePath = filePath,
                Generation = generation,
                Timestamp = timestamp,
                FileSize = fileInfo.Length
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a checkpoint filename.
    /// </summary>
    public static string GenerateFilename(int generation, DateTime? timestamp = null)
    {
        timestamp ??= DateTime.UtcNow;
        return $"checkpoint_gen{generation:D6}_{timestamp:yyyyMMdd_HHmmss}.bin";
    }

    /// <summary>
    /// Generates a best genome filename.
    /// </summary>
    public static string GenerateBestGenomeFilename(float fitness, int generation)
    {
        return $"best_fit{fitness:F0}_gen{generation:D6}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";
    }

    public override string ToString()
    {
        if (IsLatest)
            return $"CheckpointInfo[LATEST] Size={FileSize / 1024}KB";
        if (IsBestGenome)
            return $"CheckpointInfo[BEST] Size={FileSize / 1024}KB";

        return $"CheckpointInfo[Gen={Generation}] Stage={StageName}, " +
               $"BestFit={BestFitness:F2}, Size={FileSize / 1024}KB";
    }
}
