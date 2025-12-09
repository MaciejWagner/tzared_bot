using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Selection;

/// <summary>
/// Interface for selection strategies that choose parents for reproduction.
/// </summary>
public interface ISelectionStrategy
{
    /// <summary>
    /// Name of this selection strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Selects a parent from the population.
    /// </summary>
    /// <param name="population">Population to select from.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>Selected genome.</returns>
    NetworkGenome Select(IReadOnlyList<NetworkGenome> population, Random random);

    /// <summary>
    /// Selects multiple parents from the population.
    /// </summary>
    /// <param name="population">Population to select from.</param>
    /// <param name="count">Number of parents to select.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>Selected genomes.</returns>
    IEnumerable<NetworkGenome> SelectMany(
        IReadOnlyList<NetworkGenome> population,
        int count,
        Random random);

    /// <summary>
    /// Selects pairs of parents for crossover.
    /// </summary>
    /// <param name="population">Population to select from.</param>
    /// <param name="pairCount">Number of pairs to select.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>Pairs of parent genomes.</returns>
    IEnumerable<(NetworkGenome parent1, NetworkGenome parent2)> SelectPairs(
        IReadOnlyList<NetworkGenome> population,
        int pairCount,
        Random random);
}
