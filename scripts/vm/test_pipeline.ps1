# test_pipeline.ps1
# URUCHOMIC NA VM DEV!
# Testuje caly pipeline: uruchomienie gry, nawigacja, wykonanie akcji, detekcja wyniku

param(
    [string]$TzarPath = "C:\Program Files\Tzared\Tzared.exe",
    [switch]$Record
)

Write-Host "=== TEST PIPELINE TZARBOT ===" -ForegroundColor Cyan
Write-Host "Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

# Funkcje pomocnicze do Input Injection (PowerShell native)
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public class InputHelper {
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    public static void LeftClick() {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public static void RightClick() {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
    }

    public static void MoveTo(int x, int y) {
        SetCursorPos(x, y);
    }

    public static void ClickAt(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        LeftClick();
    }

    public static void RightClickAt(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        RightClick();
    }
}
"@

function Click-At {
    param([int]$X, [int]$Y)
    Write-Host "  Click at ($X, $Y)" -ForegroundColor Gray
    [InputHelper]::ClickAt($X, $Y)
    Start-Sleep -Milliseconds 300
}

function RightClick-At {
    param([int]$X, [int]$Y)
    Write-Host "  Right-Click at ($X, $Y)" -ForegroundColor Gray
    [InputHelper]::RightClickAt($X, $Y)
    Start-Sleep -Milliseconds 300
}

function Move-To {
    param([int]$X, [int]$Y)
    [InputHelper]::MoveTo($X, $Y)
    Start-Sleep -Milliseconds 100
}

function Take-Screenshot {
    param([string]$Name)
    $screenshotDir = "C:\TzarBot\Screenshots"
    if (-not (Test-Path $screenshotDir)) {
        New-Item -ItemType Directory -Path $screenshotDir -Force | Out-Null
    }
    $filename = "$screenshotDir\$Name`_$(Get-Date -Format 'HH-mm-ss').png"

    # Uzyj narzedzia do screenshot (nircmd lub wbudowane)
    Add-Type -AssemblyName System.Windows.Forms
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bitmap = New-Object System.Drawing.Bitmap($screen.Width, $screen.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($screen.Location, [System.Drawing.Point]::Empty, $screen.Size)
    $bitmap.Save($filename)
    $graphics.Dispose()
    $bitmap.Dispose()

    Write-Host "  Screenshot: $filename" -ForegroundColor Gray
    return $filename
}

# ============================================
# KROK 1: Sprawdz srodowisko
# ============================================
Write-Host "[1/5] Sprawdzanie srodowiska..." -ForegroundColor Yellow

if (-not (Test-Path $TzarPath)) {
    Write-Host "BLAD: Tzar nie znaleziony: $TzarPath" -ForegroundColor Red
    exit 1
}
Write-Host "  Tzar: OK" -ForegroundColor Green

# Sprawdz rozdzielczosc ekranu
Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
Write-Host "  Rozdzielczosc: $($screen.Width)x$($screen.Height)" -ForegroundColor Green

# ============================================
# KROK 2: Uruchom gre
# ============================================
Write-Host ""
Write-Host "[2/5] Uruchamianie Tzar..." -ForegroundColor Yellow

# Zamknij stara instancje jesli dziala
$existingTzar = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue
if ($existingTzar) {
    Write-Host "  Zamykam stara instancje..."
    $existingTzar | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Uruchom gre
$tzarProcess = Start-Process -FilePath $TzarPath -PassThru
Write-Host "  Uruchomiono (PID: $($tzarProcess.Id))"

# Czekaj na zaladowanie
Write-Host "  Czekam na zaladowanie menu (10s)..."
Start-Sleep -Seconds 10

Take-Screenshot -Name "01_main_menu"

# ============================================
# KROK 3: Nawigacja do mapy (wymaga dopasowania do menu!)
# ============================================
Write-Host ""
Write-Host "[3/5] Nawigacja przez menu..." -ForegroundColor Yellow
Write-Host "  UWAGA: Pozycje przyciskow moga wymagac kalibracji!" -ForegroundColor Magenta

# Te koordynaty sa PRZYKLADOWE - trzeba je skalibrowan do rozdzielczosci!
# Zakladam rozdzielczosc 1920x1080

# Przykladowa nawigacja (DO KALIBRACJI):
# 1. Kliknij "Skirmish" lub "Single Player" w menu glownym
# 2. Kliknij "Load Map"
# 3. Wybierz mape
# 4. Kliknij "Start"

Write-Host "  [MANUAL] Prosze recznie zaladowac mape training-0" -ForegroundColor Yellow
Write-Host "  Nacisnij ENTER gdy gra bedzie gotowa..." -ForegroundColor Yellow
Read-Host

Take-Screenshot -Name "02_game_started"

# ============================================
# KROK 4: Wykonaj akcje testowa
# ============================================
Write-Host ""
Write-Host "[4/5] Wykonywanie akcji testowej..." -ForegroundColor Yellow

# Zakladamy ze mapa training-0 ma wieniaka na srodku
# Kliknij na wieniaka (selekcja) - centrum ekranu
$centerX = $screen.Width / 2
$centerY = $screen.Height / 2

Write-Host "  Selekcja jednostki (Left-Click na centrum)..."
Click-At -X $centerX -Y $centerY
Start-Sleep -Seconds 1

Take-Screenshot -Name "03_unit_selected"

# Wydaj rozkaz ruchu (Right-Click gdzies obok)
Write-Host "  Rozkaz ruchu (Right-Click)..."
RightClick-At -X ($centerX + 200) -Y ($centerY + 100)
Start-Sleep -Seconds 2

Take-Screenshot -Name "04_move_ordered"

# Poczekaj na wykonanie ruchu i Victory Screen
Write-Host "  Czekam na Victory Screen (30s timeout)..."
$victoryDetected = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1

    # Tutaj mozna dodac detekcje Victory Screen przez porownanie obrazu
    # Na razie tylko screenshot co kilka sekund
    if ($i % 5 -eq 0) {
        Take-Screenshot -Name "05_waiting_$i"
    }
}

Take-Screenshot -Name "06_final_state"

# ============================================
# KROK 5: Raport
# ============================================
Write-Host ""
Write-Host "[5/5] Generowanie raportu..." -ForegroundColor Yellow

$reportPath = "C:\TzarBot\Reports\test_pipeline_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').txt"
$reportDir = Split-Path -Parent $reportPath
if (-not (Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

$report = @"
=== RAPORT TESTU PIPELINE ===
Data: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
VM: $env:COMPUTERNAME
Rozdzielczosc: $($screen.Width)x$($screen.Height)

Status: MANUAL TEST
- Gra uruchomiona: TAK
- Nawigacja: MANUAL
- Akcja wykonana: TAK (klikniecia)
- Victory Screen: DO WERYFIKACJI

Screenshoty: C:\TzarBot\Screenshots\

NASTEPNE KROKI:
1. Sprawdz screenshoty czy akcje zostaly wykonane
2. Skalibruj pozycje przyciskow menu
3. Dodaj automatyczna detekcje Victory Screen
"@

$report | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "  Raport zapisany: $reportPath"

# Zamknij gre
Write-Host ""
Write-Host "Zamykam gre..."
$tzarProcess | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== TEST ZAKONCZONY ===" -ForegroundColor Cyan
Write-Host "Sprawdz screenshoty w: C:\TzarBot\Screenshots\"
