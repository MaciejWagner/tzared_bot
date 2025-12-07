#
# Capture Screenshots from VM using Enhanced Session Mode
# This script must be run while Enhanced Session or RDP is active to the VM
#
# Usage:
# 1. Connect to VM using vmconnect (Enhanced Session Mode)
# 2. Run this script from host - it will trigger screenshots via scheduled task
# 3. Screenshots will be collected to local directory
#

param(
    [string]$VMName = "DEV",
    [string]$VMUsername = "test",
    [string]$VMPassword = "password123",
    [string]$LocalOutputDir = "C:\Users\maciek\ai_experiments\tzar_bot\project_management\demo"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host @"

===============================================================================
              VM Screenshot Capture Script
===============================================================================
This script captures screenshots from the VM for demo documentation.

PREREQUISITE: You must have an Enhanced Session or RDP connection
              active to the VM for screenshots to work!

To connect: vmconnect localhost $VMName

===============================================================================

"@ -ForegroundColor Cyan

# Create credential
$secPassword = ConvertTo-SecureString $VMPassword -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($VMUsername, $secPassword)

# Create screenshot script to deploy on VM
$screenshotScript = @'

param(
    [string]$OutputDir = "C:\TzarBot-Screenshots",
    [string]$Prefix = "screenshot"
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Take-Screenshot {
    param([string]$Name)

    $timestamp = Get-Date -Format "HH-mm-ss"
    $filename = Join-Path $OutputDir "$($Name)_$timestamp.png"

    try {
        $screen = [System.Windows.Forms.Screen]::PrimaryScreen
        $bitmap = New-Object System.Drawing.Bitmap($screen.Bounds.Width, $screen.Bounds.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($screen.Bounds.Location, [System.Drawing.Point]::Empty, $screen.Bounds.Size)
        $bitmap.Save($filename, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Write-Host "Screenshot saved: $filename"
        return $filename
    } catch {
        Write-Host "Screenshot failed: $_" -ForegroundColor Red
        return $null
    }
}

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$screenshots = @()

# Screenshot 1: Desktop/System Info
Write-Host "`n=== Screenshot 1: System Info ===" -ForegroundColor Yellow
Start-Process "msinfo32.exe" -Wait -WindowStyle Maximized
Start-Sleep -Seconds 2
$screenshots += Take-Screenshot -Name "01_system_info"
Get-Process msinfo32 -ErrorAction SilentlyContinue | Stop-Process -Force

# Screenshot 2: PowerShell with dotnet version
Write-Host "`n=== Screenshot 2: .NET Version ===" -ForegroundColor Yellow
$psWindow = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Write-Host 'TzarBot Demo - .NET Version Check' -ForegroundColor Cyan; dotnet --version; dotnet --list-sdks; dotnet --list-runtimes; Read-Host 'Press Enter to close'" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 3
$screenshots += Take-Screenshot -Name "02_dotnet_version"
$psWindow | Stop-Process -Force -ErrorAction SilentlyContinue

# Screenshot 3: Project Build
Write-Host "`n=== Screenshot 3: Build Output ===" -ForegroundColor Yellow
$buildWindow = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\TzarBot; Write-Host 'TzarBot Build Output' -ForegroundColor Cyan; dotnet build TzarBot.sln --configuration Release 2>&1; Read-Host 'Press Enter'" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 120  # Build takes ~100 seconds on VM
$screenshots += Take-Screenshot -Name "03_build_output"
$buildWindow | Stop-Process -Force -ErrorAction SilentlyContinue

# Screenshot 4: Test Results
Write-Host "`n=== Screenshot 4: Test Results ===" -ForegroundColor Yellow
$testWindow = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\TzarBot; Write-Host 'TzarBot Test Results' -ForegroundColor Cyan; dotnet test TzarBot.sln --configuration Release --verbosity normal 2>&1; Read-Host 'Press Enter'" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 30  # Tests take ~20 seconds
$screenshots += Take-Screenshot -Name "04_test_results"
$testWindow | Stop-Process -Force -ErrorAction SilentlyContinue

# Screenshot 5: Tzar Game installed
Write-Host "`n=== Screenshot 5: Tzar Game Installation ===" -ForegroundColor Yellow
$explorerWindow = Start-Process explorer -ArgumentList "C:\Program Files\Tzared" -PassThru
Start-Sleep -Seconds 3
$screenshots += Take-Screenshot -Name "05_tzar_installation"
Get-Process explorer -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "*Tzared*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Summary
Write-Host "`n=== Screenshot Capture Complete ===" -ForegroundColor Green
Write-Host "Screenshots saved to: $OutputDir"
$screenshots | Where-Object { $_ } | ForEach-Object { Write-Host "  - $_" }

# Create marker file
"SCREENSHOTS_COMPLETE" | Out-File "$OutputDir\COMPLETE.flag"

return $screenshots
'@

# Deploy screenshot script to VM
Write-Host "[1/4] Deploying screenshot script to VM..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($script)
    $scriptPath = "C:\TzarBot\scripts\demo\TakeScreenshots.ps1"
    New-Item -ItemType Directory -Path "C:\TzarBot\scripts\demo" -Force | Out-Null
    Set-Content -Path $scriptPath -Value $script -Encoding UTF8
    Write-Host "Screenshot script deployed to $scriptPath"
} -ArgumentList $screenshotScript

# Create a scheduled task to run screenshots in interactive session
Write-Host "[2/4] Creating scheduled task for screenshot capture..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($username)

    $taskName = "TzarBot-Screenshots"
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -WindowStyle Normal -File C:\TzarBot\scripts\demo\TakeScreenshots.ps1"
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1)
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    $principal = New-ScheduledTaskPrincipal -UserId $username -LogonType Interactive -RunLevel Highest

    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force

    # Start the task immediately
    Start-ScheduledTask -TaskName $taskName

    Write-Host "Scheduled task created and started"
} -ArgumentList $VMUsername

Write-Host @"

[3/4] Waiting for screenshots to be captured...
      This will take approximately 3-4 minutes.

      IMPORTANT: Ensure you have Enhanced Session or RDP connected to VM!
      If not connected, run: vmconnect localhost $VMName

"@ -ForegroundColor Yellow

# Wait for completion
$maxWait = 300  # 5 minutes
$waited = 0
$complete = $false

while (-not $complete -and $waited -lt $maxWait) {
    Start-Sleep -Seconds 10
    $waited += 10

    $checkResult = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        Test-Path "C:\TzarBot-Screenshots\COMPLETE.flag"
    }

    if ($checkResult) {
        $complete = $true
        Write-Host "Screenshots captured!" -ForegroundColor Green
    } else {
        Write-Host "Waiting... ($waited/$maxWait seconds)" -ForegroundColor Gray
    }
}

if (-not $complete) {
    Write-Host "Timeout waiting for screenshots. They may still be running on VM." -ForegroundColor Red
}

# Copy screenshots to local
Write-Host "[4/4] Copying screenshots to local..." -ForegroundColor Yellow

$session = New-PSSession -VMName $VMName -Credential $cred

# Create evidence directories
$phase0Evidence = Join-Path $LocalOutputDir "phase_0_evidence"
$phase1Evidence = Join-Path $LocalOutputDir "phase_1_evidence"
New-Item -ItemType Directory -Path $phase0Evidence -Force | Out-Null
New-Item -ItemType Directory -Path $phase1Evidence -Force | Out-Null

# Copy screenshots
Copy-Item -FromSession $session -Path "C:\TzarBot-Screenshots\*.png" -Destination $phase1Evidence -Force

Remove-PSSession $session

Write-Host @"

===============================================================================
                         Screenshot Capture Complete
===============================================================================
Screenshots saved to: $phase1Evidence

Check the following files:
- 01_system_info_*.png
- 02_dotnet_version_*.png
- 03_build_output_*.png
- 04_test_results_*.png
- 05_tzar_installation_*.png
===============================================================================

"@ -ForegroundColor Green
