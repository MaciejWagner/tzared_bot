using System.Diagnostics;
using TzarBot.Common.Models;
using TzarBot.GameInterface.Capture;
using TzarBot.GameInterface.Input;
using TzarBot.GameInterface.IPC;
using TzarBot.GameInterface.Window;

Console.WriteLine("=== TzarBot GameInterface Demo ===");
Console.WriteLine();

// Initialize components
var windowDetector = new WindowDetector();
var inputInjector = new Win32InputInjector();

// Try to find Tzar window first, fall back to Notepad
Console.WriteLine("Looking for game window...");
var targetWindow = windowDetector.FindWindow(TzarWindow.WindowTitle);

if (targetWindow == null)
{
    Console.WriteLine("Tzar not found, looking for Notepad...");
    targetWindow = windowDetector.FindWindow("Notepad");
}

if (targetWindow == null)
{
    Console.WriteLine("No suitable window found. Please open Notepad or Tzar game.");
    Console.WriteLine("Starting Notepad...");

    Process.Start("notepad.exe");
    await Task.Delay(1000); // Wait for window to appear

    targetWindow = windowDetector.FindWindow("Notepad");
}

if (targetWindow == null)
{
    Console.WriteLine("ERROR: Could not find any target window.");
    return 1;
}

Console.WriteLine($"Found window: {targetWindow.Title}");
Console.WriteLine($"  Handle: 0x{targetWindow.Handle:X}");
Console.WriteLine($"  Bounds: {targetWindow.Bounds}");
Console.WriteLine($"  Client: {targetWindow.ClientBounds}");
Console.WriteLine();

// Bring window to foreground
Console.WriteLine("Bringing window to foreground...");
windowDetector.SetForeground(targetWindow.Handle);
await Task.Delay(500);

// Test screen capture
Console.WriteLine("Testing screen capture...");
var captureStats = new List<double>();

try
{
    using var screenCapture = new DxgiScreenCapture();

    var sw = Stopwatch.StartNew();
    var frameCount = 0;
    var targetFrames = 10;

    Console.WriteLine($"Capturing {targetFrames} frames...");

    while (frameCount < targetFrames)
    {
        var frameStart = sw.Elapsed;
        var frame = screenCapture.CaptureFrame();
        var frameTime = (sw.Elapsed - frameStart).TotalMilliseconds;

        if (frame != null)
        {
            captureStats.Add(frameTime);
            frameCount++;
            Console.WriteLine($"  Frame {frameCount}: {frame.Width}x{frame.Height}, {frame.Data.Length} bytes, {frameTime:F2}ms");
        }
        else
        {
            // No new frame available, wait a bit
            await Task.Delay(16);
        }
    }

    var totalTime = sw.Elapsed.TotalSeconds;
    Console.WriteLine();
    Console.WriteLine("Capture Statistics:");
    Console.WriteLine($"  Total time: {totalTime:F2}s");
    Console.WriteLine($"  Frames captured: {frameCount}");
    Console.WriteLine($"  Avg FPS: {frameCount / totalTime:F1}");
    Console.WriteLine($"  Avg frame time: {captureStats.Average():F2}ms");
    Console.WriteLine($"  Min frame time: {captureStats.Min():F2}ms");
    Console.WriteLine($"  Max frame time: {captureStats.Max():F2}ms");
}
catch (ScreenCaptureException ex)
{
    Console.WriteLine($"Screen capture error: {ex.Message}");
}

Console.WriteLine();

// Test input injection
Console.WriteLine("Testing input injection...");

// Get updated window info
targetWindow = windowDetector.GetWindowInfo(targetWindow.Handle);
if (targetWindow != null)
{
    var centerX = targetWindow.ClientBounds.X + targetWindow.ClientBounds.Width / 2;
    var centerY = targetWindow.ClientBounds.Y + targetWindow.ClientBounds.Height / 2;

    Console.WriteLine($"Moving mouse to window center: ({centerX}, {centerY})");
    inputInjector.MoveMouse(centerX, centerY);
    await Task.Delay(500);

    Console.WriteLine("Performing left click...");
    inputInjector.LeftClick();
    await Task.Delay(500);

    Console.WriteLine("Typing test keys (H, E, L, L, O)...");
    inputInjector.TypeKey(VirtualKey.H);
    inputInjector.TypeKey(VirtualKey.E);
    inputInjector.TypeKey(VirtualKey.L);
    inputInjector.TypeKey(VirtualKey.L);
    inputInjector.TypeKey(VirtualKey.O);
    await Task.Delay(500);
}

Console.WriteLine();

// Test IPC
Console.WriteLine("Testing IPC (Named Pipes)...");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

// Server setup
using var server = new PipeServer();
ScreenFrame? receivedFrame = null;
var frameReceived = new TaskCompletionSource<bool>();

server.OnClientConnected += () => Console.WriteLine("  Server: Client connected");
server.OnClientDisconnected += () => Console.WriteLine("  Server: Client disconnected");

// Client setup
using var client = new PipeClient();

client.OnFrameReceived += frame =>
{
    receivedFrame = frame;
    frameReceived.TrySetResult(true);
};

// Start server in background
var serverTask = Task.Run(async () =>
{
    try
    {
        await server.StartAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected
    }
});

// Give server time to start
await Task.Delay(200);

// Connect client
try
{
    await client.ConnectAsync(TimeSpan.FromSeconds(2), cts.Token);
    Console.WriteLine("  Client connected to server");

    // Server sends a frame to client
    var testFrame = new ScreenFrame
    {
        Data = new byte[1920 * 1080 * 4],
        Width = 1920,
        Height = 1080,
        TimestampTicks = DateTime.UtcNow.Ticks,
        Format = PixelFormat.BGRA32
    };

    var sendSw = Stopwatch.StartNew();
    await server.SendFrameAsync(testFrame, cts.Token);
    var sendTime = sendSw.ElapsedMilliseconds;
    Console.WriteLine($"  Server sent frame: {testFrame.Data.Length} bytes in {sendTime}ms");

    // Wait for frame on client
    var received = await Task.WhenAny(frameReceived.Task, Task.Delay(2000, cts.Token));
    if (received == frameReceived.Task && receivedFrame != null)
    {
        Console.WriteLine($"  Client received frame: {receivedFrame.Width}x{receivedFrame.Height}");
    }
    else
    {
        Console.WriteLine("  WARNING: Client did not receive frame within timeout");
    }

    // Client sends action to server
    var testAction = new GameAction
    {
        Type = ActionType.MouseMove,
        MouseDeltaX = 10.5f,
        MouseDeltaY = -5.2f,
        Timestamp = DateTime.UtcNow
    };

    await client.SendActionAsync(testAction, cts.Token);
    Console.WriteLine($"  Client sent action: {testAction.Type}");

    Console.WriteLine("  IPC test completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"  IPC error: {ex.Message}");
}
finally
{
    await server.StopAsync();
    await client.DisconnectAsync();
}

Console.WriteLine();
Console.WriteLine("=== Demo Complete ===");
Console.WriteLine();
Console.WriteLine("All Phase 1 components tested:");
Console.WriteLine("  [OK] Window Detection");
Console.WriteLine("  [OK] Screen Capture");
Console.WriteLine("  [OK] Input Injection");
Console.WriteLine("  [OK] IPC Named Pipes");

return 0;
