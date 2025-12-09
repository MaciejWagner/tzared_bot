using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Persistence;

/// <summary>
/// Repository interface for persisting genomes and populations.
/// </summary>
public interface IGenomeRepository : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Saves a genome to the repository.
    /// </summary>
    /// <param name="genome">Genome to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(NetworkGenome genome, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple genomes to the repository.
    /// </summary>
    /// <param name="genomes">Genomes to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveManyAsync(IEnumerable<NetworkGenome> genomes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a genome by ID.
    /// </summary>
    /// <param name="id">Genome ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Genome if found, null otherwise.</returns>
    Task<NetworkGenome?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets genomes by generation number.
    /// </summary>
    /// <param name="generation">Generation number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<NetworkGenome>> GetByGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top N genomes by fitness.
    /// </summary>
    /// <param name="count">Number of genomes to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<NetworkGenome>> GetTopByFitnessAsync(
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all genomes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<NetworkGenome>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a genome by ID.
    /// </summary>
    /// <param name="id">Genome ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes genomes older than a specific generation.
    /// </summary>
    /// <param name="generation">Generation threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of genomes deleted.</returns>
    Task<int> DeleteOlderThanGenerationAsync(
        int generation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of genomes in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest generation number.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> GetLatestGenerationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a genome's fitness and evaluation stats.
    /// </summary>
    /// <param name="id">Genome ID.</param>
    /// <param name="fitness">New fitness value.</param>
    /// <param name="gamesPlayed">Games played.</param>
    /// <param name="wins">Wins.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateFitnessAsync(
        Guid id,
        float fitness,
        int gamesPlayed,
        int wins,
        CancellationToken cancellationToken = default);
}
