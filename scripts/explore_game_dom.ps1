# Explore tza.red game DOM and JavaScript environment
# This script investigates what data is accessible without screenshots

$ErrorActionPreference = "Stop"

# Create a simple C# script to explore the game
$code = @'
using Microsoft.Playwright;

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    Channel = "msedge",
    Args = new[] { "--use-gl=swiftshader", "--enable-unsafe-swiftshader" }
});

var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
});

var page = await context.NewPageAsync();
await page.GotoAsync("https://tza.red", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

Console.WriteLine("=== Exploring tza.red DOM ===\n");

// 1. Check for canvas elements
var canvases = await page.EvaluateAsync<string>(@"() => {
    const canvases = document.querySelectorAll('canvas');
    return Array.from(canvases).map(c => `Canvas: ${c.id || 'unnamed'} - ${c.width}x${c.height}`).join('\n');
}");
Console.WriteLine("CANVAS ELEMENTS:");
Console.WriteLine(canvases);
Console.WriteLine();

// 2. Check for global game variables
var globals = await page.EvaluateAsync<string>(@"() => {
    const gameVars = [];
    const candidates = ['game', 'Game', 'engine', 'Engine', 'state', 'State', 'world', 'World',
                        'player', 'Player', 'units', 'Units', 'map', 'Map', 'tzar', 'Tzar',
                        'gameState', 'gameEngine', 'gameWorld', 'Module', 'FS'];
    for (const name of candidates) {
        if (window[name] !== undefined) {
            const type = typeof window[name];
            const keys = type === 'object' ? Object.keys(window[name]).slice(0, 10).join(', ') : '';
            gameVars.push(`window.${name}: ${type}${keys ? ' [' + keys + '...]' : ''}`);
        }
    }
    return gameVars.join('\n') || 'No common game variables found';
}");
Console.WriteLine("GLOBAL VARIABLES:");
Console.WriteLine(globals);
Console.WriteLine();

// 3. Check for WebAssembly/Emscripten
var wasm = await page.EvaluateAsync<string>(@"() => {
    if (typeof Module !== 'undefined') {
        const keys = Object.keys(Module).filter(k => !k.startsWith('_')).slice(0, 20);
        return 'Emscripten Module found! Keys: ' + keys.join(', ');
    }
    return 'No Emscripten Module found';
}");
Console.WriteLine("WEBASSEMBLY/EMSCRIPTEN:");
Console.WriteLine(wasm);
Console.WriteLine();

// 4. Check for exposed functions
var funcs = await page.EvaluateAsync<string>(@"() => {
    const funcs = [];
    for (const key of Object.keys(window)) {
        if (typeof window[key] === 'function' && !key.startsWith('webkit') && !key.startsWith('on')) {
            if (key.toLowerCase().includes('game') || key.toLowerCase().includes('unit') ||
                key.toLowerCase().includes('player') || key.toLowerCase().includes('map')) {
                funcs.push(key);
            }
        }
    }
    return funcs.slice(0, 20).join('\n') || 'No game-related functions found';
}");
Console.WriteLine("GAME-RELATED FUNCTIONS:");
Console.WriteLine(funcs);
Console.WriteLine();

// 5. Check canvas context type
var ctxType = await page.EvaluateAsync<string>(@"() => {
    const canvas = document.querySelector('canvas');
    if (!canvas) return 'No canvas found';

    // Try different context types
    const gl2 = canvas.getContext('webgl2');
    const gl = canvas.getContext('webgl');
    const ctx2d = canvas.getContext('2d');

    if (gl2) return 'WebGL2 context';
    if (gl) return 'WebGL context';
    if (ctx2d) return '2D context';
    return 'Context type: ' + (canvas.getContext ? 'unknown' : 'none');
}");
Console.WriteLine("CANVAS CONTEXT:");
Console.WriteLine(ctxType);
Console.WriteLine();

// 6. Performance test - canvas pixel reading
var perfTest = await page.EvaluateAsync<string>(@"() => {
    const canvas = document.querySelector('canvas');
    if (!canvas) return 'No canvas for performance test';

    const start = performance.now();
    const ctx = canvas.getContext('2d', { willReadFrequently: true });
    if (!ctx) return 'Cannot get 2D context for reading';

    for (let i = 0; i < 10; i++) {
        ctx.getImageData(0, 0, canvas.width, canvas.height);
    }
    const elapsed = performance.now() - start;

    return `10x getImageData(${canvas.width}x${canvas.height}): ${elapsed.toFixed(1)}ms (${(elapsed/10).toFixed(1)}ms avg)`;
}");
Console.WriteLine("PIXEL READING PERFORMANCE:");
Console.WriteLine(perfTest);

await browser.CloseAsync();
Console.WriteLine("\n=== Exploration complete ===");
'@

Write-Host "This script requires running interactively with Playwright..."
Write-Host "The exploration will check:"
Write-Host "  1. Canvas elements"
Write-Host "  2. Global JavaScript variables (game state)"
Write-Host "  3. WebAssembly/Emscripten module"
Write-Host "  4. Game-related functions"
Write-Host "  5. Canvas context type"
Write-Host "  6. Pixel reading performance"
