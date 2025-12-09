# Copy nvapi64.dll with workaround (copy to temp first, then move inside VM)
$ErrorActionPreference = "Continue"
$VMName = "DEV"

$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Write-Host "=== Copying nvapi64.dll (workaround) ===" -ForegroundColor Cyan

# Copy to temp location first
$tempDest = "C:\Temp\nvapi64.dll"

Write-Host "Copying nvapi64.dll to C:\Temp on VM..." -ForegroundColor Yellow
try {
    Copy-VMFile -VMName $VMName -SourcePath "C:\Windows\System32\nvapi64.dll" -DestinationPath $tempDest -CreateFullPath -FileSource Host -Force
    Write-Host "Copy successful!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

# Move from temp to System32 from within VM (as admin)
Write-Host "Moving to System32 from within VM..." -ForegroundColor Yellow
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    $src = "C:\Temp\nvapi64.dll"
    $dest = "C:\Windows\System32\nvapi64.dll"

    if (Test-Path $src) {
        # Need to run as admin
        Move-Item $src -Destination $dest -Force
        Write-Host "nvapi64.dll moved to System32"
        Write-Host "Verification: $(Test-Path $dest)"
    } else {
        Write-Host "Source file not found at $src"
    }
}

# Also copy nvcuda.dll
Write-Host "`nCopying nvcuda.dll..." -ForegroundColor Yellow
try {
    Copy-VMFile -VMName $VMName -SourcePath "C:\Windows\System32\nvcuda.dll" -DestinationPath "C:\Temp\nvcuda.dll" -CreateFullPath -FileSource Host -Force
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        Move-Item "C:\Temp\nvcuda.dll" -Destination "C:\Windows\System32\nvcuda.dll" -Force
        Write-Host "nvcuda.dll: $(Test-Path 'C:\Windows\System32\nvcuda.dll')"
    }
}
catch {
    Write-Host "Warning: $_" -ForegroundColor Yellow
}

# Final verification
Write-Host "`n=== Final Check ===" -ForegroundColor Cyan
Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
    Write-Host "nvapi64.dll: $(Test-Path 'C:\Windows\System32\nvapi64.dll')"
    Write-Host "nvcuda.dll: $(Test-Path 'C:\Windows\System32\nvcuda.dll')"

    Get-PnpDevice -Class Display | Format-Table FriendlyName, Status -AutoSize
}
