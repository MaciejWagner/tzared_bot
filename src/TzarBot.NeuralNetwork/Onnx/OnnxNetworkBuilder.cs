using System.Diagnostics;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.NeuralNetwork.Onnx;

/// <summary>
/// Builds ONNX neural network models from NetworkGenome.
///
/// Architecture:
/// - Input: 4x135x240 (channels x height x width) - NCHW format
/// - Conv1: 32 filters, 8x8, stride 4, ReLU -> 32x32x59
/// - Conv2: 64 filters, 4x4, stride 2, ReLU -> 64x15x28
/// - Conv3: 64 filters, 3x3, stride 1, ReLU -> 64x13x26
/// - Flatten: 21632 neurons
/// - Hidden layers: dynamic from genome (64-1024 neurons each), ReLU
/// - Output Head 1 (MousePosition): 2 neurons, Tanh (dx, dy in [-1, 1])
/// - Output Head 2 (ActionType): N neurons, Softmax (action probabilities)
///
/// Conv layers use Xavier-initialized weights (fixed, not from genome).
/// Hidden and output layers use weights from genome.
/// </summary>
public sealed class OnnxNetworkBuilder
{
    private readonly NetworkConfig _config;
    private readonly int _convWeightSeed;
    private Random _convWeightRng;

    // ONNX opset version (13 supports all operations we need)
    private const int OpsetVersion = 13;

    /// <summary>
    /// Creates a new ONNX network builder.
    /// </summary>
    /// <param name="config">Network configuration (null = default)</param>
    /// <param name="convWeightSeed">Seed for conv layer weight initialization (for reproducibility)</param>
    public OnnxNetworkBuilder(NetworkConfig? config = null, int convWeightSeed = 42)
    {
        _config = config ?? NetworkConfig.Default();
        _convWeightSeed = convWeightSeed;
        _convWeightRng = new Random(convWeightSeed);
    }

    /// <summary>
    /// Builds an ONNX model from the given genome.
    /// </summary>
    /// <param name="genome">Network genome containing topology and weights</param>
    /// <returns>ONNX model as byte array</returns>
    public byte[] Build(NetworkGenome genome)
    {
        // Reset RNG to ensure deterministic conv weights for each Build() call
        // This is critical for serialization tests - same genome must produce identical models
        _convWeightRng = new Random(_convWeightSeed);

        ValidateGenome(genome);

        var graph = new OnnxGraphBuilder("TzarBotNetwork");

        // Track current tensor name and size through the network
        string currentTensor = "input";
        int currentSize = _config.FlattenedConvOutputSize;

        // Build input - ONNX uses NCHW format: batch x channels x height x width
        graph.AddInput("input", new[] { 1L, _config.InputChannels, _config.InputHeight, _config.InputWidth });

        // Build convolutional layers (frozen, Xavier-initialized)
        currentTensor = BuildConvLayers(graph, currentTensor);

        // Flatten after conv layers
        currentTensor = graph.AddFlatten(currentTensor, "flatten_output");

        // Build hidden layers from genome
        int weightIndex = 0;
        int prevSize = currentSize;

        for (int i = 0; i < genome.HiddenLayers.Count; i++)
        {
            var layer = genome.HiddenLayers[i];
            string layerName = $"hidden_{i}";

            // Extract weights and biases for this layer
            int weightCount = prevSize * layer.NeuronCount;
            int biasCount = layer.NeuronCount;

            var weights = ExtractWeights(genome.Weights, ref weightIndex, weightCount);
            var biases = ExtractWeights(genome.Weights, ref weightIndex, biasCount);

            // Add dense layer: MatMul + Add + Activation
            currentTensor = graph.AddDenseLayer(
                currentTensor,
                layerName,
                prevSize,
                layer.NeuronCount,
                weights,
                biases,
                layer.Activation);

            prevSize = layer.NeuronCount;
        }

        // Build output heads - both share the same input from last hidden layer
        string lastHiddenOutput = currentTensor;

        // Mouse position head (2 neurons, Tanh)
        int mouseWeightCount = prevSize * genome.MouseHead.NeuronCount;
        int mouseBiasCount = genome.MouseHead.NeuronCount;
        var mouseWeights = ExtractWeights(genome.Weights, ref weightIndex, mouseWeightCount);
        var mouseBiases = ExtractWeights(genome.Weights, ref weightIndex, mouseBiasCount);

        string mouseHeadOutput = graph.AddDenseLayer(
            lastHiddenOutput,
            "mouse_head",
            prevSize,
            genome.MouseHead.NeuronCount,
            mouseWeights,
            mouseBiases,
            genome.MouseHead.Activation);

        // Rename mouse output to user-friendly name using Identity
        string mouseOutput = graph.AddIdentity(mouseHeadOutput, "mouse_position");

        // Action type head (N neurons, Softmax)
        int actionWeightCount = prevSize * genome.ActionHead.NeuronCount;
        int actionBiasCount = genome.ActionHead.NeuronCount;
        var actionWeights = ExtractWeights(genome.Weights, ref weightIndex, actionWeightCount);
        var actionBiases = ExtractWeights(genome.Weights, ref weightIndex, actionBiasCount);

        string actionHeadOutput = graph.AddDenseLayer(
            lastHiddenOutput,
            "action_head",
            prevSize,
            genome.ActionHead.NeuronCount,
            actionWeights,
            actionBiases,
            genome.ActionHead.Activation);

        // Rename action output to user-friendly name using Identity
        string actionOutput = graph.AddIdentity(actionHeadOutput, "action_type");

        // Verify all weights were used
        Debug.Assert(weightIndex == genome.Weights.Length,
            $"Weight mismatch: used {weightIndex}, expected {genome.Weights.Length}");

        // Define outputs with the renamed tensor names
        graph.AddOutput("mouse_position", mouseOutput, new[] { 1L, genome.MouseHead.NeuronCount });
        graph.AddOutput("action_type", actionOutput, new[] { 1L, genome.ActionHead.NeuronCount });

        return graph.Build(OpsetVersion);
    }

    /// <summary>
    /// Builds the convolutional layers (frozen, Xavier-initialized).
    /// </summary>
    private string BuildConvLayers(OnnxGraphBuilder graph, string inputTensor)
    {
        string currentTensor = inputTensor;
        int currentChannels = _config.InputChannels;

        for (int i = 0; i < _config.ConvLayers.Length; i++)
        {
            var conv = _config.ConvLayers[i];
            string layerName = $"conv_{i}";

            // Xavier initialization for conv weights
            // Shape: [output_channels, input_channels, kernel_h, kernel_w]
            var weights = InitializeConvWeights(
                conv.FilterCount,
                currentChannels,
                conv.KernelSize,
                conv.KernelSize);

            var biases = new float[conv.FilterCount]; // Zero-initialized biases

            currentTensor = graph.AddConv2D(
                currentTensor,
                layerName,
                currentChannels,
                conv.FilterCount,
                conv.KernelSize,
                conv.Stride,
                conv.Padding,
                weights,
                biases,
                conv.Activation);

            currentChannels = conv.FilterCount;
        }

        return currentTensor;
    }

    /// <summary>
    /// Initializes convolutional layer weights using Xavier initialization.
    /// Xavier: weights ~ N(0, sqrt(2 / (fan_in + fan_out)))
    /// where fan_in = in_channels * kernel_h * kernel_w
    /// and fan_out = out_channels * kernel_h * kernel_w
    /// </summary>
    private float[] InitializeConvWeights(int outChannels, int inChannels, int kernelH, int kernelW)
    {
        int fanIn = inChannels * kernelH * kernelW;
        int fanOut = outChannels * kernelH * kernelW;
        double std = Math.Sqrt(2.0 / (fanIn + fanOut));

        int totalWeights = outChannels * inChannels * kernelH * kernelW;
        var weights = new float[totalWeights];

        for (int i = 0; i < totalWeights; i++)
        {
            weights[i] = (float)(NextGaussian() * std);
        }

        return weights;
    }

    /// <summary>
    /// Box-Muller transform for Gaussian random numbers.
    /// </summary>
    private double NextGaussian()
    {
        double u1 = 1.0 - _convWeightRng.NextDouble();
        double u2 = _convWeightRng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Extracts a slice of weights from the genome's weight array.
    /// </summary>
    private static float[] ExtractWeights(float[] allWeights, ref int index, int count)
    {
        var weights = new float[count];
        Array.Copy(allWeights, index, weights, 0, count);
        index += count;
        return weights;
    }

    /// <summary>
    /// Validates that the genome is compatible with the network configuration.
    /// </summary>
    private void ValidateGenome(NetworkGenome genome)
    {
        if (genome.Weights.Length == 0)
        {
            throw new ArgumentException("Genome has no weights");
        }

        int expectedWeights = genome.TotalWeightCount(_config.FlattenedConvOutputSize);
        if (genome.Weights.Length != expectedWeights)
        {
            throw new ArgumentException(
                $"Weight count mismatch: genome has {genome.Weights.Length}, expected {expectedWeights}");
        }

        if (genome.HiddenLayers.Count == 0)
        {
            throw new ArgumentException("Genome must have at least one hidden layer");
        }

        foreach (var layer in genome.HiddenLayers)
        {
            if (!layer.IsValid())
            {
                throw new ArgumentException($"Invalid hidden layer configuration: {layer}");
            }
        }
    }

    /// <summary>
    /// Calculates the total size of convolutional layer weights (for debugging/info).
    /// </summary>
    public int CalculateConvWeightCount()
    {
        int count = 0;
        int channels = _config.InputChannels;

        foreach (var conv in _config.ConvLayers)
        {
            // Weights: [out_channels, in_channels, kernel_h, kernel_w]
            count += conv.FilterCount * channels * conv.KernelSize * conv.KernelSize;
            // Biases: [out_channels]
            count += conv.FilterCount;
            channels = conv.FilterCount;
        }

        return count;
    }
}
