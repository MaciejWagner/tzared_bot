# GameLauncher.ps1
# Skrypt uruchamiany NA VM DEV (w sesji interaktywnej)
# Uruchamia Tzar i czeka na okno gry

param(
    [string]$TzarPath = "C:\Program Files\Tzared\Tzared.exe",
    [int]$WaitTimeout = 30
)

$logDir = "C:\TzarBot\Logs"
$logFile = "$logDir\game_launcher_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logEntry = "[$timestamp] $Message"
    Write-Host $logEntry -ForegroundColor $Color
    Add-Content -Path $logFile -Value $logEntry
}

# Utworz katalog logow
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

Write-Log "=== GameLauncher ===" "Cyan"
Write-Log "Hostname: $(hostname)"
Write-Log "TzarPath: $TzarPath"

# Sprawdz czy Tzar istnieje
if (-not (Test-Path $TzarPath)) {
    Write-Log "BLAD: Nie znaleziono Tzar: $TzarPath" "Red"
    exit 1
}

# Sprawdz czy gra juz dziala
$existingProcess = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Log "Tzar juz dziala (PID: $($existingProcess.Id)). Zamykam..." "Yellow"
    $existingProcess | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Uruchom Tzar
Write-Log "Uruchamiam Tzar..." "Green"
$tzarProcess = Start-Process -FilePath $TzarPath -PassThru

if (-not $tzarProcess) {
    Write-Log "BLAD: Nie udalo sie uruchomic procesu" "Red"
    exit 1
}

Write-Log "Proces uruchomiony (PID: $($tzarProcess.Id))"

# Czekaj na okno gry
Write-Log "Oczekiwanie na okno gry (max $WaitTimeout s)..."
$elapsed = 0
$windowHandle = $null

while ($elapsed -lt $WaitTimeout) {
    Start-Sleep -Seconds 1
    $elapsed++

    # Sprawdz czy proces wciaz zyje
    if ($tzarProcess.HasExited) {
        Write-Log "BLAD: Proces zakonczyl sie przedwczesnie (Exit code: $($tzarProcess.ExitCode))" "Red"
        exit 1
    }

    # Sprawdz okno
    $proc = Get-Process -Id $tzarProcess.Id -ErrorAction SilentlyContinue
    if ($proc -and $proc.MainWindowHandle -ne 0) {
        $windowHandle = $proc.MainWindowHandle
        Write-Log "Okno gry wykryte! (Handle: $windowHandle)" "Green"
        break
    }

    if ($elapsed % 5 -eq 0) {
        Write-Log "  Czekam... ($elapsed s)"
    }
}

if (-not $windowHandle) {
    Write-Log "UWAGA: Okno gry nie zostalo wykryte w czasie $WaitTimeout s" "Yellow"
}

# Zapisz info o procesie dla innych skryptow
$processInfo = @{
    PID = $tzarProcess.Id
    StartTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    WindowHandle = $windowHandle
    TzarPath = $TzarPath
}

$processInfo | ConvertTo-Json | Out-File "C:\TzarBot\tzar_process.json" -Force
Write-Log "Zapisano info o procesie: C:\TzarBot\tzar_process.json"

Write-Log ""
Write-Log "=== Gra uruchomiona ===" "Cyan"
Write-Log "PID: $($tzarProcess.Id)"
Write-Log "Window Handle: $windowHandle"
Write-Log ""

# Zwroc wynik
return @{
    Success = $true
    PID = $tzarProcess.Id
    WindowHandle = $windowHandle
}
