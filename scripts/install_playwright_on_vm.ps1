# install_playwright_on_vm.ps1
# Instaluje Playwright i przegladarki na VM DEV

# Tworzenie credentials
$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Instalacja Playwright na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "Hostname: $(hostname)"

    # Sprawdz czy dotnet jest zainstalowany
    $dotnetVersion = dotnet --version 2>$null
    if (-not $dotnetVersion) {
        Write-Host "BLAD: .NET SDK nie jest zainstalowany!" -ForegroundColor Red
        return
    }
    Write-Host ".NET SDK: $dotnetVersion" -ForegroundColor Green

    # Utworz katalog roboczy
    $workDir = "C:\TzarBot\Playwright"
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null
    Set-Location $workDir

    # Stworz tymczasowy projekt do instalacji Playwright CLI
    Write-Host ""
    Write-Host "Tworzenie projektu Playwright..." -ForegroundColor Yellow

    if (-not (Test-Path "playwright-install.csproj")) {
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" Version="1.49.0" />
  </ItemGroup>
</Project>
"@ | Out-File "playwright-install.csproj" -Encoding UTF8

        @"
using Microsoft.Playwright;
Console.WriteLine("Playwright installed!");
"@ | Out-File "Program.cs" -Encoding UTF8
    }

    # Restore dependencies
    Write-Host "Przywracanie zaleznosci..." -ForegroundColor Yellow
    dotnet restore

    # Build
    Write-Host "Budowanie..." -ForegroundColor Yellow
    dotnet build --no-restore

    # Install Playwright browsers
    Write-Host ""
    Write-Host "Instalowanie przegladarek Playwright..." -ForegroundColor Yellow
    Write-Host "(To moze zajac kilka minut)"

    $playwrightPath = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.playwright" -Recurse -Filter "playwright.ps1" | Select-Object -First 1

    if ($playwrightPath) {
        & powershell -ExecutionPolicy Bypass -File $playwrightPath.FullName install chromium
        Write-Host ""
        Write-Host "Przegladarka Chromium zainstalowana!" -ForegroundColor Green
    } else {
        # Alternatywna metoda - przez dotnet tool
        Write-Host "Probuje alternatywna metode instalacji..." -ForegroundColor Yellow
        dotnet tool install --global Microsoft.Playwright.CLI 2>$null
        playwright install chromium
    }

    Write-Host ""
    Write-Host "=== Instalacja zakonczona ===" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Playwright zainstalowany na VM DEV" -ForegroundColor Green
