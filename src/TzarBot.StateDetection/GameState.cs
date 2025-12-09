namespace TzarBot.StateDetection;

/// <summary>
/// Represents the current state of the game as detected from screen analysis.
/// </summary>
public enum GameState
{
    /// <summary>
    /// State could not be determined (detection failed or ambiguous).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Game main menu is displayed.
    /// </summary>
    MainMenu = 1,

    /// <summary>
    /// Loading screen is displayed (map loading, game starting).
    /// </summary>
    Loading = 2,

    /// <summary>
    /// Active gameplay - the game is in progress.
    /// </summary>
    InGame = 3,

    /// <summary>
    /// Victory screen is displayed - player won the game.
    /// </summary>
    Victory = 4,

    /// <summary>
    /// Defeat screen is displayed - player lost the game.
    /// </summary>
    Defeat = 5,

    /// <summary>
    /// Game is paused (pause menu visible).
    /// </summary>
    Paused = 6,

    /// <summary>
    /// Game window is not responding or crashed.
    /// </summary>
    NotResponding = 7,

    /// <summary>
    /// Game window was closed or not found.
    /// </summary>
    Closed = 8
}

/// <summary>
/// Extension methods for GameState.
/// </summary>
public static class GameStateExtensions
{
    /// <summary>
    /// Returns true if the game state indicates the game has ended (Victory or Defeat).
    /// </summary>
    public static bool IsGameOver(this GameState state)
        => state == GameState.Victory || state == GameState.Defeat;

    /// <summary>
    /// Returns true if the game state indicates active gameplay.
    /// </summary>
    public static bool IsActive(this GameState state)
        => state == GameState.InGame || state == GameState.Paused;

    /// <summary>
    /// Returns true if the game state indicates an error condition.
    /// </summary>
    public static bool IsError(this GameState state)
        => state == GameState.NotResponding || state == GameState.Closed || state == GameState.Unknown;

    /// <summary>
    /// Returns true if the game state indicates a transitional state (loading, menu).
    /// </summary>
    public static bool IsTransitional(this GameState state)
        => state == GameState.MainMenu || state == GameState.Loading;
}
