using System.Diagnostics;
using System.Text.Json;
using SkiaSharp;
using TzarBot.BrowserInterface;
using TzarBot.Common.Models;
using TzarBot.NeuralNetwork.Inference;
using TzarBot.NeuralNetwork.Preprocessing;

namespace TrainingRunner;

/// <summary>
/// Tracks current mouse position for action execution.
/// </summary>
public sealed class MouseState
{
    public int X { get; set; } = 960;  // Center of 1920x1080
    public int Y { get; set; } = 540;
}

/// <summary>
/// Runs a single training trial: loads ONNX model, plays game, collects metrics.
/// Usage: TrainingRunner.exe <model_path> <map_path> <duration_seconds> [output_json]
/// </summary>
public static class Program
{
    private const int TargetFps = 10; // 10 actions per second
    private const int FrameDelayMs = 1000 / TargetFps;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: TrainingRunner.exe <model_path> <map_path> <duration_seconds> [output_json]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  model_path       - Path to ONNX neural network model");
            Console.WriteLine("  map_path         - Path to .tzared map file");
            Console.WriteLine("  duration_seconds - Maximum game duration in seconds");
            Console.WriteLine("  output_json      - (Optional) Path to save metrics JSON");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine(@"  TrainingRunner.exe C:\TzarBot\onnx\network_00.onnx C:\TzarBot\Maps\training-0.tzared 300 C:\TzarBot\results.json");
            return 1;
        }

        string modelPath = args[0];
        string mapPath = args[1];
        int durationSeconds = int.Parse(args[2]);
        string? outputPath = args.Length > 3 ? args[3] : null;

        Console.WriteLine("=== TzarBot Training Runner ===");
        Console.WriteLine($"Model: {modelPath}");
        Console.WriteLine($"Map: {mapPath}");
        Console.WriteLine($"Duration: {durationSeconds}s");
        Console.WriteLine();

        try
        {
            var metrics = await RunTrialAsync(modelPath, mapPath, durationSeconds);

            // Print results
            Console.WriteLine();
            Console.WriteLine("=== RESULTS ===");
            Console.WriteLine($"Outcome: {metrics.Outcome}");
            Console.WriteLine($"Duration: {metrics.ActualDurationSeconds:F1}s");
            Console.WriteLine($"Total Actions: {metrics.TotalActions}");
            Console.WriteLine($"Avg Inference: {metrics.AverageInferenceMs:F2}ms");
            Console.WriteLine($"Actions/sec: {metrics.ActionsPerSecond:F1}");

            // Save JSON if requested
            if (outputPath != null)
            {
                var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outputPath, json);
                Console.WriteLine($"Results saved to: {outputPath}");
            }

            return metrics.Outcome == "VICTORY" ? 0 : (metrics.Outcome == "DEFEAT" ? 1 : 2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return -1;
        }
    }

    private static async Task<TrialMetrics> RunTrialAsync(string modelPath, string mapPath, int durationSeconds)
    {
        var metrics = new TrialMetrics
        {
            ModelPath = modelPath,
            MapPath = mapPath,
            TargetDurationSeconds = durationSeconds,
            StartTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();
        var inferenceTimings = new List<double>();

        // Load neural network
        Console.WriteLine("Loading neural network...");
        using var engine = OnnxInferenceEngine.FromFile(modelPath, useGpu: false);
        var modelInfo = engine.GetModelInfo();
        Console.WriteLine($"Model loaded: {modelInfo}");

        // Create preprocessor matching network config
        var preprocessorConfig = PreprocessorConfig.Default();
        using var preprocessor = new ImagePreprocessor(preprocessorConfig);

        // Initialize browser
        Console.WriteLine("Initializing browser...");
        await using var browser = new PlaywrightGameInterface();
        await browser.InitializeAsync(headless: false);

        // Load map and start game
        Console.WriteLine("Loading map...");
        await browser.LoadMapAsync(mapPath);
        await Task.Delay(2000);

        Console.WriteLine("Starting game...");
        await browser.StartGameAsync();
        await Task.Delay(5000); // Wait for game to fully load

        // Game loop
        Console.WriteLine($"Starting game loop (max {durationSeconds}s)...");
        var gameStart = DateTime.UtcNow;
        int frameCount = 0;
        int actionCount = 0;
        string outcome = "TIMEOUT";

        // Track current mouse position (center of screen initially)
        var mouseState = new MouseState();

        while (stopwatch.Elapsed.TotalSeconds < durationSeconds)
        {
            var frameStart = Stopwatch.GetTimestamp();

            try
            {
                // 1. Take screenshot
                var screenshotBytes = await browser.TakeScreenshotAsync();

                // 2. Convert PNG to BGRA32 for preprocessing
                var frame = ConvertPngToScreenFrame(screenshotBytes);
                if (frame == null)
                {
                    Console.WriteLine($"[{frameCount}] Failed to convert screenshot");
                    await Task.Delay(FrameDelayMs);
                    continue;
                }

                // 3. Preprocess frame
                bool ready = preprocessor.ProcessFrame(frame);

                if (!ready)
                {
                    // Need more frames in buffer (first 3 frames)
                    frameCount++;
                    await Task.Delay(FrameDelayMs / 2); // Faster initial frame collection
                    continue;
                }

                // 4. Run inference
                var inferenceStart = Stopwatch.GetTimestamp();
                var tensor = preprocessor.GetTensor();
                var action = engine.Infer(tensor);
                var inferenceEnd = Stopwatch.GetTimestamp();

                var inferenceMs = (inferenceEnd - inferenceStart) * 1000.0 / Stopwatch.Frequency;
                inferenceTimings.Add(inferenceMs);

                // 5. Execute action
                await ExecuteActionAsync(browser, action, mouseState);
                actionCount++;

                // 6. Check game state every 10 frames
                if (frameCount % 10 == 0)
                {
                    var state = await browser.DetectGameStateAsync();
                    if (state.State == GameState.Victory)
                    {
                        outcome = "VICTORY";
                        break;
                    }
                    else if (state.State == GameState.Defeat)
                    {
                        outcome = "DEFEAT";
                        break;
                    }

                    // Log progress
                    Console.WriteLine($"[{stopwatch.Elapsed.TotalSeconds:F1}s] Actions: {actionCount}, FPS: {frameCount / stopwatch.Elapsed.TotalSeconds:F1}, Inference: {inferenceMs:F2}ms");
                }

                frameCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{frameCount}] Error: {ex.Message}");
            }

            // Maintain target FPS
            var elapsed = (Stopwatch.GetTimestamp() - frameStart) * 1000.0 / Stopwatch.Frequency;
            var sleepMs = Math.Max(0, FrameDelayMs - (int)elapsed);
            if (sleepMs > 0)
            {
                await Task.Delay(sleepMs);
            }
        }

        stopwatch.Stop();

        // Fill metrics
        metrics.EndTime = DateTime.UtcNow;
        metrics.Outcome = outcome;
        metrics.ActualDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        metrics.TotalFrames = frameCount;
        metrics.TotalActions = actionCount;
        metrics.AverageInferenceMs = inferenceTimings.Count > 0 ? inferenceTimings.Average() : 0;
        metrics.MinInferenceMs = inferenceTimings.Count > 0 ? inferenceTimings.Min() : 0;
        metrics.MaxInferenceMs = inferenceTimings.Count > 0 ? inferenceTimings.Max() : 0;
        metrics.ActionsPerSecond = actionCount / stopwatch.Elapsed.TotalSeconds;

        return metrics;
    }

    private static ScreenFrame? ConvertPngToScreenFrame(byte[] pngData)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(pngData);
            if (bitmap == null) return null;

            // Convert to BGRA32 format
            using var converted = bitmap.Copy(SKColorType.Bgra8888);

            var pixels = converted.GetPixelSpan();
            var data = new byte[pixels.Length];
            pixels.CopyTo(data);

            return new ScreenFrame
            {
                Data = data,
                Width = converted.Width,
                Height = converted.Height,
                Format = PixelFormat.BGRA32,
                TimestampTicks = DateTime.UtcNow.Ticks
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task ExecuteActionAsync(
        IBrowserGameInterface browser,
        GameAction action,
        MouseState mouse)
    {
        // Update mouse position based on delta
        if (action.MouseDeltaX != 0 || action.MouseDeltaY != 0)
        {
            var (dx, dy) = ActionDecoder.ScaleMouseToPixels(action.MouseDeltaX, action.MouseDeltaY);
            mouse.X = Math.Clamp(mouse.X + dx, 0, 1920);
            mouse.Y = Math.Clamp(mouse.Y + dy, 0, 1080);
        }

        switch (action.Type)
        {
            case ActionType.None:
                // No action
                break;

            case ActionType.MouseMove:
                // Mouse move is implicit in position tracking
                break;

            case ActionType.LeftClick:
                await browser.ClickAtAsync(mouse.X, mouse.Y);
                break;

            case ActionType.RightClick:
                await browser.RightClickAtAsync(mouse.X, mouse.Y);
                break;

            case ActionType.DoubleClick:
                await browser.ClickAtAsync(mouse.X, mouse.Y);
                await Task.Delay(50);
                await browser.ClickAtAsync(mouse.X, mouse.Y);
                break;

            case ActionType.DragStart:
                // Store drag start position (simplified - no state tracking)
                await browser.ClickAtAsync(mouse.X, mouse.Y);
                break;

            case ActionType.DragEnd:
                // Simplified drag - just another click
                await browser.ClickAtAsync(mouse.X, mouse.Y);
                break;

            case ActionType.Hotkey:
                if (action.HotkeyNumber.HasValue)
                {
                    var key = action.HotkeyNumber.Value == 0 ? "0" : action.HotkeyNumber.Value.ToString();
                    await browser.PressKeyAsync(key);
                }
                break;

            case ActionType.HotkeyCtrl:
                if (action.HotkeyNumber.HasValue)
                {
                    var key = action.HotkeyNumber.Value == 0 ? "0" : action.HotkeyNumber.Value.ToString();
                    await browser.PressKeyAsync($"Control+{key}");
                }
                break;

            case ActionType.Escape:
                await browser.PressKeyAsync("Escape");
                break;

            case ActionType.Enter:
                await browser.PressKeyAsync("Enter");
                break;

            case ActionType.ScrollUp:
            case ActionType.ScrollDown:
                // Not implemented in browser interface yet
                break;
        }
    }
}

/// <summary>
/// Metrics collected during a single training trial.
/// </summary>
public sealed class TrialMetrics
{
    public string ModelPath { get; set; } = "";
    public string MapPath { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Outcome { get; set; } = "UNKNOWN"; // VICTORY, DEFEAT, TIMEOUT
    public int TargetDurationSeconds { get; set; }
    public double ActualDurationSeconds { get; set; }
    public int TotalFrames { get; set; }
    public int TotalActions { get; set; }
    public double AverageInferenceMs { get; set; }
    public double MinInferenceMs { get; set; }
    public double MaxInferenceMs { get; set; }
    public double ActionsPerSecond { get; set; }
}
