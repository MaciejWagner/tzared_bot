# MenuNavigator.ps1
# Skrypt uruchamiany NA VM DEV (w sesji interaktywnej)
# Nawiguje przez menu Tzar do zaladowania mapy
#
# Sciezka nawigacji:
# 1. POTYCZKA Z SI (menu glowne)
# 2. WCZYTAJ GRE (okno potyczki)
# 3. Windows File Dialog - wybor mapy .tzared
# 4. GRAJ (start gry)

param(
    [string]$MapName = "training-0",
    [string]$MapsFolder = "C:\TzarBot\Maps",
    [int]$ScreenWidth = 1024,
    [int]$ScreenHeight = 768
)

# Importy Win32
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public class InputHelper {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public const int WM_SETTEXT = 0x000C;
    public const int BM_CLICK = 0x00F5;

    public static void MoveTo(int x, int y) {
        SetCursorPos(x, y);
        Thread.Sleep(50);
    }

    public static void LeftClick() {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        Thread.Sleep(100);
    }

    public static void ClickAt(int x, int y) {
        MoveTo(x, y);
        Thread.Sleep(100);
        LeftClick();
    }

    public static void PressKey(byte vk) {
        keybd_event(vk, 0, 0, IntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        Thread.Sleep(50);
    }

    public static void TypeText(string text) {
        foreach (char c in text) {
            SendKeys.SendWait(c.ToString());
            Thread.Sleep(20);
        }
    }
}
"@

Add-Type -AssemblyName System.Windows.Forms

$logDir = "C:\TzarBot\Logs"
$logFile = "$logDir\menu_navigator_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logEntry = "[$timestamp] $Message"
    Write-Host $logEntry -ForegroundColor $Color
    Add-Content -Path $logFile -Value $logEntry
}

# Utworz katalog logow i map
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
New-Item -ItemType Directory -Path $MapsFolder -Force | Out-Null

Write-Log "=== MenuNavigator ===" "Cyan"
Write-Log "Mapa do zaladowania: $MapName"
Write-Log "Folder map: $MapsFolder"

# Sprawdz czy mapa istnieje
$mapPath = Join-Path $MapsFolder "$MapName.tzared"
if (-not (Test-Path $mapPath)) {
    Write-Log "UWAGA: Mapa nie istnieje: $mapPath" "Yellow"
    Write-Log "Kontynuuje - moze byc w innej lokalizacji"
}

# Znajdz okno Tzar
$tzarProc = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $tzarProc -or $tzarProc.MainWindowHandle -eq 0) {
    Write-Log "BLAD: Nie znaleziono okna Tzar!" "Red"
    exit 1
}

Write-Log "Znaleziono Tzar (PID: $($tzarProc.Id))"

# Aktywuj okno
[InputHelper]::ShowWindow($tzarProc.MainWindowHandle, 9)
Start-Sleep -Milliseconds 300
[InputHelper]::SetForegroundWindow($tzarProc.MainWindowHandle)
Start-Sleep -Milliseconds 500

Write-Log "Okno Tzar aktywowane"

# =====================================================
# POZYCJE PRZYCISKOW W MENU
# Bazowane na rozdzielczosci 1024x768
# =====================================================

# Pozycje przyciskow (bazowe dla 1024x768)
# Te pozycje sa SZACOWANE - wymaga kalibracji
$menuPositions = @{
    # Menu glowne (lewy panel)
    "POTYCZKA_Z_SI" = @{ X = 235; Y = 375 }

    # Okno Potyczki z SI (srodkowy panel)
    "WCZYTAJ_GRE" = @{ X = 440; Y = 215 }      # Przycisk "WCZYTAJ GRE"
    "EDYTOR_MAPY" = @{ X = 583; Y = 215 }      # Przycisk "EDYTOR MAPY"
    "GRAJ" = @{ X = 435; Y = 602 }             # Przycisk "GRAJ"
    "USTAWIENIA" = @{ X = 567; Y = 602 }       # Przycisk "USTAWIENIA"
    "ZAMKNIJ_POTYCZKE" = @{ X = 750; Y = 130 } # X w rogu

    # Windows File Dialog (pozycje szacowane)
    "DIALOG_FILENAME" = @{ X = 305; Y = 497 }  # Pole "Nazwa pliku"
    "DIALOG_OPEN" = @{ X = 456; Y = 526 }      # Przycisk "Otworz"
}

Write-Log ""
Write-Log "=== Nawigacja do mapy ===" "Yellow"

# Krok 1: Kliknij POTYCZKA Z SI
Write-Log "Krok 1: POTYCZKA Z SI..."
[InputHelper]::ClickAt($menuPositions["POTYCZKA_Z_SI"].X, $menuPositions["POTYCZKA_Z_SI"].Y)
Start-Sleep -Seconds 1.5

# Krok 2: Kliknij WCZYTAJ GRE
Write-Log "Krok 2: WCZYTAJ GRE..."
[InputHelper]::ClickAt($menuPositions["WCZYTAJ_GRE"].X, $menuPositions["WCZYTAJ_GRE"].Y)
Start-Sleep -Seconds 1.5

# Krok 3: Wypelnij dialog Windows
Write-Log "Krok 3: Wybieram mape w dialogu..."

# Czekaj na dialog
Start-Sleep -Seconds 1

# Wpisz sciezke do mapy w pole "Nazwa pliku"
# Uzyj Ctrl+L aby przejsc do paska adresu, potem wpisz pelna sciezke
Write-Log "  Wpisuje sciezke: $mapPath"

# Metoda 1: Wpisz pelna sciezke bezposrednio w pole nazwy
[InputHelper]::ClickAt($menuPositions["DIALOG_FILENAME"].X, $menuPositions["DIALOG_FILENAME"].Y)
Start-Sleep -Milliseconds 300

# Wyczysc pole (Ctrl+A, Delete)
[System.Windows.Forms.SendKeys]::SendWait("^a")
Start-Sleep -Milliseconds 100
[System.Windows.Forms.SendKeys]::SendWait("{DELETE}")
Start-Sleep -Milliseconds 100

# Wpisz pelna sciezke do mapy
[System.Windows.Forms.SendKeys]::SendWait($mapPath)
Start-Sleep -Milliseconds 500

# Krok 4: Kliknij Otworz
Write-Log "Krok 4: Klikam Otworz..."
[InputHelper]::ClickAt($menuPositions["DIALOG_OPEN"].X, $menuPositions["DIALOG_OPEN"].Y)
Start-Sleep -Seconds 2

# Krok 5: Kliknij GRAJ
Write-Log "Krok 5: GRAJ..."
[InputHelper]::ClickAt($menuPositions["GRAJ"].X, $menuPositions["GRAJ"].Y)
Start-Sleep -Seconds 2

Write-Log ""
Write-Log "=== Nawigacja zakonczona ===" "Cyan"
Write-Log "Mapa powinna byc zaladowana. Sprawdz ekran gry."

# Zwroc wynik
return @{
    Success = $true
    MapName = $MapName
    MapPath = $mapPath
    LogFile = $logFile
}
