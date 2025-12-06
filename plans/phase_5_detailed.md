# Phase 5: Game State Detection - Detailed Plan

## Overview

The Game State Detection module recognizes the current state of the game (in-game, victory, defeat, menu, etc.) by analyzing screenshots. This is critical for determining when games end and collecting results.

## Task Dependency Diagram

```
F5.T1 (Template Capture)
   │
   ▼
F5.T2 (GameStateDetector)
   │
   ├──────────────┐
   │              │
   ▼              ▼
F5.T3          F5.T4
(GameMonitor)  (Stats OCR)
```

## Definition of Done - Phase 5

- [ ] All 4 tasks completed with passing tests
- [ ] Templates captured for all game states
- [ ] Victory/Defeat detection accuracy > 99%
- [ ] In-game detection accuracy > 95%
- [ ] GameMonitor can track game from start to end
- [ ] Demo: detect game state changes in real-time
- [ ] Git tag: `phase-5-complete`

---

## Task Definitions

### F5.T1: Template Capture Tool

```yaml
task_id: "F5.T1"
name: "Template Capture Tool"
description: |
  Create a tool to capture and save template images from the game
  for use in template matching. These templates will be used to
  detect game states like Victory, Defeat, and Menu screens.

inputs:
  - "src/TzarBot.GameInterface/Capture/DxgiScreenCapture.cs"
  - "Running Tzar game (for capturing templates)"

outputs:
  - "tools/TemplateCapturer/TemplateCapturer.csproj"
  - "tools/TemplateCapturer/Program.cs"
  - "assets/templates/victory.png"
  - "assets/templates/defeat.png"
  - "assets/templates/menu.png"
  - "assets/templates/in_game_minimap.png"
  - "assets/templates/loading.png"
  - "assets/templates/template_manifest.json"

test_command: "dotnet run --project tools/TemplateCapturer -- --verify"

test_criteria: |
  - Tool runs and captures screenshots
  - Templates can be saved to files
  - Region selection works (full or partial)
  - Template manifest is generated
  - Templates load correctly for matching

dependencies: ["F1.T2"]
estimated_complexity: "S"

claude_prompt: |
  Create a template capture tool for game state detection.

  ## Context
  Create tool in `tools/TemplateCapturer/`. This tool captures screenshots
  from the game and saves regions as templates.

  ## Requirements

  1. Create console application:
     ```csharp
     // Usage:
     // TemplateCapturer.exe --capture <name> --region <x,y,w,h>
     // TemplateCapturer.exe --capture victory --fullscreen
     // TemplateCapturer.exe --list
     // TemplateCapturer.exe --verify

     class Program
     {
         static async Task Main(string[] args)
         {
             var parser = new CommandLineParser(args);

             if (parser.HasFlag("capture"))
             {
                 await CaptureTemplate(
                     parser.GetValue("capture"),
                     parser.GetRegion("region"));
             }
             else if (parser.HasFlag("list"))
             {
                 ListTemplates();
             }
             else if (parser.HasFlag("verify"))
             {
                 await VerifyTemplates();
             }
             else
             {
                 PrintHelp();
             }
         }
     }
     ```

  2. Implement capture functionality:
     ```csharp
     static async Task CaptureTemplate(string name, Rectangle? region)
     {
         Console.WriteLine($"Capturing template: {name}");
         Console.WriteLine("Position the game to show the target screen...");
         Console.WriteLine("Press ENTER when ready...");
         Console.ReadLine();

         using var capture = new DxgiScreenCapture();
         var frame = capture.CaptureFrame();

         if (frame == null)
         {
             Console.WriteLine("Failed to capture screen!");
             return;
         }

         using var mat = CreateMatFromFrame(frame);

         Mat template;
         if (region.HasValue)
         {
             template = new Mat(mat, new Rect(
                 region.Value.X, region.Value.Y,
                 region.Value.Width, region.Value.Height));
         }
         else
         {
             template = mat;
         }

         var path = $"assets/templates/{name}.png";
         Cv2.ImWrite(path, template);
         Console.WriteLine($"Saved: {path}");

         // Update manifest
         UpdateManifest(name, region, frame.Width, frame.Height);
     }
     ```

  3. Create template manifest:
     ```csharp
     public class TemplateManifest
     {
         public Dictionary<string, TemplateInfo> Templates { get; set; }
         public int SourceWidth { get; set; }
         public int SourceHeight { get; set; }
         public DateTime CreatedAt { get; set; }
     }

     public class TemplateInfo
     {
         public string Name { get; set; }
         public string FilePath { get; set; }
         public Rectangle? Region { get; set; }  // null = fullscreen
         public float MatchThreshold { get; set; } = 0.8f;
         public string Description { get; set; }
     }
     ```

  4. Create verification:
     ```csharp
     static async Task VerifyTemplates()
     {
         var manifest = LoadManifest();
         Console.WriteLine($"Verifying {manifest.Templates.Count} templates...");

         foreach (var (name, info) in manifest.Templates)
         {
             if (!File.Exists(info.FilePath))
             {
                 Console.WriteLine($"[MISSING] {name}: {info.FilePath}");
                 continue;
             }

             using var template = Cv2.ImRead(info.FilePath);
             Console.WriteLine($"[OK] {name}: {template.Width}x{template.Height}");
         }
     }
     ```

  5. Default templates to capture:
     ```json
     {
       "templates": {
         "victory": {
           "filePath": "assets/templates/victory.png",
           "region": null,
           "matchThreshold": 0.85,
           "description": "Victory screen showing after winning a game"
         },
         "defeat": {
           "filePath": "assets/templates/defeat.png",
           "region": null,
           "matchThreshold": 0.85,
           "description": "Defeat screen showing after losing a game"
         },
         "menu_main": {
           "filePath": "assets/templates/menu_main.png",
           "region": {"x": 0, "y": 0, "width": 400, "height": 100},
           "matchThreshold": 0.8,
           "description": "Main menu header area"
         },
         "in_game_minimap": {
           "filePath": "assets/templates/in_game_minimap.png",
           "region": {"x": 0, "y": 0, "width": 200, "height": 200},
           "matchThreshold": 0.7,
           "description": "Corner of screen where minimap appears"
         },
         "loading": {
           "filePath": "assets/templates/loading.png",
           "region": null,
           "matchThreshold": 0.75,
           "description": "Loading screen between games"
         }
       },
       "sourceWidth": 1920,
       "sourceHeight": 1080,
       "createdAt": "2024-01-15T10:00:00Z"
     }
     ```

  ## Manual Steps After Implementation
  1. Run the game
  2. Navigate to each screen state
  3. Use tool to capture each template
  4. Verify all templates are saved

  After completion:
  1. Run: `dotnet build tools/TemplateCapturer`
  2. Run: `dotnet run --project tools/TemplateCapturer -- --verify`

validation_steps:
  - "Tool compiles successfully"
  - "Can capture screenshot"
  - "Region extraction works"
  - "Manifest is created/updated"
  - "Templates are valid PNG files"

on_failure: |
  If capture fails:
  1. Ensure game is running and visible
  2. Check screen capture permissions
  3. Verify DXGI capture works
  4. Try running as administrator
```

---

### F5.T2: GameStateDetector

```yaml
task_id: "F5.T2"
name: "GameStateDetector Implementation"
description: |
  Implement the core game state detection using template matching
  and other image analysis techniques.

inputs:
  - "assets/templates/*.png"
  - "assets/templates/template_manifest.json"
  - "src/TzarBot.GameInterface/Capture/DxgiScreenCapture.cs"

outputs:
  - "src/TzarBot.StateDetection/TzarBot.StateDetection.csproj"
  - "src/TzarBot.StateDetection/Detection/GameState.cs"
  - "src/TzarBot.StateDetection/Detection/IGameStateDetector.cs"
  - "src/TzarBot.StateDetection/Detection/TemplateMatchingDetector.cs"
  - "src/TzarBot.StateDetection/Detection/DetectionResult.cs"
  - "tests/TzarBot.Tests/Phase5/GameStateDetectorTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase5.GameStateDetector\""

test_criteria: |
  - Detector loads templates successfully
  - Template matching works on test images
  - Victory detection accuracy > 99%
  - Defeat detection accuracy > 99%
  - In-game detection accuracy > 95%
  - Detection time < 50ms per frame

dependencies: ["F5.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement game state detection using template matching.

  ## Context
  Create new project `src/TzarBot.StateDetection/`. Use OpenCvSharp4.

  ## Requirements

  1. Create `GameState` enum:
     ```csharp
     public enum GameState
     {
         Unknown,
         Loading,
         MainMenu,
         InGame,
         Paused,
         Victory,
         Defeat,
         Crashed
     }
     ```

  2. Create `DetectionResult`:
     ```csharp
     public class DetectionResult
     {
         public GameState State { get; set; }
         public float Confidence { get; set; }
         public TimeSpan DetectionTime { get; set; }
         public Dictionary<string, float> TemplateScores { get; set; }
         public DateTime Timestamp { get; set; }
     }
     ```

  3. Create interface:
     ```csharp
     public interface IGameStateDetector
     {
         DetectionResult Detect(ScreenFrame frame);
         DetectionResult Detect(Mat image);
         void ReloadTemplates();
         IReadOnlyDictionary<string, float> LastMatchScores { get; }
     }
     ```

  4. Implement `TemplateMatchingDetector`:
     ```csharp
     public class TemplateMatchingDetector : IGameStateDetector
     {
         private readonly Dictionary<string, Mat> _templates;
         private readonly TemplateManifest _manifest;
         private readonly Dictionary<string, float> _lastScores;

         public TemplateMatchingDetector(string templatesPath)
         {
             _manifest = LoadManifest(templatesPath);
             _templates = new Dictionary<string, Mat>();

             foreach (var (name, info) in _manifest.Templates)
             {
                 _templates[name] = Cv2.ImRead(info.FilePath);
             }
         }

         public DetectionResult Detect(Mat image)
         {
             var sw = Stopwatch.StartNew();
             var scores = new Dictionary<string, float>();

             // Check each template
             foreach (var (name, template) in _templates)
             {
                 var score = MatchTemplate(image, template);
                 scores[name] = score;
             }

             _lastScores = scores;

             // Determine state based on scores
             var state = DetermineState(scores);

             return new DetectionResult
             {
                 State = state,
                 Confidence = GetConfidence(scores, state),
                 DetectionTime = sw.Elapsed,
                 TemplateScores = scores,
                 Timestamp = DateTime.UtcNow
             };
         }

         private float MatchTemplate(Mat image, Mat template)
         {
             // Handle size mismatch
             if (template.Width > image.Width || template.Height > image.Height)
             {
                 return 0f;
             }

             using var result = new Mat();
             Cv2.MatchTemplate(image, template, result, TemplateMatchModes.CCoeffNormed);
             Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

             return (float)maxVal;
         }

         private GameState DetermineState(Dictionary<string, float> scores)
         {
             var threshold = 0.8f;

             // Priority order: Victory/Defeat > Menu > InGame

             if (scores.GetValueOrDefault("victory", 0) > threshold)
                 return GameState.Victory;

             if (scores.GetValueOrDefault("defeat", 0) > threshold)
                 return GameState.Defeat;

             if (scores.GetValueOrDefault("menu_main", 0) > 0.75f)
                 return GameState.MainMenu;

             if (scores.GetValueOrDefault("loading", 0) > 0.75f)
                 return GameState.Loading;

             // For in-game, check minimap presence
             if (scores.GetValueOrDefault("in_game_minimap", 0) > 0.6f)
                 return GameState.InGame;

             return GameState.Unknown;
         }

         private float GetConfidence(Dictionary<string, float> scores, GameState state)
         {
             return state switch
             {
                 GameState.Victory => scores.GetValueOrDefault("victory", 0),
                 GameState.Defeat => scores.GetValueOrDefault("defeat", 0),
                 GameState.MainMenu => scores.GetValueOrDefault("menu_main", 0),
                 GameState.Loading => scores.GetValueOrDefault("loading", 0),
                 GameState.InGame => scores.GetValueOrDefault("in_game_minimap", 0),
                 _ => 0f
             };
         }
     }
     ```

  5. Add color histogram analysis (backup method):
     ```csharp
     private GameState DetectByColorHistogram(Mat image)
     {
         // Victory screens often have gold/green tones
         // Defeat screens often have red tones

         using var hsv = new Mat();
         Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

         var hist = new Mat();
         Cv2.CalcHist(
             new[] { hsv },
             new[] { 0 },  // Hue channel
             null,
             hist,
             1,
             new[] { 180 },
             new[] { new Rangef(0, 180) });

         // Analyze histogram for characteristic colors
         // ...
     }
     ```

  6. Create tests:
     ```csharp
     public class GameStateDetectorTests
     {
         private readonly TemplateMatchingDetector _detector;

         [Fact]
         public void Detect_VictoryScreen_ReturnsVictory()
         {
             var image = Cv2.ImRead("test_images/victory_screen.png");
             var result = _detector.Detect(image);

             Assert.Equal(GameState.Victory, result.State);
             Assert.True(result.Confidence > 0.85f);
         }

         [Fact]
         public void Detect_DefeatScreen_ReturnsDefeat()
         {
             var image = Cv2.ImRead("test_images/defeat_screen.png");
             var result = _detector.Detect(image);

             Assert.Equal(GameState.Defeat, result.State);
         }

         [Fact]
         public void Detect_InGame_ReturnsInGame()
         {
             var image = Cv2.ImRead("test_images/in_game.png");
             var result = _detector.Detect(image);

             Assert.Equal(GameState.InGame, result.State);
         }

         [Fact]
         public void DetectionTime_Under50ms()
         {
             var image = Cv2.ImRead("test_images/in_game.png");
             var result = _detector.Detect(image);

             Assert.True(result.DetectionTime.TotalMilliseconds < 50);
         }
     }
     ```

  ## Note
  Create test_images/ folder with sample screenshots for testing.

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase5.GameStateDetector"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests with sample images"
  - "Verify detection accuracy"
  - "Check detection speed"

on_failure: |
  If detection fails:
  1. Check template quality (resize, noise)
  2. Try different TemplateMatchModes
  3. Lower match threshold
  4. Use scale-invariant matching
  5. Add pre-processing (blur, normalize)
```

---

### F5.T3: GameMonitor

```yaml
task_id: "F5.T3"
name: "GameMonitor Implementation"
description: |
  Implement continuous game monitoring that tracks state changes,
  detects game end, and handles edge cases like crashes and timeouts.

inputs:
  - "src/TzarBot.StateDetection/Detection/TemplateMatchingDetector.cs"
  - "src/TzarBot.GameInterface/Capture/DxgiScreenCapture.cs"

outputs:
  - "src/TzarBot.StateDetection/Monitor/IGameMonitor.cs"
  - "src/TzarBot.StateDetection/Monitor/GameMonitor.cs"
  - "src/TzarBot.StateDetection/Monitor/MonitorConfig.cs"
  - "src/TzarBot.StateDetection/Monitor/MonitorResult.cs"
  - "tests/TzarBot.Tests/Phase5/GameMonitorTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase5.GameMonitor\""

test_criteria: |
  - Monitor detects game start/end
  - State changes are tracked
  - Timeout detection works
  - Crash detection works
  - Stuck detection works (no change for long time)
  - Events are fired correctly

dependencies: ["F5.T2"]
estimated_complexity: "M"

claude_prompt: |
  Implement continuous game state monitoring.

  ## Context
  Project: `src/TzarBot.StateDetection/`. Build on GameStateDetector.

  ## Requirements

  1. Create configuration:
     ```csharp
     public class MonitorConfig
     {
         public TimeSpan MaxGameDuration { get; set; } = TimeSpan.FromMinutes(30);
         public TimeSpan StuckTimeout { get; set; } = TimeSpan.FromMinutes(2);
         public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);
         public int ConsecutiveUnknownThreshold { get; set; } = 30; // 3 seconds at 100ms
         public int StateChangeConfirmationCount { get; set; } = 3;
     }
     ```

  2. Create `MonitorResult`:
     ```csharp
     public class MonitorResult
     {
         public GameOutcome Outcome { get; set; }
         public TimeSpan Duration { get; set; }
         public DateTime StartedAt { get; set; }
         public DateTime EndedAt { get; set; }
         public List<StateChangeEvent> StateHistory { get; set; }
         public string? ErrorMessage { get; set; }
         public ScreenFrame? FinalFrame { get; set; }
     }

     public class StateChangeEvent
     {
         public GameState FromState { get; set; }
         public GameState ToState { get; set; }
         public DateTime Timestamp { get; set; }
         public float Confidence { get; set; }
     }
     ```

  3. Create interface:
     ```csharp
     public interface IGameMonitor
     {
         Task<MonitorResult> MonitorGameAsync(CancellationToken ct);
         GameState CurrentState { get; }
         TimeSpan ElapsedTime { get; }
         bool IsMonitoring { get; }

         event Action<StateChangeEvent>? OnStateChanged;
         event Action<GameState>? OnGameEnded;
         event Action<string>? OnWarning;
     }
     ```

  4. Implement `GameMonitor`:
     ```csharp
     public class GameMonitor : IGameMonitor
     {
         private readonly IScreenCapture _capture;
         private readonly IGameStateDetector _detector;
         private readonly MonitorConfig _config;

         private GameState _currentState = GameState.Unknown;
         private GameState _pendingState = GameState.Unknown;
         private int _pendingStateCount = 0;
         private int _consecutiveUnknown = 0;
         private DateTime _lastActivityTime;
         private Mat? _lastFrame;

         public async Task<MonitorResult> MonitorGameAsync(CancellationToken ct)
         {
             var startTime = DateTime.UtcNow;
             var stateHistory = new List<StateChangeEvent>();
             _lastActivityTime = startTime;

             while (!ct.IsCancellationRequested)
             {
                 // Check timeout
                 var elapsed = DateTime.UtcNow - startTime;
                 if (elapsed > _config.MaxGameDuration)
                 {
                     return new MonitorResult
                     {
                         Outcome = GameOutcome.Timeout,
                         Duration = elapsed,
                         StartedAt = startTime,
                         EndedAt = DateTime.UtcNow,
                         StateHistory = stateHistory
                     };
                 }

                 // Capture frame
                 var frame = _capture.CaptureFrame();
                 if (frame == null)
                 {
                     await Task.Delay(_config.PollingInterval, ct);
                     continue;
                 }

                 using var mat = CreateMatFromFrame(frame);
                 var detection = _detector.Detect(mat);

                 // Handle state
                 var stateChange = ProcessDetection(detection, mat);
                 if (stateChange != null)
                 {
                     stateHistory.Add(stateChange);
                     OnStateChanged?.Invoke(stateChange);
                 }

                 // Check for game end
                 if (_currentState == GameState.Victory)
                 {
                     return new MonitorResult
                     {
                         Outcome = GameOutcome.Victory,
                         Duration = elapsed,
                         StartedAt = startTime,
                         EndedAt = DateTime.UtcNow,
                         StateHistory = stateHistory,
                         FinalFrame = frame
                     };
                 }

                 if (_currentState == GameState.Defeat)
                 {
                     return new MonitorResult
                     {
                         Outcome = GameOutcome.Defeat,
                         Duration = elapsed,
                         StartedAt = startTime,
                         EndedAt = DateTime.UtcNow,
                         StateHistory = stateHistory,
                         FinalFrame = frame
                     };
                 }

                 // Check for crash (consecutive unknown)
                 if (detection.State == GameState.Unknown)
                 {
                     _consecutiveUnknown++;
                     if (_consecutiveUnknown > _config.ConsecutiveUnknownThreshold)
                     {
                         if (!IsGameProcessRunning())
                         {
                             return new MonitorResult
                             {
                                 Outcome = GameOutcome.Crashed,
                                 Duration = elapsed,
                                 StateHistory = stateHistory,
                                 ErrorMessage = "Game process not responding"
                             };
                         }
                     }
                 }
                 else
                 {
                     _consecutiveUnknown = 0;
                 }

                 // Check for stuck (no significant change)
                 if (_currentState == GameState.InGame)
                 {
                     if (HasSignificantChange(mat, _lastFrame))
                     {
                         _lastActivityTime = DateTime.UtcNow;
                     }
                     else if (DateTime.UtcNow - _lastActivityTime > _config.StuckTimeout)
                     {
                         OnWarning?.Invoke("Bot appears to be stuck");
                         return new MonitorResult
                         {
                             Outcome = GameOutcome.Stuck,
                             Duration = elapsed,
                             StateHistory = stateHistory
                         };
                     }
                 }

                 _lastFrame?.Dispose();
                 _lastFrame = mat.Clone();

                 await Task.Delay(_config.PollingInterval, ct);
             }

             return new MonitorResult
             {
                 Outcome = GameOutcome.Cancelled,
                 Duration = DateTime.UtcNow - startTime,
                 StateHistory = stateHistory
             };
         }

         private StateChangeEvent? ProcessDetection(DetectionResult detection, Mat frame)
         {
             // Require multiple consistent detections for state change
             if (detection.State != _pendingState)
             {
                 _pendingState = detection.State;
                 _pendingStateCount = 1;
                 return null;
             }

             _pendingStateCount++;

             if (_pendingStateCount >= _config.StateChangeConfirmationCount &&
                 _pendingState != _currentState)
             {
                 var oldState = _currentState;
                 _currentState = _pendingState;

                 return new StateChangeEvent
                 {
                     FromState = oldState,
                     ToState = _currentState,
                     Timestamp = DateTime.UtcNow,
                     Confidence = detection.Confidence
                 };
             }

             return null;
         }

         private bool HasSignificantChange(Mat current, Mat? previous)
         {
             if (previous == null) return true;

             // Calculate frame difference
             using var diff = new Mat();
             Cv2.Absdiff(current, previous, diff);
             var meanDiff = Cv2.Mean(diff);

             // If average pixel difference > threshold, there's activity
             return meanDiff.Val0 > 5.0;
         }

         private bool IsGameProcessRunning()
         {
             var processes = Process.GetProcessesByName("Tzar");
             return processes.Any(p => !p.HasExited);
         }
     }
     ```

  5. Create tests:
     - Test_Monitor_DetectsVictory
     - Test_Monitor_DetectsDefeat
     - Test_Monitor_DetectsTimeout
     - Test_Monitor_DetectsStuck
     - Test_StateChange_RequiresConfirmation
     - Test_Events_AreEmitted

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase5.GameMonitor"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test with game running"

on_failure: |
  If monitoring fails:
  1. Check polling interval (too fast may cause issues)
  2. Verify state detection accuracy
  3. Adjust confirmation count
  4. Add more logging for debugging
```

---

### F5.T4: Stats Extraction (OCR)

```yaml
task_id: "F5.T4"
name: "Game Stats Extraction via OCR"
description: |
  Implement extraction of game statistics from the victory/defeat
  screen using OCR (Optical Character Recognition).

inputs:
  - "src/TzarBot.StateDetection/Detection/GameStateDetector.cs"
  - "Victory/Defeat screen screenshots"

outputs:
  - "src/TzarBot.StateDetection/Stats/IStatsExtractor.cs"
  - "src/TzarBot.StateDetection/Stats/OcrStatsExtractor.cs"
  - "src/TzarBot.StateDetection/Stats/GameStats.cs"
  - "src/TzarBot.StateDetection/Stats/StatsConfig.cs"
  - "tests/TzarBot.Tests/Phase5/StatsExtractionTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase5.StatsExtraction\""

test_criteria: |
  - OCR extracts text from result screen
  - Statistics are parsed correctly
  - Handles missing or unreadable values
  - Works on different resolutions
  - Extraction time < 500ms

dependencies: ["F5.T2"]
estimated_complexity: "M"

claude_prompt: |
  Implement game statistics extraction using OCR.

  ## Context
  Project: `src/TzarBot.StateDetection/`. Use Tesseract OCR via TesseractSharp or similar.

  ## Requirements

  1. Add NuGet packages:
     - Tesseract (or TesseractSharp)

  2. Create `GameStats`:
     ```csharp
     public class GameStats
     {
         // Basic stats
         public int? UnitsBuilt { get; set; }
         public int? UnitsLost { get; set; }
         public int? UnitsKilled { get; set; }
         public int? BuildingsBuilt { get; set; }
         public int? BuildingsLost { get; set; }
         public int? BuildingsDestroyed { get; set; }

         // Resources
         public int? GoldGathered { get; set; }
         public int? WoodGathered { get; set; }
         public int? FoodGathered { get; set; }

         // Time
         public TimeSpan? GameDuration { get; set; }

         // Metadata
         public float OcrConfidence { get; set; }
         public DateTime ExtractedAt { get; set; }
         public Dictionary<string, string> RawValues { get; set; }
     }
     ```

  3. Create configuration:
     ```csharp
     public class StatsConfig
     {
         // Regions of interest on result screen (relative coordinates 0-1)
         public Dictionary<string, RectangleF> StatRegions { get; set; } = new()
         {
             ["units_built"] = new RectangleF(0.3f, 0.3f, 0.1f, 0.02f),
             ["units_killed"] = new RectangleF(0.3f, 0.35f, 0.1f, 0.02f),
             // ... more regions
         };

         public string TesseractDataPath { get; set; } = "./tessdata";
         public string Language { get; set; } = "eng";
     }
     ```

  4. Create interface:
     ```csharp
     public interface IStatsExtractor
     {
         GameStats ExtractStats(ScreenFrame frame);
         GameStats ExtractStats(Mat image);
         bool IsResultScreen(Mat image);
     }
     ```

  5. Implement `OcrStatsExtractor`:
     ```csharp
     public class OcrStatsExtractor : IStatsExtractor, IDisposable
     {
         private readonly TesseractEngine _engine;
         private readonly StatsConfig _config;
         private readonly IGameStateDetector _detector;

         public OcrStatsExtractor(StatsConfig config)
         {
             _config = config;
             _engine = new TesseractEngine(
                 config.TesseractDataPath,
                 config.Language,
                 EngineMode.Default);

             // Configure for number recognition
             _engine.SetVariable("tessedit_char_whitelist", "0123456789:.");
         }

         public GameStats ExtractStats(Mat image)
         {
             var stats = new GameStats
             {
                 ExtractedAt = DateTime.UtcNow,
                 RawValues = new Dictionary<string, string>()
             };

             float totalConfidence = 0;
             int valueCount = 0;

             foreach (var (name, region) in _config.StatRegions)
             {
                 var absoluteRegion = ToAbsolute(region, image.Width, image.Height);
                 using var roi = new Mat(image, absoluteRegion);

                 // Preprocess for better OCR
                 using var processed = PreprocessForOcr(roi);

                 // Run OCR
                 using var pix = PixConverter.ToPix(processed);
                 using var page = _engine.Process(pix);

                 var text = page.GetText().Trim();
                 var confidence = page.GetMeanConfidence();

                 stats.RawValues[name] = text;
                 totalConfidence += confidence;
                 valueCount++;

                 // Parse value
                 SetStatValue(stats, name, text);
             }

             stats.OcrConfidence = valueCount > 0 ? totalConfidence / valueCount : 0;

             return stats;
         }

         private Mat PreprocessForOcr(Mat image)
         {
             var processed = new Mat();

             // Convert to grayscale
             Cv2.CvtColor(image, processed, ColorConversionCodes.BGR2GRAY);

             // Increase contrast
             Cv2.Threshold(processed, processed, 0, 255,
                 ThresholdTypes.Binary | ThresholdTypes.Otsu);

             // Scale up for better OCR
             Cv2.Resize(processed, processed, new Size(0, 0),
                 2.0, 2.0, InterpolationFlags.Cubic);

             return processed;
         }

         private void SetStatValue(GameStats stats, string name, string text)
         {
             if (!int.TryParse(text, out int value))
                 return;

             switch (name)
             {
                 case "units_built":
                     stats.UnitsBuilt = value;
                     break;
                 case "units_killed":
                     stats.UnitsKilled = value;
                     break;
                 case "buildings_built":
                     stats.BuildingsBuilt = value;
                     break;
                 // ... more cases
             }
         }

         public void Dispose()
         {
             _engine.Dispose();
         }
     }
     ```

  6. Create fallback regex extractor:
     ```csharp
     public class RegexStatsExtractor : IStatsExtractor
     {
         // Simpler approach: OCR the whole stats panel
         // Then use regex to extract numbers

         private static readonly Regex UnitsBuiltPattern =
             new Regex(@"Units Built:\s*(\d+)", RegexOptions.IgnoreCase);

         public GameStats ExtractStats(Mat image)
         {
             var text = OcrFullRegion(image);

             var stats = new GameStats();

             var match = UnitsBuiltPattern.Match(text);
             if (match.Success)
             {
                 stats.UnitsBuilt = int.Parse(match.Groups[1].Value);
             }

             // ... more patterns

             return stats;
         }
     }
     ```

  7. Create tests:
     - Test_ExtractStats_FromVictoryScreen
     - Test_ExtractStats_FromDefeatScreen
     - Test_OcrPreprocessing_ImprovesAccuracy
     - Test_HandlesMissingValues
     - Test_ExtractionTime_Under500ms

  ## Notes
  - Download Tesseract trained data from GitHub
  - Place in tessdata/ folder
  - Consider training custom model for game font

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase5.StatsExtraction"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests with sample screenshots"
  - "Verify OCR accuracy"

on_failure: |
  If OCR fails:
  1. Check Tesseract data is installed correctly
  2. Improve image preprocessing
  3. Adjust region coordinates
  4. Try different OCR engine modes
  5. Consider custom training for game font
```

---

## Rollback Plan

If Phase 5 implementation fails:

1. **Manual Detection**: Simple color-based detection
   - Victory = high green/gold in center
   - Defeat = high red in center
   - Less accurate but simpler

2. **Skip Stats Extraction**: Focus only on win/loss detection
   - Stats are nice-to-have
   - Core functionality is game outcome

3. **Timeout-Only Mode**: Use only timeout detection
   - Assume game ends at timeout
   - Less informative but works

---

## API Documentation

### GameStateDetector API

```csharp
var detector = new TemplateMatchingDetector("assets/templates");

// Detect from screen frame
var frame = capture.CaptureFrame();
var result = detector.Detect(frame);

Console.WriteLine($"State: {result.State}, Confidence: {result.Confidence:P}");
```

### GameMonitor API

```csharp
var monitor = new GameMonitor(capture, detector, config);

monitor.OnStateChanged += change =>
{
    Console.WriteLine($"{change.FromState} -> {change.ToState}");
};

var result = await monitor.MonitorGameAsync(ct);

Console.WriteLine($"Game ended: {result.Outcome} after {result.Duration}");
```

### StatsExtractor API

```csharp
var extractor = new OcrStatsExtractor(config);

if (monitor.CurrentState == GameState.Victory)
{
    var frame = capture.CaptureFrame();
    var stats = extractor.ExtractStats(frame);

    Console.WriteLine($"Units Built: {stats.UnitsBuilt}");
    Console.WriteLine($"Enemies Killed: {stats.UnitsKilled}");
}
```

---

*Phase 5 Detailed Plan - Version 1.0*
*See prompts/phase_5/ for individual task prompts*
