# test_tzared_menu.ps1
# Testuje nawigacje w tza.red - szuka elementow DOM

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test menu tza.red na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_MenuTest"
    $workDir = "C:\TzarBot\MenuTest"
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
    $csprojContent | Out-File "$workDir\MenuTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== TzaRed Menu Test ===");
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

Console.WriteLine("Navigating to tza.red...");
await page.GotoAsync("https://tza.red", new PageGotoOptions
{
    WaitUntil = WaitUntilState.DOMContentLoaded,
    Timeout = 60000
});

Console.WriteLine("Page loaded.");
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\menu_01_initial.png" });

// Wait a bit for any JS to load
await Task.Delay(3000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\menu_02_after_wait.png" });

// Look for key DOM elements
Console.WriteLine("");
Console.WriteLine("=== Analyzing DOM ===");

var mainElement = await page.QuerySelectorAsync("main");
if (mainElement != null) Console.WriteLine("Found: main element");

var indexElement = await page.QuerySelectorAsync("#index");
if (indexElement != null) Console.WriteLine("Found: #index element");

var topLinks = await page.QuerySelectorAsync("#topLinks");
if (topLinks != null) Console.WriteLine("Found: #topLinks element");

var commonMenu = await page.QuerySelectorAsync("#commonMenu");
if (commonMenu != null) Console.WriteLine("Found: #commonMenu element");

var multElement = await page.QuerySelectorAsync("#mult");
if (multElement != null) Console.WriteLine("Found: #mult element");

// Get all elements with IDs
Console.WriteLine("");
Console.WriteLine("=== All elements with IDs ===");
var elementsWithIds = await page.QuerySelectorAllAsync("[id]");
foreach (var elem in elementsWithIds)
{
    var id = await elem.GetAttributeAsync("id");
    var tagName = await elem.EvaluateAsync<string>("e => e.tagName");
    Console.WriteLine("  " + tagName + "#" + id);
}

// Look for button-like elements
Console.WriteLine("");
Console.WriteLine("=== Looking for buttons/links ===");
var buttons = await page.QuerySelectorAllAsync("button, a, [role='button'], .btn, .button");
Console.WriteLine("Found " + buttons.Count + " button-like elements");

foreach (var btn in buttons)
{
    var text = await btn.InnerTextAsync();
    if (!string.IsNullOrWhiteSpace(text) && text.Length < 50)
    {
        Console.WriteLine("  Button: \"" + text.Trim().Replace("\\n", " ") + "\"");
    }
}

// Look for text content that might be menu items
Console.WriteLine("");
Console.WriteLine("=== Searching for game-related text ===");
var pageContent = await page.ContentAsync();

var keywords = new[] { "GRAJ", "PLAY", "POTYCZKA", "SKIRMISH", "SINGLE", "LOAD", "WCZYTAJ", "NOWA", "NEW" };
foreach (var keyword in keywords)
{
    if (pageContent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Found keyword: " + keyword);
    }
}

// Try to find and click on game start elements
Console.WriteLine("");
Console.WriteLine("=== Trying to interact ===");

// Look for "SINGLE PLAYER" or similar
var singlePlayer = await page.QuerySelectorAsync("text=SINGLE PLAYER");
if (singlePlayer == null) singlePlayer = await page.QuerySelectorAsync("text=Single Player");
if (singlePlayer == null) singlePlayer = await page.QuerySelectorAsync("text=POTYCZKA");

if (singlePlayer != null)
{
    Console.WriteLine("Found Single Player option, clicking...");
    await singlePlayer.ClickAsync();
    await Task.Delay(2000);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\menu_03_after_single_click.png" });
}
else
{
    Console.WriteLine("Single Player option not found in DOM");
}

// Final screenshot
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\menu_final.png" });

Console.WriteLine("");
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
Set-Location "C:\TzarBot\MenuTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\menu_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_menu_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_menu_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test menu..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    $maxWait = 90
    $waited = 0
    do {
        Start-Sleep -Seconds 5
        $waited += 5
        $taskInfo = Get-ScheduledTask -TaskName $taskName
        $status = $taskInfo.State
        Write-Host "  Status: $status ($waited s)"
    } while ($status -eq "Running" -and $waited -lt $maxWait)

    # Pokaz log
    Write-Host ""
    Write-Host "=== Log testu ===" -ForegroundColor Cyan
    if (Test-Path "C:\TzarBot\Logs\menu_test.log") {
        Get-Content "C:\TzarBot\Logs\menu_test.log"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
