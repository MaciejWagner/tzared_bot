using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Operators;

/// <summary>
/// Uniform crossover for network structure.
/// Each layer configuration is independently inherited from either parent.
/// Weights are then interpolated using arithmetic crossover.
///
/// For different-sized networks, structure is inherited from the parent
/// with higher fitness (if configured), otherwise randomly.
/// </summary>
public sealed class UniformCrossover : ICrossoverOperator
{
    private readonly GeneticAlgorithmConfig _config;
    private readonly ArithmeticCrossover _weightCrossover;

    /// <inheritdoc />
    public string Name => "UniformCrossover";

    public UniformCrossover(GeneticAlgorithmConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _weightCrossover = new ArithmeticCrossover(config);
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

        var child1 = CrossoverSingle(parent1, parent2, random);
        var child2 = CrossoverSingle(parent2, parent1, random);

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

        // Determine which parent has better fitness
        var betterParent = parent1.Fitness >= parent2.Fitness ? parent1 : parent2;
        var worseParent = parent1.Fitness >= parent2.Fitness ? parent2 : parent1;

        // Choose structure source
        NetworkGenome structureSource;
        if (_config.InheritFromBetterParent)
        {
            structureSource = betterParent;
        }
        else
        {
            structureSource = random.NextDouble() < 0.5 ? parent1 : parent2;
        }

        // Create child with inherited structure
        var child = new NetworkGenome
        {
            ConvLayers = structureSource.ConvLayers.ToArray(),
            HiddenLayers = CreateChildLayers(parent1, parent2, structureSource, random),
            MouseHead = new DenseLayerConfig
            {
                NeuronCount = structureSource.MouseHead.NeuronCount,
                Activation = structureSource.MouseHead.Activation,
                DropoutRate = structureSource.MouseHead.DropoutRate
            },
            ActionHead = new DenseLayerConfig
            {
                NeuronCount = structureSource.ActionHead.NeuronCount,
                Activation = structureSource.ActionHead.Activation,
                DropoutRate = structureSource.ActionHead.DropoutRate
            },
            Id = Guid.NewGuid(),
            Generation = Math.Max(parent1.Generation, parent2.Generation) + 1,
            ParentIds = new[] { parent1.Id, parent2.Id },
            CreatedAt = DateTime.UtcNow,
            Fitness = 0f,
            GamesPlayed = 0,
            Wins = 0
        };

        // Initialize weights
        var config = NetworkConfig.Default();
        int totalWeights = child.TotalWeightCount(config.FlattenedConvOutputSize);
        child.Weights = new float[totalWeights];

        // If parents have same structure, use arithmetic crossover
        if (HaveSameStructure(parent1, parent2))
        {
            _weightCrossover.CrossoverWeights(
                parent1.Weights.AsSpan(),
                parent2.Weights.AsSpan(),
                child.Weights.AsSpan(),
                random);
        }
        else
        {
            // Different structures: inherit from structure source and perturb
            InheritWeightsFromBestMatch(child, parent1, parent2, random);
        }

        return child;
    }

    /// <summary>
    /// Creates hidden layers for child using uniform crossover.
    /// </summary>
    private List<DenseLayerConfig> CreateChildLayers(
        NetworkGenome parent1,
        NetworkGenome parent2,
        NetworkGenome structureSource,
        Random random)
    {
        var childLayers = new List<DenseLayerConfig>();

        // Use structure source's layer count
        int layerCount = structureSource.HiddenLayers.Count;

        for (int i = 0; i < layerCount; i++)
        {
            DenseLayerConfig sourceLayer;

            // If both parents have this layer, randomly choose
            if (i < parent1.HiddenLayers.Count && i < parent2.HiddenLayers.Count)
            {
                sourceLayer = random.NextDouble() < 0.5
                    ? parent1.HiddenLayers[i]
                    : parent2.HiddenLayers[i];
            }
            else if (i < parent1.HiddenLayers.Count)
            {
                sourceLayer = parent1.HiddenLayers[i];
            }
            else if (i < parent2.HiddenLayers.Count)
            {
                sourceLayer = parent2.HiddenLayers[i];
            }
            else
            {
                sourceLayer = structureSource.HiddenLayers[i];
            }

            childLayers.Add(new DenseLayerConfig
            {
                NeuronCount = sourceLayer.NeuronCount,
                Activation = sourceLayer.Activation,
                DropoutRate = sourceLayer.DropoutRate
            });
        }

        return childLayers;
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

        return true;
    }

    /// <summary>
    /// Inherits weights from the parent with the best matching structure.
    /// Weights that don't have a matching parent weight are randomly initialized.
    /// </summary>
    private void InheritWeightsFromBestMatch(
        NetworkGenome child,
        NetworkGenome parent1,
        NetworkGenome parent2,
        Random random)
    {
        var config = NetworkConfig.Default();
        int prevSize = config.FlattenedConvOutputSize;
        int weightIdx = 0;

        for (int layerIdx = 0; layerIdx < child.HiddenLayers.Count; layerIdx++)
        {
            var childLayer = child.HiddenLayers[layerIdx];
            int fanIn = prevSize;
            int fanOut = childLayer.NeuronCount;

            // Find matching layer in parents
            var p1Layer = layerIdx < parent1.HiddenLayers.Count ? parent1.HiddenLayers[layerIdx] : null;
            var p2Layer = layerIdx < parent2.HiddenLayers.Count ? parent2.HiddenLayers[layerIdx] : null;

            // Initialize layer weights
            weightIdx = InitializeLayerFromParents(
                child.Weights, weightIdx, fanIn, fanOut,
                parent1.Weights, parent2.Weights,
                GetLayerWeightOffset(parent1, layerIdx, config.FlattenedConvOutputSize),
                GetLayerWeightOffset(parent2, layerIdx, config.FlattenedConvOutputSize),
                p1Layer, p2Layer, random);

            prevSize = fanOut;
        }

        // Output heads
        InitializeOutputHeadFromParents(
            child, parent1, parent2, weightIdx, prevSize, random);
    }

    /// <summary>
    /// Gets the weight offset for a specific layer.
    /// </summary>
    private static int GetLayerWeightOffset(NetworkGenome genome, int layerIndex, int inputSize)
    {
        int offset = 0;
        int prevSize = inputSize;

        for (int i = 0; i < layerIndex && i < genome.HiddenLayers.Count; i++)
        {
            int layerSize = genome.HiddenLayers[i].NeuronCount;
            offset += prevSize * layerSize + layerSize; // weights + biases
            prevSize = layerSize;
        }

        return offset;
    }

    /// <summary>
    /// Initializes weights for a layer from parents or randomly.
    /// </summary>
    private int InitializeLayerFromParents(
        float[] childWeights, int startIdx, int fanIn, int fanOut,
        float[] parent1Weights, float[] parent2Weights,
        int p1Offset, int p2Offset,
        DenseLayerConfig? p1Layer, DenseLayerConfig? p2Layer,
        Random random)
    {
        double std = Math.Sqrt(2.0 / (fanIn + fanOut));
        int idx = startIdx;

        // Copy weights with interpolation where possible
        for (int i = 0; i < fanIn * fanOut; i++)
        {
            bool canUseP1 = p1Layer != null && p1Offset + i < parent1Weights.Length;
            bool canUseP2 = p2Layer != null && p2Offset + i < parent2Weights.Length;

            if (canUseP1 && canUseP2)
            {
                float alpha = (float)random.NextDouble();
                childWeights[idx] = parent1Weights[p1Offset + i] * alpha +
                                   parent2Weights[p2Offset + i] * (1 - alpha);
            }
            else if (canUseP1)
            {
                childWeights[idx] = parent1Weights[p1Offset + i];
            }
            else if (canUseP2)
            {
                childWeights[idx] = parent2Weights[p2Offset + i];
            }
            else
            {
                childWeights[idx] = (float)(NextGaussian(random) * std);
            }
            idx++;
        }

        // Biases
        int biasOffsetP1 = p1Offset + (p1Layer?.NeuronCount ?? 0) * fanIn;
        int biasOffsetP2 = p2Offset + (p2Layer?.NeuronCount ?? 0) * fanIn;

        for (int i = 0; i < fanOut; i++)
        {
            bool canUseP1 = p1Layer != null && biasOffsetP1 + i < parent1Weights.Length;
            bool canUseP2 = p2Layer != null && biasOffsetP2 + i < parent2Weights.Length;

            if (canUseP1 && canUseP2)
            {
                float alpha = (float)random.NextDouble();
                childWeights[idx] = parent1Weights[biasOffsetP1 + i] * alpha +
                                   parent2Weights[biasOffsetP2 + i] * (1 - alpha);
            }
            else if (canUseP1)
            {
                childWeights[idx] = parent1Weights[biasOffsetP1 + i];
            }
            else if (canUseP2)
            {
                childWeights[idx] = parent2Weights[biasOffsetP2 + i];
            }
            else
            {
                childWeights[idx] = 0f;
            }
            idx++;
        }

        return idx;
    }

    /// <summary>
    /// Initializes output head weights from parents.
    /// </summary>
    private void InitializeOutputHeadFromParents(
        NetworkGenome child,
        NetworkGenome parent1,
        NetworkGenome parent2,
        int startIdx,
        int prevSize,
        Random random)
    {
        var config = NetworkConfig.Default();

        // Use the simpler approach: interpolate with arithmetic crossover
        // if weight arrays are compatible, otherwise use Xavier init
        int totalChildWeights = child.TotalWeightCount(config.FlattenedConvOutputSize);
        int remainingWeights = totalChildWeights - startIdx;

        if (remainingWeights > 0)
        {
            double std = Math.Sqrt(2.0 / (prevSize + child.MouseHead.NeuronCount));

            // Mouse head
            for (int i = 0; i < prevSize * 2 + 2; i++)
            {
                if (startIdx + i < totalChildWeights)
                {
                    child.Weights[startIdx + i] = (float)(NextGaussian(random) * std);
                }
            }
            startIdx += prevSize * 2 + 2;

            // Action head
            std = Math.Sqrt(2.0 / (prevSize + child.ActionHead.NeuronCount));
            for (int i = 0; i < prevSize * child.ActionHead.NeuronCount + child.ActionHead.NeuronCount; i++)
            {
                if (startIdx + i < totalChildWeights)
                {
                    child.Weights[startIdx + i] = (float)(NextGaussian(random) * std);
                }
            }
        }
    }

    private static double NextGaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
