using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Operators;

/// <summary>
/// Interface for mutation operators that modify genomes.
/// </summary>
public interface IMutationOperator
{
    /// <summary>
    /// Name of this mutation operator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Mutates a genome in place.
    /// </summary>
    /// <param name="genome">The genome to mutate.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>True if any mutation was applied, false otherwise.</returns>
    bool Mutate(NetworkGenome genome, Random random);
}

/// <summary>
/// Result of a mutation operation with details.
/// </summary>
public sealed class MutationResult
{
    /// <summary>
    /// Whether any mutation was applied.
    /// </summary>
    public bool WasMutated { get; init; }

    /// <summary>
    /// Description of mutations applied.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Number of weights changed.
    /// </summary>
    public int WeightsChanged { get; init; }

    /// <summary>
    /// Number of layers added.
    /// </summary>
    public int LayersAdded { get; init; }

    /// <summary>
    /// Number of layers removed.
    /// </summary>
    public int LayersRemoved { get; init; }

    /// <summary>
    /// Change in total neuron count.
    /// </summary>
    public int NeuronDelta { get; init; }

    public static MutationResult None => new() { WasMutated = false };
}
