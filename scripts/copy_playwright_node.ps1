# Copy Playwright node and package folders to VM DEV
$VMName = "DEV"
$sourceDir = "C:\Users\maciek\ai_experiments\tzar_bot\tools\TrainingRunner\publish-sc\.playwright"

Write-Host "=== Copying Playwright Node to VM ===" -ForegroundColor Cyan

# Create destination directories
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "Creating destination directories..."
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    New-Item -ItemType Directory -Force -Path "C:\.playwright\node\win32_x64" | Out-Null
    New-Item -ItemType Directory -Force -Path "C:\.playwright\package\lib" | Out-Null
}

# Copy node folder files
Write-Host "Copying node files..."
$nodeFiles = Get-ChildItem "$sourceDir\node\win32_x64" -File -Recurse
$copied = 0
foreach ($file in $nodeFiles) {
    $relativePath = $file.FullName.Replace("$sourceDir\node\win32_x64\", "")
    $destPath = "C:\.playwright\node\win32_x64\$relativePath"

    try {
        Copy-VMFile -Name $VMName -SourcePath $file.FullName -DestinationPath $destPath -FileSource Host -Force -CreateFullPath
        $copied++
    }
    catch {
        Write-Host "FAILED: $relativePath" -ForegroundColor Red
    }
}
Write-Host "  Copied $copied node files"

# Copy package folder files
Write-Host "Copying package files..."
$packageFiles = Get-ChildItem "$sourceDir\package" -File -Recurse
$copied = 0
foreach ($file in $packageFiles) {
    $relativePath = $file.FullName.Replace("$sourceDir\package\", "")
    $destPath = "C:\.playwright\package\$relativePath"

    try {
        Copy-VMFile -Name $VMName -SourcePath $file.FullName -DestinationPath $destPath -FileSource Host -Force -CreateFullPath
        $copied++
    }
    catch {
        Write-Host "FAILED: $relativePath" -ForegroundColor Red
    }
}
Write-Host "  Copied $copied package files"

# Verify
Write-Host ""
Write-Host "Verifying..."
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    if (Test-Path "C:\.playwright\node\win32_x64\node.exe") {
        Write-Host "node.exe: EXISTS" -ForegroundColor Green
    }
    else {
        Write-Host "node.exe: MISSING" -ForegroundColor Red
    }

    $pkgCount = (Get-ChildItem "C:\.playwright\package" -Recurse -File -ErrorAction SilentlyContinue).Count
    Write-Host "Package files: $pkgCount"
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
