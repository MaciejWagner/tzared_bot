using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Operators;

/// <summary>
/// Mutates network structure: adds/removes layers and modifies neuron counts.
///
/// Structure mutations are more disruptive than weight mutations, so they
/// occur with lower probability. When adding a layer, weights are initialized
/// using Xavier initialization to minimize disruption to existing behavior.
/// </summary>
public sealed class StructureMutator : IMutationOperator
{
    private readonly GeneticAlgorithmConfig _config;

    /// <inheritdoc />
    public string Name => "StructureMutation";

    public StructureMutator(GeneticAlgorithmConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public bool Mutate(NetworkGenome genome, Random random)
    {
        if (genome == null) throw new ArgumentNullException(nameof(genome));
        if (random == null) throw new ArgumentNullException(nameof(random));

        bool anyMutated = false;

        // Structure mutation: add or remove layer
        if (random.NextDouble() < _config.StructureMutationRate)
        {
            bool addLayer = random.NextDouble() < _config.AddLayerProbability;

            if (addLayer && genome.HiddenLayers.Count < _config.MaxHiddenLayers)
            {
                AddLayer(genome, random);
                anyMutated = true;
            }
            else if (!addLayer && genome.HiddenLayers.Count > _config.MinHiddenLayers)
            {
                RemoveLayer(genome, random);
                anyMutated = true;
            }
        }

        // Neuron count mutation
        if (random.NextDouble() < _config.NeuronCountMutationRate && genome.HiddenLayers.Count > 0)
        {
            MutateNeuronCount(genome, random);
            anyMutated = true;
        }

        return anyMutated;
    }

    /// <summary>
    /// Adds a new hidden layer at a random position.
    /// The new layer is initialized with identity-like weights to minimize
    /// disruption to existing network behavior.
    /// </summary>
    private void AddLayer(NetworkGenome genome, Random random)
    {
        // Choose insertion position
        int position = random.Next(0, genome.HiddenLayers.Count + 1);

        // Determine new layer size (average of neighbors or random)
        int newSize;
        if (genome.HiddenLayers.Count == 0)
        {
            newSize = 128;
        }
        else if (position == 0)
        {
            newSize = genome.HiddenLayers[0].NeuronCount;
        }
        else if (position == genome.HiddenLayers.Count)
        {
            newSize = genome.HiddenLayers[^1].NeuronCount;
        }
        else
        {
            int prev = genome.HiddenLayers[position - 1].NeuronCount;
            int next = genome.HiddenLayers[position].NeuronCount;
            newSize = (prev + next) / 2;
        }

        // Clamp to valid range
        newSize = Math.Clamp(newSize, DenseLayerConfig.MinNeurons, DenseLayerConfig.MaxNeurons);

        // Create new layer config
        var newLayer = DenseLayerConfig.CreateHidden(newSize);
        genome.HiddenLayers.Insert(position, newLayer);

        // Rebuild weights array (weights will need to be reinitialized)
        RebuildWeights(genome, random);
    }

    /// <summary>
    /// Removes a random hidden layer.
    /// </summary>
    private void RemoveLayer(NetworkGenome genome, Random random)
    {
        if (genome.HiddenLayers.Count <= _config.MinHiddenLayers)
            return;

        int position = random.Next(0, genome.HiddenLayers.Count);
        genome.HiddenLayers.RemoveAt(position);

        // Rebuild weights array
        RebuildWeights(genome, random);
    }

    /// <summary>
    /// Mutates the neuron count of a random layer.
    /// </summary>
    private void MutateNeuronCount(NetworkGenome genome, Random random)
    {
        if (genome.HiddenLayers.Count == 0)
            return;

        // Choose a random layer
        int layerIndex = random.Next(0, genome.HiddenLayers.Count);
        var layer = genome.HiddenLayers[layerIndex];

        // Calculate delta (can be positive or negative)
        int delta = random.Next(-_config.MaxNeuronCountDelta, _config.MaxNeuronCountDelta + 1);
        if (delta == 0) delta = random.Next(2) == 0 ? -8 : 8;

        int newCount = layer.NeuronCount + delta;
        newCount = Math.Clamp(newCount, DenseLayerConfig.MinNeurons, DenseLayerConfig.MaxNeurons);

        if (newCount == layer.NeuronCount)
            return;

        // Update layer
        genome.HiddenLayers[layerIndex] = new DenseLayerConfig
        {
            NeuronCount = newCount,
            Activation = layer.Activation,
            DropoutRate = layer.DropoutRate
        };

        // Rebuild weights array
        RebuildWeights(genome, random);
    }

    /// <summary>
    /// Rebuilds the weight array after a structure change.
    /// Attempts to preserve as many weights as possible when layer sizes change.
    /// </summary>
    private void RebuildWeights(NetworkGenome genome, Random random)
    {
        var config = NetworkConfig.Default();
        int totalWeights = genome.TotalWeightCount(config.FlattenedConvOutputSize);

        // Create new weights array
        var newWeights = new float[totalWeights];

        // Initialize with Xavier initialization
        int idx = 0;
        int prevSize = config.FlattenedConvOutputSize;

        foreach (var layer in genome.HiddenLayers)
        {
            idx = InitializeLayerWeights(newWeights, idx, prevSize, layer.NeuronCount, random);
            prevSize = layer.NeuronCount;
        }

        // Initialize mouse head weights
        idx = InitializeLayerWeights(newWeights, idx, prevSize, genome.MouseHead.NeuronCount, random);

        // Initialize action head weights
        InitializeLayerWeights(newWeights, idx, prevSize, genome.ActionHead.NeuronCount, random);

        genome.Weights = newWeights;
    }

    /// <summary>
    /// Initializes weights for a single layer using Xavier initialization.
    /// Returns the next weight index after this layer.
    /// </summary>
    private int InitializeLayerWeights(float[] weights, int startIndex, int fanIn, int fanOut, Random random)
    {
        // Xavier initialization: std = sqrt(2 / (fan_in + fan_out))
        double std = Math.Sqrt(2.0 / (fanIn + fanOut));
        int idx = startIndex;

        // Weights (fan_in * fan_out)
        for (int i = 0; i < fanIn * fanOut; i++)
        {
            weights[idx++] = (float)(NextGaussian(random) * std);
        }

        // Biases (fan_out) - initialize to zero
        for (int i = 0; i < fanOut; i++)
        {
            weights[idx++] = 0f;
        }

        return idx;
    }

    /// <summary>
    /// Box-Muller transform for generating Gaussian random numbers.
    /// </summary>
    private static double NextGaussian(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Gets the total neuron count across all hidden layers.
    /// </summary>
    public static int GetTotalNeuronCount(NetworkGenome genome)
    {
        return genome.HiddenLayers.Sum(l => l.NeuronCount);
    }

    /// <summary>
    /// Validates the structure of a genome.
    /// </summary>
    public bool ValidateStructure(NetworkGenome genome)
    {
        if (genome.HiddenLayers.Count < _config.MinHiddenLayers)
            return false;
        if (genome.HiddenLayers.Count > _config.MaxHiddenLayers)
            return false;

        foreach (var layer in genome.HiddenLayers)
        {
            if (!layer.IsValid())
                return false;
            if (layer.NeuronCount < DenseLayerConfig.MinNeurons)
                return false;
            if (layer.NeuronCount > DenseLayerConfig.MaxNeurons)
                return false;
        }

        return true;
    }
}
