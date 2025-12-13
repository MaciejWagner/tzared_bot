# test_single_player.ps1
# Testuje klikniecie Single Player i sprawdza co sie dzieje

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test Single Player na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_SPTest"
    $workDir = "C:\TzarBot\SPTest"
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
    $csprojContent | Out-File "$workDir\SPTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== Single Player Test ===");
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

// Monitor for new pages (popups, new tabs)
context.Page += (_, newPage) =>
{
    Console.WriteLine("NEW PAGE OPENED: " + newPage.Url);
};

Console.WriteLine("Navigating to tza.red...");
await page.GotoAsync("https://tza.red", new PageGotoOptions
{
    WaitUntil = WaitUntilState.DOMContentLoaded,
    Timeout = 60000
});

Console.WriteLine("Initial URL: " + page.Url);
await Task.Delay(2000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\sp_01_initial.png" });

// Find Single Player element
Console.WriteLine("");
Console.WriteLine("=== Looking for Single Player ===");

// Try different selectors
var singlePlayerSelectors = new[]
{
    "text=Single Player",
    "text=SINGLE PLAYER",
    "#rnd0",
    ".single-player",
    "[data-mode='single']"
};

IElementHandle? spElement = null;
foreach (var selector in singlePlayerSelectors)
{
    try
    {
        spElement = await page.QuerySelectorAsync(selector);
        if (spElement != null)
        {
            Console.WriteLine("Found Single Player with selector: " + selector);
            break;
        }
    }
    catch { }
}

if (spElement != null)
{
    // Get element info
    var box = await spElement.BoundingBoxAsync();
    if (box != null)
    {
        Console.WriteLine("Element position: (" + box.X + ", " + box.Y + "), size: " + box.Width + "x" + box.Height);
    }

    var html = await spElement.InnerHTMLAsync();
    Console.WriteLine("Element HTML (first 500 chars): " + (html.Length > 500 ? html.Substring(0, 500) : html));

    Console.WriteLine("");
    Console.WriteLine("=== Clicking Single Player ===");
    await spElement.ClickAsync();

    // Wait and check for changes
    await Task.Delay(3000);
    Console.WriteLine("URL after click: " + page.Url);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\sp_02_after_click.png" });

    // Check if new page opened
    var pages = context.Pages;
    Console.WriteLine("Number of pages in context: " + pages.Count);
    foreach (var p in pages)
    {
        Console.WriteLine("  Page URL: " + p.Url);
    }

    // Wait more for game to load
    Console.WriteLine("");
    Console.WriteLine("=== Waiting for game to load ===");
    await Task.Delay(10000);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\sp_03_after_wait.png" });

    // Check for game canvas or game elements
    var canvas = await page.QuerySelectorAsync("canvas");
    Console.WriteLine("Canvas found: " + (canvas != null));

    // Look for game menu elements
    var gameElements = await page.QuerySelectorAllAsync("[id]");
    Console.WriteLine("Elements with IDs: " + gameElements.Count);
}
else
{
    Console.WriteLine("Single Player element NOT found!");

    // Debug: show all clickable elements
    Console.WriteLine("Showing all clickable elements:");
    var clickables = await page.QuerySelectorAllAsync("button, a, [role='button'], [onclick]");
    foreach (var elem in clickables)
    {
        var text = await elem.InnerTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("  - " + text.Trim().Replace("\\n", " ").Substring(0, Math.Min(50, text.Length)));
        }
    }
}

await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\sp_final.png" });

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
Set-Location "C:\TzarBot\SPTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\sp_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_sp_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_sp_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test Single Player..." -ForegroundColor Yellow
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
    if (Test-Path "C:\TzarBot\Logs\sp_test.log") {
        Get-Content "C:\TzarBot\Logs\sp_test.log"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
