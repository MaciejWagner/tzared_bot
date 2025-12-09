# Find NVIDIA drivers on host
$driverPath = "C:\Windows\System32\DriverStore\FileRepository"
$nvFolders = Get-ChildItem $driverPath -Directory | Where-Object { $_.Name -like "nv*" } | Sort-Object LastWriteTime -Descending | Select-Object -First 5

Write-Host "=== NVIDIA Driver Folders ===" -ForegroundColor Cyan
$nvFolders | ForEach-Object {
    Write-Host "$($_.Name) - $($_.LastWriteTime)" -ForegroundColor Yellow
}

Write-Host "`n=== First folder details ===" -ForegroundColor Cyan
$first = $nvFolders | Select-Object -First 1
if ($first) {
    Write-Host "Path: $($first.FullName)"
    Write-Host "Size: $([math]::Round((Get-ChildItem $first.FullName -Recurse | Measure-Object Length -Sum).Sum / 1MB, 2)) MB"
    Write-Host "Files count: $((Get-ChildItem $first.FullName -Recurse -File).Count)"
}
