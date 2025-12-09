# Template Images for Game State Detection

This directory should contain template images for the TemplateMatchingDetector to identify game states.

## Required Templates

| Filename | Description | Approximate Region (1920x1080) |
|----------|-------------|--------------------------------|
| `victory_template.png` | Victory screen text/banner | Center top (576,216,768,216) |
| `defeat_template.png` | Defeat screen text/banner | Center top (576,216,768,216) |
| `ingame_minimap_template.png` | In-game minimap | Bottom-right (1440,810,480,270) |
| `mainmenu_template.png` | Main menu buttons | Center (576,324,768,432) |
| `loading_template.png` | Loading bar/screen | Bottom center (384,864,1152,108) |

## How to Capture Templates

### Option 1: Using the TemplateCapturer Tool

```bash
# Navigate to the tools directory
cd tools/TemplateCapturer

# Build the tool
dotnet build

# Run in interactive mode
dotnet run -- interactive

# Or capture specific regions
dotnet run -- capture --output templates/victory_template.png --region 576,216,768,216 --delay 3
```

### Option 2: Manual Capture

1. Start the Tzar game
2. Navigate to the screen you want to capture (victory, defeat, in-game, etc.)
3. Use Windows Snipping Tool or ShareX to capture the relevant region
4. Save as PNG with the appropriate filename

## Capture Checklist

- [ ] **Victory Screen**: Navigate to a completed game where you won. Capture the "VICTORY" text banner.
- [ ] **Defeat Screen**: Lose a game and capture the "DEFEAT" text banner.
- [ ] **In-Game (Minimap)**: During gameplay, capture the minimap region in the bottom-right corner.
- [ ] **Main Menu**: Capture the main menu button area.
- [ ] **Loading Screen**: Start a new game and capture the loading bar/progress indicator.

## Tips for Good Templates

1. **Resolution**: Templates are captured at a specific resolution. The detector supports multi-scale matching, but 1920x1080 is the reference resolution.

2. **Unique Features**: Choose regions with unique visual features that don't appear in other game states.

3. **Avoid Variable Content**: Don't include areas with variable content (player names, scores, minimap content).

4. **Consistent Lighting**: Capture in consistent lighting conditions.

5. **Clean Captures**: Ensure no UI overlays or tooltips are visible when capturing.

## Scaling for Different Resolutions

If your game runs at a different resolution, scale the region coordinates:

| Resolution | Scale Factor |
|------------|--------------|
| 1280x720 | 0.667 |
| 1600x900 | 0.833 |
| 1920x1080 | 1.000 (reference) |
| 2560x1440 | 1.333 |
| 3840x2160 | 2.000 |

Example: For 1280x720, multiply all coordinates and dimensions by 0.667.

## Verification

After capturing templates, verify they work by running the unit tests:

```bash
dotnet test TzarBot.Tests --filter "FullyQualifiedName~Phase5"
```

Or use the detector directly:

```csharp
var detector = new TemplateMatchingDetector(new DetectionConfig
{
    TemplateDirectory = "path/to/templates"
});
detector.Initialize();

var result = detector.Detect(capturedFrame);
Console.WriteLine($"Detected: {result.State} (confidence: {result.Confidence:P1})");
```
