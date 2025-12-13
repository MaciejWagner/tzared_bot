Import-Module Microsoft.PowerShell.Security -ErrorAction SilentlyContinue
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== Test FFmpeg na VM ===" -ForegroundColor Cyan

    # Odswiez PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

    # Test wersji
    Write-Host "Wersja FFmpeg:"
    ffmpeg -version 2>&1 | Select-Object -First 3

    # Sprawdz czy gdigrab dziala (screen capture)
    Write-Host ""
    Write-Host "Dostepne urzadzenia wejsciowe:"
    ffmpeg -list_devices true -f dshow -i dummy 2>&1 | Select-String "DirectShow"

    Write-Host ""
    Write-Host "Test krotki - nagranie 3 sekundy:"
    $testOutput = "C:\TzarBot\Recordings\test_3sec.mp4"
    New-Item -ItemType Directory -Path (Split-Path $testOutput) -Force | Out-Null

    # Krotkie nagranie testowe
    $result = & ffmpeg -y -f gdigrab -framerate 10 -t 3 -i desktop -c:v libx264 -preset ultrafast $testOutput 2>&1

    if (Test-Path $testOutput) {
        $size = (Get-Item $testOutput).Length / 1KB
        Write-Host "OK! Nagranie testowe: $testOutput ($size KB)" -ForegroundColor Green
    } else {
        Write-Host "BLAD: Nagranie nie zostalo utworzone" -ForegroundColor Red
        $result | Select-Object -Last 10
    }

    Write-Host ""
    Write-Host "=== Test zakonczony ===" -ForegroundColor Cyan
}
