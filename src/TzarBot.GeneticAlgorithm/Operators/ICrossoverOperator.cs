using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Operators;

/// <summary>
/// Interface for crossover operators that combine two parent genomes.
/// </summary>
public interface ICrossoverOperator
{
    /// <summary>
    /// Name of this crossover operator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Creates offspring from two parent genomes.
    /// </summary>
    /// <param name="parent1">First parent genome.</param>
    /// <param name="parent2">Second parent genome.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>Two offspring genomes.</returns>
    (NetworkGenome child1, NetworkGenome child2) Crossover(
        NetworkGenome parent1,
        NetworkGenome parent2,
        Random random);

    /// <summary>
    /// Creates a single offspring from two parents.
    /// </summary>
    /// <param name="parent1">First parent genome.</param>
    /// <param name="parent2">Second parent genome.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>Single offspring genome.</returns>
    NetworkGenome CrossoverSingle(
        NetworkGenome parent1,
        NetworkGenome parent2,
        Random random);
}
