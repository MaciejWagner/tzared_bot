using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Selection;

/// <summary>
/// Tournament selection: randomly selects k individuals and returns the best one.
///
/// Selection pressure is controlled by tournament size (k):
/// - k=2: Low pressure, more exploration
/// - k=3: Moderate pressure (default)
/// - k=5+: High pressure, more exploitation
///
/// Advantages:
/// - Simple and efficient
/// - Selection pressure easily tunable
/// - Works well with parallel evaluation
/// </summary>
public sealed class TournamentSelection : ISelectionStrategy
{
    private readonly int _tournamentSize;

    /// <inheritdoc />
    public string Name => $"Tournament(k={_tournamentSize})";

    /// <summary>
    /// Creates tournament selection with specified tournament size.
    /// </summary>
    /// <param name="tournamentSize">Number of participants in each tournament.</param>
    public TournamentSelection(int tournamentSize = 3)
    {
        if (tournamentSize < 2)
            throw new ArgumentOutOfRangeException(nameof(tournamentSize), "Tournament size must be at least 2");

        _tournamentSize = tournamentSize;
    }

    /// <summary>
    /// Creates tournament selection from config.
    /// </summary>
    public TournamentSelection(GeneticAlgorithmConfig config)
        : this(config?.TournamentSize ?? 3)
    {
    }

    /// <inheritdoc />
    public NetworkGenome Select(IReadOnlyList<NetworkGenome> population, Random random)
    {
        if (population == null || population.Count == 0)
            throw new ArgumentException("Population cannot be empty", nameof(population));
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        // Adjust tournament size for small populations
        int actualTournamentSize = Math.Min(_tournamentSize, population.Count);

        NetworkGenome? best = null;
        float bestFitness = float.NegativeInfinity;

        // Select tournament participants and find best
        for (int i = 0; i < actualTournamentSize; i++)
        {
            int idx = random.Next(population.Count);
            var candidate = population[idx];

            if (candidate.Fitness > bestFitness)
            {
                bestFitness = candidate.Fitness;
                best = candidate;
            }
        }

        return best ?? population[0];
    }

    /// <inheritdoc />
    public IEnumerable<NetworkGenome> SelectMany(
        IReadOnlyList<NetworkGenome> population,
        int count,
        Random random)
    {
        if (population == null || population.Count == 0)
            throw new ArgumentException("Population cannot be empty", nameof(population));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        for (int i = 0; i < count; i++)
        {
            yield return Select(population, random);
        }
    }

    /// <inheritdoc />
    public IEnumerable<(NetworkGenome parent1, NetworkGenome parent2)> SelectPairs(
        IReadOnlyList<NetworkGenome> population,
        int pairCount,
        Random random)
    {
        if (population == null || population.Count < 2)
            throw new ArgumentException("Population must have at least 2 individuals", nameof(population));
        if (pairCount < 0)
            throw new ArgumentOutOfRangeException(nameof(pairCount));
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        for (int i = 0; i < pairCount; i++)
        {
            var parent1 = Select(population, random);
            var parent2 = Select(population, random);

            // Ensure different parents if possible
            int attempts = 0;
            while (parent2.Id == parent1.Id && attempts < 5 && population.Count > 1)
            {
                parent2 = Select(population, random);
                attempts++;
            }

            yield return (parent1, parent2);
        }
    }

    /// <summary>
    /// Performs deterministic tournament selection (for testing/reproducibility).
    /// Sorts all candidates and returns the best.
    /// </summary>
    public NetworkGenome SelectDeterministic(
        IReadOnlyList<NetworkGenome> population,
        int[] candidateIndices)
    {
        if (population == null || population.Count == 0)
            throw new ArgumentException("Population cannot be empty", nameof(population));
        if (candidateIndices == null || candidateIndices.Length == 0)
            throw new ArgumentException("Must provide candidate indices", nameof(candidateIndices));

        NetworkGenome? best = null;
        float bestFitness = float.NegativeInfinity;

        foreach (int idx in candidateIndices)
        {
            if (idx < 0 || idx >= population.Count)
                continue;

            var candidate = population[idx];
            if (candidate.Fitness > bestFitness)
            {
                bestFitness = candidate.Fitness;
                best = candidate;
            }
        }

        return best ?? population[candidateIndices[0]];
    }
}
