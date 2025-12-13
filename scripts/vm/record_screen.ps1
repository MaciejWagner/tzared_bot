# record_screen.ps1
# URUCHOMIC NA VM DEV!
# Skrypt do nagrywania ekranu przez FFmpeg

param(
    [string]$OutputFile = "C:\TzarBot\Recordings\recording_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').mp4",
    [int]$Duration = 60,        # Czas nagrywania w sekundach (0 = do zatrzymania)
    [int]$Framerate = 30,       # FPS
    [string]$Resolution = "1920x1080"  # Rozdzielczosc
)

Write-Host "=== Nagrywanie Ekranu FFmpeg ===" -ForegroundColor Cyan
Write-Host "Output: $OutputFile"
Write-Host "Duration: $Duration s (0 = manual stop)"
Write-Host "Framerate: $Framerate FPS"
Write-Host "Resolution: $Resolution"

# Utworz katalog jesli nie istnieje
$outputDir = Split-Path -Parent $OutputFile
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "Utworzono katalog: $outputDir"
}

# Sprawdz FFmpeg
$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if (-not $ffmpeg) {
    Write-Host "BLAD: FFmpeg nie znaleziony! Uruchom najpierw install_ffmpeg.ps1" -ForegroundColor Red
    exit 1
}

# Buduj komende FFmpeg
# -f gdigrab: przechwytywanie ekranu Windows
# -i desktop: caly desktop
# -framerate: FPS
# -t: czas trwania
# -c:v libx264: kodek H.264
# -preset ultrafast: szybkie kodowanie (mniejsze CPU)
# -crf 23: jakosc (nizsza = lepsza, 18-28 typowe)

$ffmpegArgs = @(
    "-y"                        # Nadpisz plik jesli istnieje
    "-f", "gdigrab"             # Windows screen capture
    "-framerate", $Framerate
    "-i", "desktop"             # Caly desktop
    "-c:v", "libx264"           # Kodek
    "-preset", "ultrafast"      # Szybkie kodowanie
    "-crf", "23"                # Jakosc
    "-pix_fmt", "yuv420p"       # Format pikseli (kompatybilnosc)
)

if ($Duration -gt 0) {
    $ffmpegArgs += @("-t", $Duration)
}

$ffmpegArgs += $OutputFile

Write-Host ""
Write-Host "Rozpoczynam nagrywanie..." -ForegroundColor Green
Write-Host "Aby zatrzymac: Ctrl+C lub zamknij okno"
Write-Host ""

# Uruchom FFmpeg
$process = Start-Process -FilePath "ffmpeg" -ArgumentList $ffmpegArgs -NoNewWindow -PassThru -Wait

if ($process.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "Nagrywanie zakonczone pomyslnie!" -ForegroundColor Green
    Write-Host "Plik: $OutputFile"

    # Pokaz info o pliku
    $fileInfo = Get-Item $OutputFile
    Write-Host "Rozmiar: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
} else {
    Write-Host "BLAD: FFmpeg zakonczyl z kodem $($process.ExitCode)" -ForegroundColor Red
}
