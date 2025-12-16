# Analyze screenshot colors for Victory/Defeat detection calibration
# Uses SkiaSharp to analyze the same way as TrainingRunner

param(
    [Parameter(Mandatory=$true)]
    [string]$ImagePath
)

Add-Type -Path "C:\Users\maciek\ai_experiments\tzar_bot\publish\TrainingRunner\SkiaSharp.dll"

$bytes = [System.IO.File]::ReadAllBytes($ImagePath)
$bitmap = [SkiaSharp.SKBitmap]::Decode($bytes)

$width = $bitmap.Width
$height = $bitmap.Height

Write-Host "Image: $ImagePath"
Write-Host "Size: ${width}x${height}"

# Analyze center region (same as TrainingRunner)
$startX = [int]($width * 0.25)
$endX = [int]($width * 0.75)
$startY = [int]($height * 0.25)
$endY = [int]($height * 0.75)

$redCount = 0
$goldCount = 0
$darkCount = 0
$brownCount = 0
$totalPixels = 0

for ($y = $startY; $y -lt $endY; $y += 3) {
    for ($x = $startX; $x -lt $endX; $x += 3) {
        $pixel = $bitmap.GetPixel($x, $y)
        $totalPixels++

        # Red tones
        if ($pixel.Red -gt 120 -and $pixel.Red -gt ($pixel.Green + 30) -and $pixel.Red -gt ($pixel.Blue + 30)) {
            $redCount++
        }
        # Gold/yellow
        elseif ($pixel.Red -gt 150 -and $pixel.Green -gt 120 -and $pixel.Blue -lt 100) {
            $goldCount++
        }
        # Dark
        elseif ($pixel.Red -lt 80 -and $pixel.Green -lt 80 -and $pixel.Blue -lt 80) {
            $darkCount++
        }
        # Brown
        elseif ($pixel.Red -gt 80 -and $pixel.Red -lt 150 -and
                $pixel.Green -gt 50 -and $pixel.Green -lt 120 -and
                $pixel.Blue -gt 30 -and $pixel.Blue -lt 100 -and
                $pixel.Red -gt $pixel.Green -and $pixel.Green -gt $pixel.Blue) {
            $brownCount++
        }
    }
}

$redRatio = $redCount / $totalPixels
$goldRatio = $goldCount / $totalPixels
$darkRatio = $darkCount / $totalPixels
$brownRatio = $brownCount / $totalPixels

Write-Host ""
Write-Host "Color Analysis (center 50%):"
Write-Host "  Red:   $([math]::Round($redRatio * 100, 2))%"
Write-Host "  Gold:  $([math]::Round($goldRatio * 100, 2))%"
Write-Host "  Dark:  $([math]::Round($darkRatio * 100, 2))%"
Write-Host "  Brown: $([math]::Round($brownRatio * 100, 2))%"
Write-Host ""
Write-Host "Overlay (dark+brown): $([math]::Round(($darkRatio + $brownRatio) * 100, 2))%"

$bitmap.Dispose()
