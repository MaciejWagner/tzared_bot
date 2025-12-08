# Check GPU and DXGI support on VM
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "=== GPU and DXGI Info on VM DEV ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Write-Host "`n=== Video Adapter ===" -ForegroundColor Yellow
    Get-WmiObject Win32_VideoController | Select-Object Name, DriverVersion, VideoModeDescription, AdapterRAM | Format-List

    Write-Host "`n=== DXGI Factory Test ===" -ForegroundColor Yellow
    # Check if we can enumerate adapters using dxdiag
    $dxdiagPath = "$env:TEMP\dxdiag.txt"
    & dxdiag /t $dxdiagPath
    Start-Sleep -Seconds 5

    if (Test-Path $dxdiagPath) {
        $content = Get-Content $dxdiagPath
        $displaySection = $false
        foreach ($line in $content) {
            if ($line -match "Display Devices") { $displaySection = $true }
            if ($displaySection -and $line -match "^\s*$") { break }
            if ($displaySection) { Write-Host $line }
        }
    }

    Write-Host "`n=== Feature Level Support ===" -ForegroundColor Yellow
    # Check registry for D3D feature level
    $d3dKey = "HKLM:\SOFTWARE\Microsoft\Direct3D"
    if (Test-Path $d3dKey) {
        Get-ItemProperty -Path $d3dKey -ErrorAction SilentlyContinue
    } else {
        Write-Host "Direct3D registry key not found"
    }
}
