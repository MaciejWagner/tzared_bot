using Microsoft.Playwright;

namespace TzarBot.BrowserInterface;

/// <summary>
/// Implementation of IBrowserGameInterface using Playwright.
/// Controls Tzar game through tza.red in a browser on VM.
/// </summary>
public sealed class PlaywrightGameInterface : IBrowserGameInterface
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly string _gameUrl;

    public bool IsInitialized => _page != null;

    /// <summary>
    /// Creates a new PlaywrightGameInterface.
    /// </summary>
    /// <param name="gameUrl">URL of the game (default: https://tza.red)</param>
    public PlaywrightGameInterface(string gameUrl = "https://tza.red")
    {
        _gameUrl = gameUrl;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(bool headless = false)
    {
        if (IsInitialized) return;

        _playwright = await Playwright.CreateAsync();

        // Use Edge (msedge) - works better with tza.red than Chromium
        // Edge uses the same engine but doesn't have map loading issues
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Channel = "msedge",
            Args = new[]
            {
                "--start-maximized",
                "--disable-infobars",
                "--no-sandbox"
            }
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });

        _page = await context.NewPageAsync();

        // Navigate to game
        await _page.GotoAsync(_gameUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60000
        });

        // Wait for game to load
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        Console.WriteLine($"[PlaywrightGameInterface] Initialized at {_gameUrl}");
    }

    /// <inheritdoc />
    public async Task LoadMapAsync(string mapPath)
    {
        EnsureInitialized();

        // Step 1: Navigate to Skirmish mode
        // Click on "POTYCZKA Z SI" section (#rnd0)
        Console.WriteLine("[PlaywrightGameInterface] Clicking POTYCZKA Z SI...");
        await _page!.ClickAsync("#rnd0");
        await Task.Delay(2000);

        // Step 2: Click on "WCZYTAJ GRĘ" button (#load1)
        Console.WriteLine("[PlaywrightGameInterface] Clicking WCZYTAJ GRĘ...");

        // Set up file chooser handler before clicking
        var fileChooserTask = _page.WaitForFileChooserAsync(new PageWaitForFileChooserOptions
        {
            Timeout = 10000
        });

        await _page.ClickAsync("#load1");

        // Handle file dialog
        var fileChooser = await fileChooserTask;
        await fileChooser.SetFilesAsync(mapPath);

        Console.WriteLine($"[PlaywrightGameInterface] Map loaded: {mapPath}");
        await Task.Delay(2000);
    }

    /// <inheritdoc />
    public async Task StartGameAsync()
    {
        EnsureInitialized();

        // After loading custom map, the GRAJ button has id #startCustom
        // Try multiple selectors for the Play button
        Console.WriteLine("[PlaywrightGameInterface] Looking for GRAJ button...");

        var playButton = await _page!.QuerySelectorAsync("#startCustom");
        playButton ??= await _page.QuerySelectorAsync("#start2");
        playButton ??= await _page.QuerySelectorAsync("button:has-text('GRAJ')");

        if (playButton != null)
        {
            await playButton.ClickAsync();
            Console.WriteLine("[PlaywrightGameInterface] Game started");
        }
        else
        {
            Console.WriteLine("[PlaywrightGameInterface] Warning: GRAJ button not found, trying text selector...");
            await _page.ClickAsync("text=GRAJ");
        }

        // Wait for game to load
        await Task.Delay(5000);
    }

    /// <inheritdoc />
    public async Task ClickAtAsync(int x, int y)
    {
        EnsureInitialized();

        // Find the game canvas element
        var canvas = await GetGameCanvasAsync();
        if (canvas != null)
        {
            await canvas.ClickAsync(new ElementHandleClickOptions
            {
                Position = new Position { X = x, Y = y }
            });
        }
        else
        {
            // Fallback: click on page directly
            await _page!.Mouse.ClickAsync(x, y);
        }
    }

    /// <inheritdoc />
    public async Task RightClickAtAsync(int x, int y)
    {
        EnsureInitialized();

        var canvas = await GetGameCanvasAsync();
        if (canvas != null)
        {
            await canvas.ClickAsync(new ElementHandleClickOptions
            {
                Position = new Position { X = x, Y = y },
                Button = MouseButton.Right
            });
        }
        else
        {
            await _page!.Mouse.ClickAsync(x, y, new MouseClickOptions
            {
                Button = MouseButton.Right
            });
        }
    }

    /// <inheritdoc />
    public async Task DragSelectAsync(int startX, int startY, int endX, int endY)
    {
        EnsureInitialized();

        await _page!.Mouse.MoveAsync(startX, startY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = 10 });
        await _page.Mouse.UpAsync();
    }

    /// <inheritdoc />
    public async Task PressKeyAsync(string key)
    {
        EnsureInitialized();
        await _page!.Keyboard.PressAsync(key);
    }

    /// <inheritdoc />
    public async Task<byte[]> TakeScreenshotAsync()
    {
        EnsureInitialized();

        return await _page!.ScreenshotAsync(new PageScreenshotOptions
        {
            Type = ScreenshotType.Png,
            FullPage = false
        });
    }

    /// <inheritdoc />
    public async Task<GameStateResult> DetectGameStateAsync()
    {
        EnsureInitialized();

        try
        {
            // Note: Victory/Defeat screens in tza.red are rendered in canvas,
            // so DOM-based detection may not work. For accurate detection,
            // use TakeScreenshotAsync() + template matching with OpenCV.

            // Check for main menu elements (DOM-based)
            var potyczkaElement = await _page!.QuerySelectorAsync("#rnd0");
            if (potyczkaElement != null)
            {
                // Check if we're on the main page (not in game)
                var url = _page.Url;
                if (!url.Contains("#RandomMap") && !url.Contains("#Custom"))
                {
                    return new GameStateResult(GameState.MainMenu, 0.9f, "Main menu detected");
                }
            }

            // Check for skirmish setup screen
            var loadButton = await _page.QuerySelectorAsync("#load1");
            var startButton = await _page.QuerySelectorAsync("#start2");
            if (loadButton != null || startButton != null)
            {
                return new GameStateResult(GameState.SkirmishSetup, 0.8f, "Skirmish setup detected");
            }

            // Check for custom map loaded (GRAJ button with #startCustom)
            var customStartButton = await _page.QuerySelectorAsync("#startCustom");
            if (customStartButton != null)
            {
                return new GameStateResult(GameState.SkirmishSetup, 0.9f, "Custom map loaded");
            }

            // Check for in-game (canvas is active)
            var canvas = await GetGameCanvasAsync();
            if (canvas != null)
            {
                // Canvas exists - game is running
                // Victory/Defeat must be detected via screenshot + template matching
                return new GameStateResult(GameState.InGame, 0.7f, "Game canvas detected - use screenshot for Victory/Defeat detection");
            }

            return new GameStateResult(GameState.Unknown, 0f, "Unable to determine state");
        }
        catch (Exception ex)
        {
            return new GameStateResult(GameState.Unknown, 0f, $"Detection error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        _page = null;

        Console.WriteLine("[PlaywrightGameInterface] Closed");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Browser interface not initialized. Call InitializeAsync first.");
        }
    }

    private async Task<IElementHandle?> GetGameCanvasAsync()
    {
        // Try to find the game canvas element
        // The exact selector depends on how tza.red renders the game
        var canvas = await _page!.QuerySelectorAsync("canvas");
        canvas ??= await _page.QuerySelectorAsync("#game-canvas");
        canvas ??= await _page.QuerySelectorAsync(".game-container canvas");

        return canvas;
    }

    private async Task ClickByTextAsync(params string[] textOptions)
    {
        foreach (var text in textOptions)
        {
            try
            {
                var element = await _page!.QuerySelectorAsync($"text={text}");
                if (element != null)
                {
                    await element.ClickAsync();
                    Console.WriteLine($"[PlaywrightGameInterface] Clicked: {text}");
                    return;
                }
            }
            catch
            {
                // Try next option
            }
        }

        Console.WriteLine($"[PlaywrightGameInterface] Warning: Could not find any of: {string.Join(", ", textOptions)}");
    }
}
