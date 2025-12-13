# test_full_game.ps1
# Pelny test - wczytanie mapy i uruchomienie gry az do wyniku

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Pelny test gry na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_FullGameTest"
    $workDir = "C:\TzarBot\FullGameTest"
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot\Screenshots" -Force | Out-Null
    New-Item -ItemType Directory -Path "C:\TzarBot\Logs" -Force | Out-Null

    Write-Host "Tworzenie projektu testowego..."

    $csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.49.0" />
  </ItemGroup>
</Project>
"@
    $csprojContent | Out-File "$workDir\FullGameTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== Full Game Test ===");
Console.WriteLine("Starting at: " + DateTime.Now);

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    Args = new[] { "--start-maximized" }
});

var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
});

var page = await context.NewPageAsync();

Console.WriteLine("Step 1: Navigate to tza.red...");
await page.GotoAsync("https://tza.red", new PageGotoOptions
{
    WaitUntil = WaitUntilState.DOMContentLoaded,
    Timeout = 60000
});
await Task.Delay(2000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fg_01_main.png" });

Console.WriteLine("Step 2: Click POTYCZKA Z SI (#rnd0)");
await page.ClickAsync("#rnd0");
await Task.Delay(2000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fg_02_skirmish.png" });

Console.WriteLine("Step 3: Load training map...");
var fileChooserTask = page.WaitForFileChooserAsync(new PageWaitForFileChooserOptions { Timeout = 10000 });
await page.ClickAsync("#load1");

try
{
    var fileChooser = await fileChooserTask;
    var mapPath = @"C:\\TzarBot\\Maps\\training-0.tzared";
    if (System.IO.File.Exists(mapPath))
    {
        await fileChooser.SetFilesAsync(mapPath);
        Console.WriteLine("Map file selected: " + mapPath);
    }
    else
    {
        Console.WriteLine("ERROR: Map file not found!");
        return;
    }
}
catch (TimeoutException)
{
    Console.WriteLine("ERROR: File chooser did not appear!");
    return;
}

await Task.Delay(3000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fg_03_map_loaded.png" });

// After loading map, analyze new DOM structure
Console.WriteLine("");
Console.WriteLine("=== Analyzing DOM after map load ===");
var buttons = await page.QuerySelectorAllAsync("button");
Console.WriteLine("Buttons found: " + buttons.Count);
foreach (var btn in buttons)
{
    var text = await btn.InnerTextAsync();
    var id = await btn.GetAttributeAsync("id");
    if (!string.IsNullOrWhiteSpace(text))
    {
        Console.WriteLine("  Button: \"" + text.Trim().Replace("\\n", " ") + "\" (id: " + (id ?? "null") + ")");
    }
}

// Look for Play/GRAJ button with different selectors
Console.WriteLine("");
Console.WriteLine("Step 4: Looking for GRAJ button...");
var playSelectors = new[]
{
    "#start2",
    "text=GRAJ",
    "text=Graj",
    "button:has-text('GRAJ')",
    "button:has-text('▶')"
};

IElementHandle? playButton = null;
foreach (var selector in playSelectors)
{
    try
    {
        playButton = await page.QuerySelectorAsync(selector);
        if (playButton != null)
        {
            Console.WriteLine("Found Play button with selector: " + selector);
            break;
        }
    }
    catch { }
}

if (playButton == null)
{
    Console.WriteLine("ERROR: Play button not found!");
    // Try clicking any button with GRAJ text
    try
    {
        await page.ClickAsync("button:has-text('GRAJ')");
        Console.WriteLine("Clicked button with GRAJ text");
    }
    catch
    {
        Console.WriteLine("Could not click GRAJ button");
        return;
    }
}
else
{
    await playButton.ClickAsync();
    Console.WriteLine("Clicked Play button!");
}

await Task.Delay(2000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fg_04_after_play_click.png" });

// Wait for game to load
Console.WriteLine("");
Console.WriteLine("Step 5: Waiting for game to start...");
await Task.Delay(10000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fg_05_game_loading.png" });

// Check for canvas (game running)
var canvas = await page.QuerySelectorAsync("canvas");
Console.WriteLine("Canvas found: " + (canvas != null));

// Monitor game for 30 seconds (training map should end quickly)
Console.WriteLine("");
Console.WriteLine("Step 6: Monitoring game for victory/defeat (30 seconds)...");

var startTime = DateTime.Now;
var timeout = TimeSpan.FromSeconds(30);
var result = "TIMEOUT";

while (DateTime.Now - startTime < timeout)
{
    await Task.Delay(3000);

    // Take periodic screenshots
    var elapsed = (int)(DateTime.Now - startTime).TotalSeconds;
    Console.WriteLine("  " + elapsed + "s elapsed...");

    // Check for Victory/Defeat text in page content
    var content = await page.ContentAsync();

    if (content.Contains("VICTORIOUS") || content.Contains("Victorious") || content.Contains("ZWYCI"))
    {
        result = "VICTORY";
        Console.WriteLine("VICTORY DETECTED!");
        break;
    }

    if (content.Contains("DEFEAT") || content.Contains("Defeat") || content.Contains("PRZEGRA"))
    {
        result = "DEFEAT";
        Console.WriteLine("DEFEAT DETECTED!");
        break;
    }

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = @"C:\\TzarBot\\Screenshots\\fg_game_" + elapsed.ToString("D3") + ".png"
    });
}

await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fg_final.png" });

Console.WriteLine("");
Console.WriteLine("=== RESULT: " + result + " ===");
Console.WriteLine("Test completed at: " + DateTime.Now);
await browser.CloseAsync();
"@
    $programContent | Out-File "$workDir\Program.cs" -Encoding UTF8

    # Build
    Write-Host "Budowanie projektu..."
    Set-Location $workDir
    $buildResult = dotnet build 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "BLAD budowania:" -ForegroundColor Red
        $buildResult | Write-Host
        return
    }
    Write-Host "Build OK" -ForegroundColor Green

    # Skrypt uruchomieniowy
    $runScript = @"
Set-Location "C:\TzarBot\FullGameTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\full_game_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_full_game_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_full_game_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam pelny test gry (to moze zajac ~1 minute)..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    $maxWait = 180
    $waited = 0
    do {
        Start-Sleep -Seconds 10
        $waited += 10
        $taskInfo = Get-ScheduledTask -TaskName $taskName
        $status = $taskInfo.State
        Write-Host "  Status: $status ($waited s)"
    } while ($status -eq "Running" -and $waited -lt $maxWait)

    # Pokaz log
    Write-Host ""
    Write-Host "=== Log testu ===" -ForegroundColor Cyan
    if (Test-Path "C:\TzarBot\Logs\full_game_test.log") {
        Get-Content "C:\TzarBot\Logs\full_game_test.log"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
