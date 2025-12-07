# Run command on VM DEV using PowerShell Direct
# Użytkownik: test, hasło: puste

$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

try {
    Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
        Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
        Set-Location "C:\TzarBot\scripts\demo"
        & ".\Setup-AndRunDemo.ps1"
    }
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "PowerShell Direct may require a non-empty password on VM" -ForegroundColor Yellow
}
