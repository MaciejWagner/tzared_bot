using Microsoft.Playwright;

Console.WriteLine("=== TzarBot Game Explorer ===\n");
Console.WriteLine("Exploring tza.red DOM and JavaScript environment...\n");

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
Console.WriteLine("Loading tza.red...");
await page.GotoAsync("https://tza.red", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
await Task.Delay(3000);

// 1. Canvas elements
Console.WriteLine("\n=== CANVAS ELEMENTS ===");
var canvases = await page.EvaluateAsync<string>(@"() => {
    const canvases = document.querySelectorAll('canvas');
    return Array.from(canvases).map(c =>
        `${c.id || 'unnamed'}: ${c.width}x${c.height}`
    ).join('\n') || 'No canvas found';
}");
Console.WriteLine(canvases);

// 2. Global game variables
Console.WriteLine("\n=== GLOBAL VARIABLES ===");
var globals = await page.EvaluateAsync<string>(@"() => {
    const found = [];
    const candidates = ['game', 'Game', 'engine', 'Engine', 'state', 'State',
                        'world', 'World', 'player', 'Player', 'units', 'Units',
                        'map', 'Map', 'tzar', 'Tzar', 'Module', 'FS', '_malloc',
                        'gameState', 'gameEngine', 'canvas', 'gl', 'ctx'];
    for (const name of candidates) {
        if (window[name] !== undefined) {
            const type = typeof window[name];
            let info = type;
            if (type === 'object' && window[name]) {
                const keys = Object.keys(window[name]).slice(0, 5);
                info += keys.length ? ` {${keys.join(', ')}...}` : '';
            }
            found.push(`window.${name}: ${info}`);
        }
    }
    return found.join('\n') || 'No game variables found';
}");
Console.WriteLine(globals);

// 3. Emscripten/WASM check
Console.WriteLine("\n=== WEBASSEMBLY/EMSCRIPTEN ===");
var wasm = await page.EvaluateAsync<string>(@"() => {
    if (typeof Module !== 'undefined') {
        const m = Module;
        const info = [];
        if (m.HEAP8) info.push('HEAP8 available');
        if (m.HEAP32) info.push('HEAP32 available');
        if (m.ccall) info.push('ccall available');
        if (m.cwrap) info.push('cwrap available');
        if (m.FS) info.push('FS (filesystem) available');
        const funcs = Object.keys(m).filter(k => k.startsWith('_') && typeof m[k] === 'function').slice(0, 10);
        if (funcs.length) info.push('Exported functions: ' + funcs.join(', '));
        return info.join('\n') || 'Module exists but no useful exports';
    }
    return 'No Emscripten Module';
}");
Console.WriteLine(wasm);

// 4. Screenshot performance test
Console.WriteLine("\n=== PERFORMANCE TEST ===");
var perf = await page.EvaluateAsync<string>(@"() => {
    const results = [];

    // Test 1: Canvas toDataURL
    const canvas = document.querySelector('canvas');
    if (canvas) {
        let start = performance.now();
        for (let i = 0; i < 5; i++) canvas.toDataURL('image/png');
        results.push(`toDataURL (5x): ${(performance.now() - start).toFixed(0)}ms`);

        // Test 2: 2D context getImageData
        try {
            const ctx = canvas.getContext('2d', { willReadFrequently: true });
            if (ctx) {
                start = performance.now();
                for (let i = 0; i < 5; i++) ctx.getImageData(0, 0, canvas.width, canvas.height);
                results.push(`getImageData (5x): ${(performance.now() - start).toFixed(0)}ms`);
            }
        } catch(e) { results.push('getImageData: ' + e.message); }
    }

    return results.join('\n') || 'No canvas for testing';
}");
Console.WriteLine(perf);

// 5. DOM elements that might have game info
Console.WriteLine("\n=== GAME UI ELEMENTS ===");
var ui = await page.EvaluateAsync<string>(@"() => {
    const selectors = ['#resources', '#gold', '#wood', '#food', '#stone',
                       '#population', '#units', '#minimap', '.resource',
                       '[class*=""resource""]', '[id*=""resource""]'];
    const found = [];
    for (const sel of selectors) {
        const el = document.querySelector(sel);
        if (el) {
            found.push(`${sel}: ${el.textContent?.slice(0,50) || el.innerHTML?.slice(0,50) || 'empty'}`);
        }
    }
    return found.join('\n') || 'No resource UI elements found';
}");
Console.WriteLine(ui);

Console.WriteLine("\n=== EXPLORATION COMPLETE ===");
Console.WriteLine("Press Enter to close browser...");
Console.ReadLine();

await browser.CloseAsync();
