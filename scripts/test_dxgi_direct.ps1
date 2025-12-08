# Direct test of DXGI capabilities on VM
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Write-Host "=== Testing DXGI directly on VM ===" -ForegroundColor Cyan

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    # Create a scheduled task to run in user session
    $taskName = "DxgiTest_Temp"
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    $testScript = @'
Add-Type -AssemblyName System.Windows.Forms

$result = @{
    SessionId = [System.Diagnostics.Process]::GetCurrentProcess().SessionId
    Screens = @()
    DxgiTest = "Not tested"
}

# Get screens
[System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
    $result.Screens += "$($_.DeviceName): $($_.Bounds.Width)x$($_.Bounds.Height)"
}

# Test if we can create D3D11 device - requires Vortice
try {
    # Check if Vortice is available
    $vorticeAssembly = [System.IO.Path]::Combine("C:\TzarBot\src\TzarBot.GameInterface\bin\Debug\net8.0", "Vortice.DXGI.dll")
    if (Test-Path $vorticeAssembly) {
        $result.DxgiTest = "Vortice.DXGI.dll found"
    } else {
        $result.DxgiTest = "Vortice.DXGI.dll not found at expected path"
    }
} catch {
    $result.DxgiTest = "Error: $($_.Exception.Message)"
}

# Check video adapter info
$adapter = Get-WmiObject Win32_VideoController
$result | Add-Member -NotePropertyName "VideoAdapter" -NotePropertyValue $adapter.Name
$result | Add-Member -NotePropertyName "DriverVersion" -NotePropertyValue $adapter.DriverVersion
$result | Add-Member -NotePropertyName "VideoMode" -NotePropertyValue $adapter.VideoModeDescription

$result | ConvertTo-Json | Out-File "C:\TzarBot-Tests\dxgi_test.json"
'@

    $testScript | Out-File "C:\TzarBot-Tests\dxgi_test.ps1" -Encoding UTF8

    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument '-ExecutionPolicy Bypass -File "C:\TzarBot-Tests\dxgi_test.ps1"'
    $principal = New-ScheduledTaskPrincipal -UserId "test" -LogonType Interactive -RunLevel Highest

    $task = New-ScheduledTask -Action $action -Principal $principal
    Register-ScheduledTask -TaskName $taskName -InputObject $task | Out-Null

    Start-ScheduledTask -TaskName $taskName
    Start-Sleep -Seconds 5

    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

    if (Test-Path "C:\TzarBot-Tests\dxgi_test.json") {
        Get-Content "C:\TzarBot-Tests\dxgi_test.json"
    } else {
        Write-Host "Test did not produce output"
    }
}
