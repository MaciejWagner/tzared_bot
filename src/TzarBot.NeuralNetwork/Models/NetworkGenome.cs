using MessagePack;

namespace TzarBot.NeuralNetwork.Models;

/// <summary>
/// Represents the complete genome of a neural network for genetic algorithm evolution.
///
/// Architecture:
/// - ConvLayers: Frozen feature extraction layers (not evolved)
/// - HiddenLayers: Evolved dense layers (topology and weights)
/// - OutputHeads: Fixed output structure (mouse dx/dy + action selection)
/// - Weights: Flat vector containing all trainable parameters
///
/// The genome encodes both the network topology (through HiddenLayers config)
/// and the weights. This allows the GA to evolve both structure and parameters.
/// </summary>
[MessagePackObject]
public sealed class NetworkGenome
{
    #region Network Structure

    /// <summary>
    /// Convolutional layer configurations (frozen, not evolved).
    /// These are shared across all genomes in a population.
    /// </summary>
    [Key(0)]
    public ConvLayerConfig[] ConvLayers { get; set; } = Array.Empty<ConvLayerConfig>();

    /// <summary>
    /// Hidden dense layer configurations (evolved by GA).
    /// The GA can mutate neuron counts, activations, and dropout rates.
    /// </summary>
    [Key(1)]
    public List<DenseLayerConfig> HiddenLayers { get; set; } = new();

    /// <summary>
    /// Mouse position output head configuration.
    /// Fixed: 2 neurons with Tanh activation.
    /// </summary>
    [Key(2)]
    public DenseLayerConfig MouseHead { get; set; } = DenseLayerConfig.CreateMouseOutput();

    /// <summary>
    /// Action selection output head configuration.
    /// Fixed: N neurons with Softmax activation.
    /// </summary>
    [Key(3)]
    public DenseLayerConfig ActionHead { get; set; } = DenseLayerConfig.CreateActionOutput(30);

    #endregion

    #region Weights

    /// <summary>
    /// Flat array of all trainable weights in the network.
    /// Layout: [hidden_layer_0_weights, hidden_layer_0_biases, ...,
    ///          mouse_head_weights, mouse_head_biases,
    ///          action_head_weights, action_head_biases]
    ///
    /// Weight matrices are stored in row-major order.
    /// </summary>
    [Key(4)]
    public float[] Weights { get; set; } = Array.Empty<float>();

    #endregion

    #region Metadata

    /// <summary>
    /// Unique identifier for this genome.
    /// </summary>
    [Key(5)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Generation number when this genome was created.
    /// </summary>
    [Key(6)]
    public int Generation { get; set; }

    /// <summary>
    /// Fitness score from the last evaluation.
    /// Higher is better.
    /// </summary>
    [Key(7)]
    public float Fitness { get; set; }

    /// <summary>
    /// Parent genome IDs (for tracking lineage).
    /// Empty for initial random genomes.
    /// </summary>
    [Key(8)]
    public Guid[] ParentIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Timestamp when this genome was created.
    /// </summary>
    [Key(9)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of games played for fitness evaluation.
    /// </summary>
    [Key(10)]
    public int GamesPlayed { get; set; }

    /// <summary>
    /// Number of wins in evaluation games.
    /// </summary>
    [Key(11)]
    public int Wins { get; set; }

    #endregion

    #region Weight Calculation

    /// <summary>
    /// Calculates the total number of weights needed for this network topology.
    /// Includes weights and biases for all hidden layers and output heads.
    /// </summary>
    /// <param name="flattenedInputSize">Size of flattened conv output (input to first dense layer)</param>
    public int TotalWeightCount(int flattenedInputSize)
    {
        int count = 0;
        int prevSize = flattenedInputSize;

        // Hidden layers: weights + biases
        foreach (var layer in HiddenLayers)
        {
            count += prevSize * layer.NeuronCount; // weights
            count += layer.NeuronCount; // biases
            prevSize = layer.NeuronCount;
        }

        // Mouse head: weights + biases
        count += prevSize * MouseHead.NeuronCount;
        count += MouseHead.NeuronCount;

        // Action head: weights + biases
        count += prevSize * ActionHead.NeuronCount;
        count += ActionHead.NeuronCount;

        return count;
    }

    /// <summary>
    /// Calculates weight count using default network config.
    /// </summary>
    public int TotalWeightCount()
    {
        var config = NetworkConfig.Default();
        return TotalWeightCount(config.FlattenedConvOutputSize);
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a random genome with specified hidden layers and Xavier-initialized weights.
    ///
    /// Xavier initialization: weights ~ N(0, sqrt(2 / (fan_in + fan_out)))
    /// This helps maintain variance across layers and improves training stability.
    /// </summary>
    /// <param name="hiddenLayerSizes">Number of neurons in each hidden layer</param>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <param name="config">Network configuration (null = default)</param>
    public static NetworkGenome CreateRandom(
        int[] hiddenLayerSizes,
        int seed,
        NetworkConfig? config = null)
    {
        config ??= NetworkConfig.Default();
        var random = new Random(seed);

        // Create hidden layer configs
        var hiddenLayers = hiddenLayerSizes
            .Select(size => DenseLayerConfig.CreateHidden(size))
            .ToList();

        // Create output heads
        var (mouseHead, actionHead) = config.CreateOutputHeads();

        var genome = new NetworkGenome
        {
            ConvLayers = config.ConvLayers,
            HiddenLayers = hiddenLayers,
            MouseHead = mouseHead,
            ActionHead = actionHead,
            Generation = 0,
            Fitness = 0f
        };

        // Initialize weights with Xavier initialization
        int totalWeights = genome.TotalWeightCount(config.FlattenedConvOutputSize);
        genome.Weights = new float[totalWeights];

        int weightIndex = 0;
        int prevSize = config.FlattenedConvOutputSize;

        // Initialize hidden layer weights
        foreach (var layer in genome.HiddenLayers)
        {
            weightIndex = InitializeLayerWeights(
                genome.Weights, weightIndex, prevSize, layer.NeuronCount, random);
            prevSize = layer.NeuronCount;
        }

        // Initialize mouse head weights
        weightIndex = InitializeLayerWeights(
            genome.Weights, weightIndex, prevSize, mouseHead.NeuronCount, random);

        // Initialize action head weights
        InitializeLayerWeights(
            genome.Weights, weightIndex, prevSize, actionHead.NeuronCount, random);

        return genome;
    }

    /// <summary>
    /// Initializes weights for a single layer using Xavier initialization.
    /// Returns the next weight index after this layer.
    /// </summary>
    private static int InitializeLayerWeights(
        float[] weights, int startIndex, int fanIn, int fanOut, Random random)
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
        double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] to avoid log(0)
        double u2 = random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Creates a deep clone of this genome with a new ID.
    /// </summary>
    public NetworkGenome Clone()
    {
        return new NetworkGenome
        {
            ConvLayers = ConvLayers.ToArray(),
            HiddenLayers = HiddenLayers
                .Select(l => new DenseLayerConfig
                {
                    NeuronCount = l.NeuronCount,
                    Activation = l.Activation,
                    DropoutRate = l.DropoutRate
                })
                .ToList(),
            MouseHead = new DenseLayerConfig
            {
                NeuronCount = MouseHead.NeuronCount,
                Activation = MouseHead.Activation,
                DropoutRate = MouseHead.DropoutRate
            },
            ActionHead = new DenseLayerConfig
            {
                NeuronCount = ActionHead.NeuronCount,
                Activation = ActionHead.Activation,
                DropoutRate = ActionHead.DropoutRate
            },
            Weights = (float[])Weights.Clone(),
            Id = Guid.NewGuid(),
            Generation = Generation,
            Fitness = Fitness,
            ParentIds = new[] { Id },
            CreatedAt = DateTime.UtcNow,
            GamesPlayed = 0,
            Wins = 0
        };
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates that the genome is internally consistent.
    /// </summary>
    public bool IsValid(int? flattenedInputSize = null)
    {
        flattenedInputSize ??= NetworkConfig.Default().FlattenedConvOutputSize;

        // Check hidden layers
        if (HiddenLayers.Any(l => !l.IsValid()))
            return false;

        // Check weight count matches topology
        int expectedWeights = TotalWeightCount(flattenedInputSize.Value);
        if (Weights.Length != expectedWeights)
            return false;

        // Check for NaN or Inf in weights
        if (Weights.Any(w => float.IsNaN(w) || float.IsInfinity(w)))
            return false;

        return true;
    }

    #endregion

    public override string ToString()
    {
        var layers = string.Join(" -> ", HiddenLayers.Select(l => l.NeuronCount));
        return $"Genome[{Id:N8}] Gen={Generation} Fit={Fitness:F2} " +
               $"Layers=[{layers}] Weights={Weights.Length:N0}";
    }
}
