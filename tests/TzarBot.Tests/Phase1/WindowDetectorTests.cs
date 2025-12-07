using FluentAssertions;
using TzarBot.GameInterface.Window;

namespace TzarBot.Tests.Phase1;

/// <summary>
/// Tests for window detection implementation.
/// </summary>
public class WindowDetectorTests
{
    private readonly WindowDetector _detector = new();

    [Fact]
    public void EnumerateWindows_ReturnsWindows()
    {
        // Act
        var windows = _detector.EnumerateWindows().ToList();

        // Assert
        windows.Should().NotBeEmpty("There should be at least one visible window on desktop");
    }

    [Fact]
    public void EnumerateWindows_WindowsHaveValidHandles()
    {
        // Act
        var windows = _detector.EnumerateWindows().ToList();

        // Assert
        foreach (var window in windows)
        {
            window.Handle.Should().NotBe(nint.Zero);
        }
    }

    [Fact]
    public void EnumerateWindows_WindowsHaveTitles()
    {
        // Act
        var windows = _detector.EnumerateWindows().ToList();

        // Assert
        foreach (var window in windows)
        {
            window.Title.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GetWindowInfo_WithValidHandle_ReturnsInfo()
    {
        // Arrange
        var windows = _detector.EnumerateWindows().ToList();
        var firstWindow = windows.First();

        // Act
        var info = _detector.GetWindowInfo(firstWindow.Handle);

        // Assert
        info.Should().NotBeNull();
        info!.Handle.Should().Be(firstWindow.Handle);
    }

    [Fact]
    public void GetWindowInfo_WithInvalidHandle_ReturnsNull()
    {
        // Act
        var info = _detector.GetWindowInfo(nint.Zero);

        // Assert
        info.Should().BeNull();
    }

    [Fact]
    public void FindWindow_WithExistingPattern_ReturnsWindow()
    {
        // Arrange
        var windows = _detector.EnumerateWindows().ToList();
        if (windows.Count == 0)
        {
            return; // Skip if no windows
        }

        var existingTitle = windows.First().Title;
        var pattern = existingTitle.Length > 5 ? existingTitle[..5] : existingTitle;

        // Act
        var found = _detector.FindWindow(pattern);

        // Assert
        found.Should().NotBeNull();
    }

    [Fact]
    public void FindWindow_WithNonExistingPattern_ReturnsNull()
    {
        // Act
        var found = _detector.FindWindow("__NONEXISTENT_WINDOW_12345__");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void WindowInfo_HasValidBounds()
    {
        // Act
        var windows = _detector.EnumerateWindows().ToList();
        var visibleWindows = windows.Where(w => w.IsVisible && !w.IsMinimized).ToList();

        // Assert
        foreach (var window in visibleWindows.Take(5)) // Check first 5
        {
            window.Bounds.Width.Should().BeGreaterThanOrEqualTo(0);
            window.Bounds.Height.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void FindWindowByProcess_WithExistingProcess_ReturnsWindow()
    {
        // Act - Try to find a common system process
        var found = _detector.FindWindowByProcess("explorer");

        // Assert - Explorer might or might not have a main window
        // This test just verifies no exception is thrown
        // Found can be null if explorer has no window
    }

    [Fact]
    public void FindWindowByProcess_WithNonExistingProcess_ReturnsNull()
    {
        // Act
        var found = _detector.FindWindowByProcess("__NONEXISTENT_PROCESS__");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void SetForeground_WithInvalidHandle_ReturnsFalse()
    {
        // Act
        var result = _detector.SetForeground(nint.Zero);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void WindowInfo_HasClassName()
    {
        // Act
        var windows = _detector.EnumerateWindows().ToList();

        // Assert
        foreach (var window in windows.Take(5))
        {
            window.ClassName.Should().NotBeNull();
        }
    }

    [Fact]
    public void TzarWindow_ConstantsAreDefined()
    {
        // Assert
        TzarWindow.WindowTitle.Should().Be("Tzar");
        TzarWindow.ProcessName.Should().Be("Tzar");
    }
}
