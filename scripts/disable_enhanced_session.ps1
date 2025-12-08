# Disable Enhanced Session Mode in VM guest to enable DXGI
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "=== Disabling Enhanced Session Mode in VM guest ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "Disabling Hyper-V Enhanced Session Mode service (vmicvmsession)..."

    # Stop and disable the VM Session service
    Stop-Service -Name "vmicvmsession" -Force -ErrorAction SilentlyContinue
    Set-Service -Name "vmicvmsession" -StartupType Disabled

    Write-Host "Service vmicvmsession disabled. Status:"
    Get-Service vmicvmsession | Select-Object Name, Status, StartType

    Write-Host "`nYou may need to restart the VM for changes to take effect."
    Write-Host "After restart, connect using Basic Session (not Enhanced) in Hyper-V Manager."
}

Write-Host "`n=== Restarting VM DEV ===" -ForegroundColor Yellow
Restart-VM -Name DEV -Force
Write-Host "Waiting 45 seconds for VM to boot..."
Start-Sleep -Seconds 45

# Check status
$vmState = (Get-VM -Name DEV).State
Write-Host "VM State: $vmState" -ForegroundColor $(if ($vmState -eq 'Running') { 'Green' } else { 'Red' })
