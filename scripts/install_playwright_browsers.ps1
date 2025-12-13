# install_playwright_browsers.ps1
# Instaluje przegladarki Playwright na VM DEV dla projektu testowego

$securePassword = New-Object System.Security.SecureString
"password123".ToCharArray() | ForEach-Object { $securePassword.AppendChar($_) }
$securePassword.MakeReadOnly()
$cred = New-Object System.Management.Automation.PSCredential("test", $securePassword)

Write-Host "=== Instalacja przegladarek Playwright na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Set-Location "C:\TzarBot\PlaywrightTest"

    Write-Host "Szukam playwright.ps1..."

    # Znajdz playwright.ps1 w bin
    $ps1Files = Get-ChildItem -Path "bin" -Recurse -Filter "playwright.ps1" -ErrorAction SilentlyContinue

    if ($ps1Files -and $ps1Files.Count -gt 0) {
        $playwrightScript = $ps1Files[0].FullName
        Write-Host "Znaleziono: $playwrightScript" -ForegroundColor Green

        Write-Host "Instaluje Chromium..." -ForegroundColor Yellow
        & powershell -ExecutionPolicy Bypass -File $playwrightScript install chromium

        Write-Host ""
        Write-Host "Instalacja zakonczona!" -ForegroundColor Green
    } else {
        Write-Host "Nie znaleziono playwright.ps1 w bin, sprawdzam nuget cache..." -ForegroundColor Yellow

        $nugetPath = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.playwright" -Recurse -Filter "playwright.ps1" -ErrorAction SilentlyContinue | Select-Object -First 1

        if ($nugetPath) {
            Write-Host "Znaleziono w nuget: $($nugetPath.FullName)" -ForegroundColor Green
            & powershell -ExecutionPolicy Bypass -File $nugetPath.FullName install chromium
        } else {
            Write-Host "BLAD: Nie znaleziono playwright.ps1!" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "=== Gotowe ===" -ForegroundColor Cyan
