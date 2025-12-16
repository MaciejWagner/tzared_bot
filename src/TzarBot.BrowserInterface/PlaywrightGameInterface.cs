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
    private IElementHandle? _canvas;  // Cached canvas element for faster screenshots
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

        // Edge is the only browser that works with tza.red map loading
        // Chromium: can't load maps, Firefox: stuck on Loading screen
        // tza.red requires WebGPU - try native GPU first, fallback to SwiftShader if needed
        var useSwiftShader = Environment.GetEnvironmentVariable("TZARBOT_USE_SWIFTSHADER") == "1";
        var args = new List<string>
        {
            "--start-maximized",
            "--disable-infobars",
            "--no-sandbox"
        };

        if (useSwiftShader)
        {
            args.Add("--use-gl=swiftshader");       // Software GL - CPU intensive but works in VMs
            args.Add("--enable-unsafe-swiftshader");
        }
        // else: use native GPU WebGL (much faster, less CPU)

        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Channel = "msedge",
            Args = args.ToArray()
        });

        // Smaller viewport for faster SwiftShader rendering in Hyper-V VM
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true
        });

        _page = await context.NewPageAsync();

        // Log console errors for debugging
        _page.Console += (_, msg) =>
        {
            if (msg.Type == "error" || msg.Type == "warning")
            {
                Console.WriteLine($"[Browser {msg.Type}] {msg.Text}");
            }
        };

        _page.PageError += (_, error) =>
        {
            Console.WriteLine($"[Browser PageError] {error}");
        };

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

        // Close any blocking popups (fwindow2 class blocks clicks in Firefox)
        Console.WriteLine("[PlaywrightGameInterface] Checking for blocking popups...");
        try
        {
            var popup = await _page!.QuerySelectorAsync(".fwindow2");
            if (popup != null)
            {
                Console.WriteLine("[PlaywrightGameInterface] Found blocking popup, trying to close...");
                // Try clicking X button or outside the popup
                var closeBtn = await _page.QuerySelectorAsync(".fwindow2 .close, .fwindow2 button, .fwindow2 .x");
                if (closeBtn != null)
                {
                    await closeBtn.ClickAsync();
                    await Task.Delay(500);
                }
                else
                {
                    // Press Escape to close
                    await _page.Keyboard.PressAsync("Escape");
                    await Task.Delay(500);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlaywrightGameInterface] Popup check error: {ex.Message}");
        }

        // Step 1: Navigate to Skirmish mode
        // Click on "POTYCZKA Z SI" section (#rnd0)
        Console.WriteLine("[PlaywrightGameInterface] Clicking POTYCZKA Z SI...");
        await _page!.ClickAsync("#rnd0", new PageClickOptions { Force = true });
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

    /// <summary>
    /// Sets game speed via in-game menu.
    /// Speed: 1-7 where 1=x1 (normal), 3=x3, 7=x7 (max)
    /// </summary>
    public async Task SetGameSpeedAsync(int speed = 3)
    {
        EnsureInitialized();

        try
        {
            // Click MENU button in bottom-left corner
            Console.WriteLine("[PlaywrightGameInterface] Opening game menu...");
            var menuButton = await _page!.QuerySelectorAsync("text=MENU");
            menuButton ??= await _page.QuerySelectorAsync("#menu");
            menuButton ??= await _page.QuerySelectorAsync("[id*='menu']");

            if (menuButton != null)
            {
                await menuButton.ClickAsync();
                await Task.Delay(800); // Wait for menu animation
            }
            else
            {
                // Fallback: try clicking at bottom-left where MENU button is
                await _page.Mouse.ClickAsync(50, 695);
                await Task.Delay(800);
            }

            // Look for speed slider - first range input is "Prędkość gry"
            var sliders = await _page.QuerySelectorAllAsync("input[type='range']");
            Console.WriteLine($"[PlaywrightGameInterface] Found {sliders.Count} sliders");

            if (sliders.Count > 0)
            {
                // First slider is game speed (Prędkość gry)
                var speedSlider = sliders[0];

                // Get slider attributes via JavaScript
                var sliderInfo = await _page.EvaluateAsync<SliderInfo>(@"el => ({
                    min: parseInt(el.min) || 1,
                    max: parseInt(el.max) || 7,
                    step: parseInt(el.step) || 1,
                    value: parseInt(el.value) || 1
                })", speedSlider);

                Console.WriteLine($"[PlaywrightGameInterface] Slider: min={sliderInfo?.Min}, max={sliderInfo?.Max}, current={sliderInfo?.Value}");

                // Clamp speed to valid range
                int targetSpeed = Math.Clamp(speed, sliderInfo?.Min ?? 1, sliderInfo?.Max ?? 7);

                // Set value directly via JavaScript
                await _page.EvaluateAsync(@"(data) => {
                    const el = data.element;
                    el.value = data.value;
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                    el.dispatchEvent(new Event('change', { bubbles: true }));
                }", new { element = speedSlider, value = targetSpeed });

                Console.WriteLine($"[PlaywrightGameInterface] Game speed set to x{targetSpeed}");
                await Task.Delay(200);
            }
            else
            {
                Console.WriteLine("[PlaywrightGameInterface] No sliders found");
            }

            // Close menu by clicking the red X button in top-right corner
            Console.WriteLine("[PlaywrightGameInterface] Closing menu by clicking X button...");

            // Try to find and click the X button (it's a red X in top-right of menu)
            var closeButton = await _page.QuerySelectorAsync(".close-button");
            closeButton ??= await _page.QuerySelectorAsync("[class*='close']");
            closeButton ??= await _page.QuerySelectorAsync("button.x");

            if (closeButton != null)
            {
                await closeButton.ClickAsync();
                Console.WriteLine("[PlaywrightGameInterface] Clicked close button");
            }
            else
            {
                // Fallback: click at approximate X button position (top-right of menu dialog)
                // Menu appears centered, X button is at top-right corner around x=390, y=50
                Console.WriteLine("[PlaywrightGameInterface] Close button not found, clicking at X position...");
                await _page.Mouse.ClickAsync(390, 50);
            }
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlaywrightGameInterface] Error setting speed: {ex.Message}");
            // Try to close menu by clicking X position
            try { await _page!.Mouse.ClickAsync(390, 50); } catch { }
        }
    }

    private class SliderInfo
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Step { get; set; }
        public int Value { get; set; }
    }

    /// <inheritdoc />
    public async Task ClickAtAsync(int x, int y)
    {
        EnsureInitialized();
        // Use direct mouse click to avoid actionability checks (overlays cause 30s timeout)
        await _page!.Mouse.ClickAsync(x, y);
    }

    /// <inheritdoc />
    public async Task RightClickAtAsync(int x, int y)
    {
        EnsureInitialized();
        // Use direct mouse click to avoid actionability checks (overlays cause 30s timeout)
        await _page!.Mouse.ClickAsync(x, y, new MouseClickOptions
        {
            Button = MouseButton.Right
        });
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

        // Use cached canvas element for faster screenshots
        if (_canvas == null)
        {
            _canvas = await _page!.QuerySelectorAsync("canvas");
        }

        if (_canvas != null)
        {
            return await _canvas.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Type = ScreenshotType.Jpeg,
                Quality = 80
            });
        }

        // Fallback to full page if no canvas found
        return await _page!.ScreenshotAsync(new PageScreenshotOptions
        {
            Type = ScreenshotType.Jpeg,
            Quality = 80,
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
