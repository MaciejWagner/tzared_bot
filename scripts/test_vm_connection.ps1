$secPw = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $secPw)
try {
    $result = Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
        return "Connected to $env:COMPUTERNAME at $(Get-Date)"
    }
    Write-Host "SUCCESS: $result" -ForegroundColor Green
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
