using System.Diagnostics;
using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Inference;
using TzarBot.NeuralNetwork.Models;
using TzarBot.NeuralNetwork.Onnx;
using TzarBot.NeuralNetwork.Preprocessing;

namespace TzarBot.Tests.NeuralNetwork;

/// <summary>
/// Phase 2 Integration Tests - Full pipeline from frame to action.
///
/// Tests cover:
/// - Complete pipeline: frame → preprocess → model → infer → action
/// - Genome serialization preserves inference behavior
/// - Performance is acceptable for real-time use
/// - Multiple genomes produce different behaviors
/// </summary>
public class Phase2IntegrationTests
{
    private readonly NetworkConfig _config = NetworkConfig.Default();
    private readonly PreprocessorConfig _preprocessorConfig = PreprocessorConfig.Default();

    #region Full Pipeline Tests

    [Fact]
    public void FullPipeline_Frame_To_Action()
    {
        // Arrange
        var frame = CreateTestFrame(1920, 1080);
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);

        // Build model
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        using var engine = new OnnxInferenceEngine(modelData);

        // Process 4 frames to fill buffer
        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(frame);
        }

        // Act
        var tensor = preprocessor.GetTensor();
        var action = engine.Infer(tensor);

        // Assert
        action.Should().NotBeNull();
        action.MouseDeltaX.Should().BeInRange(-1f, 1f);
        action.MouseDeltaY.Should().BeInRange(-1f, 1f);
        action.Confidence.Should().BeGreaterThanOrEqualTo(0f);
        action.Confidence.Should().BeLessThanOrEqualTo(1f);
    }

    [Fact]
    public void FullPipeline_DifferentFrames_ProduceDifferentActions()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var preprocessor1 = new ImagePreprocessor(_preprocessorConfig);
        using var preprocessor2 = new ImagePreprocessor(_preprocessorConfig);
        using var engine = new OnnxInferenceEngine(modelData);

        // Create different frames
        var frame1 = CreateTestFrame(1920, 1080, intensity: 50);
        var frame2 = CreateTestFrame(1920, 1080, intensity: 200);

        // Process frames
        for (int i = 0; i < 4; i++)
        {
            preprocessor1.ProcessFrame(frame1);
            preprocessor2.ProcessFrame(frame2);
        }

        // Act
        var action1 = engine.Infer(preprocessor1.GetTensor());
        var action2 = engine.Infer(preprocessor2.GetTensor());

        // Assert - Different inputs should produce different outputs
        // (At least mouse deltas should differ for sufficiently different inputs)
        var totalDiff = Math.Abs(action1.MouseDeltaX - action2.MouseDeltaX) +
                        Math.Abs(action1.MouseDeltaY - action2.MouseDeltaY);
        totalDiff.Should().BeGreaterThan(0.001f);
    }

    [Fact]
    public void FullPipeline_MultipleGenomes_DifferentBehavior()
    {
        // Arrange
        var genome1 = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var genome2 = NetworkGenome.CreateRandom(new[] { 256 }, seed: 43);

        var builder = new OnnxNetworkBuilder();

        using var engine1 = new OnnxInferenceEngine(builder.Build(genome1));
        using var engine2 = new OnnxInferenceEngine(builder.Build(genome2));

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);

        var frame = CreateTestFrame(1920, 1080);
        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(frame);
        }

        var tensor = preprocessor.GetTensor();

        // Act
        var action1 = engine1.Infer(tensor);
        var action2 = engine2.Infer(tensor);

        // Assert - Different genomes should produce different outputs
        var (mouse1, _) = engine1.InferRaw(tensor);
        var (mouse2, _) = engine2.InferRaw(tensor);

        mouse1.Should().NotBeEquivalentTo(mouse2);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void Genome_Serialization_PreservesBehavior()
    {
        // Arrange
        var original = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        var frame = CreateTestFrame(1920, 1080);
        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(frame);
        }
        var tensor = preprocessor.GetTensor();

        // Get original action
        using var engine1 = new OnnxInferenceEngine(builder.Build(original));
        var (mouse1, action1) = engine1.InferRaw(tensor);

        // Serialize and deserialize
        var data = GenomeSerializer.Serialize(original);
        var restored = GenomeSerializer.Deserialize(data);

        // Get restored action
        using var engine2 = new OnnxInferenceEngine(builder.Build(restored));
        var (mouse2, action2) = engine2.InferRaw(tensor);

        // Assert - Should produce identical outputs
        for (int i = 0; i < mouse1.Length; i++)
        {
            mouse1[i].Should().BeApproximately(mouse2[i], 1e-5f);
        }
        for (int i = 0; i < action1.Length; i++)
        {
            action1[i].Should().BeApproximately(action2[i], 1e-5f);
        }
    }

    [Fact]
    public void Genome_Clone_PreservesBehavior()
    {
        // Arrange
        var original = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var clone = original.Clone();

        var builder = new OnnxNetworkBuilder();

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        var frame = CreateTestFrame(1920, 1080);
        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(frame);
        }
        var tensor = preprocessor.GetTensor();

        // Act
        using var engine1 = new OnnxInferenceEngine(builder.Build(original));
        using var engine2 = new OnnxInferenceEngine(builder.Build(clone));

        var action1 = engine1.Infer(tensor);
        var action2 = engine2.Infer(tensor);

        // Assert - Clone should behave identically
        action1.MouseDeltaX.Should().BeApproximately(action2.MouseDeltaX, 1e-5f);
        action1.MouseDeltaY.Should().BeApproximately(action2.MouseDeltaY, 1e-5f);
        action1.Type.Should().Be(action2.Type);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Performance_EndToEnd_Under100ms()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        using var engine = new OnnxInferenceEngine(modelData);

        var frame = CreateTestFrame(1920, 1080);

        // Warmup
        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(frame);
        }
        engine.Infer(preprocessor.GetTensor());

        // Act - Measure 100 iterations
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            preprocessor.Reset();
            for (int j = 0; j < 4; j++)
            {
                preprocessor.ProcessFrame(frame);
            }
            engine.Infer(preprocessor.GetTensor());
        }
        sw.Stop();

        // Assert - Average should be under 100ms
        var avgMs = sw.ElapsedMilliseconds / 100.0;
        avgMs.Should().BeLessThan(100);
    }

    [Fact]
    public void Performance_Preprocessing_Under25ms()
    {
        // Arrange
        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        var frame = CreateTestFrame(1920, 1080);

        // Warmup
        preprocessor.ProcessFrame(frame);

        // Act
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            preprocessor.ProcessSingleFrame(frame);
        }
        sw.Stop();

        // Assert - 25ms threshold allows for 40 FPS preprocessing which is sufficient
        // Actual target is 10ms but test environments may be slower
        var avgMs = sw.ElapsedMilliseconds / 100.0;
        avgMs.Should().BeLessThan(25);
    }

    [Fact]
    public void Performance_Inference_Under50ms()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);
        var input = CreateRandomInput(_config);

        // Warmup
        engine.Infer(input);

        // Act
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            engine.Infer(input);
        }
        sw.Stop();

        // Assert
        var avgMs = sw.ElapsedMilliseconds / 100.0;
        avgMs.Should().BeLessThan(50);
    }

    #endregion

    #region Architecture Variations

    [Theory]
    [InlineData(new[] { 128 })]
    [InlineData(new[] { 256 })]
    [InlineData(new[] { 512 })]
    [InlineData(new[] { 256, 128 })]
    [InlineData(new[] { 512, 256, 128 })]
    public void FullPipeline_DifferentArchitectures_Work(int[] hiddenLayers)
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(hiddenLayers, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);
        var input = CreateRandomInput(_config);

        // Act
        var action = engine.Infer(input);

        // Assert
        action.Should().NotBeNull();
        action.Confidence.Should().BeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public void FullPipeline_LargeNetwork_StillWorks()
    {
        // Arrange - larger network
        var genome = NetworkGenome.CreateRandom(new[] { 512, 512, 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);
        var input = CreateRandomInput(_config);

        // Act
        var action = engine.Infer(input);

        // Assert
        action.Should().NotBeNull();

        // Larger network should still be reasonably fast
        engine.LastInferenceTime.TotalMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FullPipeline_AllBlackFrame_Handles()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        using var engine = new OnnxInferenceEngine(modelData);

        var blackFrame = CreateTestFrame(1920, 1080, intensity: 0);

        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(blackFrame);
        }

        // Act
        var action = engine.Infer(preprocessor.GetTensor());

        // Assert - Should not crash, should return valid action
        action.Should().NotBeNull();
        float.IsNaN(action.MouseDeltaX).Should().BeFalse();
        float.IsNaN(action.MouseDeltaY).Should().BeFalse();
    }

    [Fact]
    public void FullPipeline_AllWhiteFrame_Handles()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var preprocessor = new ImagePreprocessor(_preprocessorConfig);
        using var engine = new OnnxInferenceEngine(modelData);

        var whiteFrame = CreateTestFrame(1920, 1080, intensity: 255);

        for (int i = 0; i < 4; i++)
        {
            preprocessor.ProcessFrame(whiteFrame);
        }

        // Act
        var action = engine.Infer(preprocessor.GetTensor());

        // Assert
        action.Should().NotBeNull();
        float.IsNaN(action.MouseDeltaX).Should().BeFalse();
        float.IsNaN(action.MouseDeltaY).Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static ScreenFrame CreateTestFrame(int width, int height, byte intensity = 128)
    {
        var data = new byte[width * height * 4];

        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = intensity;     // B
            data[i + 1] = intensity; // G
            data[i + 2] = intensity; // R
            data[i + 3] = 255;       // A
        }

        return new ScreenFrame
        {
            Data = data,
            Width = width,
            Height = height,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = PixelFormat.BGRA32
        };
    }

    private static float[] CreateRandomInput(NetworkConfig config, int seed = 42)
    {
        var random = new Random(seed);
        var size = config.InputChannels * config.InputHeight * config.InputWidth;
        var input = new float[size];

        for (int i = 0; i < size; i++)
        {
            input[i] = (float)random.NextDouble();
        }

        return input;
    }

    #endregion
}
