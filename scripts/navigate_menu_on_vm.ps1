# navigate_menu_on_vm.ps1
# Uruchamia MenuNavigator na VM DEV z poziomu hosta
# Nawiguje przez menu Tzar i laduje mape treningowa

param(
    [string]$MapName = "training-0"
)

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "=== Nawigacja menu Tzar na VM DEV ===" -ForegroundColor Cyan
Write-Host "Mapa: $MapName"

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($mapName)

    $taskName = "TzarBot_MenuNavigator"
    $logDir = "C:\TzarBot\Logs"
    $mapsFolder = "C:\TzarBot\Maps"
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    New-Item -ItemType Directory -Path $mapsFolder -Force | Out-Null

    # Sprawdz czy Tzar dziala
    $tzarProc = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
    if (-not $tzarProc) {
        Write-Host "BLAD: Tzar nie dziala! Uruchom najpierw launch_tzar_on_vm.ps1" -ForegroundColor Red
        return
    }
    Write-Host "Tzar dziala (PID: $($tzarProc.Id))"

    # Skrypt do wykonania w sesji interaktywnej
    $mapPath = "$mapsFolder\$mapName.tzared"

    $scriptContent = @"

Add-Type -AssemblyName System.Windows.Forms

Add-Type @`"
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class MenuInput {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public static void ClickAt(int x, int y) {
        SetCursorPos(x, y);
        Thread.Sleep(100);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        Thread.Sleep(200);
    }
}
`"@

`$logFile = "C:\TzarBot\Logs\menu_nav_`$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

function Log(`$msg) {
    `$entry = "[`$(Get-Date -Format 'HH:mm:ss')] `$msg"
    Write-Host `$entry
    Add-Content -Path `$logFile -Value `$entry
}

Log "=== MenuNavigator ==="
Log "Mapa: $mapName"
Log "Sciezka: $mapPath"

# Znajdz i aktywuj Tzar - szukaj procesu z oknem
`$tzar = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue | Where-Object { `$_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not `$tzar) {
    # Probuj wszystkie procesy Tzared
    `$allTzar = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
    Log "Znaleziono $(`$allTzar.Count) procesow Tzared"
    foreach (`$p in `$allTzar) {
        Log "  PID: `$(`$p.Id), Handle: `$(`$p.MainWindowHandle)"
    }
    # Uzyj pierwszego z niezerowym handle lub pierwszego w ogole
    `$tzar = `$allTzar | Where-Object { `$_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not `$tzar) {
        `$tzar = `$allTzar | Select-Object -First 1
    }
}

if (`$tzar) {
    Log "Uzywam Tzar PID: `$(`$tzar.Id), Handle: `$(`$tzar.MainWindowHandle)"
    if (`$tzar.MainWindowHandle -ne 0) {
        [MenuInput]::ShowWindow(`$tzar.MainWindowHandle, 9)
        Start-Sleep -Milliseconds 200
        [MenuInput]::SetForegroundWindow(`$tzar.MainWindowHandle)
        Start-Sleep -Milliseconds 500
        Log "Okno Tzar aktywowane"
    } else {
        Log "UWAGA: Handle = 0, kontynuuje bez aktywacji okna"
    }
} else {
    Log "BLAD: Nie znaleziono procesu Tzar"
    exit 1
}

# Nawigacja
Log "Krok 1: POTYCZKA Z SI..."
[MenuInput]::ClickAt(235, 375)
Start-Sleep -Seconds 1.5

Log "Krok 2: WCZYTAJ GRE..."
[MenuInput]::ClickAt(440, 215)
Start-Sleep -Seconds 1.5

Log "Krok 3: Dialog - wpisuje sciezke..."
Start-Sleep -Seconds 1

# Kliknij pole nazwy pliku
[MenuInput]::ClickAt(305, 497)
Start-Sleep -Milliseconds 300

# Wyczysc i wpisz sciezke
[System.Windows.Forms.SendKeys]::SendWait("^a")
Start-Sleep -Milliseconds 100
[System.Windows.Forms.SendKeys]::SendWait("{DELETE}")
Start-Sleep -Milliseconds 100
[System.Windows.Forms.SendKeys]::SendWait("$mapPath")
Start-Sleep -Milliseconds 500

Log "Krok 4: Otworz..."
[MenuInput]::ClickAt(456, 526)
Start-Sleep -Seconds 2

Log "Krok 5: GRAJ..."
[MenuInput]::ClickAt(435, 602)
Start-Sleep -Seconds 2

Log "=== Nawigacja zakonczona ==="
"@

    $scriptPath = "C:\TzarBot\menu_navigator_run.ps1"
    $scriptContent | Out-File $scriptPath -Encoding UTF8
    Write-Host "Utworzono skrypt: $scriptPath"

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $scriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
    Write-Host "Zarejestrowano task: $taskName"

    # Uruchom task
    Write-Host "Uruchamiam nawigacje menu..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $taskName

    # Czekaj na zakonczenie
    $maxWait = 30
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

    # Sprawdz log
    Write-Host ""
    Write-Host "=== Log nawigacji ===" -ForegroundColor Cyan
    $latestLog = Get-ChildItem "$logDir\menu_nav_*.log" -ErrorAction SilentlyContinue |
                 Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Get-Content $latestLog.FullName
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

} -ArgumentList $MapName

Write-Host ""
Write-Host "=== Zakonczono ===" -ForegroundColor Cyan
