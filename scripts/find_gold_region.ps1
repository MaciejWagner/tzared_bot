# Find gold pixels in screenshot to locate the "F" letter
param([string]$ImagePath)

Add-Type -Path "C:\Users\maciek\ai_experiments\tzar_bot\publish\TrainingRunner\SkiaSharp.dll"

$bytes = [System.IO.File]::ReadAllBytes($ImagePath)
$bitmap = [SkiaSharp.SKBitmap]::Decode($bytes)

$width = $bitmap.Width
$height = $bitmap.Height

Write-Host "Scanning: $ImagePath ($width x $height)" -ForegroundColor Cyan

# Scan different Y regions
$regions = @(
    @{Name="40-50%"; StartY=0.40; EndY=0.50},
    @{Name="50-55%"; StartY=0.50; EndY=0.55},
    @{Name="55-60%"; StartY=0.55; EndY=0.60},
    @{Name="60-65%"; StartY=0.60; EndY=0.65},
    @{Name="65-70%"; StartY=0.65; EndY=0.70},
    @{Name="70-75%"; StartY=0.70; EndY=0.75}
)

foreach ($region in $regions) {
    $startY = [int]($height * $region.StartY)
    $endY = [int]($height * $region.EndY)
    $startX = [int]($width * 0.40)
    $endX = [int]($width * 0.60)

    $goldCount = 0
    $total = 0

    for ($y = $startY; $y -lt $endY; $y += 2) {
        for ($x = $startX; $x -lt $endX; $x += 2) {
            $pixel = $bitmap.GetPixel($x, $y)
            $total++
            # Gold/yellow detection
            if ($pixel.Red -gt 180 -and $pixel.Green -gt 150 -and $pixel.Blue -lt 120) {
                $goldCount++
            }
        }
    }

    $goldRatio = if ($total -gt 0) { $goldCount / $total } else { 0 }
    Write-Host "$($region.Name) (y=$startY-$endY): gold=$([math]::Round($goldRatio * 100, 2))%"
}

$bitmap.Dispose()
