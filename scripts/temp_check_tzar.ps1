# Sprawdz instalacje Tzar na VM DEV
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "=== Sprawdzanie instalacji Tzar ===" -ForegroundColor Cyan
    Write-Host "Hostname: $(hostname)"
    Write-Host ""

    # Szukaj Tzar w standardowych lokalizacjach
    $searchPaths = @(
        "C:\Program Files\Tzared",
        "C:\Program Files (x86)\Tzared",
        "C:\Program Files\Tzar",
        "C:\Program Files (x86)\Tzar",
        "C:\Games\Tzar",
        "C:\Games\Tzared",
        "D:\Games\Tzar",
        "D:\Games\Tzared"
    )

    Write-Host "Szukam w standardowych lokalizacjach..."
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            Write-Host "ZNALEZIONO: $path" -ForegroundColor Green
            Get-ChildItem $path -File | Where-Object { $_.Extension -eq ".exe" } | ForEach-Object {
                Write-Host "  - $($_.Name)"
            }
        }
    }

    Write-Host ""
    Write-Host "Szukam Tzared.exe i Tzar.exe na dysku C:..."
    $found = Get-ChildItem -Path "C:\" -Recurse -Filter "Tzared.exe" -ErrorAction SilentlyContinue | Select-Object -First 3
    if ($found) {
        Write-Host "Znaleziono Tzared.exe:" -ForegroundColor Green
        $found | ForEach-Object { Write-Host "  $($_.FullName)" }
    }

    $found2 = Get-ChildItem -Path "C:\" -Recurse -Filter "Tzar.exe" -ErrorAction SilentlyContinue | Select-Object -First 3
    if ($found2) {
        Write-Host "Znaleziono Tzar.exe:" -ForegroundColor Green
        $found2 | ForEach-Object { Write-Host "  $($_.FullName)" }
    }

    Write-Host ""
    Write-Host "=== Katalogi TzarBot ===" -ForegroundColor Cyan
    if (Test-Path "C:\TzarBot") {
        Write-Host "C:\TzarBot istnieje:"
        Get-ChildItem "C:\TzarBot" | ForEach-Object { Write-Host "  $($_.Name)" }
    } else {
        Write-Host "C:\TzarBot nie istnieje"
    }

    Write-Host ""
    Write-Host "=== Zakonczono ===" -ForegroundColor Cyan
}
