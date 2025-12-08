# Diagnose GUI session issues on VM DEV
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "=== Diagnozowanie sesji GUI na VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "`n=== 1. Zalogowani użytkownicy ===" -ForegroundColor Yellow
    query user 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Brak zalogowanych użytkowników lub query user niedostępne"
    }

    Write-Host "`n=== 2. Sesje logowania ===" -ForegroundColor Yellow
    Get-WmiObject -Class Win32_LogonSession | Where-Object { $_.LogonType -in @(2, 10, 11) } |
        Select-Object LogonId, LogonType, @{N='StartTime';E={$_.ConvertToDateTime($_.StartTime)}}

    Write-Host "`n=== 3. Procesy Explorer.exe (GUI) ===" -ForegroundColor Yellow
    $explorer = Get-Process explorer -ErrorAction SilentlyContinue
    if ($explorer) {
        $explorer | Select-Object Id, SessionId, StartTime
    } else {
        Write-Host "Explorer.exe NIE DZIAŁA - brak aktywnej sesji GUI!"
    }

    Write-Host "`n=== 4. Usługa WinRM ===" -ForegroundColor Yellow
    Get-Service WinRM | Select-Object Status, StartType

    Write-Host "`n=== 5. Display Adapters (DXGI) ===" -ForegroundColor Yellow
    Get-WmiObject Win32_VideoController | Select-Object Name, Status, DriverVersion

    Write-Host "`n=== 6. Hyper-V Integration Services ===" -ForegroundColor Yellow
    Get-Service | Where-Object { $_.Name -like "vmic*" } | Select-Object Name, Status

    Write-Host "`n=== 7. Czy sesja jest interaktywna? ===" -ForegroundColor Yellow
    $sessionId = [System.Diagnostics.Process]::GetCurrentProcess().SessionId
    Write-Host "Current Session ID: $sessionId"

    # Check if we're in session 0 (non-interactive)
    if ($sessionId -eq 0) {
        Write-Host "PROBLEM: Działamy w Session 0 (nieinteraktywna)" -ForegroundColor Red
    } else {
        Write-Host "Działamy w Session $sessionId" -ForegroundColor Green
    }

    Write-Host "`n=== 8. Auto-logon configuration ===" -ForegroundColor Yellow
    $winlogon = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    $autoLogon = Get-ItemProperty -Path $winlogon -Name "AutoAdminLogon" -ErrorAction SilentlyContinue
    $defaultUser = Get-ItemProperty -Path $winlogon -Name "DefaultUserName" -ErrorAction SilentlyContinue
    Write-Host "AutoAdminLogon: $($autoLogon.AutoAdminLogon)"
    Write-Host "DefaultUserName: $($defaultUser.DefaultUserName)"

    Write-Host "`n=== 9. Aktywne okna (EnumWindows test) ===" -ForegroundColor Yellow
    Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    public class WinApi {
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
"@

    $windowCount = 0
    $visibleCount = 0

    $callback = {
        param([IntPtr]$hwnd, [IntPtr]$lparam)
        $script:windowCount++
        if ([WinApi]::IsWindowVisible($hwnd)) {
            $script:visibleCount++
        }
        return $true
    }

    [WinApi]::EnumWindows($callback, [IntPtr]::Zero) | Out-Null

    Write-Host "Total windows: $windowCount"
    Write-Host "Visible windows: $visibleCount"

    Write-Host "`n=== 10. Desktop available? ===" -ForegroundColor Yellow
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $screens = [System.Windows.Forms.Screen]::AllScreens
        Write-Host "Screens found: $($screens.Count)"
        foreach ($screen in $screens) {
            Write-Host "  - $($screen.DeviceName): $($screen.Bounds.Width)x$($screen.Bounds.Height)"
        }
    } catch {
        Write-Host "ERROR: Cannot access display info - $($_.Exception.Message)" -ForegroundColor Red
    }
}
