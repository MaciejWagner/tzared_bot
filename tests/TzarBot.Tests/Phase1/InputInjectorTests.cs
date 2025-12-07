using System.Diagnostics;
using FluentAssertions;
using TzarBot.GameInterface.Input;

namespace TzarBot.Tests.Phase1;

/// <summary>
/// Tests for the Win32 input injector implementation.
/// Note: These tests simulate input and should be run in an interactive session.
/// </summary>
public class InputInjectorTests
{
    [Fact]
    public void MoveMouse_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() => injector.MoveMouse(500, 500));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void MoveMouseRelative_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() => injector.MoveMouseRelative(10, 10));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void LeftClick_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() => injector.LeftClick());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void RightClick_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() => injector.RightClick());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void DoubleClick_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() => injector.DoubleClick());

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void DragStartAndEnd_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() =>
        {
            injector.DragStart();
            injector.MoveMouseRelative(50, 50);
            injector.DragEnd();
        });

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void TypeKey_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act - Press Escape (safe key that won't interfere with system)
        var exception = Record.Exception(() => injector.TypeKey(VirtualKey.Escape));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void TypeHotkey_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act - Ctrl+A (safe hotkey)
        var exception = Record.Exception(() =>
            injector.TypeHotkey(VirtualKey.Control, VirtualKey.A));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void Scroll_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() =>
        {
            injector.Scroll(1);  // Scroll up
            injector.Scroll(-1); // Scroll down
        });

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void ActionCooldown_EnforcesMinDelay()
    {
        // Arrange
        var injector = new Win32InputInjector
        {
            MinActionDelay = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var sw = Stopwatch.StartNew();
        injector.LeftClick();
        injector.LeftClick();
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public void DefaultMinActionDelay_Is50ms()
    {
        // Arrange & Act
        var injector = new Win32InputInjector();

        // Assert
        injector.MinActionDelay.Should().Be(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void MinActionDelay_CanBeChanged()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        injector.MinActionDelay = TimeSpan.FromMilliseconds(200);

        // Assert
        injector.MinActionDelay.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void PressAndReleaseKey_DoesNotThrow()
    {
        // Arrange
        var injector = new Win32InputInjector();

        // Act
        var exception = Record.Exception(() =>
        {
            injector.PressKey(VirtualKey.Shift);
            injector.ReleaseKey(VirtualKey.Shift);
        });

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void MultipleConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        var injector = new Win32InputInjector
        {
            MinActionDelay = TimeSpan.Zero // Disable delay for speed
        };

        // Act
        var exception = Record.Exception(() =>
        {
            Parallel.For(0, 10, _ =>
            {
                injector.MoveMouseRelative(1, 1);
            });
        });

        // Assert
        exception.Should().BeNull();
    }
}
