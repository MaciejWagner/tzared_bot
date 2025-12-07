#
# Run Demo on VM in Interactive Session
# This script deploys and runs demo scripts on VM DEV with screenshot capture
#

param(
    [string]$VMName = "DEV",
    [string]$VMUsername = "test",
    [string]$VMPassword = "password123"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$localEvidence = "C:\Users\maciek\ai_experiments\tzar_bot\project_management\demo"

Write-Host @"

===============================================================================
            TzarBot Demo Runner - Interactive Session
===============================================================================
VM: $VMName
User: $VMUsername
Timestamp: $timestamp

This script will:
1. Deploy demo scripts to VM
2. Create scheduled task to run in interactive session
3. Wait for completion
4. Copy results (screenshots + logs) to local evidence folder
===============================================================================

"@ -ForegroundColor Cyan

# Create credential
$secPassword = ConvertTo-SecureString $VMPassword -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($VMUsername, $secPassword)

Write-Host "[1/5] Testing VM connection..." -ForegroundColor Yellow
try {
    $testResult = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        return "Connected to $env:COMPUTERNAME"
    }
    Write-Host "  OK: $testResult" -ForegroundColor Green
} catch {
    Write-Host "  FAIL: Cannot connect to VM. Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host "[2/5] Deploying demo runner script to VM..." -ForegroundColor Yellow

# Combined demo script that runs both Phase 0 and Phase 1 with screenshots
$demoScript = @'
param(
    [string]$OutputDir = "C:\TzarBot-Demo-Evidence"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

# Create output directory
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$logFile = Join-Path $OutputDir "demo_$timestamp.log"

function Log {
    param($msg)
    $logMsg = "$(Get-Date -Format 'HH:mm:ss') - $msg"
    Write-Host $logMsg
    Add-Content -Path $logFile -Value $logMsg
}

function Take-Screenshot {
    param([string]$Name)

    $screenshotPath = Join-Path $OutputDir "$Name.png"
    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing

        $screen = [System.Windows.Forms.Screen]::PrimaryScreen
        $bitmap = New-Object System.Drawing.Bitmap($screen.Bounds.Width, $screen.Bounds.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($screen.Bounds.Location, [System.Drawing.Point]::Empty, $screen.Bounds.Size)
        $bitmap.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Log "Screenshot saved: $screenshotPath"
        return $true
    } catch {
        Log "Screenshot FAILED: $_"
        return $false
    }
}

Log "=== TzarBot Demo Started ==="
Log "Output directory: $OutputDir"

# Screenshot 1: Desktop
Log "Screenshot 1: Desktop"
Start-Sleep -Seconds 2
Take-Screenshot -Name "01_desktop"

# Screenshot 2: System Info
Log "Screenshot 2: System Info via PowerShell"
$sysInfo = @"
Computer: $env:COMPUTERNAME
OS: $(Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty Caption)
RAM: $([math]::Round((Get-WmiObject Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)) GB
CPU: $(Get-WmiObject Win32_Processor | Select-Object -ExpandProperty Name)
.NET: $(dotnet --version 2>$null)
"@
$sysInfo | Out-File (Join-Path $OutputDir "system_info.txt")
Log $sysInfo

# Screenshot 3: .NET version in PowerShell window
Log "Screenshot 3: .NET SDK Check"
$psProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cls; Write-Host '=== TzarBot Demo - .NET SDK ===' -ForegroundColor Cyan; dotnet --version; dotnet --list-sdks; Write-Host ''; Write-Host 'Press any key to continue...' -ForegroundColor Yellow" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 3
Take-Screenshot -Name "02_dotnet_version"
Stop-Process -Id $psProcess.Id -Force -ErrorAction SilentlyContinue

# Screenshot 4: Build
Log "Screenshot 4: Build TzarBot"
cd C:\TzarBot
$buildOutput = dotnet build TzarBot.sln --configuration Release 2>&1
$buildOutput | Out-File (Join-Path $OutputDir "build.log")
$buildProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cls; Write-Host '=== TzarBot Build Output ===' -ForegroundColor Cyan; type $(Join-Path $OutputDir 'build.log') | Select-Object -Last 30; Write-Host ''; Write-Host 'Build complete!' -ForegroundColor Green" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 3
Take-Screenshot -Name "03_build_output"
Stop-Process -Id $buildProcess.Id -Force -ErrorAction SilentlyContinue

# Screenshot 5: Tests
Log "Screenshot 5: Run Tests"
$testOutput = dotnet test TzarBot.sln --configuration Release --verbosity normal 2>&1
$testOutput | Out-File (Join-Path $OutputDir "tests.log")
$testProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cls; Write-Host '=== TzarBot Test Results ===' -ForegroundColor Cyan; type $(Join-Path $OutputDir 'tests.log') | Select-Object -Last 50; Write-Host ''; Write-Host 'Tests complete!' -ForegroundColor Green" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 3
Take-Screenshot -Name "04_test_results"
Stop-Process -Id $testProcess.Id -Force -ErrorAction SilentlyContinue

# Screenshot 6: Tzar Game folder
Log "Screenshot 6: Tzar Game Installation"
if (Test-Path "C:\Program Files\Tzared") {
    Start-Process explorer "C:\Program Files\Tzared"
    Start-Sleep -Seconds 3
    Take-Screenshot -Name "05_tzar_game"
    # Close explorer window
    Get-Process explorer -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "*Tzared*" } | Stop-Process -Force -ErrorAction SilentlyContinue
} else {
    Log "Tzar not found at C:\Program Files\Tzared"
}

# Screenshot 7: Network check
Log "Screenshot 7: Network Configuration"
$netProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cls; Write-Host '=== Network Configuration ===' -ForegroundColor Cyan; ipconfig | Select-String -Pattern 'IPv4|Subnet|Gateway'; Write-Host ''; ping 192.168.100.1 -n 2; Write-Host ''; Write-Host 'Network OK!' -ForegroundColor Green" -PassThru -WindowStyle Maximized
Start-Sleep -Seconds 5
Take-Screenshot -Name "06_network"
Stop-Process -Id $netProcess.Id -Force -ErrorAction SilentlyContinue

Log "=== Demo Complete ==="

# Create completion marker
"COMPLETE" | Out-File (Join-Path $OutputDir "COMPLETE.flag")

# Summary
$screenshots = Get-ChildItem $OutputDir -Filter "*.png" | Select-Object -ExpandProperty Name
Log "Screenshots captured: $($screenshots.Count)"
$screenshots | ForEach-Object { Log "  - $_" }
'@

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($script)
    $scriptPath = "C:\TzarBot\RunDemo.ps1"
    Set-Content -Path $scriptPath -Value $script -Encoding UTF8
    Write-Host "Demo script deployed to $scriptPath"
} -ArgumentList $demoScript

Write-Host "  OK: Demo script deployed" -ForegroundColor Green

Write-Host "[3/5] Creating and starting scheduled task..." -ForegroundColor Yellow

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    param($username)

    $taskName = "TzarBot-Demo"

    # Remove existing task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Create task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -WindowStyle Normal -File C:\TzarBot\RunDemo.ps1"
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddSeconds(5)
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    $principal = New-ScheduledTaskPrincipal -UserId $username -LogonType Interactive -RunLevel Highest

    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force | Out-Null

    # Start immediately
    Start-ScheduledTask -TaskName $taskName

    Write-Host "Scheduled task '$taskName' created and started"
} -ArgumentList $VMUsername

Write-Host "  OK: Task started" -ForegroundColor Green

Write-Host "[4/5] Waiting for demo to complete (up to 5 minutes)..." -ForegroundColor Yellow
Write-Host "      (Build + Tests can take 2-3 minutes on VM)" -ForegroundColor Gray

$maxWait = 300
$waited = 0
$complete = $false

while (-not $complete -and $waited -lt $maxWait) {
    Start-Sleep -Seconds 10
    $waited += 10

    $checkResult = Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        Test-Path "C:\TzarBot-Demo-Evidence\COMPLETE.flag"
    }

    if ($checkResult) {
        $complete = $true
        Write-Host "  Demo completed!" -ForegroundColor Green
    } else {
        Write-Host "  Waiting... ($waited/$maxWait seconds)" -ForegroundColor Gray
    }
}

if (-not $complete) {
    Write-Host "  Timeout - demo may still be running" -ForegroundColor Yellow
}

Write-Host "[5/5] Copying evidence to local..." -ForegroundColor Yellow

# Create PSSession for file copy
$session = New-PSSession -VMName $VMName -Credential $cred

# Create evidence directories
$phase0Evidence = Join-Path $localEvidence "phase_0_evidence"
$phase1Evidence = Join-Path $localEvidence "phase_1_evidence"
New-Item -ItemType Directory -Path $phase0Evidence -Force | Out-Null
New-Item -ItemType Directory -Path $phase1Evidence -Force | Out-Null

# Copy all files
Copy-Item -FromSession $session -Path "C:\TzarBot-Demo-Evidence\*" -Destination $phase1Evidence -Force -Recurse

Remove-PSSession $session

# List copied files
Write-Host "`nEvidence copied:" -ForegroundColor Green
Get-ChildItem $phase1Evidence | ForEach-Object {
    Write-Host "  - $($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)"
}

Write-Host @"

===============================================================================
                         DEMO COMPLETE
===============================================================================
Evidence saved to: $phase1Evidence

Screenshots:
"@ -ForegroundColor Green

Get-ChildItem $phase1Evidence -Filter "*.png" | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Cyan }

Write-Host @"

Logs:
"@ -ForegroundColor Green
Get-ChildItem $phase1Evidence -Filter "*.log" | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Cyan }

Write-Host "===============================================================================" -ForegroundColor Green
