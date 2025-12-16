param(
    [int]$StartNetwork = 0,
    [int]$Count = 3
)

$baseDir = "C:\Users\maciek\ai_experiments\tzar_bot"
$exe = "$baseDir\publish\TrainingRunner\TrainingRunner.exe"
$map = "$baseDir\training_maps\training-0b.tzared"
$duration = 30

$processes = @()

for ($i = $StartNetwork; $i -lt ($StartNetwork + $Count); $i++) {
    $networkNum = "{0:D2}" -f $i
    $networkPath = "$baseDir\training\generation_6\onnx\network_$networkNum.onnx"
    $outputPath = "$baseDir\temp\parallel_net$networkNum.json"

    Write-Host "Starting network_$networkNum..." -ForegroundColor Yellow

    $proc = Start-Process -FilePath $exe -ArgumentList "`"$networkPath`"", "`"$map`"", $duration, "`"$outputPath`"" -PassThru -NoNewWindow
    $processes += @{Process=$proc; Network=$networkNum; Output=$outputPath}

    Start-Sleep -Seconds 3  # Stagger start
}

Write-Host "Waiting for $($processes.Count) processes to complete..." -ForegroundColor Cyan

foreach ($p in $processes) {
    $p.Process.WaitForExit()
}

Write-Host ""
Write-Host "=== RESULTS ===" -ForegroundColor Green

foreach ($p in $processes) {
    $json = Get-Content $p.Output | ConvertFrom-Json
    $outcome = $json.Outcome
    $dur = [math]::Round($json.ActualDurationSeconds, 1)
    $actions = $json.TotalActions

    $color = if ($outcome -eq "VICTORY") { "Green" } else { "Red" }
    Write-Host "network_$($p.Network): $outcome ($dur s, $actions actions)" -ForegroundColor $color
}
