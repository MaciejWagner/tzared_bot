#
# TzarBot Demo Setup & Run Script
# Ten skrypt rozpakowuje pliki i uruchamia demo
#

$ErrorActionPreference = "Continue"

Write-Host "=== TzarBot Demo Setup ===" -ForegroundColor Cyan

# Rozpakuj src
if (Test-Path "C:\TzarBot\src.zip") {
    Write-Host "Extracting src.zip..." -ForegroundColor Yellow
    Expand-Archive -Path "C:\TzarBot\src.zip" -DestinationPath "C:\TzarBot\src" -Force
    Remove-Item "C:\TzarBot\src.zip" -Force
    Write-Host "src extracted" -ForegroundColor Green
}

# Rozpakuj tests
if (Test-Path "C:\TzarBot\tests.zip") {
    Write-Host "Extracting tests.zip..." -ForegroundColor Yellow
    Expand-Archive -Path "C:\TzarBot\tests.zip" -DestinationPath "C:\TzarBot\tests" -Force
    Remove-Item "C:\TzarBot\tests.zip" -Force
    Write-Host "tests extracted" -ForegroundColor Green
}

# Pokaz strukture
Write-Host "`nProject structure:" -ForegroundColor Cyan
Get-ChildItem "C:\TzarBot" -Recurse -Depth 2 | Where-Object { -not $_.PSIsContainer } | Select-Object FullName

# Uruchom demo
Write-Host "`n=== Running All Demos ===" -ForegroundColor Cyan
Set-Location "C:\TzarBot\scripts\demo"
& ".\Run-AllDemos.ps1" -ProjectPath "C:\TzarBot" -SkipScreenshots

Write-Host "`n=== Demo Complete ===" -ForegroundColor Green
Write-Host "Results saved to C:\TzarBot-Demo" -ForegroundColor Cyan
