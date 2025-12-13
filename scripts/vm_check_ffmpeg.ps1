# Check FFmpeg on VM DEV
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== Checking FFmpeg ==="
    $ffmpegPath = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($ffmpegPath) {
        Write-Host "FFmpeg found at: $($ffmpegPath.Source)"
        ffmpeg -version 2>&1 | Select-Object -First 2
    } else {
        Write-Host "FFmpeg NOT installed"
        Write-Host "Checking winget..."
        winget --version 2>&1
    }
}
