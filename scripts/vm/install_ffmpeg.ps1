# install_ffmpeg.ps1
# URUCHOMIC NA VM DEV!
# Ten skrypt instaluje FFmpeg na VM

Write-Host "=== Instalacja FFmpeg na VM DEV ===" -ForegroundColor Cyan

# Sprawdz czy FFmpeg juz jest
$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if ($ffmpeg) {
    Write-Host "FFmpeg juz zainstalowany: $($ffmpeg.Source)" -ForegroundColor Green
    ffmpeg -version | Select-Object -First 2
    exit 0
}

Write-Host "FFmpeg nie znaleziony. Instaluje..." -ForegroundColor Yellow

# Opcja 1: Przez winget (preferowane)
$winget = Get-Command winget -ErrorAction SilentlyContinue
if ($winget) {
    Write-Host "Instalacja przez winget..."
    winget install Gyan.FFmpeg --silent --accept-package-agreements --accept-source-agreements

    # Odswierz PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

    $ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($ffmpeg) {
        Write-Host "FFmpeg zainstalowany pomyslnie!" -ForegroundColor Green
        ffmpeg -version | Select-Object -First 2
        exit 0
    }
}

# Opcja 2: Reczne pobranie
Write-Host "Winget nie dziala. Pobieram recznie..." -ForegroundColor Yellow

$ffmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$downloadPath = "$env:TEMP\ffmpeg.zip"
$extractPath = "C:\Tools\FFmpeg"

# Utworz katalog
New-Item -ItemType Directory -Path $extractPath -Force | Out-Null

# Pobierz
Write-Host "Pobieranie z: $ffmpegUrl"
Invoke-WebRequest -Uri $ffmpegUrl -OutFile $downloadPath -UseBasicParsing

# Rozpakuj
Write-Host "Rozpakowywanie..."
Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force

# Znajdz folder z ffmpeg.exe
$ffmpegExe = Get-ChildItem -Path $extractPath -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1
if ($ffmpegExe) {
    $ffmpegBinDir = $ffmpegExe.DirectoryName

    # Dodaj do PATH
    $currentPath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
    if ($currentPath -notlike "*$ffmpegBinDir*") {
        [System.Environment]::SetEnvironmentVariable("Path", "$currentPath;$ffmpegBinDir", "Machine")
        $env:Path = "$env:Path;$ffmpegBinDir"
        Write-Host "Dodano do PATH: $ffmpegBinDir" -ForegroundColor Green
    }

    Write-Host "FFmpeg zainstalowany pomyslnie!" -ForegroundColor Green
    & "$ffmpegBinDir\ffmpeg.exe" -version | Select-Object -First 2
} else {
    Write-Host "BLAD: Nie znaleziono ffmpeg.exe po rozpakowaniu" -ForegroundColor Red
    exit 1
}

# Cleanup
Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

Write-Host "=== Instalacja zakonczona ===" -ForegroundColor Cyan
