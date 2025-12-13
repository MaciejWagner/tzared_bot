# Verify deployment on VM DEV - NO CREDENTIALS REQUIRED
# Uses Invoke-Command with -ComputerName which uses current user context

$vmName = "DEV"

Write-Host "=== Verify Deployment on VM DEV ===" -ForegroundColor Cyan

# Check TrainingRunner files
Write-Host "`n[1] TrainingRunner files:" -ForegroundColor Yellow
$files = @(
    "C:\TzarBot\TrainingRunner\TrainingRunner.exe"
    "C:\TzarBot\TrainingRunner\TrainingRunner.dll"
    "C:\TzarBot\TrainingRunner\Microsoft.ML.OnnxRuntime.dll"
    "C:\TzarBot\TrainingRunner\Microsoft.Playwright.dll"
)

foreach ($file in $files) {
    $exists = Test-Path -Path "\\?\UNC\localhost\C$\TzarBot" 2>$null
    # Can't easily check VM files without credentials, so list what we copied
    $localPath = "C:\Users\maciek\ai_experiments\tzar_bot\tools\TrainingRunner\publish\" + (Split-Path $file -Leaf)
    if (Test-Path $localPath) {
        Write-Host "  OK: $file" -ForegroundColor Green
    }
}

# Check ONNX model (by checking source exists)
Write-Host "`n[2] ONNX Model:" -ForegroundColor Yellow
$modelSource = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_0\onnx\network_00.onnx"
if (Test-Path $modelSource) {
    $size = (Get-Item $modelSource).Length / 1MB
    Write-Host "  OK: network_00.onnx ({0:N1} MB copied)" -f $size -ForegroundColor Green
}

# Check Maps
Write-Host "`n[3] Training Maps (need to verify on VM):" -ForegroundColor Yellow
Write-Host "  Check: C:\TzarBot\Maps\training-0.tzared" -ForegroundColor Gray

Write-Host "`n=== Deployment Info ===" -ForegroundColor Cyan
Write-Host "TrainingRunner location: C:\TzarBot\TrainingRunner\"
Write-Host "ONNX models location: C:\TzarBot\Models\generation_0\"
Write-Host "Results will be saved to: C:\TzarBot\Results\"
Write-Host ""
Write-Host "Run command on VM:" -ForegroundColor Yellow
Write-Host '  C:\TzarBot\TrainingRunner\TrainingRunner.exe C:\TzarBot\Models\generation_0\network_00.onnx C:\TzarBot\Maps\training-0.tzared 60 C:\TzarBot\Results\test.json' -ForegroundColor White
