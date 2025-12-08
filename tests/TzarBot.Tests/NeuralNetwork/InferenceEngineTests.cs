using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.NeuralNetwork.Inference;
using TzarBot.NeuralNetwork.Models;
using TzarBot.NeuralNetwork.Onnx;

namespace TzarBot.Tests.NeuralNetwork;

/// <summary>
/// Tests for OnnxInferenceEngine and ActionDecoder.
/// </summary>
public class InferenceEngineTests
{
    private readonly NetworkConfig _config = NetworkConfig.Default();

    #region ActionDecoder Tests

    [Fact]
    public void ActionDecoder_Decode_ReturnsValidAction()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0.5f, -0.3f };
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 2); // LeftClick

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput);

        // Assert
        action.Should().NotBeNull();
        action.Type.Should().Be(ActionType.LeftClick);
        action.MouseDeltaX.Should().BeApproximately(0.5f, 0.001f);
        action.MouseDeltaY.Should().BeApproximately(-0.3f, 0.001f);
        action.Confidence.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void ActionDecoder_Decode_NoneAction()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0f, 0f };
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 0); // None

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput);

        // Assert
        action.Type.Should().Be(ActionType.None);
    }

    [Fact]
    public void ActionDecoder_Decode_MouseMove()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 1f, 1f }; // Max movement
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 1); // MouseMove

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput);

        // Assert
        action.Type.Should().Be(ActionType.MouseMove);
        action.MouseDeltaX.Should().Be(1f);
        action.MouseDeltaY.Should().Be(1f);
    }

    [Fact]
    public void ActionDecoder_Decode_HotkeyWithNumber()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0f, 0f };
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 10); // Hotkey 3

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput);

        // Assert
        action.Type.Should().Be(ActionType.Hotkey);
        action.HotkeyNumber.Should().Be(2); // Index 10 = Hotkey 3 (0-indexed)
    }

    [Fact]
    public void ActionDecoder_Decode_CtrlHotkeyWithNumber()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0f, 0f };
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 18); // Ctrl+1

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput);

        // Assert
        action.Type.Should().Be(ActionType.HotkeyCtrl);
        action.HotkeyNumber.Should().Be(0); // Index 18 = Ctrl+1 (0-indexed)
    }

    [Fact]
    public void ActionDecoder_Decode_LowConfidence_ReturnsNone()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0f, 0f };
        // Create uniform distribution (all ~0.033, below threshold)
        var actionOutput = new float[30];
        Array.Fill(actionOutput, 1f / 30f);

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput);

        // Assert
        action.Type.Should().Be(ActionType.None);
    }

    [Fact]
    public void ActionDecoder_Decode_WrongMouseOutputSize_Throws()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0.5f }; // Wrong size
        var actionOutput = new float[30];

        // Act & Assert
        var act = () => decoder.Decode(mouseOutput, actionOutput);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Mouse output must have 2 elements*");
    }

    [Fact]
    public void ActionDecoder_Decode_SetsTimestamp()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var mouseOutput = new[] { 0f, 0f };
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 0);
        var beforeTime = DateTime.UtcNow;

        // Act
        var action = decoder.Decode(mouseOutput, actionOutput, frameId: 42);

        // Assert
        action.Timestamp.Should().BeOnOrAfter(beforeTime);
        action.SourceFrameId.Should().Be(42);
    }

    [Fact]
    public void ActionDecoder_ScaleMouseToPixels_CorrectScaling()
    {
        // Act
        var (dx1, dy1) = ActionDecoder.ScaleMouseToPixels(1f, 1f);
        var (dx2, dy2) = ActionDecoder.ScaleMouseToPixels(-1f, -1f);
        var (dx3, dy3) = ActionDecoder.ScaleMouseToPixels(0f, 0f);

        // Assert
        dx1.Should().Be(ActionDecoder.MaxMouseDelta);
        dy1.Should().Be(ActionDecoder.MaxMouseDelta);
        dx2.Should().Be(-ActionDecoder.MaxMouseDelta);
        dy2.Should().Be(-ActionDecoder.MaxMouseDelta);
        dx3.Should().Be(0);
        dy3.Should().Be(0);
    }

    [Fact]
    public void ActionDecoder_GetActionProbabilities_ReturnsDictionary()
    {
        // Arrange
        var decoder = new ActionDecoder();
        var actionOutput = CreateSoftmaxOutput(30, maxIndex: 2);

        // Act
        var probs = decoder.GetActionProbabilities(actionOutput);

        // Assert
        probs.Should().ContainKey(ActionType.LeftClick);
        probs[ActionType.LeftClick].Should().BeGreaterThan(0.5f);
    }

    #endregion

    #region OnnxInferenceEngine Tests

    [Fact]
    public void OnnxInferenceEngine_Constructor_LoadsModel()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        // Act
        using var engine = new OnnxInferenceEngine(modelData);

        // Assert
        engine.Should().NotBeNull();
        engine.IsGpuEnabled.Should().BeFalse(); // Default is CPU
        engine.Config.Should().NotBeNull();
    }

    [Fact]
    public void OnnxInferenceEngine_Infer_ReturnsAction()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input = CreateRandomInput(_config);

        // Act
        var action = engine.Infer(input);

        // Assert
        action.Should().NotBeNull();
        action.Confidence.Should().BeGreaterThanOrEqualTo(0f);
        action.Confidence.Should().BeLessThanOrEqualTo(1f);
        action.MouseDeltaX.Should().BeInRange(-1f, 1f);
        action.MouseDeltaY.Should().BeInRange(-1f, 1f);
    }

    [Fact]
    public void OnnxInferenceEngine_InferRaw_ReturnsTwoOutputs()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input = CreateRandomInput(_config);

        // Act
        var (mouseOutput, actionOutput) = engine.InferRaw(input);

        // Assert
        mouseOutput.Should().HaveCount(2);
        actionOutput.Should().HaveCount(_config.ActionCount);

        // Mouse output should be in [-1, 1] (Tanh activation)
        mouseOutput.Should().AllSatisfy(v => v.Should().BeInRange(-1f, 1f));

        // Action output should be probabilities (Softmax)
        actionOutput.Sum().Should().BeApproximately(1.0f, 0.001f);
        actionOutput.Should().AllSatisfy(v => v.Should().BeGreaterThanOrEqualTo(0f));
    }

    [Fact]
    public void OnnxInferenceEngine_Infer_WrongInputSize_Throws()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var wrongInput = new float[100]; // Wrong size

        // Act & Assert
        var act = () => engine.Infer(wrongInput);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Input size mismatch*");
    }

    [Fact]
    public void OnnxInferenceEngine_TracksInferenceTime()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input = CreateRandomInput(_config);

        // Act
        engine.Infer(input);
        var time1 = engine.LastInferenceTime;

        engine.Infer(input);
        var time2 = engine.LastInferenceTime;

        // Assert
        time1.Should().BeGreaterThan(TimeSpan.Zero);
        time2.Should().BeGreaterThan(TimeSpan.Zero);
        engine.AverageInferenceTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void OnnxInferenceEngine_DifferentInputs_DifferentOutputs()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input1 = CreateRandomInput(_config, seed: 1);
        var input2 = CreateRandomInput(_config, seed: 2);

        // Act
        var (mouse1, action1) = engine.InferRaw(input1);
        var (mouse2, action2) = engine.InferRaw(input2);

        // Assert
        mouse1.Should().NotBeEquivalentTo(mouse2);
    }

    [Fact]
    public void OnnxInferenceEngine_SameInput_SameOutput()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input = CreateRandomInput(_config, seed: 42);

        // Act
        var (mouse1, action1) = engine.InferRaw(input);
        var (mouse2, action2) = engine.InferRaw(input);

        // Assert - deterministic inference
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
    public void OnnxInferenceEngine_GetModelInfo_ReturnsInfo()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        // Act
        var info = engine.GetModelInfo();

        // Assert
        info.InputName.Should().Be("input");
        info.InputShape.Should().BeEquivalentTo(new[] { 1, 4, 135, 240 });
        info.MouseOutputSize.Should().Be(2);
        info.ActionOutputSize.Should().Be(30);
        info.IsGpuEnabled.Should().BeFalse();
    }

    [Fact]
    public void OnnxInferenceEngine_Dispose_PreventsUse()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        var engine = new OnnxInferenceEngine(modelData);
        var input = CreateRandomInput(_config);

        // Act
        engine.Dispose();

        // Assert
        var act = () => engine.Infer(input);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void OnnxInferenceEngine_Dispose_MultipleTimesNoError()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        var engine = new OnnxInferenceEngine(modelData);

        // Act & Assert - should not throw
        engine.Dispose();
        engine.Dispose();
        engine.Dispose();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void OnnxInferenceEngine_InferenceTime_Under100ms()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input = CreateRandomInput(_config);

        // Warmup
        for (int i = 0; i < 5; i++)
        {
            engine.Infer(input);
        }

        // Act - measure 20 inferences
        for (int i = 0; i < 20; i++)
        {
            engine.Infer(input);
        }

        // Assert
        engine.AverageInferenceTime.TotalMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void OnnxInferenceEngine_Benchmark_ReturnsReasonableTime()
    {
        // Arrange
        var genome = NetworkGenome.CreateRandom(new[] { 128 }, seed: 42);
        var builder = new OnnxNetworkBuilder();
        var modelData = builder.Build(genome);

        using var engine = new OnnxInferenceEngine(modelData);

        var input = CreateRandomInput(_config);

        // Act
        var avgTime = engine.Benchmark(input, iterations: 50);

        // Assert
        avgTime.TotalMilliseconds.Should().BeGreaterThan(0);
        avgTime.TotalMilliseconds.Should().BeLessThan(200);
    }

    #endregion

    #region Helper Methods

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

    private static float[] CreateSoftmaxOutput(int size, int maxIndex, float maxValue = 0.9f)
    {
        var output = new float[size];
        float otherValue = (1f - maxValue) / (size - 1);

        for (int i = 0; i < size; i++)
        {
            output[i] = i == maxIndex ? maxValue : otherValue;
        }

        return output;
    }

    #endregion
}
