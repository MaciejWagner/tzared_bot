# copy_screenshots_from_vm.ps1
# Kopiuje screenshoty z VM DEV na hosta

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

$destDir = "C:\Users\maciek\ai_experiments\tzar_bot\demo_results\Screenshots"
New-Item -ItemType Directory -Path $destDir -Force | Out-Null

Write-Host "Kopiowanie screenshotow z VM DEV..." -ForegroundColor Cyan

$session = New-PSSession -VMName "DEV" -Credential $cred

# Lista plikow na VM
$vmFiles = Invoke-Command -Session $session -ScriptBlock {
    Get-ChildItem "C:\TzarBot\Screenshots\*.png" | Select-Object Name, FullName, Length
}

if ($vmFiles) {
    foreach ($file in $vmFiles) {
        Write-Host "Kopiowanie: $($file.Name)"
        Copy-Item -FromSession $session -Path $file.FullName -Destination "$destDir\$($file.Name)"
    }
    Write-Host ""
    Write-Host "Skopiowano $($vmFiles.Count) plikow do: $destDir" -ForegroundColor Green
} else {
    Write-Host "Brak screenshotow do skopiowania" -ForegroundColor Yellow
}

Remove-PSSession $session

# Pokaz skopiowane
Write-Host ""
Write-Host "Pliki:" -ForegroundColor Cyan
Get-ChildItem $destDir\*.png | ForEach-Object {
    $size = [math]::Round($_.Length/1KB, 2)
    Write-Host "  $($_.Name) - $size KB"
}
