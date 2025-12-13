# copy_map_to_vm.ps1
# Kopiuje mape treningowa na VM DEV

param(
    [string]$MapName = "training-0"
)

$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

$sourceDir = "C:\Users\maciek\ai_experiments\tzar_bot\training_maps"
$sourceFile = "$sourceDir\$MapName.tzared"

if (-not (Test-Path $sourceFile)) {
    Write-Host "BLAD: Mapa nie istnieje: $sourceFile" -ForegroundColor Red
    exit 1
}

Write-Host "Kopiowanie mapy na VM DEV..." -ForegroundColor Cyan
Write-Host "Zrodlo: $sourceFile"

$session = New-PSSession -VMName "DEV" -Credential $cred

# Utworz katalog na VM
Invoke-Command -Session $session -ScriptBlock {
    New-Item -ItemType Directory -Path "C:\TzarBot\Maps" -Force | Out-Null
}

# Skopiuj mape
Copy-Item -ToSession $session -Path $sourceFile -Destination "C:\TzarBot\Maps\$MapName.tzared"

# Sprawdz
$copied = Invoke-Command -Session $session -ScriptBlock {
    param($name)
    $path = "C:\TzarBot\Maps\$name.tzared"
    if (Test-Path $path) {
        $size = (Get-Item $path).Length / 1KB
        return "OK: $path ($([math]::Round($size, 2)) KB)"
    } else {
        return "BLAD: Nie skopiowano"
    }
} -ArgumentList $MapName

Write-Host $copied -ForegroundColor Green

Remove-PSSession $session
