# Copy TrainingRunner files to VM DEV one by one

$publishPath = "C:\Users\maciek\ai_experiments\tzar_bot\tools\TrainingRunner\publish"
$vmName = "DEV"
$destPath = "C:\TzarBot\TrainingRunner"

Write-Host "Copying TrainingRunner files to VM DEV..."

$files = Get-ChildItem -Path $publishPath -File
$total = $files.Count
$i = 0

foreach ($file in $files) {
    $i++
    Write-Host "[$i/$total] Copying $($file.Name)..."
    Copy-VMFile -Name $vmName -SourcePath $file.FullName -DestinationPath "$destPath\$($file.Name)" -FileSource Host -Force -CreateFullPath 2>&1 | Out-Null
}

Write-Host "Done!"
