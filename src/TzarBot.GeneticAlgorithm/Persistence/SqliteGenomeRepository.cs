using Microsoft.Data.Sqlite;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Persistence;

/// <summary>
/// SQLite-based repository for persisting genomes.
/// Uses MessagePack for efficient serialization of genome data.
///
/// Schema:
/// - genomes: id, generation, fitness, games_played, wins, created_at, data (blob)
/// - metadata: key, value (for tracking evolution progress)
/// </summary>
public sealed class SqliteGenomeRepository : IGenomeRepository
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;
    private bool _disposed;

    /// <summary>
    /// Creates repository with specified database path.
    /// </summary>
    /// <param name="databasePath">Path to SQLite database file.</param>
    public SqliteGenomeRepository(string databasePath)
    {
        if (string.IsNullOrEmpty(databasePath))
            throw new ArgumentNullException(nameof(databasePath));

        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={databasePath}";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        InitializeDatabase();
    }

    /// <summary>
    /// Creates repository with in-memory database (for testing).
    /// </summary>
    public static SqliteGenomeRepository CreateInMemory()
    {
        return new SqliteGenomeRepository(":memory:");
    }

    private void InitializeDatabase()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS genomes (
                id TEXT PRIMARY KEY,
                generation INTEGER NOT NULL,
                fitness REAL NOT NULL,
                games_played INTEGER NOT NULL DEFAULT 0,
                wins INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                data BLOB NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_genomes_generation ON genomes(generation);
            CREATE INDEX IF NOT EXISTS idx_genomes_fitness ON genomes(fitness DESC);

            CREATE TABLE IF NOT EXISTS metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public async Task SaveAsync(NetworkGenome genome, CancellationToken cancellationToken = default)
    {
        if (genome == null)
            throw new ArgumentNullException(nameof(genome));

        var data = GenomeSerializer.Serialize(genome);

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO genomes (id, generation, fitness, games_played, wins, created_at, data)
            VALUES (@id, @generation, @fitness, @gamesPlayed, @wins, @createdAt, @data)
        ";
        command.Parameters.AddWithValue("@id", genome.Id.ToString());
        command.Parameters.AddWithValue("@generation", genome.Generation);
        command.Parameters.AddWithValue("@fitness", genome.Fitness);
        command.Parameters.AddWithValue("@gamesPlayed", genome.GamesPlayed);
        command.Parameters.AddWithValue("@wins", genome.Wins);
        command.Parameters.AddWithValue("@createdAt", genome.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@data", data);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<NetworkGenome> genomes, CancellationToken cancellationToken = default)
    {
        if (genomes == null)
            throw new ArgumentNullException(nameof(genomes));

        using var transaction = _connection.BeginTransaction();
        try
        {
            foreach (var genome in genomes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SaveAsync(genome, cancellationToken);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<NetworkGenome?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT data FROM genomes WHERE id = @id";
        command.Parameters.AddWithValue("@id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var data = (byte[])reader["data"];
            return GenomeSerializer.Deserialize(data);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkGenome>> GetByGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        var genomes = new List<NetworkGenome>();

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT data FROM genomes WHERE generation = @generation ORDER BY fitness DESC";
        command.Parameters.AddWithValue("@generation", generation);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var data = (byte[])reader["data"];
            genomes.Add(GenomeSerializer.Deserialize(data));
        }

        return genomes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkGenome>> GetTopByFitnessAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var genomes = new List<NetworkGenome>();

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT data FROM genomes ORDER BY fitness DESC LIMIT @count";
        command.Parameters.AddWithValue("@count", count);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var data = (byte[])reader["data"];
            genomes.Add(GenomeSerializer.Deserialize(data));
        }

        return genomes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkGenome>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var genomes = new List<NetworkGenome>();

        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT data FROM genomes ORDER BY generation DESC, fitness DESC";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var data = (byte[])reader["data"];
            genomes.Add(GenomeSerializer.Deserialize(data));
        }

        return genomes;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM genomes WHERE id = @id";
        command.Parameters.AddWithValue("@id", id.ToString());

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    /// <inheritdoc />
    public async Task<int> DeleteOlderThanGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM genomes WHERE generation < @generation";
        command.Parameters.AddWithValue("@generation", generation);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM genomes";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    /// <inheritdoc />
    public async Task<int> GetLatestGenerationAsync(CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT MAX(generation) FROM genomes";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    /// <inheritdoc />
    public async Task UpdateFitnessAsync(
        Guid id,
        float fitness,
        int gamesPlayed,
        int wins,
        CancellationToken cancellationToken = default)
    {
        // First get the genome to update
        var genome = await GetByIdAsync(id, cancellationToken);
        if (genome == null)
            return;

        genome.Fitness = fitness;
        genome.GamesPlayed = gamesPlayed;
        genome.Wins = wins;

        await SaveAsync(genome, cancellationToken);
    }

    /// <summary>
    /// Gets metadata value by key.
    /// </summary>
    public async Task<string?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT value FROM metadata WHERE key = @key";
        command.Parameters.AddWithValue("@key", key);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    /// <summary>
    /// Sets metadata value.
    /// </summary>
    public async Task SetMetadataAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO metadata (key, value)
            VALUES (@key, @value)
        ";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Gets generation statistics.
    /// </summary>
    public async Task<GenerationStatistics?> GetGenerationStatisticsAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT
                COUNT(*) as count,
                AVG(fitness) as avg_fitness,
                MAX(fitness) as max_fitness,
                MIN(fitness) as min_fitness,
                SUM(games_played) as total_games,
                SUM(wins) as total_wins
            FROM genomes
            WHERE generation = @generation
        ";
        command.Parameters.AddWithValue("@generation", generation);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            int count = Convert.ToInt32(reader["count"]);
            if (count == 0) return null;

            return new GenerationStatistics
            {
                Generation = generation,
                PopulationSize = count,
                AverageFitness = Convert.ToSingle(reader["avg_fitness"]),
                MaxFitness = Convert.ToSingle(reader["max_fitness"]),
                MinFitness = Convert.ToSingle(reader["min_fitness"]),
                TotalGamesPlayed = Convert.ToInt32(reader["total_games"]),
                TotalWins = Convert.ToInt32(reader["total_wins"])
            };
        }

        return null;
    }

    /// <summary>
    /// Disposes the repository.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _connection.Close();
        _connection.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Disposes the repository asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        _disposed = true;
    }
}

/// <summary>
/// Statistics for a generation.
/// </summary>
public sealed class GenerationStatistics
{
    public int Generation { get; init; }
    public int PopulationSize { get; init; }
    public float AverageFitness { get; init; }
    public float MaxFitness { get; init; }
    public float MinFitness { get; init; }
    public int TotalGamesPlayed { get; init; }
    public int TotalWins { get; init; }

    public float WinRate => TotalGamesPlayed > 0 ? (float)TotalWins / TotalGamesPlayed : 0f;
}
