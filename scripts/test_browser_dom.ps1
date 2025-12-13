# test_browser_dom.ps1
# Testuje strukture DOM tza.red i czeka na pelne zaladowanie gry

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test struktury DOM tza.red na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_DomTest"
    $workDir = "C:\TzarBot\DomTest"
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
    $csprojContent | Out-File "$workDir\DomTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== DOM Structure Test ===");
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
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = 120000
});

Console.WriteLine("Page loaded. Waiting 30 seconds for Unity WebGL to fully load...");
for (int i = 0; i < 6; i++)
{
    await Task.Delay(5000);
    Console.WriteLine("  Waited " + ((i + 1) * 5) + " seconds...");

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = @"C:\\TzarBot\\Screenshots\\dom_" + i.ToString("D2") + "_wait.png"
    });
}

// Get page content and structure
Console.WriteLine("");
Console.WriteLine("=== Checking DOM structure ===");

var html = await page.InnerHTMLAsync("body");
Console.WriteLine("Body HTML length: " + html.Length);

// Look for canvas
var canvasElements = await page.QuerySelectorAllAsync("canvas");
Console.WriteLine("Canvas elements found: " + canvasElements.Count);

foreach (var canvas in canvasElements)
{
    var box = await canvas.BoundingBoxAsync();
    if (box != null)
    {
        Console.WriteLine("  Canvas: " + box.Width + "x" + box.Height + " at (" + box.X + ", " + box.Y + ")");
    }
    var id = await canvas.GetAttributeAsync("id");
    var className = await canvas.GetAttributeAsync("class");
    Console.WriteLine("  Canvas id: " + (id ?? "null") + ", class: " + (className ?? "null"));
}

// Look for Unity container
var unityContainer = await page.QuerySelectorAsync("#unity-container");
if (unityContainer != null)
{
    Console.WriteLine("Found #unity-container!");
    var box = await unityContainer.BoundingBoxAsync();
    if (box != null)
    {
        Console.WriteLine("  Size: " + box.Width + "x" + box.Height);
    }
}

var unityCanvas = await page.QuerySelectorAsync("#unity-canvas");
if (unityCanvas != null)
{
    Console.WriteLine("Found #unity-canvas!");
    var box = await unityCanvas.BoundingBoxAsync();
    if (box != null)
    {
        Console.WriteLine("  Size: " + box.Width + "x" + box.Height);
    }
}

// Final screenshot
await page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = @"C:\\TzarBot\\Screenshots\\dom_final.png"
});

// Try clicking in the center of the page
Console.WriteLine("");
Console.WriteLine("=== Testing clicks ===");
var viewport = page.ViewportSize;
if (viewport != null)
{
    int centerX = viewport.Width / 2;
    int centerY = viewport.Height / 2;

    Console.WriteLine("Clicking center of page: (" + centerX + ", " + centerY + ")");
    await page.Mouse.ClickAsync(centerX, centerY);
    await Task.Delay(2000);

    await page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = @"C:\\TzarBot\\Screenshots\\dom_after_center_click.png"
    });
}

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
Set-Location "C:\TzarBot\DomTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\dom_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_dom_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_dom_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test DOM (to zajmie okolo 60s)..." -ForegroundColor Yellow
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
    if (Test-Path "C:\TzarBot\Logs\dom_test.log") {
        Get-Content "C:\TzarBot\Logs\dom_test.log"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
