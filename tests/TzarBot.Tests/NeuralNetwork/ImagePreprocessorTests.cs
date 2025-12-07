using System.Drawing;
using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.NeuralNetwork.Preprocessing;

namespace TzarBot.Tests.NeuralNetwork;

public class ImagePreprocessorTests
{
    #region PreprocessorConfig Tests

    [Fact]
    public void PreprocessorConfig_Default_HasCorrectValues()
    {
        var config = PreprocessorConfig.Default();

        config.InputWidth.Should().Be(1920);
        config.InputHeight.Should().Be(1080);
        config.OutputWidth.Should().Be(240);
        config.OutputHeight.Should().Be(135);
        config.FrameStackSize.Should().Be(4);
        config.UseGrayscale.Should().BeTrue();
        config.NormalizationMode.Should().Be(NormalizationMode.ZeroToOne);
    }

    [Fact]
    public void PreprocessorConfig_OutputTensorSize_CalculatesCorrectly()
    {
        var config = PreprocessorConfig.Default();

        // 4 frames * 135 height * 240 width = 129,600
        int expected = 4 * 135 * 240;

        config.OutputTensorSize.Should().Be(expected);
    }

    [Fact]
    public void PreprocessorConfig_SingleFrameSize_CalculatesCorrectly()
    {
        var config = PreprocessorConfig.Default();

        // 135 height * 240 width = 32,400
        int expected = 135 * 240;

        config.SingleFrameSize.Should().Be(expected);
    }

    [Fact]
    public void PreprocessorConfig_Create_SetsCustomDimensions()
    {
        var config = PreprocessorConfig.Create(800, 600, 160, 120);

        config.InputWidth.Should().Be(800);
        config.InputHeight.Should().Be(600);
        config.OutputWidth.Should().Be(160);
        config.OutputHeight.Should().Be(120);
    }

    [Fact]
    public void PreprocessorConfig_CreateWithCrop_SetsRegion()
    {
        var cropRegion = new Rectangle(100, 50, 600, 400);
        var config = PreprocessorConfig.CreateWithCrop(800, 600, cropRegion);

        config.HasCropRegion.Should().BeTrue();
        config.CropRegion.Should().Be(cropRegion);
        config.EffectiveInputWidth.Should().Be(600);
        config.EffectiveInputHeight.Should().Be(400);
    }

    [Fact]
    public void PreprocessorConfig_ScaleFactor_CalculatesCorrectly()
    {
        var config = PreprocessorConfig.Create(1920, 1080, 240, 135);

        config.ScaleX.Should().Be(8f);
        config.ScaleY.Should().Be(8f);
    }

    [Theory]
    [InlineData(0, 100, 50, 50, false)]
    [InlineData(100, 0, 50, 50, false)]
    [InlineData(100, 100, 0, 50, false)]
    [InlineData(100, 100, 50, 0, false)]
    [InlineData(100, 100, 50, 50, true)]
    public void PreprocessorConfig_IsValid_ChecksDimensions(
        int inW, int inH, int outW, int outH, bool expectedValid)
    {
        var config = new PreprocessorConfig
        {
            InputWidth = inW,
            InputHeight = inH,
            OutputWidth = outW,
            OutputHeight = outH
        };

        config.IsValid().Should().Be(expectedValid);
    }

    [Fact]
    public void PreprocessorConfig_IsValid_RejectsBadCropRegion()
    {
        var config = new PreprocessorConfig
        {
            InputWidth = 800,
            InputHeight = 600,
            OutputWidth = 100,
            OutputHeight = 100,
            CropRegion = new Rectangle(700, 500, 200, 200)
        };

        config.IsValid().Should().BeFalse();
    }

    [Fact]
    public void PreprocessorConfig_IsValid_RejectsInvalidFrameStackSize()
    {
        var config = PreprocessorConfig.Default();
        config.FrameStackSize = 0;
        config.IsValid().Should().BeFalse();

        config.FrameStackSize = 20;
        config.IsValid().Should().BeFalse();

        config.FrameStackSize = 4;
        config.IsValid().Should().BeTrue();
    }

    #endregion

    #region FrameBuffer Tests

    [Fact]
    public void FrameBuffer_Constructor_InitializesCorrectly()
    {
        using var buffer = new FrameBuffer(capacity: 4, frameSize: 100);

        buffer.Capacity.Should().Be(4);
        buffer.FrameSize.Should().Be(100);
        buffer.Count.Should().Be(0);
        buffer.IsFull.Should().BeFalse();
    }

    [Fact]
    public void FrameBuffer_AddFrame_IncreasesCount()
    {
        using var buffer = new FrameBuffer(4, 100);
        var frame = new float[100];

        buffer.AddFrame(frame);
        buffer.Count.Should().Be(1);

        buffer.AddFrame(frame);
        buffer.Count.Should().Be(2);
    }

    [Fact]
    public void FrameBuffer_AddFrame_WrapsAround()
    {
        using var buffer = new FrameBuffer(3, 10);

        for (int i = 0; i < 5; i++)
        {
            var frame = new float[10];
            frame[0] = i;
            buffer.AddFrame(frame);
        }

        buffer.Count.Should().Be(3);
        buffer.IsFull.Should().BeTrue();
    }

    [Fact]
    public void FrameBuffer_AddFrame_RejectsWrongSize()
    {
        using var buffer = new FrameBuffer(4, 100);
        var wrongSizeFrame = new float[50];

        var action = () => buffer.AddFrame(wrongSizeFrame);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Frame size mismatch*");
    }

    [Fact]
    public void FrameBuffer_GetStackedFrames_ReturnsCorrectSize()
    {
        using var buffer = new FrameBuffer(4, 100);

        buffer.AddFrame(new float[100]);
        buffer.AddFrame(new float[100]);

        var result = buffer.GetStackedFrames();

        result.Should().HaveCount(4 * 100);
    }

    [Fact]
    public void FrameBuffer_GetStackedFrames_ReturnsFramesInOrder()
    {
        using var buffer = new FrameBuffer(3, 5);

        buffer.AddFrame(new float[] { 1, 1, 1, 1, 1 });
        buffer.AddFrame(new float[] { 2, 2, 2, 2, 2 });
        buffer.AddFrame(new float[] { 3, 3, 3, 3, 3 });

        var result = buffer.GetStackedFrames();

        result[0].Should().Be(1);
        result[5].Should().Be(2);
        result[10].Should().Be(3);
    }

    [Fact]
    public void FrameBuffer_GetStackedFrames_PadsWithOldestWhenNotFull()
    {
        using var buffer = new FrameBuffer(4, 3);

        buffer.AddFrame(new float[] { 1, 1, 1 });
        buffer.AddFrame(new float[] { 2, 2, 2 });

        var result = buffer.GetStackedFrames();

        result[0].Should().Be(1);
        result[3].Should().Be(1);
        result[6].Should().Be(1);
        result[9].Should().Be(2);
    }

    [Fact]
    public void FrameBuffer_GetStackedFrames_IntoSpan_Works()
    {
        using var buffer = new FrameBuffer(2, 5);

        buffer.AddFrame(new float[] { 1, 2, 3, 4, 5 });
        buffer.AddFrame(new float[] { 6, 7, 8, 9, 10 });

        var destination = new float[10];
        buffer.GetStackedFrames(destination);

        destination[0].Should().Be(1);
        destination[5].Should().Be(6);
    }

    [Fact]
    public void FrameBuffer_GetLatestFrame_ReturnsNewest()
    {
        using var buffer = new FrameBuffer(4, 3);

        buffer.AddFrame(new float[] { 1, 2, 3 });
        buffer.AddFrame(new float[] { 4, 5, 6 });
        buffer.AddFrame(new float[] { 7, 8, 9 });

        var latest = buffer.GetLatestFrame();

        latest.Should().NotBeNull();
        latest![0].Should().Be(7);
        latest[1].Should().Be(8);
        latest[2].Should().Be(9);
    }

    [Fact]
    public void FrameBuffer_GetLatestFrame_ReturnsNullWhenEmpty()
    {
        using var buffer = new FrameBuffer(4, 10);

        buffer.GetLatestFrame().Should().BeNull();
    }

    [Fact]
    public void FrameBuffer_Clear_ResetsBuffer()
    {
        using var buffer = new FrameBuffer(4, 10);

        buffer.AddFrame(new float[10]);
        buffer.AddFrame(new float[10]);
        buffer.Count.Should().Be(2);

        buffer.Clear();

        buffer.Count.Should().Be(0);
        buffer.IsFull.Should().BeFalse();
    }

    [Fact]
    public async Task FrameBuffer_IsThreadSafe()
    {
        using var buffer = new FrameBuffer(10, 100);
        const int iterations = 1000;
        var exceptions = new List<Exception>();

        var writers = Enumerable.Range(0, 4).Select(i => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < iterations; j++)
                {
                    var frame = new float[100];
                    frame[0] = i * iterations + j;
                    buffer.AddFrame(frame);
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        })).ToArray();

        var readers = Enumerable.Range(0, 4).Select(idx => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < iterations; j++)
                {
                    float[] frames = buffer.GetStackedFrames();
                    int count = buffer.Count;
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(writers.Concat(readers).ToArray());

        exceptions.Should().BeEmpty();
    }

    [Fact]
    public void FrameBuffer_Dispose_PreventsUse()
    {
        var buffer = new FrameBuffer(4, 10);
        buffer.Dispose();

        var action = () => buffer.AddFrame(new float[10]);
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region ImagePreprocessor Tests

    [Fact]
    public void ImagePreprocessor_Constructor_AcceptsDefaultConfig()
    {
        using var preprocessor = new ImagePreprocessor();

        preprocessor.Config.Should().NotBeNull();
        preprocessor.OutputTensorSize.Should().Be(4 * 135 * 240);
    }

    [Fact]
    public void ImagePreprocessor_Constructor_RejectsInvalidConfig()
    {
        var invalidConfig = new PreprocessorConfig { InputWidth = 0 };

        var action = () => new ImagePreprocessor(invalidConfig);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImagePreprocessor_ProcessFrame_AcceptsBGRA32()
    {
        var config = PreprocessorConfig.Create(100, 100, 10, 10);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(100, 100, PixelFormat.BGRA32);

        var result = preprocessor.ProcessFrame(frame);

        result.Should().BeFalse();
    }

    [Fact]
    public void ImagePreprocessor_ProcessFrame_RejectsNonBGRA32()
    {
        var config = PreprocessorConfig.Create(100, 100, 10, 10);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(100, 100, PixelFormat.RGB24);

        var action = () => preprocessor.ProcessFrame(frame);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Expected BGRA32*");
    }

    [Fact]
    public void ImagePreprocessor_ProcessFrame_ReturnsTrueWhenFull()
    {
        var config = PreprocessorConfig.Create(100, 100, 10, 10);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(100, 100, PixelFormat.BGRA32);

        preprocessor.ProcessFrame(frame).Should().BeFalse();
        preprocessor.ProcessFrame(frame).Should().BeFalse();
        preprocessor.ProcessFrame(frame).Should().BeFalse();
        preprocessor.ProcessFrame(frame).Should().BeTrue();
    }

    [Fact]
    public void ImagePreprocessor_GetTensor_ReturnsCorrectSize()
    {
        var config = PreprocessorConfig.Create(100, 100, 10, 10);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(100, 100, PixelFormat.BGRA32);
        preprocessor.ProcessFrame(frame);

        var tensor = preprocessor.GetTensor();

        tensor.Should().HaveCount(4 * 10 * 10);
    }

    [Fact]
    public void ImagePreprocessor_ProcessSingleFrame_ReturnsCorrectSize()
    {
        var config = PreprocessorConfig.Create(100, 100, 10, 10);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(100, 100, PixelFormat.BGRA32);

        var result = preprocessor.ProcessSingleFrame(frame);

        result.Should().HaveCount(10 * 10);
    }

    [Fact]
    public void ImagePreprocessor_ProcessFrame_DownscalesCorrectly()
    {
        var config = PreprocessorConfig.Create(4, 4, 2, 2);
        config.FrameStackSize = 1;
        using var preprocessor = new ImagePreprocessor(config);

        var data = new byte[4 * 4 * 4];
        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = 255;
            data[i + 1] = 255;
            data[i + 2] = 255;
            data[i + 3] = 255;
        }

        var frame = new ScreenFrame
        {
            Data = data,
            Width = 4,
            Height = 4,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };

        var result = preprocessor.ProcessSingleFrame(frame);

        result.Should().OnlyContain(v => Math.Abs(v - 1.0f) < 0.01f);
    }

    [Fact]
    public void ImagePreprocessor_GrayscaleConversion_UsesCorrectFormula()
    {
        var config = PreprocessorConfig.Create(1, 1, 1, 1);
        config.FrameStackSize = 1;
        using var preprocessor = new ImagePreprocessor(config);

        var data = new byte[] { 0, 0, 255, 255 };

        var frame = new ScreenFrame
        {
            Data = data,
            Width = 1,
            Height = 1,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };

        var result = preprocessor.ProcessSingleFrame(frame);

        result[0].Should().BeApproximately(0.299f, 0.01f);
    }

    [Fact]
    public void ImagePreprocessor_Normalization_ZeroToOne()
    {
        var config = PreprocessorConfig.Create(1, 1, 1, 1);
        config.FrameStackSize = 1;
        config.NormalizationMode = NormalizationMode.ZeroToOne;
        using var preprocessor = new ImagePreprocessor(config);

        var data = new byte[] { 255, 255, 255, 255 };
        var frame = new ScreenFrame
        {
            Data = data,
            Width = 1,
            Height = 1,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };

        var result = preprocessor.ProcessSingleFrame(frame);

        result[0].Should().BeApproximately(1.0f, 0.01f);
    }

    [Fact]
    public void ImagePreprocessor_Normalization_MinusOneToOne()
    {
        var config = PreprocessorConfig.Create(1, 1, 1, 1);
        config.FrameStackSize = 1;
        config.NormalizationMode = NormalizationMode.MinusOneToOne;
        using var preprocessor = new ImagePreprocessor(config);

        var blackFrame = new ScreenFrame
        {
            Data = new byte[] { 0, 0, 0, 255 },
            Width = 1,
            Height = 1,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };

        var resultBlack = preprocessor.ProcessSingleFrame(blackFrame);
        resultBlack[0].Should().BeApproximately(-1.0f, 0.01f);

        var whiteFrame = new ScreenFrame
        {
            Data = new byte[] { 255, 255, 255, 255 },
            Width = 1,
            Height = 1,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };

        var resultWhite = preprocessor.ProcessSingleFrame(whiteFrame);
        resultWhite[0].Should().BeApproximately(1.0f, 0.01f);
    }

    [Fact]
    public void ImagePreprocessor_Crop_ExtractsCorrectRegion()
    {
        var config = PreprocessorConfig.CreateWithCrop(10, 10, new Rectangle(3, 3, 4, 4), 2, 2);
        config.FrameStackSize = 1;
        using var preprocessor = new ImagePreprocessor(config);

        var data = new byte[10 * 10 * 4];
        for (int y = 3; y < 7; y++)
        {
            for (int x = 3; x < 7; x++)
            {
                int offset = (y * 10 + x) * 4;
                data[offset] = 255;
                data[offset + 1] = 255;
                data[offset + 2] = 255;
                data[offset + 3] = 255;
            }
        }

        var frame = new ScreenFrame
        {
            Data = data,
            Width = 10,
            Height = 10,
            TimestampTicks = 0,
            Format = PixelFormat.BGRA32
        };

        var result = preprocessor.ProcessSingleFrame(frame);

        result.Should().OnlyContain(v => v > 0.9f);
    }

    [Fact]
    public void ImagePreprocessor_FrameStacking_Works()
    {
        var config = PreprocessorConfig.Create(4, 4, 2, 2);
        config.FrameStackSize = 4;
        using var preprocessor = new ImagePreprocessor(config);

        for (int i = 0; i < 4; i++)
        {
            byte intensity = (byte)(i * 85);
            var frame = CreateUniformFrame(4, 4, intensity, intensity, intensity);
            preprocessor.ProcessFrame(frame);
        }

        var tensor = preprocessor.GetTensor();

        tensor.Should().HaveCount(4 * 2 * 2);

        float avg0 = tensor.Take(4).Average();
        float avg3 = tensor.Skip(12).Take(4).Average();

        avg0.Should().BeApproximately(0f, 0.05f);
        avg3.Should().BeApproximately(1f, 0.05f);
    }

    [Fact]
    public void ImagePreprocessor_Reset_ClearsFrameBuffer()
    {
        var config = PreprocessorConfig.Create(10, 10, 5, 5);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(10, 10, PixelFormat.BGRA32);
        preprocessor.ProcessFrame(frame);
        preprocessor.ProcessFrame(frame);

        preprocessor.FrameBuffer.Count.Should().Be(2);

        preprocessor.Reset();

        preprocessor.FrameBuffer.Count.Should().Be(0);
    }

    [Fact]
    public void ImagePreprocessor_Dispose_PreventsUse()
    {
        var config = PreprocessorConfig.Create(10, 10, 5, 5);
        var preprocessor = new ImagePreprocessor(config);
        preprocessor.Dispose();

        var frame = CreateTestFrame(10, 10, PixelFormat.BGRA32);
        var action = () => preprocessor.ProcessFrame(frame);

        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ImagePreprocessor_GetTensor_IntoSpan_Works()
    {
        var config = PreprocessorConfig.Create(10, 10, 5, 5);
        using var preprocessor = new ImagePreprocessor(config);

        var frame = CreateTestFrame(10, 10, PixelFormat.BGRA32);
        preprocessor.ProcessFrame(frame);

        var destination = new float[preprocessor.OutputTensorSize];
        preprocessor.GetTensor(destination);

        destination.Should().HaveCount(preprocessor.OutputTensorSize);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ImagePreprocessor_FullPipeline_ProducesValidTensor()
    {
        var config = PreprocessorConfig.Default();
        using var preprocessor = new ImagePreprocessor(config);

        for (int i = 0; i < 4; i++)
        {
            var frame = CreateTestFrame(1920, 1080, PixelFormat.BGRA32);
            preprocessor.ProcessFrame(frame);
        }

        var tensor = preprocessor.GetTensor();

        tensor.Should().HaveCount(4 * 135 * 240);
        tensor.Should().OnlyContain(v => v >= 0f && v <= 1f);
    }

    [Fact]
    public void ImagePreprocessor_OutputMatchesNetworkInput()
    {
        var preprocessorConfig = PreprocessorConfig.Default();
        var networkConfig = TzarBot.NeuralNetwork.Models.NetworkConfig.Default();

        preprocessorConfig.FrameStackSize.Should().Be(networkConfig.StackedFrames);
        preprocessorConfig.OutputWidth.Should().Be(networkConfig.InputWidth);
        preprocessorConfig.OutputHeight.Should().Be(networkConfig.InputHeight);

        using var preprocessor = new ImagePreprocessor(preprocessorConfig);

        int expectedSize = networkConfig.StackedFrames * networkConfig.InputHeight * networkConfig.InputWidth;

        preprocessor.OutputTensorSize.Should().Be(expectedSize);
    }

    #endregion

    #region Helper Methods

    private static ScreenFrame CreateTestFrame(int width, int height, PixelFormat format)
    {
        int bytesPerPixel = format switch
        {
            PixelFormat.BGRA32 => 4,
            PixelFormat.RGB24 => 3,
            PixelFormat.Grayscale8 => 1,
            _ => 4
        };

        var data = new byte[width * height * bytesPerPixel];

        var random = new Random(42);
        random.NextBytes(data);

        return new ScreenFrame
        {
            Data = data,
            Width = width,
            Height = height,
            TimestampTicks = DateTime.UtcNow.Ticks,
            Format = format
        };
    }

    private static ScreenFrame CreateUniformFrame(int width, int height, byte r, byte g, byte b)
    {
        var data = new byte[width * height * 4];

        for (int i = 0; i < data.Length; i += 4)
        {
            data[i] = b;
            data[i + 1] = g;
            data[i + 2] = r;
            data[i + 3] = 255;
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

    #endregion
}
