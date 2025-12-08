# Check VM environment for test readiness
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

try {
    $result = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
        Write-Host "=== .NET Version ===" -ForegroundColor Cyan
        dotnet --version

        Write-Host "`n=== TzarBot Directory ===" -ForegroundColor Cyan
        if (Test-Path "C:\TzarBot") {
            Get-ChildItem "C:\TzarBot" -Recurse -Depth 1 | Select-Object FullName | Format-Table
        } else {
            Write-Host "C:\TzarBot does not exist"
        }

        Write-Host "`n=== TzarBot.sln exists? ===" -ForegroundColor Cyan
        Test-Path "C:\TzarBot\TzarBot.sln"

        Write-Host "`n=== Available space ===" -ForegroundColor Cyan
        Get-WmiObject Win32_LogicalDisk -Filter "DeviceID='C:'" | Select-Object @{N='FreeGB';E={[math]::Round($_.FreeSpace/1GB,2)}}
    }
    Write-Host $result
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
