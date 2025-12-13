# Test FFmpeg z sesja interaktywna na VM DEV
# Uzywa Scheduled Task aby uruchomic w kontekscie zalogowanego uzytkownika

Import-Module Microsoft.PowerShell.Security -ErrorAction SilentlyContinue
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "Laczenie z VM DEV..." -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== Test FFmpeg w trybie interaktywnym ===" -ForegroundColor Cyan
    Write-Host "Hostname: $(hostname)"

    # Sprawdz czy uzytkownik jest zalogowany w sesji konsoli
    $sessions = query user 2>$null
    Write-Host "Sesje uzytkownikow:"
    $sessions | ForEach-Object { Write-Host $_ }

    # Przygotuj katalog
    $recordDir = "C:\TzarBot\Recordings"
    New-Item -ItemType Directory -Path $recordDir -Force | Out-Null

    # Stworz skrypt do uruchomienia w sesji interaktywnej
    $scriptPath = "C:\TzarBot\test_record.ps1"
    $recordScript = @'
$testOutput = "C:\TzarBot\Recordings\test_interactive.mp4"
$logFile = "C:\TzarBot\Recordings\ffmpeg_log.txt"

# Odswiez PATH
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

# Uruchom FFmpeg
$result = & ffmpeg -y -f gdigrab -framerate 10 -t 3 -i desktop -c:v libx264 -preset ultrafast $testOutput 2>&1
$result | Out-File $logFile

if (Test-Path $testOutput) {
    $size = (Get-Item $testOutput).Length / 1KB
    "OK: Nagranie $testOutput ($size KB)" | Out-File $logFile -Append
} else {
    "BLAD: Nagranie nie utworzone" | Out-File $logFile -Append
}
'@

    $recordScript | Out-File $scriptPath -Encoding UTF8
    Write-Host "Utworzono skrypt: $scriptPath"

    # Stworz Scheduled Task ktore uruchomi skrypt w sesji interaktywnej
    $taskName = "TzarBot_FFmpegTest"

    # Usun stary task jesli istnieje
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz nowy task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $scriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
    Write-Host "Zarejestrowano task: $taskName"

    # Uruchom task
    Write-Host "Uruchamiam task..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj na zakonczenie
    $maxWait = 30
    $waited = 0
    do {
        Start-Sleep -Seconds 1
        $waited++
        $taskInfo = Get-ScheduledTask -TaskName $taskName
        $status = $taskInfo.State
        Write-Host "  Status: $status ($waited s)"
    } while ($status -eq "Running" -and $waited -lt $maxWait)

    # Sprawdz wyniki
    Write-Host ""
    Write-Host "=== Wyniki ===" -ForegroundColor Cyan

    $logFile = "C:\TzarBot\Recordings\ffmpeg_log.txt"
    if (Test-Path $logFile) {
        Write-Host "Log FFmpeg:"
        Get-Content $logFile | Select-Object -Last 15
    }

    $testOutput = "C:\TzarBot\Recordings\test_interactive.mp4"
    if (Test-Path $testOutput) {
        $size = (Get-Item $testOutput).Length / 1KB
        Write-Host ""
        Write-Host "SUKCES! Nagranie: $testOutput ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "BLAD: Nagranie nie zostalo utworzone" -ForegroundColor Red
    }

    # Cleanup task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
}

Write-Host "Zakonczono." -ForegroundColor Cyan
