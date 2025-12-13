# Install Visual C++ Redistributable on VM DEV
# Downloads and installs VC++ 2015-2022 x64

$VMName = "DEV"
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "=== Installing VC++ Redistributable on VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    $downloadUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe"
    $installerPath = "C:\TzarBot\vc_redist.x64.exe"

    Write-Host "Downloading VC++ Redistributable..."
    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
        Write-Host "Download complete: $installerPath"
    }
    catch {
        Write-Host "Download failed: $_" -ForegroundColor Red
        return
    }

    Write-Host ""
    Write-Host "Installing VC++ Redistributable (silent)..."
    $process = Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru

    if ($process.ExitCode -eq 0) {
        Write-Host "Installation successful!" -ForegroundColor Green
    }
    elseif ($process.ExitCode -eq 1638) {
        Write-Host "Already installed (exit code 1638)" -ForegroundColor Yellow
    }
    else {
        Write-Host "Installation finished with exit code: $($process.ExitCode)" -ForegroundColor Yellow
    }

    # Cleanup
    Remove-Item $installerPath -ErrorAction SilentlyContinue

    # Verify installation
    Write-Host ""
    Write-Host "Verifying installation..."
    $vcpp = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" -ErrorAction SilentlyContinue
    if ($vcpp) {
        Write-Host "VC++ Runtime installed: Version $($vcpp.Version)" -ForegroundColor Green
    }
    else {
        Write-Host "Warning: Could not verify VC++ installation" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
