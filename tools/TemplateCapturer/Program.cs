using System.CommandLine;
using OpenCvSharp;
using TzarBot.GameInterface.Capture;
using TzarBot.Common.Models;

namespace TzarBot.Tools.TemplateCapturer;

/// <summary>
/// Console tool for capturing game screen templates for state detection.
///
/// Usage:
///   TemplateCapturer capture --output templates/victory_template.png --region 576,216,768,216
///   TemplateCapturer capture --output templates/minimap_template.png --region 1440,810,480,270
///   TemplateCapturer fullscreen --output templates/screenshot.png
///   TemplateCapturer interactive
///
/// The tool captures screen regions and saves them as PNG templates for use
/// with the TemplateMatchingDetector.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Template Capturer - Capture game screen templates for state detection");

        // Full screen capture command
        var fullscreenCommand = new Command("fullscreen", "Capture the entire screen");
        var fullOutputOption = new Option<string>("--output", () => "screenshot.png", "Output file path");
        var delayOption = new Option<int>("--delay", () => 0, "Delay in seconds before capture");
        fullscreenCommand.AddOption(fullOutputOption);
        fullscreenCommand.AddOption(delayOption);
        fullscreenCommand.SetHandler(CaptureFullScreen, fullOutputOption, delayOption);
        rootCommand.AddCommand(fullscreenCommand);

        // Region capture command
        var captureCommand = new Command("capture", "Capture a specific screen region");
        var outputOption = new Option<string>("--output", "Output file path") { IsRequired = true };
        var regionOption = new Option<string>("--region", "Region to capture: x,y,width,height") { IsRequired = true };
        var captureDelayOption = new Option<int>("--delay", () => 0, "Delay in seconds before capture");
        captureCommand.AddOption(outputOption);
        captureCommand.AddOption(regionOption);
        captureCommand.AddOption(captureDelayOption);
        captureCommand.SetHandler(CaptureRegion, outputOption, regionOption, captureDelayOption);
        rootCommand.AddCommand(captureCommand);

        // Interactive capture command
        var interactiveCommand = new Command("interactive", "Interactive mode - press keys to capture predefined regions");
        interactiveCommand.SetHandler(InteractiveCapture);
        rootCommand.AddCommand(interactiveCommand);

        // List predefined regions command
        var listCommand = new Command("list", "List predefined capture regions for Tzar");
        listCommand.SetHandler(ListPredefinedRegions);
        rootCommand.AddCommand(listCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task CaptureFullScreen(string output, int delaySeconds)
    {
        if (delaySeconds > 0)
        {
            Console.WriteLine($"Capturing in {delaySeconds} seconds...");
            for (int i = delaySeconds; i > 0; i--)
            {
                Console.WriteLine($"  {i}...");
                await Task.Delay(1000);
            }
        }

        using var capture = new DxgiScreenCapture();
        var frame = capture.CaptureFrame();

        if (frame == null)
        {
            Console.WriteLine("ERROR: Failed to capture screen. Is DXGI available?");
            return;
        }

        SaveFrameAsPng(frame, output);
        Console.WriteLine($"Full screen captured: {output} ({frame.Width}x{frame.Height})");
    }

    private static async Task CaptureRegion(string output, string region, int delaySeconds)
    {
        var (x, y, width, height) = ParseRegion(region);
        if (width <= 0 || height <= 0)
        {
            Console.WriteLine("ERROR: Invalid region format. Use: x,y,width,height");
            return;
        }

        if (delaySeconds > 0)
        {
            Console.WriteLine($"Capturing in {delaySeconds} seconds...");
            for (int i = delaySeconds; i > 0; i--)
            {
                Console.WriteLine($"  {i}...");
                await Task.Delay(1000);
            }
        }

        using var capture = new DxgiScreenCapture();
        var frame = capture.CaptureFrame();

        if (frame == null)
        {
            Console.WriteLine("ERROR: Failed to capture screen. Is DXGI available?");
            return;
        }

        // Extract region
        using var fullMat = ConvertFrameToMat(frame);

        // Validate bounds
        if (x < 0 || y < 0 || x + width > frame.Width || y + height > frame.Height)
        {
            Console.WriteLine($"WARNING: Region extends beyond screen bounds ({frame.Width}x{frame.Height})");
            x = Math.Max(0, Math.Min(x, frame.Width - 1));
            y = Math.Max(0, Math.Min(y, frame.Height - 1));
            width = Math.Min(width, frame.Width - x);
            height = Math.Min(height, frame.Height - y);
        }

        using var regionMat = new Mat(fullMat, new Rect(x, y, width, height));

        // Ensure output directory exists
        var dir = Path.GetDirectoryName(output);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        Cv2.ImWrite(output, regionMat);
        Console.WriteLine($"Region captured: {output} (region: {x},{y},{width},{height})");
    }

    private static async Task InteractiveCapture()
    {
        Console.WriteLine("=== Interactive Template Capture Mode ===");
        Console.WriteLine();
        Console.WriteLine("Instructions:");
        Console.WriteLine("  1. Navigate to the game screen you want to capture");
        Console.WriteLine("  2. Press the corresponding key to capture");
        Console.WriteLine("  3. Templates will be saved to the 'templates' directory");
        Console.WriteLine();
        Console.WriteLine("Capture Keys (for 1920x1080 resolution):");
        Console.WriteLine("  V - Victory screen region");
        Console.WriteLine("  D - Defeat screen region");
        Console.WriteLine("  M - Minimap region (in-game)");
        Console.WriteLine("  R - Resource bar region");
        Console.WriteLine("  N - Main menu region");
        Console.WriteLine("  L - Loading screen region");
        Console.WriteLine("  F - Full screen capture");
        Console.WriteLine("  Q - Quit");
        Console.WriteLine();

        var templateDir = "templates";
        if (!Directory.Exists(templateDir))
        {
            Directory.CreateDirectory(templateDir);
        }

        using var capture = new DxgiScreenCapture();

        while (true)
        {
            Console.Write("Press key (V/D/M/R/N/L/F/Q): ");
            var key = Console.ReadKey(true);
            Console.WriteLine(key.KeyChar.ToString().ToUpper());

            if (key.Key == ConsoleKey.Q)
            {
                Console.WriteLine("Exiting...");
                break;
            }

            // Small delay to switch to game window
            Console.WriteLine("Capturing in 2 seconds...");
            await Task.Delay(2000);

            var frame = capture.CaptureFrame();
            if (frame == null)
            {
                Console.WriteLine("ERROR: Failed to capture. Try again.");
                continue;
            }

            var (region, filename) = GetRegionForKey(key.Key, frame.Width, frame.Height);
            if (region == default)
            {
                Console.WriteLine("Unknown key. Try again.");
                continue;
            }

            var outputPath = Path.Combine(templateDir, filename);

            if (region.Width == frame.Width && region.Height == frame.Height)
            {
                // Full screen
                SaveFrameAsPng(frame, outputPath);
            }
            else
            {
                // Region
                using var fullMat = ConvertFrameToMat(frame);
                using var regionMat = new Mat(fullMat, region);
                Cv2.ImWrite(outputPath, regionMat);
            }

            Console.WriteLine($"Captured: {outputPath} ({region.Width}x{region.Height})");
        }
    }

    private static void ListPredefinedRegions()
    {
        Console.WriteLine("=== Predefined Capture Regions for Tzar (1920x1080) ===");
        Console.WriteLine();
        Console.WriteLine("These regions are approximate and should be calibrated for your setup:");
        Console.WriteLine();

        var regions = new Dictionary<string, (string Description, Rect Region)>
        {
            { "victory_template.png", ("Victory screen text region", new Rect(576, 216, 768, 216)) },
            { "defeat_template.png", ("Defeat screen text region", new Rect(576, 216, 768, 216)) },
            { "ingame_minimap_template.png", ("In-game minimap (bottom-right)", new Rect(1440, 810, 480, 270)) },
            { "mainmenu_template.png", ("Main menu buttons region", new Rect(576, 324, 768, 432)) },
            { "loading_template.png", ("Loading bar region", new Rect(384, 864, 1152, 108)) },
            { "resource_bar_template.png", ("Resource bar (top)", new Rect(0, 0, 1920, 54)) }
        };

        foreach (var (filename, (description, region)) in regions)
        {
            Console.WriteLine($"  {filename}");
            Console.WriteLine($"    Description: {description}");
            Console.WriteLine($"    Region: {region.X},{region.Y},{region.Width},{region.Height}");
            Console.WriteLine($"    Command: TemplateCapturer capture --output templates/{filename} --region {region.X},{region.Y},{region.Width},{region.Height}");
            Console.WriteLine();
        }

        Console.WriteLine("For other resolutions, scale the regions proportionally:");
        Console.WriteLine("  1920x1080 -> 1280x720: multiply all values by 0.667");
        Console.WriteLine("  1920x1080 -> 2560x1440: multiply all values by 1.333");
    }

    private static (Rect Region, string Filename) GetRegionForKey(ConsoleKey key, int screenWidth, int screenHeight)
    {
        // Scale factors for different resolutions (base: 1920x1080)
        float scaleX = screenWidth / 1920f;
        float scaleY = screenHeight / 1080f;

        return key switch
        {
            ConsoleKey.V => (ScaleRegion(new Rect(576, 216, 768, 216), scaleX, scaleY), "victory_template.png"),
            ConsoleKey.D => (ScaleRegion(new Rect(576, 216, 768, 216), scaleX, scaleY), "defeat_template.png"),
            ConsoleKey.M => (ScaleRegion(new Rect(1440, 810, 480, 270), scaleX, scaleY), "ingame_minimap_template.png"),
            ConsoleKey.R => (ScaleRegion(new Rect(0, 0, 1920, 54), scaleX, scaleY), "resource_bar_template.png"),
            ConsoleKey.N => (ScaleRegion(new Rect(576, 324, 768, 432), scaleX, scaleY), "mainmenu_template.png"),
            ConsoleKey.L => (ScaleRegion(new Rect(384, 864, 1152, 108), scaleX, scaleY), "loading_template.png"),
            ConsoleKey.F => (new Rect(0, 0, screenWidth, screenHeight), $"fullscreen_{DateTime.Now:yyyyMMdd_HHmmss}.png"),
            _ => (default, "")
        };
    }

    private static Rect ScaleRegion(Rect region, float scaleX, float scaleY)
    {
        return new Rect(
            (int)(region.X * scaleX),
            (int)(region.Y * scaleY),
            (int)(region.Width * scaleX),
            (int)(region.Height * scaleY)
        );
    }

    private static (int X, int Y, int Width, int Height) ParseRegion(string region)
    {
        try
        {
            var parts = region.Split(',');
            if (parts.Length != 4)
            {
                return (0, 0, 0, 0);
            }

            return (
                int.Parse(parts[0].Trim()),
                int.Parse(parts[1].Trim()),
                int.Parse(parts[2].Trim()),
                int.Parse(parts[3].Trim())
            );
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private static void SaveFrameAsPng(ScreenFrame frame, string path)
    {
        using var mat = ConvertFrameToMat(frame);

        // Ensure output directory exists
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        Cv2.ImWrite(path, mat);
    }

    private static Mat ConvertFrameToMat(ScreenFrame frame)
    {
        var matType = frame.Format switch
        {
            PixelFormat.BGRA32 => MatType.CV_8UC4,
            PixelFormat.RGB24 => MatType.CV_8UC3,
            PixelFormat.Grayscale8 => MatType.CV_8UC1,
            _ => throw new ArgumentException($"Unsupported pixel format: {frame.Format}")
        };

        var mat = new Mat(frame.Height, frame.Width, matType);

        unsafe
        {
            fixed (byte* dataPtr = frame.Data)
            {
                Buffer.MemoryCopy(
                    dataPtr,
                    (void*)mat.DataPointer,
                    frame.Data.Length,
                    frame.Data.Length);
            }
        }

        // Convert BGRA to BGR for saving
        if (frame.Format == PixelFormat.BGRA32)
        {
            using var bgra = mat;
            mat = new Mat();
            Cv2.CvtColor(bgra, mat, ColorConversionCodes.BGRA2BGR);
        }

        return mat;
    }
}
