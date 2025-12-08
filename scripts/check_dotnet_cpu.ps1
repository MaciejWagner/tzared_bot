# Check CPU usage of dotnet processes on VM
$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object {
        $cpu = [math]::Round($_.CPU, 1)
        $mem = [math]::Round($_.WorkingSet64/1MB, 1)
        $runtime = (Get-Date) - $_.StartTime
        Write-Host "PID $($_.Id): CPU=$cpu s, Mem=$mem MB, Running: $([math]::Round($runtime.TotalSeconds, 0)) sec"
    }
}
