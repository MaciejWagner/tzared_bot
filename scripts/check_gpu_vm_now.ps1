# Check GPU status in VM DEV
$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

Write-Host "=== Checking GPU in VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName DEV -Credential $cred -ScriptBlock {
    Write-Host "`n=== Video Controllers ===" -ForegroundColor Yellow
    Get-WmiObject Win32_VideoController | Format-Table Name, DriverVersion, Status -AutoSize

    Write-Host "`n=== Display Adapters (Device Manager) ===" -ForegroundColor Yellow
    Get-PnpDevice -Class Display | Format-Table FriendlyName, Status, InstanceId -AutoSize

    Write-Host "`n=== DXGI Adapters ===" -ForegroundColor Yellow
    # Check if NVIDIA drivers are present
    $nvDriverPath = "C:\Windows\System32\DriverStore\FileRepository"
    if (Test-Path $nvDriverPath) {
        $nvFolders = Get-ChildItem $nvDriverPath -Directory | Where-Object { $_.Name -like "nv_*" }
        if ($nvFolders) {
            Write-Host "Found NVIDIA driver folders:" -ForegroundColor Green
            $nvFolders | ForEach-Object { Write-Host "  - $($_.Name)" }
        } else {
            Write-Host "No NVIDIA drivers found in FileRepository" -ForegroundColor Red
        }
    }

    Write-Host "`n=== nvapi64.dll exists? ===" -ForegroundColor Yellow
    if (Test-Path "C:\Windows\System32\nvapi64.dll") {
        Write-Host "nvapi64.dll: EXISTS" -ForegroundColor Green
    } else {
        Write-Host "nvapi64.dll: NOT FOUND" -ForegroundColor Red
    }
}
