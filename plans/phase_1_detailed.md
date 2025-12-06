# Phase 1: Game Interface - Detailed Plan

## Overview

The Game Interface is the foundation layer that enables communication between the Neural Network and the Tzar game. It consists of screen capture, input injection, and IPC modules.

## Task Dependency Diagram

```
F1.T1 (Project Setup)
   │
   ├──────────┬──────────┬──────────┐
   │          │          │          │
   ▼          ▼          ▼          │
F1.T2      F1.T3      F1.T4        │
(Screen)   (Input)    (IPC)        │
   │          │          │          │
   └──────────┼──────────┘          │
              │                     │
              ▼                     ▼
           F1.T5 ◄────────────── (Window)
              │
              ▼
           F1.T6
        (Integration)
```

## Definition of Done - Phase 1

- [ ] All 6 tasks completed with passing tests
- [ ] Bot can capture screen at 10+ FPS
- [ ] Bot can send mouse/keyboard events to game
- [ ] IPC communication works between processes
- [ ] Demo: screenshot saved + click executed in game window
- [ ] Git tag: `phase-1-complete`

---

## Task Definitions

### F1.T1: Project Setup

```yaml
task_id: "F1.T1"
name: "Project Setup"
description: |
  Create the .NET 8 solution structure with all required projects and NuGet packages.
  This establishes the foundation for all subsequent development.

inputs:
  - "plans/1general_plan.md (technology stack reference)"
  - "plans/2_implementation_workflow.md (directory structure)"

outputs:
  - "TzarBot.sln"
  - "src/TzarBot.GameInterface/TzarBot.GameInterface.csproj"
  - "src/TzarBot.Common/TzarBot.Common.csproj"
  - "tests/TzarBot.Tests/TzarBot.Tests.csproj"
  - ".editorconfig"
  - "Directory.Build.props"

test_command: "dotnet build TzarBot.sln"

test_criteria: |
  - Solution builds without errors
  - All projects reference .NET 8
  - Required NuGet packages installed:
    - Vortice.Windows (or SharpDX)
    - OpenCvSharp4
    - MessagePack
    - xUnit, FluentAssertions

dependencies: []
estimated_complexity: "S"

claude_prompt: |
  Create the .NET 8 solution structure for TzarBot project.

  ## Requirements

  1. Create solution file `TzarBot.sln` in the root directory

  2. Create the following projects:
     - `src/TzarBot.GameInterface/` - Class library for game interaction
     - `src/TzarBot.Common/` - Shared types and utilities
     - `tests/TzarBot.Tests/` - xUnit test project

  3. Add NuGet packages to TzarBot.GameInterface:
     - Vortice.Direct3D11 (for DXGI screen capture)
     - Vortice.DXGI
     - OpenCvSharp4
     - OpenCvSharp4.runtime.win
     - MessagePack

  4. Add NuGet packages to TzarBot.Tests:
     - xUnit
     - xUnit.runner.visualstudio
     - FluentAssertions
     - Microsoft.NET.Test.Sdk

  5. Create `.editorconfig` with C# coding standards

  6. Create `Directory.Build.props` with common settings:
     - <TargetFramework>net8.0</TargetFramework>
     - <Nullable>enable</Nullable>
     - <ImplicitUsings>enable</ImplicitUsings>
     - <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

  7. Create placeholder classes:
     - `src/TzarBot.Common/Models/ScreenFrame.cs`
     - `src/TzarBot.Common/Models/GameAction.cs`

  After completion, run: `dotnet build TzarBot.sln`

validation_steps:
  - "Verify all files created"
  - "Run dotnet restore"
  - "Run dotnet build"
  - "Verify no warnings or errors"

on_failure: |
  If build fails:
  1. Check NuGet package versions are compatible with .NET 8
  2. Verify Vortice.Windows supports your Windows version
  3. Check for missing runtime dependencies
```

---

### F1.T2: Screen Capture Implementation

```yaml
task_id: "F1.T2"
name: "Screen Capture Implementation"
description: |
  Implement screen capture using DXGI Desktop Duplication API.
  This module captures the game window at high FPS with minimal CPU overhead.

inputs:
  - "src/TzarBot.GameInterface/TzarBot.GameInterface.csproj"
  - "src/TzarBot.Common/Models/ScreenFrame.cs"
  - "plans/1general_plan.md (section 1.1)"

outputs:
  - "src/TzarBot.GameInterface/Capture/IScreenCapture.cs"
  - "src/TzarBot.GameInterface/Capture/DxgiScreenCapture.cs"
  - "src/TzarBot.GameInterface/Capture/ScreenCaptureException.cs"
  - "tests/TzarBot.Tests/Phase1/ScreenCaptureTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase1.ScreenCapture\""

test_criteria: |
  - All tests pass (exit code 0)
  - CaptureFrame() returns non-null ScreenFrame
  - Buffer size equals Width * Height * 4 (BGRA32)
  - Can capture at 10+ FPS for 5 seconds without crash
  - Memory does not grow during continuous capture

dependencies: ["F1.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement screen capture using DXGI Desktop Duplication API.

  ## Context
  The project is in `src/TzarBot.GameInterface/`. Use Vortice.Windows library.

  ## Requirements

  1. Create interface `IScreenCapture`:
     ```csharp
     public interface IScreenCapture : IDisposable
     {
         ScreenFrame? CaptureFrame();
         int Width { get; }
         int Height { get; }
         bool IsInitialized { get; }
     }
     ```

  2. Create `ScreenFrame` in Common project if not exists:
     ```csharp
     public class ScreenFrame
     {
         public byte[] Data { get; init; }
         public int Width { get; init; }
         public int Height { get; init; }
         public long TimestampTicks { get; init; }
         public PixelFormat Format { get; init; } // BGRA32
     }
     ```

  3. Implement `DxgiScreenCapture`:
     - Use DXGI 1.2 Desktop Duplication API
     - Target primary monitor (can be extended later)
     - Handle display mode changes gracefully
     - Implement proper disposal of D3D resources
     - Return null if no new frame available (duplicate)

  4. Create tests in `tests/TzarBot.Tests/Phase1/ScreenCaptureTests.cs`:
     - Test_CaptureFrame_ReturnsValidData
     - Test_ScreenFrame_HasCorrectDimensions
     - Test_BufferSize_MatchesDimensions
     - Test_ContinuousCapture_NoMemoryLeak (capture 100 frames, check memory)
     - Test_CaptureRate_AtLeast10Fps

  ## Technical Notes
  - DXGI Desktop Duplication requires Windows 8+
  - May need to run as admin or have specific permissions
  - Consider using Span<byte> for zero-copy when possible

  ## Example Usage
  ```csharp
  using var capture = new DxgiScreenCapture();
  var frame = capture.CaptureFrame();
  if (frame != null)
  {
      Console.WriteLine($"Captured {frame.Width}x{frame.Height}");
  }
  ```

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase1.ScreenCapture"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test: run capture, verify screenshot is saved"

on_failure: |
  If tests fail:
  1. Check if running with admin privileges
  2. Verify GPU drivers are up to date
  3. Check if desktop composition is enabled
  4. Try Software Renderer as fallback (WARP device)
  5. Check if secure desktop is blocking (screen saver, UAC)
```

---

### F1.T3: Input Injection Implementation

```yaml
task_id: "F1.T3"
name: "Input Injection Implementation"
description: |
  Implement mouse and keyboard input injection using Windows SendInput API.
  This module sends commands to the game as if from a real user.

inputs:
  - "src/TzarBot.GameInterface/TzarBot.GameInterface.csproj"
  - "src/TzarBot.Common/Models/GameAction.cs"
  - "plans/1general_plan.md (section 1.2)"

outputs:
  - "src/TzarBot.GameInterface/Input/IInputInjector.cs"
  - "src/TzarBot.GameInterface/Input/Win32InputInjector.cs"
  - "src/TzarBot.GameInterface/Input/InputAction.cs"
  - "src/TzarBot.GameInterface/Input/NativeMethods.cs"
  - "tests/TzarBot.Tests/Phase1/InputInjectorTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase1.InputInjector\""

test_criteria: |
  - All tests pass
  - Mouse moves to specified coordinates
  - Left/right click works
  - Keyboard input works
  - No exceptions thrown during rapid input
  - Cooldown between actions is enforced

dependencies: ["F1.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement input injection using Windows SendInput API.

  ## Context
  Project: `src/TzarBot.GameInterface/`. Use P/Invoke for Win32 API calls.

  ## Requirements

  1. Create interface `IInputInjector`:
     ```csharp
     public interface IInputInjector
     {
         void MoveMouse(int x, int y, bool absolute = true);
         void MoveMouseRelative(int dx, int dy);
         void LeftClick();
         void RightClick();
         void DoubleClick();
         void DragStart();
         void DragEnd();
         void PressKey(VirtualKey key);
         void ReleaseKey(VirtualKey key);
         void TypeKey(VirtualKey key);
         void TypeHotkey(VirtualKey modifier, VirtualKey key);
         TimeSpan MinActionDelay { get; set; }
     }
     ```

  2. Create `NativeMethods.cs` with P/Invoke declarations:
     - SendInput
     - GetSystemMetrics (for screen resolution)
     - INPUT, MOUSEINPUT, KEYBDINPUT structures

  3. Create `VirtualKey` enum with common keys:
     - Numbers 0-9
     - Letters A-Z
     - F1-F12
     - Ctrl, Alt, Shift
     - Escape, Enter, Space

  4. Implement `Win32InputInjector`:
     - Use SendInput API (not PostMessage/SendMessage)
     - Convert screen coordinates to absolute (0-65535 range)
     - Implement cooldown between actions (default 50ms)
     - Thread-safe implementation

  5. Create tests:
     - Test_MoveMouse_UpdatesPosition (mock or verify via GetCursorPos)
     - Test_Click_SendsInput (verify via event count)
     - Test_ActionCooldown_EnforcedMinDelay
     - Test_RelativeMove_CorrectDelta

  ## Important Notes
  - SendInput is more reliable than PostMessage for games
  - Some games may have input lag - document this
  - Coordinate conversion: x_abs = x * 65535 / screen_width

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase1.InputInjector"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test: open Notepad, inject keystrokes, verify text appears"

on_failure: |
  If input not working:
  1. Check if target window is focused
  2. Verify coordinates are within screen bounds
  3. Some games block SendInput - try different input methods
  4. Check if running as admin (some apps require it)
```

---

### F1.T4: IPC Named Pipes

```yaml
task_id: "F1.T4"
name: "IPC Named Pipes Implementation"
description: |
  Implement inter-process communication using Named Pipes.
  This enables the Neural Network process to communicate with the Game Interface.

inputs:
  - "src/TzarBot.GameInterface/TzarBot.GameInterface.csproj"
  - "src/TzarBot.Common/Models/ScreenFrame.cs"
  - "src/TzarBot.Common/Models/GameAction.cs"

outputs:
  - "src/TzarBot.GameInterface/IPC/IPipeServer.cs"
  - "src/TzarBot.GameInterface/IPC/PipeServer.cs"
  - "src/TzarBot.GameInterface/IPC/IPipeClient.cs"
  - "src/TzarBot.GameInterface/IPC/PipeClient.cs"
  - "src/TzarBot.GameInterface/IPC/Protocol.cs"
  - "tests/TzarBot.Tests/Phase1/IpcTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase1.Ipc\""

test_criteria: |
  - Server starts and accepts connections
  - Client connects within timeout
  - Binary messages are sent and received correctly
  - ScreenFrame data transfers without corruption
  - GameAction commands are received correctly
  - Connection recovery after disconnect

dependencies: ["F1.T1"]
estimated_complexity: "M"

claude_prompt: |
  Implement Named Pipes IPC for communication between processes.

  ## Context
  Project: `src/TzarBot.GameInterface/`. Use System.IO.Pipes.

  ## Requirements

  1. Define binary protocol in `Protocol.cs`:
     ```csharp
     public static class Protocol
     {
         public const string PipeName = "TzarBot";

         // Message types
         public const byte MSG_FRAME = 0x01;      // Server -> Client: Screen frame
         public const byte MSG_ACTION = 0x02;     // Client -> Server: Action to perform
         public const byte MSG_HEARTBEAT = 0x03;  // Bidirectional: Keep-alive
         public const byte MSG_STATUS = 0x04;     // Server -> Client: Game status

         // Frame format: [MSG_TYPE:1][FRAME_ID:4][WIDTH:2][HEIGHT:2][DATA:W*H*4]
         // Action format: [MSG_TYPE:1][FRAME_ID:4][ACTION_TYPE:1][PARAMS:...]
     }
     ```

  2. Create `IPipeServer` interface:
     ```csharp
     public interface IPipeServer : IDisposable
     {
         Task StartAsync(CancellationToken ct);
         Task SendFrameAsync(ScreenFrame frame, CancellationToken ct);
         event Action<GameAction>? OnActionReceived;
         event Action? OnClientConnected;
         event Action? OnClientDisconnected;
         bool IsClientConnected { get; }
     }
     ```

  3. Create `IPipeClient` interface:
     ```csharp
     public interface IPipeClient : IDisposable
     {
         Task ConnectAsync(TimeSpan timeout, CancellationToken ct);
         Task SendActionAsync(GameAction action, CancellationToken ct);
         event Action<ScreenFrame>? OnFrameReceived;
         bool IsConnected { get; }
     }
     ```

  4. Implement `PipeServer` and `PipeClient`:
     - Use NamedPipeServerStream / NamedPipeClientStream
     - Binary serialization with MessagePack
     - Handle disconnection gracefully
     - Support reconnection
     - Implement heartbeat (every 1 second)

  5. Create tests:
     - Test_ServerAcceptsConnection
     - Test_ClientConnects
     - Test_SendReceiveFrame
     - Test_SendReceiveAction
     - Test_ReconnectAfterDisconnect
     - Test_Heartbeat_KeepsConnectionAlive

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase1.Ipc"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test: run server and client in separate processes"

on_failure: |
  If connection fails:
  1. Check pipe name is correct and consistent
  2. Verify no other process is using the same pipe name
  3. Check Windows permissions for named pipes
  4. Increase connection timeout for slower systems
```

---

### F1.T5: Window Detection

```yaml
task_id: "F1.T5"
name: "Window Detection Implementation"
description: |
  Implement detection and tracking of the Tzar game window.
  This module finds the game window and provides its position and focus state.

inputs:
  - "src/TzarBot.GameInterface/TzarBot.GameInterface.csproj"

outputs:
  - "src/TzarBot.GameInterface/Window/IWindowDetector.cs"
  - "src/TzarBot.GameInterface/Window/WindowDetector.cs"
  - "src/TzarBot.GameInterface/Window/WindowInfo.cs"
  - "tests/TzarBot.Tests/Phase1/WindowDetectorTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase1.WindowDetector\""

test_criteria: |
  - FindWindow returns handle for known window
  - GetWindowRect returns correct dimensions
  - Focus detection works
  - Returns null for non-existent window
  - Handles window close/reopen

dependencies: ["F1.T1"]
estimated_complexity: "S"

claude_prompt: |
  Implement window detection using Win32 API.

  ## Context
  Project: `src/TzarBot.GameInterface/`. Use P/Invoke for Win32 API calls.

  ## Requirements

  1. Create `WindowInfo` class:
     ```csharp
     public class WindowInfo
     {
         public IntPtr Handle { get; init; }
         public string Title { get; init; }
         public string ClassName { get; init; }
         public Rectangle Bounds { get; init; }
         public Rectangle ClientBounds { get; init; }
         public bool IsFocused { get; init; }
         public bool IsMinimized { get; init; }
         public bool IsVisible { get; init; }
     }
     ```

  2. Create interface `IWindowDetector`:
     ```csharp
     public interface IWindowDetector
     {
         WindowInfo? FindWindow(string titlePattern);
         WindowInfo? FindWindowByClass(string className);
         WindowInfo? FindWindowByProcess(string processName);
         WindowInfo? GetWindowInfo(IntPtr handle);
         bool SetForeground(IntPtr handle);
         IEnumerable<WindowInfo> EnumerateWindows();
     }
     ```

  3. Implement `WindowDetector` with Win32 calls:
     - FindWindow / FindWindowEx
     - GetWindowRect / GetClientRect
     - GetWindowText
     - IsWindowVisible, IsIconic
     - GetForegroundWindow
     - SetForegroundWindow
     - EnumWindows

  4. Add Tzar-specific constants:
     ```csharp
     public static class TzarWindow
     {
         public const string WindowTitle = "Tzar"; // Adjust based on actual title
         public const string ClassName = "TzarClass"; // Discover actual class name
     }
     ```

  5. Create tests:
     - Test_FindWindow_ReturnsNullForNonExistent
     - Test_FindWindow_FindsNotepad (using Notepad as test target)
     - Test_GetWindowInfo_ReturnsCorrectBounds
     - Test_EnumerateWindows_FindsMultiple

  After completion, run:
  `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase1.WindowDetector"`

validation_steps:
  - "Check all files created"
  - "Run dotnet build"
  - "Run tests"
  - "Manual test: open Notepad, verify it's detected"

on_failure: |
  If window not found:
  1. Use Spy++ or similar to find exact window class/title
  2. Check if window title contains additional text
  3. Try FindWindow with NULL class name
  4. Verify EnumWindows callback is working
```

---

### F1.T6: Integration & Smoke Tests

```yaml
task_id: "F1.T6"
name: "Integration & Smoke Tests"
description: |
  Create integration tests that verify all Phase 1 components work together.
  This includes a demo application that captures screen and injects input.

inputs:
  - "All Phase 1 source files"
  - "Working Tzar game installation (for manual testing)"

outputs:
  - "tests/TzarBot.Tests/Phase1/IntegrationTests.cs"
  - "src/TzarBot.GameInterface.Demo/Program.cs"
  - "src/TzarBot.GameInterface.Demo/TzarBot.GameInterface.Demo.csproj"

test_command: "dotnet test tests/TzarBot.Tests --filter \"FullyQualifiedName~Phase1.Integration\""

test_criteria: |
  - All integration tests pass
  - Demo app runs without crash
  - Screen capture + input injection work in sequence
  - IPC transfers frame data correctly
  - Demo can be run manually with game

dependencies: ["F1.T2", "F1.T3", "F1.T4", "F1.T5"]
estimated_complexity: "M"

claude_prompt: |
  Create integration tests and demo application for Phase 1.

  ## Context
  All Phase 1 components are implemented. Now verify they work together.

  ## Requirements

  1. Create demo console app `src/TzarBot.GameInterface.Demo/`:
     ```csharp
     // Demo workflow:
     // 1. Find Tzar window (or any window for testing)
     // 2. Start screen capture
     // 3. Start IPC server
     // 4. Capture 10 frames, save first and last as PNG
     // 5. Move mouse to center of window
     // 6. Perform left click
     // 7. Print statistics (FPS, latency, etc.)
     ```

  2. Create integration tests:
     ```csharp
     [Collection("Integration")]
     public class IntegrationTests
     {
         [Fact]
         public async Task ScreenCapture_To_IPC_TransfersData()
         {
             // Start server
             // Capture frame
             // Send via IPC
             // Receive on client
             // Verify data matches
         }

         [Fact]
         public void WindowDetection_And_InputInjection_WorkTogether()
         {
             // Find Notepad window
             // Click in center
             // Type some text
             // Verify (via window title or other means)
         }

         [Fact]
         public async Task FullLoop_CaptureDecideAct()
         {
             // Simulate full bot loop:
             // Capture -> Send to "NN" (mock) -> Receive action -> Execute
         }
     }
     ```

  3. Add performance metrics to demo:
     - Frames per second
     - Capture latency
     - IPC transfer time
     - Total loop time

  4. Create `README.md` in Demo project with:
     - How to run
     - Expected output
     - Troubleshooting tips

  ## Smoke Test Checklist
  - [ ] Demo starts without error
  - [ ] Window detection finds target
  - [ ] 10 frames captured successfully
  - [ ] PNG files saved correctly
  - [ ] Mouse click executed
  - [ ] FPS >= 10
  - [ ] No memory leaks after 1 minute

  After completion:
  1. Run: `dotnet test tests/TzarBot.Tests --filter "FullyQualifiedName~Phase1.Integration"`
  2. Run: `dotnet run --project src/TzarBot.GameInterface.Demo`

validation_steps:
  - "All integration tests pass"
  - "Demo runs and produces output"
  - "PNG screenshots are valid"
  - "Performance metrics are acceptable"

on_failure: |
  If integration fails:
  1. Run individual component tests first
  2. Check for resource contention (multiple capture instances)
  3. Verify IPC pipe names match
  4. Check timing issues (add delays if needed)
  5. Review error logs for specific failure
```

---

## Rollback Plan

If Phase 1 implementation fails fundamentally:

1. **Alternative Screen Capture**: Use GDI BitBlt instead of DXGI
   - Slower but more compatible
   - `Graphics.CopyFromScreen()` in System.Drawing

2. **Alternative Input**: Use Windows.Forms automation
   - `System.Windows.Forms.SendKeys`
   - Less reliable but simpler

3. **Alternative IPC**: Use TCP sockets instead of Named Pipes
   - `System.Net.Sockets.TcpListener/TcpClient`
   - Works cross-machine if needed later

---

## API Documentation

### ScreenCapture API

```csharp
// Initialize capture
using var capture = new DxgiScreenCapture();

// Capture frame (returns null if no new frame)
var frame = capture.CaptureFrame();

// Access frame data
byte[] rawData = frame.Data;           // BGRA32 format
int width = frame.Width;
int height = frame.Height;
long timestamp = frame.TimestampTicks;
```

### InputInjector API

```csharp
var injector = new Win32InputInjector();

// Mouse operations
injector.MoveMouse(500, 300);          // Absolute position
injector.MoveMouseRelative(10, -5);    // Relative movement
injector.LeftClick();
injector.RightClick();
injector.DoubleClick();

// Drag operation
injector.MoveMouse(100, 100);
injector.DragStart();
injector.MoveMouse(200, 200);
injector.DragEnd();

// Keyboard
injector.TypeKey(VirtualKey.A);
injector.TypeHotkey(VirtualKey.Control, VirtualKey.S);  // Ctrl+S
```

### IPC API

```csharp
// Server side (Game Interface)
using var server = new PipeServer();
await server.StartAsync(cts.Token);
server.OnActionReceived += action => ExecuteAction(action);
await server.SendFrameAsync(frame, cts.Token);

// Client side (Neural Network)
using var client = new PipeClient();
await client.ConnectAsync(TimeSpan.FromSeconds(5), cts.Token);
client.OnFrameReceived += frame => ProcessFrame(frame);
await client.SendActionAsync(action, cts.Token);
```

### WindowDetector API

```csharp
var detector = new WindowDetector();

// Find Tzar window
var window = detector.FindWindow("Tzar");
if (window != null)
{
    Console.WriteLine($"Found at {window.Bounds}");
    detector.SetForeground(window.Handle);
}
```

---

*Phase 1 Detailed Plan - Version 1.0*
*See prompts/phase_1/ for individual task prompts*
