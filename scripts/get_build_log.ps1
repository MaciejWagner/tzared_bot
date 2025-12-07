$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Get-Content "C:\TzarBot-Demo\Phase1\build_2025-12-07_11-03-57.log" -ErrorAction SilentlyContinue
    if (-not (Test-Path "C:\TzarBot-Demo\Phase1\build_2025-12-07_11-03-57.log")) {
        Get-ChildItem "C:\TzarBot-Demo\Phase1\*.log" | ForEach-Object {
            Write-Host "=== $($_.Name) ==="
            Get-Content $_.FullName
        }
    }
}
