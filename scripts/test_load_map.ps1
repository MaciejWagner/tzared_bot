# test_load_map.ps1
# Testuje nawigacje do ladowania mapy treningowej

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test ladowania mapy na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_LoadMapTest"
    $workDir = "C:\TzarBot\LoadMapTest"
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
    $csprojContent | Out-File "$workDir\LoadMapTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== Load Map Test ===");
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
await Task.Delay(2000);

Console.WriteLine("Step 1: Click POTYCZKA Z SI (#rnd0)");
var potyczka = await page.QuerySelectorAsync("#rnd0");
if (potyczka != null)
{
    await potyczka.ClickAsync();
    await Task.Delay(2000);
}
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\lm_01_potyczka.png" });

Console.WriteLine("URL: " + page.Url);

// Look for all elements on the skirmish screen
Console.WriteLine("");
Console.WriteLine("=== Analyzing Skirmish Screen ===");

var allElements = await page.QuerySelectorAllAsync("[id]");
Console.WriteLine("Elements with IDs:");
foreach (var elem in allElements)
{
    var id = await elem.GetAttributeAsync("id");
    var tagName = await elem.EvaluateAsync<string>("e => e.tagName");
    var text = await elem.InnerTextAsync();
    if (text.Length > 100) text = text.Substring(0, 100) + "...";
    text = text.Replace("\\n", " ").Trim();
    Console.WriteLine("  " + tagName + "#" + id + ": \"" + text + "\"");
}

// Look for Load Game option
Console.WriteLine("");
Console.WriteLine("=== Looking for Load Game / WCZYTAJ GRE ===");

var loadSelectors = new[]
{
    "text=WCZYTAJ",
    "text=Wczytaj",
    "text=LOAD",
    "text=Load",
    "#accb",  // From earlier analysis
    "button:has-text('WCZYTAJ')",
    "a:has-text('WCZYTAJ')"
};

IElementHandle? loadElement = null;
string? foundSelector = null;
foreach (var selector in loadSelectors)
{
    try
    {
        loadElement = await page.QuerySelectorAsync(selector);
        if (loadElement != null)
        {
            foundSelector = selector;
            Console.WriteLine("Found Load element with selector: " + selector);
            break;
        }
    }
    catch { }
}

if (loadElement != null)
{
    var text = await loadElement.InnerTextAsync();
    Console.WriteLine("Load element text: " + text);

    Console.WriteLine("Step 2: Click Load Game");
    await loadElement.ClickAsync();
    await Task.Delay(2000);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\lm_02_after_load_click.png" });
    Console.WriteLine("URL after load click: " + page.Url);
}
else
{
    Console.WriteLine("Load Game element NOT found!");

    // Try clicking different areas to find it
    Console.WriteLine("Trying to find Load Game by text search in page content...");
    var content = await page.ContentAsync();
    if (content.Contains("WCZYTAJ"))
    {
        Console.WriteLine("Page contains 'WCZYTAJ' text");
    }
    if (content.Contains("LOAD"))
    {
        Console.WriteLine("Page contains 'LOAD' text");
    }
}

// Look for file input (for map upload)
Console.WriteLine("");
Console.WriteLine("=== Looking for file input ===");
var fileInputs = await page.QuerySelectorAllAsync("input[type='file']");
Console.WriteLine("File inputs found: " + fileInputs.Count);

// Look for buttons
Console.WriteLine("");
Console.WriteLine("=== All buttons on page ===");
var buttons = await page.QuerySelectorAllAsync("button");
foreach (var btn in buttons)
{
    var text = await btn.InnerTextAsync();
    var id = await btn.GetAttributeAsync("id");
    Console.WriteLine("  Button: \"" + text.Trim().Replace("\\n", " ") + "\" (id: " + (id ?? "null") + ")");
}

// Check sections
Console.WriteLine("");
Console.WriteLine("=== All sections on page ===");
var sections = await page.QuerySelectorAllAsync("section");
foreach (var sec in sections)
{
    var id = await sec.GetAttributeAsync("id");
    var text = await sec.InnerTextAsync();
    if (text.Length > 80) text = text.Substring(0, 80) + "...";
    Console.WriteLine("  Section#" + (id ?? "?") + ": \"" + text.Trim().Replace("\\n", " ") + "\"");
}

await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\lm_final.png" });

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
Set-Location "C:\TzarBot\LoadMapTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\load_map_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_load_map_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_load_map_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test ladowania mapy..." -ForegroundColor Yellow
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
    if (Test-Path "C:\TzarBot\Logs\load_map_test.log") {
        Get-Content "C:\TzarBot\Logs\load_map_test.log"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
