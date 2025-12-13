# Deploy self-contained TrainingRunner to VM DEV
$VMName = "DEV"
$sourceDir = "C:\Users\maciek\ai_experiments\tzar_bot\tools\TrainingRunner\publish-sc"
$destDir = "C:\TzarBot\TrainingRunner"

Write-Host "=== Deploying Self-Contained TrainingRunner to VM ===" -ForegroundColor Cyan

# Clear destination
$pass = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $pass)

Write-Host "Cleaning destination folder..."
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Remove-Item -Path "C:\TzarBot\TrainingRunner\*" -Recurse -Force -ErrorAction SilentlyContinue
}

# Get all files (excluding .playwright folder)
$files = Get-ChildItem $sourceDir -File
Write-Host "Files to copy: $($files.Count)"

$copied = 0
foreach ($file in $files) {
    Write-Progress -Activity "Copying files" -Status "$($file.Name)" -PercentComplete (($copied / $files.Count) * 100)

    try {
        Copy-VMFile -Name $VMName -SourcePath $file.FullName -DestinationPath "$destDir\$($file.Name)" -FileSource Host -Force -CreateFullPath
        $copied++
    }
    catch {
        Write-Host "FAILED: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Progress -Activity "Copying files" -Completed

Write-Host ""
Write-Host "Copied: $copied / $($files.Count) files" -ForegroundColor Green

# Verify
Write-Host ""
Write-Host "Verifying deployment..."
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    $count = (Get-ChildItem "C:\TzarBot\TrainingRunner" -File).Count
    Write-Host "Files on VM: $count"

    # Test if exe exists
    if (Test-Path "C:\TzarBot\TrainingRunner\TrainingRunner.exe") {
        Write-Host "TrainingRunner.exe: EXISTS" -ForegroundColor Green
    } else {
        Write-Host "TrainingRunner.exe: MISSING" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
