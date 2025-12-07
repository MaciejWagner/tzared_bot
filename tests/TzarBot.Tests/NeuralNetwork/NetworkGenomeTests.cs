using FluentAssertions;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.NeuralNetwork;

public class NetworkGenomeTests
{
    #region NetworkConfig Tests

    [Fact]
    public void NetworkConfig_Default_HasCorrectInputDimensions()
    {
        var config = NetworkConfig.Default();

        config.StackedFrames.Should().Be(4);
        config.InputWidth.Should().Be(240);
        config.InputHeight.Should().Be(135);
        config.InputChannels.Should().Be(4);
    }

    [Fact]
    public void NetworkConfig_Default_HasThreeConvLayers()
    {
        var config = NetworkConfig.Default();

        config.ConvLayers.Should().HaveCount(3);

        // Layer 1: 32@8x8s4
        config.ConvLayers[0].FilterCount.Should().Be(32);
        config.ConvLayers[0].KernelSize.Should().Be(8);
        config.ConvLayers[0].Stride.Should().Be(4);

        // Layer 2: 64@4x4s2
        config.ConvLayers[1].FilterCount.Should().Be(64);
        config.ConvLayers[1].KernelSize.Should().Be(4);
        config.ConvLayers[1].Stride.Should().Be(2);

        // Layer 3: 64@3x3s1
        config.ConvLayers[2].FilterCount.Should().Be(64);
        config.ConvLayers[2].KernelSize.Should().Be(3);
        config.ConvLayers[2].Stride.Should().Be(1);
    }

    [Fact]
    public void NetworkConfig_FlattenedConvOutputSize_IsCalculatedCorrectly()
    {
        var config = NetworkConfig.Default();

        // Calculate manually:
        // Input: 240x135
        // After conv1 (8x8s4): floor((240-8)/4)+1 = 59, floor((135-8)/4)+1 = 32 -> 32x59x32
        // After conv2 (4x4s2): floor((59-4)/2)+1 = 28, floor((32-4)/2)+1 = 15 -> 64x28x15
        // After conv3 (3x3s1): floor((28-3)/1)+1 = 26, floor((15-3)/1)+1 = 13 -> 64x26x13
        // Flattened: 64 * 26 * 13 = 21,632
        int expected = 64 * 26 * 13;

        config.FlattenedConvOutputSize.Should().Be(expected);
    }

    #endregion

    #region ConvLayerConfig Tests

    [Theory]
    [InlineData(240, 8, 4, 0, 59)]   // First layer width
    [InlineData(135, 8, 4, 0, 32)]   // First layer height
    [InlineData(59, 4, 2, 0, 28)]    // Second layer width
    [InlineData(32, 4, 2, 0, 15)]    // Second layer height
    [InlineData(28, 3, 1, 0, 26)]    // Third layer width
    [InlineData(15, 3, 1, 0, 13)]    // Third layer height
    public void ConvLayerConfig_CalculateOutputSize_IsCorrect(
        int input, int kernel, int stride, int padding, int expectedOutput)
    {
        var conv = ConvLayerConfig.Create(32, kernel, stride, padding);

        conv.CalculateOutputSize(input).Should().Be(expectedOutput);
    }

    #endregion

    #region DenseLayerConfig Tests

    [Fact]
    public void DenseLayerConfig_CreateHidden_ClampsNeuronCount()
    {
        var layer1 = DenseLayerConfig.CreateHidden(10); // Below min
        var layer2 = DenseLayerConfig.CreateHidden(2000); // Above max
        var layer3 = DenseLayerConfig.CreateHidden(256); // Valid

        layer1.NeuronCount.Should().Be(DenseLayerConfig.MinNeurons);
        layer2.NeuronCount.Should().Be(DenseLayerConfig.MaxNeurons);
        layer3.NeuronCount.Should().Be(256);
    }

    [Fact]
    public void DenseLayerConfig_CreateHidden_ClampsDropout()
    {
        var layer1 = DenseLayerConfig.CreateHidden(128, -0.1f);
        var layer2 = DenseLayerConfig.CreateHidden(128, 0.8f);
        var layer3 = DenseLayerConfig.CreateHidden(128, 0.3f);

        layer1.DropoutRate.Should().Be(0f);
        layer2.DropoutRate.Should().Be(DenseLayerConfig.MaxDropout);
        layer3.DropoutRate.Should().BeApproximately(0.3f, 0.001f);
    }

    [Fact]
    public void DenseLayerConfig_CreateMouseOutput_HasCorrectConfig()
    {
        var head = DenseLayerConfig.CreateMouseOutput();

        head.NeuronCount.Should().Be(2);
        head.Activation.Should().Be(ActivationType.Tanh);
        head.DropoutRate.Should().Be(0f);
    }

    [Fact]
    public void DenseLayerConfig_CreateActionOutput_HasCorrectConfig()
    {
        var head = DenseLayerConfig.CreateActionOutput(30);

        head.NeuronCount.Should().Be(30);
        head.Activation.Should().Be(ActivationType.Softmax);
        head.DropoutRate.Should().Be(0f);
    }

    [Fact]
    public void DenseLayerConfig_IsValid_ReturnsTrueForValidConfig()
    {
        var layer = DenseLayerConfig.CreateHidden(128, 0.2f);

        layer.IsValid().Should().BeTrue();
    }

    [Fact]
    public void DenseLayerConfig_IsValid_ReturnsFalseForInvalidDropout()
    {
        var layer = new DenseLayerConfig
        {
            NeuronCount = 128,
            Activation = ActivationType.ReLU,
            DropoutRate = 0.8f // Invalid: > 0.5
        };

        layer.IsValid().Should().BeFalse();
    }

    #endregion

    #region NetworkGenome Creation Tests

    [Fact]
    public void NetworkGenome_CreateRandom_HasCorrectStructure()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);

        genome.ConvLayers.Should().HaveCount(3);
        genome.HiddenLayers.Should().HaveCount(2);
        genome.HiddenLayers[0].NeuronCount.Should().Be(256);
        genome.HiddenLayers[1].NeuronCount.Should().Be(128);
        genome.MouseHead.NeuronCount.Should().Be(2);
        genome.ActionHead.NeuronCount.Should().Be(30);
    }

    [Fact]
    public void NetworkGenome_CreateRandom_InitializesWeights()
    {
        var config = NetworkConfig.Default();
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42, config);

        int expectedWeights = genome.TotalWeightCount(config.FlattenedConvOutputSize);

        genome.Weights.Should().HaveCount(expectedWeights);
        genome.Weights.Should().Contain(w => w != 0); // Not all zeros
    }

    [Fact]
    public void NetworkGenome_CreateRandom_IsDeterministic()
    {
        var genome1 = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var genome2 = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);

        // Same seed should produce same weights
        genome1.Weights.Should().BeEquivalentTo(genome2.Weights);
    }

    [Fact]
    public void NetworkGenome_CreateRandom_DifferentSeedsProduceDifferentWeights()
    {
        var genome1 = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var genome2 = NetworkGenome.CreateRandom(new[] { 256 }, seed: 43);

        // Different seeds should produce different weights
        genome1.Weights.Should().NotBeEquivalentTo(genome2.Weights);
    }

    [Fact]
    public void NetworkGenome_CreateRandom_WeightsHaveReasonableMagnitude()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);

        // Xavier-initialized weights should be small
        float maxAbs = genome.Weights.Max(w => Math.Abs(w));
        float mean = genome.Weights.Average();
        float variance = genome.Weights.Average(w => (w - mean) * (w - mean));

        maxAbs.Should().BeLessThan(1.0f); // Weights shouldn't be huge
        Math.Abs(mean).Should().BeLessThan(0.1f); // Mean should be near zero
        variance.Should().BeLessThan(0.1f); // Variance shouldn't be too large
    }

    #endregion

    #region Weight Count Tests

    [Fact]
    public void NetworkGenome_TotalWeightCount_CalculatesCorrectly()
    {
        var config = NetworkConfig.Default();
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42, config);

        int flattenSize = config.FlattenedConvOutputSize; // 21632

        // Hidden layer 1: 21632 * 256 + 256 = 5,537,792 + 256 = 5,538,048
        // Hidden layer 2: 256 * 128 + 128 = 32,768 + 128 = 32,896
        // Mouse head: 128 * 2 + 2 = 258
        // Action head: 128 * 30 + 30 = 3,870
        // Total: 5,538,048 + 32,896 + 258 + 3,870 = 5,575,072

        int expected = (flattenSize * 256 + 256) +  // Hidden 1
                       (256 * 128 + 128) +           // Hidden 2
                       (128 * 2 + 2) +               // Mouse
                       (128 * 30 + 30);              // Action

        genome.TotalWeightCount(flattenSize).Should().Be(expected);
        genome.Weights.Length.Should().Be(expected);
    }

    [Fact]
    public void NetworkGenome_TotalWeightCount_SingleHiddenLayer()
    {
        var config = NetworkConfig.Default();
        var genome = NetworkGenome.CreateRandom(new[] { 512 }, seed: 42, config);

        int flattenSize = config.FlattenedConvOutputSize;
        int expected = (flattenSize * 512 + 512) +   // Hidden
                       (512 * 2 + 2) +                // Mouse
                       (512 * 30 + 30);               // Action

        genome.TotalWeightCount(flattenSize).Should().Be(expected);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void NetworkGenome_Clone_CreatesDeepCopy()
    {
        var original = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        original.Fitness = 100.5f;
        original.Generation = 5;

        var clone = original.Clone();

        // Clone should have new ID
        clone.Id.Should().NotBe(original.Id);

        // Clone should preserve structure
        clone.HiddenLayers.Should().HaveCount(original.HiddenLayers.Count);
        clone.Weights.Should().BeEquivalentTo(original.Weights);
        clone.Fitness.Should().Be(original.Fitness);
        clone.Generation.Should().Be(original.Generation);

        // Clone should have parent reference
        clone.ParentIds.Should().Contain(original.Id);

        // Clone should reset game stats
        clone.GamesPlayed.Should().Be(0);
        clone.Wins.Should().Be(0);
    }

    [Fact]
    public void NetworkGenome_Clone_IsIndependent()
    {
        var original = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var clone = original.Clone();

        // Modify clone
        clone.Weights[0] = 999f;
        clone.HiddenLayers[0] = DenseLayerConfig.CreateHidden(512);

        // Original should be unchanged
        original.Weights[0].Should().NotBe(999f);
        original.HiddenLayers[0].NeuronCount.Should().Be(256);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void NetworkGenome_IsValid_ReturnsTrueForValidGenome()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);

        genome.IsValid().Should().BeTrue();
    }

    [Fact]
    public void NetworkGenome_IsValid_ReturnsFalseForWrongWeightCount()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        genome.Weights = new float[100]; // Wrong count

        genome.IsValid().Should().BeFalse();
    }

    [Fact]
    public void NetworkGenome_IsValid_ReturnsFalseForNaNWeights()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        genome.Weights[0] = float.NaN;

        genome.IsValid().Should().BeFalse();
    }

    [Fact]
    public void NetworkGenome_IsValid_ReturnsFalseForInfWeights()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        genome.Weights[0] = float.PositiveInfinity;

        genome.IsValid().Should().BeFalse();
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void GenomeSerializer_RoundTrip_PreservesGenome()
    {
        var original = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        original.Fitness = 123.456f;
        original.Generation = 10;
        original.GamesPlayed = 5;
        original.Wins = 3;

        var serialized = GenomeSerializer.Serialize(original);
        var deserialized = GenomeSerializer.Deserialize(serialized);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Generation.Should().Be(original.Generation);
        deserialized.Fitness.Should().BeApproximately(original.Fitness, 0.001f);
        deserialized.GamesPlayed.Should().Be(original.GamesPlayed);
        deserialized.Wins.Should().Be(original.Wins);
        deserialized.HiddenLayers.Should().HaveCount(original.HiddenLayers.Count);
        deserialized.Weights.Should().HaveCount(original.Weights.Length);

        for (int i = 0; i < original.Weights.Length; i++)
        {
            deserialized.Weights[i].Should().BeApproximately(original.Weights[i], 1e-6f);
        }
    }

    [Fact]
    public void GenomeSerializer_ValidateRoundTrip_ReturnsTrue()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);

        GenomeSerializer.ValidateRoundTrip(genome).Should().BeTrue();
    }

    [Fact]
    public void GenomeSerializer_SerializedSize_IsReasonable()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256, 128 }, seed: 42);
        var serialized = GenomeSerializer.Serialize(genome);

        // Note: Xavier-initialized weights (random floats) do not compress well
        // LZ4 is optimized for repetitive data, not random data
        // The serialized data should be roughly the same size as raw floats
        // plus some overhead for MessagePack structure
        double rawSize = genome.Weights.Length * sizeof(float);
        double compressedSize = serialized.Length;
        double ratio = compressedSize / rawSize;

        // With random float data, expect ratio around 1.0-1.3 (MessagePack overhead)
        // The important thing is it doesn't explode in size
        ratio.Should().BeLessThan(1.5); // Serialization overhead should be reasonable

        // Also verify we can deserialize successfully
        var deserialized = GenomeSerializer.Deserialize(serialized);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public void GenomeSerializer_PopulationRoundTrip_Works()
    {
        var population = Enumerable.Range(0, 10)
            .Select(i => NetworkGenome.CreateRandom(new[] { 128 }, seed: i))
            .ToList();

        var serialized = GenomeSerializer.SerializePopulation(population);
        var deserialized = GenomeSerializer.DeserializePopulation(serialized);

        deserialized.Should().HaveCount(10);

        for (int i = 0; i < population.Count; i++)
        {
            deserialized[i].Id.Should().Be(population[i].Id);
            deserialized[i].Weights.Length.Should().Be(population[i].Weights.Length);
        }
    }

    [Fact]
    public async Task GenomeSerializer_FileRoundTrip_Works()
    {
        var genome = NetworkGenome.CreateRandom(new[] { 256 }, seed: 42);
        var tempFile = Path.GetTempFileName();

        try
        {
            await GenomeSerializer.SaveAsync(genome, tempFile);
            var loaded = await GenomeSerializer.LoadAsync(tempFile);

            loaded.Id.Should().Be(genome.Id);
            loaded.Weights.Should().BeEquivalentTo(genome.Weights);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion
}
