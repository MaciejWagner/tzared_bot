# Copy native DLLs directly to TrainingRunner folder
$VMName = "DEV"
$sourceDir = "C:\Users\maciek\ai_experiments\tzar_bot\tools\TrainingRunner\publish\runtimes\win-x64\native"
$destDir = "C:\TzarBot\TrainingRunner"

Write-Host "=== Copying native DLLs directly to TrainingRunner ===" -ForegroundColor Cyan

# Get DLL files only (not .lib)
$files = Get-ChildItem $sourceDir -Filter "*.dll"
Write-Host "DLL files to copy: $($files.Count)"

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
