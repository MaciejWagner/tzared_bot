# Copy demo results from VM DEV to host
$ErrorActionPreference = "Stop"

$password = ConvertTo-SecureString "password123" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("test", $password)

$hostResultsDir = "C:\Users\maciek\ai_experiments\tzar_bot\demo_results"
New-Item -ItemType Directory -Path $hostResultsDir -Force | Out-Null

Write-Host "Copying demo results from VM..." -ForegroundColor Yellow

# Create zip of results on VM
Invoke-Command -VMName "DEV" -Credential $cred -ScriptBlock {
    if (Test-Path "C:\TzarBot-Demo") {
        Compress-Archive -Path "C:\TzarBot-Demo\*" -DestinationPath "C:\TzarBot-Demo.zip" -Force
        Write-Host "Results zipped: C:\TzarBot-Demo.zip"
    } else {
        Write-Host "No demo results found!"
        exit 1
    }
}

# Copy-VMFile only works host->VM, need to use session for VM->host
$session = New-PSSession -VMName "DEV" -Credential $cred
Copy-Item -Path "C:\TzarBot-Demo.zip" -Destination $hostResultsDir -FromSession $session
Remove-PSSession $session

# Extract on host
Expand-Archive -Path "$hostResultsDir\TzarBot-Demo.zip" -DestinationPath $hostResultsDir -Force
Remove-Item "$hostResultsDir\TzarBot-Demo.zip" -Force

Write-Host "Results copied to: $hostResultsDir" -ForegroundColor Green
Get-ChildItem $hostResultsDir -Recurse | Select-Object FullName
