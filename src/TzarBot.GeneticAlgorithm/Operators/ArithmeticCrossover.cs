using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Operators;

/// <summary>
/// Arithmetic crossover for weights.
/// Creates offspring weights as a weighted average of parent weights.
///
/// For each weight: child_w = alpha * parent1_w + (1-alpha) * parent2_w
///
/// Alpha can be:
/// - Fixed (e.g., 0.5 for equal contribution)
/// - Random per genome
/// - Random per weight (most diverse)
/// </summary>
public sealed class ArithmeticCrossover : ICrossoverOperator
{
    private readonly GeneticAlgorithmConfig _config;

    /// <inheritdoc />
    public string Name => "ArithmeticCrossover";

    /// <summary>
    /// Mode for alpha selection.
    /// </summary>
    public enum AlphaMode
    {
        /// <summary>Use fixed alpha value from config.</summary>
        Fixed,
        /// <summary>Random alpha per genome.</summary>
        PerGenome,
        /// <summary>Random alpha per weight (most diverse).</summary>
        PerWeight
    }

    /// <summary>
    /// Current alpha mode.
    /// </summary>
    public AlphaMode Mode { get; set; } = AlphaMode.PerWeight;

    public ArithmeticCrossover(GeneticAlgorithmConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public (NetworkGenome child1, NetworkGenome child2) Crossover(
        NetworkGenome parent1,
        NetworkGenome parent2,
        Random random)
    {
        if (parent1 == null) throw new ArgumentNullException(nameof(parent1));
        if (parent2 == null) throw new ArgumentNullException(nameof(parent2));
        if (random == null) throw new ArgumentNullException(nameof(random));

        // Ensure parents have same structure for pure arithmetic crossover
        if (!HaveSameStructure(parent1, parent2))
        {
            throw new InvalidOperationException(
                "ArithmeticCrossover requires parents with identical structure. " +
                "Use UniformCrossover for different structures.");
        }

        var child1 = CreateChild(parent1, parent2, random);
        var child2 = CreateChild(parent2, parent1, random);

        return (child1, child2);
    }

    /// <inheritdoc />
    public NetworkGenome CrossoverSingle(
        NetworkGenome parent1,
        NetworkGenome parent2,
        Random random)
    {
        if (parent1 == null) throw new ArgumentNullException(nameof(parent1));
        if (parent2 == null) throw new ArgumentNullException(nameof(parent2));
        if (random == null) throw new ArgumentNullException(nameof(random));

        if (!HaveSameStructure(parent1, parent2))
        {
            throw new InvalidOperationException(
                "ArithmeticCrossover requires parents with identical structure.");
        }

        return CreateChild(parent1, parent2, random);
    }

    /// <summary>
    /// Creates a child genome from two parents.
    /// </summary>
    private NetworkGenome CreateChild(NetworkGenome parent1, NetworkGenome parent2, Random random)
    {
        // Clone structure from first parent
        var child = new NetworkGenome
        {
            ConvLayers = parent1.ConvLayers.ToArray(),
            HiddenLayers = parent1.HiddenLayers
                .Select(l => new DenseLayerConfig
                {
                    NeuronCount = l.NeuronCount,
                    Activation = l.Activation,
                    DropoutRate = l.DropoutRate
                })
                .ToList(),
            MouseHead = new DenseLayerConfig
            {
                NeuronCount = parent1.MouseHead.NeuronCount,
                Activation = parent1.MouseHead.Activation,
                DropoutRate = parent1.MouseHead.DropoutRate
            },
            ActionHead = new DenseLayerConfig
            {
                NeuronCount = parent1.ActionHead.NeuronCount,
                Activation = parent1.ActionHead.Activation,
                DropoutRate = parent1.ActionHead.DropoutRate
            },
            Weights = new float[parent1.Weights.Length],
            Id = Guid.NewGuid(),
            Generation = Math.Max(parent1.Generation, parent2.Generation) + 1,
            ParentIds = new[] { parent1.Id, parent2.Id },
            CreatedAt = DateTime.UtcNow,
            Fitness = 0f,
            GamesPlayed = 0,
            Wins = 0
        };

        // Crossover weights
        CrossoverWeights(
            parent1.Weights.AsSpan(),
            parent2.Weights.AsSpan(),
            child.Weights.AsSpan(),
            random);

        return child;
    }

    /// <summary>
    /// Performs arithmetic crossover on weight arrays.
    /// </summary>
    public void CrossoverWeights(
        ReadOnlySpan<float> parent1Weights,
        ReadOnlySpan<float> parent2Weights,
        Span<float> childWeights,
        Random random)
    {
        if (parent1Weights.Length != parent2Weights.Length)
            throw new ArgumentException("Parent weight arrays must have same length");
        if (childWeights.Length != parent1Weights.Length)
            throw new ArgumentException("Child weight array must have same length as parents");

        float alpha;

        switch (Mode)
        {
            case AlphaMode.Fixed:
                alpha = _config.ArithmeticCrossoverAlpha;
                for (int i = 0; i < childWeights.Length; i++)
                {
                    childWeights[i] = parent1Weights[i] * alpha + parent2Weights[i] * (1 - alpha);
                }
                break;

            case AlphaMode.PerGenome:
                alpha = (float)random.NextDouble();
                for (int i = 0; i < childWeights.Length; i++)
                {
                    childWeights[i] = parent1Weights[i] * alpha + parent2Weights[i] * (1 - alpha);
                }
                break;

            case AlphaMode.PerWeight:
                for (int i = 0; i < childWeights.Length; i++)
                {
                    alpha = (float)random.NextDouble();
                    childWeights[i] = parent1Weights[i] * alpha + parent2Weights[i] * (1 - alpha);
                }
                break;
        }
    }

    /// <summary>
    /// Checks if two genomes have the same structure.
    /// </summary>
    private static bool HaveSameStructure(NetworkGenome g1, NetworkGenome g2)
    {
        if (g1.HiddenLayers.Count != g2.HiddenLayers.Count)
            return false;

        for (int i = 0; i < g1.HiddenLayers.Count; i++)
        {
            if (g1.HiddenLayers[i].NeuronCount != g2.HiddenLayers[i].NeuronCount)
                return false;
        }

        return g1.Weights.Length == g2.Weights.Length;
    }

    /// <summary>
    /// Blend crossover (BLX-alpha) - creates offspring in extended range.
    /// child = parent1 + rand(-alpha, 1+alpha) * (parent2 - parent1)
    /// </summary>
    public void BlendCrossover(
        ReadOnlySpan<float> parent1Weights,
        ReadOnlySpan<float> parent2Weights,
        Span<float> childWeights,
        Random random,
        float blendAlpha = 0.5f)
    {
        for (int i = 0; i < childWeights.Length; i++)
        {
            float min = Math.Min(parent1Weights[i], parent2Weights[i]);
            float max = Math.Max(parent1Weights[i], parent2Weights[i]);
            float range = max - min;

            float extendedMin = min - blendAlpha * range;
            float extendedMax = max + blendAlpha * range;

            childWeights[i] = extendedMin + (float)random.NextDouble() * (extendedMax - extendedMin);

            // Clamp to valid range
            childWeights[i] = Math.Clamp(childWeights[i], _config.MinWeight, _config.MaxWeight);
        }
    }
}
