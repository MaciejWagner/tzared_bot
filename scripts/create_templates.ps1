# create_templates.ps1
# Tworzy template'y dla detektora Victory/Defeat ze screenshotow
# Uzywa System.Drawing do wycinania regionow

Add-Type -AssemblyName System.Drawing

$sourceDir = "C:\Users\maciek\ai_experiments\tzar_bot\game_screenshots"
$outputDir = "C:\Users\maciek\ai_experiments\tzar_bot\src\TzarBot.StateDetection\Templates"

# Utworz katalog jesli nie istnieje
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host "=== Tworzenie templateow ===" -ForegroundColor Cyan

# Funkcja do wycinania regionu z obrazu
function Extract-Region {
    param(
        [string]$SourcePath,
        [string]$OutputPath,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [string]$Description
    )

    if (-not (Test-Path $SourcePath)) {
        Write-Host "  BLAD: Nie znaleziono: $SourcePath" -ForegroundColor Red
        return $false
    }

    try {
        $source = [System.Drawing.Image]::FromFile($SourcePath)

        # Skaluj wspolrzedne do rozmiaru obrazu zrodlowego
        $scaleX = $source.Width / 1920.0
        $scaleY = $source.Height / 1080.0

        $scaledX = [int]($X * $scaleX)
        $scaledY = [int]($Y * $scaleY)
        $scaledW = [int]($Width * $scaleX)
        $scaledH = [int]($Height * $scaleY)

        # Upewnij sie ze region miesci sie w obrazie
        if ($scaledX + $scaledW -gt $source.Width) { $scaledW = $source.Width - $scaledX }
        if ($scaledY + $scaledH -gt $source.Height) { $scaledH = $source.Height - $scaledY }

        $rect = New-Object System.Drawing.Rectangle($scaledX, $scaledY, $scaledW, $scaledH)
        $bitmap = New-Object System.Drawing.Bitmap($scaledW, $scaledH)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

        $graphics.DrawImage($source, 0, 0, $rect, [System.Drawing.GraphicsUnit]::Pixel)

        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

        $graphics.Dispose()
        $bitmap.Dispose()
        $source.Dispose()

        Write-Host "  OK: $Description -> $OutputPath (${scaledW}x${scaledH})" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  BLAD: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Regiony dla templateow (bazowane na 1920x1080)
# Victory/Defeat - srodek ekranu gdzie pojawia sie modal

# Victory template - wycinek tekstu "YOU ARE VICTORIOUS"
Write-Host ""
Write-Host "Victory template:" -ForegroundColor Yellow
Extract-Region -SourcePath "$sourceDir\won_game.PNG" `
               -OutputPath "$outputDir\victory_template.png" `
               -X 500 -Y 300 -Width 920 -Height 300 `
               -Description "Victory banner"

# Defeat template - wycinek tekstu porazki
Write-Host ""
Write-Host "Defeat template:" -ForegroundColor Yellow
Extract-Region -SourcePath "$sourceDir\defeated_game.PNG" `
               -OutputPath "$outputDir\defeat_template.png" `
               -X 500 -Y 300 -Width 920 -Height 300 `
               -Description "Defeat banner"

# Sprawdz co zostalo utworzone
Write-Host ""
Write-Host "=== Utworzone templaty ===" -ForegroundColor Cyan
Get-ChildItem $outputDir\*.png | ForEach-Object {
    $size = [math]::Round($_.Length / 1KB, 2)
    Write-Host "  $($_.Name) - $size KB"
}

Write-Host ""
Write-Host "Templaty gotowe w: $outputDir" -ForegroundColor Green
