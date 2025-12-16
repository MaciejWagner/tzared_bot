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
    // Start at center of viewport (1271x715) where units are likely to be
    public int X { get; set; } = 635;
    public int Y { get; set; } = 357;
}

/// <summary>
/// Runs a single training trial: loads ONNX model, plays game, collects metrics.
/// Usage: TrainingRunner.exe <model_path> <map_path> <duration_seconds> [output_json]
/// </summary>
public static class Program
{
    private const int TargetFps = 2; // 2 actions per second - reduced for multi-instance training (less GPU stalls)
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

        // Load neural network with GPU acceleration
        Console.WriteLine("Loading neural network...");
        using var engine = OnnxInferenceEngine.FromFile(modelPath, useGpu: true);
        var modelInfo = engine.GetModelInfo();
        Console.WriteLine($"Model loaded: {modelInfo}");

        // Create preprocessor matching network config
        var preprocessorConfig = PreprocessorConfig.Default();
        using var preprocessor = new ImagePreprocessor(preprocessorConfig);

        // Warmup CUDA - first inference compiles cuDNN kernels (takes 2-6 seconds)
        // This ensures fast inference during actual gameplay
        Console.WriteLine("Warming up CUDA (first inference)...");
        var warmupStart = Stopwatch.GetTimestamp();
        var dummyInput = new float[modelInfo.InputSize];
        engine.Infer(dummyInput); // First inference - slow (cuDNN kernel compilation)
        engine.Infer(dummyInput); // Second inference - should be fast
        var warmupMs = (Stopwatch.GetTimestamp() - warmupStart) * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"CUDA warmup complete: {warmupMs:F0}ms (subsequent inference: {engine.LastInferenceTime.TotalMilliseconds:F1}ms)");

        // Initialize browser (visible window for best compatibility with tza.red)
        Console.WriteLine("Initializing browser...");
        await using var browser = new PlaywrightGameInterface();
        await browser.InitializeAsync(headless: false);

        // Load map and start game
        Console.WriteLine("Loading map...");
        await browser.LoadMapAsync(mapPath);
        await Task.Delay(2000);

        Console.WriteLine("Starting game...");
        await browser.StartGameAsync();

        Console.WriteLine("Waiting 3s for game to fully load...");
        await Task.Delay(3000);

        // Save debug screenshot at game start
        var startScreenshot = await browser.TakeScreenshotAsync();
        var startDebugDir = Path.Combine(Path.GetDirectoryName(modelPath)!, "debug");
        Directory.CreateDirectory(startDebugDir);
        var startScreenshotPath = Path.Combine(startDebugDir, $"game_start_{DateTime.Now:HHmmss}.png");
        await File.WriteAllBytesAsync(startScreenshotPath, startScreenshot!);
        Console.WriteLine($"[Debug] Saved start screenshot: {startScreenshotPath}");

        // CURRICULUM PHASE 2: No auto-select
        // Network must learn to LeftClick on units to select them
        // Map has many peasants spread around to increase chance of random selection
        Console.WriteLine("[Curriculum Phase 2] No auto-select - network must click to select units");

        // Game loop - reset stopwatch here so game duration is from game start, not program start
        stopwatch.Restart();
        Console.WriteLine($"Starting game loop (max {durationSeconds}s)...");
        var gameStart = DateTime.UtcNow;
        int frameCount = 0;
        int actionCount = 0;
        string outcome = "TIMEOUT";
        int loopIterations = 0;
        double lastEndGameCheck = 0; // Track last endgame check time

        // Action log for debugging
        var actionLog = new List<ActionLogEntry>();

        // Track current mouse position (center of screen initially)
        var mouseState = new MouseState();

        while (stopwatch.Elapsed.TotalSeconds < durationSeconds)
        {
            loopIterations++;
            var frameStart = Stopwatch.GetTimestamp();

            try
            {
                // 1. Take screenshot
                var screenshotBytes = await browser.TakeScreenshotAsync();

                // 2. Convert PNG to BGRA32 for preprocessing
                var frame = ConvertPngToScreenFrame(screenshotBytes!);
                if (frame == null)
                {
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

                // 5. Execute action and log it
                await ExecuteActionAsync(browser, action, mouseState);
                actionCount++;

                // Log the action
                actionLog.Add(new ActionLogEntry
                {
                    Timestamp = stopwatch.Elapsed.TotalSeconds,
                    ActionType = action.Type.ToString(),
                    MouseX = mouseState.X,
                    MouseY = mouseState.Y,
                    MouseDeltaX = action.MouseDeltaX,
                    MouseDeltaY = action.MouseDeltaY,
                    HotkeyNumber = action.HotkeyNumber ?? 0
                });

                // 6. Check for Victory/Defeat every 3 seconds using visual detection
                // Skip first 10 seconds to avoid false positives from menu/loading screens
                var currentTime = stopwatch.Elapsed.TotalSeconds;
                if (currentTime > 10 && currentTime - lastEndGameCheck >= 3.0)
                {
                    lastEndGameCheck = currentTime;
                    var (endGameResult, diagnostics) = DetectEndGameFromScreenshotWithDiagnostics(screenshotBytes);
                    if (endGameResult != null)
                    {
                        outcome = endGameResult;
                        Console.WriteLine($"[{currentTime:F1}s] Game ended: {outcome}");

                        // Save screenshot for analysis
                        var debugDir = Path.Combine(Path.GetDirectoryName(modelPath) ?? ".", "debug");
                        Directory.CreateDirectory(debugDir);
                        var timestamp = DateTime.Now.ToString("HHmmss");
                        var debugFile = Path.Combine(debugDir, $"endgame_{outcome}_{timestamp}.png");
                        await File.WriteAllBytesAsync(debugFile, screenshotBytes);
                        Console.WriteLine($"[Debug] Saved screenshot: {debugFile}");
                        Console.WriteLine($"[Debug] Diagnostics: {diagnostics}");

                        break;
                    }
                }

                // Log progress every 10 frames
                if (frameCount % 10 == 0)
                {
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
        metrics.ActionLog = actionLog;

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

    /// <summary>
    /// Detects Victory/Defeat screen from screenshot with diagnostics.
    /// </summary>
    private static (string? result, string diagnostics) DetectEndGameFromScreenshotWithDiagnostics(byte[] screenshotBytes)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(screenshotBytes);
            if (bitmap == null) return (null, "bitmap decode failed");

            int width = bitmap.Width;
            int height = bitmap.Height;

            // First check if end-game dialog is visible (center 50% of screen)
            int startX = width * 25 / 100;
            int endX = width * 75 / 100;
            int startY = height * 25 / 100;
            int endY = height * 75 / 100;

            int darkCount = 0;
            int redCount = 0;
            int totalPixels = 0;

            for (int y = startY; y < endY; y += 4)
            {
                for (int x = startX; x < endX; x += 4)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    totalPixels++;

                    if (pixel.Red < 80 && pixel.Green < 80 && pixel.Blue < 80)
                        darkCount++;
                    else if (pixel.Red > 120 && pixel.Red > pixel.Green + 30 && pixel.Red > pixel.Blue + 30)
                        redCount++;
                }
            }

            float darkRatio = (float)darkCount / totalPixels;
            float redRatio = (float)redCount / totalPixels;

            // Check if end-game dialog is visible
            bool isEndGame = darkRatio > 0.55f && darkRatio < 0.85f && redRatio > 0.02f;

            if (!isEndGame)
                return (null, $"not endgame: dark={darkRatio:P1}, red={redRatio:P1}");

            // Key difference:
            // VICTORY: has golden text "YOU ARE VICTORIOUS" at bottom → goldRatio ~2.4%
            // DEFEAT: has white text "YOU WERE DEFEATED" at bottom → goldRatio ~0.1%

            // Check bottom text area (y=75-85%) for gold text
            var (_, goldRatio, bottomDiag) = ScanBottomTextColors(bitmap);

            string diag = $"size={width}x{height}, dark={darkRatio:P1}, red={redRatio:P1}, bottom_gold={goldRatio:P1}";

            // Simple threshold: VICTORY has ~2.4% gold, DEFEAT has ~0.1%
            // Use 0.5% as threshold
            bool isVictory = goldRatio > 0.005f;

            if (isVictory)
            {
                Console.WriteLine($"[EndGame] VICTORY detected (gold={goldRatio:P1})");
                return ("VICTORY", diag);
            }
            else
            {
                Console.WriteLine($"[EndGame] DEFEAT detected (gold={goldRatio:P1})");
                return ("DEFEAT", diag);
            }
        }
        catch (Exception ex)
        {
            return (null, $"error: {ex.Message}");
        }
    }

    /// <summary>
    /// Scans the bottom text area for white (DEFEAT) vs gold (VICTORY) text.
    /// DEFEAT: "YOU WERE DEFEATED" in white/gray
    /// VICTORY: "YOU ARE VICTORIOUS" in gold
    /// </summary>
    private static (float whiteRatio, float goldRatio, string diagnostics) ScanBottomTextColors(SKBitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Bottom text area: y=75-85%, x=30-70%
        int startX = width * 30 / 100;
        int endX = width * 70 / 100;
        int startY = height * 75 / 100;
        int endY = height * 85 / 100;

        int whiteCount = 0;
        int goldCount = 0;
        int total = 0;

        for (int y = startY; y < endY; y += 2)
        {
            for (int x = startX; x < endX; x += 2)
            {
                var pixel = bitmap.GetPixel(x, y);
                total++;

                // White/light gray text (DEFEAT text)
                if (pixel.Red > 180 && pixel.Green > 180 && pixel.Blue > 180 &&
                    Math.Abs(pixel.Red - pixel.Green) < 30 &&
                    Math.Abs(pixel.Red - pixel.Blue) < 30)
                {
                    whiteCount++;
                }

                // Gold/yellow text (VICTORY text)
                if (pixel.Red > 180 && pixel.Green > 150 && pixel.Blue < 100 &&
                    pixel.Red > pixel.Blue + 80)
                {
                    goldCount++;
                }
            }
        }

        float whiteRatio = total > 0 ? (float)whiteCount / total : 0;
        float goldRatio = total > 0 ? (float)goldCount / total : 0;
        return (whiteRatio, goldRatio, $"white={whiteCount}, gold={goldCount}, total={total}");
    }

    /// <summary>
    /// Scans a region for gold/yellow colored pixels.
    /// </summary>
    private static (float goldRatio, string diagnostics) ScanRegionForGold(
        SKBitmap bitmap, int yStartPercent, int yEndPercent, int xStartPercent, int xEndPercent)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        int startX = width * xStartPercent / 100;
        int endX = width * xEndPercent / 100;
        int startY = height * yStartPercent / 100;
        int endY = height * yEndPercent / 100;

        int goldCount = 0;
        int total = 0;

        for (int y = startY; y < endY; y += 2)
        {
            for (int x = startX; x < endX; x += 2)
            {
                var pixel = bitmap.GetPixel(x, y);
                total++;

                // Gold/yellow color detection for big "F" letter
                bool isGold = (pixel.Red > 170 && pixel.Green > 130 && pixel.Blue < 120) ||
                              (pixel.Red > 200 && pixel.Green > 180 && pixel.Blue < 100);

                if (isGold) goldCount++;
            }
        }

        float goldRatio = total > 0 ? (float)goldCount / total : 0;
        return (goldRatio, $"y={yStartPercent}-{yEndPercent}%, gold={goldCount}/{total}");
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

            case ActionType.DragSelect:
                // Drag-select: create selection box centered on mouse position
                int halfSize = ActionDecoder.DragSize / 2;
                int x1 = Math.Max(0, mouse.X - halfSize);
                int y1 = Math.Max(0, mouse.Y - halfSize);
                int x2 = Math.Min(1920, mouse.X + halfSize);
                int y2 = Math.Min(1080, mouse.Y + halfSize);
                await browser.DragSelectAsync(x1, y1, x2, y2);
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
    public List<ActionLogEntry>? ActionLog { get; set; }
}

/// <summary>
/// Log entry for a single action.
/// </summary>
public sealed class ActionLogEntry
{
    public double Timestamp { get; set; }
    public string ActionType { get; set; } = "";
    public int MouseX { get; set; }
    public int MouseY { get; set; }
    public float MouseDeltaX { get; set; }
    public float MouseDeltaY { get; set; }
    public int HotkeyNumber { get; set; }
}
