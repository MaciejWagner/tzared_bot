# screenshot_tzar_on_vm.ps1
# Aktywuje okno Tzar i robi screenshot

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "=== Screenshot Tzar na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    $taskName = "TzarBot_ScreenshotTzar"
    $screenshotDir = "C:\TzarBot\Screenshots"
    New-Item -ItemType Directory -Path $screenshotDir -Force | Out-Null

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $outputPath = "$screenshotDir\tzar_game_${timestamp}.png"

    # Skrypt do wykonania w sesji interaktywnej
    $scriptContent = @"
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
}
"@

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

`$logFile = "C:\TzarBot\screenshot_tzar_log.txt"
"Szukam okna Tzar..." | Out-File `$logFile

# Znajdz proces Tzar
`$tzarProc = Get-Process -Name "Tzared" -ErrorAction SilentlyContinue | Select-Object -First 1
if (`$tzarProc -and `$tzarProc.MainWindowHandle -ne 0) {
    "Znaleziono Tzar PID: `$(`$tzarProc.Id), Handle: `$(`$tzarProc.MainWindowHandle)" | Out-File `$logFile -Append

    # Aktywuj okno
    [Win32]::ShowWindow(`$tzarProc.MainWindowHandle, 9)  # SW_RESTORE
    Start-Sleep -Milliseconds 200
    [Win32]::SetForegroundWindow(`$tzarProc.MainWindowHandle)
    Start-Sleep -Milliseconds 500

    "Okno aktywowane, robie screenshot..." | Out-File `$logFile -Append
} else {
    "Nie znaleziono okna Tzar!" | Out-File `$logFile -Append
}

# Zrob screenshot
Start-Sleep -Milliseconds 300
`$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
`$bitmap = New-Object System.Drawing.Bitmap(`$bounds.Width, `$bounds.Height)
`$graphics = [System.Drawing.Graphics]::FromImage(`$bitmap)
`$graphics.CopyFromScreen(`$bounds.Location, [System.Drawing.Point]::Empty, `$bounds.Size)

`$bitmap.Save("$outputPath", [System.Drawing.Imaging.ImageFormat]::Png)
`$graphics.Dispose()
`$bitmap.Dispose()

if (Test-Path "$outputPath") {
    "Zapisano: $outputPath" | Out-File `$logFile -Append
} else {
    "BLAD: Nie zapisano screenshota" | Out-File `$logFile -Append
}
"@

    $scriptPath = "C:\TzarBot\screenshot_tzar.ps1"
    $scriptContent | Out-File $scriptPath -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $scriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Aktywuje okno Tzar i robie screenshot..."
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    Start-Sleep -Seconds 5

    # Sprawdz log
    if (Test-Path "C:\TzarBot\screenshot_tzar_log.txt") {
        Write-Host "Log:"
        Get-Content "C:\TzarBot\screenshot_tzar_log.txt"
    }

    # Sprawdz wynik
    if (Test-Path $outputPath) {
        $size = (Get-Item $outputPath).Length / 1KB
        Write-Host ""
        Write-Host "SUKCES! Screenshot: $outputPath ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Host "BLAD: Screenshot nie zostal utworzony" -ForegroundColor Red
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host "=== Zakonczono ===" -ForegroundColor Cyan
