# Test end-game detection on screenshot
param([string]$ImagePath)

Add-Type -Path "C:\Users\maciek\ai_experiments\tzar_bot\publish\TrainingRunner\SkiaSharp.dll"

$bytes = [System.IO.File]::ReadAllBytes($ImagePath)
$bitmap = [SkiaSharp.SKBitmap]::Decode($bytes)

$width = $bitmap.Width
$height = $bitmap.Height

Write-Host "Testing: $ImagePath" -ForegroundColor Cyan
Write-Host "Size: ${width}x${height}"

# Check if end-game dialog visible
$startX = [int]($width * 0.25)
$endX = [int]($width * 0.75)
$startY = [int]($height * 0.25)
$endY = [int]($height * 0.75)

$darkCount = 0
$redCount = 0
$totalPixels = 0

for ($y = $startY; $y -lt $endY; $y += 4) {
    for ($x = $startX; $x -lt $endX; $x += 4) {
        $pixel = $bitmap.GetPixel($x, $y)
        $totalPixels++
        if ($pixel.Red -lt 80 -and $pixel.Green -lt 80 -and $pixel.Blue -lt 80) {
            $darkCount++
        }
        elseif ($pixel.Red -gt 120 -and $pixel.Red -gt ($pixel.Green + 30) -and $pixel.Red -gt ($pixel.Blue + 30)) {
            $redCount++
        }
    }
}

$darkRatio = $darkCount / $totalPixels
$redRatio = $redCount / $totalPixels

Write-Host "Center region: dark=$([math]::Round($darkRatio * 100, 1))%, red=$([math]::Round($redRatio * 100, 1))%"

$isEndGame = $darkRatio -gt 0.55 -and $darkRatio -lt 0.85 -and $redRatio -gt 0.02
Write-Host "Is end-game: $isEndGame"

if ($isEndGame) {
    # Check for "F" letter region (y=60-65%, where only DEFEAT has gold)
    $fStartX = [int]($width * 0.40)
    $fEndX = [int]($width * 0.60)
    $fStartY = [int]($height * 0.60)
    $fEndY = [int]($height * 0.65)

    Write-Host "F region: x=$fStartX-$fEndX, y=$fStartY-$fEndY"

    $goldCountInF = 0
    $totalInF = 0

    for ($y = $fStartY; $y -lt $fEndY; $y += 2) {
        for ($x = $fStartX; $x -lt $fEndX; $x += 2) {
            $pixel = $bitmap.GetPixel($x, $y)
            $totalInF++
            if ($pixel.Red -gt 180 -and $pixel.Green -gt 150 -and $pixel.Blue -lt 120) {
                $goldCountInF++
            }
        }
    }

    $goldRatioInF = $goldCountInF / $totalInF
    Write-Host "Gold in F region: $([math]::Round($goldRatioInF * 100, 2))%"

    if ($goldRatioInF -gt 0.02) {
        Write-Host "RESULT: DEFEAT" -ForegroundColor Red
    } else {
        Write-Host "RESULT: VICTORY" -ForegroundColor Green
    }
}

$bitmap.Dispose()
