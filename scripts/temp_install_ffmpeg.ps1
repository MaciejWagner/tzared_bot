# Zaladuj modul Security jesli potrzebny
Import-Module Microsoft.PowerShell.Security -ErrorAction SilentlyContinue

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "Laczenie z VM DEV..."

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== Instalacja FFmpeg na VM ===" -ForegroundColor Cyan
    Write-Host "Hostname: $(hostname)"

    # Sprawdz czy FFmpeg juz jest
    $ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($ffmpeg) {
        Write-Host "FFmpeg juz zainstalowany: $($ffmpeg.Source)" -ForegroundColor Green
        ffmpeg -version 2>&1 | Select-Object -First 2
        return
    }

    Write-Host "FFmpeg nie znaleziony. Instaluje recznie..." -ForegroundColor Yellow

    $ffmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
    $downloadPath = "$env:TEMP\ffmpeg.zip"
    $extractPath = "C:\Tools\FFmpeg"

    # Utworz katalog
    New-Item -ItemType Directory -Path $extractPath -Force | Out-Null

    # Pobierz
    Write-Host "Pobieranie z: $ffmpegUrl"
    Write-Host "To moze zajac kilka minut..."

    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $ffmpegUrl -OutFile $downloadPath -UseBasicParsing
        Write-Host "Pobrano: $((Get-Item $downloadPath).Length / 1MB) MB"
    } catch {
        Write-Host "BLAD pobierania: $_" -ForegroundColor Red
        return
    }

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
        & "$ffmpegBinDir\ffmpeg.exe" -version 2>&1 | Select-Object -First 2
    } else {
        Write-Host "BLAD: Nie znaleziono ffmpeg.exe po rozpakowaniu" -ForegroundColor Red
    }

    # Cleanup
    Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

    Write-Host "=== Instalacja zakonczona ===" -ForegroundColor Cyan
}

Write-Host "Zakonczono."
