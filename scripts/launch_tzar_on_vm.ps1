# launch_tzar_on_vm.ps1
# Uruchamia Tzar na VM DEV z poziomu hosta
# Uzywa Scheduled Task aby uruchomic w sesji interaktywnej

param(
    [switch]$WithRecording,      # Czy nagrac sesje
    [int]$RecordDuration = 120   # Czas nagrywania (sekundy)
)

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "=== Uruchamianie Tzar na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($withRecording, $recordDuration)

    Write-Host "Hostname: $(hostname)"
    $taskName = "TzarBot_GameLauncher"
    $logDir = "C:\TzarBot\Logs"
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null

    # Stworz skrypt do uruchomienia w sesji interaktywnej
    $scriptContent = @"

`$logFile = "C:\TzarBot\Logs\launcher_`$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
"Starting Tzar..." | Out-File `$logFile

`$tzarPath = "C:\Program Files\Tzared\Tzared.exe"
if (-not (Test-Path `$tzarPath)) {
    "BLAD: Nie znaleziono Tzar" | Out-File `$logFile -Append
    exit 1
}

# Zamknij istniejaca instancje
Get-Process -Name "Tzared" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Uruchom gre
`$proc = Start-Process -FilePath `$tzarPath -PassThru
"Uruchomiono PID: `$(`$proc.Id)" | Out-File `$logFile -Append

# Czekaj na okno
`$timeout = 30
`$elapsed = 0
while (`$elapsed -lt `$timeout) {
    Start-Sleep -Seconds 1
    `$elapsed++
    if (`$proc.MainWindowHandle -ne 0) {
        "Okno wykryte po `$elapsed s (Handle: `$(`$proc.MainWindowHandle))" | Out-File `$logFile -Append
        break
    }
}

# Zapisz info
@{
    PID = `$proc.Id
    WindowHandle = `$proc.MainWindowHandle
    StartTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
} | ConvertTo-Json | Out-File "C:\TzarBot\tzar_process.json"

"Gra uruchomiona pomyslnie!" | Out-File `$logFile -Append
"@

    $scriptPath = "C:\TzarBot\launch_game.ps1"
    $scriptContent | Out-File $scriptPath -Encoding UTF8
    Write-Host "Utworzono skrypt: $scriptPath"

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz nowy task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $scriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
    Write-Host "Zarejestrowano task: $taskName"

    # Uruchom task
    Write-Host "Uruchamiam Tzar..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj na zakonczenie taska
    $maxWait = 45
    $waited = 0
    do {
        Start-Sleep -Seconds 1
        $waited++
        $taskInfo = Get-ScheduledTask -TaskName $taskName
        $status = $taskInfo.State
        if ($waited % 5 -eq 0) {
            Write-Host "  Status: $status ($waited s)"
        }
    } while ($status -eq "Running" -and $waited -lt $maxWait)

    # Sprawdz wyniki
    Write-Host ""
    Write-Host "=== Wyniki ===" -ForegroundColor Cyan

    $latestLog = Get-ChildItem "$logDir\launcher_*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-Host "Log:"
        Get-Content $latestLog.FullName
    }

    # Sprawdz czy gra dziala
    $tzarProc = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
    if ($tzarProc) {
        Write-Host ""
        Write-Host "SUKCES! Tzar dziala (PID: $($tzarProc.Id))" -ForegroundColor Green

        # Pokaz info z JSON
        if (Test-Path "C:\TzarBot\tzar_process.json") {
            $info = Get-Content "C:\TzarBot\tzar_process.json" | ConvertFrom-Json
            Write-Host "Window Handle: $($info.WindowHandle)"
        }
    } else {
        Write-Host ""
        Write-Host "BLAD: Tzar nie dziala" -ForegroundColor Red
    }

    # Cleanup task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

} -ArgumentList $WithRecording, $RecordDuration

Write-Host ""
Write-Host "=== Zakonczono ===" -ForegroundColor Cyan
