using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TzarBot.GameInterface.Window;

/// <summary>
/// Window detector implementation using Win32 API.
/// </summary>
public sealed partial class WindowDetector : IWindowDetector
{
    /// <inheritdoc />
    public WindowInfo? FindWindow(string titlePattern)
    {
        foreach (var window in EnumerateWindows())
        {
            if (window.Title.Contains(titlePattern, StringComparison.OrdinalIgnoreCase))
            {
                return window;
            }
        }
        return null;
    }

    /// <inheritdoc />
    public WindowInfo? FindWindowByClass(string className)
    {
        var handle = NativeMethods.FindWindow(className, null);
        if (handle == nint.Zero)
        {
            return null;
        }
        return GetWindowInfo(handle);
    }

    /// <inheritdoc />
    public WindowInfo? FindWindowByProcess(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        foreach (var process in processes)
        {
            if (process.MainWindowHandle != nint.Zero)
            {
                var info = GetWindowInfo(process.MainWindowHandle);
                process.Dispose();
                return info;
            }
            process.Dispose();
        }
        return null;
    }

    /// <inheritdoc />
    public WindowInfo? GetWindowInfo(nint handle)
    {
        if (handle == nint.Zero || !NativeMethods.IsWindow(handle))
        {
            return null;
        }

        // Get window title
        var titleLength = NativeMethods.GetWindowTextLength(handle);
        var titleBuffer = new char[titleLength + 1];
        NativeMethods.GetWindowText(handle, titleBuffer, titleBuffer.Length);
        var title = new string(titleBuffer, 0, titleLength);

        // Get class name
        var classBuffer = new char[256];
        var classLength = NativeMethods.GetClassName(handle, classBuffer, classBuffer.Length);
        var className = new string(classBuffer, 0, classLength);

        // Get window bounds
        NativeMethods.GetWindowRect(handle, out var windowRect);
        var bounds = new Rectangle(
            windowRect.Left,
            windowRect.Top,
            windowRect.Right - windowRect.Left,
            windowRect.Bottom - windowRect.Top);

        // Get client bounds
        NativeMethods.GetClientRect(handle, out var clientRect);
        var clientPoint = new POINT { X = 0, Y = 0 };
        NativeMethods.ClientToScreen(handle, ref clientPoint);
        var clientBounds = new Rectangle(
            clientPoint.X,
            clientPoint.Y,
            clientRect.Right,
            clientRect.Bottom);

        return new WindowInfo
        {
            Handle = handle,
            Title = title,
            ClassName = className,
            Bounds = bounds,
            ClientBounds = clientBounds,
            IsFocused = NativeMethods.GetForegroundWindow() == handle,
            IsMinimized = NativeMethods.IsIconic(handle),
            IsVisible = NativeMethods.IsWindowVisible(handle)
        };
    }

    /// <inheritdoc />
    public bool SetForeground(nint handle)
    {
        if (handle == nint.Zero)
        {
            return false;
        }

        // If minimized, restore first
        if (NativeMethods.IsIconic(handle))
        {
            NativeMethods.ShowWindow(handle, SW_RESTORE);
        }

        return NativeMethods.SetForegroundWindow(handle);
    }

    /// <inheritdoc />
    public IEnumerable<WindowInfo> EnumerateWindows()
    {
        var windows = new List<WindowInfo>();

        NativeMethods.EnumWindows((handle, _) =>
        {
            if (NativeMethods.IsWindowVisible(handle))
            {
                var info = GetWindowInfo(handle);
                if (info != null && !string.IsNullOrEmpty(info.Title))
                {
                    windows.Add(info);
                }
            }
            return true; // Continue enumeration
        }, nint.Zero);

        return windows;
    }

    private const int SW_RESTORE = 9;

    private static partial class NativeMethods
    {
        public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

        [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial nint FindWindow(string? lpClassName, string? lpWindowName);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindow(nint hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindowVisible(nint hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsIconic(nint hWnd);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
        public static partial int GetWindowTextLength(nint hWnd);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial int GetWindowText(nint hWnd, [Out] char[] lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial int GetClassName(nint hWnd, [Out] char[] lpClassName, int nMaxCount);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetClientRect(nint hWnd, out RECT lpRect);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        [LibraryImport("user32.dll")]
        public static partial nint GetForegroundWindow();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(nint hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(nint hWnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
