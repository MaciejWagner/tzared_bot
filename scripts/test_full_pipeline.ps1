# test_full_pipeline.ps1
# Pelny test pipeline na VM DEV:
# 1. Uruchom Tzar
# 2. Nawiguj do mapy treningowej
# 3. Nagraj sesje
# 4. Wykryj zwyciestwo (Victory Screen)

param(
    [string]$MapName = "training-0",
    [int]$TimeoutSeconds = 20,    # Timeout na probe (mapa ma warunek czasu)
    [int]$MaxAttempts = 10,       # Liczba prob na bota
    [switch]$SkipLaunch           # Pomin uruchamianie jesli gra juz dziala
)

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   TzarBot Pipeline Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Mapa: $MapName"
Write-Host "Timeout: $TimeoutSeconds s na probe"
Write-Host "Proby: $MaxAttempts"
Write-Host ""

# Krok 1: Sprawdz/uruchom Tzar
Write-Host "[1/4] Sprawdzam Tzar..." -ForegroundColor Yellow

$tzarRunning = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $proc = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
    return ($null -ne $proc)
}

if (-not $tzarRunning -and -not $SkipLaunch) {
    Write-Host "  Uruchamiam Tzar..."
    & "$PSScriptRoot\launch_tzar_on_vm.ps1"
    Start-Sleep -Seconds 3
} elseif ($tzarRunning) {
    Write-Host "  Tzar juz dziala" -ForegroundColor Green
} else {
    Write-Host "  Tzar nie dziala - uzyj bez -SkipLaunch" -ForegroundColor Red
    exit 1
}

# Krok 2: Sprawdz mape
Write-Host ""
Write-Host "[2/4] Sprawdzam mape treningowa..." -ForegroundColor Yellow

$mapExists = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($mapName)
    $mapPath = "C:\TzarBot\Maps\$mapName.tzared"
    return (Test-Path $mapPath)
} -ArgumentList $MapName

if ($mapExists) {
    Write-Host "  Mapa znaleziona: C:\TzarBot\Maps\$MapName.tzared" -ForegroundColor Green
} else {
    Write-Host "  UWAGA: Mapa nie znaleziona!" -ForegroundColor Yellow
    Write-Host "  Skopiuj mape do C:\TzarBot\Maps\$MapName.tzared na VM DEV"
    Write-Host ""
    Write-Host "Kontynuowac mimo to? (tak/nie)"
    $response = Read-Host
    if ($response -ne "tak") {
        exit 1
    }
}

# Krok 3: Nawiguj do mapy
Write-Host ""
Write-Host "[3/4] Nawigacja do mapy..." -ForegroundColor Yellow
& "$PSScriptRoot\navigate_menu_on_vm.ps1" -MapName $MapName

# Krok 4: Nagraj i monitoruj
Write-Host ""
Write-Host "[4/4] Nagrywanie i monitorowanie..." -ForegroundColor Yellow

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($maxTime, $mapName)

    $taskName = "TzarBot_RecordAndMonitor"
    $recordDir = "C:\TzarBot\Recordings"
    New-Item -ItemType Directory -Path $recordDir -Force | Out-Null

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $outputFile = "$recordDir\${mapName}_${timestamp}.mp4"

    # Skrypt do nagrywania
    $scriptContent = @"
`$logFile = "C:\TzarBot\Logs\pipeline_`$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

function Log(`$msg) {
    `$entry = "[`$(Get-Date -Format 'HH:mm:ss')] `$msg"
    Write-Host `$entry
    Add-Content -Path `$logFile -Value `$entry
}

Log "=== Pipeline Recording ==="
Log "Nagrywam do: $outputFile"
Log "Max czas: $maxTime s"

# Odswiez PATH dla FFmpeg
`$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

# Uruchom FFmpeg w tle
Log "Uruchamiam nagrywanie..."
`$ffmpegJob = Start-Job -ScriptBlock {
    param(`$output, `$duration)
    & ffmpeg -y -f gdigrab -framerate 10 -t `$duration -i desktop -c:v libx264 -preset ultrafast `$output 2>&1
} -ArgumentList "$outputFile", $maxTime

Log "FFmpeg uruchomiony (Job: `$(`$ffmpegJob.Id))"

# Monitoruj przez max czas
`$elapsed = 0
`$checkInterval = 5
while (`$elapsed -lt $maxTime) {
    Start-Sleep -Seconds `$checkInterval
    `$elapsed += `$checkInterval

    # Sprawdz czy gra dziala
    `$tzar = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
    if (-not `$tzar) {
        Log "Gra zakonczona po `$elapsed s"
        break
    }

    Log "Monitoring: `$elapsed s / $maxTime s"
}

# Zatrzymaj nagrywanie
Log "Zatrzymuje nagrywanie..."
Stop-Job -Job `$ffmpegJob -ErrorAction SilentlyContinue
Receive-Job -Job `$ffmpegJob | Out-Null
Remove-Job -Job `$ffmpegJob -ErrorAction SilentlyContinue

# Sprawdz wynik
if (Test-Path "$outputFile") {
    `$size = (Get-Item "$outputFile").Length / 1KB
    Log "SUKCES! Nagranie: $outputFile (`$([math]::Round(`$size, 2)) KB)"
} else {
    Log "BLAD: Nagranie nie zostalo utworzone"
}

Log "=== Pipeline zakonczony ==="
"@

    $scriptPath = "C:\TzarBot\record_monitor.ps1"
    $scriptContent | Out-File $scriptPath -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $scriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Uruchamiam nagrywanie ($maxTime s max)..."
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    $waited = 0
    $maxWait = $maxTime + 30
    do {
        Start-Sleep -Seconds 5
        $waited += 5
        $taskInfo = Get-ScheduledTask -TaskName $taskName
        $status = $taskInfo.State
        Write-Host "  Recording: $waited s (status: $status)"
    } while ($status -eq "Running" -and $waited -lt $maxWait)

    # Pokaz log
    Write-Host ""
    Write-Host "=== Log pipeline ===" -ForegroundColor Cyan
    $latestLog = Get-ChildItem "C:\TzarBot\Logs\pipeline_*.log" -ErrorAction SilentlyContinue |
                 Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Get-Content $latestLog.FullName
    }

    # Pokaz nagrania
    Write-Host ""
    Write-Host "=== Nagrania ===" -ForegroundColor Cyan
    Get-ChildItem $recordDir\*.mp4 | Sort-Object LastWriteTime -Descending | Select-Object -First 3 |
        ForEach-Object {
            $size = [math]::Round($_.Length/1KB, 2)
            Write-Host "  $($_.Name) - $size KB"
        }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

} -ArgumentList $MaxGameTime, $MapName

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   Pipeline Test Zakonczony" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
