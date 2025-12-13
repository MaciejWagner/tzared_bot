# take_screenshot_on_vm.ps1
# Robi screenshot ekranu na VM DEV
# Pomaga zrozumiec uklad menu gry

param(
    [string]$OutputName = "screenshot"
)

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "=== Screenshot z VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    param($outputName)

    $taskName = "TzarBot_Screenshot"
    $screenshotDir = "C:\TzarBot\Screenshots"
    New-Item -ItemType Directory -Path $screenshotDir -Force | Out-Null

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $outputPath = "$screenshotDir\${outputName}_${timestamp}.png"

    # Skrypt do wykonania w sesji interaktywnej
    $scriptContent = @"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

`$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
`$bitmap = New-Object System.Drawing.Bitmap(`$bounds.Width, `$bounds.Height)
`$graphics = [System.Drawing.Graphics]::FromImage(`$bitmap)
`$graphics.CopyFromScreen(`$bounds.Location, [System.Drawing.Point]::Empty, `$bounds.Size)

`$bitmap.Save("$outputPath", [System.Drawing.Imaging.ImageFormat]::Png)
`$graphics.Dispose()
`$bitmap.Dispose()

"Zapisano: $outputPath" | Out-File "C:\TzarBot\screenshot_result.txt"
"@

    $scriptPath = "C:\TzarBot\take_screenshot.ps1"
    $scriptContent | Out-File $scriptPath -Encoding UTF8

    # Usun stary task
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    # Utworz task
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -File $scriptPath"
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

    Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null

    Write-Host "Robie screenshot..."
    Start-ScheduledTask -TaskName $taskName

    # Czekaj
    Start-Sleep -Seconds 3

    # Sprawdz wynik
    if (Test-Path $outputPath) {
        $size = (Get-Item $outputPath).Length / 1KB
        Write-Host "SUKCES! Screenshot: $outputPath ($([math]::Round($size, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Host "BLAD: Screenshot nie zostal utworzony" -ForegroundColor Red
        if (Test-Path "C:\TzarBot\screenshot_result.txt") {
            Get-Content "C:\TzarBot\screenshot_result.txt"
        }
    }

    # Cleanup
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    return $outputPath

} -ArgumentList $OutputName

Write-Host "=== Zakonczono ===" -ForegroundColor Cyan
