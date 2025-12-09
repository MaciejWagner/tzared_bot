# Detailed DXGI test on VM
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Invoke-Command -VMName DEV -Credential $cred -ScriptBlock {
    Write-Host "=== Session Information ===" -ForegroundColor Cyan

    # Get current session info
    Write-Host "Current user: $env:USERNAME"
    Write-Host "Computer name: $env:COMPUTERNAME"

    # Check session
    $sessions = quser 2>&1
    Write-Host "`nActive sessions:"
    Write-Host $sessions

    # Check if console session
    Write-Host "`n=== Process Session ===" -ForegroundColor Cyan
    $proc = Get-Process -Id $PID
    Write-Host "Process SessionId: $($proc.SessionId)"

    # Check for Secure Desktop (UAC desktop)
    Write-Host "`n=== DXGI Desktop Access ===" -ForegroundColor Cyan

    # Check desktops
    $code = @"
using System;
using System.Runtime.InteropServices;

public class DesktopInfo {
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetThreadDesktop(uint dwThreadId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

    public static void Check() {
        var desktopWindow = GetDesktopWindow();
        Console.WriteLine("Desktop Window: " + desktopWindow);

        var threadDesktop = GetThreadDesktop(GetCurrentThreadId());
        Console.WriteLine("Thread Desktop: " + threadDesktop);

        var inputDesktop = OpenInputDesktop(0, false, 0x00020000);
        Console.WriteLine("Input Desktop: " + inputDesktop + " (0 = access denied)");
    }
}
"@

    Add-Type -TypeDefinition $code
    [DesktopInfo]::Check()

    # Check if running in Session 0
    Write-Host "`n=== Is Session Interactive? ===" -ForegroundColor Cyan
    $session = [System.Environment]::UserInteractive
    Write-Host "UserInteractive: $session"
}
