$ErrorActionPreference = "Continue"
$VMName = "DEV"

$password = ConvertTo-SecureString 'password123' -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential('test', $password)

$nvFolderName = "nv_dispig.inf_amd64_0afec3f2050014a0"
$srcFile = "C:\Windows\System32\DriverStore\FileRepository\$nvFolderName\nvlddmkm.sys"

Write-Host "=== Copying nvlddmkm.sys (72 MB) ===" -ForegroundColor Cyan
Write-Host "This may take a minute..." -ForegroundColor Yellow

$tempDest = "C:\Temp\nvlddmkm.sys"

try {
    Copy-VMFile -VMName $VMName -SourcePath $srcFile -DestinationPath $tempDest -CreateFullPath -FileSource Host -Force
    Write-Host "Copy to temp successful!" -ForegroundColor Green

    # Move to HostDriverStore
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        param($folderName)

        $src = "C:\Temp\nvlddmkm.sys"
        $dest = "C:\Windows\System32\HostDriverStore\FileRepository\$folderName\nvlddmkm.sys"

        if (Test-Path $src) {
            Move-Item $src -Destination $dest -Force
            Write-Host "Moved to: $dest"
            Write-Host "File exists: $(Test-Path $dest)"
        }
    } -ArgumentList $nvFolderName

    # Restart GPU
    Write-Host "`nRestarting GPU..." -ForegroundColor Yellow
    Invoke-Command -VMName $VMName -Credential $cred -ScriptBlock {
        $gpu = Get-PnpDevice | Where-Object { $_.FriendlyName -like "*NVIDIA*" }
        if ($gpu) {
            Disable-PnpDevice -InstanceId $gpu.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            Enable-PnpDevice -InstanceId $gpu.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3
        }

        Write-Host "`nGPU Status:"
        Get-PnpDevice -Class Display | Format-Table FriendlyName, Status -AutoSize
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
