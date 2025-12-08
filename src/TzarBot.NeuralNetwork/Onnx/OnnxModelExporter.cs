using Microsoft.ML.OnnxRuntime;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.NeuralNetwork.Onnx;

/// <summary>
/// Exports and validates ONNX models built from NetworkGenome.
///
/// Provides:
/// - Export to file
/// - Validation using ONNX Runtime
/// - Model information retrieval
/// </summary>
public sealed class OnnxModelExporter
{
    private readonly OnnxNetworkBuilder _builder;

    /// <summary>
    /// Creates a new ONNX model exporter.
    /// </summary>
    /// <param name="config">Network configuration (null = default)</param>
    /// <param name="convWeightSeed">Seed for conv layer weight initialization</param>
    public OnnxModelExporter(NetworkConfig? config = null, int convWeightSeed = 42)
    {
        _builder = new OnnxNetworkBuilder(config, convWeightSeed);
    }

    /// <summary>
    /// Exports a genome to an ONNX model file.
    /// </summary>
    /// <param name="genome">Network genome to export</param>
    /// <param name="filePath">Output file path (.onnx)</param>
    public void Export(NetworkGenome genome, string filePath)
    {
        var modelData = _builder.Build(genome);
        File.WriteAllBytes(filePath, modelData);
    }

    /// <summary>
    /// Exports a genome to an ONNX model file asynchronously.
    /// </summary>
    public async Task ExportAsync(NetworkGenome genome, string filePath, CancellationToken cancellationToken = default)
    {
        var modelData = _builder.Build(genome);
        await File.WriteAllBytesAsync(filePath, modelData, cancellationToken);
    }

    /// <summary>
    /// Builds an ONNX model from a genome and returns the raw bytes.
    /// </summary>
    public byte[] BuildModel(NetworkGenome genome)
    {
        return _builder.Build(genome);
    }

    /// <summary>
    /// Validates that a model can be loaded by ONNX Runtime.
    /// </summary>
    /// <param name="modelData">ONNX model as byte array</param>
    /// <returns>True if the model is valid and loadable</returns>
    public static bool ValidateModel(byte[] modelData)
    {
        try
        {
            using var session = new InferenceSession(modelData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a model file can be loaded by ONNX Runtime.
    /// </summary>
    public static bool ValidateModelFile(string filePath)
    {
        try
        {
            using var session = new InferenceSession(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets model information from an ONNX model.
    /// </summary>
    public static OnnxModelInfo GetModelInfo(byte[] modelData)
    {
        using var session = new InferenceSession(modelData);
        return ExtractModelInfo(session);
    }

    /// <summary>
    /// Gets model information from an ONNX model file.
    /// </summary>
    public static OnnxModelInfo GetModelInfo(string filePath)
    {
        using var session = new InferenceSession(filePath);
        return ExtractModelInfo(session);
    }

    private static OnnxModelInfo ExtractModelInfo(InferenceSession session)
    {
        var inputs = session.InputMetadata
            .Select(kvp => new OnnxTensorSpec(
                kvp.Key,
                kvp.Value.Dimensions.Select(d => (long)d).ToArray(),
                kvp.Value.ElementType.ToString()))
            .ToArray();

        var outputs = session.OutputMetadata
            .Select(kvp => new OnnxTensorSpec(
                kvp.Key,
                kvp.Value.Dimensions.Select(d => (long)d).ToArray(),
                kvp.Value.ElementType.ToString()))
            .ToArray();

        return new OnnxModelInfo
        {
            Inputs = inputs,
            Outputs = outputs
        };
    }

    /// <summary>
    /// Creates an inference session for a model.
    /// The caller is responsible for disposing the session.
    /// </summary>
    public static InferenceSession CreateSession(byte[] modelData, SessionOptions? options = null)
    {
        return options != null
            ? new InferenceSession(modelData, options)
            : new InferenceSession(modelData);
    }

    /// <summary>
    /// Creates an inference session from a file.
    /// The caller is responsible for disposing the session.
    /// </summary>
    public static InferenceSession CreateSessionFromFile(string filePath, SessionOptions? options = null)
    {
        return options != null
            ? new InferenceSession(filePath, options)
            : new InferenceSession(filePath);
    }
}

/// <summary>
/// Information about an ONNX model.
/// </summary>
public sealed class OnnxModelInfo
{
    /// <summary>
    /// Input tensor specifications.
    /// </summary>
    public OnnxTensorSpec[] Inputs { get; init; } = Array.Empty<OnnxTensorSpec>();

    /// <summary>
    /// Output tensor specifications.
    /// </summary>
    public OnnxTensorSpec[] Outputs { get; init; } = Array.Empty<OnnxTensorSpec>();

    public override string ToString()
    {
        var inputsStr = string.Join(", ", Inputs.Select(i => i.ToString()));
        var outputsStr = string.Join(", ", Outputs.Select(o => o.ToString()));
        return $"Inputs: [{inputsStr}], Outputs: [{outputsStr}]";
    }
}

/// <summary>
/// Specification for an ONNX tensor (input or output).
/// </summary>
public sealed class OnnxTensorSpec
{
    public string Name { get; }
    public long[] Shape { get; }
    public string DataType { get; }

    public OnnxTensorSpec(string name, long[] shape, string dataType)
    {
        Name = name;
        Shape = shape;
        DataType = dataType;
    }

    public override string ToString()
    {
        return $"{Name}[{string.Join("x", Shape)}] ({DataType})";
    }
}
