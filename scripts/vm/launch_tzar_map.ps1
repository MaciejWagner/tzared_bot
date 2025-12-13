# launch_tzar_map.ps1
# URUCHOMIC NA VM DEV!
# Uruchamia Tzar i laduje mape przez nawigacje menu

param(
    [string]$MapName = "training-0",    # Nazwa mapy (bez rozszerzenia)
    [string]$TzarPath = "C:\Program Files\Tzared\Tzared.exe",
    [switch]$RecordSession,              # Czy nagrac sesje
    [int]$RecordDuration = 120           # Czas nagrywania
)

Write-Host "=== Uruchamianie Tzar z mapa: $MapName ===" -ForegroundColor Cyan

# Sprawdz czy Tzar istnieje
if (-not (Test-Path $TzarPath)) {
    Write-Host "BLAD: Nie znaleziono Tzar: $TzarPath" -ForegroundColor Red
    exit 1
}

# Sprawdz czy mapa istnieje
$mapPaths = @(
    "C:\TzarBot\Maps\$MapName.tzared",
    "C:\TzarBot\Maps\$MapName.scn",
    "C:\Program Files\Tzared\Maps\$MapName.tzared",
    "C:\Program Files\Tzared\Maps\$MapName.scn"
)

$mapFound = $null
foreach ($path in $mapPaths) {
    if (Test-Path $path) {
        $mapFound = $path
        break
    }
}

if (-not $mapFound) {
    Write-Host "UWAGA: Mapa '$MapName' nie znaleziona w standardowych lokalizacjach" -ForegroundColor Yellow
    Write-Host "Sprawdzone sciezki:"
    $mapPaths | ForEach-Object { Write-Host "  - $_" }
    Write-Host ""
    Write-Host "Kontynuuje - mapa moze byc w innym miejscu lub nazwa bedzie wpisana recznie"
}

# Opcjonalnie: rozpocznij nagrywanie w tle
$recordJob = $null
if ($RecordSession) {
    Write-Host "Uruchamiam nagrywanie ekranu..." -ForegroundColor Yellow
    $recordScript = Join-Path $PSScriptRoot "record_screen.ps1"
    if (Test-Path $recordScript) {
        $recordJob = Start-Job -ScriptBlock {
            param($script, $duration)
            & $script -Duration $duration
        } -ArgumentList $recordScript, $RecordDuration
        Write-Host "Nagrywanie uruchomione w tle (Job ID: $($recordJob.Id))"
        Start-Sleep -Seconds 2  # Daj czas na start nagrywania
    } else {
        Write-Host "UWAGA: Brak skryptu nagrywania: $recordScript" -ForegroundColor Yellow
    }
}

# Uruchom Tzar
Write-Host "Uruchamiam Tzar..." -ForegroundColor Green
$tzarProcess = Start-Process -FilePath $TzarPath -PassThru

# Poczekaj az gra sie uruchomi
Write-Host "Czekam na uruchomienie gry..."
$timeout = 30
$elapsed = 0
while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++

    # Sprawdz czy okno gry istnieje
    $tzarWindow = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 }

    if ($tzarWindow) {
        Write-Host "Gra uruchomiona! (PID: $($tzarWindow.Id))"
        break
    }
}

if (-not $tzarWindow) {
    Write-Host "UWAGA: Nie wykryto okna gry po $timeout sekundach" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== INSTRUKCJA NAWIGACJI DO MAPY ===" -ForegroundColor Cyan
Write-Host "1. W menu glownym kliknij 'Single Player' lub 'Skirmish'"
Write-Host "2. Wybierz 'Load Map' lub 'Custom Map'"
Write-Host "3. Znajdz mape: $MapName"
Write-Host "4. Kliknij 'Start' lub 'Play'"
Write-Host ""
Write-Host "Po zakonczeniu gry:"
Write-Host "- Nagranie zostanie zapisane automatycznie (jesli wlaczone)"
Write-Host "- Zamknij gre przez menu lub Alt+F4"

# Czekaj na zakonczenie gry
Write-Host ""
Write-Host "Oczekiwanie na zakonczenie gry..." -ForegroundColor Yellow
if ($tzarProcess) {
    $tzarProcess.WaitForExit()
    Write-Host "Gra zakonczona."
}

# Zatrzymaj nagrywanie
if ($recordJob) {
    Write-Host "Zatrzymuje nagrywanie..."
    Stop-Job -Job $recordJob -ErrorAction SilentlyContinue
    Receive-Job -Job $recordJob
    Remove-Job -Job $recordJob
}

Write-Host "=== Sesja zakonczona ===" -ForegroundColor Cyan
