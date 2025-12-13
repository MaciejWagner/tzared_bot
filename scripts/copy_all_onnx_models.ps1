# Copy all ONNX models from generation_0 to VM DEV
# Uses Copy-VMFile (no credentials needed)

$ErrorActionPreference = "Stop"

$OnnxModelsPath = "C:\Users\maciek\ai_experiments\tzar_bot\training\generation_0\onnx"
$VMName = "DEV"
$VMDestination = "C:\TzarBot\Models\generation_0"

Write-Host "=== Copying ONNX Models to VM DEV ===" -ForegroundColor Cyan

# Get all ONNX files
$onnxFiles = Get-ChildItem "$OnnxModelsPath\*.onnx"
Write-Host "Found $($onnxFiles.Count) ONNX models to copy"
Write-Host ""

$copied = 0
$skipped = 0

foreach ($file in $onnxFiles) {
    $destPath = "$VMDestination\$($file.Name)"
    $sizeMB = [math]::Round($file.Length / 1MB, 1)

    Write-Host "[$($copied + $skipped + 1)/$($onnxFiles.Count)] $($file.Name) ($sizeMB MB)..." -NoNewline

    try {
        Copy-VMFile -Name $VMName -SourcePath $file.FullName -DestinationPath $destPath -FileSource Host -Force -CreateFullPath
        Write-Host " OK" -ForegroundColor Green
        $copied++
    }
    catch {
        Write-Host " FAILED: $($_.Exception.Message)" -ForegroundColor Red
        $skipped++
    }
}

Write-Host ""
Write-Host "=== Copy Complete ===" -ForegroundColor Cyan
Write-Host "Copied: $copied"
Write-Host "Skipped: $skipped"
