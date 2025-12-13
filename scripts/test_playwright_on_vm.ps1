# test_playwright_on_vm.ps1
# Testuje Playwright na VM DEV - otwiera tza.red w przegladarce

# Tworzenie credentials
$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Test Playwright na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_PlaywrightTest"
    $workDir = "C:\TzarBot\PlaywrightTest"
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null

    Write-Host "Tworzenie projektu testowego..."

    # Stworz prosty projekt testowy
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
    $csprojContent | Out-File "$workDir\PlaywrightTest.csproj" -Encoding UTF8

    $programContent = @"
using Microsoft.Playwright;

Console.WriteLine("Starting Playwright test...");

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false  // Show browser window
});

var page = await browser.NewPageAsync();
await page.GotoAsync("https://tza.red");

Console.WriteLine("Opened tza.red");
Console.WriteLine("Title: " + await page.TitleAsync());

// Take screenshot
await page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = @"C:\\TzarBot\\Screenshots\\playwright_test.png"
});
Console.WriteLine("Screenshot saved to C:\\TzarBot\\Screenshots\\playwright_test.png");

// Wait a bit to see the page
await Task.Delay(5000);

await browser.CloseAsync();
Console.WriteLine("Test completed successfully!");
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

    # Stworz skrypt do uruchomienia w sesji interaktywnej
    $runScript = @"
Set-Location "C:\TzarBot\PlaywrightTest"
`$env:PLAYWRIGHT_BROWSERS_PATH = "`$env:LOCALAPPDATA\ms-playwright"
dotnet run 2>&1 | Out-File "C:\TzarBot\Logs\playwright_test.log"
"@
    $runScript | Out-File "C:\TzarBot\run_playwright_test.ps1" -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task (musi byc w sesji interaktywnej aby pokazac przegladarke)
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File C:\TzarBot\run_playwright_test.ps1"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam test Playwright..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    $maxWait = 60
    $waited = 0
    do {
        Start-Sleep -Seconds 2
        $waited += 2
        $taskInfo = Get-ScheduledTask -TaskName $taskName
        $status = $taskInfo.State
        if ($waited % 10 -eq 0) {
            Write-Host "  Status: $status ($waited s)"
        }
    } while ($status -eq "Running" -and $waited -lt $maxWait)

    # Pokaz log
    Write-Host ""
    Write-Host "=== Log testu ===" -ForegroundColor Cyan
    if (Test-Path "C:\TzarBot\Logs\playwright_test.log") {
        Get-Content "C:\TzarBot\Logs\playwright_test.log"
    }

    # Sprawdz screenshot
    if (Test-Path "C:\TzarBot\Screenshots\playwright_test.png") {
        $size = (Get-Item "C:\TzarBot\Screenshots\playwright_test.png").Length / 1KB
        Write-Host ""
        Write-Host "SUKCES! Screenshot: C:\TzarBot\Screenshots\playwright_test.png ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
