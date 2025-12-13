# Kopiuj dowod dzialania FFmpeg z VM DEV na host
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

$destDir = "C:\Users\maciek\ai_experiments\tzar_bot\demo_results\FFmpegTest"
New-Item -ItemType Directory -Path $destDir -Force | Out-Null

Write-Host "Kopiowanie plikow z VM DEV..." -ForegroundColor Cyan

# Pobierz pliki z VM
$session = New-PSSession -VMName "DEV" -Credential $cred

# Kopiuj nagranie testowe
Copy-Item -FromSession $session -Path "C:\TzarBot\Recordings\test_interactive.mp4" -Destination "$destDir\test_interactive.mp4" -ErrorAction SilentlyContinue

# Kopiuj log
Copy-Item -FromSession $session -Path "C:\TzarBot\Recordings\ffmpeg_log.txt" -Destination "$destDir\ffmpeg_log.txt" -ErrorAction SilentlyContinue

Remove-PSSession $session

# Sprawdz co skopiowano
Write-Host ""
Write-Host "Skopiowane pliki:" -ForegroundColor Green
Get-ChildItem $destDir | ForEach-Object {
    $size = if ($_.Length -gt 1KB) { "$([math]::Round($_.Length/1KB, 2)) KB" } else { "$($_.Length) B" }
    Write-Host "  $($_.Name) - $size"
}

Write-Host ""
Write-Host "Pliki dostepne w: $destDir" -ForegroundColor Cyan
