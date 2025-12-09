using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Operators;

/// <summary>
/// Mutates weights using Gaussian noise.
/// Implements two mutation strategies:
/// 1. Perturbation: Add Gaussian noise to existing weights
/// 2. Reset: Replace weight with a new random value
///
/// All weights are clamped to [MinWeight, MaxWeight] after mutation.
/// </summary>
public sealed class WeightMutator : IMutationOperator
{
    private readonly GeneticAlgorithmConfig _config;

    /// <inheritdoc />
    public string Name => "WeightMutation";

    public WeightMutator(GeneticAlgorithmConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public bool Mutate(NetworkGenome genome, Random random)
    {
        if (genome == null) throw new ArgumentNullException(nameof(genome));
        if (random == null) throw new ArgumentNullException(nameof(random));

        // Check if weight mutation should occur at all
        if (random.NextDouble() > _config.WeightMutationRate)
            return false;

        var weights = genome.Weights;
        if (weights.Length == 0)
            return false;

        bool anyMutated = false;

        for (int i = 0; i < weights.Length; i++)
        {
            // Check if this weight should be mutated
            if (random.NextDouble() > _config.WeightPerturbationRate)
                continue;

            // Decide between reset and perturbation
            if (random.NextDouble() < _config.WeightResetRate)
            {
                // Reset: new random value in [-1, 1] scaled by initial stddev
                weights[i] = (float)((random.NextDouble() * 2.0 - 1.0) * 2.0);
            }
            else
            {
                // Perturbation: add Gaussian noise
                double noise = NextGaussian(random) * _config.WeightMutationStrength;
                weights[i] += (float)noise;
            }

            // Clamp weight to valid range
            weights[i] = Math.Clamp(weights[i], _config.MinWeight, _config.MaxWeight);
            anyMutated = true;
        }

        return anyMutated;
    }

    /// <summary>
    /// Mutates a specific range of weights.
    /// Useful for targeted mutation of specific layers.
    /// </summary>
    /// <param name="weights">Weight array to mutate.</param>
    /// <param name="startIndex">Start index (inclusive).</param>
    /// <param name="count">Number of weights to potentially mutate.</param>
    /// <param name="random">Random number generator.</param>
    /// <returns>Number of weights mutated.</returns>
    public int MutateRange(float[] weights, int startIndex, int count, Random random)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));
        if (startIndex < 0 || startIndex >= weights.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (count < 0 || startIndex + count > weights.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        int mutatedCount = 0;
        int endIndex = startIndex + count;

        for (int i = startIndex; i < endIndex; i++)
        {
            if (random.NextDouble() > _config.WeightPerturbationRate)
                continue;

            if (random.NextDouble() < _config.WeightResetRate)
            {
                weights[i] = (float)((random.NextDouble() * 2.0 - 1.0) * 2.0);
            }
            else
            {
                double noise = NextGaussian(random) * _config.WeightMutationStrength;
                weights[i] += (float)noise;
            }

            weights[i] = Math.Clamp(weights[i], _config.MinWeight, _config.MaxWeight);
            mutatedCount++;
        }

        return mutatedCount;
    }

    /// <summary>
    /// Box-Muller transform for generating Gaussian random numbers.
    /// </summary>
    private static double NextGaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] to avoid log(0)
        double u2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Validates that all weights are within the valid range.
    /// </summary>
    public bool ValidateWeights(ReadOnlySpan<float> weights)
    {
        foreach (var w in weights)
        {
            if (float.IsNaN(w) || float.IsInfinity(w))
                return false;
            if (w < _config.MinWeight || w > _config.MaxWeight)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Clamps all weights to the valid range.
    /// </summary>
    public void ClampWeights(Span<float> weights)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            if (float.IsNaN(weights[i]) || float.IsInfinity(weights[i]))
            {
                weights[i] = 0f;
            }
            else
            {
                weights[i] = Math.Clamp(weights[i], _config.MinWeight, _config.MaxWeight);
            }
        }
    }
}
