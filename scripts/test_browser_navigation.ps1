# test_browser_navigation.ps1
# Testuje nawigację do mapy treningowej przez Playwright na VM DEV

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test nawigacji w przegladarce na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_BrowserNavTest"
    $workDir = "C:\TzarBot\BrowserNavTest"
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
    $csprojContent | Out-File "$workDir\BrowserNavTest.csproj" -Encoding UTF8

    # Program testujacy nawigacje
    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== Browser Navigation Test ===");
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
    Timeout = 60000
});

Console.WriteLine("Page loaded. Taking screenshot of main menu...");
await page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = @"C:\\TzarBot\\Screenshots\\nav_01_main_menu.png"
});

// Wait for game to fully load (Unity WebGL needs time)
Console.WriteLine("Waiting for game to load (10 seconds)...");
await Task.Delay(10000);

await page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = @"C:\\TzarBot\\Screenshots\\nav_02_after_load.png"
});

// Try to find and click canvas
var canvas = await page.QuerySelectorAsync("canvas");
if (canvas != null)
{
    Console.WriteLine("Found canvas element!");
    var box = await canvas.BoundingBoxAsync();
    if (box != null)
    {
        Console.WriteLine("Canvas size: " + box.Width + " x " + box.Height);

        // Menu structure (based on screenshots):
        // Main menu has buttons on the left side
        // "POTYCZKA Z SI" (Skirmish) is typically in the middle-left area

        // Step 1: Click on "POTYCZKA Z SI" / Skirmish button
        // Based on typical Tzar menu layout - buttons are on left side, middle height
        int skirmishX = (int)(box.Width * 0.15);  // Left side
        int skirmishY = (int)(box.Height * 0.4);   // Middle

        Console.WriteLine("Clicking at Skirmish position: (" + skirmishX + ", " + skirmishY + ")");
        await page.Mouse.ClickAsync(box.X + skirmishX, box.Y + skirmishY);
        await Task.Delay(2000);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = @"C:\\TzarBot\\Screenshots\\nav_03_after_skirmish_click.png"
        });

        // Step 2: Click on "WCZYTAJ GRE" / Load Game button
        // This should be visible after clicking Skirmish
        int loadGameX = (int)(box.Width * 0.15);
        int loadGameY = (int)(box.Height * 0.5);

        Console.WriteLine("Clicking at Load Game position: (" + loadGameX + ", " + loadGameY + ")");
        await page.Mouse.ClickAsync(box.X + loadGameX, box.Y + loadGameY);
        await Task.Delay(2000);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = @"C:\\TzarBot\\Screenshots\\nav_04_after_loadgame_click.png"
        });

        // Step 3: The file picker should appear
        // In browser version, this might work differently than desktop
        Console.WriteLine("Checking for file picker or map list...");
        await Task.Delay(3000);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = @"C:\\TzarBot\\Screenshots\\nav_05_file_picker.png"
        });
    }
}
else
{
    Console.WriteLine("Canvas not found - trying direct mouse clicks on page...");
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = @"C:\\TzarBot\\Screenshots\\nav_error_no_canvas.png"
    });
}

Console.WriteLine("Navigation test completed at: " + DateTime.Now);
Console.WriteLine("Screenshots saved to C:\\TzarBot\\Screenshots\\");

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
Set-Location "C:\TzarBot\BrowserNavTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\browser_nav_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_browser_nav_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_browser_nav_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test nawigacji..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    $maxWait = 120
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
    if (Test-Path "C:\TzarBot\Logs\browser_nav_test.log") {
        Get-Content "C:\TzarBot\Logs\browser_nav_test.log"
    }

    # Lista screenshotow
    Write-Host ""
    Write-Host "=== Screenshoty ===" -ForegroundColor Cyan
    Get-ChildItem "C:\TzarBot\Screenshots\nav_*.png" | ForEach-Object {
        $size = $_.Length / 1KB
        Write-Host "$($_.Name) - $([math]::Round($size, 2)) KB"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
