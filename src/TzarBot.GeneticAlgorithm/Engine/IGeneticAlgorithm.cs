using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Engine;

/// <summary>
/// Represents a delegate for evaluating genome fitness.
/// </summary>
/// <param name="genome">The genome to evaluate.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Fitness score (higher is better).</returns>
public delegate Task<float> FitnessEvaluator(NetworkGenome genome, CancellationToken cancellationToken);

/// <summary>
/// Interface for genetic algorithm engine.
/// Manages population evolution through selection, crossover, and mutation.
/// </summary>
public interface IGeneticAlgorithm
{
    /// <summary>
    /// Current generation number.
    /// </summary>
    int Generation { get; }

    /// <summary>
    /// Current population of genomes.
    /// </summary>
    IReadOnlyList<NetworkGenome> Population { get; }

    /// <summary>
    /// Best genome found so far (highest fitness).
    /// </summary>
    NetworkGenome? BestGenome { get; }

    /// <summary>
    /// Statistics for the current generation.
    /// </summary>
    GenerationStats CurrentStats { get; }

    /// <summary>
    /// Configuration for this GA instance.
    /// </summary>
    GeneticAlgorithmConfig Config { get; }

    /// <summary>
    /// Initializes the population with random genomes.
    /// </summary>
    /// <param name="seed">Random seed for reproducibility.</param>
    void InitializePopulation(int? seed = null);

    /// <summary>
    /// Loads a population from a checkpoint.
    /// </summary>
    /// <param name="population">Population to load.</param>
    void LoadPopulation(IEnumerable<NetworkGenome> population);

    /// <summary>
    /// Runs a single generation: evaluation, selection, crossover, mutation.
    /// </summary>
    /// <param name="evaluator">Function to evaluate genome fitness.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics for this generation.</returns>
    Task<GenerationStats> RunGenerationAsync(
        FitnessEvaluator evaluator,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs multiple generations until max generations or cancellation.
    /// </summary>
    /// <param name="evaluator">Function to evaluate genome fitness.</param>
    /// <param name="maxGenerations">Maximum generations to run (-1 for unlimited).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of generation statistics.</returns>
    IAsyncEnumerable<GenerationStats> RunAsync(
        FitnessEvaluator evaluator,
        int maxGenerations = -1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a generation completes.
    /// </summary>
    event EventHandler<GenerationStats>? GenerationCompleted;

    /// <summary>
    /// Event raised when a new best genome is found.
    /// </summary>
    event EventHandler<NetworkGenome>? NewBestGenomeFound;
}

/// <summary>
/// Statistics for a single generation.
/// </summary>
public sealed class GenerationStats
{
    /// <summary>
    /// Generation number.
    /// </summary>
    public int Generation { get; init; }

    /// <summary>
    /// Best fitness in this generation.
    /// </summary>
    public float BestFitness { get; init; }

    /// <summary>
    /// Average fitness of the population.
    /// </summary>
    public float AverageFitness { get; init; }

    /// <summary>
    /// Worst fitness in this generation.
    /// </summary>
    public float WorstFitness { get; init; }

    /// <summary>
    /// Standard deviation of fitness values.
    /// </summary>
    public float FitnessStdDev { get; init; }

    /// <summary>
    /// Number of elite genomes preserved.
    /// </summary>
    public int EliteCount { get; init; }

    /// <summary>
    /// Number of crossover operations performed.
    /// </summary>
    public int CrossoverCount { get; init; }

    /// <summary>
    /// Number of mutations applied.
    /// </summary>
    public int MutationCount { get; init; }

    /// <summary>
    /// Time taken to evaluate the generation.
    /// </summary>
    public TimeSpan EvaluationTime { get; init; }

    /// <summary>
    /// Time taken for selection, crossover, and mutation.
    /// </summary>
    public TimeSpan EvolutionTime { get; init; }

    /// <summary>
    /// ID of the best genome.
    /// </summary>
    public Guid BestGenomeId { get; init; }

    /// <summary>
    /// Improvement from previous generation (best fitness delta).
    /// </summary>
    public float Improvement { get; init; }

    /// <summary>
    /// Timestamp when this generation completed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Gen {Generation}: Best={BestFitness:F3}, Avg={AverageFitness:F3}, " +
               $"StdDev={FitnessStdDev:F3}, Elites={EliteCount}, " +
               $"Time={EvaluationTime.TotalSeconds:F1}s";
    }
}
