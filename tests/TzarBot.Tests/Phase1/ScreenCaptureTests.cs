using System.Diagnostics;
using FluentAssertions;
using TzarBot.Common.Models;
using TzarBot.GameInterface.Capture;

namespace TzarBot.Tests.Phase1;

/// <summary>
/// Tests for the DXGI screen capture implementation.
/// Note: These tests require a display and may not work in CI/CD environments.
/// </summary>
public class ScreenCaptureTests
{
    [Fact]
    public void CaptureFrame_ReturnsValidData()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Act - try multiple times as first frame might be null
        ScreenFrame? frame = null;
        for (var i = 0; i < 10 && frame == null; i++)
        {
            frame = capture.CaptureFrame();
            if (frame == null) Thread.Sleep(100);
        }

        // Assert
        frame.Should().NotBeNull();
        frame!.Data.Should().NotBeEmpty();
    }

    [Fact]
    public void ScreenFrame_HasCorrectDimensions()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Act
        ScreenFrame? frame = null;
        for (var i = 0; i < 10 && frame == null; i++)
        {
            frame = capture.CaptureFrame();
            if (frame == null) Thread.Sleep(100);
        }

        // Assert
        frame.Should().NotBeNull();
        frame!.Width.Should().BeGreaterThan(0);
        frame.Height.Should().BeGreaterThan(0);
        frame.Width.Should().Be(capture.Width);
        frame.Height.Should().Be(capture.Height);
    }

    [Fact]
    public void BufferSize_MatchesDimensions()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Act
        ScreenFrame? frame = null;
        for (var i = 0; i < 10 && frame == null; i++)
        {
            frame = capture.CaptureFrame();
            if (frame == null) Thread.Sleep(100);
        }

        // Assert
        frame.Should().NotBeNull();
        var expectedSize = frame!.Width * frame.Height * 4; // BGRA32 = 4 bytes per pixel
        frame.Data.Length.Should().Be(expectedSize);
        frame.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ScreenFrame_HasCorrectFormat()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Act
        ScreenFrame? frame = null;
        for (var i = 0; i < 10 && frame == null; i++)
        {
            frame = capture.CaptureFrame();
            if (frame == null) Thread.Sleep(100);
        }

        // Assert
        frame.Should().NotBeNull();
        frame!.Format.Should().Be(PixelFormat.BGRA32);
        frame.GetBytesPerPixel().Should().Be(4);
    }

    [Fact]
    public void ContinuousCapture_NoMemoryLeak()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Warm up and force initial GC
        for (var i = 0; i < 10; i++)
        {
            _ = capture.CaptureFrame();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (var i = 0; i < 100; i++)
        {
            var frame = capture.CaptureFrame();
            // Don't keep reference to frame
        }

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - allow 10MB growth (generous for GC timing)
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessThan(10 * 1024 * 1024, "Memory should not grow significantly during capture");
    }

    [Fact]
    public void CaptureRate_AtLeast10Fps()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Warm up
        for (var i = 0; i < 5; i++)
        {
            _ = capture.CaptureFrame();
            Thread.Sleep(50);
        }

        // Act
        var sw = Stopwatch.StartNew();
        var frameCount = 0;

        while (sw.ElapsedMilliseconds < 1000)
        {
            var frame = capture.CaptureFrame();
            if (frame != null) frameCount++;
            Thread.Sleep(10); // ~100 FPS max attempt rate
        }

        // Assert
        frameCount.Should().BeGreaterThanOrEqualTo(10, $"Should capture at least 10 frames per second, got {frameCount}");
    }

    [Fact]
    public void Capture_IsInitialized_ReturnsTrue()
    {
        // Arrange & Act
        using var capture = new DxgiScreenCapture();

        // Assert
        capture.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Capture_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var capture = new DxgiScreenCapture();
        capture.Dispose();

        // Act & Assert
        var act = () => capture.CaptureFrame();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ScreenFrame_Stride_CalculatedCorrectly()
    {
        // Arrange
        using var capture = new DxgiScreenCapture();

        // Act
        ScreenFrame? frame = null;
        for (var i = 0; i < 10 && frame == null; i++)
        {
            frame = capture.CaptureFrame();
            if (frame == null) Thread.Sleep(100);
        }

        // Assert
        frame.Should().NotBeNull();
        frame!.Stride.Should().Be(frame.Width * 4); // BGRA32
    }
}
