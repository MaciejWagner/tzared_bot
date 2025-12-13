namespace TzarBot.BrowserInterface;

/// <summary>
/// Interface for controlling the Tzar game through a web browser (tza.red).
/// Each bot instance runs on a separate VM with its own browser.
/// </summary>
public interface IBrowserGameInterface : IAsyncDisposable
{
    /// <summary>
    /// Initializes the browser and navigates to tza.red.
    /// </summary>
    /// <param name="headless">Run browser in headless mode (no UI). Should be false on VM for screen capture.</param>
    Task InitializeAsync(bool headless = false);

    /// <summary>
    /// Indicates whether the browser is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Navigates to Skirmish (Potyczka z SI) mode and loads a map.
    /// </summary>
    /// <param name="mapPath">Path to the .tzared map file on the VM.</param>
    Task LoadMapAsync(string mapPath);

    /// <summary>
    /// Starts the game after map is loaded.
    /// </summary>
    Task StartGameAsync();

    /// <summary>
    /// Clicks at the specified screen coordinates (relative to game canvas).
    /// </summary>
    Task ClickAtAsync(int x, int y);

    /// <summary>
    /// Right-clicks at the specified screen coordinates.
    /// </summary>
    Task RightClickAtAsync(int x, int y);

    /// <summary>
    /// Performs a drag selection from start to end coordinates.
    /// </summary>
    Task DragSelectAsync(int startX, int startY, int endX, int endY);

    /// <summary>
    /// Presses a keyboard key.
    /// </summary>
    Task PressKeyAsync(string key);

    /// <summary>
    /// Takes a screenshot of the current game state.
    /// </summary>
    /// <returns>Screenshot as byte array (PNG format).</returns>
    Task<byte[]> TakeScreenshotAsync();

    /// <summary>
    /// Detects the current game state (InGame, Victory, Defeat, Menu, etc.).
    /// </summary>
    Task<GameStateResult> DetectGameStateAsync();

    /// <summary>
    /// Closes the browser and cleans up resources.
    /// </summary>
    Task CloseAsync();
}

/// <summary>
/// Result of game state detection.
/// </summary>
public record GameStateResult(
    GameState State,
    float Confidence,
    string? Message = null
);

/// <summary>
/// Possible game states.
/// </summary>
public enum GameState
{
    Unknown,
    MainMenu,
    SkirmishSetup,
    Loading,
    InGame,
    Victory,
    Defeat,
    Paused
}
