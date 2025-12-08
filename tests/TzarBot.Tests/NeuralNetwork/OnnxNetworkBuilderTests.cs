using FluentAssertions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using TzarBot.NeuralNetwork.Models;
using TzarBot.NeuralNetwork.Onnx;

namespace TzarBot.Tests.NeuralNetwork;

/// <summary>
/// Tests for OnnxNetworkBuilder and OnnxModelExporter.
///
/// Verifies:
/// - Conv layers are built correctly (32, 64, 64 filters)
/// - Dynamic hidden layers from genome work
/// - Output heads: MousePosition (2, Tanh) + ActionType (N, Softmax)
/// - Export to ONNX format works
/// - Model loads in ONNX Runtime
/// </summary>
public class OnnxNetworkBuilderTests
{
    private readonly NetworkConfig _config = NetworkConfig.Default();

    #region Model Building Tests

    [Fact]
    public void Build_WithValidGenome_ReturnsNonEmptyByteArray()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();

        // Act
        var modelData = builder.Build(genome);

        // Assert
        modelData.Should().NotBeNull();
        modelData.Should().NotBeEmpty();
        modelData.Length.Should().BeGreaterThan(1000); // Should be substantial
    }

    [Fact]
    public void Build_WithSingleHiddenLayer_Works()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 512 }, seed: 42);
        var builder = new OnnxNetworkBuilder();

        // Act
        var modelData = builder.Build(genome);

        // Assert
        modelData.Should().NotBeEmpty();
        OnnxModelExporter.ValidateModel(modelData).Should().BeTrue();
    }

    [Fact]
    public void Build_WithMultipleHiddenLayers_Works()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 512, 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();

        // Act
        var modelData = builder.Build(genome);

        // Assert
        modelData.Should().NotBeEmpty();
        OnnxModelExporter.ValidateModel(modelData).Should().BeTrue();
    }

    [Fact]
    public void Build_IsDeterministic_SameGenomeSameSeed()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder1 = new OnnxNetworkBuilder(convWeightSeed: 123);
        var builder2 = new OnnxNetworkBuilder(convWeightSeed: 123);

        // Act
        var modelData1 = builder1.Build(genome);
        var modelData2 = builder2.Build(genome);

        // Assert
        modelData1.Should().BeEquivalentTo(modelData2);
    }

    [Fact]
    public void Build_DifferentConvSeeds_ProduceDifferentModels()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder1 = new OnnxNetworkBuilder(convWeightSeed: 123);
        var builder2 = new OnnxNetworkBuilder(convWeightSeed: 456);

        // Act
        var modelData1 = builder1.Build(genome);
        var modelData2 = builder2.Build(genome);

        // Assert
        modelData1.Should().NotBeEquivalentTo(modelData2);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Build_WithEmptyWeights_ThrowsArgumentException()
    {
        // Arrange
        var genome = new NetworkGenome
        {
            HiddenLayers = new List<DenseLayerConfig> { DenseLayerConfig.CreateHidden(256) },
            Weights = Array.Empty<float>()
        };
        var builder = new OnnxNetworkBuilder();

        // Act & Assert
        var act = () => builder.Build(genome);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*no weights*");
    }

    [Fact]
    public void Build_WithWrongWeightCount_ThrowsArgumentException()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        genome.Weights = new float[100]; // Wrong count
        var builder = new OnnxNetworkBuilder();

        // Act & Assert
        var act = () => builder.Build(genome);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Weight count mismatch*");
    }

    [Fact]
    public void Build_WithNoHiddenLayers_ThrowsArgumentException()
    {
        // Arrange - Need proper weight count for the network structure without hidden layers
        var config = NetworkConfig.Default();
        // With no hidden layers: weights go directly from flatten (21632) to outputs
        // Mouse head: 21632 * 2 + 2 = 43266
        // Action head: 21632 * 30 + 30 = 648990
        // Total: 43266 + 648990 = 692256
        var genome = new NetworkGenome
        {
            HiddenLayers = new List<DenseLayerConfig>(),
            MouseHead = DenseLayerConfig.CreateMouseOutput(),
            ActionHead = DenseLayerConfig.CreateActionOutput(config.ActionCount),
            Weights = new float[692256] // Exact count for this topology
        };
        var builder = new OnnxNetworkBuilder();

        // Act & Assert
        var act = () => builder.Build(genome);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one hidden layer*");
    }

    #endregion

    #region ONNX Runtime Loading Tests

    [Fact]
    public void Build_ModelLoadsInOnnxRuntime()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();

        // Act
        var modelData = builder.Build(genome);

        // Assert
        using var session = new InferenceSession(modelData);
        session.Should().NotBeNull();
    }

    [Fact]
    public void Build_ModelHasCorrectInputShape()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        // Act
        using var session = new InferenceSession(modelData);
        var inputMeta = session.InputMetadata["input"];

        // Assert
        inputMeta.Should().NotBeNull();
        inputMeta.Dimensions.Should().HaveCount(4); // NCHW
        inputMeta.Dimensions[0].Should().Be(1); // Batch
        inputMeta.Dimensions[1].Should().Be(_config.InputChannels); // Channels (4)
        inputMeta.Dimensions[2].Should().Be(_config.InputHeight); // Height (135)
        inputMeta.Dimensions[3].Should().Be(_config.InputWidth); // Width (240)
    }

    [Fact]
    public void Build_ModelHasCorrectOutputs()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        // Act
        using var session = new InferenceSession(modelData);

        // Assert - Mouse position output
        session.OutputMetadata.Should().ContainKey("mouse_position");
        var mouseMeta = session.OutputMetadata["mouse_position"];
        mouseMeta.Dimensions.Should().HaveCount(2);
        mouseMeta.Dimensions[0].Should().Be(1); // Batch
        mouseMeta.Dimensions[1].Should().Be(2); // dx, dy

        // Assert - Action type output
        session.OutputMetadata.Should().ContainKey("action_type");
        var actionMeta = session.OutputMetadata["action_type"];
        actionMeta.Dimensions.Should().HaveCount(2);
        actionMeta.Dimensions[0].Should().Be(1); // Batch
        actionMeta.Dimensions[1].Should().Be(_config.ActionCount); // 30 actions
    }

    #endregion

    #region Inference Tests

    [Fact]
    public void Build_ModelCanRunInference()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var session = new InferenceSession(modelData);

        // Create input tensor: 1 x 4 x 135 x 240
        var inputShape = new[] { 1, _config.InputChannels, _config.InputHeight, _config.InputWidth };
        var inputData = new float[inputShape.Aggregate(1, (a, b) => a * b)];

        // Fill with random data
        var random = new Random(42);
        for (int i = 0; i < inputData.Length; i++)
        {
            inputData[i] = (float)random.NextDouble();
        }

        var inputTensor = new DenseTensor<float>(inputData, inputShape);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        // Act
        using var results = session.Run(inputs);
        var resultsList = results.ToList();

        // Assert
        resultsList.Should().HaveCount(2);

        // Check mouse position output
        var mouseResult = resultsList.First(r => r.Name == "mouse_position");
        var mouseTensor = mouseResult.AsTensor<float>();
        mouseTensor.Dimensions.ToArray().Should().BeEquivalentTo(new[] { 1, 2 });

        // Tanh output should be in [-1, 1]
        var mouseValues = mouseTensor.ToArray();
        mouseValues.Should().AllSatisfy(v => v.Should().BeInRange(-1f, 1f));

        // Check action type output
        var actionResult = resultsList.First(r => r.Name == "action_type");
        var actionTensor = actionResult.AsTensor<float>();
        actionTensor.Dimensions.ToArray().Should().BeEquivalentTo(new[] { 1, _config.ActionCount });

        // Softmax output should sum to approximately 1
        var actionValues = actionTensor.ToArray();
        actionValues.Sum().Should().BeApproximately(1.0f, 0.001f);
        actionValues.Should().AllSatisfy(v => v.Should().BeGreaterThanOrEqualTo(0f));
    }

    [Fact]
    public void Build_DifferentGenomesProduceDifferentOutputs()
    {
        // Arrange
        var genome1 = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var genome2 = NetworkGenome.CreateRandom(new[] { 256 }, seed: 43);
        var builder = new OnnxNetworkBuilder();

        var modelData1 = builder.Build(genome1);
        var modelData2 = builder.Build(genome2);

        using var session1 = new InferenceSession(modelData1);
        using var session2 = new InferenceSession(modelData2);

        // Create identical input
        var inputShape = new[] { 1, _config.InputChannels, _config.InputHeight, _config.InputWidth };
        var inputData = new float[inputShape.Aggregate(1, (a, b) => a * b)];
        Array.Fill(inputData, 0.5f);

        var inputTensor = new DenseTensor<float>(inputData, inputShape);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        // Act
        using var results1 = session1.Run(inputs);
        using var results2 = session2.Run(inputs);

        var mouse1 = results1.First(r => r.Name == "mouse_position").AsTensor<float>().ToArray();
        var mouse2 = results2.First(r => r.Name == "mouse_position").AsTensor<float>().ToArray();

        // Assert - Different genomes should produce different outputs
        mouse1.Should().NotBeEquivalentTo(mouse2);
    }

    #endregion

    #region Export Tests

    [Fact]
    public void Export_SavesValidModelToFile()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var exporter = new OnnxModelExporter();
        var tempFile = Path.GetTempFileName() + ".onnx";

        try
        {
            // Act
            exporter.Export(genome, tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            OnnxModelExporter.ValidateModelFile(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportAsync_SavesValidModelToFile()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var exporter = new OnnxModelExporter();
        var tempFile = Path.GetTempFileName() + ".onnx";

        try
        {
            // Act
            await exporter.ExportAsync(genome, tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            OnnxModelExporter.ValidateModelFile(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetModelInfo_ReturnsCorrectInformation()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var exporter = new OnnxModelExporter();
        var modelData = exporter.BuildModel(genome);

        // Act
        var info = OnnxModelExporter.GetModelInfo(modelData);

        // Assert
        info.Inputs.Should().HaveCount(1);
        info.Inputs[0].Name.Should().Be("input");

        info.Outputs.Should().HaveCount(2);
        info.Outputs.Should().Contain(o => o.Name == "mouse_position");
        info.Outputs.Should().Contain(o => o.Name == "action_type");
    }

    #endregion

    #region ConvLayerConfig Verification Tests

    [Fact]
    public void Build_ConvLayersHaveCorrectFilterCounts()
    {
        // This test verifies the architecture matches the specification:
        // Conv1: 32 filters, 8x8, stride 4
        // Conv2: 64 filters, 4x4, stride 2
        // Conv3: 64 filters, 3x3, stride 1

        // Arrange
        var config = NetworkConfig.Default();

        // Assert
        config.ConvLayers.Should().HaveCount(3);
        config.ConvLayers[0].FilterCount.Should().Be(32);
        config.ConvLayers[0].KernelSize.Should().Be(8);
        config.ConvLayers[0].Stride.Should().Be(4);

        config.ConvLayers[1].FilterCount.Should().Be(64);
        config.ConvLayers[1].KernelSize.Should().Be(4);
        config.ConvLayers[1].Stride.Should().Be(2);

        config.ConvLayers[2].FilterCount.Should().Be(64);
        config.ConvLayers[2].KernelSize.Should().Be(3);
        config.ConvLayers[2].Stride.Should().Be(1);
    }

    [Fact]
    public void CalculateConvWeightCount_ReturnsExpectedValue()
    {
        // Arrange
        var builder = new OnnxNetworkBuilder();

        // Calculate expected:
        // Conv1: 4 * 32 * 8 * 8 + 32 = 8192 + 32 = 8224
        // Conv2: 32 * 64 * 4 * 4 + 64 = 32768 + 64 = 32832
        // Conv3: 64 * 64 * 3 * 3 + 64 = 36864 + 64 = 36928
        // Total: 77984
        int expected = (4 * 32 * 8 * 8 + 32) + (32 * 64 * 4 * 4 + 64) + (64 * 64 * 3 * 3 + 64);

        // Act
        var count = builder.CalculateConvWeightCount();

        // Assert
        count.Should().Be(expected);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Build_CompletesInReasonableTime()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 512, 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var modelData = builder.Build(genome);
        sw.Stop();

        // Assert - should complete in under 1 second
        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public void Inference_CompletesInReasonableTime()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var session = new InferenceSession(modelData);

        var inputShape = new[] { 1, _config.InputChannels, _config.InputHeight, _config.InputWidth };
        var inputData = new float[inputShape.Aggregate(1, (a, b) => a * b)];
        var inputTensor = new DenseTensor<float>(inputData, inputShape);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        // Warmup
        using (var _ = session.Run(inputs)) { }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            using var results = session.Run(inputs);
        }
        sw.Stop();

        // Assert - average inference should be under 100ms on CPU
        var avgMs = sw.ElapsedMilliseconds / 10.0;
        avgMs.Should().BeLessThan(100);
    }

    #endregion
}
