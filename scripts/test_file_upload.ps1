# test_file_upload.ps1
# Testuje upload mapy treningowej

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test uploadu mapy na VM DEV ===" -ForegroundColor Cyan

# Najpierw skopiuj mape treningowa na VM
Write-Host "Kopiuje mape treningowa na VM..."
$session = New-PSSession -VMName "DEV" -Credential $cred
$mapSource = "C:\Users\maciek\ai_experiments\tzar_bot\training_maps\training-0.tzared"
$mapDest = "C:\TzarBot\Maps\training-0.tzared"

Invoke-Command -Session $session -ScriptBlock {
    param($dest)
    $dir = Split-Path $dest -Parent
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
} -ArgumentList $mapDest

Copy-Item -ToSession $session -Path $mapSource -Destination $mapDest
Write-Host "Mapa skopiowana do: $mapDest"
Remove-PSSession $session

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_FileUploadTest"
    $workDir = "C:\TzarBot\FileUploadTest"
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
    $csprojContent | Out-File "$workDir\FileUploadTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("=== File Upload Test ===");
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
await page.ClickAsync("#rnd0");
await Task.Delay(2000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fu_01_potyczka.png" });

Console.WriteLine("Step 2: Setting up file chooser handler...");

// Set up file chooser handler BEFORE clicking
var fileChooserTask = page.WaitForFileChooserAsync(new PageWaitForFileChooserOptions { Timeout = 10000 });

Console.WriteLine("Step 3: Click WCZYTAJ GRE (#load1)");
await page.ClickAsync("#load1");

try
{
    Console.WriteLine("Waiting for file chooser...");
    var fileChooser = await fileChooserTask;
    Console.WriteLine("File chooser opened!");
    Console.WriteLine("  IsMultiple: " + fileChooser.IsMultiple);

    // Select the training map
    var mapPath = @"C:\\TzarBot\\Maps\\training-0.tzared";
    Console.WriteLine("Selecting file: " + mapPath);

    if (System.IO.File.Exists(mapPath))
    {
        await fileChooser.SetFilesAsync(mapPath);
        Console.WriteLine("File selected!");
    }
    else
    {
        Console.WriteLine("ERROR: Map file not found at " + mapPath);
    }
}
catch (TimeoutException)
{
    Console.WriteLine("File chooser did NOT appear (timeout)");
    Console.WriteLine("Maybe the Load button works differently...");

    // Check if there's an input[type=file] that appeared
    var fileInputs = await page.QuerySelectorAllAsync("input[type='file']");
    Console.WriteLine("File inputs after click: " + fileInputs.Count);
}

await Task.Delay(3000);
await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fu_02_after_file_select.png" });

// Check URL and page state
Console.WriteLine("");
Console.WriteLine("Current URL: " + page.Url);

// Look for game canvas
var canvas = await page.QuerySelectorAsync("canvas");
Console.WriteLine("Canvas found: " + (canvas != null));

// Check if GRAJ button is still there
var playButton = await page.QuerySelectorAsync("#start2");
Console.WriteLine("GRAJ button found: " + (playButton != null));

// If map was loaded, try clicking GRAJ
if (playButton != null)
{
    Console.WriteLine("");
    Console.WriteLine("Step 4: Click GRAJ (#start2)");
    await playButton.ClickAsync();
    await Task.Delay(5000);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fu_03_after_play.png" });

    // Wait for game to load
    Console.WriteLine("Waiting 15 seconds for game to load...");
    await Task.Delay(15000);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fu_04_game_loading.png" });
}

await page.ScreenshotAsync(new PageScreenshotOptions { Path = @"C:\\TzarBot\\Screenshots\\fu_final.png" });

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
Set-Location "C:\TzarBot\FileUploadTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\file_upload_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_file_upload_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_file_upload_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test uploadu mapy..." -ForegroundColor Yellow
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
    if (Test-Path "C:\TzarBot\Logs\file_upload_test.log") {
        Get-Content "C:\TzarBot\Logs\file_upload_test.log"
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
