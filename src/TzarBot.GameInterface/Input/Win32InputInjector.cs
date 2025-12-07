using System.Runtime.InteropServices;
using static TzarBot.GameInterface.Input.NativeMethods;

namespace TzarBot.GameInterface.Input;

/// <summary>
/// Input injector implementation using Windows SendInput API.
/// </summary>
public sealed class Win32InputInjector : IInputInjector
{
    private readonly object _lock = new();
    private DateTime _lastActionTime = DateTime.MinValue;
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    /// <inheritdoc />
    public TimeSpan MinActionDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Creates a new Win32InputInjector instance.
    /// </summary>
    public Win32InputInjector()
    {
        _screenWidth = GetSystemMetrics(SM_CXSCREEN);
        _screenHeight = GetSystemMetrics(SM_CYSCREEN);
    }

    /// <inheritdoc />
    public void MoveMouse(int x, int y, bool absolute = true)
    {
        lock (_lock)
        {
            if (absolute)
            {
                // Convert to normalized coordinates (0-65535)
                var normalizedX = (int)((x * 65535.0) / _screenWidth);
                var normalizedY = (int)((y * 65535.0) / _screenHeight);

                SendMouseInput(normalizedX, normalizedY, MouseEventFlags.Move | MouseEventFlags.Absolute);
            }
            else
            {
                SendMouseInput(x, y, MouseEventFlags.Move);
            }
        }
    }

    /// <inheritdoc />
    public void MoveMouseRelative(int dx, int dy)
    {
        lock (_lock)
        {
            SendMouseInput(dx, dy, MouseEventFlags.Move);
        }
    }

    /// <inheritdoc />
    public void LeftClick()
    {
        lock (_lock)
        {
            EnforceDelay();
            SendMouseInput(0, 0, MouseEventFlags.LeftDown);
            Thread.Sleep(10);
            SendMouseInput(0, 0, MouseEventFlags.LeftUp);
            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void RightClick()
    {
        lock (_lock)
        {
            EnforceDelay();
            SendMouseInput(0, 0, MouseEventFlags.RightDown);
            Thread.Sleep(10);
            SendMouseInput(0, 0, MouseEventFlags.RightUp);
            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void DoubleClick()
    {
        lock (_lock)
        {
            EnforceDelay();
            SendMouseInput(0, 0, MouseEventFlags.LeftDown);
            Thread.Sleep(10);
            SendMouseInput(0, 0, MouseEventFlags.LeftUp);
            Thread.Sleep(50);
            SendMouseInput(0, 0, MouseEventFlags.LeftDown);
            Thread.Sleep(10);
            SendMouseInput(0, 0, MouseEventFlags.LeftUp);
            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void DragStart()
    {
        lock (_lock)
        {
            EnforceDelay();
            SendMouseInput(0, 0, MouseEventFlags.LeftDown);
            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void DragEnd()
    {
        lock (_lock)
        {
            EnforceDelay();
            SendMouseInput(0, 0, MouseEventFlags.LeftUp);
            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void PressKey(VirtualKey key)
    {
        lock (_lock)
        {
            SendKeyInput((ushort)key, KeyEventFlags.None);
        }
    }

    /// <inheritdoc />
    public void ReleaseKey(VirtualKey key)
    {
        lock (_lock)
        {
            SendKeyInput((ushort)key, KeyEventFlags.KeyUp);
        }
    }

    /// <inheritdoc />
    public void TypeKey(VirtualKey key)
    {
        lock (_lock)
        {
            EnforceDelay();
            SendKeyInput((ushort)key, KeyEventFlags.None);
            Thread.Sleep(10);
            SendKeyInput((ushort)key, KeyEventFlags.KeyUp);
            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void TypeHotkey(VirtualKey modifier, VirtualKey key)
    {
        lock (_lock)
        {
            EnforceDelay();

            // Press modifier
            SendKeyInput((ushort)modifier, KeyEventFlags.None);
            Thread.Sleep(10);

            // Press and release key
            SendKeyInput((ushort)key, KeyEventFlags.None);
            Thread.Sleep(10);
            SendKeyInput((ushort)key, KeyEventFlags.KeyUp);
            Thread.Sleep(10);

            // Release modifier
            SendKeyInput((ushort)modifier, KeyEventFlags.KeyUp);

            UpdateLastActionTime();
        }
    }

    /// <inheritdoc />
    public void Scroll(int delta)
    {
        lock (_lock)
        {
            EnforceDelay();

            var input = new INPUT
            {
                Type = InputType.Mouse,
                Data = new InputUnion
                {
                    Mouse = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = delta * WHEEL_DELTA,
                        dwFlags = MouseEventFlags.Wheel,
                        time = 0,
                        dwExtraInfo = nint.Zero
                    }
                }
            };

            SendInput(1, [input], Marshal.SizeOf<INPUT>());
            UpdateLastActionTime();
        }
    }

    private void SendMouseInput(int dx, int dy, MouseEventFlags flags)
    {
        var input = new INPUT
        {
            Type = InputType.Mouse,
            Data = new InputUnion
            {
                Mouse = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };

        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private void SendKeyInput(ushort virtualKey, KeyEventFlags flags)
    {
        var input = new INPUT
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = nint.Zero
                }
            }
        };

        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private void EnforceDelay()
    {
        var elapsed = DateTime.UtcNow - _lastActionTime;
        if (elapsed < MinActionDelay)
        {
            Thread.Sleep(MinActionDelay - elapsed);
        }
    }

    private void UpdateLastActionTime()
    {
        _lastActionTime = DateTime.UtcNow;
    }
}
