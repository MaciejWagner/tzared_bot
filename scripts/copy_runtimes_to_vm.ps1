# Copy runtimes (native DLLs) to VM DEV
$VMName = "DEV"
$sourceDir = "C:\Users\maciek\ai_experiments\tzar_bot\tools\TrainingRunner\publish\runtimes\win-x64\native"
$destDir = "C:\TzarBot\TrainingRunner\runtimes\win-x64\native"

Write-Host "=== Copying runtimes to VM ===" -ForegroundColor Cyan

# Get all files
$files = Get-ChildItem $sourceDir -File
Write-Host "Files to copy: $($files.Count)"

foreach ($file in $files) {
    Write-Host "  Copying $($file.Name)..." -NoNewline
    try {
        Copy-VMFile -Name $VMName -SourcePath $file.FullName -DestinationPath "$destDir\$($file.Name)" -FileSource Host -Force -CreateFullPath
        Write-Host " OK" -ForegroundColor Green
    }
    catch {
        Write-Host " FAILED: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
