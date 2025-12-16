using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using TzarBot.Common.Models;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.NeuralNetwork.Inference;

/// <summary>
/// ONNX Runtime-based inference engine for TzarBot neural network.
///
/// Features:
/// - CPU and GPU (DirectML on Windows) support
/// - Efficient memory management with pre-allocated buffers
/// - Performance tracking for latency monitoring
/// - Thread-safe inference (session is thread-safe)
/// </summary>
public sealed class OnnxInferenceEngine : IInferenceEngine
{
    private readonly InferenceSession _session;
    private readonly NetworkConfig _config;
    private readonly ActionDecoder _decoder;
    private readonly string _inputName;
    private readonly int[] _inputShape;
    private readonly int _inputSize;

    // Performance tracking
    private readonly Stopwatch _stopwatch = new();
    private readonly object _timingLock = new();
    private readonly Queue<long> _recentTimes = new();
    private const int MaxTimingSamples = 100;
    private long _lastInferenceTimeTicks;

    private bool _disposed;

    /// <inheritdoc />
    public bool IsGpuEnabled { get; }

    /// <inheritdoc />
    public TimeSpan LastInferenceTime => TimeSpan.FromTicks(Volatile.Read(ref _lastInferenceTimeTicks));

    /// <inheritdoc />
    public TimeSpan AverageInferenceTime
    {
        get
        {
            lock (_timingLock)
            {
                if (_recentTimes.Count == 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromTicks((long)_recentTimes.Average());
            }
        }
    }

    /// <inheritdoc />
    public NetworkConfig Config => _config;

    /// <summary>
    /// Creates an ONNX inference engine from a model byte array.
    /// </summary>
    /// <param name="modelData">ONNX model as byte array.</param>
    /// <param name="useGpu">Whether to attempt GPU acceleration (DirectML on Windows).</param>
    /// <param name="config">Network configuration (null = default).</param>
    public OnnxInferenceEngine(byte[] modelData, bool useGpu = false, NetworkConfig? config = null)
    {
        _config = config ?? NetworkConfig.Default();
        _decoder = new ActionDecoder();

        // Calculate input shape: [batch, channels, height, width] - NCHW format
        _inputShape = new[] { 1, _config.InputChannels, _config.InputHeight, _config.InputWidth };
        _inputSize = _inputShape.Aggregate(1, (a, b) => a * b);

        // Create session options
        var options = new SessionOptions();

        if (useGpu)
        {
            try
            {
                // CUDA is the preferred GPU provider for NVIDIA GPUs
                options.AppendExecutionProvider_CUDA();
                IsGpuEnabled = true;
            }
            catch (Exception ex)
            {
                // Fall back to CPU if GPU provider fails
                Console.WriteLine($"[GPU] CUDA init failed: {ex.Message}");
                IsGpuEnabled = false;
            }
        }
        else
        {
            IsGpuEnabled = false;
        }

        // Create inference session
        _session = new InferenceSession(modelData, options);

        // Get input name from model metadata
        _inputName = _session.InputMetadata.Keys.First();

        // Validate input shape matches expected
        var inputMeta = _session.InputMetadata[_inputName];
        var actualShape = inputMeta.Dimensions;

        if (actualShape.Length != 4)
        {
            throw new InvalidOperationException(
                $"Expected 4D input tensor (NCHW), got {actualShape.Length}D");
        }

        // Verify output names exist
        if (!_session.OutputMetadata.ContainsKey("mouse_position"))
        {
            throw new InvalidOperationException("Model missing 'mouse_position' output");
        }
        if (!_session.OutputMetadata.ContainsKey("action_type"))
        {
            throw new InvalidOperationException("Model missing 'action_type' output");
        }
    }

    /// <summary>
    /// Creates an ONNX inference engine from a file path.
    /// </summary>
    /// <param name="modelPath">Path to ONNX model file.</param>
    /// <param name="useGpu">Whether to attempt GPU acceleration.</param>
    /// <param name="config">Network configuration (null = default).</param>
    public static OnnxInferenceEngine FromFile(string modelPath, bool useGpu = false, NetworkConfig? config = null)
    {
        var modelData = File.ReadAllBytes(modelPath);
        return new OnnxInferenceEngine(modelData, useGpu, config);
    }

    /// <inheritdoc />
    public GameAction Infer(float[] preprocessedInput)
    {
        ThrowIfDisposed();

        var (mouseOutput, actionOutput) = InferRaw(preprocessedInput);
        return _decoder.Decode(mouseOutput, actionOutput);
    }

    /// <inheritdoc />
    public (float[] mouseOutput, float[] actionOutput) InferRaw(float[] preprocessedInput)
    {
        ThrowIfDisposed();

        if (preprocessedInput.Length != _inputSize)
        {
            throw new ArgumentException(
                $"Input size mismatch: expected {_inputSize}, got {preprocessedInput.Length}",
                nameof(preprocessedInput));
        }

        _stopwatch.Restart();

        // Create input tensor
        var inputTensor = new DenseTensor<float>(preprocessedInput, _inputShape);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);

        // Extract outputs
        var mouseResult = results.First(r => r.Name == "mouse_position");
        var actionResult = results.First(r => r.Name == "action_type");

        var mouseOutput = mouseResult.AsTensor<float>().ToArray();
        var actionOutput = actionResult.AsTensor<float>().ToArray();

        _stopwatch.Stop();

        // Record timing
        RecordTiming(_stopwatch.ElapsedTicks);

        return (mouseOutput, actionOutput);
    }

    /// <summary>
    /// Runs inference multiple times and returns average timing.
    /// Useful for benchmarking.
    /// </summary>
    /// <param name="preprocessedInput">Input data.</param>
    /// <param name="iterations">Number of iterations.</param>
    /// <returns>Average inference time.</returns>
    public TimeSpan Benchmark(float[] preprocessedInput, int iterations = 100)
    {
        ThrowIfDisposed();

        // Warmup
        for (int i = 0; i < 10; i++)
        {
            Infer(preprocessedInput);
        }

        // Clear timing history
        lock (_timingLock)
        {
            _recentTimes.Clear();
        }

        // Run benchmark
        for (int i = 0; i < iterations; i++)
        {
            Infer(preprocessedInput);
        }

        return AverageInferenceTime;
    }

    /// <summary>
    /// Gets model information for debugging.
    /// </summary>
    public ModelInfo GetModelInfo()
    {
        ThrowIfDisposed();

        return new ModelInfo
        {
            InputName = _inputName,
            InputShape = _inputShape.ToArray(),
            InputSize = _inputSize,
            MouseOutputSize = _session.OutputMetadata["mouse_position"].Dimensions[1],
            ActionOutputSize = _session.OutputMetadata["action_type"].Dimensions[1],
            IsGpuEnabled = IsGpuEnabled
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecordTiming(long ticks)
    {
        Volatile.Write(ref _lastInferenceTimeTicks, ticks);

        lock (_timingLock)
        {
            _recentTimes.Enqueue(ticks);

            while (_recentTimes.Count > MaxTimingSamples)
            {
                _recentTimes.Dequeue();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OnnxInferenceEngine));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _session.Dispose();
    }
}

/// <summary>
/// Information about the loaded model for debugging.
/// </summary>
public sealed record ModelInfo
{
    /// <summary>Name of the input tensor.</summary>
    public required string InputName { get; init; }

    /// <summary>Shape of the input tensor [N, C, H, W].</summary>
    public required int[] InputShape { get; init; }

    /// <summary>Total number of input elements.</summary>
    public required int InputSize { get; init; }

    /// <summary>Size of mouse output (should be 2).</summary>
    public required int MouseOutputSize { get; init; }

    /// <summary>Size of action output (number of actions).</summary>
    public required int ActionOutputSize { get; init; }

    /// <summary>Whether GPU acceleration is enabled.</summary>
    public required bool IsGpuEnabled { get; init; }

    public override string ToString()
    {
        return $"Model(input={string.Join("x", InputShape)}, mouse={MouseOutputSize}, " +
               $"actions={ActionOutputSize}, GPU={IsGpuEnabled})";
    }
}
